using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Ginger
{
	public partial class VariablesDialog : FormEx
	{
		public List<CustomVariable> Variables;
		public bool Changed = false;

		private string _editVarName = null;
		private bool _bEditing = false;

		public VariablesDialog()
		{
			InitializeComponent();

			this.Load += VariablesDialog_Load;
			this.FormClosing += VariablesDialog_FormClosing;

			btnAdd.Click += BtnAdd_Click;
			btnRemove.Click += BtnRemove_Click;
			btnApply.Click += BtnApply_Click;
			btnCancel.Click += BtnCancel_Click;

			dataGridView.CellBeginEdit += DataGridView_CellBeginEdit;
			dataGridView.CellEndEdit += DataGridView_CellEndEdit;
			dataGridView.SelectionChanged += DataGridView_SelectionChanged;
		}

		private void VariablesDialog_Load(object sender, EventArgs e)
		{
			PopulateTable();
			dataGridView.ClearSelection();
		}

		private void VariablesDialog_FormClosing(object sender, FormClosingEventArgs e)
		{
			dataGridView.EndEdit();
			if (DialogResult == DialogResult.Cancel && Changed)
			{
				var mr = MessageBox.Show(Resources.msg_dismiss_changes, Resources.cap_confirm, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
				if (mr == DialogResult.No)
					e.Cancel = true;
			}
		}

		private void DataGridView_SelectionChanged(object sender, EventArgs e)
		{
			btnRemove.Enabled = dataGridView.SelectedRows.Count > 0;
		}

		private void PopulateTable()
		{
			dataGridView.EndEdit();
			dataGridView.ClearSelection();
			dataGridView.Rows.Clear();

			foreach (var variable in Variables)
				dataGridView.Rows.Add(string.Concat("$", variable.Name), variable.Value);
		}


		private void DataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
		{
			_bEditing = true;
			if (e.ColumnIndex == 0)
			{
				string value = _editVarName = dataGridView.CurrentCell.Value as string ?? "";
				if (value.BeginsWith("$"))
					dataGridView.CurrentCell.Value = value.Substring(1, value.Length - 1);
			}
		}

		private void DataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			_bEditing = false;
			if (e.ColumnIndex == 0) // Name
			{
				string varName = (dataGridView.CurrentCell.EditedFormattedValue as string ?? "").Trim();
				if (varName.BeginsWith("$"))
					varName = varName.Substring(1);
				varName = new CustomVariableName(varName).ToString();
				if (string.IsNullOrEmpty(varName))
				{
					dataGridView.CurrentCell.Value = _editVarName;
					return;
				}

				dataGridView.CurrentCell.Value = string.Concat("$", varName);
				Variables[e.RowIndex] = new CustomVariable(varName, Variables[e.RowIndex].Value);
				ResolveDuplicateNames();
				Changed = true;
			}
			else // Value
			{
				Variables[e.RowIndex] = new CustomVariable(Variables[e.RowIndex].Name,(dataGridView.CurrentCell.EditedFormattedValue as string ?? "").Trim());
				Changed = true;
			}
		}

		private void BtnApply_Click(object sender, EventArgs e)
		{
			dataGridView.EndEdit();
			Variables = Variables
				.Where(v => string.IsNullOrWhiteSpace(v.Name.ToString()) == false)
				.ToList();
			DialogResult = DialogResult.OK;
			Close();
		}
		
		private void BtnCancel_Click(object sender, EventArgs e)
		{
			dataGridView.EndEdit();
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void BtnRemove_Click(object sender, EventArgs e)
		{
			dataGridView.EndEdit();

			List<int> removeRows = new List<int>();
			for (int i = dataGridView.RowCount - 1; i >= 0; --i)
			{
				if (dataGridView.Rows[i].Selected)
					removeRows.Add(i);
			}

			for (int i = 0; i < removeRows.Count; ++i)
			{
				dataGridView.Rows.RemoveAt(removeRows[i]);
				Variables.RemoveAt(removeRows[i]);
			}

			dataGridView.ClearSelection();
			Changed = true;
		}

		private void BtnAdd_Click(object sender, EventArgs e)
		{
			dataGridView.EndEdit();

			if (dataGridView.RowCount > 0 
				&& string.IsNullOrEmpty(dataGridView.Rows[dataGridView.RowCount - 1].Cells[0].Value as string)
				&& string.IsNullOrEmpty(dataGridView.Rows[dataGridView.RowCount - 1].Cells[1].Value as string))
			{
				// Edit bottom empty row
				dataGridView.SelectRow(dataGridView.Rows.Count - 1);
				dataGridView.BeginEdit(true);
				return;
			}

			// Add new row
			dataGridView.Rows.Add("", "");
			Variables.Add(new CustomVariable("", ""));
			dataGridView.SelectRow(dataGridView.Rows.Count - 1);
			dataGridView.BeginEdit(true);
			Changed = true;
		}

		public override void ApplyTheme()
		{
			base.ApplyTheme();

			Theme.Apply(dataGridView);

			dataGridView.Columns[0].DefaultCellStyle.WrapMode = DataGridViewTriState.False;
			dataGridView.Columns[1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
		}

		private void ResolveDuplicateNames()
		{
			var used_names = new Dictionary<string, int>();

			for (int i = 0; i < Variables.Count; ++i)
			{
				string name = Variables[i].Name;
				if (name == "")
					continue;

				if (used_names.ContainsKey(name.ToLower()) == false)
				{
					used_names.Add(name.ToLower(), 1);
					continue;
				}

				int count = used_names[name.ToLower()];
				string testName = string.Format("{0}{1}", name, ++count);
				while (used_names.ContainsKey(testName.ToLower()))
					testName = string.Format("{0}{1}", name, ++count);
				used_names.Add(testName.ToLower(), 1);
				used_names[name] = count;
				Variables[i] = new CustomVariable(testName, Variables[i].Value);
			}

			// Refresh data table
			int row = 0;
			for (int i = 0; i < Variables.Count; ++i)
			{
				var variable = Variables[i];
				var varName = string.Concat("$", variable.Name);

				string value = dataGridView.Rows[row].Cells[0].Value as string;
				if (value != varName)
					dataGridView.Rows[row].Cells[0].Value = varName;
				++row;
			}
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
