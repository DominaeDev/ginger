
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
			System.Windows.Forms.Panel leftPanel;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VariablesDialog));
			this.assetsDataView = new System.Windows.Forms.DataGridView();
			this.btnApply = new System.Windows.Forms.Button();
			this.btnRemove = new System.Windows.Forms.Button();
			this.btnAdd = new System.Windows.Forms.Button();
			this.exportFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.importFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.columnValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
			listPanel = new System.Windows.Forms.Panel();
			leftPanel = new System.Windows.Forms.Panel();
			listPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.assetsDataView)).BeginInit();
			leftPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// listPanel
			// 
			listPanel.Controls.Add(this.assetsDataView);
			listPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			listPanel.Location = new System.Drawing.Point(0, 0);
			listPanel.Name = "listPanel";
			listPanel.Padding = new System.Windows.Forms.Padding(4, 4, 0, 4);
			listPanel.Size = new System.Drawing.Size(514, 414);
			listPanel.TabIndex = 0;
			// 
			// assetsDataView
			// 
			this.assetsDataView.AllowUserToAddRows = false;
			this.assetsDataView.AllowUserToDeleteRows = false;
			this.assetsDataView.AllowUserToResizeColumns = false;
			this.assetsDataView.AllowUserToResizeRows = false;
			this.assetsDataView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.assetsDataView.CausesValidation = false;
			this.assetsDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.assetsDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.columnName,
            this.columnValue});
			this.assetsDataView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.assetsDataView.Location = new System.Drawing.Point(4, 4);
			this.assetsDataView.Name = "assetsDataView";
			this.assetsDataView.RowHeadersWidth = 32;
			this.assetsDataView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			this.assetsDataView.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.assetsDataView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.assetsDataView.ShowCellErrors = false;
			this.assetsDataView.ShowCellToolTips = false;
			this.assetsDataView.ShowRowErrors = false;
			this.assetsDataView.Size = new System.Drawing.Size(510, 406);
			this.assetsDataView.TabIndex = 0;
			// 
			// leftPanel
			// 
			leftPanel.Controls.Add(this.btnApply);
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
			this.btnApply.Location = new System.Drawing.Point(4, 379);
			this.btnApply.Name = "btnApply";
			this.btnApply.Size = new System.Drawing.Size(192, 31);
			this.btnApply.TabIndex = 5;
			this.btnApply.Text = "Apply";
			this.btnApply.UseVisualStyleBackColor = true;
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
			// importFileDialog
			// 
			this.importFileDialog.Multiselect = true;
			// 
			// columnName
			// 
			this.columnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.columnName.HeaderText = "Name";
			this.columnName.MinimumWidth = 100;
			this.columnName.Name = "columnName";
			this.columnName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.columnName.Width = 206;
			// 
			// columnValue
			// 
			this.columnValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.columnValue.HeaderText = "Value";
			this.columnValue.MinimumWidth = 100;
			this.columnValue.Name = "columnValue";
			this.columnValue.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
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
			((System.ComponentModel.ISupportInitialize)(this.assetsDataView)).EndInit();
			leftPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView assetsDataView;
		private System.Windows.Forms.Button btnRemove;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.Button btnApply;
		private System.Windows.Forms.SaveFileDialog exportFileDialog;
		private System.Windows.Forms.OpenFileDialog importFileDialog;
		private System.Windows.Forms.DataGridViewTextBoxColumn columnName;
		private System.Windows.Forms.DataGridViewTextBoxColumn columnValue;
	}
}