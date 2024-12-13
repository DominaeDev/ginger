using System;
using System.Windows.Forms;

namespace Ginger
{
	public partial class ChoiceParameterPanel : ChoiceParameterPanelDummy, ISearchableContainer
	{
		protected override CheckBox parameterCheckBox { get { return cbEnabled; } }
		protected override Label parameterLabel { get { return label; } }

		public ChoiceParameterPanel()
		{
			InitializeComponent();

			comboBox.SelectedValueChanged += OnValueChanged;
			textBox_Custom.richTextBox.ValueChanged += OnCustomTextChanged;

			FontChanged += FontDidChange;
		}
		
		protected void FontDidChange(object sender, EventArgs e)
		{
			WhileIgnoringEvents(() => {
				textBox_Custom.Font = this.Font;
			});
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
			foreach (var item in parameter.items)
				comboBox.Items.Add(item.label);
			if (parameter.style == ChoiceParameter.Style.Custom)
				comboBox.Items.Add("(Other)");

			SelectByValue(parameter.value);
			SetSelectedIndex();
			comboBox.EndUpdate();

			comboBox.Enabled = parameter.isEnabled || !parameter.isOptional;

			textBox_Custom.Visible = parameter.style == ChoiceParameter.Style.Custom && comboBox.SelectedIndex == comboBox.Items.Count - 1;
			textBox_Custom.Enabled = parameter.isEnabled || !parameter.isOptional;
			textBox_Custom.Placeholder = parameter.placeholder;

			SetTooltip(label, comboBox);
		}

		private void SetSelectedIndex()
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

		protected override void OnSetReserved(bool bReserved)
		{
			cbEnabled.Enabled = !bReserved && parameter.isOptional;
			comboBox.Enabled = !bReserved && parameter.isEnabled;
			textBox_Custom.Enabled = !bReserved && parameter.isEnabled;
		}

		private void CbEnabled_CheckedChanged(object sender, EventArgs e)
		{
			comboBox.Enabled = cbEnabled.Checked || !parameter.isOptional;
			textBox_Custom.Enabled = cbEnabled.Checked || !parameter.isOptional;
			if (isIgnoringEvents)
				return;
			parameter.isEnabled = cbEnabled.Checked || !parameter.isOptional;
			NotifyEnabledChanged();
		}

		private void OnValueChanged(object sender, EventArgs e)
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
					this.parameter.value = parameter.items[index].id.ToString();
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

		private void OnCustomTextChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			if (parameter.style == ChoiceParameter.Style.Custom && comboBox.SelectedIndex >= comboBox.Items.Count - 1)
			{
				if (this.parameter.value != textBox_Custom.Text)
				{
					this.parameter.value = textBox_Custom.Text;
					this.parameter.selectedIndex = -2;
					NotifyValueChanged();
				}
			}
		}

		private void SelectByValue(string value)
		{
			if (comboBox.Items.Count == 0)
				return;

			int index = this.parameter.items.FindIndex(i => string.Compare(i.value, value, true) == 0 || string.Compare(i.label, value, true) == 0);

			if (index == -1 && parameter.style == ChoiceParameter.Style.Custom && string.IsNullOrWhiteSpace(value) == false)
			{
				index = comboBox.Items.Count - 1;
				textBox_Custom.Text = value;
			}
			else if (index == -1 && parameter.isOptional)
				index = 0; // (not set)
			else if (parameter.isOptional)
				index += 1; // (not set)...

			if (index < 0 || index >= comboBox.Items.Count)
				index = 0;
			comboBox.SelectedItem = comboBox.Items[index];
		}

		public ISearchable[] GetSearchables()
		{
			return new ISearchable[] { textBox_Custom.richTextBox };
		}

		private void OnMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			ParameterPanel_MouseClick(sender, e);
		}
	}
}
