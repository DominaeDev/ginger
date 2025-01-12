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
	public class BackupData
	{
		public FaradayCardV4 characterCard;
		public List<Image> images;
		public List<Image> backgrounds;
		public List<Chat> chats;
		public UserData userInfo;
		public Image userPortrait;
		
		public class Image
		{
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

		public bool hasParameters { get { return chats != null && chats.ContainsAny(c => c.parameters != null); } }
	}

	public static class BackupUtil
	{
		public static Backyard.Error CreateBackup(CharacterInstance characterInstance, out BackupData backupInfo)
		{
			FaradayCardV4 card = null;
			ImageInstance[] images;
			ChatInstance[] chatInstances = null;
			UserData userInfo;
			var error = Backyard.ImportCharacter(characterInstance, out card, out images, out userInfo);
			if (error != Backyard.Error.NoError)
			{
				backupInfo = null;
				return error;
			}

			if (characterInstance.groupId != null)
			{
				error = Backyard.GetChats(characterInstance.groupId, out chatInstances);
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
			backupInfo.characterCard = card;
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

						List<CharacterInstance> participants = new List<CharacterInstance>(4);
						participants.Add(c.participants.Select(id => Backyard.GetCharacter(id)).FirstOrDefault(cc => cc.isUser)); // User first
						participants.AddRange(c.participants.Select(id => Backyard.GetCharacter(id)).Where(cc => cc.isUser == false));

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

		public static FileUtil.Error WriteBackup(string filename, BackupData backup)
		{
			byte[] nonPNGImageData = null;
			string nonPNGImageExt = "png";
			var intermediateCardFilename = Path.GetTempFileName();

			// Write png file
			try
			{
				Image portraitImage = null;
				if (backup.images.Count > 0) // Convert portrait
				{
					string ext = Utility.GetFileExt(backup.images[0].filename);
					if (Utility.IsWebP(backup.images[0].data))
					{
						nonPNGImageData = backup.images[0].data;
						nonPNGImageExt = "webp";
						Utility.LoadImageFromMemory(backup.images[0].data, out portraitImage);
					}
					else if (ext == "png")
					{
						Utility.LoadImageFromMemory(backup.images[0].data, out portraitImage);
					}
					else
					{
						nonPNGImageData = backup.images[0].data;
						nonPNGImageExt = ext;
						Utility.LoadImageFromMemory(backup.images[0].data, out portraitImage);
					}
				}
				
				if (portraitImage == null)
					portraitImage = DefaultPortrait.Image;

				// Write character card (png)
				using (var stream = new FileStream(intermediateCardFilename, FileMode.OpenOrCreate, FileAccess.Write))
				{
					if (portraitImage.RawFormat.Equals(ImageFormat.Png) == false) // Convert to PNG
					{
						using (Image bmpNewImage = new Bitmap(portraitImage.Width, portraitImage.Height))
						{
							Graphics gfxNewImage = Graphics.FromImage(bmpNewImage);
							gfxNewImage.DrawImage(portraitImage, new Rectangle(0, 0, bmpNewImage.Width, bmpNewImage.Height),
													0, 0,
													portraitImage.Width, portraitImage.Height,
													GraphicsUnit.Pixel);
							gfxNewImage.Dispose();
							bmpNewImage.Save(stream, ImageFormat.Png);
						}
					}
					else
					{
						portraitImage.Save(stream, ImageFormat.Png);
					}
				}
			}
			catch (IOException e)
			{
				if (e.HResult == Win32.HR_ERROR_DISK_FULL || e.HResult == Win32.HR_ERROR_HANDLE_DISK_FULL)
					return FileUtil.Error.DiskFullError;
				return FileUtil.Error.FileWriteError;
			}
			catch
			{
				File.Delete(intermediateCardFilename);
				return FileUtil.Error.NoError;
			}

			try
			{
				// Write json to PNG
				string faradayJson = backup.characterCard.ToJson();
				var faradayBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(faradayJson));
				if (FileUtil.WriteExifMetaData(intermediateCardFilename, faradayBase64) == false)
					return FileUtil.Error.FileWriteError; // Error

				// Read in temporary file
				byte[] cardBytes = Utility.LoadFile(intermediateCardFilename);
				if (cardBytes == null)
					return FileUtil.Error.FileReadError;

				// Delete temporary file
				File.Delete(intermediateCardFilename);

				// Write chat logs
				List<KeyValuePair<string, string>> chats = new List<KeyValuePair<string, string>>();
				foreach (var backupChat in backup.chats.OrderBy(c => c.creationDate))
				{
					// Backyard-compatible format
					{
						string chatFilename = string.Format("chatLog_{0}_{1}.json", backup.characterCard.data.displayName, backupChat.creationDate.ToUnixTimeSeconds()).Replace(" ", "_");

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
						string chatFilename = string.Format("chatLog_{0}_{1}.log", backup.characterCard.data.displayName, backupChat.creationDate.ToUnixTimeSeconds()).Replace(" ", "_");

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
				var intermediateFilename = Path.GetTempFileName();
				using (ZipArchive zip = ZipFile.Open(intermediateFilename, ZipArchiveMode.Update, Encoding.ASCII))
				{
					// Write card json (UTF8)
					var cardEntry = zip.CreateEntry("card.png", CompressionLevel.NoCompression);
					using (Stream writer = cardEntry.Open())
                    {
						writer.Write(cardBytes, 0, cardBytes.Length);
                    }

					// Write non-png portrait (situational)
					if (nonPNGImageData != null && nonPNGImageData.Length > 0)
					{
						string entryName = string.Format("images/image_00.{0}", nonPNGImageExt);
						var fileEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
						using (Stream writer = fileEntry.Open())
						{
							for (long n = nonPNGImageData.Length; n > 0;)
							{
								int length = (int)Math.Min(n, (long)int.MaxValue);
								writer.Write(nonPNGImageData, 0, length);
								n -= (long)length;
							}
						}
					}

					// Write image files
					for (int i = 1; i < backup.images.Count; ++i)
					{
						if (backup.images[i].data == null || backup.images[i].data.Length == 0)
							continue; // No file data

						string ext = Utility.GetFileExt(backup.images[i].filename);
						string entryName = string.Format("images/image_{0:00}.{1}", i, ext);
						var fileEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
						using (Stream writer = fileEntry.Open())
						{
							for (long n = backup.images[i].data.Length; n > 0;)
							{
								int length = (int)Math.Min(n, (long)int.MaxValue);
								writer.Write(backup.images[i].data, 0, length);
								n -= (long)length;
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
			FaradayCardV4 characterCard = null;
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
							if (entryPath == "" && entryExt == "png" && characterCard == null)
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
										characterCard = FaradayCardV4.FromJson(data.faradayJson);
										if (characterCard != null)
										{
											images.Add(new BackupData.Image() {
												filename = entryFullName,
												data = buffer,
											});
											continue;
										}
									}
								}
							}

							// Read chat log
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

							// Read chat log
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
							if (entryPath == "images" && Utility.IsSupportedImageFileExt(entryExt))
							{
								long dataSize = entry.Length;
								if (dataSize > 0)
								{
									var dataStream = entry.Open();
									byte[] buffer = new byte[dataSize];
									dataStream.Read(buffer, 0, (int)dataSize);
									images.Add(new BackupData.Image() {
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

				// images/portrait.*, images/image_00.* supercedes png.
				int idxPortrait = images.FindIndex(i => {
					string fn = Path.GetFileNameWithoutExtension(i.filename).ToLowerInvariant();
					return fn == "portrait" || fn == "image_00";
				});
				if (idxPortrait > 0 && images.Count > 0)
				{
					// Move to front (remove existing)
					var image = images[idxPortrait];
					images.RemoveAt(idxPortrait);
					images.RemoveAt(0);
					images.Insert(0, image);
				}

				if (characterCard == null)
				{
					backup = default(BackupData);
					return FileUtil.Error.NoDataFound;
				}

				backup = new BackupData() {
					characterCard = characterCard,
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

		
	}
}
