
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
			System.Windows.Forms.Panel leftPanel;
			System.Windows.Forms.ListViewGroup listViewGroup1 = new System.Windows.Forms.ListViewGroup("Today", System.Windows.Forms.HorizontalAlignment.Left);
			System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "Untitled Chat",
            "19:35"}, 0);
			System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem(new string[] {
            "Untitled Chat #2",
            "15:30"}, 1);
			System.Windows.Forms.ColumnHeader columnTitle;
			System.Windows.Forms.ColumnHeader columnDate;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinkEditChatDialog));
			System.Windows.Forms.Panel centerPanel;
			this.chatInstanceList = new System.Windows.Forms.ListView();
			this.imageList = new System.Windows.Forms.ImageList(this.components);
			this.btnImport = new System.Windows.Forms.Button();
			this.btnExport = new System.Windows.Forms.Button();
			this.chatView = new Ginger.ChatListBox();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.exportFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.importFileDialog = new System.Windows.Forms.OpenFileDialog();
			leftPanel = new System.Windows.Forms.Panel();
			columnTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			columnDate = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			centerPanel = new System.Windows.Forms.Panel();
			leftPanel.SuspendLayout();
			centerPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// leftPanel
			// 
			leftPanel.Controls.Add(this.chatInstanceList);
			leftPanel.Controls.Add(this.btnImport);
			leftPanel.Controls.Add(this.btnExport);
			leftPanel.Dock = System.Windows.Forms.DockStyle.Left;
			leftPanel.Location = new System.Drawing.Point(0, 0);
			leftPanel.Name = "leftPanel";
			leftPanel.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
			leftPanel.Size = new System.Drawing.Size(260, 761);
			leftPanel.TabIndex = 0;
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
			this.chatInstanceList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.chatInstanceList.HideSelection = false;
			this.chatInstanceList.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2});
			this.chatInstanceList.Location = new System.Drawing.Point(3, 2);
			this.chatInstanceList.MultiSelect = false;
			this.chatInstanceList.Name = "chatInstanceList";
			this.chatInstanceList.ShowItemToolTips = true;
			this.chatInstanceList.Size = new System.Drawing.Size(254, 697);
			this.chatInstanceList.SmallImageList = this.imageList;
			this.chatInstanceList.TabIndex = 1;
			this.chatInstanceList.UseCompatibleStateImageBehavior = false;
			this.chatInstanceList.View = System.Windows.Forms.View.Details;
			this.chatInstanceList.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.chatList_ColumnWidthChanging);
			// 
			// columnTitle
			// 
			columnTitle.Text = "Title";
			columnTitle.Width = 118;
			// 
			// columnDate
			// 
			columnDate.Text = "Last messaged";
			columnDate.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			columnDate.Width = 100;
			// 
			// imageList
			// 
			this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
			this.imageList.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList.Images.SetKeyName(0, "chat.png");
			this.imageList.Images.SetKeyName(1, "chat_empty.png");
			// 
			// btnImport
			// 
			this.btnImport.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.btnImport.Location = new System.Drawing.Point(3, 699);
			this.btnImport.Name = "btnImport";
			this.btnImport.Size = new System.Drawing.Size(254, 30);
			this.btnImport.TabIndex = 1;
			this.btnImport.Text = "Import chat";
			this.btnImport.UseVisualStyleBackColor = true;
			this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
			// 
			// btnExport
			// 
			this.btnExport.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnExport.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.btnExport.Location = new System.Drawing.Point(3, 729);
			this.btnExport.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
			this.btnExport.Name = "btnExport";
			this.btnExport.Size = new System.Drawing.Size(254, 30);
			this.btnExport.TabIndex = 2;
			this.btnExport.Text = "Export chat";
			this.btnExport.UseVisualStyleBackColor = true;
			this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
			// 
			// centerPanel
			// 
			centerPanel.Controls.Add(this.chatView);
			centerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			centerPanel.Location = new System.Drawing.Point(260, 0);
			centerPanel.Name = "centerPanel";
			centerPanel.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
			centerPanel.Size = new System.Drawing.Size(774, 761);
			centerPanel.TabIndex = 1;
			// 
			// chatView
			// 
			this.chatView.BackColor = System.Drawing.SystemColors.Window;
			this.chatView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.chatView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.chatView.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.chatView.Location = new System.Drawing.Point(3, 2);
			this.chatView.Name = "chatView";
			this.chatView.Size = new System.Drawing.Size(768, 757);
			this.chatView.TabIndex = 0;
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
			this.exportFileDialog.Title = "Export";
			// 
			// importFileDialog
			// 
			this.importFileDialog.DefaultExt = "png";
			this.importFileDialog.Filter = "Json file|*.json";
			this.importFileDialog.SupportMultiDottedExtensions = true;
			this.importFileDialog.Title = "Import";
			// 
			// LinkEditChatDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1034, 761);
			this.Controls.Add(centerPanel);
			this.Controls.Add(leftPanel);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(800, 600);
			this.Name = "LinkEditChatDialog";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Chat viewer";
			leftPanel.ResumeLayout(false);
			centerPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.ListView chatInstanceList;
		private System.Windows.Forms.Button btnImport;
		private System.Windows.Forms.Button btnExport;
		private ChatListBox chatView;
		private System.Windows.Forms.ImageList imageList;
		private System.Windows.Forms.SaveFileDialog exportFileDialog;
		private System.Windows.Forms.OpenFileDialog importFileDialog;
	}
}