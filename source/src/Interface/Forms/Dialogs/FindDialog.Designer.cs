namespace Ginger
{
	partial class FindDialog
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.FlowLayoutPanel buttonLayout;
			System.Windows.Forms.Label label_Find;
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.cbWholeWords = new System.Windows.Forms.CheckBox();
			this.cbMatchCase = new System.Windows.Forms.CheckBox();
			this.textBox_Find = new Ginger.TextBoxEx();
			buttonLayout = new System.Windows.Forms.FlowLayoutPanel();
			label_Find = new System.Windows.Forms.Label();
			buttonLayout.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonLayout
			// 
			buttonLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			buttonLayout.Controls.Add(this.btnCancel);
			buttonLayout.Controls.Add(this.btnOk);
			buttonLayout.Dock = System.Windows.Forms.DockStyle.Bottom;
			buttonLayout.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			buttonLayout.Location = new System.Drawing.Point(8, 80);
			buttonLayout.Margin = new System.Windows.Forms.Padding(0);
			buttonLayout.Name = "buttonLayout";
			buttonLayout.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
			buttonLayout.Size = new System.Drawing.Size(467, 41);
			buttonLayout.TabIndex = 5;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.Location = new System.Drawing.Point(364, 7);
			this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(100, 30);
			this.btnCancel.TabIndex = 2;
			this.btnCancel.Text = "Close";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(258, 7);
			this.btnOk.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(100, 30);
			this.btnOk.TabIndex = 1;
			this.btnOk.Text = "Find next";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
			// 
			// label_Find
			// 
			label_Find.AutoEllipsis = true;
			label_Find.Dock = System.Windows.Forms.DockStyle.Top;
			label_Find.Location = new System.Drawing.Point(8, 3);
			label_Find.Margin = new System.Windows.Forms.Padding(0);
			label_Find.MinimumSize = new System.Drawing.Size(117, 21);
			label_Find.Name = "label_Find";
			label_Find.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			label_Find.Size = new System.Drawing.Size(467, 23);
			label_Find.TabIndex = 4;
			label_Find.Text = "Find what:";
			// 
			// cbWholeWords
			// 
			this.cbWholeWords.AutoSize = true;
			this.cbWholeWords.Location = new System.Drawing.Point(8, 93);
			this.cbWholeWords.Name = "cbWholeWords";
			this.cbWholeWords.Size = new System.Drawing.Size(169, 21);
			this.cbWholeWords.TabIndex = 3;
			this.cbWholeWords.Text = "Match whole words only";
			this.cbWholeWords.UseVisualStyleBackColor = true;
			// 
			// cbMatchCase
			// 
			this.cbMatchCase.AutoSize = true;
			this.cbMatchCase.Checked = true;
			this.cbMatchCase.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbMatchCase.Location = new System.Drawing.Point(8, 66);
			this.cbMatchCase.Name = "cbMatchCase";
			this.cbMatchCase.Size = new System.Drawing.Size(93, 21);
			this.cbMatchCase.TabIndex = 2;
			this.cbMatchCase.Text = "Match case";
			this.cbMatchCase.UseVisualStyleBackColor = true;
			// 
			// textBox_Find
			// 
			this.textBox_Find.AcceptsReturn = true;
			this.textBox_Find.Dock = System.Windows.Forms.DockStyle.Top;
			this.textBox_Find.Location = new System.Drawing.Point(8, 26);
			this.textBox_Find.Name = "textBox_Find";
			this.textBox_Find.Placeholder = null;
			this.textBox_Find.Size = new System.Drawing.Size(467, 25);
			this.textBox_Find.TabIndex = 0;
			// 
			// FindDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.ClientSize = new System.Drawing.Size(483, 124);
			this.Controls.Add(this.cbWholeWords);
			this.Controls.Add(this.cbMatchCase);
			this.Controls.Add(buttonLayout);
			this.Controls.Add(this.textBox_Find);
			this.Controls.Add(label_Find);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FindDialog";
			this.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Find";
			buttonLayout.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private TextBoxEx textBox_Find;
		private System.Windows.Forms.CheckBox cbWholeWords;
		private System.Windows.Forms.CheckBox cbMatchCase;
	}
}