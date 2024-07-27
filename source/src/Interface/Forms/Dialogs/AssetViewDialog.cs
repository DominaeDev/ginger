using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Ginger
{
	public partial class AssetViewDialog : Form
	{
		public AssetCollection Assets;
		public bool Changed = false;
		private bool _bIgnoreEvents = false;

		public AssetViewDialog()
		{
			InitializeComponent();

			this.Load += AssetViewDialog_Load;
			this.FormClosed += AssetViewDialog_FormClosed;

			btnApply.Click += BtnApply_Click;
			btnView.Click += BtnView_Click;
			btnExport.Click += BtnExport_Click;
			btnRemove.Click += BtnRemove_Click;
			btnAdd.Click += BtnAdd_Click;
		}

		private void AssetViewDialog_FormClosed(object sender, FormClosedEventArgs e)
		{
			assetsDataView.EndEdit();
		}

		private static readonly string [] TypeLabels = new string[] { "Undefined", "Portrait", "User portrait", "Background", "Expression", "Other" };

		private void AssetViewDialog_Load(object sender, EventArgs e)
		{
			((DataGridViewComboBoxColumn)assetsDataView.Columns[2]).DataSource = TypeLabels;

			assetsDataView.CellEndEdit += AssetsDataView_CellEndEdit;
			assetsDataView.SelectionChanged += AssetsDataView_SelectionChanged;

			Assets = (AssetCollection)Current.Card.assets.Clone();

			PopulateList();
			assetsDataView.ClearSelection();
		}

		private void AssetsDataView_SelectionChanged(object sender, EventArgs e)
		{
			btnRemove.Enabled = assetsDataView.SelectedRows.Count > 0;
			btnExport.Enabled = assetsDataView.SelectedRows.Count == 1;
			btnView.Enabled = assetsDataView.SelectedRows.Count == 1;
		}

		private void PopulateList()
		{
			_bIgnoreEvents = true;
			assetsDataView.Rows.Clear();

			foreach (var asset in Assets.Where(a => a.isEmbeddedAsset))
			{
				AddRowForAsset(asset);
			}
			_bIgnoreEvents = false;
		}

		private void AddRowForAsset(AssetFile asset)
		{
			string assetName = asset.name;
			string assetExt = (asset.ext ?? "N/A").ToUpperInvariant();
			if (assetExt == "JPG")
				assetExt = "JPEG";
			string assetType = TypeLabels[EnumHelper.ToInt(asset.assetType)];
			string assetSize = "N/A";
			if (asset.data.bytes != null)
			{
				decimal size = (decimal)asset.data.length / 1_000_000m;
				if (size >= 1.0m)
					assetSize = string.Format(CultureInfo.InvariantCulture, "{0:0.0} MB", size);
				else
					assetSize = string.Format(CultureInfo.InvariantCulture, "{0:0.0} KB", size * 1000);
			}

			assetsDataView.Rows.Add(assetName, assetExt, assetType, assetSize);
		}

		private int GetSelectedAsset()
		{
			if (assetsDataView.SelectedRows.Count != 1)
				return -1;

			int selectedRow = assetsDataView.SelectedRows[0].Index;
			int index = 0;
			for (int i = 0; i < Assets.Count; ++i)
			{
				if (Assets[i].isEmbeddedAsset == false)
					continue;
				if (selectedRow != index++)
					continue;
				return i;
			}
			return -1;
		}

		private KeyValuePair<int, int>[] GetSelectedAssets() // <Assets index, Row index>
		{
			var selection = new List<KeyValuePair<int, int>>(16);
			int index = 0;
			for (int i = 0; i < Assets.Count; ++i)
			{
				if (Assets[i].isEmbeddedAsset == false)
					continue;
				if (assetsDataView.Rows[index].Selected)
					selection.Add(new KeyValuePair<int, int>(i, index));
				++index;
			}
			return selection.ToArray();
		}

		private void AssetsDataView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int index = 0;
			for (int i = 0; i < Assets.Count; ++i)
			{
				if (Assets[i].isEmbeddedAsset == false)
					continue;
				if (e.RowIndex != index++)
					continue;

				if (e.ColumnIndex == 0) // Name
				{
					string value = assetsDataView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string;
					if (value != Assets[i].name)
					{
						Assets[i].name = value;
						Changed = true;

						ResolveDuplicateNames();
					}
				}
				else if (e.ColumnIndex == 2) // Type
				{
					int idxValue = Array.IndexOf(TypeLabels, assetsDataView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string);
					var value = EnumHelper.FromInt(idxValue, AssetFile.AssetType.Undefined);
					if (value != Assets[i].assetType)
					{
						Assets[i].assetType = value;
						Changed = true;

						ResolveDuplicateNames();
					}
				}
			}
		}

		private void BtnApply_Click(object sender, EventArgs e)
		{
			assetsDataView.EndEdit();
			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnView_Click(object sender, EventArgs e)
		{
			assetsDataView.EndEdit();

			int selectedIndex = GetSelectedAsset();
			if (selectedIndex == -1)
				return;

			AssetFile asset = Assets[selectedIndex];
			if (asset == null || asset.data.length == 0)
				return;

			string tempPath = Path.Combine(Path.GetTempPath(), "Ginger");
			string filename = Path.Combine(tempPath, string.Format("temp_{0:X8}.{1}", asset.data.bytes.GetHashCode(), asset.ext ?? ""));

			if (File.Exists(filename) == false)
			{
				try
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
				catch
				{
					MessageBox.Show(Resources.error_open_file_in_exporer, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					return;
				}
			}

			LaunchTextEditor.OpenAnyFile(filename);
		}

		private void BtnExport_Click(object sender, EventArgs e)
		{
			assetsDataView.EndEdit();

			int selectedIndex = GetSelectedAsset();
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
			exportFileDialog.InitialDirectory = AppSettings.Paths.LastImportPath ?? AppSettings.Paths.LastImagePath ?? Utility.AppPath("Characters");

			var result = exportFileDialog.ShowDialog();
			if (result != DialogResult.OK || string.IsNullOrWhiteSpace(exportFileDialog.FileName))
				return;

			AppSettings.Paths.LastImportPath = Path.GetDirectoryName(exportFileDialog.FileName);
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
				MessageBox.Show(Resources.error_export_file, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
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
			importFileDialog.Filter = "Image files|*.png;*.apng;*.jpg;*.jpeg;*.webp;*.avif;*.gif|Audio files|*.mp3;*.ogg;*.wav;*.aiff|Video files|*.mp4;*.webm;*.wmv;*.mov;*.mkv|Other files|*.*";
			importFileDialog.InitialDirectory = AppSettings.Paths.LastImportPath ?? AppSettings.Paths.LastImagePath ?? Utility.AppPath("Characters");
			var result = importFileDialog.ShowDialog();
			if (result != DialogResult.OK)
				return;

			AppSettings.Paths.LastImportPath = Path.GetDirectoryName(importFileDialog.FileName);

			_bIgnoreEvents = true;
			foreach (var filename in importFileDialog.FileNames)
			{
				AddAsset(filename);
			}
			_bIgnoreEvents = false;
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

				if (bytes != null && bytes.Length > 0)
				{
					var asset = new AssetFile() {
						name = name,
						ext = ext,
						assetType = AssetFile.AssetType.Undefined,
						data = AssetData.FromBytes(bytes),
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
			var types = new AssetFile.AssetType[] {
				AssetFile.AssetType.Icon,
				AssetFile.AssetType.UserIcon,
				AssetFile.AssetType.Background,
				AssetFile.AssetType.Expression,
				AssetFile.AssetType.Other,
			};

			foreach (var assetType in types)
			{
				var used_names = new Dictionary<string, int>();
				for (int i = 0; i < Assets.Count; ++i)
				{
					var asset = Assets[i];
					if (asset.assetType != assetType || asset.isEmbeddedAsset == false)
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
				if (Assets[i].isEmbeddedAsset == false)
					continue;

				string value = assetsDataView.Rows[row].Cells[0].Value as string;
				if (value != Assets[i].name)
					assetsDataView.Rows[row].Cells[0].Value = Assets[i].name;
				++row;
			}

		}
	}
}
