namespace Ginger
{
	public static partial class BackyardDatabaseTables
	{
		/*
			Version 0.37.0
			Release 2025-XX-XX
			
			Second major database change that added group chats ("parties").
		*/
		public static readonly string[][] Version37 = new string[][]
		{
			new string[]
			{
				"AppFolder",
				"id",							"TEXT",
				"createdAt",					"DATETIME",
				"updatedAt",					"DATETIME",
				"name",							"TEXT",
				"url",							"TEXT",
				"pinnedToSidebarPosition",		"TEXT",
				"parentFolderId",				"TEXT",
				"isRoot",						"BOOLEAN",
				"sortIsDesc",					"BOOLEAN",
				"sortType",						"TEXT",
			},
			new string[]
			{
				"AppImage",
				"id",							"TEXT",
				"createdAt",					"DATETIME",
				"updatedAt",					"DATETIME",
				"imageUrl",						"TEXT",
				"label",						"TEXT",
				"order",						"INTEGER",
				"aspectRatio",					"TEXT",
			},
			new string[]
			{
				"AppCharacterLorebookItem",
				"id",							"TEXT",
				"createdAt",					"DATETIME",
				"updatedAt",					"DATETIME",
				"order",						"TEXT",
				"key",							"TEXT",
				"value",						"TEXT",
			},
			new string[]
			{
				"_AppImageToCharacterConfigVersion",
				"A",							"TEXT",
				"B",							"TEXT",
			},
			new string[]
			{
				"_AppCharacterLorebookItemToCharacterConfigVersion",
				"A",							"TEXT",
				"B",							"TEXT",
			},
			new string[]
			{
				"ExampleMessage",
				"id",							"TEXT",
				"text",							"TEXT",
				"chatId",						"TEXT",
				"characterConfigId",			"TEXT",
				"createdAt",					"DATETIME",
				"updatedAt",					"DATETIME",
				"position",						"TEXT",
			},
			new string[]
			{
				"GreetingMessage",
				"id",							"TEXT",
				"text",							"TEXT",
				"chatId",						"TEXT",
				"characterConfigId",			"TEXT",
				"createdAt", 					"DATETIME",
				"updatedAt", 					"DATETIME",
				"position", 					"TEXT",
			},
			new string[]
			{
				"BackgroundChatImage",
				"id", 							"TEXT",
				"imageUrl", 					"TEXT",
				"aspectRatio", 					"TEXT",
				"chatId", 						"TEXT",
			},
			new string[]
			{
				"Message",
				"id",							"TEXT",
				"liked",						"BOOLEAN",
				"chatId", 						"TEXT",
				"characterConfigId",			"TEXT",
				"createdAt", 					"DATETIME",
				"updatedAt", 					"DATETIME",
			},
			new string[]
			{
				"RegenSwipe",
				"id",							"TEXT",
				"text",							"TEXT",
				"activeTimestamp",				"DATETIME",
				"canContinue",					"BOOLEAN",
				"messageId", 					"TEXT",
				"createdAt", 					"DATETIME",
				"updatedAt", 					"DATETIME",
			},
			new string[]
			{
				"CharacterConfigVersion",
				"id",							"TEXT",
				"displayName", 					"TEXT",
				"name", 						"TEXT",
				"persona", 						"TEXT",
				"exampleDialogue",				"TEXT",
				"ttsVoice", 					"TEXT",
				"ttsSpeed", 					"REAL",
				"characterConfigId",			"TEXT",
				"createdAt", 					"DATETIME",
				"updatedAt", 					"DATETIME",
				"tagline", 						"TEXT",
			},
			new string[]
			{
				"GroupConfigCharacterLink",
				"assignedAt", 					"DATETIME",
				"position", 					"TEXT",
				"isActive", 					"BOOLEAN",
				"groupConfigId",				"TEXT",
				"characterConfigId",			"TEXT",
			},
			new string[]
			{
				"CharacterConfig",
				"id", 							"TEXT",
				"isUserControlled", 			"BOOLEAN",
				"isDefaultUserCharacter", 		"BOOLEAN",
				"isTemplateChar",				"BOOLEAN",
				"isNSFW",						"BOOLEAN",
				"hubCharacterConfigId",			"TEXT",
				"hubAuthorId", 					"TEXT",
				"hubAuthorUsername",			"TEXT",
				"hasHubInfoMigration",			"BOOLEAN",
				"createdAt",					"DATETIME",
				"updatedAt",					"DATETIME",
				"deletedAt",					"DATETIME",
			},
			new string[]
			{
				"Chat",
				"id", 							"TEXT",
				"name",							"TEXT",
				"context",						"TEXT",
				"canDeleteCustomDialogue", 		"BOOLEAN",
				"authorNote",					"TEXT",
				"model",						"TEXT",
				"modelInstructions",			"TEXT",
				"modelInstructionsType",		"TEXT",
				"temperature", 					"REAL",
				"topP", 						"REAL",
				"minP", 						"REAL",
				"minPEnabled", 					"BOOLEAN",
				"topK", 						"INTEGER",
				"repeatPenalty", 				"REAL",
				"repeatLastN", 					"INTEGER",
				"grammar", 						"TEXT",
				"promptTemplate", 				"TEXT",
				"groupConfigId", 				"TEXT",
				"greetingDialogue_DO_NOT_USE",	"TEXT",
				"customDialogue_DO_NOT_USE",	"TEXT",
				"hasMigratedDialogueItems",		"BOOLEAN",
				"hubChatId",					"TEXT",
				"hubChatAuthorId",				"TEXT",
				"hasHubInfoMigration",			"BOOLEAN",
				"createdAt",					"DATETIME",
				"updatedAt",					"DATETIME",
			},
			new string[]
			{
				"GroupConfig",
				"id", 							"TEXT",
				"folderId", 					"TEXT",
				"folderSortPosition",			"TEXT",
				"name", 						"TEXT",
				"tagline", 						"TEXT",
				"isNSFW", 						"BOOLEAN",
				"ttsAutoPlay", 					"BOOLEAN",
				"ttsInputFilter", 				"TEXT",
				"hubGroupConfigId",				"TEXT",
				"hubAuthorId",					"TEXT",
				"hubAuthorUsername",			"TEXT",
				"hasHubInfoMigration", 			"BOOLEAN",
				"createdAt", 					"DATETIME",
				"updatedAt", 					"DATETIME",
				"hasCharFieldsMigrated", 		"BOOLEAN",
				"hasRegenSwipesMigrated", 		"BOOLEAN",
			},

			// Ignored tables

			new string[]
			{
				"_prisma_migrations",
			},
			new string[]
			{
				"AppSettings",
			},
			new string[]
			{
				"GpuLayersCache",
			},
			new string[]
			{
				"ModelLayerSamples",
			},
			new string[]
			{
				"ScratchBufferSize",
			},
		};
	}
}
