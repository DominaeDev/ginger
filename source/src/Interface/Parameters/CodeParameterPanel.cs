using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public partial class CodeParameterPanel : CodeParameterPanelDummy
	{
		protected override CheckBox parameterCheckBox { get { return cbEnabled; } }
		protected override Label parameterLabel { get { return label; } }

		public CodeParameterPanel()
		{
			InitializeComponent();

			Init(label, textBox, cbEnabled, btnWrite);

			richTextBox.syntaxFlags = RichTextBoxEx.SyntaxFlags.Code;
			richTextBox.AcceptsTab = true;

			FontChanged -= base.FontDidChange;
			FontChanged += FontDidChange;
		}

		protected new void FontDidChange(object sender, EventArgs e)
		{
			WhileIgnoringEvents(() => {
				textBox.Font = new Font(FontFamily.GenericMonospace, this.Font.Size);
			});
		}

		protected override void OnSetParameter()
		{
			if (!parameter.isRaw)
			{
				richTextBox.RefreshSyntaxHighlight();
			}

			base.OnSetParameter();
		}

		private void OnMouseClick(object sender, MouseEventArgs e)
		{
			ParameterPanel_MouseClick(sender, e);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			SizeLabel(label);
			if (textBox != null)
				SizeToWidth(textBox);
		}

		public override int GetParameterHeight()
		{
			return textBox.Location.Y + textBox.Height;
		}
	}
}
