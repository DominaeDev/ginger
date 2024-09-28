namespace Ginger
{
	partial class FlatRichTextBox
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
			this.richTextBox = new Ginger.RichTextBoxEx();
			this.SuspendLayout();
			// 
			// richTextBox
			// 
			this.richTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.richTextBox.DetectUrls = false;
			this.richTextBox.Location = new System.Drawing.Point(1, 1);
			this.richTextBox.Margin = new System.Windows.Forms.Padding(0);
			this.richTextBox.Name = "richTextBox";
			this.richTextBox.Placeholder = null;
			this.richTextBox.Size = new System.Drawing.Size(168, 167);
			this.richTextBox.SpellChecking = false;
			this.richTextBox.SyntaxHighlighting = false;
			this.richTextBox.TabIndex = 0;
			this.richTextBox.Text = "";
			// 
			// FlatRichTextBox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.richTextBox);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Margin = new System.Windows.Forms.Padding(0);
			this.Name = "FlatRichTextBox";
			this.Padding = new System.Windows.Forms.Padding(1);
			this.Size = new System.Drawing.Size(175, 173);
			this.ResumeLayout(false);

		}

		#endregion

		public RichTextBoxEx richTextBox;
	}
}
