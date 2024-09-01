
namespace Ginger.src.Interface.Controls
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
			this.listBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.listBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
			this.listBox.FormattingEnabled = true;
			this.listBox.Location = new System.Drawing.Point(3, 0);
			this.listBox.Name = "listBox";
			this.listBox.ScrollAlwaysVisible = true;
			this.listBox.Size = new System.Drawing.Size(144, 147);
			this.listBox.TabIndex = 0;
			this.listBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listBox_DrawItem);
			this.listBox.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.listBox_MeasureItem);
			// 
			// ChatListBox
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.Controls.Add(this.listBox);
			this.Name = "ChatListBox";
			this.Size = new System.Drawing.Size(148, 148);
			this.Resize += new System.EventHandler(this.ChatListView_Resize);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListBox listBox;
	}
}
