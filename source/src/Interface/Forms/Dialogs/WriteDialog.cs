using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WinFormsSyntaxHighlighter;

using Timer = System.Timers.Timer;
using Text = Ginger.Text;

namespace Ginger
{
	public partial class WriteDialog : Form
	{
		public string Value
		{
			get
			{
				return textBox.Text;
			}
			set
			{
				_bIgnoreEvents = true;
				textBox.Text = value;
				textBox.SelectionStart = textBox.Text.Length;
				textBox.InitUndo();
				_bIgnoreEvents = false;
			}
		}

		public int SelectionStart
		{
			get { return textBox.SelectionStart; }
			set { textBox.SelectionStart = value; }
		}

		public int SelectionLength
		{
			get { return textBox.SelectionLength; }
			set { textBox.SelectionLength = value; }
		}

		private bool _bIgnoreEvents = false;
		private bool _bChanged = false;

		private FindDialog _findDialog;
		private Dictionary<string, ToolStripMenuItem> _spellCheckLangMenuItems = new Dictionary<string, ToolStripMenuItem>();

		private TokenizerQueue tokenQueue = new TokenizerQueue();
		private int _inputHash = 0;
		private Timer _timer = new Timer();


		public WriteDialog()
		{
			InitializeComponent();

			Load += WriteDialog_Load;
			FormClosing += WriteDialog_FormClosing;
			
			tokenQueue.onTokenCount += TokenQueue_onTokenCount;

			_bIgnoreEvents = true;
			textBox.Font = AppSettings.WriteDialog.Font;
			textBox.AutoWordSelection = false;
			textBox.HideSelection = false;
			textBox.TextChanged += TextBox_TextChanged;
			textBox.Resize += TextBox_Resize;

			var syntaxFlags = RichTextBoxEx.SyntaxFlags.None;
			if (AppSettings.WriteDialog.Highlight)
			{
				syntaxFlags = RichTextBoxEx.SyntaxFlags.Default;
				EnumHelper.Toggle(ref syntaxFlags, RichTextBoxEx.SyntaxFlags.Names, AppSettings.WriteDialog.HighlightNames);
				EnumHelper.Toggle(ref syntaxFlags, RichTextBoxEx.SyntaxFlags.Numbers, AppSettings.WriteDialog.HighlightNumbers);
				EnumHelper.Toggle(ref syntaxFlags, RichTextBoxEx.SyntaxFlags.Pronouns, AppSettings.WriteDialog.HighlightPronouns);
			}
			EnumHelper.Toggle(ref syntaxFlags, RichTextBoxEx.SyntaxFlags.SpellChecking, AppSettings.Settings.SpellChecking);
			EnumHelper.Toggle(ref syntaxFlags, RichTextBoxEx.SyntaxFlags.DarkMode, AppSettings.WriteDialog.DarkMode);
			textBox.syntaxFlags = syntaxFlags;
			textBox.SetLineHeight(Constants.LineHeight);

			textBox.ControlEnterPressed += BtnOk_Click;

			if (AppSettings.WriteDialog.WordWrap)
			{
				textBox.WordWrap = true;
				textBox.ScrollBars = RichTextBoxScrollBars.ForcedVertical;
			}
			else
			{
				textBox.WordWrap = false;
				textBox.ScrollBars = RichTextBoxScrollBars.ForcedBoth;
			}

			this.Size = new Size(Math.Max(AppSettings.WriteDialog.WindowSize.X, this.MinimumSize.Width), Math.Max(AppSettings.WriteDialog.WindowSize.Y, this.MinimumSize.Height));
			if (AppSettings.WriteDialog.WindowLocation != default(Point))
			{
				this.StartPosition = FormStartPosition.Manual;
				this.Location = AppSettings.WriteDialog.WindowLocation;
			}
			else
			{
				this.StartPosition = FormStartPosition.CenterParent;
			}

			RefreshLineWidth();

			this.DoubleBuffered = true;
			this.SetStyle(ControlStyles.DoubleBuffer, true);

			_timer.Interval = 300;
			_timer.Elapsed += OnTimerElapsed;
			_timer.AutoReset = false;
			_timer.SynchronizingObject = this;

			_bIgnoreEvents = false;
		}

		private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			CalculateTokens();
		}

		~WriteDialog()
		{
			_timer.Dispose();
		}

		private void WriteDialog_Load(object sender, EventArgs e)
		{
			_bIgnoreEvents = true;
			textBox.ScrollToSelection();
			textBox.SetInnerMargins(6, 3, 5, 2);

			if (AppSettings.WriteDialog.DarkMode)
			{
				textBox.ForeColor = Constants.Colors.Dark.Foreground;
				textBox.BackColor = Constants.Colors.Dark.Background;
			}
			else
			{
				textBox.ForeColor = Constants.Colors.Light.Foreground;
				textBox.BackColor = Constants.Colors.Light.Background;
			}
			textBox.RefreshSyntaxHighlight(true);

			// Create dictionary menu items
			if (Dictionaries.IsOk())
			{
				foreach (var langKvp in Dictionaries.dictionaries)
				{
					var menuItem = new ToolStripMenuItem(langKvp.Value);
					menuItem.Click += delegate {
						if (MainForm.ChangeSpellingLanguage(langKvp.Key))
							textBox.SpellCheck(true, true);
					};
					spellCheckingMenuItem.DropDownItems.Add(menuItem);
					_spellCheckLangMenuItems.Add(langKvp.Key, menuItem);
				}
			}
			else
			{
				//checkSpellingMenuItem.Enabled = false;
				AppSettings.Settings.SpellChecking = false;
			}

			CalculateTokens();

			_bIgnoreEvents = false;
		}

		private void TextBox_Resize(object sender, EventArgs e)
		{
			textBox.SetInnerMargins(5, 3, 5, 2);
		}

		private void TextBox_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents || (textBox.syntaxHighlighter != null && textBox.syntaxHighlighter.isHighlighting))
				return;

			_bChanged = true;

			_timer.Stop();
			_timer.Start();
		}

		private void WriteDialog_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (DialogResult == DialogResult.Cancel && _bChanged)
			{
				var mr = MessageBox.Show(Resources.msg_dismiss_changes, Resources.cap_confirm, MessageBoxButtons.YesNo, MessageBoxIcon.None, MessageBoxDefaultButton.Button2);
				if (mr == DialogResult.No)
					e.Cancel = true;
			}
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == ShortcutKeys.Cancel)
			{
				DialogResult = DialogResult.Cancel;
				Close();
				return true;
			}
			else if (keyData == ShortcutKeys.Find)
			{
				findMenuItem_Click(this, EventArgs.Empty);
				return true;
			}
			else if (keyData == ShortcutKeys.FindNext)
			{
				findNextMenuItem_Click(this, EventArgs.Empty);
				return true;
			}
			else if (keyData == ShortcutKeys.FindPrevious)
			{
				findPreviousMenuItem_Click(this, EventArgs.Empty);
				return true;
			}		
			else if (keyData == ShortcutKeys.Replace)
			{
				replaceMenuItem_Click(this, EventArgs.Empty);
				return true;
			}
			return false;
		}

		private void WordWrapMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			AppSettings.WriteDialog.WordWrap = wordWrapMenuItem.Checked;

			if (wordWrapMenuItem.Checked)
			{
				textBox.WordWrap = true;
				textBox.ScrollBars = RichTextBoxScrollBars.ForcedVertical;
			}
			else
			{
				textBox.WordWrap = false;
				textBox.ScrollBars = RichTextBoxScrollBars.ForcedBoth;
			}

			RefreshLineWidth();
		}

		private void CopyMenuItem_Click(object sender, EventArgs e)
		{
			textBox.Copy();
			textBox.syntaxHighlighter.ReHighlight();
		}

		private void CutMenuItem_Click(object sender, EventArgs e)
		{
			textBox.Cut();
		}

		private void PasteMenuItem_Click(object sender, EventArgs e)
		{
			textBox.Paste(DataFormats.GetFormat("UnicodeText"));
		}

		private void SelectAllMenuItem_Click(object sender, EventArgs e)
		{
			textBox.SelectionStart = 0;
			textBox.SelectionLength = textBox.Text.Length;
		}

		private void ChangeFontMenuItem_Click(object sender, EventArgs e)
		{
			fontDialog.Font = textBox.Font;
			if (fontDialog.ShowDialog() == DialogResult.OK)
			{
				textBox.Font = fontDialog.Font;
				textBox.syntaxHighlighter.ReHighlight();
				AppSettings.WriteDialog.Font = fontDialog.Font;

				RefreshLineWidth();
			}
		}

		private void RefreshLineWidth()
		{
			if (AppSettings.WriteDialog.AutoBreakLine && AppSettings.WriteDialog.WordWrap)
				textBox.RightMargin = Math.Min((int)Math.Round(Constants.AutoWrapWidth * textBox.Font.SizeInPoints), Math.Max(this.Size.Width - 44, 0));
			else
				textBox.RightMargin = 0;
		}

		private void MenuStrip_MenuActivate(object sender, EventArgs e)
		{
			_bIgnoreEvents = true;
			findNextMenuItem.Enabled = string.IsNullOrEmpty(AppSettings.User.FindMatch) == false;
			findPreviousMenuItem.Enabled = string.IsNullOrEmpty(AppSettings.User.FindMatch) == false;
			wordWrapMenuItem.Checked = AppSettings.WriteDialog.WordWrap;
			autoBreakMenuItem.Checked = AppSettings.WriteDialog.AutoBreakLine;
			autoBreakMenuItem.Enabled = AppSettings.WriteDialog.WordWrap;
			enableHighlightingMenuItem.Checked = AppSettings.WriteDialog.Highlight;
			highlightNamesMenuItem.Checked = AppSettings.WriteDialog.HighlightNames;
			highlightNamesMenuItem.Enabled = AppSettings.WriteDialog.Highlight;
			highlightNumbersMenuItem.Checked = AppSettings.WriteDialog.HighlightNumbers;
			highlightNumbersMenuItem.Enabled = AppSettings.WriteDialog.Highlight;
			highlightPronounsMenuItem.Checked = AppSettings.WriteDialog.HighlightPronouns;
			highlightPronounsMenuItem.Enabled = AppSettings.WriteDialog.Highlight;
			darkModeMenuItem.Checked = AppSettings.WriteDialog.DarkMode;
			pasteMenuItem.Enabled = textBox.CanPaste(DataFormats.GetFormat("UnicodeText"));
			enableSpellCheckingMenuItem.Checked = AppSettings.Settings.SpellChecking;

			// Spell checking
			foreach (var kvp in _spellCheckLangMenuItems)
			{
				kvp.Value.Checked = string.Compare(AppSettings.Settings.Dictionary, kvp.Key, StringComparison.OrdinalIgnoreCase) == 0;
				kvp.Value.Enabled = AppSettings.Settings.SpellChecking;
			}

			_bIgnoreEvents = false;
		}

		private void WriteDialog_ResizeEnd(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			AppSettings.WriteDialog.WindowLocation = this.Location;
			AppSettings.WriteDialog.WindowSize = new Point(this.Size.Width, this.Size.Height);
		}

		private void AutoBreakLinesMenuItem_CheckStateChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			AppSettings.WriteDialog.AutoBreakLine = autoBreakMenuItem.Checked;
			RefreshLineWidth();			
		}

		private void swapGenderToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (var dlg = new GenderSwapDialog())
			{
				if (dlg.ShowDialog() == DialogResult.OK && dlg.Valid)
				{
					string text = textBox.Text;
					int selection = textBox.SelectionStart;
					int replacements = GenderSwap.SwapGenders(ref text, dlg.CharacterFrom, dlg.CharacterTo, dlg.UserFrom, dlg.UserTo, dlg.SwapCharacter, dlg.SwapUser);

					if (replacements > 0)
					{
						textBox.Text = text;
						textBox.SelectionStart = selection;
						textBox.SpellCheck(true, true);
					}
					if (replacements == 1)
					{
						MessageBox.Show(string.Format(Resources.msg_replace_single, replacements), Resources.cap_swap_pronouns, MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					else
					{
						MessageBox.Show(string.Format(Resources.msg_replace_plural, replacements), Resources.cap_swap_pronouns, MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
				}
			}
		}

		private void replaceMenuItem_Click(object sender, EventArgs e)
		{
			using (var dlg = new FindReplaceDialog())
			{
				if (textBox.SelectionLength > 0)
					dlg.Match = textBox.SelectedText;

				dlg.context = FindReplaceDialog.Context.Write;
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					string text = textBox.Text;
					int selection = textBox.SelectionStart;
					int replacements = FindReplace.Replace(ref text, dlg.Match, dlg.Replace, dlg.MatchWholeWord, dlg.IgnoreCase);

					if (replacements > 0)
					{
						textBox.Text = text;
						textBox.SelectionStart = selection;
					}
					if (replacements == 1)
						MessageBox.Show(string.Format(Resources.msg_replace_single, replacements), Resources.cap_replace, MessageBoxButtons.OK, MessageBoxIcon.Information);
					else
						MessageBox.Show(string.Format(Resources.msg_replace_plural, replacements), Resources.cap_replace, MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
		}

		private void autoReplacePronounMenuItem_Click(object sender, EventArgs e)
		{
			textBox.AutoReplaceWord(ReplaceWord.Option.Default);

		}

		private void autoReplaceUserPronounMenuItem_Click(object sender, EventArgs e)
		{
			textBox.AutoReplaceWord(ReplaceWord.Option.User);
		}

		private void findMenuItem_Click(object sender, EventArgs e)
		{
			if (_findDialog != null && !_findDialog.IsDisposed)
				_findDialog.Close(); // Close existing

			_findDialog = new FindDialog();
			_findDialog.Find += OnFind;

			if (textBox.SelectionLength > 0)
				_findDialog.Match = textBox.SelectedText;

			_findDialog.Show(this);
		}

		private void OnFind(object sender, FindDialog.FindEventArgs e)
		{
			if (string.IsNullOrEmpty(e.match))
				return;

			var findOptions = RichTextBoxFinds.None;
			if (e.matchCase)
				findOptions |= RichTextBoxFinds.MatchCase;
			if (e.wholeWord)
				findOptions |= RichTextBoxFinds.WholeWord;
			if (e.reverse)
				findOptions |= RichTextBoxFinds.Reverse;

			int start = textBox.SelectionStart + (e.reverse ? 0 :textBox.SelectionLength);
			int pos;
			if (e.reverse)
				pos = textBox.Find(e.match, 0, start, findOptions);
			else
				pos = textBox.Find(e.match, start, findOptions);

			// Wrap around?
			if (pos == -1)
				pos = textBox.Find(e.match, e.reverse ? textBox.Text.Length - 1 : 0, findOptions);

			if (pos >= 0)
			{
				textBox.SelectionStart = pos;
				textBox.SelectionLength = e.match.Length;
			}
			else
			{
				MessageBox.Show(Resources.msg_no_match, Resources.cap_find, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void findNextMenuItem_Click(object sender, EventArgs e)
		{
			OnFind(this, new FindDialog.FindEventArgs() {
				match = AppSettings.User.FindMatch ?? "",
				matchCase = AppSettings.User.FindMatchCase,
				wholeWord = AppSettings.User.FindWholeWords,
				reverse = false,
			});
		}

		private void findPreviousMenuItem_Click(object sender, EventArgs e)
		{
			OnFind(this, new FindDialog.FindEventArgs() {
				match = AppSettings.User.FindMatch ?? "",
				matchCase = AppSettings.User.FindMatchCase,
				wholeWord = AppSettings.User.FindWholeWords,
				reverse = true,
			});
		}

		private void enableHighlightingMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents || textBox.syntaxHighlighter == null)
				return;

			AppSettings.WriteDialog.Highlight = enableHighlightingMenuItem.Checked;

			var syntaxFlags = RichTextBoxEx.SyntaxFlags.None;
			if (AppSettings.WriteDialog.Highlight)
			{
				syntaxFlags = RichTextBoxEx.SyntaxFlags.Default;
				EnumHelper.Toggle(ref syntaxFlags, RichTextBoxEx.SyntaxFlags.Names, AppSettings.WriteDialog.HighlightNames);
				EnumHelper.Toggle(ref syntaxFlags, RichTextBoxEx.SyntaxFlags.Numbers, AppSettings.WriteDialog.HighlightNumbers);
				EnumHelper.Toggle(ref syntaxFlags, RichTextBoxEx.SyntaxFlags.Pronouns, AppSettings.WriteDialog.HighlightPronouns);
			}
			EnumHelper.Toggle(ref syntaxFlags, RichTextBoxEx.SyntaxFlags.SpellChecking, AppSettings.Settings.SpellChecking);
			EnumHelper.Toggle(ref syntaxFlags, RichTextBoxEx.SyntaxFlags.DarkMode, AppSettings.WriteDialog.DarkMode);
			textBox.syntaxFlags = syntaxFlags;
			

			if ((AppSettings.WriteDialog.Highlight || AppSettings.Settings.SpellChecking) == false)
			{
				_bIgnoreEvents = true;
				textBox.syntaxHighlighter.EnableHighlighting = false;
				textBox.Text = textBox.Text;
				_bIgnoreEvents = false;
			}
			else
			{
				_bIgnoreEvents = true;
				textBox.syntaxHighlighter.EnableHighlighting = true;
				_bIgnoreEvents = false;
				textBox.RefreshSyntaxHighlight(true);
			}
			textBox.syntaxHighlighter.ReHighlight(true);
		}

		private void highlightNamesMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			AppSettings.WriteDialog.HighlightNames = highlightNamesMenuItem.Checked;

			if (AppSettings.WriteDialog.HighlightNames)
				textBox.syntaxFlags = textBox.syntaxFlags | RichTextBoxEx.SyntaxFlags.Names;
			else
				textBox.syntaxFlags = textBox.syntaxFlags & ~RichTextBoxEx.SyntaxFlags.Names;
			textBox.RefreshSyntaxHighlight(true);
		}

		private void highlightPronounsMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			AppSettings.WriteDialog.HighlightPronouns = highlightPronounsMenuItem.Checked;

			if (AppSettings.WriteDialog.HighlightPronouns)
				textBox.syntaxFlags = textBox.syntaxFlags | RichTextBoxEx.SyntaxFlags.Pronouns;
			else
				textBox.syntaxFlags = textBox.syntaxFlags & ~RichTextBoxEx.SyntaxFlags.Pronouns;
			textBox.RefreshSyntaxHighlight(true);
		}

		private void highlightNumbersMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			AppSettings.WriteDialog.HighlightNumbers = highlightNumbersMenuItem.Checked;

			if (AppSettings.WriteDialog.HighlightNumbers)
				textBox.syntaxFlags = textBox.syntaxFlags | RichTextBoxEx.SyntaxFlags.Numbers;
			else
				textBox.syntaxFlags = textBox.syntaxFlags & ~RichTextBoxEx.SyntaxFlags.Numbers;
			textBox.RefreshSyntaxHighlight(true);

		}

		private void WriteDialog_Resize(object sender, EventArgs e)
		{
			RefreshLineWidth();
		}

		private void EnableSpellCheckingMenuItem_Click(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			MainForm.EnableSpellChecking(!enableSpellCheckingMenuItem.Checked);
			textBox.EnableSpellCheck(!enableSpellCheckingMenuItem.Checked);
			if ((AppSettings.WriteDialog.Highlight || AppSettings.Settings.SpellChecking) == false)
			{
				_bIgnoreEvents = true;
				textBox.syntaxHighlighter.EnableHighlighting = false;
				textBox.Text = textBox.Text;
				_bIgnoreEvents = false;
			}
			else
			{
				_bIgnoreEvents = true;
				textBox.syntaxHighlighter.EnableHighlighting = true;
				_bIgnoreEvents = false;
			}
			textBox.syntaxHighlighter.ReHighlight(true);
		}

		private void CalculateTokens()
		{
			// Recalculate token count
			Generator.Output output = new Generator.Output();

			string text = GingerString.FromParameter(textBox.Text).ToString();
			text = Ginger.Text.Process(text, Ginger.Text.EvalOption.LimitedOutputFormatting);

			output.persona = GingerString.FromOutput(text, Current.SelectedCharacter, Current.SelectedCharacter == 0);

			_inputHash = output.GetHashCode();
			tokenQueue.Schedule(output, _inputHash, this);
		}

		private void TokenQueue_onTokenCount(TokenizerQueue.Result result)
		{
			if (result.hash != _inputHash)
				return;

			labelTokens.Text = string.Format("Token count: {0}", result.tokens_total);
		}

		private void darkModeMenuItem_CheckStateChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			AppSettings.WriteDialog.DarkMode = darkModeMenuItem.Checked;

			if (AppSettings.WriteDialog.DarkMode)
				textBox.syntaxFlags = textBox.syntaxFlags | RichTextBoxEx.SyntaxFlags.DarkMode;
			else
				textBox.syntaxFlags = textBox.syntaxFlags & ~RichTextBoxEx.SyntaxFlags.DarkMode;

			if (AppSettings.WriteDialog.DarkMode)
			{
				textBox.ForeColor = Constants.Colors.Dark.Foreground;
				textBox.BackColor = Constants.Colors.Dark.Background;
			}
			else
			{
				textBox.ForeColor = Constants.Colors.Light.Foreground;
				textBox.BackColor = Constants.Colors.Light.Background;
			}

			textBox.RefreshSyntaxHighlight(true);
		}
	}
}
