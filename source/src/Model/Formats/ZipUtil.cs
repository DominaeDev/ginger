using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Ginger
{
	public static partial class FileUtil
	{
		private static Error ExtractJsonFromArchive(string filename, out EmbeddedData result)
		{
			result = new EmbeddedData();
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
									result.gingerXml = new string(Encoding.UTF8.GetChars(buffer));
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
									result.tavernJsonV3 = new string(Encoding.UTF8.GetChars(buffer));
								}
								continue;
							}
						}
					}
				}

				if (result.isEmpty)
					return Error.InvalidData;
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
					assets.Add(AssetFile.FromV3Asset(assetInfo));
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
							int idxAsset = assets.FindIndex(a => string.Compare(string.Concat(a.uriPath ?? "", a.uriName ?? ""), entryName, StringComparison.Ordinal) == 0);
							if (idxAsset == -1)
								continue; // Unreferenced asset

							long dataSize = entry.Length;
							if (dataSize > 0)
							{
								var buffer = new byte[dataSize];
								var dataStream = entry.Open();
								dataStream.Read(buffer, 0, (int)dataSize);
								assets[idxAsset].data = AssetData.FromBytes(buffer);
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
				assets = new AssetCollection(); // Empty
				return Error.FileReadError;
			}
		}

		public static bool ExportToCharX(string filename)
		{
			var card = TavernCardV3.FromOutput(Generator.Generate(Generator.Option.Export | Generator.Option.SillyTavernV3));
			card.data.extensions.ginger = GingerExtensionData.FromOutput(Generator.Generate(Generator.Option.Snippet));

			AssetCollection assets = (AssetCollection)Current.Card.assets.Clone();

			assets.AddPortraitAsset(FileType.CharX);
			assets.Validate();

			card.data.assets = assets
				.Select(a => a.ToV3Asset(AssetFile.UriFormat.CharX_Prefix))
				.ToArray();

			var json = card.ToJson();
			if (json == null)
				return false; // Error

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

					// Write readme.txt
					var gingerEntry = zip.CreateEntry("readme.txt", CompressionLevel.NoCompression);
					using (StreamWriter writer = new StreamWriter(gingerEntry.Open(), Encoding.UTF8))
                    {
						writer.WriteLine("This character card was created using Ginger.");
						writer.WriteLine();
						writer.WriteLine(Constants.WebsiteURL);
                    }

					// Write asset files
					foreach (var asset in assets)
					{
						if (asset.data.length == 0)
							continue; // No file data
						if (asset.uriType != AssetFile.UriType.Embedded)
							continue; // Not an embed

						string entryName = asset.GetUri(AssetFile.UriFormat.CharX);
						var fileEntry = zip.CreateEntry(string.Format(entryName, CompressionLevel.NoCompression));
						using (Stream writer = fileEntry.Open())
						{
							for (long n = asset.data.length; n > 0;)
							{
								int length = (int)Math.Min(n, (long)int.MaxValue);
								writer.Write(asset.data.bytes, 0, length);
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
				return false;
			}
		}
	}
}
