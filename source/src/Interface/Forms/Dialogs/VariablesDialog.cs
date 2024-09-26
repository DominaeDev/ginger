using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Ginger
{
	public partial class VariablesDialog : FormEx
	{
		public Dictionary<string, string> Variables;
		public bool Changed = false;
		private bool _bIgnoreEvents = false;

		public VariablesDialog()
		{
			InitializeComponent();

			this.Load += AssetViewDialog_Load;
			this.FormClosing += AssetViewDialog_FormClosing;

			btnAdd.Click += BtnAdd_Click;
			btnRemove.Click += BtnRemove_Click;
			btnApply.Click += BtnApply_Click;


			assetsDataView.EditingControlShowing += AssetsDataView_EditingControlShowing;
			assetsDataView.DataError += AssetsDataView_DataError;
		}

		private void AssetViewDialog_Load(object sender, EventArgs e)
		{
			assetsDataView.CellEndEdit += AssetsDataView_CellEndEdit;
			assetsDataView.SelectionChanged += AssetsDataView_SelectionChanged;

			Variables = new Dictionary<string, string>(Current.Card.Variables);

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
		}

		private void AssetsDataView_DataError(object sender, DataGridViewDataErrorEventArgs e)
		{
		}

		private void AssetsDataView_SelectionChanged(object sender, EventArgs e)
		{
			btnRemove.Enabled = assetsDataView.SelectedRows.Count > 0;
		}

		private void PopulateTable()
		{
			_bIgnoreEvents = true;
			assetsDataView.Rows.Clear();

			foreach (var kvp in Variables)
				AddRow(kvp.Key, kvp.Value);
			_bIgnoreEvents = false;
		}

		private void AddRow(string name, string value)
		{
			assetsDataView.Rows.Add(name, value);
		}

		private void AssetsDataView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			if (_bIgnoreEvents)
				return;
		}

		private void BtnApply_Click(object sender, EventArgs e)
		{
			assetsDataView.EndEdit();
			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnRemove_Click(object sender, EventArgs e)
		{
			assetsDataView.EndEdit();

			_bIgnoreEvents = true;

			// ...

			assetsDataView.ClearSelection();
			_bIgnoreEvents = false;
		}

		private void BtnAdd_Click(object sender, EventArgs e)
		{
			assetsDataView.EndEdit();

			_bIgnoreEvents = true;
			// ...

			_bIgnoreEvents = false;
		}

		public override void ApplyTheme()
		{
			base.ApplyTheme();

			Theme.Apply(assetsDataView);
		}

		private void ResolveDuplicateNames()
		{
			/*
			var types = Assets.Select(a => a.type).Distinct();

			foreach (var type in types)
			{
				var assetType = AssetFile.AssetTypeFromString(type);
				var used_names = new Dictionary<string, int>();
				if (assetType == AssetFile.AssetType.Icon && Current.Card.portraitImage != null)
					used_names.Add("main", 1); // Reserve name for main portrait

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
			}*/
		}
	
	}
}
