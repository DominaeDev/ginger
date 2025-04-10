﻿
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
			this.btnApply = new Ginger.ButtonEx();
			this.btnCancel = new Ginger.ButtonEx();
			this.btnRemove = new Ginger.ButtonEx();
			this.btnExport = new Ginger.ButtonEx();
			this.btnView = new Ginger.ButtonEx();
			this.btnAddRemote = new Ginger.ButtonEx();
			this.btnAdd = new Ginger.ButtonEx();
			this.btnAddBackground = new Ginger.ButtonEx();
			this.btnAddUserIcon = new Ginger.ButtonEx();
			this.btnAddIcon = new Ginger.ButtonEx();
			this.exportFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.importFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.columnFileExt = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.columnType = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this.columnSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.columnTag = new System.Windows.Forms.DataGridViewImageColumn();
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
			listPanel.Size = new System.Drawing.Size(514, 411);
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
            this.columnSize,
            this.columnTag});
			this.assetsDataView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.assetsDataView.Location = new System.Drawing.Point(4, 4);
			this.assetsDataView.Name = "assetsDataView";
			this.assetsDataView.RowHeadersWidth = 32;
			this.assetsDataView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			this.assetsDataView.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.assetsDataView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.assetsDataView.ShowCellErrors = false;
			this.assetsDataView.ShowRowErrors = false;
			this.assetsDataView.Size = new System.Drawing.Size(510, 403);
			this.assetsDataView.TabIndex = 0;
			// 
			// leftPanel
			// 
			leftPanel.Controls.Add(label_ccv3);
			leftPanel.Controls.Add(spacer_2);
			leftPanel.Controls.Add(this.btnApply);
			leftPanel.Controls.Add(this.btnCancel);
			leftPanel.Controls.Add(this.btnRemove);
			leftPanel.Controls.Add(this.btnExport);
			leftPanel.Controls.Add(this.btnView);
			leftPanel.Controls.Add(spacer_1);
			leftPanel.Controls.Add(this.btnAddRemote);
			leftPanel.Controls.Add(this.btnAdd);
			leftPanel.Controls.Add(this.btnAddBackground);
			leftPanel.Controls.Add(this.btnAddUserIcon);
			leftPanel.Controls.Add(this.btnAddIcon);
			leftPanel.Dock = System.Windows.Forms.DockStyle.Right;
			leftPanel.Location = new System.Drawing.Point(514, 0);
			leftPanel.Name = "leftPanel";
			leftPanel.Padding = new System.Windows.Forms.Padding(4);
			leftPanel.Size = new System.Drawing.Size(200, 411);
			leftPanel.TabIndex = 0;
			// 
			// label_ccv3
			// 
			label_ccv3.Dock = System.Windows.Forms.DockStyle.Top;
			label_ccv3.ForeColor = System.Drawing.SystemColors.GrayText;
			label_ccv3.Location = new System.Drawing.Point(4, 276);
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
			spacer_2.Location = new System.Drawing.Point(4, 264);
			spacer_2.Name = "spacer_2";
			spacer_2.Size = new System.Drawing.Size(192, 12);
			spacer_2.TabIndex = 4;
			// 
			// btnApply
			// 
			this.btnApply.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.btnApply.Highlighted = false;
			this.btnApply.Location = new System.Drawing.Point(4, 345);
			this.btnApply.Name = "btnApply";
			this.btnApply.Size = new System.Drawing.Size(192, 31);
			this.btnApply.TabIndex = 5;
			this.btnApply.Text = "Apply";
			this.btnApply.UseVisualStyleBackColor = true;
			// 
			// btnCancel
			// 
			this.btnCancel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.btnCancel.Highlighted = false;
			this.btnCancel.Location = new System.Drawing.Point(4, 376);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(192, 31);
			this.btnCancel.TabIndex = 8;
			this.btnCancel.Text = "Discard";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnRemove
			// 
			this.btnRemove.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnRemove.Highlighted = false;
			this.btnRemove.Location = new System.Drawing.Point(4, 233);
			this.btnRemove.Name = "btnRemove";
			this.btnRemove.Size = new System.Drawing.Size(192, 31);
			this.btnRemove.TabIndex = 2;
			this.btnRemove.Text = "Remove selected";
			this.btnRemove.UseVisualStyleBackColor = true;
			// 
			// btnExport
			// 
			this.btnExport.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnExport.Highlighted = false;
			this.btnExport.Location = new System.Drawing.Point(4, 202);
			this.btnExport.Name = "btnExport";
			this.btnExport.Size = new System.Drawing.Size(192, 31);
			this.btnExport.TabIndex = 4;
			this.btnExport.Text = "Export selected";
			this.btnExport.UseVisualStyleBackColor = true;
			// 
			// btnView
			// 
			this.btnView.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnView.Highlighted = false;
			this.btnView.Location = new System.Drawing.Point(4, 171);
			this.btnView.Name = "btnView";
			this.btnView.Size = new System.Drawing.Size(192, 31);
			this.btnView.TabIndex = 3;
			this.btnView.Text = "View selected";
			this.btnView.UseVisualStyleBackColor = true;
			// 
			// spacer_1
			// 
			spacer_1.Dock = System.Windows.Forms.DockStyle.Top;
			spacer_1.Location = new System.Drawing.Point(4, 159);
			spacer_1.Name = "spacer_1";
			spacer_1.Size = new System.Drawing.Size(192, 12);
			spacer_1.TabIndex = 3;
			// 
			// btnAddRemote
			// 
			this.btnAddRemote.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnAddRemote.Highlighted = false;
			this.btnAddRemote.Location = new System.Drawing.Point(4, 128);
			this.btnAddRemote.Name = "btnAddRemote";
			this.btnAddRemote.Size = new System.Drawing.Size(192, 31);
			this.btnAddRemote.TabIndex = 1;
			this.btnAddRemote.Text = "Add remote asset...";
			this.btnAddRemote.UseVisualStyleBackColor = true;
			// 
			// btnAdd
			// 
			this.btnAdd.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnAdd.Highlighted = false;
			this.btnAdd.Location = new System.Drawing.Point(4, 97);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Size = new System.Drawing.Size(192, 31);
			this.btnAdd.TabIndex = 0;
			this.btnAdd.Text = "Add file...";
			this.btnAdd.UseVisualStyleBackColor = true;
			// 
			// btnAddBackground
			// 
			this.btnAddBackground.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnAddBackground.Highlighted = false;
			this.btnAddBackground.Location = new System.Drawing.Point(4, 66);
			this.btnAddBackground.Name = "btnAddBackground";
			this.btnAddBackground.Size = new System.Drawing.Size(192, 31);
			this.btnAddBackground.TabIndex = 10;
			this.btnAddBackground.Text = "Add background image...";
			this.btnAddBackground.UseVisualStyleBackColor = true;
			// 
			// btnAddUserIcon
			// 
			this.btnAddUserIcon.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnAddUserIcon.Highlighted = false;
			this.btnAddUserIcon.Location = new System.Drawing.Point(4, 35);
			this.btnAddUserIcon.Name = "btnAddUserIcon";
			this.btnAddUserIcon.Size = new System.Drawing.Size(192, 31);
			this.btnAddUserIcon.TabIndex = 9;
			this.btnAddUserIcon.Text = "Add user portrait...";
			this.btnAddUserIcon.UseVisualStyleBackColor = true;
			// 
			// btnAddIcon
			// 
			this.btnAddIcon.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnAddIcon.Highlighted = false;
			this.btnAddIcon.Location = new System.Drawing.Point(4, 4);
			this.btnAddIcon.Name = "btnAddIcon";
			this.btnAddIcon.Size = new System.Drawing.Size(192, 31);
			this.btnAddIcon.TabIndex = 11;
			this.btnAddIcon.Text = "Add portrait...";
			this.btnAddIcon.UseVisualStyleBackColor = true;
			// 
			// importFileDialog
			// 
			this.importFileDialog.Multiselect = true;
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
			this.columnSize.MinimumWidth = 72;
			this.columnSize.Name = "columnSize";
			this.columnSize.ReadOnly = true;
			this.columnSize.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.columnSize.Width = 72;
			// 
			// columnTag
			// 
			this.columnTag.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.columnTag.HeaderText = "";
			this.columnTag.MinimumWidth = 25;
			this.columnTag.Name = "columnTag";
			this.columnTag.ReadOnly = true;
			this.columnTag.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.columnTag.Width = 25;
			// 
			// AssetViewDialog
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(714, 411);
			this.Controls.Add(listPanel);
			this.Controls.Add(leftPanel);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ForeColor = System.Drawing.SystemColors.ControlText;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(600, 450);
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
		private ButtonEx btnRemove;
		private ButtonEx btnAdd;
		private ButtonEx btnApply;
		private ButtonEx btnExport;
		private ButtonEx btnView;
		private System.Windows.Forms.SaveFileDialog exportFileDialog;
		private System.Windows.Forms.OpenFileDialog importFileDialog;
		private ButtonEx btnAddRemote;
		private ButtonEx btnCancel;
		private ButtonEx btnAddBackground;
		private ButtonEx btnAddUserIcon;
		private ButtonEx btnAddIcon;
		private System.Windows.Forms.DataGridViewTextBoxColumn columnName;
		private System.Windows.Forms.DataGridViewTextBoxColumn columnFileExt;
		private System.Windows.Forms.DataGridViewComboBoxColumn columnType;
		private System.Windows.Forms.DataGridViewTextBoxColumn columnSize;
		private System.Windows.Forms.DataGridViewImageColumn columnTag;
	}
}