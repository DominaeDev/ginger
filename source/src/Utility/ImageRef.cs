using System;
using System.Drawing;

namespace Ginger
{
	/// <summary>
	/// Disposes the Image before it gets garbage collected
	/// </summary>
	public class ImageRef
	{
		public static ImageRef FromImage(Image image, bool bDisposable = true)
		{
			if (image != null)
				return new ImageRef(image, bDisposable);
			return null;
		}

		private Image _image;
		private bool _bDisposable;

		private ImageRef(Image image, bool bDisposable)
		{
			_image = image;
			_bDisposable = bDisposable;
		}
		
		~ImageRef()
		{
			if (_bDisposable)
				_image.Dispose();
		}

		public static implicit operator Image(ImageRef imageRef)
		{
			if (ReferenceEquals(imageRef, null))
				return null;
			return imageRef._image;
		}

		public int Width { get { return _image.Width; } }
		public int Height { get { return _image.Height; } }

		public string uid
		{ 
			get
			{
				if (string.IsNullOrEmpty(_uid))
					_uid = Guid.NewGuid().ToString();
				return _uid;
			}
			set { _uid = value; }
		}
		private string _uid;

		public Image Clone()
		{
			return (Image)_image.Clone();
		}
	}
}
