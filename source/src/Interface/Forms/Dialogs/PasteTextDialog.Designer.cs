
namespace Ginger
{
	partial class PasteTextDialog
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
			System.Windows.Forms.Label label;
			this.btnCancel = new ButtonEx();
			this.btnOk = new ButtonEx();
			this.comboBox = new Ginger.ComboBoxEx();
			buttonLayout = new System.Windows.Forms.FlowLayoutPanel();
			label = new System.Windows.Forms.Label();
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
			buttonLayout.Location = new System.Drawing.Point(9, 59);
			buttonLayout.Margin = new System.Windows.Forms.Padding(0);
			buttonLayout.Name = "buttonLayout";
			buttonLayout.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			buttonLayout.Size = new System.Drawing.Size(345, 42);
			buttonLayout.TabIndex = 6;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.Location = new System.Drawing.Point(225, 9);
			this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(117, 30);
			this.btnCancel.TabIndex = 2;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(102, 9);
			this.btnOk.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(117, 30);
			this.btnOk.TabIndex = 1;
			this.btnOk.Text = "Paste";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// label
			// 
			label.AutoEllipsis = true;
			label.Dock = System.Windows.Forms.DockStyle.Top;
			label.Location = new System.Drawing.Point(9, 4);
			label.Margin = new System.Windows.Forms.Padding(0);
			label.MinimumSize = new System.Drawing.Size(136, 27);
			label.Name = "label";
			label.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			label.Size = new System.Drawing.Size(345, 30);
			label.TabIndex = 8;
			label.Text = "Paste text as:";
			// 
			// comboBox
			// 
			this.comboBox.Dock = System.Windows.Forms.DockStyle.Top;
			this.comboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.comboBox.FormattingEnabled = true;
			this.comboBox.Items.AddRange(new object[] {
            "Model instructions",
            "Character persona",
            "User persona",
            "Scenario",
            "Greeting",
            "Example chat",
            "Grammar"});
			this.comboBox.Location = new System.Drawing.Point(9, 34);
			this.comboBox.Margin = new System.Windows.Forms.Padding(0);
			this.comboBox.MaxLength = 128;
			this.comboBox.Name = "comboBox";
			this.comboBox.Size = new System.Drawing.Size(345, 25);
			this.comboBox.TabIndex = 9;
			// 
			// PasteTextDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(363, 105);
			this.Controls.Add(this.comboBox);
			this.Controls.Add(label);
			this.Controls.Add(buttonLayout);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.Name = "PasteTextDialog";
			this.Padding = new System.Windows.Forms.Padding(9, 4, 9, 4);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Paste";
			buttonLayout.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private ButtonEx btnCancel;
		private ButtonEx btnOk;
		private ComboBoxEx comboBox;
	}
}