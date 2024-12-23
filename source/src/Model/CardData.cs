using System;
using System.Collections.Generic;
using System.Drawing;

namespace Ginger
{
	public class CardData
	{
		public string uuid 
		{ 
			get
			{
				if (string.IsNullOrEmpty(_uuid))
					_uuid = Guid.NewGuid().ToString();
				return _uuid;
			}
			set { _uuid = value; }
		}
		private string _uuid = null;
		public string name = "";
		public ImageRef portraitImage;
		public string _userPlaceholder;
		public string creator = "";
		public string comment = "";
		public string userGender = "";
		public string versionString = "";
		public HashSet<string> tags = new HashSet<string>();
		public AssetCollection assets = new AssetCollection(); // ccv3/charx
		public List<CustomVariable> customVariables = new List<CustomVariable>();

		public DateTime? creationDate = null;
		public JsonExtensionData extensionData = null; // Store extensions from imported json
		public List<string> sources = null;

		// Token count(s)
		public int tokens { get { return lastTokenCounts[0]; } }
		public int permanentTokensFaraday { get { return lastTokenCounts[1]; } }
		public int permanentTokensSilly { get { return lastTokenCounts[2]; } }
		public int[] lastTokenCounts = new int[3] { 0, 0, 0 };

		public string userPlaceholder
		{
			get { return string.IsNullOrWhiteSpace(_userPlaceholder) ? Constants.DefaultUserName : _userPlaceholder.Trim() ?? ""; }
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

		[Flags]
		public enum Flag : int
		{
			None = 0,
			PruneScenario				= 1 << 0,
			UserPersonaInScenario		= 1 << 1,

			OmitSystemPrompt			= 1 << 25,
			OmitAttributes				= 1 << 26,
			OmitScenario				= 1 << 27,
			OmitExample					= 1 << 28,
			OmitGreeting				= 1 << 29,
			OmitGrammar					= 1 << 30,
			OmitLore					= 1 << 31,

			Default = None,
		}
		public Flag extraFlags = Flag.Default;

		public CardData()
		{
			_userPlaceholder = AppSettings.Settings.UserPlaceholder;
		}

		public CardData Clone()
		{
			CardData clone = (CardData)this.MemberwiseClone();
			clone.tags = new HashSet<string>(this.tags);
			clone.assets = (AssetCollection)this.assets.Clone();
			clone.customVariables = new List<CustomVariable>(this.customVariables);
			clone.sources = this.sources != null ? new List<string>(this.sources) : null;
			return clone;
		}

		public bool TryGetVariable(CustomVariableName name, out string value)
		{
			if (string.IsNullOrEmpty(name))
			{
				value = default(string);
				return false;
			}

			CustomVariable variable;
			for (int i = 0; i < customVariables.Count; ++i)
			{
				if (string.Compare(customVariables[i].Name, name, StringComparison.OrdinalIgnoreCase) == 0)
				{
					value = customVariables[i].Value;
					return true;
				}
			}
			value = default(string);
			return false;
		}

		public bool ReplaceMainPortraitAsset(string filename)
		{
			var ext = Utility.GetFileExt(filename);
			var bytes = Utility.LoadFile(filename);
			if (bytes == null || bytes.Length == 0)
				return false;

			if (Utility.IsSupportedImageFileExt(ext) == false)
				return false;

			// Remove existing
			Current.Card.assets.RemoveAll(a => a.isDefaultAsset && a.assetType == AssetFile.AssetType.Icon);
			int idxExisting = Current.Card.assets.FindIndex(a => a.isMainPortrait);
			if (idxExisting != -1)
				Current.Card.assets.RemoveAt(idxExisting);

			// Add new asset
			Current.Card.assets.Insert(0, new AssetFile() {
				name = AssetFile.MainAssetName,
				uriType = AssetFile.UriType.Embedded,
				assetType = AssetFile.AssetType.Icon,
				data = AssetData.FromBytes(bytes),
				ext = ext,
			});
			return true;
		}

		public bool RemoveMainPortraitAsset()
		{
			// Remove existing
			int idxExisting = Current.Card.assets.FindIndex(a => a.isMainPortrait && a.isDefaultAsset == false);
			if (idxExisting != -1)
			{
				Current.Card.assets.RemoveAt(idxExisting);
				return true;
			}
			return false;
		}
	}

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
