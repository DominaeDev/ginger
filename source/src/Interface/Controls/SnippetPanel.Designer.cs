namespace Ginger
{
	partial class SnippetPanel
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
			this.labelChannel = new System.Windows.Forms.Label();
			this.tableLayout = new System.Windows.Forms.TableLayoutPanel();
			this.btn_Remove = new System.Windows.Forms.PictureBox();
			this.textBox_Text = new Ginger.FlatRichTextBox();
			this.tableLayout.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.btn_Remove)).BeginInit();
			this.SuspendLayout();
			// 
			// labelChannel
			// 
			this.labelChannel.AutoEllipsis = true;
			this.labelChannel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelChannel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelChannel.Location = new System.Drawing.Point(2, 2);
			this.labelChannel.Margin = new System.Windows.Forms.Padding(2);
			this.labelChannel.MinimumSize = new System.Drawing.Size(100, 16);
			this.labelChannel.Name = "labelChannel";
			this.labelChannel.Size = new System.Drawing.Size(441, 17);
			this.labelChannel.TabIndex = 0;
			this.labelChannel.Text = "Persona";
			// 
			// tableLayout
			// 
			this.tableLayout.ColumnCount = 2;
			this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
			this.tableLayout.Controls.Add(this.labelChannel, 0, 0);
			this.tableLayout.Controls.Add(this.btn_Remove, 1, 0);
			this.tableLayout.Dock = System.Windows.Forms.DockStyle.Top;
			this.tableLayout.Location = new System.Drawing.Point(0, 0);
			this.tableLayout.Margin = new System.Windows.Forms.Padding(0);
			this.tableLayout.Name = "tableLayout";
			this.tableLayout.RowCount = 1;
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayout.Size = new System.Drawing.Size(495, 21);
			this.tableLayout.TabIndex = 0;
			// 
			// btn_Remove
			// 
			this.btn_Remove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btn_Remove.BackColor = System.Drawing.Color.Transparent;
			this.btn_Remove.Image = global::Ginger.Properties.Resources.delete_small;
			this.btn_Remove.Location = new System.Drawing.Point(471, 0);
			this.btn_Remove.Margin = new System.Windows.Forms.Padding(0);
			this.btn_Remove.Name = "btn_Remove";
			this.btn_Remove.Size = new System.Drawing.Size(24, 21);
			this.btn_Remove.TabIndex = 7;
			this.btn_Remove.TabStop = false;
			this.btn_Remove.MouseClick += new System.Windows.Forms.MouseEventHandler(this.Btn_Remove_MouseClick);
			// 
			// textBox_Text
			// 
			this.textBox_Text.Dock = System.Windows.Forms.DockStyle.Top;
			this.textBox_Text.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBox_Text.Location = new System.Drawing.Point(0, 21);
			this.textBox_Text.Margin = new System.Windows.Forms.Padding(0);
			this.textBox_Text.Multiline = true;
			this.textBox_Text.Name = "textBox_Text";
			this.textBox_Text.Padding = new System.Windows.Forms.Padding(1);
			this.textBox_Text.Placeholder = null;
			this.textBox_Text.SelectionLength = 0;
			this.textBox_Text.SelectionStart = 0;
			this.textBox_Text.Size = new System.Drawing.Size(495, 114);
			this.textBox_Text.SpellChecking = true;
			this.textBox_Text.SyntaxHighlighting = true;
			this.textBox_Text.TabIndex = 1;
			this.textBox_Text.TextSize = new System.Drawing.Size(0, 0);
			// 
			// SnippetPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.BackColor = System.Drawing.Color.Honeydew;
			this.Controls.Add(this.textBox_Text);
			this.Controls.Add(this.tableLayout);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.MinimumSize = new System.Drawing.Size(180, 25);
			this.Name = "SnippetPanel";
			this.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
			this.Size = new System.Drawing.Size(495, 139);
			this.tableLayout.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.btn_Remove)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Label labelChannel;
		private System.Windows.Forms.TableLayoutPanel tableLayout;
		private System.Windows.Forms.PictureBox btn_Remove;
		private FlatRichTextBox textBox_Text;
	}
}
