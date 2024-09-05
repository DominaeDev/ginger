using Ginger.Properties;
using System.Drawing;
using System.IO;

namespace Ginger
{
	public static class DefaultPortrait
	{
		public static ImageRef @Image 
		{ 
			get
			{
				if (_image == null)
					ReadImage();
				var imageRef = ImageRef.FromImage(_image);
				imageRef.uid = "__default";
				return imageRef;
			} 
		}
		private static Image _image;

		private static void ReadImage()
		{
			using (var stream = new MemoryStream(Resources.default_portrait, false))
			{
				_image = System.Drawing.Image.FromStream(stream);
			}
		}

		public static void Dispose()
		{
			if (_image != null)
			{
				_image.Dispose();
				_image = null;
			}
		}
	}
}
