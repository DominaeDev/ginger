using System;
using System.Windows.Forms;

namespace Ginger
{
	public partial class EnterNameDialog : FormEx
	{
		public string Value { get; set; }
		public string Label
		{
			set { label.Text = string.Concat(value, ":"); }
		}
		public bool AllowEmpty = false;

		public EnterNameDialog()
		{
			InitializeComponent();

			Load += EnterNameDialog_Load;
			textBox.EnterPressed += btnOk_Click;
		}

		private void EnterNameDialog_Load(object sender, EventArgs e)
		{
			textBox.Text = Value ?? "";
			textBox.FocusAndSelect(0, textBox.Text.Length);
			btnOk.Enabled = AllowEmpty || textBox.Text.Trim().Length > 0;
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			Value = textBox.Text.Trim();
			if (Value.Length == 0 && !AllowEmpty) // Invalid
				return;

			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			Value = default(string);
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void textBox_TextChanged(object sender, EventArgs e)
		{
			btnOk.Enabled = AllowEmpty || textBox.Text.Trim().Length > 0;
		}
	}
}
