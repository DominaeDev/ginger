using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Ginger
{
	public partial class SidePanel : UserControl, IThemedControl
	{
		public event EventHandler<PortraitPreview.ChangePortraitImageEventArgs> ChangePortraitImage;
		public event EventHandler ResizePortraitImage;
		public event EventHandler PastePortraitImage;
		public event EventHandler RemovePortraitImage;

		public class EditNameEventArgs : EventArgs
		{
			public string OldName { get; set; }
			public string NewName { get; set; }
		}
		public event EventHandler<EditNameEventArgs> EditName;
		public event EventHandler ChangedGender;

		private bool _bIgnoreEvents = false;
		private int lastLoreCount = 0;
		private string _genderOverride = null;
		private string _userGenderOverride = null;

		private string CardName { get { return textBox_characterName.Text.Trim(); } }
		private string SpokenName { get { return textBox_characterSpokenName.Text.Trim(); } }
		private string UserName { get { return textBox_userPlaceholder.Text.Trim(); } }

		private Dictionary<Control, ToolTip> _tooltips = new Dictionary<Control, ToolTip>();

		public SidePanel()
		{
			InitializeComponent();
		}

		private void SidePanel_Load(object sender, EventArgs e)
		{
			this.portraitImage.ChangePortraitImage += OnChangePortraitImage;
			root.VerticalScroll.Visible = false;

			group_CardInfo.OnCollapse += Group_CardInfo_OnCollapse;
			group_User.OnCollapse += Group_User_OnCollapse;
			group_Generation.OnCollapse += Group_Generation_OnCollapse;
			group_Components.OnCollapse += Group_Components_OnCollapse;
			group_Background.OnCollapse += Group_Background_OnCollapse;
			group_Stats.OnCollapse += Group_Stats_OnCollapse;

			_bIgnoreEvents = true;
			comboBox_gender.SelectedItem = comboBox_gender.Items[0]; // (Not set)
			comboBox_userGender.SelectedIndex = 0; // (Not set)
			comboBox_Detail.SelectedIndex = 0; // Default
			comboBox_textStyle.SelectedIndex = 0; // (Not set)

			group_CardInfo.Collapsed = !AppSettings.User.ShowCardInfo;
			group_User.Collapsed = !AppSettings.User.ShowUserInfo;
			group_Generation.Collapsed = !AppSettings.User.ShowOutputSettings;
			group_Components.Collapsed = !AppSettings.User.ShowOutputComponents;
			group_Background.Collapsed = !AppSettings.User.ShowBackground;
			group_Stats.Collapsed = !AppSettings.User.ShowStats;
			_bIgnoreEvents = false;

			SetToolTip(Resources.tooltip_character_name, label_characterName, textBox_characterName);
			SetToolTip(Resources.tooltip_spoken_name, label_characterSpokenName, textBox_characterSpokenName);
			SetToolTip(Resources.tooltip_character_gender, label_gender, comboBox_gender);
			SetToolTip(Resources.tooltip_user_name, label_userPlaceholder, textBox_userPlaceholder);
			SetToolTip(Resources.tooltip_user_gender, label_userGender, comboBox_userGender);
			SetToolTip(Resources.tooltip_detail_level, label_Detail, comboBox_Detail);
			SetToolTip(Resources.tooltip_text_style, label_textStyle, comboBox_textStyle);
			SetToolTip(Resources.tooltip_tokens, label_Tokens_Value);
			SetToolTip(Resources.tooltip_tokens_permanent, label_Tokens_Permanent_Value);

//			SetToolTip(Resources.tooltip_creator, label_creator, textBox_creator);
//			SetToolTip(Resources.tooltip_version, label_version, textBox_version);
//			SetToolTip(Resources.tooltip_creator_notes, label_creatorNotes, textBox_creatorNotes);
//			SetToolTip(Resources.tooltip_tags, label_tags, textBox_tags);

			SetToolTip(Resources.tooltip_include_model, cbIncludeModelInstructions);
			SetToolTip(Resources.tooltip_include_scenario, cbIncludeScenario);
			SetToolTip(Resources.tooltip_include_attributes, cbIncludeAttributes);
			SetToolTip(Resources.tooltip_include_user_persona, cbIncludeUser);
			SetToolTip(Resources.tooltip_include_greeting, cbIncludeGreetings);
			SetToolTip(Resources.tooltip_include_example, cbIncludeExampleChat);
			SetToolTip(Resources.tooltip_include_lore, cbIncludeLore);
			SetToolTip(Resources.tooltip_include_grammar, cbIncludeGrammar);
			SetToolTip(Resources.tooltip_prune_scenario, cbPruneScenario);
			SetToolTip(Resources.tooltip_user_in_persona, rbUserInPersona);
			SetToolTip(Resources.tooltip_user_in_scenario, rbUserInScenario);

		}

		private void OnChangePortraitImage(object sender, PortraitPreview.ChangePortraitImageEventArgs e)
		{
			ChangePortraitImage.Invoke(sender, e);
		}

		public void RefreshValues()
		{
			_bIgnoreEvents = true;
			textBox_characterName.Text = Current.Card.name;
			textBox_characterName.InitUndo();
			textBox_characterName.Enabled = Current.SelectedCharacter == 0;
			textBox_characterName.Placeholder = Utility.FirstNonEmpty(Current.Character._spokenName, Constants.DefaultCharacterName);

			textBox_characterSpokenName.Text = Current.Character._spokenName;
			textBox_characterSpokenName.Placeholder = Utility.FirstNonEmpty(Current.Card.name, Constants.DefaultCharacterName);
			textBox_characterSpokenName.InitUndo();

			if (string.IsNullOrEmpty(Current.Card.volatileUserPlaceholder) == false && Current.Card.volatileUserPlaceholder != Current.Card._userPlaceholder)
			{
				textBox_userPlaceholder.Text = Current.Card.volatileUserPlaceholder;
				textBox_userPlaceholder.ForeColor = Theme.Current.Name;
				SetToolTip(Resources.tooltip_user_name_volatile, label_userPlaceholder, textBox_userPlaceholder);
			}
			else
			{
				textBox_userPlaceholder.Text = Current.Card._userPlaceholder;
				textBox_userPlaceholder.ForeColor = Theme.Current.TextBoxForeground;
				SetToolTip(Resources.tooltip_user_name, label_userPlaceholder, textBox_userPlaceholder);
			}
			textBox_userPlaceholder.InitUndo();
			textBox_userPlaceholder.Enabled = AppSettings.Settings.AutoConvertNames;

			// Creator
			textBox_creator.Text = Current.Card.creator;
			textBox_creator.InitUndo();

			// Creator notes
			textBox_creatorNotes.Text = (Current.Card.comment ?? "").ConvertLinebreaks(Linebreak.CRLF);
			textBox_creatorNotes.InitUndo();

			// Version string
			textBox_version.Text = Current.Card.versionString;
			textBox_version.InitUndo();

			// Tags
			textBox_tags.Text = Utility.ListToCommaSeparatedString(Current.Card.tags);
			textBox_tags.InitUndo();

			// Gender
			RefreshGender(false);

			// Detail level
			comboBox_Detail.SelectedItem = comboBox_Detail.Items[EnumHelper.ToInt(Current.Card.detailLevel) + 1];

			// Text style
			comboBox_textStyle.SelectedIndex = EnumHelper.ToInt(Current.Card.textStyle);

			RefreshTokenCount();

			// Lore count
			label_Lore_Value.Text = lastLoreCount.ToString();

			// Portrait
			if (Current.SelectedCharacter == 0)
			{
				var portraitOverride = Current.Card.assets.EmbeddedPortraits.FirstOrDefault(a => a.isMainPortraitOverride);
				portraitImage.SetImage(Current.Card.portraitImage, portraitOverride != null && portraitOverride.HasTag(AssetFile.Tag.Animated));
				portraitImage.IsGrayedOut = false;
			}
			else
			{
				var asset = Current.Card.assets.GetPortrait(Current.SelectedCharacter);
				if (asset != null)
				{
					Image actorImage;
					Utility.LoadImageFromMemory(asset.data.bytes, out actorImage);
					if (actorImage != null)
					{
						portraitImage.SetImage(ImageRef.FromImage(actorImage, false), asset != null && asset.HasTag(AssetFile.Tag.Animated));
						portraitImage.IsGrayedOut = false;
					}
					else
					{
						portraitImage.SetImage(Current.Card.portraitImage);
						portraitImage.IsGrayedOut = true;
					}
				}
				else
				{
					portraitImage.SetImage(Current.Card.portraitImage);
					portraitImage.IsGrayedOut = true;
				}
			}

			if (Current.Card.portraitImage != null)
			{
				SetToolTip(Resources.tooltip_portrait_image, portraitImage);
				RefreshImageAspectRatio();
			}
			else
			{
				SetToolTip(Resources.tooltip_no_portrait_image, portraitImage);
				label_Image_Value.Text = "-";
				label_Image_Value.ForeColor = this.ForeColor;
			}

			if (Current.SelectedCharacter > 0)
			{
				var asset = Current.Card.assets.GetPortrait(Current.SelectedCharacter);
				if (asset != null)
				{
					SetToolTip(Resources.tooltip_portrait_image, portraitImage);
					RefreshImageAspectRatio();
				}
				else
				{
					SetToolTip(Resources.tooltip_no_portrait_image, portraitImage);
					label_Image_Value.Text = "-";
					label_Image_Value.ForeColor = this.ForeColor;
				}
			}

			// Background
			var backgroundAsset = Current.Card.assets.FirstOrDefault(a => a.assetType == AssetFile.AssetType.Background);
			if (backgroundAsset != null)
			{
				Image backgroundImage;
				Utility.LoadImageFromMemory(backgroundAsset.data.bytes, out backgroundImage);
				if (backgroundImage != null)
				{
					backgroundPreview.SetImage(ImageRef.FromImage(backgroundImage, false), backgroundAsset != null && backgroundAsset.HasTag(AssetFile.Tag.Animated));
				}
				else
					backgroundPreview.SetImage(null);
			}
			else
				backgroundPreview.SetImage(null);


			// Output components
			cbIncludeModelInstructions.Checked = !Current.Card.extraFlags.Contains(CardData.Flag.OmitSystemPrompt);
			cbIncludeScenario.Checked = !Current.Card.extraFlags.Contains(CardData.Flag.OmitScenario);
			cbIncludeAttributes.Checked = !Current.Card.extraFlags.Contains(CardData.Flag.OmitAttributes);
			cbIncludeUser.Checked = !Current.Card.extraFlags.Contains(CardData.Flag.OmitUserPersona);
			cbIncludeGreetings.Checked = !Current.Card.extraFlags.Contains(CardData.Flag.OmitGreeting);
			cbIncludeExampleChat.Checked = !Current.Card.extraFlags.Contains(CardData.Flag.OmitExample);
			cbIncludeLore.Checked = !Current.Card.extraFlags.Contains(CardData.Flag.OmitLore);
			cbIncludeGrammar.Checked = !Current.Card.extraFlags.Contains(CardData.Flag.OmitGrammar);

			if (cbIncludeScenario.Checked)
			{
				rbUserInPersona.Enabled = cbIncludeUser.Checked;
				rbUserInScenario.Enabled = cbIncludeUser.Checked;
				rbUserInPersona.Checked = !Current.Card.extraFlags.Contains(CardData.Flag.UserPersonaInScenario);
				rbUserInScenario.Checked = Current.Card.extraFlags.Contains(CardData.Flag.UserPersonaInScenario);
			}
			else
			{
				rbUserInPersona.Enabled = false;
				rbUserInScenario.Enabled = false;
				rbUserInPersona.Checked = true;
				rbUserInScenario.Checked = false;
			}
			
			cbPruneScenario.Checked = Current.Card.extraFlags.Contains(CardData.Flag.PruneScenario);

			_bIgnoreEvents = false;
		}

		private void RefreshImageAspectRatio()
		{
			Func<int, int, int> GetGCD = (int a, int b) => {
				if (b > a)
					Utility.Swap(ref a, ref b);
				while (b != 0)
				{
					int mod = a % b;
					a = b;
					b = mod;
				}
				return a;
			};

			int width, height;
			if (Current.SelectedCharacter == 0)
			{
				width = Current.Card.portraitImage.Width;
				height = Current.Card.portraitImage.Height;
			}
			else
			{
				var asset = Current.Card.assets.GetPortrait(Current.SelectedCharacter);
				if (asset != null)
				{
					width = asset.knownWidth;
					height = asset.knownHeight;

					if (width == 0 || height == 0)
					{
						Utility.GetImageDimensions(asset.data.bytes, out width, out height);
						asset.knownWidth = width;
						asset.knownHeight = height;
					}
				}
				else
					return;
			}

			int gcd = GetGCD(width, height);

			if (gcd > 0 && (width / gcd < 50) && (height / gcd < 50))
			{
				label_Image_Value.Text = string.Format("{0} x {1} ({2}:{3})",
					width,
					height,
					width / gcd,
					height / gcd);
			}
			else
			{
				label_Image_Value.Text = string.Format("{0} x {1}",
					width,
					height);
			}
			if (width > Constants.MaxImageDimension || height > Constants.MaxImageDimension)
				label_Image_Value.ForeColor = Theme.Current.WarningRed;
			else
				label_Image_Value.ForeColor = this.ForeColor;
		}

		public void Reset()
		{
			_genderOverride = null;
			_userGenderOverride = null;
			
			_bIgnoreEvents = true;
			comboBox_gender.SelectedIndex = 0;
			_bIgnoreEvents = false;
		}

		public void RefreshTokenCount()
		{
			int tokens = Current.Card.tokens;
			int permanent_tokens;
			if (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday)
				permanent_tokens = Current.Card.permanentTokensFaraday;
			else
				permanent_tokens = Current.Card.permanentTokensSilly;


			// Token budget
			if (AppSettings.Settings.TokenBudget <= 0)
			{
				label_TokenBudget_Value.Text = "-";
				label_Tokens_Value.Text = tokens.ToString();
				label_Tokens_Value.ForeColor = this.ForeColor;
				label_Tokens_Permanent_Value.Text = permanent_tokens.ToString();
				label_Tokens_Permanent_Value.ForeColor = this.ForeColor;
			}
			else
			{
				label_TokenBudget_Value.Text = AppSettings.Settings.TokenBudget.ToString();

				if (tokens <= AppSettings.Settings.TokenBudget)
				{
					label_Tokens_Value.Text = string.Format("{0} ({1} left)", tokens, AppSettings.Settings.TokenBudget - tokens);
					label_Tokens_Value.ForeColor = this.ForeColor;
				}
				else
				{
					label_Tokens_Value.Text = string.Format("{0} ({1} over budget)", tokens, tokens - AppSettings.Settings.TokenBudget);
					label_Tokens_Value.ForeColor = Theme.Current.WarningRed;
				}

				if (permanent_tokens <= AppSettings.Settings.TokenBudget)
				{
					if (permanent_tokens != tokens)
						label_Tokens_Permanent_Value.Text = string.Format("{0} ({1} left)", permanent_tokens, AppSettings.Settings.TokenBudget - permanent_tokens);
					else
						label_Tokens_Permanent_Value.Text = permanent_tokens.ToString();
					label_Tokens_Permanent_Value.ForeColor = this.ForeColor;
				}
				else
				{
					if (permanent_tokens != tokens)
						label_Tokens_Permanent_Value.Text = string.Format("{0} ({1} over budget)", permanent_tokens, permanent_tokens - AppSettings.Settings.TokenBudget);
					else
						label_Tokens_Permanent_Value.Text = permanent_tokens.ToString(); 
					label_Tokens_Permanent_Value.ForeColor = Theme.Current.WarningRed;
				}
			}

			// Recommended context size
			int[] contextSizes = {
				2048, 4096, 6144, 8192, 12288, 16384, 24576, 32768
			};

			int contextSize = 2048;
			for (int i = 0; i < contextSizes.Length; ++i)
			{
				contextSize = contextSizes[i];
				if (contextSize - Current.Card.permanentTokensFaraday >= 1024)
					break;
			}

			label_Context_Value.Text = contextSize.ToString();
		}

		public void ForceCommitName()
		{
			if (textBox_characterName.Focused)
				UpdateCharacterName();
			else if (textBox_characterSpokenName.Focused)
				UpdateCharacterSpokenName();
			else if (textBox_userPlaceholder.Focused)
				UpdateUserPlaceholder();
		}

		private void TextBox_characterName_Leave(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;
			
			UpdateCharacterName();
		}

		private void TextBox_characterPlaceholder_Leave(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			UpdateCharacterSpokenName();
		}

		private void TextBox_userPlaceholder_Leave(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			UpdateUserPlaceholder();
		}

		private void TextBox_characterName_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter)
			{
				UpdateCharacterName();
			}
		}

		private void TextBox_characterPlaceholder_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter)
			{
				UpdateCharacterSpokenName();
			}
		}

		private void TextBox_userPlaceholder_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter)
			{
				UpdateUserPlaceholder();
			}
		}

		private void UpdateCharacterName()
		{
			if (Current.SelectedCharacter != 0 || Current.IsLoading)
				return;

			string prevPlaceholder = Current.Character.namePlaceholder;
			if (string.IsNullOrWhiteSpace(prevPlaceholder))
				prevPlaceholder = GingerString.CharacterMarker;
			prevPlaceholder = prevPlaceholder.Trim();

			bool bChanged = Current.Card.name != CardName;
			Current.IsDirty |= bChanged;
			Current.Card.name = CardName;

			string newPlaceholder = Current.Character.namePlaceholder;
			if (string.IsNullOrWhiteSpace(newPlaceholder))
				newPlaceholder = GingerString.CharacterMarker;
			newPlaceholder = newPlaceholder.Trim();
				
			EditName?.Invoke(this, new EditNameEventArgs() {
				OldName = prevPlaceholder,
				NewName = newPlaceholder,
			});

			if (bChanged)
				Undo.Push(Undo.Kind.Parameter, "Change name");
		}

		private void UpdateCharacterSpokenName()
		{
			if (Current.IsLoading)
				return;

			string prevPlaceholder = Current.Character.namePlaceholder;
			if (string.IsNullOrWhiteSpace(prevPlaceholder))
				prevPlaceholder = GingerString.CharacterMarker;
			prevPlaceholder = prevPlaceholder.Trim();

			bool bChanged = Current.Character.spokenName != SpokenName;
			Current.IsDirty |= bChanged;
			Current.Character.spokenName = SpokenName;

			string newPlaceholder = Current.Character.namePlaceholder;
			if (string.IsNullOrWhiteSpace(newPlaceholder))
				newPlaceholder = GingerString.CharacterMarker;
			newPlaceholder = newPlaceholder.Trim();

			EditName?.Invoke(this, new EditNameEventArgs() {
				OldName = prevPlaceholder,
				NewName = newPlaceholder,
			});

			if (bChanged)
				Undo.Push(Undo.Kind.Parameter, "Change name");
		}

		private void UpdateUserPlaceholder()
		{
			if (Current.IsLoading)
				return;

			string prevPlaceholder = Current.Card.userPlaceholder;
			if (string.IsNullOrWhiteSpace(prevPlaceholder))
				prevPlaceholder = GingerString.UserMarker;
			prevPlaceholder = prevPlaceholder.Trim();

			bool bChanged = Current.Card.userPlaceholder != UserName;
			Current.IsDirty |= bChanged;
			Current.Card.userPlaceholder = UserName;
			AppSettings.Settings.UserPlaceholder = Current.Card.userPlaceholder;

			string newPlaceholder = Current.Card.userPlaceholder;
			if (string.IsNullOrWhiteSpace(newPlaceholder))
				newPlaceholder = GingerString.UserMarker;
			newPlaceholder = newPlaceholder.Trim();

			EditName?.Invoke(this, new EditNameEventArgs() {
				OldName = prevPlaceholder,
				NewName = newPlaceholder,
			});

			textBox_userPlaceholder.ForeColor = Theme.Current.TextBoxForeground;
			SetToolTip(Resources.tooltip_user_name, label_userPlaceholder, textBox_userPlaceholder);
			if (bChanged)
			{
				Undo.Push(Undo.Kind.Parameter, "Change user name");

			}
		}

		private void TextBox_characterName_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int cursorPos = textBox_characterName.SelectionStart;
			textBox_characterName.Text = textBox_characterName.Text;
			textBox_characterName.SelectionStart = cursorPos;

			if (AppSettings.Settings.AutoConvertNames == false) // If AutoConvert is on, we do this on completion
			{
				Current.Card.name = CardName;
				Current.IsDirty = true;
			}
			textBox_characterSpokenName.Placeholder = Utility.FirstNonEmpty(CardName, Constants.DefaultCharacterName);
		}

		private void TextBox_characterSpokenName_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int cursorPos = textBox_characterSpokenName.SelectionStart;
			textBox_characterSpokenName.Text = textBox_characterSpokenName.Text;
			textBox_characterSpokenName.SelectionStart = cursorPos;

			textBox_characterName.Placeholder = Utility.FirstNonEmpty(SpokenName, Constants.DefaultCharacterName);
		}

		private void PortraitImage_MouseClick(object sender, MouseEventArgs args)
		{
			if (args.Button == MouseButtons.Right)
			{
				bool bHasPortrait;
				bool bCanResize;
				if (Current.SelectedCharacter == 0)
				{
					bHasPortrait = Current.Card.portraitImage != null;
					bCanResize = bHasPortrait && (Current.Card.portraitImage.Width > Constants.MaxImageDimension || Current.Card.portraitImage.Height > Constants.MaxImageDimension);
				}
				else
				{
					var asset = Current.Card.assets.GetPortrait(Current.SelectedCharacter);
					bHasPortrait = asset != null;
					bCanResize = asset != null && (asset.knownWidth > Constants.MaxImageDimension || asset.knownHeight > Constants.MaxImageDimension);
				}

				var menu = new ContextMenuStrip();

				menu.Items.Add(new ToolStripMenuItem("Change character portrait", null, (s, e) => {
					ChangePortraitImage?.Invoke(this, new PortraitPreview.ChangePortraitImageEventArgs() {
						Filename = null,
					});
				}));

				if (Clipboard.ContainsImage())
				{
					menu.Items.Add(new ToolStripMenuItem("Paste image", null, (s, e) => {
						PastePortraitImage?.Invoke(this, EventArgs.Empty);
					}));
				}
				else
				{
					menu.Items.Add(new ToolStripMenuItem("Paste image") { Enabled = false });
				}

				menu.Items.Add(new ToolStripMenuItem("Reduce size", null, (s, e) => {
					ResizePortraitImage?.Invoke(this, EventArgs.Empty);
				}) {
					Enabled = bCanResize,
					ToolTipText = Resources.tooltip_resize_portrait_image,
				});

				menu.Items.Add(new ToolStripSeparator()); // ----
				menu.Items.Add(new ToolStripMenuItem("Clear portrait", null, (s, e) => {
					RemovePortraitImage?.Invoke(this, EventArgs.Empty);
				}) {
					Enabled = bHasPortrait,
				});

				Theme.Apply(menu);
				menu.Show(sender as Control, new System.Drawing.Point(args.X, args.Y));
			}
		}

		private void TextBox_creator_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			Current.IsFileDirty |= Current.Card.creator != textBox_creator.Text;
			Current.Card.creator = textBox_creator.Text;
		}

		private void TextBox_creatorNotes_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			var comment = textBox_creatorNotes.Text.ConvertLinebreaks(Linebreak.LF);
			Current.IsFileDirty |= Current.Card.comment != comment;
			Current.Card.comment = comment;
		}

		private void TextBox_version_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			Current.IsDirty |= Current.Card.versionString != textBox_version.Text;
			Current.Card.versionString = textBox_version.Text;
		}

		private void ComboBox_userGender_SelectedValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			Current.IsDirty |= Current.Card.userGender != comboBox_userGender.Text;

			if (comboBox_userGender.SelectedIndex == 0)
				Current.Card.userGender = null;
			else if (comboBox_userGender.SelectedIndex == 1)
				Current.Card.userGender = "male";
			else if (comboBox_userGender.SelectedIndex == 2)
				Current.Card.userGender = "female";
			else if (comboBox_userGender.SelectedIndex == 3)
				Current.Card.userGender = "any";
		}

		private void ComboBox_gender_SelectedValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			Current.IsDirty |= Current.Character.gender != comboBox_gender.Text;

			if (comboBox_gender.SelectedIndex == 0)
				Current.Character.gender = null;
			else if (comboBox_gender.SelectedIndex == 1)
				Current.Character.gender = "Male";
			else if (comboBox_gender.SelectedIndex == 2)
				Current.Character.gender = "Female";
			else if (comboBox_gender.SelectedIndex == 3)
				Current.Character.gender = "";

			_bIgnoreEvents = true;
			if (comboBox_gender.SelectedIndex == 3)
			{
				textBox_customGender.Visible = true;
				textBox_customGender.Text = Current.Character.gender;
				textBox_customGender.Focus();
			}
			else
			{
				textBox_customGender.Visible = false;
				textBox_customGender.Text = "";
			}
			_bIgnoreEvents = false;

			ChangedGender?.Invoke(this, EventArgs.Empty);
		}

		private void TextBox_customGender_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			Current.IsDirty |= Current.Character.gender != textBox_customGender.Text;
			Current.Character.gender = textBox_customGender.Text;

			ChangedGender?.Invoke(this, EventArgs.Empty);
		}

		private void TextBox_tags_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			var tags = Utility.ListFromCommaSeparatedString(textBox_tags.Text);
			Current.IsDirty |= Current.Card.tags.Compare(tags) == false;
			Current.Card.tags = new HashSet<string>(tags);
		}

		public void SetLoreCount(int loreCount, bool bRefresh)
		{
			lastLoreCount = loreCount;
			if (bRefresh)
				label_Lore_Value.Text = loreCount.ToString();
		}

		private void TextBox_userPlaceholder_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int cursorPos = textBox_userPlaceholder.SelectionStart;
			textBox_userPlaceholder.Text = textBox_userPlaceholder.Text;
			textBox_userPlaceholder.SelectionStart = cursorPos;
			textBox_userPlaceholder.ForeColor = Theme.Current.TextBoxForeground;
		}

		public void SetSpokenName(string name, bool focus = true)
		{
			textBox_characterSpokenName.Text = name;
			if (focus)
			{
				textBox_characterSpokenName.SelectAll();
				textBox_characterSpokenName.Focus();
			}
		}

		private void ComboBox_Detail_SelectedValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			CardData.DetailLevel detailLevel = EnumHelper.FromInt(comboBox_Detail.SelectedIndex - 1, CardData.DetailLevel.Normal);

			Current.IsDirty |= Current.Card.detailLevel != detailLevel;
			Current.Card.detailLevel = detailLevel;
		}

		private void ComboBox_textStyle_SelectedValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			CardData.TextStyle textStyle = EnumHelper.FromInt(comboBox_textStyle.SelectedIndex, CardData.TextStyle.Default);
			Current.IsDirty |= Current.Card.textStyle != textStyle;
			Current.Card.textStyle = textStyle;
		}

		private void SetToolTip(Control control, string text)
		{
			if (control == null)
				return;

			if (this.components == null)
				this.components = new System.ComponentModel.Container();

			ToolTip toolTip;
			if (_tooltips.TryGetValue(control, out toolTip) == false)
			{
				toolTip = new ToolTip(this.components);
				toolTip.UseFading = false;
				toolTip.UseAnimation = false;
				toolTip.AutomaticDelay = 250;
				toolTip.AutoPopDelay = 3500;
			}

			if (string.IsNullOrEmpty(text) == false)
			{
				toolTip.SetToolTip(control, text);
				toolTip.Active = true;
			}
			else
				toolTip.Active = false;

			_tooltips.TryAdd(control, toolTip);
		}

		private void SetToolTip(string text, params Control[] controls)
		{
			if (controls == null || controls.Length == 0)
				return;

			foreach (var control in controls)
				SetToolTip(control, text);
		}

		public void OnRegenerate()
		{
			_bIgnoreEvents = true;
			RefreshGender(true);
			_bIgnoreEvents = false;
		}

		public void OnActorChanged()
		{
			_bIgnoreEvents = true;
			RefreshGender(true);
			_bIgnoreEvents = false;
		}

		private void RefreshGender(bool bUpdate)
		{
			if (bUpdate)
			{
				string gender = Current.Character.gender;
				string newGender;
				var context = Current.Character.GetContext(CharacterData.ContextType.Full);
				if (context.TryGetValue("gender", out newGender) && string.Compare(gender, newGender, StringComparison.OrdinalIgnoreCase) != 0)
					_genderOverride = newGender;
				else
					_genderOverride = null;

				string userGender = Current.Card.userGender;
				string newUserGender;
				if (context.TryGetValue("user-gender", out newUserGender) && string.Compare(userGender, newUserGender, StringComparison.OrdinalIgnoreCase) != 0)
					_userGenderOverride = newUserGender;
				else
					_userGenderOverride = null;
			}

			if (string.IsNullOrEmpty(_genderOverride))
			{
				comboBox_gender.Enabled = true;

				if (string.IsNullOrWhiteSpace(Current.Character.gender) == false)
				{
					if (string.Compare(Current.Character.gender, "male", true) == 0)
						comboBox_gender.SelectedIndex = 1;
					else if (string.Compare(Current.Character.gender, "female", true) == 0)
						comboBox_gender.SelectedIndex = 2;
					else if (string.IsNullOrWhiteSpace(Current.Character.gender) == false)
						comboBox_gender.SelectedIndex = 3;
					else
						comboBox_gender.SelectedIndex = 0;
				}
				else if (comboBox_gender.SelectedIndex != 3)
					comboBox_gender.SelectedIndex = 0;

				// Custom gender
				if (comboBox_gender.SelectedIndex == 3)
				{
					textBox_customGender.Visible = true;
					textBox_customGender.Enabled = true;
					textBox_customGender.Text = Current.Character.gender;
				}
				else
				{
					textBox_customGender.Visible = false;
					textBox_customGender.Enabled = true;
					textBox_customGender.Text = "";
				}
			}
			else // Recipe gender override
			{
				comboBox_gender.Enabled = false;
				comboBox_gender.SelectedIndex = 3; // Other
				textBox_customGender.Visible = true; 
				textBox_customGender.Enabled = false;
				textBox_customGender.Text = _genderOverride;
			}

			// User gender
			if (string.IsNullOrEmpty(_userGenderOverride))
			{
				comboBox_userGender.Enabled = true;

				if (string.IsNullOrWhiteSpace(Current.Card.userGender) == false)
				{
					if (string.Compare(Current.Card.userGender, "any", true) == 0)
						comboBox_userGender.SelectedIndex = 3;
					if (string.Compare(Current.Card.userGender, "male", StringComparison.OrdinalIgnoreCase) == 0)
						comboBox_userGender.SelectedIndex = 1; // Male
					else if (string.Compare(Current.Card.userGender, "female", StringComparison.OrdinalIgnoreCase) == 0)
						comboBox_userGender.SelectedIndex = 2; // Female
					else
						comboBox_userGender.Text = Current.Card.userGender;
				}
				else
					comboBox_userGender.SelectedIndex = 0; // (Not set)
			}
			else
			{
				comboBox_userGender.Enabled = false;
				if (string.Compare(_userGenderOverride, "male", StringComparison.OrdinalIgnoreCase) == 0)
					comboBox_userGender.SelectedIndex = 1; // Male
				else if (string.Compare(_userGenderOverride, "female", StringComparison.OrdinalIgnoreCase) == 0)
					comboBox_userGender.SelectedIndex = 2; // Female
				else
					comboBox_userGender.Text = _userGenderOverride;
			}
		}

		private void SetExtraFlag(CardData.Flag flag, bool bEnabled)
		{
			if (bEnabled)
				Current.Card.extraFlags |= flag;
			else
				Current.Card.extraFlags &= ~flag;

			Current.IsDirty = true;
		}

		public void ApplyVisualTheme()
		{
			Theme.Apply(this);

			if (Current.Characters == null)
				return;

			RefreshValues();
			portraitImage.BackgroundImage = Theme.Current.Checker;
		}

		public void RefreshLayout()
		{ 
			root.Height = (int)(this.ClientSize.Height - group_Character.Height - 4 );
		}
		
		private void Group_CardInfo_OnCollapse(object sender, bool bCollapsed)
		{
			if (_bIgnoreEvents)
				return;
			AppSettings.User.ShowCardInfo = !bCollapsed;
		}
		
		private void Group_User_OnCollapse(object sender, bool bCollapsed)
		{
			if (_bIgnoreEvents)
				return;
			AppSettings.User.ShowUserInfo = !bCollapsed;
		}

		private void Group_Generation_OnCollapse(object sender, bool bCollapsed)
		{
			if (_bIgnoreEvents)
				return;
			AppSettings.User.ShowOutputSettings = !bCollapsed;
		}

		private void Group_Components_OnCollapse(object sender, bool bCollapsed)
		{
			if (_bIgnoreEvents)
				return;
			AppSettings.User.ShowOutputComponents = !bCollapsed;
		}
		
		private void Group_Background_OnCollapse(object sender, bool bCollapsed)
		{
			if (_bIgnoreEvents)
				return;
			AppSettings.User.ShowBackground = !bCollapsed;
		}

		private void Group_Stats_OnCollapse(object sender, bool bCollapsed)
		{
			if (_bIgnoreEvents)
				return;
			AppSettings.User.ShowStats = !bCollapsed;
		}

		private void cbIncludeModelInstructions_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			SetExtraFlag(CardData.Flag.OmitSystemPrompt, !cbIncludeModelInstructions.Checked);
		}

		private void cbIncludeScenario_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			SetExtraFlag(CardData.Flag.OmitScenario, !cbIncludeScenario.Checked);
			
			_bIgnoreEvents = true;
			if (cbIncludeScenario.Checked)
			{
				rbUserInPersona.Enabled = cbIncludeUser.Checked;
				rbUserInScenario.Enabled = cbIncludeUser.Checked;
				rbUserInPersona.Checked = !Current.Card.extraFlags.Contains(CardData.Flag.UserPersonaInScenario);
				rbUserInScenario.Checked = Current.Card.extraFlags.Contains(CardData.Flag.UserPersonaInScenario);
			}
			else
			{
				rbUserInPersona.Enabled = false;
				rbUserInScenario.Enabled = false;
				rbUserInPersona.Checked = true;
				rbUserInScenario.Checked = false;
			}
			_bIgnoreEvents = false;
		}

		private void cbIncludeAttributes_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			SetExtraFlag(CardData.Flag.OmitAttributes, !cbIncludeAttributes.Checked);
		}

		private void cbIncludeUser_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			SetExtraFlag(CardData.Flag.OmitUserPersona, !cbIncludeUser.Checked);
			rbUserInPersona.Enabled = cbIncludeUser.Checked;
			rbUserInScenario.Enabled = cbIncludeUser.Checked;
		}

		private void cbIncludeGreetings_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			SetExtraFlag(CardData.Flag.OmitGreeting, !cbIncludeGreetings.Checked);
		}

		private void cbIncludeExampleChat_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			SetExtraFlag(CardData.Flag.OmitExample, !cbIncludeExampleChat.Checked);
		}

		private void cbIncludeLore_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			SetExtraFlag(CardData.Flag.OmitLore, !cbIncludeLore.Checked);
		}

		private void cbIncludeGrammar_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			SetExtraFlag(CardData.Flag.OmitGrammar, !cbIncludeGrammar.Checked);
		}

		private void cbPruneScenario_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			SetExtraFlag(CardData.Flag.PruneScenario, cbPruneScenario.Checked);
		}

		private void rbUserInPersona_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			SetExtraFlag(CardData.Flag.UserPersonaInScenario, !rbUserInPersona.Checked);
		}

		private void rbUserInScenario_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			SetExtraFlag(CardData.Flag.UserPersonaInScenario, rbUserInScenario.Checked);
		}
	}
}
