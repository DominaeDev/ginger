﻿using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Text;
using Ginger.Properties;

namespace Ginger
{
	public static class BackyardBridge
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
			public string creator;          // GroupConfig.hubAuthorUsername
		}

		public struct FolderInstance
		{
			public string instanceId;		// AppFolder.id
			public string parentId;         // AppFolder.parentFolderId
			public string name;				// AppFolder.name
			public bool isRoot;
		}

		public class ImageInstance
		{
			public string instanceId;		// AppImage.id
			public string imageUrl;			// AppImage.imageUrl
			public string label;			// AppImage.label
			public int width;				// AppImage.aspectRatio
			public int height;				// AppImage.aspectRatio
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

			public struct Image
			{
				public string uid;
				public string filename;
			}

			public Image[] imageLinks;

			public bool LoadFromXml(XmlNode xmlNode)
			{
				characterId = xmlNode.GetAttribute("id", null);
				isActive = xmlNode.GetAttributeBool("active") && string.IsNullOrEmpty(characterId) == false;
				updateDate = DateTimeExtensions.FromUnixTime(xmlNode.GetAttributeLong("updated"));
				isDirty = xmlNode.GetAttributeBool("dirty");

				var imageNode = xmlNode.GetFirstElement("Image");
				if (imageNode != null)
				{
					var lsImages = new List<Image>();

					while (imageNode != null)
					{
						string uid = imageNode.GetAttribute("id", null);
						string imageUrl = imageNode.GetTextValue();
						if (string.IsNullOrEmpty(uid) == false && string.IsNullOrEmpty(imageUrl) == false)
						{
							lsImages.Add(new Image() {
								uid = uid,
								filename = imageUrl,
							});
						}
						imageNode = imageNode.GetNextSibling();
					}
					imageLinks = lsImages.ToArray();
				}
				return characterId != null;
			}

			public void SaveToXml(XmlNode xmlNode)
			{
				xmlNode.AddAttribute("id", characterId);
				xmlNode.AddAttribute("active", isActive);
				xmlNode.AddAttribute("updated", updateDate.ToUnixTimeMilliseconds());
				if (isActive)
					xmlNode.AddAttribute("dirty", isDirty);

				if (imageLinks != null)
				{
					foreach (var image in imageLinks)
					{
						var imageNode = xmlNode.AddElement("Image");
						imageNode.AddAttribute("id", image.uid);
						imageNode.AddTextValue(image.filename);
					}
				}

			}

			public void RefreshState()
			{
				if (BackyardBridge.ConnectionEstablished)
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
			string faradayPath = AppSettings.BackyardLink.Location;
			if (string.IsNullOrWhiteSpace(faradayPath))
				faradayPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
				"faraday-canary"); // Use canary database during development and testing
			string faradayDatabase = Path.Combine(faradayPath, "db.sqlite");
			if (File.Exists(faradayDatabase) == false)
				throw new FileNotFoundException();

			AppSettings.BackyardLink.Location = faradayPath;
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
			AppSettings.BackyardLink.Enabled = false;
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

					// Fetch character images
					var characterImages = new Dictionary<string, ImageInstance>();
					using (var cmdImageData = connection.CreateCommand())
					{
						cmdImageData.CommandText =
						@"
							SELECT 
								id, imageUrl, label, aspectRatio
							FROM AppImage
							ORDER BY ""order"" ASC;
						";
						using (var reader = cmdImageData.ExecuteReader())
						{
							while (reader.Read())
							{
								string instanceId = reader.GetString(0);
								string imageUrl = reader.GetString(1);
								string label = reader[2] as string;
								string aspectRatio = reader[3] as string;
								int width, height;

								if (string.IsNullOrEmpty(aspectRatio) == false)
								{
									int pos_slash = aspectRatio.IndexOf('/');
									if (pos_slash != -1)
									{
										int.TryParse(aspectRatio.Substring(0, pos_slash), out width);
										int.TryParse(aspectRatio.Substring(pos_slash + 1), out height);
									}
									else
									{
										width = 0;
										height = 0;
									}
								}
								else
								{
									width = 0;
									height = 0;
								}

								characterImages.TryAdd(instanceId,
									new ImageInstance() {
										instanceId = instanceId,
										label = label,
										imageUrl = imageUrl,
										width = width,
										height = height,
									});
							}
						}
					}

					var imagesByConfigId = new Dictionary<string, List<string>>();
					using (var cmdImageRef = connection.CreateCommand())
					{
						cmdImageRef.CommandText =
						@"
							SELECT A, B
							FROM _AppImageToCharacterConfigVersion
						";
						using (var reader = cmdImageRef.ExecuteReader())
						{
							while (reader.Read())
							{
								string imageId = reader.GetString(0);
								string configId = reader.GetString(1);

								if (imagesByConfigId.ContainsKey(configId) == false)
									imagesByConfigId.Add(configId, new List<string>());

								imagesByConfigId[configId].Add(imageId);
							}
						}
					}

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

								ImageInstance[] images = null;
								if (imagesByConfigId.ContainsKey(configId))
								{
									var lsImages = new List<ImageInstance>();
									foreach (var id in imagesByConfigId[configId])
									{
										ImageInstance imageInstance;
										if (characterImages.TryGetValue(id, out imageInstance))
											lsImages.Add(imageInstance);
									}
									images = lsImages.ToArray();
								}

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

		public static Error ImportCharacter(CharacterInstance character, out FaradayCardV4 card, out string[] imageUrls)
		{
			if (ConnectionEstablished == false)
			{
				card = null;
				imageUrls = null;
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
								ON B.A = $charId
							INNER JOIN Chat AS C 
								ON C.groupConfigId = B.B
							INNER JOIN GroupConfig AS D
								ON D.id = B.B
							WHERE A.id = $configId
							ORDER BY C.createdAt DESC
						";
						cmdCharacterData.Parameters.AddWithValue("$charId", character.instanceId);
						cmdCharacterData.Parameters.AddWithValue("$configId", character.configId);

						card = null;

						var characterInstanceIds = new HashSet<string>();
						using (var reader = cmdCharacterData.ExecuteReader())
						{
							if (!reader.Read())
							{
								imageUrls = null;
								return Error.NoDataFound; // No character
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
								WHERE B = $configId
							)
							ORDER BY ""order"" ASC
						";
						cmdLoreItems.Parameters.AddWithValue("$configId", character.configId);

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
					var lsImageUrls = new List<string>();
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
								WHERE B = $configId
							)
							ORDER BY ""order"" ASC
						";
						cmdImageLookup.Parameters.AddWithValue("$configId", character.configId);

						using (var reader = cmdImageLookup.ExecuteReader())
						{
							while (reader.Read())
							{
								string filename = reader.GetString(0);
								if (filename.BeginsWith("http")) // Remote URL -> Local filename
									filename = Path.Combine(AppSettings.BackyardLink.Location, "images", Path.GetFileName(filename));
								lsImageUrls.Add(filename);
							}
						}
					}

					imageUrls = lsImageUrls.ToArray();

					connection.Close();
					return card == null ? Error.NoDataFound : Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				Disconnect();
				card = null;
				imageUrls = null;
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				Disconnect();
				card = null;
				imageUrls = null;
				return Error.CommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				card = null;
				imageUrls = null;
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
								ON B.A = $charId
							INNER JOIN Chat AS C
								ON C.groupConfigId = B.B
							WHERE A.characterConfigId = $charId
						";
						cmdCharacterData.Parameters.AddWithValue("$charId", linkInfo.characterId);

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

		public static Error UpdateCharacter(FaradayCardV4 card, Link linkInfo, out DateTime updateDate, out Link.Image[] updatedImageLinks)
		{
			if (card == null || linkInfo == null || string.IsNullOrEmpty(linkInfo.characterId))
			{
				updateDate = default(DateTime);
				updatedImageLinks = null;
				return Error.NoDataFound;
			}

			if (ConnectionEstablished == false)
			{
				updateDate = default(DateTime);
				updatedImageLinks = null;
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

					// Get row ids
					using (var cmdGetIds = connection.CreateCommand())
					{
						cmdGetIds.CommandText =
						@"
							SELECT 
								A.id, B.B
							FROM CharacterConfigVersion AS A
							INNER JOIN _CharacterConfigToGroupConfig AS B 
								ON B.A = $charId
							WHERE A.characterConfigId = $charId
						";
						cmdGetIds.Parameters.AddWithValue("$charId", characterId);

						using (var reader = cmdGetIds.ExecuteReader())
						{
							if (reader.Read() == false)
							{
								updateDate = default(DateTime);
								updatedImageLinks = null;
								return Error.NoDataFound;
							}

							configId = reader.GetString(0);
							groupId = reader.GetString(1);	// Primary group
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
								WHERE B = $configId
							);
						";
						cmdLore.Parameters.AddWithValue("$configId", configId);

						using (var reader = cmdLore.ExecuteReader())
						{
							while (reader.Read())
								existingLoreItems.Add(reader.GetString(0));
						}
					}

					// Get image ids
					List<ImageInstance> imageInstances = new List<ImageInstance>();
					using (var cmdImages = connection.CreateCommand())
					{
						cmdImages.CommandText =
						@"
							SELECT A.id, A.imageUrl, A.label, A.aspectRatio
							FROM AppImage AS A
							WHERE A.id IN (
								SELECT B.A
								FROM _AppImageToCharacterConfigVersion AS B
								WHERE B.B = $configId
							)
							ORDER BY ""order"" ASC;
						";
						cmdImages.Parameters.AddWithValue("$configId", configId);

						using (var reader = cmdImages.ExecuteReader())
						{
							while (reader.Read())
							{
								string instanceId = reader.GetString(0);
								string imageUrl = reader.GetString(1);
								string label = reader[2] as string;
								string aspectRatio = reader[3] as string;
								int width, height;

								if (string.IsNullOrEmpty(aspectRatio) == false)
								{
									int pos_slash = aspectRatio.IndexOf('/');
									if (pos_slash != -1)
									{
										int.TryParse(aspectRatio.Substring(0, pos_slash), out width);
										int.TryParse(aspectRatio.Substring(pos_slash + 1), out height);
									}
									else
									{
										width = 0;
										height = 0;
									}
								}
								else
								{
									width = 0;
									height = 0;
								}
								imageInstances.Add(new ImageInstance() {
									instanceId = instanceId,
									imageUrl = imageUrl,
									label = label,
									width = width,
									height = height,
								});
							}
						}
					}

					// Compile list of images to update / insert
					ImageOutput[] images;
					Link.Image[] imageLinks;
					bool bUpdateImages = GetImageUpdates(imageInstances, linkInfo.imageLinks, out images, out imageLinks);

					DateTime now = DateTime.Now;
					long updatedAt = now.ToUnixTimeMilliseconds();

					// Write to database
					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							int updates = 0;
							int expectedUpdates = 0;
							
							// Update character persona
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
								cmdUpdate.CommandText = sbCommand.ToString();
								cmdUpdate.Parameters.AddWithValue("$configId", configId);
								cmdUpdate.Parameters.AddWithValue("$displayName", card.data.displayName);
								cmdUpdate.Parameters.AddWithValue("$name", card.data.name);
								cmdUpdate.Parameters.AddWithValue("$persona", card.data.persona);
								cmdUpdate.Parameters.AddWithValue("$timestamp", updatedAt);

								expectedUpdates += 1;

								updates += cmdUpdate.ExecuteNonQuery();
							}

							// Update chat data
							using (var cmdChat = new SQLiteCommand(connection))
							{
								var sbCommand = new StringBuilder();
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
									WHERE groupConfigId = $groupId;
								");
								cmdChat.CommandText = sbCommand.ToString();
								cmdChat.Parameters.AddWithValue("$groupId", groupId);
								cmdChat.Parameters.AddWithValue("$system", card.data.system);
								cmdChat.Parameters.AddWithValue("$scenario", card.data.scenario);
								cmdChat.Parameters.AddWithValue("$example", card.data.example);
								cmdChat.Parameters.AddWithValue("$greeting", card.data.greeting);
								cmdChat.Parameters.AddWithValue("$grammar", card.data.grammar);
								cmdChat.Parameters.AddWithValue("$timestamp", updatedAt);

								int nChats = cmdChat.ExecuteNonQuery();
								expectedUpdates += Math.Max(nChats, 1); // Expect at least one
								updates += nChats;
							}

							// Delete images
							using (var cmdImage = new SQLiteCommand(connection))
							{
								var sbCommand = new StringBuilder();

								sbCommand.AppendLine(
								$@"
									DELETE FROM AppImage
									WHERE id IN (
										SELECT A
										FROM _AppImageToCharacterConfigVersion
										WHERE B = $configId
									);
								");
																
								sbCommand.AppendLine(
								$@"
									DELETE FROM _AppImageToCharacterConfigVersion
									WHERE B = $configId;
								");

								cmdImage.CommandText = sbCommand.ToString();
								cmdImage.Parameters.AddWithValue("$configId", configId);

								int nDeletes = cmdImage.ExecuteNonQuery();
								expectedUpdates += nDeletes;
								updates += nDeletes;
							}

							// Add images
							using (var cmdImage = new SQLiteCommand(connection))
							{
								var sbCommand = new StringBuilder();

								var sortedImageIds = new List<string>(images.Length);
								for (int i = 0; i < images.Length; ++i)
									sortedImageIds.Add(Cuid.NewCuid());
								sortedImageIds.Sort(); // Backyard bug

								// AppImage
								sbCommand.AppendLine(
								$@"
									INSERT INTO AppImage
										(id, createdAt, updatedAt, imageUrl, label, ""order"", aspectRatio)
									VALUES 
								");
								for (int i = 0; i < images.Length; ++i)
								{
									if (i > 0)
										sbCommand.Append(",\n");
									sbCommand.Append($"($imageId{i:000}, $timestamp, $timestamp, $imageUrl{i:000}, $label{i:000}, {i}, $aspectRatio{i:000})");

									cmdImage.Parameters.AddWithValue($"$imageId{i:000}", sortedImageIds[i]);
									cmdImage.Parameters.AddWithValue($"$imageUrl{i:000}", images[i].imageUrl);
									cmdImage.Parameters.AddWithValue($"$label{i:000}", images[i].label ?? "");
									if (images[i].width > 0 && images[i].height > 0)
										cmdImage.Parameters.AddWithValue($"$aspectRatio{i:000}", string.Format("{0}/{1}", images[i].width, images[i].height));
									else 
										cmdImage.Parameters.AddWithValue($"$aspectRatio{i:000}", "");

								}
								sbCommand.Append(";");

								// _AppImageToCharacterConfigVersion
								sbCommand.AppendLine(
								$@"
									INSERT INTO _AppImageToCharacterConfigVersion
										(A, B)
									VALUES 
								");

								for (int i = 0; i < images.Length; ++i)
								{
									if (i > 0)
										sbCommand.Append(",\n");
									sbCommand.Append($@"($imageId{i:000}, $configId)");
								}

								expectedUpdates += images.Length * 2;

								cmdImage.CommandText = sbCommand.ToString();
								cmdImage.Parameters.AddWithValue("$configId", configId);
								cmdImage.Parameters.AddWithValue("$timestamp", updatedAt);

								updates += cmdImage.ExecuteNonQuery();
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
										sbCommand.Append(
										$@"
											UPDATE AppCharacterLorebookItem
											SET 
												createdAt = $timestamp,
												updatedAt = $timestamp,
												""order"" = $order{i:000},
												key = $key{i:000},
												value = $value{i:000}
											WHERE id = $id{i:000};
										");
										cmdLore.Parameters.AddWithValue($"$key{i:000}", card.data.loreItems[i].key);
										cmdLore.Parameters.AddWithValue($"$value{i:000}", card.data.loreItems[i].value);
										cmdLore.Parameters.AddWithValue($"$id{i:000}", existingLoreItems[i]);
										cmdLore.Parameters.AddWithValue($"$order{i:000}", MakeLoreSortPosition(i, card.data.loreItems.Length - 1, hash));
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
											WHERE B = $configId;
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
										cmdDeleteLore.Parameters.AddWithValue("$configId", configId);

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
											INSERT into AppCharacterLorebookItem 
												(id, createdAt, updatedAt, ""order"", key, value)
											VALUES ");
										
										for (int i = 0; i < card.data.loreItems.Length; ++i)
										{
											if (i > 0)
												sbCommand.Append(",\n");
											sbCommand.Append($"($id{i:000}, $timestamp, $timestamp, $order{i:000}, $key{i:000}, $value{i:000})");

											cmdInsertLore.Parameters.AddWithValue($"$id{i:000}", uids[i]);
											cmdInsertLore.Parameters.AddWithValue($"$key{i:000}", card.data.loreItems[i].key);
											cmdInsertLore.Parameters.AddWithValue($"$value{i:000}", card.data.loreItems[i].value);
											cmdInsertLore.Parameters.AddWithValue($"$order{i:000}", MakeLoreSortPosition(i, card.data.loreItems.Length - 1, hash));
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
											sbCommand.Append($"($id{i:000}, $configId)");

											cmdLoreRef.Parameters.AddWithValue($"$id{i:000}", uids[i]);
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
								updatedImageLinks = null;
								return Error.CommandFailed;
							}

							// Write images to disk
							if (images != null && images.ContainsAny(i => i.data.isEmpty == false))
							{
								foreach (var image in images.Where(i => i.data.isEmpty == false))
								{
									try
									{
										// Ensure images folder exists
										string imagedir = Path.GetDirectoryName(image.imageUrl);
										if (Directory.Exists(imagedir) == false)
											Directory.CreateDirectory(imagedir);

										using (FileStream fs = File.Open(image.imageUrl, FileMode.CreateNew, FileAccess.Write))
										{
											fs.Write(image.data.bytes, 0, image.data.bytes.Length);
										}
									}
									catch
									{
										// Do nothing
									}
								}
							}

							
							updateDate = now;
							updatedImageLinks = imageLinks;
							
							transaction.Commit();
							return Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();
						}
					}
					
					updateDate = default(DateTime);
					updatedImageLinks = null;
					return Error.Unknown;
				}
			}
			catch (FileNotFoundException e)
			{
				Disconnect();
				updateDate = default(DateTime);
				updatedImageLinks = null;
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				Disconnect();
				updateDate = default(DateTime);
				updatedImageLinks = null;
				return Error.CommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				updateDate = default(DateTime);
				updatedImageLinks = null;
				return Error.Unknown;
			}
		}
		#endregion

		#region Save new character

		public static Error CreateNewCharacter(FaradayCardV4 card, out CharacterInstance characterInstance, out Link.Image[] imageLinks)
		{
			if (card == null)
			{
				characterInstance = default(CharacterInstance);
				imageLinks = null;
				return Error.NoDataFound;
			}

			if (ConnectionEstablished == false)
			{
				characterInstance = default(CharacterInstance);
				imageLinks = null;
				return Error.NotConnected;
			}

			// Get root folder
			var rootFolder = _Folders.Values.FirstOrDefault(f => f.isRoot);
			if (rootFolder.isRoot == false || string.IsNullOrEmpty(rootFolder.instanceId))
			{
				characterInstance = default(CharacterInstance);
				imageLinks = null;
				return Error.Unknown;
			}

			// Prepare image information
			ImageOutput[] images;
			GetImageUpdates(null, null, out images, out imageLinks);
						
			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					string characterId	= Cuid.NewCuid();
					string configId		= Cuid.NewCuid();
					string chatId		= Cuid.NewCuid();
					string groupId		= Cuid.NewCuid();
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
							characterInstance = default(CharacterInstance);
							imageLinks = null;
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

								// Add images
								if (images != null)
								{
									for (int i = 0; i < images.Length; ++i)
									{
										// AppImage
										sbCommand.AppendLine(
										$@"
											INSERT INTO AppImage
												(id, createdAt, updatedAt, imageUrl, label, ""order"", aspectRatio)
											VALUES 
												($imageId{i:000}, $timestamp, $timestamp, $imageUrl{i:000}, $label{i:000}, {i}, $aspectRatio{i:000});
										");

										// _AppImageToCharacterConfigVersion
										sbCommand.AppendLine(
										$@"
											INSERT INTO _AppImageToCharacterConfigVersion
												(A, B)
											VALUES 
												($imageId{i:000}, $configId);
										");

										cmdCreate.Parameters.AddWithValue($"$imageId{i:000}", images[i].instanceId);
										cmdCreate.Parameters.AddWithValue($"$imageUrl{i:000}", images[i].imageUrl);
										cmdCreate.Parameters.AddWithValue($"$label{i:000}", images[i].label ?? "");
										if (images[i].width > 0 && images[i].height > 0)
											cmdCreate.Parameters.AddWithValue($"$aspectRatio{i:000}", string.Format("{0}/{1}", images[i].width, images[i].height));
										else 
											cmdCreate.Parameters.AddWithValue($"$aspectRatio{i:000}", "");

										expectedUpdates += 2;
									}
								}

								cmdCreate.CommandText = sbCommand.ToString();
								cmdCreate.Parameters.AddWithValue("$charId", characterId);
								cmdCreate.Parameters.AddWithValue("$userId", userId);
								cmdCreate.Parameters.AddWithValue("$configId", configId);
								cmdCreate.Parameters.AddWithValue("$groupId", groupId);
								cmdCreate.Parameters.AddWithValue("$chatId", chatId);
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
										sbCommand.Append($"($id{i:000}, $timestamp, $timestamp, $order{i:000}, $key{i:000}, $value{i:000})");

										cmdInsertLore.Parameters.AddWithValue($"$id{i:000}", uids[i]);
										cmdInsertLore.Parameters.AddWithValue($"$key{i:000}", card.data.loreItems[i].key);
										cmdInsertLore.Parameters.AddWithValue($"$value{i:000}", card.data.loreItems[i].value);
										cmdInsertLore.Parameters.AddWithValue($"$order{i:000}", MakeLoreSortPosition(i, card.data.loreItems.Length - 1, hash));
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
										sbCommand.Append($"($id{i:000}, $configId)");

										cmdLoreRef.Parameters.AddWithValue($"$id{i:000}", uids[i]);
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
								characterInstance = default(CharacterInstance);
								imageLinks = null;
								return Error.CommandFailed;
							}

							// Write images to disk
							if (images != null)
							{
								foreach (var image in images.Where(i => i.data.isEmpty == false))
								{
									try
									{
										// Ensure images folder exists
										string imagedir = Path.GetDirectoryName(image.imageUrl);
										if (Directory.Exists(imagedir) == false)
											Directory.CreateDirectory(imagedir);

										using (FileStream fs = File.Open(image.imageUrl, FileMode.CreateNew, FileAccess.Write))
										{
											fs.Write(image.data.bytes, 0, image.data.bytes.Length);
										}
									}
									catch
									{
										// Do nothing
									}
								}
							}

							characterInstance = new CharacterInstance() {
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

							characterInstance = default(CharacterInstance);
							return Error.CommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				Disconnect();
				characterInstance = default(CharacterInstance);
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				Disconnect();
				characterInstance = default(CharacterInstance);
				return Error.CommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				characterInstance = default(CharacterInstance);
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

		private struct ImageOutput
		{
			public string instanceId;
			public string imageUrl;
			public string label;
			public int width;
			public int height;
			public AssetData data;
		}

		private static bool GetImageUpdates(List<ImageInstance> imageInstances, Link.Image[] imageLinks, out ImageOutput[] imagesToSave, out Link.Image[] newImageLinks)
		{
			// Prepare image information
			string destPath = Path.Combine(AppSettings.BackyardLink.Location, "images");

			List<ImageOutput> results = new List<ImageOutput>();
			List<Link.Image> lsImageLinks = new List<Link.Image>();

			// Main portrait
			var portraitImage = Current.Card.portraitImage;
			string portraitUID;
			if (portraitImage != null)
				portraitUID = portraitImage.uid;
			else
				portraitUID = "__default";
			
			ImageInstance existingPortrait = null;
			int idxPortraitLink = -1;
			if (imageLinks != null)
			{
				idxPortraitLink = Array.FindIndex(imageLinks, l => l.uid == portraitUID);
				if (idxPortraitLink != -1)
				{
					existingPortrait = imageInstances.FirstOrDefault(kvp => string.Compare(Path.GetFileName(kvp.imageUrl), imageLinks[idxPortraitLink].filename, StringComparison.InvariantCultureIgnoreCase) == 0);
				}
			}
			
			if (existingPortrait != null)
			{
				// No change
				results.Add(new ImageOutput() {
					instanceId = existingPortrait.instanceId,
					imageUrl = existingPortrait.imageUrl,
					label = existingPortrait.label,
					width = existingPortrait.width,
					height = existingPortrait.height,
				});

				lsImageLinks.Add(imageLinks[idxPortraitLink]);
			}
			else
			{
				var filename = string.Concat(Guid.NewGuid().ToString().ToLowerInvariant(), ".png"); // Random filename
				ImageOutput output;
				if (portraitImage != null)
				{
					output = new ImageOutput() {
						instanceId = Cuid.NewCuid(),
						imageUrl = Path.Combine(destPath, filename),
						data = AssetData.FromBytes(Utility.ImageToMemory(portraitImage)),
						width = portraitImage.Width,
						height = portraitImage.Height,
					};
				}
				else
				{
					output = new ImageOutput() {
						instanceId = Cuid.NewCuid(),
						imageUrl = Path.Combine(destPath, filename),
						data = AssetData.FromBytes(Resources.default_portrait),
						width = Constants.DefaultPortraitWidth,
						height = Constants.DefaultPortraitHeight,
					};
				}

				results.Add(output);
				lsImageLinks.Add(new Link.Image() {
					uid = portraitUID,
					filename = filename,
				});
			}

			// Embedded assets
			if (Current.Card.assets != null)
			{
				foreach (var asset in Current.Card.assets
					.Where(a => a.isEmbeddedAsset 
						&& a.data.length > 0
						&& (a.assetType == AssetFile.AssetType.Icon || a.assetType == AssetFile.AssetType.Expression)))
				{
					ImageInstance existingInstance = null;
					if (imageLinks != null)
					{
						int idxLink = Array.FindIndex(imageLinks, l => l.uid == asset.uid); // Same id
						if (idxLink == -1)
						{
							idxLink = Array.FindIndex(imageLinks, l => string.Compare(l.filename, string.Concat(asset.name, ".", asset.ext), StringComparison.InvariantCultureIgnoreCase) == 0); // Same filename
						}
						if (idxLink != -1)
						{
							existingInstance = imageInstances.FirstOrDefault(kvp => string.Compare(Path.GetFileName(kvp.imageUrl), imageLinks[idxLink].filename, StringComparison.InvariantCultureIgnoreCase) == 0);
							if (existingInstance != null)
							{
								// No change
								results.Add(new ImageOutput() {
									instanceId = existingInstance.instanceId,
									imageUrl = existingInstance.imageUrl,
									label = existingInstance.label,
									width = existingInstance.width,
									height = existingInstance.height,
								});

								lsImageLinks.Add(imageLinks[idxLink]);
								continue;
							}
						}
					}


					int imageWidth = asset.knownWidth;
					int imageHeight = asset.knownHeight;
					if ((imageWidth > 0 && imageHeight > 0) || Utility.GetImageDimensions(asset.data.bytes, out imageWidth, out imageHeight))
					{
						asset.knownWidth = imageWidth;
						asset.knownHeight = imageHeight;
						ImageOutput output;
						if (asset.ext == "jpeg" || asset.ext == "jpg") // Jpeg
						{
							output = new ImageOutput() {
								instanceId = Cuid.NewCuid(),
								imageUrl = Path.Combine(destPath, string.Concat(Guid.NewGuid().ToString().ToLowerInvariant(), ".jpeg")), // Random filename
								data = asset.data,
								width = imageWidth,
								height = imageHeight,
							};
						}
						else // Png
						{
							output = new ImageOutput() {
								instanceId = Cuid.NewCuid(),
								imageUrl = Path.Combine(destPath, string.Concat(Guid.NewGuid().ToString().ToLowerInvariant(), ".png")), // Random filename
								data = asset.data,
								width = imageWidth,
								height = imageHeight,
							};
						}
						
						results.Add(output);
						lsImageLinks.Add(new Link.Image() {
							uid = asset.uid,
							filename = Path.GetFileName(output.imageUrl),
						});
					}
				}
			}

			// Add unrecognized images (added in Backyard; there's little reason to not include them)
			if (imageInstances != null)
			{
				var unrecognizedImageInstances = imageInstances
					.Where(i => results.ContainsNoneOf(r => r.instanceId == i.instanceId || string.Compare(r.imageUrl, i.imageUrl, StringComparison.InvariantCultureIgnoreCase) == 0))
					.ToArray();
				
				results.AddRange(unrecognizedImageInstances.Select(i => 
					new ImageOutput() {
						instanceId = i.instanceId,
						imageUrl = i.imageUrl,
						label = i.label,
						width = i.width,
						height = i.height,
					}));
			}


			imagesToSave = results.ToArray();
			newImageLinks = lsImageLinks.Count > 0 ? lsImageLinks.ToArray() : null;
			return imagesToSave.ContainsAny(i => i.data.isEmpty == false);
		}

		#endregion

	}
}