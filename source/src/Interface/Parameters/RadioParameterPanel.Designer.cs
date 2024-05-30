namespace Ginger
{
	partial class RadioParameterPanel
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
			this.radioPanel2 = new System.Windows.Forms.Panel();
			this.radioPanel1 = new System.Windows.Forms.Panel();
			this.radioPanel0 = new System.Windows.Forms.Panel();
			this.cbEnabled = new System.Windows.Forms.CheckBox();
			this.rightPanel = new System.Windows.Forms.Panel();
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.rightPanel.SuspendLayout();
			this.tableLayoutPanel.SuspendLayout();
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
			// radioPanel2
			// 
			this.radioPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.radioPanel2.Location = new System.Drawing.Point(216, 0);
			this.radioPanel2.Margin = new System.Windows.Forms.Padding(0);
			this.radioPanel2.Name = "radioPanel2";
			this.radioPanel2.Padding = new System.Windows.Forms.Padding(0, 2, 8, 0);
			this.radioPanel2.Size = new System.Drawing.Size(110, 25);
			this.radioPanel2.TabIndex = 2;
			// 
			// radioPanel1
			// 
			this.radioPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.radioPanel1.Location = new System.Drawing.Point(108, 0);
			this.radioPanel1.Margin = new System.Windows.Forms.Padding(0);
			this.radioPanel1.Name = "radioPanel1";
			this.radioPanel1.Padding = new System.Windows.Forms.Padding(0, 2, 8, 0);
			this.radioPanel1.Size = new System.Drawing.Size(108, 25);
			this.radioPanel1.TabIndex = 1;
			// 
			// radioPanel0
			// 
			this.radioPanel0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.radioPanel0.Location = new System.Drawing.Point(0, 0);
			this.radioPanel0.Margin = new System.Windows.Forms.Padding(0);
			this.radioPanel0.Name = "radioPanel0";
			this.radioPanel0.Padding = new System.Windows.Forms.Padding(0, 2, 8, 0);
			this.radioPanel0.Size = new System.Drawing.Size(108, 25);
			this.radioPanel0.TabIndex = 0;
			// 
			// cbEnabled
			// 
			this.cbEnabled.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.cbEnabled.Dock = System.Windows.Forms.DockStyle.Top;
			this.cbEnabled.Location = new System.Drawing.Point(0, 0);
			this.cbEnabled.Name = "cbEnabled";
			this.cbEnabled.Padding = new System.Windows.Forms.Padding(3, 0, 3, 3);
			this.cbEnabled.Size = new System.Drawing.Size(26, 21);
			this.cbEnabled.TabIndex = 3;
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
			this.rightPanel.Size = new System.Drawing.Size(26, 30);
			this.rightPanel.TabIndex = 6;
			// 
			// tableLayoutPanel
			// 
			this.tableLayoutPanel.ColumnCount = 3;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel.Controls.Add(this.radioPanel2, 2, 0);
			this.tableLayoutPanel.Controls.Add(this.radioPanel1, 1, 0);
			this.tableLayoutPanel.Controls.Add(this.radioPanel0, 0, 0);
			this.tableLayoutPanel.Location = new System.Drawing.Point(140, 0);
			this.tableLayoutPanel.MinimumSize = new System.Drawing.Size(0, 25);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 1;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.Size = new System.Drawing.Size(326, 25);
			this.tableLayoutPanel.TabIndex = 0;
			this.tableLayoutPanel.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnMouseClick);
			// 
			// RadioParameterPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label);
			this.Controls.Add(this.tableLayoutPanel);
			this.Controls.Add(this.rightPanel);
			this.Name = "RadioParameterPanel";
			this.Size = new System.Drawing.Size(495, 30);
			this.rightPanel.ResumeLayout(false);
			this.tableLayoutPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Label label;
		private System.Windows.Forms.CheckBox cbEnabled;
		private System.Windows.Forms.Panel radioPanel0;
		private System.Windows.Forms.Panel radioPanel2;
		private System.Windows.Forms.Panel radioPanel1;
		private System.Windows.Forms.Panel rightPanel;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
	}
}
