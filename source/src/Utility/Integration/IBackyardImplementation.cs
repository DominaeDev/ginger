using System;
using System.Collections.Generic;

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

	public interface IBackyardImplementation
	{
		// Getters
		IEnumerable<FolderInstance> Folders { get; }
		IEnumerable<CharacterInstance> Characters { get; }
		IEnumerable<GroupInstance> Groups { get; }

		// State
		bool GetCharacter(string characterId, out CharacterInstance character);
		bool GetGroup(string groupId, out GroupInstance group);
		GroupInstance GetGroupForCharacter(string characterId);
		Backyard.Error RefreshCharacters();
		string LastError { get; }

		// Characters
		Backyard.Error ImportCharacter(CharacterInstance character, out FaradayCardV4 card, out ImageInstance[] images);
		Backyard.Error ImportCharacter(CharacterInstance character, out FaradayCardV4 card, out ImageInstance[] images, out UserData userInfo);
		Backyard.Error GetImageUrls(CharacterInstance characterInstance, out string[] imageUrls);
		Backyard.Error ConfirmSaveCharacter(FaradayCardV4 card, Backyard.Link linkInfo, out bool newerChangesFound);
		Backyard.Error CreateNewCharacter(FaradayCardV4 card, ImageInput[] imageInput, BackupData.Chat[] chats, out CharacterInstance characterInstance, out Backyard.Link.Image[] imageLinks, UserData userInfo = null, FolderInstance folder = default(FolderInstance));
		Backyard.Error UpdateCharacter(FaradayCardV4 card, Backyard.Link linkInfo, out DateTime updateDate, out Backyard.Link.Image[] updatedImageLinks, UserData userInfo = null);

		Backyard.Error ConfirmDeleteCharacters(CharacterInstance[] characterInstances, out ConfirmDeleteResult result);
		Backyard.Error DeleteCharacters(string[] characterIds, string[] groupIds, string[] imageIds);
		Backyard.Error DeleteOrphanedUsers();
		Backyard.Error DeleteOrphanedUsers(out string[] imageUrls);

		// Chat
		Backyard.Error GetChatCounts(out Dictionary<string, ChatCount> counts);
		Backyard.Error GetChats(string groupId, out ChatInstance[] chatInstances);
		Backyard.Error GetChat(string chatId, string groupId, out ChatInstance chatInstance);
		Backyard.Error CreateNewChat(CreateChatArguments args, string groupId, out ChatInstance chatInstance);
		Backyard.Error RenameChat(ChatInstance chatInstance, string newName);
		Backyard.Error ConfirmDeleteChat(ChatInstance chatInstance, GroupInstance groupInstance, out int chatCount);
		Backyard.Error ConfirmChatExists(string chatId);
		Backyard.Error DeleteChat(ChatInstance chatInstance);
		Backyard.Error PurgeChats(GroupInstance groupInstance);
		Backyard.Error UpdateChat(ChatInstance chatInstance, string groupId);
		Backyard.Error RepairChats(GroupInstance groupInstance, out int modified);
		Backyard.Error UpdateChatParameters(string chatId, ChatParameters parameters, ChatStaging staging);
		Backyard.Error UpdateChatParameters(string[] chatIds, ChatParameters parameters, ChatStaging staging);
		Backyard.Error UpdateChatBackground(string[] chatIds, string imageUrl, int width, int height);

		// Utility
		Backyard.Error CreateNewFolder(string folderName, out FolderInstance folderInstance);
		BackupData.Chat[] GatherChats(FaradayCardV4 card, Generator.Output output, ImageInput[] images);
		Backyard.Error RepairImages(out int modified, out int skipped);
		Backyard.Error GetAllImageUrls(out string[] imageUrls);
		Backyard.Error GetUserInfo(string groupId, out string userId, out string name, out string persona, out ImageInstance image);
	}

	public static class BackyardImplExtensions
	{
		public static CharacterInstance GetCharacter(this IBackyardImplementation impl, string characterId)
		{
			CharacterInstance character;
			if (impl.GetCharacter(characterId, out character))
				return character;
			return default(CharacterInstance);
		}

		public static bool HasCharacter(this IBackyardImplementation impl, string characterId)
		{
			CharacterInstance tmp;
			return impl.GetCharacter(characterId, out tmp);
		}

		public static GroupInstance GetGroup(this IBackyardImplementation impl, string groupId)
		{
			GroupInstance group;
			if (impl.GetGroup(groupId, out group))
				return group;
			return default(GroupInstance);
		}

	}
}
