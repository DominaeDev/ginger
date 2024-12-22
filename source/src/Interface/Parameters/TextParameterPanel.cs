using System;
using System.Drawing;

namespace Ginger
{
	public partial class TextParameterPanel : TextParameterPanelDummy
	{
		public TextParameterPanel() : base()
		{
			InitializeComponent();

			Init(label, textBox, cbEnabled, null);

			textBox.richTextBox.EnterPressed += TextBox_EnterPressed;
		}

		private void TextBox_EnterPressed(object sender, EventArgs e)
		{
			if (isIgnoringEvents || !Enabled)
				return;

			this.parameter.value = textBox.Text;
			var newContentHash = textBox.Text.GetHashCode();
			if (_contentHash != newContentHash)
			{
				_contentHash = newContentHash;
				NotifyValueChanged(_contentHash);
			}

			textBox.richTextBox.SelectAll();
		}

		private void OnMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			ParameterPanel_MouseClick(sender, e);
		}

		public override int GetParameterHeight()
		{
			return textBox.Location.Y + textBox.Height;
		}
	}
}
