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
	using CharacterInstance = Backyard.CharacterInstance;
	using FolderInstance = Backyard.FolderInstance;
	using GroupInstance = Backyard.GroupInstance;
	using ChatInstance = Backyard.ChatInstance;
	using ChatParameters = Backyard.ChatParameters;
	using ChatStaging = Backyard.ChatStaging;
	using ChatBackground = Backyard.ChatBackground;
	using ChatCount = Backyard.ChatCount;
	using CreateChatArguments = Backyard.CreateChatArguments;
	using ImageInput = Backyard.ImageInput;
	using ImageInstance = Backyard.ImageInstance;
	using ConfirmDeleteResult = Backyard.ConfirmDeleteResult;

	public class BackyardImpl_v1 : IBackyardImplementation
	{
		public IEnumerable<FolderInstance> Folders { get { return _Folders.Values; } }
		public IEnumerable<CharacterInstance> Characters { get { return _Characters.Values; } }
		public IEnumerable<GroupInstance> Groups { get { return _Groups.Values; } }

		private Dictionary<string, FolderInstance> _Folders = new Dictionary<string, FolderInstance>();
		private Dictionary<string, CharacterInstance> _Characters = new Dictionary<string, CharacterInstance>();
		private Dictionary<string, GroupInstance> _Groups = new Dictionary<string, GroupInstance>();

		public string LastError { get; private set; }

		public bool ConnectionEstablished = false;

		private SQLiteConnection CreateSQLiteConnection()
		{
			return BackyardUtil.CreateSQLiteConnection();
		}

#region Enumerate characters and groups
		public bool GetCharacter(string characterId, out CharacterInstance character)
		{
			return _Characters.TryGetValue(characterId, out character);
		}

		public bool GetGroup(string groupId, out GroupInstance group)
		{
			if (groupId != null)
				return _Groups.TryGetValue(groupId, out group);
			group = default(GroupInstance);
			return false;
		}

		public GroupInstance GetGroupForCharacter(string characterId)
		{
			return _Groups.Values
				.FirstOrDefault(g => g.members != null && g.members.Length == 2
				&& g.members.Contains(characterId));
		}

		public Backyard.Error RefreshCharacters()
		{
			if (ConnectionEstablished == false)
			{
				LastError = "Not connected";
				return Backyard.Error.NotConnected;
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
							}
						}
					}

					// Fetch group configs
					using (var cmdGroupData = connection.CreateCommand())
					{
						cmdGroupData.CommandText =
						@"
							SELECT 
								id, createdAt, updatedAt, name, folderId, folderSortPosition, hubCharId, hubAuthorUsername
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
								string folderSortPosition = reader.GetString(5);
								string hubCharId = reader[6] as string;
								string hubAuthorUsername = reader[7] as string;

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
										folderSortPosition = folderSortPosition,
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

								// Get group info
								GroupInstance groupInstance = GetGroupForCharacter(instanceId);
								string folderId = groupInstance.folderId;
								string folderSortPosition = groupInstance.folderSortPosition;
								string hubCharId = groupInstance.hubCharId;
								string hubAuthorUsername = groupInstance.hubAuthorUsername;

								_Characters.TryAdd(instanceId,
									new CharacterInstance() {
										instanceId = instanceId,
										configId = configId,
										groupId = groupInstance.instanceId,
										displayName = displayName,
										name = name,
										creationDate = createdAt,
										updateDate = updatedAt,
										isUser = isUser,
										persona = persona,
										loreEntries = numLoreEntries,
										creator = hubAuthorUsername ?? "",
										folderId = folderId,
										folderSortPosition = folderSortPosition,
									});
							}
						}
					}

					connection.Close();
					LastError = null;
					return Backyard.Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				LastError = "File not found";
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				LastError = "Sqlite returned: " + e.Message;
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				LastError = "Exception thrown: " + e.Message;
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}
#endregion

#region Characters
		public Backyard.Error ImportCharacter(CharacterInstance character, out FaradayCardV4 card, out ImageInstance[] images)
		{
			UserData unused;
			return ImportCharacter(character, out card, out images, out unused);
		}

		public Backyard.Error ImportCharacter(CharacterInstance character, out FaradayCardV4 card, out ImageInstance[] images, out UserData userInfo)
		{
			if (ConnectionEstablished == false)
			{
				card = null;
				images = null;
				userInfo = null;
				LastError = "Not connected";
				return Backyard.Error.NotConnected;
			}
			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					DateTime createdAt;
					DateTime updatedAt;
					string displayName = null;
					string name = null;
					string persona = null;
					ChatStaging staging = new ChatStaging();
					string hubCharId = null;
					string hubAuthorUsername = null;

					using (var cmdCharacter = connection.CreateCommand())
					{
						var sbCommand = new StringBuilder();
						sbCommand.AppendLine(
						@"
							SELECT 
								createdAt, updatedAt, displayName, name, persona
							FROM CharacterConfigVersion
							WHERE id = $configId
						");

						cmdCharacter.CommandText = sbCommand.ToString();
						cmdCharacter.Parameters.AddWithValue("$configId", character.configId);

						using (var reader = cmdCharacter.ExecuteReader())
						{
							if (!reader.Read())
							{
								images = null;
								userInfo = null;
								card = null;
								return Backyard.Error.NotFound; // No character
							}

							createdAt = reader.GetTimestamp(0);
							updatedAt = reader.GetTimestamp(1);
							displayName = reader.GetString(2);
							name = reader.GetString(3);
							persona = reader.GetString(4);
						}
					}

					if (character.groupId != null)
					{
						using (var cmdGroupData = connection.CreateCommand())
						{
							var sbCommand = new StringBuilder();
							sbCommand.AppendLine(
							@"
								SELECT 
									hubCharId, hubAuthorUsername
								FROM GroupConfig
								WHERE id = $groupId;
							");
							cmdGroupData.CommandText = sbCommand.ToString();
							cmdGroupData.Parameters.AddWithValue("$groupId", character.groupId);

							using (var reader = cmdGroupData.ExecuteReader())
							{
								if (reader.Read())
								{
									hubCharId = reader[0] as string;
									hubAuthorUsername = reader[1] as string;
								}
							}
						}

						using (var cmdChatData = connection.CreateCommand())
						{
							var sbCommand = new StringBuilder();
							sbCommand.AppendLine(
							@"
								SELECT 
									context, customDialogue, modelInstructions, greetingDialogue, grammar, authorNote
								FROM Chat
								WHERE groupConfigId = $groupId
							");

							if (AppSettings.BackyardLink.ApplyChatSettings == AppSettings.BackyardLink.ActiveChatSetting.First)
								sbCommand.AppendLine("ORDER BY createdAt ASC");
							else
								sbCommand.AppendLine("ORDER BY createdAt DESC");
							cmdChatData.CommandText = sbCommand.ToString();
							cmdChatData.Parameters.AddWithValue("$groupId", character.groupId);

							using (var reader = cmdChatData.ExecuteReader())
							{
								if (reader.Read())
								{
									staging.scenario = reader.GetString(0);
									staging.example = reader.GetString(1);
									staging.system = reader.GetString(2);
									staging.greeting = reader.GetString(3);
									staging.grammar = reader[4] as string;
									staging.authorNote = reader.GetString(5);
								}
							}
						}
					}

					if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats))
						FromPartyNames(staging, character.groupId);

					card = new FaradayCardV4();
					card.data.displayName = displayName;
					card.data.name = name;
					card.data.persona = persona;
					card.data.system = staging.system;
					card.data.scenario = staging.scenario;
					card.data.greeting = staging.greeting;
					card.data.example = staging.example;
					card.data.grammar = staging.grammar;
					card.authorNote = staging.authorNote;
					card.data.creationDate = createdAt.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
					card.data.updateDate = updatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
					card.hubCharacterId = hubCharId;
					card.hubAuthorUsername = hubAuthorUsername;

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
					var lsImages = new List<ImageInstance>();
					using (var cmdImageLookup = connection.CreateCommand())
					{
						cmdImageLookup.CommandText =
						@"
							SELECT 
								id, imageUrl, label, aspectRatio
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
								string instanceId = reader.GetString(0);
								string imageUrl = reader.GetString(1);
								string label = reader[2] as string;
								string aspectRatio = reader[3] as string ?? "";

								lsImages.Add(new ImageInstance() {
									instanceId = instanceId,
									imageUrl = imageUrl,
									label = label,
									aspectRatio = aspectRatio,
									imageType = AssetFile.AssetType.Icon,
								});
							}
						}
					}

					// Gather background image files
					if (BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds))
					{
						// Get existing chats
						ImageInstance[] backgrounds;
						if (FetchChatBackgrounds(connection, character.groupId, out backgrounds) == Backyard.Error.NoError)
							lsImages.AddRange(backgrounds);
					}

					// Get user info
					string userId;
					string userName;
					string userPersona;
					ImageInstance userImage;
					if (FetchUserInfo(connection, character.groupId, out userId, out userName, out userPersona, out userImage))
					{
						if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats))
							FromPartyNames(ref userPersona, character.groupId);

						userInfo = new UserData() {
							name = userName?.Trim(),
							persona = userPersona?.Trim(),
						};
						if (userImage != null)
							lsImages.Add(userImage);
					}
					else
						userInfo = null;

					images = lsImages.ToArray();
					connection.Close();
					LastError = null;
					return card == null ? Backyard.Error.NotFound : Backyard.Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				card = null;
				images = null;
				userInfo = null;
				LastError = "File not found";
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				card = null;
				images = null;
				userInfo = null;
				LastError = e.Message;
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				card = null;
				images = null;
				userInfo = null;
				LastError = e.Message;
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error GetImageUrls(CharacterInstance characterInstance, out string[] imageUrls)
		{
			if (ConnectionEstablished == false)
			{
				imageUrls = null;
				return Backyard.Error.NotConnected;
			}

			if (characterInstance.configId == null)
			{
				imageUrls = null;
				return Backyard.Error.InvalidArgument;
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
					return Backyard.Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				imageUrls = null;
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				imageUrls = null;
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				imageUrls = null;
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error ConfirmSaveCharacter(FaradayCardV4 card, Backyard.Link linkInfo, out bool newerChangesFound)
		{
			if (ConnectionEstablished == false)
			{
				newerChangesFound = default(bool);
				return Backyard.Error.NotConnected;
			}

			if (card == null || linkInfo == null || string.IsNullOrEmpty(linkInfo.characterId))
			{
				newerChangesFound = default(bool);
				return Backyard.Error.NotFound;
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
								updatedAt
							FROM CharacterConfigVersion
							WHERE characterConfigId = $charId
						";
						cmdCharacterData.Parameters.AddWithValue("$charId", linkInfo.characterId);

						using (var reader = cmdCharacterData.ExecuteReader())
						{
							if (reader.Read() == false)
							{
								newerChangesFound = default(bool);
								return Backyard.Error.NotFound;
							}

							DateTime updatedAt = reader.GetTimestamp(0);
							newerChangesFound = updatedAt > linkInfo.updateDate;

							connection.Close();
							return Backyard.Error.NoError;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				newerChangesFound = default(bool);
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				newerChangesFound = default(bool);
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				newerChangesFound = default(bool);
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error CreateNewCharacter(FaradayCardV4 card, ImageInput[] imageInput, BackupData.Chat[] chats, out CharacterInstance characterInstance, out Backyard.Link.Image[] imageLinks, UserData userInfo = null, FolderInstance folder = default(FolderInstance))
		{
			if (ConnectionEstablished == false)
			{
				characterInstance = default(CharacterInstance);
				imageLinks = null;
				return Backyard.Error.NotConnected;
			}

			if (card == null)
			{
				characterInstance = default(CharacterInstance);
				imageLinks = null;
				return Backyard.Error.InvalidArgument;
			}

			if (chats != null && chats.ContainsAny(c => c.history != null && c.history.numSpeakers > 2))
			{
				characterInstance = default(CharacterInstance);
				imageLinks = null;
				return Backyard.Error.InvalidArgument;
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
					return Backyard.Error.Unknown;
				}
			}

			bool bAllowUserPersona = userInfo != null;

			// Prepare image information
			List<ImageOutput> images = new List<ImageOutput>();
			List<ImageOutput> backgrounds = new List<ImageOutput>();
			ImageOutput userPortrait = default(ImageOutput);
			Dictionary<string, ImageOutput> backgroundUrlByName = new Dictionary<string, ImageOutput>();

			ImageOutput[] imageOutput;
			if (PrepareImages(imageInput, out imageOutput, out imageLinks))
			{
				images = imageOutput.Where(i => i.imageType == AssetFile.AssetType.Icon || i.imageType == AssetFile.AssetType.Expression).ToList();

				if (BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds))
				{
					backgrounds = imageOutput.Where(i => i.imageType == AssetFile.AssetType.Background)
						.ToList();
					for (int i = 0; i < imageInput.Length && i < imageOutput.Length; ++i)
					{
						if (imageOutput[i].imageType == AssetFile.AssetType.Background)
							backgroundUrlByName.TryAdd(imageInput[i].asset.name, imageOutput[i]);
					}

					if (chats == null || chats.Length == 0)
						backgrounds = backgrounds.Take(1).ToList();
				}
				else
					backgrounds.Clear();

				// User portrait
				if (bAllowUserPersona)
				{
					int idxUserPortrait = Array.FindIndex(imageInput, i => i.asset != null && i.asset.assetType == AssetFile.AssetType.UserIcon);
					if (idxUserPortrait != -1)
						userPortrait = imageOutput[idxUserPortrait];
				}
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					string characterId = Cuid.NewCuid();
					string configId = Cuid.NewCuid();
					string groupId = Cuid.NewCuid();
					string userId = null;
					string userConfigId = null;
					DateTime now = DateTime.Now;
					long createdAt = now.ToUnixTimeMilliseconds();
					string folderOrder = null;
					string[] chatIds = null;
					string defaultModel = null;
					ChatParameters chatParameters = AppSettings.BackyardSettings.UserSettings;
					string folderSortPosition = null;

					// Fetch default user
					if (FetchDefaultUser(connection, out userId) == false)
					{
						characterInstance = default(CharacterInstance);
						imageLinks = null;
						return Backyard.Error.SQLCommandFailed; // Requires default user
					}

					// Fetch default user
					FetchDefaultModel(connection, out defaultModel);

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
						folderSortPosition = MakeFolderSortPosition(SortPosition.Before, folderOrder);
					}

					// Write to database
					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							int updates = 0;
							int expectedUpdates = 0;

							// Create custom user (default user as base)
							if (bAllowUserPersona)
							{
								if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats))
									ToPartyNames(ref userInfo.persona, characterId, userId);
								WriteUser(connection, null, userInfo, userPortrait, out userId, out userConfigId, out userPortrait, ref updates, ref expectedUpdates);
							}

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
										($groupId, $timestamp, $timestamp, $isNSFW, $folderId, $folderSortPosition, $groupName);
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

									var staging = new ChatStaging() {
										system = card.data.system ?? "",
										scenario = card.data.scenario ?? "",
										greeting = card.data.greeting ?? "",
										example = card.data.example ?? "",
										grammar = card.data.grammar ?? "",
										authorNote = card.authorNote ?? "",
									};

									if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats))
										ToPartyNames(staging, characterId, userId);

									cmdCreate.Parameters.AddWithValue("$chatId", chatId);
									cmdCreate.Parameters.AddWithValue("$system", staging.system ?? "");
									cmdCreate.Parameters.AddWithValue("$scenario", staging.scenario ?? "");
									cmdCreate.Parameters.AddWithValue("$example", staging.example ?? "");
									cmdCreate.Parameters.AddWithValue("$greeting", staging.greeting ?? "");
									cmdCreate.Parameters.AddWithValue("$grammar", staging.grammar ?? "");
									cmdCreate.Parameters.AddWithValue("$authorNote", staging.authorNote ?? "");
									cmdCreate.Parameters.AddWithValue("$model", chatParameters.model ?? defaultModel ?? "");
									cmdCreate.Parameters.AddWithValue("$temperature", chatParameters.temperature);
									cmdCreate.Parameters.AddWithValue("$topP", chatParameters.topP);
									cmdCreate.Parameters.AddWithValue("$minP", chatParameters.minP);
									cmdCreate.Parameters.AddWithValue("$minPEnabled", chatParameters.minPEnabled);
									cmdCreate.Parameters.AddWithValue("$topK", chatParameters.topK);
									cmdCreate.Parameters.AddWithValue("$repeatPenalty", chatParameters.repeatPenalty);
									cmdCreate.Parameters.AddWithValue("$repeatLastN", chatParameters.repeatLastN);
									cmdCreate.Parameters.AddWithValue("$promptTemplate", chatParameters.promptTemplate);
									cmdCreate.Parameters.AddWithValue("$pruneExample", AppSettings.BackyardLink.PruneExampleChat);

									expectedUpdates += 1;

									// Add background image
									if (BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds) && backgrounds.Count > 0)
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
								else // One or more chats
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
											system = card.data.system ?? "",
											scenario = card.data.scenario ?? "",
											greeting = card.data.greeting ?? "",
											example = card.data.example ?? "",
											grammar = card.data.grammar ?? "",
											authorNote = card.authorNote ?? "",
										};

										if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats))
											ToPartyNames(staging, characterId, userId);

										var parameters = chats[i].parameters ?? new ChatParameters();
										cmdCreate.Parameters.AddWithValue($"$system{i:000}", staging.system ?? "");
										cmdCreate.Parameters.AddWithValue($"$scenario{i:000}", staging.scenario ?? "");
										cmdCreate.Parameters.AddWithValue($"$example{i:000}", staging.example ?? "");
										cmdCreate.Parameters.AddWithValue($"$greeting{i:000}", staging.greeting ?? "");
										cmdCreate.Parameters.AddWithValue($"$grammar{i:000}", staging.grammar ?? "");
										cmdCreate.Parameters.AddWithValue($"$authorNote{i:000}", staging.authorNote ?? "");
										cmdCreate.Parameters.AddWithValue($"$pruneExample{i:000}", staging.pruneExampleChat);
										cmdCreate.Parameters.AddWithValue($"$ttsAutoPlay{i:000}", staging.ttsAutoPlay);
										cmdCreate.Parameters.AddWithValue($"$ttsInputFilter{i:000}", staging.ttsInputFilter);
										cmdCreate.Parameters.AddWithValue($"$model{i:000}", parameters.model ?? defaultModel ?? "");
										cmdCreate.Parameters.AddWithValue($"$temperature{i:000}", parameters.temperature);
										cmdCreate.Parameters.AddWithValue($"$topP{i:000}", parameters.topP);
										cmdCreate.Parameters.AddWithValue($"$minP{i:000}", parameters.minP);
										cmdCreate.Parameters.AddWithValue($"$minPEnabled{i:000}", parameters.minPEnabled);
										cmdCreate.Parameters.AddWithValue($"$topK{i:000}", parameters.topK);
										cmdCreate.Parameters.AddWithValue($"$repeatPenalty{i:000}", parameters.repeatPenalty);
										cmdCreate.Parameters.AddWithValue($"$repeatLastN{i:000}", parameters.repeatLastN);
										cmdCreate.Parameters.AddWithValue($"$promptTemplate{i:000}", parameters.promptTemplate);

										// Add background images
										if (BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds))
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
											($imageId{i:000}, $imageConfigId{i:000});
									");

									cmdCreate.Parameters.AddWithValue($"$imageId{i:000}", images[i].instanceId);
									cmdCreate.Parameters.AddWithValue($"$imageUrl{i:000}", images[i].imageUrl);
									cmdCreate.Parameters.AddWithValue($"$label{i:000}", images[i].label ?? "");
									cmdCreate.Parameters.AddWithValue($"$aspectRatio{i:000}", images[i].aspectRatio);
									cmdCreate.Parameters.AddWithValue($"$imageConfigId{i:000}", configId);

									expectedUpdates += 2;
								}

								cmdCreate.CommandText = sbCommand.ToString();
								cmdCreate.Parameters.AddWithValue("$charId", characterId);
								cmdCreate.Parameters.AddWithValue("$userId", userId);
								cmdCreate.Parameters.AddWithValue("$configId", configId);
								cmdCreate.Parameters.AddWithValue("$groupId", groupId);
								cmdCreate.Parameters.AddWithValue("$name", card.data.name ?? "");
								cmdCreate.Parameters.AddWithValue("$displayName", card.data.displayName ?? "");
								cmdCreate.Parameters.AddWithValue("$groupName", "");
								cmdCreate.Parameters.AddWithValue("$persona", card.data.persona ?? "");
								cmdCreate.Parameters.AddWithValue("$folderId", parentFolder.instanceId ?? "");
								cmdCreate.Parameters.AddWithValue("$folderSortPosition", folderSortPosition);
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
								return Backyard.Error.SQLCommandFailed;
							}

							// Write images to disk
							foreach (var image in images
								.Union(backgrounds)
								.Union(new ImageOutput[] { userPortrait })
								.Where(i => i.isDefined
									&& i.hasAsset
									&& File.Exists(i.imageUrl) == false))
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

							characterInstance = new CharacterInstance() {
								instanceId = characterId,
								configId = configId,
								groupId = groupId,
								displayName = card.data.displayName,
								name = card.data.name,
								creationDate = now,
								updateDate = now,
								folderId = parentFolder.instanceId,
								folderSortPosition = folderSortPosition,
							};

							transaction.Commit();
							return Backyard.Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();

							characterInstance = default(CharacterInstance);
							return Backyard.Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				characterInstance = default(CharacterInstance);
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				characterInstance = default(CharacterInstance);
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				characterInstance = default(CharacterInstance);
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error UpdateCharacter(FaradayCardV4 card, Backyard.Link linkInfo, out DateTime updateDate, out Backyard.Link.Image[] updatedImageLinks, UserData userInfo = null)
		{
			if (card == null || linkInfo == null || string.IsNullOrEmpty(linkInfo.characterId))
			{
				updateDate = default(DateTime);
				updatedImageLinks = null;
				return Backyard.Error.NotFound;
			}

			if (ConnectionEstablished == false)
			{
				updateDate = default(DateTime);
				updatedImageLinks = null;
				return Backyard.Error.NotConnected;
			}

			string characterId = linkInfo.characterId;
			bool bAllowUserPersona = userInfo != null;

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					string userId = null;
					string configId = null;
					string groupId = null;

					using (var cmdGetId = connection.CreateCommand())
					{
						cmdGetId.CommandText =
						@"
							SELECT 
								id
							FROM CharacterConfigVersion
							WHERE characterConfigId = $charId
						";
						cmdGetId.Parameters.AddWithValue("$charId", characterId);

						using (var reader = cmdGetId.ExecuteReader())
						{
							if (reader.Read() == false)
							{
								updateDate = default(DateTime);
								updatedImageLinks = null;
								return Backyard.Error.NotFound;
							}

							configId = reader.GetString(0);
						}
					}

					// Find (primary) group
					using (var cmdGetGroup = connection.CreateCommand())
					{
						cmdGetGroup.CommandText =
						@"
							SELECT 
								A.B, ( SELECT COUNT(*) FROM _CharacterConfigToGroupConfig WHERE ""B"" = A.B )
							FROM _CharacterConfigToGroupConfig AS A
							WHERE A.A = $charId
						";
						cmdGetGroup.Parameters.AddWithValue("$charId", characterId);

						using (var reader = cmdGetGroup.ExecuteReader())
						{
							if (reader.Read())
							{
								string instanceId = reader.GetString(0);
								int memberCount = reader.GetInt32(1);
								if (memberCount <= 2)
									groupId = instanceId;  // Primary group
							}
						}
					}

					// Get existing chats
					List<_Chat> chats;
					if (groupId != null)
					{
						var error = FetchChatInstances(connection, groupId, out chats);
						if (error != Backyard.Error.NoError)
						{
							updateDate = default(DateTime);
							updatedImageLinks = null;
							return error;
						}
					}
					else
						chats = new List<_Chat>();

					// Get existing images
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

								imageInstances.Add(new ImageInstance() {
									instanceId = instanceId,
									imageUrl = imageUrl,
									label = label,
									aspectRatio = aspectRatio,
									imageType = AssetFile.AssetType.Icon,
								});
							}
						}
					}

					// Get existing backgrounds
					ImageInstance[] existingBackgrounds;
					if (FetchChatBackgrounds(connection, groupId, out existingBackgrounds) == Backyard.Error.NoError)
						imageInstances.AddRange(existingBackgrounds);

					// Get existing user portrait
					ImageInstance existingUserPortrait = null;
					string userName, userPersona;
					if (FetchUserInfo(connection, groupId, out userId, out userName, out userPersona, out existingUserPortrait) && existingUserPortrait != null)
						imageInstances.Add(existingUserPortrait);

					// Compile list of images to update / insert
					ImageOutput[] imageOutput;
					Backyard.Link.Image[] imageLinks;
					PrepareImageUpdates(imageInstances, linkInfo.imageLinks, out imageOutput, out imageLinks);

					List<ImageOutput> images = imageOutput.Where(i => i.imageType == AssetFile.AssetType.Icon || i.imageType == AssetFile.AssetType.Expression).ToList();
					List<ImageOutput> backgrounds = imageOutput.Where(i => i.imageType == AssetFile.AssetType.Background).ToList();
					ImageOutput userPortrait = default(ImageOutput);
					if (bAllowUserPersona)
						userPortrait = imageOutput.FirstOrDefault(i => i.imageType == AssetFile.AssetType.UserIcon);

					// Write to database
					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							int updates = 0;
							int expectedUpdates = 0;

							// Create group (if one doesn't exist)
							if (groupId == null)
							{
								_Chat chat;
								if (CreateGroup(connection, characterId, out groupId, out chat, ref updates, ref expectedUpdates))
									chats.Add(chat);
							}

							// Create/update custom user
							if (bAllowUserPersona)
							{
								if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats))
									ToPartyNames(ref userInfo.persona, characterId, userId);

								string userConfigId = null;
								WriteUser(connection, groupId, userInfo, userPortrait, out userId, out userConfigId, out userPortrait, ref updates, ref expectedUpdates);
							}

							var now = DateTime.Now;
							long updatedAt = now.ToUnixTimeMilliseconds();

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
								cmdUpdate.Parameters.AddWithValue("$displayName", card.data.displayName ?? "");
								cmdUpdate.Parameters.AddWithValue("$name", card.data.name ?? "");
								cmdUpdate.Parameters.AddWithValue("$persona", card.data.persona ?? "");
								cmdUpdate.Parameters.AddWithValue("$timestamp", updatedAt);

								expectedUpdates += 1;
								updates += cmdUpdate.ExecuteNonQuery();
							}

							if (groupId != null)
							{
								// Update GroupConfig
								using (var cmdUpdateGroup = new SQLiteCommand(connection))
								{
									var sbCommand = new StringBuilder();
									sbCommand.AppendLine(
									@"
										UPDATE GroupConfig
										SET
											updatedAt = $timestamp,
											name = $groupName,
											isNSFW = $isNSFW
										WHERE id = $groupId;
									");
									cmdUpdateGroup.CommandText = sbCommand.ToString();
									cmdUpdateGroup.Parameters.AddWithValue("$groupId", groupId);
									cmdUpdateGroup.Parameters.AddWithValue("$groupName", "");
									cmdUpdateGroup.Parameters.AddWithValue("$isNSFW", card.data.isNSFW);
									cmdUpdateGroup.Parameters.AddWithValue("$timestamp", updatedAt);

									expectedUpdates += 1;
									updates += cmdUpdateGroup.ExecuteNonQuery();
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

										if (AppSettings.BackyardLink.WriteAuthorNote)
										{
											sbCommand.AppendLine(
											@"
												UPDATE Chat
												SET 
													authorNote = $authorNote
												WHERE groupConfigId = $groupId;
											");
										}
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

										if (AppSettings.BackyardLink.WriteAuthorNote)
										{
											sbCommand.AppendLine(
											@"
												UPDATE Chat
												SET 
													authorNote = $authorNote
												WHERE id = $chatId;
											");
										}
									}

									var staging = new ChatStaging() {
										system = card.data.system ?? "",
										scenario = card.data.scenario ?? "",
										greeting = card.data.greeting ?? "",
										example = card.data.example ?? "",
										grammar = card.data.grammar ?? "",
										authorNote = card.authorNote ?? "",
									};

									if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats))
										ToPartyNames(staging, characterId, userId);

									cmdChat.CommandText = sbCommand.ToString();
									cmdChat.Parameters.AddWithValue("$system", staging.system ?? "");
									cmdChat.Parameters.AddWithValue("$scenario", staging.scenario ?? "");
									cmdChat.Parameters.AddWithValue("$example", staging.example ?? "");
									cmdChat.Parameters.AddWithValue("$greeting", staging.greeting ?? "");
									cmdChat.Parameters.AddWithValue("$grammar", staging.grammar ?? "");
									cmdChat.Parameters.AddWithValue("$authorNote", card.authorNote ?? "");
									cmdChat.Parameters.AddWithValue("$timestamp", updatedAt);

									int nChats = cmdChat.ExecuteNonQuery();
									expectedUpdates += Math.Max(nChats, 1); // Expect at least one
									updates += nChats;
								}
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

								var sortedImageIds = new List<string>(images.Count);
								for (int i = 0; i < images.Count; ++i)
									sortedImageIds.Add(Cuid.NewCuid());
								sortedImageIds.Sort(); // Backyard bug ?

								// AppImage
								sbCommand.AppendLine(
								$@"
									INSERT INTO AppImage
										(id, createdAt, updatedAt, imageUrl, label, ""order"", aspectRatio)
									VALUES 
								");
								for (int i = 0; i < images.Count; ++i)
								{
									if (i > 0)
										sbCommand.Append(",\n");
									sbCommand.Append($"($imageId{i:000}, $timestamp, $timestamp, $imageUrl{i:000}, $label{i:000}, {i}, $aspectRatio{i:000})");
								}
								sbCommand.Append(";");

								// _AppImageToCharacterConfigVersion
								sbCommand.AppendLine(
								$@"
									INSERT INTO _AppImageToCharacterConfigVersion
										(A, B)
									VALUES 
								");

								for (int i = 0; i < images.Count; ++i)
								{
									if (i > 0)
										sbCommand.Append(",\n");
									sbCommand.Append($@"($imageId{i:000}, $imageConfigId{i:000})");

									cmdImage.Parameters.AddWithValue($"$imageId{i:000}", sortedImageIds[i]);
									cmdImage.Parameters.AddWithValue($"$imageUrl{i:000}", images[i].imageUrl);
									cmdImage.Parameters.AddWithValue($"$label{i:000}", images[i].label ?? "");
									cmdImage.Parameters.AddWithValue($"$aspectRatio{i:000}", images[i].aspectRatio);
									cmdImage.Parameters.AddWithValue($"$imageConfigId{i:000}", configId);
								}

								expectedUpdates += images.Count * 2;

								cmdImage.CommandText = sbCommand.ToString();
								cmdImage.Parameters.AddWithValue("$configId", configId);
								cmdImage.Parameters.AddWithValue("$timestamp", updatedAt);
								updates += cmdImage.ExecuteNonQuery();
							}

							WriteLorebook(connection, configId, card.data.loreItems, ref updates, ref expectedUpdates);

							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								updateDate = default(DateTime);
								updatedImageLinks = null;
								return Backyard.Error.SQLCommandFailed;
							}

							// Write images to disk
							foreach (var image in images
								.Union(backgrounds)
								.Union(new ImageOutput[] { userPortrait })
								.Where(i => i.isDefined
									&& i.hasAsset
									&& File.Exists(i.imageUrl) == false))
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


							updateDate = now;
							updatedImageLinks = imageLinks;

							transaction.Commit();
							return Backyard.Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();
						}
					}

					updateDate = default(DateTime);
					updatedImageLinks = null;
					return Backyard.Error.Unknown;
				}
			}
			catch (FileNotFoundException e)
			{
				updateDate = default(DateTime);
				updatedImageLinks = null;
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				updateDate = default(DateTime);
				updatedImageLinks = null;
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				updateDate = default(DateTime);
				updatedImageLinks = null;
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error ConfirmDeleteCharacters(CharacterInstance[] characterInstances, out ConfirmDeleteResult result)
		{
			if (ConnectionEstablished == false)
			{
				result = default(ConfirmDeleteResult);
				return Backyard.Error.NotConnected;
			}

			if (characterInstances == null || characterInstances.Length == 0)
			{
				result = default(ConfirmDeleteResult);
				return Backyard.Error.InvalidArgument;
			}

			HashSet<string> characterIds = new HashSet<string>(characterInstances.Select(c => c.instanceId));
			HashSet<string> configIds = new HashSet<string>(characterInstances.Select(c => c.configId));
			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					// Fetch character-group memberships
					var groupMembers = new Dictionary<string, HashSet<string>>(); // group, characterIds...
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
							}
						}
					}

					groupMembers = groupMembers
						.Where(kvp => kvp.Value.ContainsAnyIn(characterIds))
						.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

					// AppImage
					Dictionary<string, string> images = new Dictionary<string, string>();
					using (var cmdAppImage = connection.CreateCommand())
					{
						cmdAppImage.CommandText =
						@"
							SELECT 
								A.B, B.id, B.imageUrl
							FROM _AppImageToCharacterConfigVersion as A
							INNER JOIN AppImage as B ON B.id = A.A
						";

						using (var reader = cmdAppImage.ExecuteReader())
						{
							while (reader.Read())
							{
								string configId = reader.GetString(0);
								string imageId = reader.GetString(1);
								string imageUrl = reader.GetString(2);
								if (configIds.Contains(configId))
									images.Add(imageId, imageUrl);
							}
						}
					}

					List<string> backgroundUrls = new List<string>();

					if (BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds))
					{
						// Find backgrounds (chats)
						var groupIds = groupMembers.Keys.ToArray();
						using (var cmdBackgrounds = connection.CreateCommand())
						{
							var sbCommand = new StringBuilder();
							sbCommand.Append(
							@"
								SELECT imageUrl
								FROM BackgroundChatImage
								WHERE chatId IN (
									SELECT id
									FROM Chat
									WHERE groupConfigId IN (
								");

							for (int i = 0; i < groupIds.Length; ++i)
							{
								if (i > 0)
									sbCommand.Append(", ");
								sbCommand.AppendFormat("'{0}'", groupIds[i]);
							}
							sbCommand.AppendLine("));");
							cmdBackgrounds.CommandText = sbCommand.ToString();

							using (var reader = cmdBackgrounds.ExecuteReader())
							{
								while (reader.Read())
									backgroundUrls.Add(reader.GetString(0));
							}
						}
					}

					result = new ConfirmDeleteResult() {
						characterIds = groupMembers
							.SelectMany(kvp => kvp.Value)
							.Distinct()
							.Intersect(characterIds)
							.ToArray(),
						groupIds = groupMembers.Select(kvp => kvp.Key).ToArray(),
						imageIds = images.Keys.ToArray(),
						imageUrls = images.Values.Union(backgroundUrls).Distinct().ToArray(),
					};

					return Backyard.Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				result = default(ConfirmDeleteResult);
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				result = default(ConfirmDeleteResult);
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				result = default(ConfirmDeleteResult);
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error DeleteCharacters(string[] characterIds, string[] groupIds, string[] imageIds)
		{
			if (ConnectionEstablished == false)
				return Backyard.Error.NotConnected;

			if (characterIds == null || characterIds.Length == 0 || groupIds == null || groupIds.Length == 0)
				return Backyard.Error.InvalidArgument;

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					// Get config ids
					Dictionary<string, string> configIds = new Dictionary<string, string>();
					using (var cmdConfigs = connection.CreateCommand())
					{
						var sbCommand = new StringBuilder();
						sbCommand.Append(
						@"
							SELECT 
								id, characterConfigId
							FROM CharacterConfigVersion
							WHERE characterConfigId IN (");

						for (int i = 0; i < characterIds.Length; ++i)
						{
							if (i > 0)
								sbCommand.Append(", ");
							sbCommand.AppendFormat("'{0}'", characterIds[i]);
						}
						sbCommand.AppendLine(");");

						cmdConfigs.CommandText = sbCommand.ToString();
						using (var reader = cmdConfigs.ExecuteReader())
						{
							while (reader.Read())
							{
								string configId = reader.GetString(0);
								string characterId = reader.GetString(1);
								configIds.Add(configId, characterId);
							}
						}
					}

					// Find lore items
					List<string> loreItems = new List<string>();
					using (var cmdLore = connection.CreateCommand())
					{
						cmdLore.CommandText =
						@"
							SELECT 
								A, B
							FROM _AppCharacterLorebookItemToCharacterConfigVersion
						";

						using (var reader = cmdLore.ExecuteReader())
						{
							while (reader.Read())
							{
								string loreId = reader.GetString(0);
								string configId = reader.GetString(1);
								if (configIds.ContainsKey(configId))
									loreItems.Add(loreId);
							}
						}
					}

					// Delete from database
					int updates = 0;
					int expectedUpdates = 0;

					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							using (var cmdDelete = connection.CreateCommand())
							{
								var sbCommand = new StringBuilder();

								// Delete images
								if (imageIds.Length > 0)
								{
									sbCommand.AppendLine(
									@"
										DELETE FROM AppImage
										WHERE id IN (
									");
									for (int i = 0; i < imageIds.Length; ++i)
									{
										if (i > 0)
											sbCommand.Append(", ");
										sbCommand.AppendFormat("'{0}'", imageIds[i]);
									}
									sbCommand.AppendLine(");");
									expectedUpdates += imageIds.Length;
								}

								// Lore items
								if (loreItems.Count > 0)
								{
									sbCommand.AppendLine(
									@"
										DELETE FROM AppCharacterLorebookItem
										WHERE id IN (
									");
									for (int i = 0; i < loreItems.Count; ++i)
									{
										if (i > 0)
											sbCommand.Append(", ");
										sbCommand.AppendFormat("'{0}'", loreItems[i]);
									}
									sbCommand.AppendLine(");");
									expectedUpdates += loreItems.Count;
								}

								// Delete groups
								if (groupIds.Length > 0)
								{
									sbCommand.AppendLine(
									@"
										DELETE FROM GroupConfig
										WHERE id IN (
									");

									for (int i = 0; i < groupIds.Length; ++i)
									{
										if (i > 0)
											sbCommand.Append(", ");
										sbCommand.AppendFormat("'{0}'", groupIds[i]);
									}
									sbCommand.AppendLine(");");
									expectedUpdates += groupIds.Length;
								}

								if (characterIds.Length > 0)
								{
									// Delete characters
									sbCommand.AppendLine(
									@"
										DELETE FROM CharacterConfig
										WHERE id IN (
									");

									for (int i = 0; i < characterIds.Length; ++i)
									{
										if (i > 0)
											sbCommand.Append(", ");
										sbCommand.AppendFormat("'{0}'", characterIds[i]);
									}
									sbCommand.AppendLine(");");
									expectedUpdates += characterIds.Length;
								}

								cmdDelete.CommandText = sbCommand.ToString();
								updates += cmdDelete.ExecuteNonQuery();
							}

							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								return Backyard.Error.SQLCommandFailed;
							}

							transaction.Commit();
							return Backyard.Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();
							return Backyard.Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error DeleteOrphanedUsers()
		{
			string[] imageUrls;
			Backyard.Error error = DeleteOrphanedUsers(out imageUrls);
			if (error != Backyard.Error.NoError)
				return error;

			// Delete image files
			if (imageUrls != null && imageUrls.Length > 0)
			{
				try
				{
					foreach (var imageUrl in imageUrls)
					{
						if (File.Exists(imageUrl))
							File.Delete(imageUrl);
					}
				}
				catch
				{
				}
			}
			return Backyard.Error.NoError;
		}

		public Backyard.Error DeleteOrphanedUsers(out string[] imageUrls)
		{
			if (ConnectionEstablished == false)
			{
				imageUrls = null;
				return Backyard.Error.NotConnected;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					// Get user config ids
					List<string> configIds = new List<string>();
					List<string> characterIds = new List<string>();
					using (var cmdUsers = connection.CreateCommand())
					{
						cmdUsers.CommandText =
						@"
							SELECT 
								A.id, B.id
							FROM CharacterConfig as A
							INNER JOIN CharacterConfigVersion as B ON A.id = B.characterConfigId
							WHERE A.isUserControlled = 1
								AND A.isTemplateChar = 0
								AND NOT EXISTS (
									SELECT 1 from _CharacterConfigToGroupConfig C WHERE C.A = A.id
								);";

						using (var reader = cmdUsers.ExecuteReader())
						{
							while (reader.Read())
							{
								string instanceId = reader.GetString(0);
								string configId = reader.GetString(1);
								characterIds.Add(instanceId);
								configIds.Add(configId);
							}
						}
					}

					if (configIds.Count == 0)
					{
						imageUrls = null;
						return Backyard.Error.NoError;
					}

					// AppImage
					Dictionary<string, string> images = new Dictionary<string, string>();
					using (var cmdAppImage = connection.CreateCommand())
					{
						var sbCommand = new StringBuilder();
						sbCommand.AppendLine(
						@"
							SELECT 
								B.id, B.imageUrl
							FROM _AppImageToCharacterConfigVersion as A
							INNER JOIN AppImage as B ON B.id = A.A
							WHERE B.id IN (
								SELECT A
								FROM _AppImageToCharacterConfigVersion
								WHERE B IN (
						");

						for (int i = 0; i < configIds.Count; ++i)
						{
							if (i > 0)
								sbCommand.Append(", ");
							sbCommand.AppendFormat("'{0}'", configIds[i]);
						}
						sbCommand.AppendLine("));");
						cmdAppImage.CommandText = sbCommand.ToString();

						using (var reader = cmdAppImage.ExecuteReader())
						{
							while (reader.Read())
							{
								string imageId = reader.GetString(0);
								string imageUrl = reader.GetString(1);
								images.Add(imageId, imageUrl);
							}
						}
					}

					// Delete from database
					int updates = 0;
					int expectedUpdates = 0;
					var imageIds = images.Keys.ToArray();

					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							using (var cmdDelete = connection.CreateCommand())
							{
								var sbCommand = new StringBuilder();

								// Delete images
								if (imageIds.Length > 0)
								{
									sbCommand.AppendLine(
									@"
										DELETE FROM AppImage
										WHERE id IN (
									");
									for (int i = 0; i < imageIds.Length; ++i)
									{
										if (i > 0)
											sbCommand.Append(", ");
										sbCommand.AppendFormat("'{0}'", imageIds[i]);
									}
									sbCommand.AppendLine(");");
									expectedUpdates += imageIds.Length;
								}

								// Delete characters
								sbCommand.AppendLine(
								@"
									DELETE FROM CharacterConfig
									WHERE id IN (
								");

								for (int i = 0; i < characterIds.Count; ++i)
								{
									if (i > 0)
										sbCommand.Append(", ");
									sbCommand.AppendFormat("'{0}'", characterIds[i]);
								}
								sbCommand.AppendLine(");");
								cmdDelete.CommandText = sbCommand.ToString();

								expectedUpdates += characterIds.Count;
								updates += cmdDelete.ExecuteNonQuery();
							}

							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								imageUrls = null;
								return Backyard.Error.SQLCommandFailed;
							}

							transaction.Commit();
							imageUrls = images.Values.ToArray();
							return Backyard.Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();
							imageUrls = null;
							return Backyard.Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				imageUrls = null;
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				imageUrls = null;
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				imageUrls = null;
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

#endregion

#region Chat

		public Backyard.Error GetChatCounts(out Dictionary<string, ChatCount> counts)
		{
			if (ConnectionEstablished == false)
			{
				counts = null;
				return Backyard.Error.NotConnected;
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

					return Backyard.Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				counts = null;
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				counts = null;
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				counts = null;
				return Backyard.Error.Unknown;
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

		public Backyard.Error GetChats(string groupId, out ChatInstance[] chatInstances)
		{
			if (ConnectionEstablished == false)
			{
				chatInstances = null;
				return Backyard.Error.NotConnected;
			}

			if (string.IsNullOrEmpty(groupId))
			{
				chatInstances = null;
				return Backyard.Error.NotFound;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					List<_Character> characters;
					var error = FetchCharacters(connection, out characters);
					if (error != Backyard.Error.NoError)
					{
						chatInstances = null;
						return error;
					}

					List<_Character> groupMembers;
					error = FetchGroupMembers(connection, groupId, null, out groupMembers);
					if (error != Backyard.Error.NoError)
					{
						chatInstances = null;
						return error;
					}

					ImageInstance[] backgrounds;
					FetchChatBackgrounds(connection, groupId, out backgrounds);

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

								ChatBackground chatBackground = null;
								if (BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds))
								{
									int idxBackground = Array.FindIndex(backgrounds, b => b.associatedInstanceId == chatId);
									if (idxBackground != -1)
									{
										var bg = backgrounds[idxBackground];
										chatBackground = new ChatBackground() {
											instanceId = bg.instanceId,
											imageUrl = bg.imageUrl,
											width = bg.width,
											height = bg.height,
										};
									}
								}

								var staging = new ChatStaging() {
									system = system,
									scenario = scenario,
									greeting = greeting,
									example = example,
									grammar = grammar,
									authorNote = authorNote,
									background = chatBackground,
									pruneExampleChat = pruneExampleChat,
									ttsAutoPlay = ttsAutoPlay,
									ttsInputFilter = ttsInputFilter,
								};
								if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats))
									FromPartyNames(staging, groupId);

								chats.Add(new _Chat() {
									instanceId = chatId,
									creationDate = createdAt,
									updateDate = updatedAt,
									name = name,
									staging = staging,
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
					return Backyard.Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				chatInstances = null;
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				chatInstances = null;
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				chatInstances = null;
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error GetChat(string chatId, string groupId, out ChatInstance chatInstance)
		{
			if (ConnectionEstablished == false)
			{
				chatInstance = null;
				return Backyard.Error.NotConnected;
			}

			if (string.IsNullOrEmpty(chatId) || string.IsNullOrEmpty(groupId))
			{
				chatInstance = null;
				return Backyard.Error.InvalidArgument;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					List<_Character> characters;
					var error = FetchCharacters(connection, out characters);
					if (error != Backyard.Error.NoError)
					{
						chatInstance = null;
						return error;
					}
					List<_Character> groupMembers;
					error = FetchGroupMembers(connection, groupId, null, out groupMembers);
					if (error != Backyard.Error.NoError)
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
								return Backyard.Error.NotFound;
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
									authorNote = authorNote,
									pruneExampleChat = pruneExampleChat,
									ttsAutoPlay = ttsAutoPlay,
									ttsInputFilter = ttsInputFilter,
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
								}
							};
						}
					}

					// Get background image
					if (BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds))
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
									string aspectRatio = reader[2] as string ?? "";
									int width, height;
									ImageInstance.ParseAspectRatio(aspectRatio, out width, out height);

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
						return Backyard.Error.NotFound;
					return Backyard.Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				chatInstance = null;
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				chatInstance = null;
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				chatInstance = null;
				return Backyard.Error.Unknown;
			}
		}

		private ChatInstance FetchChatInstance(SQLiteConnection connection, _Chat chatInfo, List<_Character> characters, List<_Character> groupMembers)
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

		public Backyard.Error CreateNewChat(CreateChatArguments args, string groupId, out ChatInstance chatInstance)
		{
			if (ConnectionEstablished == false)
			{
				chatInstance = default(ChatInstance);
				return Backyard.Error.NotConnected;
			}

			if (args.history == null || args.history.messages == null || groupId == null)
			{
				chatInstance = default(ChatInstance);
				return Backyard.Error.InvalidArgument;
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
					bool hasBackground = BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds)
						&& args.staging != null
						&& args.staging.background != null
						&& string.IsNullOrEmpty(args.staging.background.imageUrl) == false
						&& File.Exists(args.staging.background.imageUrl);

					// Get default model
					string defaultModel;
					FetchDefaultModel(connection, out defaultModel);

					// Read default chat settings (latest)
					using (var cmdGroupInfo = connection.CreateCommand())
					{
						cmdGroupInfo.CommandText =
						@"
							SELECT 
								modelInstructions, context, greetingDialogue, customDialogue, grammar, 
								authorNote, canDeleteCustomDialogue, ttsAutoplay, ttsInputFilter,
								model, temperature, topP, minP, minPEnabled, topK, 
								repeatPenalty, repeatLastN, promptTemplate
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
								defaultStaging.authorNote = reader.GetString(5);
								defaultStaging.pruneExampleChat = reader.GetBoolean(6);
								defaultStaging.ttsAutoPlay = reader.GetBoolean(7);
								defaultStaging.ttsInputFilter = reader.GetString(8);
								defaultParameters.model = reader.GetString(9);
								defaultParameters.temperature = reader.GetDecimal(10);
								defaultParameters.topP = reader.GetDecimal(11);
								defaultParameters.minP = reader.GetDecimal(12);
								defaultParameters.minPEnabled = reader.GetBoolean(13);
								defaultParameters.topK = reader.GetInt32(14);
								defaultParameters.repeatPenalty = reader.GetDecimal(15);
								defaultParameters.repeatLastN = reader.GetInt32(16);
								defaultParameters.promptTemplate = reader[17] as string;
							}
						}
					}

					// Fetch group members
					List<_Character> groupMembers;
					var error = FetchGroupMembers(connection, groupId, args.history, out groupMembers);
					if (error != Backyard.Error.NoError)
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
								cmdCreateChat.Parameters.AddWithValue("$system", staging.system ?? "");
								cmdCreateChat.Parameters.AddWithValue("$scenario", staging.scenario ?? "");
								cmdCreateChat.Parameters.AddWithValue("$example", staging.example ?? "");
								cmdCreateChat.Parameters.AddWithValue("$greeting", greeting ?? "");
								cmdCreateChat.Parameters.AddWithValue("$grammar", staging.grammar ?? "");
								cmdCreateChat.Parameters.AddWithValue("$authorNote", staging.authorNote ?? "");
								cmdCreateChat.Parameters.AddWithValue("$pruneExample", staging.pruneExampleChat);
								cmdCreateChat.Parameters.AddWithValue("$ttsAutoPlay", staging.ttsAutoPlay);
								cmdCreateChat.Parameters.AddWithValue("$ttsInputFilter", staging.ttsInputFilter ?? "default");
								cmdCreateChat.Parameters.AddWithValue("$model", parameters.model ?? defaultModel ?? "");
								cmdCreateChat.Parameters.AddWithValue("$temperature", parameters.temperature);
								cmdCreateChat.Parameters.AddWithValue("$topP", parameters.topP);
								cmdCreateChat.Parameters.AddWithValue("$minP", parameters.minP);
								cmdCreateChat.Parameters.AddWithValue("$minPEnabled", parameters.minPEnabled);
								cmdCreateChat.Parameters.AddWithValue("$topK", parameters.topK);
								cmdCreateChat.Parameters.AddWithValue("$repeatPenalty", parameters.repeatPenalty);
								cmdCreateChat.Parameters.AddWithValue("$repeatLastN", parameters.repeatLastN);
								cmdCreateChat.Parameters.AddWithValue("$promptTemplate", parameters.promptTemplate);

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
								return Backyard.Error.SQLCommandFailed;
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
							return Backyard.Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();

							chatInstance = default(ChatInstance);
							return Backyard.Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				chatInstance = default(ChatInstance);
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				chatInstance = default(ChatInstance);
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				chatInstance = default(ChatInstance);
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error RenameChat(ChatInstance chatInstance, string newName)
		{
			if (ConnectionEstablished == false)
				return Backyard.Error.NotConnected;

			if (chatInstance == null || string.IsNullOrEmpty(chatInstance.instanceId))
				return Backyard.Error.InvalidArgument;

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
								return Backyard.Error.NotFound;
							}

							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								chatInstance = default(ChatInstance);
								return Backyard.Error.SQLCommandFailed;
							}

							chatInstance.updateDate = now;
							chatInstance.history.name = newName;

							transaction.Commit();
							return Backyard.Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();

							chatInstance = default(ChatInstance);
							return Backyard.Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error ConfirmDeleteChat(ChatInstance chatInstance, GroupInstance groupInstance, out int chatCount)
		{
			if (ConnectionEstablished == false)
			{
				chatCount = 0;
				return Backyard.Error.NotConnected;
			}

			if (chatInstance == null || string.IsNullOrEmpty(chatInstance.instanceId) || string.IsNullOrEmpty(groupInstance.instanceId))
			{
				chatCount = 0;
				return Backyard.Error.NotFound;
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
							return Backyard.Error.NotFound;

						return Backyard.Error.NoError;
					}
				}
			}
			catch (FileNotFoundException e)
			{
				chatCount = 0;
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				chatCount = 0;
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				chatCount = 0;
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error ConfirmChatExists(string chatId)
		{
			if (ConnectionEstablished == false)
				return Backyard.Error.NotConnected;

			if (string.IsNullOrEmpty(chatId))
				return Backyard.Error.InvalidArgument;

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
							return Backyard.Error.NotFound;
						return Backyard.Error.NoError;
					}
				}
			}
			catch (FileNotFoundException e)
			{
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error DeleteChat(ChatInstance chatInstance)
		{
			if (ConnectionEstablished == false)
				return Backyard.Error.NotConnected;

			if (chatInstance == null || string.IsNullOrEmpty(chatInstance.instanceId))
				return Backyard.Error.InvalidArgument;

			var error = ConfirmChatExists(chatInstance.instanceId);
			if (error != Backyard.Error.NoError)
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
							if (BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds))
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
								return Backyard.Error.SQLCommandFailed;
							}

							transaction.Commit();
							return Backyard.Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();

							chatInstance = default(ChatInstance);
							return Backyard.Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error PurgeChats(GroupInstance groupInstance)
		{
			if (ConnectionEstablished == false)
				return Backyard.Error.NotConnected;

			if (string.IsNullOrEmpty(groupInstance.instanceId))
				return Backyard.Error.InvalidArgument;

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
						return Backyard.Error.NotFound;
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
								return Backyard.Error.SQLCommandFailed;
							}

							transaction.Commit();
							return Backyard.Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();
							return Backyard.Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error UpdateChat(ChatInstance chatInstance, string groupId)
		{
			if (ConnectionEstablished == false)
				return Backyard.Error.NotConnected;

			if (chatInstance == null)
				return Backyard.Error.InvalidArgument;

			if (string.IsNullOrEmpty(chatInstance.instanceId) || string.IsNullOrEmpty(groupId))
				return Backyard.Error.InvalidArgument;

			var error = ConfirmChatExists(chatInstance.instanceId);
			if (error != Backyard.Error.NoError)
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
					if (error != Backyard.Error.NoError)
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
								return Backyard.Error.SQLCommandFailed;
							}

							chatInstance = new ChatInstance() {
								instanceId = chatId,
								history = new ChatHistory() {
									messages = lsMessages.ToArray(),
								},
							};

							transaction.Commit();
							return Backyard.Error.NoError;

						}
						catch (Exception e)
						{
							transaction.Rollback();

							chatInstance = default(ChatInstance);
							return Backyard.Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

		private List<ChatHistory.Message> WriteChatMessages(SQLiteConnection connection, string chatId, ChatHistory chatHistory, string[] speakerIds, ref int expectedUpdates, ref int updates)
		{
			List<ChatHistory.Message> lsMessages = new List<ChatHistory.Message>();
			if (chatHistory == null)
				return lsMessages; // No history

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

					var creationTime = message.creationDate;
					var updateTime = DateTimeExtensions.Max(message.updateDate, creationTime);

					cmdMessages.Parameters.AddWithValue($"$messageId{iMessage:000}", messageIds[iMessage]);
					cmdMessages.Parameters.AddWithValue($"$messageCreatedAt{iMessage:000}", creationTime.ToUnixTimeMilliseconds());
					cmdMessages.Parameters.AddWithValue($"$messageUpdatedAt{iMessage:000}", updateTime.ToUnixTimeMilliseconds());
					cmdMessages.Parameters.AddWithValue($"$charId{iMessage:000}", speakerIds[message.speaker]);

					lsMessages.Add(new ChatHistory.Message() {
						instanceId = messageIds[iMessage],
						activeSwipe = message.activeSwipe,
						creationDate = creationTime,
						updateDate = updateTime,
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

						DateTime creationTime = message.creationDate;
						DateTime activeTime = creationTime;
						if (i == message.activeSwipe)
							activeTime += TimeSpan.FromMilliseconds(5000);
						DateTime swipeTime = creationTime + TimeSpan.FromMilliseconds(i * 10);

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

		public Backyard.Error RepairChats(GroupInstance groupInstance, out int modified)
		{
			if (ConnectionEstablished == false)
			{
				modified = 0;
				return Backyard.Error.NotConnected;
			}

			if (string.IsNullOrEmpty(groupInstance.instanceId))
			{
				modified = 0;
				return Backyard.Error.InvalidArgument;
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
							return Backyard.Error.NotFound;
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
						return Backyard.Error.NoError;
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
								return Backyard.Error.SQLCommandFailed;
							}

							transaction.Commit();
							modified = repairs.DistinctBy(r => r.chatId).Count();
							return Backyard.Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();
							modified = 0;
							return Backyard.Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				modified = 0;
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				modified = 0;
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				modified = 0;
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error UpdateChatParameters(string chatId, ChatParameters parameters, ChatStaging staging)
		{
			if (ConnectionEstablished == false)
				return Backyard.Error.NotConnected;

			if (parameters == null && staging == null)
				return Backyard.Error.InvalidArgument;

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					int updates = 0;
					int expectedUpdates = 0;

					DateTime now = DateTime.Now;
					long updatedAt = now.ToUnixTimeMilliseconds();

					string defaultModel;
					FetchDefaultModel(connection, out defaultModel);

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
									SET ");

								if (staging != null)
								{
									sbCommand.AppendLine(
									@"
										context = $scenario,
										customDialogue = $example,
										modelInstructions = $system,
										grammar = $grammar,
										greetingDialogue = $greeting,
										canDeleteCustomDialogue = $pruneExample");
								}
								if (parameters != null)
								{
									if (staging != null)
										sbCommand.Append(",");
									sbCommand.AppendLine(
									@"
										model = $model, 
										temperature = $temperature,
										topP = $topP,
										minP = $minP,
										minPEnabled = $minPEnabled,
										topK = $topK,
										repeatPenalty = $repeatPenalty,
										repeatLastN = $repeatLastN,
										promptTemplate = $promptTemplate");
								}
								sbCommand.AppendLine(
								@"
									WHERE id = $chatId;
								");

								cmdUpdateChat.CommandText = sbCommand.ToString();
								cmdUpdateChat.Parameters.AddWithValue("$chatId", chatId);
								cmdUpdateChat.Parameters.AddWithValue("$timestamp", updatedAt);
								if (staging != null)
								{
									cmdUpdateChat.Parameters.AddWithValue("$system", Utility.FirstNonEmpty(staging.system, FaradayCardV4.OriginalModelInstructionsByFormat[0]));
									cmdUpdateChat.Parameters.AddWithValue("$scenario", staging.scenario ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$greeting", staging.greeting ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$example", staging.example ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$grammar", staging.grammar ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$pruneExample", staging.pruneExampleChat);
									// staging.authorNote
									// staging.ttsAutoPlay
									// staging.ttsInputFilter
								}
								if (parameters != null)
								{
									cmdUpdateChat.Parameters.AddWithValue("$model", parameters.model ?? defaultModel ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$temperature", parameters.temperature);
									cmdUpdateChat.Parameters.AddWithValue("$topP", parameters.topP);
									cmdUpdateChat.Parameters.AddWithValue("$minP", parameters.minP);
									cmdUpdateChat.Parameters.AddWithValue("$minPEnabled", parameters.minPEnabled);
									cmdUpdateChat.Parameters.AddWithValue("$topK", parameters.topK);
									cmdUpdateChat.Parameters.AddWithValue("$repeatPenalty", parameters.repeatPenalty);
									cmdUpdateChat.Parameters.AddWithValue("$repeatLastN", parameters.repeatLastN);
									cmdUpdateChat.Parameters.AddWithValue("$promptTemplate", parameters.promptTemplate);
								}

								expectedUpdates += 1;
								updates += cmdUpdateChat.ExecuteNonQuery();
							}

							if (updates == 0)
							{
								transaction.Rollback(); // Superfluous. jic.
								return Backyard.Error.NotFound;
							}

							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								return Backyard.Error.SQLCommandFailed;
							}

							transaction.Commit();
							return Backyard.Error.NoError;

						}
						catch (Exception e)
						{
							transaction.Rollback();
							return Backyard.Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error UpdateChatParameters(string[] chatIds, ChatParameters parameters, ChatStaging staging)
		{
			if (ConnectionEstablished == false)
				return Backyard.Error.NotConnected;

			if (parameters == null && staging == null)
				return Backyard.Error.InvalidArgument;

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					int updates = 0;
					int expectedUpdates = 0;

					DateTime now = DateTime.Now;
					long updatedAt = now.ToUnixTimeMilliseconds();

					string defaultModel;
					FetchDefaultModel(connection, out defaultModel);

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
									SET ");

								if (staging != null)
								{
									sbCommand.AppendLine(
									@"  
										context = $scenario,
										customDialogue = $example,
										modelInstructions = $system,
										grammar = $grammar,
										greetingDialogue = $greeting,
										canDeleteCustomDialogue = $pruneExample");
								}
								if (parameters != null)
								{
									if (staging != null)
										sbCommand.Append(",");
									sbCommand.AppendLine(
									@"
										model = $model, 
										temperature = $temperature,
										topP = $topP,
										minP = $minP,
										minPEnabled = $minPEnabled,
										topK = $topK,
										repeatPenalty = $repeatPenalty,
										repeatLastN = $repeatLastN,
										promptTemplate = $promptTemplate");
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
								if (staging != null)
								{
									cmdUpdateChat.Parameters.AddWithValue("$system", Utility.FirstNonEmpty(staging.system, FaradayCardV4.OriginalModelInstructionsByFormat[0]));
									cmdUpdateChat.Parameters.AddWithValue("$scenario", staging.scenario ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$greeting", staging.greeting ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$example", staging.example ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$grammar", staging.grammar ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$pruneExample", staging.pruneExampleChat);
									// staging.authorNote
									// staging.ttsAutoPlay
									// staging.ttsInputFilter
								}
								if (parameters != null)
								{
									cmdUpdateChat.Parameters.AddWithValue("$model", parameters.model ?? defaultModel ?? "");
									cmdUpdateChat.Parameters.AddWithValue("$temperature", parameters.temperature);
									cmdUpdateChat.Parameters.AddWithValue("$topP", parameters.topP);
									cmdUpdateChat.Parameters.AddWithValue("$minP", parameters.minP);
									cmdUpdateChat.Parameters.AddWithValue("$minPEnabled", parameters.minPEnabled);
									cmdUpdateChat.Parameters.AddWithValue("$topK", parameters.topK);
									cmdUpdateChat.Parameters.AddWithValue("$repeatPenalty", parameters.repeatPenalty);
									cmdUpdateChat.Parameters.AddWithValue("$repeatLastN", parameters.repeatLastN);
									cmdUpdateChat.Parameters.AddWithValue("$promptTemplate", parameters.promptTemplate);
								}

								expectedUpdates += chatIds.Length;
								updates += cmdUpdateChat.ExecuteNonQuery();
							}

							if (updates == 0)
							{
								transaction.Rollback(); // Superfluous. jic.
								return Backyard.Error.NotFound;
							}

							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								return Backyard.Error.SQLCommandFailed;
							}

							transaction.Commit();
							return Backyard.Error.NoError;

						}
						catch (Exception e)
						{
							transaction.Rollback();
							return Backyard.Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error UpdateChatBackground(string[] chatIds, string imageUrl, int width, int height)
		{
			if (ConnectionEstablished == false)
				return Backyard.Error.NotConnected;

			if (BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds) == false)
				return Backyard.Error.UnsupportedFeature;

			if (chatIds == null)
				return Backyard.Error.InvalidArgument;

			if (string.IsNullOrEmpty(imageUrl) == false && (width <= 0 || width <= 0))
				return Backyard.Error.InvalidArgument;

			if (chatIds.Length == 0)
				return Backyard.Error.NoError;

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
							// Clear chat backgrounds
							using (var cmdDelete = new SQLiteCommand(connection))
							{
								var sbCommand = new StringBuilder();

								sbCommand.AppendLine(
								@"
									DELETE FROM BackgroundChatImage
									WHERE chatId IN (");

								for (int i = 0; i < chatIds.Length; ++i)
								{
									if (i > 0)
										sbCommand.Append(", ");
									sbCommand.AppendFormat("'{0}'", chatIds[i]);
								}
								sbCommand.AppendLine(");");
								cmdDelete.CommandText = sbCommand.ToString();

								int nDeletes = cmdDelete.ExecuteNonQuery();
								expectedUpdates += nDeletes;
								updates += nDeletes;
							}

							// Set chat backgrounds
							if (string.IsNullOrEmpty(imageUrl) == false)
							{
								using (var cmdBackground = new SQLiteCommand(connection))
								{
									var sbCommand = new StringBuilder();

									for (int i = 0; i < chatIds.Length; ++i)
									{
										// Chat
										sbCommand.AppendLine(
										$@"
											INSERT INTO BackgroundChatImage
												(id, imageUrl, aspectRatio, chatId)
											VALUES 
												($backgroundId{i:000}, $imageUrl, $aspectRatio, $chatId{i:000});
										");

										cmdBackground.CommandText = sbCommand.ToString();
										cmdBackground.Parameters.AddWithValue($"$backgroundId{i:000}", Cuid.NewCuid());
										cmdBackground.Parameters.AddWithValue($"$chatId{i:000}", chatIds[i]);

										expectedUpdates += 1;
									}
									cmdBackground.Parameters.AddWithValue("$imageUrl", imageUrl ?? "");
									cmdBackground.Parameters.AddWithValue("$aspectRatio", string.Format("{0}/{1}", width, height));
									updates += cmdBackground.ExecuteNonQuery();
								}
							}

							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								return Backyard.Error.SQLCommandFailed;
							}

							transaction.Commit();
							return Backyard.Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();
							return Backyard.Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
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

		public string ToFolderUrl(string label)
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

		public Backyard.Error CreateNewFolder(string folderName, out FolderInstance folderInstance)
		{
			if (ConnectionEstablished == false)
			{
				folderInstance = default(FolderInstance);
				return Backyard.Error.NotConnected;
			}

			if (string.IsNullOrWhiteSpace(folderName))
			{
				folderInstance = default(FolderInstance);
				return Backyard.Error.InvalidArgument;
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
						return Backyard.Error.NotFound;
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
								return Backyard.Error.SQLCommandFailed;
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
							return Backyard.Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();

							folderInstance = default(FolderInstance);
							return Backyard.Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				folderInstance = default(FolderInstance);
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				folderInstance = default(FolderInstance);
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				folderInstance = default(FolderInstance);
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

#endregion

#region Utilities
		private string MakeLoreSortPosition(int index, int maxIndex, int hash)
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

		private enum SortPosition { Before, After };
		private string MakeFolderSortPosition(SortPosition sortPos, string lastOrderKey)
		{
			RandomNoise rng = new RandomNoise();
			const string key = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
			char[] p = new char[6];
			for (int i = 0; i < p.Length; ++i)
				p[i] = key[rng.Int(52)];
			string prefix = new string(p);

			if (string.IsNullOrEmpty(lastOrderKey) == false)
			{
				if (sortPos == SortPosition.Before)
				{
					// Decrement last character
					lastOrderKey = string.Concat(lastOrderKey.Substring(0, lastOrderKey.Length - 1), (char)(lastOrderKey[lastOrderKey.Length - 1] - 1));
				}
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
			public AssetFile.AssetType imageType;

			public string aspectRatio 
			{ 
				get
				{
					if (width > 0 && height > 0)
						return string.Format("{0}/{1}", width, height);
					return "";
				} 
			}

			public bool isDefined { get { return imageType != AssetFile.AssetType.Undefined; } }
			public bool hasAsset { get { return !data.isEmpty; } }
		}

		private bool PrepareImageUpdates(List<ImageInstance> imageInstances, Backyard.Link.Image[] imageLinks, out ImageOutput[] imagesToSave, out Backyard.Link.Image[] newImageLinks)
		{
			// Prepare image information
			string destPath = Path.Combine(AppSettings.BackyardLink.Location, "images");

			var assets = (AssetCollection)Current.Card.assets.Clone();

			List<ImageOutput> results = new List<ImageOutput>();
			List<Backyard.Link.Image> lsImageLinks = new List<Backyard.Link.Image>();

			// Main portrait
			ImageRef portraitImage = Current.Card.portraitImage;
			ImageInstance existingPortrait = null;
			int idxPortraitLink = -1;
			string portraitUID = null;

			var mainPortraitAsset = assets.GetMainPortraitOverride();
			if (portraitImage == null && mainPortraitAsset == null)
				mainPortraitAsset = assets.GetPortraitAsset(); // Use first portrait asset

			if (mainPortraitAsset != null) // Has embedded asset
			{
				if (imageLinks != null)
				{
					idxPortraitLink = Array.FindIndex(imageLinks, l => l.uid == mainPortraitAsset.uid);
					if (idxPortraitLink != -1)
					{
						existingPortrait = imageInstances.FirstOrDefault(kvp => string.Compare(Path.GetFileName(kvp.imageUrl), imageLinks[idxPortraitLink].filename, StringComparison.InvariantCultureIgnoreCase) == 0);
						portraitUID = mainPortraitAsset.uid;
						assets.Remove(mainPortraitAsset);
					}
				}
			}
			
			if (portraitUID == null)
			{
				if (portraitImage != null)
					portraitUID = portraitImage.uid;
				else
					portraitUID = "__default";

				if (imageLinks != null)
				{
					idxPortraitLink = Array.FindIndex(imageLinks, l => l.uid == portraitUID);
					if (idxPortraitLink != -1)
					{
						existingPortrait = imageInstances.FirstOrDefault(kvp => string.Compare(Path.GetFileName(kvp.imageUrl), imageLinks[idxPortraitLink].filename, StringComparison.InvariantCultureIgnoreCase) == 0);
					}
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
					imageType = AssetFile.AssetType.Icon,
				});

				lsImageLinks.Add(imageLinks[idxPortraitLink]);
			}
			else if (mainPortraitAsset != null)
			{
				var filename = Utility.CreateRandomFilename(mainPortraitAsset.ext);

				results.Add(new ImageOutput() {
					instanceId = Cuid.NewCuid(),
					imageUrl = Path.Combine(destPath, filename),
					data = mainPortraitAsset.data,
					imageType = AssetFile.AssetType.Icon,
				});
				lsImageLinks.Add(new Backyard.Link.Image() {
					uid = mainPortraitAsset.uid,
					filename = filename,
				});
				assets.Remove(mainPortraitAsset);
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
						imageType = AssetFile.AssetType.Icon,
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
						imageType = AssetFile.AssetType.Icon,
					};
				}

				results.Add(output);
				lsImageLinks.Add(new Backyard.Link.Image() {
					uid = portraitUID,
					filename = filename,
				});
			}

			// Embedded assets
			foreach (var asset in assets
				.Where(a => a.isEmbeddedAsset 
					&& a.data.length > 0
					&& (a.assetType == AssetFile.AssetType.Icon 
						|| a.assetType == AssetFile.AssetType.Expression
						|| a.assetType == AssetFile.AssetType.Background
						|| (a.assetType == AssetFile.AssetType.UserIcon))
						))
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
								imageType = existingInstance.imageType,
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
						imageType = asset.assetType,
					};
						
					results.Add(output);
					lsImageLinks.Add(new Backyard.Link.Image() {
						uid = asset.uid,
						filename = filename,
					});
				}
			}

			// Add images with no known links (added via Backyard; kept to prevent data loss)
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
						imageType = i.imageType,
					}));
			}

			imagesToSave = results.ToArray();
			newImageLinks = lsImageLinks.Count > 0 ? lsImageLinks.ToArray() : null;

			LimitPortraitCount(ref imagesToSave, ref newImageLinks);

			return imagesToSave.ContainsAny(i => i.hasAsset);
		}

		private bool PrepareImages(ImageInput[] imageInput, out ImageOutput[] imagesToSave, out Backyard.Link.Image[] newImageLinks)
		{
			if (imageInput == null || imageInput.Length == 0)
			{
				imagesToSave = null;
				newImageLinks = null;
				return false;
			}

			// Prepare image information
			string destPath = Path.Combine(AppSettings.BackyardLink.Location, "images");

			List<ImageOutput> results = new List<ImageOutput>();
			List<Backyard.Link.Image> lsImageLinks = new List<Backyard.Link.Image>();
			
			foreach (var input in imageInput)
			{
				if (input.asset != null && input.asset.data.isEmpty == false) // Data buffer
				{
					// Measure dimensions
					int imageWidth;
					int imageHeight;
					if (Utility.GetImageDimensions(input.asset.data.bytes, out imageWidth, out imageHeight) == false)
					{
						imageWidth = 1024;
						imageHeight = 1024;
					}	
					
					string filename = Utility.CreateRandomFilename(input.fileExt);
					results.Add(new ImageOutput() {
						instanceId = Cuid.NewCuid(),
						imageUrl = Path.Combine(destPath, filename),
						data = input.asset.data,
						width = imageWidth,
						height = imageHeight,
						imageType = input.asset.assetType,
					});

					lsImageLinks.Add(new Backyard.Link.Image() {
						uid = input.asset.uid,
						filename = filename,
					});
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
						imageType = AssetFile.AssetType.Icon,
					});

					lsImageLinks.Add(new Backyard.Link.Image() {
						uid = input.image.uid,
						filename = filename,
					});
				}
			}

			imagesToSave = results.ToArray();
			newImageLinks = lsImageLinks.Count > 0 ? lsImageLinks.ToArray() : null;
			LimitPortraitCount(ref imagesToSave, ref newImageLinks);
			return imagesToSave.ContainsAny(i => i.hasAsset);
		}

		private void LimitPortraitCount(ref ImageOutput[] imagesToSave, ref Backyard.Link.Image[] newImageLinks)
		{
			if (imagesToSave == null || imagesToSave.Length < ImageInstance.MaxImages)
				return; // Do nothing

			var keepPortraits = imagesToSave.Where(i => i.imageType == AssetFile.AssetType.Icon).Take(ImageInstance.MaxImages);
			var nonPortraits = imagesToSave.Where(i => i.isDefined && i.imageType != AssetFile.AssetType.Icon);
			var excludeLinks = new HashSet<string>(
				imagesToSave
					.Except(keepPortraits.Union(nonPortraits))
					.Select(i => Path.GetFileName(i.imageUrl))
				);

			if (excludeLinks.Count == 0)
				return; // Exclude nothing

			imagesToSave = keepPortraits.Union(nonPortraits).ToArray();
			if (newImageLinks != null)
				newImageLinks = newImageLinks.Except(newImageLinks.Where(l => excludeLinks.Contains(l.filename))).ToArray();

		}

		private Backyard.Error FetchCharacters(SQLiteConnection connection, out List<_Character> characters)
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
						return Backyard.Error.NotFound;
					return Backyard.Error.NoError;
				}
			}
			catch (SQLiteException e)
			{
				characters = null;
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				characters = null;
				return Backyard.Error.Unknown;
			}
		}

		private Backyard.Error FetchGroupMembers(SQLiteConnection connection, string groupId, ChatHistory chatHistory, out List<_Character> members)
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

					if (members.Count(c => c.isUser) == 0 || members.Count(c => c.isUser == false) == 0)
					{
						// Groups must contain at least one user and one non-user
						return Backyard.Error.Unknown;
					}

					// Validate message indices
					if (chatHistory != null && chatHistory.messages != null)
					{
						for (int i = 0; i < chatHistory.messages.Length; ++i)
						{
							if (chatHistory.messages[i].speaker < 0 || chatHistory.messages[i].speaker >= members.Count)
								return Backyard.Error.Unknown;
						}
					}

					// Place user first
					var user = members.First(c => c.isUser);
					members.Remove(user);
					members.Insert(0, user);
					return Backyard.Error.NoError;
				}
			}
			catch (SQLiteException e)
			{
				members = null;
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				members = null;
				return Backyard.Error.Unknown;
			}
		}

		private Backyard.Error FetchChatBackgrounds(SQLiteConnection connection, string groupId, out ImageInstance[] backgrounds)
		{
			if (BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds) == false)
			{
				backgrounds = new ImageInstance[0];
				return Backyard.Error.NoError;
			}

			if (string.IsNullOrEmpty(groupId))
			{
				backgrounds = new ImageInstance[0];
				return Backyard.Error.InvalidArgument;
			}

			try
			{
				using (var cmdBackgrounds = connection.CreateCommand())
				{
					cmdBackgrounds.CommandText =
					@"
						SELECT 
							id, chatId, imageUrl, aspectRatio
						FROM BackgroundChatImage
						WHERE chatId IN (
							SELECT id
							FROM Chat
							WHERE groupConfigId = $groupId
						);
					";
					cmdBackgrounds.Parameters.AddWithValue("$groupId", groupId);

					var lsBackgrounds = new List<ImageInstance>();
					using (var reader = cmdBackgrounds.ExecuteReader())
					{
						while (reader.Read())
						{
							string id = reader.GetString(0);
							string chatId = reader.GetString(1);
							string imageUrl = reader.GetString(2);
							string aspectRatio = reader[3] as string ?? "";

							lsBackgrounds.Add(new ImageInstance() {
								instanceId = id,
								associatedInstanceId = chatId,
								imageUrl = imageUrl,
								aspectRatio = aspectRatio,
								imageType = AssetFile.AssetType.Background,
							});
						}

						backgrounds = lsBackgrounds.ToArray();
						return Backyard.Error.NoError;
					}
				}
			}
			catch (SQLiteException e)
			{
				backgrounds = new ImageInstance[0];
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				backgrounds = new ImageInstance[0];
				return Backyard.Error.Unknown;
			}
		}

		private void WriteLorebook(SQLiteConnection connection, string configId, FaradayCardV1.LoreBookEntry[] loreItems, ref int updates, ref int expectedUpdates)
		{
			int hash = configId.GetHashCode();
			long updatedAt = DateTime.Now.ToUnixTimeMilliseconds();

			// Get existing lore items
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

			// Lorebook
			if (loreItems != null && loreItems.Length > 0 && loreItems.Length == existingLoreItems.Count)
			{
				// If there's an identical number of lore items in the DB already, only update the values
				using (var cmdLore = new SQLiteCommand(connection))
				{
					var sbCommand = new StringBuilder();
					for (int i = 0; i < loreItems.Length; ++i)
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
						cmdLore.Parameters.AddWithValue($"$key{i:000}", loreItems[i].key);
						cmdLore.Parameters.AddWithValue($"$value{i:000}", loreItems[i].value);
						cmdLore.Parameters.AddWithValue($"$order{i:000}", MakeLoreSortPosition(i, loreItems.Length - 1, hash));
					}
					cmdLore.CommandText = sbCommand.ToString();
					cmdLore.Parameters.AddWithValue("$timestamp", updatedAt);

					expectedUpdates += loreItems.Length;
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
				if (loreItems != null && loreItems.Length > 0)
				{
					// Generate unique IDs
					var uids = new string[loreItems.Length];
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

						for (int i = 0; i < loreItems.Length; ++i)
						{
							if (i > 0)
								sbCommand.Append(",\n");
							sbCommand.Append($"($id{i:000}, $timestamp, $timestamp, $order{i:000}, $key{i:000}, $value{i:000})");

							cmdInsertLore.Parameters.AddWithValue($"$id{i:000}", uids[i]);
							cmdInsertLore.Parameters.AddWithValue($"$key{i:000}", loreItems[i].key);
							cmdInsertLore.Parameters.AddWithValue($"$value{i:000}", loreItems[i].value);
							cmdInsertLore.Parameters.AddWithValue($"$order{i:000}", MakeLoreSortPosition(i, loreItems.Length - 1, hash));
						}
						sbCommand.Append(";");
						cmdInsertLore.CommandText = sbCommand.ToString();
						cmdInsertLore.Parameters.AddWithValue("$timestamp", updatedAt);

						expectedUpdates += loreItems.Length;
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
		}

		private Backyard.Error FetchChatInstances(SQLiteConnection connection, string groupId, out List<_Chat> chats)
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

				return Backyard.Error.NoError;
			}
			catch (SQLiteException e)
			{
				chats = null;
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				chats = null;
				return Backyard.Error.Unknown;
			}
		}

		public BackupData.Chat[] GatherChats(FaradayCardV4 card, Generator.Output output, ImageInput[] images)
		{
			var lsChats = new List<BackupData.Chat>();

			DateTime timestamp = DateTime.Now;

			string backgroundName = null;
			if (images != null)
			{
				backgroundName = images
					.Where(i => i.asset != null && i.asset.assetType == AssetFile.AssetType.Background)
					.Select(i => i.asset.name)
					.FirstOrDefault();
			}

			var parameters = AppSettings.BackyardSettings.UserSettings;

			// Primary greeting
			lsChats.Add(new BackupData.Chat() {
				name = "Primary greeting",
				creationDate = timestamp,
				updateDate = timestamp,
				backgroundName = backgroundName,
				staging = new ChatStaging() {
					system = card.data.system,
					scenario = card.data.scenario,
					greeting = card.data.greeting,
					example = card.data.example,
					grammar = card.data.grammar,
				},
				parameters = parameters,
				history = new ChatHistory(),
			});

			// Alternate greetings
			var altGreetings = output.alternativeGreetings;
			for (int i = 0; i < altGreetings.Length; ++i)
			{
				var altGreeting = altGreetings[i].ToFaradayGreeting();
				timestamp -= TimeSpan.FromMilliseconds(10);

				lsChats.Add(new BackupData.Chat() {
					name = string.Format("Alt. greeting #{0}", i + 1),
					creationDate = timestamp,
					updateDate = timestamp,
					staging = new ChatStaging() {
						system = card.data.system,
						scenario = card.data.scenario,
						greeting = altGreeting,
						example = card.data.example,
						grammar = card.data.grammar,
					},
					parameters = parameters,
					backgroundName = backgroundName,
					history = new ChatHistory(),
				});
			}

			return lsChats.ToArray();
		}

		private struct _ImageInfo
		{
			public string instanceId;
			public string imageUrl;
			public string filename;
		}

		public Backyard.Error RepairImages(out int modified, out int skipped)
		{
			if (ConnectionEstablished == false)
			{
				modified = 0;
				skipped = 0;
				return Backyard.Error.NotConnected;
			}

			var imagesFolder = Path.Combine(AppSettings.BackyardLink.Location, "images");
			if (Directory.Exists(imagesFolder) == false)
			{
				modified = 0;
				skipped = 0;
				return Backyard.Error.NotFound;
			}

			var foundImages = new HashSet<string>(
				Utility.FindFilesInFolder(imagesFolder)
					.Select(fn => Path.GetFileName(fn))
					.Where(fn => Utility.IsSupportedImageFilename(fn))
				);

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					// AppImage
					var characterImages = new List<_ImageInfo>();
					using (var cmdGetImages = connection.CreateCommand())
					{
						cmdGetImages.CommandText =
						@"
							SELECT id, imageUrl FROM AppImage
							WHERE id IN (
								SELECT A
								FROM _AppImageToCharacterConfigVersion)";

						using (var reader = cmdGetImages.ExecuteReader())
						{
							while (reader.Read())
							{
								string id = reader.GetString(0);
								string imageUrl = reader.GetString(1);

								characterImages.Add(new _ImageInfo() {
									instanceId = id,
									filename = Path.GetFileName(imageUrl),
									imageUrl = imageUrl,
								});
							}
						}
					}

					// BackgroundChatImage
					var backgroundImages = new List<_ImageInfo>();
					if (BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds))
					{
						using (var cmdGetBackgrounds = connection.CreateCommand())
						{
							cmdGetBackgrounds.CommandText =
							@"
							SELECT id, imageUrl, chatId FROM BackgroundChatImage
							WHERE chatId IN (
								SELECT id
								FROM Chat)";

							using (var reader = cmdGetBackgrounds.ExecuteReader())
							{
								while (reader.Read())
								{
									string id = reader.GetString(0);
									string imageUrl = reader.GetString(1);

									backgroundImages.Add(new _ImageInfo() {
										instanceId = id,
										filename = Path.GetFileName(imageUrl),
										imageUrl = imageUrl,
									});
								}
							}
						}
					}

					var modifiedCharacterImages = characterImages
						.Where(i => foundImages.Contains(i.filename, StringComparer.OrdinalIgnoreCase)
							&& File.Exists(i.imageUrl) == false)
						.ToList();

					var modifiedBackgroundImages = backgroundImages
						.Where(i => foundImages.Contains(i.filename, StringComparer.OrdinalIgnoreCase)
							&& File.Exists(i.imageUrl) == false)
						.ToList();

					modified = modifiedCharacterImages.Count + modifiedBackgroundImages.Count;
					
					var unknownImages = characterImages
						.Where(i => !foundImages.Contains(i.filename, StringComparer.OrdinalIgnoreCase))
						.Union(
							backgroundImages.Where(i => !foundImages.Contains(i.filename, StringComparer.OrdinalIgnoreCase))
						)
						.ToList();

					skipped = unknownImages.Count;
					if (modified == 0)
						return Backyard.Error.NoError; // No changes

					// Write to database
					int updates = 0;
					int expectedUpdates = 0;

					using (var transaction = connection.BeginTransaction())
					{
						try
						{
							// Character portraits
							if (modifiedCharacterImages.Count > 0)
							{
								using (var cmdUpdateAppImage = connection.CreateCommand())
								{
									var sbCommand = new StringBuilder();
									sbCommand.AppendLine(
									$@"
										WITH updated(id, imageUrl) AS (VALUES
									");

									for (int i = 0; i < modifiedCharacterImages.Count; ++i)
									{
										if (i > 0)
											sbCommand.Append(",\n");
										string newFilename = Path.Combine(imagesFolder, modifiedCharacterImages[i].filename);
										sbCommand.Append($"($id{i:000}, $imageUrl{i:000})");

										cmdUpdateAppImage.Parameters.AddWithValue($"$id{i:000}", modifiedCharacterImages[i].instanceId);
										cmdUpdateAppImage.Parameters.AddWithValue($"$imageUrl{i:000}", newFilename);
										expectedUpdates += 1;
									}
									sbCommand.AppendLine(
									$@"
									) UPDATE AppImage
										SET
											imageUrl = updated.imageUrl
										FROM updated
										WHERE (AppImage.id = updated.id);");
									cmdUpdateAppImage.CommandText = sbCommand.ToString();
									updates += cmdUpdateAppImage.ExecuteNonQuery();
								}
							}

							// Background images
							if (BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds) && modifiedBackgroundImages.Count > 0)
							{
								using (var cmdUpdateBackgrounds = connection.CreateCommand())
								{
									var sbCommand = new StringBuilder();
									sbCommand.AppendLine(
									$@"
										WITH updated(id, imageUrl) AS (VALUES
									");

									for (int i = 0; i < modifiedBackgroundImages.Count; ++i)
									{
										if (i > 0)
											sbCommand.Append(",\n");
										string newFilename = Path.Combine(imagesFolder, modifiedBackgroundImages[i].filename);
										sbCommand.Append($"($id{i:000}, $imageUrl{i:000})");

										cmdUpdateBackgrounds.Parameters.AddWithValue($"$id{i:000}", modifiedBackgroundImages[i].instanceId);
										cmdUpdateBackgrounds.Parameters.AddWithValue($"$imageUrl{i:000}", newFilename);
										expectedUpdates += 1;
									}
									sbCommand.AppendLine(
									$@"  
										) UPDATE BackgroundChatImage
										SET
											imageUrl = updated.imageUrl
										FROM updated
										WHERE (BackgroundChatImage.id = updated.id);");

									cmdUpdateBackgrounds.CommandText = sbCommand.ToString();
									updates += cmdUpdateBackgrounds.ExecuteNonQuery();
								}
							}

							if (updates != expectedUpdates)
							{
								transaction.Rollback();
								modified = 0;
								skipped = 0;
								return Backyard.Error.SQLCommandFailed;
							}

							transaction.Commit();
							return Backyard.Error.NoError;
						}
						catch (Exception e)
						{
							transaction.Rollback();
							modified = 0;
							skipped = 0;
							return Backyard.Error.SQLCommandFailed;
						}
					}
				}
			}
			catch (FileNotFoundException e)
			{
				modified = 0;
				skipped = 0;

				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				modified = 0;
				skipped = 0;
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				modified = 0;
				skipped = 0;
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

		public Backyard.Error GetAllImageUrls(out string[] imageUrls)
		{
			if (ConnectionEstablished == false)
			{
				imageUrls = null;
				return Backyard.Error.NotConnected;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					var lsImageUrls = new List<string>();

					// AppImage
					using (var cmdGetImages = connection.CreateCommand())
					{
						cmdGetImages.CommandText =
						@"
							SELECT id, imageUrl FROM AppImage
							WHERE id IN (
								SELECT A
								FROM _AppImageToCharacterConfigVersion)";

						using (var reader = cmdGetImages.ExecuteReader())
						{
							while (reader.Read())
							{
								string imageUrl = reader.GetString(1);
								lsImageUrls.Add(imageUrl);
							}
						}
					}

					// BackgroundChatImage
					if (BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds))
					{
						using (var cmdGetBackgrounds = connection.CreateCommand())
						{
							cmdGetBackgrounds.CommandText =
							@"
								SELECT id, imageUrl, chatId FROM BackgroundChatImage
								WHERE chatId IN (
									SELECT id
									FROM Chat
								);
							";

							using (var reader = cmdGetBackgrounds.ExecuteReader())
							{
								while (reader.Read())
								{
									string imageUrl = reader.GetString(1);

									lsImageUrls.Add(imageUrl);
								}
							}
						}
					}

					imageUrls = lsImageUrls.ToArray();
					return Backyard.Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				imageUrls = null;

				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				imageUrls = null;
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				imageUrls = null;
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

		private bool WriteUser(SQLiteConnection connection, string groupId, UserData userInfo, ImageOutput userPortrait, out string newUserId, out string newUserConfigId, out ImageOutput newUserPortrait, ref int updates, ref int expectedUpdates)
		{
			// Get existing user in group
			string userId = null;
			string userConfigId = null;
			bool isTemplate = true;

			if (groupId != null)
			{
				using (var cmdGetUser = connection.CreateCommand())
				{
					cmdGetUser.CommandText =
					@"
						SELECT 
							A.A, C.id, B.isTemplateChar
						FROM _CharacterConfigToGroupConfig AS A
						INNER JOIN CharacterConfig AS B ON B.id = A.A
						INNER JOIN CharacterConfigVersion AS C ON C.characterConfigId = B.id
						WHERE A.B = $groupId AND B.isUserControlled = 1;
					";
					cmdGetUser.Parameters.AddWithValue("$groupId", groupId);

					using (var reader = cmdGetUser.ExecuteReader())
					{
						if (reader.Read())
						{
							userId = reader.GetString(0);
							userConfigId = reader.GetString(1);
							isTemplate = reader.GetBoolean(2);
						}
					}
				}
			}

			if (userId == null) // New user (from default template)
			{
				if (CreateUserCharacter(connection, userInfo, userPortrait, null, out newUserId, out newUserConfigId, out newUserPortrait, ref updates, ref expectedUpdates) == false)
					return false;
			}
			else if (userId != null && isTemplate) // Replace template user
			{
				string templateUserId = userId;
				if (CreateUserCharacter(connection, userInfo, userPortrait, templateUserId, out newUserId, out newUserConfigId, out newUserPortrait, ref updates, ref expectedUpdates) == false)
					return false;

				// Replace existing user with new user
				if (groupId != null)
					ReplaceCharacterInGroup(connection, groupId, templateUserId, newUserId, ref updates, ref expectedUpdates);
			}
			else if (userConfigId != null && isTemplate == false) // Update existing user
			{
				// Update existing user
				if (UpdateCustomUser(connection, userId, userInfo, ref updates, ref expectedUpdates) == false)
				{
					newUserId = null;
					newUserConfigId = null;
					newUserPortrait = default(ImageOutput);
					return false;
				}

				newUserId = userId;
				newUserConfigId = userConfigId;
				newUserPortrait = userPortrait;
			}
			else // No change
			{
				newUserId = userId;
				newUserConfigId = userConfigId;
				newUserPortrait = userPortrait;
			}

			// Update user portrait
			if (newUserPortrait.hasAsset)
			{
				// Delete old entries
				using (var cmdDeleteImage = new SQLiteCommand(connection))
				{
					var sbCommand = new StringBuilder();

					sbCommand.AppendLine(
					$@"
						DELETE FROM AppImage
						WHERE id IN (
							SELECT A
							FROM _AppImageToCharacterConfigVersion
							WHERE B = $userConfigId
						);
					");
																
					sbCommand.AppendLine(
					$@"
						DELETE FROM _AppImageToCharacterConfigVersion
						WHERE B = $userConfigId;
					");

					cmdDeleteImage.CommandText = sbCommand.ToString();
					cmdDeleteImage.Parameters.AddWithValue("$userConfigId", newUserConfigId);

					int nDeletes = cmdDeleteImage.ExecuteNonQuery();
					expectedUpdates += nDeletes;
					updates += nDeletes;
				}

				using (var cmdAppImage = new SQLiteCommand(connection))
				{
					var sbCommand = new StringBuilder();

					string instanceId = Cuid.NewCuid();

					// AppImage
					sbCommand.AppendLine(
					$@"
						INSERT INTO AppImage
							(id, createdAt, updatedAt, imageUrl, label, ""order"", aspectRatio)
						VALUES
							($imageId, $timestamp, $timestamp, $imageUrl, $label, 0, $aspectRatio);
					");

					// _AppImageToCharacterConfigVersion
					sbCommand.AppendLine(
					$@"
						INSERT INTO _AppImageToCharacterConfigVersion
							(A, B)
						VALUES
							($imageId, $userConfigId);
					");

					cmdAppImage.CommandText = sbCommand.ToString();
					cmdAppImage.Parameters.AddWithValue($"$imageId", instanceId);
					cmdAppImage.Parameters.AddWithValue($"$imageUrl", newUserPortrait.imageUrl);
					cmdAppImage.Parameters.AddWithValue($"$label", newUserPortrait.label ?? "");
					cmdAppImage.Parameters.AddWithValue($"$aspectRatio", newUserPortrait.aspectRatio);
					cmdAppImage.Parameters.AddWithValue($"$userConfigId", newUserConfigId);
					cmdAppImage.Parameters.AddWithValue("$timestamp", DateTime.Now.ToUnixTimeMilliseconds());

					expectedUpdates += 2;
					updates += cmdAppImage.ExecuteNonQuery();
				}
			}
			return true;
		}
				
		private bool CreateUserCharacter(SQLiteConnection connection, UserData userInfo, ImageOutput portrait, string templateUserId, out string newUserId, out string newUserConfigId, out ImageOutput newPortrait, ref int updates, ref int expectedUpdates)
		{
			string defaultUserName = null;
			string defaultUserImageUrl = null;
			string defaultUserPersona = null;
			int defaultUserImageWidth = 0;
			int defaultUserImageHeight = 0;
			DateTime now = DateTime.Now;
			long createdAt = now.ToUnixTimeMilliseconds();

			if (string.IsNullOrEmpty(templateUserId) && FetchDefaultUser(connection, out templateUserId) == false)
			{
				newUserId = null;
				newUserConfigId = null;
				newPortrait = default(ImageOutput);
				return false; // Error
			}

			// Get template user's values
			using (var cmdBaseUser = connection.CreateCommand())
			{
				cmdBaseUser.CommandText =
				@"
					SELECT
						A.name, A.persona, C.imageUrl, C.aspectRatio
					FROM CharacterConfigVersion as A
					INNER JOIN _AppImageToCharacterConfigVersion AS B ON B.B = A.id
					INNER JOIN AppImage AS C ON C.id = B.A
					WHERE characterConfigId = $userId
				";
				cmdBaseUser.Parameters.AddWithValue("$userId", templateUserId);

				using (var reader = cmdBaseUser.ExecuteReader())
				{
					if (reader.Read())
					{
						defaultUserName = reader.GetString(0);
						defaultUserPersona = reader.GetString(1);
						defaultUserImageUrl = reader[2] as string;
						var aspectRatio = reader[3] as string;

						ImageInstance.ParseAspectRatio(aspectRatio, out defaultUserImageWidth, out defaultUserImageHeight);
					}
				}
			}

			newUserId = Cuid.NewCuid();
			newUserConfigId = Cuid.NewCuid();

			if (portrait.isDefined)
			{
				// Custom portrait
				newPortrait = portrait;
			}
			else if (defaultUserImageUrl != null && File.Exists(defaultUserImageUrl))
			{
				// Copy template portrait
				var filename = Utility.CreateRandomFilename(Utility.GetFileExt(defaultUserImageUrl));

				newPortrait = new ImageOutput() {
					instanceId = Cuid.NewCuid(),
					imageUrl = Path.Combine(AppSettings.BackyardLink.Location, "images", filename),
					data = AssetData.FromFile(defaultUserImageUrl),
					width = defaultUserImageWidth,
					height = defaultUserImageHeight,
					imageType = AssetFile.AssetType.UserIcon,
				};
			}
			else
			{
				// No portrait
				newPortrait = default(ImageOutput);
			}

			if (string.Compare(userInfo.name, Constants.DefaultUserName, StringComparison.OrdinalIgnoreCase) == 0)
				userInfo.name = null;

			// Create new user character
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
						($userId, $timestamp, $timestamp, 1, 0, 0);
				");

				// CharacterConfigVersion
				sbCommand.AppendLine(
				@"
					INSERT INTO CharacterConfigVersion
						(id, createdAt, updatedAt, displayName, name, persona, characterConfigId)
					VALUES 
						($configId, $timestamp, $timestamp, $name, $name, $persona, $userId);
				");

				cmdCreate.Parameters.AddWithValue("$userId", newUserId);
				cmdCreate.Parameters.AddWithValue("$configId", newUserConfigId);
				cmdCreate.Parameters.AddWithValue("$name", Utility.FirstNonEmpty(userInfo?.name, defaultUserName, Constants.DefaultUserName));
				cmdCreate.Parameters.AddWithValue("$persona", Utility.FirstNonEmpty(userInfo?.persona, defaultUserPersona) ?? "");
				cmdCreate.Parameters.AddWithValue("$timestamp", createdAt);

				cmdCreate.CommandText = sbCommand.ToString();

				expectedUpdates += 2;
				updates += cmdCreate.ExecuteNonQuery();
			}
			return true;
		}

		private bool UpdateCustomUser(SQLiteConnection connection, string userId, UserData userInfo, ref int updates, ref int expectedUpdates)
		{
			if (string.IsNullOrEmpty(userId) || userInfo == null)
				return false;

			string userConfigId = null;
			string defaultUserName = null;
			string defaultUserPersona = null;

			// Get default template
			using (var cmdCurrentUser = connection.CreateCommand())
			{
				cmdCurrentUser.CommandText =
				@"
					SELECT
						id, name, persona
					FROM CharacterConfigVersion
					WHERE characterConfigId = $userId;
				";
				cmdCurrentUser.Parameters.AddWithValue("$userId", userId);

				using (var reader = cmdCurrentUser.ExecuteReader())
				{
					if (reader.Read() == false)
						return false; // Error

					userConfigId = reader.GetString(0);
					defaultUserName = reader.GetString(1);
					defaultUserPersona = reader.GetString(2);
				}
			}

			if (string.Compare(userInfo.name, Constants.DefaultUserName, StringComparison.OrdinalIgnoreCase) == 0)
				userInfo.name = null;

			// Update user
			using (var cmdCreate = new SQLiteCommand(connection))
			{
				var sbCommand = new StringBuilder();

				// CharacterConfigVersion
				sbCommand.AppendLine(
				@"
					UPDATE CharacterConfigVersion
					SET 
						updatedAt = $timestamp,
						displayName = $name,
						name = $name,
						persona = $persona
					WHERE id = $userConfigId;
				");

				cmdCreate.Parameters.AddWithValue("$userConfigId", userConfigId);
				cmdCreate.Parameters.AddWithValue("$name", Utility.FirstNonEmpty(userInfo.name, defaultUserName, Constants.DefaultUserName));
				cmdCreate.Parameters.AddWithValue("$persona", Utility.FirstNonEmpty(userInfo.persona, defaultUserPersona) ?? "");
				cmdCreate.Parameters.AddWithValue("$timestamp", DateTime.Now.ToUnixTimeMilliseconds());
				cmdCreate.CommandText = sbCommand.ToString();

				expectedUpdates += 1;
				updates += cmdCreate.ExecuteNonQuery();
			}
			return true;
		}
		
		private void ReplaceCharacterInGroup(SQLiteConnection connection, string groupId, string oldCharacterId, string newCharacterId, ref int updates, ref int expectedUpdates)
		{
			using (var cmdReplace = new SQLiteCommand(connection))
			{
				var sbCommand = new StringBuilder();

				// _CharacterConfigToGroupConfig
				sbCommand.AppendLine(
				@"
					UPDATE _CharacterConfigToGroupConfig
					SET 
						A = $newId
					WHERE A = $oldId AND B = $groupId;
				");

				// Message
				sbCommand.AppendLine(
				@"
					UPDATE Message
					SET 
						characterConfigId = $newId
					WHERE characterConfigId = $oldId AND chatId IN (
						SELECT id
						FROM Chat
						WHERE groupConfigId = $groupId
					);
				");

				cmdReplace.Parameters.AddWithValue("$oldId", oldCharacterId);
				cmdReplace.Parameters.AddWithValue("$newId", newCharacterId);
				cmdReplace.Parameters.AddWithValue("$groupId", groupId);
				cmdReplace.CommandText = sbCommand.ToString();

				int nChanged = cmdReplace.ExecuteNonQuery();
				expectedUpdates += nChanged;
				updates += nChanged;
			}
		}

		public Backyard.Error GetUserInfo(string groupId, out string userId, out string name, out string persona, out ImageInstance image)
		{
			if (ConnectionEstablished == false)
			{
				userId = null;
				name = null;
				persona = null;
				image = null;
				return Backyard.Error.NotConnected;
			}

			try
			{
				using (var connection = CreateSQLiteConnection())
				{
					connection.Open();

					if (FetchUserInfo(connection, groupId, out userId, out name, out persona, out image) == false)
						return Backyard.Error.NotFound;

					return Backyard.Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				userId = null;
				name = null;
				persona = null;
				image = null;
				Backyard.Disconnect();
				return Backyard.Error.NotConnected;
			}
			catch (SQLiteException e)
			{
				userId = null;
				name = null;
				persona = null;
				image = null;
				Backyard.Disconnect();
				return Backyard.Error.SQLCommandFailed;
			}
			catch (Exception e)
			{
				userId = null;
				name = null;
				persona = null;
				image = null;
				Backyard.Disconnect();
				return Backyard.Error.Unknown;
			}
		}

		private bool FetchDefaultUser(SQLiteConnection connection, out string userId)
		{
			using (var cmdUser = connection.CreateCommand())
			{ 
				cmdUser.CommandText =
				@"
					SELECT id
					FROM CharacterConfig
					WHERE isDefaultUserCharacter = 1;
				";

				userId = cmdUser.ExecuteScalar() as string;
				if (userId != null)
					return true;

				// Get any user character
				cmdUser.CommandText =
				@"
					SELECT id
					FROM CharacterConfig
					WHERE isUserControlled = 1 and isTemplateChar = 1
					ORDER BY ""createdAt""
				";
				userId = cmdUser.ExecuteScalar() as string;
				if (userId != null)
					return true;
			}
			userId = null;
			return false;
		}

		private bool FetchDefaultModel(SQLiteConnection connection, out string model)
		{
			try
			{
				using (var cmdSettings = connection.CreateCommand())
				{
					cmdSettings.CommandText = @"SELECT model FROM AppSettings";

					using (var reader = cmdSettings.ExecuteReader())
					{
						if (reader.Read())
						{
							model = reader[0] as string ?? "";
							return true;
						}
					}
				}
			}
			catch 
			{ 
				// Do nothing
			}

			model = "";
			return false;
		}

		private bool FetchUserInfo(SQLiteConnection connection, string groupId, out string userId, out string name, out string persona, out ImageInstance image)
		{
			// Get user
			string configId = null;
			using (var cmdGetUser = connection.CreateCommand())
			{
				cmdGetUser.CommandText =
				@"
					SELECT 
						B.id, C.id, C.name, C.persona
					FROM _CharacterConfigToGroupConfig AS A
					INNER JOIN CharacterConfig AS B ON B.id = A.A
					INNER JOIN CharacterConfigVersion AS C ON C.characterConfigId = B.id
					WHERE A.B = $groupId AND B.isUserControlled = 1;
				";
				cmdGetUser.Parameters.AddWithValue("$groupId", groupId);

				using (var reader = cmdGetUser.ExecuteReader())
				{
					if (reader.Read() == false)
					{
						userId = null;
						name = null;
						persona = null;
						image = null;
						return false;
					}

					userId = reader.GetString(0);
					configId = reader.GetString(1);
					name = reader.GetString(2);
					persona = reader.GetString(3);

					if (name != null && name.Length > 1 && persona != null)
						persona = persona.Replace(name, "{user}");
				}
			}

			using (var cmdImageLookup = connection.CreateCommand())
			{
				cmdImageLookup.CommandText =
				@"
					SELECT 
						id, label, imageUrl, aspectRatio
					FROM AppImage
					WHERE id IN (
						SELECT A
						FROM _AppImageToCharacterConfigVersion
						WHERE B = $configId
					)
					ORDER BY ""order"" ASC
				";
				cmdImageLookup.Parameters.AddWithValue("$configId", configId);

				using (var reader = cmdImageLookup.ExecuteReader())
				{
					if (reader.Read())
					{
						string instanceId = reader.GetString(0);
						string label = reader[1] as string ?? "";
						string imageUrl = reader.GetString(2);
						string aspectRatio = reader.GetString(3);

						image = new ImageInstance() {
							instanceId = instanceId,
							associatedInstanceId = configId,
							label = label,
							imageUrl = imageUrl,
							aspectRatio = aspectRatio,
							imageType = AssetFile.AssetType.UserIcon,
						};
					}
					else
						image = null;
				}
			}
			return true;
		}
		
		private bool CreateGroup(SQLiteConnection connection, string characterId, out string groupId, out _Chat chat, ref int updates, ref int expectedUpdates)
		{
			string parentFolder = null;
			string folderOrder = null;

			// Fetch folder sort position
			using (var cmdFolder = connection.CreateCommand())
			{
				cmdFolder.CommandText =
				@"
					SELECT id
					FROM AppFolder
					WHERE isRoot = 1
				";
				parentFolder = cmdFolder.ExecuteScalar() as string;
			}

			using (var cmdFolderOrder = connection.CreateCommand())
			{
				cmdFolderOrder.CommandText =
				@"
					SELECT folderSortPosition
					FROM GroupConfig
					WHERE folderId = $folderId
					ORDER BY folderSortPosition ASC;
				";
				cmdFolderOrder.Parameters.AddWithValue("$folderId", parentFolder);
				folderOrder = cmdFolderOrder.ExecuteScalar() as string;
			}

			// Fetch default user
			string userId;
			if (FetchDefaultUser(connection, out userId) == false)
			{
				groupId = null;
				chat = default(_Chat);
				return false;
			}

			string defaultModel;
			FetchDefaultModel(connection, out defaultModel);

			groupId = Cuid.NewCuid();
			DateTime now = DateTime.Now;
			long createdAt = now.ToUnixTimeMilliseconds();

			// Create group
			using (var cmdUpdate = new SQLiteCommand(connection))
			{
				cmdUpdate.CommandText =
				@"
					INSERT INTO GroupConfig
						(id, createdAt, updatedAt, folderId, folderSortPosition, name)
					VALUES 
						($groupId, $timestamp, $timestamp, $folderId, $folderSortPosition, '');
				
					INSERT INTO _CharacterConfigToGroupConfig
						(A, B)
					VALUES 
						($charId, $groupId),
						($userId, $groupId);
				";
				cmdUpdate.Parameters.AddWithValue("$groupId", groupId);
				cmdUpdate.Parameters.AddWithValue("$userId", userId);
				cmdUpdate.Parameters.AddWithValue("$charId", characterId);
				cmdUpdate.Parameters.AddWithValue("$timestamp", createdAt);
				cmdUpdate.Parameters.AddWithValue("$folderId", parentFolder);
				cmdUpdate.Parameters.AddWithValue("$folderSortPosition", MakeFolderSortPosition(SortPosition.Before, folderOrder));

				expectedUpdates += 3;
				updates += cmdUpdate.ExecuteNonQuery();
			}

			// Create chat
			string chatId = Cuid.NewCuid();

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
						($chatId, $timestamp, $timestamp, '', '', $pruneExample, '', '', '', 
							$groupId, 
							$model, $temperature, $topP, $minP, $minPEnabled, $topK, $repeatPenalty, $repeatLastN, $promptTemplate,
							$chatName, '', $ttsAutoPlay, $ttsInputFilter);
				");

				var staging = new ChatStaging();
				var parameters = new ChatParameters();

				cmdCreateChat.CommandText = sbCommand.ToString();
				cmdCreateChat.Parameters.AddWithValue("$chatId", chatId);
				cmdCreateChat.Parameters.AddWithValue("$groupId", groupId);
				cmdCreateChat.Parameters.AddWithValue("$chatName", ChatInstance.DefaultName);
				cmdCreateChat.Parameters.AddWithValue("$timestamp", createdAt);
				cmdCreateChat.Parameters.AddWithValue("$pruneExample", staging.pruneExampleChat);
				cmdCreateChat.Parameters.AddWithValue("$ttsAutoPlay", staging.ttsAutoPlay);
				cmdCreateChat.Parameters.AddWithValue("$ttsInputFilter", staging.ttsInputFilter ?? "default");
				cmdCreateChat.Parameters.AddWithValue("$model", parameters.model ?? defaultModel ?? "");
				cmdCreateChat.Parameters.AddWithValue("$temperature", parameters.temperature);
				cmdCreateChat.Parameters.AddWithValue("$topP", parameters.topP);
				cmdCreateChat.Parameters.AddWithValue("$minP", parameters.minP);
				cmdCreateChat.Parameters.AddWithValue("$minPEnabled", parameters.minPEnabled);
				cmdCreateChat.Parameters.AddWithValue("$topK", parameters.topK);
				cmdCreateChat.Parameters.AddWithValue("$repeatPenalty", parameters.repeatPenalty);
				cmdCreateChat.Parameters.AddWithValue("$repeatLastN", parameters.repeatLastN);
				cmdCreateChat.Parameters.AddWithValue("$promptTemplate", parameters.promptTemplate);

				expectedUpdates += 1;
				updates += cmdCreateChat.ExecuteNonQuery();

				chat = new _Chat() {
					instanceId = chatId,
					creationDate = now,
					updateDate = now,
					name = ChatInstance.DefaultName,
					parameters = parameters,
					staging = staging,
				};
			}

			return true;
		}

		public void ToPartyNames(ChatStaging staging, string characterId, string userId)
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

		private void ToPartyNames(ref string text, string characterId, string userId)
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

		public void FromPartyNames(ChatStaging staging, string groupId)
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

		private void FromPartyNames(ref string text, string groupId, Dictionary<string, string> knownIds = null)
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
				else
				{
					CharacterInstance character;
					if (GetCharacter(characterId, out character))
					{
						if (character.isUser)
							placeholder = "{user}";
						else if (groupId != null)
						{
							GroupInstance group;
							if (!(GetGroup(groupId, out group) && group.members != null && group.members.Contains(characterId)))
								placeholder = character.name; // Not primary character
						}
					}
					sb.Insert(pos_begin, placeholder);

					if (knownIds != null)
						knownIds.Add(characterId, placeholder);
				}
				
				pos_begin = sb.IndexOf("{_cfg&:", pos_begin);
			}

			text = sb.ToString();
		}

#endregion // Utilities

	}

}
