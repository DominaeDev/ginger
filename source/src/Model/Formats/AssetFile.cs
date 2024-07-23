using System;
using System.Collections.Generic;
using System.Drawing;
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
		public static readonly string PNGEmbedKeyPrefix = "chara-ext-asset_"; // ?

		public enum AssetType
		{
			Undefined,
			Icon,
			UserIcon,
			Background,
			Expression, // Emotion
			Other,
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

			uri = uri.Replace("embedded://", CharXEmbedUriPrefix); // Quirk in the v3 spec
			uri = uri.Replace(PNGEmbedUriPrefix, CharXEmbedUriPrefix);
			uri = uri.Replace('\\', '/');

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
			
			// Remote/Custom asset
			if (uri.BeginsWith(CharXEmbedUriPrefix) == false)
			{
				string uriPath = uri;
				int idxProtocol = uri.IndexOf("://");
				if (idxProtocol == -1)
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

			// Embedded asset
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
		}

		public string GetUri(UriFormat format)
		{
			if (uriType == UriType.Default)
				return DefaultUri;
			else if (uriType == UriType.Custom)
				return fullUri;
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
		public byte[] data;
		public long length { get { return data != null ? data.Length : 0; } }

		public static AssetData FromBytes(byte[] bytes)
		{
			return new AssetData() {
				data = bytes,
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
				using (var stream = new MemoryStream(assetData.data))
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
					string name = assetsOfType[i].name.ToLowerInvariant().Trim();
					if (name == "")
						assetsOfType[i].name = name = "untitled"; // Name mustn't be empty

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
			return this.ContainsAny(a => a.isDefaultAsset && a.assetType == AssetFile.AssetType.Icon);
		}
	}
}
