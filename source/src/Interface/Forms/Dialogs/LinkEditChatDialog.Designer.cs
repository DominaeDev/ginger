
namespace Ginger
{
	partial class LinkEditChatDialog
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
			System.Windows.Forms.ColumnHeader columnTitle;
			System.Windows.Forms.ColumnHeader columnDate;
			System.Windows.Forms.Panel leftPanel;
			System.Windows.Forms.ListViewGroup listViewGroup1 = new System.Windows.Forms.ListViewGroup("Today", System.Windows.Forms.HorizontalAlignment.Left);
			System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "Untitled Chat",
            "19:35"}, 0);
			System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem(new string[] {
            "Untitled Chat #2",
            "15:30"}, 1);
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinkEditChatDialog));
			System.Windows.Forms.Panel centerPanel;
			this.groupBox = new Ginger.GroupBoxEx();
			this.chatInstanceList = new Ginger.ChatListView();
			this.imageList = new System.Windows.Forms.ImageList(this.components);
			this.portraitPanel = new System.Windows.Forms.Panel();
			this.portraitImage = new Ginger.PortraitPreview();
			this.chatView = new Ginger.ChatListBox();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.exportFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.importFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.menuBar = new System.Windows.Forms.MenuStrip();
			this.fileMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.selectCharacterMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.refreshMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
			this.editModelSettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setBackgroundMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setBackgroundFromFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setBackgroundFromPortraitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.clearBackgroundMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			this.importMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exportMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.duplicateMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolsMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.repairChatsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.statusBar = new System.Windows.Forms.StatusStrip();
			this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.statusChatLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.splitter = new System.Windows.Forms.Splitter();
			columnTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			columnDate = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			leftPanel = new System.Windows.Forms.Panel();
			centerPanel = new System.Windows.Forms.Panel();
			leftPanel.SuspendLayout();
			this.groupBox.SuspendLayout();
			this.portraitPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.portraitImage)).BeginInit();
			centerPanel.SuspendLayout();
			this.menuBar.SuspendLayout();
			this.statusBar.SuspendLayout();
			this.SuspendLayout();
			// 
			// columnTitle
			// 
			columnTitle.Text = "Title";
			columnTitle.Width = 118;
			// 
			// columnDate
			// 
			columnDate.Text = "Last message";
			columnDate.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			columnDate.Width = 100;
			// 
			// leftPanel
			// 
			leftPanel.Controls.Add(this.groupBox);
			leftPanel.Controls.Add(this.portraitPanel);
			leftPanel.Dock = System.Windows.Forms.DockStyle.Left;
			leftPanel.Location = new System.Drawing.Point(0, 24);
			leftPanel.MaximumSize = new System.Drawing.Size(600, 0);
			leftPanel.Name = "leftPanel";
			leftPanel.Padding = new System.Windows.Forms.Padding(3, 2, 2, 2);
			leftPanel.Size = new System.Drawing.Size(260, 715);
			leftPanel.TabIndex = 0;
			// 
			// groupBox
			// 
			this.groupBox.Controls.Add(this.chatInstanceList);
			this.groupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBox.Location = new System.Drawing.Point(3, 204);
			this.groupBox.Name = "groupBox";
			this.groupBox.Size = new System.Drawing.Size(255, 509);
			this.groupBox.TabIndex = 1;
			this.groupBox.TabStop = false;
			this.groupBox.Text = "Chat logs";
			// 
			// chatInstanceList
			// 
			this.chatInstanceList.Activation = System.Windows.Forms.ItemActivation.OneClick;
			this.chatInstanceList.AutoArrange = false;
			this.chatInstanceList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            columnTitle,
            columnDate});
			this.chatInstanceList.Dock = System.Windows.Forms.DockStyle.Fill;
			this.chatInstanceList.FullRowSelect = true;
			listViewGroup1.Header = "Today";
			listViewGroup1.Name = "listViewGroup1";
			this.chatInstanceList.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
            listViewGroup1});
			this.chatInstanceList.HideSelection = false;
			this.chatInstanceList.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2});
			this.chatInstanceList.LabelEdit = true;
			this.chatInstanceList.LabelWrap = false;
			this.chatInstanceList.Location = new System.Drawing.Point(3, 19);
			this.chatInstanceList.MultiSelect = false;
			this.chatInstanceList.Name = "chatInstanceList";
			this.chatInstanceList.OwnerDraw = true;
			this.chatInstanceList.ShowItemToolTips = true;
			this.chatInstanceList.Size = new System.Drawing.Size(249, 487);
			this.chatInstanceList.SmallImageList = this.imageList;
			this.chatInstanceList.TabIndex = 0;
			this.chatInstanceList.UseCompatibleStateImageBehavior = false;
			this.chatInstanceList.View = System.Windows.Forms.View.Details;
			this.chatInstanceList.OnContextMenu += new System.EventHandler<Ginger.ChatListView.ContextMenuEventArgs>(this.chatInstanceList_OnContextMenu);
			this.chatInstanceList.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.chatInstanceList_AfterLabelEdit);
			this.chatInstanceList.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.chatList_ColumnWidthChanging);
			this.chatInstanceList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.chatInstanceList_KeyDown);
			// 
			// imageList
			// 
			this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
			this.imageList.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList.Images.SetKeyName(0, "chat_empty.png");
			this.imageList.Images.SetKeyName(1, "chat_short.png");
			this.imageList.Images.SetKeyName(2, "chat_long.png");
			// 
			// portraitPanel
			// 
			this.portraitPanel.Controls.Add(this.portraitImage);
			this.portraitPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.portraitPanel.Location = new System.Drawing.Point(3, 2);
			this.portraitPanel.Margin = new System.Windows.Forms.Padding(0);
			this.portraitPanel.Name = "portraitPanel";
			this.portraitPanel.Size = new System.Drawing.Size(255, 202);
			this.portraitPanel.TabIndex = 2;
			// 
			// portraitImage
			// 
			this.portraitImage.AllowDrop = true;
			this.portraitImage.BackColor = System.Drawing.Color.DimGray;
			this.portraitImage.BackgroundImage = global::Ginger.Properties.Resources.checker;
			this.portraitImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.portraitImage.IsAnimation = false;
			this.portraitImage.Location = new System.Drawing.Point(3, 2);
			this.portraitImage.Name = "portraitImage";
			this.portraitImage.Size = new System.Drawing.Size(150, 200);
			this.portraitImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.portraitImage.TabIndex = 1;
			this.portraitImage.TabStop = false;
			// 
			// centerPanel
			// 
			centerPanel.Controls.Add(this.chatView);
			centerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			centerPanel.Location = new System.Drawing.Point(262, 24);
			centerPanel.Name = "centerPanel";
			centerPanel.Size = new System.Drawing.Size(772, 715);
			centerPanel.TabIndex = 1;
			// 
			// chatView
			// 
			this.chatView.BackColor = System.Drawing.Color.Gray;
			this.chatView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.chatView.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.chatView.Location = new System.Drawing.Point(0, 0);
			this.chatView.Name = "chatView";
			this.chatView.Padding = new System.Windows.Forms.Padding(1, 0, 0, 0);
			this.chatView.Size = new System.Drawing.Size(772, 715);
			this.chatView.TabIndex = 0;
			this.chatView.OnContextMenu += new System.EventHandler<Ginger.ChatListBox.ContextMenuEventArgs>(this.chatView_OnContextMenu);
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
			// exportFileDialog
			// 
			this.exportFileDialog.Filter = "Json file|*.json";
			this.exportFileDialog.Title = "Export chat";
			// 
			// importFileDialog
			// 
			this.importFileDialog.DefaultExt = "png";
			this.importFileDialog.Filter = "Json file|*.json";
			this.importFileDialog.SupportMultiDottedExtensions = true;
			this.importFileDialog.Title = "Import chat";
			// 
			// menuBar
			// 
			this.menuBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenu,
            this.toolsMenu});
			this.menuBar.Location = new System.Drawing.Point(0, 0);
			this.menuBar.MinimumSize = new System.Drawing.Size(400, 0);
			this.menuBar.Name = "menuBar";
			this.menuBar.Size = new System.Drawing.Size(1034, 24);
			this.menuBar.TabIndex = 1;
			this.menuBar.Text = "menuStrip1";
			this.menuBar.MenuActivate += new System.EventHandler(this.menuBar_MenuActivate);
			// 
			// fileMenu
			// 
			this.fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.selectCharacterMenuItem,
            this.refreshMenuItem,
            this.toolStripMenuItem4,
            this.editModelSettingsMenuItem,
            this.setBackgroundMenuItem,
            this.toolStripMenuItem3,
            this.importMenuItem,
            this.exportMenuItem,
            this.duplicateMenuItem,
            this.toolStripMenuItem1,
            this.closeToolStripMenuItem});
			this.fileMenu.Name = "fileMenu";
			this.fileMenu.Size = new System.Drawing.Size(37, 20);
			this.fileMenu.Text = "&File";
			// 
			// selectCharacterMenuItem
			// 
			this.selectCharacterMenuItem.Name = "selectCharacterMenuItem";
			this.selectCharacterMenuItem.Size = new System.Drawing.Size(184, 22);
			this.selectCharacterMenuItem.Text = "Select &character...";
			this.selectCharacterMenuItem.Click += new System.EventHandler(this.selectCharacterMenuItem_Click);
			// 
			// refreshMenuItem
			// 
			this.refreshMenuItem.Name = "refreshMenuItem";
			this.refreshMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.refreshMenuItem.Size = new System.Drawing.Size(184, 22);
			this.refreshMenuItem.Text = "Refresh";
			this.refreshMenuItem.Click += new System.EventHandler(this.refreshMenuItem_Click);
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new System.Drawing.Size(181, 6);
			// 
			// editModelSettingsMenuItem
			// 
			this.editModelSettingsMenuItem.Name = "editModelSettingsMenuItem";
			this.editModelSettingsMenuItem.Size = new System.Drawing.Size(184, 22);
			this.editModelSettingsMenuItem.Text = "Edit &model settings...";
			this.editModelSettingsMenuItem.Click += new System.EventHandler(this.editModelSettingsMenuItem_Click);
			// 
			// setBackgroundMenuItem
			// 
			this.setBackgroundMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setBackgroundFromFileMenuItem,
            this.setBackgroundFromPortraitMenuItem,
            this.toolStripMenuItem2,
            this.clearBackgroundMenuItem});
			this.setBackgroundMenuItem.Name = "setBackgroundMenuItem";
			this.setBackgroundMenuItem.Size = new System.Drawing.Size(184, 22);
			this.setBackgroundMenuItem.Text = "Set background";
			// 
			// setBackgroundFromFileMenuItem
			// 
			this.setBackgroundFromFileMenuItem.Name = "setBackgroundFromFileMenuItem";
			this.setBackgroundFromFileMenuItem.Size = new System.Drawing.Size(180, 22);
			this.setBackgroundFromFileMenuItem.Text = "Load from file...";
			this.setBackgroundFromFileMenuItem.Click += new System.EventHandler(this.setBackgroundFromFileMenuItem_Click);
			// 
			// setBackgroundFromPortraitMenuItem
			// 
			this.setBackgroundFromPortraitMenuItem.Name = "setBackgroundFromPortraitMenuItem";
			this.setBackgroundFromPortraitMenuItem.Size = new System.Drawing.Size(180, 22);
			this.setBackgroundFromPortraitMenuItem.Text = "Same as portrait";
			this.setBackgroundFromPortraitMenuItem.Click += new System.EventHandler(this.setBackgroundFromPortraitMenuItem_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(177, 6);
			// 
			// clearBackgroundMenuItem
			// 
			this.clearBackgroundMenuItem.Name = "clearBackgroundMenuItem";
			this.clearBackgroundMenuItem.Size = new System.Drawing.Size(180, 22);
			this.clearBackgroundMenuItem.Text = "Clear background";
			this.clearBackgroundMenuItem.Click += new System.EventHandler(this.clearBackgroundMenuItem_Click);
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(181, 6);
			// 
			// importMenuItem
			// 
			this.importMenuItem.Name = "importMenuItem";
			this.importMenuItem.Size = new System.Drawing.Size(184, 22);
			this.importMenuItem.Text = "&Import chat...";
			this.importMenuItem.Click += new System.EventHandler(this.btnImport_Click);
			// 
			// exportMenuItem
			// 
			this.exportMenuItem.Name = "exportMenuItem";
			this.exportMenuItem.Size = new System.Drawing.Size(184, 22);
			this.exportMenuItem.Text = "&Export chat...";
			this.exportMenuItem.Click += new System.EventHandler(this.btnExport_Click);
			// 
			// duplicateMenuItem
			// 
			this.duplicateMenuItem.Name = "duplicateMenuItem";
			this.duplicateMenuItem.Size = new System.Drawing.Size(184, 22);
			this.duplicateMenuItem.Text = "&Duplicate chat...";
			this.duplicateMenuItem.Click += new System.EventHandler(this.duplicateMenuItem_Click);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(181, 6);
			// 
			// closeToolStripMenuItem
			// 
			this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
			this.closeToolStripMenuItem.ShortcutKeyDisplayString = "Escape";
			this.closeToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
			this.closeToolStripMenuItem.Text = "&Close";
			this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
			// 
			// toolsMenu
			// 
			this.toolsMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.repairChatsMenuItem});
			this.toolsMenu.Name = "toolsMenu";
			this.toolsMenu.Size = new System.Drawing.Size(46, 20);
			this.toolsMenu.Text = "&Tools";
			// 
			// repairChatsMenuItem
			// 
			this.repairChatsMenuItem.Name = "repairChatsMenuItem";
			this.repairChatsMenuItem.Size = new System.Drawing.Size(166, 22);
			this.repairChatsMenuItem.Text = "&Fix legacy chats...";
			this.repairChatsMenuItem.Click += new System.EventHandler(this.repairChatsMenuItem_Click);
			// 
			// statusBar
			// 
			this.statusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel,
            this.statusChatLabel});
			this.statusBar.Location = new System.Drawing.Point(0, 739);
			this.statusBar.Name = "statusBar";
			this.statusBar.Size = new System.Drawing.Size(1034, 22);
			this.statusBar.TabIndex = 1;
			this.statusBar.Text = "statusStrip1";
			// 
			// statusLabel
			// 
			this.statusLabel.AutoSize = false;
			this.statusLabel.Name = "statusLabel";
			this.statusLabel.Size = new System.Drawing.Size(260, 17);
			this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// statusChatLabel
			// 
			this.statusChatLabel.Name = "statusChatLabel";
			this.statusChatLabel.Size = new System.Drawing.Size(759, 17);
			this.statusChatLabel.Spring = true;
			this.statusChatLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// splitter
			// 
			this.splitter.Location = new System.Drawing.Point(260, 24);
			this.splitter.MinExtra = 400;
			this.splitter.MinSize = 260;
			this.splitter.Name = "splitter";
			this.splitter.Size = new System.Drawing.Size(2, 715);
			this.splitter.TabIndex = 1;
			this.splitter.TabStop = false;
			this.splitter.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitter_SplitterMoved);
			// 
			// LinkEditChatDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1034, 761);
			this.Controls.Add(centerPanel);
			this.Controls.Add(this.splitter);
			this.Controls.Add(leftPanel);
			this.Controls.Add(this.menuBar);
			this.Controls.Add(this.statusBar);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuBar;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(800, 600);
			this.Name = "LinkEditChatDialog";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Chat history";
			leftPanel.ResumeLayout(false);
			this.groupBox.ResumeLayout(false);
			this.portraitPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.portraitImage)).EndInit();
			centerPanel.ResumeLayout(false);
			this.menuBar.ResumeLayout(false);
			this.menuBar.PerformLayout();
			this.statusBar.ResumeLayout(false);
			this.statusBar.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.ToolTip toolTip;
		private ChatListView chatInstanceList;
		private ChatListBox chatView;
		private System.Windows.Forms.ImageList imageList;
		private System.Windows.Forms.SaveFileDialog exportFileDialog;
		private System.Windows.Forms.OpenFileDialog importFileDialog;
		private System.Windows.Forms.MenuStrip menuBar;
		private System.Windows.Forms.ToolStripMenuItem fileMenu;
		private System.Windows.Forms.ToolStripMenuItem selectCharacterMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
		private System.Windows.Forms.ToolStripMenuItem importMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exportMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toolsMenu;
		private System.Windows.Forms.ToolStripMenuItem repairChatsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem duplicateMenuItem;
		private System.Windows.Forms.StatusStrip statusBar;
		private System.Windows.Forms.ToolStripStatusLabel statusLabel;
		private System.Windows.Forms.ToolStripStatusLabel statusChatLabel;
		private GroupBoxEx groupBox;
		private System.Windows.Forms.ToolStripMenuItem refreshMenuItem;
		private System.Windows.Forms.Splitter splitter;
		private PortraitPreview portraitImage;
		private System.Windows.Forms.Panel portraitPanel;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
		private System.Windows.Forms.ToolStripMenuItem editModelSettingsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setBackgroundMenuItem;
		private System.Windows.Forms.ToolStripMenuItem clearBackgroundMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setBackgroundFromPortraitMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setBackgroundFromFileMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
	}
}