using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public partial class SidePanel : UserControl
	{
		public event EventHandler<PortraitPreview.ChangePortraitImageEventArgs> ChangePortraitImage;
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
		private bool _bOverridingGender = false;
		private string _overrideGender = "";

		private string CardName { get { return textBox_characterName.Text.Trim(); } }
		private string SpokenName { get { return textBox_characterSpokenName.Text.Trim(); } }
		private string UserName { get { return textBox_userPlaceholder.Text.Trim(); } }

		private Dictionary<Control, ToolTip> _tooltips = new Dictionary<Control, ToolTip>();

		public SidePanel()
		{
			InitializeComponent();

			_bIgnoreEvents = true;
			comboBox_gender.SelectedItem = comboBox_gender.Items[0]; // (Not set)
			comboBox_userGender.SelectedIndex = 0; // (Not set)
			comboBox_Detail.SelectedIndex = 0; // Default
			comboBox_textStyle.SelectedIndex = 0; // (Not set)
			_bIgnoreEvents = false;
		}

		private void SidePanel_Load(object sender, EventArgs e)
		{
			this.portraitImage.ChangePortraitImage += OnChangePortraitImage;

			SetToolTip(Resources.tooltip_character_name, label_characterName, textBox_characterName);
			SetToolTip(Resources.tooltip_spoken_name, label_characterSpokenName, textBox_characterSpokenName);
			SetToolTip(Resources.tooltip_character_gender, label_gender, comboBox_gender);
			SetToolTip(Resources.tooltip_user_name, label_userPlaceholder, textBox_userPlaceholder);
			SetToolTip(Resources.tooltip_user_gender, label_userGender, comboBox_userGender);
			SetToolTip(Resources.tooltip_detail_level, label_Detail, comboBox_Detail);
			SetToolTip(Resources.tooltip_text_style, label_textStyle, comboBox_textStyle);
			SetToolTip(Resources.tooltip_tokens, label_Tokens_Value);
			SetToolTip(Resources.tooltip_tokens_permanent, label_Tokens_Permanent_Value);
			SetToolTip(Resources.tooltip_misc_settings, label_More, btn_More);

			SetToolTip(Resources.tooltip_creator, label_creator, textBox_creator);
			SetToolTip(Resources.tooltip_version, label_version, textBox_version);

			SetToolTip(Resources.tooltip_creator_notes, label_creatorNotes, textBox_creatorNotes);
			SetToolTip(Resources.tooltip_tags, label_tags, textBox_tags);
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
			textBox_userPlaceholder.Text = Current.Card._userPlaceholder;
			textBox_userPlaceholder.InitUndo();
			textBox_userPlaceholder.Enabled = AppSettings.Settings.AutoConvertNames;

			// Creator
			textBox_creator.Text = Current.Card.creator;
			textBox_creator.InitUndo();

			// Creator notes
			textBox_creatorNotes.Text = Current.Card.comment.ConvertLinebreaks(Linebreak.CRLF);
			textBox_creatorNotes.InitUndo();

			// Version string
			textBox_version.Text = Current.Card.versionString;
			textBox_version.InitUndo();

			// Tags
			textBox_tags.Text = Utility.ListToCommaSeparatedString(Current.Card.tags);
			textBox_tags.InitUndo();

			// Gender
			if (!_bOverridingGender)
			{
				if (string.IsNullOrWhiteSpace(Current.Character.gender) == false)
				{
					if (string.Compare(Current.Character.gender, "male", true) == 0)
						comboBox_gender.SelectedItem = comboBox_gender.Items[1];
					else if (string.Compare(Current.Character.gender, "female", true) == 0)
						comboBox_gender.SelectedItem = comboBox_gender.Items[2];
					else if (string.IsNullOrWhiteSpace(Current.Character.gender) == false)
						comboBox_gender.SelectedItem = comboBox_gender.Items[3];
					else
						comboBox_gender.SelectedItem = comboBox_gender.Items[0];
				}
				else
					comboBox_gender.SelectedItem = comboBox_gender.Items[0];

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
				textBox_customGender.Text = _overrideGender;
			}

			// User gender
			if (string.IsNullOrWhiteSpace(Current.Card.userGender) == false)
			{
				if (string.Compare(Current.Card.userGender, "any", true) == 0)
					comboBox_userGender.SelectedItem = comboBox_userGender.Items[3];
				else
					comboBox_userGender.Text = Current.Card.userGender;
			}
			else
				comboBox_userGender.SelectedItem = comboBox_userGender.Items[0];

			// Detail level
			comboBox_Detail.SelectedItem = comboBox_Detail.Items[EnumHelper.ToInt(Current.Card.detailLevel) + 1];

			// Text style
			comboBox_textStyle.SelectedIndex = EnumHelper.ToInt(Current.Card.textStyle);

			RefreshTokenCount();

			// Lore count
			label_Lore_Value.Text = lastLoreCount.ToString();

			// Potrait
			portraitImage.SetImage(Current.Card.portraitImage);

			if (Current.Card.portraitImage != null)
			{
				SetToolTip(Resources.tooltip_portrait_image, portraitImage);
				RefreshImageAspectRatio();
			}
			else
			{
				SetToolTip(Resources.tooltip_no_portrait_image, portraitImage);
				label_Image_Value.Text = "-";
			}

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

			int width = Current.Card.portraitImage.Width;
			int height = Current.Card.portraitImage.Height;
			int gcd = GetGCD(Current.Card.portraitImage.Width, Current.Card.portraitImage.Height);

			if ((width / gcd < 50) && (height / gcd < 50))
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
			if (width > 2048 || height > 2048)
				label_Image_Value.ForeColor = System.Drawing.Color.Red;
			else
				label_Image_Value.ForeColor = this.ForeColor;
		}

		public void Reset()
		{
			_bOverridingGender = false;
			_overrideGender = null;
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
					label_Tokens_Value.ForeColor = System.Drawing.Color.Red;
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
					label_Tokens_Permanent_Value.ForeColor = System.Drawing.Color.Red;
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

			if (bChanged)
				Undo.Push(Undo.Kind.Parameter, "Change user name");
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
				var menu = new ContextMenuStrip();

				menu.Items.Add(new ToolStripMenuItem("Change portrait", null, (s, e) => {
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

				menu.Items.Add(new ToolStripSeparator()); // ----
				menu.Items.Add(new ToolStripMenuItem("Clear portrait", null, (s, e) => {
					RemovePortraitImage?.Invoke(this, EventArgs.Empty);
				}) {
					Enabled = Current.Card.portraitImage != null,
				});

				menu.Show(sender as Control, new System.Drawing.Point(args.X, args.Y));
			}
		}

		private void PortraitImage_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right) // Double right click: Clear portrait
			{
				Current.Card.portraitImage = null;
				portraitImage.SetImage(null);
				label_Image_Value.Text = "-";
				Undo.Push(Undo.Kind.Parameter, "Clear portrait image");
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
			var context = Current.Character.GetContext(CharacterData.ContextType.Full);
			_bIgnoreEvents = true;

			_bOverridingGender = context.HasTag(Constants.Flag.OverrideGender);

			if (_bOverridingGender)
			{
				context.TryGetValue("gender", out _overrideGender);

				if (comboBox_gender.Enabled)
				{
					comboBox_gender.Enabled = false;
					comboBox_gender.SelectedIndex = 3; // Other
					textBox_customGender.Visible = true;
					textBox_customGender.Enabled = false;
					textBox_customGender.Text = _overrideGender;
				}
			}
			else
			{
				_overrideGender = null;
				if (comboBox_gender.Enabled == false)
				{
					comboBox_gender.Enabled = true;
					textBox_customGender.Visible = comboBox_gender.SelectedIndex == 3;
					textBox_customGender.Enabled = true;
				}
			}

			_bIgnoreEvents = false;

		}

		private void btn_More_MouseClick(object sender, MouseEventArgs args)
		{
			ContextMenuStrip menu = new ContextMenuStrip();

//			var label = new ToolStripStatusLabel("Miscellaneous settings");
//			label.ForeColor = SystemColors.GrayText;
//			menu.Items.Add(label);
//			menu.Items.Add(new ToolStripSeparator());

			var scenario = new ToolStripMenuItem("Scenario");
			menu.Items.Add(scenario);
			AddSetting(scenario,
				CardData.Flag.PruneScenario,
				"Prune scenario",
				Resources.tooltip_prune_scenario);

			var userMenu = new ToolStripMenuItem("User persona");
			menu.Items.Add(userMenu);
			AddSetting(userMenu,
				CardData.Flag.UserPersonaInScenario,
				"In character persona",
				Resources.tooltip_user_in_persona,
				true);
			AddSetting(userMenu,
				CardData.Flag.UserPersonaInScenario,
				"In scenario",
				Resources.tooltip_user_in_scenario,
				false);

			menu.Show(sender as Control, new Point(args.X + 10, args.Y + 10));
		}

		private ToolStripMenuItem AddSetting(ContextMenuStrip menu, CardData.Flag flag, string label, string tooltip, bool inverse = false)
		{
			var menuItem = new ToolStripMenuItem(label, null, (s, e) => { ChangeFlag(flag); }) {
				Checked = Current.Card.extraFlags.Contains(flag) != inverse,
				ToolTipText = tooltip,
			};
			menu.Items.Add(menuItem);
			return menuItem;
		}

		private ToolStripMenuItem AddSetting(ToolStripMenuItem parentItem, CardData.Flag flag, string label, string tooltip, bool inverse = false)
		{
			var menuItem = new ToolStripMenuItem(label, null, (s, e) => { ChangeFlag(flag); }) {
				Checked = Current.Card.extraFlags.Contains(flag) != inverse,
				ToolTipText = tooltip,
			};
			parentItem.DropDownItems.Add(menuItem);
			return menuItem;
		}

		private void ChangeFlag(CardData.Flag flag)
		{
			if (Current.Card.extraFlags.Contains(flag))
				Current.Card.extraFlags &= ~flag;
			else
				Current.Card.extraFlags |= flag;
			Current.IsDirty = true;
		}
	}
}
