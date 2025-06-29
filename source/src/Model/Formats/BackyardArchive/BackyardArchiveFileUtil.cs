using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Ginger.Integration;

namespace Ginger
{
	public static class BackyardArchiveUtil
	{
		private class CharacterData
		{
			public string id;
			public string jsonData;
			public BackupData.Image[] images;
		}

		private class ScenarioData
		{
			public string id;
			public string jsonData;
			public BackupData.Image backgroundImage;
		}

		public static FileUtil.Error ReadArchive(string filename, out BackupData backup)
		{
			ByafManifestV1 manifest = null;
			var lsCharacterData = new Dictionary<string, ByafCharacterV1>();
			var lsScenarioData = new Dictionary<string, ByafScenarioV1>();
			List<BackupData.Image> lsImageData = new List<BackupData.Image>();
			
			try
			{
				using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
					{
						foreach (var entry in archive.Entries)
						{
							if (entry.Name == "")
								continue; // Skip folder entries

							string entryFullName = entry.FullName.Replace('\\', '/');
							string entryExt = Utility.GetFileExt(entry.FullName);
							long dataSize = entry.Length;

							if (entryExt == "json")
							{
								// Read manifest
								using (var dataStream = entry.Open())
								{
									byte[] buffer = new byte[dataSize];
									dataStream.Read(buffer, 0, (int)dataSize);
									string json = new string(Encoding.UTF8.GetChars(buffer));

									if (entryFullName == "manifest.json")
									{
										manifest = ByafManifestV1.FromJson(json);
										if (manifest == null)
										{
											backup = default(BackupData);
											return FileUtil.Error.InvalidJson;
										}
									}
									else
									{
										if (ByafCharacterV1.Validate(json))
										{
											ByafCharacterV1 character = ByafCharacterV1.FromJson(json);
											if (character != null)
												lsCharacterData.Add(entryFullName, character);
											else
											{
												backup = default(BackupData);
												return FileUtil.Error.InvalidJson;
											}
										}
										else if (ByafScenarioV1.Validate(json))
										{
											ByafScenarioV1 scenario = ByafScenarioV1.FromJson(json);
											if (scenario != null)
												lsScenarioData.Add(entryFullName, scenario);
											else
											{
												backup = default(BackupData);
												return FileUtil.Error.InvalidJson;
											}
										}
									}
								}
							}
							else if (Utility.IsSupportedImageFileExt(entryExt))
							{
								// Read manifest
								using (var dataStream = entry.Open())
								{
									byte[] buffer = new byte[dataSize];
									if (dataStream.Read(buffer, 0, (int)dataSize) == (int)dataSize)
									{
										lsImageData.Add(new BackupData.Image() {
											filename = entryFullName,
											data = buffer,
										});
									}
								}
							}
						}
					}
				}
			}
			catch
			{
				backup = default(BackupData);
				return FileUtil.Error.FileReadError;
			}
			
			if (lsCharacterData.IsEmpty() || lsScenarioData.IsEmpty() || manifest == null || manifest.characters.IsEmpty() || manifest.scenarios.IsEmpty())
			{
				backup = default(BackupData);
				return FileUtil.Error.NoDataFound;
			}

			var cards = new List<FaradayCardV4>();
			var chats = new List<BackupData.Chat>();
			var images = new List<BackupData.Image>();
			var backgrounds = new List<BackupData.Image>();

			// Read characters
			for (int i = 0; i < manifest.characters.Length; ++i)
			{
				ByafCharacterV1 character;
				if (lsCharacterData.TryGetValue(manifest.characters[i], out character))
				{
					cards.Add(character.ToFaradayCard());
					string characterPath = Path.GetDirectoryName(manifest.characters[i]);
					foreach (var imagePath in character.images.Select(img => img.path))
					{
						var image = lsImageData.Find(img => img.filename == Path.Combine(characterPath, imagePath).Replace('\\', '/'));
						if (image != null)
						{
							images.Add(new BackupData.Image() {
								filename = Path.GetFileName(image.filename),
								characterIndex = i,
								data = image.data,
							});
						}
					}
				}
				else
				{
					backup = default(BackupData);
					return FileUtil.Error.InvalidData;
				}
			}

			// Read scenarios
			for (int i = 0; i < manifest.scenarios.Length; ++i)
			{
				ByafScenarioV1 scenario;
				if (lsScenarioData.TryGetValue(manifest.scenarios[i], out scenario))
				{
					chats.Add(scenario.ToChat());
					if (string.IsNullOrEmpty(scenario.backgroundImage) == false)
					{
						var backgroundImage = lsImageData.Find(img => !img.filename.BeginsWith("characters/") 
							&& Path.GetFileName(img.filename) == Path.GetFileName(scenario.backgroundImage));
						if (backgroundImage != null)
						{
							backgrounds.Add(new BackupData.Image() {
								filename = Path.GetFileName(backgroundImage.filename),
								data = backgroundImage.data,
							});
						}
					}
				}
				else
				{
					backup = default(BackupData);
					return FileUtil.Error.InvalidData;
				}
			}

			backup = new BackupData() {
				characterCards = cards.ToArray(),
				displayName =  cards[0].data.displayName,
				chats = chats,
				images = images,
				backgrounds = backgrounds,
				userInfo = null,
			};
			return FileUtil.Error.NoError;
		}

		public static FileUtil.Error WriteArchive(string filename, BackupData backup)
		{
			var characters = new List<CharacterData>();
			var scenarios = new List<ScenarioData>();

			// Characters
			bool bSoloCharacter = backup.characterCards.Length < 2;
			for (int iChar = 0; iChar < backup.characterCards.Length; ++iChar)
			{
				var characterData = new CharacterData();
				characterData.id = string.Format("character{0}", iChar + 1);
				characterData.images = backup.images
					.Where(i => 
						i.data.IsEmpty() == false 
						&& (bSoloCharacter || i.characterIndex == iChar || (iChar == 0 && i.characterIndex < 0)))
					.ToArray();

				var character = ByafCharacterV1.FromFaradayCard(backup.characterCards[iChar]);
				character.id = characterData.id;

				character.images = new ByafCharacterV1.Image[characterData.images.Length];
				for (int i = 0; i < characterData.images.Length; ++i)
				{
					character.images[i] = new ByafCharacterV1.Image() {
						path = string.Format(i == 0 ? "images/portrait.{1}" : "images/image{0}.{1}", i, characterData.images[i].ext),
						label = i == 0 ? character.name : "",
					};
				}
				
				characterData.jsonData = character.ToJson();
				characters.Add(characterData);
			}

			// Scenarios (chats)
			int scenarioIndex = 1;
			foreach (var backupChat in backup.chats.OrderBy(c => c.creationDate))
			{
				var scenarioData = new ScenarioData();
				scenarioData.id = string.Format("scenario{0}", scenarioIndex++);

				var scenario = ByafScenarioV1.FromChat(new BackupData.Chat() {
					name = backupChat.name,
					creationDate = backupChat.creationDate,
					updateDate = backupChat.updateDate,
					staging = backupChat.staging,
					parameters = backupChat.parameters,
					history = ChatHistory.LegacyFix(backupChat.history),
				}, "character1");

				if (string.IsNullOrEmpty(backupChat.backgroundName) == false && backup.backgrounds.Count > 0)
				{
					int bgIndex = backup.backgrounds.FindIndex(b => Path.GetFileNameWithoutExtension(b.filename) == backupChat.backgroundName);
                    if (bgIndex == -1 && backupChat.backgroundName.BeginsWith("background_"))
					{
						if (int.TryParse(Path.GetFileNameWithoutExtension(backupChat.backgroundName.Substring(11)), out bgIndex))
							bgIndex--; // 1-based
						else
							bgIndex = -1;
					}
					if (bgIndex >= 0 && bgIndex < backup.backgrounds.Count)
					{
						var background = backup.backgrounds[bgIndex];
						scenario.backgroundImage = string.Format("scenarios/{0}-background.{1}", scenarioData.id, background.ext);
						scenarioData.backgroundImage = background;
					}
				}

				scenarioData.jsonData = scenario.ToJson();
				scenarios.Add(scenarioData);
			}

			// Manifest
			ByafManifestV1 manifest = new ByafManifestV1();
			manifest.characters = characters.Select(c => string.Format("characters/{0}/character.json", c.id)).ToArray();
			manifest.scenarios = scenarios.Select(s => string.Format("scenarios/{0}.json", s.id)).ToArray();

			if (string.IsNullOrEmpty(backup.characterCards[0].hubAuthorUsername) == false)
			{
				manifest.author = new ByafManifestV1.Author() {
					name = backup.characterCards[0].hubAuthorUsername,
					backyardURL = string.Concat("https://backyard.ai/hub/user/", backup.characterCards[0].hubAuthorUsername),
				};
			}
			else if (string.IsNullOrWhiteSpace(backup.characterCards[0].creator) == false)
			{
				manifest.author = new ByafManifestV1.Author() {
					name = backup.characterCards[0].creator,
					backyardURL = "https://backyard.ai/hub/",
				};
			}

			try
			{ 
				// Create zip archive
				var intermediateFilename = Path.GetTempFileName();
				using (ZipArchive zip = ZipFile.Open(intermediateFilename, ZipArchiveMode.Update, Encoding.ASCII))
				{
					// Write manifest.json
					var manifestEntry = zip.CreateEntry("manifest.json", CompressionLevel.NoCompression);
					using (Stream writer = manifestEntry.Open())
					{
						byte[] textBuffer = Encoding.UTF8.GetBytes(manifest.ToJson());
						writer.Write(textBuffer, 0, textBuffer.Length);
					}

					// Write character entries
					foreach (var character in characters)
					{
						var characterJsonEntry = zip.CreateEntry(string.Format("characters/{0}/character.json", character.id), CompressionLevel.NoCompression);
						using (Stream writer = characterJsonEntry.Open())
						{
							byte[] textBuffer = Encoding.UTF8.GetBytes(character.jsonData);
							writer.Write(textBuffer, 0, textBuffer.Length);
						}

						// Write character images
						int imageIndex = 0;
						foreach (var image in character.images)
						{
							string entryPath = string.Format(imageIndex == 0 ? "characters/{0}/images/portrait.{2}" : "characters/{0}/images/image{1}.{2}", character.id, imageIndex++, image.ext);
							var imageEntry = zip.CreateEntry(entryPath, CompressionLevel.NoCompression);
							using (Stream writer = imageEntry.Open())
							{
								writer.Write(image.data, 0, image.data.Length);
							}
						}
					}

					// Write scenario entries
					foreach (var scenario in scenarios)
					{
						var scenarioJsonEntry = zip.CreateEntry(string.Format("scenarios/{0}.json", scenario.id), CompressionLevel.NoCompression);
						using (Stream writer = scenarioJsonEntry.Open())
						{
							byte[] textBuffer = Encoding.UTF8.GetBytes(scenario.jsonData);
							writer.Write(textBuffer, 0, textBuffer.Length);
						}

						// Write background
						if (scenario.backgroundImage != null)
						{ 
							var bgEntry = zip.CreateEntry(string.Format("scenarios/{0}-background.{1}", scenario.id, scenario.backgroundImage.ext), CompressionLevel.NoCompression);
							using (Stream writer = bgEntry.Open())
							{
								writer.Write(scenario.backgroundImage.data, 0, scenario.backgroundImage.data.Length);
							}
						}
					}

				}

				// Rename Temporaty file to Target file
				if (File.Exists(filename))
					File.Delete(filename);
				File.Move(intermediateFilename, filename);
				return FileUtil.Error.NoError;
			}
			catch
			{
				return FileUtil.Error.UnknownError;
			}
		}

	
	}
}
