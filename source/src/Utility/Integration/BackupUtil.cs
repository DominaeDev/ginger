﻿using System;
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
		public List<Chat> chats;
		
		public class Image
		{
			public string filename;
			public byte[] data;

			public string ext
			{
				get
				{
					var ext = Path.GetExtension(filename ?? "");
					if (ext.BeginsWith('.'))
						ext = ext.Substring(1);
					return ext;
				}
			}
		}

		public class Chat
		{
			public string name;
			public DateTime creationDate;
			public DateTime updateDate;
			public ChatHistory history;
			public ChatStaging staging = new ChatStaging();
			public ChatParameters parameters = new ChatParameters();
		}

		public bool hasParameters { get { return chats != null && chats.ContainsAny(c => c.parameters != null); } }
	}

	public static class BackupUtil
	{
		public static Backyard.Error CreateBackup(CharacterInstance characterInstance, out BackupData backupInfo)
		{
			FaradayCardV4 card = null;
			string[] imageUrls = null;
			ChatInstance[] chatInstances = null;
			var error = Backyard.ImportCharacter(characterInstance, out card, out imageUrls);
			if (error != Backyard.Error.NoError)
			{
				backupInfo = null;
				return error;
			}

			error = Backyard.GetChats(characterInstance.groupId, out chatInstances);
			if (error != Backyard.Error.NoError)
			{
				backupInfo = null;
				return error;
			}

			// Create backup
			backupInfo = new BackupData();
			backupInfo.characterCard = card;
			backupInfo.chats = chatInstances
				.Where(c => c.history.isEmpty == false)
				.Select(c => new BackupData.Chat() { 
					name = c.name,
					history = c.history,
					staging = c.staging,
					parameters = c.parameters,
					creationDate = c.creationDate,
					updateDate = c.updateDate,
				})
				.ToList();
			backupInfo.images = imageUrls.Select(url => new BackupData.Image() { 
				filename = Path.GetFileName(url),
				data = Utility.LoadFile(url),
			}).ToList();

			return Backyard.Error.NoError;
		}

		public static bool WriteBackup(string filename, BackupData backup)
		{
			try
			{
				Image image = null;
				if (backup.images.Count > 0)
					Utility.LoadImageFromMemory(backup.images[0].data, out image);
				if (image == null)
					image = DefaultPortrait.Image;

				// Write character card (png)
				var intermediateCardFilename = Path.GetTempFileName();
				using (var stream = new FileStream(intermediateCardFilename, FileMode.OpenOrCreate, FileAccess.Write))
				{
					if (image.RawFormat.Equals(ImageFormat.Png) == false) // Convert to PNG
					{
						using (Image bmpNewImage = new Bitmap(image.Width, image.Height))
						{
							Graphics gfxNewImage = Graphics.FromImage(bmpNewImage);
							gfxNewImage.DrawImage(image, new Rectangle(0, 0, bmpNewImage.Width, bmpNewImage.Height),
												  0, 0,
												  image.Width, image.Height,
												  GraphicsUnit.Pixel);
							gfxNewImage.Dispose();
							bmpNewImage.Save(stream, ImageFormat.Png);
						}
					}
					else
					{
						image.Save(stream, ImageFormat.Png);
					}
				}

				// Write json
				string faradayJson = backup.characterCard.ToJson();
				var faradayBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(faradayJson));
				if (FileUtil.WriteExifMetaData(intermediateCardFilename, faradayBase64) == false)
					return false; // Error

				// Read in temporary file
				byte[] cardBytes = Utility.LoadFile(intermediateCardFilename);
				if (cardBytes == null)
					return false;

				// Delete temporary file
				File.Delete(intermediateCardFilename);

				List<KeyValuePair<string, string>> chats = new List<KeyValuePair<string, string>>();
				foreach (var chat in backup.chats.OrderBy(c => c.creationDate))
				{
					string chatFilename = string.Format("chatLog_{0}_{1}.json", backup.characterCard.data.displayName, chat.creationDate.ToUnixTimeSeconds()).Replace(" ", "_");

					var chatBackup = BackyardChatBackup.FromChat(new BackupData.Chat() {
						name = chat.name,
						creationDate = chat.creationDate,
						updateDate = chat.updateDate,
						staging = chat.staging,
						parameters = chat.parameters,
						history = ChatHistory.LegacyFix(chat.history),
					});
					string json = chatBackup.ToJson();
					if (json != null)
						chats.Add(new KeyValuePair<string, string>(chatFilename, json));
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

					// Write image files
					for (int i = 1; i < backup.images.Count; ++i)
					{
						if (backup.images[i].data == null || backup.images[i].data.Length == 0)
							continue; // No file data

						string ext = Path.GetExtension(backup.images[i].filename);
						if (ext.BeginsWith('.'))
							ext = ext.Substring(1);
						string entryName = string.Format("images/image_{0:00}.{1}", i, ext);

						var fileEntry = zip.CreateEntry(string.Format(entryName, CompressionLevel.NoCompression));
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

					// Write chat files
					for (int i = 0; i < chats.Count; ++i)
					{
						string entryName = string.Concat("logs/", chats[i].Key);

						var fileEntry = zip.CreateEntry(string.Format(entryName, CompressionLevel.NoCompression));
						using (Stream writer = fileEntry.Open())
						{
							byte[] textBuffer = Encoding.UTF8.GetBytes(chats[i].Value);
							writer.Write(textBuffer, 0, textBuffer.Length);
						}
					}
				}

				// Rename Temporaty file to Target file
				if (File.Exists(filename))
					File.Delete(filename);
				File.Move(intermediateFilename, filename);
				return true;
			}
			catch
			{
			}
			return false;
		}

		public static FileUtil.Error ReadBackup(string filename, out BackupData backup)
		{
			FaradayCardV4 characterCard = null;
			List<BackupData.Image> images = new List<BackupData.Image>();
			List<BackupData.Chat> chats = new List<BackupData.Chat>();

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
							string entryExt = Path.GetExtension(entry.FullName).ToLowerInvariant();
							
							// Read character png
							if (entryPath == "" && entryExt == ".png" && characterCard == null)
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
							if ((entryPath == "chats" || entryPath == "logs") && entryExt == ".json")
							{
								long dataSize = entry.Length;
								if (dataSize > 0)
								{
									var dataStream = entry.Open();
									byte[] buffer = new byte[dataSize];
									dataStream.Read(buffer, 0, (int)dataSize);
									string chatJson = new string(Encoding.UTF8.GetChars(buffer));

									var chatBackup = BackyardChatBackup.FromJson(chatJson).ToChat();

									chats.Add(new BackupData.Chat() {
										creationDate = chatBackup.creationDate,
										updateDate = chatBackup.updateDate,
										name = chatBackup.name ?? ChatInstance.DefaultName,
										staging = chatBackup.staging,
										parameters = chatBackup.parameters,
										history = chatBackup.history,
									});
								}
								continue;
							}
							// Images
							if (entryPath == "images"
								&& (entryExt == ".png" || entryExt == ".apng" || entryExt == ".jpeg" || entryExt == ".jpg" 
								|| entryExt == ".gif" || entryExt == ".webp" || entryExt == ".avif" ))
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
							}
						}
					}
				}

				if (characterCard == null)
				{
					backup = default(BackupData);
					return FileUtil.Error.NoDataFound;
				}

				backup = new BackupData() {
					characterCard = characterCard,
					chats = chats,
					images = images,
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
