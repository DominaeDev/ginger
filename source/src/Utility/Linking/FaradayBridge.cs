using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.IO;
using System.Drawing;

namespace Ginger
{
	public static class FaradayBridge
	{
		public struct CharacterInstance
		{
			public string instanceID;	// CharacterConfig
			public string configID;		// CharacterConfigVersion
			public string displayName;
			public string name;
			public DateTime creationDate;
			public DateTime updateDate;
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

		private static SQLiteConnection ConnectToDB()
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

		public static Error GetCharacters(out CharacterInstance[] characters)
		{
			if (ConnectionEstablished == false)
			{
				characters = null;
				return Error.NotConnected;
			}

			try
			{
				using (var connection = ConnectToDB())
				{
					connection.Open();

					var result = new List<CharacterInstance>();

					// Fetch character instance ids
					var cmdReadCharacterIds = connection.CreateCommand();
					cmdReadCharacterIds.CommandText =
						@"
						SELECT 
							A.id, 
							B.id, 
							B.displayName, 
							B.name, 
							B.createdAt, 
							B.updatedAt 
						FROM CharacterConfig as A
						INNER JOIN CharacterConfigVersion AS B ON B.characterConfigId = A.id
						WHERE isUserControlled=0";

					var characterInstanceIds = new HashSet<string>();
					using (var reader = cmdReadCharacterIds.ExecuteReader())
					{
						while (reader.Read())
						{
							string instanceId = reader.GetString(0);
							string configId = reader.GetString(1);
							string displayName = reader.GetString(2);
							string name = reader.GetString(3);
							DateTime createdAt = DateTimeExtensions.FromUnixTime(reader.GetInt64(4));
							DateTime updatedAt = DateTimeExtensions.FromUnixTime(reader.GetInt64(5));
							if (string.IsNullOrEmpty(instanceId) || string.IsNullOrEmpty(configId))
								continue;

							result.Add(new CharacterInstance() {
									instanceID = instanceId,
									configID = configId,
									displayName = displayName,
									name = name,
									creationDate = createdAt,
									updateDate = updatedAt,
								});
						}
					}

					connection.Close();
					characters = result.ToArray();
					return Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				Disconnect();
				characters = null;
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				Disconnect();
				characters = null;
				return Error.CommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				characters = null;
				return Error.Unknown;
			}
			finally
			{
				SQLiteConnection.ClearAllPools(); // Releases the lock on the db file
			}
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
				using (var connection = ConnectToDB())
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
				using (var connection = ConnectToDB())
				{
					connection.Open();

					var result = new List<CharacterInstance>();

					// Fetch character instance ids
					var cmdCharacterData = connection.CreateCommand();
					cmdCharacterData.CommandText = @"
						SELECT 
							A.id,
							A.createdAt, 
							A.updatedAt, 
							A.displayName, 
							A.name, 
							A.persona, 
							C.context,
							C.customDialogue,
							C.modelInstructions,
							C.grammar
						FROM CharacterConfigVersion as A
						INNER JOIN _CharacterConfigToGroupConfig AS B 
							ON B.A = $1
						INNER JOIN Chat AS C 
							ON C.groupConfigId = B.B
						WHERE A.id = $2;";
					cmdCharacterData.Parameters.AddWithValue("$1", character.instanceID);
					cmdCharacterData.Parameters.AddWithValue("$2", character.configID);

					card = null;

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
						string grammar = reader.GetString(9);

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

					// Find image
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
					cmdImageLookup.Parameters.AddWithValue("$1", character.configID);
					var imageUrls = new List<string>();
					using (var reader = cmdImageLookup.ExecuteReader())
					{
						while (reader.Read())
						{
							imageUrls.Add(reader.GetString(0));
						}
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
