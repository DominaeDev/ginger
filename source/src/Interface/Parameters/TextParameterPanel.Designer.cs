namespace Ginger
{
	partial class TextParameterPanel
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
			this.label = new System.Windows.Forms.Label();
			this.textBox = new Ginger.FlatRichTextBox();
			this.cbEnabled = new System.Windows.Forms.CheckBox();
			this.rightPanel = new System.Windows.Forms.Panel();
			this.rightPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// label
			// 
			this.label.AutoEllipsis = true;
			this.label.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label.Location = new System.Drawing.Point(2, 2);
			this.label.MinimumSize = new System.Drawing.Size(100, 16);
			this.label.Name = "label";
			this.label.Size = new System.Drawing.Size(136, 21);
			this.label.TabIndex = 0;
			this.label.Text = "Label";
			this.label.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnMouseClick);
			// 
			// textBox
			// 
			this.textBox.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBox.Location = new System.Drawing.Point(142, 2);
			this.textBox.Margin = new System.Windows.Forms.Padding(0);
			this.textBox.Multiline = false;
			this.textBox.Name = "textBox";
			this.textBox.Padding = new System.Windows.Forms.Padding(1);
			this.textBox.Placeholder = null;
			this.textBox.SelectionLength = 0;
			this.textBox.SelectionStart = 0;
			this.textBox.Size = new System.Drawing.Size(325, 21);
			this.textBox.SpellChecking = true;
			this.textBox.SyntaxHighlighting = true;
			this.textBox.TabIndex = 0;
			this.textBox.TextSize = new System.Drawing.Size(0, 0);
			// 
			// cbEnabled
			// 
			this.cbEnabled.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.cbEnabled.Dock = System.Windows.Forms.DockStyle.Top;
			this.cbEnabled.Location = new System.Drawing.Point(0, 0);
			this.cbEnabled.Name = "cbEnabled";
			this.cbEnabled.Size = new System.Drawing.Size(26, 21);
			this.cbEnabled.TabIndex = 1;
			this.cbEnabled.TabStop = false;
			this.cbEnabled.UseMnemonic = false;
			// 
			// rightPanel
			// 
			this.rightPanel.Controls.Add(this.cbEnabled);
			this.rightPanel.Dock = System.Windows.Forms.DockStyle.Right;
			this.rightPanel.Location = new System.Drawing.Point(469, 0);
			this.rightPanel.Margin = new System.Windows.Forms.Padding(0);
			this.rightPanel.Name = "rightPanel";
			this.rightPanel.Size = new System.Drawing.Size(26, 25);
			this.rightPanel.TabIndex = 6;
			// 
			// TextParameterPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label);
			this.Controls.Add(this.textBox);
			this.Controls.Add(this.rightPanel);
			this.Name = "TextParameterPanel";
			this.Size = new System.Drawing.Size(495, 25);
			this.rightPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Label label;
		private FlatRichTextBox textBox;
		private System.Windows.Forms.CheckBox cbEnabled;
		private System.Windows.Forms.Panel rightPanel;
	}
}
