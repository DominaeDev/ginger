using System;
using System.Globalization;
using System.Windows.Forms;

namespace Ginger
{
	public partial class NumberParameterPanel : NumberParameterPanelDummy, ISearchableContainer, ISyntaxHighlighted
	{
		protected override CheckBox parameterCheckBox { get { return cbEnabled; } }
		protected override Label parameterLabel { get { return label; } }

		public string Suffix = null;
		private NumberParameter.Mode mode = NumberParameter.Mode.Decimal;
		private int _contentHash;

		public NumberParameterPanel()
		{
			InitializeComponent();

			textBox.richTextBox.syntaxFlags = RichTextBoxEx.SyntaxFlags.Numbers;
			textBox.richTextBox.PreviewKeyDown += TextBox_PreviewKeyDown;
			textBox.richTextBox.EnterPressed += TextBox_EnterPressed;
			textBox.richTextBox.ValueChanged += OnValueChanged;
			textBox.richTextBox.KeyPress += TextBox_KeyPress;
			textBox.richTextBox.GotFocus += TextBox_GotFocus;
			textBox.richTextBox.LostFocus += TextBox_LostFocus;
			textBox.richTextBox.OnBeforeUndoState += TextBox_OnBeforeUndoState;

			FontChanged += FontDidChange;

			SizeToWidth(textBox, 140);
		}

		protected void FontDidChange(object sender, EventArgs e)
		{
			WhileIgnoringEvents(() => {
				textBox.Font = this.Font;
			});
		}

		private void TextBox_OnBeforeUndoState(RichTextBoxEx.BeforeUndoEventArgs args)
		{
			if (string.IsNullOrEmpty(Suffix))
				return;

			// Remove suffix from undo state
			if (args.State.before.text.EndsWith(string.Concat(" ", Suffix)))
				args.State = new RichTextBoxEx.UndoState() {
					before = new RichTextBoxEx.UndoState.Content() {
						text = args.State.before.text.Substring(0, args.State.before.text.Length - (Suffix.Length + 1)),
						start = Math.Min(args.State.before.start, args.State.before.text.Length - (Suffix.Length + 1)),
						length = 0,
					},
					after = args.State.after,
				};
			if (args.State.after.text.EndsWith(string.Concat(" ", Suffix)))
				args.State = new RichTextBoxEx.UndoState() {
					before = args.State.before,
					after = new RichTextBoxEx.UndoState.Content() {
						text = args.State.after.text.Substring(0, args.State.after.text.Length - (Suffix.Length + 1)),
						start = Math.Min(args.State.after.start, args.State.after.text.Length - (Suffix.Length + 1)),
						length = 0,
					},
				};
			args.Handled = true;
		}

		protected override void OnSetParameter()
		{
			mode = parameter.mode;

			// Text box
			Suffix = parameter.suffix;
//			if (parameter.value != default(decimal))
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
			if (parameter.value == default(decimal) && parameter.isOptional && parameter.GetDefaultValue() == default(decimal))
				textBox.SetTextSilent("");
			else
				SetValueSilent(parameter.value);

			cbEnabled.Checked = this.parameter.isEnabled;
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
				decimal number;
				if (bReserved)
					decimal.TryParse(reservedValue, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
				else
					number = parameter.value;

				if (parameter.value == default(decimal) && parameter.isOptional && parameter.GetDefaultValue() == default(decimal))
				{
					textBox.Text = "";
					textBox.InitUndo();
				}
				else
					SetValue(number);
			});
		}

		private void SetValue(decimal value, bool bSuffix = true)
		{
			var min = Math.Min(parameter.minValue, parameter.maxValue);
			var max = Math.Max(parameter.minValue, parameter.maxValue);
			value = Math.Min(Math.Max(value, min), max);

			if (mode == NumberParameter.Mode.Decimal)
			{
				textBox.Text = value.ToString("g", CultureInfo.InvariantCulture);
				if (string.IsNullOrEmpty(Suffix) == false && bSuffix)
					textBox.Text = string.Concat(textBox.Text, " ", Suffix);
			}
			else
			{
				textBox.Text = Convert.ToInt32(Math.Floor(value)).ToString();
				if (string.IsNullOrEmpty(Suffix) == false && bSuffix)
					textBox.Text = string.Concat(textBox.Text, " ", Suffix);
			}
			textBox.InitUndo();
		}

		private void SetValueSilent(decimal value, bool bSuffix = true)
		{
			var min = Math.Min(parameter.minValue, parameter.maxValue);
			var max = Math.Max(parameter.minValue, parameter.maxValue);
			value = Math.Min(Math.Max(value, min), max);

			if (mode == NumberParameter.Mode.Decimal)
			{
				string sValue = value.ToString("g", CultureInfo.InvariantCulture);
				if (string.IsNullOrEmpty(Suffix) == false && bSuffix)
					sValue = string.Concat(sValue, " ", Suffix);
				textBox.SetTextSilent(sValue);
			}
			else
			{
				string sValue = Convert.ToInt32(Math.Floor(value)).ToString();
				if (string.IsNullOrEmpty(Suffix) == false && bSuffix)
					sValue = string.Concat(sValue, " ", Suffix);
				textBox.SetTextSilent(sValue);
			}
			textBox.InitUndo();
		}

		private void TextBox_EnterPressed(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			TextBox_LostFocus(this, EventArgs.Empty);
			textBox.SelectAll();
			NotifyValueChanged();
		}

		private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == 0x7f) // Ctrl+Backspace
			{
				e.Handled = true;
				return;
			}

			if (char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar) || e.KeyChar == '-'
				|| (mode == NumberParameter.Mode.Decimal && e.KeyChar == '.'))
			{
				if (e.KeyChar == '.' && textBox.Text.IndexOf('.') != -1)
				{
					e.Handled = true; // Allow only one point
					return;
				}

				e.Handled = false; // Do not reject the input
			}
			else
			{
				e.Handled = true; // Reject the input
			}
		}

		private void TextBox_GotFocus(object sender, EventArgs e)
		{
			if (isIgnoringEvents || !Enabled)
				return;

			_contentHash = parameter.value.GetHashCode();

			if (textBox.Text.EndsWith(string.Concat(" ", Suffix)))
			{
				textBox.Text = textBox.Text.Substring(0, textBox.Text.Length - (Suffix.Length + 1));
				textBox.SelectAll();
			}
		}

		private void TextBox_LostFocus(object sender, EventArgs e)
		{
			if (isIgnoringEvents || !Enabled)
				return;

			// Validate
			if (string.IsNullOrEmpty(textBox.Text) == false)
			{
				string value = textBox.Text;
				if (value.EndsWith(string.Concat(" ", Suffix)))
					value = value.Substring(0, value.Length - (Suffix.Length + 1));

				decimal number;
				if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
					SetValue(number);
				else
					textBox.Text = string.Empty;
			}
			
			var newContentHash = parameter.value.GetHashCode();
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

		private void TextBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			// Step up and down
			if (e.KeyData == Keys.Up || e.KeyData == Keys.Down)
			{
				string value = textBox.Text;
				if (value.EndsWith(string.Concat(" ", Suffix)))
					value = value.Substring(0, value.Length - (Suffix.Length + 1));

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

		private void OnValueChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents || Enabled == false)
				return;

			string value = textBox.Text;
			if (value.EndsWith(string.Concat(" ", Suffix)))
				value = value.Substring(0, value.Length - (Suffix.Length + 1));

			decimal number;
			decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number);

			this.parameter.value = number;
			Current.IsFileDirty = true;
		}

		public ISearchable[] GetSearchables()
		{
			return new ISearchable[] { textBox.richTextBox };
		}

		private void OnMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
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
