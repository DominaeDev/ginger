using System;
using System.Windows.Forms;

namespace Ginger
{
	public partial class FindReplaceDialog : FormEx
	{
		public enum Context
		{
			Main,
			Write,
		}
		public Context context = Context.Main;
		public string Match { get { return textBox_Find.Text; } set { textBox_Find.Text = value; } }
		public string Replace { get { return textBox_Replace.Text; } }
		public bool MatchWholeWord { get { return cbWholeWords.Checked; } }
		public bool IgnoreCase { get { return !cbMatchCase.Checked; } }
		public bool IncludeLorebooks { get { return context == Context.Main && cbLorebooks.Checked; } }

		public FindReplaceDialog()
		{
			InitializeComponent();

			AcceptButton = btnOk;
			CancelButton = btnCancel;
			
			textBox_Find.Text = AppSettings.User.ReplaceLastFind;
			textBox_Find.EnterPressed += BtnOk_Click;
			textBox_Replace.EnterPressed += BtnOk_Click;

			Load += FindReplaceDialog_Load;
		}

		private void FindReplaceDialog_Load(object sender, EventArgs e)
		{
			textBox_Replace.Text = AppSettings.User.ReplaceLastReplace;
			textBox_Find.SelectAll();
			cbMatchCase.Checked = AppSettings.User.ReplaceMatchCase;
			cbWholeWords.Checked = AppSettings.User.ReplaceWholeWords;
			cbLorebooks.Checked = AppSettings.User.ReplaceLorebooks;
			cbLorebooks.Enabled = context == Context.Main;
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
			AppSettings.User.ReplaceLastFind = textBox_Find.Text;
			AppSettings.User.ReplaceLastReplace = textBox_Replace.Text;
			AppSettings.User.ReplaceWholeWords = cbWholeWords.Checked;
			AppSettings.User.ReplaceMatchCase = cbMatchCase.Checked;
			AppSettings.User.ReplaceLorebooks = cbLorebooks.Checked;

			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
