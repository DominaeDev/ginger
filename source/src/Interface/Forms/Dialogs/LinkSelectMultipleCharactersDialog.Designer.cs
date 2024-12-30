
namespace Ginger
{
	partial class LinkSelectMultipleCharactersDialog
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
			System.Windows.Forms.FlowLayoutPanel buttonLayout;
			System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Character", 1, 1);
			System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Group", 2, 2, new System.Windows.Forms.TreeNode[] {
            treeNode1});
			System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("Folder", new System.Windows.Forms.TreeNode[] {
            treeNode2});
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinkSelectMultipleCharactersDialog));
			this.btnCancel = new Ginger.ButtonEx();
			this.btnOk = new Ginger.ButtonEx();
			this.listPanel = new System.Windows.Forms.Panel();
			this.treeView = new Ginger.TreeViewEx();
			this.imageList_Light = new System.Windows.Forms.ImageList(this.components);
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.imageList_Dark = new System.Windows.Forms.ImageList(this.components);
			this.cbSelectAll = new System.Windows.Forms.CheckBox();
			buttonLayout = new System.Windows.Forms.FlowLayoutPanel();
			buttonLayout.SuspendLayout();
			this.listPanel.SuspendLayout();
			this.SuspendLayout();
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
			this.btnCancel.Highlighted = false;
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
			this.btnOk.Highlighted = false;
			this.btnOk.Location = new System.Drawing.Point(242, 2);
			this.btnOk.Margin = new System.Windows.Forms.Padding(0);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(117, 30);
			this.btnOk.TabIndex = 1;
			this.btnOk.Text = "Select";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
			// 
			// listPanel
			// 
			this.listPanel.BackColor = System.Drawing.SystemColors.Window;
			this.listPanel.Controls.Add(this.treeView);
			this.listPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listPanel.Location = new System.Drawing.Point(0, 0);
			this.listPanel.Name = "listPanel";
			this.listPanel.Padding = new System.Windows.Forms.Padding(2, 4, 2, 2);
			this.listPanel.Size = new System.Drawing.Size(484, 326);
			this.listPanel.TabIndex = 0;
			// 
			// treeView
			// 
			this.treeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.treeView.CausesValidation = false;
			this.treeView.CheckBoxes = true;
			this.treeView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeView.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.treeView.FullRowSelect = true;
			this.treeView.ImageIndex = 0;
			this.treeView.ImageList = this.imageList_Light;
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
			this.treeView.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treeView_AfterCheck);
			this.treeView.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView_NodeMouseDoubleClick);
			// 
			// imageList_Light
			// 
			this.imageList_Light.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList_Light.ImageStream")));
			this.imageList_Light.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList_Light.Images.SetKeyName(0, "tree_folder.png");
			this.imageList_Light.Images.SetKeyName(1, "character_neutral.png");
			this.imageList_Light.Images.SetKeyName(2, "character_male.png");
			this.imageList_Light.Images.SetKeyName(3, "character_female.png");
			this.imageList_Light.Images.SetKeyName(4, "character_other.png");
			this.imageList_Light.Images.SetKeyName(5, "character_neutral_lore.png");
			this.imageList_Light.Images.SetKeyName(6, "character_male_lore.png");
			this.imageList_Light.Images.SetKeyName(7, "character_female_lore.png");
			this.imageList_Light.Images.SetKeyName(8, "character_other_lore.png");
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
			// imageList_Dark
			// 
			this.imageList_Dark.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList_Dark.ImageStream")));
			this.imageList_Dark.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList_Dark.Images.SetKeyName(0, "dark_tree_folder.png");
			this.imageList_Dark.Images.SetKeyName(1, "character_neutral.png");
			this.imageList_Dark.Images.SetKeyName(2, "character_male.png");
			this.imageList_Dark.Images.SetKeyName(3, "character_female.png");
			this.imageList_Dark.Images.SetKeyName(4, "character_other.png");
			this.imageList_Dark.Images.SetKeyName(5, "character_neutral_lore.png");
			this.imageList_Dark.Images.SetKeyName(6, "character_male_lore.png");
			this.imageList_Dark.Images.SetKeyName(7, "character_female_lore.png");
			this.imageList_Dark.Images.SetKeyName(8, "character_other_lore.png");
			// 
			// cbSelectAll
			// 
			this.cbSelectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.cbSelectAll.AutoSize = true;
			this.cbSelectAll.Checked = true;
			this.cbSelectAll.CheckState = System.Windows.Forms.CheckState.Indeterminate;
			this.cbSelectAll.Location = new System.Drawing.Point(24, 334);
			this.cbSelectAll.Name = "cbSelectAll";
			this.cbSelectAll.Size = new System.Drawing.Size(72, 19);
			this.cbSelectAll.TabIndex = 4;
			this.cbSelectAll.Text = "Select all";
			this.cbSelectAll.UseVisualStyleBackColor = true;
			this.cbSelectAll.CheckedChanged += new System.EventHandler(this.cbSelectAll_CheckedChanged);
			// 
			// LinkSelectMultipleCharactersDialog
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(484, 361);
			this.Controls.Add(this.cbSelectAll);
			this.Controls.Add(this.listPanel);
			this.Controls.Add(buttonLayout);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(450, 300);
			this.Name = "LinkSelectMultipleCharactersDialog";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Select characters";
			buttonLayout.ResumeLayout(false);
			this.listPanel.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private ButtonEx btnCancel;
		private ButtonEx btnOk;
		private TreeViewEx treeView;
		private System.Windows.Forms.ImageList imageList_Light;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.Panel listPanel;
		private System.Windows.Forms.ImageList imageList_Dark;
		private System.Windows.Forms.CheckBox cbSelectAll;
	}
}