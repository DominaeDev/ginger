namespace Ginger
{
	partial class LoreBookParameterPanel
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
			this.centerPanel = new System.Windows.Forms.Panel();
			this.bottomPanel = new System.Windows.Forms.Panel();
			this.btnAddEntry = new System.Windows.Forms.Button();
			this.pageChanger = new Ginger.PageChanger();
			this.spacer = new System.Windows.Forms.Panel();
			this.bottomPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// centerPanel
			// 
			this.centerPanel.Location = new System.Drawing.Point(2, 3);
			this.centerPanel.Margin = new System.Windows.Forms.Padding(0);
			this.centerPanel.Name = "centerPanel";
			this.centerPanel.Size = new System.Drawing.Size(439, 180);
			this.centerPanel.TabIndex = 0;
			// 
			// bottomPanel
			// 
			this.bottomPanel.Controls.Add(this.btnAddEntry);
			this.bottomPanel.Controls.Add(this.spacer);
			this.bottomPanel.Controls.Add(this.pageChanger);
			this.bottomPanel.Location = new System.Drawing.Point(0, 186);
			this.bottomPanel.Name = "bottomPanel";
			this.bottomPanel.Padding = new System.Windows.Forms.Padding(3);
			this.bottomPanel.Size = new System.Drawing.Size(443, 30);
			this.bottomPanel.TabIndex = 1;
			// 
			// btnAddEntry
			// 
			this.btnAddEntry.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnAddEntry.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnAddEntry.Location = new System.Drawing.Point(3, 3);
			this.btnAddEntry.Name = "btnAddEntry";
			this.btnAddEntry.Size = new System.Drawing.Size(264, 24);
			this.btnAddEntry.TabIndex = 0;
			this.btnAddEntry.Text = "Add entry";
			this.btnAddEntry.UseVisualStyleBackColor = true;
			this.btnAddEntry.Click += new System.EventHandler(this.OnAddEntry);
			// 
			// pageChanger
			// 
			this.pageChanger.Dock = System.Windows.Forms.DockStyle.Right;
			this.pageChanger.Location = new System.Drawing.Point(271, 3);
			this.pageChanger.Name = "pageChanger";
			this.pageChanger.Size = new System.Drawing.Size(169, 24);
			this.pageChanger.TabIndex = 0;
			// 
			// spacer
			// 
			this.spacer.Dock = System.Windows.Forms.DockStyle.Right;
			this.spacer.Location = new System.Drawing.Point(267, 3);
			this.spacer.Name = "spacer";
			this.spacer.Size = new System.Drawing.Size(4, 24);
			this.spacer.TabIndex = 0;
			// 
			// LoreBookParameterPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.Controls.Add(this.bottomPanel);
			this.Controls.Add(this.centerPanel);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.MinimumSize = new System.Drawing.Size(180, 25);
			this.Name = "LoreBookParameterPanel";
			this.Size = new System.Drawing.Size(446, 219);
			this.bottomPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Panel centerPanel;
		private System.Windows.Forms.Button btnAddEntry;
		private System.Windows.Forms.Panel bottomPanel;
		private System.Windows.Forms.Panel spacer;
		private PageChanger pageChanger;
	}
}
