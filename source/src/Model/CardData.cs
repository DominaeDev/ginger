using System;
using System.Collections.Generic;
using System.Drawing;

namespace Ginger
{
	public class CardData
	{
		public string name = "";
		public ImageRef portraitImage;
		public string _userPlaceholder;
		public string creator = "";
		public string comment = "";
		public string userGender = "";
		public string versionString = "";
		public HashSet<string> tags = new HashSet<string>();
		public AssetCollection assets = new AssetCollection(); // ccv3/charx

		public DateTime? creationDate = null;
		public JsonExtensionData extensionData = null; // Store extensions from imported json

		// Token count(s)
		public int tokens { get { return lastTokenCounts[0]; } }
		public int permanentTokensFaraday { get { return lastTokenCounts[1]; } }
		public int permanentTokensSilly { get { return lastTokenCounts[2]; } }
		public int[] lastTokenCounts = new int[3] { 0, 0, 0 };

		public string userPlaceholder
		{
			get { return string.IsNullOrWhiteSpace(_userPlaceholder) ? "User" : _userPlaceholder.Trim() ?? ""; }
			set { _userPlaceholder = value; }
		}

		public enum DetailLevel
		{
			Low = -1,
			Normal = 0,
			High = 1,
		}

		public DetailLevel detailLevel = DetailLevel.Normal;

		public enum TextStyle
		{
			None = 0,
			Chat,			// 1. Asterisks
			Novel,          // 2. Quotes
			Mixed,          // 3. Quotes + Asterisks
			Decorative,     // 4. Decorative quotes
			Bold,			// 5. Double asterisks
			Parentheses,    // 6. Parentheses instead of asterisks
			Japanese,       // 7. Japanese quotes
			Default = None,
		}
		public TextStyle textStyle = TextStyle.Default;

		public CardData()
		{
			_userPlaceholder = AppSettings.Settings.UserPlaceholder;
		}

		public CardData Clone()
		{
			CardData clone = (CardData)this.MemberwiseClone();
			clone.tags = new HashSet<string>(this.tags);
			clone.assets = (AssetCollection)this.assets.Clone();
			return clone;
		}
	}

	/// <summary>
	/// Disposes the Image before it gets garbage collected
	/// </summary>
	public class ImageRef
	{
		public static ImageRef FromImage(Image image)
		{
			if (image != null)
				return new ImageRef(image);
			return null;
		}

		private Image _image;
		private ImageRef(Image image)
		{
			_image = image;
		}
		
		~ImageRef()
		{
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

		public Image Clone()
		{
			return (Image)_image.Clone();
		}
	}

}
