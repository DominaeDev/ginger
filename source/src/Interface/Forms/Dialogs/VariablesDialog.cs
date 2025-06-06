﻿using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Ginger
{
	public partial class VariablesDialog : FormEx
	{
		public List<CustomVariable> Variables;
		public CustomVariableName EditOnLoad;
		public bool Changed = false;

		private string _previousCellValue = null;

		public VariablesDialog()
		{
			InitializeComponent();

			this.Load += VariablesDialog_Load;
			this.FormClosing += VariablesDialog_FormClosing;
			this.Shown += VariablesDialog_Shown;

			btnAdd.Click += BtnAdd_Click;
			btnRemove.Click += BtnRemove_Click;
			btnApply.Click += BtnApply_Click;
			btnCancel.Click += BtnCancel_Click;

			dataGridView.CellBeginEdit += DataGridView_CellBeginEdit;
			dataGridView.CellEndEdit += DataGridView_CellEndEdit;
			dataGridView.SelectionChanged += DataGridView_SelectionChanged;
			dataGridView.MouseClick += DataGridView_MouseClick;
		}

		private void VariablesDialog_Shown(object sender, EventArgs e)
		{
			if (EditOnLoad.IsEmpty == false)
			{
				int index = Variables.FindIndex(v => string.Compare(v.Name, EditOnLoad, StringComparison.OrdinalIgnoreCase) == 0);
				if (index >= 0 && index < dataGridView.RowCount)
				{
					dataGridView.Select();
					dataGridView.SelectRow(index);
					dataGridView.CurrentCell = dataGridView.CurrentRow.Cells[1];
					dataGridView.BeginEdit(true);
				}
			}
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
				var mr = MsgBox.AskYesNoCancel(Resources.msg_apply_changes, Resources.cap_confirm, this);
				if (mr == DialogResult.Cancel)
					e.Cancel = true;
				else if (mr == DialogResult.Yes)
					DialogResult = DialogResult.OK;
			}
		}

		private void PopulateTable()
		{
			dataGridView.EndEdit();
			dataGridView.ClearSelection();
			dataGridView.Rows.Clear();

			foreach (var variable in Variables)
				dataGridView.Rows.Add(variable.Name, variable.Value);
		}

		private List<int> GetSelectedVariables()
		{
			var selected = new List<int>();
			for (int i = 0; i < dataGridView.RowCount; ++i)
			{
				if (dataGridView.Rows[i].Cells[0].Selected && dataGridView.Rows[i].Cells[1].Selected)
					selected.Add(i);
			}
			return selected;
		}

		private void DataGridView_SelectionChanged(object sender, EventArgs e)
		{
			btnRemove.Enabled = GetSelectedVariables().Count > 0;
		}

		private void DataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
		{
			_previousCellValue = dataGridView.CurrentCell.Value as string ?? "";
			if (e.ColumnIndex == 0)
			{
				string value = _previousCellValue;
				if (value.BeginsWith("$")) // Remove prefix
					dataGridView.CurrentCell.Value = value.Substring(1, value.Length - 1);
			}
		}

		private void DataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			if (e.ColumnIndex == 0) // Name
			{
				string varName = (dataGridView.CurrentCell.EditedFormattedValue as string ?? "").Trim();
				varName = new CustomVariableName(varName).ToString();
				
				if (string.IsNullOrEmpty(varName))
				{
					dataGridView.CurrentCell.Value = _previousCellValue;
					return;
				}

				dataGridView.CurrentCell.Value = varName;
				Variables[e.RowIndex] = new CustomVariable(varName, Variables[e.RowIndex].Value);
				ResolveDuplicateNames();
				Changed |= (string)dataGridView.CurrentCell.Value != _previousCellValue;
			}
			else // Value
			{
				Variables[e.RowIndex] = new CustomVariable(Variables[e.RowIndex].Name,(dataGridView.CurrentCell.EditedFormattedValue as string ?? "").Trim());
				Changed |= (string)dataGridView.CurrentCell.Value != _previousCellValue;
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

		private void BtnAdd_Click(object sender, EventArgs e)
		{
			dataGridView.EndEdit();

			if (dataGridView.RowCount > 0 
				&& string.IsNullOrEmpty(dataGridView.Rows[dataGridView.RowCount - 1].Cells[0].Value as string)
				&& string.IsNullOrEmpty(dataGridView.Rows[dataGridView.RowCount - 1].Cells[1].Value as string))
			{
				// Edit bottom empty row
				dataGridView.SelectRow(dataGridView.Rows.Count - 1);
				dataGridView.BeginEdit(false);
				return;
			}

			// Add new row
			dataGridView.Rows.Add("", "");
			Variables.Add(new CustomVariable("", ""));
			dataGridView.SelectRow(dataGridView.Rows.Count - 1);
			dataGridView.BeginEdit(false);
			Changed = true;
		}
		
		private void BtnRemove_Click(object sender, EventArgs e)
		{
			dataGridView.EndEdit();

			List<int> removeRows = GetSelectedVariables();
			if (removeRows.Count == 0)
			{
				ClearCells();
				return;
			}

			for (int i = removeRows.Count - 1; i >= 0; --i)
			{
				dataGridView.Rows.RemoveAt(removeRows[i]);
				Variables.RemoveAt(removeRows[i]);
			}

			dataGridView.ClearSelection();
			Changed = true;
		}

		private void ClearCells()
		{
			if (dataGridView.SelectedCells.Count == 0)
				return;

			foreach (DataGridViewCell cell in dataGridView.SelectedCells)
			{
				if (cell.ColumnIndex == 1)
				{
					Variables[cell.RowIndex] = new CustomVariable() {
						Name = Variables[cell.RowIndex].Name,
						Value = "",
					};
					cell.Value = null;
				}
			}

			dataGridView.ClearSelection();
			Changed = true;
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
				string varName = variable.Name;
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
				if (dataGridView.IsCurrentCellInEditMode == false)
				{
					DialogResult = DialogResult.Cancel;
					Close();
					return true;
				}
			}
			else if (keyData == ShortcutKeys.Paste && dataGridView.IsCurrentCellInEditMode == false)
			{
				PasteVariables();
				return true;
			}
			else if (keyData == Keys.Delete && dataGridView.IsCurrentCellInEditMode == false)
			{
				BtnRemove_Click(this, EventArgs.Empty);
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void PasteVariables()
		{
			if (Clipboard.ContainsText() == false)
				return;

			string text = Clipboard.GetText();
			string[] rows = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
			var variables = new List<CustomVariable>();
			foreach (var row in rows)
			{
				if (row.BeginsWith("$") == false)
					continue;
				int pos_delim = row.IndexOf('\t');
				if (pos_delim == -1)
					continue;
				
				string name = row.Substring(1, pos_delim - 1).Trim();
				string value = row.Substring(pos_delim + 1).Trim();
				if (string.IsNullOrEmpty(name) == false)
					variables.Add(new CustomVariable(name, value));
			}

			dataGridView.ClearSelection();
			foreach (var variable in variables)
			{
				Variables.Add(variable);
				dataGridView.Rows.Add(variable.Name, variable.Value);
				dataGridView.Rows[dataGridView.RowCount - 1].Selected = true;
			}
			ResolveDuplicateNames();
		}

		public override void ApplyTheme()
		{
			base.ApplyTheme();

			Theme.Apply(dataGridView);

			dataGridView.Columns[0].DefaultCellStyle.WrapMode = DataGridViewTriState.False;
			dataGridView.Columns[1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
		}	

		private void DataGridView_MouseClick(object sender, MouseEventArgs args)
		{
			dataGridView.EndEdit();

			bool bCanCopy = GetSelectedVariables().Count > 0;
			bool bCanPaste = false;
			if (Clipboard.ContainsText())
			{
				string text = Clipboard.GetText();
				string[] rows = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
				var variables = new List<CustomVariable>();
				foreach (var row in rows)
				{
					if (row.BeginsWith("$") && row.IndexOf('\t') != -1)
					{
						bCanPaste = true;
						break;
					}
				}
			}

			if (args.Button == MouseButtons.Right)
			{
				var menu = new ContextMenuStrip();
				menu.Items.Add(new ToolStripMenuItem("Copy", null, (s, e) => {
					Clipboard.SetDataObject(dataGridView.GetClipboardContent(), true);
				}) {
					Enabled = bCanCopy,
				});
				menu.Items.Add(new ToolStripMenuItem("Paste", null, (s, e) => {
					PasteVariables();
				}) {
					Enabled = bCanPaste,
				});

				Theme.Apply(menu);
				menu.Show(sender as Control, args.Location);
			}
		}		
	}
}
