
namespace Ginger
{
	partial class LinkImportDialog
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
			System.Windows.Forms.FlowLayoutPanel buttonLayout;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinkImportDialog));
			this.assetsDataView = new System.Windows.Forms.DataGridView();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.exportFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.importFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.columnFileExt = new System.Windows.Forms.DataGridViewTextBoxColumn();
			listPanel = new System.Windows.Forms.Panel();
			buttonLayout = new System.Windows.Forms.FlowLayoutPanel();
			listPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.assetsDataView)).BeginInit();
			buttonLayout.SuspendLayout();
			this.SuspendLayout();
			// 
			// listPanel
			// 
			listPanel.Controls.Add(this.assetsDataView);
			listPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			listPanel.Location = new System.Drawing.Point(0, 0);
			listPanel.Name = "listPanel";
			listPanel.Padding = new System.Windows.Forms.Padding(4, 4, 0, 4);
			listPanel.Size = new System.Drawing.Size(515, 316);
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
            this.columnFileExt});
			this.assetsDataView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.assetsDataView.Location = new System.Drawing.Point(4, 4);
			this.assetsDataView.Name = "assetsDataView";
			this.assetsDataView.RowHeadersVisible = false;
			this.assetsDataView.RowHeadersWidth = 32;
			this.assetsDataView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			this.assetsDataView.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.assetsDataView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.assetsDataView.ShowCellErrors = false;
			this.assetsDataView.ShowCellToolTips = false;
			this.assetsDataView.ShowEditingIcon = false;
			this.assetsDataView.ShowRowErrors = false;
			this.assetsDataView.Size = new System.Drawing.Size(511, 308);
			this.assetsDataView.TabIndex = 0;
			// 
			// buttonLayout
			// 
			buttonLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			buttonLayout.Controls.Add(this.btnCancel);
			buttonLayout.Controls.Add(this.btnOk);
			buttonLayout.Dock = System.Windows.Forms.DockStyle.Bottom;
			buttonLayout.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			buttonLayout.Location = new System.Drawing.Point(0, 274);
			buttonLayout.Margin = new System.Windows.Forms.Padding(0);
			buttonLayout.Name = "buttonLayout";
			buttonLayout.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			buttonLayout.Size = new System.Drawing.Size(515, 42);
			buttonLayout.TabIndex = 7;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.Location = new System.Drawing.Point(395, 9);
			this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(117, 30);
			this.btnCancel.TabIndex = 2;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(272, 9);
			this.btnOk.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(117, 30);
			this.btnOk.TabIndex = 1;
			this.btnOk.Text = "Import";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
			// 
			// importFileDialog
			// 
			this.importFileDialog.Multiselect = true;
			// 
			// columnName
			// 
			this.columnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.columnName.HeaderText = "Display Name";
			this.columnName.MinimumWidth = 64;
			this.columnName.Name = "columnName";
			this.columnName.ReadOnly = true;
			this.columnName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// columnFileExt
			// 
			this.columnFileExt.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.columnFileExt.HeaderText = "Real Name";
			this.columnFileExt.MinimumWidth = 60;
			this.columnFileExt.Name = "columnFileExt";
			this.columnFileExt.ReadOnly = true;
			this.columnFileExt.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// LinkImportDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(515, 316);
			this.Controls.Add(buttonLayout);
			this.Controls.Add(listPanel);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "LinkImportDialog";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Import character";
			listPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.assetsDataView)).EndInit();
			buttonLayout.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView assetsDataView;
		private System.Windows.Forms.SaveFileDialog exportFileDialog;
		private System.Windows.Forms.OpenFileDialog importFileDialog;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.DataGridViewTextBoxColumn columnName;
		private System.Windows.Forms.DataGridViewTextBoxColumn columnFileExt;
	}
}