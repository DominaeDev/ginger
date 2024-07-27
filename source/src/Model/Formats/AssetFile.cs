using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Ginger
{
	public class AssetFile : ICloneable
	{
		public string name;
		public AssetType assetType = AssetType.Undefined;
		public string ext;
		public AssetData data;

		// Uri
		public UriType uriType = UriType.Undefined;
		public string fullUri { get; private set; }
		public string uriPath { get; private set; }
		public string uriName { get; set; } // with ext

		public bool isDefaultAsset { get { return uriType == UriType.Default; } }
		public bool isEmbeddedAsset { get { return uriType == UriType.Embedded; } }

		public static readonly string DefaultUri = "ccdefault:";
		public static readonly string CharXEmbedUriPrefix = "embeded://";
		public static readonly string PNGEmbedUriPrefix = "__asset:";
		public static readonly string PNGEmbedKeyPrefix = "chara-ext-asset_:";
		public static readonly string PNGEmbedKeyPrefix_Risu = "chara-ext-asset_";

		public enum AssetType
		{
			Undefined	= 0,
			Icon		= 1,
			UserIcon	= 2,
			Background	= 3,
			Expression	= 4, // Emotion
			Other		= 5,
		};

		public enum UriType
		{
			Undefined,
			Default,
			Embedded,
			Custom,
		}

		private static AssetType AssetTypeFromString(string value)
		{
			if (string.IsNullOrEmpty(value) == false)
			{
				value = value.Trim().ToLowerInvariant();
				switch (value)
				{
				case "icon":
					return AssetType.Icon;
				case "user_icon":
					return AssetType.UserIcon;
				case "background":
					return AssetType.Background;
				case "emotion":
					return AssetType.Expression;
				case "other":
					return AssetType.Other;
				}
			}
			return AssetType.Undefined;
		}

		public string GetTypeName()
		{
			switch (assetType)
			{
			case AssetType.Icon:
				return "icon";
			case AssetType.UserIcon:
				return "user_icon";
			case AssetType.Background:
				return "background";
			case AssetType.Expression:
				return "emotion";
			default:
				return "other";
			}
		}

		public static AssetFile FromV3Asset(TavernCardV3.Data.Asset assetInfo, byte[] data = null)
		{
			string name = assetInfo.name.Trim();
			string uri = assetInfo.uri.Trim();
			string ext = assetInfo.ext != null ? assetInfo.ext : null;
			string type = assetInfo.type.Trim().ToLowerInvariant();

			// Default asset
			if (uri.Length == 0 || string.Compare(uri, DefaultUri, StringComparison.OrdinalIgnoreCase) == 0)
			{
				return new AssetFile() {
					name = name,
					ext = ext ?? "unknown",
					assetType = AssetTypeFromString(type),
					uriType = UriType.Default,
					fullUri = DefaultUri,
					uriPath = null,
					uriName = null,
				};
			}
			
			// Data uri
			if (uri.BeginsWith("data:"))
			{
				int pos_mime_end = uri.IndexOf(";base64,");
				if (pos_mime_end != -1)
				{
					try
					{
						string mimeType = uri.Substring(5, pos_mime_end - 5);
						byte[] bytes = Convert.FromBase64String(uri.Substring(pos_mime_end + 8));
						return new AssetFile() {
							name = name,
							ext = ext,
							assetType = AssetTypeFromString(type),
							uriType = UriType.Embedded,
							fullUri = string.Concat(CharXEmbedUriPrefix, name ?? "unnamed", ext != null ? "." : "", ext ?? ""),
							uriPath = null,
							uriName = null,
							data = AssetData.FromBytes(bytes),
						};
					}
					catch
					{
					}
				}
			}

			uri = uri.Replace("embedded://", CharXEmbedUriPrefix); // Quirk in the v3 spec
			uri = uri.Replace(PNGEmbedUriPrefix, CharXEmbedUriPrefix);
			uri = uri.Replace('\\', '/');

			// Embedded asset
			if (uri.BeginsWith(CharXEmbedUriPrefix))
			{
				string path = null;
				string filename = uri.Substring(CharXEmbedUriPrefix.Length);
				int idxPath = filename.LastIndexOf('/');
				if (idxPath != -1)
				{
					path = filename.Substring(0, idxPath + 1);
					filename = filename.Substring(idxPath + 1);
				}

				return new AssetFile() {
					name = name,
					ext = ext,
					assetType = AssetTypeFromString(type),
					uriType = UriType.Embedded,
					fullUri = uri,
					uriPath = path,
					uriName = filename,
					data = AssetData.FromBytes(data),
				};
			}

			// Remote/other asset
			string uriPath = uri;
			int idxProtocol = uri.IndexOf("://");
			if (idxProtocol != -1)
				uriPath = uri.Substring(idxProtocol + 3);

			return new AssetFile() {
				name = name,
				ext = ext,
				assetType = AssetTypeFromString(type),
				uriType = UriType.Custom,
				fullUri = uri,
				uriPath = uriPath,
				uriName = null,
			};
		}

		public TavernCardV3.Data.Asset ToV3Asset(UriFormat format)
		{
			return new TavernCardV3.Data.Asset() 
			{
				type = GetTypeName(),
				uri = GetUri(format),
				name = name,
				ext = ext,
			};
		}

		public static AssetFile MakeDefault(AssetType type, string name, string ext = null)
		{
			return new AssetFile() {
				assetType = type,
				fullUri = DefaultUri,
				uriType = UriType.Default,
				name = name ?? "main",
				ext = ext ?? "unknown",
				uriName = null,
				uriPath = null,
			};
		}

		public enum UriFormat 
		{ 
			Png,
			Png_Prefix,
			CharX, 
			CharX_Prefix, 
			Data,
		}

		public string GetUri(UriFormat format)
		{
			if (uriType == UriType.Default)
				return DefaultUri;
			else if (uriType == UriType.Custom)
				return fullUri;
			else if (format == UriFormat.Data)
			{
				string mimeType;
				switch (ext)
				{
				case "jpeg":
				case "jpg":
					mimeType = "image/jpeg";
					break;
				case "gif":
				case "png":
				case "apng":
				case "avif":
				case "webp":
				case "tiff":
					mimeType = string.Format("image/{0}", ext);
					break;
				case "wav":
				case "mp3":
				case "ogg":
				case "wma":
					mimeType = string.Format("audio/{0}", ext);
					break;
				case "mp4":
				case "mpeg":
				case "wmv":
				case "av1":
					mimeType = string.Format("audio/{0}", ext);
					break;
				case "mkv":
					mimeType = "audio/matroska";
					break;
				case "mov":
					mimeType = "audio/quicktime";
					break;
				default:
					mimeType = "application/octet-stream";
					break;
				}

				return string.Concat("data:", mimeType, ";base64,", data.length > 0 ?
					Convert.ToBase64String(data.bytes) : "");
			}
			else
			{
				string path;
				if (assetType == AssetType.Icon)
					path = "assets/icon/";
				else if (assetType == AssetType.UserIcon)
					path = "assets/user_icon/";
				else if (assetType == AssetType.Background)
					path = "assets/background/";
				else if (assetType == AssetType.Expression)
					path = "assets/emotion/";
				else
					path = "assets/other/";

				switch (format)
				{
				case UriFormat.Png:
					return string.Concat(path, uriName ?? "");
				case UriFormat.Png_Prefix:
					return string.Concat(PNGEmbedUriPrefix, path, uriName ?? "");
				default:
				case UriFormat.CharX:
					return string.Concat(path, uriName ?? "", ext != null ? "." : "", ext ?? "");
				case UriFormat.CharX_Prefix:
					return string.Concat(CharXEmbedUriPrefix, path, uriName ?? "", ext != null ? "." : "", ext ?? "");
				}
			}
		}

		public object Clone()
		{
			return this.MemberwiseClone();
		}
	}

	public struct AssetData
	{
		public byte[] bytes;
		public long length { get { return bytes != null ? bytes.Length : 0; } }

		public static AssetData FromBytes(byte[] bytes)
		{
			return new AssetData() {
				bytes = bytes,
			};
		}
	}

	public class AssetCollection : List<AssetFile>, ICloneable
	{
		public AssetCollection() : base() {}
		public AssetCollection(AssetCollection other) : base(other) {}
		public AssetCollection(IEnumerable<AssetFile> other) : base(other) {}

		public Image GetPortraitImage()
		{
			var images = this.Where(a => a.assetType == AssetFile.AssetType.Icon).ToList();
			if (images.Count == 0)
				return null;
			
			AssetData assetData;
			if (images.Count == 1)
				assetData = images[0].data;
			else
			{
				var mainAsset = images.FirstOrDefault(a => a.name == "main");
				if (mainAsset != null)
					assetData = mainAsset.data;
				else
					assetData = images[0].data;
			}
			if (assetData.length == 0)
				return null;

			try
			{
				using (var stream = new MemoryStream(assetData.bytes))
				{
					return Image.FromStream(stream);
				}
			}
			catch
			{
				return null;
			}
		}

		public bool RemovePortraitImage()
		{
			int imageCount = this.Count(a => a.assetType == AssetFile.AssetType.Icon);
			if (imageCount == 0)
				return false;

			if (imageCount == 1)
			{
				this.RemoveAll(a => a.assetType == AssetFile.AssetType.Icon);
				return true;
			}
			else
			{
				int idx = this.FindIndex(a => string.Compare(a.name, "main", StringComparison.OrdinalIgnoreCase) == 0);
				if (idx != -1)
				{
					this.RemoveAt(idx);
					return true;
				}

				idx = this.FindIndex(a => a.assetType == AssetFile.AssetType.Icon);
				this.RemoveAt(idx);
				return true;
			}
		}

		public void Validate()
		{
			var validated = this
				// Ensure there is at least one "main" asset per type
				.GroupBy(a => a.assetType)
				.Select(g => {
					var assetType = g.Key;
					var assetsOfType = g.ToList();

					if (assetType == AssetFile.AssetType.Other || assetType == AssetFile.AssetType.Undefined)
						return new {
							type = assetType,
							assets = assetsOfType,
						};

					string mainName = assetType == AssetFile.AssetType.Expression ? "neutral" : "main";

					if (assetsOfType.Count == 1)
					{
						assetsOfType[0].name = mainName;
						return new {
							type = assetType,
							assets = assetsOfType,
						};
					}

					int nMain = this.Count(a => string.Compare(a.name, mainName, StringComparison.OrdinalIgnoreCase) == 0);
					if (nMain == 0)
						assetsOfType[0].name = mainName;

					return new {
						type = assetType,
						assets = assetsOfType,
					};
				})
			.SelectMany(x => {
				// Ensure unique names within each asset group
				var assetsOfType = x.assets;

				var used_names = new Dictionary<string, int>();
				for (int i = 0; i < assetsOfType.Count; ++i)
				{
					string name = (assetsOfType[i].name ?? "").ToLowerInvariant().Trim();
					if (name == "")
						assetsOfType[i].name = name = "untitled"; // Name mustn't be empty

					if (used_names.ContainsKey(name) == false)
					{
						used_names.Add(name, 1);
						continue;
					}

					int count = used_names[name];
					string testName = string.Format("{0}_{1:00}", name, ++count);
					while (used_names.ContainsKey(testName))
						testName = string.Format("{0}_{1:00}", name, ++count);
					used_names.Add(testName, 1);
					used_names[name] = count;
					assetsOfType[i].name = testName;
				}

				// Assign new filenames
				int counter = 1;
				for (int i = 0; i < assetsOfType.Count; ++i)
				{
					if (assetsOfType[i].isEmbeddedAsset == false)
						continue;
					
					assetsOfType[i].uriName = string.Format("{0:00}", counter++);
				}

				return assetsOfType;
			})
			.ToArray();

			Clear();
			AddRange(validated);
		}

		public object Clone()
		{
			var list = new List<AssetFile>(this.Count);
			for (int i = 0; i < this.Count; ++i)
				list.Add((AssetFile)this[i].Clone());
			return new AssetCollection(list);			
		}

		public bool HasDefaultIcon()
		{
			return this.ContainsAny(a => a.assetType == AssetFile.AssetType.Icon && (a.isDefaultAsset || a.name == "main" ));
		}

		public void BakePortraitImage(bool bAddDefault = true)
		{
			// Add current portrait image
			Image image = Current.Card.portraitImage;
			if (image != null)
			{
				// Write image to buffer
				try
				{
					using (var stream = new MemoryStream())
					{
						if (image.RawFormat.Equals(ImageFormat.Png)) // Save png
						{
							image.Save(stream, ImageFormat.Png);
						}
						else // or convert to png
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

						// Add asset
						this.Insert(0, new AssetFile() {
							assetType = AssetFile.AssetType.Icon,
							name = "main",
							ext = "png",
							uriType = AssetFile.UriType.Embedded,
							data = new AssetData() {
								bytes = stream.ToArray(),
							},
						});

						// Remove any existing default icon(s)
						this.RemoveAll(a => a.isDefaultAsset && a.assetType == AssetFile.AssetType.Icon);
						bAddDefault = false;
					}
				}
				catch
				{
				}
			}

			if (bAddDefault)
			{
				// Add default icon asset
				this.Insert(0, AssetFile.MakeDefault(AssetFile.AssetType.Icon, "main"));
			}
		}
	}
}
