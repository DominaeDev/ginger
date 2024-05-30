using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	// This control contains a RichTextBoxEx and surrounds it with a visible border
	// Doing it this way may look really dumb, and it is, but it actually avoids a whole
	// host of issues with WinForms' TERRIBLE native RichTextBox.

	public partial class FlatRichTextBox : UserControl
	{
		[Category("Behavior"), Description("Placeholder text")]
		public string Placeholder
		{
			get { return richTextBox.Placeholder; }
			set { richTextBox.Placeholder = value; }
		}		

		[Browsable(true)]
		[Category("Appearance"), Description("Text")]
		public new string Text
		{
			get { return richTextBox.Text; }
			set {
				_bIgnoreEvents = true;
				richTextBox.Text = value;
				_bIgnoreEvents = false;
			}
		}

		[Category("Behavior"), Description("Multiline")]
		public bool Multiline
		{
			get { return richTextBox.Multiline; }
			set { richTextBox.Multiline = value; }
		}

		[Browsable(true)]
		[Category("Behavior"), Description("Spell checking")]
		public bool SpellChecking
		{
			get { return richTextBox.SpellChecking; }
			set { richTextBox.SpellChecking = value; }
		}

		[Browsable(true)]
		[Category("Behavior"), Description("Syntax highlighting")]
		public bool SyntaxHighlighting
		{
			get { return richTextBox.SyntaxHighlighting; }
			set { richTextBox.SyntaxHighlighting = value; }
		}

		[Browsable(true)]
		[Category("Appearance")]
		[DefaultValue(typeof(Color), "WindowFrame")]
		public Color BorderColor = SystemColors.WindowFrame;

		public int SelectionStart
		{
			get { return richTextBox.SelectionStart; }
			set { richTextBox.SelectionStart = value; }
		}

		public int SelectionLength
		{
			get { return richTextBox.SelectionLength; }
			set { richTextBox.SelectionLength = value; }
		}

		public bool HighlightBorder = true;
		private bool _bIgnoreEvents = false;

		public new event EventHandler TextChanged;
		public event EventHandler TextSizeChanged;

		[Browsable(false)]
		public Size TextSize { get; set; }

		public FlatRichTextBox()
		{
			InitializeComponent();

			richTextBox.VScroll += RichTextBox_VScroll;
			richTextBox.TextChanged += RichTextBox_TextChanged;
			richTextBox.ContentsResized += RichTextBox_ContentsResized;
			
			Resize += FlatRichTextBox_Resize;
			FontChanged += FlatRichTextBox_FontChanged;
			Load += FlatRichTextBox_Load;
			VisibleChanged += FlatRichTextBox_VisibleChanged;
			EnabledChanged += FlatRichTextBox_EnabledChanged;

			DoubleBuffered = true;
		}

		private void FlatRichTextBox_Load(object sender, EventArgs e)
		{
			richTextBox.GotFocus += (s, x) => {
				Invalidate(); // Eliminates flicker
				MainForm.EnableFormLevelDoubleBuffering(false);
			};
			richTextBox.LostFocus += (s, x) => {
				Invalidate(); // Eliminates flicker
				MainForm.EnableFormLevelDoubleBuffering(true);
			};
			richTextBox.Font = this.Font;
		}

		protected void RichTextBox_ContentsResized(object sender, ContentsResizedEventArgs e)
		{
			if (_bIgnoreEvents || (richTextBox.syntaxHighlighter != null && richTextBox.syntaxHighlighter.isHighlighting))
				return;

			TextSize = new Size(e.NewRectangle.Width, e.NewRectangle.Height);
			TextSizeChanged?.Invoke(this, EventArgs.Empty);
			richTextBox.RefreshScrollbar(e.NewRectangle.Height);
		}

		private void RichTextBox_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents || (richTextBox.syntaxHighlighter != null && richTextBox.syntaxHighlighter.isHighlighting))
				return;

			TextChanged?.Invoke(sender, e);
		}

		private void FlatRichTextBox_FontChanged(object sender, EventArgs e)
		{
			_bIgnoreEvents = true;
			richTextBox.Font = this.Font;
			_bIgnoreEvents = false;
		}

		private void FlatRichTextBox_Resize(object sender, EventArgs e)
		{
			SetInnerMargins();
		}

		private void FlatRichTextBox_VisibleChanged(object sender, EventArgs e)
		{
			SetInnerMargins();
			richTextBox.SetTabWidth(4);
		}

		private void SetInnerMargins()
		{
			if (richTextBox.Multiline)
			{
				richTextBox.Location = new Point(2, 2);
				richTextBox.SetInnerMargins(3, 2, 3, 0);
				richTextBox.Size = new Size(this.Width - 4, this.Height - 4);
			}
			else
			{
				richTextBox.Location = new Point(5, 4);
				richTextBox.SetInnerMargins(0, 0, 0, 0);
				richTextBox.Size = new Size(this.Width - 7, this.Height - 6);
			}
		}

		private static Brush s_SolidWhite = new SolidBrush(SystemColors.Window);
		private static Brush s_SolidGray = new SolidBrush(SystemColors.Control);

		protected override void OnPaint(PaintEventArgs e)
		{
			// BG
			if (Enabled)
				e.Graphics.FillRectangle(s_SolidWhite, new Rectangle(0, 0, Width - 1, Height - 1));
			else
			{
				e.Graphics.FillRectangle(s_SolidWhite, new Rectangle(0, 0, Width - 1, Height - 1));
				e.Graphics.FillRectangle(s_SolidGray, new Rectangle(2, 2, Width - 4, Height - 4));
			}

			// Border
			e.Graphics.DrawRectangle(new Pen(BorderColor), new Rectangle(0, 0, Width - 1, Height - 1));
		}

		private void TextBox_Enter(object sender, EventArgs e)
		{
			if (HighlightBorder)
				BorderColor = SystemColors.Highlight;
		}

		private void TextBox_Leave(object sender, EventArgs e)
		{
			if (HighlightBorder)
				BorderColor = SystemColors.WindowFrame;
		}

		private void FlatRichTextBox_EnabledChanged(object sender, EventArgs e)
		{
			if (HighlightBorder)
				BorderColor = SystemColors.WindowFrame;
		}

		private void RichTextBox_VScroll(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			Invalidate();
			
			if (Control.MouseButtons == MouseButtons.Left && richTextBox.GetScrollPos() != 0) // Dragging scrollbar
				MainForm.EnableFormLevelDoubleBuffering(false);
		}

		public void InitUndo()
		{
			richTextBox.InitUndo();
		}

		public void Select(int start, int length = 0)
		{
			richTextBox.Select(start, length);
		}

		public void SelectAll()
		{
			richTextBox.SelectAll();
		}

		public void AppendText(string text)
		{
			richTextBox.AppendText(text);
		}

		public void SetTextSilent(string text)
		{
			_bIgnoreEvents = true;
			richTextBox.SetTextSilent(text);
			_bIgnoreEvents = false;
		}
	}
}
