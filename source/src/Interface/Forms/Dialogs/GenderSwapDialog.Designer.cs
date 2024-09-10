namespace Ginger
{
	partial class GenderSwapDialog
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
			System.Windows.Forms.TableLayoutPanel characterLayout;
			System.Windows.Forms.Label label_Folder;
			System.Windows.Forms.Label label_Name;
			System.Windows.Forms.TableLayoutPanel userLayout;
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label4;
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.cbCharacter = new System.Windows.Forms.CheckBox();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.cbUser = new System.Windows.Forms.CheckBox();
			this.pictureBox2 = new System.Windows.Forms.PictureBox();
			this.comboBox_UserTarget = new Ginger.ComboBoxEx();
			this.comboBox_UserGender = new Ginger.ComboBoxEx();
			this.comboBox_CharacterTarget = new Ginger.ComboBoxEx();
			this.comboBox_CharacterGender = new Ginger.ComboBoxEx();
			buttonLayout = new System.Windows.Forms.FlowLayoutPanel();
			characterLayout = new System.Windows.Forms.TableLayoutPanel();
			label_Folder = new System.Windows.Forms.Label();
			label_Name = new System.Windows.Forms.Label();
			userLayout = new System.Windows.Forms.TableLayoutPanel();
			label3 = new System.Windows.Forms.Label();
			label4 = new System.Windows.Forms.Label();
			buttonLayout.SuspendLayout();
			characterLayout.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			userLayout.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
			this.SuspendLayout();
			// 
			// buttonLayout
			// 
			buttonLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			buttonLayout.Controls.Add(this.btnCancel);
			buttonLayout.Controls.Add(this.btnOk);
			buttonLayout.Dock = System.Windows.Forms.DockStyle.Bottom;
			buttonLayout.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			buttonLayout.Location = new System.Drawing.Point(8, 117);
			buttonLayout.Margin = new System.Windows.Forms.Padding(0);
			buttonLayout.Name = "buttonLayout";
			buttonLayout.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
			buttonLayout.Size = new System.Drawing.Size(496, 41);
			buttonLayout.TabIndex = 2;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.Location = new System.Drawing.Point(362, 7);
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
			this.btnOk.Location = new System.Drawing.Point(225, 7);
			this.btnOk.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(131, 30);
			this.btnOk.TabIndex = 1;
			this.btnOk.Text = "Replace";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
			// 
			// characterLayout
			// 
			characterLayout.ColumnCount = 4;
			characterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
			characterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			characterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
			characterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			characterLayout.Controls.Add(this.comboBox_CharacterTarget, 3, 1);
			characterLayout.Controls.Add(label_Folder, 3, 0);
			characterLayout.Controls.Add(this.comboBox_CharacterGender, 1, 1);
			characterLayout.Controls.Add(label_Name, 1, 0);
			characterLayout.Controls.Add(this.cbCharacter, 0, 1);
			characterLayout.Controls.Add(this.pictureBox1, 2, 1);
			characterLayout.Dock = System.Windows.Forms.DockStyle.Top;
			characterLayout.Location = new System.Drawing.Point(8, 3);
			characterLayout.Margin = new System.Windows.Forms.Padding(0);
			characterLayout.Name = "characterLayout";
			characterLayout.RowCount = 2;
			characterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
			characterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			characterLayout.Size = new System.Drawing.Size(496, 54);
			characterLayout.TabIndex = 0;
			// 
			// label_Folder
			// 
			label_Folder.AutoEllipsis = true;
			label_Folder.Dock = System.Windows.Forms.DockStyle.Top;
			label_Folder.Location = new System.Drawing.Point(281, 0);
			label_Folder.Margin = new System.Windows.Forms.Padding(0);
			label_Folder.MinimumSize = new System.Drawing.Size(117, 21);
			label_Folder.Name = "label_Folder";
			label_Folder.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			label_Folder.Size = new System.Drawing.Size(215, 23);
			label_Folder.TabIndex = 5;
			label_Folder.Text = "Replace with";
			// 
			// label_Name
			// 
			label_Name.AutoEllipsis = true;
			label_Name.Dock = System.Windows.Forms.DockStyle.Top;
			label_Name.Location = new System.Drawing.Point(26, 0);
			label_Name.Margin = new System.Windows.Forms.Padding(0);
			label_Name.MinimumSize = new System.Drawing.Size(117, 21);
			label_Name.Name = "label_Name";
			label_Name.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			label_Name.Size = new System.Drawing.Size(215, 23);
			label_Name.TabIndex = 4;
			label_Name.Text = "Pronouns";
			// 
			// cbCharacter
			// 
			this.cbCharacter.Checked = true;
			this.cbCharacter.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbCharacter.Dock = System.Windows.Forms.DockStyle.Top;
			this.cbCharacter.Location = new System.Drawing.Point(3, 26);
			this.cbCharacter.Name = "cbCharacter";
			this.cbCharacter.Size = new System.Drawing.Size(20, 21);
			this.cbCharacter.TabIndex = 0;
			this.cbCharacter.UseVisualStyleBackColor = true;
			this.cbCharacter.CheckedChanged += new System.EventHandler(this.cbCharacter_CheckedChanged);
			// 
			// pictureBox1
			// 
			this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBox1.Image = global::Ginger.Properties.Resources.arrow_right;
			this.pictureBox1.Location = new System.Drawing.Point(247, 23);
			this.pictureBox1.Margin = new System.Windows.Forms.Padding(0);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(34, 31);
			this.pictureBox1.TabIndex = 6;
			this.pictureBox1.TabStop = false;
			// 
			// userLayout
			// 
			userLayout.ColumnCount = 4;
			userLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
			userLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			userLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
			userLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			userLayout.Controls.Add(this.comboBox_UserTarget, 3, 1);
			userLayout.Controls.Add(label3, 3, 0);
			userLayout.Controls.Add(this.comboBox_UserGender, 1, 1);
			userLayout.Controls.Add(label4, 1, 0);
			userLayout.Controls.Add(this.cbUser, 0, 1);
			userLayout.Controls.Add(this.pictureBox2, 2, 1);
			userLayout.Dock = System.Windows.Forms.DockStyle.Top;
			userLayout.Location = new System.Drawing.Point(8, 57);
			userLayout.Margin = new System.Windows.Forms.Padding(0);
			userLayout.Name = "userLayout";
			userLayout.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			userLayout.RowCount = 2;
			userLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
			userLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			userLayout.Size = new System.Drawing.Size(496, 54);
			userLayout.TabIndex = 1;
			// 
			// label3
			// 
			label3.AutoEllipsis = true;
			label3.Dock = System.Windows.Forms.DockStyle.Top;
			label3.Location = new System.Drawing.Point(281, 4);
			label3.Margin = new System.Windows.Forms.Padding(0);
			label3.MinimumSize = new System.Drawing.Size(117, 21);
			label3.Name = "label3";
			label3.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			label3.Size = new System.Drawing.Size(215, 23);
			label3.TabIndex = 5;
			label3.Text = "Replace with";
			// 
			// label4
			// 
			label4.AutoEllipsis = true;
			label4.Dock = System.Windows.Forms.DockStyle.Top;
			label4.Location = new System.Drawing.Point(26, 4);
			label4.Margin = new System.Windows.Forms.Padding(0);
			label4.MinimumSize = new System.Drawing.Size(117, 21);
			label4.Name = "label4";
			label4.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
			label4.Size = new System.Drawing.Size(215, 23);
			label4.TabIndex = 4;
			label4.Text = "Pronouns";
			// 
			// cbUser
			// 
			this.cbUser.Dock = System.Windows.Forms.DockStyle.Top;
			this.cbUser.Location = new System.Drawing.Point(3, 30);
			this.cbUser.Name = "cbUser";
			this.cbUser.Size = new System.Drawing.Size(20, 21);
			this.cbUser.TabIndex = 0;
			this.cbUser.UseVisualStyleBackColor = true;
			this.cbUser.CheckedChanged += new System.EventHandler(this.cbUser_CheckedChanged);
			// 
			// pictureBox2
			// 
			this.pictureBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBox2.Image = global::Ginger.Properties.Resources.arrow_right;
			this.pictureBox2.Location = new System.Drawing.Point(247, 27);
			this.pictureBox2.Margin = new System.Windows.Forms.Padding(0);
			this.pictureBox2.Name = "pictureBox2";
			this.pictureBox2.Size = new System.Drawing.Size(34, 27);
			this.pictureBox2.TabIndex = 6;
			this.pictureBox2.TabStop = false;
			// 
			// comboBox_UserTarget
			// 
			this.comboBox_UserTarget.Dock = System.Windows.Forms.DockStyle.Top;
			this.comboBox_UserTarget.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox_UserTarget.Enabled = false;
			this.comboBox_UserTarget.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.comboBox_UserTarget.FormattingEnabled = true;
			this.comboBox_UserTarget.Items.AddRange(new object[] {
			"Masculine",
			"Feminine",
			"Neutral (They/Them)",
			"Mixed (He/She)",
			"Variables",
			"Variables (User)",
            "Objective (It/It)",
			"Neopronouns (Shi/Hir)",
			"Neopronouns (Ey/Em)",
			"Neopronouns (Ze/Zir)",
			"Neopronouns (Xe/Xem)",
			"Neopronouns (Fae/Faer)",
			});
			this.comboBox_UserTarget.Location = new System.Drawing.Point(281, 27);
			this.comboBox_UserTarget.Margin = new System.Windows.Forms.Padding(0);
			this.comboBox_UserTarget.MaxLength = 128;
			this.comboBox_UserTarget.Name = "comboBox_UserTarget";
			this.comboBox_UserTarget.Size = new System.Drawing.Size(215, 25);
			this.comboBox_UserTarget.TabIndex = 2;
			this.comboBox_UserTarget.SelectedIndexChanged += new System.EventHandler(this.comboBox_UserTarget_SelectedIndexChanged);
			// 
			// comboBox_UserGender
			// 
			this.comboBox_UserGender.Dock = System.Windows.Forms.DockStyle.Top;
			this.comboBox_UserGender.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox_UserGender.Enabled = false;
			this.comboBox_UserGender.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.comboBox_UserGender.FormattingEnabled = true;
			this.comboBox_UserGender.Items.AddRange(new object[] {
            "Masculine",
            "Feminine",
            "Neutral (They/Them)",
            "Mixed (He/She)",
            "Variables",
			"Variables (User)",
            "Objective (It/It)",
			"Neopronouns (Shi/Hir)",
			"Neopronouns (Ey/Em)",
			"Neopronouns (Ze/Zir)",
			"Neopronouns (Xe/Xem)",
			"Neopronouns (Fae/Faer)",
			});
			this.comboBox_UserGender.Location = new System.Drawing.Point(26, 27);
			this.comboBox_UserGender.Margin = new System.Windows.Forms.Padding(0);
			this.comboBox_UserGender.MaxLength = 128;
			this.comboBox_UserGender.Name = "comboBox_UserGender";
			this.comboBox_UserGender.Size = new System.Drawing.Size(215, 25);
			this.comboBox_UserGender.TabIndex = 1;
			this.comboBox_UserGender.SelectedIndexChanged += new System.EventHandler(this.comboBox_UserGender_SelectedIndexChanged);
			// 
			// comboBox_CharacterTarget
			// 
			this.comboBox_CharacterTarget.Dock = System.Windows.Forms.DockStyle.Top;
			this.comboBox_CharacterTarget.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox_CharacterTarget.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.comboBox_CharacterTarget.FormattingEnabled = true;
			this.comboBox_CharacterTarget.Items.AddRange(new object[] {
			"Masculine",
			"Feminine",
			"Neutral (They/Them)",
			"Mixed (He/She)",
			"Variables",
			"Variables (User)",
            "Objective (It/It)",
			"Neopronouns (Shi/Hir)",
			"Neopronouns (Ey/Em)",
			"Neopronouns (Ze/Zir)",
			"Neopronouns (Xe/Xem)",
			"Neopronouns (Fae/Faer)",
			});
			this.comboBox_CharacterTarget.Location = new System.Drawing.Point(281, 23);
			this.comboBox_CharacterTarget.Margin = new System.Windows.Forms.Padding(0);
			this.comboBox_CharacterTarget.MaxLength = 128;
			this.comboBox_CharacterTarget.Name = "comboBox_CharacterTarget";
			this.comboBox_CharacterTarget.Size = new System.Drawing.Size(215, 25);
			this.comboBox_CharacterTarget.TabIndex = 2;
			this.comboBox_CharacterTarget.SelectedIndexChanged += new System.EventHandler(this.comboBox_CharacterTarget_SelectedIndexChanged);
			// 
			// comboBox_CharacterGender
			// 
			this.comboBox_CharacterGender.Dock = System.Windows.Forms.DockStyle.Top;
			this.comboBox_CharacterGender.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox_CharacterGender.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.comboBox_CharacterGender.FormattingEnabled = true;
			this.comboBox_CharacterGender.Items.AddRange(new object[] {
            "Masculine",
            "Feminine",
            "Neutral (They/Them)",
            "Mixed (He/She)",
            "Variables",
			"Variables (User)",
            "Objective (It/It)",
			"Neopronouns (Shi/Hir)",
			"Neopronouns (Ey/Em)",
			"Neopronouns (Ze/Zir)",
			"Neopronouns (Xe/Xem)",
			"Neopronouns (Fae/Faer)",
			});
			this.comboBox_CharacterGender.Location = new System.Drawing.Point(26, 23);
			this.comboBox_CharacterGender.Margin = new System.Windows.Forms.Padding(0);
			this.comboBox_CharacterGender.MaxLength = 128;
			this.comboBox_CharacterGender.Name = "comboBox_CharacterGender";
			this.comboBox_CharacterGender.Size = new System.Drawing.Size(215, 25);
			this.comboBox_CharacterGender.TabIndex = 1;
			this.comboBox_CharacterGender.SelectedIndexChanged += new System.EventHandler(this.comboBox_CharacterGender_SelectedIndexChanged);
			// 
			// GenderSwapDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.ClientSize = new System.Drawing.Size(512, 161);
			this.Controls.Add(buttonLayout);
			this.Controls.Add(userLayout);
			this.Controls.Add(characterLayout);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "GenderSwapDialog";
			this.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Replace pronouns";
			buttonLayout.ResumeLayout(false);
			characterLayout.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			userLayout.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private ComboBoxEx comboBox_CharacterTarget;
		private ComboBoxEx comboBox_CharacterGender;
		private ComboBoxEx comboBox_UserTarget;
		private ComboBoxEx comboBox_UserGender;
		private System.Windows.Forms.CheckBox cbUser;
		private System.Windows.Forms.CheckBox cbCharacter;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.PictureBox pictureBox2;
	}
}