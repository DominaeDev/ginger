using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Xml;
using System.Text;
using Ginger.Properties;

namespace Ginger
{
	public static class FaradayBridge
	{
		public struct CharacterInstance
		{
			public string instanceId;		// CharacterConfig.id
			public string configId;			// CharacterConfigVersion.id
			public string groupId;			// GroupConfig.id
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

		public static IEnumerable<FolderInstance> Folders { get { return _Folders.Values; } }
		public static IEnumerable<CharacterInstance> Characters { get { return _Characters.Values; } }
		public static string DefaultModel = null;
		public static string DefaultUserConfigId = null;

		private static Dictionary<string, FolderInstance> _Folders = new Dictionary<string, FolderInstance>();	// instanceId, value
		private static Dictionary<string, CharacterInstance> _Characters = new Dictionary<string, CharacterInstance>(); // instanceId, value

		public class Link : IXmlLoadable, IXmlSaveable
		{
			public bool isActive;
			public string characterId;
			public DateTime updateDate;
			public bool isDirty;

			public bool LoadFromXml(XmlNode xmlNode)
			{
				characterId = xmlNode.GetAttribute("id", null);
				isActive = xmlNode.GetAttributeBool("active") && string.IsNullOrEmpty(characterId) == false;
				updateDate = DateTimeExtensions.FromUnixTime(xmlNode.GetAttributeLong("updated"));
				isDirty = xmlNode.GetAttributeBool("dirty");
				return true;
			}

			public void SaveToXml(XmlNode xmlNode)
			{
				xmlNode.AddAttribute("active", isActive);
				xmlNode.AddAttribute("id", characterId);
				xmlNode.AddAttribute("updated", updateDate.ToUnixTimeMilliseconds());
				if (isActive)
					xmlNode.AddAttribute("dirty", isDirty);
			}

			public void RefreshState()
			{
				if (FaradayBridge.ConnectionEstablished)
				{
					CharacterInstance character;
					if (GetCharacter(characterId, out character))
					{
						if (character.updateDate > updateDate)
							isDirty = true; // Outdated
					}
					else // Unrecognized character
					{
						isActive = false; 
					}
				}
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
			Dismissed,
			Cancelled,
		}

		public static bool ConnectionEstablished = false;

		private static SQLiteConnection CreateSQLiteConnection()
		{
			string faradayPath = AppSettings.FaradayLink.Location;
			if (string.IsNullOrWhiteSpace(faradayPath))
				faradayPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
				"faraday-canary"); // Use canary database during development and testing
			string faradayDatabase = Path.Combine(faradayPath, "db.sqlite");
			if (File.Exists(faradayDatabase) == false)
				throw new FileNotFoundException();

			AppSettings.FaradayLink.Location = faradayPath;
			return new SQLiteConnection($"Data Source={faradayDatabase}; Version=3; Foreign Keys=True; Pooled=True;");
		}

		#region Establish Link
		// Validation table
		private static string[][] s_TableValidation = new string[][] {
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

		public static Error EstablishConnection()
		{
			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					var result = new List<CharacterInstance>();

					// Check table names and columns
					for (int i = 0; i < s_TableValidation.Length; ++i)
					{
						string[] table = s_TableValidation[i];
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

					// Read settings
					using (var cmdSettings = connection.CreateCommand())
					{
						cmdSettings.CommandText = @"SELECT model FROM AppSettings";

						using (var reader = cmdSettings.ExecuteReader())
						{
							if (reader.Read())
							{
								DefaultModel = reader[0] as string;
							}
							else
							{
								DefaultModel = null;
							}
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
		}

		public static void Disconnect()
		{
			_Folders.Clear();
			_Characters.Clear();
			ConnectionEstablished = false;
			AppSettings.FaradayLink.Enabled = false;
			SQLiteConnection.ClearAllPools(); // Releases the lock on the db file
		}
		#endregion

		#region Character information
		public static bool GetCharacter(string characterId, out CharacterInstance character)
		{
			return _Characters.TryGetValue(characterId, out character);
		}

		public static bool HasCharacter(string characterId)
		{
			return _Characters.ContainsKey(characterId);
		}

		public static Error RefreshCharacters()
		{
			if (ConnectionEstablished == false)
				return Error.NotConnected;

			_Folders.Clear();
			_Characters.Clear();

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					// Fetch character instance ids
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

								_Characters.TryAdd(instanceId, 
									new CharacterInstance() {
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

								_Folders.TryAdd(instanceId, 
									new FolderInstance() {
										instanceId = instanceId,
										parentId = parentId,
										name = name,
										isRoot = isRoot,
									});
							}
						}
					}

					connection.Close();
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
		}
		#endregion

		#region Import character

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
					{
						var filename = imageUrls[0];
						if (filename.BeginsWith("http")) // Remote URL
							filename = Path.Combine(AppSettings.FaradayLink.Location, "images", Path.GetFileName(imageUrls[0]));

						Utility.LoadImageFile(filename, out portraitImage);
					}
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
		}

		#endregion

		#region Update character
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
		}

		public static Error UpdateCharacter(FaradayCardV4 card, Link linkInfo, out DateTime updateDate)
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
			int hash = characterId.GetHashCode();
			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					string configId = null;
					string groupId = null;
					string chatId = null;

					// Get row ids
					using (var cmdGetIds = connection.CreateCommand())
					{
						cmdGetIds.CommandText =
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
						cmdGetIds.Parameters.AddWithValue("$1", characterId);

						using (var reader = cmdGetIds.ExecuteReader())
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

					// Get lore items
					List<string> existingLoreItems = new List<string>();
					using (var cmdLore = connection.CreateCommand())
					{
						cmdLore.CommandText =
						@"
							SELECT 
								id
							FROM AppCharacterLorebookItem AS A
							WHERE A.id IN (
								SELECT A
								FROM _AppCharacterLorebookItemToCharacterConfigVersion
								WHERE B = $1
							);
						";
						cmdLore.Parameters.AddWithValue("$1", configId);

						using (var reader = cmdLore.ExecuteReader())
						{
							while (reader.Read())
								existingLoreItems.Add(reader.GetString(0));
						}
					}

					DateTime now = DateTime.Now;
					long updatedAt = now.ToUnixTimeMilliseconds();

					// Write to database
					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							int updates = 0;
							int expectedUpdates = 0;
							
							// Update character data

							using (var cmdUpdate = new SQLiteCommand(connection))
							{
								var sbCommand = new StringBuilder();
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

								expectedUpdates += 2;

								updates += cmdUpdate.ExecuteNonQuery();
							}

							// Lorebook
							if (card.data.loreItems.Length > 0 && card.data.loreItems.Length == existingLoreItems.Count)
							{
								// If there's an identical number of lore items in the DB already, only update the values
								using (var cmdLore = new SQLiteCommand(connection))
								{
									var sbCommand = new StringBuilder();
									for (int i = 0; i < card.data.loreItems.Length; ++i)
									{
										sbCommand.AppendFormat(
										@"
											UPDATE AppCharacterLorebookItem
											SET 
												createdAt = $timestamp,
												updatedAt = $timestamp,
												""order"" = $order{0:000},
												key = $key{0:000},
												value = $value{0:000}
											WHERE id = $id{0:000};
										", i);
										cmdLore.Parameters.AddWithValue(string.Format("$key{0:000}", i), card.data.loreItems[i].key);
										cmdLore.Parameters.AddWithValue(string.Format("$value{0:000}", i), card.data.loreItems[i].value);
										cmdLore.Parameters.AddWithValue(string.Format("$id{0:000}", i), existingLoreItems[i]);
										cmdLore.Parameters.AddWithValue(string.Format("$order{0:000}", i), MakeLoreSortPosition(i, card.data.loreItems.Length - 1, hash));
									}
									cmdLore.CommandText = sbCommand.ToString();
									cmdLore.Parameters.AddWithValue("$timestamp", updatedAt);

									expectedUpdates += card.data.loreItems.Length;
									updates += cmdLore.ExecuteNonQuery();
								}
							}
							else // Otherwise, if the count between existing and new lore items differs, do a full rewrite
							{
								// Delete old lore
								if (existingLoreItems.Count > 0)
								{
									using (var cmdDeleteLore = new SQLiteCommand(connection))
									{
										var sbCommand = new StringBuilder();
										sbCommand.AppendLine(
										@"
											DELETE FROM _AppCharacterLorebookItemToCharacterConfigVersion
											WHERE B = $1;
										");
										sbCommand.AppendLine(
										@"
											DELETE FROM AppCharacterLorebookItem
											WHERE id IN (
										");
										for (int i = 0; i < existingLoreItems.Count; ++i)
										{
											if (i > 0)
												sbCommand.Append(", ");
											sbCommand.AppendFormat("'{0}'", existingLoreItems[i]);
										}
										sbCommand.AppendLine(");");
										
										cmdDeleteLore.CommandText = sbCommand.ToString();
										cmdDeleteLore.Parameters.AddWithValue("$1", configId);

										expectedUpdates += existingLoreItems.Count * 2;
										updates += cmdDeleteLore.ExecuteNonQuery();
									}
								}

								// Insert new lore
								if (card.data.loreItems.Length > 0)
								{
									// Generate unique IDs
									var uids = new string[card.data.loreItems.Length];
									for (int i = 0; i < uids.Length; ++i)
										uids[i] = Cuid.NewCuid();

									using (var cmdInsertLore = new SQLiteCommand(connection))
									{
										var sbCommand = new StringBuilder();
										sbCommand.AppendLine(
										@"
											INSERT into AppCharacterLorebookItem (id, createdAt, updatedAt, ""order"", key, value)
											VALUES ");
										
										for (int i = 0; i < card.data.loreItems.Length; ++i)
										{
											if (i > 0)
												sbCommand.Append(",\n");
											sbCommand.AppendFormat("($id{0:000}, $timestamp, $timestamp, $order{0:000}, $key{0:000}, $value{0:000})", i);

											cmdInsertLore.Parameters.AddWithValue(string.Format("$id{0:000}", i), uids[i]);
											cmdInsertLore.Parameters.AddWithValue(string.Format("$key{0:000}", i), card.data.loreItems[i].key);
											cmdInsertLore.Parameters.AddWithValue(string.Format("$value{0:000}", i), card.data.loreItems[i].value);
											cmdInsertLore.Parameters.AddWithValue(string.Format("$order{0:000}", i), MakeLoreSortPosition(i, card.data.loreItems.Length - 1, hash));
										}
										sbCommand.Append(";");
										cmdInsertLore.CommandText = sbCommand.ToString();
										cmdInsertLore.Parameters.AddWithValue("$timestamp", updatedAt);

										expectedUpdates += card.data.loreItems.Length;
										updates += cmdInsertLore.ExecuteNonQuery();
									}

									using (var cmdLoreRef = new SQLiteCommand(connection))
									{
										var sbCommand = new StringBuilder();
										sbCommand.AppendLine(
										@"
											INSERT into _AppCharacterLorebookItemToCharacterConfigVersion (A, B)
											VALUES ");
										
										for (int i = 0; i < uids.Length; ++i)
										{
											if (i > 0)
												sbCommand.Append(",\n");
											sbCommand.AppendFormat("($id{0:000}, $configId)", i);

											cmdLoreRef.Parameters.AddWithValue(string.Format("$id{0:000}", i), uids[i]);
										}
										sbCommand.Append(";");
										cmdLoreRef.CommandText = sbCommand.ToString();
										cmdLoreRef.Parameters.AddWithValue("$configId", configId);
										cmdLoreRef.Parameters.AddWithValue("$timestamp", updatedAt);

										expectedUpdates += uids.Length;
										updates += cmdLoreRef.ExecuteNonQuery();
									}

								}
							}

							if (updates != expectedUpdates)
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
		}
		#endregion

		#region Save new character
		public static Error CreateNewCharacter(FaradayCardV4 card, out CharacterInstance character)
		{
			if (card == null)
			{
				character = default(CharacterInstance);
				return Error.NoDataFound;
			}

			if (ConnectionEstablished == false)
			{
				character = default(CharacterInstance);
				return Error.NotConnected;
			}

			// Get root folder
			var rootFolder = _Folders.Values.FirstOrDefault(f => f.isRoot);
			if (rootFolder.isRoot == false || string.IsNullOrEmpty(rootFolder.instanceId))
			{
				character = default(CharacterInstance);
				return Error.Unknown;
			}

			// Prepare image information
			bool bCanSaveImage = true;
			int portraitWidth = 0;
			int portraitHeight = 0;
			byte[] imageBytes = null;
			string imagePath = null;

			// Ensure images folder exists
			if (bCanSaveImage)
			{
				if (Directory.Exists(AppSettings.FaradayLink.Location) == false)
					bCanSaveImage = false; // Error
				else if (Directory.Exists(Path.Combine(AppSettings.FaradayLink.Location, "images")) == false)
				{
					try
					{
						Directory.CreateDirectory(Path.Combine(AppSettings.FaradayLink.Location, "images"));
					}
					catch (Exception e)
					{
						bCanSaveImage = false;
					}
				}

				if (bCanSaveImage)
				{
					if (Current.Card.portraitImage != null && Current.Card.portraitImage.Width > 0 && Current.Card.portraitImage.Height > 0)
					{
						Image image = Current.Card.portraitImage;
						portraitWidth = image.Width;
						portraitHeight = image.Height;
						imageBytes = Utility.ImageToMemory(image);
					}
					else
					{
						portraitWidth = 256;
						portraitHeight = 256;
						imageBytes = Resources.default_portrait;
					}
					
					imagePath = Path.Combine(AppSettings.FaradayLink.Location, "images", string.Concat(Guid.NewGuid().ToString().ToLowerInvariant(), ".png")); // Random filename
				}
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					string characterId	= Cuid.NewCuid();
					string configId		= Cuid.NewCuid();
					string chatId		= Cuid.NewCuid();
					string groupId		= Cuid.NewCuid();
					string imageId		= Cuid.NewCuid();
					string userId		= null;
					DateTime now = DateTime.Now;
					long createdAt = now.ToUnixTimeMilliseconds();
					string folderOrder = null;

					// Fetch default user
					using (var cmdUser = connection.CreateCommand())
					{ 
						cmdUser.CommandText =
						@"
							SELECT id
							FROM CharacterConfig
							WHERE isDefaultUserCharacter = 1;
						";

						userId = cmdUser.ExecuteScalar() as string;
						if (userId == null)
						{
							character = default(CharacterInstance);
							return Error.CommandFailed; // Requires default user
						}
					}

					// Fetch folder sort position
					using (var cmdFolderOrder = connection.CreateCommand())
					{ 
						cmdFolderOrder.CommandText =
						@"
							SELECT folderSortPosition
							FROM GroupConfig
							WHERE folderId = $folderId
							ORDER BY folderSortPosition ASC;
						";
						cmdFolderOrder.Parameters.AddWithValue("$folderId", rootFolder.instanceId);
						folderOrder = cmdFolderOrder.ExecuteScalar() as string;
					}

					// Write to database
					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							int updates = 0;
							int expectedUpdates = 0;
							
							using (var cmdCreate = new SQLiteCommand(connection))
							{
								var sbCommand = new StringBuilder();

								// CharacterConfig
								sbCommand.AppendLine(
								@"
									INSERT INTO CharacterConfig 
										(id, createdAt, updatedAt, 
											isUserControlled, isDefaultUserCharacter, isTemplateChar)
									VALUES 
										($charId, $timestamp, $timestamp, 
											0, 0, 0);
								");

								// CharacterConfigVersion
								sbCommand.AppendLine(
								@"
									INSERT INTO CharacterConfigVersion
										(id, createdAt, updatedAt, displayName, name, persona, characterConfigId)
									VALUES 
										($configId, $timestamp, $timestamp, $displayName, $name, $persona, $charId);
								");
								
								// GroupConfig
								sbCommand.AppendLine(
								@"
									INSERT INTO GroupConfig
										(id, createdAt, updatedAt, isNSFW, folderId, folderSortPosition,
											name)
									VALUES 
										($groupId, $timestamp, $timestamp, $isNSFW, $folderId, $folderSortPosition,
											''
										);
								");

								// _CharacterConfigToGroupConfig
								sbCommand.AppendLine(
								@"
									INSERT INTO _CharacterConfigToGroupConfig
										(A, B)
									VALUES 
										($charId, $groupId),
										($userId, $groupId);
								");

								// Chat
								sbCommand.AppendLine(
								@"
									INSERT INTO Chat
										(id, createdAt, updatedAt, context, customDialogue, canDeleteCustomDialogue, 
											modelInstructions, greetingDialogue, grammar, groupConfigId, 
											model, temperature, topP, minP, minPEnabled, topK, repeatPenalty, repeatLastN, promptTemplate,
											name, authorNote)
									VALUES 
										($chatId, $timestamp, $timestamp, $scenario, $example, $pruneExample, 
											$system, $greeting, $grammar, $groupId, 
											$model, $temperature, $topP, $minP, $minPEnabled, $topK, $repeatPenalty, $repeatLastN, $promptTemplate,
											'', '');
								");

								expectedUpdates += 6;

								// AppImage
								if (bCanSaveImage)
								{
									sbCommand.AppendLine(
									@"
										INSERT INTO AppImage
											(id, createdAt, updatedAt, imageUrl, label, ""order"", aspectRatio)
										VALUES 
											($imageId, $timestamp, $timestamp, $imageUrl, '', 0, $aspectRatio);
									");

										// _AppImageToCharacterConfigVersion
										sbCommand.AppendLine(
										@"
										INSERT INTO _AppImageToCharacterConfigVersion
											(A, B)
										VALUES 
											($imageId, $configId);
									");

									cmdCreate.Parameters.AddWithValue("$imageUrl", imagePath);
									cmdCreate.Parameters.AddWithValue("$aspectRatio", string.Format("{0}/{1}", portraitWidth, portraitHeight));

									expectedUpdates += 2;
								}

								cmdCreate.CommandText = sbCommand.ToString();
								cmdCreate.Parameters.AddWithValue("$charId", characterId);
								cmdCreate.Parameters.AddWithValue("$userId", userId);
								cmdCreate.Parameters.AddWithValue("$configId", configId);
								cmdCreate.Parameters.AddWithValue("$groupId", groupId);
								cmdCreate.Parameters.AddWithValue("$chatId", chatId);
								cmdCreate.Parameters.AddWithValue("$imageId", imageId);
								cmdCreate.Parameters.AddWithValue("$displayName", card.data.displayName);
								cmdCreate.Parameters.AddWithValue("$name", card.data.name);
								cmdCreate.Parameters.AddWithValue("$system", card.data.system);
								cmdCreate.Parameters.AddWithValue("$persona", card.data.persona);
								cmdCreate.Parameters.AddWithValue("$scenario", card.data.scenario);
								cmdCreate.Parameters.AddWithValue("$example", card.data.example);
								cmdCreate.Parameters.AddWithValue("$greeting", card.data.greeting);
								cmdCreate.Parameters.AddWithValue("$grammar", card.data.grammar ?? "");
								cmdCreate.Parameters.AddWithValue("$folderId", rootFolder.instanceId);
								cmdCreate.Parameters.AddWithValue("$folderSortPosition", MakeFolderSortPosition(folderOrder));
								cmdCreate.Parameters.AddWithValue("$isNSFW", card.data.isNSFW);
								cmdCreate.Parameters.AddWithValue("$timestamp", createdAt);
								cmdCreate.Parameters.AddWithValue("$model", DefaultModel);
								cmdCreate.Parameters.AddWithValue("$pruneExample", AppSettings.Faraday.PruneExampleChat);
								cmdCreate.Parameters.AddWithValue("$temperature", AppSettings.Faraday.Temperature);
								cmdCreate.Parameters.AddWithValue("$topP", AppSettings.Faraday.TopP);
								cmdCreate.Parameters.AddWithValue("$minP", AppSettings.Faraday.MinP);
								cmdCreate.Parameters.AddWithValue("$minPEnabled", AppSettings.Faraday.MinPEnabled);
								cmdCreate.Parameters.AddWithValue("$topK", AppSettings.Faraday.TopK);
								cmdCreate.Parameters.AddWithValue("$repeatPenalty", AppSettings.Faraday.RepeatPenalty);
								cmdCreate.Parameters.AddWithValue("$repeatLastN", AppSettings.Faraday.RepeatPenaltyTokens);
								cmdCreate.Parameters.AddWithValue("$promptTemplate", AppSettings.Faraday.GetPromptTemplateName());

								updates += cmdCreate.ExecuteNonQuery();
							}

							if (card.data.loreItems.Length > 0)
							{
								// Generate unique IDs
								var uids = new string[card.data.loreItems.Length];
								for (int i = 0; i < uids.Length; ++i)
									uids[i] = Cuid.NewCuid();

								int hash = characterId.GetHashCode();
								using (var cmdInsertLore = new SQLiteCommand(connection))
								{
									var sbCommand = new StringBuilder();
									sbCommand.AppendLine(
									@"
										INSERT into AppCharacterLorebookItem (id, createdAt, updatedAt, ""order"", key, value)
										VALUES ");
										
									for (int i = 0; i < card.data.loreItems.Length; ++i)
									{
										if (i > 0)
											sbCommand.Append(",\n");
										sbCommand.AppendFormat("($id{0:000}, $timestamp, $timestamp, $order{0:000}, $key{0:000}, $value{0:000})", i);

										cmdInsertLore.Parameters.AddWithValue(string.Format("$id{0:000}", i), uids[i]);
										cmdInsertLore.Parameters.AddWithValue(string.Format("$key{0:000}", i), card.data.loreItems[i].key);
										cmdInsertLore.Parameters.AddWithValue(string.Format("$value{0:000}", i), card.data.loreItems[i].value);
										cmdInsertLore.Parameters.AddWithValue(string.Format("$order{0:000}", i), MakeLoreSortPosition(i, card.data.loreItems.Length - 1, hash));
									}
									sbCommand.Append(";");
									cmdInsertLore.CommandText = sbCommand.ToString();
									cmdInsertLore.Parameters.AddWithValue("$timestamp", createdAt);

									expectedUpdates += card.data.loreItems.Length;
									updates += cmdInsertLore.ExecuteNonQuery();
								}

								using (var cmdLoreRef = new SQLiteCommand(connection))
								{
									var sbCommand = new StringBuilder();
									sbCommand.AppendLine(
									@"
										INSERT into _AppCharacterLorebookItemToCharacterConfigVersion (A, B)
										VALUES ");
										
									for (int i = 0; i < uids.Length; ++i)
									{
										if (i > 0)
											sbCommand.Append(",\n");
										sbCommand.AppendFormat("($id{0:000}, $configId)", i);

										cmdLoreRef.Parameters.AddWithValue(string.Format("$id{0:000}", i), uids[i]);
									}
									sbCommand.Append(";");
									cmdLoreRef.CommandText = sbCommand.ToString();
									cmdLoreRef.Parameters.AddWithValue("$configId", configId);
									cmdLoreRef.Parameters.AddWithValue("$timestamp", createdAt);

									expectedUpdates += uids.Length;
									updates += cmdLoreRef.ExecuteNonQuery();
								}

							}
							
							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								character = default(CharacterInstance);
								return Error.CommandFailed;
							}

							// Write image to file
							if (bCanSaveImage)
							{
								try
								{
									using (FileStream fs = File.Open(imagePath, FileMode.CreateNew, FileAccess.Write))
									{
										fs.Write(imageBytes, 0, imageBytes.Length);
									}
								}
								catch
								{
									// Do nothing
								}
							}

							character = new CharacterInstance() {
								instanceId = characterId,
								configId = configId,
								groupId = groupId,
								displayName = card.data.displayName,
								name = card.data.name,
								creationDate = now,
								updateDate = now,
								folderId = rootFolder.instanceId,
							};

							transaction.Commit();
							return Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();

							character = default(CharacterInstance);
							return Error.CommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				Disconnect();
				character = default(CharacterInstance);
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				Disconnect();
				character = default(CharacterInstance);
				return Error.CommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				character = default(CharacterInstance);
				return Error.Unknown;
			}
		}
		#endregion

		#region Utility
		private static string MakeLoreSortPosition(int index, int maxIndex, int hash)
		{
			RandomNoise rng = new RandomNoise(hash, 0);
			const string key = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
			char[] p = new char[6];
			for (int i = 0; i < p.Length; ++i)
				p[i] = key[rng.Int(52)];
			string prefix = new string(p);

			const int @base = 26;
			int digits = (int)Math.Ceiling(Math.Log((maxIndex * 2) + 1, @base)); // Allocate as many digits as we need
			char[] n = new char[digits];
			for (int i = 0; i < digits; ++i)
				n[i] = key[0];

			int quotient = (index * 2) + 1; // Required for reordering to work
			for (int i = 0; quotient != 0 && i < digits; ++i)
			{
				n[digits - i - 1] = key[Math.Abs(quotient % @base)];
				quotient /= @base;
			}

			return string.Concat(prefix, ".", new string(n));
		}

		private static string MakeFolderSortPosition(string lastOrderKey)
		{
			RandomNoise rng = new RandomNoise();
			const string key = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
			char[] p = new char[6];
			for (int i = 0; i < p.Length; ++i)
				p[i] = key[rng.Int(52)];
			string prefix = new string(p);

			if (string.IsNullOrEmpty(lastOrderKey) == false)
			{
				// Decrement last character
				lastOrderKey = string.Concat(lastOrderKey.Substring(0, lastOrderKey.Length - 1), (char)(lastOrderKey[lastOrderKey.Length - 1] - 1));
				return string.Concat(lastOrderKey, ",", prefix, ".B");
			}
			else
			{
				return string.Concat(prefix, ".B");
			}
		}
		#endregion
	}
}
