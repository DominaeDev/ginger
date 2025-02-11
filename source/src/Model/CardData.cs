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

		public string volatileUserPlaceholder = null; // Not saved (from Backyard import)

		public string userPlaceholder
		{
			get { return Utility.FirstNonEmpty(volatileUserPlaceholder, _userPlaceholder, Constants.DefaultUserName).Trim() ?? ""; }
			set { _userPlaceholder = value; volatileUserPlaceholder = null; }
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

			OmitUserPersona				= 1 << 24,
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
		
		public bool LoadPortraitImageFromFile(string filename, out Image image)
		{
			if (Utility.LoadImageFromFile(filename, out image) == false)
				return false;

			// Is animated image?
			var ext = Utility.GetFileExt(filename);
			bool bAnimated;
			if (ext == "apng" || ext == "png")
				bAnimated = Utility.IsAnimatedPNG(filename);
			else if (ext == "webp")
				bAnimated = Utility.IsAnimatedWebP(filename);
			else if (ext == "gif")
				bAnimated = Utility.IsAnimatedImage(image);
			else
				bAnimated = false;

			portraitImage = ImageRef.FromImage(image);
			Current.IsFileDirty = true;

			if (bAnimated)
			{
				AssetFile asset;
				if (assets.ReplaceMainPortraitOverride(filename, out asset))
				{
					portraitImage.uid = asset.uid; //?
					asset.name = "Portrait (animation)";
					asset.AddTags(AssetFile.Tag.Animated);
				}
			}
			else
				assets.RemoveMainPortraitOverride();
			return true;
		}
	}

}
