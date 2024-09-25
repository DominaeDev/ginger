using Ginger.Properties;
using System;
using System.Drawing;
using System.Windows.Forms;
using WinFormsSyntaxHighlighter;

namespace Ginger
{
	public partial class LorebookEntryPanel : UserControl, IFlexibleParameterPanel, IVisualThemed
	{
		private bool _bIgnoreEvents = false;
		private int _keyHash;
		private int _contentHash;

		public class LorebookChangedEventArgs : EventArgs
		{
			public string[] Keys { get; set; }
			public string Text { get; set; }
			public int? SortOrder { get; set; }
			public bool Enabled { get; set; }
		}

		public class MoveToIndexEventArgs : EventArgs
		{
			public int Index { get; set; }
			public bool Focus { get; set; }
		}

		public class LorebookSortEventArgs : EventArgs
		{
			public Lorebook.Sorting Sorting { get; set; }
		}

		public class ResetOrderEventArgs : EventArgs
		{
			public enum Order {
				Default,
				Zero,
				OneHundred,
				ByRow,
			}
			public Order Ordering { get; set; }
		}

		public event EventHandler OnRemove;
		public event EventHandler OnMoveUp;
		public event EventHandler OnMoveDown;
		public event EventHandler OnMoveToTop;
		public event EventHandler OnMoveToBottom;
		public event EventHandler<MoveToIndexEventArgs> OnMoveToIndex;
		public event EventHandler OnCopy;
		public event EventHandler OnPaste;
		public event EventHandler OnInsert;
		public event EventHandler OnDuplicate;
		public event EventHandler OnAddEntry;
		public event EventHandler OnNextPage;
		public event EventHandler OnPreviousPage;
		public event EventHandler<ResetOrderEventArgs> OnResetOrder;
		public event EventHandler TextSizeChanged;
		public event EventHandler<LorebookChangedEventArgs> Changed;
		public event EventHandler<LorebookSortEventArgs> OnSortEntries;

		public Lorebook lorebook { get; private set; }
		public Lorebook.Entry lorebookEntry { get; private set; }

		public bool isEmpty { get { return (lorebookEntry.keys.Length == 0) && string.IsNullOrWhiteSpace(lorebookEntry.value); } }

		public static bool AllowFlexibleHeight = true;
		private bool _isPressingKey = false;


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
			textBox_Keys.richTextBox.syntaxFlags = RichTextBoxEx.SyntaxFlags.LoreKey;

			textBox_Text.richTextBox.ControlEnterPressed += TextBox_OnControlEnterPressed;
			textBox_Text.richTextBox.GotFocus += RichTextBox_GotFocus;
			textBox_Text.richTextBox.LostFocus += RichTextBox_LostFocus;
			textBox_Text.richTextBox.ControlAltEnterPressed += RichTextBox_ControlAltEnterPressed;
			textBox_Text.richTextBox.syntaxFlags = RichTextBoxEx.SyntaxFlags.LoreText;
			textBox_Text.TextSizeChanged += TextBox_TextSizeChanged;
			textBox_Text.Resize += TextBox_Text_Resize;

			textBox_Index.GotFocus += TextBox_Index_GotFocus;
			textBox_Index.LostFocus += TextBox_Index_LostFocus;
			textBox_Index.KeyPress += TextBox_Index_KeyPress;
			textBox_Index.PreviewKeyDown += TextBox_Index_PreviewKeyDown;

			SetTooltip(Resources.tooltip_open_write, btnWrite);
			SetTooltip(Resources.tooltip_move_up, btnMoveUp);
			SetTooltip(Resources.tooltip_move_down, btnMoveDown);
			SetTooltip(Resources.tooltip_remove_lore, btnRemove);
			SetTooltip(Resources.tooltip_lore_order, textBox_Index, labelIndex);
		}

		private void TextBox_Text_Resize(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			RefreshLineWidth();
		}

		public void RefreshLineWidth()
		{
			var richTextBox = textBox_Text.richTextBox;
			if (AppSettings.Settings.AutoBreakLine)
			{
				richTextBox.RightMargin = Math.Min((int)Math.Round(Constants.AutoWrapWidth * richTextBox.Font.SizeInPoints), Math.Max(richTextBox.Size.Width - 26, 0));
				richTextBox.WordWrap = true;
			}
			else
			{
				richTextBox.RightMargin = Math.Max(richTextBox.Size.Width - 26, 0); // Account for scrollbar
				richTextBox.WordWrap = true;
			}

			if (richTextBox.Multiline)
				richTextBox.SetInnerMargins(3, 2, 2, 0);
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
			textBox_Index.Font = this.Font;
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
					SortOrder = null,
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
			
			// Index
			textBox_Index.SetText(lorebookEntry.sortOrder.ToString());
			textBox_Index.InitUndo();
			textBox_Index.Enabled = lorebookEntry.isEnabled;

			_keyHash = textBox_Keys.Text.GetHashCode();
			_contentHash = textBox_Text.Text.GetHashCode();

			// Enabled checkbox
			cbEnabled.Checked = lorebookEntry.isEnabled;
		}

		public void SetContent(Lorebook.Entry entry, Lorebook lorebook)
		{
			this.lorebookEntry = entry;
			this.lorebook = lorebook;

			bool bRearranging = AppSettings.Settings.EnableRearrangeLoreMode;

			_bIgnoreEvents = true;

			// Label
			textBox_Keys.richTextBox.SetText(entry.key);
			textBox_Keys.InitUndo();
			textBox_Keys.Enabled = entry.isEnabled;

			// Text box
			textBox_Text.richTextBox.SetText((entry.value ?? "").Trim());
			textBox_Text.InitUndo();
			textBox_Text.Enabled = entry.isEnabled;

			// Order / Row
			if (bRearranging)
				textBox_Index.SetText((lorebook.entries.IndexOf(entry) + 1).ToString()); // One-based
			else
				textBox_Index.SetText(entry.sortOrder.ToString());
			textBox_Index.InitUndo();
			textBox_Index.Enabled = entry.isEnabled;

			// Enabled checkbox
			cbEnabled.Checked = entry.isEnabled;

			_keyHash = textBox_Keys.Text.GetHashCode();
			_contentHash = textBox_Text.Text.GetHashCode();

			RefreshTokenCount();

			// Rearrange buttons
			btnWrite.Visible = !bRearranging;
			btnMoveUp.Visible = bRearranging;
			btnMoveDown.Visible = bRearranging;
			var scaleFactor = this.Font.SizeInPoints / Constants.ReferenceFontSize;
			btnRemove.Top = (int)((bRearranging ? 76 : 56) * scaleFactor);
			labelIndex.Text = bRearranging ? "Item #" : "Order:";

			_bIgnoreEvents = false;
		}

		private void cbEnabled_CheckedChanged(object sender, EventArgs e)
		{
			textBox_Keys.Enabled = cbEnabled.Checked;
			textBox_Text.Enabled = cbEnabled.Checked;
			textBox_Index.Enabled = cbEnabled.Checked;
			if (_bIgnoreEvents || Enabled == false)
				return;

			lorebookEntry.isEnabled = cbEnabled.Checked;

			Changed?.Invoke(this, new LorebookChangedEventArgs() {
				Keys = null,
				Text = textBox_Text.Text,
				SortOrder = null,
				Enabled = lorebookEntry.isEnabled,
			});
		}

		private void Btn_Remove_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				OnRemove?.Invoke(this, EventArgs.Empty);
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
			{
				if (ModifierKeys == Keys.Shift)
					OnMoveToTop?.Invoke(this, EventArgs.Empty);
				else
					OnMoveUp?.Invoke(this, EventArgs.Empty);
			}
		}

		private void btnMoveDown_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (ModifierKeys == Keys.Shift)
					OnMoveToBottom?.Invoke(this, EventArgs.Empty);
				else
					OnMoveDown?.Invoke(this, EventArgs.Empty);
			}
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
						labelTokens.ForeColor = VisualTheme.Theme.WarningRed;
					}
					else
						labelTokens.ForeColor = VisualTheme.Theme.ControlForeground;
				}
				else
					labelTokens.ForeColor = VisualTheme.Theme.ControlForeground;
			}
		}

		private void LorebookEntryPanel_MouseClick(object sender, MouseEventArgs args)
		{
			if (args.Button == MouseButtons.Right)
			{
				var menu = new ContextMenuStrip();
				menu.Items.Add(new ToolStripMenuItem("Enabled", null, (s, e) => {
					CommitChange();
					cbEnabled.Checked = !lorebookEntry.isEnabled;
				}) {
					Checked = lorebookEntry.isEnabled,
				});
				menu.Items.Add("-");
				menu.Items.Add(new ToolStripMenuItem("Insert entry here", null, (s, e) => {
					CommitChange();
					OnInsert?.Invoke(this, EventArgs.Empty);
				}));
				menu.Items.Add(new ToolStripMenuItem("Copy", null, (s, e) => {
					CommitChange();
					OnCopy?.Invoke(this, EventArgs.Empty);
				}) {
					Enabled = !isEmpty,
				});
				menu.Items.Add(new ToolStripMenuItem("Paste", null, (s, e) => {
					CommitChange();
					OnPaste?.Invoke(this, EventArgs.Empty);
				}) {
					Enabled = Clipboard.ContainsData(LoreClipboard.Format),
				});
				menu.Items.Add(new ToolStripMenuItem("Duplicate", null, (s, e) => {
					CommitChange();
					OnDuplicate?.Invoke(this, EventArgs.Empty);
				}) {
					Enabled = !isEmpty,
				});
				menu.Items.Add(new ToolStripSeparator());

				menu.Items.Add(new ToolStripMenuItem("Move up", null, (s, e) => { CommitChange(); OnMoveUp?.Invoke(this, e); }));
				menu.Items.Add(new ToolStripMenuItem("Move down", null, (s, e) => { CommitChange(); OnMoveDown?.Invoke(this, e); }));
				menu.Items.Add(new ToolStripMenuItem("Move to top", null, (s, e) => { CommitChange(); OnMoveToTop?.Invoke(this, e); }));
				menu.Items.Add(new ToolStripMenuItem("Move to bottom", null, (s, e) => { CommitChange(); OnMoveToBottom?.Invoke(this, e); }));
				menu.Items.Add(new ToolStripSeparator());

				var sortMenu = new ToolStripMenuItem("Sort all entries");
				sortMenu.DropDownItems.Add(new ToolStripMenuItem("By key", null, (s, e) => {
					CommitChange(); 
					OnSortEntries?.Invoke(this, new LorebookSortEventArgs() {
						Sorting = Lorebook.Sorting.ByKey,
					});
				}) { Enabled = lorebook.entries.Count > 1 });
				sortMenu.DropDownItems.Add(new ToolStripMenuItem("By order", null, (s, e) => {
					CommitChange(); 
					OnSortEntries?.Invoke(this, new LorebookSortEventArgs() {
						Sorting = Lorebook.Sorting.ByOrder,
					});
				}) { Enabled = lorebook.entries.Count > 1 });
				sortMenu.DropDownItems.Add(new ToolStripMenuItem("By creation", null, (s, e) => {
					CommitChange(); 
					OnSortEntries?.Invoke(this, new LorebookSortEventArgs() {
						Sorting = Lorebook.Sorting.ByIndex,
					});
				}) { Enabled = lorebook.entries.Count > 1 });
				menu.Items.Add(sortMenu);
				var resetOrderMenu = new ToolStripMenuItem("Set order (all)");
					menu.Items.Add(resetOrderMenu);
				resetOrderMenu.DropDownItems.Add(new ToolStripMenuItem("Default", null, (s, e) => { 
					CommitChange(); 
					OnResetOrder?.Invoke(s, new ResetOrderEventArgs() { Ordering = ResetOrderEventArgs.Order.Default });
				}) {
					Enabled = lorebook.entries.Count > 0,
				});
				resetOrderMenu.DropDownItems.Add(new ToolStripMenuItem("Zero", null, (s, e) => { 
					CommitChange();
					OnResetOrder?.Invoke(s, new ResetOrderEventArgs() { Ordering = ResetOrderEventArgs.Order.Zero });
				}) {
					Enabled = lorebook.entries.Count > 0,
				});
				resetOrderMenu.DropDownItems.Add(new ToolStripMenuItem("One hundred", null, (s, e) => { 
					CommitChange();
					OnResetOrder?.Invoke(s, new ResetOrderEventArgs() { Ordering = ResetOrderEventArgs.Order.OneHundred });
				}) {
					Enabled = lorebook.entries.Count > 0,
				});
				resetOrderMenu.DropDownItems.Add(new ToolStripMenuItem("By row", null, (s, e) => {
					CommitChange();
					OnResetOrder?.Invoke(s, new ResetOrderEventArgs() { Ordering = ResetOrderEventArgs.Order.ByRow });
				}) {
					Enabled = lorebook.entries.Count > 0,
				});
				menu.Items.Add(new ToolStripMenuItem("Rearrange lore", null, (s, e) => {
					CommitChange();
					MainForm.instance.rearrangeLoreMenuItem_Click(s, e);
				}) {
					Checked = AppSettings.Settings.EnableRearrangeLoreMode,
					ToolTipText = Resources.tooltip_rearrange_lore,
				});

				int numEntries = lorebook.entries.Count;
				if (numEntries > AppSettings.Settings.LoreEntriesPerPage)
				{
					menu.Items.Add(new ToolStripSeparator());
					int pageIndex = lorebook.entries.IndexOf(lorebookEntry) / AppSettings.Settings.LoreEntriesPerPage;
					menu.Items.Add(new ToolStripMenuItem("Next page", null, (s, e) => { CommitChange(); OnNextPage?.Invoke(this, e); }) {
						Enabled = pageIndex < numEntries / AppSettings.Settings.LoreEntriesPerPage,
					});
					menu.Items.Add(new ToolStripMenuItem("Previous page", null, (s, e) => { CommitChange(); OnPreviousPage?.Invoke(this, e); }){
						Enabled = pageIndex > 0,
					});
				}

				VisualTheme.ApplyTheme(menu);
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
			toolTip.AutoPopDelay = 3500;

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

			var scaleFactor = this.Font.SizeInPoints / Constants.ReferenceFontSize;

			int kIndexWidth = 40;

			textBox_Index.Bounds = new Rectangle(
				this.Size.Width - Convert.ToInt32((Constants.ParameterPanel.CheckboxWidth + kIndexWidth) * scaleFactor),
				textBox_Keys.Location.Y,
				Convert.ToInt32(Math.Ceiling(kIndexWidth * scaleFactor)),
				textBox_Index.Size.Height);

			labelIndex.Bounds = new Rectangle(
				textBox_Index.Left - labelIndex.Width,
				labelKey.Location.Y - 1,
				labelIndex.Width,
				textBox_Index.Size.Height);

			textBox_Keys.Bounds = new Rectangle(
				Convert.ToInt32(Constants.ParameterPanel.LabelWidth * scaleFactor - 3),
				textBox_Keys.Location.Y,
				Convert.ToInt32((this.Size.Width - (Constants.ParameterPanel.LabelWidth + Constants.ParameterPanel.CheckboxWidth) * scaleFactor) + 4 
				- (labelIndex.Width + textBox_Index.Width)),
				textBox_Keys.Size.Height);

			//SizeToWidth(textBox_Keys);
			SizeToWidth(textBox_Text);
		}

		private int CalcFaradayLoreLimit(int tokenBudget)
		{
			return Math.Max(Convert.ToInt32(Math.Floor((Math.Log(tokenBudget * 2, 2) - 10.0) * 256)), 256) - 4;
		}

		private void TextBox_Index_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar))
			{
				e.Handled = false; //Do not reject the input
			}
			else
			{
				e.Handled = true; //Reject the input
			}
		}

		private void TextBox_Index_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
			{
				_isPressingKey = true;
				textBox_Index.KillFocus();
				_isPressingKey = false;
			}
		}

		private void TextBox_Index_GotFocus(object sender, EventArgs e)
		{
			textBox_Index.SelectAll();
		}

		private void TextBox_Index_LostFocus(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			bool bRearranging = AppSettings.Settings.EnableRearrangeLoreMode;

			int sortOrder;
			if (int.TryParse(textBox_Index.Text, out sortOrder) == false)
				sortOrder = 0;
			
			if (bRearranging)
				sortOrder = Math.Min(Math.Max(sortOrder, 1), lorebook.entries.Count);
			else
				sortOrder = Math.Max(sortOrder, 0);

			_bIgnoreEvents = true;
			textBox_Index.SetText(sortOrder.ToString());
			textBox_Index.InitUndo();
			_bIgnoreEvents = false;

			if (bRearranging)
			{
				sortOrder -= 1; // One-based
				int currentIndex = lorebook.entries.IndexOf(lorebookEntry);
				if (currentIndex == sortOrder)
					return; // No move

				_bIgnoreEvents = true;
				OnMoveToIndex?.Invoke(this, new MoveToIndexEventArgs() {
					Index = sortOrder,
					Focus = _isPressingKey,
				});
				_bIgnoreEvents = false;
			}
			else
			{
				Changed?.Invoke(this, new LorebookChangedEventArgs() {
					Keys = null,
					Text = textBox_Text.Text,
					SortOrder = sortOrder,
					Enabled = lorebookEntry.isEnabled,
				});
			}
		}

		public void CommitChange()
		{
			if (ActiveControl != null)
			{
				var focused = MainForm.instance.GetFocusedControl();
				if (focused != null)
					focused.KillFocus(); // Send WM_KILLFOCUS, which triggers the value to be saved, if changed.
			}
		}

		private void TextBox_TextSizeChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			RefreshFlexibleSize();
		}

		public void RefreshFlexibleSize()
		{
			if (AllowFlexibleHeight == false)
				return;

			_bIgnoreEvents = true;
			float scaleFactor = this.Font.SizeInPoints / Constants.ReferenceFontSize;

			int height = textBox_Text.TextSize.Height;
			height += 16; // Padding
			height = (int)Math.Min(Math.Max(height, 72f * scaleFactor), 203f * scaleFactor); // Clamp

			if (textBox_Text.Size.Height != height)
			{
				if (height <= (int)(203f * scaleFactor))
					this.HideVerticalScrollbar();

				textBox_Text.Size = new Size(textBox_Text.Size.Width, height);
				textBox_Text.Invalidate(); // Repaint (to avoid border artifacts)

				this.Size = new Size(this.Size.Width, textBox_Text.Bottom);

				TextSizeChanged?.Invoke(this, EventArgs.Empty); // Notify parent the size has changed
			}
			_bIgnoreEvents = false;
		}

		public void ApplyVisualTheme()
		{
			btnMoveUp.Image = VisualTheme.Theme.MoveLoreUp;
			btnMoveDown.Image = VisualTheme.Theme.MoveLoreDown;
			btnRemove.Image = VisualTheme.Theme.RemoveLore;
			btnWrite.Image = VisualTheme.Theme.Write;

			if (textBox_Index.Enabled)
			{
				textBox_Index.ForeColor = VisualTheme.Theme.TextBoxForeground;
				textBox_Index.BackColor = VisualTheme.Theme.TextBoxBackground;
			}
			else
			{
				textBox_Index.ForeColor = VisualTheme.Theme.GrayText;
				textBox_Index.BackColor = VisualTheme.Theme.TextBoxDisabledBackground;
			}

			RefreshTokenCount();
		}
	}
}
