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

		public void AddVariablesFromText(string text)
		{
			List<CustomVariableName> varNames = new List<CustomVariableName>();
			var pos_var = text.IndexOf("{$", 0);
			while (pos_var != -1)
			{
				int pos_var_end = text.IndexOf("}", pos_var + 2);
				if (pos_var_end == -1)
					break;

				CustomVariableName varName = text.Substring(pos_var + 2, pos_var_end - pos_var - 2);
				if (string.IsNullOrWhiteSpace(varName.ToString()) == false)
					varNames.Add(varName);

				pos_var = text.IndexOf("{$", pos_var + 2);
			}

			foreach (var varName in varNames)
			{
				string tmp;
				if (TryGetVariable(varName, out tmp) == false)
					customVariables.Add(new CustomVariable(varName));
			}
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

		public static implicit operator ImageRef(Image image)
		{
			if (ReferenceEquals(image, null))
				return null;

			return FromImage(image);
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
