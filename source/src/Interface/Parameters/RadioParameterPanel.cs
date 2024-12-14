using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public partial class RadioParameterPanel : RadioParameterPanelDummy
	{
		protected override CheckBox parameterCheckBox { get { return cbEnabled; } }
		protected override Label parameterLabel { get { return label; } }

		private static int EntryHeight { get { return 21; } }

		private static readonly int MinPerColumn = 3;

		public RadioParameterPanel()
		{
			InitializeComponent();
		}

		private List<RadioButton> radioButtons = new List<RadioButton>();

		private int Selected
		{
			get
			{
				for (int i = 0; i < radioButtons.Count; ++i)
					if (radioButtons[i].Checked)
						return i;
				return -1;
			}
		}

		protected override void OnSetParameter()
		{
			radioPanel0.SuspendLayout();
			radioPanel1.SuspendLayout();
			radioPanel2.SuspendLayout();

			int iParameterCount = parameter.items.Count;
			if (parameter.isOptional)
			{
				AddOption(0, "(Not set)"); // Empty
				iParameterCount += 1;
			}

			int iColumnHeight;
			if (iParameterCount <= MinPerColumn)
			{
				foreach (var item in parameter.items)
					AddOption(0, item.label);
				iColumnHeight = iParameterCount;
			}
			else if (iParameterCount <= MinPerColumn * 2)
			{
				int col0 = Convert.ToInt32(Math.Ceiling(iParameterCount / 2.0f));
				iColumnHeight = col0;
				if (parameter.isOptional)
					col0 -= 1;

				for (int i = 0; i < parameter.items.Count; ++i)
				{
					var item = parameter.items[i];

					if (i < col0)
						AddOption(0, item.label);
					else
						AddOption(1, item.label);
				}
			}
			else
			{
				int col0 = Convert.ToInt32(Math.Ceiling(iParameterCount / 3.0f));
				int col1 = col0 * 2;
				iColumnHeight = col0;
				if (parameter.isOptional)
				{
					col0 -= 1;
					col1 -= 1;
				}


				for (int i = 0; i < parameter.items.Count; ++i)
				{
					var item = parameter.items[i];

					if (i < col0)
						AddOption(0, item.label);
					else if (i < col1)
						AddOption(1, item.label);
					else
						AddOption(2, item.label);
				}
			}

			if (string.IsNullOrEmpty(parameter.value) == false)
				SelectByValue(parameter.value);
			else if (string.IsNullOrEmpty(parameter.defaultValue) == false)
				SelectByValue(parameter.defaultValue);
			else if (radioButtons.Count > 0)
				radioButtons[0].Checked = true;

			radioPanel0.Enabled = parameter.isEnabled || !parameter.isOptional;
			radioPanel1.Enabled = parameter.isEnabled || !parameter.isOptional;
			radioPanel2.Enabled = parameter.isEnabled || !parameter.isOptional;

			radioPanel2.ResumeLayout(false);
			radioPanel1.ResumeLayout(false);
			radioPanel0.ResumeLayout(false);

			// Enabled checkbox
			cbEnabled.Enabled = parameter.isOptional;
			cbEnabled.Checked = parameter.isEnabled;

			tableLayoutPanel.Size = new Size(this.Size.Width, EntryHeight * iColumnHeight + 4);

			// Tooltip
			SetTooltip(label);
		}

		protected override void OnSetEnabled(bool bEnabled)
		{
			cbEnabled.Enabled = bEnabled && parameter.isOptional;
			radioPanel0.Enabled = bEnabled && parameter.isEnabled;
			radioPanel1.Enabled = bEnabled && parameter.isEnabled;
			radioPanel2.Enabled = bEnabled && parameter.isEnabled;
		}

		protected override void OnSetReserved(bool bReserved, string reservedValue)
		{
			cbEnabled.Enabled = !bReserved && parameter.isOptional;
			radioPanel0.Enabled = !bReserved && parameter.isEnabled;
			radioPanel1.Enabled = !bReserved && parameter.isEnabled;
			radioPanel2.Enabled = !bReserved && parameter.isEnabled;

			WhileIgnoringEvents(() => {
				SelectByValue(bReserved ? reservedValue : this.parameter.value);	
			});
		}

		private void AddOption(int column, string label)
		{
			var radioButton = new RadioButton();
			radioButton.AutoSize = true;
			radioButton.Dock = DockStyle.Top;
			radioButton.Size = new Size(200, EntryHeight);
			radioButton.TabIndex = radioButtons.Count;
			radioButton.TabStop = true;
			radioButton.Text = label;
			radioButton.Font = this.Font;
			radioButton.UseVisualStyleBackColor = true;
			radioButton.CheckedChanged += OnValueChanged;

			switch (column)
			{
			case 0:
				radioPanel0.Controls.Add(radioButton);
				radioPanel0.Controls.SetChildIndex(radioButton, 0);
				break;
			case 1:
				radioPanel1.Controls.Add(radioButton);
				radioPanel1.Controls.SetChildIndex(radioButton, 0);
				break;
			case 2:
				radioPanel2.Controls.Add(radioButton);
				radioPanel2.Controls.SetChildIndex(radioButton, 0);
				break;
			}

			radioButtons.Add(radioButton);
		}

		private void CbEnabled_CheckedChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			radioPanel0.Enabled = cbEnabled.Checked || !parameter.isOptional;
			radioPanel1.Enabled = cbEnabled.Checked || !parameter.isOptional;
			radioPanel2.Enabled = cbEnabled.Checked || !parameter.isOptional;

			if (isIgnoringEvents)
				return;

			parameter.isEnabled = cbEnabled.Checked || !parameter.isOptional;
			NotifyEnabledChanged();
		}

		private void OnValueChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			// Manually uncheck all other radio buttons because they could be inside different panels
			WhileIgnoringEvents(() => {
				RadioButton rb = (RadioButton)sender;
				if (rb.Checked)
				{
					foreach (RadioButton other in radioButtons)
					{
						if (other == rb)
							continue;
						other.Checked = false;
					}
				}
			});

			int index = Selected;
			if (index <= -1 && parameter.isOptional)
			{
				this.parameter.value = "";
				this.parameter.selectedIndex = -1;
			}
			else
			{
				if (parameter.isOptional)
					index -= 1; // (not set)

				if (index >= 0 && index < radioButtons.Count)
				{
					this.parameter.value = parameter.items[index].value;
					this.parameter.selectedIndex = index;
				}
				else
				{
					this.parameter.value = "";
					this.parameter.selectedIndex = -1;
				}
			}
			NotifyValueChanged();
		}

		protected override void OnRefreshValue()
		{
			cbEnabled.Checked = this.parameter.isEnabled;
			
			SelectByValue(this.parameter.value);
		}

		private void SelectByValue(string value)
		{
			if (radioButtons.Count == 0)
				return;

			int index = this.parameter.items.FindIndex(i => i.value == value || string.Compare(i.label, value, true) == 0);
			if (index == -1 && parameter.isOptional)
				index = 0; // (not set)
			else if (parameter.isOptional)
				index += 1; // (not set)...

			if (index < 0 || index >= radioButtons.Count)
				index = 0;

			radioButtons[index].Checked = true;
		}

		private void OnMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			ParameterPanel_MouseClick(sender, e);
		}

		public override int GetParameterHeight()
		{
			return tableLayoutPanel.Size.Height;
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			SizeLabel(label);
			SizeToWidth(tableLayoutPanel);
		}
	}
}
