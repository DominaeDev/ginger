using Ginger.Properties;
using System;
using System.Drawing;
using System.Windows.Forms;
using WinFormsSyntaxHighlighter;

namespace Ginger
{
	public partial class LorebookEntryPanel : UserControl
	{
		private bool _bIgnoreEvents = false;
		private int _keyHash;
		private int _contentHash;

		public class LorebookChangedEventArgs : EventArgs
		{
			public string[] Keys { get; set; }
			public string Text { get; set; }
			public bool Enabled { get; set; }
		}
		public event EventHandler<LorebookChangedEventArgs> Changed;

		public event EventHandler RemoveClicked;
		public event EventHandler MoveUpClicked;
		public event EventHandler MoveDownClicked;
		public event EventHandler OnCopy;
		public event EventHandler OnPaste;
		public event EventHandler OnDuplicate;
		public event EventHandler OnAddEntry;

		public Lorebook.Entry lorebookEntry { get; private set; }

		public bool isEmpty { get { return (lorebookEntry.keys.Length == 0) && string.IsNullOrWhiteSpace(lorebookEntry.value); } }

		public LorebookEntryPanel()
		{
			InitializeComponent();

			Load += LorebookEntryPanel_Load;
			FontChanged += FontDidChange;
		}

		private void LorebookEntryPanel_Load(object sender, EventArgs e)
		{
			textBox_Keys.richTextBox.GotFocus += RichTextBox_GotFocus;
			textBox_Keys.richTextBox.LostFocus += RichTextBox_LostFocus;
			textBox_Keys.richTextBox.PreviewKeyDown += OnPreviewKeyDown;
			textBox_Keys.richTextBox.KeyPress += RichTextBox_KeyPress;
			textBox_Keys.richTextBox.ControlAltEnterPressed += RichTextBox_ControlAltEnterPressed;
			textBox_Keys.richTextBox.syntaxFlags = RichTextBoxEx.SyntaxFlags.Default;

			textBox_Text.richTextBox.ControlEnterPressed += TextBox_OnControlEnterPressed;
			textBox_Text.richTextBox.GotFocus += RichTextBox_GotFocus;
			textBox_Text.richTextBox.LostFocus += RichTextBox_LostFocus;
			textBox_Text.richTextBox.ControlAltEnterPressed += RichTextBox_ControlAltEnterPressed;
			textBox_Text.richTextBox.syntaxFlags = RichTextBoxEx.SyntaxFlags.Default;

			SetTooltip(Resources.tooltip_open_write, btnWrite);
			SetTooltip(Resources.tooltip_move_up, btnMoveUp);
			SetTooltip(Resources.tooltip_move_down, btnMoveDown);
			SetTooltip(Resources.tooltip_remove_lore, btnRemove);
		}

		private void RichTextBox_ControlAltEnterPressed(object sender, EventArgs e)
		{
			OnAddEntry?.Invoke(this, EventArgs.Empty);
		}

		private void FontDidChange(object sender, EventArgs e)
		{
			_bIgnoreEvents = true;
			textBox_Keys.Font = this.Font;
			textBox_Text.Font = this.Font;
			_bIgnoreEvents = false;
		}

		private void RichTextBox_GotFocus(object sender, EventArgs e)
		{
			_keyHash = textBox_Keys.Text.GetHashCode();
			_contentHash = textBox_Text.Text.GetHashCode();
		}

		private void RichTextBox_LostFocus(object sender, EventArgs e)
		{
			// Clean up
			_bIgnoreEvents = true;
			var keys = Utility.ListFromCommaSeparatedString(textBox_Keys.Text);
			string text = Utility.ListToCommaSeparatedString(keys);
			if (textBox_Keys.Text != text)
				textBox_Keys.Text = text;
			_bIgnoreEvents = false;

			var newKeyHash = textBox_Keys.Text.GetHashCode();
			var newContentHash = textBox_Text.Text.GetHashCode();

			if (_keyHash != newKeyHash || _contentHash != newContentHash)
			{
				_contentHash = newContentHash;
				Changed?.Invoke(this, new LorebookChangedEventArgs() {
					Keys = keys.ToArray(),
					Text = textBox_Text.Text,
					Enabled = lorebookEntry.isEnabled,
				});
			}
		}

		public void RefreshValue()
		{
			// Label
			textBox_Keys.richTextBox.SetText(lorebookEntry.key);
			textBox_Keys.InitUndo();
			textBox_Keys.Enabled = lorebookEntry.isEnabled;

			// Text box
			textBox_Text.richTextBox.SetText(lorebookEntry.value.Trim());
			textBox_Text.InitUndo();
			textBox_Text.Enabled = lorebookEntry.isEnabled;

			_keyHash = textBox_Keys.Text.GetHashCode();
			_contentHash = textBox_Text.Text.GetHashCode();

			// Enabled checkbox
			cbEnabled.Checked = lorebookEntry.isEnabled;
		}

		public void SetContent(Lorebook.Entry entry)
		{
			this.lorebookEntry = entry;

			_bIgnoreEvents = true;

			// Label
			textBox_Keys.richTextBox.SetText(entry.key);
			textBox_Keys.InitUndo();
			textBox_Keys.Enabled = entry.isEnabled;

			// Text box
			textBox_Text.richTextBox.SetText((entry.value ?? "").Trim());
			textBox_Text.InitUndo();
			textBox_Text.Enabled = entry.isEnabled;

			// Enabled checkbox
			cbEnabled.Checked = entry.isEnabled;

			_keyHash = textBox_Keys.Text.GetHashCode();
			_contentHash = textBox_Text.Text.GetHashCode();

			RefreshTokenCount();

			_bIgnoreEvents = false;
		}

		private void cbEnabled_CheckedChanged(object sender, EventArgs e)
		{
			textBox_Keys.Enabled = cbEnabled.Checked;
			textBox_Text.Enabled = cbEnabled.Checked;
			if (_bIgnoreEvents || Enabled == false)
				return;

			lorebookEntry.isEnabled = cbEnabled.Checked;

			Changed?.Invoke(this, new LorebookChangedEventArgs() {
				Keys = null,
				Text = textBox_Text.Text,
				Enabled = lorebookEntry.isEnabled,
			});
		}

		private void Btn_Remove_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				RemoveClicked?.Invoke(this, EventArgs.Empty);
		}

		private void btnWrite_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				ShowWriteDialog(textBox_Text.richTextBox);
		}

		private void TextBox_OnControlEnterPressed(object sender, EventArgs e)
		{
			ShowWriteDialog(textBox_Text.richTextBox);
		}

		private bool ShowWriteDialog(TextBoxBase textBox)
		{
			MainForm.HideFindDialog();

			textBox.Focus();
			using (var dlg = new WriteDialog())
			{
				if (lorebookEntry.keys != null && lorebookEntry.keys.Length > 0 && string.IsNullOrWhiteSpace(lorebookEntry.keys[0]) == false)
					dlg.Text = Utility.EscapeMenu(lorebookEntry.keys[0]);
				else
					dlg.Text = "Lore entry";

				dlg.Value = textBox.Text;
				dlg.SelectionStart = textBox.SelectionStart;
				dlg.SelectionLength = textBox.SelectionLength;

				var contentHash = textBox.Text.GetHashCode();
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					textBox.Text = dlg.Value;
					textBox.Select(dlg.SelectionStart, dlg.SelectionLength);
					textBox.ScrollToSelection();

					var newContentHash = textBox.Text.GetHashCode();
					if (contentHash != newContentHash)
					{
						_contentHash = newContentHash;
						Changed?.Invoke(this, new LorebookChangedEventArgs() {
							Keys = null,
							Text = textBox_Text.Text,
							Enabled = lorebookEntry.isEnabled,
						});
					}
					return true;
				}

				return false;
			}
		}

		private void btnMoveUp_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				MoveUpClicked?.Invoke(this, EventArgs.Empty);
		}

		private void btnMoveDown_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				MoveDownClicked?.Invoke(this, EventArgs.Empty);
		}

		public void RefreshTokenCount()
		{
			if (lorebookEntry != null)
			{
				if (lorebookEntry.tokenCount == 0)
					labelTokens.Text = "";
				else if (lorebookEntry.tokenCount == 1)
					labelTokens.Text = "1 token";
				else
					labelTokens.Text = string.Format("{0} tokens", lorebookEntry.tokenCount);

				// Faraday: Warn if lore item exceeds Faraday's lore size limit
				if (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday
					&& AppSettings.Settings.TokenBudget > 0)
				{
					var faradayLoreLimit = CalcFaradayLoreLimit(AppSettings.Settings.TokenBudget);
					if (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday
						&& lorebookEntry.tokenCount > faradayLoreLimit)
					{
						labelTokens.Text = string.Format("{0}/{1} tokens", lorebookEntry.tokenCount, faradayLoreLimit);
						labelTokens.ForeColor = Color.Crimson;
					}
					else
						labelTokens.ForeColor = this.ForeColor;
				}
				else
					labelTokens.ForeColor = this.ForeColor;
			}
		}

		private void LorebookEntryPanel_MouseClick(object sender, MouseEventArgs args)
		{
			if (args.Button == MouseButtons.Right)
			{
				var menu = new ContextMenuStrip();
				menu.Items.Add(new ToolStripMenuItem("Enabled", null, (s, e) => {
					cbEnabled.Checked = !lorebookEntry.isEnabled;
				}) {
					Checked = lorebookEntry.isEnabled,
				});
				menu.Items.Add("-");
				menu.Items.Add(new ToolStripMenuItem("Copy this entry", null, (s, e) => {
					OnCopy?.Invoke(this, EventArgs.Empty);
				}) {
					Enabled = !isEmpty,
				});
				menu.Items.Add(new ToolStripMenuItem("Duplicate this entry", null, (s, e) => {
					OnDuplicate?.Invoke(this, EventArgs.Empty);
				}) {
					Enabled = !isEmpty,
				});

				menu.Items.Add(new ToolStripMenuItem("Paste", null, (s, e) => {
					OnPaste?.Invoke(this, EventArgs.Empty);
				}) {
					Enabled = Clipboard.ContainsData(LoreClipboard.Format),
				});

				menu.Show(sender as Control, new Point(args.X, args.Y));
			}
		}

		protected void SetTooltip(string tooltip, params Control[] controls)
		{
			if (string.IsNullOrEmpty(tooltip))
				return;

			if (this.components == null)
			{
				this.components = new System.ComponentModel.Container();
			}

			var toolTip = new ToolTip(this.components);
			toolTip.UseFading = false;
			toolTip.UseAnimation = false;
			toolTip.AutomaticDelay = 250;

			foreach (var control in controls)
				toolTip.SetToolTip(control, tooltip);
		}

		private void OnPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyData == Keys.Return || e.KeyData == Keys.Enter)
			{
				var keys = Utility.ListFromCommaSeparatedString(textBox_Keys.Text);

				_bIgnoreEvents = true;
				textBox_Keys.Text = Utility.ListToCommaSeparatedString(keys) + ", ";
				textBox_Keys.Select(textBox_Keys.Text.Length);
				_bIgnoreEvents = false;

				e.IsInputKey = false;
			}
		}

		private void RichTextBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == ',')
			{
				if (textBox_Keys.Text.Substring(textBox_Keys.SelectionStart).ContainsNoneOf(c => char.IsWhiteSpace(c) == false))
				{
					var keys = Utility.ListFromCommaSeparatedString(textBox_Keys.Text);

					_bIgnoreEvents = true;
					textBox_Keys.Text = Utility.ListToCommaSeparatedString(keys) + ", ";
					textBox_Keys.Select(textBox_Keys.Text.Length);
					_bIgnoreEvents = false;

					e.Handled = true;
				}
			}
		}

		protected void SizeToWidth(Control control)
		{
			var scaleFactor = this.Font.SizeInPoints / Constants.ReferenceFontSize;

			control.Bounds = new Rectangle(
				Convert.ToInt32(Constants.ParameterPanel.LabelWidth * scaleFactor - 3),
				control.Location.Y,
				Convert.ToInt32((this.Size.Width - (Constants.ParameterPanel.LabelWidth + Constants.ParameterPanel.CheckboxWidth) * scaleFactor) + 4),
				control.Size.Height);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			SizeToWidth(textBox_Keys);
			SizeToWidth(textBox_Text);
		}

		private int CalcFaradayLoreLimit(int tokenBudget)
		{
			return Math.Max(Convert.ToInt32(Math.Floor((Math.Log(tokenBudget * 2, 2) - 10.0) * 256)), 256) - 4;
		}
	}
}
