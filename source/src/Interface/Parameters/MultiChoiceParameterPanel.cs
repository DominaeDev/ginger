using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public partial class MultiChoiceParameterPanel : MultiChoiceParameterPanelDummy
	{
		private static Font _font = new Font(Constants.DefaultFontFace, Constants.DefaultFontSize, FontStyle.Regular, GraphicsUnit.Point, 0);

		protected override CheckBox parameterCheckBox { get { return cbEnabled; } }
		protected override Label parameterLabel { get { return label; } }

		private static int EntryHeight { get { return 21; } }

		private static readonly int MinPerColumn = 3;

		public MultiChoiceParameterPanel()
		{
			InitializeComponent();
		}

		private List<CheckBox> checkBoxes = new List<CheckBox>();

		protected override void OnSetParameter()
		{
			checkBoxPanel0.SuspendLayout();
			checkBoxPanel1.SuspendLayout();
			checkBoxPanel2.SuspendLayout();

			int iColumnHeight;
			if (parameter.items.Count <= MinPerColumn)
			{
				foreach (var item in parameter.items)
					AddOption(0, item.label, IsSelected(parameter, item.id));
				iColumnHeight = parameter.items.Count;
			}
			else if (parameter.items.Count <= MinPerColumn * 2)
			{
				int col0 = Convert.ToInt32(Math.Ceiling(parameter.items.Count / 2.0f));
				iColumnHeight = col0;

				for (int i = 0; i < parameter.items.Count; ++i)
				{
					var item = parameter.items[i];

					if (i < col0)
						AddOption(0, item.label, IsSelected(parameter, item.id));
					else
						AddOption(1, item.label, IsSelected(parameter, item.id));
				}
			}
			else
			{
				int col0 = Convert.ToInt32(Math.Ceiling(parameter.items.Count / 3.0f));
				int col1 = col0 * 2;
				iColumnHeight = col0;

				for (int i = 0; i < parameter.items.Count; ++i)
				{
					var item = parameter.items[i];

					if (i < col0)
						AddOption(0, item.label, IsSelected(parameter, item.id));
					else if (i < col1)
						AddOption(1, item.label, IsSelected(parameter, item.id));
					else
						AddOption(2, item.label, IsSelected(parameter, item.id));
				}
			}

			checkBoxPanel0.Enabled = parameter.isEnabled || !parameter.isOptional;
			checkBoxPanel1.Enabled = parameter.isEnabled || !parameter.isOptional;
			checkBoxPanel2.Enabled = parameter.isEnabled || !parameter.isOptional;

			checkBoxPanel2.ResumeLayout(false);
			checkBoxPanel1.ResumeLayout(false);
			checkBoxPanel0.ResumeLayout(false);

			// Enabled checkbox
			cbEnabled.Enabled = parameter.isOptional;
			cbEnabled.Checked = parameter.isEnabled;

			tableLayoutPanel.Size = new Size(this.Size.Width, EntryHeight * iColumnHeight + 4);

			// Tooltip
			SetTooltip(label);
		}

		private static bool IsSelected(MultiChoiceParameter parameter, StringHandle itemID)
		{
			return parameter.value.Contains(itemID.ToString());
		}

		protected override void OnSetEnabled(bool bEnabled)
		{
			cbEnabled.Enabled = bEnabled && parameter.isOptional;
			checkBoxPanel0.Enabled = bEnabled && parameter.isEnabled;
			checkBoxPanel1.Enabled = bEnabled && parameter.isEnabled;
			checkBoxPanel2.Enabled = bEnabled && parameter.isEnabled;
		}

		protected override void OnSetReserved(bool bReserved, string reservedValue)
		{
			cbEnabled.Enabled = !bReserved && parameter.isOptional;
			checkBoxPanel0.Enabled = !bReserved && parameter.isEnabled;
			checkBoxPanel1.Enabled = !bReserved && parameter.isEnabled;
			checkBoxPanel2.Enabled = !bReserved && parameter.isEnabled;

			WhileIgnoringEvents(() => {
				if (bReserved)
					SelectByValue(new HashSet<string>(Utility.ListFromCommaSeparatedString(reservedValue)));
				else
					SelectByValue(parameter.value);
			});
		}

		private void AddOption(int column, string label, bool bChecked = false)
		{
			var checkBox = new CheckBox();
			checkBox.AutoSize = true;
			checkBox.Dock = DockStyle.Top;
			checkBox.Size = new Size(200, EntryHeight);
			checkBox.TabIndex = checkBoxes.Count;
			checkBox.TabStop = true;
			checkBox.Text = label;
			checkBox.Checked = bChecked;
			checkBox.Font = _font;
			checkBox.UseVisualStyleBackColor = true;
			checkBox.CheckedChanged += OnCheckBoxChecked;

			switch (column)
			{
			case 0:
				checkBoxPanel0.Controls.Add(checkBox);
				checkBoxPanel0.Controls.SetChildIndex(checkBox, 0);
				break;
			case 1:
				checkBoxPanel1.Controls.Add(checkBox);
				checkBoxPanel1.Controls.SetChildIndex(checkBox, 0);
				break;
			case 2:
				checkBoxPanel2.Controls.Add(checkBox);
				checkBoxPanel2.Controls.SetChildIndex(checkBox, 0);
				break;
			}

			checkBoxes.Add(checkBox);
		}

		private void CbEnabled_CheckedChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			checkBoxPanel0.Enabled = cbEnabled.Checked || !parameter.isOptional;
			checkBoxPanel1.Enabled = cbEnabled.Checked || !parameter.isOptional;
			checkBoxPanel2.Enabled = cbEnabled.Checked || !parameter.isOptional;

			if (isIgnoringEvents)
				return;

			parameter.isEnabled = cbEnabled.Checked || !parameter.isOptional;
			NotifyEnabledChanged();
		}

		private void OnCheckBoxChecked(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			CheckBox cb = sender as CheckBox;
			if (cb == null)
				return;

			int index = checkBoxes.IndexOf(cb);
			if (index < 0)
				return;

			StringHandle id = this.parameter.items[index].id;

			if (cb.Checked)
				this.parameter.value.Add(id.ToString());
			else
				this.parameter.value.Remove(id.ToString());

			NotifyValueChanged();
		}

		protected override void OnRefreshValue()
		{
			cbEnabled.Checked = this.parameter.isEnabled;
			
			SelectByValue(this.parameter.value);
		}

		private void SelectByValue(HashSet<string> value)
		{
			if (checkBoxes.Count == 0)
				return;

			for (int i = 0; i < checkBoxes.Count && i < this.parameter.items.Count; ++i)
				checkBoxes[i].Checked = value.Contains(this.parameter.items[i].id.ToString());
		}

		private void OnMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			ParameterPanel_MouseClick(sender, e);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			SizeLabel(label);
			SizeToWidth(tableLayoutPanel);
		}

		public override int GetParameterHeight()
		{
			return tableLayoutPanel.Size.Height;
		}
	}
}
