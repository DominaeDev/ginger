using System;

namespace Ginger
{
	public partial class ProgressBarDialog : FormEx
	{
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
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			onCancel?.Invoke(this, EventArgs.Empty);
		}
	}
}
