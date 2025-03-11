using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Ginger
{
	public partial class ChoiceParameterPanel : ChoiceParameterPanelDummy, ISearchableContainer
	{
		protected override CheckBox parameterCheckBox { get { return cbEnabled; } }
		protected override Label parameterLabel { get { return label; } }

		protected virtual List<ChoiceParameter.Item> ChoiceItems { get { return this.parameter.items; } }

		private int _contentHash;

		public ChoiceParameterPanel()
		{
			InitializeComponent();

			comboBox.SelectedValueChanged += OnValueChanged;
			textBox_Custom.richTextBox.ValueChanged += TextBox_Custom_TextChanged;
			textBox_Custom.richTextBox.EnterPressed += TextBox_Custom_EnterPressed;
			textBox_Custom.richTextBox.GotFocus += TextBox_Custom_GotFocus;
			textBox_Custom.LostFocus += TextBox_Custom_LostFocus;

			FontChanged += FontDidChange;
		}
		
		protected void FontDidChange(object sender, EventArgs e)
		{
			WhileIgnoringEvents(() => {
				textBox_Custom.Font = this.Font;
			});
		}

		private void TextBox_Custom_LostFocus(object sender, EventArgs e)
		{
			if (isIgnoringEvents || !Enabled)
				return;

			var newContentHash = textBox_Custom.Text.GetHashCode();
			if (_contentHash != newContentHash)
			{
				Current.IsFileDirty = this.parameter.value != textBox_Custom.Text;
				this.parameter.value = textBox_Custom.Text;

				_contentHash = newContentHash;
				NotifyValueChanged(_contentHash);
			}
		}

		private void TextBox_Custom_GotFocus(object sender, EventArgs e)
		{
			_contentHash = textBox_Custom.Text.GetHashCode();
		}

		private void TextBox_Custom_EnterPressed(object sender, EventArgs e)
		{
			if (isIgnoringEvents || Enabled == false)
				return;

			this.parameter.value = textBox_Custom.Text;
			var newContentHash = textBox_Custom.Text.GetHashCode();
			if (_contentHash != newContentHash)
			{
				_contentHash = newContentHash;
				NotifyValueChanged(_contentHash);
			}

			textBox_Custom.richTextBox.SelectAll();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			SizeLabel(label);
			SizeToWidth(valuePanel);
		}

		protected override void OnSetParameter()
		{
			comboBox.BeginUpdate();
			comboBox.Items.Clear();
			// Drop down
			if (parameter.isOptional)
				comboBox.Items.Add("\u2014"); // Empty
			foreach (var item in ChoiceItems)
				comboBox.Items.Add(item.label);
			if (parameter.style == ChoiceParameter.Style.Custom)
				comboBox.Items.Add("(Other)");

			SelectByValue(parameter.value);
			SetSelectedIndex();
			comboBox.EndUpdate();

			comboBox.Enabled = parameter.isEnabled || !parameter.isOptional;

			textBox_Custom.Visible = parameter.style == ChoiceParameter.Style.Custom && comboBox.SelectedIndex == comboBox.Items.Count - 1 && !isReserved;
			textBox_Custom.Enabled = parameter.isEnabled || !parameter.isOptional;
			textBox_Custom.Placeholder = parameter.placeholder;

			SetTooltip(label, comboBox);
		}

		protected void SetSelectedIndex()
		{
			int index = comboBox.SelectedIndex;
			if (index <= -1 && parameter.isOptional)
			{
				this.parameter.selectedIndex = -1; // Not set
			}
			else if (parameter.style == ChoiceParameter.Style.Custom && index >= comboBox.Items.Count - 1)
			{
				this.parameter.selectedIndex = -2; // Custom
			}
			else
			{
				if (parameter.isOptional)
					index -= 1; // (not set)

				if (index >= 0 && index < comboBox.Items.Count)
					this.parameter.selectedIndex = index;
				else
					this.parameter.selectedIndex = -1;
			}
		}

		protected override void OnRefreshValue()
		{
			cbEnabled.Checked = this.parameter.isEnabled;

			SelectByValue(this.parameter.value);
		}

		protected override void OnSetEnabled(bool bEnabled)
		{
			cbEnabled.Enabled = bEnabled && parameter.isOptional;
			comboBox.Enabled = bEnabled && parameter.isEnabled;
			textBox_Custom.Enabled = bEnabled && parameter.isEnabled;
		}

		protected override void OnSetReserved(bool bReserved, string reservedValue)
		{
			if (isNotifyingValueChanged)
				return;

			cbEnabled.Enabled = !bReserved && parameter.isOptional;
			comboBox.Enabled = !bReserved && parameter.isEnabled;
			textBox_Custom.Enabled = !bReserved && parameter.isEnabled;

			SelectByValueReserved(bReserved ? reservedValue : null);
		}

		private void CbEnabled_CheckedChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			comboBox.Enabled = cbEnabled.Checked || !parameter.isOptional;
			textBox_Custom.Enabled = cbEnabled.Checked || !parameter.isOptional;
			if (isIgnoringEvents)
				return;
			parameter.isEnabled = cbEnabled.Checked || !parameter.isOptional;
			NotifyEnabledChanged();
		}

		protected virtual void OnValueChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			int index = comboBox.SelectedIndex;
			if (index <= -1 && parameter.isOptional)
			{
				this.parameter.value = "";
				this.parameter.selectedIndex = -1; // Not set
			}
			else if (parameter.style == ChoiceParameter.Style.Custom && index >= comboBox.Items.Count - 1)
			{
				this.parameter.value = textBox_Custom.Text;
				this.parameter.selectedIndex = -2; // Custom
				textBox_Custom.Visible = true;
				NotifyValueChanged();
				return;
			}
			else
			{
				if (parameter.isOptional)
					index -= 1; // (not set)

				if (index >= 0 && index < comboBox.Items.Count)
				{
					this.parameter.value = ChoiceItems[index].value;
					this.parameter.selectedIndex = index;
				}
				else
				{
					this.parameter.value = "";
					this.parameter.selectedIndex = -1;
				}
			}

			textBox_Custom.Visible = false;
			NotifyValueChanged();
		}

		private void TextBox_Custom_TextChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents || parameter.style != ChoiceParameter.Style.Custom || comboBox.SelectedIndex < comboBox.Items.Count - 1)
				return;

			this.parameter.value = textBox_Custom.Text;
			Current.IsFileDirty = true;

			/*if (parameter.style == ChoiceParameter.Style.Custom && comboBox.SelectedIndex >= comboBox.Items.Count - 1)
			{
				if (this.parameter.value != textBox_Custom.Text)
				{
					this.parameter.value = textBox_Custom.Text;
					this.parameter.selectedIndex = -2;
					NotifyValueChanged();
				}
			}*/
		}

		private void SelectByValue(string value)
		{
			if (comboBox.Items.Count == 0)
				return;

			WhileIgnoringEvents(() => {
				int index = ChoiceItems.FindIndex(i => string.Compare(i.value, value, true) == 0 || string.Compare(i.label, value, true) == 0);

				if (index == -1 && parameter.style == ChoiceParameter.Style.Custom && string.IsNullOrWhiteSpace(value) == false)
				{
					index = comboBox.Items.Count - 1;
					textBox_Custom.Text = value;
					_contentHash = textBox_Custom.Text.GetHashCode();
				}
				else if (index == -1 && parameter.isOptional)
					index = 0; // (not set)
				else if (parameter.isOptional)
					index += 1; // (not set)...

				if (index < 0 || index >= comboBox.Items.Count)
					index = 0;

				comboBox.SelectedItem = comboBox.Items[index];
			});

			textBox_Custom.Visible = parameter.style == ChoiceParameter.Style.Custom && comboBox.SelectedIndex == comboBox.Items.Count - 1;
		}

		public ISearchable[] GetSearchables()
		{
			return new ISearchable[] { textBox_Custom.richTextBox };
		}

		private void OnMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			ParameterPanel_MouseClick(sender, e);
		}

		private void SelectByValueReserved(string reservedValue)
		{
			if (comboBox.Items.Count == 0)
				return;

			WhileIgnoringEvents(() => {
				if (string.IsNullOrEmpty(reservedValue) == false) // Reserved
				{
					int index = ChoiceItems.FindIndex(i => string.Compare(i.value, reservedValue, true) == 0 || string.Compare(i.label, reservedValue, true) == 0);
					if (index != -1)
					{
						if (parameter.isOptional)
							index += 1; // (not set)
						comboBox.SelectedIndex = index;
						textBox_Custom.Visible = false;
					}
					else if (parameter.style == ChoiceParameter.Style.Custom)
					{
						comboBox.SelectedIndex = comboBox.Items.Count - 1;
						textBox_Custom.Visible = true;
						textBox_Custom.Text = reservedValue;
					}
					else
					{
						comboBox.SelectedIndex = 0;
						textBox_Custom.Visible = false;
					}
				}
				else
				{
					if (parameter.style == ChoiceParameter.Style.Custom)
					{
						if (this.parameter.selectedIndex == -2)
						{
							comboBox.SelectedIndex = comboBox.Items.Count - 1;
							textBox_Custom.Visible = true;
							textBox_Custom.Text = this.parameter.value;
							return;
						}
					}

					SelectByValue(this.parameter.value);
				}
			});
		}
	}
}
