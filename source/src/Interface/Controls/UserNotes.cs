using System.Windows.Forms;

namespace Ginger.Interface.Controls
{
	public partial class UserNotes : UserControl
	{
		public new string Text
		{
			get { return richTextBox.Text; }
			set 
			{
				_bIgnoreEvents = true;
				richTextBox.Text = value;
				_bIgnoreEvents = false;
			}
		}

		private bool _bIgnoreEvents = false;

		private RichTextBoxEx richTextBox { get { return textBox.richTextBox; } }

		public UserNotes()
		{
			InitializeComponent();

			richTextBox.TextChanged += TextBox_TextChanged;
			textBox.HighlightBorder = false;

			Load += UserNotes_Load;
		}

		private void UserNotes_Load(object sender, System.EventArgs e)
		{
			richTextBox.BackColor = System.Drawing.Color.FromArgb(255, 255, 248);
			richTextBox.AcceptsTab = true;
			richTextBox.AutoWordSelection = false;
			richTextBox.HideSelection = false;
			richTextBox.WordWrap = true;
			richTextBox.ScrollBars = RichTextBoxScrollBars.ForcedVertical;
			richTextBox.syntaxFlags = RichTextBoxEx.SyntaxFlags.None;
		}

		private void TextBox_TextChanged(object sender, System.EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			Current.IsFileDirty = true;
		}

		public void Clear()
		{
			_bIgnoreEvents = true;
			Text = "";
			_bIgnoreEvents = false;
		}
	}
}
