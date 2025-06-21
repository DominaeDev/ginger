using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Ginger.Integration;

namespace Ginger
{
	using CharacterInstance = Backyard.CharacterInstance;
	using GroupInstance = Backyard.GroupInstance;
	using ChatInstance = Backyard.ChatInstance;
	using ChatParameters = Backyard.ChatParameters;
	using ChatStaging = Backyard.ChatStaging;
	using ImageInstance = Backyard.ImageInstance;
	using CharacterMessage = Backyard.CharacterMessage;

	/*public class ArchiveData
	{
		public FaradayCardV4[] characterCards;
		public List<Image> images;
		public List<Image> backgrounds;
		public List<Chat> chats;
		public UserData userInfo;
		public Image userPortrait;
		public string displayName;
		
		public class Image
		{
			public int characterIndex;
			public string filename;
			public byte[] data;
			public string ext { get { return Utility.GetFileExt(filename); } }
		}

		public class Chat
		{
			public string name;
			public string[] participants;
			public DateTime creationDate;
			public DateTime updateDate;
			public ChatHistory history;
			public ChatStaging staging = new ChatStaging();
			public ChatParameters parameters = new ChatParameters();
			public string backgroundName;
		}

		public bool hasModelSettings { get { return chats != null && chats.ContainsAny(c => c.parameters != null); } }
	}*/

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
			List<FaradayCardV4> lsCharacterCards = new List<FaradayCardV4>();
			List<BackupData.Image> images = new List<BackupData.Image>();
			List<BackupData.Image> backgrounds = new List<BackupData.Image>();
			List<BackupData.Image> userImages = new List<BackupData.Image>();
			Dictionary<string, BackupData.Chat> chats = new Dictionary<string, BackupData.Chat>();
			UserData userInfo = null;

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

							string entryPath = Path.GetDirectoryName(entry.FullName).Replace('\\', '/');
							string entryFullName = Path.GetFileName(entry.FullName);
							string entryName = Path.GetFileNameWithoutExtension(entry.FullName);
							string entryExt = Utility.GetFileExt(entry.FullName);
							
							// Read character png
							if (entryPath == "" && entryExt == "png")
							{
								long dataSize = entry.Length;
								if (dataSize > 0)
								{
									var dataStream = entry.Open();
									byte[] buffer = new byte[dataSize];
									dataStream.Read(buffer, 0, (int)dataSize);

									FileUtil.EmbeddedData data;
									if (FileUtil.ExtractJsonFromPNG(buffer, out data) == FileUtil.Error.NoError && data.faradayJson != null)
									{
										var characterCard = FaradayCardV4.FromJson(data.faradayJson);
										if (characterCard != null)
										{
											lsCharacterCards.Add(characterCard);

											images.Add(new BackupData.Image() {
												characterIndex = lsCharacterCards.Count - 1,
												filename = entryFullName,
												data = buffer,
											});
											continue;
										}
									}
								}
							}

							// Read chat log (Backyard json)
							if ((entryPath == "chats" || entryPath == "logs") && entryExt == "json")
							{
								long dataSize = entry.Length;
								if (dataSize > 0)
								{
									var dataStream = entry.Open();
									byte[] buffer = new byte[dataSize];
									dataStream.Read(buffer, 0, (int)dataSize);
									string chatJson = new string(Encoding.UTF8.GetChars(buffer));

									if (BackyardChatBackupV2.Validate(chatJson))
									{
										var chatBackup = BackyardChatBackupV2.FromJson(chatJson);
										if (chatBackup != null)
										{
											var chat = chatBackup.ToChat();
											if (chat.name == null)
												chat.name = ChatInstance.DefaultName;

											chats.TryAdd(entryName.ToLowerInvariant(), chat);
										}
									}
									else if (BackyardChatBackupV1.Validate(chatJson))
									{
										var chatBackup = BackyardChatBackupV1.FromJson(chatJson);
										if (chatBackup != null)
										{
											var chat = chatBackup.ToChat();
											if (chat.name == null)
												chat.name = ChatInstance.DefaultName;

											chats.TryAdd(entryName.ToLowerInvariant(), chat);
										}
									}
								}
								continue;
							}

							// Read chat log (Ginger log)
							if ((entryPath == "chats" || entryPath == "logs") && entryExt == "log")
							{
								long dataSize = entry.Length;
								if (dataSize > 0)
								{
									var dataStream = entry.Open();
									byte[] buffer = new byte[dataSize];
									dataStream.Read(buffer, 0, (int)dataSize);
									string chatJson = new string(Encoding.UTF8.GetChars(buffer));

									if (GingerChatV2.Validate(chatJson))
									{
										entryName = Path.GetFileNameWithoutExtension(entryName);
										var chat = GingerChatV2.FromJson(chatJson);
										if (chat != null)
											chats.Set(entryName.ToLowerInvariant(), chat.ToBackupChat());
									}
									else if (GingerChatV1.Validate(chatJson))
									{
										entryName = Path.GetFileNameWithoutExtension(entryName);
										var chat = GingerChatV1.FromJson(chatJson);
										if (chat != null)
											chats.Set(entryName.ToLowerInvariant(), chat.ToBackupChat());
									}
								}
								continue;
							}

							// Images
							if (entryPath.BeginsWith("images") && Utility.IsSupportedImageFileExt(entryExt))
							{
								int characterIndex = 0;

								if (entryPath.Length >= 7)
								{
									string imagePath = entryPath.Substring(7);

									if (imagePath.Length > 0)
									{
										int pos_slash = imagePath.IndexOf('/');
										if (pos_slash != -1)
											int.TryParse(imagePath.Substring(0, pos_slash), out characterIndex);
										else
											int.TryParse(imagePath, out characterIndex);
									}
								}

								long dataSize = entry.Length;
								if (dataSize > 0)
								{
									var dataStream = entry.Open();
									byte[] buffer = new byte[dataSize];
									dataStream.Read(buffer, 0, (int)dataSize);
									images.Add(new BackupData.Image() {
										characterIndex = characterIndex,
										filename = entryFullName,
										data = buffer,
									});
								}
								continue;
							}

							// Backgrounds
							if (entryPath == "backgrounds" && Utility.IsSupportedImageFileExt(entryExt))
							{
								long dataSize = entry.Length;
								if (dataSize > 0)
								{
									var dataStream = entry.Open();
									byte[] buffer = new byte[dataSize];
									dataStream.Read(buffer, 0, (int)dataSize);
									backgrounds.Add(new BackupData.Image() {
										filename = entryFullName,
										data = buffer,
									});
								}
								continue;
							}

							// User info
							if (entryPath == "user" && entryExt == "json")
							{
								long dataSize = entry.Length;
								if (dataSize > 0)
								{
									var dataStream = entry.Open();
									byte[] buffer = new byte[dataSize];
									dataStream.Read(buffer, 0, (int)dataSize);
									string chatJson = new string(Encoding.UTF8.GetChars(buffer));

									if (UserData.Validate(chatJson))
										userInfo = UserData.FromJson(chatJson);
								}
								continue;
							}

							// User portrait
							if (entryPath == "user" && Utility.IsSupportedImageFileExt(entryExt))
							{
								long dataSize = entry.Length;
								if (dataSize > 0)
								{
									var dataStream = entry.Open();
									byte[] buffer = new byte[dataSize];
									dataStream.Read(buffer, 0, (int)dataSize);
									userImages.Add(new BackupData.Image() {
										filename = entryFullName,
										data = buffer,
									});
								}
							}
						}
					}
				}
				
				if (lsCharacterCards.IsEmpty())
				{
					backup = default(BackupData);
					return FileUtil.Error.NoDataFound;
				}

				if (images.Count > 0)
				{
					for (int i = 0; i < lsCharacterCards.Count; ++i)
					{
						int portraitIndex = images.FindIndex(img => img.characterIndex == i);
						if (portraitIndex == -1)
							continue;

						// images/portrait.*, images/image_00.* supercedes png.
						int idxPortrait = images.FindIndex(img => {
							string fn = Path.GetFileNameWithoutExtension(img.filename).ToLowerInvariant();
							return (img.characterIndex == i) && (fn == "portrait" || fn == "image_00");
						});

						if (idxPortrait > portraitIndex)
						{
							// Move to front (remove existing)
							var image = images[idxPortrait];
							images.RemoveAt(idxPortrait);
							images.RemoveAt(portraitIndex);
							images.Insert(portraitIndex, image);
						}
					}
				}

				backup = new BackupData() {
					characterCards = lsCharacterCards.ToArray(),
					displayName = lsCharacterCards[0].data.displayName,
					chats = chats.Values.ToList(),
					images = images,
					backgrounds = backgrounds,
					userInfo = userInfo,
					userPortrait = userImages.FirstOrDefault(),
				};
				return FileUtil.Error.NoError;
			}
			catch
			{
				backup = default(BackupData);
				return FileUtil.Error.FileReadError;
			}
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
				characterData.images = backup.images.Where(i => bSoloCharacter || i.characterIndex == iChar || (iChar == 0 && i.characterIndex < 0)).ToArray();

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
				});

				if (string.IsNullOrEmpty(backupChat.backgroundName) == false && backup.backgrounds != null)
				{
					int bgIndex = backup.backgrounds.FindIndex(b => Path.GetFileNameWithoutExtension(b.filename) == backupChat.backgroundName);
					if (bgIndex != -1)
					{
						var background = backup.backgrounds[bgIndex];
						scenario.backgroundImage = string.Format("scenarios/backgrounds/{0}.{1}", scenarioData.id, background.ext);
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
							var bgEntry = zip.CreateEntry(string.Format("scenarios/backgrounds/{0}.{1}", scenario.id, scenario.backgroundImage.ext), CompressionLevel.NoCompression);
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
