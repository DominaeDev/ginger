using System;
using System.Globalization;
using System.Windows.Forms;

namespace Ginger
{
	public partial class SliderParameterPanel : SliderParameterPanelDummy
	{
		protected override CheckBox parameterCheckBox { get { return cbEnabled; } }
		protected override Label parameterLabel { get { return label; } }

		private RangeParameter.Mode mode = RangeParameter.Mode.Integer;
		public string suffix = null;

		public SliderParameterPanel()
		{
			InitializeComponent();

			textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
			textBox.TextChanged += OnTextChanged;
			textBox.LostFocus += OnValidate;
			textBox.GotFocus += OnFocus;
		}

		protected override void OnSetParameter()
		{
			mode = parameter.mode;

			// Text box
			if (string.IsNullOrEmpty(parameter.suffix) == false)
				suffix = string.Concat(" ", parameter.suffix);
			textBox.Placeholder = parameter.placeholder;
			textBox.Enabled = parameter.isEnabled || !parameter.isOptional;
			textBox.InitUndo();

			// Slider
			var min = Math.Min(parameter.minValue, parameter.maxValue);
			var max = Math.Max(parameter.minValue, parameter.maxValue);
			switch (parameter.mode)
			{
			default:
				slider.Minimum = Convert.ToInt32(min);
				slider.Maximum = Convert.ToInt32(max);
				slider.SmallChange = 1;
				slider.LargeChange = 5;
				slider.TickFrequency = Math.Max((int)(max - min) / 10, 1);
				break;
			case RangeParameter.Mode.Decimal:
				slider.Minimum = Convert.ToInt32(min * 10);
				slider.Maximum = Convert.ToInt32(max * 10);
				slider.SmallChange = 1;
				slider.LargeChange = 5;
				slider.TickFrequency = Math.Min(Math.Max((int)((max - min) * 0.5m), 1), (int)(max - min));
				break;
			case RangeParameter.Mode.Percent:
				slider.Minimum = 0;
				slider.Maximum = 100;
				slider.SmallChange = 1;
				slider.LargeChange = 5;
				slider.TickFrequency = 10;
				suffix = "%";
				break;
			}
			slider.Enabled = parameter.isEnabled || !parameter.isOptional;

			SetValue(parameter.value);


			// Enabled checkbox
			cbEnabled.Enabled = parameter.isOptional;
			cbEnabled.Checked = parameter.isEnabled;

			// Tooltip
			SetTooltip(label, textBox);
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

		private void SetValue(decimal value, bool bSuffix = true)
		{
			var min = Math.Min(parameter.minValue, parameter.maxValue);
			var max = Math.Max(parameter.minValue, parameter.maxValue);
			value = Math.Min(Math.Max(value, min), max);

			string sValue;
			if (mode == RangeParameter.Mode.Decimal)
				sValue = value.ToString("0.0", CultureInfo.InvariantCulture);
			else
				sValue = Convert.ToInt32(Math.Floor(value)).ToString();
			if (suffix != null && bSuffix)
				sValue = string.Concat(sValue, suffix);

			textBox.SetState(sValue, 0);

			try
			{
				if (mode == RangeParameter.Mode.Decimal)
					slider.Value = Convert.ToInt32(value * 10);
				else
					slider.Value = Convert.ToInt32(value);
			}
			catch
			{
				slider.Value = slider.Minimum;
			}

			textBox.InitUndo();
		}

		private void SetValueSilent(decimal value, bool bSuffix = true)
		{
			var min = Math.Min(parameter.minValue, parameter.maxValue);
			var max = Math.Max(parameter.minValue, parameter.maxValue);
			value = Math.Min(Math.Max(value, min), max);

			string sValue;
			if (mode == RangeParameter.Mode.Decimal)
				sValue = value.ToString("0.0", CultureInfo.InvariantCulture);
			else
				sValue = Convert.ToInt32(Math.Floor(value)).ToString();
			if (suffix != null && bSuffix)
				sValue = string.Concat(sValue, suffix);

			textBox.SetState(sValue, 0);
			textBox.InitUndo();

			try
			{
				if (mode == RangeParameter.Mode.Decimal)
					slider.Value = Convert.ToInt32(value * 10);
				else
					slider.Value = Convert.ToInt32(value);
			}
			catch
			{
				slider.Value = slider.Minimum;
			}

		}

		private void OnFocus(object sender, EventArgs e)
		{
			if (suffix != null && textBox.Text.EndsWith(suffix))
			{
				textBox.Text = textBox.Text.Substring(0, textBox.Text.Length - suffix.Length);
				textBox.SelectAll();
			}
		}

		private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == 0x7f) // Ctrl+Backspace
			{
				e.Handled = true;
				return;
			}

			if (char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar) || e.KeyChar == '-'
				|| (mode == RangeParameter.Mode.Decimal && e.KeyChar == '.')
				|| (mode == RangeParameter.Mode.Percent && e.KeyChar == '%'))
			{
				if (e.KeyChar == '.' && textBox.Text.IndexOf('.') != -1)
				{
					e.Handled = true; // Allow only one point
					return;
				}
				if (e.KeyChar == '%' && textBox.Text.IndexOf('%') != -1)
				{
					e.Handled = true; // Allow only one percent sign
					return;
				}

				e.Handled = false; //Do not reject the input
			}
			else
			{
				e.Handled = true; //Reject the input
			}
		}

		private void OnValidate(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(textBox.Text))
				return;

			string value = textBox.Text;
			if (suffix != null && value.EndsWith(suffix))
				value = value.Substring(0, value.Length - suffix.Length);

			decimal number;
			if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
				SetValue(number);
			else
				textBox.Text = string.Empty;
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
			if (e.KeyData == Keys.Up || e.KeyData == Keys.Down)
			{
				string value = textBox.Text;
				if (suffix != null && value.EndsWith(suffix))
					value = value.Substring(0, value.Length - suffix.Length);

				decimal number;
				if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) == false)
					number = 0;

				if (e.KeyData == Keys.Up)
					SetValue(number + 1, false);
				else if (e.KeyData == Keys.Down)
					SetValue(number - 1, false);

				e.IsInputKey = true;
			}
		}

		private void OnTextChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			string value = textBox.Text;
			if (suffix != null && value.EndsWith(suffix))
				value = value.Substring(0, value.Length - suffix.Length);

			decimal number;
			decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
			var min = Math.Min(parameter.minValue, parameter.maxValue);
			var max = Math.Max(parameter.minValue, parameter.maxValue);
			number = Math.Min(Math.Max(number, min), max);

			this.parameter.value = number;

			WhileIgnoringEvents(() => {
				if (mode == RangeParameter.Mode.Decimal)
					slider.Value = Convert.ToInt32(number * 10);
				else
					slider.Value = Convert.ToInt32(number);
			});

			NotifyValueChanged();
		}

		protected override void OnRefreshValue()
		{
			if (parameter.value == default(decimal) && parameter.isOptional && parameter.GetDefaultValue() == default(decimal))
				textBox.SetState("", 0);
			else
				SetValueSilent(parameter.value);
			cbEnabled.Checked = this.parameter.isEnabled;
		}

		private void slider_ValueChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			if (mode == RangeParameter.Mode.Decimal)
				this.parameter.value = slider.Value * 0.1m;
			else
				this.parameter.value = slider.Value;

			WhileIgnoringEvents(() => {
				if (mode == RangeParameter.Mode.Decimal)
					textBox.Text = (slider.Value * 0.1m).ToString("0.0", CultureInfo.InvariantCulture);
				else
					textBox.Text = slider.Value.ToString();

				if (suffix != null)
					textBox.Text = string.Concat(textBox.Text, suffix);
			});

			NotifyValueChanged();
		}

		private void OnMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			ParameterPanel_MouseClick(sender, e);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			SizeLabel(label);
			SizeToWidth(valuePanel);
		}
	}
}
