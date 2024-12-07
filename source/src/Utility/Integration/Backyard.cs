using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Text;
using Ginger.Properties;
using System.Globalization;

namespace Ginger.Integration
{
	public struct CharacterInstance
	{
		public string instanceId;       // CharacterConfig.id
		public DateTime creationDate;   // CharacterConfig.createdAt
		public DateTime updateDate;     // CharacterConfig.updatedAt
		public bool isUser;             // CharacterConfig.isUserControlled
		public string configId;         // CharacterConfigVersion.id
		public string displayName;      // CharacterConfigVersion.displayName
		public string name;             // CharacterConfigVersion.name
		public string groupId;          // GroupConfig.id (Primary group)
		public string folderId;         // GroupConfig.folderId (Primary group)
		public string creator;          // GroupConfig.hubAuthorUsername
		public string persona;          // CharacterConfigVersion.persona

		public string inferredGender { get { return Utility.InferGender(GingerString.FromFaraday(persona).ToString()); } }
		public int loreEntries;
	}

	public struct GroupInstance
	{
		public string instanceId;			// GroupConfig.id
		public string name;					// GroupConfig.name
		public string folderId;				// GroupConfig.folderId
		public string hubCharId;			// GroupConfig.hubCharId
		public string hubAuthorUsername;	// GroupConfig.hubAuthorUsername
		public DateTime creationDate;		// CharacterConfig.createdAt
		public DateTime updateDate;			// CharacterConfig.updatedAt
		public string[] members;			// CharacterConfigVersion.id ...

		public bool isEmpty
		{
			get { return string.IsNullOrEmpty(instanceId) || members == null || members.Length == 0; }
		}
	}

	public struct FolderInstance
	{
		public string instanceId;       // AppFolder.id
		public string parentId;         // AppFolder.parentFolderId
		public string name;             // AppFolder.name
		public string url;				// AppFolder.url
		public bool isRoot;

		public bool isEmpty { get { return string.IsNullOrEmpty(instanceId); } }
	}

	public class ImageInstance
	{
		public string instanceId;       // AppImage.id
		public string imageUrl;         // AppImage.imageUrl
		public string label;            // AppImage.label
		public int width;               // AppImage.aspectRatio
		public int height;              // AppImage.aspectRatio
	}

	[Serializable]
	public class ChatStaging
	{
		public string system = "";      // Chat.modelInstructions
		public string scenario = "";    // Chat.context
		public string greeting = "";    // Chat.greetingDialogue
		public string example = "";     // Chat.customDialogue
		public string grammar = "";     // Chat.grammar
		public ChatBackground background = null;
	}

	[Serializable]
	public class ChatBackground
	{
		public string instanceId;			// BackgroundChatImage.id
		public string imageUrl;				// BackgroundChatImage.imageUrl
		public int width;					// BackgroundChatImage.aspectRatio
		public int height;					// BackgroundChatImage.aspectRatio

		public string aspectRatio { get { return (width > 0 && height > 0) ? string.Format("{0}/{1}", width, height) : ""; } }
	}

	[Serializable]
	public class ChatParameters : ICloneable
	{
		public string name = "";					// Preset name
		public string model                         // Chat.model
		{ 
			get { return _model; }
			set
			{
				// Strip extension
				if (string.IsNullOrEmpty(value) || string.Compare(value, "default", StringComparison.OrdinalIgnoreCase) == 0)
					_model = null;
				else if (value.ToLowerInvariant().EndsWith(".gguf"))
					_model = Path.GetFileNameWithoutExtension(value);
				else
					_model = value;
			}
		}
		private string _model = "";
		public string modelForDB
		{
			get
			{
				if (_model == null)
					return Backyard.DefaultModel;
				return string.Concat(_model, ".gguf");
			}
		}

		public string authorNote = "";              // Chat.authorNote
		public decimal temperature = 1.2m;          // Chat.temperature
		public decimal topP = 0.9m;                 // Chat.topP
		public decimal minP = 0.1m;                 // Chat.minP
		public int topK = 30;                       // Chat.topK
		public bool minPEnabled = true;             // Chat.minPEnabled
		public decimal repeatPenalty = 1.05m;       // Chat.repeatPenalty
		public int repeatLastN = 256;				// Chat.repeatLastN
		public bool pruneExampleChat = true;        // Chat.canDeleteCustomDialogue
		public bool ttsAutoPlay = false;            // Chat.ttsAutoPlay
		public string ttsInputFilter = "default";   // Chat.ttsInputFilter
		private int _iPromptTemplate = 0;           // Chat.promptTemplate

		public static readonly int MaxPromptTemplate = 6;
		public int iPromptTemplate
		{
			get { return _iPromptTemplate; }
			set { _iPromptTemplate = Math.Min(Math.Max(value, 0), MaxPromptTemplate); }
		}

		public string promptTemplate
		{
			get
			{
				switch (_iPromptTemplate)
				{
				case 1: return "general";
				case 2: return "ChatML";
				case 3: return "Llama3";
				case 4: return "Gemma2";
				case 5: return "CommandR";
				case 6: return "MistralInstruct";
				default:
					return null;
				}
			}

			set
			{
				var sValue = (value ?? "").Trim().ToLowerInvariant();
				int iValue;
				if (sValue == "")
				{
					_iPromptTemplate = 0;
				}
				else if (int.TryParse(sValue, out iValue))
				{
					if (iValue >= 0 && iValue <= MaxPromptTemplate)
						_iPromptTemplate = iValue;
					else
						_iPromptTemplate = 0;
				}
				else
				{
					switch (sValue)
					{
					case "text":
					case "plain":
					case "general":
						_iPromptTemplate = 1;
						break;
					case "chatml":
						_iPromptTemplate = 2;
						break;
					case "llama3":
						_iPromptTemplate = 3;
						break;
					case "gemma":
					case "gemma2":
						_iPromptTemplate = 4;
						break;
					case "commandr":
					case "command-r":
						_iPromptTemplate = 5;
						break;
					case "mistral":
					case "mistralinstruct":
					case "mistral-instruct":
						_iPromptTemplate = 6;
						break;
					default:
						_iPromptTemplate = 0;
						break;
					}
				}
			}
		}

		public static ChatParameters Default
		{
			get
			{
				return new ChatParameters() {
					name = null,
					model = null,
					authorNote = "",
					temperature = 1.2m,
					topP = 0.9m,
					minP = 0.1m,
					topK = 30,
					minPEnabled = true,
					repeatLastN = 256,
					repeatPenalty = 1.05m,
					iPromptTemplate = 0,
					pruneExampleChat = true,
					ttsAutoPlay = false,
					ttsInputFilter = "default",
				};
			}
		}

		public object Clone()
		{
			return new ChatParameters() {
				name = this.name,
				_model = this._model,
				authorNote = this.authorNote,
				temperature = this.temperature,
				topP = this.topP,
				minP = this.minP,
				topK = this.topK,
				minPEnabled = this.minPEnabled,
				repeatLastN = this.repeatLastN,
				repeatPenalty = this.repeatPenalty,
				iPromptTemplate = this.iPromptTemplate,
				pruneExampleChat = this.pruneExampleChat,
				ttsAutoPlay = this.ttsAutoPlay,
				ttsInputFilter = this.ttsInputFilter,
			};
		}

		public ChatParameters Copy()
		{
			return (ChatParameters)Clone();
		}
	}

	public class ChatInstance
	{
		public ChatInstance()
		{
			creationDate = DateTime.Now;
			updateDate = DateTime.Now;
		}

		public string instanceId = null;    // Chat.id
		public DateTime creationDate;       // Chat.createdAt
		public DateTime updateDate;         // Chat.updatedAt

		public string[] participants = null;
		public ChatHistory history = new ChatHistory();
		public ChatStaging staging = null;
		public ChatParameters parameters = null;

		public string name { get { return history.name; } }
		public bool hasGreeting { get { return staging != null && string.IsNullOrEmpty(staging.greeting) == false; } }
		public bool hasBackground { get { return staging != null && staging.background != null && string.IsNullOrEmpty(staging.background.imageUrl) == false; } }

		public static string DefaultName = "Untitled chat";
	}

	public static class Backyard
	{
		public static IEnumerable<FolderInstance> Folders { get { return _Folders.Values; } }
		public static IEnumerable<CharacterInstance> Characters { get { return _Characters.Values; } }
		public static IEnumerable<CharacterInstance> CharactersNoUser { get { return Characters.Where(c => c.isUser == false); } }
		public static IEnumerable<GroupInstance> Groups { get { return _Groups.Values; } }
		public static string DefaultModel = null;
		public static string DefaultUserConfigId = null;
		public static string[] Models = null;

		private static Dictionary<string, FolderInstance> _Folders = new Dictionary<string, FolderInstance>();
		private static Dictionary<string, CharacterInstance> _Characters = new Dictionary<string, CharacterInstance>();
		private static Dictionary<string, GroupInstance> _Groups = new Dictionary<string, GroupInstance>();

		public static string LastError;

		public enum Feature
		{
			Undefined = 0,
			ChatBackgrounds = 1,
		}

		public static bool CheckFeature(Feature feature)
		{
			if (DatabaseVersion == BackyardDatabaseVersion.Unknown)
				return false;

			switch (feature)
			{
			case Feature.ChatBackgrounds:
				return DatabaseVersion >= BackyardDatabaseVersion.Version_0_29_0;
			}
			return false;
		}

		public class Link : IXmlLoadable, IXmlSaveable
		{
			public string characterId;
			public DateTime updateDate;
			public bool isActive;
			public bool isDirty;

			public string filenameHash
			{
				get { return _filenameHash; }
				set
				{
					if (string.IsNullOrEmpty(value))
						_filenameHash = null;
					else
					{
						using (var sha1 = new System.Security.Cryptography.SHA256CryptoServiceProvider())
						{
							byte[] bytes = Encoding.UTF8.GetBytes(value.ToLowerInvariant());
							_filenameHash = string.Concat(sha1.ComputeHash(bytes).Select(x => x.ToString("X2")));
						}
					}
				}
			}
			private string _filenameHash = null;

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

				_filenameHash = xmlNode.GetValueElement("File");

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

				xmlNode.AddValueElement("File", _filenameHash);

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

				if (CompareFilename(Current.Filename) == false)
					isActive = false; // Different file
			}

			public bool CompareFilename(string otherFilename)
			{
				if (string.IsNullOrEmpty(otherFilename))
					return string.IsNullOrEmpty(_filenameHash);
				else if (_filenameHash == null)
					return false;

				using (var sha1 = new System.Security.Cryptography.SHA256CryptoServiceProvider())
				{
					byte[] bytes = Encoding.UTF8.GetBytes(otherFilename.ToLowerInvariant());
					string hash = string.Concat(sha1.ComputeHash(bytes).Select(x => x.ToString("X2")));
					return hash.Equals(_filenameHash);
				}
			}
		}

		public enum Error
		{
			NoError,
			NotConnected,
			InvalidArgument,
			ValidationFailed,
			SQLCommandFailed,
			NotFound,
			DismissedByUser,
			CancelledByUser,
			Unknown,
		}

		public static bool ConnectionEstablished = false;

		public static BackyardDatabaseVersion DatabaseVersion = BackyardDatabaseVersion.Unknown;

		private static SQLiteConnection CreateSQLiteConnection()
		{
			string backyardPath = AppSettings.BackyardLink.Location;
			if (string.IsNullOrWhiteSpace(backyardPath))
			{
#if DEBUG
				string appPath = "faraday-canary"; // Use canary database during development and testing
#else
				string appPath = "faraday"; // Use production database 
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

					// Match tables
					var foundTables = new HashSet<string>();
					using (var cmdTable = connection.CreateCommand())
					{
						cmdTable.CommandText =
						@"
							SELECT name 
							FROM sqlite_schema
							WHERE type = 'table' AND name NOT LIKE 'sqlite_%';
						";

						using (var reader = cmdTable.ExecuteReader())
						{
							while (reader.Read())
							{
								foundTables.Add(reader.GetString(0));
							}
						}
					}

					// Detect database version
					if (foundTables.Contains("BackgroundChatImage"))
						DatabaseVersion = BackyardDatabaseVersion.Version_0_29_0;
					else if (foundTables.Contains("GroupConfig"))
						DatabaseVersion = BackyardDatabaseVersion.Version_0_28_0;
					else // Outdated or unknown
					{
						LastError = "Validation failed";
						return Error.ValidationFailed;
					}

					// Compare database structure with known tables
					var validationTable = BackyardValidation.TablesByVersion[DatabaseVersion];
					if (AppSettings.BackyardLink.Strict && foundTables.Count != validationTable.Length)
					{
						LastError = "Validation failed";
						return Error.ValidationFailed;
					}

					// Check table names and columns
					for (int i = 0; i < validationTable.Length; ++i)
					{
						string[] table = validationTable[i];
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

						if (expectedNames.Length == 0 && foundColumns.Count > 0)
							continue; // A table we want to exist, but don't care about its columns/contents

						if ((AppSettings.BackyardLink.Strict && foundColumns.Count != expectedNames.Length)
							|| foundColumns.Count < expectedNames.Length)
						{
							LastError = "Validation failed";
							return Error.ValidationFailed;
						}

						for (int j = 0; j < expectedNames.Length; ++j)
						{
							int idxFound = foundColumns.FindIndex(kvp => kvp.Key == expectedNames[j]);
							if (idxFound == -1 || foundColumns[idxFound].Value != expectedTypes[j])
							{
								LastError = "Validation failed";
								return Error.ValidationFailed;
							}
						}
					}

					// Read settings
					using (var cmdSettings = connection.CreateCommand())
					{
						cmdSettings.CommandText = @"SELECT model, modelDownloadLocation FROM AppSettings";

						using (var reader = cmdSettings.ExecuteReader())
						{
							if (reader.Read())
							{
								DefaultModel = reader[0] as string;
								string modelDirectory = reader[1] as string;
								FindModels(modelDirectory);
							}
							else
							{
								DefaultModel = null;
								Models = null;
							}
						}
					}

					connection.Close();
					ConnectionEstablished = true;
					LastError = null;
					return Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				Disconnect();
				LastError = e.Message;
				return Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				Disconnect();
				LastError = e.Message;
				return Error.ValidationFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				LastError = e.Message;
				return Error.Unknown;
			}
		}

		public static void Disconnect()
		{
			_Folders.Clear();
			_Characters.Clear();
			_Groups.Clear();
			ConnectionEstablished = false;
			DatabaseVersion = BackyardDatabaseVersion.Unknown;
			AppSettings.BackyardLink.Enabled = false;
			SQLiteConnection.ClearAllPools(); // Releases the lock on the db file
		}

		private static void FindModels(string modelDirectory)
		{
			try
			{
				Models = Directory.EnumerateFiles(modelDirectory, "*.gguf")
					.Select(fn => Path.GetFileNameWithoutExtension(fn))
					.OrderBy(fn => fn)
					.ToArray();
			}
			catch
			{
				Models = null;
			}
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

		public static GroupInstance GetGroupForCharacter(string characterId)
		{
			return _Groups.Values.FirstOrDefault(g => g.members.Length == 2 && g.members.Contains(characterId));
		}

		public static Error RefreshCharacters()
		{
			if (ConnectionEstablished == false)
			{
				LastError = "Not connected";
				return Error.NotConnected;
			}

			_Folders.Clear();
			_Characters.Clear();
			_Groups.Clear();

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					// Fetch character-group memberships
					var groupMembers = new Dictionary<string, HashSet<string>>();
					var characterGroups = new Dictionary<string, string>();
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
								string characterId = reader.GetString(0);
								string groupId = reader.GetString(1);

								if (groupMembers.ContainsKey(groupId) == false)
									groupMembers.Add(groupId, new HashSet<string>());

								groupMembers[groupId].Add(characterId);

								characterGroups.TryAdd(characterId, groupId);
							}
						}
					}

					// Fetch group configs
					using (var cmdGroupData = connection.CreateCommand())
					{ 
						cmdGroupData.CommandText =
						@"
							SELECT 
								id, createdAt, updatedAt, name, folderId, hubCharId, hubAuthorUsername
							FROM GroupConfig
						";

						using (var reader = cmdGroupData.ExecuteReader())
						{
							while (reader.Read())
							{
								string instanceId = reader.GetString(0);
								DateTime createdAt = reader.GetTimestamp(1);
								DateTime updatedAt = reader.GetTimestamp(2);
								string name = reader.GetString(3);
								string folderId = reader.GetString(4);
								string hubCharId = reader[5] as string;
								string hubAuthorUsername = reader[6] as string;

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
										hubCharId = hubCharId,
										hubAuthorUsername = hubAuthorUsername,
										members = members.ToArray(),
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
								id, parentFolderId, name, url, isRoot
							FROM AppFolder
						";

						using (var reader = cmdFolderData.ExecuteReader())
						{
							while (reader.Read())
							{
								string instanceId = reader.GetString(0);
								string parentId = reader[1] as string;
								string name = reader.GetString(2);
								string url = reader.GetString(3);
								bool isRoot = reader.GetBoolean(4);

								_Folders.TryAdd(instanceId, 
									new FolderInstance() {
										instanceId = instanceId,
										parentId = parentId,
										name = name,
										url = url,
										isRoot = isRoot,
									});
							}
						}
					}

					// Fetch character configs
					using (var cmdCharacter = connection.CreateCommand())
					{
						cmdCharacter.CommandText =
						@"
							SELECT 
								A.id, B.id, B.displayName, B.name, A.createdAt, B.updatedAt, B.persona, A.isUserControlled,
								( SELECT COUNT(*) FROM _AppCharacterLorebookItemToCharacterConfigVersion WHERE ""B"" = B.id )
							FROM CharacterConfig as A
							INNER JOIN CharacterConfigVersion AS B ON B.characterConfigId = A.id;
						";
						using (var reader = cmdCharacter.ExecuteReader())
						{
							while (reader.Read())
							{
								string instanceId = reader.GetString(0);
								string configId = reader.GetString(1);
								string displayName = reader.GetString(2);
								string name = reader.GetString(3);
								DateTime createdAt = reader.GetTimestamp(4);
								DateTime updatedAt = reader.GetTimestamp(5);
								string persona = reader.GetString(6);
								bool isUser = reader.GetBoolean(7);
								int numLoreEntries = reader.GetInt32(8);

								string groupId;
								if (characterGroups.TryGetValue(instanceId, out groupId) == false)
									continue; // No group

								// Get info from group
								string folderId = null;
								string hubCharId = null;
								string hubAuthorUsername = null;
								GroupInstance groupInstance;
								if (_Groups.TryGetValue(groupId, out groupInstance))
								{
									folderId = groupInstance.folderId;
									hubCharId = groupInstance.hubCharId;
									hubAuthorUsername = groupInstance.hubAuthorUsername;
								}

								_Characters.TryAdd(instanceId, 
									new CharacterInstance() {
										instanceId = instanceId,
										configId = configId,
										groupId = groupId,
										displayName = displayName,
										name = name,
										creationDate = createdAt,
										updateDate = updatedAt,
										isUser = isUser,
										persona = persona,
										loreEntries = numLoreEntries,
										creator = hubAuthorUsername ?? "",
										folderId = folderId,
									});
							}
						}
					}

					connection.Close();
					LastError = null;
					return Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				Disconnect();
				LastError = "File not found";
				return Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				Disconnect();
				LastError = "Sqlite returned: " + e.Message;
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				LastError = "Exception thrown: " + e.Message;
				return Error.Unknown;
			}
		}
		#endregion

		#region Characters
		public static Error ImportCharacter(CharacterInstance character, out FaradayCardV4 card, out string[] imageUrls)
		{
			string[] tmp;
			return ImportCharacter(character, out card, out imageUrls, out tmp);
		}

		public static Error ImportCharacter(CharacterInstance character, out FaradayCardV4 card, out string[] imageUrls, out string[] backgroundUrls)
		{
			if (ConnectionEstablished == false)
			{
				card = null;
				imageUrls = null;
				backgroundUrls = null;
				LastError = "Not connected";
				return Error.NotConnected;
			}
			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					using (var cmdCharacterData = connection.CreateCommand())
					{
						var sbCommand = new StringBuilder();
						sbCommand.AppendLine(
						@"
							SELECT 
								A.id, A.createdAt, A.updatedAt, A.displayName, A.name, A.persona, 
								C.context, C.customDialogue, C.modelInstructions, C.greetingDialogue, C.grammar
							FROM CharacterConfigVersion as A
							INNER JOIN _CharacterConfigToGroupConfig AS B 
								ON B.A = $charId
							INNER JOIN Chat AS C 
								ON C.groupConfigId = ""B"".B
							WHERE A.id = $configId
						");

						if (AppSettings.BackyardLink.ApplyChatSettings == AppSettings.BackyardLink.ActiveChatSetting.First)
							sbCommand.AppendLine("ORDER BY C.createdAt ASC");
						else
							sbCommand.AppendLine("ORDER BY C.createdAt DESC");

						cmdCharacterData.CommandText = sbCommand.ToString();
						
						cmdCharacterData.Parameters.AddWithValue("$charId", character.instanceId);
						cmdCharacterData.Parameters.AddWithValue("$configId", character.configId);

						card = null;

						var characterInstanceIds = new HashSet<string>();
						using (var reader = cmdCharacterData.ExecuteReader())
						{
							if (!reader.Read())
							{
								imageUrls = null;
								backgroundUrls = null;
								return Error.NotFound; // No character
							}

							string instanceId = reader.GetString(0);
							DateTime createdAt = reader.GetTimestamp(1);
							DateTime updatedAt = reader.GetTimestamp(2);
							string displayName = reader.GetString(3);
							string name = reader.GetString(4);
							string persona = reader.GetString(5);
							string scenario = reader.GetString(6);
							string example = reader.GetString(7);
							string system = reader.GetString(8);
							string greeting = reader.GetString(9);
							string grammar = reader[10] as string;

							// Get info from group
							string hubCharId = null;
							string hubAuthorUsername = null;
							GroupInstance groupInstance;
							if (_Groups.TryGetValue(character.groupId, out groupInstance))
							{
								hubCharId = groupInstance.hubCharId;
								hubAuthorUsername = groupInstance.hubAuthorUsername;
							}

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
							card.hubAuthorUsername = hubAuthorUsername;
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

					// Gather portrait image files
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

					// Gather background image files
					var lsBackgroundUrls = new List<string>();
					if (CheckFeature(Feature.ChatBackgrounds))
					{
						// Get existing chats
						List<_Chat> chats;
						List<_Background> backgrounds;
						if (FetchChatInstances(connection, character.groupId, out chats) == Error.NoError
							&& FetchChatBackgrounds(connection, out backgrounds) == Error.NoError)
						{
							foreach (var chat in chats)
							{
								int idxBackground = backgrounds.FindIndex(b => b.chatId == chat.instanceId);
								if (idxBackground == -1)
									continue;

								lsBackgroundUrls.Add(backgrounds[idxBackground].imageUrl);
							}
						}
						lsBackgroundUrls = lsBackgroundUrls
							.Distinct()
							.Where(s => string.IsNullOrEmpty(s) == false && File.Exists(s))
							.ToList();
					}

					imageUrls = lsImageUrls.ToArray();
					backgroundUrls = lsBackgroundUrls.ToArray();
					connection.Close();
					LastError = null;
					return card == null ? Error.NotFound : Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				Disconnect();
				card = null;
				imageUrls = null;
				backgroundUrls = null;
				LastError = "File not found";
				return Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				Disconnect();
				card = null;
				imageUrls = null;
				backgroundUrls = null;
				LastError = e.Message;
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				card = null;
				imageUrls = null;
				backgroundUrls = null;
				LastError = e.Message;
				return Error.Unknown;
			}
		}

		public static Error GetImageUrls(CharacterInstance characterInstance, out string[] imageUrls)
		{
			if (ConnectionEstablished == false)
			{
				imageUrls = null;
				return Error.NotConnected;
			}

			if (characterInstance.configId == null)
			{
				imageUrls = null;
				return Error.InvalidArgument;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

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
						cmdImageLookup.Parameters.AddWithValue("$configId", characterInstance.configId);

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
					return Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				imageUrls = null;
				return Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				imageUrls = null;
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				imageUrls = null;
				return Error.Unknown;
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
								A.id, B.B, A.updatedAt
							FROM CharacterConfigVersion AS A
							INNER JOIN _CharacterConfigToGroupConfig AS B 
								ON B.A = $charId
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
							string groupId = reader.GetString(1); // Primary group
							DateTime updatedAt = reader.GetTimestamp(2);
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
				return Error.NotConnected;
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

					// Get existing chats
					List<_Chat> chats;
					var error = FetchChatInstances(connection, groupId, out chats);
					if (error != Error.NoError)
					{
						updateDate = default(DateTime);
						updatedImageLinks = null;
						return error;
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
					bool bUpdateImages = PrepareImageUpdates(imageInstances, linkInfo.imageLinks, out images, out imageLinks);

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
								if (AppSettings.BackyardLink.ApplyChatSettings == AppSettings.BackyardLink.ActiveChatSetting.All)
								{
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
									cmdChat.Parameters.AddWithValue("$groupId", groupId);
								}
								else
								{
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

									if (AppSettings.BackyardLink.ApplyChatSettings == AppSettings.BackyardLink.ActiveChatSetting.Last)
										cmdChat.Parameters.AddWithValue("$chatId", chats.OrderByDescending(c => c.updateDate).Select(c => c.instanceId).First());
									else // First
										cmdChat.Parameters.AddWithValue("$chatId", chats.OrderBy(c => c.creationDate).Select(c => c.instanceId).First());
								}
								
								cmdChat.CommandText = sbCommand.ToString();
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
									cmdImage.Parameters.AddWithValue($"$aspectRatio{i:000}", images[i].aspectRatio);
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

										cmdLore.Parameters.AddWithValue($"$id{i:000}", existingLoreItems[i]);
										cmdLore.Parameters.AddWithValue($"$key{i:000}", card.data.loreItems[i].key);
										cmdLore.Parameters.AddWithValue($"$value{i:000}", card.data.loreItems[i].value);
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
				return Error.NotConnected;
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

		public static Error CreateNewCharacter(FaradayCardV4 card, ImageInput[] imageInput, BackupData.Chat[] chats, out CharacterInstance characterInstance, out Link.Image[] imageLinks, FolderInstance folder = default(FolderInstance))
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

			if (chats != null && chats.ContainsAny(c => c.history != null && c.history.numSpeakers > 2))
			{
				characterInstance = default(CharacterInstance);
				imageLinks = null;
				return Error.InvalidArgument;
			}

			FolderInstance parentFolder = folder;
			if (parentFolder.isEmpty)
			{
				// Get root folder
				parentFolder = _Folders.Values.FirstOrDefault(f => f.isRoot);
				if (parentFolder.isEmpty)
				{
					characterInstance = default(CharacterInstance);
					imageLinks = null;
					return Error.Unknown;
				}
			}

			bool bAllowBackgrounds = CheckFeature(Feature.ChatBackgrounds);

			// Prepare image information
			List<ImageOutput> images = new List<ImageOutput>();
			List<ImageOutput> backgrounds = new List<ImageOutput>();
			ImageOutput[] imageOutput;
			Dictionary<string, ImageOutput> backgroundUrlByName = new Dictionary<string, ImageOutput>();
			if (PrepareImages(imageInput, out imageOutput, out imageLinks))
			{
				for (int i = 0; i < imageOutput.Length && i < imageInput.Length; ++i)
				{
					if (imageInput[i].asset != null && imageInput[i].asset.assetType == AssetFile.AssetType.Background)
					{
						backgrounds.Add(imageOutput[i]);
						backgroundUrlByName.TryAdd(imageInput[i].asset.name ?? "", imageOutput[i]);
					}
					else
						images.Add(imageOutput[i]);
				}

				if (bAllowBackgrounds)
				{
					if (chats == null || chats.Length == 0)
						backgrounds = backgrounds.Take(1).ToList();
				}
				else
					backgrounds.Clear();

				imageOutput = Utility.ConcatenateArrays(images.ToArray(), backgrounds.ToArray());
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					string characterId	= Cuid.NewCuid();
					string configId		= Cuid.NewCuid();
					string groupId		= Cuid.NewCuid();
					string userId		= null;
					DateTime now = DateTime.Now;
					long createdAt = now.ToUnixTimeMilliseconds();
					string folderOrder = null;
					string[] chatIds = null;
					ChatParameters chatParameters = AppSettings.BackyardSettings.UserSettings;

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
						cmdFolderOrder.Parameters.AddWithValue("$folderId", parentFolder.instanceId);
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
										(id, createdAt, updatedAt, isNSFW, folderId, folderSortPosition, name)
									VALUES 
										($groupId, $timestamp, $timestamp, $isNSFW, $folderId, $folderSortPosition, '');
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

								expectedUpdates += 5;

								if (chats == null || chats.Length == 0)
								{
									string chatId = Cuid.NewCuid();

									// Create first chat
									sbCommand.AppendLine(
									@"
										INSERT INTO Chat
											(id, name, createdAt, updatedAt,
												groupConfigId, 
												modelInstructions, context, greetingDialogue, customDialogue, grammar, 
												model, temperature, topP, minP, minPEnabled, topK, 
												repeatPenalty, repeatLastN, promptTemplate, canDeleteCustomDialogue, authorNote)
										VALUES 
											($chatId, '', $timestamp, $timestamp, 
												$groupId,
												$system, $scenario, $greeting, $example, $grammar, 
												$model, $temperature, $topP, $minP, $minPEnabled, $topK, 
												$repeatPenalty, $repeatLastN, $promptTemplate, $pruneExample, $authorNote);
									");
	
									cmdCreate.Parameters.AddWithValue("$chatId", chatId);
									cmdCreate.Parameters.AddWithValue("$system", card.data.system);
									cmdCreate.Parameters.AddWithValue("$scenario", card.data.scenario);
									cmdCreate.Parameters.AddWithValue("$example", card.data.example);
									cmdCreate.Parameters.AddWithValue("$greeting", card.data.greeting);
									cmdCreate.Parameters.AddWithValue("$grammar", card.data.grammar ?? "");
									cmdCreate.Parameters.AddWithValue("$model", chatParameters.modelForDB);
									cmdCreate.Parameters.AddWithValue("$temperature", chatParameters.temperature);
									cmdCreate.Parameters.AddWithValue("$topP", chatParameters.topP);
									cmdCreate.Parameters.AddWithValue("$minP", chatParameters.minP);
									cmdCreate.Parameters.AddWithValue("$minPEnabled", chatParameters.minPEnabled);
									cmdCreate.Parameters.AddWithValue("$topK", chatParameters.topK);
									cmdCreate.Parameters.AddWithValue("$repeatPenalty", chatParameters.repeatPenalty);
									cmdCreate.Parameters.AddWithValue("$repeatLastN", chatParameters.repeatLastN);
									cmdCreate.Parameters.AddWithValue("$promptTemplate", chatParameters.promptTemplate);
									cmdCreate.Parameters.AddWithValue("$pruneExample", chatParameters.pruneExampleChat);
									cmdCreate.Parameters.AddWithValue("$authorNote", chatParameters.authorNote ?? "");

									expectedUpdates += 1;

									// Add background image
									if (bAllowBackgrounds && backgrounds.Count > 0)
									{
										// BackgroundChatImage
										sbCommand.AppendLine(
										$@"
											INSERT INTO BackgroundChatImage
												(id, imageUrl, aspectRatio, chatId)
											VALUES 
												($backgroundId, $backgroundImageUrl, $backgroundAspectRatio, $chatId);
										");

										cmdCreate.Parameters.AddWithValue($"$backgroundId", Cuid.NewCuid());
										cmdCreate.Parameters.AddWithValue($"$backgroundImageUrl", backgrounds[0].imageUrl);
										cmdCreate.Parameters.AddWithValue($"$backgroundAspectRatio", backgrounds[0].aspectRatio);

										expectedUpdates += 1;
									}
								}
								else
								{
									// Generate unique IDs
									chatIds = new string[chats.Length];
									for (int i = 0; i < chatIds.Length; ++i)
										chatIds[i] = Cuid.NewCuid();

									for (int i = 0; i < chats.Length; ++i)
									{
										sbCommand.AppendLine(
										$@"
											INSERT INTO Chat
												(id, name, createdAt, updatedAt, 
													groupConfigId,
													modelInstructions, context, greetingDialogue, customDialogue, grammar,
													model, temperature, topP, minP, minPEnabled, topK, 
													repeatPenalty, repeatLastN, promptTemplate, canDeleteCustomDialogue, 
													authorNote, ttsAutoPlay, ttsInputFilter)
											VALUES 
												($chatId{i:000}, $chatName{i:000}, $chatCreatedAt{i:000}, $chatUpdatedAt{i:000}, 
													$groupId, 
													$system{i:000}, $scenario{i:000}, $greeting{i:000}, $example{i:000}, $grammar{i:000}, 
													$model{i:000}, $temperature{i:000}, $topP{i:000}, $minP{i:000}, $minPEnabled{i:000}, $topK{i:000}, 
													$repeatPenalty{i:000}, $repeatLastN{i:000}, $promptTemplate{i:000}, $pruneExample{i:000},
													$authorNote{i:000}, $ttsAutoPlay{i:000}, $ttsInputFilter{i:000});
										");
										cmdCreate.Parameters.AddWithValue($"$chatId{i:000}", chatIds[i]);
										cmdCreate.Parameters.AddWithValue($"$chatName{i:000}", chats[i].name ?? "");
										cmdCreate.Parameters.AddWithValue($"$chatCreatedAt{i:000}", chats[i].creationDate.ToUnixTimeMilliseconds());
										cmdCreate.Parameters.AddWithValue($"$chatUpdatedAt{i:000}", chats[i].updateDate.ToUnixTimeMilliseconds());

										var staging = chats[i].staging ?? new ChatStaging() {
											system = card.data.system,
											scenario = card.data.scenario,
											greeting = card.data.greeting,
											example = card.data.example,
											grammar = card.data.grammar,
										};

										var parameters = chats[i].parameters ?? new ChatParameters();
										cmdCreate.Parameters.AddWithValue($"$system{i:000}", staging.system);
										cmdCreate.Parameters.AddWithValue($"$scenario{i:000}", staging.scenario);
										cmdCreate.Parameters.AddWithValue($"$example{i:000}", staging.example);
										cmdCreate.Parameters.AddWithValue($"$greeting{i:000}", staging.greeting);
										cmdCreate.Parameters.AddWithValue($"$grammar{i:000}", staging.grammar ?? "");
										cmdCreate.Parameters.AddWithValue($"$model{i:000}", parameters.modelForDB);
										cmdCreate.Parameters.AddWithValue($"$temperature{i:000}", parameters.temperature);
										cmdCreate.Parameters.AddWithValue($"$topP{i:000}", parameters.topP);
										cmdCreate.Parameters.AddWithValue($"$minP{i:000}", parameters.minP);
										cmdCreate.Parameters.AddWithValue($"$minPEnabled{i:000}", parameters.minPEnabled);
										cmdCreate.Parameters.AddWithValue($"$topK{i:000}", parameters.topK);
										cmdCreate.Parameters.AddWithValue($"$repeatPenalty{i:000}", parameters.repeatPenalty);
										cmdCreate.Parameters.AddWithValue($"$repeatLastN{i:000}", parameters.repeatLastN);
										cmdCreate.Parameters.AddWithValue($"$promptTemplate{i:000}", parameters.promptTemplate);
										cmdCreate.Parameters.AddWithValue($"$pruneExample{i:000}", parameters.pruneExampleChat);
										cmdCreate.Parameters.AddWithValue($"$authorNote{i:000}", parameters.authorNote ?? "");
										cmdCreate.Parameters.AddWithValue($"$ttsAutoPlay{i:000}", parameters.ttsAutoPlay);
										cmdCreate.Parameters.AddWithValue($"$ttsInputFilter{i:000}", parameters.ttsInputFilter);

										// Add background images
										if (bAllowBackgrounds)
										{
											if (string.IsNullOrEmpty(chats[i].backgroundName))
												continue;

											ImageOutput background;
											if (backgroundUrlByName.TryGetValue(chats[i].backgroundName, out background) == false)
												continue;

											// BackgroundChatImage
											sbCommand.AppendLine(
											$@"
												INSERT INTO BackgroundChatImage
													(id, imageUrl, aspectRatio, chatId)
												VALUES 
													($backgroundId{i:000}, $backgroundImageUrl{i:000}, $backgroundAspectRatio{i:000}, $chatId{i:000});
											");

											cmdCreate.Parameters.AddWithValue($"$backgroundId{i:000}", Cuid.NewCuid());
											cmdCreate.Parameters.AddWithValue($"$backgroundImageUrl{i:000}", background.imageUrl);
											cmdCreate.Parameters.AddWithValue($"$backgroundAspectRatio{i:000}", background.aspectRatio);

											expectedUpdates += 1;
										}
									}

									expectedUpdates += chats.Length;
								}

								// Add images
								for (int i = 0; i < images.Count; ++i)
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

									cmdCreate.Parameters.AddWithValue($"$imageId{i:000}", imageOutput[i].instanceId);
									cmdCreate.Parameters.AddWithValue($"$imageUrl{i:000}", imageOutput[i].imageUrl);
									cmdCreate.Parameters.AddWithValue($"$label{i:000}", imageOutput[i].label ?? "");
									cmdCreate.Parameters.AddWithValue($"$aspectRatio{i:000}", imageOutput[i].aspectRatio);

									expectedUpdates += 2;
								}

								cmdCreate.CommandText = sbCommand.ToString();
								cmdCreate.Parameters.AddWithValue("$charId", characterId);
								cmdCreate.Parameters.AddWithValue("$userId", userId);
								cmdCreate.Parameters.AddWithValue("$configId", configId);
								cmdCreate.Parameters.AddWithValue("$groupId", groupId);
								cmdCreate.Parameters.AddWithValue("$name", card.data.name);
								cmdCreate.Parameters.AddWithValue("$displayName", card.data.displayName);
								cmdCreate.Parameters.AddWithValue("$persona", card.data.persona);
								cmdCreate.Parameters.AddWithValue("$folderId", parentFolder.instanceId);
								cmdCreate.Parameters.AddWithValue("$folderSortPosition", MakeFolderSortPosition(folderOrder));
								cmdCreate.Parameters.AddWithValue("$isNSFW", card.data.isNSFW);
								cmdCreate.Parameters.AddWithValue("$timestamp", createdAt);

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

							// Write messages
							if (chats != null)
							{
								var speakerIds = new string[] { userId, characterId };
								for (int i = 0; i < chats.Length; ++i)
									WriteChatMessages(connection, chatIds[i], chats[i].history, speakerIds, ref expectedUpdates, ref updates);
							}
							
							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								characterInstance = default(CharacterInstance);
								imageLinks = null;
								return Error.SQLCommandFailed;
							}

							// Write images to disk
							if (imageOutput != null)
							{
								foreach (var image in imageOutput.Where(i => i.data.isEmpty == false))
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
								folderId = parentFolder.instanceId,
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
				return Error.NotConnected;
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

		public struct ChatCount 
		{
			public int count;
			public DateTime lastMessage;
			public bool hasMessages { get { return lastMessage > DateTime.MinValue; } }
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
								(
									SELECT MAX(M.updatedAt)
									FROM Message as M
									WHERE M.chatId = C.id
								)
							FROM Chat AS C
						";

						using (var reader = cmdChat.ExecuteReader())
						{
							while (reader.Read())
							{
								string groupId = reader.GetString(0);
								DateTime lastMessage = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetTimestamp(1);

								if (counts.ContainsKey(groupId) == false)
								{
									counts.Add(groupId, new ChatCount() {
										count = 1,
										lastMessage = lastMessage,
									});
								}
								else
								{
									counts[groupId] = new ChatCount() {
										count = counts[groupId].count + 1,
										lastMessage = DateTimeExtensions.Max(counts[groupId].lastMessage, lastMessage),
									};
								}
							}
						}
					}
					
					return Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				counts = null;
				return Error.NotConnected;
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
		
		// Intermediaries
		private struct _Character
		{
			public string instanceId;
			public string name;
			public bool isUser;
		}

		private struct _Chat
		{
			public string instanceId;
			public string name;
			public DateTime creationDate;
			public DateTime updateDate;
			public ChatStaging staging;
			public ChatParameters parameters;
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

		private struct _Background
		{
			public string instanceId;
			public string chatId;
			public string imageUrl;
			public int width;
			public int height;
		}

		public static Error GetChats(string groupId, out ChatInstance[] chatInstances)
		{
			if (ConnectionEstablished == false)
			{
				chatInstances = null;
				return Error.NotConnected;
			}

			if (string.IsNullOrEmpty(groupId))
			{
				chatInstances = null;
				return Error.NotFound;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					List<_Character> characters;
					var error = FetchCharacters(connection, out characters);
					if (error != Error.NoError)
					{
						chatInstances = null;
						return error;
					}

					List<_Character> groupMembers;
					error = FetchGroupMembers(connection, groupId, null, out groupMembers);
					if (error != Error.NoError)
					{
						chatInstances = null;
						return error;
					}

					List<_Background> backgrounds;
					FetchChatBackgrounds(connection, out backgrounds);

					var lsChatInstances = new List<ChatInstance>();
					var chats = new List<_Chat>();
					using (var cmdChat = connection.CreateCommand())
					{
						cmdChat.CommandText =
						@"
							SELECT 
								id, name, createdAt, updatedAt, 
								modelInstructions, context, greetingDialogue, customDialogue, grammar,
								model, temperature, topP, minP, minPEnabled, topK, 
								repeatPenalty, repeatLastN, promptTemplate, canDeleteCustomDialogue, 
								authorNote, ttsAutoplay, ttsInputFilter
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
								DateTime createdAt = reader.GetTimestamp(2);
								DateTime updatedAt = reader.GetTimestamp(3);

								// Staging
								string system = reader.GetString(4);
								string scenario = reader.GetString(5);
								string greeting = reader.GetString(6);
								string example = reader.GetString(7);
								string grammar = reader[8] as string;

								// Parameters
								string model = reader.GetString(9);
								decimal temperature = reader.GetDecimal(10);
								decimal topP = reader.GetDecimal(11);
								decimal minP = reader.GetDecimal(12);
								bool minPEnabled = reader.GetBoolean(13);
								int topK = reader.GetInt32(14);
								decimal repeatPenalty = reader.GetDecimal(15);
								int repeatLastN = reader.GetInt32(16);
								string promptTemplate = reader[17] as string;
								bool pruneExampleChat = reader.GetBoolean(18);
								string authorNote = reader.GetString(19);
								bool ttsAutoPlay = reader.GetBoolean(20);
								string ttsInputFilter = reader.GetString(21);

								if (string.IsNullOrWhiteSpace(name))
								{
									if (++untitledCounter > 1)
										name = string.Concat(ChatInstance.DefaultName, " #", untitledCounter.ToString());
									else
										name = ChatInstance.DefaultName;
								}

								ChatBackground background = null;
								int idxBackground = backgrounds.FindIndex(b => b.chatId == chatId);
								if (idxBackground != -1)
								{
									var bg = backgrounds[idxBackground];
									background = new ChatBackground() {
										instanceId = bg.instanceId,
										imageUrl = bg.imageUrl,
										width = bg.width,
										height = bg.height,
									};
								}

								chats.Add(new _Chat() {
									instanceId = chatId,
									creationDate = createdAt,
									updateDate = updatedAt,
									name = name,
									staging = new ChatStaging() {
										system = system,
										scenario = scenario,
										greeting = greeting,
										example = example,
										grammar = grammar,
										background = background,
									},
									parameters = new ChatParameters() {
										model = model,
										temperature = temperature,
										topP = topP,
										minP = minP,
										minPEnabled = minPEnabled,
										topK = topK,
										repeatPenalty = repeatPenalty,
										repeatLastN = repeatLastN,
										promptTemplate = promptTemplate,
										pruneExampleChat = pruneExampleChat,
										authorNote = authorNote,
										ttsAutoPlay = ttsAutoPlay,
										ttsInputFilter = ttsInputFilter,
									}
								});
							}
						}
					}

					// Collect messages
					for (int i = 0; i < chats.Count; ++i)
					{
						var chatInstance = FetchChatInstance(connection, chats[i], characters, groupMembers);
						if (chatInstance != null)
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
				return Error.NotConnected;
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

		public static Error GetChat(string chatId, string groupId, out ChatInstance chatInstance)
		{
			if (ConnectionEstablished == false)
			{
				chatInstance = null;
				return Error.NotConnected;
			}

			if (string.IsNullOrEmpty(chatId) || string.IsNullOrEmpty(groupId))
			{
				chatInstance = null;
				return Error.InvalidArgument;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					List<_Character> characters;
					var error = FetchCharacters(connection, out characters);
					if (error != Error.NoError)
					{
						chatInstance = null;
						return error;
					}
					List<_Character> groupMembers;
					error = FetchGroupMembers(connection, groupId, null, out groupMembers);
					if (error != Error.NoError)
					{
						chatInstance = null;
						return error;
					}

					var lsChatInstances = new List<ChatInstance>();
					_Chat chat;
					using (var cmdChat = connection.CreateCommand())
					{
						cmdChat.CommandText =
						@"
							SELECT 
								name, createdAt, updatedAt, 
								modelInstructions, context, greetingDialogue, customDialogue, grammar,
								model, temperature, topP, minP, minPEnabled, topK, 
								repeatPenalty, repeatLastN, promptTemplate, canDeleteCustomDialogue, 
								authorNote, ttsAutoplay, ttsInputFilter
							FROM Chat
							WHERE id = $chatId;
						";
						cmdChat.Parameters.AddWithValue("$chatId", chatId);

						using (var reader = cmdChat.ExecuteReader())
						{
							int untitledCounter = 0;
							if (reader.Read() == false)
							{
								chatInstance = null;
								return Error.NotFound;
							}

							string name = reader.GetString(0);
							DateTime createdAt = reader.GetTimestamp(1);
							DateTime updatedAt = reader.GetTimestamp(2);

							// Staging
							string system = reader.GetString(3);
							string scenario = reader.GetString(4);
							string greeting = reader.GetString(5);
							string example = reader.GetString(6);
							string grammar = reader[7] as string;

							// Parameters
							string model = reader.GetString(8);
							decimal temperature = reader.GetDecimal(9);
							decimal topP = reader.GetDecimal(10);
							decimal minP = reader.GetDecimal(11);
							bool minPEnabled = reader.GetBoolean(12);
							int topK = reader.GetInt32(13);
							decimal repeatPenalty = reader.GetDecimal(14);
							int repeatLastN = reader.GetInt32(15);
							string promptTemplate = reader[16] as string;
							bool pruneExampleChat = reader.GetBoolean(17);
							string authorNote = reader.GetString(18);
							bool ttsAutoPlay = reader.GetBoolean(19);
							string ttsInputFilter = reader.GetString(20);

							if (string.IsNullOrWhiteSpace(name))
							{
								if (++untitledCounter > 1)
									name = string.Concat(ChatInstance.DefaultName, " #", untitledCounter.ToString());
								else
									name = ChatInstance.DefaultName;
							}

							chat = new _Chat() {
								instanceId = chatId,
								creationDate = createdAt,
								updateDate = updatedAt,
								name = name,
								staging = new ChatStaging() {
									system = system,
									scenario = scenario,
									greeting = greeting,
									example = example,
									grammar = grammar,
								},
								parameters = new ChatParameters() {
									model = model,
									temperature = temperature,
									topP = topP,
									minP = minP,
									minPEnabled = minPEnabled,
									topK = topK,
									repeatPenalty = repeatPenalty,
									repeatLastN = repeatLastN,
									promptTemplate = promptTemplate,
									pruneExampleChat = pruneExampleChat,
									authorNote = authorNote,
									ttsAutoPlay = ttsAutoPlay,
									ttsInputFilter = ttsInputFilter,
								}
							};
						}
					}

					// Get background image
					if (CheckFeature(Feature.ChatBackgrounds))
					{
						using (var cmdBackground = connection.CreateCommand())
						{
							cmdBackground.CommandText =
							@"
								SELECT 
									id, imageUrl, aspectRatio
								FROM BackgroundChatImage
								WHERE chatId = $chatId;
							";
							cmdBackground.Parameters.AddWithValue("$chatId", chatId);
							using (var reader = cmdBackground.ExecuteReader())
							{
								if (reader.Read() && chat.staging != null)
								{
									string id = reader.GetString(0);
									string imageUrl = reader.GetString(1);
									string aspectRatio = reader.GetString(2);
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

									chat.staging.background = new ChatBackground() {
										instanceId = id,
										imageUrl = imageUrl,
										width = width,
										height = height,
									};
								}
							}
						}
					}

					// Collect messages
					chatInstance = FetchChatInstance(connection, chat, characters, groupMembers);
					if (chatInstance == null)
						return Error.NotFound;
					return Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				chatInstance = null;
				return Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				chatInstance = null;
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				chatInstance = null;
				return Error.Unknown;
			}
		}

		public struct CreateChatArguments
		{
			public ChatStaging staging;
			public ChatParameters parameters;
			public ChatHistory history;
			public bool isImport;
		}

		private static ChatInstance FetchChatInstance(SQLiteConnection connection, _Chat chatInfo, List<_Character> characters, List<_Character> groupMembers)
		{
			int index = 1;
			var indexById = new Dictionary<string, int>();

			string chatId = chatInfo.instanceId;
			var characterLookup = characters.ToDictionary(c => c.instanceId, c => c);
			var users = new HashSet<string>(characters.Where(c => c.isUser).Select(c => c.instanceId));
			var nonUsers = new HashSet<string>(characters.Where(c => c.isUser == false).Select(c => c.instanceId));
			var canonicalUser = groupMembers.FirstOrDefault(c => c.isUser);

			var messages = new List<_Message>(64);
			using (var cmdMessages = connection.CreateCommand())
			{
				cmdMessages.CommandText =
				@"
					SELECT 
						R.messageId, M.createdAt, M.updatedAt, R.activeTimestamp, M.characterConfigId, R.text 
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
						DateTime createdAt = reader.GetTimestamp(1);
						DateTime updatedAt = reader.GetTimestamp(2);
						DateTime activeAt = reader.GetTimestamp(3);
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

					// Assign a character index
					int speakerIdx;
					if (indexById.TryGetValue(message.characterId, out speakerIdx) == false)
					{
						if (users.Contains(message.characterId)) // User (there can be many, but they should be treated as one)
							speakerIdx = 0;
						else if (nonUsers.Contains(message.characterId)) // Character (there can be many)
						{
							indexById.Add(message.characterId, index);
							speakerIdx = index++;
						}
						else // Unknown character id
							return null;
					}

					return new ChatHistory.Message() {
						instanceId = message.messageId,
						speaker = speakerIdx,
						creationDate = message.createdAt,
						updateDate = message.updatedAt,
						activeSwipe = swipes.OrderByDescending(x => x.active).Select(x => x.index).First(),
						swipes = swipes.Select(x => x.text)
						.ToArray(),
					};
				})
				.NotNull()
				.ToList();

			var participants = indexById
				.Select(kvp => new {
					index = kvp.Value,
					id = kvp.Key,
				})
				.OrderBy(x => x.index)
				.Select(x => {
					_Character character;
					if (characterLookup.TryGetValue(x.id, out character))
						return character;
					return default(_Character);
				})
				.Where(c => c.instanceId != null)
				.ToList();
			participants.Insert(0, canonicalUser);

			if (groupMembers.Count < 2)
				return null; // Error

			// Insert greeting
			if (string.IsNullOrEmpty(chatInfo.staging.greeting) == false)
			{
				string userName = Utility.FirstNonEmpty(groupMembers[0].name, Constants.DefaultUserName);
				string characterName = Utility.FirstNonEmpty(groupMembers[1].name, Constants.DefaultCharacterName);

				var sb = new StringBuilder(GingerString.FromFaraday(chatInfo.staging.greeting).ToString());
				sb.Replace(GingerString.CharacterMarker, characterName, true);
				sb.Replace(GingerString.UserMarker, userName, true);

				entries.Insert(0, new ChatHistory.Message() {
					speaker = 1,
					creationDate = chatInfo.creationDate,
					updateDate = chatInfo.updateDate,
					activeSwipe = 0,
					swipes = new string[1] { sb.ToString() },
				});
			}

			var chatInstance = new ChatInstance() {
				instanceId = chatInfo.instanceId,
				creationDate = chatInfo.creationDate,
				updateDate = chatInfo.updateDate,
				participants = groupMembers.Select(c => c.instanceId).ToArray(),
				staging = chatInfo.staging,
				parameters = chatInfo.parameters,
				history = new ChatHistory() {
					name = chatInfo.name,
					messages = entries.ToArray(),
				},
			};

			return chatInstance;
		}

		public static Error CreateNewChat(CreateChatArguments args, string groupId, out ChatInstance chatInstance)
		{
			if (ConnectionEstablished == false)
			{
				chatInstance = default(ChatInstance);
				return Error.NotConnected;
			}

			if (args.history == null || args.history.messages == null || groupId == null)
			{
				chatInstance = default(ChatInstance);
				return Error.InvalidArgument;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					var defaultStaging = new ChatStaging() {
						system = FaradayCardV4.OriginalModelInstructionsByFormat[0],
					};
					var defaultParameters = new ChatParameters();

					// Background image
					bool hasBackground = CheckFeature(Feature.ChatBackgrounds)
						&& args.staging != null 
						&& args.staging.background != null
						&& string.IsNullOrEmpty(args.staging.background.imageUrl) == false
						&& File.Exists(args.staging.background.imageUrl);

					// Read default chat settings (latest)
					using (var cmdGroupInfo = connection.CreateCommand())
					{ 
						cmdGroupInfo.CommandText =
						@"
							SELECT 
								modelInstructions, context, greetingDialogue, customDialogue, grammar, 
								model, temperature, topP, minP, minPEnabled, topK, 
								repeatPenalty, repeatLastN, promptTemplate, canDeleteCustomDialogue, 
								authorNote, ttsAutoplay, ttsInputFilter
							FROM Chat
							WHERE groupConfigId = $groupId
							ORDER BY updatedAt DESC
						";

						cmdGroupInfo.Parameters.AddWithValue("$groupId", groupId);

						using (var reader = cmdGroupInfo.ExecuteReader())
						{
							if (reader.Read())
							{
								defaultStaging.system = reader.GetString(0);
								defaultStaging.scenario = reader.GetString(1);
								defaultStaging.greeting = reader.GetString(2);
								defaultStaging.example = reader.GetString(3);
								defaultStaging.grammar = reader[4] as string ?? "";
								defaultParameters.model = reader.GetString(5);
								defaultParameters.temperature = reader.GetDecimal(6);
								defaultParameters.topP = reader.GetDecimal(7);
								defaultParameters.minP = reader.GetDecimal(8);
								defaultParameters.minPEnabled = reader.GetBoolean(9);
								defaultParameters.topK = reader.GetInt32(10);
								defaultParameters.repeatPenalty = reader.GetDecimal(11);
								defaultParameters.repeatLastN = reader.GetInt32(12);
								defaultParameters.promptTemplate = reader[13] as string;
								defaultParameters.pruneExampleChat = reader.GetBoolean(14);
								defaultParameters.authorNote = reader.GetString(15);
								defaultParameters.ttsAutoPlay = reader.GetBoolean(16);
								defaultParameters.ttsInputFilter = reader.GetString(17);
							}
						}
					}

					// Fetch group members
					List<_Character> groupMembers;
					var error = FetchGroupMembers(connection, groupId, args.history, out groupMembers);
					if (error != Error.NoError)
					{
						chatInstance = null;
						return error;
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
							var staging = args.staging ?? defaultStaging;
							var parameters = args.parameters ?? defaultParameters;
							var chatName = args.history.name ?? "";
							var greeting = staging.greeting;
							if (args.isImport)
								greeting = args.history.hasGreeting ? args.history.greeting : "";

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
											name, authorNote, ttsAutoPlay, ttsInputFilter)
									VALUES 
										($chatId, $timestamp, $timestamp, $scenario, $example, $pruneExample, 
											$system, $greeting, $grammar, 
											$groupId, 
											$model, $temperature, $topP, $minP, $minPEnabled, $topK, $repeatPenalty, $repeatLastN, $promptTemplate,
											$chatName, $authorNote, $ttsAutoPlay, $ttsInputFilter);
								");

								cmdCreateChat.CommandText = sbCommand.ToString();
								cmdCreateChat.Parameters.AddWithValue("$chatId", chatId);
								cmdCreateChat.Parameters.AddWithValue("$groupId", groupId);
								cmdCreateChat.Parameters.AddWithValue("$chatName", chatName ?? "");
								cmdCreateChat.Parameters.AddWithValue("$timestamp", createdAt);
								cmdCreateChat.Parameters.AddWithValue("$system", staging.system);
								cmdCreateChat.Parameters.AddWithValue("$scenario",  staging.scenario);
								cmdCreateChat.Parameters.AddWithValue("$example",  staging.example);
								cmdCreateChat.Parameters.AddWithValue("$greeting",  greeting);
								cmdCreateChat.Parameters.AddWithValue("$grammar",  staging.grammar ?? "");
								cmdCreateChat.Parameters.AddWithValue("$model", parameters.modelForDB);
								cmdCreateChat.Parameters.AddWithValue("$pruneExample", parameters.pruneExampleChat);
								cmdCreateChat.Parameters.AddWithValue("$temperature", parameters.temperature);
								cmdCreateChat.Parameters.AddWithValue("$topP", parameters.topP);
								cmdCreateChat.Parameters.AddWithValue("$minP", parameters.minP);
								cmdCreateChat.Parameters.AddWithValue("$minPEnabled", parameters.minPEnabled);
								cmdCreateChat.Parameters.AddWithValue("$topK", parameters.topK);
								cmdCreateChat.Parameters.AddWithValue("$repeatPenalty", parameters.repeatPenalty);
								cmdCreateChat.Parameters.AddWithValue("$repeatLastN", parameters.repeatLastN);
								cmdCreateChat.Parameters.AddWithValue("$promptTemplate", parameters.promptTemplate);
								cmdCreateChat.Parameters.AddWithValue("$authorNote", parameters.authorNote ?? "");
								cmdCreateChat.Parameters.AddWithValue("$ttsAutoPlay", parameters.ttsAutoPlay);
								cmdCreateChat.Parameters.AddWithValue("$ttsInputFilter", parameters.ttsInputFilter ?? "default");

								expectedUpdates += 1;
								updates += cmdCreateChat.ExecuteNonQuery();
							}

							// Write background
							if (hasBackground)
							{
								using (var cmdBackground = new SQLiteCommand(connection))
								{
									var sbCommand = new StringBuilder();

									// Chat
									sbCommand.AppendLine(
									@"
										INSERT INTO BackgroundChatImage
											(id, imageUrl, aspectRatio, chatId)
										VALUES 
											($backgroundId, $imageUrl, $aspectRatio, $chatId);
									");

									cmdBackground.CommandText = sbCommand.ToString();
									cmdBackground.Parameters.AddWithValue("$backgroundId", Cuid.NewCuid());
									cmdBackground.Parameters.AddWithValue("$chatId", chatId);
									cmdBackground.Parameters.AddWithValue("$imageUrl", args.staging.background.imageUrl);
									cmdBackground.Parameters.AddWithValue("$aspectRatio", args.staging.background.aspectRatio);

									expectedUpdates += 1;
									updates += cmdBackground.ExecuteNonQuery();
								}
							}

							// Write messages
							var speakerIds = groupMembers.Select(m => m.instanceId).ToArray();							
							var lsMessages = WriteChatMessages(connection, chatId, args.history, speakerIds, ref expectedUpdates, ref updates);
							
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
								staging = staging,
								parameters = parameters,
								history = new ChatHistory() {
									name = chatName,
									messages = lsMessages.ToArray(),
								},
								participants = groupMembers.Select(c => c.instanceId).ToArray(),
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
				return Error.NotConnected;
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
							
							if (updates == 0)
							{
								transaction.Rollback(); // Superfluous. jic.
								return Error.NotFound;
							}

							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								chatInstance = default(ChatInstance);
								return Error.SQLCommandFailed;
							}

							chatInstance.updateDate = now;
							chatInstance.history.name = newName;

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
				return Error.NotConnected;
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
				return Error.NotConnected;
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

		public static Error ConfirmChatExists(string chatId)
		{
			if (ConnectionEstablished == false)
				return Error.NotConnected;

			if (string.IsNullOrEmpty(chatId))
				return Error.InvalidArgument;

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
							SELECT 1
							FROM Chat
							WHERE id = $chatId
						";
						cmdConfirm.Parameters.AddWithValue("$chatId", chatId);

						if (cmdConfirm.ExecuteScalar() == null)
							return Error.NotFound;
						return Error.NoError;
					}
				}
			}
			catch (FileNotFoundException e)
			{
				return Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				return Error.Unknown;
			}
		}

		public static Error DeleteChat(ChatInstance chatInstance)
		{
			if (ConnectionEstablished == false)
				return Error.NotConnected;

			if (chatInstance == null || string.IsNullOrEmpty(chatInstance.instanceId))
				return Error.InvalidArgument;

			var error = ConfirmChatExists(chatInstance.instanceId);
			if (error != Error.NoError)
				return error;

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
							// Delete background (if any)
							if (CheckFeature(Feature.ChatBackgrounds))
							{
								using (var cmdDeleteBackground = connection.CreateCommand())
								{
									cmdDeleteBackground.CommandText =
									@"
										DELETE FROM BackgroundChatImage
										WHERE chatId = $chatId;
									";

									cmdDeleteBackground.Parameters.AddWithValue("$chatId", chatId);

									int nDeleted = cmdDeleteBackground.ExecuteNonQuery();
									expectedUpdates += nDeleted;
									updates += nDeleted;
								}
							}

							// Delete chat
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
				return Error.NotConnected;
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

		public static Error PurgeChats(GroupInstance groupInstance)
		{
			if (ConnectionEstablished == false)
				return Error.NotConnected;

			if (string.IsNullOrEmpty(groupInstance.instanceId))
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
						cmdGetChats.Parameters.AddWithValue("$groupId", groupInstance.instanceId);

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
										createdAt = $timestamp,
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
				return Error.NotConnected;
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

		public static Error UpdateChat(ChatInstance chatInstance, string groupId)
		{
			if (ConnectionEstablished == false)
				return Error.NotConnected;

			if (chatInstance == null)
				return Error.InvalidArgument;

			if (string.IsNullOrEmpty(chatInstance.instanceId) || string.IsNullOrEmpty(groupId))
				return Error.InvalidArgument;

			var error = ConfirmChatExists(chatInstance.instanceId);
			if (error != Error.NoError)
				return error;

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

					// Fetch group members
					List<_Character> groupMembers;
					error = FetchGroupMembers(connection, groupId, chatInstance.history, out groupMembers);
					if (error != Error.NoError)
					{
						chatInstance = null;
						return error;
					}

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
										name = $name,
										greetingDialogue = $greeting
									WHERE id = $chatId;
								";

								cmdEditChat.Parameters.AddWithValue("$chatId", chatId);
								cmdEditChat.Parameters.AddWithValue("$timestamp", updatedAt);
								cmdEditChat.Parameters.AddWithValue("$name", chatInstance.name ?? "");
								cmdEditChat.Parameters.AddWithValue("$greeting", chatInstance.history.greeting ?? "");

								expectedUpdates += 1;
								updates += cmdEditChat.ExecuteNonQuery();
							}

							// Write messages
							string[] speakerIds = groupMembers.Select(m => m.instanceId).ToArray();
							var lsMessages = WriteChatMessages(connection, chatId, chatInstance.history, speakerIds, ref expectedUpdates, ref updates);

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
				return Error.NotConnected;
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

		private static List<ChatHistory.Message> WriteChatMessages(SQLiteConnection connection, string chatId, ChatHistory chatHistory, string[] speakerIds, ref int expectedUpdates, ref int updates)
		{
			List<ChatHistory.Message> lsMessages = new List<ChatHistory.Message>();
			int messageCount = chatHistory.messagesWithoutGreeting.Count();
			if (messageCount == 0)
				return lsMessages; // No messages

			// Generate unique IDs
			var messageIds = new string[messageCount];
			for (int i = 0; i < messageIds.Length; ++i)
				messageIds[i] = Cuid.NewCuid();

			using (var cmdMessages = new SQLiteCommand(connection))
			{
				var sbCommand = new StringBuilder();
				sbCommand.AppendLine(
				@"
					INSERT INTO Message 
						(id, createdAt, updatedAt, chatId, characterConfigId)
					VALUES ");

				int iMessage = 0;
				foreach (var message in chatHistory.messagesWithoutGreeting)
				{
					if (iMessage > 0)
						sbCommand.Append(",\n");
					sbCommand.Append($"($messageId{iMessage:000}, $messageCreatedAt{iMessage:000}, $messageUpdatedAt{iMessage:000}, $chatId, $charId{iMessage:000})");

					cmdMessages.Parameters.AddWithValue($"$messageId{iMessage:000}", messageIds[iMessage]);
					cmdMessages.Parameters.AddWithValue($"$messageCreatedAt{iMessage:000}", message.creationDate.ToUnixTimeMilliseconds());
					cmdMessages.Parameters.AddWithValue($"$messageUpdatedAt{iMessage:000}", message.updateDate.ToUnixTimeMilliseconds());
					cmdMessages.Parameters.AddWithValue($"$charId{iMessage:000}", speakerIds[message.speaker]);

					lsMessages.Add(new ChatHistory.Message() {
						instanceId = messageIds[iMessage],
						activeSwipe = message.activeSwipe,
						creationDate = message.creationDate,
						updateDate = message.updateDate,
						speaker = message.speaker,
						swipes = message.swipes,
					});
					++iMessage;
				}
				sbCommand.Append(";");

				sbCommand.AppendLine(
				@"
					INSERT INTO RegenSwipe
						(id, createdAt, updatedAt, activeTimestamp, text, messageId)
					VALUES ");
				iMessage = 0;
				int iSwipe = 0;
				foreach (var message in chatHistory.messagesWithoutGreeting)
				{
					for (int i = 0; i < message.swipes.Length; ++i, ++iSwipe)
					{
						if (iMessage > 0)
							sbCommand.Append(",\n");
						sbCommand.Append($"($swipeId{iSwipe:000}, $swipeCreatedAt{iSwipe:000}, $swipeCreatedAt{iSwipe:000}, $swipeActiveAt{iSwipe:000}, $text{iSwipe:000}, $messageId{iMessage:000})");

						DateTime activeTime = message.creationDate;
						if (i == message.activeSwipe)
							activeTime += TimeSpan.FromMilliseconds(5000);
						DateTime swipeTime = message.creationDate + TimeSpan.FromMilliseconds(i * 10);
						cmdMessages.Parameters.AddWithValue($"$swipeId{iSwipe:000}", Cuid.NewCuid());
						cmdMessages.Parameters.AddWithValue($"$swipeCreatedAt{iSwipe:000}", swipeTime.ToUnixTimeMilliseconds());
						cmdMessages.Parameters.AddWithValue($"$swipeActiveAt{iSwipe:000}", activeTime.ToUnixTimeMilliseconds());
						cmdMessages.Parameters.AddWithValue($"$text{iSwipe:000}", message.swipes[i]);
						++expectedUpdates;
					}
					++iMessage;
				}
				sbCommand.Append(";");
				cmdMessages.CommandText = sbCommand.ToString();

				cmdMessages.Parameters.AddWithValue("$chatId", chatId);

				expectedUpdates += messageCount;
				updates += cmdMessages.ExecuteNonQuery();
			}
			return lsMessages;
		}

		private struct _SwipeRepair
		{
			public string instanceId;
			public string chatId;
			public string text;
		}

		public static Error RepairChats(GroupInstance groupInstance, out int modified)
		{
			if (ConnectionEstablished == false)
			{
				modified = 0;
				return Error.NotConnected;
			}

			if (string.IsNullOrEmpty(groupInstance.instanceId))
			{
				modified = 0;
				return Error.InvalidArgument;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					// Confirm group exists
					using (var cmdConfirm = connection.CreateCommand())
					{ 
						cmdConfirm.CommandText =
						@"
							SELECT 1
							FROM GroupConfig
							WHERE id = $groupId
						";
						cmdConfirm.Parameters.AddWithValue("$groupId", groupInstance.instanceId);

						if (cmdConfirm.ExecuteScalar() == null)
						{
							modified = 0;
							return Error.NotFound;
						}
					}

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
						cmdGetChats.Parameters.AddWithValue("$groupId", groupInstance.instanceId);

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
						int pos_begin = swipe.text.IndexOf("#{character}:");
						int pos_end = swipe.text.IndexOf("\n#{user}:");
						bool bFront = pos_begin == 0;
						bool bBack = pos_end >= 0 && pos_end >= swipe.text.Length - 10;
						if (bFront && bBack)
						{
							repairs.Add(new _SwipeRepair() {
								instanceId = swipe.instanceId,
								chatId = swipe.chatId,
								text = swipe.text.Substring(pos_begin + 13, pos_end - pos_begin - 13).Trim(),
							});
						}
						else if (bFront)
						{
							repairs.Add(new _SwipeRepair() {
								instanceId = swipe.instanceId,
								chatId = swipe.chatId,
								text = swipe.text.Substring(pos_begin + 13).TrimStart(),
							});
						}
						else if (bBack)
						{
							repairs.Add(new _SwipeRepair() {
								instanceId = swipe.instanceId,
								chatId = swipe.chatId,
								text = swipe.text.Substring(0, pos_end).TrimEnd(),
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
				return Error.NotConnected;
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

		public static Error UpdateChatParameters(string chatId, ChatParameters parameters, ChatStaging staging)
		{
			if (ConnectionEstablished == false)
				return Error.NotConnected;

			if (parameters == null && staging == null)
				return Error.InvalidArgument;

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					int updates = 0;
					int expectedUpdates = 0;

					DateTime now = DateTime.Now;
					long updatedAt = now.ToUnixTimeMilliseconds();

					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							// Update chat info
							using (var cmdUpdateChat = connection.CreateCommand())
							{
								var sbCommand = new StringBuilder();
								sbCommand.AppendLine(
								@"
									UPDATE Chat
									SET 
										updatedAt = $timestamp");

								if (parameters != null)
								{
									sbCommand.AppendLine(
									@",
										model = $model, 
										temperature = $temperature,
										topP = $topP,
										minP = $minP,
										minPEnabled = $minPEnabled,
										topK = $topK,
										repeatPenalty = $repeatPenalty,
										repeatLastN = $repeatLastN,
										promptTemplate = $promptTemplate,
										canDeleteCustomDialogue = $pruneExample");
								}
								if (staging != null)
								{
									sbCommand.AppendLine(
									@",
										context = $scenario,
										customDialogue = $example,
										modelInstructions = $system,
										grammar = $grammar,
										greetingDialogue = $greeting");
								}
								sbCommand.AppendLine(
								@"
									WHERE id = $chatId;
								");

								cmdUpdateChat.CommandText = sbCommand.ToString();
								cmdUpdateChat.Parameters.AddWithValue("$chatId", chatId);
								cmdUpdateChat.Parameters.AddWithValue("$timestamp", updatedAt);
								if (parameters != null)
								{
									cmdUpdateChat.Parameters.AddWithValue("$model", parameters.modelForDB);
									cmdUpdateChat.Parameters.AddWithValue("$temperature", parameters.temperature);
									cmdUpdateChat.Parameters.AddWithValue("$topP", parameters.topP);
									cmdUpdateChat.Parameters.AddWithValue("$minP", parameters.minP);
									cmdUpdateChat.Parameters.AddWithValue("$minPEnabled", parameters.minPEnabled);
									cmdUpdateChat.Parameters.AddWithValue("$topK", parameters.topK);
									cmdUpdateChat.Parameters.AddWithValue("$repeatPenalty", parameters.repeatPenalty);
									cmdUpdateChat.Parameters.AddWithValue("$repeatLastN", parameters.repeatLastN);
									cmdUpdateChat.Parameters.AddWithValue("$promptTemplate", parameters.promptTemplate);
									cmdUpdateChat.Parameters.AddWithValue("$pruneExample", parameters.pruneExampleChat);
								}
								if (staging != null)
								{
									cmdUpdateChat.Parameters.AddWithValue("$system", Utility.FirstNonEmpty(staging.system, FaradayCardV4.OriginalModelInstructionsByFormat[0]));
									cmdUpdateChat.Parameters.AddWithValue("$scenario", staging.scenario ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$greeting", staging.greeting ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$example", staging.example ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$grammar", staging.grammar ?? "");
								}

								expectedUpdates += 1;
								updates += cmdUpdateChat.ExecuteNonQuery();
							}
							
							if (updates == 0)
							{
								transaction.Rollback(); // Superfluous. jic.
								return Error.NotFound;
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
				return Error.NotConnected;
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

		public static Error UpdateChatParameters(string[] chatIds, ChatParameters parameters, ChatStaging staging)
		{
			if (ConnectionEstablished == false)
				return Error.NotConnected;

			if (parameters == null && staging == null)
				return Error.InvalidArgument;

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					int updates = 0;
					int expectedUpdates = 0;

					DateTime now = DateTime.Now;
					long updatedAt = now.ToUnixTimeMilliseconds();

					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							// Update chat info
							using (var cmdUpdateChat = connection.CreateCommand())
							{
								var sbCommand = new StringBuilder();
								sbCommand.AppendLine(
								@"
									UPDATE Chat
									SET 
										updatedAt = $timestamp");

								if (parameters != null)
								{
									sbCommand.AppendLine(
									@",
										model = $model, 
										temperature = $temperature,
										topP = $topP,
										minP = $minP,
										minPEnabled = $minPEnabled,
										topK = $topK,
										repeatPenalty = $repeatPenalty,
										repeatLastN = $repeatLastN,
										promptTemplate = $promptTemplate,
										canDeleteCustomDialogue = $pruneExample");
								}
								if (staging != null)
								{
									sbCommand.AppendLine(
									@",
										context = $scenario,
										customDialogue = $example,
										modelInstructions = $system,
										grammar = $grammar,
										greetingDialogue = $greeting");
								}
								sbCommand.AppendLine(
								@"
									WHERE id IN ( 
								");
								for (int i = 0; i < chatIds.Length; ++i)
								{
									if (i > 0)
										sbCommand.Append(", ");
									sbCommand.AppendFormat("'{0}'", chatIds[i]);
								}
								sbCommand.AppendLine(");");

								cmdUpdateChat.CommandText = sbCommand.ToString();
								cmdUpdateChat.Parameters.AddWithValue("$timestamp", updatedAt);
								if (parameters != null)
								{
									cmdUpdateChat.Parameters.AddWithValue("$model", parameters.modelForDB);
									cmdUpdateChat.Parameters.AddWithValue("$temperature", parameters.temperature);
									cmdUpdateChat.Parameters.AddWithValue("$topP", parameters.topP);
									cmdUpdateChat.Parameters.AddWithValue("$minP", parameters.minP);
									cmdUpdateChat.Parameters.AddWithValue("$minPEnabled", parameters.minPEnabled);
									cmdUpdateChat.Parameters.AddWithValue("$topK", parameters.topK);
									cmdUpdateChat.Parameters.AddWithValue("$repeatPenalty", parameters.repeatPenalty);
									cmdUpdateChat.Parameters.AddWithValue("$repeatLastN", parameters.repeatLastN);
									cmdUpdateChat.Parameters.AddWithValue("$promptTemplate", parameters.promptTemplate);
									cmdUpdateChat.Parameters.AddWithValue("$pruneExample", parameters.pruneExampleChat);
								}
								if (staging != null)
								{
									cmdUpdateChat.Parameters.AddWithValue("$system", Utility.FirstNonEmpty(staging.system, FaradayCardV4.OriginalModelInstructionsByFormat[0]));
									cmdUpdateChat.Parameters.AddWithValue("$scenario", staging.scenario ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$greeting", staging.greeting ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$example", staging.example ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$grammar", staging.grammar ?? "");
								}

								expectedUpdates += chatIds.Length;
								updates += cmdUpdateChat.ExecuteNonQuery();
							}
							
							if (updates == 0)
							{
								transaction.Rollback(); // Superfluous. jic.
								return Error.NotFound;
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
				return Error.NotConnected;
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

		#endregion // Chat

		#region Folder

		private struct _FolderInfo 
		{
			public string instanceId;
			public string parentId;
			public string name;
			public string url;
			public bool isRoot;
			public bool isSortedDesc;
			public string sortType;
		}

		public static string ToFolderUrl(string label)
		{
			if (string.IsNullOrWhiteSpace(label))
				return null;

			label = label.Trim().ToLowerInvariant();

			StringBuilder sbFormat = new StringBuilder(label.Length);
			for (int i = 0; i < label.Length; ++i)
			{
				char c = label[i];
				if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_') // Valid chars
					sbFormat.Append(c);
				else if (c >= 'A' && c <= 'Z')
					sbFormat.Append(char.ToLowerInvariant(c));
				else if (char.IsWhiteSpace(c))
					sbFormat.Append('-');
				else if ((c & 0xFF) == c)
					sbFormat.Append(string.Format("{0:x2}", c & 0xFF));
				else
					sbFormat.Append(string.Format("{0:x4}", c & 0xFFFF));
			}
			return sbFormat.ToString();
		}

		public static Error CreateNewFolder(string folderName, out FolderInstance folderInstance)
		{
			if (ConnectionEstablished == false)
			{
				folderInstance = default(FolderInstance);
				return Error.NotConnected;
			}

			if (string.IsNullOrWhiteSpace(folderName))
			{
				folderInstance = default(FolderInstance);
				return Error.InvalidArgument;
			}

			folderName = folderName.Trim();

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					// Fetch existing folders
					var existingFolders = new List<_FolderInfo>();
					using (var cmdReadFolders = connection.CreateCommand())
					{ 
						cmdReadFolders.CommandText =
						@"
							SELECT 
								id, name, url, parentFolderId, isRoot, sortIsDesc, sortType
							FROM AppFolder
						";

						using (var reader = cmdReadFolders.ExecuteReader())
						{
							while (reader.Read())
							{
								string folderId = reader.GetString(0);
								string name = reader.GetString(1);
								string folderUrl = reader.GetString(2);
								string parentFolderId = reader[3] as string;
								bool isRoot = reader.GetBoolean(4);
								bool isSortedDesc = reader.GetBoolean(5);
								string sortType = reader.GetString(6);

								existingFolders.Add(new _FolderInfo() {
									instanceId = folderId,
									parentId = parentFolderId,
									url = folderUrl,
									name = name,
									isRoot = isRoot,
									isSortedDesc = isSortedDesc,
									sortType = sortType,
								});
							}
						}
					}

					string rootFolderId = existingFolders
						.Where(f => f.isRoot)
						.Select(f => f.instanceId)
						.FirstOrDefault();

					if (string.IsNullOrEmpty(rootFolderId))
					{
						folderInstance = default(FolderInstance);
						return Error.NotFound;
					}

					// Make unique folder url
					var existingUrls = new HashSet<string>(existingFolders
						.Where(f => !f.isRoot)
						.Select(f => f.url));

					string url = ToFolderUrl(folderName);
					if (existingUrls.Contains(url))
					{
						string testUrl = url;
						int iUrl = 1;
						while (existingUrls.Contains(testUrl))
							testUrl = string.Format("{0}-{1:00}", url, iUrl++);
						url = testUrl;
					}

					// Write to database
					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							string folderId = Cuid.NewCuid();
							DateTime now = DateTime.Now;
							long createdAt = now.ToUnixTimeMilliseconds();

							int updates = 0;
							int expectedUpdates = 0;

							using (var cmdCreateFolder = new SQLiteCommand(connection))
							{
								var sbCommand = new StringBuilder();

								// Chat
								sbCommand.AppendLine(
								@"
									INSERT INTO AppFolder
										(id, createdAt, updatedAt, name, url, pinnedToSidebarPosition, parentFolderId, isRoot, sortIsDesc, sortType)
									VALUES 
										($folderId, $timestamp, $timestamp, 
										$name, $url, $pinnedToSidebarPosition, $parentFolderId, $isRoot, $sortIsDesc, $sortType);
								");

								cmdCreateFolder.CommandText = sbCommand.ToString();
								cmdCreateFolder.Parameters.AddWithValue("$folderId", folderId);
								cmdCreateFolder.Parameters.AddWithValue("$timestamp", createdAt);
								cmdCreateFolder.Parameters.AddWithValue("$name", folderName);
								cmdCreateFolder.Parameters.AddWithValue("$url", url);
								cmdCreateFolder.Parameters.AddWithValue("$pinnedToSidebarPosition", null);
								cmdCreateFolder.Parameters.AddWithValue("$parentFolderId", rootFolderId);
								cmdCreateFolder.Parameters.AddWithValue("$isRoot", false);
								cmdCreateFolder.Parameters.AddWithValue("$sortIsDesc", true);
								cmdCreateFolder.Parameters.AddWithValue("$sortType", "Custom");

								expectedUpdates += 1;
								updates += cmdCreateFolder.ExecuteNonQuery();
							}

							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								folderInstance = default(FolderInstance);
								return Error.SQLCommandFailed;
							}

							transaction.Commit();

							folderInstance = new FolderInstance() {
								instanceId = folderId,
								isRoot = false,
								name = folderName,
								url = url,
								parentId = rootFolderId,
							};
							_Folders.Add(folderInstance.instanceId, folderInstance);
							return Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();

							folderInstance = default(FolderInstance);
							return Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				Disconnect();
				folderInstance = default(FolderInstance);
				return Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				Disconnect();
				folderInstance = default(FolderInstance);
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Disconnect();
				folderInstance = default(FolderInstance);
				return Error.Unknown;
			}
		}

		#endregion

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
			public string aspectRatio 
			{ 
				get
				{
					if (width > 0 && height > 0)
						return string.Format("{0}/{1}", width, height);
					return "";
				} 
			}
		}

		public struct ImageInput
		{
			public ImageRef image;
			public AssetFile asset;
			public string fileExt; // "png"
		}

		private static bool PrepareImageUpdates(List<ImageInstance> imageInstances, Link.Image[] imageLinks, out ImageOutput[] imagesToSave, out Link.Image[] newImageLinks)
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
				var filename = Utility.CreateRandomFilename("png");
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

						string filename = Utility.CreateRandomFilename(asset.ext);
						ImageOutput output = new ImageOutput() {
							instanceId = Cuid.NewCuid(),
							imageUrl = Path.Combine(destPath, filename),
							data = asset.data,
							width = imageWidth,
							height = imageHeight,
						};
						
						results.Add(output);
						lsImageLinks.Add(new Link.Image() {
							uid = asset.uid,
							filename = filename,
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

		private static bool PrepareImages(ImageInput[] imageInput, out ImageOutput[] imagesToSave, out Link.Image[] newImageLinks)
		{
			if (imageInput == null || imageInput.Length == 0)
			{
				imagesToSave = null;
				newImageLinks = null;
				return false;
			}

			// Prepare image information
			string destPath = Path.Combine(AppSettings.BackyardLink.Location, "images");
			bool bUseBackgrounds = CheckFeature(Feature.ChatBackgrounds);

			List<ImageOutput> results = new List<ImageOutput>();
			List<Link.Image> lsImageLinks = new List<Link.Image>();
			
			foreach (var input in imageInput)
			{
				if (input.asset != null && input.asset.data.isEmpty == false) // Data buffer
				{
					// Measure dimensions
					int imageWidth;
					int imageHeight;
					if (Utility.GetImageDimensions(input.asset.data.bytes, out imageWidth, out imageHeight))
					{
						string filename = Utility.CreateRandomFilename(input.fileExt);
						results.Add(new ImageOutput() {
							instanceId = Cuid.NewCuid(),
							imageUrl = Path.Combine(destPath, filename),
							data = input.asset.data,
							width = imageWidth,
							height = imageHeight,
						});
						lsImageLinks.Add(new Link.Image() {
							uid = input.asset.uid,
							filename = filename,
						});
					}
				}
				else if (input.image != null) // Image reference
				{
					string filename = Utility.CreateRandomFilename(input.fileExt);
					results.Add(new ImageOutput() {
						instanceId = Cuid.NewCuid(),
						imageUrl = Path.Combine(destPath, filename),
						data = AssetData.FromBytes(Utility.ImageToMemory(input.image)),
						width = input.image.Width,
						height = input.image.Height,
					});
					lsImageLinks.Add(new Link.Image() {
						uid = input.image.uid,
						filename = filename,
					});
				}
			}

			imagesToSave = results.ToArray();
			newImageLinks = lsImageLinks.Count > 0 ? lsImageLinks.ToArray() : null;
			return imagesToSave.ContainsAny(i => i.data.isEmpty == false);
		}

		private static Error FetchCharacters(SQLiteConnection connection, out List<_Character> characters)
		{
			try
			{
				using (var cmdCharacters = connection.CreateCommand())
				{
					cmdCharacters.CommandText =
					@"
					SELECT 
						A.id, B.name, A.isUserControlled
					FROM CharacterConfig AS A
					INNER JOIN CharacterConfigVersion AS B ON B.characterConfigId = A.id;
				";

					characters = new List<_Character>();
					using (var reader = cmdCharacters.ExecuteReader())
					{
						while (reader.Read())
						{
							string instanceId = reader.GetString(0);
							string name = reader.GetString(1);
							bool isUser = reader.GetBoolean(2);
							characters.Add(new _Character() {
								instanceId = instanceId,
								name = name,
								isUser = isUser,
							});
						}
					}
					if (characters.Count == 0)
						return Error.NotFound;
					return Error.NoError;
				}
			}
			catch (SQLiteException e)
			{
				characters = null;
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				characters = null;
				return Error.Unknown;
			}
		}

		private static Error FetchGroupMembers(SQLiteConnection connection, string groupId, ChatHistory chatHistory, out List<_Character> members)
		{
			try
			{
				using (var cmdGroupMembers = connection.CreateCommand())
				{
					cmdGroupMembers.CommandText =
					@"
						SELECT 
							A.A, C.name, B.isUserControlled
						FROM _CharacterConfigToGroupConfig AS A
						INNER JOIN CharacterConfig AS B ON B.id = A.A
						INNER JOIN CharacterConfigVersion AS C ON C.characterConfigId = B.id
						WHERE A.B = $groupId;
					";

					cmdGroupMembers.Parameters.AddWithValue("$groupId", groupId);

					members = new List<_Character>();
					using (var reader = cmdGroupMembers.ExecuteReader())
					{
						while (reader.Read())
						{
							string instanceId = reader.GetString(0);
							string name = reader.GetString(1);
							bool isUser = reader.GetBoolean(2);
							members.Add(new _Character() {
								instanceId = instanceId,
								name = name,
								isUser = isUser,
							});
						}
					}

					if (members.Count(c => c.isUser) > 1)
					{
						// Groups can only contain one user
						return Error.Unknown;
					}

					// Validate message indices
					if (chatHistory != null && chatHistory.messages != null)
					{
						for (int i = 0; i < chatHistory.messages.Length; ++i)
						{
							if (chatHistory.messages[i].speaker < 0 || chatHistory.messages[i].speaker >= members.Count)
								return Error.Unknown;
						}
					}

					// Place user first
					var user = members.First(c => c.isUser);
					members.Remove(user);
					members.Insert(0, user);
					return Error.NoError;
				}
			}
			catch (SQLiteException e)
			{
				members = null;
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				members = null;
				return Error.Unknown;
			}
		}

		private static Error FetchChatBackgrounds(SQLiteConnection connection, out List<_Background> backgrounds)
		{
			backgrounds = new List<_Background>();
			if (CheckFeature(Feature.ChatBackgrounds) == false)
				return Error.NoError;

			try
			{
				using (var cmdBackgrounds = connection.CreateCommand())
				{
					cmdBackgrounds.CommandText =
					@"
						SELECT 
							id, chatId, imageUrl, aspectRatio
						FROM BackgroundChatImage
					";

					using (var reader = cmdBackgrounds.ExecuteReader())
					{
						while (reader.Read())
						{
							string id = reader.GetString(0);
							string chatId = reader.GetString(1);
							string imageUrl = reader.GetString(2);
							string aspectRatio = reader.GetString(3);
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

							backgrounds.Add(new _Background() {
								instanceId = id,
								chatId = chatId,
								imageUrl = imageUrl,
								width = width,
								height = height,
							});
						}
					}
				}

				return Error.NoError;
			}
			catch (SQLiteException e)
			{
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				return Error.Unknown;
			}
		}

		private static Error FetchChatInstances(SQLiteConnection connection, string groupId, out List<_Chat> chats)
		{
			try
			{
				chats = new List<_Chat>();
				using (var cmdConfirm = connection.CreateCommand())
				{ 
					cmdConfirm.CommandText =
					@"
						SELECT id, createdAt, updatedAt
						FROM Chat
						WHERE groupConfigId = $groupId
					";
					cmdConfirm.Parameters.AddWithValue("$groupId", groupId);

					using (var reader = cmdConfirm.ExecuteReader())
					{
						while (reader.Read())
						{
							string chatId = reader.GetString(0);
							DateTime chatCreatedAt = reader.GetTimestamp(1);
							DateTime chatUpdatedAt = reader.GetTimestamp(2);

							chats.Add(new _Chat() {
								instanceId = chatId,
								creationDate = chatCreatedAt,
								updateDate = chatUpdatedAt,
							});
						}
					}
				}

				return Error.NoError;
			}
			catch (SQLiteException e)
			{
				chats = null;
				return Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				chats = null;
				return Error.Unknown;
			}
		}

		public static bool GetAppVersion(out VersionNumber appVersion)
		{
#if DEBUG
				// Use canary database during development and testing
				string appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "faraday-canary", "Backyard AI - Canary.exe");
#else
				// Use production database 
				string appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "faraday", "Backyard AI.exe");
#endif
			if (File.Exists(appPath) == false)
			{
				appVersion = VersionNumber.Zero;
				return false;
			}

			try
			{
				var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(appPath);
				if (versionInfo != null)
				{
					appVersion = new VersionNumber(versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart);
					return true;
				}
			}
			catch
			{
			}
			appVersion = VersionNumber.Zero;
			return false;
		}

		#endregion // Utilities

	}

	public static class SqlExtensions
	{
		public static DateTime GetTimestamp(this SQLiteDataReader reader, int index)
		{
			TypeAffinity typeAffinity = reader.GetFieldAffinity(index);

			if (reader.IsDBNull(index))
				return DateTime.MinValue;

			try
			{
				if (typeAffinity == TypeAffinity.DateTime)
					return reader.GetDateTime(index);
				else if (typeAffinity == TypeAffinity.Text)	//	Text date
				{
					string value = reader.GetString(index);
					DateTime dateTime;
					if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
						return dateTime;
					else if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
						return dateTime;
					else if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
						return dateTime;
				}
				else if (typeAffinity == TypeAffinity.Int64)	// Unix date
				{
					long unixTime = reader.GetInt64(index);
					return DateTimeExtensions.FromUnixTime(unixTime);
				}
				else if (typeAffinity == TypeAffinity.Double)	// Julian date
				{
					double julianDate = reader.GetDouble(index);
					return DateTime.FromOADate(julianDate - 2415018.5);
				}
			}
			catch
			{
			}
			return DateTime.MinValue;
		}
	}
}
