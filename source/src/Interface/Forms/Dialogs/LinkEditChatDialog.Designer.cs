
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
			System.Windows.Forms.ColumnHeader columnTitle;
			System.Windows.Forms.ColumnHeader columnDate;
			System.Windows.Forms.Panel centerPanel;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinkEditChatDialog));
			this.chatInstanceList = new System.Windows.Forms.ListView();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.chatListBox = new Ginger.src.Interface.Controls.ChatListBox();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
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
			leftPanel.Controls.Add(this.btnOk);
			leftPanel.Controls.Add(this.btnCancel);
			leftPanel.Dock = System.Windows.Forms.DockStyle.Left;
			leftPanel.Location = new System.Drawing.Point(0, 0);
			leftPanel.Name = "leftPanel";
			leftPanel.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
			leftPanel.Size = new System.Drawing.Size(260, 641);
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
			this.chatInstanceList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.chatInstanceList.HideSelection = false;
			this.chatInstanceList.Location = new System.Drawing.Point(3, 2);
			this.chatInstanceList.MultiSelect = false;
			this.chatInstanceList.Name = "chatInstanceList";
			this.chatInstanceList.ShowItemToolTips = true;
			this.chatInstanceList.Size = new System.Drawing.Size(254, 577);
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
			// chatListBox
			// 
			this.chatListBox.BackColor = System.Drawing.SystemColors.Window;
			this.chatListBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.chatListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.chatListBox.Location = new System.Drawing.Point(3, 2);
			this.chatListBox.Name = "chatListBox";
			this.chatListBox.Size = new System.Drawing.Size(668, 637);
			this.chatListBox.TabIndex = 0;
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
		private System.Windows.Forms.ListView chatInstanceList;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private src.Interface.Controls.ChatListBox chatListBox;
	}
}