using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PNGNet;

namespace Ginger
{
	public static partial class FileUtil
	{ 
		public enum Error
		{
			NoError,
			InvalidData,
			NoDataFound,
			FileReadError,
			FileNotFound,
			UnrecognizedFormat,
			FallbackError,
		}

		public struct EmbeddedData
		{
			public string faradayJson;
			public string tavernJsonV2;
			public string tavernJsonV3;
			public string gingerXml;
			public Dictionary<string, byte[]> embeddedAssets;

			public bool isEmpty 
			{
				get
				{
					return string.IsNullOrEmpty(faradayJson)
						&& string.IsNullOrEmpty(tavernJsonV2)
						&& string.IsNullOrEmpty(tavernJsonV3)
						&& string.IsNullOrEmpty(gingerXml);
				}
			}
		}

		public static Error ExtractJsonFromPNG(string filename, out EmbeddedData result)
		{
			bool bDataFound = false;
			result = new EmbeddedData();
			try
			{
				// Read Faraday json (Exif)
				ExifData exifData = new ExifData(filename);
				if (exifData.GetTagValue(ExifTag.UserComment, out result.faradayJson, StrCoding.IdCode_UsAscii))
				{
					bDataFound = true;

					// Decode base64
					if (result.faradayJson.BeginsWith('{') == false
						 && result.faradayJson.Length > 0 && result.faradayJson.Length % 4 == 0) // Base64?
					{
						byte[] byteArray = Convert.FromBase64String(result.faradayJson);
						result.faradayJson = new string(Encoding.UTF8.GetChars(byteArray));
					}
				}
			}
			catch
			{
				result.faradayJson = null;
			}

			try
			{
				// Read PNG chunks
				Dictionary<string, string> metaData = new Dictionary<string, string>();
				using (FileStream stream = File.OpenRead(filename))
				{
					PNGImage image = new PNGImage(stream);
					foreach (var chunk in image.Chunks)
					{
						if (chunk is tEXtChunk) // Uncompressed
						{
							var textChunk = chunk as tEXtChunk;
							metaData.TryAdd(textChunk.Keyword.ToLowerInvariant(), textChunk.Text);
						}
						else if (chunk is zTXtChunk) // Compressed
						{
							var textChunk = chunk as zTXtChunk;
							metaData.TryAdd(textChunk.Keyword.ToLowerInvariant(), textChunk.Text);
						}
					}
				}

				try
				{

					// Read ginger (PNG chunk)
					if (metaData.ContainsKey("ginger"))
					{
						bDataFound = true;
						string gingerBase64 = metaData["ginger"];
						byte[] byteArray = Convert.FromBase64String(gingerBase64);
						result.gingerXml = new string(Encoding.UTF8.GetChars(byteArray));
					}
				}
				catch
				{
					result.gingerXml = null;
				}
				
				try
				{
					// Read Tavern v2 json (PNG chunk)
					if (metaData.ContainsKey("chara"))
					{
						bDataFound = true;
						string charaBase64 = metaData["chara"];
						byte[] byteArray = Convert.FromBase64String(charaBase64);
						result.tavernJsonV2 = new string(Encoding.UTF8.GetChars(byteArray));
					}

					if (AppSettings.CCV3.EnablePNGV3)
					{
						// Read Tavern v3 json (PNG chunk)
						if (metaData.ContainsKey("ccv3"))
						{
							bDataFound = true;
							string charaBase64 = metaData["ccv3"];
							byte[] byteArray = Convert.FromBase64String(charaBase64);
							result.tavernJsonV3 = new string(Encoding.UTF8.GetChars(byteArray));
						}

						// Read embedded PNGv3 assets
						if (metaData.Keys.ContainsAny(key => key.BeginsWith(AssetFile.PNGEmbeddedNamePrefix)))
						{
							result.embeddedAssets = new Dictionary<string, byte[]>();
							foreach (var kvp in metaData.Where(kvp => kvp.Key.BeginsWith(AssetFile.PNGEmbeddedNamePrefix)))
							{
								string path = kvp.Key.Substring(AssetFile.PNGEmbeddedNamePrefix.Length);
								byte[] byteArray = Convert.FromBase64String(kvp.Value);
								if (path.Length > 0 && byteArray.Length > 0)
									result.embeddedAssets.TryAdd(path, byteArray);
							}
						}
					}
				}
				catch
				{
					result.tavernJsonV2 = null;
					result.tavernJsonV3 = null;
				}
			}
			catch
			{
				result.tavernJsonV2 = null;
				result.tavernJsonV3 = null;
				result.gingerXml = null;
			}

			if (!bDataFound)
				return Error.NoDataFound;
			if (result.isEmpty)
				return Error.InvalidData;
			return Error.NoError;
		}

		public struct ImportResult
		{
			public GingerCardV1 gingerData;
			public TavernCardV2 tavernDataV2;
			public TavernCardV3 tavernDataV3;
			public FaradayCardV4 faradayData;   // Version 4
			public int jsonErrors;
			public AssetCollection embeddedAssets;
		}

		public static Error Import(string filename, out ImportResult result)
		{
			if (File.Exists(filename) == false)
			{
				result = new ImportResult();
				return Error.FileNotFound;
			}

			Error readError;
			EmbeddedData extractResult;
			if (Path.GetExtension(filename).ToLowerInvariant() == ".charx")
				readError = ExtractJsonFromArchive(filename, out extractResult);
			else
				readError = ExtractJsonFromPNG(filename, out extractResult);

			if (readError != Error.NoError)
			{
				result = new ImportResult();
				return readError;
			}
			
			result = new ImportResult();
			if (extractResult.faradayJson != null)
				result.faradayData = FaradayCardV4.FromJson(extractResult.faradayJson);
			if (extractResult.tavernJsonV2 != null)
				result.tavernDataV2 = TavernCardV2.FromJson(extractResult.tavernJsonV2, out result.jsonErrors);
			if (extractResult.tavernJsonV3 != null)
			{
				result.tavernDataV3 = TavernCardV3.FromJson(extractResult.tavernJsonV3, out result.jsonErrors);
				if (result.tavernDataV3 != null && result.tavernDataV3.data.assets != null && extractResult.embeddedAssets != null)
				{
					result.embeddedAssets = new AssetCollection();
					for (int i = 0; i < result.tavernDataV3.data.assets.Length; ++i)
					{
						var assetInfo = result.tavernDataV3.data.assets[i];
						if (assetInfo.uri.BeginsWith(AssetFile.PNGEmbeddedURIPrefix))
						{
							var path = assetInfo.uri.Substring(AssetFile.PNGEmbeddedURIPrefix.Length);
							byte[] data;
							extractResult.embeddedAssets.TryGetValue(path, out data);
							result.embeddedAssets.Add(AssetFile.FromV3Asset(assetInfo, data));
						}
					}
				}
			}
			if (extractResult.gingerXml != null)
				result.gingerData = GingerCardV1.FromXml(extractResult.gingerXml);
			
			if (result.faradayData == null && result.tavernDataV2 == null && result.tavernDataV3 == null && result.gingerData == null)
			{
				// No valid data
				result = new ImportResult();
				return Error.InvalidData;
			}

			// Failed to parse ginger data (use other data as fallback)
			if (extractResult.gingerXml != null && result.gingerData == null)
				return Error.FallbackError;

			// Success
			return Error.NoError;
		}

		private struct MetaData
		{
			public string key;
			public string value;
			public bool compressed;
		}

		[Flags]
		public enum Format 
		{
			Undefined = 0,
			Ginger = 1 << 0,
			Faraday = 1 << 1,
			SillyTavernV2 = 1 << 2,
			SillyTavernV3 = 1 << 3,
			
			SillyTavern = SillyTavernV2 | SillyTavernV3,
			All = Ginger | SillyTavern | Faraday,
		}

		public static bool Export(string filename, Image image, Format formats = Format.All)
		{
			try
			{
				var intermediateFilename = Path.GetTempFileName();
				using (var stream = new FileStream(intermediateFilename, FileMode.OpenOrCreate, FileAccess.Write))
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

				List<MetaData> metaData = new List<MetaData>();

				// Tavern json (v2)
				if (formats.Contains(Format.SillyTavernV2) && false) //!!
				{
					var tavernData = TavernCardV2_Export.FromOutput(Generator.Generate(Generator.Option.Export | Generator.Option.SillyTavern));
					tavernData.data.extensions.ginger = GingerExtensionData.FromOutput(Generator.Generate(Generator.Option.Snippet));
					var tavernJson = tavernData.ToJson();
					var tavernBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(tavernJson));
					metaData.Add(new MetaData() {
						key = "chara",
						value = tavernBase64,
						compressed = false,
					});
				}

				// Tavern json (v3)
				if (formats.Contains(Format.SillyTavernV3) && AppSettings.CCV3.EnablePNGV3)
				{
					var tavernData = TavernCardV3.FromOutput(Generator.Generate(Generator.Option.Export | Generator.Option.SillyTavern));
					tavernData.data.extensions.ginger = GingerExtensionData.FromOutput(Generator.Generate(Generator.Option.Snippet));

					// Compile assets
					var assets = (AssetCollection)Current.Card.assets.Clone();
					if (assets.HasDefaultIcon() == false)
					{
						// Add default icon asset
						assets.Add(new AssetFile() {
							assetType = AssetFile.AssetType.Icon,
							uri = AssetFile.DefaultURI,
							name = "main",
							ext = "png",
						});
					}

					assets.Validate(AssetCollection.Mode.Png);

					tavernData.data.assets = assets
						.Select(a => new TavernCardV3.Data.Asset() 
						{
							type = a.GetTypeName(),
							uri = a.uri,
							name = a.name,
							ext = a.ext.ToLowerInvariant(),
						})
						.ToArray();

					var tavernJson = tavernData.ToJson();
					var tavernBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(tavernJson));
					metaData.Add(new MetaData() {
						key = "ccv3",
						value = tavernBase64,
						compressed = false,
					});

					// Write assets
					foreach (var asset in assets)
					{
						if (asset.isDefaultAsset || asset.data.length == 0)
							continue;

						if (asset.uri.BeginsWith(AssetFile.PNGEmbeddedURIPrefix) == false)
							continue;

						string path = asset.uri.Substring(AssetFile.PNGEmbeddedURIPrefix.Length);
						var assetBase64 = Convert.ToBase64String(asset.data.data);
						metaData.Add(new MetaData() {
							key = string.Concat(AssetFile.PNGEmbeddedNamePrefix, path),
							value = assetBase64,
							compressed = false,
						});
					}
				}

				// Ginger xml
				if (formats.Contains(Format.Ginger))
				{
					var gingerXml = GingerCardV1.Create().ToXml();
					string gingerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(gingerXml));

					metaData.Add(new MetaData() {
						key = "ginger",
						value = gingerBase64,
						compressed = true,
					});
				}

				if (metaData.Count > 0 && WritePNGMetaData(intermediateFilename, metaData) == false)
					return false; // Error

				// Write Faraday json
				if (formats.Contains(Format.Faraday))
				{
					string faradayJson = FaradayCardV4.FromOutput(Generator.Generate(Generator.Option.Export | Generator.Option.Faraday)).ToJson();
					var faradayBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(faradayJson));
					if (WriteExifMetaData(intermediateFilename, faradayBase64) == false)
						return false; // Error
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

		public static bool ExportTextFile(string filename, string text)
		{
			try
			{
				var intermediateFilename = Path.GetTempFileName();

				// Write text file
				using (StreamWriter outputFile = new StreamWriter(intermediateFilename))
				{
					outputFile.Write(text);
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

		public static bool ExportTavernV2Lorebook(Lorebook lorebook, string filename)
		{
			var tavernBook = new TavernWorldBook();
			if (string.IsNullOrWhiteSpace(lorebook.name) == false)
				tavernBook.name = lorebook.name.Trim();
			else if (string.IsNullOrWhiteSpace(Current.Card.name) == false)
				tavernBook.name = Current.Card.name;
			else if (string.IsNullOrWhiteSpace(Current.Character.spokenName) == false)
				tavernBook.name = Current.Character.spokenName;
			else
				tavernBook.name = Path.GetFileNameWithoutExtension(filename);

			tavernBook.description = lorebook.description;
			if (lorebook.unused != null)
			{
				tavernBook.recursive_scanning = lorebook.unused.recursive_scanning;
				tavernBook.scan_depth = lorebook.unused.scan_depth;
				tavernBook.token_budget = lorebook.unused.token_budget;
				if (lorebook.unused.extensions != null)
					tavernBook.extensions = lorebook.unused.extensions.WithGingerVersion();
				else
					tavernBook.extensions = new SmallGingerJsonExtensionData();
			}

			int index = 0;
			foreach (var loreEntry in lorebook.entries)
			{
				if (loreEntry.keys == null || loreEntry.keys.Length == 0)
					continue;

				int entryId = index + 1;
				var tavernBookEntry = new TavernWorldBook.Entry() {
					uid = entryId,
					displayIndex = index,
					comment = loreEntry.keys[0] ?? "",
					key = loreEntry.keys,
					content = GingerString.FromString(loreEntry.value).ToTavern(),
				};

				if (loreEntry.unused != null)
				{
					tavernBookEntry.comment = loreEntry.unused.comment;
					tavernBookEntry.constant = loreEntry.unused.constant;
					tavernBookEntry.disable = !loreEntry.unused.enabled;
					tavernBookEntry.comment = loreEntry.unused.name;
					tavernBookEntry.order = loreEntry.unused.insertion_order;
					tavernBookEntry.position = loreEntry.unused.position;
					tavernBookEntry.secondary_keys = loreEntry.unused.secondary_keys;
					tavernBookEntry.addMemo = loreEntry.unused.addMemo;
					tavernBookEntry.depth = loreEntry.unused.depth;
					tavernBookEntry.excludeRecursion = loreEntry.unused.excludeRecursion;
					tavernBookEntry.selective = loreEntry.unused.selective;
					tavernBookEntry.selectiveLogic = loreEntry.unused.selectiveLogic;
					tavernBookEntry.useProbability = loreEntry.unused.useProbability;
					tavernBookEntry.probability = loreEntry.unused.probability;
					tavernBookEntry.group = loreEntry.unused.group;
					tavernBookEntry.extensions = loreEntry.unused.extensions ?? new JsonExtensionData();
				}

				tavernBook.entries.TryAdd(entryId.ToString(), tavernBookEntry);

				index++;
			}

			try
			{
				string json = Newtonsoft.Json.JsonConvert.SerializeObject(tavernBook);
				if (json != null && FileUtil.ExportTextFile(filename, json))
					return true; // Success
			}
			catch
			{
			}
			return false;
		}

		public static bool ExportTavernV3Lorebook(Lorebook lorebook, string filename)
		{
			var lorebookV3 = new TavernLorebookV3() {
				spec = "lorebook_v3",
				data = new TavernCardV3.CharacterBook(),
			};
			var data = lorebookV3.data;

			if (string.IsNullOrWhiteSpace(lorebook.name) == false)
				data.name = lorebook.name.Trim();
			else if (string.IsNullOrWhiteSpace(Current.Card.name) == false)
				data.name = Current.Card.name;
			else if (string.IsNullOrWhiteSpace(Current.Character.spokenName) == false)
				data.name = Current.Character.spokenName;
			else
				data.name = Path.GetFileNameWithoutExtension(filename);
			data.description = lorebook.description;

			if (lorebook.unused != null)
			{
				data.recursive_scanning = lorebook.unused.recursive_scanning;
				data.scan_depth = lorebook.unused.scan_depth;
				data.token_budget = lorebook.unused.token_budget;
				if (lorebook.unused.extensions != null)
					data.extensions = lorebook.unused.extensions.WithGingerVersion();
				else
					data.extensions = new SmallGingerJsonExtensionData();
			}
			else
				data.extensions = new SmallGingerJsonExtensionData();

			int index = 0;
			var entries = new List<TavernCardV3.CharacterBook.Entry>();
			foreach (var loreEntry in lorebook.entries)
			{
				if (loreEntry.keys == null || loreEntry.keys.Length == 0)
					continue;

				int entryId = index + 1;
				var copy = new TavernCardV3.CharacterBook.Entry() {
					id = entryId,
					keys = loreEntry.keys,
					name = loreEntry.key,
					content = GingerString.FromString(loreEntry.value).ToTavern(),
					use_regex = false,
				};

				if (loreEntry.unused != null)
				{
					copy.name = loreEntry.unused.name;
					copy.comment = loreEntry.unused.comment;
					copy.constant = loreEntry.unused.constant;
					copy.enabled = loreEntry.unused.enabled;
					copy.insertion_order = loreEntry.unused.insertion_order;
					copy.position = loreEntry.unused.placement;
					copy.secondary_keys = loreEntry.unused.secondary_keys;
					copy.priority = loreEntry.unused.priority;
					copy.selective = loreEntry.unused.selective;
					copy.case_sensitive = loreEntry.unused.case_sensitive;
					copy.extensions = loreEntry.unused.extensions ?? new JsonExtensionData();
				}

				entries.Add(copy);
				index++;
			}
			data.entries = entries.ToArray();

			try
			{
				string json = Newtonsoft.Json.JsonConvert.SerializeObject(lorebookV3);
				if (json != null && FileUtil.ExportTextFile(filename, json))
					return true; // Success
			}
			catch
			{
			}
			return false;
		}

		public static bool ExportAgnaisticLorebook(Lorebook lorebook, string filename)
		{
			var agnaiBook = new AgnaisticCard.CharacterBook();
			if (string.IsNullOrWhiteSpace(Current.Card.name) == false)
				agnaiBook.name = Current.Card.name;
			else if (string.IsNullOrWhiteSpace(Current.Character.spokenName) == false)
				agnaiBook.name = Current.Character.spokenName;
			else
				agnaiBook.name = Path.GetFileNameWithoutExtension(filename);
			agnaiBook.description = lorebook.description;
			if (lorebook.unused != null)
			{
				agnaiBook.recursiveScanning = lorebook.unused.recursive_scanning;
				agnaiBook.scanDepth = lorebook.unused.scan_depth;
				agnaiBook.tokenBudget = lorebook.unused.token_budget;
			}

			agnaiBook.entries = new AgnaisticCard.CharacterBook.Entry[lorebook.entries.Count];
			for (int i = 0; i < lorebook.entries.Count; ++i)
			{
				var loreEntry = lorebook.entries[i];

				var agnaiBookEntry = new AgnaisticCard.CharacterBook.Entry() {
					id = i + 1,
					name = loreEntry.keys[0],
					keywords = loreEntry.keys,
					entry = loreEntry.value,
				};

				if (loreEntry.unused != null)
				{
					agnaiBookEntry.comment = loreEntry.unused.comment;
					agnaiBookEntry.constant = loreEntry.unused.constant;
					agnaiBookEntry.enabled = loreEntry.unused.enabled;
					agnaiBookEntry.weight = loreEntry.unused.insertion_order;
					agnaiBookEntry.position = loreEntry.unused.placement;
					agnaiBookEntry.priority = loreEntry.unused.priority;
					agnaiBookEntry.secondaryKeys = loreEntry.unused.secondary_keys;
					agnaiBookEntry.selective = loreEntry.unused.selective;
				}

				agnaiBook.entries[i] = agnaiBookEntry;
			}

			try
			{
				string json = Newtonsoft.Json.JsonConvert.SerializeObject(agnaiBook);
				if (json != null && FileUtil.ExportTextFile(filename, json))
					return true; // Success
			}
			catch
			{
			}

			return false;
		}

		public static bool ExportLorebookCsv(Lorebook lorebook, string filename)
		{
			if (lorebook == null || lorebook.entries.Count == 0)
				return false;

			try
			{
				var intermediateFilename = Path.GetTempFileName();

				// Write text file
				using (StreamWriter outputFile = new StreamWriter(intermediateFilename))
				{
					outputFile.NewLine = "\r\n";

					StringBuilder sb = new StringBuilder();
					foreach (var entry in lorebook.entries)
					{
						if (entry.keys == null || entry.keys.Length == 0)
							continue;

						sb.Append("\"");
						for (int i = 0; i < entry.keys.Length; ++i)
						{
							if (i > 0)
								sb.Append(", ");
							sb.Append(entry.keys[i].Replace("\"", "\"\""));
						}
						sb.Append("\",\"");
						sb.Append(entry.value.Replace("\"", "\"\""));
						sb.AppendLine("\"");
					}
					sb.ConvertLinebreaks(Linebreak.CRLF);
					outputFile.Write(sb.ToString());
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

		public static bool ReadTextFile(string filename, out string json)
		{
			try
			{
				using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
					{
						json = sr.ReadToEnd();
						return true;
					}
				}
			}
			catch
			{
				json = default(string);
				return false;
			}
		}
		
		private static bool WriteExifMetaData(string filename, string faradayPayload)
		{
			try
			{
				ExifData exifData = new ExifData(filename);
				if (exifData.SetTagValue(ExifTag.UserComment, faradayPayload, StrCoding.IdCode_UsAscii))
					exifData.Save(filename);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static bool WritePNGMetaData(string origFilename, List<MetaData> metaData)
		{
			if (metaData == null || metaData.Count == 0)
				return true;

			PNGImage image;
			try
			{
				using (FileStream stream = File.OpenRead(origFilename))
				{
					image = new PNGImage(stream);
				}

				// Remove any existing tEXt chunks because they will upset poorly written v2 card importers
				image.Chunks.RemoveAll(c => c.Type == "tEXt");

				// Skip until the first IDAT chunk
				int insertionIndex = 0;
				for (; insertionIndex < image.Chunks.Count; ++insertionIndex)
				{
					if (image.Chunks[insertionIndex].Type == "IDAT")
						break;
				}

				foreach (var data in metaData)
				{
					if (data.compressed)
					{
						image.Chunks.Insert(insertionIndex++, new zTXtChunk(image) {
							Keyword = data.key,
							Text = data.value,
						});
					}
					else
					{
						image.Chunks.Insert(insertionIndex++, new tEXtChunk(image) {
							Keyword = data.key,
							Text = data.value,
						});
					}
				}
				image.Write(origFilename);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static string ImageToBase64(Image image)
		{
			if (image == null)
				return null;

			try
			{
				using (var stream = new MemoryStream())
				{
					if (image.RawFormat.Equals(ImageFormat.Png) == false)
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

					var imageBase64 = Convert.ToBase64String(stream.ToArray());
					return string.Concat("data:image/png;base64,", imageBase64);
				}
			}
			catch
			{
				return null;
			}
		}

		public static Image ImageFromBase64(string base64)
		{
			if (string.IsNullOrEmpty(base64))
				return null;

			try
			{
				if (base64.BeginsWith("data:image/png;base64,"))
					base64 = base64.Remove(0, 22);
				else if (base64.BeginsWith("data:image/jpeg;base64,"))
					base64 = base64.Remove(0, 23);
				else
					return null; // Invalid mime

				// Load image first
				Image image;
				try
				{
					byte[] bytes = Convert.FromBase64String(base64);
					using (var stream = new MemoryStream(bytes))
					{
						image = Image.FromStream(stream);
						return image;
					}
				}
				catch
				{
					return null;
				}
			}
			catch
			{
				return null;
			}
		}

		[Flags]
		public enum FileType
		{
			Unknown			= 0,
			Png				= 1 << 0,
			Json			= 1 << 1,
			Csv				= 1 << 2,
			Character		= 1 << 3,
			Lorebook		= 1 << 4,
			Ginger			= 1 << 10,
			Faraday			= 1 << 11,
			SillyTavernV2	= 1 << 12,
			SillyTavernV3	= 1 << 14,
			Agnaistic		= 1 << 15,
			Pygmalion		= 1 << 16,
			CharX			= 1 << 17,
		}

		public static FileType CheckFileType(string filename)
		{
			// Check filename
			if (File.Exists(filename) == false)
				return FileType.Unknown;

			string ext = Path.GetExtension(filename).ToLowerInvariant();
			if (ext == ".json")
			{
				string jsonData = Utility.LoadTextFile(filename);
				if (string.IsNullOrEmpty(jsonData) == false)
					return DetectJsonFileType(jsonData, FileType.Json);
			}

			if (ext == ".charx")
				return FileType.CharX;
			
			if (ext != ".png")
				return FileType.Unknown;

			try
			{
				// Check exif
				ExifData exifData = new ExifData(filename);
				string jsonData;
				exifData.GetTagValue(ExifTag.UserComment, out jsonData, StrCoding.IdCode_UsAscii);
				if (jsonData != null && jsonData.BeginsWith('{'))
					return DetectJsonFileType(jsonData, FileType.Png);
				if (jsonData != null && jsonData.Length > 0 && jsonData.Length % 4 == 0 && Regex.IsMatch(jsonData, @"^[a-zA-Z0-9\+/]*={0,2}$")) // Base64?
				{
					byte[] byteArray = Convert.FromBase64String(jsonData);
					jsonData = new string(Encoding.UTF8.GetChars(byteArray));
					return DetectJsonFileType(jsonData, FileType.Png);
				}
			}
			catch
			{
			}

			try
			{
				// Check png data
				Dictionary<string, string> metaData = new Dictionary<string, string>();
				using (FileStream stream = File.OpenRead(filename))
				{
					PNGImage image = new PNGImage(stream);
					bool hasGingerData = false;
					string jsonData = null;
					foreach (var chunk in image.Chunks)
					{
						if (chunk is tEXtChunk) // Uncompressed
						{
							var textChunk = chunk as tEXtChunk;
							hasGingerData |= string.Compare(textChunk.Keyword, "ginger", true) == 0;
							if (string.Compare(textChunk.Keyword, "chara", true) == 0 && string.IsNullOrEmpty(jsonData))
								jsonData = textChunk.Text;
							if (string.Compare(textChunk.Keyword, "ccv3", true) == 0 && AppSettings.CCV3.EnablePNGV3)
								jsonData = textChunk.Text;
						}
						else if (chunk is zTXtChunk) // Compressed
						{
							var textChunk = chunk as zTXtChunk;
							hasGingerData |= string.Compare(textChunk.Keyword, "ginger", true) == 0;
						}
					}
					if (hasGingerData)
						return FileType.Ginger | FileType.Character | FileType.Png;

					if (jsonData != null && jsonData.BeginsWith('{'))
						return DetectJsonFileType(jsonData, FileType.Png);
					if (jsonData != null && jsonData.Length > 0 && jsonData.Length % 4 == 0 && Regex.IsMatch(jsonData, @"^[a-zA-Z0-9\+/]*={0,2}$")) // Base64?
					{
						byte[] byteArray = Convert.FromBase64String(jsonData);
						jsonData = new string(Encoding.UTF8.GetChars(byteArray));
						return DetectJsonFileType(jsonData, FileType.Png);
					}
				}
			}
			catch
			{
			}

			return FileType.Unknown;
		}

		private static FileType DetectJsonFileType(string jsonData, FileType knownFileType = 0)
		{
			if (string.IsNullOrEmpty(jsonData) || jsonData.Length == 0 || jsonData[0] != '{')
				return FileType.Unknown;

			// Character cards
			if (TavernCardV3.Validate(jsonData))
				return FileType.Character | FileType.SillyTavernV3 | knownFileType;
			if (TavernCardV2.Validate(jsonData))
				return FileType.Character | FileType.SillyTavernV2 | knownFileType;
			if (FaradayCardV4.Validate(jsonData))
				return FileType.Character | FileType.Faraday | knownFileType;
			if (AgnaisticCard.Validate(jsonData))
				return FileType.Character | FileType.Agnaistic | knownFileType;
			if (PygmalionCard.Validate(jsonData))
				return FileType.Character | FileType.Pygmalion | knownFileType;

			// Lorebooks
			if (TavernWorldBook.Validate(jsonData))
				return FileType.Lorebook | FileType.SillyTavernV2 | knownFileType;
			if (TavernCardV2.CharacterBook.Validate(jsonData))
				return FileType.Lorebook | FileType.SillyTavernV2 | knownFileType;
			if (TavernLorebookV3.Validate(jsonData))
				return FileType.Lorebook | FileType.SillyTavernV3 | knownFileType;
			if (AgnaisticCard.CharacterBook.Validate(jsonData))
				return FileType.Lorebook | FileType.Agnaistic | knownFileType;
			return FileType.Unknown;
		}

		public static Error ImportCharacterJson(string filename, out int jsonErrors)
		{
			if (File.Exists(filename) == false)
			{
				jsonErrors = 0;
				return Error.FileNotFound;
			}

			string json;
			if (ReadTextFile(filename, out json) == false)
			{
				jsonErrors = 0;
				return Error.FileReadError;
			}

			// Tavern
			var card = TavernCardV2.FromJson(json, out jsonErrors);
			if (card != null)
			{
				Current.ReadTavernCard(card, null);
				return Error.NoError;
			}

			// Agnaistic
			var agnaisticCard = AgnaisticCard.FromJson(json, out jsonErrors);
			if (agnaisticCard != null)
			{
				Current.ReadAgnaisticCard(agnaisticCard);
				return Error.NoError;
			}

			// Pygmalion
			var pygmalionCard = PygmalionCard.FromJson(json);
			if (pygmalionCard != null)
			{
				Current.LoadCharacter(pygmalionCard);
				return Error.NoError;
			}

			return Error.UnrecognizedFormat;
		}

		public static Error ImportCharacterFromPNG(string filename, out int errors, Format formats = Format.All)
		{
			if (File.Exists(filename) == false)
			{
				errors = 0;
				return Error.FileNotFound;
			}

			// Load character data
			ImportResult importResult;
			bool bFallbackWarning = false;
			var importError = Import(filename, out importResult);
			if (importError == Error.FallbackError)
			{
				importError = Error.NoError;
				bFallbackWarning = true;
			}

			if (importError != Error.NoError)
			{
				errors = 0;
				return importError;
			}

			errors = importResult.jsonErrors;

			// Load image
			Image image = null;
			string ext = Path.GetExtension(filename).ToLowerInvariant();
			if (ext == ".png")
			{
				try
				{
					byte[] bytes = File.ReadAllBytes(filename);
					using (var stream = new MemoryStream(bytes))
					{
						image = Image.FromStream(stream);
					}
				}
				catch
				{
				}
			}

			if (importResult.gingerData != null && formats.Contains(Format.Ginger))
				Current.ReadGingerCard(importResult.gingerData, image);
			else if (importResult.faradayData != null && formats.Contains(Format.Faraday))
				Current.ReadFaradayCard(importResult.faradayData, image);
			else if (importResult.tavernDataV3 != null && formats.Contains(Format.SillyTavernV3))
			{
				Current.ReadTavernCard(importResult.tavernDataV3, image);

				if (ext == ".charx")
				{
					// Extract assets from charx archive
					ExtractAssetsFromArchive(filename, importResult.tavernDataV3, out Current.Card.assets);

					// Load portrait image
					Current.Card.portraitImage = ImageRef.FromImage(Current.Card.assets.GetPortraitImage());

					// Remove portrait image (it will be re-added on save/export)
					Current.Card.assets.RemovePortraitImage();
				}
			}
			else if (importResult.tavernDataV2 != null && formats.Contains(Format.SillyTavernV2))
				Current.ReadTavernCard(importResult.tavernDataV2, image);
			else
				return Error.UnrecognizedFormat;

			if (importResult.embeddedAssets != null)
				Current.Card.assets = importResult.embeddedAssets;

			return bFallbackWarning ? Error.FallbackError : Error.NoError;
		}

		
	}
}
