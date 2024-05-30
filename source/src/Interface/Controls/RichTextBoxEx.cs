using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WinFormsSyntaxHighlighter;

namespace Ginger
{
	public class RichTextBoxEx : RichTextBox, IHasTextUndoStack<RichTextBoxEx.UndoState>, IHasPlaceholderText, ISearchable, ISpellChecked
	{
		#region Events
		public event EventHandler EnterPressed;
		public event EventHandler ControlEnterPressed;
		public event EventHandler ControlAltEnterPressed;
		public event EventHandler ValueChanged;
		public event BeforeUndoState OnBeforeUndoState;
		public class BeforeUndoEventArgs : EventArgs
		{
			public UndoState State { get; set; }
			public bool Handled { get; set; }
		}
		public delegate void BeforeUndoState(BeforeUndoEventArgs args);
		#endregion

		#region Properties
		[Category("RichTextBoxEx"), Description("Placeholder text")]
		public string Placeholder
		{
			get { return _placeholder; }
			set { _placeholder = value; }
		}
		private string _placeholder;

		[Browsable(true)]
		[Category("Behavior"), Description("Syntax highlighting")]
		public bool SyntaxHighlighting
		{
			get 
			{ 
				return _bEnableSyntaxHighlighting; 
			}
			set 
			{ 
				_bEnableSyntaxHighlighting = value;
				if (_bEnableSyntaxHighlighting && _syntaxHighlighter == null)
					_syntaxHighlighter = new SyntaxHighlighter(this);
			}
		}
		private bool _bEnableSyntaxHighlighting = false;

		[Browsable(true)]
		[Category("Behavior"), Description("Text")]
		public new string Text // Set text content only
		{
			get { return base.Text; }
			set { base.Text = value; PushHistory(); }
		}

		[Browsable(true)]
		[Category("RichTextBoxEx"), Description("Enable spell checking")]
		public bool SpellChecking { get; set; }

		#endregion

		#region Undo
		// Undo
		public struct UndoState
		{
			public struct Content
			{
				public string text;
				public int start;
				public int length;
			}
			public Content before;
			public Content after;
			public int hash;

			public UndoState(Content before, Content after, int selectionStart)
			{
				this.before = before;
				this.after = after;
				int lastWord;
				int count = CountWords(this.after.text, selectionStart, out lastWord);
				string leading = this.after.text.Substring(0, lastWord);
				this.hash = (31 * count) + leading.GetHashCode();
			}

			public bool Compare(UndoState other)
			{
				return this.hash == other.hash;
			}

			public static int CountWords(string text, int endPosition, out int lastWord)
			{
				if (text == null || text.Length == 0)
				{
					lastWord = 0;
					return 0;
				}

				if (endPosition < 0 || endPosition > text.Length)
					endPosition = text.Length;

				lastWord = 0;
				int count = 1;
				int pos = text.FindIndex(0, c => char.IsWhiteSpace(c));
				while (pos != -1 && pos < endPosition)
				{
					int next_word = text.FindIndex(pos, c => !char.IsWhiteSpace(c));
					if (next_word == -1 || next_word >= endPosition)
						return count;
					count++;
					lastWord = next_word;
					pos = text.FindIndex(next_word, c => char.IsWhiteSpace(c));
				}
				return count;
			}
		}

		private TextUndoStack<UndoState> _undo;

		// Buffer that stores the last couple of state changes.
		// This allows us to retrieve the before-state AFTER the text content has been changed.
		private UndoState.Content[] _stateBuffer = new UndoState.Content[2] { new UndoState.Content(), new UndoState.Content() };
		#endregion

		#region Syntax highlighting
		[Flags]
		public enum SyntaxFlags
		{
			None = 0,
			Standard		= 1 << 0,
			Names			= 1 << 1,
			Dialogue		= 1 << 2,
			Actions			= 1 << 3,
			Commands		= 1 << 4,
			Pronouns		= 1 << 5,
			Numbers			= 1 << 6,
			CodeBlock		= 1 << 7,
			SpellChecking	= 1 << 8,

			Default = Standard | Names | Dialogue | Actions | Commands | Numbers | CodeBlock | SpellChecking,
			Limited = Standard | Names | Commands | Numbers | SpellChecking,
			Code = Standard | Names | Commands | Numbers,
		}
		public SyntaxFlags syntaxFlags
		{
			get { return _syntaxFlags; }
			set
			{
				SetSyntaxHighlightPattern(value);
			}
		}
		private SyntaxFlags _syntaxFlags = SyntaxFlags.None;
		public SyntaxHighlighter syntaxHighlighter { get { return _syntaxHighlighter; } }
		private SyntaxHighlighter _syntaxHighlighter;
		private AsyncSyntaxHighlighter _asyncSyntaxHighlighter;

		public static bool AllowSyntaxHighlighting = true;
		private bool isHighlighting { get { return _isHighlighting || (_syntaxHighlighter != null && _syntaxHighlighter.isHighlighting); } }
		private bool _isHighlighting = false;
		#endregion

		#region Spell checking
		public TextSpans textSpans { get { return _textSpans; } }
		private TextSpans _textSpans;
		private bool _bHasIncompleteWords = false;
		#endregion

		#region DLL imports

		[DllImport("user32.dll")]
		private static extern int SendMessage(HandleRef hWnd, int msg, int wParam, int lParam);

		[DllImport("user32.dll")]
		private static extern int SendMessage(HandleRef hWnd, int msg, int wParam, ref PARAFORMAT lp);

		[DllImport("user32.dll")]
		private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

		[DllImport("user32.dll")]
		private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, Int32 wMsg, Int32 wParam, ref Point lParam);

		[DllImport("user32.dll")]
		private extern static IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		public static extern int SetCursor(IntPtr cursor);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		extern static bool GetScrollInfo(IntPtr hwnd, int fnBar, ref SCROLLINFO lpsi);

		[DllImport("user32.dll")]
		extern static int SetScrollInfo(IntPtr hwnd, int fnBar, [In] ref SCROLLINFO lpsi, bool fRedraw);

		// Constants from the Platform SDK
		private const int WM_SETCURSOR = 0x20;
		private const int WM_MOUSEWHEEL = 0x20A;
		private const int WM_VSCROLL = 0x115;
		private const int WM_SETREDRAW = 0x000B;
		private const int WM_PAINT = 0x000F;
		private const int WM_USER = 0x400;
		private const int EM_GETEVENTMASK = (WM_USER + 59);
		private const int EM_SETEVENTMASK = (WM_USER + 69);
		private const int EM_GETSCROLLPOS = WM_USER + 221;
		private const int SB_PAGEBOTTOM = 7;
		private const int EM_GETPARAFORMAT = 1085;
		private const int EM_SETPARAFORMAT = 1095;
		private const int EM_SETTYPOGRAPHYOPTIONS = 1226;
		private const int TO_ADVANCEDTYPOGRAPHY = 1;
		private const int PFM_ALIGNMENT = 0x8;
		private const int PFM_LINESPACING = 0x100;
		private const int SCF_DEFAULT = 0x0;
		private const int SCF_SELECTION = 0x1;
		private const int SB_LINEUP = 0;
		private const int SB_LINEDOWN = 1;
		private const int SB_THUMBPOSITION = 4;
		private const int SB_THUMBTRACK = 5;
		private const int SB_TOP = 6;
		private const int SB_BOTTOM = 7;
		private const int SB_ENDSCROLL = 8;

		struct SCROLLINFO
		{
			public uint cbSize;
			public uint fMask;
			public int nMin;
			public int nMax;
			public uint nPage;
			public int nPos;
			public int nTrackPos;
		}

		enum ScrollBarDirection
		{
			SB_HORZ = 0,
			SB_VERT = 1,
			SB_CTL = 2,
			SB_BOTH = 3
		}

		enum ScrollInfoMask
		{
			SIF_RANGE = 0x1,
			SIF_PAGE = 0x2,
			SIF_POS = 0x4,
			SIF_DISABLENOSCROLL = 0x8,
			SIF_TRACKPOS = 0x10,
			SIF_ALL = SIF_RANGE + SIF_PAGE + SIF_POS + SIF_TRACKPOS
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct PARAFORMAT
		{
			public int cbSize;
			public uint dwMask;
			public short wNumbering;
			public short wReserved;
			public int dxStartIndent;
			public int dxRightIndent;
			public int dxOffset;
			public short wAlignment;
			public short cTabCount;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public int[] rgxTabs;

			// PARAFORMAT2 from here onwards.
			public int dySpaceBefore;
			public int dySpaceAfter;
			public int dyLineSpacing;
			public short sStyle;
			public byte bLineSpacingRule;
			public byte bOutlineLevel;
			public short wShadingWeight;
			public short wShadingStyle;
			public short wNumberingStart;
			public short wNumberingStyle;
			public short wNumberingTab;
			public short wBorderSpace;
			public short wBorderWidth;
			public short wBorders;
		}
		#endregion

		private int _ignoreTextChanged = 0;
		private bool _bLocked = false;
		private bool _bRightDown = false; // Context menu
		private bool _bBreakUndoMerge = false;

#if DEBUG
		private static bool InDesignMode = LicenseManager.UsageMode == LicenseUsageMode.Designtime;
#endif

		public RichTextBoxEx() : base()
		{
			KeyPress += OnKeyPress;
			SelectionChanged += OnSelectionChanged;
			MouseLeave += OnMouseLeave;
			MouseDown += OnMouseDown;
			MouseUp += OnMouseUp;
			LostFocus += OnLostFocus;
			EnabledChanged += OnEnabledChanged;

			DetectUrls = false;
			AutoWordSelection = false;

			_undo = new TextUndoStack<UndoState>(this);
			_asyncSyntaxHighlighter = new AsyncSyntaxHighlighter();
			_asyncSyntaxHighlighter.onResult += OnAsyncSyntaxHighlighterResult;
		}

		protected override void OnTextChanged(EventArgs e)
		{
			if (isHighlighting)
			{
//				base.OnTextChanged(e);
				return;
			}

			if (_ignoreTextChanged > 0) // Guarded
				return;

			// Spell checking
			SpellCheck();

			// Rehighlight (if spell checker hasn't already done it)
			Rehighlight(false);

			// Scroll to bottom when typing on the bottom row
			// This prevents text from jumping around
			IntPtr handle = Handle;
			SCROLLINFO si = new SCROLLINFO();
			si.cbSize = (uint)Marshal.SizeOf(si);
			si.fMask = (uint)ScrollInfoMask.SIF_ALL;
			GetScrollInfo(handle, (int)ScrollBarDirection.SB_VERT, ref si);
			if (SelectionStart == this.Text.Length || si.nPage + si.nPos >= si.nMax - 16) // Scrollbar at bottom
				ScrollToBottom();

			PushUndo(new UndoState(
				_stateBuffer[0],
				_stateBuffer[1],
				SelectionStart)
			);

			// Notify parameter panels
			ValueChanged?.Invoke(this, EventArgs.Empty);

			// Notify other controls
			base.OnTextChanged(e);
		}

		private void OnKeyPress(object sender, KeyPressEventArgs e)
		{
/*			if (SelectionLength == 0)
			{
				__Guard(() => {
					SelectionColor = this.ForeColor;
				});
			}*/

			ScrollFix(false);
		}

		private void OnEnabledChanged(object sender, EventArgs e)
		{
			__Guard(() => {
				if (Enabled)
					ForeColor = SystemColors.WindowText;
				else
					ForeColor = SystemColors.GrayText;

				RefreshPatterns();
				RefreshSyntaxHighlight(true);
			});
		}

		private void OnLostFocus(object sender, EventArgs e)
		{
			_bRightDown = false;
			CheckIncompleteWords();
		}

		private void OnMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				_bRightDown = true;

				// Move cursor
				int pos_cursor = GetCharIndexFromPosition(e.Location);
				Select(pos_cursor, 0);
			}
		}

		private void OnMouseLeave(object sender, EventArgs e)
		{
			_bRightDown = false;
		}

		private void OnMouseUp(object sender, MouseEventArgs args)
		{
			if (args.Button == MouseButtons.Right)
			{
				if (_bRightDown)
					OnRightClick(args.Location);
				_bRightDown = false;
			}
		}

		private void OnSelectionChanged(object sender, EventArgs e)
		{
			if (_ignoreTextChanged > 0 || isHighlighting)
				return;

			if (_stateBuffer[1].start != this.SelectionStart
				|| _stateBuffer[1].length != this.SelectionLength) // Selection changed
			{
				PushHistory();
			}
		}

		private void OnRightClick(Point location)
		{
			ContextMenuStrip menu = new ContextMenuStrip();

			int pos_cursor = GetCharIndexFromPosition(location);

			// Spelling suggestions
			if (SpellChecking
				&& SpellChecker.IsInitialized
				&& AppSettings.Settings.SpellChecking
				&& _textSpans != null
				&& _textSpans.GetTagAt(pos_cursor) == TextSpan.Word.Tag.Misspelled)
			{
				int start, length;
				string word = _textSpans.GetWordAt(pos_cursor, out start, out length);
				if (word != null)
				{
					var suggestions = SpellChecker.Suggest(word);
					if (suggestions != null && suggestions.Count > 0)
					{
						for (int i = 0; i < suggestions.Count; ++i)
						{
							string replacement = suggestions[i];
							menu.Items.Add(new ToolStripMenuItem(suggestions[i], null,
								(s, e) => {
									ReplaceWordAt(start, length, replacement);
								}));
						}
					}
					menu.Items.Add(new ToolStripMenuItem("Add to dictionary", null, (s, e) => {
						AddWordToDictionary(word);
					}));
					menu.Items.Add(new ToolStripSeparator());
				}
			}

			menu.Items.Add(new ToolStripMenuItem("Undo", null,
				(s, e) => { Undo(); }) {
				Enabled = _undo.CanUndo(),
			});
			menu.Items.Add(new ToolStripMenuItem("Redo", null,
				(s, e) => { Redo(); }) {
				Enabled = _undo.CanRedo(),
			});
			menu.Items.Add(new ToolStripSeparator());

			menu.Items.Add(new ToolStripMenuItem("Copy", null, (s, e) => { Copy(); }) {
				Enabled = SelectionLength > 0,
			});

			menu.Items.Add(new ToolStripMenuItem("Cut", null, (s, e) => { CutSelection(); }) {
				Enabled = SelectionLength > 0,
			});

			menu.Items.Add(new ToolStripMenuItem("Paste", null, (s, e) => { PasteFromClipboard(); }) {
				Enabled = CanPaste(DataFormats.GetFormat("UnicodeText")),
			});

			menu.Show(this, location);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == ShortcutKeys.Undo)
			{
				ScrollFix(false);
				Undo();
				return true;
			}
			else if (keyData == ShortcutKeys.Redo)
			{
				ScrollFix(false);
				Redo();
				return true;
			}
			else if (keyData == ShortcutKeys.EraseWord)
			{
				ScrollFix(false);
				EraseWordBackward();
				return true;
			}
			else if (keyData == ShortcutKeys.DeleteWord)
			{
				ScrollFix(false);
				EraseWordForward();
				return true;
			}
			else if (keyData == (Keys.Control | Keys.X))
			{
				ScrollFix(false);
				CutSelection();
				return true;
			}
			else if (keyData == ShortcutKeys.AutoReplace)
			{
				ScrollFix(false);
				AutoReplaceWord(ReplaceWord.Option.Default);
				return true;
			}
			else if (keyData == ShortcutKeys.AutoReplaceUser)
			{
				ScrollFix(false);
				AutoReplaceWord(ReplaceWord.Option.User);
				return true;
			}
			else if (keyData == ShortcutKeys.AutoReplaceErase)
			{
				ScrollFix(false);
				AutoReplaceWord(ReplaceWord.Option.Erase);
				return true;
			}
			else if (keyData == Keys.Return && !Multiline)
			{
				ScrollFix(false);
				EnterPressed?.Invoke(this, EventArgs.Empty);
				return true;
			}
			else if (keyData == (Keys.Control | Keys.V) || keyData == (Keys.Shift | Keys.Insert))
			{
				ScrollFix(false);
				PasteFromClipboard();
				return true;
			}
			else if (keyData == (Keys.Control | Keys.Return) || keyData == (Keys.Control | Keys.Enter))
			{
				ControlEnterPressed?.Invoke(this, EventArgs.Empty);
				return true;
			}		
			else if (keyData == (Keys.Control | Keys.Alt | Keys.Return) || keyData == (Keys.Control | Keys.Alt | Keys.Enter))
			{
				ControlAltEnterPressed?.Invoke(this, EventArgs.Empty);
				return true;
			}
			else if (keyData == Keys.Return || keyData == Keys.Right || keyData == Keys.Left || keyData == Keys.Up || keyData == Keys.Down)
			{
				ScrollFix(false);
				CheckIncompleteWords();
			}
			
			return base.ProcessCmdKey(ref msg, keyData);
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_MOUSEWHEEL) // Override mouse wheel scroll to remove awkward and slow smooth scrolling
			{
				int scrollLines = SystemInformation.MouseWheelScrollLines;
				for (int i = 0; i < scrollLines; i++)
				{
					long delta = (m.WParam.ToInt64() & 0xff000000) >> 24;
					if (delta > 0) // when wParam is greater than 0
						SendMessage(this.Handle, WM_VSCROLL, (IntPtr)1, IntPtr.Zero); // else scroll down
					else
						SendMessage(this.Handle, WM_VSCROLL, (IntPtr)0, IntPtr.Zero); // scroll up 
				}
				return;
			}
			else if (m.Msg == WM_SETCURSOR) // Fixes a bug where cursor flicker between i-bar and arrow
			{
				var scrollbarWidth = System.Windows.Forms.SystemInformation.VerticalScrollBarWidth;
				var x = PointToClient(Control.MousePosition).X;
				var inScrollbar = x > this.Width - scrollbarWidth;

				var cursor = inScrollbar ? Cursors.Arrow : Cursors.IBeam;
				SetCursor(cursor.Handle);
				m.Result = new IntPtr(1);
				return;
			}
			else if (m.Msg == WM_PAINT)
			{
				base.WndProc(ref m);

				using (Graphics g = Graphics.FromHwnd(m.HWnd))
				{
					OnUserPaint(new PaintEventArgs(g, ClientRectangle));
				}
				return;
			}

			base.WndProc(ref m);
		}
		
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			// Enable support for adjusting lineheight
			SendMessage(new HandleRef(this, Handle), EM_SETTYPOGRAPHYOPTIONS, TO_ADVANCEDTYPOGRAPHY, TO_ADVANCEDTYPOGRAPHY);
		}

		private void OnUserPaint(PaintEventArgs e)
		{
			// Draw placeholder
			if (this.Text == string.Empty && _placeholder != null && Enabled && !Focused)
			{
				if (Multiline)
				{
					TextRenderer.DrawText(e.Graphics, _placeholder, this.Font,
						new Point(ClientRectangle.Location.X, ClientRectangle.Location.Y + 2),
						Color.FromArgb(144, 144, 144), Color.Empty);
				}
				else
				{
					TextRenderer.DrawText(e.Graphics, _placeholder, this.Font,
						new Point(ClientRectangle.Location.X - 3, ClientRectangle.Location.Y),
						Color.FromArgb(144, 144, 144), Color.Empty);
				}
			}
			// Draw border
			//			e.Graphics.DrawRectangle(new Pen(Color.Green), new Rectangle(0, 0, Width - 1, Height - 1));
		}

		public void InitUndo()
		{
			_undo.Clear();
		}

		public new void Undo()
		{
			this.Suspend();
			ClearSpellCheck();
			bool didUndo = _undo.Undo();
			this.Resume();
			_bBreakUndoMerge = true;
			if (didUndo)
				Refresh(); // Visual feedback
		}

		public new void Redo()
		{
			this.Suspend();
			ClearSpellCheck();
			bool didRedo = _undo.Redo();
			this.Resume();
			_bBreakUndoMerge = true;
			if (didRedo)
				Refresh(); // Visual feedback
		}

		private void PushUndo(UndoState state)
		{
			// Compare hash (based on word count), and push to stack
			if (!_bBreakUndoMerge && _undo.undoCount > 0 && _undo.PeekUndo().hash == state.hash)
			{
				var prevState = _undo.Pop(); // Discard
				_undo.PushState(new UndoState() {
					before = prevState.before,
					after = state.after,
					hash = state.hash,
				});
				return;
			}
			_bBreakUndoMerge = false;
			_undo.PushState(state);
		}

		private void PushHistory()
		{
			_stateBuffer[0] = _stateBuffer[1];
			_stateBuffer[1] = new UndoState.Content() {
				text = this.Text,
				start = this.SelectionStart,
				length = this.SelectionLength,
			};
		}

		public void OnUndoState(UndoState state, bool redo = false)
		{
			if (OnBeforeUndoState != null)
			{
				// Call handlers
				foreach (BeforeUndoState @delegate in OnBeforeUndoState.GetInvocationList())
				{
					BeforeUndoEventArgs args = new BeforeUndoEventArgs() {
						State = state,
						Handled = false,
					};
					@delegate(args);
					if (args.Handled)
					{
						state = args.State;
						break;
					}
				}
			}

			__Guard(() => {
				if (redo)
					SetState(state.after.text, state.after.start, state.after.length);
				else
					SetState(state.before.text, state.before.start, state.before.length);
			}, true);
		}

		public void RefreshPatterns()
		{
			var flags = _syntaxFlags;
			_syntaxFlags = SyntaxFlags.None;
			SetSyntaxHighlightPattern(flags);
		}

		private void SetSyntaxHighlightPattern(SyntaxFlags flags)
		{
			if (_syntaxHighlighter == null)
				return;

			if (_syntaxFlags == flags)
				return; // No change
			_syntaxFlags = flags;

			syntaxHighlighter.ClearPatterns();
			if (_syntaxFlags.Contains(SyntaxFlags.Standard))
			{
				// Comment /*...*/
				syntaxHighlighter.AddPattern(new PatternDefinition(@"\/\*[\s\S]*?\*\/"), new SyntaxStyle(ColorTranslator.FromHtml("#969696"), false, true), -1);
				syntaxHighlighter.AddPattern(new PatternDefinition(@"<!--[\s\S]*?-->"), new SyntaxStyle(ColorTranslator.FromHtml("#969696"), false, true), -1);

				// Markdown image ![]()
				syntaxHighlighter.AddPattern(new PatternDefinition(@"!\[(.*)\]\((.+)\)"), new SyntaxStyle(ColorTranslator.FromHtml("#C00000"), false, false), -1);
			}

			// Code `...`
			if (_syntaxFlags.Contains(SyntaxFlags.CodeBlock))
				syntaxHighlighter.AddPattern(new PatternDefinition("`[^`]*`"), SyntaxStyle.Monospaced(ColorTranslator.FromHtml("#606060")), -1);

			// Feet/Inches
			if (_syntaxFlags.Contains(SyntaxFlags.Numbers)) 
				syntaxHighlighter.AddPattern(new PatternDefinition("\\d+(\"|\'|st|nd|th)"), new SyntaxStyle(Color.Purple), -1);

			// Dialogue "..."
			if (_syntaxFlags.Contains(SyntaxFlags.Dialogue))
			{
				syntaxHighlighter.AddPattern(new PatternDefinition("\"[^\"]*\""), new SyntaxStyle(ColorTranslator.FromHtml("#c06000")), 0);
				syntaxHighlighter.AddPattern(new PatternDefinition("\u201C[^\"]*\u201D"), new SyntaxStyle(ColorTranslator.FromHtml("#c06000")), 0);
			}

			// Narration *...*
			if (_syntaxFlags.Contains(SyntaxFlags.Actions))
				syntaxHighlighter.AddPattern(new PatternDefinition("\\*+[^\\*]*\\*+"), new SyntaxStyle(ColorTranslator.FromHtml("#406080"), false, false), 0);

			// Commands {char}, {user}, {they}, etc.
			if (_syntaxFlags.Contains(SyntaxFlags.Commands))
			{
				// Invalid patterns
				syntaxHighlighter.AddPattern(new PatternDefinition(@"\{\{(?i)\b(char|user|original)\b\}\}"), SyntaxStyle.Underlined(Color.FromArgb(192, 0, 0)), 2);
				syntaxHighlighter.AddPattern(new PatternDefinition(@"\{(?i)\bcharacter\b\}"), SyntaxStyle.Underlined(Color.FromArgb(192, 0, 0)), 2);

				// character
				syntaxHighlighter.AddPattern(new PatternDefinition(@"\{(?i)\b(char|user|card|name|original|gender|they'll|they're|they've|they'd|they|them|theirs|their|themselves|he'll|he's|he's|he'd|he|him|his|his|himself|she'll|she's|she's|she'd|she|her|hers|her|herself|is|are|isn't|aren't|has|have|hasn't|haven't|was|were|wasn't|weren't|does|do|doesn't|don't|s|y|ies|es)\b\}"),
					new SyntaxStyle(Color.Maroon), 1);

				// user
				syntaxHighlighter.AddPattern(new PatternDefinition(@"\{\#(?i)\b(gender|name|they'll|they're|they've|they'd|they|them|theirs|their|themselves|he'll|he's|he's|he'd|he|him|his|his|himself|she'll|she's|she's|she'd|she|her|hers|her|herself|is|are|isn't|aren't|has|have|hasn't|haven't|was|were|wasn't|weren't|does|do|doesn't|don't|s|y|ies|es)\b\}"),
					new SyntaxStyle(Color.Maroon), 1);
			}

			// Pronouns
			if (_syntaxFlags.Contains(SyntaxFlags.Pronouns))
			{
				syntaxHighlighter.AddPattern(new PatternDefinition(@"(?i)\b(he/she|him/her|his/hers|his/her|himself/herself|he's|he'll|he'd|he|him|his|himself|she'll|she's|she'd|she|her|hers|herself|they'll|they're|they've|they'd|they|them|theirs|their|themselves)\b"), new SyntaxStyle(Color.FromArgb(192, 0, 192)), 1);
			}

			// Digits
			if (_syntaxFlags.Contains(SyntaxFlags.Numbers))
				syntaxHighlighter.AddPattern(new PatternDefinition(@"[-+#]?\b\d+(?:[.,]\d)?\b"), new SyntaxStyle(Color.Purple), -1);

			// Names
			if (Current.Characters != null && AppSettings.Settings.AutoConvertNames && _syntaxFlags.Contains(SyntaxFlags.Names))
			{
				string[] names = new string[Current.Characters.Count + 1];
				names[0] = Current.Card.userPlaceholder;
				for (int i = 0; i < Current.Characters.Count; ++i)
					names[i + 1] = Current.Characters[i].namePlaceholder;
				syntaxHighlighter.SetCharacterNames(names, new SyntaxStyle(Color.FromArgb(0, 0, 192)), 1);
			}
		}

		public void InvalidateSyntaxHighlighting()
		{
			_asyncSyntaxHighlighter.Invalidate();
		}

		public void RefreshSyntaxHighlight(bool immediate = false)
		{
			if (syntaxHighlighter == null)
				return;

			if (_syntaxFlags.Contains(SyntaxFlags.Names) != AppSettings.Settings.AutoConvertNames)
			{
				var syntaxFlagsNoSpelling = _syntaxFlags & ~SyntaxFlags.SpellChecking;

				if (AppSettings.Settings.AutoConvertNames && syntaxFlagsNoSpelling != SyntaxFlags.None)
					SetSyntaxHighlightPattern(_syntaxFlags | SyntaxFlags.Names);
				else
					SetSyntaxHighlightPattern(_syntaxFlags & ~SyntaxFlags.Names);
				RefreshPatterns();
			}

			// Update names
			if (Current.Characters != null && AppSettings.Settings.AutoConvertNames && _syntaxFlags.Contains(SyntaxFlags.Names))
			{
				string[] names = new string[Current.Characters.Count + 1];
				names[0] = Current.Card.userPlaceholder;
				for (int i = 0; i < Current.Characters.Count; ++i)
					names[i + 1] = Current.Characters[i].namePlaceholder;
				syntaxHighlighter.SetCharacterNames(names, new SyntaxStyle(Color.FromArgb(0, 0, 192)), 1);
			}

			Rehighlight(immediate);
		}

		private void Rehighlight(bool immediate)
		{
#if DEBUG
			if (InDesignMode)
				return; // We mustn't do this in the VS2019 WinForms designer
#endif

			if (_syntaxHighlighter == null 
				|| _syntaxHighlighter.EnableHighlighting == false 
				|| AllowSyntaxHighlighting == false)
				return;

			_syntaxHighlighter.Font = this.Font;

			if (immediate)
			{
				_asyncSyntaxHighlighter.Cancel();
				_syntaxHighlighter.SetSpellChecking(_textSpans);
				if (this.Enabled)
					_syntaxHighlighter.EnableColoredStyles = true;
				else
					_syntaxHighlighter.EnableColoredStyles = false;

				__Guard(() => {
					if (!_bLocked)
					{
						DisableRedrawAndDo(() => {
							_syntaxHighlighter.ReHighlight();
						});
					}
					else
					{
						_syntaxHighlighter.ReHighlight();
					}
				});
			}
			else // Enqueue async
			{
				if (this.Enabled)
					_syntaxHighlighter.EnableColoredStyles = true;
				else
					_syntaxHighlighter.EnableColoredStyles = false;

				_asyncSyntaxHighlighter.Schedule(this);
			}
		}

		private void OnAsyncSyntaxHighlighterResult(AsyncSyntaxHighlighter.Result result)
		{
			if (result.hash != this.Text.GetHashCode())
				return;

			if (IsDisposed)
				return; // Textbox no longer exists

			__Guard(() => {
				DisableRedrawAndDo(() => {
					_isHighlighting = true;
					this.Rtf = result.rtfText;
					_isHighlighting = false;
				}, SelectionStart, SelectionLength);
			});
		}

		public void SpellCheck(bool bForce = false, bool bRehighlight = false)
		{
			if (!(SpellChecking && AppSettings.Settings.SpellChecking && SpellChecker.IsInitialized))
				return;

			if (!syntaxFlags.Contains(SyntaxFlags.SpellChecking))
			{
				ClearSpellCheck();
				if (bRehighlight)
					Rehighlight(true);
				return;
			}
			if (string.IsNullOrWhiteSpace(this.Text))
			{
				_textSpans = null;
				return;
			}
			if (bForce)
				_textSpans = null;

			var textSpans = TextSpans.FromString(this.Text);
			if (_textSpans == null)
			{
				_textSpans = textSpans;
				SpellChecker.SpellCheck(_textSpans);
			}
			else
			{
				// Only update the rows that has changed.
				// Then, find the currently typed word and remove it from the spell check.
				int top = 0;
				for (; top < textSpans.Count && top < _textSpans.Count; ++top)
				{
					if (textSpans.spans[top].GetHashCode() != _textSpans.spans[top].GetHashCode())
						break;
				}
				int bottom = 0;
				for (; bottom < textSpans.Count && bottom < _textSpans.Count - top; ++bottom)
				{
					if (textSpans.spans[textSpans.Count - bottom - 1].GetHashCode() != _textSpans.spans[_textSpans.Count - bottom - 1].GetHashCode())
						break;
				}
				if (_textSpans.Count == textSpans.Count && top == _textSpans.Count)
					return;

				bottom = Math.Min(bottom, _textSpans.Count - top);

				// Combine spans: [unchanged]+[changed]+[unchanged]
				var rows = new List<TextSpan>();
				int offset = 0;
				int cursor = SelectionStart;
				for (int i = 0; i < textSpans.Count; ++i)
				{
					if (i < top) // Unchanged top
					{
						var span = _textSpans.spans[i];
						rows.Add(span); // As is
						offset = span.offset + span.length;
					}
					else // Changed middle
					{
						var span = textSpans.spans[i];
						SpellChecker.SpellCheck(new TextSpan[] { span });
						rows.Add(span); // Changed
						offset = span.offset + span.length;

						// Has incomplete word?
						int idxCurrent = Array.FindIndex(span.words, w => (cursor >= span.offset + w.start) && (cursor <= span.offset + w.end));
						if (idxCurrent != -1)
						{
							span.words[idxCurrent].tag = TextSpan.Word.Tag.Incomplete;
							_bHasIncompleteWords = true;
						}
					}
				}
				for (int i = bottom; i > 0; --i) // Unchanged bottom
				{
					var span = _textSpans.spans[_textSpans.Count - i];
					rows.Add(span); // As is
					span.offset = offset; // Adjust offset
					offset += span.text.Length;
					continue;
				}

				_textSpans = new TextSpans() {
					text = textSpans.text,
					spans = rows.ToArray(),
				};
			}
			if (bRehighlight)
				Rehighlight(true);
		}

		public void EnableSpellCheck(bool enabled)
		{
			if (!SpellChecking)
				return;

			if (enabled)
			{
				syntaxFlags |= SyntaxFlags.SpellChecking;
				SpellCheck();
				Rehighlight(true);
			}
			else
			{
				syntaxFlags &= ~SyntaxFlags.SpellChecking;
				ClearSpellCheck();
				Rehighlight(true);
			}
		}

		private void AddWordToDictionary(string word)
		{
			SpellChecker.AddToDictionary(word);
			MainForm.RefreshSpellChecking();
			ClearSpellCheck();
			SpellCheck();
			Rehighlight(true);
		}

		public void CheckIncompleteWords()
		{
			if (_bHasIncompleteWords && _textSpans != null)
			{
				_bHasIncompleteWords = false;
				var incompleteRows = _textSpans.spans.Where(r => r.words.ContainsAny(w => w.tag == TextSpan.Word.Tag.Incomplete));
				if (incompleteRows.IsEmpty())
					return;

				SpellChecker.SpellCheck(incompleteRows);
				Rehighlight(false);
			}
		}

		public void ClearSpellCheck()
		{
			_textSpans = null;
		}

		public void SetText(string text, int selectionStart = 0, int selectionLength = 0)
		{
			SetState(text, selectionStart, selectionLength);
		}

		public void SetTextSilent(string text)
		{
			// Sets the text without triggering events (perf)
			__Guard(() => {
				base.Text = text;
			}, true);
		}

		private void SetState(string text, int selectionStart, int selectionLength = 0, bool bResize = true)
		{
			DisableRedrawAndDo(() => {
				__Guard(() => {
					base.Text = text;

					SpellCheck();
					Rehighlight(true);
				}, true);
			}, selectionStart, selectionLength);
			PushHistory();
		}

		// Perform action while ignoring TextChanged event (which RichTextBox loves to spam at every opportunity)
		private void __Guard(Action fn, bool textChanged = false)
		{
			_ignoreTextChanged++;
			fn.Invoke();
			_ignoreTextChanged--;
			if (textChanged)
				ValueChanged?.Invoke(this, EventArgs.Empty);
		}

		private void PasteFromClipboard()
		{
			try
			{
				var formats = Clipboard.GetDataObject().GetFormats();

				string content;
				if (Array.IndexOf(formats, "System.String") != -1)
					content = (string)Clipboard.GetDataObject().GetData("System.String");
				else if (Array.IndexOf(formats, "UnicodeText") != -1)
					content = (string)Clipboard.GetDataObject().GetData("UnicodeText");
				else
					content = Clipboard.GetText();

				if (content == null)
					return;

				StringBuilder sb = new StringBuilder(content);

				// Replace placeholders
				string user = AppSettings.Settings.AutoConvertNames ? Current.Card.userPlaceholder : GingerString.UserMarker;
				string character = AppSettings.Settings.AutoConvertNames ? Current.Character.namePlaceholder : GingerString.CharacterMarker;

				Utility.ReplaceWholeWord(sb, "<bot>", character, true);         // Tavern
				Utility.ReplaceWholeWord(sb, "<user>", user, true);             // Tavern
				Utility.ReplaceWholeWord(sb, "{{char}}", character, true);      // Tavern
				Utility.ReplaceWholeWord(sb, "{{user}}", user, true);           // Tavern
				Utility.ReplaceWholeWord(sb, "#{character}", character, true);  // Faraday
				Utility.ReplaceWholeWord(sb, "{character}", character, true);   // Faraday

				string beforeText = Text;
				int beforePos = SelectionStart;
				int beforeLength = SelectionLength;

				Clipboard.SetText(sb.ToString()); // Replace with processed text
				Paste();
				Clipboard.SetText(content); // Restore to original content

				sb.ConvertLinebreaks(Linebreak.LF);

				PushUndo(new UndoState(
					new UndoState.Content() {
						text = beforeText,
						start = beforePos,
						length = beforeLength,
					},
					new UndoState.Content() {
						text = Text,
						start = beforePos,
						length = sb.Length,
					}, SelectionStart));

				PushHistory();
				_bBreakUndoMerge = true;

				Rehighlight(true);
			}
			catch
			{ }
		}

		private void EraseWordBackward()
		{
			int pos = SelectionStart;
			int pos_end = pos;

			if (SelectionLength > 0)
			{
				pos = SelectionStart;
				pos_end = SelectionStart + SelectionLength;
			}

			bool onWhitespace = pos > 0 && char.IsWhiteSpace(Text[pos - 1]);

			// Erase whitespace first
			if (onWhitespace)
			{
				for (int i = pos - 1; i >= 0; --i)
				{
					pos = i;
					if (char.IsWhiteSpace(Text[i]))
						continue;
					pos = i + 1;
					break;
				}
			}

			bool onPunctuation = pos > 0 && char.IsPunctuation(Text[pos - 1]);
			bool onLetter = pos > 0 && char.IsLetterOrDigit(Text[pos - 1]);

			onLetter |= !(onWhitespace || onPunctuation || onLetter);
			if (pos > 0 && onPunctuation)
			{
				// Delete 
				for (int i = pos - 1; i >= 0; --i)
				{
					pos = i;
					if (char.IsPunctuation(Text[i]))
						continue;
					pos = i + 1;
					break;
				}
			}
			else if (pos > 0 && onLetter)
			{
				// Delete 
				for (int i = pos - 1; i >= 0; --i)
				{
					pos = i;
					if (!(char.IsPunctuation(Text[i]) || char.IsWhiteSpace(Text[i])))
						continue;
					pos = i + 1;
					break;
				}
			}

			string beforeText = Text;
			int beforePos = SelectionStart;
			int beforeLength = SelectionLength;

			SetState(Text.Remove(pos, pos_end - pos), pos);
			Refresh();

			PushUndo(new UndoState(
				new UndoState.Content() {
					text = beforeText,
					start = beforePos,
					length = beforeLength,
				},
				new UndoState.Content() {
					text = Text,
					start = SelectionStart,
					length = SelectionLength,
				}, SelectionStart));
			PushHistory();
			_bBreakUndoMerge = true;
		}

		private void EraseWordForward()
		{
			int pos = SelectionStart;
			int pos_start = pos;

			if (SelectionLength > 0)
			{
				pos_start = SelectionStart;
				pos = SelectionStart + SelectionLength;
			}
			else if (pos == Text.Length)
				return;

			// Erase whitespace
			for (; pos < Text.Length; ++pos)
			{
				if (!char.IsWhiteSpace(Text[pos]))
					break;
			}

			bool onPunctuation = pos < Text.Length && char.IsPunctuation(Text[pos]);
			bool onLetter = pos < Text.Length && char.IsLetterOrDigit(Text[pos]);
			onLetter |= !(onPunctuation || onLetter);

			if (onPunctuation)
			{
				// Erase word
				for (; pos < Text.Length; ++pos)
				{
					if (!char.IsPunctuation(Text[pos]))
						break;
				}
			}
			else if (onLetter)
			{
				// Erase word
				for (; pos < Text.Length; ++pos)
				{
					if (char.IsPunctuation(Text[pos]) || char.IsWhiteSpace(Text[pos]))
						break;
				}
			}

			// Also erase superceding whitespace
			for (; pos < Text.Length; ++pos)
			{
				if (!char.IsWhiteSpace(Text[pos]))
					break;
			}

			string beforeText = Text;
			int beforePos = SelectionStart;
			int beforeLength = SelectionLength;

			this.Suspend();
			if (pos >= Text.Length)
				SetState(Text.Remove(pos_start), pos_start);
			else
				SetState(Text.Remove(pos_start, pos - pos_start), pos_start);
			this.Resume();
			Refresh();

			PushUndo(new UndoState(
				new UndoState.Content() {
					text = beforeText,
					start = beforePos,
					length = beforeLength,
				},
				new UndoState.Content() {
					text = Text,
					start = SelectionStart,
					length = SelectionLength,
				}, SelectionStart));
			PushHistory();
			_bBreakUndoMerge = true;
		}

		private void CutSelection()
		{
			if (SelectionLength == 0)
				return;

			string beforeText = Text;
			int beforePos = SelectionStart;
			int beforeLength = SelectionLength;

			__Guard(() => {
				Cut();
			}, true);

			PushUndo(new UndoState(
				new UndoState.Content() {
					text = beforeText,
					start = beforePos,
					length = beforeLength,
				},
				new UndoState.Content() {
					text = Text,
					start = SelectionStart,
					length = SelectionLength,
				}, SelectionStart));
			PushHistory();
			_bBreakUndoMerge = true;
		}

		public void AutoReplaceWord(ReplaceWord.Option option)
		{
			int startPos;
			int length;
			string replacement;
			if (ReplaceWord.FindReplacement(this, option, out startPos, out length, out replacement) == false)
				return;

			string beforeText = Text;
			int beforePos = SelectionStart;
			int beforeLength = SelectionLength;

			this.Suspend();
			var scrollY = this.GetVScrollPos();
			StringBuilder sb = new StringBuilder(Text);
			sb.Remove(startPos, length);
			sb.Insert(startPos, replacement);
			SetState(sb.ToString(), startPos + replacement.Length);
			this.SetVScrollPos(scrollY);
			this.Resume();

			PushUndo(new UndoState(
				new UndoState.Content() {
					text = beforeText,
					start = beforePos,
					length = beforeLength,
				},
				new UndoState.Content() {
					text = Text,
					start = SelectionStart,
					length = SelectionLength,
				}, SelectionStart));
			PushHistory();
			_bBreakUndoMerge = true;
		}

		public void ScrollToBottom()
		{
			SendMessage(this.Handle, WM_VSCROLL, (IntPtr)SB_PAGEBOTTOM, IntPtr.Zero);
		}

		public int Find(string match, bool matchCase, bool matchWord, bool reverse, int startIndex = -1)
		{
			return ControlExtensions.Find(this, match, matchCase, matchWord, reverse, startIndex);
		}


		private void ReplaceWordAt(int pos, int length, string replacement)
		{
			string beforeText = Text;
			int beforePos = SelectionStart;
			int beforeLength = SelectionLength;

			// Replace word
			this.Suspend();
			var scrollY = this.GetVScrollPos();
			StringBuilder sb = new StringBuilder(Text);
			sb.Remove(pos, length);
			sb.Insert(pos, replacement);
			SetState(sb.ToString(), pos + replacement.Length);
			this.SetVScrollPos(scrollY);
			this.Resume();

			PushUndo(new UndoState(
				new UndoState.Content() {
					text = beforeText,
					start = beforePos,
					length = beforeLength,
				},
				new UndoState.Content() {
					text = Text,
					start = SelectionStart,
					length = SelectionLength,
				}, SelectionStart));
			PushHistory();
			_bBreakUndoMerge = true;
		}

		public void DisableRedrawAndDo(Action action, int selectStart = -1, int selectLength = 0)
		{
			IntPtr stateLocked = IntPtr.Zero;
			Lock(ref stateLocked);
			int hscroll = this.GetHScrollPos();
			int vscroll = this.GetVScrollPos();

			if (selectStart < 0)
			{
				selectStart = this.SelectionStart;
				selectLength = this.SelectionLength;
			}

			action();
			this.Select(selectStart, selectLength);

			if (Multiline)
			{
				this.SetHScrollPos(hscroll);
				this.SetVScrollPos(vscroll);
			}

			Unlock(ref stateLocked);
		}

		private void Lock(ref IntPtr stateLocked)
		{
			if (_bLocked)
				throw new Exception("Already locked");

			_bLocked = true;
			// Stop redrawing:  
			SendMessage(this.Handle, WM_SETREDRAW, 0, IntPtr.Zero);
			// Stop sending of events:  
			stateLocked = SendMessage(this.Handle, EM_GETEVENTMASK, 0, IntPtr.Zero);
			// change colors and stuff in the RichTextBox 
		}

		private void Unlock(ref IntPtr stateLocked)
		{
			_bLocked = false;
			// turn on events  
			SendMessage(this.Handle, EM_SETEVENTMASK, 0, stateLocked);
			// turn on redrawing  
			SendMessage(this.Handle, WM_SETREDRAW, 1, IntPtr.Zero);

			stateLocked = IntPtr.Zero;
			this.Invalidate();
		}

		private void ScrollFix(bool isScrolling)
		{
			MainForm.EnableFormLevelDoubleBuffering(!isScrolling);
		}

		public void SetLineHeight(float lineHeight)
		{
			if (Multiline)
			{
				__Guard(() => {
					var fmt = new PARAFORMAT();
					fmt.cbSize = Marshal.SizeOf(fmt);
					fmt.dwMask = PFM_LINESPACING;
					fmt.dyLineSpacing = Convert.ToInt32(Math.Max(lineHeight * 240, 0));
					fmt.bLineSpacingRule = 0x3;
					SendMessage(new HandleRef(this, Handle), EM_SETPARAFORMAT, SCF_DEFAULT, ref fmt);

					if (_syntaxHighlighter != null)
						_syntaxHighlighter.LineHeight = Constants.LineHeight;
				});
			}
		}

		public float GetLineHeight()
		{
			var fmt = new PARAFORMAT();
			fmt.cbSize = Marshal.SizeOf(fmt);
			SendMessage(new HandleRef(this, Handle), EM_GETPARAFORMAT, SCF_DEFAULT, ref fmt);
			return fmt.dyLineSpacing / 240.0f;
		}

		public int GetScrollPos()
		{
			Point rtfPoint = Point.Empty;
			SendMessage(this.Handle, EM_GETSCROLLPOS, 0, ref rtfPoint);
			return rtfPoint.Y;
		}

		public void RefreshScrollbar(int height)
		{
			OnVScroll(EventArgs.Empty);
		}

		public void ScrollToLine(int iLine)
		{
			if (iLine < 0)
				return;

			int iChar = GetFirstCharIndexFromLine(iLine);

			Point cPos = GetPositionFromCharIndex(iChar);

			IntPtr handle = Handle;

			// Get current scroller position

			SCROLLINFO si = new SCROLLINFO();
			si.cbSize = (uint)Marshal.SizeOf(si);
			si.fMask = (uint)ScrollInfoMask.SIF_ALL;
			GetScrollInfo(handle, (int)ScrollBarDirection.SB_VERT, ref si);

			// Increase position by pixels
			si.nPos += cPos.Y - 2; // Inner margin
//			si.nPos = Math.Min(si.nPos, si)

			// Reposition scroller
			SetScrollInfo(handle, (int)ScrollBarDirection.SB_VERT, ref si, true);

			// Send a WM_VSCROLL scroll message using SB_THUMBTRACK as wParam
			// SB_THUMBTRACK: low-order word of wParam, si.nPos high-order word of  wParam

			IntPtr ptrWparam = new IntPtr(SB_THUMBTRACK + 0x10000 * si.nPos);
			IntPtr ptrLparam = new IntPtr(0);
			SendMessage(handle, WM_VSCROLL, ptrWparam, ptrLparam);
		}
	}
}
