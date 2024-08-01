namespace Ginger
{
	partial class ChatParameterPanel
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
			this.bottomPanel = new System.Windows.Forms.Panel();
			this.btnAddEntry = new System.Windows.Forms.Button();
			this.label = new System.Windows.Forms.Label();
			this.textBox = new Ginger.FlatRichTextBox();
			this.rightPanel = new System.Windows.Forms.Panel();
			this.cbEnabled = new System.Windows.Forms.CheckBox();
			this.btnWrite = new System.Windows.Forms.PictureBox();
			this.bottomPanel.SuspendLayout();
			this.rightPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.btnWrite)).BeginInit();
			this.SuspendLayout();
			// 
			// bottomPanel
			// 
			this.bottomPanel.Controls.Add(this.btnAddEntry);
			this.bottomPanel.Location = new System.Drawing.Point(0, 208);
			this.bottomPanel.Name = "bottomPanel";
			this.bottomPanel.Padding = new System.Windows.Forms.Padding(3);
			this.bottomPanel.Size = new System.Drawing.Size(495, 30);
			this.bottomPanel.TabIndex = 4;
			// 
			// btnAddEntry
			// 
			this.btnAddEntry.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnAddEntry.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnAddEntry.Location = new System.Drawing.Point(3, 3);
			this.btnAddEntry.Name = "btnAddEntry";
			this.btnAddEntry.Size = new System.Drawing.Size(489, 24);
			this.btnAddEntry.TabIndex = 0;
			this.btnAddEntry.Text = "Add entry";
			this.btnAddEntry.UseVisualStyleBackColor = true;
			this.btnAddEntry.Click += new System.EventHandler(this.BtnAddEntry_Click);
			// 
			// label
			// 
			this.label.AutoEllipsis = true;
			this.label.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label.Location = new System.Drawing.Point(0, 0);
			this.label.Margin = new System.Windows.Forms.Padding(2);
			this.label.MinimumSize = new System.Drawing.Size(100, 16);
			this.label.Name = "label";
			this.label.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			this.label.Size = new System.Drawing.Size(136, 101);
			this.label.TabIndex = 0;
			this.label.Text = "Label";
			this.label.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnMouseClick);
			// 
			// textBox
			// 
			this.textBox.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBox.Location = new System.Drawing.Point(136, 2);
			this.textBox.Margin = new System.Windows.Forms.Padding(2);
			this.textBox.Multiline = true;
			this.textBox.Name = "textBox";
			this.textBox.Padding = new System.Windows.Forms.Padding(1);
			this.textBox.Placeholder = null;
			this.textBox.SelectionLength = 0;
			this.textBox.SelectionStart = 0;
			this.textBox.Size = new System.Drawing.Size(325, 204);
			this.textBox.SpellChecking = true;
			this.textBox.SyntaxHighlighting = true;
			this.textBox.TabIndex = 0;
			// 
			// rightPanel
			// 
			this.rightPanel.Controls.Add(this.cbEnabled);
			this.rightPanel.Controls.Add(this.btnWrite);
			this.rightPanel.Dock = System.Windows.Forms.DockStyle.Right;
			this.rightPanel.Location = new System.Drawing.Point(469, 0);
			this.rightPanel.Margin = new System.Windows.Forms.Padding(0);
			this.rightPanel.Name = "rightPanel";
			this.rightPanel.Size = new System.Drawing.Size(26, 238);
			this.rightPanel.TabIndex = 1;
			// 
			// cbEnabled
			// 
			this.cbEnabled.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.cbEnabled.Dock = System.Windows.Forms.DockStyle.Top;
			this.cbEnabled.Location = new System.Drawing.Point(0, 0);
			this.cbEnabled.Name = "cbEnabled";
			this.cbEnabled.Padding = new System.Windows.Forms.Padding(3, 0, 3, 3);
			this.cbEnabled.Size = new System.Drawing.Size(26, 21);
			this.cbEnabled.TabIndex = 1;
			this.cbEnabled.TabStop = false;
			this.cbEnabled.UseMnemonic = false;
			// 
			// btnWrite
			// 
			this.btnWrite.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnWrite.Image = global::Ginger.Properties.Resources.write;
			this.btnWrite.Location = new System.Drawing.Point(4, 21);
			this.btnWrite.Margin = new System.Windows.Forms.Padding(0);
			this.btnWrite.Name = "btnWrite";
			this.btnWrite.Size = new System.Drawing.Size(20, 20);
			this.btnWrite.TabIndex = 3;
			this.btnWrite.TabStop = false;
			// 
			// ChatParameterPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.textBox);
			this.Controls.Add(this.label);
			this.Controls.Add(this.bottomPanel);
			this.Controls.Add(this.rightPanel);
			this.Name = "ChatParameterPanel";
			this.Size = new System.Drawing.Size(495, 238);
			this.bottomPanel.ResumeLayout(false);
			this.rightPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.btnWrite)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Label label;
		private FlatRichTextBox textBox;
		private System.Windows.Forms.CheckBox cbEnabled;
		private System.Windows.Forms.Button btnAddEntry;
		private System.Windows.Forms.Panel rightPanel;
		private System.Windows.Forms.PictureBox btnWrite;
		private System.Windows.Forms.Panel bottomPanel;
	}
}
