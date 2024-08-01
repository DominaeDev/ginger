namespace Ginger
{
	partial class LorebookEntryPanel
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
			this.labelKey = new System.Windows.Forms.Label();
			this.labelText = new System.Windows.Forms.Label();
			this.labelTokens = new System.Windows.Forms.Label();
			this.btnRemove = new System.Windows.Forms.PictureBox();
			this.btnWrite = new System.Windows.Forms.PictureBox();
			this.btnMoveUp = new System.Windows.Forms.PictureBox();
			this.btnMoveDown = new System.Windows.Forms.PictureBox();
			this.cbEnabled = new System.Windows.Forms.CheckBox();
			this.rightPanel = new System.Windows.Forms.Panel();
			this.labelIndex = new System.Windows.Forms.Label();
			this.textBox_Index = new Ginger.TextBoxEx();
			this.textBox_Keys = new Ginger.FlatRichTextBox();
			this.textBox_Text = new Ginger.FlatRichTextBox();
			((System.ComponentModel.ISupportInitialize)(this.btnRemove)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.btnWrite)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.btnMoveUp)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.btnMoveDown)).BeginInit();
			this.rightPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// labelKey
			// 
			this.labelKey.AutoEllipsis = true;
			this.labelKey.Location = new System.Drawing.Point(2, 2);
			this.labelKey.Margin = new System.Windows.Forms.Padding(2);
			this.labelKey.MinimumSize = new System.Drawing.Size(100, 16);
			this.labelKey.Name = "labelKey";
			this.labelKey.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			this.labelKey.Size = new System.Drawing.Size(136, 21);
			this.labelKey.TabIndex = 0;
			this.labelKey.Text = "Keyword(s)";
			this.labelKey.MouseClick += new System.Windows.Forms.MouseEventHandler(this.LorebookEntryPanel_MouseClick);
			// 
			// labelText
			// 
			this.labelText.AutoEllipsis = true;
			this.labelText.Location = new System.Drawing.Point(2, 25);
			this.labelText.Margin = new System.Windows.Forms.Padding(0);
			this.labelText.MinimumSize = new System.Drawing.Size(100, 16);
			this.labelText.Name = "labelText";
			this.labelText.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			this.labelText.Size = new System.Drawing.Size(136, 17);
			this.labelText.TabIndex = 0;
			this.labelText.Text = "Content";
			this.labelText.MouseClick += new System.Windows.Forms.MouseEventHandler(this.LorebookEntryPanel_MouseClick);
			// 
			// labelTokens
			// 
			this.labelTokens.AutoEllipsis = true;
			this.labelTokens.Font = new System.Drawing.Font("Segoe UI", 6.75F);
			this.labelTokens.Location = new System.Drawing.Point(2, 42);
			this.labelTokens.Margin = new System.Windows.Forms.Padding(0);
			this.labelTokens.MinimumSize = new System.Drawing.Size(100, 16);
			this.labelTokens.Name = "labelTokens";
			this.labelTokens.Padding = new System.Windows.Forms.Padding(1, 1, 0, 0);
			this.labelTokens.Size = new System.Drawing.Size(136, 18);
			this.labelTokens.TabIndex = 1;
			this.labelTokens.Text = "0 tokens";
			this.labelTokens.MouseClick += new System.Windows.Forms.MouseEventHandler(this.LorebookEntryPanel_MouseClick);
			// 
			// btnRemove
			// 
			this.btnRemove.BackColor = System.Drawing.Color.Transparent;
			this.btnRemove.Image = global::Ginger.Properties.Resources.delete;
			this.btnRemove.Location = new System.Drawing.Point(2, 88);
			this.btnRemove.Margin = new System.Windows.Forms.Padding(0);
			this.btnRemove.Name = "btnRemove";
			this.btnRemove.Size = new System.Drawing.Size(22, 22);
			this.btnRemove.TabIndex = 7;
			this.btnRemove.TabStop = false;
			this.btnRemove.MouseClick += new System.Windows.Forms.MouseEventHandler(this.Btn_Remove_MouseClick);
			// 
			// btnWrite
			// 
			this.btnWrite.Image = global::Ginger.Properties.Resources.write;
			this.btnWrite.Location = new System.Drawing.Point(4, 26);
			this.btnWrite.Margin = new System.Windows.Forms.Padding(0);
			this.btnWrite.Name = "btnWrite";
			this.btnWrite.Size = new System.Drawing.Size(20, 20);
			this.btnWrite.TabIndex = 2;
			this.btnWrite.TabStop = false;
			this.btnWrite.MouseClick += new System.Windows.Forms.MouseEventHandler(this.btnWrite_MouseClick);
			// 
			// btnMoveUp
			// 
			this.btnMoveUp.Image = global::Ginger.Properties.Resources.lore_up;
			this.btnMoveUp.Location = new System.Drawing.Point(4, 46);
			this.btnMoveUp.Margin = new System.Windows.Forms.Padding(0);
			this.btnMoveUp.Name = "btnMoveUp";
			this.btnMoveUp.Size = new System.Drawing.Size(20, 20);
			this.btnMoveUp.TabIndex = 8;
			this.btnMoveUp.TabStop = false;
			this.btnMoveUp.MouseClick += new System.Windows.Forms.MouseEventHandler(this.btnMoveUp_MouseClick);
			// 
			// btnMoveDown
			// 
			this.btnMoveDown.Image = global::Ginger.Properties.Resources.lore_down;
			this.btnMoveDown.Location = new System.Drawing.Point(4, 66);
			this.btnMoveDown.Margin = new System.Windows.Forms.Padding(0);
			this.btnMoveDown.Name = "btnMoveDown";
			this.btnMoveDown.Size = new System.Drawing.Size(20, 20);
			this.btnMoveDown.TabIndex = 9;
			this.btnMoveDown.TabStop = false;
			this.btnMoveDown.MouseClick += new System.Windows.Forms.MouseEventHandler(this.btnMoveDown_MouseClick);
			// 
			// cbEnabled
			// 
			this.cbEnabled.CheckAlign = System.Drawing.ContentAlignment.TopCenter;
			this.cbEnabled.Dock = System.Windows.Forms.DockStyle.Top;
			this.cbEnabled.Location = new System.Drawing.Point(0, 0);
			this.cbEnabled.Name = "cbEnabled";
			this.cbEnabled.Size = new System.Drawing.Size(26, 21);
			this.cbEnabled.TabIndex = 0;
			this.cbEnabled.TabStop = false;
			this.cbEnabled.UseMnemonic = false;
			this.cbEnabled.CheckedChanged += new System.EventHandler(this.cbEnabled_CheckedChanged);
			// 
			// rightPanel
			// 
			this.rightPanel.Controls.Add(this.cbEnabled);
			this.rightPanel.Controls.Add(this.btnWrite);
			this.rightPanel.Controls.Add(this.btnMoveUp);
			this.rightPanel.Controls.Add(this.btnMoveDown);
			this.rightPanel.Controls.Add(this.btnRemove);
			this.rightPanel.Dock = System.Windows.Forms.DockStyle.Right;
			this.rightPanel.Location = new System.Drawing.Point(469, 0);
			this.rightPanel.Margin = new System.Windows.Forms.Padding(0);
			this.rightPanel.Name = "rightPanel";
			this.rightPanel.Size = new System.Drawing.Size(26, 90);
			this.rightPanel.TabIndex = 2;
			// 
			// labelIndex
			// 
			this.labelIndex.Location = new System.Drawing.Point(380, 0);
			this.labelIndex.Margin = new System.Windows.Forms.Padding(2);
			this.labelIndex.MinimumSize = new System.Drawing.Size(50, 16);
			this.labelIndex.Name = "labelIndex";
			this.labelIndex.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			this.labelIndex.Size = new System.Drawing.Size(50, 21);
			this.labelIndex.TabIndex = 3;
			this.labelIndex.Text = "Order:";
			this.labelIndex.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// textBox_Index
			// 
			this.textBox_Index.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBox_Index.Location = new System.Drawing.Point(405, 1);
			this.textBox_Index.Name = "textBox_Index";
			this.textBox_Index.Placeholder = null;
			this.textBox_Index.ShortcutsEnabled = false;
			this.textBox_Index.Size = new System.Drawing.Size(64, 23);
			this.textBox_Index.TabIndex = 1;
			// 
			// textBox_Keys
			// 
			this.textBox_Keys.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBox_Keys.Location = new System.Drawing.Point(140, 0);
			this.textBox_Keys.Margin = new System.Windows.Forms.Padding(0);
			this.textBox_Keys.Multiline = false;
			this.textBox_Keys.Name = "textBox_Keys";
			this.textBox_Keys.Padding = new System.Windows.Forms.Padding(1);
			this.textBox_Keys.Placeholder = "Enter keyword (or keywords, separated by commas)";
			this.textBox_Keys.SelectionLength = 0;
			this.textBox_Keys.SelectionStart = 0;
			this.textBox_Keys.Size = new System.Drawing.Size(168, 21);
			this.textBox_Keys.SpellChecking = true;
			this.textBox_Keys.SyntaxHighlighting = true;
			this.textBox_Keys.TabIndex = 0;
			this.textBox_Keys.TextSize = new System.Drawing.Size(0, 0);
			// 
			// textBox_Text
			// 
			this.textBox_Text.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBox_Text.Location = new System.Drawing.Point(140, 25);
			this.textBox_Text.Margin = new System.Windows.Forms.Padding(0);
			this.textBox_Text.Multiline = true;
			this.textBox_Text.Name = "textBox_Text";
			this.textBox_Text.Padding = new System.Windows.Forms.Padding(1);
			this.textBox_Text.Placeholder = null;
			this.textBox_Text.SelectionLength = 0;
			this.textBox_Text.SelectionStart = 0;
			this.textBox_Text.Size = new System.Drawing.Size(329, 65);
			this.textBox_Text.SpellChecking = true;
			this.textBox_Text.SyntaxHighlighting = true;
			this.textBox_Text.TabIndex = 2;
			this.textBox_Text.TextSize = new System.Drawing.Size(0, 0);
			// 
			// LorebookEntryPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.labelIndex);
			this.Controls.Add(this.labelText);
			this.Controls.Add(this.labelTokens);
			this.Controls.Add(this.textBox_Index);
			this.Controls.Add(this.textBox_Keys);
			this.Controls.Add(this.textBox_Text);
			this.Controls.Add(this.labelKey);
			this.Controls.Add(this.rightPanel);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.Name = "LorebookEntryPanel";
			this.Size = new System.Drawing.Size(495, 90);
			this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.LorebookEntryPanel_MouseClick);
			((System.ComponentModel.ISupportInitialize)(this.btnRemove)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.btnWrite)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.btnMoveUp)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.btnMoveDown)).EndInit();
			this.rightPanel.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Label labelKey;
		private System.Windows.Forms.Label labelText;
		public FlatRichTextBox textBox_Text;
		public FlatRichTextBox textBox_Keys;
		private System.Windows.Forms.Label labelTokens;
		private System.Windows.Forms.PictureBox btnRemove;
		private System.Windows.Forms.PictureBox btnWrite;
		private System.Windows.Forms.PictureBox btnMoveUp;
		private System.Windows.Forms.PictureBox btnMoveDown;
		private System.Windows.Forms.CheckBox cbEnabled;
		private System.Windows.Forms.Panel rightPanel;
		private System.Windows.Forms.Label labelIndex;
		private TextBoxEx textBox_Index;
	}
}
