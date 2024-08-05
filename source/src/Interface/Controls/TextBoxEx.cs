using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Ginger
{
	public interface IHasPlaceholderText
	{
		string Placeholder { set; }
	}

	public class TextBoxEx : TextBox, IHasTextUndoStack<TextBoxEx.UndoState>, IHasPlaceholderText, ISearchable
	{
		// Placeholder
		private const int EM_SETCUEBANNER = 0x1501;
		[DllImport("user32.dll")]
		private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)]string lParam);

		[Category("TextBoxEx"), Description("Placeholder text")]
		public string Placeholder
		{
			get { return _placeholder; }
			set
			{
				_placeholder = value;
				SendMessage(this.Handle, EM_SETCUEBANNER, 0, _placeholder);
			}
		}
		private string _placeholder;

		[DefaultValue(false)]
		[Browsable(true)]
		public override bool AutoSize
		{
			get { return base.AutoSize; }
			set { base.AutoSize = value; }
		}

		public event EventHandler EnterPressed;

		public class BeforeUndoEventArgs : EventArgs
		{
			public UndoState State { get; set; }
			public bool Handled { get; set; }
		}
		public delegate void BeforeUndoState(BeforeUndoEventArgs args);
		public event BeforeUndoState OnBeforeUndoState;

		// Undo
		public struct UndoState
		{
			public struct State
			{
				public string text;
				public int start;
				public int length;
			}
			public State before;
			public State after;

			public UndoState(State before, State after)
			{
				this.before = before;
				this.after = after;
			}
		}
		private TextUndoStack<UndoState> _undo;
		private UndoState.State[] stateHistory = new UndoState.State[2] { new UndoState.State(), new UndoState.State() };
		private bool _bIgnoreEvents = false;

		public TextBoxEx() : base()
		{
			TextChanged += OnTextChanged;

			_undo = new TextUndoStack<UndoState>(this);
		}

		private void OnTextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			StoreState();

			_undo.PushState(new UndoState(stateHistory[0], stateHistory[1]));
		}

		public void SetText(string text)
		{
			SetState(text, 0, 0);
		}

		public void SetState(string text, int selectionStart, int selectionLength = 0)
		{
			if (_bIgnoreEvents == false)
			{
				_bIgnoreEvents = true;
				this.Text = text;
				this.Select(selectionStart, selectionLength);
				_bIgnoreEvents = false;
			}
			else
			{
				this.Text = text;
				this.Select(selectionStart, selectionLength);
			}
			StoreState();
		}

		private void StoreState()
		{
			stateHistory[0] = stateHistory[1];
			stateHistory[1] = new UndoState.State() {
				text = this.Text,
				start = this.SelectionStart,
				length = this.SelectionLength,
			};
		}

		public void OnUndoState(UndoState state, bool redo = false)
		{
			// Call handlers
			if (OnBeforeUndoState != null)
			{
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

			_bIgnoreEvents = true;
			if (redo)
				SetState(state.after.text, state.after.start, state.after.length);
			else
				SetState(state.before.text, state.before.start, state.before.length);
			_bIgnoreEvents = false;
			Refresh(); // Repaint
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == ShortcutKeys.Undo)
			{
				_undo.Undo();
				return true;
			}
			else if (keyData == ShortcutKeys.Redo)
			{
				_undo.Redo();
				return true;
			}
			else if (keyData == ShortcutKeys.EraseWord)
			{
				EraseWordBackward();
				return true;
			}
			else if (keyData == ShortcutKeys.DeleteWord)
			{
				EraseWordForward();
				return true;
			}
			else if (keyData == ShortcutKeys.AutoReplace)
			{
				AutoReplaceWord(ReplaceWord.Option.Default);
				return true;
			}
			else if (keyData == ShortcutKeys.AutoReplaceUser)
			{
				AutoReplaceWord(ReplaceWord.Option.User);
				return true;
			}
			else if (keyData == ShortcutKeys.AutoReplaceErase)
			{
				AutoReplaceWord(ReplaceWord.Option.Erase);
				return true;
			}
			else if (keyData == Keys.Return)
			{
				if (!Multiline)
				{
					EnterPressed?.Invoke(this, EventArgs.Empty);
					return true;
				}
			}
			else
			{
				if (!_bIgnoreEvents)
					StoreState();
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		public void InitUndo()
		{
			_undo.Clear();
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
					if (char.IsLetterOrDigit(Text[i]))
						continue;
					pos = i + 1;
					break;
				}
			}

			string beforeText = Text;
			int beforePos = SelectionStart;
			int beforeLength = SelectionLength;

			_bIgnoreEvents = true;
			SetState(Text.Remove(pos, pos_end - pos), pos);
			_bIgnoreEvents = false;

			_undo.PushState(new UndoState(
				new UndoState.State() {
					text = beforeText,
					start = beforePos,
					length = beforeLength,
				},
				new UndoState.State() {
					text = Text,
					start = SelectionStart,
					length = SelectionLength,
				}));
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
					if (!char.IsLetterOrDigit(Text[pos]))
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

			_bIgnoreEvents = true;
			if (pos >= Text.Length)
				SetState(Text.Remove(pos_start), pos_start);
			else
				SetState(Text.Remove(pos_start, pos - pos_start), pos_start);
			_bIgnoreEvents = false;

			_undo.PushState(new UndoState(
				new UndoState.State() {
					text = beforeText,
					start = beforePos,
					length = beforeLength,
				},
				new UndoState.State() {
					text = Text,
					start = SelectionStart,
					length = SelectionLength,
				}));
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (this.Multiline == false && e.KeyCode == Keys.Enter) // Suppress 'ding'
			{
				e.Handled = true;
				e.SuppressKeyPress = true;
				return;
			}
			base.OnKeyDown(e);
		}

		private void AutoReplaceWord(ReplaceWord.Option option)
		{
			int startPos;
			int length;
			string replacement;
			if (ReplaceWord.FindReplacement(this, option, out startPos, out length, out replacement) == false)
				return;

			string beforeText = Text;
			int beforePos = SelectionStart;
			int beforeLength = SelectionLength;

			_bIgnoreEvents = true;
			this.Suspend();
			StringBuilder sb = new StringBuilder(Text);
			sb.Remove(startPos, length);
			sb.Insert(startPos, replacement);
			SetState(sb.ToString(), startPos + replacement.Length);
			this.Resume();

			_bIgnoreEvents = false;
			_undo.PushState(new UndoState(
				new UndoState.State() {
					text = beforeText,
					start = beforePos,
					length = beforeLength,
				},
				new UndoState.State() {
					text = Text,
					start = SelectionStart,
					length = SelectionLength,
				}));
		}

		public int Find(string match, bool matchCase, bool matchWord, bool reverse, int startIndex = -1)
		{
			return ControlExtensions.Find(this, match, matchCase, matchWord, reverse, startIndex);
		}

		public TextBoxBase SearchableControl { get { return this; } }

		public void FocusAndSelect(int start, int length)
		{
			Focus();
			Select(start, length);
		}
	}
}
