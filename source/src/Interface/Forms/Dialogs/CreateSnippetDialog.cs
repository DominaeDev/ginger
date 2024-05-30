using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Ginger
{
	public partial class CreateSnippetDialog : Form
	{
		List<SnippetPanel> panels = new List<SnippetPanel>();

		public string SnippetName;
		public string FileName;
		public string[] Texts = null;

		private bool _bIgnoreEvents = false;

		public CreateSnippetDialog()
		{
			InitializeComponent();

			Load += CreateSnippetDialog_Load;
			textBox.TextChanged += TextBox_TextChanged;
			textBox.GotFocus += TextBox_GotFocus;
			textBox.LostFocus += TextBox_LostFocus;
			textBox.EnterPressed += BtnOk_Click;
		}

		private static string[] GetSnippetPaths()
		{
			return RecipeBook.allRecipes
				.Where(r => r.type == Recipe.Type.Snippet)
				.Select(r => {
					StringBuilder sbPath = new StringBuilder();
					foreach (var path in r.path)
					{
						if (sbPath.Length > 0)
							sbPath.Append('/');
						sbPath.Append(path);
					}
					return sbPath.ToString();
				})
				.Where(s => string.IsNullOrEmpty(s) == false)
				.DistinctBy(s => s.ToLowerInvariant())
				.OrderBy(s => s)
				.ToArray();
		}

		private void TextBox_GotFocus(object sender, EventArgs e)
		{
			AcceptButton = btnOk;
		}

		private void TextBox_LostFocus(object sender, EventArgs e)
		{
			AcceptButton = null;
		}

		private void TextBox_TextChanged(object sender, EventArgs e)
		{
			btnOk.Enabled = string.IsNullOrWhiteSpace(textBox.Text) == false;
		}

		private void CreateSnippetDialog_Load(object sender, EventArgs e)
		{
			textBox.Text = "New snippet";
			textBox.SelectAll();
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == ShortcutKeys.Cancel)
			{
				DialogResult = DialogResult.Cancel;
				Close();
				return true;
			}
			return false;
		}

		public void SetOutput(Generator.Output output)
		{
			SuspendLayout();

			int panelHeight = 140;

			bool bEmpty = true;
			int numChannels = EnumHelper.ToInt(Recipe.Component.Count);
			bool canSwap = false;

			var channels = new Recipe.Component[] {
				Recipe.Component.System,
				Recipe.Component.System_PostHistory,
				Recipe.Component.Persona,
				Recipe.Component.UserPersona,
				Recipe.Component.Scenario,
				Recipe.Component.Example,
				Recipe.Component.Greeting,
				Recipe.Component.Grammar,
			};

			for (int i = 0; i < numChannels; ++i)
			{
				var channel = channels[i];
				string text = output.GetText(channel).ToString();

				if (string.IsNullOrWhiteSpace(text) == false)
				{
					string originalText = ToSnippet(text);
					if (channel == Recipe.Component.Greeting || channel == Recipe.Component.Example)
						originalText = TextStyleConverter.Convert(originalText, CardData.TextStyle.Mixed);

					string swappedText = originalText;
					GenderSwap.ToNeutralMarkers(ref swappedText); // him -> {them}

					if (string.Compare(originalText, swappedText, true) != 0)
						canSwap = true;

					var snippetPanel = new SnippetPanel();
					panelList.Controls.Add(snippetPanel);
					panelList.Controls.SetChildIndex(snippetPanel, 0);
					snippetPanel.Dock = DockStyle.Top;

					snippetPanel.SetText(originalText, swappedText, channel);
					panels.Add(snippetPanel);

					panelHeight = snippetPanel.Size.Height;
					bEmpty = false;
				}
			}

			if (bEmpty)
			{
				// Add empty persona panel
				var snippetPanel = new SnippetPanel();
				panelList.Controls.Add(snippetPanel);
				panelList.Controls.SetChildIndex(snippetPanel, 0);
				snippetPanel.Dock = DockStyle.Top;

				snippetPanel.SetText("", "", Recipe.Component.Persona);
				snippetPanel.SetSwapped(cbSwapPronouns.Checked);
				panels.Add(snippetPanel);

				panelHeight = snippetPanel.Size.Height;
			}

			canSwap &= GenderSwap.PronounsFromGender(Current.MainCharacter) != GenderSwap.Pronouns.Neutral
				|| GenderSwap.PronounsFromGender(Current.Card.userGender) != GenderSwap.Pronouns.Neutral;

			_bIgnoreEvents = true;
			cbSwapPronouns.Checked = AppSettings.User.SnippetSwapPronouns;
			cbSwapPronouns.Enabled = canSwap;
			foreach (var snippetPanel in panels)
				snippetPanel.SetSwapped(cbSwapPronouns.Checked);
			_bIgnoreEvents = false;


			// Tab stop
			for (int i = 0; i < panels.Count; ++i)
				panels[i].TabIndex = i;

			int listHeight = panelHeight * Math.Min(panels.Count, 3);

			panelList.Size = new System.Drawing.Size(panelList.Size.Width, listHeight + 2);

			ResumeLayout();
		}

		private static string ToSnippet(string text)
		{
			StringBuilder sb = new StringBuilder(text);

			// Unescape
			GingerString.Unescape(sb);

			sb.Trim();

			sb.ConvertLinebreaks(Linebreak.CRLF);
			text = sb.ToString();
			return text;
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			// Ensure name
			SnippetName = textBox.Text.Trim();
			if (string.IsNullOrEmpty(SnippetName))
				return;

			// Path
			var lsPath = SnippetName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.ToList();

			string filename = Utility.ValidFilename(lsPath[lsPath.Count - 1]);
			StringBuilder sbFullName = new StringBuilder();
			foreach (var path in lsPath)
			{
				if (sbFullName.Length > 0)
					sbFullName.Append('/');
				sbFullName.Append(path);
			}
			SnippetName = sbFullName.ToString();

			// Prepare texts
			int numChannels = EnumHelper.ToInt(Recipe.Component.Count);
			Texts = new string[numChannels];
			foreach (var panel in panels)
			{
				if (panel.isEnabled == false)
					continue;

				string text = panel.CurrentText.Trim();
				if (string.IsNullOrEmpty(text))
					continue;

				Texts[EnumHelper.ToInt(panel.channel)] = text;
			}

			if (Texts.ContainsNoneOf(t => string.IsNullOrEmpty(t) == false))
			{
				MessageBox.Show(Resources.error_empty_snippet, Resources.cap_save_snippet_error, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return; // All text was erased by user
			}

			if (filename.Length == 0)
				return;

			FileName = Utility.ContentPath("Snippets", filename + ".snippet");
			if (File.Exists(FileName))
			{
				if (MessageBox.Show(Resources.msg_overwrite_snippet, Resources.cap_overwrite_snippet, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
					return;
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			SnippetName = null;
			FileName = null;
			Texts = null;
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void TextBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyData == Keys.Return || e.KeyData == Keys.Enter)
			{
				DialogResult = DialogResult.OK;
				Close();
			}
		}

		private void cbSwapPronouns_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			bool bSwapped = cbSwapPronouns.Checked;
			AppSettings.User.SnippetSwapPronouns = bSwapped;

			foreach (var panel in panels)
				panel.SetSwapped(bSwapped);
		}
	}
}
