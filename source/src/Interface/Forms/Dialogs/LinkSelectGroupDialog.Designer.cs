﻿
namespace Ginger
{
	partial class LinkSelectGroupDialog
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
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Panel listPanel;
			System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Character", 1, 1);
			System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Group", 2, 2);
			System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("Folder", new System.Windows.Forms.TreeNode[] {
            treeNode1,
            treeNode2});
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinkSelectGroupDialog));
			System.Windows.Forms.FlowLayoutPanel buttonLayout;
			this.treeView = new Ginger.TreeViewEx();
			this.imageList = new System.Windows.Forms.ImageList(this.components);
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			listPanel = new System.Windows.Forms.Panel();
			buttonLayout = new System.Windows.Forms.FlowLayoutPanel();
			listPanel.SuspendLayout();
			buttonLayout.SuspendLayout();
			this.SuspendLayout();
			// 
			// listPanel
			// 
			listPanel.BackColor = System.Drawing.SystemColors.Window;
			listPanel.Controls.Add(this.treeView);
			listPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			listPanel.Location = new System.Drawing.Point(0, 0);
			listPanel.Name = "listPanel";
			listPanel.Padding = new System.Windows.Forms.Padding(2, 4, 2, 2);
			listPanel.Size = new System.Drawing.Size(484, 326);
			listPanel.TabIndex = 0;
			// 
			// treeView
			// 
			this.treeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.treeView.CausesValidation = false;
			this.treeView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeView.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.treeView.FullRowSelect = true;
			this.treeView.HideSelection = false;
			this.treeView.ImageIndex = 0;
			this.treeView.ImageList = this.imageList;
			this.treeView.Location = new System.Drawing.Point(2, 4);
			this.treeView.Name = "treeView";
			treeNode1.ImageIndex = 1;
			treeNode1.Name = "Node1";
			treeNode1.SelectedImageIndex = 1;
			treeNode1.Text = "Character";
			treeNode2.ImageIndex = 2;
			treeNode2.Name = "tmpNode1";
			treeNode2.SelectedImageIndex = 2;
			treeNode2.Text = "Group";
			treeNode3.Name = "tmpNode1";
			treeNode3.SelectedImageIndex = 0;
			treeNode3.Text = "Folder";
			this.treeView.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode3});
			this.treeView.SelectedImageIndex = 0;
			this.treeView.ShowLines = false;
			this.treeView.ShowNodeToolTips = true;
			this.treeView.Size = new System.Drawing.Size(480, 320);
			this.treeView.TabIndex = 0;
			this.treeView.OnRightClick += new System.Windows.Forms.MouseEventHandler(this.treeView_MouseClick);
			this.treeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_AfterSelect);
			this.treeView.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView_NodeMouseDoubleClick);
			// 
			// imageList
			// 
			this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
			this.imageList.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList.Images.SetKeyName(0, "tree_folder.png");
			this.imageList.Images.SetKeyName(1, "character_pair.png");
			this.imageList.Images.SetKeyName(2, "character_group.png");
			// 
			// buttonLayout
			// 
			buttonLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			buttonLayout.Controls.Add(this.btnCancel);
			buttonLayout.Controls.Add(this.btnOk);
			buttonLayout.Dock = System.Windows.Forms.DockStyle.Bottom;
			buttonLayout.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			buttonLayout.Location = new System.Drawing.Point(0, 326);
			buttonLayout.Margin = new System.Windows.Forms.Padding(0);
			buttonLayout.Name = "buttonLayout";
			buttonLayout.Padding = new System.Windows.Forms.Padding(0, 2, 4, 0);
			buttonLayout.Size = new System.Drawing.Size(484, 35);
			buttonLayout.TabIndex = 7;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(363, 2);
			this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
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
			this.btnOk.Location = new System.Drawing.Point(242, 2);
			this.btnOk.Margin = new System.Windows.Forms.Padding(0);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(117, 30);
			this.btnOk.TabIndex = 1;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
			// 
			// toolTip
			// 
			this.toolTip.AutomaticDelay = 250;
			this.toolTip.AutoPopDelay = 3500;
			this.toolTip.InitialDelay = 250;
			this.toolTip.ReshowDelay = 50;
			this.toolTip.UseAnimation = false;
			this.toolTip.UseFading = false;
			// 
			// LinkSelectChatGroupDialog
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(484, 361);
			this.Controls.Add(listPanel);
			this.Controls.Add(buttonLayout);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(450, 300);
			this.Name = "LinkSelectChatGroupDialog";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Select a character or group";
			listPanel.ResumeLayout(false);
			buttonLayout.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private TreeViewEx treeView;
		private System.Windows.Forms.ImageList imageList;
		private System.Windows.Forms.ToolTip toolTip;
	}
}