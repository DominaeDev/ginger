using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public partial class RearrangeActorsDialog : FormEx
	{
		public int[] NewOrder { get; private set; }
		public bool Changed { get; private set; }

		private bool _bIgnoreEvents = false;
		private struct Item
		{
			public int index;
			public string label;

			public override string ToString()
			{
				return label;
			}
		}

		public RearrangeActorsDialog()
		{
			InitializeComponent();

			this.Load += AssetViewDialog_Load;
		}

		private void AssetViewDialog_Load(object sender, EventArgs e)
		{
			_bIgnoreEvents = true;
			lbActors.Items.Clear();
			for (int i = 0; i < Current.Characters.Count; ++i)
			{
				lbActors.Items.Add(new Item() {
					index = i,
					label = Current.Characters[i].spokenName,
				});
			}

			lbActors.SelectedIndex = -1;
			btnMoveUp.Enabled = false;
			btnMoveDown.Enabled = false;
			_bIgnoreEvents = false;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				btnCancel_Click(this, EventArgs.Empty);
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void lbActors_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int index = lbActors.SelectedIndex;
			btnMoveUp.Enabled = index > 0;
			btnMoveDown.Enabled = index >= 0 && index < lbActors.Items.Count - 1;
		}

		private void btnMoveUp_Click(object sender, EventArgs e)
		{
			int index = lbActors.SelectedIndex;
			if (index <= 0)
				return;

			_bIgnoreEvents = true;
			var item = lbActors.Items[index];
			lbActors.Items.RemoveAt(index);
			lbActors.Items.Insert(index - 1, item);
			_bIgnoreEvents = false;

			lbActors.SelectedIndex = index - 1;
		}

		private void btnMoveDown_Click(object sender, EventArgs e)
		{
			int index = lbActors.SelectedIndex;
			if (index < 0 || index >= lbActors.Items.Count)
				return;

			_bIgnoreEvents = true;
			var item = lbActors.Items[index];
			lbActors.Items.RemoveAt(index);
			lbActors.Items.Insert(index + 1, item);
			_bIgnoreEvents = false;

			lbActors.SelectedIndex = index + 1;
		}

		private void btnConfirm_Click(object sender, EventArgs e)
		{
			NewOrder = new int[lbActors.Items.Count];
			for (int i = 0; i < lbActors.Items.Count; ++i)
			{
				NewOrder[i] = ((Item)lbActors.Items[i]).index;
				Changed |= NewOrder[i] != i;
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
				
		public override void ApplyTheme()
		{
			base.ApplyTheme();

			this.Suspend();
			lbActors.ForeColor = Theme.Current.TextBoxForeground;
			lbActors.BackColor = Theme.IsDarkModeEnabled ? Theme.Current.TextBoxBackground : Color.WhiteSmoke;
			lbActors.Invalidate();
			this.Resume();
		}

	}
}
