using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Ginger
{
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
			if (assetData.Length == 0)
				return null;

			try
			{
				using (var stream = new MemoryStream(assetData.Data))
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
				int idx = this.FindIndex(a => string.Compare(a.name, "main", System.StringComparison.OrdinalIgnoreCase) == 0);
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
					string name = assetsOfType[i].name.ToLowerInvariant();
					if (name == "")
						assetsOfType[i].name = name = "untitled"; // Name cannot be empty

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
				// Set uri
				if (asset.isDefaultAsset)
					asset.uri = AssetFile.DefaultURI;
				else
					asset.uri = string.Concat(asset.protocol, asset.GetPath(), EscapeName(asset.name), ".", asset.ext).ToLowerInvariant();

				// Fix ext
				if (asset.ext != null)
					asset.ext = asset.ext.ToLowerInvariant();
				else
					asset.ext = "unknown";
				return asset;
			})
			.ToArray();
			
			Clear();
			AddRange(validated);
		}

		public static string EscapeName(string name)
		{
			var sb = new StringBuilder(name.Length);
			var illegal_chars = new char[] { '%', ':', '\\', '/', '?', '"', '*', '<', '>', '|' };
			for (int i = 0; i < name.Length; ++i)
			{
				char ch = name[i];
				if ((ch & 0xFF) != ch)
					sb.AppendFormat("%u{0:X4}", (int)ch);
				else if ((ch >= 0x00 && ch <= 0x1f) || (ch >= 0x7f && ch <= 0x9f) || ch >= 0xA0 || illegal_chars.Contains(ch))
					sb.AppendFormat("%{0:X2}", (int)(ch & 0xff));
				else
					sb.Append(ch);
			}
			return sb.ToString();
		}

		public object Clone()
		{
			var list = new List<AssetFile>(this.Count);
			for (int i = 0; i < this.Count; ++i)
				list.Add((AssetFile)this[i].Clone());
			return new AssetCollection(list);			
		}
	}

	public class AssetFile : ICloneable
	{
		public string name;
		public string ext;
		public string uri;
		public AssetType assetType = AssetType.Undefined;
		public FileType fileType = FileType.Undefined;
		public AssetData data;
		public string protocol
		{
			get
			{
				if (string.IsNullOrEmpty(this.uri))
					return "";
				int idxProtocol = this.uri.IndexOf("://");
				if (idxProtocol == -1)
					return "";
				return this.uri.Substring(0, idxProtocol + 3).ToLowerInvariant();
			}
		}

		public static readonly string DefaultURI = "ccdefault:";
		public bool isDefaultAsset { get { return uri == DefaultURI; } }

		public enum AssetType 
		{
			Undefined,
			Icon,
			UserIcon,
			Background,
			Expression, // Emotion
			Other,
		};

		public enum FileType 
		{
			Undefined,
			Image,
			Audio,
			Video,
			Model3D,
			ModelAI,
			Live2D,
			Font,
			Script,
			Other,
		};

		public string GetPath()
		{
			string typePath;
			switch (assetType)
			{
			case AssetType.Icon:
				typePath = "icon";
				break;
			case AssetType.UserIcon:
				typePath = "user_icon";
				break;
			case AssetType.Background:
				typePath = "background";
				break;
			case AssetType.Expression:
				typePath = "emotion";
				break;
			case AssetType.Other:
			default:
				typePath = "other";
				break;
			}

			switch (fileType)
			{
			case FileType.Image:
				return string.Concat("assets/", typePath, "/images/");
			case FileType.Audio:
				return string.Concat("assets/", typePath, "/audio/");
			case FileType.Video:
				return string.Concat("assets/", typePath, "/video/");
			case FileType.Model3D:
				return string.Concat("assets/", typePath, "/3d/");
			case FileType.ModelAI:
				return string.Concat("assets/", typePath, "/ai/");
			case FileType.Live2D:
				return string.Concat("assets/", typePath, "/l2d/");
			case FileType.Font:
				return string.Concat("assets/", typePath, "/fonts/");
			case FileType.Script:
				return string.Concat("assets/", typePath, "/code/");
			case FileType.Other:
			default:
				return string.Concat("assets/", typePath, "/other/");
			}
		}

		public static FileType FileTypeFromPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return FileType.Undefined;

			if (path.EndsWith("/images/"))
				return FileType.Image;
			if (path.EndsWith("/audio/"))
				return FileType.Audio;
			if (path.EndsWith("/video/"))
				return FileType.Video;
			if (path.EndsWith("/3d/"))
				return FileType.Model3D;
			if (path.EndsWith("/ai/"))
				return FileType.ModelAI;
			if (path.EndsWith("/l2d/"))
				return FileType.Live2D;
			if (path.EndsWith("/fonts/"))
				return FileType.Font;
			if (path.EndsWith("/code/"))
				return FileType.Script;
			if (path.EndsWith("/other/"))
				return FileType.Other;
			return FileType.Undefined;
		}

		public static AssetType AssetTypeFromString(string value)
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

		public static FileType FileTypeFromExt(string ext)
		{
			if (ext != null)
			{
				ext = ext.Trim().ToLowerInvariant();
				switch (ext)
				{
				case "jpg":
				case "jpeg":
				case "png":
				case "apng":
				case "webp":
				case "avif":
					return FileType.Image;

				case "mp3":
				case "wav":
				case "ogg":
				case "flac":
				case "aiff":
					return FileType.Audio;

				case "mp4":
				case "mov":
				case "avi":
				case "mpg":
				case "mpeg":
				case "webm":
				case "mkv":
					return FileType.Video;

				case "otf":
				case "ttf":
					return FileType.Font;

				case "obj":
				case "mmd":
				case "blend":
				case "gltf":
				case "fbx":
					return FileType.Model3D;

				case "safetensor":
				case "ckpt":
				case "onnx":
					return FileType.ModelAI;

				case "lua":
				case "js":
				case "py":
				case "html":
					return FileType.Script;
				default:
					return FileType.Other;
				}
			}
			return FileType.Undefined;
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

		public object Clone()
	{
		return this.MemberwiseClone();
	}
}

	public struct AssetData
	{
		public byte[] Data;
		public long Length { get { return Data != null ? Data.Length : 0; } }

		public static AssetData FromBytes(byte[] bytes)
		{
			return new AssetData() {
				Data = bytes,
			};
		}
	}
}
