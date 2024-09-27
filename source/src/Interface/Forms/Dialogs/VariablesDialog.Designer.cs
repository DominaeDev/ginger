
namespace Ginger
{
	partial class VariablesDialog
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.Panel listPanel;
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.Panel leftPanel;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VariablesDialog));
			this.dataGridView = new Ginger.DataGridViewEx();
			this.columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.columnValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.btnApply = new Ginger.ButtonEx();
			this.btnCancel = new Ginger.ButtonEx();
			this.btnRemove = new Ginger.ButtonEx();
			this.btnAdd = new Ginger.ButtonEx();
			listPanel = new System.Windows.Forms.Panel();
			leftPanel = new System.Windows.Forms.Panel();
			listPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
			leftPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// listPanel
			// 
			listPanel.Controls.Add(this.dataGridView);
			listPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			listPanel.Location = new System.Drawing.Point(0, 0);
			listPanel.Name = "listPanel";
			listPanel.Padding = new System.Windows.Forms.Padding(4, 4, 0, 4);
			listPanel.Size = new System.Drawing.Size(514, 414);
			listPanel.TabIndex = 0;
			// 
			// dataGridView
			// 
			this.dataGridView.AllowUserToAddRows = false;
			this.dataGridView.AllowUserToDeleteRows = false;
			this.dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.dataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.dataGridView.CausesValidation = false;
			this.dataGridView.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.columnName,
            this.columnValue});
			this.dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataGridView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnKeystroke;
			this.dataGridView.Location = new System.Drawing.Point(4, 4);
			this.dataGridView.Name = "dataGridView";
			this.dataGridView.RowHeadersWidth = 32;
			this.dataGridView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			this.dataGridView.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
			this.dataGridView.ShowCellErrors = false;
			this.dataGridView.ShowCellToolTips = false;
			this.dataGridView.ShowRowErrors = false;
			this.dataGridView.Size = new System.Drawing.Size(510, 406);
			this.dataGridView.TabIndex = 0;
			// 
			// columnName
			// 
			this.columnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.columnName.DefaultCellStyle = dataGridViewCellStyle1;
			this.columnName.HeaderText = "Name";
			this.columnName.MinimumWidth = 100;
			this.columnName.Name = "columnName";
			this.columnName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.columnName.Width = 160;
			// 
			// columnValue
			// 
			this.columnValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.columnValue.HeaderText = "Value";
			this.columnValue.MinimumWidth = 100;
			this.columnValue.Name = "columnValue";
			this.columnValue.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// leftPanel
			// 
			leftPanel.Controls.Add(this.btnApply);
			leftPanel.Controls.Add(this.btnCancel);
			leftPanel.Controls.Add(this.btnRemove);
			leftPanel.Controls.Add(this.btnAdd);
			leftPanel.Dock = System.Windows.Forms.DockStyle.Right;
			leftPanel.Location = new System.Drawing.Point(514, 0);
			leftPanel.Name = "leftPanel";
			leftPanel.Padding = new System.Windows.Forms.Padding(4);
			leftPanel.Size = new System.Drawing.Size(200, 414);
			leftPanel.TabIndex = 0;
			// 
			// btnApply
			// 
			this.btnApply.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.btnApply.Location = new System.Drawing.Point(4, 348);
			this.btnApply.Name = "btnApply";
			this.btnApply.Size = new System.Drawing.Size(192, 31);
			this.btnApply.TabIndex = 5;
			this.btnApply.Text = "Apply";
			this.btnApply.UseVisualStyleBackColor = true;
			// 
			// btnCancel
			// 
			this.btnCancel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.btnCancel.Location = new System.Drawing.Point(4, 379);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(192, 31);
			this.btnCancel.TabIndex = 6;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnRemove
			// 
			this.btnRemove.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnRemove.Location = new System.Drawing.Point(4, 35);
			this.btnRemove.Name = "btnRemove";
			this.btnRemove.Size = new System.Drawing.Size(192, 31);
			this.btnRemove.TabIndex = 2;
			this.btnRemove.Text = "Remove selected";
			this.btnRemove.UseVisualStyleBackColor = true;
			// 
			// btnAdd
			// 
			this.btnAdd.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnAdd.Location = new System.Drawing.Point(4, 4);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Size = new System.Drawing.Size(192, 31);
			this.btnAdd.TabIndex = 0;
			this.btnAdd.Text = "Add variable...";
			this.btnAdd.UseVisualStyleBackColor = true;
			// 
			// VariablesDialog
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(714, 414);
			this.Controls.Add(listPanel);
			this.Controls.Add(leftPanel);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "VariablesDialog";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Custom variables";
			listPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
			leftPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private DataGridViewEx dataGridView;
		private ButtonEx btnRemove;
		private ButtonEx btnAdd;
		private ButtonEx btnApply;
		private System.Windows.Forms.DataGridViewTextBoxColumn columnName;
		private System.Windows.Forms.DataGridViewTextBoxColumn columnValue;
		private ButtonEx btnCancel;
	}
}