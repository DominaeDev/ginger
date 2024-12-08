using System;

namespace Ginger
{
	public partial class ProgressBarDialog : FormEx
	{
		public string Message 
		{ 
			set
			{
				labelText.Text = value;
			} 
		}

		public int Percentage 
		{ 
			set
			{
				progressBar.Value = Math.Min(Math.Max(0, value), 100);
			}
		}

		public EventHandler onCancel;

		public ProgressBarDialog()
		{
			InitializeComponent();

			TopMost = true;
			FormClosing += ProgressBarDialog_FormClosing;
		}

		private void ProgressBarDialog_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
		{
			// Make sure this goes away before the message box appears.
			Visible = false;
			if (Owner != null)
				Owner.Visible = true; // Because of a weird window order glitch observed while debugging.
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			onCancel?.Invoke(this, EventArgs.Empty);
		}
	}
}
