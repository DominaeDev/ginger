using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public partial class FindDialog : Form
	{
		public string Match { get { return textBox_Find.Text; } set { textBox_Find.Text = value; } }
		public bool MatchWholeWord { get { return cbWholeWords.Checked; } }
		public bool IgnoreCase { get { return !cbMatchCase.Checked; } }

		public class FindEventArgs : EventArgs
		{
			public string match;
			public bool wholeWord;
			public bool matchCase;
			public bool reverse;
		}

		public event EventHandler<FindEventArgs> Find;

		public FindDialog()
		{
			InitializeComponent();

			FormClosing += FindDialog_FormClosing;
			textBox_Find.EnterPressed += TextBox_Find_EnterPressed;

			textBox_Find.Text = AppSettings.User.FindMatch;

			AcceptButton = btnOk;
			CancelButton = btnCancel;

			Load += FindDialog_Load;
		}

		private void TextBox_Find_EnterPressed(object sender, EventArgs e)
		{
			BtnOk_Click(this, EventArgs.Empty);
		}

		private void FindDialog_Load(object sender, EventArgs e)
		{
			textBox_Find.SelectAll();
			cbMatchCase.Checked = AppSettings.User.FindMatchCase;
			cbWholeWords.Checked = AppSettings.User.FindWholeWords;

			if (AppSettings.User.FindLocation != default(Point))
				this.Location = AppSettings.User.FindLocation;
		}

		private void FindDialog_FormClosing(object sender, FormClosingEventArgs e)
		{
			AppSettings.User.FindMatch = textBox_Find.Text;
			AppSettings.User.FindWholeWords = cbWholeWords.Checked;
			AppSettings.User.FindMatchCase = cbMatchCase.Checked;
			AppSettings.User.FindLocation = this.Location;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			
			return false;
		}

		// Reduce flickering
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
				return cp;
			}
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			AppSettings.User.FindMatch = textBox_Find.Text;
			AppSettings.User.FindWholeWords = cbWholeWords.Checked;
			AppSettings.User.FindMatchCase = cbMatchCase.Checked;
			AppSettings.User.FindLocation = this.Location;

			Find?.Invoke(this, new FindEventArgs() {
				match = textBox_Find.Text,
				wholeWord = cbWholeWords.Checked,
				matchCase = cbMatchCase.Checked,
			});
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
