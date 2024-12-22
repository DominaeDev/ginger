using System;
using System.Windows.Forms;

namespace Ginger
{
	public partial class MeasurementParameterPanel : MeasurementParameterPanelDummy, ISearchableContainer, ISyntaxHighlighted
	{
		private int _contentHash;

		protected override CheckBox parameterCheckBox { get { return cbEnabled; } }
		protected override Label parameterLabel { get { return label; } }

		public MeasurementParameterPanel()
		{
			InitializeComponent();

			textBox.richTextBox.syntaxFlags = RichTextBoxEx.SyntaxFlags.Numbers;
			textBox.richTextBox.EnterPressed += TextBox_EnterPressed;
			textBox.richTextBox.ValueChanged += OnValueChanged;
			textBox.richTextBox.GotFocus += TextBox_GotFocus;
			textBox.richTextBox.LostFocus += TextBox_LostFocus;

			FontChanged += FontDidChange;

			SizeToWidth(textBox, 140);
		}

		protected void FontDidChange(object sender, EventArgs e)
		{
			WhileIgnoringEvents(() => {
				textBox.Font = this.Font;
			});
		}

		protected override void OnSetParameter()
		{
			// Text box
//			if (parameter.hasValue)
//				SetValue(parameter.value);
//			else
//				textBox.Text = string.Empty;
			textBox.Placeholder = parameter.placeholder;
			textBox.Enabled = parameter.isEnabled || !parameter.isOptional;
			textBox.InitUndo();

			// Enabled checkbox
			cbEnabled.Enabled = parameter.isOptional;
			cbEnabled.Checked = parameter.isEnabled;

			// Tooltip
			SetTooltip(label, textBox);
		}

		protected override void OnRefreshValue()
		{
			if (parameter.hasValue == false && parameter.isOptional && parameter.defaultValue == default(string)) // zero?
				textBox.Text = "";
			else
				SetValueSilent(parameter.value);
			cbEnabled.Checked = this.parameter.isEnabled;
		}

		private void SetValue(string value)
		{
			string unit;
			Measurement.UnitSystem unitSystem;
			decimal magnitude;
			if (Measurement.Parse(value, parameter.mode, out magnitude, out unit, out unitSystem))
			{
				// Clamp
				var min = Math.Min(parameter.minValue, parameter.maxValue);
				var max = Math.Max(parameter.minValue, parameter.maxValue);
				magnitude = Math.Min(Math.Max(magnitude, min), max);

				textBox.Text = Measurement.ToString(magnitude, unit);
			}
			else
				textBox.Text = value;
			textBox.InitUndo();
		}

		private void SetValueSilent(string value)
		{
			string unit;
			Measurement.UnitSystem unitSystem;
			decimal magnitude;
			if (Measurement.Parse(value, parameter.mode, out magnitude, out unit, out unitSystem))
			{
				// Clamp
				var min = Math.Min(parameter.minValue, parameter.maxValue);
				var max = Math.Max(parameter.minValue, parameter.maxValue);
				magnitude = Math.Min(Math.Max(magnitude, min), max);

				textBox.SetTextSilent(Measurement.ToString(magnitude, unit));
			}
			else
				textBox.SetTextSilent(value);
			textBox.InitUndo();
		}

		protected override void OnSetEnabled(bool bEnabled)
		{
			cbEnabled.Enabled = bEnabled && parameter.isOptional;
			textBox.Enabled = bEnabled && parameter.isEnabled;
		}

		protected override void OnSetReserved(bool bReserved, string reservedValue)
		{
			cbEnabled.Enabled = !bReserved && parameter.isOptional;
			textBox.Enabled = !bReserved && parameter.isEnabled;

			WhileIgnoringEvents(() => {
				textBox.Text = bReserved ? reservedValue : parameter.value;
				textBox.InitUndo();
			});
		}

		private void TextBox_GotFocus(object sender, EventArgs e)
		{
			if (isIgnoringEvents || !Enabled)
				return;

			_contentHash = parameter.magnitude.GetHashCode();
		}

		private void TextBox_LostFocus(object sender, EventArgs e)
		{
			if (isIgnoringEvents || !Enabled)
				return;

			if (string.IsNullOrEmpty(textBox.Text) == false)
			{
				string unit;
				Measurement.UnitSystem unitSystem;
				decimal magnitude;
				if (Measurement.Parse(textBox.Text, parameter.mode, out magnitude, out unit, out unitSystem))
				{
					// Clamp
					var min = Math.Min(parameter.minValue, parameter.maxValue);
					var max = Math.Max(parameter.minValue, parameter.maxValue);
					magnitude = Math.Min(Math.Max(magnitude, min), max);

					textBox.Text = Measurement.ToString(magnitude, unit);
				}
				else
				{
					textBox.Text = string.Empty;
				}
			}

			var newContentHash = parameter.magnitude.GetHashCode();
			if (_contentHash != newContentHash)
			{
				_contentHash = newContentHash;
				NotifyValueChanged();
			}
		}

		private void CbEnabled_CheckedChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			textBox.Enabled = cbEnabled.Checked || !parameter.isOptional;
			if (isIgnoringEvents)
				return;

			parameter.isEnabled = cbEnabled.Checked || !parameter.isOptional;
			NotifyEnabledChanged();
		}

		private void TextBox_EnterPressed(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			TextBox_LostFocus(this, EventArgs.Empty);
			textBox.SelectAll();
			NotifyValueChanged();
		}

		private void OnValueChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;
			
			this.parameter.Set(textBox.Text);
			Current.IsFileDirty = true;
		}

		public ISearchable[] GetSearchables()
		{
			return new ISearchable[] { textBox.richTextBox };
		}

		private void OnMouseClick(object sender, MouseEventArgs e)
		{
			ParameterPanel_MouseClick(sender, e);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			SizeLabel(label);
			SizeToWidth(textBox, 140);
		}

		public override int GetParameterHeight()
		{
			return textBox.Location.Y + textBox.Height;
		}

		public void RefreshSyntaxHighlight(bool immediate, bool invalidate)
		{
			if (invalidate)
				textBox.richTextBox.InvalidateSyntaxHighlighting();
			textBox.richTextBox.RefreshSyntaxHighlight(immediate);
		}
	}
}
