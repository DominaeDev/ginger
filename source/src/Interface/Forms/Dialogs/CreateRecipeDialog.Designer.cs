namespace Ginger
{
	partial class CreateRecipeDialog
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
			System.Windows.Forms.TableLayoutPanel topLayout;
			System.Windows.Forms.Label label_Name;
			System.Windows.Forms.Label label_Type;
			System.Windows.Forms.TableLayoutPanel centerLayout;
			System.Windows.Forms.Label label_Template;
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.textBox_Name = new Ginger.TextBoxEx();
			this.comboBox_Category = new Ginger.ComboBoxEx();
			this.comboBox_Template = new Ginger.ComboBoxEx();
			this.checkBoxOpenTextEditor = new System.Windows.Forms.CheckBox();
			buttonLayout = new System.Windows.Forms.FlowLayoutPanel();
			topLayout = new System.Windows.Forms.TableLayoutPanel();
			label_Name = new System.Windows.Forms.Label();
			label_Type = new System.Windows.Forms.Label();
			centerLayout = new System.Windows.Forms.TableLayoutPanel();
			label_Template = new System.Windows.Forms.Label();
			buttonLayout.SuspendLayout();
			topLayout.SuspendLayout();
			centerLayout.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonLayout
			// 
			buttonLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			buttonLayout.Controls.Add(this.btnCancel);
			buttonLayout.Controls.Add(this.btnOk);
			buttonLayout.Dock = System.Windows.Forms.DockStyle.Bottom;
			buttonLayout.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			buttonLayout.Location = new System.Drawing.Point(8, 113);
			buttonLayout.Margin = new System.Windows.Forms.Padding(0);
			buttonLayout.Name = "buttonLayout";
			buttonLayout.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
			buttonLayout.Size = new System.Drawing.Size(476, 41);
			buttonLayout.TabIndex = 3;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.Location = new System.Drawing.Point(342, 7);
			this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(131, 30);
			this.btnCancel.TabIndex = 2;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(205, 7);
			this.btnOk.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(131, 30);
			this.btnOk.TabIndex = 1;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
			// 
			// topLayout
			// 
			topLayout.ColumnCount = 3;
			topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 8F));
			topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
			topLayout.Controls.Add(this.textBox_Name, 0, 1);
			topLayout.Controls.Add(label_Name, 0, 0);
			topLayout.Controls.Add(this.comboBox_Category, 2, 1);
			topLayout.Controls.Add(label_Type, 2, 0);
			topLayout.Dock = System.Windows.Forms.DockStyle.Top;
			topLayout.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
			topLayout.Location = new System.Drawing.Point(8, 3);
			topLayout.Margin = new System.Windows.Forms.Padding(0);
			topLayout.Name = "topLayout";
			topLayout.RowCount = 2;
			topLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
			topLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			topLayout.Size = new System.Drawing.Size(476, 54);
			topLayout.TabIndex = 0;
			// 
			// textBox_Name
			// 
			this.textBox_Name.AcceptsReturn = true;
			this.textBox_Name.Dock = System.Windows.Forms.DockStyle.Top;
			this.textBox_Name.Location = new System.Drawing.Point(0, 23);
			this.textBox_Name.Margin = new System.Windows.Forms.Padding(0);
			this.textBox_Name.MaxLength = 64;
			this.textBox_Name.Name = "textBox_Name";
			this.textBox_Name.Placeholder = "Folder/Name";
			this.textBox_Name.Size = new System.Drawing.Size(368, 25);
			this.textBox_Name.TabIndex = 0;
			this.textBox_Name.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.TextBox_PreviewKeyDown);
			// 
			// label_Name
			// 
			label_Name.AutoEllipsis = true;
			label_Name.Dock = System.Windows.Forms.DockStyle.Top;
			label_Name.Location = new System.Drawing.Point(0, 0);
			label_Name.Margin = new System.Windows.Forms.Padding(0);
			label_Name.MinimumSize = new System.Drawing.Size(117, 21);
			label_Name.Name = "label_Name";
			label_Name.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			label_Name.Size = new System.Drawing.Size(368, 23);
			label_Name.TabIndex = 4;
			label_Name.Text = "Name";
			// 
			// comboBox_Drawer
			// 
			this.comboBox_Category.Dock = System.Windows.Forms.DockStyle.Top;
			this.comboBox_Category.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox_Category.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.comboBox_Category.Location = new System.Drawing.Point(376, 23);
			this.comboBox_Category.Margin = new System.Windows.Forms.Padding(0);
			this.comboBox_Category.MaxLength = 128;
			this.comboBox_Category.Name = "comboBox_Drawer";
			this.comboBox_Category.Size = new System.Drawing.Size(100, 25);
			this.comboBox_Category.TabIndex = 1;
			// 
			// label_Type
			// 
			label_Type.AutoEllipsis = true;
			label_Type.Dock = System.Windows.Forms.DockStyle.Top;
			label_Type.Location = new System.Drawing.Point(376, 0);
			label_Type.Margin = new System.Windows.Forms.Padding(0);
			label_Type.MinimumSize = new System.Drawing.Size(117, 21);
			label_Type.Name = "label_Type";
			label_Type.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			label_Type.Size = new System.Drawing.Size(117, 23);
			label_Type.TabIndex = 6;
			label_Type.Text = "Category";
			// 
			// centerLayout
			// 
			centerLayout.ColumnCount = 1;
			centerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			centerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			centerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			centerLayout.Controls.Add(this.comboBox_Template, 0, 1);
			centerLayout.Controls.Add(label_Template, 0, 0);
			centerLayout.Dock = System.Windows.Forms.DockStyle.Top;
			centerLayout.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
			centerLayout.Location = new System.Drawing.Point(8, 57);
			centerLayout.Margin = new System.Windows.Forms.Padding(0);
			centerLayout.Name = "centerLayout";
			centerLayout.RowCount = 2;
			centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
			centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			centerLayout.Size = new System.Drawing.Size(476, 48);
			centerLayout.TabIndex = 1;
			// 
			// comboBox_Template
			// 
			this.comboBox_Template.Dock = System.Windows.Forms.DockStyle.Top;
			this.comboBox_Template.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox_Template.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.comboBox_Template.Location = new System.Drawing.Point(0, 23);
			this.comboBox_Template.Margin = new System.Windows.Forms.Padding(0);
			this.comboBox_Template.MaximumSize = new System.Drawing.Size(250, 0);
			this.comboBox_Template.MaxLength = 128;
			this.comboBox_Template.Name = "comboBox_Template";
			this.comboBox_Template.Size = new System.Drawing.Size(250, 25);
			this.comboBox_Template.TabIndex = 0;
			// 
			// label_Template
			// 
			label_Template.AutoEllipsis = true;
			label_Template.Dock = System.Windows.Forms.DockStyle.Top;
			label_Template.Location = new System.Drawing.Point(0, 0);
			label_Template.Margin = new System.Windows.Forms.Padding(0);
			label_Template.MinimumSize = new System.Drawing.Size(117, 21);
			label_Template.Name = "label_Template";
			label_Template.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			label_Template.Size = new System.Drawing.Size(476, 21);
			label_Template.TabIndex = 2;
			label_Template.Text = "Template";
			// 
			// checkBoxOpenTextEditor
			// 
			this.checkBoxOpenTextEditor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxOpenTextEditor.Checked = true;
			this.checkBoxOpenTextEditor.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxOpenTextEditor.Location = new System.Drawing.Point(8, 118);
			this.checkBoxOpenTextEditor.Name = "checkBoxOpenTextEditor";
			this.checkBoxOpenTextEditor.Size = new System.Drawing.Size(199, 27);
			this.checkBoxOpenTextEditor.TabIndex = 2;
			this.checkBoxOpenTextEditor.Text = "Launch text editor";
			this.checkBoxOpenTextEditor.UseVisualStyleBackColor = true;
			// 
			// CreateRecipeDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.ClientSize = new System.Drawing.Size(492, 157);
			this.Controls.Add(this.checkBoxOpenTextEditor);
			this.Controls.Add(buttonLayout);
			this.Controls.Add(centerLayout);
			this.Controls.Add(topLayout);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "CreateRecipeDialog";
			this.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Create recipe";
			buttonLayout.ResumeLayout(false);
			topLayout.ResumeLayout(false);
			topLayout.PerformLayout();
			centerLayout.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private TextBoxEx textBox_Name;
		private ComboBoxEx comboBox_Template;
		private System.Windows.Forms.CheckBox checkBoxOpenTextEditor;
		private ComboBoxEx comboBox_Category;
	}
}