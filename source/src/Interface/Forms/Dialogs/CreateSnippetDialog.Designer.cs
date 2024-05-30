namespace Ginger
{
	partial class CreateSnippetDialog
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
			System.Windows.Forms.Label labelName;
			System.Windows.Forms.Label labelText;
			System.Windows.Forms.FlowLayoutPanel buttonLayout;
			System.Windows.Forms.TableLayoutPanel topLayout;
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.panelList = new System.Windows.Forms.Panel();
			this.cbSwapPronouns = new System.Windows.Forms.CheckBox();
			this.bottomPanel = new System.Windows.Forms.Panel();
			this.textBox = new Ginger.TextBoxEx();
			labelName = new System.Windows.Forms.Label();
			labelText = new System.Windows.Forms.Label();
			buttonLayout = new System.Windows.Forms.FlowLayoutPanel();
			topLayout = new System.Windows.Forms.TableLayoutPanel();
			buttonLayout.SuspendLayout();
			topLayout.SuspendLayout();
			this.bottomPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// labelName
			// 
			labelName.AutoEllipsis = true;
			labelName.Dock = System.Windows.Forms.DockStyle.Top;
			labelName.Location = new System.Drawing.Point(0, 0);
			labelName.Margin = new System.Windows.Forms.Padding(0);
			labelName.MinimumSize = new System.Drawing.Size(117, 21);
			labelName.Name = "labelName";
			labelName.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			labelName.Size = new System.Drawing.Size(668, 23);
			labelName.TabIndex = 4;
			labelName.Text = "Name";
			// 
			// labelText
			// 
			labelText.AutoEllipsis = true;
			labelText.Dock = System.Windows.Forms.DockStyle.Top;
			labelText.Location = new System.Drawing.Point(8, 57);
			labelText.Margin = new System.Windows.Forms.Padding(0);
			labelText.MinimumSize = new System.Drawing.Size(117, 21);
			labelText.Name = "labelText";
			labelText.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			labelText.Size = new System.Drawing.Size(668, 23);
			labelText.TabIndex = 6;
			labelText.Text = "Content";
			// 
			// buttonLayout
			// 
			buttonLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			buttonLayout.Controls.Add(this.btnCancel);
			buttonLayout.Controls.Add(this.btnOk);
			buttonLayout.Dock = System.Windows.Forms.DockStyle.Top;
			buttonLayout.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			buttonLayout.Location = new System.Drawing.Point(0, 0);
			buttonLayout.Margin = new System.Windows.Forms.Padding(0);
			buttonLayout.Name = "buttonLayout";
			buttonLayout.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
			buttonLayout.Size = new System.Drawing.Size(668, 41);
			buttonLayout.TabIndex = 2;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.Location = new System.Drawing.Point(534, 7);
			this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(131, 30);
			this.btnCancel.TabIndex = 1;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(397, 7);
			this.btnOk.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(131, 30);
			this.btnOk.TabIndex = 0;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
			// 
			// topLayout
			// 
			topLayout.ColumnCount = 1;
			topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			topLayout.Controls.Add(this.textBox, 0, 1);
			topLayout.Controls.Add(labelName, 0, 0);
			topLayout.Dock = System.Windows.Forms.DockStyle.Top;
			topLayout.Location = new System.Drawing.Point(8, 3);
			topLayout.Margin = new System.Windows.Forms.Padding(0);
			topLayout.Name = "topLayout";
			topLayout.RowCount = 2;
			topLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
			topLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			topLayout.Size = new System.Drawing.Size(668, 54);
			topLayout.TabIndex = 0;
			// 
			// panelList
			// 
			this.panelList.AutoScroll = true;
			this.panelList.BackColor = System.Drawing.SystemColors.ControlDark;
			this.panelList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelList.Dock = System.Windows.Forms.DockStyle.Top;
			this.panelList.Location = new System.Drawing.Point(8, 80);
			this.panelList.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.panelList.Name = "panelList";
			this.panelList.Size = new System.Drawing.Size(668, 279);
			this.panelList.TabIndex = 1;
			this.panelList.TabStop = true;
			// 
			// cbSwapPronouns
			// 
			this.cbSwapPronouns.AutoSize = true;
			this.cbSwapPronouns.Location = new System.Drawing.Point(0, 12);
			this.cbSwapPronouns.Name = "cbSwapPronouns";
			this.cbSwapPronouns.Size = new System.Drawing.Size(194, 21);
			this.cbSwapPronouns.TabIndex = 7;
			this.cbSwapPronouns.Text = "Replace gendered pronouns";
			this.cbSwapPronouns.UseVisualStyleBackColor = true;
			this.cbSwapPronouns.CheckedChanged += new System.EventHandler(this.cbSwapPronouns_CheckedChanged);
			// 
			// bottomPanel
			// 
			this.bottomPanel.Controls.Add(this.cbSwapPronouns);
			this.bottomPanel.Controls.Add(buttonLayout);
			this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.bottomPanel.Location = new System.Drawing.Point(8, 359);
			this.bottomPanel.Name = "bottomPanel";
			this.bottomPanel.Size = new System.Drawing.Size(668, 41);
			this.bottomPanel.TabIndex = 2;
			// 
			// textBox
			// 
			this.textBox.AcceptsReturn = true;
			this.textBox.Dock = System.Windows.Forms.DockStyle.Top;
			this.textBox.Location = new System.Drawing.Point(0, 23);
			this.textBox.Margin = new System.Windows.Forms.Padding(0);
			this.textBox.MaxLength = 64;
			this.textBox.Name = "textBox";
			this.textBox.Placeholder = "Folder/Name";
			this.textBox.Size = new System.Drawing.Size(668, 25);
			this.textBox.TabIndex = 0;
			this.textBox.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.TextBox_PreviewKeyDown);
			// 
			// CreateSnippetDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ClientSize = new System.Drawing.Size(684, 407);
			this.Controls.Add(this.bottomPanel);
			this.Controls.Add(this.panelList);
			this.Controls.Add(labelText);
			this.Controls.Add(topLayout);
			this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(700, 39);
			this.Name = "CreateSnippetDialog";
			this.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Create snippet";
			buttonLayout.ResumeLayout(false);
			topLayout.ResumeLayout(false);
			topLayout.PerformLayout();
			this.bottomPanel.ResumeLayout(false);
			this.bottomPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Panel panelList;
		private TextBoxEx textBox;
		private System.Windows.Forms.CheckBox cbSwapPronouns;
		private System.Windows.Forms.Panel bottomPanel;
	}
}