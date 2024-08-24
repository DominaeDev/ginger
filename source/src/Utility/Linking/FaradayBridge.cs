using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;

namespace Ginger
{
	public static class FaradayBridge
	{
		public struct CharacterInstance
		{
			public string instanceId;		// CharacterConfig.id
			public string configId;			// CharacterConfigVersion.id
			public string displayName;		// CharacterConfigVersion.displayName
			public string name;				// CharacterConfigVersion.name
			public string folderId;			// CharacterConfigVersion.name
			public DateTime creationDate;	// CharacterConfig.createdAt
			public DateTime updateDate;		// CharacterConfig.updatedAt
		}

		public struct FolderInstance
		{
			public string instanceId;		// AppFolder.id
			public string parentId;         // AppFolder.parentFolderId
			public string name;				// AppFolder.name
			public bool isRoot;
		}

		public enum Error
		{
			NoError,
			UnrecognizedStructure,
			NotConnected,
			FileNotFound,
			CommandFailed,
			NoDataFound,
			Unknown,
		}

		public static bool ConnectionEstablished = false;

		private static SQLiteConnection CreateSQLiteConnection()
		{
			string dbFilename = AppSettings.FaradayLink.Location;
			if (string.IsNullOrWhiteSpace(dbFilename))
				dbFilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
				"faraday-canary", 
				"db.ginger.sqlite"); // Fake database during testing

			if (File.Exists(dbFilename) == false)
				throw new FileNotFoundException();

			AppSettings.FaradayLink.Location = dbFilename;
			return new SQLiteConnection($"Data Source={dbFilename}; Version=3;Foreign Keys=True;Mode=ReadWrite;Pooling=False;");
		}

		private static string[][] s_TableInfo = new string[][] {
			new string[] {"_AppCharacterLorebookItemToCharacterConfigVersion", 
				"A", "TEXT",
				"B", "TEXT",
			},
			new string[] {"_AppImageToCharacterConfigVersion", 
				"A", "TEXT",
				"B", "TEXT", 
			},
			new string[] {"_CharacterConfigToGroupConfig", 
				"A", "TEXT",
				"B", "TEXT",
			},
			new string[] {"AppCharacterLorebookItem", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"order", "TEXT",
				"key", "TEXT",
				"value", "TEXT",
			},
			new string[] {"AppImage", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"imageUrl", "TEXT",
				"label", "TEXT",
				"order", "INTEGER",
				"aspectRatio", "TEXT",
			},
			new string[] {"CharacterConfig", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"isUserControlled", "BOOLEAN",
				"isDefaultUserCharacter", "BOOLEAN",
				"isTemplateChar", "BOOLEAN",
			},
			new string[] {"CharacterConfigVersion", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"displayName", "TEXT",
				"name", "TEXT",
				"persona", "TEXT",
				"ttsVoice", "TEXT",
				"ttsSpeed", "REAL",
				"characterConfigId", "TEXT",
			},
			new string[] {"Chat", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"name", "TEXT",
				"context", "TEXT",
				"customDialogue", "TEXT",
				"canDeleteCustomDialogue", "BOOLEAN",
				"useForTelemetry", "BOOLEAN",
				"authorNote", "TEXT",
				"model", "TEXT",
				"modelInstructions", "TEXT",
				"temperature", "REAL",
				"topP", "REAL",
				"minP", "REAL",
				"minPEnabled", "BOOLEAN",
				"topK", "INTEGER",
				"repeatPenalty", "REAL",
				"repeatLastN", "INTEGER",
				"grammar", "TEXT",
				"promptTemplate", "TEXT",
				"ttsAutoPlay", "BOOLEAN",
				"ttsInputFilter", "TEXT",
				"groupConfigId", "TEXT",
			},
			new string[] {"GroupConfig", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"hubCharId", "TEXT",
				"hubAuthorId", "TEXT",
				"hubAuthorUsername", "TEXT",
				"hubCharIdAnalytics", "TEXT",
				"forkedFromLocalId", "TEXT",
				"name", "TEXT",
				"isNSFW", "BOOLEAN",
				"folderId", "TEXT",
				"folderSortPosition", "TEXT",
			},
		};

		public static Error EstablishLink()
		{
			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					var result = new List<CharacterInstance>();

					// Check table names and columns
					for (int i = 0; i < s_TableInfo.Length; ++i)
					{
						string[] table = s_TableInfo[i];
						string[] expectedNames = new string[(table.Length - 1)/2];
						string[] expectedTypes = new string[(table.Length - 1)/2];
						string tableName = table[0];
						for (int j = 0; j < expectedNames.Length; ++j)
						{
							expectedNames[j] = table[j * 2 + 1];
							expectedTypes[j] = table[j * 2 + 2];
						}

						var cmdTable = connection.CreateCommand();
						cmdTable.CommandText = $"SELECT name, type FROM pragma_table_info('{tableName}')";
						
						var foundColumns = new List<KeyValuePair<string, string>>();
						using (var reader = cmdTable.ExecuteReader())
						{
							while (reader.Read())
							{
								foundColumns.Add(new KeyValuePair<string, string>(
									reader.GetString(0),
									reader.GetString(1)));
							}
						}

						if (foundColumns.Count < expectedNames.Length)
							return Error.UnrecognizedStructure;

						for (int j = 0; j < expectedNames.Length; ++j)
						{
							if (foundColumns.FindIndex(kvp => kvp.Key == expectedNames[j] && kvp.Value == expectedTypes[j]) == -1)
								return Error.UnrecognizedStructure;
						}
					}

					connection.Close();
					ConnectionEstablished = true;
					return Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				Disconnect();
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				Disconnect();
				return Error.CommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				return Error.Unknown;
			}
			finally
			{
				SQLiteConnection.ClearAllPools(); // Releases the lock on the db file
			}
		}

		public static void Disconnect()
		{
			ConnectionEstablished = false;
			AppSettings.FaradayLink.Enabled = false;
			SQLiteConnection.ClearAllPools(); // Releases the lock on the db file
		}

		public static Error GetCharacters(out CharacterInstance[] characters, out FolderInstance[] folders)
		{
			if (ConnectionEstablished == false)
			{
				characters = null;
				folders = null;
				return Error.NotConnected;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					// Fetch character instance ids
					var cmdCharacterData = connection.CreateCommand();
					cmdCharacterData.CommandText =
						@"
						SELECT 
							A.id, 
							B.id, B.displayName, B.name, B.createdAt, B.updatedAt,
							D.folderId
						FROM CharacterConfig as A
						INNER JOIN CharacterConfigVersion AS B ON B.characterConfigId = A.id
						INNER JOIN _CharacterConfigToGroupConfig AS C ON C.A = A.id
						INNER JOIN GroupConfig AS D ON D.id = C.B
						WHERE A.isUserControlled=0";

					var lsCharacters = new List<CharacterInstance>();
					using (var reader = cmdCharacterData.ExecuteReader())
					{
						while (reader.Read())
						{
							string instanceId = reader.GetString(0);
							string configId = reader.GetString(1);
							if (string.IsNullOrEmpty(instanceId) || string.IsNullOrEmpty(configId))
								continue;

							string displayName = reader.GetString(2);
							string name = reader.GetString(3);
							DateTime createdAt = DateTimeExtensions.FromUnixTime(reader.GetInt64(4));
							DateTime updatedAt = DateTimeExtensions.FromUnixTime(reader.GetInt64(5));
							string folderId = reader.GetString(6);

							lsCharacters.Add(new CharacterInstance() {
									instanceId = instanceId,
									configId = configId,
									displayName = displayName,
									name = name,
									creationDate = createdAt,
									updateDate = updatedAt,
									folderId = folderId,
								});
						}
					}

					// App folders
					// Fetch character instance ids
					var cmdFolderData = connection.CreateCommand();
					cmdFolderData.CommandText =
						@"
						SELECT 
							id, parentFolderId, name, isRoot
						FROM AppFolder";

					var lsFolders = new List<FolderInstance>();
					using (var reader = cmdFolderData.ExecuteReader())
					{
						while (reader.Read())
						{
							string instanceId = reader.GetString(0);
							string parentId = reader[1] as string;
							string name = reader.GetString(2);
							bool isRoot = reader.GetBoolean(3);

							lsFolders.Add(new FolderInstance() {
								instanceId = instanceId,
								parentId = parentId,
								name = name,
								isRoot = isRoot,
							});
						}
					}

					connection.Close();
					characters = lsCharacters.ToArray();
					folders = lsFolders.OrderBy(f => f.name).ToArray();
					return Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				Disconnect();
				characters = null;
				folders = null;
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				Disconnect();
				characters = null;
				folders = null;
				return Error.CommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				characters = null;
				folders = null;
				return Error.Unknown;
			}
			finally
			{
				SQLiteConnection.ClearAllPools(); // Releases the lock on the db file
			}
		}


		public static Error ImportCharacter(CharacterInstance character, out FaradayCardV4 card, out Image portraitImage)
		{
			if (ConnectionEstablished == false)
			{
				card = null;
				portraitImage = null;
				return Error.NotConnected;
			}
			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					var result = new List<CharacterInstance>();

					// Fetch character instance ids
					var cmdCharacterData = connection.CreateCommand();
					cmdCharacterData.CommandText = @"
						SELECT 
							A.id, A.createdAt, A.updatedAt,  A.displayName,  A.name,  A.persona, 
							C.context, C.customDialogue, C.modelInstructions, C.grammar, C.id
						FROM CharacterConfigVersion as A
						INNER JOIN _CharacterConfigToGroupConfig AS B 
							ON B.A = $1
						INNER JOIN Chat AS C 
							ON C.groupConfigId = B.B
						WHERE A.id = $2
						ORDER BY C.createdAt DESC";
					cmdCharacterData.Parameters.AddWithValue("$1", character.instanceId);
					cmdCharacterData.Parameters.AddWithValue("$2", character.configId);

					card = null;
					string chatId = null;

					var characterInstanceIds = new HashSet<string>();
					using (var reader = cmdCharacterData.ExecuteReader())
					{
						if (!reader.Read())
						{
							portraitImage = null;
							return Error.NoDataFound;
						}

						string instanceId = reader.GetString(0);
						DateTime createdAt = DateTimeExtensions.FromUnixTime(reader.GetInt64(1));
						DateTime updatedAt = DateTimeExtensions.FromUnixTime(reader.GetInt64(2));
						string displayName = reader.GetString(3);
						string name = reader.GetString(4);
						string persona = reader.GetString(5);
						string scenario = reader.GetString(6);
						string example = reader.GetString(7);
						string system = reader.GetString(8);
						string grammar = reader[9] as string;
						chatId = reader.GetString(10);

						card = new FaradayCardV4();
						card.data.displayName = displayName;
						card.data.name = name;
						card.data.system = system;
						card.data.persona = persona;
						card.data.scenario = scenario;
						card.data.example = example;
						card.data.grammar = grammar;
						card.data.creationDate = createdAt.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
						card.data.updateDate = updatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
					}

					// Find first message
					if (chatId != null && card != null)
					{
						var cmdGreeting = connection.CreateCommand();
						cmdGreeting.CommandText = @"
						SELECT 
							R.text
						FROM Message as M
						INNER JOIN RegenSwipe as R on R.messageId = M.id
						WHERE M.isFirstMessage = True AND M.chatId = $1 AND M.characterConfigId = $2";
						cmdGreeting.Parameters.AddWithValue("$1", chatId);
						cmdGreeting.Parameters.AddWithValue("$2", character.instanceId);

						using (var reader = cmdGreeting.ExecuteReader())
						{
							if (reader.Read())
								card.data.greeting = reader.GetString(0);
						}
					}

					// Find portrait image file
					var cmdImageLookup = connection.CreateCommand();
					cmdImageLookup.CommandText = @"
						SELECT 
							imageUrl
						FROM AppImage
						WHERE id IN (
							SELECT A
							FROM _AppImageToCharacterConfigVersion
							WHERE B = $1
						)
						ORDER BY 'order' ASC";
					cmdImageLookup.Parameters.AddWithValue("$1", character.configId);

					var imageUrls = new List<string>();
					using (var reader = cmdImageLookup.ExecuteReader())
					{
						while (reader.Read())
							imageUrls.Add(reader.GetString(0));
					}

					if (imageUrls.Count > 0)
						Utility.LoadImageFile(imageUrls[0], out portraitImage);
					else
						portraitImage = null;

					connection.Close();
					return card == null ? Error.NoDataFound : Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				Disconnect();
				card = null;
				portraitImage = null;
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				Disconnect();
				card = null;
				portraitImage = null;
				return Error.CommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				card = null;
				portraitImage = null;
				return Error.Unknown;
			}
			finally
			{
				SQLiteConnection.ClearAllPools(); // Releases the lock on the db file
			}

		}
	}
}
