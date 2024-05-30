using System;
using System.Windows.Forms;

namespace Ginger
{
	public partial class ToggleParameterPanel : ToggleParameterPanelDummy
	{
		protected override CheckBox parameterCheckBox { get { return null; } }
		protected override Label parameterLabel { get { return label; } }

		public ToggleParameterPanel()
		{
			InitializeComponent();

			cbToggle.CheckedChanged += CbToggle_CheckedChanged;
		}
		
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			SizeLabel(label);
			SizeToWidth(cbToggle);
		}

		protected override void OnSetParameter()
		{
			// Text box
			cbToggle.Checked = parameter.value;

			// Tooltip
			SetTooltip(label, cbToggle);
		}

		protected override void OnSetEnabled(bool bEnabled)
		{
			cbToggle.Enabled = bEnabled;
		}

		protected override void OnRefreshValue()
		{
			cbToggle.Checked = parameter.value;
		}

		private void CbToggle_CheckedChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents || Enabled == false)
				return;

			this.parameter.value = cbToggle.Checked;
			NotifyValueChanged();
		}

		private void OnMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			ParameterPanel_MouseClick(sender, e);
		}

	}
}
