using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Xml;
using System.Text;

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
			public DateTime updateDate;     // CharacterConfig.updatedAt
			public string creator;			// GroupConfig.hubAuthorUsername
		}

		public struct FolderInstance
		{
			public string instanceId;		// AppFolder.id
			public string parentId;         // AppFolder.parentFolderId
			public string name;				// AppFolder.name
			public bool isRoot;
		}

		public class Link : IXmlLoadable, IXmlSaveable
		{
			public bool isActive;
			public string characterId;
			public DateTime updateDate;

			public bool LoadFromXml(XmlNode xmlNode)
			{
				characterId = xmlNode.GetAttribute("id", null);
				isActive = xmlNode.GetAttributeBool("active") && string.IsNullOrEmpty(characterId) == false;
				updateDate = DateTimeExtensions.FromUnixTime(xmlNode.GetAttributeLong("updated"));
				return true;
			}

			public void SaveToXml(XmlNode xmlNode)
			{
				xmlNode.AddAttribute("active", isActive);
				xmlNode.AddAttribute("id", characterId);
				xmlNode.AddAttribute("updated", updateDate.ToUnixTimeMilliseconds());
			}
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
				"db.sqlite"); // Fake database during testing

			if (File.Exists(dbFilename) == false)
				throw new FileNotFoundException();

			AppSettings.FaradayLink.Location = dbFilename;
			return new SQLiteConnection($"Data Source={dbFilename}; Version=3; Foreign Keys=True");
		}

		#region Establish Link
		private static string[][] s_TableInfo = new string[][] {
			new string[] {
				"_AppCharacterLorebookItemToCharacterConfigVersion", 
				"A", "TEXT",
				"B", "TEXT",
			},
			new string[] {
				"_AppImageToCharacterConfigVersion", 
				"A", "TEXT",
				"B", "TEXT", 
			},
			new string[] {
				"_CharacterConfigToGroupConfig", 
				"A", "TEXT",
				"B", "TEXT",
			},
			new string[] {
				"AppCharacterLorebookItem", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"order", "TEXT",
				"key", "TEXT",
				"value", "TEXT",
			},	
			new string[] {
				"AppFolder", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"name", "TEXT",
				"url", "TEXT",
				"parentFolderId", "TEXT",
				"isRoot", "BOOLEAN",
			},
			new string[] {
				"AppImage", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"imageUrl", "TEXT",
				"label", "TEXT",
				"order", "INTEGER",
				"aspectRatio", "TEXT",
			},
			new string[] {
				"CharacterConfig", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"isUserControlled", "BOOLEAN",
				"isDefaultUserCharacter", "BOOLEAN",
				"isTemplateChar", "BOOLEAN",
			},
			new string[] {
				"CharacterConfigVersion", 
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
			new string[] {
				"Chat", 
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
				"greetingDialogue", "TEXT",
			},
			new string[] { 
				"GroupConfig", 
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
			new string[] { 
				"Message", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"liked", "BOOLEAN",
				"chatId", "TEXT",
				"characterConfigId", "TEXT",
			},		
			new string[] { 
				"RegenSwipe", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"activeTimestamp", "DATETIME",
				"text", "TEXT",
				"messageId", "TEXT",
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

						var foundColumns = new List<KeyValuePair<string, string>>();
						using (var cmdTable = connection.CreateCommand())
						{
							cmdTable.CommandText = $"SELECT name, type FROM pragma_table_info('{tableName}')";

							using (var reader = cmdTable.ExecuteReader())
							{
								while (reader.Read())
								{
									foundColumns.Add(new KeyValuePair<string, string>(
										reader.GetString(0),
										reader.GetString(1)));
								}
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
		#endregion

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
					var lsCharacters = new List<CharacterInstance>();
					using (var cmdCharacterData = connection.CreateCommand())
					{
						cmdCharacterData.CommandText =
						@"
							SELECT 
								A.id, 
								B.id, B.displayName, B.name, A.createdAt, E.updatedAt,
								D.folderId, D.hubAuthorUsername
							FROM CharacterConfig as A
							INNER JOIN CharacterConfigVersion AS B ON B.characterConfigId = A.id
							INNER JOIN _CharacterConfigToGroupConfig AS C ON C.A = A.id
							INNER JOIN GroupConfig AS D ON D.id = C.B
							INNER JOIN Chat AS E ON E.groupConfigId = D.id
							WHERE A.isUserControlled=0
						";
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
								string hubAuthorUsername = reader[7] as string;

								lsCharacters.Add(new CharacterInstance() {
									instanceId = instanceId,
									configId = configId,
									displayName = displayName,
									name = name,
									creationDate = createdAt,
									updateDate = updatedAt,
									folderId = folderId,
									creator = hubAuthorUsername,
								});
							}
						}
					}

					// Fetch app folders
					var lsFolders = new List<FolderInstance>();
					using (var cmdFolderData = connection.CreateCommand())
					{ 
						cmdFolderData.CommandText =
						@"
							SELECT 
								id, parentFolderId, name, isRoot
							FROM AppFolder
						";

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

					using (var cmdCharacterData = connection.CreateCommand())
					{ 
						cmdCharacterData.CommandText = 
						@"
							SELECT 
								A.id, A.createdAt, A.updatedAt,  A.displayName,  A.name,  A.persona, 
								C.context, C.customDialogue, C.modelInstructions, C.greetingDialogue, C.grammar,
								D.hubCharId, D.hubAuthorUsername
							FROM CharacterConfigVersion as A
							INNER JOIN _CharacterConfigToGroupConfig AS B 
								ON B.A = $1
							INNER JOIN Chat AS C 
								ON C.groupConfigId = B.B
							INNER JOIN GroupConfig AS D
								ON D.id = B.B
							WHERE A.id = $2
							ORDER BY C.createdAt DESC
						";
						cmdCharacterData.Parameters.AddWithValue("$1", character.instanceId);
						cmdCharacterData.Parameters.AddWithValue("$2", character.configId);

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
							string greeting = reader.GetString(9);
							string grammar = reader[10] as string;

							string hubCharId = reader[11] as string;
							string hubAuthorName = reader[12] as string;

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

							card.hubCharacterId = hubCharId;
							card.hubAuthorUsername = hubAuthorName;
						}
					}

					// Gather lorebook items
					using (var cmdLoreItems = connection.CreateCommand())
					{ 
						cmdLoreItems.CommandText = 
						@"
							SELECT 
								key, value, ""order""
							FROM AppCharacterLorebookItem
							WHERE id IN (
								SELECT A
								FROM _AppCharacterLorebookItemToCharacterConfigVersion
								WHERE B = $1
							)
							ORDER BY ""order"" ASC
						";
						cmdLoreItems.Parameters.AddWithValue("$1", character.configId);

						var entries = new List<KeyValuePair<string, FaradayCardV1.LoreBookEntry>>();
						using (var reader = cmdLoreItems.ExecuteReader())
						{
							while (reader.Read())
							{
								string key = reader.GetString(0);
								string value = reader.GetString(1);
								string order = reader.GetString(2);

								entries.Add(new KeyValuePair<string, FaradayCardV1.LoreBookEntry>(order, new FaradayCardV1.LoreBookEntry() {
									key = key, 
									value = value,
								}));
							}
						}

						if (entries.Count > 0)
						{
							card.data.loreItems = entries
								.OrderBy(kvp => kvp.Key)
								.Select(kvp => kvp.Value)
								.ToArray();
						}
					}

					// Find portrait image file
					var imageUrls = new List<string>();
					using (var cmdImageLookup = connection.CreateCommand())
					{ 
						cmdImageLookup.CommandText = 
						@"
							SELECT 
								imageUrl
							FROM AppImage
							WHERE id IN (
								SELECT A
								FROM _AppImageToCharacterConfigVersion
								WHERE B = $1
							)
							ORDER BY ""order"" ASC
						";
						cmdImageLookup.Parameters.AddWithValue("$1", character.configId);

						using (var reader = cmdImageLookup.ExecuteReader())
						{
							while (reader.Read())
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

		public static Error ConfirmSaveCharacter(FaradayCardV4 card, Link linkInfo, out bool newerChangesFound)
		{
			if (ConnectionEstablished == false)
			{
				newerChangesFound = default(bool);
				return Error.NotConnected;
			}

			if (card == null || linkInfo == null || string.IsNullOrEmpty(linkInfo.characterId))
			{
				newerChangesFound = default(bool);
				return Error.NoDataFound;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					// Fetch character config id
					using (var cmdCharacterData = connection.CreateCommand())
					{ 
						cmdCharacterData.CommandText =
						@"
							SELECT 
								A.id, B.B, C.updatedAt
							FROM CharacterConfigVersion AS A
							INNER JOIN _CharacterConfigToGroupConfig AS B 
								ON B.A = $1
							INNER JOIN Chat AS C
								ON C.groupConfigId = B.B
							WHERE A.characterConfigId = $1
						";
						cmdCharacterData.Parameters.AddWithValue("$1", linkInfo.characterId);

						using (var reader = cmdCharacterData.ExecuteReader())
						{
							if (reader.Read() == false)
							{
								newerChangesFound = default(bool);
								return Error.NoDataFound;
							}

							string configId = reader.GetString(0);
							string groupId = reader.GetString(1);
							DateTime updatedAt = DateTimeExtensions.FromUnixTime(reader.GetInt64(2));
							newerChangesFound = updatedAt > linkInfo.updateDate;

							connection.Close();
							return Error.NoError;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				newerChangesFound = default(bool);
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				newerChangesFound = default(bool);
				return Error.CommandFailed;
			}
			catch (Exception e)
			{
				newerChangesFound = default(bool);
				return Error.Unknown;
			}
			finally
			{
				SQLiteConnection.ClearAllPools(); // Releases the lock on the db file
			}
		}

		public static Error SaveCharacter(FaradayCardV4 card, Link linkInfo, out DateTime updateDate)
		{
			if (card == null || linkInfo == null || string.IsNullOrEmpty(linkInfo.characterId))
			{
				updateDate = default(DateTime);
				return Error.NoDataFound;
			}

			if (ConnectionEstablished == false)
			{
				updateDate = default(DateTime);
				return Error.NotConnected;
			}

			string characterId = linkInfo.characterId;
			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					string configId = null; // "cm08fot680013bi9m9bo2y9np";
					string groupId = null; // "cm08fot670010bi9mris37puk";
					string chatId = null; // "cm08fot680011bi9myeboe02u";

					// Get row ids
					using (var cmdRowIds = connection.CreateCommand())
					{
						cmdRowIds.CommandText =
							@"
							SELECT 
								A.id, B.B, C.id
							FROM CharacterConfigVersion AS A
							INNER JOIN _CharacterConfigToGroupConfig AS B 
								ON B.A = $1
							INNER JOIN Chat AS C
								ON C.groupConfigId = B.B
							WHERE A.characterConfigId = $1
						";
						cmdRowIds.Parameters.AddWithValue("$1", characterId);

						using (var reader = cmdRowIds.ExecuteReader())
						{
							if (reader.Read() == false)
							{
								updateDate = default(DateTime);
								return Error.NoDataFound;
							}

							configId = reader.GetString(0);
							groupId = reader.GetString(1);
							chatId = reader.GetString(2);
						}
					}

					DateTime now = DateTime.UtcNow;
					long updatedAt = now.ToUnixTimeMilliseconds();

					// Write to database
					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							int updates = 0;
							using (var cmdUpdate = new SQLiteCommand(connection))
							{
								var sbCommand = new StringBuilder();
								// Update character information
								sbCommand.AppendLine(
								@"
									UPDATE CharacterConfigVersion
									SET 
										updatedAt = $timestamp,
										displayName = $displayName,
										name = $name,
										persona = $persona
									WHERE id = $configId;
								");
								sbCommand.AppendLine(
								@"
									UPDATE Chat
									SET 
										updatedAt = $timestamp,
										context = $scenario,
										customDialogue = $example,
										modelInstructions = $system,
										grammar = $grammar,
										greetingDialogue = $greeting
									WHERE id = $chatId;
								");
								cmdUpdate.CommandText = sbCommand.ToString();
								cmdUpdate.Parameters.AddWithValue("$configId", configId);
								cmdUpdate.Parameters.AddWithValue("$groupId", groupId);
								cmdUpdate.Parameters.AddWithValue("$chatId", chatId);
								cmdUpdate.Parameters.AddWithValue("$displayName", card.data.displayName);
								cmdUpdate.Parameters.AddWithValue("$name", card.data.name);
								cmdUpdate.Parameters.AddWithValue("$system", card.data.system);
								cmdUpdate.Parameters.AddWithValue("$persona", card.data.persona);
								cmdUpdate.Parameters.AddWithValue("$scenario", card.data.scenario);
								cmdUpdate.Parameters.AddWithValue("$example", card.data.example);
								cmdUpdate.Parameters.AddWithValue("$greeting", card.data.greeting);
								cmdUpdate.Parameters.AddWithValue("$grammar", card.data.grammar);
								cmdUpdate.Parameters.AddWithValue("$timestamp", updatedAt);

								// (lore here)

								updates = cmdUpdate.ExecuteNonQuery();
							}

							if (updates != 2) // Expect exactly 2 changes
							{
								transaction.Rollback();
								updateDate = default(DateTime);
								return Error.CommandFailed;
							}

							transaction.Commit();
							updateDate = now;
							return Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();
						}
					}
					
					updateDate = default(DateTime);
					return Error.Unknown;
				}
			}
			catch (FileNotFoundException e)
			{
				Disconnect();
				updateDate = default(DateTime);
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				Disconnect();
				updateDate = default(DateTime);
				return Error.CommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				updateDate = default(DateTime);
				return Error.Unknown;
			}
			finally
			{
				SQLiteConnection.ClearAllPools(); // Releases the lock on the db file
			}
		}
	}
}
