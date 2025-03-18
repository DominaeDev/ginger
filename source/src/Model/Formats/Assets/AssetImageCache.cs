using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ginger
{
	public static class AssetImageCache
	{
		private static Dictionary<string, List<Image>> _Images = new Dictionary<string, List<Image>>();

		public enum ResizeFlag
		{
			None = 0,
			FitInside,
			FitOutside,
			Portrait,
		}

		public static Image GetImageForAsset(AssetFile asset, int width = 0, int height = 0, ResizeFlag resizeFlag = ResizeFlag.None)
		{
			if (asset == null || asset.data.isEmpty)
				return null;

			List<Image> images;
			if (_Images.TryGetValue(asset.uid, out images))
			{
				foreach (var i in images)
				{
					if (i.Width == width && i.Height == height)
						return i;
				}
			}

			Image image;
			if (Utility.LoadImageFromMemory(asset.data.bytes, out image) == false)
				return null;

			if (width > 0 && height > 0 && resizeFlag != ResizeFlag.None)
				ResizeImage(ref image, width, height, resizeFlag);

			if (_Images.ContainsKey(asset.uid) == false)
				_Images.Add(asset.uid, new List<Image>());
			_Images[asset.uid].Add(image);
			return image;
		}

		public static void Clear()
		{
			_Images.Clear();
		}

		private static void ResizeImage(ref Image image, int width, int height, ResizeFlag resizeFlag)
		{
			if (image == null)
				return;

			if (image.Width <= width && image.Height <= height)
				return;

			Image bmpNewImage = new Bitmap(width, height);
			int srcWidth = image.Width;
			int srcHeight = image.Height;
			int fitWidth = width;
			int fitHeight = height;
			using (Graphics gfxNewImage = Graphics.FromImage(bmpNewImage))
			{
				float scale;
				if (resizeFlag == ResizeFlag.FitInside)
					scale = Math.Min((float)fitWidth / srcWidth, (float)fitHeight / srcHeight);
				else if (resizeFlag == ResizeFlag.FitOutside || resizeFlag == ResizeFlag.Portrait)
					scale = Math.Max((float)fitWidth / srcWidth, (float)fitHeight / srcHeight);
				else
					scale = 1.0f;

				int newWidth = Math.Max((int)Math.Round(srcWidth * scale), 1);
				int newHeight = Math.Max((int)Math.Round(srcHeight * scale), 1);
				int newX = -(newWidth - fitWidth) / 2;
				int newY = -(newHeight - fitHeight) / 2;
				if (resizeFlag == ResizeFlag.Portrait)
					newY = 0;

				gfxNewImage.DrawImage(image,
					new Rectangle(newX, newY, newWidth, newHeight),
						0, 0, srcWidth, srcHeight,
						GraphicsUnit.Pixel);
			}
			image = bmpNewImage;
		}
	}
}
