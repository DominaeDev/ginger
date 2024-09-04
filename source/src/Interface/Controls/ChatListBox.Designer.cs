
namespace Ginger
{
	partial class ChatListBox
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.listBox = new System.Windows.Forms.ListBox();
			this.SuspendLayout();
			// 
			// listBox
			// 
			this.listBox.BackColor = System.Drawing.Color.WhiteSmoke;
			this.listBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.listBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
			this.listBox.FormattingEnabled = true;
			this.listBox.Location = new System.Drawing.Point(0, 0);
			this.listBox.Name = "listBox";
			this.listBox.ScrollAlwaysVisible = true;
			this.listBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.listBox.Size = new System.Drawing.Size(135, 134);
			this.listBox.TabIndex = 0;
			this.listBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listBox_DrawItem);
			this.listBox.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.listBox_MeasureItem);
			this.listBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listBox_MouseDown);
			this.listBox.MouseLeave += new System.EventHandler(this.listBox_MouseLeave);
			this.listBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listBox_MouseUp);
			// 
			// ChatListBox
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.BackColor = System.Drawing.Color.Gray;
			this.Controls.Add(this.listBox);
			this.Name = "ChatListBox";
			this.Resize += new System.EventHandler(this.ChatListView_Resize);
			this.ResumeLayout(false);

		}

		#endregion

		public System.Windows.Forms.ListBox listBox;
	}
}
