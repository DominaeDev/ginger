using System;
using System.Windows.Forms;

namespace Ginger
{
	public partial class FileFormatDialog : FormEx
	{
		public struct Format
		{
			public string Name;
			public string Ext;
			public FileUtil.FileType FileType;
		}

		private string[] Filters = new string[] {
			"Character Card V2 JSON (*.json)",
			"Character Card V3 JSON (*.json)",
			"Agnai Character JSON (*.json)",
			"PygmalionAI Character JSON (*.json)",
			"Character Card V2 PNG (*.png)",
			"Character Card V3 PNG (*.png)",
			"Backyard AI PNG (*.png)",
			"CharX file (*.charx)",
			"Text generation web ui YAML (*.yaml)",
		};

		public FileUtil.FileType FileFormat { get; private set; }

		private FileUtil.FileType[] FileTypes = new FileUtil.FileType[] {
			FileUtil.FileType.TavernV2 | FileUtil.FileType.Json | FileUtil.FileType.Character,
			FileUtil.FileType.TavernV3 | FileUtil.FileType.Json | FileUtil.FileType.Character,
			FileUtil.FileType.Agnaistic | FileUtil.FileType.Json | FileUtil.FileType.Character,
			FileUtil.FileType.Pygmalion | FileUtil.FileType.Json | FileUtil.FileType.Character,
			FileUtil.FileType.TavernV2 | FileUtil.FileType.Png | FileUtil.FileType.Character,
			FileUtil.FileType.TavernV3 | FileUtil.FileType.Png | FileUtil.FileType.Character,
			FileUtil.FileType.Faraday | FileUtil.FileType.Png | FileUtil.FileType.Character,
			FileUtil.FileType.TavernV3 | FileUtil.FileType.CharX | FileUtil.FileType.Character,
			FileUtil.FileType.TextGenWebUI | FileUtil.FileType.Yaml | FileUtil.FileType.Character,
		};

		public FileFormatDialog()
		{
			InitializeComponent();
			FileFormat = FileUtil.FileType.Unknown;
			Load += FileFormatDialog_Load;
		}

		private void FileFormatDialog_Load(object sender, EventArgs e)
		{
			comboBox.BeginUpdate();
			comboBox.Items.Clear();
			comboBox.Items.AddRange(Filters);

			if (AppSettings.User.LastExportCharacterFilter >= 0 && AppSettings.User.LastExportCharacterFilter < comboBox.Items.Count)
				comboBox.SelectedIndex = AppSettings.User.LastExportCharacterFilter;
			else
				comboBox.SelectedIndex = 0;
			comboBox.EndUpdate();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if (comboBox.SelectedIndex < 0 || comboBox.SelectedIndex >= FileTypes.Length)
				return; // Error

			AppSettings.User.LastExportCharacterFilter = comboBox.SelectedIndex;
			FileFormat = FileTypes[comboBox.SelectedIndex];
			
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			FileFormat = FileUtil.FileType.Unknown;
			DialogResult = DialogResult.Cancel;
			Close();
		}

	}
}
