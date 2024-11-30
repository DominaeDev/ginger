
namespace Ginger
{
	partial class ProgressBarDialog
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
			this.btnCancel = new Ginger.ButtonEx();
			this.labelText = new System.Windows.Forms.Label();
			this.progressBar = new System.Windows.Forms.ProgressBar();
			buttonLayout = new System.Windows.Forms.FlowLayoutPanel();
			buttonLayout.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonLayout
			// 
			buttonLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			buttonLayout.Controls.Add(this.btnCancel);
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
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(225, 9);
			this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(117, 30);
			this.btnCancel.TabIndex = 2;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// labelText
			// 
			this.labelText.AutoEllipsis = true;
			this.labelText.Dock = System.Windows.Forms.DockStyle.Top;
			this.labelText.Location = new System.Drawing.Point(9, 4);
			this.labelText.Margin = new System.Windows.Forms.Padding(0);
			this.labelText.MinimumSize = new System.Drawing.Size(136, 27);
			this.labelText.Name = "labelText";
			this.labelText.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			this.labelText.Size = new System.Drawing.Size(345, 30);
			this.labelText.TabIndex = 8;
			this.labelText.Text = "Exporting...";
			// 
			// progressBar
			// 
			this.progressBar.Dock = System.Windows.Forms.DockStyle.Top;
			this.progressBar.Location = new System.Drawing.Point(9, 34);
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new System.Drawing.Size(345, 23);
			this.progressBar.TabIndex = 9;
			// 
			// ProgressBarDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(363, 105);
			this.ControlBox = false;
			this.Controls.Add(this.progressBar);
			this.Controls.Add(this.labelText);
			this.Controls.Add(buttonLayout);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ProgressBarDialog";
			this.Padding = new System.Windows.Forms.Padding(9, 4, 9, 4);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Please wait";
			buttonLayout.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private ButtonEx btnCancel;
		private System.Windows.Forms.ProgressBar progressBar;
		private System.Windows.Forms.Label labelText;
	}
}