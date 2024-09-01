
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
			System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "Untitled chat #1",
            "2024-09-01"}, -1);
			System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem(new string[] {
            "Untitled chat #2",
            "2024-09-01"}, -1);
			System.Windows.Forms.ColumnHeader columnTitle;
			System.Windows.Forms.ColumnHeader columnDate;
			System.Windows.Forms.Panel centerPanel;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinkEditChatDialog));
			this.chatList = new System.Windows.Forms.ListView();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.chatListBox = new Ginger.src.Interface.Controls.ChatListBox();
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
			leftPanel.Controls.Add(this.chatList);
			leftPanel.Controls.Add(this.btnOk);
			leftPanel.Controls.Add(this.btnCancel);
			leftPanel.Dock = System.Windows.Forms.DockStyle.Left;
			leftPanel.Location = new System.Drawing.Point(0, 0);
			leftPanel.Name = "leftPanel";
			leftPanel.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
			leftPanel.Size = new System.Drawing.Size(260, 641);
			leftPanel.TabIndex = 0;
			// 
			// chatList
			// 
			this.chatList.AutoArrange = false;
			this.chatList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            columnTitle,
            columnDate});
			this.chatList.Dock = System.Windows.Forms.DockStyle.Fill;
			this.chatList.FullRowSelect = true;
			this.chatList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.chatList.HideSelection = false;
			this.chatList.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2});
			this.chatList.Location = new System.Drawing.Point(3, 2);
			this.chatList.MultiSelect = false;
			this.chatList.Name = "chatList";
			this.chatList.Size = new System.Drawing.Size(254, 577);
			this.chatList.TabIndex = 1;
			this.chatList.UseCompatibleStateImageBehavior = false;
			this.chatList.View = System.Windows.Forms.View.Details;
			this.chatList.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.chatList_ColumnWidthChanging);
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
			columnDate.Width = 90;
			// 
			// btnOk
			// 
			this.btnOk.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.btnOk.Location = new System.Drawing.Point(3, 579);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(254, 30);
			this.btnOk.TabIndex = 1;
			this.btnOk.Text = "Import chat";
			this.btnOk.UseVisualStyleBackColor = true;
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.btnCancel.Location = new System.Drawing.Point(3, 609);
			this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(254, 30);
			this.btnCancel.TabIndex = 2;
			this.btnCancel.Text = "Export chat";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// centerPanel
			// 
			centerPanel.Controls.Add(this.chatListBox);
			centerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			centerPanel.Location = new System.Drawing.Point(260, 0);
			centerPanel.Name = "centerPanel";
			centerPanel.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
			centerPanel.Size = new System.Drawing.Size(674, 641);
			centerPanel.TabIndex = 1;
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
			// chatListBox
			// 
			this.chatListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.chatListBox.Location = new System.Drawing.Point(3, 2);
			this.chatListBox.Name = "chatListBox";
			this.chatListBox.Size = new System.Drawing.Size(668, 637);
			this.chatListBox.TabIndex = 0;
			// 
			// LinkEditChatDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(934, 641);
			this.Controls.Add(centerPanel);
			this.Controls.Add(leftPanel);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(450, 300);
			this.Name = "LinkEditChatDialog";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Chat viewer";
			leftPanel.ResumeLayout(false);
			centerPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.ListView chatList;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private src.Interface.Controls.ChatListBox chatListBox;
	}
}