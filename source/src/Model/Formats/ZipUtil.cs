using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Ginger
{
	public static partial class FileUtil
	{
		private static Error ExtractJsonFromArchive(string filename, out string tavernJsonV3, out string gingerXml)
		{
			tavernJsonV3 = null;
			gingerXml = null;
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

							string entryName = entry.FullName.ToLowerInvariant();

							if (entryName == "ginger.xml")
							{
								long dataSize = entry.Length;
								if (dataSize > 0)
								{
									var dataStream = entry.Open();
									byte[] buffer = new byte[dataSize];
									dataStream.Read(buffer, 0, (int)dataSize);
									gingerXml = new string(Encoding.UTF8.GetChars(buffer));
								}
								continue;
							}
							if (entryName == "card.json")
							{
								long dataSize = entry.Length;
								if (dataSize > 0)
								{
									var dataStream = entry.Open();
									byte[] buffer = new byte[dataSize];
									dataStream.Read(buffer, 0, (int)dataSize);
									tavernJsonV3 = new string(Encoding.UTF8.GetChars(buffer));
								}
								continue;
							}
						}
					}
				}

				if (string.IsNullOrEmpty(tavernJsonV3) && string.IsNullOrEmpty(gingerXml))
					return Error.NoDataFound;
				return Error.NoError;
			}
			catch
			{
				return Error.FileReadError;
			}
		}

		private static Error ExtractAssetsFromArchive(string filename, TavernCardV3 tavernV3Card, out AssetCollection assets)
		{
			var assetList = tavernV3Card.data.assets;
			if (assetList == null || assetList.Length == 0)
			{
				assets = new AssetCollection(); // Empty
				return Error.NoDataFound;
			}

			try
			{
				assets = new AssetCollection();
				foreach (var assetInfo in assetList)
				{
					var assetType = AssetFile.AssetTypeFromString(assetInfo.type);
					var fileType = AssetFile.FileTypeFromExt(assetInfo.ext);
					string uri = assetInfo.uri.Trim();

					// Default asset
					if (string.Compare(uri, AssetFile.DefaultURI, StringComparison.OrdinalIgnoreCase) == 0)
					{
						assets.Add(new AssetFile() {
							name = assetInfo.name,
							assetType = assetType,
							ext = assetInfo.ext,
							fileType = fileType,
							uri = AssetFile.DefaultURI,
						});
						continue;
					}

					int idxProtocol = uri.IndexOf("://");
					if (idxProtocol == -1)
						continue; // Invalid uri

					string protocol = uri.Substring(0, idxProtocol + 3).ToLowerInvariant();
					string path = uri.Substring(idxProtocol + 3);
					if (protocol == "embedded://")
						protocol = "embeded://"; // A quirk in the v3 spec
				
					assets.Add(new AssetFile() {
						name = assetInfo.name,
						assetType = assetType,
						ext = assetInfo.ext,
						fileType = fileType,
						uri = string.Concat(protocol, path),
					});
				}

				using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
					{
						foreach (var entry in archive.Entries)
						{
							if (string.IsNullOrEmpty(entry.Name))
								continue; // Skip folder entries

							string entryName = entry.FullName.Replace('\\', '/');
							int idxAsset = assets.FindIndex(a => a.uri == string.Concat("embeded://", entryName));
							if (idxAsset == -1)
								continue; // Unreferenced asset

							long dataSize = entry.Length;
							if (dataSize > 0)
							{
								var buffer = new byte[dataSize];
								var dataStream = entry.Open();
								dataStream.Read(buffer, 0, (int)dataSize);
								assets[idxAsset].data = new AssetData() {
									Data = buffer,
								};
							}
						}
					}
				}
				if (assets.Count == 0)
					return Error.NoDataFound;
				return Error.NoError;
			}
			catch
			{
				assets = null;
				return Error.FileReadError;
			}
		}

		private static Error ExtractFileFromArchive(string filename, string entryName, out byte[] data)
		{
			try
			{
				entryName = entryName.Replace('\\', '/');

				using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
					{
						foreach (var entry in archive.Entries)
						{
							string thisEntryName = entry.FullName.Replace('\\', '/');
							if (thisEntryName == entryName) // Exact match
							{
								long dataSize = entry.Length;
								if (dataSize > 0)
								{
									var dataStream = entry.Open();
									data = new byte[dataSize];
									dataStream.Read(data, 0, (int)dataSize);
									return Error.NoError;
								}
							}
						}
					}
				}

				data = null;
				return Error.NoDataFound;
			}
			catch
			{
				data = null;
				return Error.FileReadError;
			}
		}

		public static bool ExportToCharX(string filename)
		{
			var card = TavernCardV3.FromOutput(Generator.Generate(Generator.Option.Export | Generator.Option.SillyTavern));
			card.data.extensions.ginger = GingerExtensionData.FromOutput(Generator.Generate(Generator.Option.Snippet));

			AssetCollection assets = new AssetCollection(Current.Card.assets);

			// Add current portrait image
			Image image = Current.Card.portraitImage;
			if (image != null)
			{
				// Write image to buffer
				try
				{
					using (var stream = new MemoryStream())
					{
						if (image.RawFormat.Equals(ImageFormat.Png))
						{
							image.Save(stream, ImageFormat.Png);
						}
						else // Convert to png
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

						// Add main icon entry
						assets.Insert(0, new AssetFile() {
							assetType = AssetFile.AssetType.Icon,
							fileType = AssetFile.FileType.Image,
							uri = "embeded://assets/icon/images/main.png",
							name = "main",
							ext = "png",
							data = new AssetData() {
								Data = stream.ToArray(),
							},
						});
					}
				}
				catch
				{
					// Add default icon entry
					assets.Insert(0, new AssetFile() {
						assetType = AssetFile.AssetType.Icon,
						uri = AssetFile.DefaultURI,
						name = "main",
						ext = "unknown",
					});
				}

			}
			else
			{
				// Add default entry
				assets.Insert(0, new AssetFile() {
					assetType = AssetFile.AssetType.Icon,
					uri = AssetFile.DefaultURI,
					name = "main",
					ext = "unknown",
				});
			}

			// Validate names and uris
			var correctedAssets = assets
				.GroupBy(a => a.assetType)
				.Select(g => {
					var assetType = g.Key;
					var assetsOfType = g.ToList();

					if (assetType == AssetFile.AssetType.Other || assetType == AssetFile.AssetType.Undefined)
						return new {
							type = assetType,
							assets = assetsOfType,
						};

					// Ensure there is at least one "main" asset per type
					string mainName = assetType == AssetFile.AssetType.Expression ? "neutral" : "main";

					if (assetsOfType.Count == 1)
					{
						assetsOfType[0].name = mainName;
						return new {
							type = assetType,
							assets = assetsOfType,
						};
					}

					int nMain = assets.Count(a => string.Compare(a.name, mainName, StringComparison.OrdinalIgnoreCase) == 0);
					if (nMain == 0)
						assetsOfType[0].name = mainName;
					return new {
						type = assetType,
						assets = assetsOfType,
					};
				})
			.SelectMany(x => {
				// Ensure unique names within each asset group
				var assetType = x.type;
				var assetsOfType = x.assets;

				var used_names = new Dictionary<string, int>();
				for (int i = 0; i < assetsOfType.Count; ++i)
				{
					string name = assetsOfType[i].name.ToLowerInvariant();
					if (used_names.ContainsKey(name) == false)
					{
						used_names.Add(name, 1);
						continue;
					}

					int count;
					string testName = name;
					while (used_names.TryGetValue(testName, out count))
					{
						testName = string.Format("{0}_{1:00}", name, count + 1);
						++used_names[name];
					}
					assetsOfType[i].name = testName;
				}
				return assetsOfType;
			})
			.Select(asset => {
				// Fix uri
				if (asset.isDefaultAsset)
					asset.uri = AssetFile.DefaultURI;
				else
					asset.uri = string.Concat(asset.protocol, asset.GetPath(), asset.name, ".", asset.ext).ToLowerInvariant();

				// Fix ext
				if (asset.ext != null)
					asset.ext = asset.ext.ToLowerInvariant();
				else
					asset.ext = "unknown";
				return asset;
			});

			card.data.assets = correctedAssets
				.Select(a => new TavernCardV3.Data.Asset() 
				{
					type = a.GetTypeName(),
					uri = a.uri,
					name = a.name,
					ext = a.ext.ToLowerInvariant(),
				})
				.ToArray();

			var json = card.ToJson();

			try
			{
				var intermediateFilename = Path.GetTempFileName();
				using (ZipArchive zip = ZipFile.Open(intermediateFilename, ZipArchiveMode.Update, Encoding.ASCII))
				{
					// Write card json (UTF8)
					var cardEntry = zip.CreateEntry("card.json", CompressionLevel.NoCompression);
					using (Stream writer = cardEntry.Open())
                    {
						byte[] textBuffer = Encoding.UTF8.GetBytes(json);
						writer.Write(textBuffer, 0, textBuffer.Length);
                    }

					// Write ginger.txt
					var gingerEntry = zip.CreateEntry("ginger.txt", CompressionLevel.NoCompression);
					using (StreamWriter writer = new StreamWriter(gingerEntry.Open()))
                    {
						writer.WriteLine("This character card was created using Ginger.");
						writer.WriteLine("https://www.github.com/DominaeDev/Ginger/");
                    }

					// Write asset files
					foreach (var asset in assets)
					{
						if (asset.data.Length == 0)
							continue; // No file data

						string entryName = string.Concat(asset.GetPath(), asset.name, ".", asset.ext);
						var fileEntry = zip.CreateEntry(string.Format(entryName, CompressionLevel.NoCompression));
						using (Stream writer = fileEntry.Open())
						{
							for (long n = asset.data.Length; n > 0;)
							{
								int length = (int)Math.Min(n, (long)int.MaxValue);
								writer.Write(asset.data.Data, 0, length);
								n -= (long)length;
							}
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
	}
}
