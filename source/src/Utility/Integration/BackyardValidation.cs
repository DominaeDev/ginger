﻿using System.Collections.Generic;

namespace Ginger.Integration
{
	public enum BackyardDatabaseVersion
	{
		Unknown,
		Version_0_28_0,		// Groups
		Version_0_29_0,		// Chat backgrounds (Canary 0.28.27)
	}

	public static class BackyardValidation
	{
		public static Dictionary<BackyardDatabaseVersion, string[][]> TablesByVersion = new Dictionary<BackyardDatabaseVersion, string[][]> {
			{ 
				BackyardDatabaseVersion.Version_0_28_0,
				new string[][]
				{
					new string[]
					{
						"_AppCharacterLorebookItemToCharacterConfigVersion",
						"A",						"TEXT",
						"B",						"TEXT",
					},
					new string[]
					{
						"_AppImageToCharacterConfigVersion",
						"A",						"TEXT",
						"B",						"TEXT",
					},
					new string[]
					{
						"_CharacterConfigToGroupConfig",
						"A",						"TEXT",
						"B",						"TEXT",
					},
					new string[]
					{
						"AppCharacterLorebookItem",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"order",					"TEXT",
						"key",						"TEXT",
						"value",					"TEXT",
					},
					new string[]
					{
						"AppFolder",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"name",						"TEXT",
						"url",						"TEXT",
						"pinnedToSidebarPosition",	"TEXT",
						"parentFolderId",			"TEXT",
						"isRoot",					"BOOLEAN",
						"sortIsDesc",				"BOOLEAN",
						"sortType",					"TEXT",
					},
					new string[]
					{
						"AppImage",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"imageUrl",					"TEXT",
						"label",					"TEXT",
						"order",					"INTEGER",
						"aspectRatio",				"TEXT",
					},
					new string[]
					{
						"CharacterConfig",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"isUserControlled",			"BOOLEAN",
						"isDefaultUserCharacter",	"BOOLEAN",
						"isTemplateChar",			"BOOLEAN",
					},
					new string[]
					{
						"CharacterConfigVersion",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"displayName",				"TEXT",
						"name",						"TEXT",
						"persona",					"TEXT",
						"ttsVoice",					"TEXT",
						"ttsSpeed",					"REAL",
						"characterConfigId",		"TEXT",
					},
					new string[]
					{
						"Chat",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"name",						"TEXT",
						"context",					"TEXT",
						"customDialogue",			"TEXT",
						"canDeleteCustomDialogue",	"BOOLEAN",
						"authorNote",				"TEXT",
						"model",					"TEXT",
						"modelInstructions",		"TEXT",
						"temperature",				"REAL",
						"topP",						"REAL",
						"minP",						"REAL",
						"minPEnabled",				"BOOLEAN",
						"topK",						"INTEGER",
						"repeatPenalty",			"REAL",
						"repeatLastN",				"INTEGER",
						"grammar",					"TEXT",
						"promptTemplate",			"TEXT",
						"ttsAutoPlay",				"BOOLEAN",
						"ttsInputFilter",			"TEXT",
						"groupConfigId",			"TEXT",
						"greetingDialogue",			"TEXT",
					},
					new string[]
					{
						"GroupConfig",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"hubCharId",				"TEXT",
						"hubAuthorId",				"TEXT",
						"hubAuthorUsername",		"TEXT",
						"hubCharIdAnalytics",		"TEXT",
						"forkedFromLocalId",		"TEXT",
						"name",						"TEXT",
						"isNSFW",					"BOOLEAN",
						"folderId",					"TEXT",
						"folderSortPosition",		"TEXT",
					},
					new string[]
					{
						"Message",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"liked",					"BOOLEAN",
						"chatId",					"TEXT",
						"characterConfigId",		"TEXT",
					},
					new string[]
					{
						"RegenSwipe",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"activeTimestamp",			"DATETIME",
						"text",						"TEXT",
						"messageId",				"TEXT",
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
				}
			}, // 0.28.0
			{ 
				BackyardDatabaseVersion.Version_0_29_0,
				new string[][]
				{
					new string[]
					{
						"_AppCharacterLorebookItemToCharacterConfigVersion",
						"A",						"TEXT",
						"B",						"TEXT",
					},
					new string[]
					{
						"_AppImageToCharacterConfigVersion",
						"A",						"TEXT",
						"B",						"TEXT",
					},
					new string[]
					{
						"_CharacterConfigToGroupConfig",
						"A",						"TEXT",
						"B",						"TEXT",
					},
					new string[]
					{
						"AppCharacterLorebookItem",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"order",					"TEXT",
						"key",						"TEXT",
						"value",					"TEXT",
					},
					new string[]
					{
						"AppFolder",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"name",						"TEXT",
						"url",						"TEXT",
						"pinnedToSidebarPosition",	"TEXT",
						"parentFolderId",			"TEXT",
						"isRoot",					"BOOLEAN",
						"sortIsDesc",				"BOOLEAN",
						"sortType",					"TEXT",
					},
					new string[]
					{
						"AppImage",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"imageUrl",					"TEXT",
						"label",					"TEXT",
						"order",					"INTEGER",
						"aspectRatio",				"TEXT",
					},
					new string[]
					{
						"BackgroundChatImage",
						"id",						"TEXT",
						"imageUrl",					"TEXT",
						"aspectRatio",				"TEXT",
						"chatId",					"TEXT",
					},
					new string[]
					{
						"CharacterConfig",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"isUserControlled",			"BOOLEAN",
						"isDefaultUserCharacter",	"BOOLEAN",
						"isTemplateChar",			"BOOLEAN",
					},
					new string[]
					{
						"CharacterConfigVersion",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"displayName",				"TEXT",
						"name",						"TEXT",
						"persona",					"TEXT",
						"ttsVoice",					"TEXT",
						"ttsSpeed",					"REAL",
						"characterConfigId",		"TEXT",
					},
					new string[]
					{
						"Chat",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"name",						"TEXT",
						"context",					"TEXT",
						"customDialogue",			"TEXT",
						"canDeleteCustomDialogue",	"BOOLEAN",
						"authorNote",				"TEXT",
						"model",					"TEXT",
						"modelInstructions",		"TEXT",
						"temperature",				"REAL",
						"topP",						"REAL",
						"minP",						"REAL",
						"minPEnabled",				"BOOLEAN",
						"topK",						"INTEGER",
						"repeatPenalty",			"REAL",
						"repeatLastN",				"INTEGER",
						"grammar",					"TEXT",
						"promptTemplate",			"TEXT",
						"ttsAutoPlay",				"BOOLEAN",
						"ttsInputFilter",			"TEXT",
						"groupConfigId",			"TEXT",
						"greetingDialogue",			"TEXT",
					},
					new string[]
					{
						"GroupConfig",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"hubCharId",				"TEXT",
						"hubAuthorId",				"TEXT",
						"hubAuthorUsername",		"TEXT",
						"hubCharIdAnalytics",		"TEXT",
						"forkedFromLocalId",		"TEXT",
						"name",						"TEXT",
						"isNSFW",					"BOOLEAN",
						"folderId",					"TEXT",
						"folderSortPosition",		"TEXT",
					},
					new string[]
					{
						"Message",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"liked",					"BOOLEAN",
						"chatId",					"TEXT",
						"characterConfigId",		"TEXT",
					},
					new string[]
					{
						"RegenSwipe",
						"id",						"TEXT",
						"createdAt",				"DATETIME",
						"updatedAt",				"DATETIME",
						"activeTimestamp",			"DATETIME",
						"text",						"TEXT",
						"messageId",				"TEXT",
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
				}
			}, // 0.29.0
		
		}; // TablesByVersion
	} // class
}
