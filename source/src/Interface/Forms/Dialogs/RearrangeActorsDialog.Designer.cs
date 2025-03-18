
namespace Ginger
{
	partial class RearrangeActorsDialog
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
			System.Windows.Forms.Panel listPanel;
			System.Windows.Forms.Panel leftPanel;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RearrangeActorsDialog));
			this.lbActors = new System.Windows.Forms.ListBox();
			this.btnApply = new Ginger.ButtonEx();
			this.btnCancel = new Ginger.ButtonEx();
			this.btnMoveDown = new Ginger.ButtonEx();
			this.btnMoveUp = new Ginger.ButtonEx();
			this.exportFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.importFileDialog = new System.Windows.Forms.OpenFileDialog();
			listPanel = new System.Windows.Forms.Panel();
			leftPanel = new System.Windows.Forms.Panel();
			listPanel.SuspendLayout();
			leftPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// listPanel
			// 
			listPanel.Controls.Add(this.lbActors);
			listPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			listPanel.Location = new System.Drawing.Point(0, 0);
			listPanel.Name = "listPanel";
			listPanel.Padding = new System.Windows.Forms.Padding(4, 4, 0, 4);
			listPanel.Size = new System.Drawing.Size(284, 261);
			listPanel.TabIndex = 0;
			// 
			// lbActors
			// 
			this.lbActors.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.lbActors.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lbActors.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbActors.IntegralHeight = false;
			this.lbActors.ItemHeight = 20;
			this.lbActors.Items.AddRange(new object[] {
            "Adam",
            "Becky",
            "Cindy"});
			this.lbActors.Location = new System.Drawing.Point(4, 4);
			this.lbActors.Name = "lbActors";
			this.lbActors.Size = new System.Drawing.Size(280, 253);
			this.lbActors.TabIndex = 0;
			this.lbActors.SelectedIndexChanged += new System.EventHandler(this.lbActors_SelectedIndexChanged);
			// 
			// leftPanel
			// 
			leftPanel.Controls.Add(this.btnApply);
			leftPanel.Controls.Add(this.btnCancel);
			leftPanel.Controls.Add(this.btnMoveDown);
			leftPanel.Controls.Add(this.btnMoveUp);
			leftPanel.Dock = System.Windows.Forms.DockStyle.Right;
			leftPanel.Location = new System.Drawing.Point(284, 0);
			leftPanel.Name = "leftPanel";
			leftPanel.Padding = new System.Windows.Forms.Padding(4);
			leftPanel.Size = new System.Drawing.Size(200, 261);
			leftPanel.TabIndex = 0;
			// 
			// btnApply
			// 
			this.btnApply.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.btnApply.Highlighted = false;
			this.btnApply.Location = new System.Drawing.Point(4, 195);
			this.btnApply.Name = "btnApply";
			this.btnApply.Size = new System.Drawing.Size(192, 31);
			this.btnApply.TabIndex = 5;
			this.btnApply.Text = "Confirm";
			this.btnApply.UseVisualStyleBackColor = true;
			this.btnApply.Click += new System.EventHandler(this.btnConfirm_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.btnCancel.Highlighted = false;
			this.btnCancel.Location = new System.Drawing.Point(4, 226);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(192, 31);
			this.btnCancel.TabIndex = 8;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// btnMoveDown
			// 
			this.btnMoveDown.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnMoveDown.Highlighted = false;
			this.btnMoveDown.Location = new System.Drawing.Point(4, 35);
			this.btnMoveDown.Name = "btnMoveDown";
			this.btnMoveDown.Size = new System.Drawing.Size(192, 31);
			this.btnMoveDown.TabIndex = 12;
			this.btnMoveDown.Text = "Move down";
			this.btnMoveDown.UseVisualStyleBackColor = true;
			this.btnMoveDown.Click += new System.EventHandler(this.btnMoveDown_Click);
			// 
			// btnMoveUp
			// 
			this.btnMoveUp.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnMoveUp.Highlighted = false;
			this.btnMoveUp.Location = new System.Drawing.Point(4, 4);
			this.btnMoveUp.Name = "btnMoveUp";
			this.btnMoveUp.Size = new System.Drawing.Size(192, 31);
			this.btnMoveUp.TabIndex = 11;
			this.btnMoveUp.Text = "Move up";
			this.btnMoveUp.UseVisualStyleBackColor = true;
			this.btnMoveUp.Click += new System.EventHandler(this.btnMoveUp_Click);
			// 
			// importFileDialog
			// 
			this.importFileDialog.Multiselect = true;
			// 
			// RearrangeActorsDialog
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(484, 261);
			this.Controls.Add(listPanel);
			this.Controls.Add(leftPanel);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ForeColor = System.Drawing.SystemColors.ControlText;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(500, 240);
			this.Name = "RearrangeActorsDialog";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Rearrange actors";
			listPanel.ResumeLayout(false);
			leftPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private ButtonEx btnApply;
		private System.Windows.Forms.SaveFileDialog exportFileDialog;
		private System.Windows.Forms.OpenFileDialog importFileDialog;
		private ButtonEx btnCancel;
		private System.Windows.Forms.ListBox lbActors;
		private ButtonEx btnMoveDown;
		private ButtonEx btnMoveUp;
	}
}