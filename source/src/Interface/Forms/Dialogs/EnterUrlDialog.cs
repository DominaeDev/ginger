using System;
using System.Windows.Forms;

namespace Ginger
{
	public partial class EnterUrlDialog : FormEx
	{
		public string Uri = "http://";

		public EnterUrlDialog()
		{
			InitializeComponent();

			Load += EnterUrlDialog_Load;
			textBox.EnterPressed += btnOk_Click;
		}

		private void EnterUrlDialog_Load(object sender, EventArgs e)
		{
			if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
			{
				string link = (string)Clipboard.GetDataObject().GetData("UnicodeText");
				if (link.BeginsWith("http"))
					Uri = link;
			}	

			textBox.Text = Uri;
			textBox.FocusAndSelect(0, textBox.Text.Length);
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			Uri = textBox.Text.Trim();

			int idxProtocol = Uri.IndexOf("://");
			string protocol;
			string uri;
			if (Uri.BeginsWith("file:///"))
			{
				protocol = "file:///";
				uri = Uri.Substring(8);
			}
			else if (idxProtocol != -1)
			{
				protocol = Uri.Substring(0, idxProtocol + 3);
				uri = Uri.Substring(idxProtocol + 3);
			}
			else
			{
				protocol = "http://";
				uri = Uri;
			}

			if (uri.Length == 0) // Invalid
			{
				DialogResult = DialogResult.Cancel;
				Close();
			}

			Uri = string.Concat(protocol, uri);
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			Uri = null;
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
