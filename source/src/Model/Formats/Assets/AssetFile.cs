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
		public HashSet<StringHandle> tags = null; // Not CCV3 spec

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
				  && string.Compare(name, MainAssetName, StringComparison.OrdinalIgnoreCase) == 0
				  && actorIndex <= 0;
			}
		}
		
		public bool isMainPortraitOverride {
			get
			{
				return assetType == AssetType.Icon
				  && isEmbeddedAsset
				  && (string.Compare(name, PortraitOverrideName, StringComparison.OrdinalIgnoreCase) == 0
					|| HasTag(Tag.PortraitOverride));
			}
		}

		public bool isPortrait
		{
			get { return assetType == AssetType.Icon || assetType == AssetType.UserIcon || assetType == AssetType.Expression; }
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
			HashSet<StringHandle> tags = assetInfo.tags != null ? new HashSet<StringHandle>(assetInfo.tags.Select(t => new StringHandle(t))) : null;

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
							tags = tags,
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
					tags = tags,
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
				tags = tags,
			};
		}

		public TavernCardV3.Data.Asset ToV3Asset(UriFormat format)
		{
			return new TavernCardV3.Data.Asset() {
				type = GetTypeName(),
				uri = GetUri(format),
				name = name,
				ext = ext,
				tags = this.tags != null && this.tags.Count > 0 ? this.tags.Select(t => t.ToString()).ToArray() : null,
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
			var clone = (AssetFile)this.MemberwiseClone();
			if (this.tags != null)
				clone.tags = new HashSet<StringHandle>(this.tags);
			return clone;
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

			var tagsNode = xmlNode.GetFirstElement("Tags");
			if (tagsNode != null)
			{
				tags = new HashSet<StringHandle>(
					Utility.ListFromCommaSeparatedString(tagsNode.GetTextValue())
						.Select(s => new StringHandle(s)));
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

			if (tags != null && tags.Count > 0)
			{
				var tagsNode = xmlNode.AddElement("Tags");
				tagsNode.AddTextValue(Utility.ListToCommaSeparatedString(tags));
			}
		}

		public bool HasTag(StringHandle tag)
		{
			return tags != null && tags.Contains(tag);
		}
		
		public void AddTags(params StringHandle[] tags)
		{
			if (this.tags == null)
				this.tags = new HashSet<StringHandle>(tags);
			else
				this.tags.UnionWith(tags);
		}

		public void RemoveTags(params StringHandle[] tags)
		{
			if (this.tags == null || this.tags.Count == 0)
				return;

			this.tags.ExceptWith(tags);
		}

		public int actorIndex
		{
			get
			{
				if (tags == null || tags.Count == 0 || assetType != AssetType.Icon)
					return -1;

				var actorTag = tags.FirstOrDefault(t => t.BeginsWith(Tag.ActorAsset.ToString()));
				if (StringHandle.IsNullOrEmpty(actorTag))
					return -1;

				int index;
				if (int.TryParse(actorTag.ToString().Substring(Tag.ActorAsset.Length), out index))
					return Math.Max(index, 0);
				return -1;
			}

			set
			{
				if (this.tags != null)
				{
					var existing = this.tags.Where(t => t.BeginsWith(Tag.ActorAsset.ToString())).ToArray();
					this.tags.ExceptWith(existing);
				}

				if (value < 0)
					return; // Not actor

				if (this.tags == null)
					this.tags = new HashSet<StringHandle>();

				this.tags.Add(string.Concat(Tag.ActorAsset, value));
			}
		}

		public static class Tag
		{
			public static StringHandle PortraitOverride = "portrait-override";
			public static StringHandle PortraitBackground = "portrait-background";
			public static StringHandle Animated = "animated";
			public static StringHandle ActorAsset = "actor-";
		}

		public static AssetFile FromImage(Image image, AssetType assetType = AssetType.Icon)
		{
			if (image == null)
				return null;

			return new AssetFile() {
				name = "Image",
				ext = "png",
				assetType = assetType,
				data = AssetData.FromBytes(Utility.ImageToMemory(image, Utility.ImageFileFormat.Jpeg)),
				uriType = UriType.Embedded,
			};
		}

		public Image ToImage()
		{
			if (!(assetType == AssetType.Icon
				|| assetType == AssetType.UserIcon
				|| assetType == AssetType.Expression
				|| assetType == AssetType.Background))
				return null;

			if (isEmbeddedAsset == false || data.isEmpty || Utility.IsSupportedImageFileExt(ext) == false)
				return null;

			Image image;
			if (Utility.LoadImageFromMemory(data.bytes, out image))
				return image;
			return null;
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
}
