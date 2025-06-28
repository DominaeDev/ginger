using System;
using System.Windows.Forms;

namespace Ginger
{
	public partial class FileFormatDialog : FormEx
	{
		private string[] _Filters = new string[] {
			"Ginger Multi-Format Card (*.png)",
			"Character Card V2 PNG (*.png)",
			"Character Card V3 PNG (*.png)",
			"Backyard AI PNG (*.png)",
			"CharX Card (*.charx)",
			"Character Card V2 JSON (*.json)",
			"Character Card V3 JSON (*.json)",
			"Agnai Character JSON (*.json)",
			"PygmalionAI Character JSON (*.json)",
			"Text Generation Web UI YAML (*.yaml)",
			"Character Backup (*.zip)",
			"Backyard Archive (*.byaf)",
		};

		private FileUtil.FileType[] _FileTypes = new FileUtil.FileType[] {
			FileUtil.FileType.Ginger | FileUtil.FileType.Png | FileUtil.FileType.Character,
			FileUtil.FileType.TavernV2 | FileUtil.FileType.Png | FileUtil.FileType.Character,
			FileUtil.FileType.TavernV3 | FileUtil.FileType.Png | FileUtil.FileType.Character,
			FileUtil.FileType.Faraday | FileUtil.FileType.Png | FileUtil.FileType.Character,
			FileUtil.FileType.TavernV3 | FileUtil.FileType.CharX | FileUtil.FileType.Character,
			FileUtil.FileType.TavernV2 | FileUtil.FileType.Json | FileUtil.FileType.Character,
			FileUtil.FileType.TavernV3 | FileUtil.FileType.Json | FileUtil.FileType.Character,
			FileUtil.FileType.Agnaistic | FileUtil.FileType.Json | FileUtil.FileType.Character,
			FileUtil.FileType.Pygmalion | FileUtil.FileType.Json | FileUtil.FileType.Character,
			FileUtil.FileType.TextGenWebUI | FileUtil.FileType.Yaml | FileUtil.FileType.Character,
			FileUtil.FileType.Ginger | FileUtil.FileType.Backup | FileUtil.FileType.Character,
			FileUtil.FileType.Faraday | FileUtil.FileType.BackyardArchive | FileUtil.FileType.Character,
		};

		private string[] _Group_Filters = new string[] {
			"Ginger Multi-Format Card (*.png)",
			"Character Backup Archive (*.zip)",
		};

		private FileUtil.FileType[] _Group_FileTypes = new FileUtil.FileType[] {
			FileUtil.FileType.Ginger | FileUtil.FileType.Png | FileUtil.FileType.Character,
			FileUtil.FileType.Ginger | FileUtil.FileType.Backup | FileUtil.FileType.Character,
		};

		private string[] Filters { get { return GroupFormats ? _Group_Filters : _Filters; } }
		private FileUtil.FileType[] FileTypes { get { return GroupFormats ? _Group_FileTypes: _FileTypes; } }

		public bool GroupFormats { get; set; }
		public FileUtil.FileType FileFormat { get; private set; }

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

			int filter = GroupFormats ? AppSettings.User.LastBulkExportGroupFilter : AppSettings.User.LastBulkExportCharacterFilter;
			if (filter > 0 && filter <= comboBox.Items.Count)
				comboBox.SelectedIndex = filter - 1;
			else
				comboBox.SelectedIndex = 0;

			comboBox.EndUpdate();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if (comboBox.SelectedIndex < 0 || comboBox.SelectedIndex >= FileTypes.Length)
				return; // Error

			if (GroupFormats)
				AppSettings.User.LastBulkExportGroupFilter = comboBox.SelectedIndex + 1;
			else
				AppSettings.User.LastBulkExportCharacterFilter = comboBox.SelectedIndex + 1;

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
