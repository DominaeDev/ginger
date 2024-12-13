using System;
using System.Windows.Forms;

namespace Ginger
{
	public partial class ChoiceSliderParameterPanel : ChoiceSliderParameterPanelDummy
	{
		protected override CheckBox parameterCheckBox { get { return cbEnabled; } }
		protected override Label parameterLabel { get { return label; } }

		public ChoiceSliderParameterPanel()
		{
			InitializeComponent();

			textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			SizeLabel(label);
			SizeToWidth(valuePanel);
		}

		protected override void OnSetParameter()
		{
			// Text box
			textBox.Enabled = parameter.isEnabled || !parameter.isOptional;

			// Slider
			slider.SmallChange = 1;
			slider.LargeChange = 1;
			slider.TickFrequency = 1;
			slider.Minimum = 0;
			slider.Maximum = parameter.items.Count - 1;
			slider.Enabled = parameter.isEnabled || !parameter.isOptional;

			SelectByValue(parameter.value);

			// Tooltip
			SetTooltip(label, textBox);
		}

		protected override void OnRefreshValue()
		{
			cbEnabled.Checked = this.parameter.isEnabled;

			SelectByValue(this.parameter.value);
		}

		protected override void OnSetEnabled(bool bEnabled)
		{
			cbEnabled.Enabled = bEnabled && parameter.isOptional;
			textBox.Enabled = bEnabled && parameter.isEnabled;
			slider.Enabled = bEnabled && parameter.isEnabled;
		}

		protected override void OnSetReserved(bool bReserved)
		{
			cbEnabled.Enabled = !bReserved && parameter.isOptional;
			textBox.Enabled = !bReserved && parameter.isEnabled;
			slider.Enabled = !bReserved && parameter.isEnabled;
		}

		private void SelectByValue(string value)
		{
			int count = this.parameter.items.Count;
			if (count == 0)
				return;

			int index = this.parameter.items.FindIndex(i => i.value == value || string.Compare(i.label, value, true) == 0);
			index = Math.Min(Math.Max(index, 0), this.parameter.items.Count);

			textBox.SetState(this.parameter.items[index].label, 0);
			slider.Value = Math.Min(Math.Max(index, slider.Minimum), slider.Maximum);
		}

		private void CbEnabled_CheckedChanged(object sender, EventArgs e)
		{
			textBox.Enabled = cbEnabled.Checked || !parameter.isOptional;
			slider.Enabled = cbEnabled.Checked || !parameter.isOptional;
			if (isIgnoringEvents)
				return;
			parameter.isEnabled = cbEnabled.Checked || !parameter.isOptional;
			NotifyEnabledChanged();
		}

		private void TextBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			// Step up and down
			if (e.KeyData == Keys.Up)
			{
				slider.Value = Math.Min(slider.Value + 1, slider.Maximum);
				e.IsInputKey = true;
			}
			else if (e.KeyData == Keys.Down)
			{
				slider.Value = Math.Max(slider.Value - 1, slider.Minimum);
				e.IsInputKey = true;
			}
		}

		private void slider_ValueChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			int index = Math.Min(Math.Max(slider.Value, 0), this.parameter.items.Count - 1);
			this.parameter.value = parameter.items[index].value;
			this.parameter.selectedIndex = index;
			textBox.Text = this.parameter.items[index].label;

			NotifyValueChanged();
		}

		private void OnMouseClick(object sender, MouseEventArgs e)
		{
			ParameterPanel_MouseClick(sender, e);
		}
	}
}
