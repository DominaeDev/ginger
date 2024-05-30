namespace Ginger
{
	partial class ToggleParameterPanel
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
			this.cbToggle = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// label
			// 
			this.label.AutoEllipsis = true;
			this.label.Font = new System.Drawing.Font("Segoe UI", 9.75F);
			this.label.Location = new System.Drawing.Point(0, 0);
			this.label.Margin = new System.Windows.Forms.Padding(2);
			this.label.MinimumSize = new System.Drawing.Size(100, 16);
			this.label.Name = "label";
			this.label.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			this.label.Size = new System.Drawing.Size(136, 21);
			this.label.TabIndex = 0;
			this.label.Text = "Label";
			this.label.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnMouseClick);
			// 
			// cbToggle
			// 
			this.cbToggle.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.cbToggle.Location = new System.Drawing.Point(136, 2);
			this.cbToggle.Margin = new System.Windows.Forms.Padding(2, 3, 3, 3);
			this.cbToggle.Name = "cbToggle";
			this.cbToggle.Size = new System.Drawing.Size(350, 19);
			this.cbToggle.TabIndex = 2;
			this.cbToggle.UseVisualStyleBackColor = true;
			// 
			// ToggleParameterPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.cbToggle);
			this.Controls.Add(this.label);
			this.Name = "ToggleParameterPanel";
			this.Size = new System.Drawing.Size(495, 25);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Label label;
		private System.Windows.Forms.CheckBox cbToggle;
	}
}
