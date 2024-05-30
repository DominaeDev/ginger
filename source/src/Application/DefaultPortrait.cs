using Ginger.Properties;
using System.Drawing;
using System.IO;

namespace Ginger
{
	public static class DefaultPortrait
	{
		public static Image @Image 
		{ 
			get
			{
				if (_image == null)
					ReadImage();
				return _image;
			} 
		}
		private static Image _image;

		private static void ReadImage()
		{
			using (var stream = new MemoryStream(Resources.default_portrait, false))
			{
				_image = Image.FromStream(stream);
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
