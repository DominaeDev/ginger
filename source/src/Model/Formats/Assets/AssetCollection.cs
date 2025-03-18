using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Ginger
{
	public class AssetCollection : List<AssetFile>, ICloneable
	{
		public AssetCollection() : base() { }
		public AssetCollection(AssetCollection other) : base(other) { }
		public AssetCollection(IEnumerable<AssetFile> other) : base(other) { }

		public IEnumerable<AssetFile> EmbeddedPortraits
		{
			get
			{
				return this.Where(a =>
					a.assetType == AssetFile.AssetType.Icon
					&& a.isEmbeddedAsset
					&& a.data.isEmpty == false
					&& Utility.IsSupportedImageFileExt(a.ext));
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
				.SelectMany(g => {
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
	}

	public static class AssetCollectionExtensions 
	{
		public static AssetFile GetPortrait(this AssetCollection assets, int actorIndex = 0)
		{
			// Portrait override
			AssetFile asset = null;

			var icons = assets.EmbeddedPortraits;

			if (actorIndex <= 0)
			{
				asset = GetPortraitOverride(assets);

				// 'main' portrait
				if (asset == null)
					asset = icons.FirstOrDefault(a => a.isMainAsset && a.actorIndex < 1);
			}
			else
			{
				return icons.FirstOrDefault(a => a.actorIndex == actorIndex);
			}

			// First portrait
			if (asset == null)
				asset = icons.FirstOrDefault();

			return asset;
		}

		public static void AddPortraitAsset(this AssetCollection assets, FileUtil.FileType fileType) // Exporting
		{
			if (assets.ContainsAny(a => a.isMainPortraitOverride))
			{
				int idxOverride = assets.FindIndex(a => a.isMainPortraitOverride);
				if (idxOverride > 0)
					assets.Swap(0, idxOverride); // Move to first
				assets.RemoveAll(a => a.isDefaultAsset && a.assetType == AssetFile.AssetType.Icon);
				return; // A portrait image already exists
			}

			// Remove any existing default icon(s)
			assets.RemoveAll(a => a.isDefaultAsset && a.assetType == AssetFile.AssetType.Icon);

			if (fileType == FileUtil.FileType.Png && assets.ContainsNoneOf(a => a.assetType == AssetFile.AssetType.Icon && a.isMainAsset))
			{
				assets.Insert(0, AssetFile.MakeDefault(AssetFile.AssetType.Icon, AssetFile.MainAssetName, "png")); // Add ccdefault
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
					assets.Insert(0, new AssetFile() {
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
		
		public static AssetFile GetPortraitOverride(this AssetCollection assets)
		{
			return assets.EmbeddedPortraits.FirstOrDefault(a => a.isMainPortraitOverride && a.actorIndex < 1);
		}

		public static bool CreateMainPortraitOverride(this AssetCollection assets, string filename, out AssetFile asset)
		{
			var ext = Utility.GetFileExt(filename);
			if (Utility.IsSupportedImageFileExt(ext) == false)
			{
				asset = default(AssetFile);
				return false;
			}

			var bytes = Utility.LoadFile(filename);
			if (bytes == null || bytes.Length == 0)
			{
				asset = default(AssetFile);
				return false;
			}

			int width, height;
			Utility.GetImageDimensions(bytes, out width, out height);

			// Remove existing
			assets.RemoveAll(a => a.isDefaultAsset && a.assetType == AssetFile.AssetType.Icon);
			assets.RemoveMainPortraitOverride();

			// Add new override asset
			asset = new AssetFile() {
				name = string.Format("Portrait ({0})", Current.MainCharacter.spokenName),
				uriType = AssetFile.UriType.Embedded,
				assetType = AssetFile.AssetType.Icon,
				data = AssetData.FromBytes(bytes),
				knownWidth = width,
				knownHeight = height,
				tags = new HashSet<StringHandle>() { AssetFile.Tag.PortraitOverride },
				ext = ext,
			};
			assets.Insert(0, asset);
			return true;
		}

		public static bool RemoveMainPortraitOverride(this AssetCollection assets)
		{
			int idxExisting = assets.FindIndex(a => a.isMainPortraitOverride);
			if (idxExisting == -1)
				idxExisting = assets.FindIndex(a => a.isMainAsset && a.assetType == AssetFile.AssetType.Icon);
			if (idxExisting != -1)
			{
				assets.RemoveAt(idxExisting);
				return true;
			}
			return false;
		}

		public static bool LoadActorPortraitFromFile(this AssetCollection assets, int actorIndex, string filename, out AssetFile asset)
		{
			if (actorIndex <= 0 || actorIndex >= Current.Characters.Count)
			{
				asset = null;
				return false;
			}

			var ext = Utility.GetFileExt(filename);
			if (Utility.IsSupportedImageFileExt(ext) == false)
			{
				asset = null;
				return false;
			}

			var bytes = Utility.LoadFile(filename);
			if (bytes == null || bytes.Length == 0)
			{
				asset = null;
				return false;
			}

			int width, height;
			Utility.GetImageDimensions(bytes, out width, out height);

			// Remove existing
			assets.RemoveAll(a => a.assetType == AssetFile.AssetType.Icon && a.actorIndex == actorIndex);

			// Add new asset
			asset = new AssetFile() {
				name = string.Format("Portrait ({0})", Current.Characters[actorIndex].spokenName),
				uriType = AssetFile.UriType.Embedded,
				assetType = AssetFile.AssetType.Icon,
				data = AssetData.FromBytes(bytes),
				knownWidth = width,
				knownHeight = height,
				ext = ext,
				actorIndex = actorIndex,
			};

			if (Utility.IsAnimation(bytes))
				asset.AddTags(AssetFile.Tag.Animation);

			assets.Add(asset);
			return true;
		}

		public static AssetFile SetActorPortrait(this AssetCollection assets, int actorIndex, Image image)
		{
			if (actorIndex < 0 || actorIndex >= Current.Characters.Count)
				return null;

			var bytes = Utility.ImageToMemory(image, Utility.ImageFileFormat.Png);
			if (bytes == null || bytes.Length == 0)
				return null;

			// Remove existing
			assets.RemoveAll(a => a.assetType == AssetFile.AssetType.Icon && a.actorIndex == actorIndex);

			// Add new asset
			var asset = new AssetFile() {
				name = string.Format("Portrait ({0})", Current.Characters[actorIndex].spokenName),
				uriType = AssetFile.UriType.Embedded,
				assetType = AssetFile.AssetType.Icon,
				data = AssetData.FromBytes(bytes),
				knownWidth = image.Width,
				knownHeight = image.Height,
				ext = "png",
				actorIndex = actorIndex,
			};

			assets.Add(asset);
			return asset;
		}

		public static void RemoveActorAssets(this AssetCollection assets, int actorIndex)
		{
			// Remove existing
			assets.RemoveAll(a => a.actorIndex == actorIndex);

			foreach (var asset in assets)
			{
				int index = asset.actorIndex;
				if (index > actorIndex)
				{
					if (asset.actorIndex > 1)
						asset.actorIndex -= 1;
					else
						asset.actorIndex = -1;
				}
			}
		}

		public static bool AddBackground(this AssetCollection assets, string filename, out AssetFile asset)
		{
			try
			{
				byte[] bytes = File.ReadAllBytes(filename);

				string name = Path.GetFileNameWithoutExtension(filename);
				string ext = Utility.GetFileExt(filename);
				if (ext == "jpg")
					ext = "jpeg";

				var data = AssetData.FromBytes(bytes);
				if (assets.ContainsAny(a => a.assetType == AssetFile.AssetType.Background 
					&& a.data.hash == data.hash
					&& string.Compare(a.ext, ext, StringComparison.InvariantCultureIgnoreCase) == 0))
				{
					asset = default(AssetFile);
					return false; // Already added
				}

				if (bytes != null && bytes.Length > 0)
				{
					assets.RemoveAll(a => a.assetType == AssetFile.AssetType.Background && (a.isDefaultAsset || a.HasTag(AssetFile.Tag.MainBackground)));

					asset = new AssetFile() {
						name = "Background",
						ext = ext,
						assetType = AssetFile.AssetType.Background,
						data = data,
						uriType = AssetFile.UriType.Embedded,
						tags = new HashSet<StringHandle>() { AssetFile.Tag.MainBackground }
					};

					if (Utility.IsAnimation(bytes))
						asset.AddTags(AssetFile.Tag.Animation);

					int idxExisting = assets.IndexOfAny(a => a.isEmbeddedAsset && a.assetType == AssetFile.AssetType.Background);
					if (idxExisting != -1)
						assets.Insert(idxExisting, asset);
					else
						assets.Add(asset);
					return true;
				}
			}
			catch
			{
			}

			asset = default(AssetFile);
			return false;
		}

		public static bool AddBackground(this AssetCollection assets, Image image, out AssetFile asset)
		{
			try
			{
				byte[] bytes = Utility.ImageToMemory(image, Utility.ImageFileFormat.Png);

				var data = AssetData.FromBytes(bytes);
				if (assets.ContainsAny(a => a.data.hash == data.hash
					&& string.Compare(a.ext, "png", StringComparison.InvariantCultureIgnoreCase) == 0))
				{
					asset = default(AssetFile);
					return false; // Already added
				}

				if (bytes != null && bytes.Length > 0)
				{
					assets.RemoveAll(a => a.assetType == AssetFile.AssetType.Background && (a.isDefaultAsset || a.HasTag(AssetFile.Tag.MainBackground)));

					asset = new AssetFile() {
						name = "Background",
						ext = "png",
						assetType = AssetFile.AssetType.Background,
						data = data,
						uriType = AssetFile.UriType.Embedded,
						tags = new HashSet<StringHandle>() { AssetFile.Tag.MainBackground }
					};

					int idxExisting = assets.IndexOfAny(a => a.isEmbeddedAsset && a.assetType == AssetFile.AssetType.Background);
					if (idxExisting != -1)
						assets.Insert(idxExisting, asset);
					else
						assets.Add(asset);
					return true;
				}
			}
			catch
			{
			}

			asset = default(AssetFile);
			return false;
		}

		public static bool AddBackgroundFromPortrait(this AssetCollection assets, out AssetFile backgroundAsset)
		{
			// Select portrait asset
			AssetFile asset = null;
			if (Current.SelectedCharacter == 0) // Main character
			{
				var portraitAsset = assets.GetPortraitOverride(); // 1. Override
				if (portraitAsset != null)
				{
					asset = _BackgroundFromAsset(portraitAsset);
				}
				else if (Current.Card.portraitImage != null) // 2. Main portrait
				{
					asset = new AssetFile() {
						name = "Background (portrait)",
						ext = "jpeg",
						assetType = AssetFile.AssetType.Background,
						data = AssetData.FromBytes(Utility.ImageToMemory(Current.Card.portraitImage, Utility.ImageFileFormat.Jpeg)),
						uriType = AssetFile.UriType.Embedded,
						tags = new HashSet<StringHandle>() { AssetFile.Tag.MainBackground, AssetFile.Tag.PortraitBackground },
					};
				}
				else // 3. Portrait asset
				{
					portraitAsset = assets.GetPortrait();
					if (portraitAsset != null)
						asset = _BackgroundFromAsset(portraitAsset);
				}
			}
			else // Actor
			{
				var portraitAsset = assets.GetPortrait(Current.SelectedCharacter);
				if (portraitAsset != null)
					asset = _BackgroundFromAsset(portraitAsset);
			}

			if (asset == null)
			{
				backgroundAsset = default(AssetFile);
				return false;
			}

			backgroundAsset = asset;
			assets.RemoveAll(a => a.assetType == AssetFile.AssetType.Background && (a.isDefaultAsset || a.HasTag(AssetFile.Tag.MainBackground)));
			assets.Add(asset);
			return true;

			AssetFile _BackgroundFromAsset(AssetFile portraitAsset)
			{
				AssetFile _asset = new AssetFile() {
					name = "Background (portrait)",
					ext = portraitAsset.ext,
					assetType = AssetFile.AssetType.Background,
					data = portraitAsset.data,
					uriType = AssetFile.UriType.Embedded,
					tags = new HashSet<StringHandle>() { AssetFile.Tag.MainBackground, AssetFile.Tag.PortraitBackground },
				};
				if (portraitAsset.HasTag(AssetFile.Tag.Animation))
					_asset.AddTags(AssetFile.Tag.Animation);
				return _asset;
			}
		}
				
		public static AssetFile GetBackground(this AssetCollection assets)
		{
			return assets
				.Where(a => a.assetType == AssetFile.AssetType.Background
					&& a.isEmbeddedAsset
					&& a.data.isEmpty == false
					&& Utility.IsSupportedImageFileExt(a.ext))
				.FirstOrDefault();
		}
	}
}
