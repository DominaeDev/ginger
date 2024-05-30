namespace Ginger
{
	partial class ChoiceSliderParameterPanel
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
			this.textBox = new Ginger.TextBoxEx();
			this.slider = new Ginger.TrackBarEx();
			this.cbEnabled = new System.Windows.Forms.CheckBox();
			this.rightPanel = new System.Windows.Forms.Panel();
			this.valuePanel = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this.slider)).BeginInit();
			this.rightPanel.SuspendLayout();
			this.valuePanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// label
			// 
			this.label.AutoEllipsis = true;
			this.label.Font = new System.Drawing.Font("Segoe UI", 9.75F);
			this.label.Location = new System.Drawing.Point(2, 2);
			this.label.Margin = new System.Windows.Forms.Padding(2);
			this.label.Name = "label";
			this.label.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			this.label.Size = new System.Drawing.Size(136, 21);
			this.label.TabIndex = 0;
			this.label.Text = "Label";
			this.label.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnMouseClick);
			// 
			// textBox
			// 
			this.textBox.BackColor = System.Drawing.SystemColors.Window;
			this.textBox.Dock = System.Windows.Forms.DockStyle.Left;
			this.textBox.Location = new System.Drawing.Point(0, 2);
			this.textBox.Margin = new System.Windows.Forms.Padding(2);
			this.textBox.Name = "textBox";
			this.textBox.Placeholder = null;
			this.textBox.ReadOnly = true;
			this.textBox.Size = new System.Drawing.Size(140, 20);
			this.textBox.TabIndex = 0;
			// 
			// slider
			// 
			this.slider.Dock = System.Windows.Forms.DockStyle.Fill;
			this.slider.Location = new System.Drawing.Point(140, 2);
			this.slider.Margin = new System.Windows.Forms.Padding(0);
			this.slider.Name = "slider";
			this.slider.Size = new System.Drawing.Size(186, 23);
			this.slider.TabIndex = 1;
			this.slider.TabStop = false;
			this.slider.ValueChanged += new System.EventHandler(this.slider_ValueChanged);
			// 
			// cbEnabled
			// 
			this.cbEnabled.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.cbEnabled.Dock = System.Windows.Forms.DockStyle.Top;
			this.cbEnabled.Location = new System.Drawing.Point(0, 0);
			this.cbEnabled.Name = "cbEnabled";
			this.cbEnabled.Padding = new System.Windows.Forms.Padding(3, 0, 3, 3);
			this.cbEnabled.Size = new System.Drawing.Size(26, 21);
			this.cbEnabled.TabIndex = 2;
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
			this.rightPanel.Size = new System.Drawing.Size(26, 25);
			this.rightPanel.TabIndex = 4;
			// 
			// valuePanel
			// 
			this.valuePanel.Controls.Add(this.slider);
			this.valuePanel.Controls.Add(this.textBox);
			this.valuePanel.Location = new System.Drawing.Point(140, 0);
			this.valuePanel.Name = "valuePanel";
			this.valuePanel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 0);
			this.valuePanel.Size = new System.Drawing.Size(326, 25);
			this.valuePanel.TabIndex = 3;
			// 
			// ChoiceSliderParameterPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.valuePanel);
			this.Controls.Add(this.label);
			this.Controls.Add(this.rightPanel);
			this.Name = "ChoiceSliderParameterPanel";
			this.Size = new System.Drawing.Size(495, 25);
			((System.ComponentModel.ISupportInitialize)(this.slider)).EndInit();
			this.rightPanel.ResumeLayout(false);
			this.valuePanel.ResumeLayout(false);
			this.valuePanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Label label;
		private TextBoxEx textBox;
		private System.Windows.Forms.CheckBox cbEnabled;
		private Ginger.TrackBarEx slider;
		private System.Windows.Forms.Panel rightPanel;
		private System.Windows.Forms.Panel valuePanel;
	}
}
