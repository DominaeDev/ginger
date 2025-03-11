namespace Ginger
{
	partial class ChoiceParameterPanel
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
			this.valuePanel = new System.Windows.Forms.Panel();
			this.textBox_Custom = new Ginger.FlatRichTextBox();
			this.spacer = new System.Windows.Forms.Panel();
			this.comboBox = new Ginger.ComboBoxEx();
			this.rightPanel = new System.Windows.Forms.Panel();
			this.cbEnabled = new System.Windows.Forms.CheckBox();
			this.valuePanel.SuspendLayout();
			this.rightPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// label
			// 
			this.label.AutoEllipsis = true;
			this.label.Font = new System.Drawing.Font("Segoe UI", 9.75F);
			this.label.Location = new System.Drawing.Point(2, 2);
			this.label.Margin = new System.Windows.Forms.Padding(2);
			this.label.MinimumSize = new System.Drawing.Size(100, 16);
			this.label.Name = "label";
			this.label.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			this.label.Size = new System.Drawing.Size(136, 21);
			this.label.TabIndex = 0;
			this.label.Text = "Label";
			this.label.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnMouseClick);
			// 
			// valuePanel
			// 
			this.valuePanel.Controls.Add(this.textBox_Custom);
			this.valuePanel.Controls.Add(this.spacer);
			this.valuePanel.Controls.Add(this.comboBox);
			this.valuePanel.Location = new System.Drawing.Point(139, 0);
			this.valuePanel.Margin = new System.Windows.Forms.Padding(0);
			this.valuePanel.Name = "valuePanel";
			this.valuePanel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
			this.valuePanel.Size = new System.Drawing.Size(329, 25);
			this.valuePanel.TabIndex = 4;
			// 
			// textBox_Custom
			// 
			this.textBox_Custom.Dock = System.Windows.Forms.DockStyle.Top;
			this.textBox_Custom.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBox_Custom.Location = new System.Drawing.Point(264, 2);
			this.textBox_Custom.Margin = new System.Windows.Forms.Padding(1);
			this.textBox_Custom.Multiline = false;
			this.textBox_Custom.Name = "textBox_Custom";
			this.textBox_Custom.Padding = new System.Windows.Forms.Padding(1);
			this.textBox_Custom.Placeholder = null;
			this.textBox_Custom.SelectionLength = 0;
			this.textBox_Custom.SelectionStart = 0;
			this.textBox_Custom.Size = new System.Drawing.Size(65, 20);
			this.textBox_Custom.SpellChecking = true;
			this.textBox_Custom.SyntaxHighlighting = true;
			this.textBox_Custom.TabIndex = 3;
			this.textBox_Custom.TextSize = new System.Drawing.Size(0, 0);
			// 
			// spacer
			// 
			this.spacer.Dock = System.Windows.Forms.DockStyle.Left;
			this.spacer.Location = new System.Drawing.Point(260, 2);
			this.spacer.Name = "spacer";
			this.spacer.Size = new System.Drawing.Size(4, 21);
			this.spacer.TabIndex = 4;
			// 
			// comboBox
			// 
			this.comboBox.Dock = System.Windows.Forms.DockStyle.Left;
			this.comboBox.DropDownHeight = 240;
			this.comboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.comboBox.IntegralHeight = false;
			this.comboBox.Location = new System.Drawing.Point(0, 2);
			this.comboBox.Margin = new System.Windows.Forms.Padding(2);
			this.comboBox.MaxDropDownItems = 20;
			this.comboBox.Name = "comboBox";
			this.comboBox.Size = new System.Drawing.Size(260, 21);
			this.comboBox.TabIndex = 2;
			// 
			// rightPanel
			// 
			this.rightPanel.Controls.Add(this.cbEnabled);
			this.rightPanel.Dock = System.Windows.Forms.DockStyle.Right;
			this.rightPanel.Location = new System.Drawing.Point(469, 0);
			this.rightPanel.Margin = new System.Windows.Forms.Padding(0);
			this.rightPanel.Name = "rightPanel";
			this.rightPanel.Size = new System.Drawing.Size(26, 25);
			this.rightPanel.TabIndex = 5;
			// 
			// cbEnabled
			// 
			this.cbEnabled.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.cbEnabled.Dock = System.Windows.Forms.DockStyle.Top;
			this.cbEnabled.Location = new System.Drawing.Point(0, 0);
			this.cbEnabled.Name = "cbEnabled";
			this.cbEnabled.Padding = new System.Windows.Forms.Padding(3, 0, 3, 3);
			this.cbEnabled.Size = new System.Drawing.Size(26, 21);
			this.cbEnabled.TabIndex = 2;
			this.cbEnabled.TabStop = false;
			this.cbEnabled.UseMnemonic = false;
			this.cbEnabled.CheckedChanged += new System.EventHandler(this.CbEnabled_CheckedChanged);
			// 
			// ChoiceParameterPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label);
			this.Controls.Add(this.valuePanel);
			this.Controls.Add(this.rightPanel);
			this.Name = "ChoiceParameterPanel";
			this.Size = new System.Drawing.Size(495, 25);
			this.valuePanel.ResumeLayout(false);
			this.rightPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Panel valuePanel;
		private System.Windows.Forms.Panel spacer;
		private System.Windows.Forms.Panel rightPanel;
		protected System.Windows.Forms.Label label;
		protected ComboBoxEx comboBox;
		protected FlatRichTextBox textBox_Custom;
		protected System.Windows.Forms.CheckBox cbEnabled;
	}
}
