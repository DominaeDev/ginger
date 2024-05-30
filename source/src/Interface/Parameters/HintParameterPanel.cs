using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public partial class HintParameterPanel : HintParameterPanelDummy
	{
		protected override CheckBox parameterCheckBox { get { return null; } }
		protected override Label parameterLabel { get { return null; } }

		public HintParameterPanel()
		{
			InitializeComponent();
			
			TabStop = false;
			FontChanged += FontDidChange;
		}

		private void FontDidChange(object sender, System.EventArgs e)
		{
			WhileIgnoringEvents(() => {
				label.Font = new Font(label.Font.Name, 8.5f);
			});
		}

		protected override void OnSetParameter()
		{
			// Text box
			label.Text = Utility.EscapeMenu(parameter.value);
			label.AutoSize = true;
			Size size = TextRenderer.MeasureText(label.Text, label.Font, new Size(tableLayoutPanel.Size.Width, 0), TextFormatFlags.WordBreak);
			tableLayoutPanel.Size = new Size(tableLayoutPanel.Size.Width, size.Height + 4);
		}

		protected override void OnRefreshValue()
		{
			// Do nothing
		}

		private void OnMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			ParameterPanel_MouseClick(sender, e);
		}

		public override int GetParameterHeight()
		{
			return tableLayoutPanel.Size.Height;
		}
	}
}
