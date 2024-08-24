using System;
using System.Linq;
using System.Windows.Forms;

namespace Ginger
{
	public partial class LinkImportDialog : Form
	{
		public FaradayBridge.CharacterInstance[] Characters;
		public FaradayBridge.CharacterInstance CharacterInstance;

		public LinkImportDialog()
		{
			InitializeComponent();

			this.Load += OnLoad;
		}

		private void OnLoad(object sender, EventArgs e)
		{
			assetsDataView.SelectionChanged += AssetsDataView_SelectionChanged;

			PopulateTable();
			assetsDataView.ClearSelection();

			btnOk.Enabled = false;
		}

		private void AssetsDataView_SelectionChanged(object sender, EventArgs e)
		{
			btnOk.Enabled = assetsDataView.SelectedRows.Count > 0;
		}

		private void PopulateTable()
		{
			assetsDataView.Rows.Clear();
			if (Characters != null)
			{
				foreach (var character in Characters.OrderByDescending(c => c.updateDate))
					AddRowForAsset(character);
			}
		}

		private void AddRowForAsset(FaradayBridge.CharacterInstance character)
		{
			assetsDataView.Rows.Add(character.displayName, character.name);
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			int index = -1;
			if (assetsDataView.SelectedRows.Count == 1)
				index = assetsDataView.SelectedRows[0].Index;
			if (index >= 0 && index < Characters.Length)
				CharacterInstance = Characters[index];
			else
				CharacterInstance = default(FaradayBridge.CharacterInstance);

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
