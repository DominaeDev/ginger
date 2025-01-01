using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Ginger
{
	public partial class AssetViewDialog : FormEx
	{
		public AssetCollection Assets;
		public bool Changed = false;
		private bool _bIgnoreEvents = false;

		private List<string> KnownAssetTypes = new List<string>() { "Portrait", "User portrait", "Background", "Expression", "Other" };

		private DataGridViewComboBoxColumn comboBoxColumn { get { return (DataGridViewComboBoxColumn)assetsDataView.Columns[2]; } }
		private ComboBox _currentComboBox;

		private bool _bEditing;

		public AssetViewDialog()
		{
			InitializeComponent();

			this.Load += AssetViewDialog_Load;
			this.FormClosing += AssetViewDialog_FormClosing;

			btnAdd.Click += BtnAdd_Click;
			btnAddRemote.Click += BtnAddRemote_Click;
			btnRemove.Click += BtnRemove_Click;
			btnView.Click += BtnView_Click;
			btnExport.Click += BtnExport_Click;
			btnApply.Click += BtnApply_Click;
			btnCancel.Click += BtnCancel_Click;

			DragEnter += OnDragEnter;
			DragDrop += OnDragDrop;

			assetsDataView.EditingControlShowing += AssetsDataView_EditingControlShowing;
			assetsDataView.DataError += AssetsDataView_DataError;
		}

		private void AssetViewDialog_Load(object sender, EventArgs e)
		{
			assetsDataView.CellBeginEdit += AssetsDataView_CellBeginEdit;
			assetsDataView.CellEndEdit += AssetsDataView_CellEndEdit;
			assetsDataView.SelectionChanged += AssetsDataView_SelectionChanged;

			Assets = (AssetCollection)Current.Card.assets.Clone();

			foreach (var asset in Assets.Where(a => a.assetType == AssetFile.AssetType.Custom).DistinctBy(a => a.type))
				KnownAssetTypes.Add(asset.type);

			comboBoxColumn.Items.AddRange(KnownAssetTypes.ToArray());

			PopulateTable();
			assetsDataView.ClearSelection();
		}

		private void AssetViewDialog_FormClosing(object sender, FormClosingEventArgs e)
		{
			assetsDataView.EndEdit();
			if (DialogResult == DialogResult.Cancel && Changed)
			{
				var mr = MessageBox.Show(Resources.msg_dismiss_changes, Resources.cap_confirm, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
				if (mr == DialogResult.No)
					e.Cancel = true;
			}
		}

		private void AssetsDataView_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
		{
			var comboBox = _currentComboBox = e.Control as ComboBox;
			if (comboBox != null)
				comboBox.DropDownStyle = ComboBoxStyle.DropDown;
		}

		private void AssetsDataView_DataError(object sender, DataGridViewDataErrorEventArgs e)
		{
			// Do nothing
		}

		private void AssetsDataView_SelectionChanged(object sender, EventArgs e)
		{
			btnRemove.Enabled = assetsDataView.SelectedRows.Count > 0;

			if (assetsDataView.SelectedRows.Count == 1)
			{
				var selectedAsset = GetSelectedAsset();
				btnView.Enabled = selectedAsset != null;
				btnExport.Enabled = selectedAsset != null && selectedAsset.isEmbeddedAsset;
			}
			else
			{
				btnView.Enabled = false;
				btnExport.Enabled = false;
			}
		}

		private void PopulateTable()
		{
			_bIgnoreEvents = true;
			assetsDataView.Rows.Clear();

			foreach (var asset in Assets.Where(a => a.isEmbeddedAsset || a.isRemoteAsset))
				AddRowForAsset(asset);
			_bIgnoreEvents = false;
		}

		private void AddRowForAsset(AssetFile asset)
		{
			string assetName = asset.name;
			string assetExt = (asset.ext ?? "N/A").ToUpperInvariant();
			if (assetExt == "JPG")
				assetExt = "JPEG";
			else if (assetExt == "")
				assetExt = "N/A";
			string assetSize = "N/A";
			
			if (asset.data.bytes != null)
			{
				decimal size = (decimal)asset.data.length / 1_000_000m;
				if (size >= 1.0m)
					assetSize = string.Format(CultureInfo.InvariantCulture, "{0:0.0} MB", size);
				else
					assetSize = string.Format(CultureInfo.InvariantCulture, "{0:0.0} KB", size * 1000);
			}

			if (asset.isRemoteAsset)
			{
				assetName = asset.fullUri;
				assetSize = "(Remote)";
			}

			 // Prettify asset type
			string assetType;
			switch (asset.assetType)
			{
			case AssetFile.AssetType.Icon:
				assetType = "Portrait";
				break;
			case AssetFile.AssetType.UserIcon:
				assetType = "User portrait";
				break;
			case AssetFile.AssetType.Background:
				assetType = "Background";
				break;
			case AssetFile.AssetType.Expression:
				assetType = "Expression";
				break;
			case AssetFile.AssetType.Undefined:
			case AssetFile.AssetType.Other:
				assetType = "Other";
				break;
			case AssetFile.AssetType.Custom:
			default:
				assetType = asset.type;
				break;
			}

			assetsDataView.Rows.Add(assetName, assetExt, assetType, assetSize);
		}

		/// <summary>
		/// Returns the index of the asset corresponding to the selected row
		/// </summary>
		private int GetSelectedIndex()
		{
			if (assetsDataView.SelectedRows.Count != 1)
				return -1;

			int selectedRow = assetsDataView.SelectedRows[0].Index;
			int index = 0;
			for (int i = 0; i < Assets.Count; ++i)
			{
				if ((Assets[i].isEmbeddedAsset || Assets[i].isRemoteAsset) == false)
					continue;
				if (selectedRow != index++)
					continue;
				return i;
			}
			return -1;
		}

		private AssetFile GetSelectedAsset()
		{
			int index = GetSelectedIndex();
			if (index >= 0 && index < Assets.Count)
				return Assets[index];
			return null;
		}

		private KeyValuePair<int, int>[] GetSelectedAssets() // <Assets index, Row index>
		{
			var selection = new List<KeyValuePair<int, int>>(16);
			int index = 0;
			for (int i = 0; i < Assets.Count; ++i)
			{
				if ((Assets[i].isEmbeddedAsset || Assets[i].isRemoteAsset) == false)
					continue;
				if (assetsDataView.Rows[index].Selected)
					selection.Add(new KeyValuePair<int, int>(i, index));
				++index;
			}
			return selection.ToArray();
		}

		private void AssetsDataView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
		{
			_bEditing = true;
		}

		private void AssetsDataView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			_bEditing = false;

			if (_bIgnoreEvents)
				return;

			int index = 0;
			for (int i = 0; i < Assets.Count; ++i)
			{
				if ((Assets[i].isEmbeddedAsset || Assets[i].isRemoteAsset) == false)
					continue;
				if (e.RowIndex != index++)
					continue;

				if (e.ColumnIndex == 0 && Assets[i].isEmbeddedAsset) // Name / Uri
				{
					string value = assetsDataView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string;
					if (value == null)
					{
						// Restore last value
						assetsDataView.Rows[e.RowIndex].Cells[0].Value = Assets[i].name;
						continue;
					}

					if (value != Assets[i].name)
					{
						Assets[i].name = value;
						Changed = true;

						ResolveDuplicateNames();
					}
				}
				else if (e.ColumnIndex == 0 && Assets[i].isRemoteAsset) // Uri
				{
					string value = assetsDataView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string;
					if (value == null)
					{
						// Restore last value
						assetsDataView.Rows[e.RowIndex].Cells[0].Value = Assets[i].fullUri;
						continue;
					}

					Assets[i] = AssetFile.MakeRemote(AssetFile.AssetType.Undefined, value);
					assetsDataView.Rows[e.RowIndex].Cells[0].Value = Assets[i].fullUri;
					string assetExt = (Assets[i].ext ?? "N/A").ToUpperInvariant();
					if (assetExt == "")
						assetExt = "N/A";
					else if (assetExt == "JPG")
						assetExt = "JPEG";
					assetsDataView.Rows[e.RowIndex].Cells[1].Value = assetExt;

					Changed = true;
				}
				else if (e.ColumnIndex == 2) // Type
				{
					string value;
					if (_currentComboBox != null)
						value = _currentComboBox.Text; // Only reliable way to get the entered value. This API is so dumb.
					else
						value = assetsDataView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string;
					SetAssetType(i, value);
				}
			}
			_currentComboBox = null;
		}

		private void SetAssetType(int index, string value)
		{
			if (index < 0)
				return;

			AssetFile.AssetType assetType;
			if (string.IsNullOrEmpty(value) == false)
			{
				switch (value.ToLowerInvariant())
				{
				case "portrait":
				case "image":
				case "icon":
					assetType = AssetFile.AssetType.Icon;
					value = "icon";
					break;
				case "user image":
				case "user portrait":
				case "user_icon":
					assetType = AssetFile.AssetType.UserIcon;
					value = "user_icon";
					break;
				case "background":
					assetType = AssetFile.AssetType.Background;
					value = "background";
					break;
				case "expression":
				case "emotion":
					assetType = AssetFile.AssetType.Expression;
					value = "emotion";
					break;
				case "other":
					assetType = AssetFile.AssetType.Other;
					value = "other";
					break;
				default:
					assetType = AssetFile.AssetType.Custom;
					break;
				}
			}
			else
			{
				assetType = AssetFile.AssetType.Other;
				value = null;
			}

			if (assetType != AssetFile.AssetType.Custom)
			{
				if (assetType != Assets[index].assetType)
				{
					SetAssetTypeColumn(value);
					Assets[index].assetType = assetType;
					Changed = true;

					ResolveDuplicateNames();
				}
			}
			else if (value != Assets[index].type)
			{
				SetAssetTypeColumn(value);

				Assets[index].type = value;
				Changed = true;

				ResolveDuplicateNames();
			}
		}

		private void SetAssetTypeColumn(string value)
		{
			if (assetsDataView.CurrentCell == null || assetsDataView.CurrentCell.ColumnIndex != 2)
				return;

			switch ((value ?? "").ToLowerInvariant())
			{
			case "icon":
				value = KnownAssetTypes[0]; 
				break;
			case "user_icon":
				value = KnownAssetTypes[1]; 
				break;
			case "background": 
				value = KnownAssetTypes[2]; 
				break;
			case "emotion": 
				value = KnownAssetTypes[3]; 
				break;
			case "":
			case "other": 
				value = KnownAssetTypes[4]; 
				break;
			}

			int index = comboBoxColumn.Items.Cast<string>().ToList()
				.FindIndex(s => string.Compare(s, value, StringComparison.OrdinalIgnoreCase) == 0);

			if (index != -1)
			{
				assetsDataView.CurrentCell.Value = comboBoxColumn.Items[index];
			}
			else
			{
				KnownAssetTypes.Add(value);
				comboBoxColumn.Items.Add(value);
				assetsDataView.CurrentCell.Value = value;
			}
		}

		private void BtnApply_Click(object sender, EventArgs e)
		{
			assetsDataView.EndEdit();
			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			assetsDataView.EndEdit();
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void BtnView_Click(object sender, EventArgs e)
		{
			assetsDataView.EndEdit();

			int selectedIndex = GetSelectedIndex();
			if (selectedIndex == -1)
				return;

			AssetFile asset = Assets[selectedIndex];
			if (asset == null)
				return;

			if (asset.isRemoteAsset)
			{
				Utility.OpenUrl(asset.fullUri);
				return;
			}

			if (asset.data.length == 0)
				return;

			try
			{
				string tempPath = Path.Combine(Path.GetTempPath(), "Ginger");
				string filename = Path.Combine(tempPath, string.Concat(asset.data.hash.ToLowerInvariant(), ".", asset.ext ?? ""));

				if (File.Exists(filename) == false)
				{
					// Create directory
					if (Directory.Exists(tempPath) == false)
						Directory.CreateDirectory(tempPath);

					using (var fs = File.Open(filename, FileMode.CreateNew, FileAccess.Write))
					{
						for (long n = asset.data.length; n > 0;)
						{
							int length = (int)Math.Min(n, (long)int.MaxValue);
							fs.Write(asset.data.bytes, 0, length);
							n -= (long)length;
						}
					}
				}
				LaunchTextEditor.OpenAnyFile(filename);
			}
			catch
			{
				MessageBox.Show(Resources.error_open_file_in_exporer, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}
		}

		private void BtnExport_Click(object sender, EventArgs e)
		{
			assetsDataView.EndEdit();

			int selectedIndex = GetSelectedIndex();
			if (selectedIndex == -1)
				return;

			AssetFile asset = Assets[selectedIndex];
			if (asset == null || asset.data.length == 0)
				return;

			// Save as...
			exportFileDialog.Title = Resources.cap_export_asset;
			if (string.IsNullOrEmpty(asset.ext) == false)
				exportFileDialog.Filter = string.Format("{0} files|*.{1}|All types|*.*", asset.ext.ToUpperInvariant(), asset.ext);
			else
				exportFileDialog.Filter = "All types|*.*";
			exportFileDialog.FileName = Utility.ValidFilename(string.Concat(asset.name, ".", asset.ext ?? ""));
			exportFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastImagePath ?? Utility.AppPath("Characters");

			var result = exportFileDialog.ShowDialog();
			if (result != DialogResult.OK || string.IsNullOrWhiteSpace(exportFileDialog.FileName))
				return;

			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(exportFileDialog.FileName);
			string filename = exportFileDialog.FileName;

			try
			{
				var intermediateFilename = Path.GetTempFileName();

				// Write text file
				using (var fs = new FileStream(intermediateFilename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
				{
					// Write buffer
					for (long n = asset.data.length; n > 0;)
					{
						int length = (int)Math.Min(n, (long)int.MaxValue);
						fs.Write(asset.data.bytes, 0, length);
						n -= (long)length;
					}
				}

				// Rename Temporaty file to Target file
				if (File.Exists(filename))
					File.Delete(filename);
				File.Move(intermediateFilename, filename);
			}
			catch
			{
				MessageBox.Show(Resources.error_write_file, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					return;
			}
		}

		private void BtnRemove_Click(object sender, EventArgs e)
		{
			assetsDataView.EndEdit();

			var selectedIndices = GetSelectedAssets();
			if (selectedIndices.Length == 0)
				return;

			_bIgnoreEvents = true;
			foreach (var kvp in selectedIndices.OrderByDescending(kvp => kvp.Key))
			{
				Assets.RemoveAt(kvp.Key);
				assetsDataView.Rows.RemoveAt(kvp.Value);
				Changed = true;
			}
			assetsDataView.ClearSelection();
			_bIgnoreEvents = false;
		}

		private void BtnAdd_Click(object sender, EventArgs e)
		{
			assetsDataView.EndEdit();

			// Open file...
			importFileDialog.Title = Resources.cap_import_asset;
			importFileDialog.Filter = "Image files|*.png;*.apng;*.jpg;*.jpeg;*.gif;*.webp;*.avif|Audio files|*.mp3;*.ogg;*.wav;*.aiff|Video files|*.mp4;*.webm;*.wmv;*.mov;*.mkv|Other files|*.*";
			importFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastImagePath ?? Utility.AppPath("Characters");
			var result = importFileDialog.ShowDialog();
			if (result != DialogResult.OK)
				return;

			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(importFileDialog.FileName);

			_bIgnoreEvents = true;
			foreach (var filename in importFileDialog.FileNames)
				AddAsset(filename);

			_bIgnoreEvents = false;
		}

		private void BtnAddRemote_Click(object sender, EventArgs e)
		{
			assetsDataView.EndEdit();

			// Open file...
			var dlg = new EnterUrlDialog();
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				var asset = AssetFile.MakeRemote(AssetFile.AssetType.Undefined, dlg.Uri);
				if (Assets.ContainsNoneOf(a => a.isRemoteAsset && a.fullUri == asset.fullUri))
				{
					_bIgnoreEvents = true;
					Assets.Add(asset);
					AddRowForAsset(asset);
					Changed = true;
					_bIgnoreEvents = false;
				}
			}
		}

		private bool AddAsset(string filename)
		{
			try
			{
				byte[] bytes = File.ReadAllBytes(filename);

				string name = Path.GetFileNameWithoutExtension(filename);
				string ext = Path.GetExtension(filename).ToLowerInvariant();
				if (ext.Length > 0 && ext[0] == '.')
					ext = ext.Substring(1);
				if (ext == "jpg")
					ext = "jpeg";

				AssetFile.AssetType assetType;
				var imageTypes = new string[] { "jpg", "jpeg", "gif", "png", "apng", "webp", "avif" };
				if (imageTypes.Contains(ext))
					assetType = AssetFile.AssetType.Icon;
				else
					assetType = AssetFile.AssetType.Other;

				var data = AssetData.FromBytes(bytes);
				if (Assets.ContainsAny(a => a.data.hash == data.hash 
					&& string.Compare(a.ext, ext, StringComparison.InvariantCultureIgnoreCase) == 0))
					return false; // Already added

				if (bytes != null && bytes.Length > 0)
				{
					var asset = new AssetFile() {
						name = name,
						ext = ext,
						assetType = assetType,
						data = data,
						uriType = AssetFile.UriType.Embedded,
					};
					Assets.Add(asset);
					AddRowForAsset(asset);
					Changed = true;
					return true;
				}
				return false;
			}
			catch
			{
				return false;
			}
		}

		private void ResolveDuplicateNames()
		{
			var types = Assets.Select(a => a.type).Distinct();

			foreach (var type in types)
			{
				var assetType = AssetFile.AssetTypeFromString(type);
				var used_names = new Dictionary<string, int>();
//				if (assetType == AssetFile.AssetType.Icon && Current.Card.portraitImage != null)
//					used_names.Add(AssetFile.MainAssetName, 1); // Reserve name for main portrait

				for (int i = 0; i < Assets.Count; ++i)
				{
					var asset = Assets[i];
					if (asset.type != type || asset.isEmbeddedAsset == false)
						continue;

					string name = (Assets[i].name ?? "").ToLowerInvariant().Trim();
					if (name == "")
						Assets[i].name = name = "untitled"; // Name mustn't be empty

					if (used_names.ContainsKey(name) == false)
					{
						used_names.Add(name, 1);
						continue;
					}

					int count = used_names[name];
					string testName = string.Format("{0}_{1:00}", name, ++count);
					while (used_names.ContainsKey(testName))
						testName = string.Format("{0}_{1:00}", name, ++count);
					used_names.Add(testName, 1);
					used_names[name] = count;
					Assets[i].name = testName;
				}
			}

			// Refresh data table
			int row = 0;
			for (int i = 0; i < Assets.Count; ++i)
			{
				var asset = Assets[i];
				if ((asset.isEmbeddedAsset || asset.isRemoteAsset) == false)
					continue;

				string value = assetsDataView.Rows[row].Cells[0].Value as string;
				if (value != asset.name && asset.isEmbeddedAsset)
					assetsDataView.Rows[row].Cells[0].Value = asset.name;
				else if (asset.isRemoteAsset)
				{
					assetsDataView.Rows[row].Cells[0].Value = asset.fullUri;
					string assetExt = (asset.ext ?? "N/A").ToUpperInvariant();
					if (assetExt == "")
						assetExt = "N/A";
					else if (assetExt == "JPG")
						assetExt = "JPEG";
					assetsDataView.Rows[row].Cells[1].Value = assetExt;
				}
				++row;
			}
		}

		private void OnDragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Copy;
				return;
			}
			e.Effect = DragDropEffects.None;
		}

		private void OnDragDrop(object sender, DragEventArgs e)
		{
			Activate();

			if (e.Data.GetDataPresent(DataFormats.FileDrop) == false)
				return;

			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			foreach (var filename in files)
				AddAsset(filename);
		}

		public override void ApplyTheme()
		{
			base.ApplyTheme();

			Theme.Apply(assetsDataView);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				if (_bEditing == false)
				{
					DialogResult = DialogResult.Cancel;
					Close();
					return true;
				}
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

	}
}
