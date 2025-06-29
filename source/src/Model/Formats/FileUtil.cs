﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PNGNet;
using Ginger.Integration;
using System.Xml;

namespace Ginger
{
	public static partial class FileUtil
	{ 
		public enum Error
		{
			NoError,
			InvalidData,
			InvalidJson,
			UnrecognizedFormat,
			NoDataFound,
			FileNotFound,
			FileReadError,
			FileWriteError,
			DiskFullError,
			FallbackError,
			UnknownError,
		}

		public static readonly Encoding UTF8WithoutBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

		public struct EmbeddedData
		{
			public string faradayJson;
			public string tavernJsonV2;
			public string tavernJsonV3;
			public string gingerXml;

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

					// Read Tavern v3 json (PNG chunk)
					if (metaData.ContainsKey("ccv3"))
					{
						bDataFound = true;
						string charaBase64 = metaData["ccv3"];
						byte[] byteArray = Convert.FromBase64String(charaBase64);
						result.tavernJsonV3 = new string(Encoding.UTF8.GetChars(byteArray));
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

		public static Error ExtractJsonFromPNG(byte[] buffer, out EmbeddedData result)
		{
			if (buffer == null || buffer.Length == 0)
			{
				result = default(EmbeddedData);
				return Error.FileReadError;
			}

			result = new EmbeddedData();
			bool bDataFound = false;
			try
			{
				// Read Faraday json (Exif)
				using (var stream = new MemoryStream(buffer))
				{
					ExifData exifData = new ExifData(stream);
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
			}
			catch
			{
				result.faradayJson = null;
			}

			try
			{
				// Read PNG chunks
				Dictionary<string, string> metaData = new Dictionary<string, string>();
				using (var stream = new MemoryStream(buffer))
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

					// Read Tavern v3 json (PNG chunk)
					if (metaData.ContainsKey("ccv3"))
					{
						bDataFound = true;
						string charaBase64 = metaData["ccv3"];
						byte[] byteArray = Convert.FromBase64String(charaBase64);
						result.tavernJsonV3 = new string(Encoding.UTF8.GetChars(byteArray));
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
				result.tavernDataV3 = TavernCardV3.FromJson(extractResult.tavernJsonV3, out result.jsonErrors);
			if (extractResult.gingerXml != null)
				result.gingerData = GingerCardV1.FromXml(extractResult.gingerXml);
			
			if (result.faradayData == null && result.tavernDataV2 == null && result.tavernDataV3 == null && result.gingerData == null)
			{
				// No valid data
				result = new ImportResult();
				return Error.NoDataFound;
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
			
			All = Ginger | SillyTavernV2 | SillyTavernV3 | Faraday,
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
				if (formats.Contains(Format.SillyTavernV2))
				{
					var tavernData = TavernCardV2_Export.FromOutput(Generator.Generate(Generator.Option.Export | Generator.Option.SillyTavernV2));
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
				if (formats.Contains(Format.SillyTavernV3))
				{
					var tavernData = TavernCardV3.FromOutput(Generator.Generate(Generator.Option.Export | Generator.Option.SillyTavernV3));
					tavernData.data.extensions.ginger = GingerExtensionData.FromOutput(Generator.Generate(Generator.Option.Snippet));

					// Compile assets
					var assets = (AssetCollection)Current.Card.assets.Clone();
					assets.AddPortraitAsset(FileType.Png, true);
					assets.Validate();

					tavernData.data.assets = assets
						.Select(a => a.ToV3Asset(AssetFile.UriFormat.Png_Prefix))
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
						if (asset.isEmbeddedAsset == false || asset.data.length == 0)
							continue;

						string key = string.Concat(AssetFile.PNGEmbedKeyPrefix, asset.GetUri(AssetFile.UriFormat.Png));
						var assetBase64 = Convert.ToBase64String(asset.data.bytes);
						metaData.Add(new MetaData() {
							key = key,
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
				using (StreamWriter outputFile = new StreamWriter(new FileStream(intermediateFilename, FileMode.Open, FileAccess.Write), UTF8WithoutBOM))
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
			else if (string.IsNullOrWhiteSpace(Current.Character.name) == false)
				tavernBook.name = Current.Character.name;
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
					order = loreEntry.sortOrder,
				};

				if (loreEntry.unused != null)
				{
					tavernBookEntry.comment = loreEntry.unused.comment;
					tavernBookEntry.constant = loreEntry.unused.constant;
					tavernBookEntry.disable = !loreEntry.unused.enabled;
					tavernBookEntry.comment = loreEntry.unused.name;
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
				if (json != null && ExportTextFile(filename, json))
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
			else if (string.IsNullOrWhiteSpace(Current.Character.name) == false)
				data.name = Current.Character.name;
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
					insertion_order = loreEntry.sortOrder,
					use_regex = false,
				};

				if (loreEntry.unused != null)
				{
					copy.name = loreEntry.unused.name;
					copy.comment = loreEntry.unused.comment;
					copy.constant = loreEntry.unused.constant;
					copy.enabled = loreEntry.unused.enabled;
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
				if (json != null && ExportTextFile(filename, json))
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
			else if (string.IsNullOrWhiteSpace(Current.Character.name) == false)
				agnaiBook.name = Current.Character.name;
			else
				agnaiBook.name = Path.GetFileNameWithoutExtension(filename);
			agnaiBook.description = lorebook.description;
			if (lorebook.unused != null)
			{
				agnaiBook.recursiveScanning = lorebook.unused.recursive_scanning;
				agnaiBook.scanDepth = lorebook.unused.scan_depth;
				agnaiBook.tokenBudget = lorebook.unused.token_budget;
			}

			var entries = new List<AgnaisticCard.CharacterBook.Entry>(lorebook.entries.Count);
			int id = 1;
			int minPriority = int.MaxValue;
			int maxPriority = int.MinValue;
			foreach (var loreEntry in lorebook.entries)
			{
				var agnaiBookEntry = new AgnaisticCard.CharacterBook.Entry() {
					id = id++,
					name = loreEntry.keys[0],
					keywords = loreEntry.keys,
					entry = loreEntry.value,
					priority = loreEntry.sortOrder, // Inverse
				};

				if (loreEntry.unused != null)
				{
					agnaiBookEntry.comment = loreEntry.unused.comment;
					agnaiBookEntry.constant = loreEntry.unused.constant;
					agnaiBookEntry.enabled = loreEntry.unused.enabled;
					agnaiBookEntry.weight = loreEntry.unused.weight;
					agnaiBookEntry.position = loreEntry.unused.placement;
					agnaiBookEntry.secondaryKeys = loreEntry.unused.secondary_keys;
					agnaiBookEntry.selective = loreEntry.unused.selective;
				}
				minPriority = Math.Min(loreEntry.sortOrder, minPriority);
				maxPriority = Math.Max(loreEntry.sortOrder, maxPriority);

				entries.Add(agnaiBookEntry);
			}

			// Sort order -> Priority
			if (minPriority != maxPriority)
			{
				for (int i = 0; i < entries.Count; ++i)
				{
					var entry = entries[i];
					entries[i].priority = maxPriority - (entry.priority - minPriority) - minPriority;
				}
			}

			agnaiBook.entries = entries.ToArray();

			try
			{
				string json = Newtonsoft.Json.JsonConvert.SerializeObject(agnaiBook);
				if (json != null && ExportTextFile(filename, json))
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
				using (StreamWriter outputFile = new StreamWriter(new FileStream(intermediateFilename, FileMode.Open, FileAccess.Write), UTF8WithoutBOM))
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
		
		internal static bool WriteExifMetaData(string filename, string faradayPayload)
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

		[Flags]
		public enum FileType
		{
			Unknown			= 0,
			Png				= 1 << 0,
			Json			= 1 << 1,
			Csv				= 1 << 2,
			Yaml			= 1 << 3,
			CharX			= 1 << 4,
			Backup			= 1 << 5,
			BackyardArchive	= 1 << 6,

			Character		= 1 << 10,
			Lorebook		= 1 << 11,
			Group			= 1 << 12,

			Ginger			= 1 << 20,
			Faraday			= 1 << 21,
			TavernV2		= 1 << 22,
			TavernV3		= 1 << 23,
			Agnaistic		= 1 << 24,
			Pygmalion		= 1 << 25,
			TextGenWebUI	= 1 << 26,
		}

		public static FileType CheckFileType(string filename)
		{
			return CheckFileType(filename, out var _);
		}

		public static FileType CheckFileType(string filename, out int characterCount)
		{
			// Check filename
			if (File.Exists(filename) == false)
			{
				characterCount = 0;
				return FileType.Unknown;
			}

			string ext = Path.GetExtension(filename).ToLowerInvariant();
			if (ext == ".json")
			{
				characterCount = 1;
				string jsonData = Utility.LoadTextFile(filename);
				if (string.IsNullOrEmpty(jsonData) == false)
					return DetectJsonFileType(jsonData, FileType.Json);
			}

			if (ext == ".yaml")
			{
				characterCount = 1;
				string yamlData = Utility.LoadTextFile(filename);
				if (string.IsNullOrEmpty(yamlData) == false)
					return DetectJsonFileType(yamlData, FileType.Yaml);
			}

			if (ext == ".charx")
			{
				characterCount = 1;
				return FileType.CharX;
			}

			if (ext == ".byaf")
			{
				characterCount = 1;
				return FileType.BackyardArchive;
			}

			if (ext == ".zip")
			{
				if (BackupUtil.CheckIfGroup(filename, out characterCount))
					return FileType.Backup | FileType.Group;
				return FileType.Backup;
			}

			if (ext != ".png")
			{
				characterCount = 0;
				return FileType.Unknown;
			}

			try
			{
				// Check exif
				ExifData exifData = new ExifData(filename);
				string jsonData;
				exifData.GetTagValue(ExifTag.UserComment, out jsonData, StrCoding.IdCode_UsAscii);
				characterCount = 1;
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
					characterCount = 1;
					foreach (var chunk in image.Chunks)
					{
						if (chunk is tEXtChunk) // Uncompressed
						{
							var textChunk = chunk as tEXtChunk;
							if (string.Compare(textChunk.Keyword, "ginger", true) == 0)
							{
								hasGingerData = true;
								CheckGingerChunkIfGroup(textChunk.Text, out characterCount);
							}
							if (string.Compare(textChunk.Keyword, "chara", true) == 0 && string.IsNullOrEmpty(jsonData))
								jsonData = textChunk.Text;
							if (string.Compare(textChunk.Keyword, "ccv3", true) == 0)
								jsonData = textChunk.Text;
						}
						else if (chunk is zTXtChunk) // Compressed
						{
							var textChunk = chunk as zTXtChunk;
							if (string.Compare(textChunk.Keyword, "ginger", true) == 0)
							{
								hasGingerData = true;
								CheckGingerChunkIfGroup(textChunk.Text, out characterCount);
							}
						}
					}

					if (hasGingerData)
						return FileType.Ginger | FileType.Character | FileType.Png | (characterCount > 1 ? FileType.Group : FileType.Unknown);

					if (jsonData != null && jsonData.BeginsWith('{')) // Legacy: Json
						return DetectJsonFileType(jsonData, FileType.Png);

					if (jsonData != null && jsonData.Length > 0 && jsonData.Length % 4 == 0 && Regex.IsMatch(jsonData, @"^[a-zA-Z0-9\+/]*={0,2}$")) // Base64
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

			characterCount = 0;
			return FileType.Unknown;
		}

		private static bool CheckGingerChunkIfGroup(string gingerBase64, out int count)
		{
			try
			{
				byte[] buffer = Convert.FromBase64String(gingerBase64);
				var gingerCard = GingerCardV1.FromXml(new string(Encoding.UTF8.GetChars(buffer)));
				count = gingerCard.characters.Count;
				return gingerCard.characters.Count > 1;
			}
			catch
			{
				count = 0;
				return false;
			}
		}

		private static FileType DetectJsonFileType(string jsonData, FileType knownFileType = 0)
		{
			if (jsonData == null || jsonData.Length == 0)
				return FileType.Unknown;

			if (jsonData[0] == '{') // json
			{
				// Character cards
				if (TavernCardV3.Validate(jsonData))
					return FileType.Character | FileType.TavernV3 | FileType.Json | knownFileType;
				if (TavernCardV2.Validate(jsonData))
					return FileType.Character | FileType.TavernV2 | FileType.Json | knownFileType;
				if (FaradayCardV4.Validate(jsonData))
					return FileType.Character | FileType.Faraday | FileType.Json | knownFileType;
				if (AgnaisticCard.Validate(jsonData))
					return FileType.Character | FileType.Agnaistic | FileType.Json | knownFileType;
				if (PygmalionCard.Validate(jsonData))
					return FileType.Character | FileType.Pygmalion | FileType.Json | knownFileType;

				// Lorebooks
				if (TavernWorldBook.Validate(jsonData))
					return FileType.Lorebook | FileType.TavernV2 | FileType.Json | knownFileType;
				if (TavernCardV2.CharacterBook.Validate(jsonData))
					return FileType.Lorebook | FileType.TavernV2 | FileType.Json | knownFileType;
				if (TavernLorebookV3.Validate(jsonData))
					return FileType.Lorebook | FileType.TavernV3 | FileType.Json | knownFileType;
				if (AgnaisticCard.CharacterBook.Validate(jsonData))
					return FileType.Lorebook | FileType.Agnaistic | FileType.Json | knownFileType;
			}
			else // not json
			{
				if (TextGenWebUICard.Validate(jsonData))
					return FileType.Character | FileType.TextGenWebUI | FileType.Yaml | knownFileType;
			}

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

			var fileType = DetectJsonFileType(json);

			// Text generation WebUI (yaml)
			if (fileType.Contains(FileType.TextGenWebUI))
			{
				var textGenWebUICard = TextGenWebUICard.FromYaml(json);
				if (textGenWebUICard != null)
				{
					jsonErrors = 0;
					Current.ReadTextGenWebUICard(textGenWebUICard);
					return Error.NoError;
				}
			}

			if (IsValidJson(json) == false)
			{
				jsonErrors = 0;
				return Error.InvalidJson;
			}	

			// Tavern (v3)
			if (fileType.Contains(FileType.TavernV3))
			{
				var tavernCardV3 = TavernCardV3.FromJson(json, out jsonErrors);
				if (tavernCardV3 != null)
				{
					Current.ReadTavernCard(tavernCardV3, null);

					// Read asset data
					var assets = new AssetCollection();
					foreach (var assetInfo in tavernCardV3.data.assets)
						assets.Add(AssetFile.FromV3Asset(assetInfo));
					Current.Card.assets = assets;

					// Load portrait image
					var portraitAsset = Current.Card.assets.GetPortrait();
					if (portraitAsset != null)
					{
						var portraitImage = portraitAsset.ToImage();
						if (portraitImage != null)
						{
							Current.Card.portraitImage = ImageRef.FromImage(portraitImage);

							if (Utility.IsAnimation(portraitAsset.data.bytes))
								portraitAsset.AddTags(AssetFile.Tag.PortraitOverride); // Promote to override
							else
								Current.Card.assets.Remove(portraitAsset);
						}
					}

					return Error.NoError;
				}
			}

			// Tavern (v2)
			if (fileType.Contains(FileType.TavernV2))
			{
				var tavernCardV2 = TavernCardV2.FromJson(json, out jsonErrors);
				if (tavernCardV2 != null)
				{
					Current.ReadTavernCard(tavernCardV2, null);
					return Error.NoError;
				}
			}

			// Agnaistic
			if (fileType.Contains(FileType.Agnaistic))
			{
				var agnaisticCard = AgnaisticCard.FromJson(json, out jsonErrors);
				if (agnaisticCard != null)
				{
					Current.ReadAgnaisticCard(agnaisticCard);
					return Error.NoError;
				}
			}

			// Pygmalion
			if (fileType.Contains(FileType.Pygmalion))
			{
				var pygmalionCard = PygmalionCard.FromJson(json);
				if (pygmalionCard != null)
				{
					jsonErrors = 0;
					Current.ReadPygmalionCard(pygmalionCard);
					return Error.NoError;
				}
			}

			jsonErrors = 0;
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
			string ext = Utility.GetFileExt(filename);
			if (ext == "png")
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
			{
				Current.ReadGingerCard(importResult.gingerData, image);
				if (importResult.tavernDataV3 != null && formats.Contains(Format.SillyTavernV3))
				{
					ExtractAssetsFromPNG(filename, importResult.tavernDataV3, out Current.Card.assets);
					
					// Copy over asset meta info
					if (importResult.gingerData.assets.Count == Current.Card.assets.Count)
					{
						for (int i = 0; i < importResult.gingerData.assets.Count; ++i)
						{
							var fromAsset = importResult.gingerData.assets[i];
							var toAsset = Current.Card.assets[i];

							if (fromAsset.name == toAsset.name && fromAsset.type == toAsset.type)
							{
								toAsset.uid = fromAsset.uid;
								toAsset.hash = fromAsset.hash;
								toAsset.knownWidth = fromAsset.knownWidth;
								toAsset.knownHeight = fromAsset.knownHeight;
							}
						}
					}
				}
			}
			else if (importResult.faradayData != null && formats.Contains(Format.Faraday))
				Current.ReadFaradayCard(importResult.faradayData, image);
			else if (importResult.tavernDataV3 != null && formats.Contains(Format.SillyTavernV3))
			{
				Current.ReadTavernCard(importResult.tavernDataV3, image);

				if (ext == "charx")
				{
					// Extract assets from charx archive
					ExtractAssetsFromArchive(filename, importResult.tavernDataV3, out Current.Card.assets);

					// Load portrait image
					var portraitAsset = Current.Card.assets.GetPortrait();
					if (portraitAsset != null)
					{
						var portraitImage = portraitAsset.ToImage();
						if (portraitImage != null)
						{
							Current.Card.portraitImage = ImageRef.FromImage(portraitImage);

							if (Utility.IsAnimation(portraitAsset.data.bytes))
								portraitAsset.AddTags(AssetFile.Tag.PortraitOverride); // Promote to override
							else
								Current.Card.assets.Remove(portraitAsset);
						}
					}
				}
				else if (ext == "png")
				{
					ExtractAssetsFromPNG(filename, importResult.tavernDataV3, out Current.Card.assets);
				}
			}
			else if (importResult.tavernDataV2 != null && formats.Contains(Format.SillyTavernV2))
				Current.ReadTavernCard(importResult.tavernDataV2, image);
			else
				return Error.UnrecognizedFormat;

			return bFallbackWarning ? Error.FallbackError : Error.NoError;
		}
				
		public static Error ImportCharacterFromBYAF(string filename)
		{
			if (File.Exists(filename) == false)
				return Error.FileNotFound;

			// Load character data
			ImportResult importResult;
			BackupData backup;
			var importError = BackyardArchiveUtil.ReadArchive(filename, out backup);
			if (importError != Error.NoError)
				return importError;

			if (backup.characterCards.IsEmpty() == false)
			{
				// Assign portrait
				Image mainPortrait;
				var portraitImage = backup.images.FirstOrDefault(i => i.characterIndex <= 0);
				if (portraitImage == null || !Utility.LoadImageFromMemory(portraitImage.data, out mainPortrait))
					mainPortrait = null;

				// Read staging from primary scenario
				if (backup.chats.IsEmpty() == false)
				{
					var primaryCard = backup.characterCards[0];
					var primaryChat = backup.chats.OrderByDescending(c => c.creationDate).First();
					primaryCard.data.system = primaryChat.staging.system;
					primaryCard.data.scenario = primaryChat.staging.scenario;
					primaryCard.data.greeting = primaryChat.staging.greeting.ToString();
					primaryCard.data.example = primaryChat.staging.example;
					primaryCard.data.grammar = primaryChat.staging.grammar;
				}

				// Read character(s)
				Current.ReadFaradayCards(backup.characterCards, mainPortrait, null);

				// Add portrait(s)
				IEnumerable<BackupData.Image> images = backup.images;
				if (portraitImage != null)
					images = images.Except(new BackupData.Image[] { portraitImage });

				foreach (var image in images)
				{
					var asset = new AssetFile() {
						actorIndex = image.characterIndex,
						assetType = AssetFile.AssetType.Icon,
						data = AssetData.FromBytes(image.data),
						name = Path.GetFileNameWithoutExtension(image.filename),
						ext = Utility.GetFileExt(image.filename),
						uriType = AssetFile.UriType.Embedded,
					};

					if (Utility.IsAnimation(image.data))
						asset.AddTags(AssetFile.Tag.Animation);
					Current.Card.assets.Add(asset);
				}

				// Animated main portrait?
				if (Utility.IsAnimation(portraitImage.data))
				{
					Current.Card.assets.Insert(0, new AssetFile() {
						actorIndex = 0,
						assetType = AssetFile.AssetType.Icon,
						data = AssetData.FromBytes(portraitImage.data),
						name = Path.GetFileNameWithoutExtension(portraitImage.filename),
						ext = Utility.GetFileExt(portraitImage.filename),
						uriType = AssetFile.UriType.Embedded,
						tags = new HashSet<StringHandle>() { AssetFile.Tag.Animation, AssetFile.Tag.PortraitOverride }
					});
				}

				// Background(s)
				foreach (var backgroundImage in backup.backgrounds)
				{
					var asset = new AssetFile() {
						actorIndex = -1,
						assetType = AssetFile.AssetType.Background,
						data = AssetData.FromBytes(backgroundImage.data),
						name = Path.GetFileNameWithoutExtension(backgroundImage.filename),
						ext = Utility.GetFileExt(backgroundImage.filename),
						uriType = AssetFile.UriType.Embedded,
					};

					if (Utility.IsAnimation(backgroundImage.data))
						asset.AddTags(AssetFile.Tag.Animation);
					Current.Card.assets.Add(asset);
				}

				return Error.NoError;
			}

			return Error.UnrecognizedFormat;
		}

		private static Error ExtractAssetsFromPNG(string filename, TavernCardV3 tavernV3Card, out AssetCollection assets)
		{
			var assetList = tavernV3Card.data.assets;
			if (assetList == null || assetList.Length == 0)
			{
				assets = new AssetCollection(); // Empty
				return Error.NoDataFound;
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

				assets = new AssetCollection();
				foreach (var assetInfo in assetList)
					assets.Add(AssetFile.FromV3Asset(assetInfo));

				// Read embedded PNGv3 assets
				if (metaData.Keys.ContainsAny(key => key.BeginsWith(AssetFile.PNGEmbedKeyPrefix_Risu)))
				{
					foreach (var kvp in metaData.Where(kvp => kvp.Key.BeginsWith(AssetFile.PNGEmbedKeyPrefix_Risu)))
					{
						// We need to handle both "chara-ext-asset_" and "chara-ext-asset_:" here
						// due to a bug in the RisuAI client
						string entryUri = kvp.Key.Substring(AssetFile.PNGEmbedKeyPrefix_Risu.Length);
						if (entryUri.BeginsWith(":"))
							entryUri = entryUri.Substring(1);

						int idxAsset = assets.FindIndex(a => string.Compare(string.Concat(a.uriPath ?? "", a.uriName ?? ""), entryUri, StringComparison.Ordinal) == 0);
						if (idxAsset == -1)
							continue; // Unreferenced asset

						byte[] buffer = Convert.FromBase64String(kvp.Value);
						assets[idxAsset].data = AssetData.FromBytes(buffer);
					}
				}

				if (assets.Count == 0)
					return Error.NoDataFound;
				return Error.NoError;	
			}
			catch
			{
				assets = new AssetCollection();
				return Error.FileReadError;
			}
		}
		
		public static bool ExportPNG(string filename, Image image, bool bOverwrite)
		{
			if (string.IsNullOrEmpty(filename) || image == null)
				return false;
			if (File.Exists(filename) && !bOverwrite)
				return false;

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

		public static ChatHistory ImportChat(string filename)
		{
			string textData;
			if (ReadTextFile(filename, out textData) == false)
				return null;

			string ext = Path.GetExtension(filename).ToLowerInvariant();

			if (ext == ".json")
			{
				if (BackyardChat.Validate(textData))   // Backyard JSON (c.ai?)
				{
					var chat = BackyardChat.FromJson(textData);
					if (chat != null)
						return ChatHistory.LegacyFix(chat.ToChat());
				}
				else if (TextGenWebUIChat.Validate(textData)) // Text generation web ui chat log
				{
					var chat = TextGenWebUIChat.FromJson(textData);
					if (chat != null)
						return chat.ToChat();
				} 
				else if (AgnaiChat.Validate(textData)) // Agnaistic
				{
					var chat = AgnaiChat.FromJson(textData);
					if (chat != null)
						return chat.ToChat();
				}
				else if (GingerChatV2.Validate(textData)) // Ginger V2
				{
					var chat = GingerChatV2.FromJson(textData);
					if (chat != null)
						return chat.ToChat();
				}
				else if (GingerChatV1.Validate(textData)) // Ginger V1
				{
					var chat = GingerChatV1.FromJson(textData);
					if (chat != null)
						return chat.ToChat();
				}
			}
			else if (ext == ".jsonl")
			{
				if (TavernChat.Validate(textData)) // SillyTavern chat
				{
					var chat = TavernChat.FromJson(textData);
					if (chat != null)
						return chat.ToChat();
				}
			}
			else if (ext == ".log")
			{
				if (GingerChatV2.Validate(textData)) // Ginger V2
				{
					var chat = GingerChatV2.FromJson(textData);
					if (chat != null)
						return chat.ToChat();
				}
				else if (GingerChatV1.Validate(textData)) // Ginger V1
				{
					var chat = GingerChatV1.FromJson(textData);
					if (chat != null)
						return chat.ToChat();
				}
			}
			else if (ext == ".txt")
			{
				if (TextFileChat.Validate(textData))
				{
					var chat = TextFileChat.FromString(textData);
					if (chat != null)
						return chat.ToChat();
				}
			}

			return null;
		}

		public static bool ExportBackyardChat(ChatHistory chatHistory, string filename)
		{
			if (chatHistory == null)
				return false;

			try
			{
				BackyardChat chat = BackyardChat.FromChat(chatHistory);
				string json = chat.ToJson();
				if (json == null)
					return false;

				return ExportTextFile(filename, json);
			}
			catch
			{
				return false;
			}
		}

		public static bool ExportTextGenWebUI(ChatHistory chatHistory, string filename)
		{
			if (chatHistory == null)
				return false;

			try
			{
				TextGenWebUIChat chat = TextGenWebUIChat.FromChat(chatHistory);
				string json = chat.ToJson();
				if (json == null)
					return false;

				return ExportTextFile(filename, json);
			}
			catch
			{
				return false;
			}
		}
		
		public static bool ExportTavernChat(ChatHistory chatHistory, string filename, string[] names)
		{
			if (chatHistory == null)
				return false;

			try
			{
				TavernChat chat = TavernChat.FromChat(chatHistory, names);
				string json = chat.ToJson();
				if (json == null)
					return false;

				return ExportTextFile(filename, json);
			}
			catch
			{
				return false;
			}
		}

		public static bool ExportGingerChat(Backyard.ChatInstance chatInstance, string filename)
		{
			if (chatInstance == null)
				return false;

			try
			{
				var speakers = new GingerChatV2.SpeakerList();
				for (int i = 0; i < chatInstance.participants.Length; ++i)
				{
					speakers.Add(new GingerChatV2.Speaker() {
						id = i.ToString(),
						name = chatInstance.participants[i],
					});
				}

				GingerChatV2 chat = GingerChatV2.FromChat(chatInstance, speakers);
				string json = chat.ToJson();
				if (json == null)
					return false;

				return ExportTextFile(filename, json);
			}
			catch
			{
				return false;
			}
		}
				
		public static bool IsValidJson(string json)
		{
			try
			{
				Newtonsoft.Json.Linq.JObject.Parse(json);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static bool Export(string filename, FileType fileType)
		{
			Generator.Option options = Generator.Option.None;
			if (fileType.Contains(FileType.Faraday))
				options |= Generator.Option.Faraday;
			if (fileType.Contains(FileType.TavernV2))
				options |= Generator.Option.SillyTavernV2;
			if (fileType.Contains(FileType.TavernV3))
				options |= Generator.Option.SillyTavernV3;
			if (fileType.Contains(FileType.TextGenWebUI))
				options |= Generator.Option.SinglePrompt;

			// Save as...
			var output = Generator.Generate(Generator.Option.Export | options);
			var gingerExt = GingerExtensionData.FromOutput(Generator.Generate(Generator.Option.Snippet | options));

			if (fileType.Contains(FileType.Ginger)) // Multi-format (png)
			{
				return Export(filename, (Image)Current.Card.portraitImage ?? DefaultPortrait.Image, Format.All);
			}
			else if (fileType.Contains(FileType.TavernV2 | FileType.Json)) // Tavern V2 (json)
			{
				var card = TavernCardV2.FromOutput(output);
				card.data.extensions.ginger = gingerExt;

				string tavernJson = card.ToJson();
				if (tavernJson != null && ExportTextFile(filename, tavernJson))
					return true; // Success
			}
			else if (fileType.Contains(FileType.TavernV3 | FileType.Json)) // Tavern V3 (json)
			{
				var card = TavernCardV3.FromOutput(output);
				card.data.extensions.ginger = gingerExt;

				var assets = (AssetCollection)Current.Card.assets.Clone();
				
				assets.AddPortraitAsset(FileType.Json, true);
				assets.Validate();

				card.data.assets = assets
					.Select(a => a.ToV3Asset(AssetFile.UriFormat.Data))
					.ToArray();

				string tavernJson = card.ToJson();
				if (tavernJson != null && ExportTextFile(filename, tavernJson))
					return true; // Success
			}
			else if (fileType.Contains(FileType.Agnaistic | FileType.Json)) // Agnaistic (json)
			{
				var card = AgnaisticCard.FromOutput(output);
				card.extensions.ginger = gingerExt;

				// Avatar image
				if (Current.Card.portraitImage != null)
					card.avatar = Utility.ImageToBase64(Current.Card.portraitImage);

				string agnaisticJson = card.ToJson();
				if (agnaisticJson != null && ExportTextFile(filename, agnaisticJson))
					return true; // Success
			}
			else if (fileType.Contains(FileType.Pygmalion | FileType.Json)) // Pygmalion / CAI (json)
			{
				var card = PygmalionCard.FromOutput(output);
				string json = card.ToJson();
				if (json != null && ExportTextFile(filename, json))
					return true; // Success
			}
			else if (fileType.Contains(FileType.TavernV2 | FileType.Png)) // Tavern V2 (png)
			{
				return Export(filename, (Image)Current.Card.portraitImage ?? DefaultPortrait.Image, Format.SillyTavernV2);
			}
			else if (fileType.Contains(FileType.TavernV3 | FileType.Png)) // Tavern V3 (json)
			{
				return Export(filename, (Image)Current.Card.portraitImage ?? DefaultPortrait.Image, Format.SillyTavernV3);
			}
			else if (fileType.Contains(FileType.Faraday | FileType.Png)) // Faraday (png)
			{
				return Export(filename, (Image)Current.Card.portraitImage ?? DefaultPortrait.Image, Format.Faraday);
			}
			else if (fileType.Contains(FileType.TavernV3 | FileType.CharX)) // Tavern V3 (charx)
			{
				return ExportToCharX(filename);
			}
			else if (fileType.Contains(FileType.TextGenWebUI | FileType.Yaml)) // Text Generation WebUI (Yaml)
			{
				var card = TextGenWebUICard.FromOutput(output);
				string yaml = card.ToYaml();
				if (yaml != null && ExportTextFile(filename, yaml))
				{
					// Save portrait
					if (Current.Card.portraitImage != null)
					{
						var pngFilename = Path.Combine(Path.GetDirectoryName(filename), string.Concat(Path.GetFileNameWithoutExtension(filename), ".png"));
						ExportPNG(pngFilename, Current.Card.portraitImage, false);
					}
					return true; // Success
				}
			}
			else if (fileType.Contains(FileType.Faraday | FileType.BackyardArchive)) // BYAF
			{
				BackupData backupData;
				BackupUtil.CreateBackup(out backupData);
				return BackyardArchiveUtil.WriteArchive(filename, backupData) == Error.NoError;
			}
			return false;
		}
	}
}
