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
				asset = icons.FirstOrDefault(a => a.isMainPortraitOverride && a.actorIndex < 1);

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

			bool bAnimated = false;
			if (ext == "apng" || ext == "png")
				bAnimated = Utility.IsAnimatedPNG(bytes);
			else if (ext == "webp")
				bAnimated = Utility.IsAnimatedWebP(bytes);
			else
			{
				Image image;
				if (Utility.LoadImageFromMemory(bytes, out image))
					bAnimated = Utility.IsAnimatedImage(image);
			}

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

			if (bAnimated)
				asset.AddTags(AssetFile.Tag.Animated);

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
	}
}
