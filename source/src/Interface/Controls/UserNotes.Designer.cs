
namespace Ginger.Interface.Controls
{
	partial class UserNotes
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
			this.textBox = new Ginger.FlatRichTextBox();
			this.SuspendLayout();
			// 
			// textBox
			// 
			this.textBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBox.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBox.Location = new System.Drawing.Point(0, 0);
			this.textBox.Margin = new System.Windows.Forms.Padding(0);
			this.textBox.Multiline = true;
			this.textBox.Name = "textBox";
			this.textBox.Padding = new System.Windows.Forms.Padding(1);
			this.textBox.Placeholder = null;
			this.textBox.SelectionLength = 0;
			this.textBox.SelectionStart = 0;
			this.textBox.Size = new System.Drawing.Size(150, 150);
			this.textBox.SpellChecking = false;
			this.textBox.SyntaxHighlighting = false;
			this.textBox.TabIndex = 0;
			// 
			// UserNotes
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.textBox);
			this.Name = "UserNotes";
			this.ResumeLayout(false);

		}

		#endregion

		private FlatRichTextBox textBox;
	}
}
