using System;
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
			public DateTime creationDate;	// CharacterConfig.createdAt
			public DateTime updateDate;     // CharacterConfig.updatedAt
			public bool isUser;				// CharacterConfig.isUserControlled
			public string configId;			// CharacterConfigVersion.id
			public string displayName;		// CharacterConfigVersion.displayName
			public string name;				// CharacterConfigVersion.name
			public string groupId;			// GroupConfig.id (Primary group)
			public string folderId;			// GroupConfig.folderId (Primary group)
			public string creator;          // GroupConfig.hubAuthorUsername
		}

		public struct GroupInstance
		{
			public string instanceId;		// GroupConfig.id
			public string name;				// GroupConfig.name
			public string folderId;			// GroupConfig.folderId
			public DateTime creationDate;	// CharacterConfig.createdAt
			public DateTime updateDate;     // CharacterConfig.updatedAt
			public string[] members;		// CharacterConfigVersion.id...

			public bool isEmpty
			{
				get { return string.IsNullOrEmpty(instanceId) || members == null || members.Length == 0; }
			}
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

		public class ChatInstance
		{
			public string instanceId;       // Chat.id
			public string name;				// Chat.name
			public DateTime creationDate;	// Chat.createdAt
			public DateTime updateDate;     // Chat.updatedAt
			public string greeting;			// Chat.greetingDialogue
			public string[] participants;   // CharacterGroup.id

			public ChatHistory history = new ChatHistory();

			public bool hasGreeting { get { return string.IsNullOrEmpty(greeting) == false; } }
		}

		public static IEnumerable<FolderInstance> Folders { get { return _Folders.Values; } }
		public static IEnumerable<CharacterInstance> Characters { get { return _Characters.Values; } }
		public static IEnumerable<GroupInstance> Groups { get { return _Groups.Values; } }
		public static string DefaultModel = null;
		public static string DefaultUserConfigId = null;

		private static Dictionary<string, FolderInstance> _Folders = new Dictionary<string, FolderInstance>();
		private static Dictionary<string, CharacterInstance> _Characters = new Dictionary<string, CharacterInstance>();
		private static Dictionary<string, GroupInstance> _Groups = new Dictionary<string, GroupInstance>();

		public static string DefaultChatTitle = "Untitled Chat";

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
				if (ConnectionEstablished)
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
			NotConnected,
			FileNotFound,
			InvalidArgument,
			ValidationFailed,
			SQLCommandFailed,
			NotFound,
			DismissedByUser,
			CancelledByUser,
			Unknown,
		}

		public static bool ConnectionEstablished = false;

		private static SQLiteConnection CreateSQLiteConnection()
		{
			string backyardPath = AppSettings.BackyardLink.Location;
			if (string.IsNullOrWhiteSpace(backyardPath))
			{
#if DEBUG
				string appPath = "faraday-canary"; // Use canary database during development and testing
#else
				string appPath = "faraday"; // User production database 
#endif
				backyardPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appPath);
			}
			string dbFilePath = Path.Combine(backyardPath, "db.sqlite");
			if (File.Exists(dbFilePath) == false)
				throw new FileNotFoundException();

			AppSettings.BackyardLink.Location = backyardPath;
			return new SQLiteConnection($"Data Source={dbFilePath}; Version=3; Foreign Keys=True; Pooled=True;");
		}

		#region Establish Link
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
							return Error.ValidationFailed;

						for (int j = 0; j < expectedNames.Length; ++j)
						{
							if (foundColumns.FindIndex(kvp => kvp.Key == expectedNames[j] && kvp.Value == expectedTypes[j]) == -1)
								return Error.ValidationFailed;
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
				return Error.SQLCommandFailed;
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
			_Groups.Clear();
			ConnectionEstablished = false;
			AppSettings.BackyardLink.Enabled = false;
			SQLiteConnection.ClearAllPools(); // Releases the lock on the db file
		}
		#endregion

		#region Enumerate characters and groups
		public static bool GetCharacter(string characterId, out CharacterInstance character)
		{
			return _Characters.TryGetValue(characterId, out character);
		}

		public static CharacterInstance GetCharacter(string characterId)
		{
			CharacterInstance character;
			if (_Characters.TryGetValue(characterId, out character))
				return character;
			return default(CharacterInstance);
		}

		public static bool HasCharacter(string characterId)
		{
			return _Characters.ContainsKey(characterId);
		}
		
		public static bool GetGroup(string groupId, out GroupInstance group)
		{
			if (groupId != null)
				return _Groups.TryGetValue(groupId, out group);
			group = default(GroupInstance);
			return false;
		}

		public static GroupInstance GetGroup(string groupId)
		{
			GroupInstance group;
			if (GetGroup(groupId, out group))
				return group;
			return default(GroupInstance);
		}

		public static Error RefreshCharacters()
		{
			if (ConnectionEstablished == false)
				return Error.NotConnected;

			_Folders.Clear();
			_Characters.Clear();
			_Groups.Clear();

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

					using (var cmdCharacterData = connection.CreateCommand())
					{
						cmdCharacterData.CommandText =
						@"
							SELECT 
								A.id, B.id, D.id,
								B.displayName, B.name, A.createdAt,
								E.updatedAt, D.folderId, D.hubAuthorUsername
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
								string groupId = reader.GetString(2);
								if (string.IsNullOrEmpty(instanceId) || string.IsNullOrEmpty(configId))
									continue;

								string displayName = reader.GetString(3);
								string name = reader.GetString(4);
								DateTime createdAt = reader.GetUnixTime(5);
								DateTime updatedAt = reader.GetUnixTime(6);
								string folderId = reader.GetString(7);
								string hubAuthorUsername = reader[8] as string;

								_Characters.TryAdd(instanceId, 
									new CharacterInstance() {
										instanceId = instanceId,
										configId = configId,
										groupId = groupId,
										displayName = displayName,
										name = name,
										creationDate = createdAt,
										updateDate = updatedAt,
										folderId = folderId,
										creator = hubAuthorUsername,
										isUser = false,
									});
							}
						}
					}

					// Fetch user characters
					using (var cmdUser = connection.CreateCommand())
					{
						cmdUser.CommandText =
						@"
							SELECT 
								A.id, 
								B.id, B.displayName, B.name, A.createdAt, A.updatedAt
							FROM CharacterConfig as A
							INNER JOIN CharacterConfigVersion AS B ON B.characterConfigId = A.id
							WHERE A.isUserControlled=1;
						";
						using (var reader = cmdUser.ExecuteReader())
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

								_Characters.TryAdd(instanceId, 
									new CharacterInstance() {
										instanceId = instanceId,
										configId = configId,
										displayName = displayName,
										name = name,
										creationDate = createdAt,
										updateDate = updatedAt,
										isUser = true,
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

					// Fetch character-group memberships
					var groupMembers = new Dictionary<string, HashSet<string>>();
					using (var cmdGroup = connection.CreateCommand())
					{
						cmdGroup.CommandText =
						@"
							SELECT 
								A, B
							FROM _CharacterConfigToGroupConfig
						";

						using (var reader = cmdGroup.ExecuteReader())
						{
							while (reader.Read())
							{
								string configId = reader.GetString(0);
								string groupId = reader.GetString(1);

								if (groupMembers.ContainsKey(groupId) == false)
									groupMembers.Add(groupId, new HashSet<string>());

								groupMembers[groupId].Add(configId);
							}
						}
					}

					// Fetch chat groups
					using (var cmdGroupData = connection.CreateCommand())
					{ 
						cmdGroupData.CommandText =
						@"
							SELECT 
								id, createdAt, updatedAt, name, folderId
							FROM GroupConfig
						";

						using (var reader = cmdGroupData.ExecuteReader())
						{
							while (reader.Read())
							{
								string instanceId = reader.GetString(0);
								DateTime createdAt = reader.GetUnixTime(1);
								DateTime updatedAt = reader.GetUnixTime(2);
								string name = reader.GetString(3);
								string folderId = reader.GetString(4);

								HashSet<string> members;
								if (groupMembers.TryGetValue(instanceId, out members) == false)
									continue; // No members?

								_Groups.TryAdd(instanceId, 
									new GroupInstance() {
										instanceId = instanceId,
										name = name,
										creationDate = createdAt,
										updateDate = updatedAt,
										folderId = folderId,
										members = members.ToArray(),
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
				return Error.SQLCommandFailed;
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
								return Error.NotFound; // No character
							}

							string instanceId = reader.GetString(0);
							DateTime createdAt = reader.GetUnixTime(1);
							DateTime updatedAt = reader.GetUnixTime(2);
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
							card.data.greeting = greeting;
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
					return card == null ? Error.NotFound : Error.NoError;
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
				return Error.SQLCommandFailed;
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
				return Error.NotFound;
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
								return Error.NotFound;
							}

							string configId = reader.GetString(0);
							string groupId = reader.GetString(1);
							DateTime updatedAt = reader.GetUnixTime(2);
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
				return Error.SQLCommandFailed;
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
				return Error.NotFound;
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
								return Error.NotFound;
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
								return Error.SQLCommandFailed;
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
				return Error.SQLCommandFailed;
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
			if (ConnectionEstablished == false)
			{
				characterInstance = default(CharacterInstance);
				imageLinks = null;
				return Error.NotConnected;
			}

			if (card == null)
			{
				characterInstance = default(CharacterInstance);
				imageLinks = null;
				return Error.NotFound;
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
							return Error.SQLCommandFailed; // Requires default user
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
								return Error.SQLCommandFailed;
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
							return Error.SQLCommandFailed;
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
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				characterInstance = default(CharacterInstance);
				return Error.Unknown;
			}
		}
		#endregion

		#region Chat

		public struct ChatCount {
			public int count;
			public DateTime lastMessaged;
		}

		public static Error GetChatCounts(out Dictionary<string, ChatCount> counts)
		{
			if (ConnectionEstablished == false)
			{
				counts = null;
				return Error.NotConnected;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();
					
					counts = new Dictionary<string, ChatCount>();
					using (var cmdChat = connection.CreateCommand())
					{ 
						cmdChat.CommandText =
						@"
							SELECT 
								groupConfigId, 
								COUNT(*), 
								(
									SELECT MAX(updatedAt)
									FROM Message
									WHERE chatId = C.id
								)
							FROM Chat AS C
							GROUP BY groupConfigId
						";

						using (var reader = cmdChat.ExecuteReader())
						{
							while (reader.Read())
							{
								string groupId = reader.GetString(0);
								int count = reader.GetInt32(1);
								DateTime updatedAt = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetUnixTime(2);

								counts.Add(groupId, new ChatCount() {
									count = count,
									lastMessaged = updatedAt,
								});
							}							
						}
					}
					
					return Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				counts = null;
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				counts = null;
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				counts = null;
				return Error.Unknown;
			}
		}
		
		private struct _Chat
		{
			public string instanceId;
			public string name;
			public string greeting;
			public DateTime creationDate;
			public DateTime updateDate;
		}

		private struct _Message
		{
			public string messageId;
			public string characterId;
			public string text;
			public DateTime createdAt;
			public DateTime updatedAt;
			public DateTime activeAt;
		}

		public static Error GetChats(GroupInstance groupInstance, out ChatInstance[] chatInstances)
		{
			if (ConnectionEstablished == false)
			{
				chatInstances = null;
				return Error.NotConnected;
			}

			string groupId = groupInstance.instanceId;
			if (string.IsNullOrEmpty(groupId))
			{
				chatInstances = null;
				return Error.NotFound;
			}

			var characterInstances = groupInstance.members
				.Select(id => GetCharacter(id));
			if (characterInstances.ContainsAny(c => string.IsNullOrEmpty(c.instanceId)))
			{
				chatInstances = null;
				return Error.NotFound; // Group contains unknown characters
			}

			var userId = characterInstances
				.Where(c => c.isUser)
				.Select(c => c.instanceId)
				.FirstOrDefault();

			int index = 1;
			var indexById = characterInstances
				.Where(c => c.isUser == false)
				.Select(c => new {
					id = c.instanceId,
					index = index++,
				})
				.ToDictionary(x => x.id, x => x.index);
			indexById.Add(userId, 0);

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					var lsChatInstances = new List<ChatInstance>();
					var chats = new List<_Chat>();
					using (var cmdChat = connection.CreateCommand())
					{ 
						cmdChat.CommandText =
						@"
							SELECT 
								id, name, createdAt, updatedAt, greetingDialogue
							FROM Chat
							WHERE groupConfigId = $groupId
							ORDER BY createdAt;
						";
						cmdChat.Parameters.AddWithValue("$groupId", groupId);

						using (var reader = cmdChat.ExecuteReader())
						{
							int untitledCounter = 0;
							while (reader.Read())
							{
								string chatId = reader.GetString(0);
								string name = reader.GetString(1);
								DateTime createdAt = reader.GetUnixTime(2);
								DateTime updatedAt = reader.GetUnixTime(3);
								string greeting = reader.GetString(4);

								if (string.IsNullOrWhiteSpace(name))
								{
									if (++untitledCounter > 1)
										name = string.Concat(DefaultChatTitle, " #", untitledCounter.ToString());
									else
										name = DefaultChatTitle;
								}

								chats.Add(new _Chat() {
									instanceId = chatId,
									creationDate = createdAt,
									updateDate = updatedAt,
									name = name,
									greeting = greeting,
								});
							}							
						}
					}

					// Collect messages
					for (int i = 0; i < chats.Count; ++i)
					{
						string chatId = chats[i].instanceId;

						var messages = new List<_Message>(64);
						using (var cmdMessages = connection.CreateCommand())
						{
							cmdMessages.CommandText =
							@"
								SELECT 
									R.messageId, M.createdAt, R.updatedAt, R.activeTimestamp, M.characterConfigId, R.text 
								FROM RegenSwipe As R
								INNER JOIN Message AS M ON M.id = R.messageId
								WHERE R.messageId IN (
									SELECT id
									FROM Message
									WHERE chatId = $chatId
								)
								ORDER BY M.createdAt ASC;
							";
							cmdMessages.Parameters.AddWithValue("$chatId", chatId);

							using (var reader = cmdMessages.ExecuteReader())
							{
								while (reader.Read())
								{
									string messageId = reader.GetString(0);
									DateTime createdAt = reader.GetUnixTime(1);
									DateTime updatedAt = reader.GetUnixTime(2);
									DateTime activeAt = reader.GetUnixTime(3);
									string characterId = reader.GetString(4);
									string text = reader.GetString(5);

									messages.Add(new _Message() {
										messageId = messageId,
										createdAt = createdAt,
										updatedAt = updatedAt,
										activeAt = activeAt,
										characterId = characterId,
										text = text,
									});
								}
							}
						}

						// Create entries
						var entries = messages
							.GroupBy(m => m.messageId)
							.Select(g => {
								int counter = 0;
								var swipes = g.Select(m => new {
									index = counter++,
									text = m.text,
									active = m.activeAt,
								});

								var message = g.First();

								return new ChatHistory.Message() {
									instanceId = message.messageId,
									speaker = indexById[message.characterId],
									creationDate = message.createdAt,
									updateDate = message.updatedAt,
									activeSwipe = swipes.OrderByDescending(x => x.active).Select(x => x.index).First(),
									swipes = swipes.Select(x => x.text)
									.ToArray(),
								};
							})
							.ToList();

						// Insert greeting
						if (string.IsNullOrEmpty(chats[i].greeting) == false)
						{
							string characterName = groupInstance.members
								.Select(id => GetCharacter(id))
								.Where(c => c.isUser == false)
								.Select(c => c.name)
								.FirstOrDefault() ?? "Unnamed";
							string UserName = groupInstance.members
								.Select(id => GetCharacter(id))
								.Where(c => c.isUser)
								.Select(c => c.name)
								.FirstOrDefault() ?? "User";

							var sb = new StringBuilder(GingerString.FromFaraday(chats[i].greeting).ToString());
							sb.Replace(GingerString.CharacterMarker, characterName, true);
							sb.Replace(GingerString.UserMarker, UserName, true);

							entries.Insert(0, new ChatHistory.Message() {
								speaker = 1,
								creationDate = chats[i].creationDate,
								updateDate = chats[i].updateDate,
								activeSwipe = 0,
								swipes = new string[1] { sb.ToString() },
							});
						}

						var chatInstance = new ChatInstance() {
							instanceId = chats[i].instanceId,
							creationDate = chats[i].creationDate,
							updateDate = chats[i].updateDate,
							greeting = chats[i].greeting,
							name = chats[i].name,
							participants = groupInstance.members,
							history = new ChatHistory() {
								messages = entries.ToArray(),
							},
						};

						lsChatInstances.Add(chatInstance);
					}

					chatInstances = lsChatInstances
						.OrderByDescending(c => DateTimeExtensions.Max(c.creationDate, c.updateDate))
						.ToArray();
					return Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				chatInstances = null;
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				chatInstances = null;
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				chatInstances = null;
				return Error.Unknown;
			}
		}

		public static Error CreateNewChat(string chatTitle, ChatHistory chatHistory, string groupId, out ChatInstance chatInstance)
		{
			if (ConnectionEstablished == false)
			{
				chatInstance = default(ChatInstance);
				return Error.NotConnected;
			}

			if (chatHistory == null || chatHistory.messages == null || groupId == null)
			{
				chatInstance = default(ChatInstance);
				return Error.InvalidArgument;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					var paramScenario = "";
					var paramExample = "";
					var paramSystem = "";
					var paramGreeting = "";
					var paramGrammar = "";
					var paramModel = DefaultModel;
					var paramPruneExample = AppSettings.Faraday.PruneExampleChat;
					var paramTemperature = AppSettings.Faraday.Temperature;
					var paramTopP = AppSettings.Faraday.TopP;
					var paramMinP = AppSettings.Faraday.MinP;
					var paramMinPEnabled = AppSettings.Faraday.MinPEnabled;
					var paramTopK = AppSettings.Faraday.TopK;
					var paramRepeatPenalty = AppSettings.Faraday.RepeatPenalty;
					var paramRepeatLastN = AppSettings.Faraday.RepeatPenaltyTokens;
					var paramPromptTemplate = AppSettings.Faraday.GetPromptTemplateName();

					// Fetch group chat info
					using (var cmdGroupInfo = connection.CreateCommand())
					{ 
						cmdGroupInfo.CommandText =
						@"
							SELECT 
								context, customDialogue, canDeleteCustomDialogue, 
								modelInstructions, greetingDialogue, grammar, 
								model, temperature, topP, 
								minP, minPEnabled, topK, 
								repeatPenalty, repeatLastN, promptTemplate
							FROM Chat
							WHERE groupConfigId = $groupId;
						";

						cmdGroupInfo.Parameters.AddWithValue("$groupId", groupId);

						using (var reader = cmdGroupInfo.ExecuteReader())
						{
							if (reader.Read() == false)
							{
								chatInstance = default(ChatInstance);
								return Error.NotFound;
							}

							paramScenario = reader.GetString(0);
							paramExample = reader.GetString(1);
							paramPruneExample = reader.GetBoolean(2);
							paramSystem = reader.GetString(3);
							paramGreeting = reader.GetString(4);
							paramGrammar = reader[5] as string;
							paramModel = reader.GetString(6);
							paramTemperature = reader.GetDecimal(7);
							paramTopP = reader.GetDecimal(8);
							paramMinP = reader.GetDecimal(9);
							paramMinPEnabled = reader.GetBoolean(10);
							paramTopK = reader.GetInt32(11);
							paramRepeatPenalty = reader.GetDecimal(12);
							paramRepeatLastN = reader.GetInt32(13);
							paramPromptTemplate = reader[14] as string;
						}
					}

					// Fetch group members
					var groupMembers = new List<string>();
					using (var cmdGroupMembers = connection.CreateCommand())
					{
						cmdGroupMembers.CommandText =
						@"
							SELECT 
								A.A, B.isUserControlled
							FROM _CharacterConfigToGroupConfig AS A
							INNER JOIN CharacterConfig AS B ON B.id = A.A
							WHERE A.B = $groupId;
						";

						cmdGroupMembers.Parameters.AddWithValue("$groupId", groupId);

						var members = new List<KeyValuePair<string, bool>>();
						using (var reader = cmdGroupMembers.ExecuteReader())
						{
							while (reader.Read())
							{
								string characterId = reader.GetString(0);
								bool isUser = reader.GetBoolean(1);
								members.Add(new KeyValuePair<string, bool>(characterId, isUser));
							}
						}
						if (members.Count(kvp => kvp.Value) > 1)
						{
							// Can only have one user
							chatInstance = default(ChatInstance);
							return Error.InvalidArgument;
						}

						// Validate message indices
						foreach (var message in chatHistory.messages)
						{
							if (message.speaker < 0 || message.speaker >= members.Count)
							{
								// Too many group members
								chatInstance = default(ChatInstance);
								return Error.InvalidArgument;
							}
						}

						// Place user first
						groupMembers = members.Where(kvp => kvp.Value).Select(kvp => kvp.Key)
							.Union(members.Where(kvp => kvp.Value == false).Select(kvp => kvp.Key))
							.ToList();
					}

					// Write to database
					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							string chatId = Cuid.NewCuid();
							DateTime now = DateTime.Now;
							long createdAt = now.ToUnixTimeMilliseconds();

							int updates = 0;
							int expectedUpdates = 0;
							
							using (var cmdCreateChat = new SQLiteCommand(connection))
							{
								var sbCommand = new StringBuilder();

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
											$chatName, '');
								");

								cmdCreateChat.CommandText = sbCommand.ToString();
								cmdCreateChat.Parameters.AddWithValue("$chatId", chatId);
								cmdCreateChat.Parameters.AddWithValue("$groupId", groupId);
								cmdCreateChat.Parameters.AddWithValue("$chatName", chatTitle);
								cmdCreateChat.Parameters.AddWithValue("$timestamp", createdAt);
								cmdCreateChat.Parameters.AddWithValue("$system", paramSystem);
								cmdCreateChat.Parameters.AddWithValue("$scenario", paramScenario);
								cmdCreateChat.Parameters.AddWithValue("$example", paramExample);
								cmdCreateChat.Parameters.AddWithValue("$greeting", paramGreeting);
								cmdCreateChat.Parameters.AddWithValue("$grammar", paramGrammar ?? "");
								cmdCreateChat.Parameters.AddWithValue("$model", paramModel);
								cmdCreateChat.Parameters.AddWithValue("$pruneExample", paramPruneExample);
								cmdCreateChat.Parameters.AddWithValue("$temperature", paramTemperature);
								cmdCreateChat.Parameters.AddWithValue("$topP", paramTopP);
								cmdCreateChat.Parameters.AddWithValue("$minP", paramMinP);
								cmdCreateChat.Parameters.AddWithValue("$minPEnabled", paramMinPEnabled);
								cmdCreateChat.Parameters.AddWithValue("$topK", paramTopK);
								cmdCreateChat.Parameters.AddWithValue("$repeatPenalty", paramRepeatPenalty);
								cmdCreateChat.Parameters.AddWithValue("$repeatLastN", paramRepeatLastN);
								cmdCreateChat.Parameters.AddWithValue("$promptTemplate", paramPromptTemplate);

								expectedUpdates += 1;
								updates += cmdCreateChat.ExecuteNonQuery();
							}

							// Write messages
							var lsMessages = new List<ChatHistory.Message>();
							int messageCount = chatHistory.messagesWithoutGreeting.Count();
							if (messageCount > 0)
							{
								// Generate unique IDs
								var messageIds = new string[messageCount];
								var swipeIds = new string[messageCount];
								for (int i = 0; i < messageIds.Length; ++i)
								{
									messageIds[i] = Cuid.NewCuid();
									swipeIds[i] = Cuid.NewCuid();
								}

								using (var cmdMessages = new SQLiteCommand(connection))
								{
									var sbCommand = new StringBuilder();
									sbCommand.AppendLine(
									@"
										INSERT INTO Message 
											(id, createdAt, updatedAt, chatId, characterConfigId)
										VALUES ");

									int i = 0;
									foreach (var message in chatHistory.messagesWithoutGreeting)
									{
										if (i > 0)
											sbCommand.Append(",\n");
										sbCommand.Append($"($messageId{i:000}, $createdAt{i:000}, $updatedAt{i:000}, $chatId, $charId{i:000})");

										cmdMessages.Parameters.AddWithValue($"$messageId{i:000}", messageIds[i]);
										cmdMessages.Parameters.AddWithValue($"$createdAt{i:000}", message.creationDate.ToUnixTimeMilliseconds());
										cmdMessages.Parameters.AddWithValue($"$updatedAt{i:000}", message.updateDate.ToUnixTimeMilliseconds());
										cmdMessages.Parameters.AddWithValue($"$charId{i:000}", groupMembers[message.speaker]);

										lsMessages.Add(new ChatHistory.Message() {
											instanceId = messageIds[i],
											activeSwipe = 0,
											creationDate = message.creationDate,
											updateDate = message.updateDate,
											speaker = message.speaker,
											swipes = new string[1] { message.text },
										});
										++i;
									}
									sbCommand.Append(";");

									sbCommand.AppendLine(
									@"
										INSERT INTO RegenSwipe
											(id, createdAt, updatedAt, activeTimestamp, text, messageId)
										VALUES ");
									i = 0;
									foreach (var message in chatHistory.messagesWithoutGreeting)
									{
										if (i > 0)
											sbCommand.Append(",\n");
										sbCommand.Append($"($swipeId{i:000}, $updatedAt{i:000}, $updatedAt{i:000}, $updatedAt{i:000}, $text{i:000}, $messageId{i:000})");

										cmdMessages.Parameters.AddWithValue($"$swipeId{i:000}", swipeIds[i]);
										cmdMessages.Parameters.AddWithValue($"$text{i:000}", message.text);
										++i;
									}
									sbCommand.Append(";");
									cmdMessages.CommandText = sbCommand.ToString();

									cmdMessages.Parameters.AddWithValue("$chatId", chatId);

									expectedUpdates += messageCount * 2;
									updates += cmdMessages.ExecuteNonQuery();
								}
							}
							
							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								chatInstance = default(ChatInstance);
								return Error.SQLCommandFailed;
							}

							chatInstance = new ChatInstance() {
								instanceId = chatId,
								creationDate = now,
								updateDate = now,
								name = chatTitle,
								greeting = paramGreeting,
								history = new ChatHistory() {
									messages = lsMessages.ToArray(),
								},
								participants = groupMembers.ToArray(),
							};

							transaction.Commit();
							return Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();

							chatInstance = default(ChatInstance);
							return Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				Disconnect();
				chatInstance = default(ChatInstance);
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				Disconnect();
				chatInstance = default(ChatInstance);
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				chatInstance = default(ChatInstance);
				return Error.Unknown;
			}
		}

		public static Error RenameChat(ChatInstance chatInstance, string newName)
		{
			if (ConnectionEstablished == false)
				return Error.NotConnected;

			if (chatInstance == null || string.IsNullOrEmpty(chatInstance.instanceId))
				return Error.InvalidArgument;

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					string chatId = chatInstance.instanceId;
					DateTime now = DateTime.Now;
					long updatedAt = now.ToUnixTimeMilliseconds();
					int updates = 0;
					int expectedUpdates = 0;

					// Write to database
					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							// Set chat name
							using (var cmdEditChat = connection.CreateCommand())
							{
								cmdEditChat.CommandText =
								@"
									UPDATE Chat
									SET 
										updatedAt = $timestamp,
										name = $name
									WHERE id = $chatId;
								";

								cmdEditChat.Parameters.AddWithValue("$chatId", chatId);
								cmdEditChat.Parameters.AddWithValue("$timestamp", updatedAt);
								cmdEditChat.Parameters.AddWithValue("$name", newName ?? "");

								expectedUpdates += 1;
								updates += cmdEditChat.ExecuteNonQuery();
							}
														
							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								chatInstance = default(ChatInstance);
								return Error.SQLCommandFailed;
							}

							chatInstance.updateDate = now;
							chatInstance.name = newName;

							transaction.Commit();
							return Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();

							chatInstance = default(ChatInstance);
							return Error.SQLCommandFailed;
						}
					}
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
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				return Error.Unknown;
			}
		}

		public static Error ConfirmDeleteChat(ChatInstance chatInstance, GroupInstance groupInstance, out int chatCount)
		{
			if (ConnectionEstablished == false)
			{
				chatCount = 0;
				return Error.NotConnected;
			}

			if (chatInstance == null || string.IsNullOrEmpty(chatInstance.instanceId) || string.IsNullOrEmpty(groupInstance.instanceId))
			{
				chatCount = 0;
				return Error.NotFound;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					// Count chats
					using (var cmdConfirm = connection.CreateCommand())
					{ 
						cmdConfirm.CommandText =
						@"
							SELECT id
							FROM Chat
							WHERE groupConfigId = $groupId
						";
						cmdConfirm.Parameters.AddWithValue("$groupId", groupInstance.instanceId);

						var chats = new HashSet<string>();
						using (var reader = cmdConfirm.ExecuteReader())
						{
							while (reader.Read())
								chats.Add(reader.GetString(0));
						}

						chatCount = chats.Count;

						if (chats.Contains(chatInstance.instanceId) == false)
							return Error.NotFound;

						return Error.NoError;
					}
				}
			}
			catch (FileNotFoundException e)
			{
				chatCount = 0;
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				chatCount = 0;
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				chatCount = 0;
				return Error.Unknown;
			}
		}

		public static Error DeleteChat(ChatInstance chatInstance)
		{
			if (ConnectionEstablished == false)
				return Error.NotConnected;

			if (chatInstance == null || string.IsNullOrEmpty(chatInstance.instanceId))
				return Error.InvalidArgument;

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					string chatId = chatInstance.instanceId;
					int updates = 0;
					int expectedUpdates = 0;

					// Write to database
					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							// Set chat name
							using (var cmdDeleteChat = connection.CreateCommand())
							{
								cmdDeleteChat.CommandText =
								@"
									DELETE FROM Chat
									WHERE id = $chatId;
								";

								cmdDeleteChat.Parameters.AddWithValue("$chatId", chatId);

								expectedUpdates += 1;
								updates += cmdDeleteChat.ExecuteNonQuery();
							}
														
							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								chatInstance = default(ChatInstance);
								return Error.SQLCommandFailed;
							}

							transaction.Commit();
							return Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();

							chatInstance = default(ChatInstance);
							return Error.SQLCommandFailed;
						}
					}
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
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				return Error.Unknown;
			}
		}

		public static Error PurgeChats(string groupId)
		{
			if (ConnectionEstablished == false)
				return Error.NotConnected;

			if (string.IsNullOrEmpty(groupId))
				return Error.InvalidArgument;

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					// Find chats to purge
					var chatIds = new List<string>();
					using (var cmdGetChats = connection.CreateCommand())
					{ 
						cmdGetChats.CommandText =
						@"
							SELECT id
							FROM Chat
							WHERE groupConfigId = $groupId
						";
						cmdGetChats.Parameters.AddWithValue("$groupId", groupId);

						using (var reader = cmdGetChats.ExecuteReader())
						{
							while (reader.Read())
								chatIds.Add(reader.GetString(0));
						}
					}

					if (chatIds.Count == 0)
					{
						return Error.NotFound;
					}	

					// Write to database
					int updates = 0;
					int expectedUpdates = 0;
					DateTime now = DateTime.Now;
					long updatedAt = now.ToUnixTimeMilliseconds();

					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							// Delete all but one chat
							using (var cmdDeleteChat = connection.CreateCommand())
							{
								var sbCommand = new StringBuilder();
								sbCommand.AppendLine(
								@"
									DELETE FROM Chat
									WHERE id IN (
								");

								for (int i = 1; i < chatIds.Count; ++i)
								{
									if (i > 1)
										sbCommand.Append(", ");
									sbCommand.AppendFormat("'{0}'", chatIds[i]);
								}
								sbCommand.AppendLine(");");
								cmdDeleteChat.CommandText = sbCommand.ToString();

								expectedUpdates += chatIds.Count - 1;
								updates += cmdDeleteChat.ExecuteNonQuery();
							}

							// Delete all messages from the first chat
							using (var cmdEditChat = connection.CreateCommand())
							{
								cmdEditChat.CommandText =
								@"
									DELETE FROM Message
									WHERE chatId = $chatId;
								";

								cmdEditChat.Parameters.AddWithValue("$chatId", chatIds[0]);

								int nDeleted = cmdEditChat.ExecuteNonQuery();
								expectedUpdates += nDeleted;
								updates += nDeleted;
							}

							// Update first chat info
							using (var cmdUpdateChat = connection.CreateCommand())
							{
								cmdUpdateChat.CommandText =
								@"
									UPDATE Chat
									SET 
										updatedAt = $timestamp,
										name = $name
									WHERE id = $chatId;
								";

								cmdUpdateChat.Parameters.AddWithValue("$chatId", chatIds[0]);
								cmdUpdateChat.Parameters.AddWithValue("$timestamp", updatedAt);
								cmdUpdateChat.Parameters.AddWithValue("$name", "");

								expectedUpdates += 1;
								updates += cmdUpdateChat.ExecuteNonQuery();
							}
		
							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								return Error.SQLCommandFailed;
							}

							transaction.Commit();
							return Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();
							return Error.SQLCommandFailed;
						}
					}
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
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				return Error.Unknown;
			}
		}

		public static Error UpdateChat(ChatInstance chatInstance)
		{
			if (ConnectionEstablished == false)
				return Error.NotConnected;

			if (chatInstance == null || string.IsNullOrEmpty(chatInstance.instanceId))
				return Error.InvalidArgument;

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					string chatId = chatInstance.instanceId;
					int updates = 0;
					int expectedUpdates = 0;

					DateTime now = DateTime.Now;
					long updatedAt = now.ToUnixTimeMilliseconds();

					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							// Delete all messages
							using (var cmdEditChat = connection.CreateCommand())
							{
								cmdEditChat.CommandText =
								@"
									DELETE FROM Message
									WHERE chatId = $chatId;
								";

								cmdEditChat.Parameters.AddWithValue("$chatId", chatId);

								int nDeleted = cmdEditChat.ExecuteNonQuery();
								expectedUpdates += nDeleted;
								updates += nDeleted;
							}
							
							// Update chat info
							using (var cmdEditChat = connection.CreateCommand())
							{
								cmdEditChat.CommandText =
								@"
									UPDATE Chat
									SET 
										updatedAt = $timestamp,
										name = $name
									WHERE id = $chatId;
								";

								cmdEditChat.Parameters.AddWithValue("$chatId", chatId);
								cmdEditChat.Parameters.AddWithValue("$timestamp", updatedAt);
								cmdEditChat.Parameters.AddWithValue("$name", chatInstance.name ?? "");

								expectedUpdates += 1;
								updates += cmdEditChat.ExecuteNonQuery();
							}
							
							// Write messages
							var lsMessages = new List<ChatHistory.Message>();
							int messageCount = chatInstance.history.messagesWithoutGreeting.Count();
							if (messageCount > 0)
							{
								// Generate unique IDs
								var messageIds = new string[messageCount];
								var swipeIds = new string[messageCount];
								for (int i = 0; i < messageIds.Length; ++i)
								{
									messageIds[i] = Cuid.NewCuid();
									swipeIds[i] = Cuid.NewCuid();
								}

								using (var cmdMessages = new SQLiteCommand(connection))
								{
									var sbCommand = new StringBuilder();
									sbCommand.AppendLine(
									@"
										INSERT INTO Message 
											(id, createdAt, updatedAt, chatId, characterConfigId)
										VALUES ");

									int i = 0;
									foreach (var message in chatInstance.history.messagesWithoutGreeting)
									{
										if (i > 0)
											sbCommand.Append(",\n");
										sbCommand.Append($"($messageId{i:000}, $createdAt{i:000}, $updatedAt{i:000}, $chatId, $charId{i:000})");

										cmdMessages.Parameters.AddWithValue($"$messageId{i:000}", messageIds[i]);
										cmdMessages.Parameters.AddWithValue($"$createdAt{i:000}", message.creationDate.ToUnixTimeMilliseconds());
										cmdMessages.Parameters.AddWithValue($"$updatedAt{i:000}", message.updateDate.ToUnixTimeMilliseconds());
										cmdMessages.Parameters.AddWithValue($"$charId{i:000}", chatInstance.participants[message.speaker]);

										lsMessages.Add(new ChatHistory.Message() {
											instanceId = messageIds[i],
											activeSwipe = 0,
											creationDate = message.creationDate,
											updateDate = message.updateDate,
											speaker = message.speaker,
											swipes = new string[1] { message.text },
										});
										++i;
									}
									sbCommand.Append(";");

									sbCommand.AppendLine(
									@"
										INSERT INTO RegenSwipe
											(id, createdAt, updatedAt, activeTimestamp, text, messageId)
										VALUES ");
									i = 0;
									foreach (var message in chatInstance.history.messagesWithoutGreeting)
									{
										if (i > 0)
											sbCommand.Append(",\n");
										sbCommand.Append($"($swipeId{i:000}, $updatedAt{i:000}, $updatedAt{i:000}, $updatedAt{i:000}, $text{i:000}, $messageId{i:000})");

										cmdMessages.Parameters.AddWithValue($"$swipeId{i:000}", swipeIds[i]);
										cmdMessages.Parameters.AddWithValue($"$text{i:000}", message.text);
										++i;
									}
									sbCommand.Append(";");
									cmdMessages.CommandText = sbCommand.ToString();

									cmdMessages.Parameters.AddWithValue("$chatId", chatId);

									expectedUpdates += messageCount * 2;
									updates += cmdMessages.ExecuteNonQuery();
								}
							}
							
							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								chatInstance = default(ChatInstance);
								return Error.SQLCommandFailed;
							}

							chatInstance = new ChatInstance() {
								instanceId = chatId,
								history = new ChatHistory() {
									messages = lsMessages.ToArray(),
								},
							};

							transaction.Commit();
							return Error.NoError;

						}
						catch (Exception e)
						{
							transaction.Rollback();

							chatInstance = default(ChatInstance);
							return Error.SQLCommandFailed;
						}
					}
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
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				return Error.Unknown;
			}
		}

		private struct _SwipeRepair
		{
			public string instanceId;
			public string chatId;
			public string text;
		}
		public static Error RepairChats(string groupId, out int modified)
		{
			if (ConnectionEstablished == false)
			{
				modified = 0;
				return Error.NotConnected;
			}

			if (string.IsNullOrEmpty(groupId))
			{
				modified = 0;
				return Error.InvalidArgument;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					// Find chats for group
					var chatIds = new List<string>();
					using (var cmdGetChats = connection.CreateCommand())
					{ 
						cmdGetChats.CommandText =
						@"
							SELECT id
							FROM Chat
							WHERE groupConfigId = $groupId
						";
						cmdGetChats.Parameters.AddWithValue("$groupId", groupId);

						using (var reader = cmdGetChats.ExecuteReader())
						{
							while (reader.Read())
								chatIds.Add(reader.GetString(0));
						}
					}

					// Find messages to fix
					var swipes = new List<_SwipeRepair>(512);
					for (int i = 0; i < chatIds.Count; ++i)
					{
						string chatId = chatIds[i];
						using (var cmdMessages = connection.CreateCommand())
						{
							cmdMessages.CommandText =
							@"
								SELECT 
									R.id, R.text 
								FROM RegenSwipe As R
								WHERE R.messageId IN (
									SELECT id
									FROM Message
									WHERE chatId = $chatId
								)
							";
							cmdMessages.Parameters.AddWithValue("$chatId", chatId);

							using (var reader = cmdMessages.ExecuteReader())
							{
								while (reader.Read())
								{
									string messageId = reader.GetString(0);
									string text = reader.GetString(1);

									swipes.Add(new _SwipeRepair() {
										instanceId = messageId,
										chatId = chatId,
										text = text,
									});
								}
							}
						}
					}

					// Fix strings
					var repairs = new List<_SwipeRepair>(512);
					foreach (var swipe in swipes)
					{
						bool bFront = swipe.text.BeginsWith("#{character}: ");
						bool bBack = swipe.text.EndsWith("\n#{user}: ");
						if (bFront && bBack)
						{
							repairs.Add(new _SwipeRepair() {
								instanceId = swipe.instanceId,
								chatId = swipe.chatId,
								text = swipe.text.Substring(14, swipe.text.Length - 24),
							});
						}
						else if (bFront)
						{
							repairs.Add(new _SwipeRepair() {
								instanceId = swipe.instanceId,
								chatId = swipe.chatId,
								text = swipe.text.Substring(14),
							});
						}
						else if (bBack)
						{
							repairs.Add(new _SwipeRepair() {
								instanceId = swipe.instanceId,
								chatId = swipe.chatId,
								text = swipe.text.Substring(swipe.text.Length - 10),
							});
						}
					}

					if (repairs.Count == 0)
					{
						modified = 0;
						return Error.NoError;
					}

					// Write to database
					int updates = 0;
					int expectedUpdates = 0;

					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							using (var cmdUpdateChat = connection.CreateCommand())
							{
								var sbCommand = new StringBuilder();
									
								for (int i = 0; i < repairs.Count; ++i)
								{
									sbCommand.AppendLine(
									$@"
										UPDATE RegenSwipe
										SET text = $text{i:000}
										WHERE id = $messageId{i:000};
									");
									cmdUpdateChat.Parameters.AddWithValue($"$messageId{i:000}", repairs[i].instanceId);
									cmdUpdateChat.Parameters.AddWithValue($"$text{i:000}", repairs[i].text);
									expectedUpdates += 1;
								}

								cmdUpdateChat.CommandText = sbCommand.ToString();
								updates += cmdUpdateChat.ExecuteNonQuery();
							}
		
							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								modified = 0;
								return Error.SQLCommandFailed;
							}

							transaction.Commit();
							modified = repairs.DistinctBy(r => r.chatId).Count();
							return Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();
							modified = 0;
							return Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				Disconnect();
				modified = 0;
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				Disconnect();
				modified = 0;
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				modified = 0;
				return Error.Unknown;
			}
		}

		#endregion // Chat

		#region Utilities
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
		#endregion // Utilities

		#region Validation
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
		#endregion // Validation
	}

	public static class SqlExtensions
	{
		public static DateTime GetUnixTime(this SQLiteDataReader reader, int index)
		{
			return DateTimeExtensions.FromUnixTime(reader.GetInt64(index));
		}
	}
}
