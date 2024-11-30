using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Ginger
{
	public partial class EditModelSettingsDialog : FormEx
	{
		private bool _bIgnoreEvents = false;

		public EditModelSettingsDialog()
		{
			InitializeComponent();

		//	AcceptButton = btnConfirm;
			CancelButton = btnCancel;

			Load += EditModelSettingsDialog_Load;

			textBox_Temperature.KeyPress += TextBox_Decimal_KeyPress;
			textBox_MinP.KeyPress += TextBox_Decimal_KeyPress;
			textBox_TopP.KeyPress += TextBox_Decimal_KeyPress;
			textBox_TopK.KeyPress += TextBox_Integer_KeyPress;
			textBox_RepeatPenalty.KeyPress += TextBox_Decimal_KeyPress;
			textBox_RepeatTokens.KeyPress += TextBox_Integer_KeyPress;

			cbSampling.SelectedIndexChanged += CbSampling_SelectedIndexChanged;
		}

		private void EditModelSettingsDialog_Load(object sender, EventArgs e)
		{
			cbSampling.SelectedItem = cbSampling.Items[0];
		}

		private void CbSampling_SelectedIndexChanged(object sender, EventArgs e)
		{
			bool bMinPEnabled = cbSampling.SelectedIndex == 0;
			labelMinP.Enabled = bMinPEnabled;
			panelMinP.Enabled = bMinPEnabled;
			trackBar_MinP.Visible = bMinPEnabled;
			labelTopP.Enabled = !bMinPEnabled;
			panelTopP.Enabled = !bMinPEnabled;
			trackBar_TopP.Visible = !bMinPEnabled;
			labelTopK.Enabled = !bMinPEnabled;
			panelTopK.Enabled = !bMinPEnabled;
			trackBar_TopK.Visible = !bMinPEnabled;

			ApplyTheme();
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void trackBar_Temperature_ValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			decimal value = Utility.RoundNearest(trackBar_Temperature.Value * 0.1m, 0.1m);
			textBox_Temperature.Text = value.ToString("0.0", CultureInfo.InvariantCulture);
		}

		private void trackBar_MinP_ValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			decimal value = Utility.RoundNearest(trackBar_MinP.Value * 0.01m, 0.05m);
			textBox_MinP.Text = value.ToString("0.00", CultureInfo.InvariantCulture);
		}

		private void trackBar_TopP_ValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			decimal value = Utility.RoundNearest(trackBar_TopP.Value * 0.01m, 0.05m);
			textBox_TopP.Text = value.ToString("0.00", CultureInfo.InvariantCulture);
		}

		private void trackBar_TopK_ValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;
			
			int value = trackBar_TopK.Value;
			textBox_TopK.Text = value.ToString();
		}

		private void trackBar_RepeatPenalty_ValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			decimal value = Utility.RoundNearest(trackBar_RepeatPenalty.Value * 0.01m, 0.05m);
			textBox_RepeatPenalty.Text = value.ToString("0.00", CultureInfo.InvariantCulture);
		}

		private void trackBar_RepeatTokens_ValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int value = trackBar_RepeatTokens.Value;
			textBox_RepeatTokens.Text = value.ToString();
		}

		private void TextBox_Integer_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			if (e.KeyChar == 0x7f) // Ctrl+Backspace
			{
				e.Handled = true;
				return;
			}

			if (char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar))
				e.Handled = false; //Do not reject the input
			else
				e.Handled = true; //Reject the input
		}

		
		private void TextBox_Decimal_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			if (e.KeyChar == 0x7f) // Ctrl+Backspace
			{
				e.Handled = true;
				return;
			}

			if (char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar) || e.KeyChar == '.')
			{
				var textBox = sender as TextBoxBase;
				if (e.KeyChar == '.' && textBox != null && textBox.Text.IndexOf('.') != -1)
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

	}
}
