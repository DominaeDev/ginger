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
	using FaradayCard = FaradayCardV4;

	public static class Backyard
	{
		public struct ConfirmDeleteResult
		{
			public string[] characterIds;
			public string[] groupIds;
			public string[] imageIds;
			public string[] imageUrls;
		}

		public struct CreateCharacterArguments
		{
			public FaradayCard card;
			public ImageInput[] imageInput;
			public FolderInstance folder;
			public UserData userInfo;
			public BackupData.Chat[] chats;
		}

		public struct CreatePartyArguments
		{
			public FaradayCard[] cards;
			public ImageInput[] imageInput;
			public FolderInstance folder;
			public UserData userInfo;
			public BackupData.Chat[] chats;
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
			public bool hasLorebook;

			public bool isDefined { get { return instanceId != null; } }
			public bool isCharacter { get { return isDefined && !isUser; } }
			
			public string inferredGender
			{
				get
				{
					if (_inferredGender == null)
						_inferredGender = Utility.InferGender(persona);
					return _inferredGender;
				}
			}
			private string _inferredGender;
		}

		public struct GroupInstance
		{
			public string instanceId;			// GroupConfig.id
			public string displayName;			// GroupConfig.name
			public string folderId;				// GroupConfig.folderId
			public string hubCharId;			// GroupConfig.hubCharId
			public string hubAuthorUsername;    // GroupConfig.hubAuthorUsername
			public string folderSortPosition;	// GroupConfig.folderSortPosition
			public DateTime creationDate;		// CharacterConfig.createdAt
			public DateTime updateDate;			// CharacterConfig.updatedAt
			public string[] members;			// CharacterConfigVersion.id ...

			public bool isDefined { get { return instanceId != null; } }
			public bool isParty { get { return isDefined && Count > 2; } }
			public int Count { get { return members != null ? members.Length : 0; } }

			public enum GroupType
			{
				Unknown,
				Solo,	// 1-on-1
				Party,	// 1-on-many
			}

			public GroupType GetGroupType()
			{
				if (members == null || members.Length < 2)
					return GroupType.Unknown;

				var memberInfo = members
					.Select(id => Backyard.Database.GetCharacter(id))
					.Where(m => string.IsNullOrEmpty(m.instanceId) == false)
					.ToArray();
				int nUsers = memberInfo.Count(m => m.isUser);
				int nCharacters = memberInfo.Count(m => m.isCharacter);
				if (nUsers == 1)
				{
					if (nCharacters == 1)
						return GroupType.Solo;
					else if (nCharacters > 1)
						return GroupType.Party;
				}
				return GroupType.Unknown;
			}

			public string[] GetMemberNames(bool includingUser = false)
			{
				if (this.members == null || this.members.Length == 0)
					return new string[0];

				if (includingUser)
				{
					return this.members
						.Select(id => Backyard.Database.GetCharacter(id))
						.OrderBy(c => c.isCharacter)
						.ThenBy(c => c.creationDate)
						.Select(c => c.name)
						.ToArray();
				}
				else
				{
					return this.members
						.Select(id => Backyard.Database.GetCharacter(id))
						.Where(c => c.isCharacter)
						.OrderBy(c => c.creationDate)
						.Select(c => c.name)
						.ToArray();
				}
			}

			public string GetDisplayName()
			{
				if (string.IsNullOrEmpty(this.displayName) == false)
					return this.displayName;

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
			public string greeting = "";                // Chat.greetingDialogue
			public string greetingCharacterId = null;	
			public string example = "";					// Chat.customDialogue
			public string grammar = "";					// Chat.grammar
			public string authorNote = "";				// Chat.authorNote
			public bool pruneExampleChat = true;		// Chat.canDeleteCustomDialogue
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

			public string[] participants = null; // IDs
			public ChatHistory history = new ChatHistory();
			public ChatStaging staging = null;
			public ChatParameters parameters = null;

			public string name { get { return history.name; } }
			public bool hasGreeting { get { return staging != null && string.IsNullOrEmpty(staging.greeting) == false; } }
			public bool hasBackground { get { return staging != null && staging.background != null && string.IsNullOrEmpty(staging.background.imageUrl) == false; } }

			public static string DefaultName = "Untitled chat";
		}
		
		public struct ChatCount
		{
			public int count;
			public DateTime lastMessage;
			public bool hasMessages { get { return lastMessage != default(DateTime); } }
		}

		public class Link : IXmlLoadable, IXmlSaveable
		{
			public struct Actor {
				public string remoteId;
				public string localId;
			};
			public Actor[] actors;
			public string groupId;

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

			public string mainActorId
			{
				get
				{
					if (actors != null && actors.Length > 0)
						return actors[0].remoteId;
					return null;
				}
			}

			public enum LinkType
			{
				Solo,			// 1 character w/ group
				StandAlone,		// 1 character w/o group
				Group,			// 2+ characters w/ group
				GroupMember,	// 1 character in group of 2+
			}
			public LinkType linkType = LinkType.Solo;

			public struct Image
			{
				public string uid;
				public string filename;
			}
			public Image[] imageLinks;

			public bool LoadFromXml(XmlNode xmlNode)
			{
				string characterId = xmlNode.GetAttribute("id", null);
				if (characterId != null) // Legacy
				{
					actors = new Actor[] { 
						new Actor() {
							remoteId = characterId,
							localId = null, // Unknown at this point
						},
					};
				}
				else
				{
					List<Actor> lsActors = new List<Actor>();
					var actorNode = xmlNode.GetFirstElement("Actor");
					if (actorNode != null)
					{
						while (actorNode != null)
						{
							string remoteId = actorNode.GetAttribute("remote", null);
							string localId = actorNode.GetAttribute("local", null);
							if (remoteId != null && localId != null)
							{
								lsActors.Add(new Actor() {
									remoteId = remoteId,
									localId = localId,
								});
							}
							actorNode = actorNode.GetNextSibling();
						}
						actors = lsActors.ToArray();
					}
					else
						return false;
				}

				groupId = xmlNode.GetAttribute("group", null);
				isActive = xmlNode.GetAttributeBool("active");
				updateDate = DateTimeExtensions.FromUnixTime(xmlNode.GetAttributeLong("updated"));
				isDirty = xmlNode.GetAttributeBool("dirty");
				linkType = xmlNode.GetAttributeEnum("type", LinkType.Solo);

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
				return actors.Length > 0;
			}

			public void SaveToXml(XmlNode xmlNode)
			{
				foreach (var actor in actors)
				{
					var actorNode = xmlNode.AddElement("Actor");
					actorNode.AddAttribute("remote", actor.remoteId);
					actorNode.AddAttribute("local", actor.localId);
				}
				if (groupId != null)
					xmlNode.AddAttribute("group", groupId);
				
				xmlNode.AddAttribute("active", isActive);
				xmlNode.AddAttribute("updated", updateDate.ToUnixTimeMilliseconds());
				if (isActive)
					xmlNode.AddAttribute("dirty", isDirty);
				xmlNode.AddAttribute("type", EnumHelper.ToString(linkType));

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
				if (isActive == false)
					return;

				if (ConnectionEstablished)
				{
					if (groupId == null)
						groupId = Database.GetGroupForCharacter(mainActorId).instanceId;

					if (groupId != null)
					{
						GroupInstance group;
						if (Database.GetGroup(groupId, out group))
						{
							if (group.updateDate > updateDate)
								isDirty = true; // Outdated
						}
						else // Unrecognized group
						{
							isActive = false;
						}

					}
					int foundCharacters = 0;
					if (actors != null)
					{
						for (int i = 0; i < actors.Length; ++i)
						{
							CharacterInstance character;
							if (Database.GetCharacter(actors[i].remoteId, out character))
							{
								if (character.updateDate > updateDate)
									isDirty = true; // Outdated
								foundCharacters++;
							}
						}
					}

					if (foundCharacters == 0) // Unrecognized character
						isActive = false;
				}

				if (CompareFilename(Current.Filename) == false)
					isActive = false; // Different filename
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

			public Link Clone()
			{
				var clone = (Link)MemberwiseClone();
				if (this.actors != null)
				{
					clone.actors = new Actor[this.actors.Length];
					Array.Copy(this.actors, clone.actors, this.actors.Length);
				}
				if (this.imageLinks != null)
				{
					clone.imageLinks = new Image[this.imageLinks.Length];
					Array.Copy(this.imageLinks, clone.imageLinks, this.imageLinks.Length);
				}

				return clone;
			}

			public void ValidateImages(ImageRef mainPortrait, AssetCollection assets)
			{
				// Update image links
				if (imageLinks == null)
					return;

				var uids = new HashSet<string>();
				if (mainPortrait != null)
					uids.Add(mainPortrait.uid);
				if (assets != null)
					uids.UnionWith(assets.Select(a => a.uid));

				imageLinks = imageLinks
					.Where(l => uids.Contains(l.uid))
					.ToArray();
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

		public static IBackyardDatabase Database
		{
			get { return _db; }
			private set { _db = value; }
		}
		private static IBackyardDatabase _db = null;
		public static string LastError = null;

		public static bool ConnectionEstablished { get { return Database != null; } }

		public static IEnumerable<CharacterInstance> Everyone { get { return Database != null ? Database.Everyone : new CharacterInstance[0]; } }
		public static IEnumerable<CharacterInstance> Characters { get { return Database != null ? Database.Characters : new CharacterInstance[0]; } }
		public static IEnumerable<CharacterInstance> Users { get { return Database != null ? Database.Users : new CharacterInstance[0]; } }
		public static IEnumerable<CharacterInstance> CharactersWithGroup { get { return Characters.Where(c => c.groupId != null); } }
		public static IEnumerable<CharacterInstance> EveryoneWithGroup { get { return Everyone.Where(c => c.groupId != null); } }

		public static IEnumerable<GroupInstance> Groups { get { return Database != null ? Database.Groups : new GroupInstance[0]; } }
		public static IEnumerable<FolderInstance> Folders { get { return Database != null ? Database.Folders : new FolderInstance[0]; } }

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
					if (foundTables.Contains("GroupConfigCharacterLink"))
						BackyardValidation.DatabaseVersion = BackyardDatabaseVersion.Version_0_37_0;
					else if (foundTables.Contains("BackgroundChatImage"))
						BackyardValidation.DatabaseVersion = BackyardDatabaseVersion.Version_0_29_0;
					else if (foundTables.Contains("GroupConfig"))
						BackyardValidation.DatabaseVersion = BackyardDatabaseVersion.Version_0_28_0;

					if (BackyardValidation.DatabaseVersion == BackyardDatabaseVersion.Unknown) // Outdated or unsupported
					{
						LastError = "Validation failed";
						return Error.ValidationFailed;
					}

					// Compare database structure with known tables
					var validationTable = BackyardValidation.TablesByVersion[BackyardValidation.DatabaseVersion];

					// Ignore tables we know of but don't care about
					var ignoredTables = validationTable
						.Where(t => t.Length == 1)
						.Select(t => t[0])
						.ToArray();
					foundTables.ExceptWith(ignoredTables);
					validationTable = validationTable.Where(t => t.Length > 1).ToArray();

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
						_db = new BackyardDatabase_v28();
						break;
					case BackyardDatabaseVersion.Version_0_37_0:
						_db = new BackyardDatabase_v37();
						break;
					default:
						_db = null;
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
				return Error.Unknown;
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
			if (_db != null)
				LastError = _db.LastError;
			_db = null;
			BackyardValidation.DatabaseVersion = BackyardDatabaseVersion.Unknown;
			AppSettings.BackyardLink.Enabled = false;
			SQLiteConnection.ClearAllPools(); // Releases the lock on the db file
		}

		public static Error RefreshCharacters()
		{
			if (_db == null)
				return Error.NotConnected;
			return _db.RefreshCharacters();
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
			AssetFile mainPortraitOverride = assets.GetPortraitOverride();

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
			else
			{
				AssetFile portraitAsset = assets.GetPortrait();
				if (portraitAsset != null) // Portrait asset
				{
					lsImages.Add(new Backyard.ImageInput() {
						asset = portraitAsset,
						fileExt = portraitAsset.ext,
					});
					assets.Remove(portraitAsset);
				}
				else // Default portrait
				{
					lsImages.Add(new Backyard.ImageInput() {
						image = DefaultPortrait.Image,
						fileExt = "png",
					});
				}
			}

			// Portrait as background?
			if (AppSettings.BackyardLink.UsePortraitAsBackground
				&& (Current.Card.portraitImage != null || mainPortraitOverride != null)
				&& assets.ContainsNoneOf(a => a.assetType == AssetFile.AssetType.Background))
			{
				AssetFile portraitBackground;
				if (Current.Card.assets.AddBackgroundFromPortrait(out portraitBackground))
				{
					assets.Add(portraitBackground);
					Current.IsFileDirty = true;
				}
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

		public static void ConvertToIDPlaceholders(FaradayCard card, string characterId)
		{
			ConvertToIDPlaceholders(new FaradayCard[] { card }, new string[] { characterId });
		}

		public static void ConvertToIDPlaceholders(FaradayCard[] cards, string[] characterIds)
		{
			var replacements = new List<KeyValuePair<string, string>>();
			for (int i = 0; i < cards.Length && i < characterIds.Length; ++i)
			{
				string src = GingerString.MakeInternalCharacterMarker(i);
				string dest;
				if (string.IsNullOrEmpty(characterIds[i]))
					dest = cards[i].data.name;
				else
					dest = $"{{_cfg&:{characterIds[i]}:cfg&_}}";
				replacements.Add(new KeyValuePair<string, string>(src, dest));
			}

			for (int i = 0; i < cards.Length; ++i)
			{
				foreach (var kvp in replacements)
					Convert(cards[i], kvp.Key, kvp.Value);
				Convert(cards[i], GingerString.BackyardCharacterMarker, replacements[0].Value); // jic
			}

			void Convert(FaradayCard card, string src, string dest)
			{
				__ToID(ref card.data.system, src, dest);
				__ToID(ref card.data.scenario, src, dest);
				__ToID(ref card.data.persona, src, dest);
				__ToID(ref card.data.greeting, src, dest);
				__ToID(ref card.data.example, src, dest);
				__ToID(ref card.userPersona, src, dest);
				__ToID(ref card.authorNote, src, dest);
				
				if (card.data.loreItems != null)
				{
					for (int idxLore = 0; idxLore < card.data.loreItems.Length; ++idxLore)
						__ToID(ref card.data.loreItems[idxLore].value, src, dest);
				}
			}
		}

		public static void ConvertToIDPlaceholders(ref string text, string[] characterIds)
		{
			var replacements = new List<KeyValuePair<string, string>>();
			for (int i = 0; i < characterIds.Length; ++i)
			{
				string src = GingerString.MakeInternalCharacterMarker(i);
				string dest = $"{{_cfg&:{characterIds[i]}:cfg&_}}";
				replacements.Add(new KeyValuePair<string, string>(src, dest));
			}

			foreach (var kvp in replacements)
				__ToID(ref text, kvp.Key, kvp.Value);
			__ToID(ref text, GingerString.BackyardCharacterMarker, replacements[0].Value); // jic			
		}

		public static string CreateIDPlaceholder(string characterId)
		{
			return $"{{_cfg&:{characterId}:cfg&_}}";
		}

		private static void __ToID(ref string text, string src, string dest)
		{
			if (string.IsNullOrEmpty(text))
				return;

			text = text.Replace(src, dest);
		}

		public static void ConvertToIDPlaceholders(Backyard.ChatStaging staging, string characterId)
		{
			if (string.IsNullOrEmpty(staging.system) == false)
				ConvertToIDPlaceholders(ref staging.system, characterId);
			if (string.IsNullOrEmpty(staging.scenario) == false)
				ConvertToIDPlaceholders(ref staging.scenario, characterId);
			if (string.IsNullOrEmpty(staging.greeting) == false)
				ConvertToIDPlaceholders(ref staging.greeting, characterId);
			if (string.IsNullOrEmpty(staging.example) == false)
				ConvertToIDPlaceholders(ref staging.example, characterId);
			if (string.IsNullOrEmpty(staging.authorNote) == false)
				ConvertToIDPlaceholders(ref staging.authorNote, characterId);
		}

		public static void ConvertToIDPlaceholders(Backyard.ChatStaging staging, string[] characterIds)
		{
			if (string.IsNullOrEmpty(staging.system) == false)
				ConvertToIDPlaceholders(ref staging.system, characterIds);
			if (string.IsNullOrEmpty(staging.scenario) == false)
				ConvertToIDPlaceholders(ref staging.scenario, characterIds);
			if (string.IsNullOrEmpty(staging.greeting) == false)
				ConvertToIDPlaceholders(ref staging.greeting, characterIds);
			if (string.IsNullOrEmpty(staging.example) == false)
				ConvertToIDPlaceholders(ref staging.example, characterIds);
			if (string.IsNullOrEmpty(staging.authorNote) == false)
				ConvertToIDPlaceholders(ref staging.authorNote, characterIds);
		}

		public static void ConvertToIDPlaceholders(ref string text, string characterId)
		{
			if (string.IsNullOrEmpty(text))
				return;

			var sb = new StringBuilder(text);

			// Character placeholder
			string characterPlaceholder;
			if (string.IsNullOrEmpty(characterId) == false)
				characterPlaceholder = $"{{_cfg&:{characterId}:cfg&_}}";
			else
				characterPlaceholder = Current.MainCharacter.name;
			sb.Replace(GingerString.BackyardCharacterMarker, characterPlaceholder, false); // jic

			text = sb.ToString();
		}

		public static void ConvertFromIDPlaceholders(params FaradayCard[] cards)
		{
			var knownIds = new Dictionary<string, string>();
			for (int i = 0; i < cards.Length; ++i)
				__FromID(cards[i], knownIds);
		}

		public static void ConvertFromIDPlaceholders(Backyard.ChatStaging staging)
		{
			var knownIds = new Dictionary<string, string>();
			__FromID(ref staging.system, knownIds);
			__FromID(ref staging.scenario, knownIds);
			__FromID(ref staging.greeting, knownIds);
			__FromID(ref staging.example, knownIds);
			__FromID(ref staging.authorNote, knownIds);
		}

		public static void ConvertFromIDPlaceholders(ref string text)
		{
			var knownIds = new Dictionary<string, string>();
			__FromID(ref text, knownIds);
		}

		private static void __FromID(FaradayCard card, Dictionary<string, string> dict)
		{
			__FromID(ref card.data.system, dict);
			__FromID(ref card.data.scenario, dict);
			__FromID(ref card.data.persona, dict);
			__FromID(ref card.data.greeting, dict);
			__FromID(ref card.data.example, dict);
			__FromID(ref card.userPersona, dict);
			__FromID(ref card.authorNote, dict);
				
			if (card.data.loreItems != null)
			{
				for (int idxLore = 0; idxLore < card.data.loreItems.Length; ++idxLore)
					__FromID(ref card.data.loreItems[idxLore].value, dict);
			}
		}

		private static void __FromID(ref string text, Dictionary<string, string> dict)
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

				string placeholder = null;
				if (dict != null && dict.TryGetValue(characterId, out placeholder))
				{
					sb.Insert(pos_begin, placeholder);
				}
				else if (Backyard.ConnectionEstablished)
				{
					Backyard.CharacterInstance character;
					if (Backyard.Database.GetCharacter(characterId, out character))
					{
						if (character.isUser)
							placeholder = "{user}";
						else
							placeholder = character.name;
					}
					sb.Insert(pos_begin, placeholder ?? Constants.UnknownCharacter);

					if (dict != null)
						dict.Add(characterId, placeholder);
				}
				else
				{
					sb.Insert(pos_begin, placeholder);
				}
				
				pos_begin = sb.IndexOf("{_cfg&:", pos_begin);
			}

			text = sb.ToString();
		}

		public static void GetChatCounts(out Dictionary<string, Backyard.ChatCount> counts)
		{
			if (Backyard.ConnectionEstablished && Backyard.Database.GetChatCounts(out counts) == Backyard.Error.NoError)
				return;

			counts = new Dictionary<string, Backyard.ChatCount>(); // Empty
		}

		public static string CreateSortingString(object obj)
		{
			return CreateSequentialSortingString(0, 1, obj.GetHashCode());
		}

		public static string CreateSortingString(int hash)
		{
			return CreateSequentialSortingString(0, 1, hash);
		}

		public static string CreateSequentialSortingString(int index, int length, int hash)
		{
			RandomNoise rng = new RandomNoise(hash, 0);
			const string key = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
			char[] p = new char[6];
			for (int i = 0; i < p.Length; ++i)
				p[i] = key[rng.Int(52)];
			string prefix = new string(p);

			const int @base = 26;
			int maxIndex = length - 1;
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

		public enum SortPosition { Before, After };
		public static string CreateRelativeSortingString(SortPosition sortPos, string sortOrder)
		{
			RandomNoise rng = new RandomNoise();
			const string key = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
			char[] p = new char[6];
			for (int i = 0; i < p.Length; ++i)
				p[i] = key[rng.Int(52)];
			string prefix = new string(p);

			if (string.IsNullOrEmpty(sortOrder) == false)
			{
				if (sortPos == SortPosition.Before)
				{
					// Decrement last character
					sortOrder = string.Concat(sortOrder.Substring(0, sortOrder.Length - 1), (char)(sortOrder[sortOrder.Length - 1] - 1));
				}
				return string.Concat(sortOrder, ",", prefix, ".B");
			}
			else
			{
				return string.Concat(prefix, ".B");
			}
		}
	}
}
