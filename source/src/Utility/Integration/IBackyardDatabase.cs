using System;
using System.Collections.Generic;
using System.Linq;

namespace Ginger.Integration
{
	using CharacterInstance = Backyard.CharacterInstance;
	using FolderInstance = Backyard.FolderInstance;
	using GroupInstance = Backyard.GroupInstance;
	using ChatInstance = Backyard.ChatInstance;
	using ChatParameters = Backyard.ChatParameters;
	using ChatStaging = Backyard.ChatStaging;
	using ChatBackground = Backyard.ChatBackground;
	using CreateChatArguments = Backyard.CreateChatArguments;
	using ImageInput = Backyard.ImageInput;
	using ImageInstance = Backyard.ImageInstance;
	using ConfirmDeleteResult = Backyard.ConfirmDeleteResult;

	public interface IBackyardDatabase
	{
		// Getters
		IEnumerable<CharacterInstance> Everyone { get; }
		IEnumerable<CharacterInstance> Characters { get; }
		IEnumerable<CharacterInstance> Users { get; }
		IEnumerable<GroupInstance> Groups { get; }
		IEnumerable<FolderInstance> Folders { get; }

		// State
		Backyard.Error RefreshCharacters();
		bool GetCharacter(string characterId, out CharacterInstance character);
		bool GetGroup(string groupId, out GroupInstance group);
		string LastError { get; }

		// Characters
		Backyard.Error ImportCharacter(CharacterInstance character, out FaradayCardV4 card, out ImageInstance[] images, out UserData userInfo);
		Backyard.Error CreateNewCharacter(FaradayCardV4 card, ImageInput[] imageInput, BackupData.Chat[] chats, out CharacterInstance characterInstance, out Backyard.Link.Image[] imageLinks, UserData userInfo = null, FolderInstance folder = default(FolderInstance));
		Backyard.Error UpdateCharacter(FaradayCardV4 card, Backyard.Link linkInfo, out DateTime updateDate, out Backyard.Link.Image[] updatedImageLinks, UserData userInfo = null);
		Backyard.Error ConfirmSaveCharacter(Backyard.Link linkInfo, out bool newerChangesFound);

		// Party
		Backyard.Error ImportGroup(GroupInstance character, out FaradayCardV4[] cards, out CharacterInstance[] characterInstances, out ImageInstance[] images, out UserData userInfo);
		Backyard.Error CreateNewGroup(FaradayCardV4[] cards, ImageInput[] imageInput, BackupData.Chat[] chats, out GroupInstance groupInstance, out CharacterInstance[] characterInstances, out Backyard.Link.Image[] imageLinks, UserData userInfo = null, FolderInstance folder = default(FolderInstance));
		Backyard.Error UpdateGroup(FaradayCardV4[] cards, Backyard.Link linkInfo, out DateTime updateDate, out Backyard.Link.Image[] updatedImageLinks, UserData userInfo = null);

		// Delete
		Backyard.Error ConfirmDeleteCharacters(CharacterInstance[] characterInstances, out ConfirmDeleteResult result);
		Backyard.Error DeleteCharacters(string[] characterIds, string[] groupIds, string[] imageIds);
		Backyard.Error DeleteOrphanedUsers(out string[] imageUrls);

		// Chat
		Backyard.Error GetChat(string chatId, string groupId, out ChatInstance chatInstance);
		Backyard.Error GetChats(string groupId, out ChatInstance[] chatInstances);
		Backyard.Error CreateNewChat(CreateChatArguments args, string groupId, out ChatInstance chatInstance);
		Backyard.Error RenameChat(string chatId, string newName);
		Backyard.Error ConfirmDeleteChat(string chatId, string groupId, out int chatCount);
		Backyard.Error ConfirmChatExists(string chatId);
		Backyard.Error DeleteChat(string chatId);
		Backyard.Error DeleteAllChats(string groupId);
		Backyard.Error UpdateChat(string chatId, ChatInstance chatInstance, string groupId);
		Backyard.Error UpdateChatParameters(string chatId, ChatParameters parameters, ChatStaging staging);
		Backyard.Error UpdateChatParameters(string[] chatIds, ChatParameters parameters, ChatStaging staging);
		Backyard.Error UpdateChatBackground(string[] chatIds, string imageUrl, int width, int height);

		// Utility
		Backyard.Error CreateNewFolder(string folderName, out FolderInstance folderInstance);
		BackupData.Chat[] GatherChats(FaradayCardV4 card, Generator.Output output, ImageInput[] images);
		Backyard.Error GetAllImageUrls(out string[] imageUrls);
		Backyard.Error GetImageUrls(CharacterInstance characterInstance, out string[] imageUrls);

		// Repair
		Backyard.Error RepairChats(string groupId, out int modified);
		Backyard.Error RepairImages(out int modified, out int skipped);
	}

	public static class BackyardImplExtensions
	{
		public static bool HasCharacter(this IBackyardDatabase impl, string characterId)
		{
			if (string.IsNullOrEmpty(characterId))
				return false;

			CharacterInstance tmp;
			return impl.GetCharacter(characterId, out tmp);
		}

		public static CharacterInstance GetCharacter(this IBackyardDatabase impl, string characterId)
		{
			CharacterInstance character;
			if (impl.GetCharacter(characterId, out character))
				return character;
			return default(CharacterInstance);
		}
		
		public static bool HasGroup(this IBackyardDatabase impl, string groupId)
		{
			if (string.IsNullOrEmpty(groupId))
				return false;
			GroupInstance tmp;
			return impl.GetGroup(groupId, out tmp);
		}

		public static GroupInstance GetGroup(this IBackyardDatabase impl, string groupId)
		{
			GroupInstance group;
			if (impl.GetGroup(groupId, out group))
				return group;
			return default(GroupInstance);
		}

		public static GroupInstance GetGroupForCharacter(this IBackyardDatabase impl, string characterId)
		{
			return GetGroupsForCharacter(impl, characterId).FirstOrDefault();
		}

		public static GroupInstance[] GetGroupsForCharacter(this IBackyardDatabase impl, string characterId)
		{
			if (string.IsNullOrEmpty(characterId))
				return new GroupInstance[0];

			return impl.Groups
				.Where(g => g.members != null && g.members.Contains(characterId))
				.OrderBy(g => g.members.Length) // Solo group > Party
				.ToArray();
		}

		public static bool FetchLatestChat(this IBackyardDatabase impl, string groupId, out ChatInstance chat)
		{
			ChatInstance[] chats;
			var error = impl.GetChats(groupId, out chats);
			if (error != Backyard.Error.NoError)
			{
				chat = default(ChatInstance);
				return false;
			}

			chat = chats
				.OrderByDescending(c => DateTimeExtensions.Max(c.history.lastMessageTime, c.creationDate))
				.FirstOrDefault();
			return chat != default(ChatInstance);
		}

	}
}
