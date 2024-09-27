using System.Windows.Forms;

namespace Ginger
{
	public class DataGridViewEx : DataGridView
	{
		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				var hitTest = this.HitTest(e.X, e.Y);
				if (hitTest != null && hitTest.RowIndex != -1 && hitTest.ColumnIndex != -1)
				{
					CurrentCell = Rows[hitTest.RowIndex].Cells[hitTest.ColumnIndex];
					BeginEdit(true);
					return;
				}
			}
			base.OnMouseDoubleClick(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (IsCurrentCellInEditMode == false)
			{
				if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return || e.KeyCode == Keys.F2)
				{
					BeginEdit(true);
					return;
				}
			}
			base.OnKeyDown(e);
		}
		
		public void SelectRow(int row)
		{
			ClearSelection();
			for (int i = 0; i < RowCount; ++i)
				Rows[i].Selected = i == row;
			if (row >= 0 && row < RowCount)
				CurrentCell = Rows[row].Cells[0];
		}

		public void SelectNextCell()
		{
			EndEdit();
			if (CurrentCell == null)
			{
				SelectRow(0);
				return;
			}

			if (CurrentCell.ColumnIndex < ColumnCount - 1)
			{
				CurrentCell = Rows[CurrentCell.RowIndex].Cells[CurrentCell.ColumnIndex + 1];
				BeginEdit(true);
				return;
			}
			else if (CurrentCell.RowIndex < RowCount - 1)
			{
				SelectRow(CurrentCell.RowIndex + 1);
				BeginEdit(true);
			}
			else
			{
				SelectRow(0);
				BeginEdit(true);
			}

		}
	}
}
