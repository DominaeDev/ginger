using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.IO;

namespace Ginger
{
	public static class FaradayBridge
	{
		public struct CharacterInstance
		{
			public string instanceID;	// CharacterConfig
			public string configID;		// CharacterConfigVersion
			public string cardName;
			public string characterName;
		}

		public enum Error
		{
			NoError,
			FileNotFound,
			CommandFailed,
			UnrecognizedStructure,
			Unknown,
		}

		private static SQLiteConnection ConnectToDB()
		{
			string dbFilename = AppSettings.FaradayLink.Location;
			if (string.IsNullOrWhiteSpace(dbFilename))
				dbFilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
				"faraday-canary", 
				"db.ginger.sqlite"); // Fake database during testing

			if (File.Exists(dbFilename) == false)
				throw new FileNotFoundException();

			AppSettings.FaradayLink.Location = dbFilename;
			return new SQLiteConnection($"Data Source={dbFilename}; Version=3;Foreign Keys=True;");
		}

		public static Error GetCharacters(out CharacterInstance[] characters)
		{
			try
			{
				using (var connection = ConnectToDB())
				{
					connection.Open();

					var result = new List<CharacterInstance>();

					// Fetch character instance ids
					var cmdReadCharacterIds = connection.CreateCommand();
					cmdReadCharacterIds.CommandText =
						@"
						SELECT CharacterConfig.id, CharacterConfigVersion.id, CharacterConfigVersion.displayName, CharacterConfigVersion.name 
						FROM CharacterConfig
						INNER JOIN CharacterConfigVersion ON CharacterConfigVersion.characterConfigId = CharacterConfig.id
						WHERE isUserControlled=0;";

					var characterInstanceIds = new HashSet<string>();
					using (var reader = cmdReadCharacterIds.ExecuteReader())
					{
						while (reader.Read())
						{
							string instanceId = reader.GetString(0);
							string configId = reader.GetString(1);
							string displayName = reader.GetString(2);
							string name = reader.GetString(3);

							if (string.IsNullOrEmpty(instanceId) || string.IsNullOrEmpty(configId))
								continue;

							result.Add(new CharacterInstance() {
									instanceID = instanceId,
									configID = configId,
									cardName = displayName,
									characterName = name,
								});
						}
					}

					characters = result.ToArray();
					return Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				characters = null;
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				characters = null;
				return Error.CommandFailed;
			}
			catch
			{
				characters = null;
				return Error.Unknown;
			}
		}

		private static string[][] s_TableInfo = new string[][] {
			new string[] {"_AppCharacterLorebookItemToCharacterConfigVersion", 
				"A", "TEXT",
				"B", "TEXT",
			},
			new string[] {"_AppImageToCharacterConfigVersion", 
				"A", "TEXT",
				"B", "TEXT", 
			},
			new string[] {"_CharacterConfigToGroupConfig", 
				"A", "TEXT",
				"B", "TEXT",
			},
			new string[] {"AppCharacterLorebookItem", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"order", "TEXT",
				"key", "TEXT",
				"value", "TEXT",
			},
			new string[] {"AppImage", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"imageUrl", "TEXT",
				"label", "TEXT",
				"order", "INTEGER",
				"aspectRatio", "TEXT",
			},
			new string[] {"CharacterConfig", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"isUserControlled", "BOOLEAN",
				"isDefaultUserCharacter", "BOOLEAN",
				"isTemplateChar", "BOOLEAN",
			},
			new string[] {"CharacterConfigVersion", 
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
			new string[] {"Chat", 
				"id", "TEXT",
				"createdAt", "DATETIME",
				"updatedAt", "DATETIME", 
				"name", "TEXT",
				"context", "TEXT",
				"customDialogue", "TEXT",
				"canDeleteCustomDialogue", "BOOLEAN",
				"useForTelemetry", "BOOLEAN",
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
			},
			new string[] {"GroupConfig", 
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
		};

		public static Error EstablishLink()
		{
			try
			{
				using (var connection = ConnectToDB())
				{
					connection.Open();

					var result = new List<CharacterInstance>();

					// Check table names and columns
					for (int i = 0; i < s_TableInfo.Length; ++i)
					{
						string[] table = s_TableInfo[i];
						string[] expectedNames = new string[(table.Length - 1)/2];
						string[] expectedTypes = new string[(table.Length - 1)/2];
						string tableName = table[0];
						for (int j = 0; j < expectedNames.Length; ++j)
						{
							expectedNames[j] = table[j * 2 + 1];
							expectedTypes[j] = table[j * 2 + 2];
						}

						var cmdTable = connection.CreateCommand();
						cmdTable.CommandText = $"SELECT name, type FROM pragma_table_info('{tableName}')";
						
						var foundColumns = new List<KeyValuePair<string, string>>();
						using (var reader = cmdTable.ExecuteReader())
						{
							while (reader.Read())
							{
								foundColumns.Add(new KeyValuePair<string, string>(
									reader.GetString(0),
									reader.GetString(1)));
							}
						}

						if (foundColumns.Count < expectedNames.Length)
							return Error.UnrecognizedStructure;

						for (int j = 0; j < expectedNames.Length; ++j)
						{
							if (foundColumns.FindIndex(kvp => kvp.Key == expectedNames[j] && kvp.Value == expectedTypes[j]) == -1)
								return Error.UnrecognizedStructure;
						}
					}

					return Error.NoError;
				}
			}
			catch (FileNotFoundException e)
			{
				return Error.FileNotFound;
			}
			catch (SQLiteException e)
			{
				return Error.CommandFailed;
			}
			catch (Exception e)
			{
				return Error.Unknown;
			}
		}

	}
}
