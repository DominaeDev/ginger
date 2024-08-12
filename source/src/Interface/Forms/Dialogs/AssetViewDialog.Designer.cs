
namespace Ginger
{
	partial class AssetViewDialog
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
			System.Windows.Forms.Label label_ccv3;
			System.Windows.Forms.Panel spacer_2;
			System.Windows.Forms.Panel spacer_1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AssetViewDialog));
			this.assetsDataView = new System.Windows.Forms.DataGridView();
			this.columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.columnFileExt = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.columnType = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this.columnSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.btnApply = new System.Windows.Forms.Button();
			this.btnExport = new System.Windows.Forms.Button();
			this.btnView = new System.Windows.Forms.Button();
			this.btnRemove = new System.Windows.Forms.Button();
			this.btnAdd = new System.Windows.Forms.Button();
			this.exportFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.importFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.btnAddRemote = new System.Windows.Forms.Button();
			listPanel = new System.Windows.Forms.Panel();
			leftPanel = new System.Windows.Forms.Panel();
			label_ccv3 = new System.Windows.Forms.Label();
			spacer_2 = new System.Windows.Forms.Panel();
			spacer_1 = new System.Windows.Forms.Panel();
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
            this.columnFileExt,
            this.columnType,
            this.columnSize});
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
			// columnName
			// 
			this.columnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.columnName.HeaderText = "Name";
			this.columnName.MinimumWidth = 64;
			this.columnName.Name = "columnName";
			this.columnName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// columnFileExt
			// 
			this.columnFileExt.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.columnFileExt.HeaderText = "Ext.";
			this.columnFileExt.MinimumWidth = 60;
			this.columnFileExt.Name = "columnFileExt";
			this.columnFileExt.ReadOnly = true;
			this.columnFileExt.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.columnFileExt.Width = 60;
			// 
			// columnType
			// 
			this.columnType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.columnType.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
			this.columnType.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.columnType.HeaderText = "Type";
			this.columnType.MinimumWidth = 120;
			this.columnType.Name = "columnType";
			this.columnType.Width = 120;
			// 
			// columnSize
			// 
			this.columnSize.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.columnSize.HeaderText = "Size";
			this.columnSize.MinimumWidth = 90;
			this.columnSize.Name = "columnSize";
			this.columnSize.ReadOnly = true;
			this.columnSize.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.columnSize.Width = 90;
			// 
			// leftPanel
			// 
			leftPanel.Controls.Add(label_ccv3);
			leftPanel.Controls.Add(spacer_2);
			leftPanel.Controls.Add(this.btnApply);
			leftPanel.Controls.Add(this.btnExport);
			leftPanel.Controls.Add(this.btnView);
			leftPanel.Controls.Add(spacer_1);
			leftPanel.Controls.Add(this.btnRemove);
			leftPanel.Controls.Add(this.btnAddRemote);
			leftPanel.Controls.Add(this.btnAdd);
			leftPanel.Dock = System.Windows.Forms.DockStyle.Right;
			leftPanel.Location = new System.Drawing.Point(514, 0);
			leftPanel.Name = "leftPanel";
			leftPanel.Padding = new System.Windows.Forms.Padding(4);
			leftPanel.Size = new System.Drawing.Size(200, 414);
			leftPanel.TabIndex = 0;
			// 
			// label_ccv3
			// 
			label_ccv3.Dock = System.Windows.Forms.DockStyle.Top;
			label_ccv3.ForeColor = System.Drawing.SystemColors.GrayText;
			label_ccv3.Location = new System.Drawing.Point(4, 183);
			label_ccv3.Name = "label_ccv3";
			label_ccv3.Padding = new System.Windows.Forms.Padding(3);
			label_ccv3.Size = new System.Drawing.Size(192, 52);
			label_ccv3.TabIndex = 7;
			label_ccv3.Text = "Note: Embedded assets are only supported in CCV3 and CHARX cards.";
			label_ccv3.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// spacer_2
			// 
			spacer_2.Dock = System.Windows.Forms.DockStyle.Top;
			spacer_2.Location = new System.Drawing.Point(4, 171);
			spacer_2.Name = "spacer_2";
			spacer_2.Size = new System.Drawing.Size(192, 12);
			spacer_2.TabIndex = 4;
			// 
			// btnApply
			// 
			this.btnApply.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.btnApply.Location = new System.Drawing.Point(4, 379);
			this.btnApply.Name = "btnApply";
			this.btnApply.Size = new System.Drawing.Size(192, 31);
			this.btnApply.TabIndex = 5;
			this.btnApply.Text = "Close";
			this.btnApply.UseVisualStyleBackColor = true;
			// 
			// btnExport
			// 
			this.btnExport.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnExport.Location = new System.Drawing.Point(4, 140);
			this.btnExport.Name = "btnExport";
			this.btnExport.Size = new System.Drawing.Size(192, 31);
			this.btnExport.TabIndex = 4;
			this.btnExport.Text = "Export selected";
			this.btnExport.UseVisualStyleBackColor = true;
			// 
			// btnView
			// 
			this.btnView.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnView.Location = new System.Drawing.Point(4, 109);
			this.btnView.Name = "btnView";
			this.btnView.Size = new System.Drawing.Size(192, 31);
			this.btnView.TabIndex = 3;
			this.btnView.Text = "View selected";
			this.btnView.UseVisualStyleBackColor = true;
			// 
			// spacer_1
			// 
			spacer_1.Dock = System.Windows.Forms.DockStyle.Top;
			spacer_1.Location = new System.Drawing.Point(4, 97);
			spacer_1.Name = "spacer_1";
			spacer_1.Size = new System.Drawing.Size(192, 12);
			spacer_1.TabIndex = 3;
			// 
			// btnRemove
			// 
			this.btnRemove.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnRemove.Location = new System.Drawing.Point(4, 66);
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
			this.btnAdd.Text = "Add asset...";
			this.btnAdd.UseVisualStyleBackColor = true;
			// 
			// importFileDialog
			// 
			this.importFileDialog.Multiselect = true;
			// 
			// btnAddUrl
			// 
			this.btnAddRemote.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnAddRemote.Location = new System.Drawing.Point(4, 35);
			this.btnAddRemote.Name = "btnAddUrl";
			this.btnAddRemote.Size = new System.Drawing.Size(192, 31);
			this.btnAddRemote.TabIndex = 1;
			this.btnAddRemote.Text = "Add remote asset...";
			this.btnAddRemote.UseVisualStyleBackColor = true;
			// 
			// AssetViewDialog
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
			this.Name = "AssetViewDialog";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Embedded assets";
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
		private System.Windows.Forms.Button btnExport;
		private System.Windows.Forms.Button btnView;
		private System.Windows.Forms.SaveFileDialog exportFileDialog;
		private System.Windows.Forms.OpenFileDialog importFileDialog;
		private System.Windows.Forms.DataGridViewTextBoxColumn columnName;
		private System.Windows.Forms.DataGridViewTextBoxColumn columnFileExt;
		private System.Windows.Forms.DataGridViewComboBoxColumn columnType;
		private System.Windows.Forms.DataGridViewTextBoxColumn columnSize;
		private System.Windows.Forms.Button btnAddRemote;
	}
}