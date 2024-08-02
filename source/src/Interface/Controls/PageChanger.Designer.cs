
namespace Ginger
{
	partial class PageChanger
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
			this.btnPrev = new System.Windows.Forms.Button();
			this.btnNext = new System.Windows.Forms.Button();
			this.label_Pages = new System.Windows.Forms.Label();
			this.textBox_Page = new Ginger.TextBoxEx();
			this.SuspendLayout();
			// 
			// btnPrev
			// 
			this.btnPrev.Dock = System.Windows.Forms.DockStyle.Left;
			this.btnPrev.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnPrev.Image = global::Ginger.Properties.Resources.arrow_left;
			this.btnPrev.Location = new System.Drawing.Point(0, 0);
			this.btnPrev.Name = "btnPrev";
			this.btnPrev.Size = new System.Drawing.Size(35, 24);
			this.btnPrev.TabIndex = 0;
			this.btnPrev.UseVisualStyleBackColor = true;
			this.btnPrev.Click += new System.EventHandler(this.btnPrev_Click);
			// 
			// btnNext
			// 
			this.btnNext.Dock = System.Windows.Forms.DockStyle.Right;
			this.btnNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnNext.Image = global::Ginger.Properties.Resources.arrow_right;
			this.btnNext.Location = new System.Drawing.Point(125, 0);
			this.btnNext.Name = "btnNext";
			this.btnNext.Size = new System.Drawing.Size(35, 24);
			this.btnNext.TabIndex = 2;
			this.btnNext.UseVisualStyleBackColor = true;
			this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
			// 
			// label_Pages
			// 
			this.label_Pages.Location = new System.Drawing.Point(82, 4);
			this.label_Pages.Name = "label_Pages";
			this.label_Pages.Size = new System.Drawing.Size(37, 13);
			this.label_Pages.TabIndex = 4;
			this.label_Pages.Text = "/ 10";
			// 
			// textBox_Page
			// 
			this.textBox_Page.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.textBox_Page.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBox_Page.Location = new System.Drawing.Point(55, 0);
			this.textBox_Page.Name = "textBox_Page";
			this.textBox_Page.Placeholder = null;
			this.textBox_Page.ShortcutsEnabled = false;
			this.textBox_Page.Size = new System.Drawing.Size(24, 23);
			this.textBox_Page.TabIndex = 1;
			this.textBox_Page.Text = "1";
			this.textBox_Page.EnterPressed += new System.EventHandler(this.textBox_Page_EnterPressed);
			// 
			// PageChanger
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label_Pages);
			this.Controls.Add(this.btnPrev);
			this.Controls.Add(this.btnNext);
			this.Controls.Add(this.textBox_Page);
			this.Name = "PageChanger";
			this.Size = new System.Drawing.Size(160, 24);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnPrev;
		private System.Windows.Forms.Button btnNext;
		private TextBoxEx textBox_Page;
		private System.Windows.Forms.Label label_Pages;
	}
}
