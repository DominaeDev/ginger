namespace Ginger
{
	partial class RecipePanel
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
			this.components = new System.ComponentModel.Container();
			this.btnExpand = new System.Windows.Forms.PictureBox();
			this.btnUp = new System.Windows.Forms.PictureBox();
			this.btnDown = new System.Windows.Forms.PictureBox();
			this.btn_Remove = new System.Windows.Forms.PictureBox();
			this.labelTitle = new System.Windows.Forms.Label();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.header = new System.Windows.Forms.Panel();
			this.parameters = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this.btnExpand)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.btnUp)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.btnDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.btn_Remove)).BeginInit();
			this.header.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnExpand
			// 
			this.btnExpand.BackColor = System.Drawing.Color.Transparent;
			this.btnExpand.Location = new System.Drawing.Point(0, 0);
			this.btnExpand.Name = "btnExpand";
			this.btnExpand.Size = new System.Drawing.Size(28, 28);
			this.btnExpand.TabIndex = 5;
			this.btnExpand.TabStop = false;
			this.btnExpand.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnClickHeader);
			this.btnExpand.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.OnClickHeader);
			// 
			// btnUp
			// 
			this.btnUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnUp.BackColor = System.Drawing.Color.Transparent;
			this.btnUp.Image = global::Ginger.Properties.Resources.arrow_up;
			this.btnUp.Location = new System.Drawing.Point(276, 0);
			this.btnUp.Name = "btnUp";
			this.btnUp.Size = new System.Drawing.Size(28, 28);
			this.btnUp.TabIndex = 7;
			this.btnUp.TabStop = false;
			this.btnUp.MouseClick += new System.Windows.Forms.MouseEventHandler(this.BtnUp_MouseClick);
			// 
			// btnDown
			// 
			this.btnDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnDown.BackColor = System.Drawing.Color.Transparent;
			this.btnDown.Image = global::Ginger.Properties.Resources.arrow_down;
			this.btnDown.Location = new System.Drawing.Point(304, 0);
			this.btnDown.Name = "btnDown";
			this.btnDown.Size = new System.Drawing.Size(28, 28);
			this.btnDown.TabIndex = 8;
			this.btnDown.TabStop = false;
			this.btnDown.MouseClick += new System.Windows.Forms.MouseEventHandler(this.BtnDown_MouseClick);
			// 
			// btn_Remove
			// 
			this.btn_Remove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btn_Remove.BackColor = System.Drawing.Color.Transparent;
			this.btn_Remove.Image = global::Ginger.Properties.Resources.cross;
			this.btn_Remove.Location = new System.Drawing.Point(332, 0);
			this.btn_Remove.Name = "btn_Remove";
			this.btn_Remove.Size = new System.Drawing.Size(28, 28);
			this.btn_Remove.TabIndex = 6;
			this.btn_Remove.TabStop = false;
			this.btn_Remove.MouseClick += new System.Windows.Forms.MouseEventHandler(this.Btn_Remove_MouseClick);
			// 
			// labelTitle
			// 
			this.labelTitle.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.labelTitle.AutoSize = true;
			this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 11F);
			this.labelTitle.Location = new System.Drawing.Point(29, 3);
			this.labelTitle.Name = "labelTitle";
			this.labelTitle.Size = new System.Drawing.Size(54, 20);
			this.labelTitle.TabIndex = 0;
			this.labelTitle.Text = "Recipe";
			this.labelTitle.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnClickHeader);
			this.labelTitle.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.OnClickHeader);
			// 
			// header
			// 
			this.header.Controls.Add(this.labelTitle);
			this.header.Controls.Add(this.btn_Remove);
			this.header.Controls.Add(this.btnDown);
			this.header.Controls.Add(this.btnUp);
			this.header.Controls.Add(this.btnExpand);
			this.header.Location = new System.Drawing.Point(0, 0);
			this.header.Margin = new System.Windows.Forms.Padding(0);
			this.header.Name = "header";
			this.header.Size = new System.Drawing.Size(360, 28);
			this.header.TabIndex = 9;
			this.header.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnClickHeader);
			this.header.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.OnClickHeader);
			// 
			// parameters
			// 
			this.parameters.Location = new System.Drawing.Point(0, 26);
			this.parameters.Name = "parameters";
			this.parameters.Size = new System.Drawing.Size(360, 115);
			this.parameters.TabIndex = 0;
			// 
			// RecipePanel
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.parameters);
			this.Controls.Add(this.header);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.Name = "RecipePanel";
			this.Size = new System.Drawing.Size(360, 141);
			((System.ComponentModel.ISupportInitialize)(this.btnExpand)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.btnUp)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.btnDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.btn_Remove)).EndInit();
			this.header.ResumeLayout(false);
			this.header.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Label labelTitle;
		private System.Windows.Forms.PictureBox btnExpand;
		private System.Windows.Forms.PictureBox btn_Remove;
		private System.Windows.Forms.PictureBox btnUp;
		private System.Windows.Forms.PictureBox btnDown;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.Panel header;
		private System.Windows.Forms.Panel parameters;
	}
}
