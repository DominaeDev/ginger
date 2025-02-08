using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Text;
using System.Globalization;

namespace Ginger.Integration
{
	public static class Backyard
	{
		public struct ChatCount
		{
			public int count;
			public DateTime lastMessage;
			public bool hasMessages { get { return lastMessage > DateTime.MinValue; } }
		}

		public struct ConfirmDeleteResult
		{
			public string[] characterIds;
			public string[] groupIds;
			public string[] imageIds;
			public string[] imageUrls;
		}

		public struct CreateChatArguments
		{
			public ChatStaging staging;
			public ChatParameters parameters;
			public ChatHistory history;
			public bool isImport;
		}

		public struct ImageInput
		{
			public ImageRef image;
			public AssetFile asset;
			public string fileExt; // "png"
		}

		public struct CharacterInstance
		{
			public string instanceId;			// CharacterConfig.id
			public DateTime creationDate;		// CharacterConfig.createdAt
			public DateTime updateDate;			// CharacterConfig.updatedAt
			public bool isUser;					// CharacterConfig.isUserControlled
			public string configId;				// CharacterConfigVersion.id
			public string displayName;			// CharacterConfigVersion.displayName
			public string name;					// CharacterConfigVersion.name
			public string groupId;				// GroupConfig.id (Primary group)
			public string folderId;				// GroupConfig.folderId (Primary group)
			public string folderSortPosition;	// GroupConfig.folderSortPosition (Primary group)
			public string creator;				// GroupConfig.hubAuthorUsername
			public string persona;				// CharacterConfigVersion.persona
			public int loreEntries;

			public bool isCharacter { get { return !isUser; } }
			public string inferredGender { get { return Utility.InferGender(GingerString.FromFaraday(persona).ToString()); } }
		}

		public struct GroupInstance
		{
			public string instanceId;			// GroupConfig.id
			public string name;					// GroupConfig.name
			public string folderId;				// GroupConfig.folderId
			public string hubCharId;			// GroupConfig.hubCharId
			public string hubAuthorUsername;    // GroupConfig.hubAuthorUsername
			public string folderSortPosition;	// GroupConfig.folderSortPosition
			public DateTime creationDate;		// CharacterConfig.createdAt
			public DateTime updateDate;			// CharacterConfig.updatedAt
			public string[] members;			// CharacterConfigVersion.id ...

			public int Count { get { return members != null ? members.Length : 0; } }
			public bool isEmpty { get { return Count == 0; } }
			public bool isSupported 
			{ 
				get
				{
					var groupType = GetGroupType();
					return groupType == GroupType.Solo
						|| groupType == GroupType.Group;
				} 
			}

			public enum GroupType
			{
				Unknown,
				Solo,	// 1-on-1
				Group,	// 1-on-many
				Party,	// Many-on-many (Not supported yet)
			}

			public GroupType GetGroupType()
			{
				if (members == null || members.Length < 2)
					return GroupType.Unknown;

				var memberInfo = members
					.Select(id => Backyard.Current.GetCharacter(id))
					.Where(m => string.IsNullOrEmpty(m.instanceId) == false)
					.ToArray();
				int nUsers = memberInfo.Count(m => m.isUser);
				int nCharacters = memberInfo.Count(m => m.isCharacter);
				if (nUsers == 1)
				{
					if (nCharacters == 1)
						return GroupType.Solo;
					else if (nCharacters > 1)
						return GroupType.Group;
				}
				if (nUsers > 1 && nCharacters > 0)
					return GroupType.Party;
				return GroupType.Unknown;
			}

			public string[] GetMemberNames(bool includingUser = false)
			{
				if (this.members == null || this.members.Length == 0)
					return new string[0];

				if (includingUser)
				{
					return this.members
						.Select(id => Backyard.Current.GetCharacter(id))
						.OrderBy(c => c.isCharacter)
						.ThenBy(c => c.creationDate)
						.Select(c => c.name)
						.ToArray();
				}
				else
				{
					return this.members
						.Select(id => Backyard.Current.GetCharacter(id))
						.Where(c => c.isCharacter)
						.OrderBy(c => c.creationDate)
						.Select(c => c.name)
						.ToArray();
				}
			}

			public string GetDisplayName()
			{
				if (string.IsNullOrEmpty(this.name) == false)
					return this.name;

				var names = GetMemberNames(false);
				if (names.Length == 0)
					return "(Empty)";
				if (names.Length == 1)
					return names[0] ?? Constants.DefaultCharacterName;
				if (names.Length == 2)
					return string.Format("{0} and {1}", names[0] ?? Constants.DefaultCharacterName, names[1] ?? Constants.DefaultCharacterName);
				return string.Format("{0},  {1} and others", names[0] ?? Constants.DefaultCharacterName, names[1] ?? Constants.DefaultCharacterName);
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
			public string label;            // AppImage.label
			public int width;               // AppImage.aspectRatio
			public int height;              // AppImage.aspectRatio
			public string associatedInstanceId;

			public static readonly int MaxImages = 10;

			public string imageUrl
			{
				get { return _imageUrl; }
				set
				{
					if (value != null && value.BeginsWith("http")) // Remote URL -> Local filename
						_imageUrl = Path.Combine(AppSettings.BackyardLink.Location, "images", Path.GetFileName(value));
					else
						_imageUrl = value;
				}
			}
			private string _imageUrl;         // AppImage.imageUrl
			public string aspectRatio
			{
				get
				{
					if (width > 0 && height > 0)
						return string.Format("{0}/{1}", width, height);
					return "";
				}
				set
				{
					ParseAspectRatio(value, out width, out height);
				}
			}

			public AssetFile.AssetType imageType;

			public static void ParseAspectRatio(string aspectRatio, out int width, out int height)
			{
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
			}
		}

		[Serializable]
		public class ChatStaging
		{
			public ChatStaging()
			{
				pruneExampleChat = AppSettings.BackyardLink.PruneExampleChat;
			}

			public string system = "";					// Chat.modelInstructions
			public string scenario = "";				// Chat.context
			public string greeting = "";				// Chat.greetingDialogue
			public string example = "";					// Chat.customDialogue
			public string grammar = "";					// Chat.grammar
			public string authorNote = "";				// Chat.authorNote
			public bool pruneExampleChat = true;		// Chat.canDeleteCustomDialogue
			public bool ttsAutoPlay = false;			// Chat.ttsAutoPlay
			public string ttsInputFilter = "default";	// Chat.ttsInputFilter
			public ChatBackground background = null;
		}

		[Serializable]
		public class ChatParameters : ICloneable
		{
			public string model							// Chat.model
			{ 
				get { return _modelId; }
				set
				{
					if (string.IsNullOrEmpty(value) || string.Compare(value, "default", StringComparison.OrdinalIgnoreCase) == 0)
						_modelId = null;
					else
						_modelId = value;
				}
			}
			private string _modelId = null;

			public decimal temperature = 1.2m;			// Chat.temperature
			public decimal topP = 0.9m;					// Chat.topP
			public decimal minP = 0.1m;					// Chat.minP
			public int topK = 30;						// Chat.topK
			public bool minPEnabled = true;				// Chat.minPEnabled
			public decimal repeatPenalty = 1.05m;		// Chat.repeatPenalty
			public int repeatLastN = 256;				// Chat.repeatLastN
			private int _iPromptTemplate = 0;			// Chat.promptTemplate

			public static readonly int MaxPromptTemplate = 6;
			public int iPromptTemplate
			{
				get { return _iPromptTemplate; }
				set { _iPromptTemplate = Math.Min(Math.Max(value, 0), MaxPromptTemplate); }
			}

			public string promptTemplate
			{
				get { return PromptTemplateFromInt(_iPromptTemplate); }
				set { _iPromptTemplate = PromptTemplateFromString(value); }
			}

		
			public static string PromptTemplateFromInt(int promptTemplate)
			{
				switch (promptTemplate)
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

			public static int PromptTemplateFromString(string promptTemplate)
			{
				var sValue = (promptTemplate ?? "").Trim().ToLowerInvariant();
				int iValue;
				if (sValue == "")
					return 0;
				else if (int.TryParse(sValue, out iValue))
				{
					if (iValue >= 0 && iValue <= MaxPromptTemplate)
						return iValue;
					else
						return 0;
				}
				else
				{
					switch (sValue)
					{
					case "text":
					case "plain":
					case "general":
						return 1;
					case "chatml":
						return 2;
					case "llama3":
						return 3;
					case "gemma":
					case "gemma2":
						return 4;
					case "commandr":
					case "command-r":
						return 5;
					case "mistral":
					case "mistralinstruct":
					case "mistral-instruct":
						return 6;
					default:
						return 0;
					}
				}
			}

			public static ChatParameters Default
			{
				get
				{
					return new ChatParameters() {
						model = null,
						temperature = 1.2m,
						topP = 0.9m,
						minP = 0.1m,
						topK = 30,
						minPEnabled = true,
						repeatLastN = 256,
						repeatPenalty = 1.05m,
						iPromptTemplate = 0,
					};
				}
			}

			public object Clone()
			{
				return new ChatParameters() {
					_modelId = this._modelId,
					temperature = this.temperature,
					topP = this.topP,
					minP = this.minP,
					topK = this.topK,
					minPEnabled = this.minPEnabled,
					repeatLastN = this.repeatLastN,
					repeatPenalty = this.repeatPenalty,
					iPromptTemplate = this.iPromptTemplate,
				};
			}

			public ChatParameters Copy()
			{
				return (ChatParameters)Clone();
			}
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

		public class Link : IXmlLoadable, IXmlSaveable
		{
			public string characterId;
			public DateTime updateDate;
			public bool isActive;
			public bool isDirty;

			public string filename
			{
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
			public string filenameHash
			{
				get { return _filenameHash; }
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
					if (Current.GetCharacter(characterId, out character))
					{
						if (character.updateDate > updateDate)
							isDirty = true; // Outdated
					}
					else // Unrecognized character
					{
						isActive = false;
					}
				}

				if (CompareFilename(Ginger.Current.Filename) == false)
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
			UnsupportedFeature,
			Unknown,
		}

		public static IBackyardImplementation Current
		{
			get { return _Instance; }
			private set { _Instance = value; }
		}
		private static IBackyardImplementation _Instance = null;
		public static string LastError;

		public static bool ConnectionEstablished { get { return Current != null; } }

		public static IEnumerable<FolderInstance> Folders { get { return Current != null ? Current.Folders : new FolderInstance[0]; } }
		public static IEnumerable<CharacterInstance> Characters { get { return Current != null ? Current.Characters : new CharacterInstance[0]; } }
		public static IEnumerable<GroupInstance> Groups { get { return Current != null ? Current.Groups : new GroupInstance[0];; } }

		public static IEnumerable<CharacterInstance> CharactersWithGroup { get { return Characters.Where(c => c.groupId != null); } }
		public static IEnumerable<CharacterInstance> NonUserCharacters { get { return CharactersWithGroup.Where(c => c.isCharacter); } }
		public static IEnumerable<GroupInstance> SupportedGroups { get { return Groups.Where(g => g.isSupported); } }

		public static Error EstablishConnection()
		{
			try
			{
				using (var connection = BackyardUtil.CreateSQLiteConnection())
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
						BackyardValidation.DatabaseVersion = BackyardDatabaseVersion.Version_0_29_0;
					else if (foundTables.Contains("GroupConfig"))
						BackyardValidation.DatabaseVersion = BackyardDatabaseVersion.Version_0_28_0;
					else // Outdated or unknown
					{
						LastError = "Validation failed";
						return Error.ValidationFailed;
					}

					// Compare database structure with known tables
					var validationTable = BackyardValidation.TablesByVersion[BackyardValidation.DatabaseVersion];
					if (AppSettings.BackyardLink.Strict && foundTables.Count != validationTable.Length)
					{
						LastError = "Validation failed";
						return Error.ValidationFailed;
					}

					// Check table names and columns
					for (int i = 0; i < validationTable.Length; ++i)
					{
						string[] table = validationTable[i];
						string[] expectedNames = new string[(table.Length - 1) / 2];
						string[] expectedTypes = new string[(table.Length - 1) / 2];
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

					// Select instance
					switch (BackyardValidation.DatabaseVersion)
					{
					case BackyardDatabaseVersion.Version_0_28_0:
					case BackyardDatabaseVersion.Version_0_29_0:
					case BackyardDatabaseVersion.Version_0_36_0:
						_Instance = new BackyardImpl_v1();
						break;
					default:
						_Instance = null;
						LastError = "Validation failed";
						return Error.ValidationFailed;
					}

					// Read settings
					using (var cmdSettings = connection.CreateCommand())
					{
						cmdSettings.CommandText = @"SELECT modelDownloadLocation, modelsJson FROM AppSettings";

						using (var reader = cmdSettings.ExecuteReader())
						{
							if (reader.Read())
							{
								string modelDirectory = reader[0] as string;
								string modelsJson = reader[1] as string;

								// Read model info
								BackyardModelDatabase.FindModels(modelDirectory, modelsJson);
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
			if (_Instance != null)
				LastError = _Instance.LastError;
			_Instance = null;
			BackyardValidation.DatabaseVersion = BackyardDatabaseVersion.Unknown;
			AppSettings.BackyardLink.Enabled = false;
			SQLiteConnection.ClearAllPools(); // Releases the lock on the db file
		}

		public static Error RefreshCharacters()
		{
			if (Current == null)
				return Error.NotConnected;
			return Current.RefreshCharacters();
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

	public static class BackyardUtil
	{
		public static SQLiteConnection CreateSQLiteConnection()
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

		public static Backyard.ImageInput[] GatherImages()
		{
			var lsImages = new List<Backyard.ImageInput>();
			var assets = (AssetCollection)Current.Card.assets.Clone();
			AssetFile mainPortraitOverride = assets.GetMainPortraitOverride();
			if (Current.Card.portraitImage == null && mainPortraitOverride == null)
				mainPortraitOverride = assets.GetPortraitAsset();

			if (mainPortraitOverride != null) // Embedded portrait (animated)
			{
				lsImages.Add(new Backyard.ImageInput() {
					asset = mainPortraitOverride,
					fileExt = mainPortraitOverride.ext,
				});
				assets.Remove(mainPortraitOverride);
			}
			else if (Current.Card.portraitImage != null) // Main portrait (not animated)
			{
				lsImages.Add(new Backyard.ImageInput() {
					image = Current.Card.portraitImage,
					fileExt = "png",
				});
			}
			else // Default portrait
			{
				lsImages.Add(new Backyard.ImageInput() {
					image = DefaultPortrait.Image,
					fileExt = "png",
				});
			}

			// Portrait as background?
			if (AppSettings.BackyardLink.UsePortraitAsBackground
				&& (Current.Card.portraitImage != null || mainPortraitOverride != null)
				&& assets.ContainsNoneOf(a => a.assetType == AssetFile.AssetType.Background))
			{
				if (assets == null)
					assets = new AssetCollection();

				AssetFile portraitBackground;
				if (mainPortraitOverride != null)
				{
					portraitBackground = new AssetFile() {
						name = "Portrait background",
						ext = mainPortraitOverride.ext,
						assetType = AssetFile.AssetType.Background,
						data = mainPortraitOverride.data,
						uriType = AssetFile.UriType.Embedded,
					};
				}
				else
				{
					portraitBackground = new AssetFile() {
						name = "Portrait background",
						ext = "jpeg",
						assetType = AssetFile.AssetType.Background,
						data = AssetData.FromBytes(Utility.ImageToMemory(Current.Card.portraitImage, Utility.ImageFileFormat.Jpeg)),
						uriType = AssetFile.UriType.Embedded,
					};
				}

				assets.Add(portraitBackground);
				Current.Card.assets.Add(portraitBackground);
				Current.IsFileDirty = true;
			}

			if (assets != null)
			{
				foreach (var asset in assets
					.Where(a => a.isEmbeddedAsset
						&& (a.assetType == AssetFile.AssetType.Icon || a.assetType == AssetFile.AssetType.Expression)
						&& a.data.length > 0))
				{
					lsImages.Add(new Backyard.ImageInput() {
						asset = asset,
						fileExt = asset.ext,
					});
				}

				var backgroundAsset = assets.FirstOrDefault(a => a.assetType == AssetFile.AssetType.Background);
				if (backgroundAsset != null)
				{
					lsImages.Add(new Backyard.ImageInput() {
						asset = backgroundAsset,
						fileExt = backgroundAsset.ext,
					});
				}

				var userIconAsset = assets.FirstOrDefault(a => a.assetType == AssetFile.AssetType.UserIcon);
				if (userIconAsset != null)
				{
					lsImages.Add(new Backyard.ImageInput() {
						asset = userIconAsset,
						fileExt = userIconAsset.ext,
					});
				}
			}

			return lsImages.ToArray();
		}

		public static void ToPartyNames(Backyard.ChatStaging staging, string characterId, string userId)
		{
			if (string.IsNullOrEmpty(staging.system) == false)
				ToPartyNames(ref staging.system, characterId, userId);
			if (string.IsNullOrEmpty(staging.scenario) == false)
				ToPartyNames(ref staging.scenario, characterId, userId);
			if (string.IsNullOrEmpty(staging.greeting) == false)
				ToPartyNames(ref staging.greeting, characterId, userId);
			if (string.IsNullOrEmpty(staging.example) == false)
				ToPartyNames(ref staging.example, characterId, userId);
			if (string.IsNullOrEmpty(staging.authorNote) == false)
				ToPartyNames(ref staging.authorNote, characterId, userId);
		}

		public static void ToPartyNames(ref string text, string characterId, string userId)
		{
			if (string.IsNullOrEmpty(text))
				return;

			var sb = new StringBuilder(text);

			// Character placeholder
			string characterPlaceholder;
			if (string.IsNullOrEmpty(characterId) == false)
				characterPlaceholder = $"{{_cfg&:{characterId}:cfg&_}}";
			else
				characterPlaceholder = Current.MainCharacter.namePlaceholder;
			sb.Replace("{character}", characterPlaceholder, false);

			// User placeholder
			if (string.IsNullOrEmpty(userId) == false)
				sb.Replace("{user}", $"{{_cfg&:{userId}:cfg&_}}", false);

			text = sb.ToString();
		}

		public static void FromPartyNames(Backyard.ChatStaging staging, string groupId)
		{
			var knownIds = new Dictionary<string, string>();

			if (string.IsNullOrEmpty(staging.system) == false)
				FromPartyNames(ref staging.system, groupId, knownIds);
			if (string.IsNullOrEmpty(staging.scenario) == false)
				FromPartyNames(ref staging.scenario, groupId, knownIds);
			if (string.IsNullOrEmpty(staging.greeting) == false)
				FromPartyNames(ref staging.greeting, groupId, knownIds);
			if (string.IsNullOrEmpty(staging.example) == false)
				FromPartyNames(ref staging.example, groupId, knownIds);
			if (string.IsNullOrEmpty(staging.authorNote) == false)
				FromPartyNames(ref staging.authorNote, groupId, knownIds);
		}

		public static void FromPartyNames(ref string text, string groupId, Dictionary<string, string> knownIds = null)
		{
			if (string.IsNullOrEmpty(text))
				return;

			int pos_begin = text.IndexOf("{_cfg&:");
			if (pos_begin == -1)
				return;

			var sb = new StringBuilder(text);
			while (pos_begin != -1)
			{
				int pos_end = sb.IndexOf(":cfg&_}", pos_begin + 7);
				if (pos_end == -1)
					break;

				string characterId = sb.Substring(pos_begin + 7, pos_end - pos_begin - 7);
				sb.Remove(pos_begin, pos_end - pos_begin + 7);

				string placeholder = "{character}";
				if (knownIds != null && knownIds.TryGetValue(characterId, out placeholder))
				{
					sb.Insert(pos_begin, placeholder);
				}
				else if (Backyard.ConnectionEstablished)
				{
					Backyard.CharacterInstance character;
					if (Backyard.Current.GetCharacter(characterId, out character))
					{
						if (character.isUser)
							placeholder = "{user}";
						else if (groupId != null)
						{
							Backyard.GroupInstance group;
							if (!(Backyard.Current.GetGroup(groupId, out group) && group.members != null && group.members.Contains(characterId)))
								placeholder = character.name; // Not primary character
						}
					}
					sb.Insert(pos_begin, placeholder);

					if (knownIds != null)
						knownIds.Add(characterId, placeholder);
				}
				else
				{
					sb.Insert(pos_begin, placeholder);
				}
				
				pos_begin = sb.IndexOf("{_cfg&:", pos_begin);
			}

			text = sb.ToString();
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

	}
}
