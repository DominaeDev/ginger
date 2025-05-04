using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Ginger.Integration
{
	using CharacterInstance = Backyard.CharacterInstance;
	using GroupInstance = Backyard.GroupInstance;
	using ChatInstance = Backyard.ChatInstance;
	using ChatParameters = Backyard.ChatParameters;
	using ChatStaging = Backyard.ChatStaging;
	using ImageInstance = Backyard.ImageInstance;
	using CharacterMessage = Backyard.CharacterMessage;

	public class BackupData
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
	}

	public static class BackupUtil
	{
		public static Backyard.Error CreateBackup(CharacterInstance characterInstance, out BackupData backupInfo)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				backupInfo = null;
				return Backyard.Error.NotConnected;
			}

			BackyardLinkCard card = null;
			ImageInstance[] images;
			ChatInstance[] chatInstances = null;
			UserData userInfo;
			var error = Backyard.Database.ImportCharacter(characterInstance.instanceId, out card, out images, out userInfo);
			if (error != Backyard.Error.NoError)
			{
				backupInfo = null;
				return error;
			}

			if (characterInstance.groupId != null)
			{
				error = Backyard.Database.GetChats(characterInstance.groupId, out chatInstances);
				if (error != Backyard.Error.NoError)
				{
					backupInfo = null;
					return error;
				}
			}

			string[] imageUrls = images
				.Where(i => i.imageType == AssetFile.AssetType.Icon)
				.Select(i => i.imageUrl)
				.ToArray();
			string[] backgroundUrls = images
				.Where(i => i.imageType == AssetFile.AssetType.Background)
				.Select(i => i.imageUrl)
				.ToArray();
			string userImageUrl = images
				.Where(i => i.imageType == AssetFile.AssetType.UserIcon)
				.Select(i => i.imageUrl)
				.FirstOrDefault();

			if (AppSettings.BackyardLink.BackupUserPersona == false)
			{
				// Strip user persona
				userInfo = null;
				userImageUrl = null;
			}

			// Create backup
			backupInfo = new BackupData();
			backupInfo.characterCards = new FaradayCardV4[] { card.ToFaradayCard() };
			backupInfo.displayName = card.data.displayName;
			backupInfo.userInfo = userInfo;
			if (chatInstances != null)
			{
				backupInfo.chats = chatInstances
					.Select(c => {
						string bgName = null;
						if (c.hasBackground)
						{
							int index = Array.FindIndex(backgroundUrls, url => string.Compare(url, c.staging.background.imageUrl, StringComparison.OrdinalIgnoreCase) == 0);
							if (index != -1)
								bgName = string.Format("background_{0:00}.{1}", index + 1, Utility.GetFileExt(c.staging.background.imageUrl));
						}

						List<CharacterInstance> participants = c.participants
							.Select(id => Backyard.Database.GetCharacter(id))
							.OrderBy(cc => cc.isCharacter)
							.ToList();

						return new BackupData.Chat() {
							name = c.name,
							participants = participants.Select(cc => cc.name).ToArray(),

							history = c.history,
							staging = c.staging,
							parameters = AppSettings.BackyardLink.BackupModelSettings ? c.parameters : null,
							creationDate = c.creationDate,
							updateDate = c.updateDate,
							backgroundName = bgName,
						};
					})
					.ToList();
			}
			else
				backupInfo.chats = new List<BackupData.Chat>();
			
			// Images
			backupInfo.images = imageUrls.Select(url => new BackupData.Image() { 
				characterIndex = 0,
				filename = Path.GetFileName(url),
				data = Utility.LoadFile(url),
			}).ToList();

			// Background images
			backupInfo.backgrounds = backgroundUrls.Select(url => new BackupData.Image() {
				filename = Path.GetFileName(url),
				data = Utility.LoadFile(url),
			}).ToList();

			// User image
			if (string.IsNullOrEmpty(userImageUrl) == false && File.Exists(userImageUrl))
			{
				string imageName = string.Concat("user.", Utility.GetFileExt(userImageUrl)); // user.png
				backupInfo.userInfo.image = imageName;
				backupInfo.userPortrait = new BackupData.Image() {
					filename = imageName,
					data = Utility.LoadFile(userImageUrl),
				};
			}

			return Backyard.Error.NoError;
		}

		public static Backyard.Error CreateBackup(GroupInstance groupInstance, out BackupData backupInfo)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				backupInfo = null;
				return Backyard.Error.NotConnected;
			}

			if (BackyardValidation.CheckFeature(BackyardValidation.Feature.GroupChat) == false)
			{
				backupInfo = null;
				return Backyard.Error.UnsupportedFeature;
			}

			BackyardLinkCard[] cards = null;
			ImageInstance[] images;
			CharacterInstance[] characterInstances;
			UserData userInfo;
			var error = Backyard.Database.ImportParty(groupInstance.instanceId, out cards, out characterInstances, out images, out userInfo);
			if (error != Backyard.Error.NoError)
			{
				backupInfo = null;
				return error;
			}

			ChatInstance[] chatInstances = null;
			error = Backyard.Database.GetChats(groupInstance.instanceId, out chatInstances);
			if (error != Backyard.Error.NoError)
			{
				backupInfo = null;
				return error;
			}

			var imageUrls = images
				.Where(i => i.imageType == AssetFile.AssetType.Icon)
				.Select(i => new {
					index = Array.FindIndex(characterInstances, c => c.instanceId == i.associatedInstanceId),
					url = i.imageUrl,
				}).ToArray();
			string[] backgroundUrls = images
				.Where(i => i.imageType == AssetFile.AssetType.Background)
				.Select(i => i.imageUrl)
				.ToArray();
			string userImageUrl = images
				.Where(i => i.imageType == AssetFile.AssetType.UserIcon)
				.Select(i => i.imageUrl)
				.FirstOrDefault();

			if (AppSettings.BackyardLink.BackupUserPersona == false)
			{
				// Strip user persona
				userInfo = null;
				userImageUrl = null;
			}

			// Create backup
			backupInfo = new BackupData();
			backupInfo.characterCards = cards.Select(c => c.ToFaradayCard()).ToArray();
			backupInfo.displayName = cards[0].data.displayName;
			backupInfo.userInfo = userInfo;
			if (chatInstances != null)
			{
				backupInfo.chats = chatInstances
					.Select(c => {
						string bgName = null;
						if (c.hasBackground)
						{
							int index = Array.FindIndex(backgroundUrls, url => string.Compare(url, c.staging.background.imageUrl, StringComparison.OrdinalIgnoreCase) == 0);
							if (index != -1)
								bgName = string.Format("background_{0:00}.{1}", index + 1, Utility.GetFileExt(c.staging.background.imageUrl));
						}

						List<CharacterInstance> participants = c.participants
							.Select(id => Backyard.Database.GetCharacter(id))
							.OrderBy(cc => cc.isCharacter)
							.ToList();

						return new BackupData.Chat() {
							name = c.name,
							participants = participants.Select(cc => cc.name).ToArray(),

							history = c.history,
							staging = c.staging,
							parameters = AppSettings.BackyardLink.BackupModelSettings ? c.parameters : null,
							creationDate = c.creationDate,
							updateDate = c.updateDate,
							backgroundName = bgName,
						};
					})
					.ToList();
			}
			else
				backupInfo.chats = new List<BackupData.Chat>();
			
			// Images
			backupInfo.images = imageUrls
				.Select(i => new BackupData.Image() { 
					characterIndex = i.index,
					filename = Path.GetFileName(i.url),
					data = Utility.LoadFile(i.url),
				})
				.ToList();

			// Background images
			backupInfo.backgrounds = backgroundUrls
				.Distinct()
				.Select(url => new BackupData.Image() {
					filename = Path.GetFileName(url),
					data = Utility.LoadFile(url),
				})
				.ToList();

			// User image
			if (string.IsNullOrEmpty(userImageUrl) == false && File.Exists(userImageUrl))
			{
				string imageName = string.Concat("user.", Utility.GetFileExt(userImageUrl)); // user.png
				backupInfo.userInfo.image = imageName;
				backupInfo.userPortrait = new BackupData.Image() {
					filename = imageName,
					data = Utility.LoadFile(userImageUrl),
				};
			}

			return Backyard.Error.NoError;
		}

		private class CharData
		{
			public string name;
			public Image portraitImage = null;
			public byte[] nonPNGImageData = null;
			public byte[] cardBytes = null;
			public string nonPNGImageExt = "png";
		}

		public static FileUtil.Error WriteBackup(string filename, BackupData backup)
		{
			var lsCharData = new List<CharData>();

			for (int iChar = 0; iChar < backup.characterCards.Length; ++iChar)
			{
				var intermediateCardFilename = Path.GetTempFileName();
				var charData = new CharData();
				charData.name = Utility.FirstNonEmpty(backup.characterCards[iChar].data.name, Constants.DefaultCharacterName);

				// Write png file
				try
				{
					var charImages = backup.images.Where(i => i.characterIndex == iChar).ToList();
					if (charImages.Count > 0) // Convert portrait
					{
						string ext = Utility.GetFileExt(charImages[0].filename);
						if (Utility.IsWebP(charImages[0].data))
						{
							charData.nonPNGImageData = charImages[0].data;
							charData.nonPNGImageExt = "webp";
							Utility.LoadImageFromMemory(charImages[0].data, out charData.portraitImage);
						}
						else if (ext == "png")
						{
							Utility.LoadImageFromMemory(charImages[0].data, out charData.portraitImage);
						}
						else
						{
							charData.nonPNGImageData = charImages[0].data;
							charData.nonPNGImageExt = ext;
							Utility.LoadImageFromMemory(charImages[0].data, out charData.portraitImage);
						}
					}

					if (charData.portraitImage == null)
						charData.portraitImage = DefaultPortrait.Image;

					// Write character card (png)
					using (var stream = new FileStream(intermediateCardFilename, FileMode.OpenOrCreate, FileAccess.Write))
					{
						if (charData.portraitImage.RawFormat.Equals(ImageFormat.Png) == false) // Convert to PNG
						{
							using (Image bmpNewImage = new Bitmap(charData.portraitImage.Width, charData.portraitImage.Height))
							{
								Graphics gfxNewImage = Graphics.FromImage(bmpNewImage);
								gfxNewImage.DrawImage(charData.portraitImage, new Rectangle(0, 0, bmpNewImage.Width, bmpNewImage.Height),
									0, 0,
									charData.portraitImage.Width, charData.portraitImage.Height,
									GraphicsUnit.Pixel);
								gfxNewImage.Dispose();
								bmpNewImage.Save(stream, ImageFormat.Png);
							}
						}
						else
						{
							charData.portraitImage.Save(stream, ImageFormat.Png);
						}
					}

					// Write json to PNG
					string faradayJson = backup.characterCards[iChar].ToJson();
					var faradayBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(faradayJson));
					if (FileUtil.WriteExifMetaData(intermediateCardFilename, faradayBase64) == false)
						return FileUtil.Error.FileWriteError; // Error

					// Read in temporary file
					byte[] data = Utility.LoadFile(intermediateCardFilename);
					if (data == null)
						return FileUtil.Error.FileReadError;
					charData.cardBytes = data;

					// Delete temporary file
					File.Delete(intermediateCardFilename);

					lsCharData.Add(charData);
				}
				catch (IOException e)
				{
					if (e.HResult == Win32.HR_ERROR_DISK_FULL || e.HResult == Win32.HR_ERROR_HANDLE_DISK_FULL)
						return FileUtil.Error.DiskFullError;
					return FileUtil.Error.FileWriteError;
				}
				catch (Exception e)
				{
					File.Delete(intermediateCardFilename);
					return FileUtil.Error.UnknownError;
				}
			}

			try
			{
				// Write chat logs
				List<KeyValuePair<string, string>> chats = new List<KeyValuePair<string, string>>();
				foreach (var backupChat in backup.chats.OrderBy(c => c.creationDate))
				{
					// Backyard-compatible format
					{
						string chatFilename = string.Format("chatLog_{0}_{1}.json", backup.displayName, backupChat.creationDate.ToUnixTimeSeconds()).Replace(" ", "_");

						var chat = BackyardChatBackupV2.FromChat(new BackupData.Chat() {
							name = backupChat.name,
							creationDate = backupChat.creationDate,
							updateDate = backupChat.updateDate,
							staging = backupChat.staging,
							parameters = backupChat.parameters,
							backgroundName = backupChat.backgroundName,
							history = ChatHistory.LegacyFix(backupChat.history),
						});
						string json = chat.ToJson();
						if (json != null)
							chats.Add(new KeyValuePair<string, string>(chatFilename, json));
					}

					// Ginger format
					{
						string chatFilename = string.Format("chatLog_{0}_{1}.log", backup.displayName, backupChat.creationDate.ToUnixTimeSeconds()).Replace(" ", "_");

						var gingerChat = GingerChatV2.FromBackup(new BackupData.Chat() {
							name = backupChat.name,
							participants = backupChat.participants,
							creationDate = backupChat.creationDate,
							updateDate = backupChat.updateDate,
							staging = backupChat.staging,
							parameters = backupChat.parameters,
							backgroundName = backupChat.backgroundName,
							history = ChatHistory.LegacyFix(backupChat.history),
						});

						string json = gingerChat.ToJson();
						if (json != null)
							chats.Add(new KeyValuePair<string, string>(chatFilename, json));
					}
				}

				// Create zip archive
				bool bSoloCharacter = lsCharData.Count == 1;
				var intermediateFilename = Path.GetTempFileName();
				using (ZipArchive zip = ZipFile.Open(intermediateFilename, ZipArchiveMode.Update, Encoding.ASCII))
				{
					for (int iCard = 0; iCard < lsCharData.Count; ++iCard)
					{
						var charData = lsCharData[iCard];

						// Write card json (UTF8)
						string cardEntryName = bSoloCharacter ? $"card.png" : $"card_{iCard:00}_{charData.name}.png";
						var cardEntry = zip.CreateEntry(cardEntryName, CompressionLevel.NoCompression);
						using (Stream writer = cardEntry.Open())
						{
							writer.Write(charData.cardBytes, 0, charData.cardBytes.Length);
						}

						// Write non-png portrait (situational)
						if (charData.nonPNGImageData != null && charData.nonPNGImageData.Length > 0)
						{
							string entryName = bSoloCharacter ? $"images/portrait.{charData.nonPNGImageExt}" : $"images/{iCard:00}/portrait.{charData.nonPNGImageExt}";
							var fileEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
							using (Stream writer = fileEntry.Open())
							{
								for (long n = charData.nonPNGImageData.Length; n > 0;)
								{
									int length = (int)Math.Min(n, (long)int.MaxValue);
									writer.Write(charData.nonPNGImageData, 0, length);
									n -= (long)length;
								}
							}
						}

						// Write image files
						var charImages = backup.images.Where(i => i.characterIndex == iCard).ToList();
						for (int i = 1; i < charImages.Count; ++i)
						{
							if (charImages[i].data == null || charImages[i].data.Length == 0)
								continue; // No file data

							string ext = Utility.GetFileExt(charImages[i].filename);
							string entryName = bSoloCharacter ? $"images/image_{i:00}.{ext}" : $"images/{iCard:00}/image_{i:00}.{ext}";
							var fileEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
							using (Stream writer = fileEntry.Open())
							{
								for (long n = charImages[i].data.Length; n > 0;)
								{
									int length = (int)Math.Min(n, (long)int.MaxValue);
									writer.Write(charImages[i].data, 0, length);
									n -= (long)length;
								}
							}
						}
					}

					// Write background files
					for (int i = 0; i < backup.backgrounds.Count; ++i)
					{
						if (backup.backgrounds[i].data == null || backup.backgrounds[i].data.Length == 0)
							continue; // No file data

						string ext = Utility.GetFileExt(backup.backgrounds[i].filename);
						string entryName = string.Format("backgrounds/background_{0:00}.{1}", i + 1, ext);
						var fileEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
						using (Stream writer = fileEntry.Open())
						{
							for (long n = backup.backgrounds[i].data.Length; n > 0;)
							{
								int length = (int)Math.Min(n, (long)int.MaxValue);
								writer.Write(backup.backgrounds[i].data, 0, length);
								n -= (long)length;
							}
						}
					}

					// Write chat files
					for (int i = 0; i < chats.Count; ++i)
					{
						string entryName = string.Concat("logs/", Utility.ValidFilename(chats[i].Key));
						var fileEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
						using (Stream writer = fileEntry.Open())
						{
							byte[] textBuffer = Encoding.UTF8.GetBytes(chats[i].Value);
							writer.Write(textBuffer, 0, textBuffer.Length);
						}
					}

					// Write user data
					if (backup.userInfo != null)
					{
						// User info
						if (string.IsNullOrEmpty(backup.userInfo.persona) == false)
						{
							string entryName = string.Concat("user/user.json");
							var fileEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
							using (Stream writer = fileEntry.Open())
							{
								byte[] textBuffer = Encoding.UTF8.GetBytes(backup.userInfo.ToJson());
								writer.Write(textBuffer, 0, textBuffer.Length);
							}
						}

						// User portrait
						if (backup.userPortrait != null && backup.userPortrait.data != null && backup.userPortrait.data.Length > 0)
						{
							string entryName = string.Concat("user/", backup.userPortrait.filename);
							var fileEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
							using (Stream writer = fileEntry.Open())
							{
								for (long n = backup.userPortrait.data.Length; n > 0;)
								{
									int length = (int)Math.Min(n, (long)int.MaxValue);
									writer.Write(backup.userPortrait.data, 0, length);
									n -= (long)length;
								}
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

		public static FileUtil.Error ReadBackup(string filename, out BackupData backup)
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

		public static bool CheckIfGroup(string filename, out int count)
		{
			try
			{
				count = 0;
				using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
					{
						foreach (var entry in archive.Entries)
						{
							if (entry.Name == "")
								continue; // Skip folder entries

							string entryPath = Path.GetDirectoryName(entry.FullName).Replace('\\', '/');
							string entryExt = Utility.GetFileExt(entry.FullName);

							// Read character png
							if (entryPath == "" && entryExt == "png")
								count++;
						}
					}
				}

				return count > 1;
			}
			catch
			{
				count = 0;
				return false;
			}
		}

		public static BackupData.Chat[] SplitAltGreetings(BackyardLinkCard card, Generator.Output output, Backyard.ImageInput[] images)
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
						greeting = CharacterMessage.FromString(altGreeting),
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
	}
}
