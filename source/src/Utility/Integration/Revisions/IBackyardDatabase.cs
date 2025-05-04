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
	using CreateCharacterArguments = Backyard.CreateCharacterArguments;
	using CreatePartyArguments = Backyard.CreatePartyArguments;
	using CreateChatArguments = Backyard.CreateChatArguments;
	using ImageInput = Backyard.ImageInput;
	using ImageInstance = Backyard.ImageInstance;
	using ConfirmDeleteResult = Backyard.ConfirmDeleteResult;
	using CharacterMessage = Backyard.CharacterMessage;

	using FaradayCard = BackyardLinkCard;

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
		Backyard.Error ImportCharacter(string characterId, out FaradayCard card, out ImageInstance[] images, out UserData userInfo);
		Backyard.Error CreateNewCharacter(CreateCharacterArguments args, out CharacterInstance characterInstance, out Backyard.Link.Image[] imageLinks);
		Backyard.Error UpdateCharacter(Backyard.Link link, FaradayCard card, UserData userInfo, out DateTime updateDate, out Backyard.Link.Image[] updatedImageLinks);
		Backyard.Error ConfirmSaveCharacter(Backyard.Link link, out bool newerChangesFound);
		Backyard.Error ConfirmDeleteCharacters(string[] characterIds, out ConfirmDeleteResult result);
		Backyard.Error DeleteCharacters(string[] characterIds, string[] groupIds, string[] imageIds);

		// Party
		Backyard.Error ImportParty(string groupId, out FaradayCard[] cards, out CharacterInstance[] characterInstances, out ImageInstance[] images, out UserData userInfo);
		Backyard.Error CreateNewParty(CreatePartyArguments args, out GroupInstance groupInstance, out CharacterInstance[] characterInstances, out Backyard.Link.Image[] imageLinks);
		Backyard.Error UpdateParty(Backyard.Link link, FaradayCard[] cards, UserData userInfo, out DateTime updateDate, out Backyard.Link.Image[] updatedImageLinks);

		// Chat
		Backyard.Error GetChats(string groupId, out ChatInstance[] chatInstances);
		Backyard.Error GetChatCounts(out Dictionary<string, Backyard.ChatCount> counts);
		Backyard.Error CreateNewChat(CreateChatArguments args, string groupId, out ChatInstance chatInstance);
		Backyard.Error RenameChat(string chatId, string newName);
		Backyard.Error ConfirmDeleteChat(string chatId, string groupId, out int chatCount);
		Backyard.Error ConfirmChatExists(string chatId);
		Backyard.Error UpdateChat(string chatId, ChatInstance chatInstance, string groupId);
		Backyard.Error UpdateChatParameters(string[] chatIds, ChatStaging staging, ChatParameters parameters);
		Backyard.Error UpdateChatBackground(string[] chatIds, string imageUrl, int width, int height);
		Backyard.Error DeleteChat(string chatId);
		Backyard.Error DeleteAllChats(string groupId);

		// Folders
		Backyard.Error CreateNewFolder(string folderName, out FolderInstance folderInstance);

		// Utility
		Backyard.Error GetAllImageUrls(out string[] imageUrls);
		Backyard.Error GetImageUrls(string characterConfigId, out string[] imageUrls);
		Backyard.Error RepairChats(string groupId, out int modified);
		Backyard.Error RepairImages(out int modified, out int skipped);
		Backyard.Error DeleteOrphanedUsers(out string[] imageUrls);
		Backyard.Error ResetModelDownloadLocation();
	}

	public static class BackyardImplExtensions
	{
		public static bool HasCharacter(this IBackyardDatabase impl, string characterId)
		{
			if (string.IsNullOrEmpty(characterId))
				return false;

			return impl.GetCharacter(characterId, out var tmp);
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
			return impl.GetGroup(groupId, out var tmp);
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
				.Where(g => (g.activeMembers != null && g.activeMembers.Contains(characterId)) || (g.inactiveMembers != null && g.inactiveMembers.Contains(characterId)))
				.OrderBy(g => g.Count) // Solo group < Party
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

		public static Backyard.Error UpdateChatParameters(this IBackyardDatabase impl, string chatId, ChatStaging staging, ChatParameters parameters)
		{
			return impl.UpdateChatParameters(new string[] { chatId }, staging, parameters);
		}

	}
}
