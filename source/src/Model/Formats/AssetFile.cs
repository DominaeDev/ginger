using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Xml;

namespace Ginger
{
	public class AssetFile : ICloneable, IXmlLoadable, IXmlSaveable
	{
		public static readonly string MainAssetName = "main";
		public static readonly string PortraitOverrideName = "Portrait (main)";

		public string name;
		public AssetType assetType
		{
			get { return AssetTypeFromString(type); }
			set
			{
				switch (value)
				{
				case AssetType.Icon:
					type = "icon";
					break;
				case AssetType.UserIcon:
					type = "user_icon";
					break;
				case AssetType.Background:
					type = "background";
					break;
				case AssetType.Expression:
					type = "emotion";
					break;
				case AssetType.Other:
					type = "other";
					break;
				default:
					type = null;
					break;
				}
			}
		}
		public string type;
		public string ext;
		public AssetData data;

		// Meta
		public string uid
		{
			get { return _uid; }
			set { _uid = value; }
		}
		private string _uid;
		public string hash
		{ 
			get
			{
				if (string.IsNullOrEmpty(_hash))
					_hash = data.hash;
				return _hash;
			}
			set { _hash = value; }
		}
		private string _hash;
		public int knownWidth;	// For image assets
		public int knownHeight;	// For image assets

		// Uri
		public UriType uriType = UriType.Undefined;
		public string fullUri { get; private set; }
		public string uriPath { get; private set; }
		public string uriName { get; set; } // with ext

		public bool isDefaultAsset { get { return uriType == UriType.Default; } }
		public bool isEmbeddedAsset { get { return uriType == UriType.Embedded; } }
		public bool isRemoteAsset { get { return uriType == UriType.Custom; } }
		
		public bool isMainAsset {
			get
			{
				return isEmbeddedAsset
				  && string.Compare(name, MainAssetName, StringComparison.OrdinalIgnoreCase) == 0;
			}
		}
		public bool isMainPortraitOverride {
			get
			{
				return assetType == AssetType.Icon
				  && isEmbeddedAsset
				  && string.Compare(name, PortraitOverrideName, StringComparison.OrdinalIgnoreCase) == 0;
			}
		}

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
			Custom		= 6,
		};

		public enum UriType
		{
			Undefined,
			Default,
			Embedded,
			Custom,
		}

		public static AssetType AssetTypeFromString(string value)
		{
			if (string.IsNullOrEmpty(value))
				return AssetType.Undefined;

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
			default:
				return AssetType.Custom;
			}
		}

		public AssetFile()
		{
			_uid = Guid.NewGuid().ToString();
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
			case AssetType.Custom:
				return string.IsNullOrWhiteSpace(type) ? "other" : type;
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
					type = type,
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
							type = type,
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
					type = type,
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
			if (uri.BeginsWith("file:///"))
				uriPath = uri.Substring(8);
			else if (idxProtocol != -1)
				uriPath = uri.Substring(idxProtocol + 3);

			return new AssetFile() {
				name = name,
				ext = ext,
				type = type,
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
				name = name ?? MainAssetName,
				ext = ext ?? "unknown",
				uriName = null,
				uriPath = null,
			};
		}

		public static AssetFile MakeRemote(AssetType type, string uri)
		{
			int idxProtocol = uri.IndexOf("://");
			string protocol;
			string path;
			if (uri.BeginsWith("file:///"))
			{
				protocol = "file:///";
				path = uri.Substring(8);
			}
			else if (idxProtocol != -1)
			{
				protocol = uri.Substring(0, idxProtocol + 3);
				path = uri.Substring(idxProtocol + 3);
			}
			else
			{
				protocol = "http://";
				path = uri;
			}

			// Asset name
			string name = null;
			int pos_slash = path.LastIndexOf('/');
			if (pos_slash != -1)
				name = path.Substring(pos_slash + 1);
			if (string.IsNullOrEmpty(name))
				name = path;

			// Extension
			string ext = Utility.GetFileExt(name);

			name = Path.GetFileNameWithoutExtension(name);

			if (type == AssetType.Undefined)
			{ 
				bool isImage = Utility.IsSupportedImageFileExt(ext);
				type = isImage ? AssetType.Icon : AssetType.Other;
			}

			return new AssetFile() {
				assetType = type,
				fullUri = string.Concat(protocol, path),
				uriType = UriType.Custom,
				name = name ?? "untitled",
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

		public bool LoadFromXml(XmlNode xmlNode)
		{
			name = xmlNode.GetValueElement("Name", null);
			type = xmlNode.GetValueElement("Type", null);

			var uriNode = xmlNode.GetFirstElement("Uri");
			if (uriNode == null)
				return false;
			fullUri = uriNode.GetTextValue();
			uriType = uriNode.GetAttributeEnum("type", UriType.Undefined);

			var metaNode = xmlNode.GetFirstElement("Meta");
			if (metaNode != null)
			{
				uid = metaNode.GetAttribute("uid", null);
				hash = metaNode.GetAttribute("hash", null);
				knownWidth = metaNode.GetAttributeInt("width");
				knownHeight = metaNode.GetAttributeInt("height");
			}

			return name != null && type != null && fullUri.Length > 0 && uriType != UriType.Undefined;
		}

		public void SaveToXml(XmlNode xmlNode)
		{
			xmlNode.AddValueElement("Name", name);
			xmlNode.AddValueElement("Type", type);
			var uriNode = xmlNode.AddValueElement("Uri", fullUri);
			uriNode.AddAttribute("type", EnumHelper.ToString(uriType));

			if (uid != null)
			{
				var metaNode = xmlNode.AddElement("Meta");
				metaNode.AddAttribute("uid", uid);
				if (hash != null)
					metaNode.AddAttribute("hash", hash);
				if (knownWidth > 0 && knownHeight > 0)
				{
					metaNode.AddAttribute("width", knownWidth);
					metaNode.AddAttribute("height", knownHeight);
				}
			}
		}
	}

	public struct AssetData
	{
		public byte[] bytes { get; private set; }
		public long length { get { return bytes != null ? bytes.Length : 0; } }
		public string hash { get { return _hash; } }
		private string _hash;

		public bool isEmpty { get { return bytes == null || bytes.Length == 0; } }

		public static AssetData FromBytes(byte[] bytes)
		{
			string hash;

			if (bytes != null && bytes.Length > 0)
			{
				using (var sha1 = new System.Security.Cryptography.SHA256CryptoServiceProvider())
				{
					hash = string.Concat(sha1.ComputeHash(bytes).Select(x => x.ToString("X2")));
				}
			}
			else
				hash = "";

			return new AssetData() {
				bytes = bytes,
				_hash = hash,
			};
		}

		public static AssetData FromFile(string filename)
		{
			return FromBytes(Utility.LoadFile(filename));
		}
	}

	public class AssetCollection : List<AssetFile>, ICloneable
	{
		public AssetCollection() : base() {}
		public AssetCollection(AssetCollection other) : base(other) {}
		public AssetCollection(IEnumerable<AssetFile> other) : base(other) {}

		public AssetFile GetPortraitAsset()
		{
			var images = this.Where(a => a.assetType == AssetFile.AssetType.Icon && a.isDefaultAsset == false).ToList();
			if (images.Count == 0)
				return null;
			
			AssetFile assetData;
			if (images.Count == 1)
				return images[0];

			var mainAsset = images.FirstOrDefault(a => a.isMainAsset);
			if (mainAsset != null)
				return mainAsset;
			else
				return images[0];
		}

		public Image GetPortraitImage()
		{
			var portraitAsset = GetPortraitAsset();
			if (portraitAsset == null || portraitAsset.data.isEmpty)
				return null;

			Image image;
			if (Utility.LoadImageFromMemory(portraitAsset.data.bytes, out image))
				return image;

			return null;
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
				int idx = this.FindIndex(a => a.assetType == AssetFile.AssetType.Icon && a.isMainAsset);
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
			foreach (var type in this.Select(a => a.type).Distinct())
			{
				var assetType = AssetFile.AssetTypeFromString(type);
				var used_names = new Dictionary<string, int>();

				for (int i = 0; i < this.Count; ++i)
				{
					var asset = this[i];
					if (asset.type != type)
						continue;

					string name = (this[i].name ?? "").ToLowerInvariant().Trim();
					if (name == "")
						this[i].name = name = "untitled"; // Name mustn't be empty

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

					this[i].name = testName;
				}
			}

			var validated = this
				.GroupBy(a => a.assetType)
				.SelectMany(g => 
				{
					var assetType = g.Key;
					var assetsOfType = g.ToList();

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
			var list = new AssetCollection();
			for (int i = 0; i < this.Count; ++i)
				list.Add((AssetFile)this[i].Clone());
			return list;
		}

		public void AddPortraitAsset(FileUtil.FileType fileType) // Exporting
		{
			if (this.ContainsAny(a => a.isMainPortraitOverride))
			{
				int idxOverride = this.FindIndex(a => a.isMainPortraitOverride);
				this[idxOverride].name = AssetFile.MainAssetName;
				this.RemoveAll(a => a.isDefaultAsset && a.assetType == AssetFile.AssetType.Icon);
				return; // A portrait image already exists
			}

			// Remove any existing default icon(s)
			this.RemoveAll(a => a.isDefaultAsset && a.assetType == AssetFile.AssetType.Icon);

			if (fileType == FileUtil.FileType.Png && this.ContainsNoneOf(a => a.assetType == AssetFile.AssetType.Icon && a.isMainAsset))
			{
				this.Insert(0, AssetFile.MakeDefault(AssetFile.AssetType.Icon, AssetFile.MainAssetName, "png")); // Add ccdefault
				return;
			}

			// Embed current portrait image
			Image image = Current.Card.portraitImage;
			if (image == null)
				return;

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
						name = AssetFile.MainAssetName,
						ext = "png",
						uriType = AssetFile.UriType.Embedded,
						data = AssetData.FromBytes(stream.ToArray()),
					});
				}
			}
			catch
			{
			}
		}

		public bool HasMainPortraitOverride()
		{
			return GetMainPortraitOverride() != null;
		}

		public AssetFile GetMainPortraitOverride()
		{
			var asset = this.FirstOrDefault(a => a.isMainPortraitOverride
				&& a.isDefaultAsset == false
				&& Utility.IsSupportedImageFileExt(a.ext));
			if (asset == null)
			{
				asset = this.FirstOrDefault(a => a.assetType == AssetFile.AssetType.Icon && a.isMainAsset
					&& a.isDefaultAsset == false
					&& Utility.IsSupportedImageFileExt(a.ext));
			}
			return asset;
		}

		public bool ReplaceMainPortraitOverride(string filename, out AssetFile asset)
		{
			var ext = Utility.GetFileExt(filename);
			var bytes = Utility.LoadFile(filename);
			if (bytes == null || bytes.Length == 0)
			{
				asset = default(AssetFile);
				return false;
			}

			if (Utility.IsSupportedImageFileExt(ext) == false)
			{
				asset = default(AssetFile);
				return false;
			}

			int width, height;
			Utility.GetImageDimensions(bytes, out width, out height);

			// Remove existing
			this.RemoveAll(a => a.isDefaultAsset && a.assetType == AssetFile.AssetType.Icon);
			RemoveMainPortraitOverride();
			
			// Add new asset
			asset = new AssetFile() {
				name = AssetFile.PortraitOverrideName,
				uriType = AssetFile.UriType.Embedded,
				assetType = AssetFile.AssetType.Icon,
				data = AssetData.FromBytes(bytes),
				knownWidth = width,
				knownHeight = height,
				ext = ext,
			};
			this.Insert(0, asset);
			return true;
		}

		public bool RemoveMainPortraitOverride()
		{
			int idxExisting = this.FindIndex(a => a.isMainPortraitOverride);
			if (idxExisting == -1)
				idxExisting = this.FindIndex(a => a.assetType == AssetFile.AssetType.Icon && a.isMainAsset);
			if (idxExisting != -1)
			{
				this.RemoveAt(idxExisting);
				return true;
			}
			return false;
		}
	}
}
