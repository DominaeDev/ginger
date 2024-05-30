namespace Ginger
{
	partial class MultiChoiceParameterPanel
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
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.checkBoxPanel2 = new System.Windows.Forms.Panel();
			this.checkBoxPanel1 = new System.Windows.Forms.Panel();
			this.checkBoxPanel0 = new System.Windows.Forms.Panel();
			this.label = new System.Windows.Forms.Label();
			this.cbEnabled = new System.Windows.Forms.CheckBox();
			this.rightPanel = new System.Windows.Forms.Panel();
			this.tableLayoutPanel.SuspendLayout();
			this.rightPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel
			// 
			this.tableLayoutPanel.ColumnCount = 3;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33332F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel.Controls.Add(this.checkBoxPanel2, 2, 0);
			this.tableLayoutPanel.Controls.Add(this.checkBoxPanel1, 1, 0);
			this.tableLayoutPanel.Controls.Add(this.checkBoxPanel0, 0, 0);
			this.tableLayoutPanel.Location = new System.Drawing.Point(140, 0);
			this.tableLayoutPanel.MinimumSize = new System.Drawing.Size(0, 25);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 1;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.Size = new System.Drawing.Size(326, 25);
			this.tableLayoutPanel.TabIndex = 3;
			this.tableLayoutPanel.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnMouseClick);
			// 
			// checkBoxPanel2
			// 
			this.checkBoxPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.checkBoxPanel2.Location = new System.Drawing.Point(216, 0);
			this.checkBoxPanel2.Margin = new System.Windows.Forms.Padding(0);
			this.checkBoxPanel2.Name = "checkBoxPanel2";
			this.checkBoxPanel2.Padding = new System.Windows.Forms.Padding(0, 2, 8, 0);
			this.checkBoxPanel2.Size = new System.Drawing.Size(110, 25);
			this.checkBoxPanel2.TabIndex = 4;
			// 
			// checkBoxPanel1
			// 
			this.checkBoxPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.checkBoxPanel1.Location = new System.Drawing.Point(108, 0);
			this.checkBoxPanel1.Margin = new System.Windows.Forms.Padding(0);
			this.checkBoxPanel1.Name = "checkBoxPanel1";
			this.checkBoxPanel1.Padding = new System.Windows.Forms.Padding(0, 2, 8, 0);
			this.checkBoxPanel1.Size = new System.Drawing.Size(108, 25);
			this.checkBoxPanel1.TabIndex = 3;
			// 
			// checkBoxPanel0
			// 
			this.checkBoxPanel0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.checkBoxPanel0.Location = new System.Drawing.Point(0, 0);
			this.checkBoxPanel0.Margin = new System.Windows.Forms.Padding(0);
			this.checkBoxPanel0.Name = "checkBoxPanel0";
			this.checkBoxPanel0.Padding = new System.Windows.Forms.Padding(0, 2, 8, 0);
			this.checkBoxPanel0.Size = new System.Drawing.Size(108, 25);
			this.checkBoxPanel0.TabIndex = 2;
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
			this.cbEnabled.CheckedChanged += new System.EventHandler(this.CbEnabled_CheckedChanged);
			// 
			// rightPanel
			// 
			this.rightPanel.Controls.Add(this.cbEnabled);
			this.rightPanel.Dock = System.Windows.Forms.DockStyle.Right;
			this.rightPanel.Location = new System.Drawing.Point(469, 0);
			this.rightPanel.Margin = new System.Windows.Forms.Padding(0);
			this.rightPanel.Name = "rightPanel";
			this.rightPanel.Size = new System.Drawing.Size(26, 27);
			this.rightPanel.TabIndex = 6;
			// 
			// MultiChoiceParameterPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label);
			this.Controls.Add(this.tableLayoutPanel);
			this.Controls.Add(this.rightPanel);
			this.Name = "MultiChoiceParameterPanel";
			this.Size = new System.Drawing.Size(495, 27);
			this.tableLayoutPanel.ResumeLayout(false);
			this.rightPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Label label;
		private System.Windows.Forms.CheckBox cbEnabled;
		private System.Windows.Forms.Panel checkBoxPanel0;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.Panel checkBoxPanel2;
		private System.Windows.Forms.Panel checkBoxPanel1;
		private System.Windows.Forms.Panel rightPanel;
	}
}
