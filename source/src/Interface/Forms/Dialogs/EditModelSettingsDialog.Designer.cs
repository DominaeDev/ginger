namespace Ginger
{
	partial class EditModelSettingsDialog
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
			System.Windows.Forms.Label labelPresets;
			this.labelModel = new System.Windows.Forms.Label();
			this.labelTemperature = new System.Windows.Forms.Label();
			this.labelSampling = new System.Windows.Forms.Label();
			this.labelPenaltyTokens = new System.Windows.Forms.Label();
			this.labelRepeatPenalty = new System.Windows.Forms.Label();
			this.labelPromptTemplate = new System.Windows.Forms.Label();
			this.btnCancel = new Ginger.ButtonEx();
			this.btnConfirm = new Ginger.ButtonEx();
			this.cbModel = new Ginger.ComboBoxEx();
			this.tableLayout_Settings = new System.Windows.Forms.TableLayoutPanel();
			this.labelTopK = new System.Windows.Forms.Label();
			this.labelTopP = new System.Windows.Forms.Label();
			this.labelMinP = new System.Windows.Forms.Label();
			this.panelModel = new System.Windows.Forms.Panel();
			this.panelPromptTemplate = new System.Windows.Forms.Panel();
			this.cbPromptTemplate = new Ginger.ComboBoxEx();
			this.panelSampling = new System.Windows.Forms.Panel();
			this.cbSampling = new Ginger.ComboBoxEx();
			this.panelTemperature = new System.Windows.Forms.Panel();
			this.trackBar_Temperature = new Ginger.TrackBarEx();
			this.textBox_Temperature = new Ginger.TextBoxEx();
			this.panelMinP = new System.Windows.Forms.Panel();
			this.trackBar_MinP = new Ginger.TrackBarEx();
			this.textBox_MinP = new Ginger.TextBoxEx();
			this.panelTopP = new System.Windows.Forms.Panel();
			this.trackBar_TopP = new Ginger.TrackBarEx();
			this.textBox_TopP = new Ginger.TextBoxEx();
			this.panelTopK = new System.Windows.Forms.Panel();
			this.trackBar_TopK = new Ginger.TrackBarEx();
			this.textBox_TopK = new Ginger.TextBoxEx();
			this.panelRepeatPenalty = new System.Windows.Forms.Panel();
			this.trackBar_RepeatPenalty = new Ginger.TrackBarEx();
			this.textBox_RepeatPenalty = new Ginger.TextBoxEx();
			this.panelPenaltyTokens = new System.Windows.Forms.Panel();
			this.trackBar_PenaltyTokens = new Ginger.TrackBarEx();
			this.textBox_RepeatTokens = new Ginger.TextBoxEx();
			this.panelPresets = new System.Windows.Forms.Panel();
			this.btnNewPreset = new Ginger.ButtonEx();
			this.btnSavePreset = new Ginger.ButtonEx();
			this.btnRemovePreset = new Ginger.ButtonEx();
			this.cbPresets = new Ginger.ComboBoxEx();
			this.tableLayout_Buttons = new System.Windows.Forms.TableLayoutPanel();
			this.btnCopy = new Ginger.ButtonEx();
			this.btnPaste = new Ginger.ButtonEx();
			this.tableLayout_Presets = new System.Windows.Forms.TableLayoutPanel();
			this.separator = new System.Windows.Forms.Panel();
			this.line = new Ginger.HorizontalLine();
			this.panel1 = new System.Windows.Forms.Panel();
			this.horizontalLine1 = new Ginger.HorizontalLine();
			this.cbSavePromptTemplate = new System.Windows.Forms.CheckBox();
			labelPresets = new System.Windows.Forms.Label();
			this.tableLayout_Settings.SuspendLayout();
			this.panelModel.SuspendLayout();
			this.panelPromptTemplate.SuspendLayout();
			this.panelSampling.SuspendLayout();
			this.panelTemperature.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_Temperature)).BeginInit();
			this.panelMinP.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_MinP)).BeginInit();
			this.panelTopP.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_TopP)).BeginInit();
			this.panelTopK.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_TopK)).BeginInit();
			this.panelRepeatPenalty.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_RepeatPenalty)).BeginInit();
			this.panelPenaltyTokens.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_PenaltyTokens)).BeginInit();
			this.panelPresets.SuspendLayout();
			this.tableLayout_Buttons.SuspendLayout();
			this.tableLayout_Presets.SuspendLayout();
			this.separator.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// labelPresets
			// 
			labelPresets.AutoSize = true;
			labelPresets.Location = new System.Drawing.Point(3, 4);
			labelPresets.Name = "labelPresets";
			labelPresets.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			labelPresets.Size = new System.Drawing.Size(50, 21);
			labelPresets.TabIndex = 23;
			labelPresets.Text = "Presets";
			// 
			// labelModel
			// 
			this.labelModel.AutoSize = true;
			this.labelModel.Dock = System.Windows.Forms.DockStyle.Top;
			this.labelModel.Location = new System.Drawing.Point(3, 0);
			this.labelModel.Name = "labelModel";
			this.labelModel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			this.labelModel.Size = new System.Drawing.Size(114, 21);
			this.labelModel.TabIndex = 0;
			this.labelModel.Text = "Model";
			// 
			// labelTemperature
			// 
			this.labelTemperature.AutoSize = true;
			this.labelTemperature.Dock = System.Windows.Forms.DockStyle.Top;
			this.labelTemperature.Location = new System.Drawing.Point(3, 84);
			this.labelTemperature.Name = "labelTemperature";
			this.labelTemperature.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			this.labelTemperature.Size = new System.Drawing.Size(114, 21);
			this.labelTemperature.TabIndex = 2;
			this.labelTemperature.Text = "Temperature";
			// 
			// labelSampling
			// 
			this.labelSampling.AutoSize = true;
			this.labelSampling.Dock = System.Windows.Forms.DockStyle.Top;
			this.labelSampling.Location = new System.Drawing.Point(3, 56);
			this.labelSampling.Name = "labelSampling";
			this.labelSampling.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			this.labelSampling.Size = new System.Drawing.Size(114, 21);
			this.labelSampling.TabIndex = 3;
			this.labelSampling.Text = "Sampler";
			// 
			// labelPenaltyTokens
			// 
			this.labelPenaltyTokens.AutoSize = true;
			this.labelPenaltyTokens.Dock = System.Windows.Forms.DockStyle.Top;
			this.labelPenaltyTokens.Location = new System.Drawing.Point(3, 224);
			this.labelPenaltyTokens.Name = "labelPenaltyTokens";
			this.labelPenaltyTokens.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			this.labelPenaltyTokens.Size = new System.Drawing.Size(114, 21);
			this.labelPenaltyTokens.TabIndex = 7;
			this.labelPenaltyTokens.Text = "Penalty tokens";
			// 
			// labelRepeatPenalty
			// 
			this.labelRepeatPenalty.AutoSize = true;
			this.labelRepeatPenalty.Dock = System.Windows.Forms.DockStyle.Top;
			this.labelRepeatPenalty.Location = new System.Drawing.Point(3, 196);
			this.labelRepeatPenalty.Name = "labelRepeatPenalty";
			this.labelRepeatPenalty.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			this.labelRepeatPenalty.Size = new System.Drawing.Size(114, 21);
			this.labelRepeatPenalty.TabIndex = 7;
			this.labelRepeatPenalty.Text = "Repeat penalty";
			// 
			// labelPromptTemplate
			// 
			this.labelPromptTemplate.AutoSize = true;
			this.labelPromptTemplate.Dock = System.Windows.Forms.DockStyle.Top;
			this.labelPromptTemplate.Location = new System.Drawing.Point(3, 28);
			this.labelPromptTemplate.Name = "labelPromptTemplate";
			this.labelPromptTemplate.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			this.labelPromptTemplate.Size = new System.Drawing.Size(114, 21);
			this.labelPromptTemplate.TabIndex = 8;
			this.labelPromptTemplate.Text = "Prompt template";
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.Location = new System.Drawing.Point(451, 4);
			this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(98, 30);
			this.btnCancel.TabIndex = 3;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
			// 
			// btnConfirm
			// 
			this.btnConfirm.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnConfirm.Location = new System.Drawing.Point(347, 4);
			this.btnConfirm.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnConfirm.Name = "btnConfirm";
			this.btnConfirm.Size = new System.Drawing.Size(98, 30);
			this.btnConfirm.TabIndex = 2;
			this.btnConfirm.Text = "Apply";
			this.btnConfirm.UseVisualStyleBackColor = true;
			this.btnConfirm.Click += new System.EventHandler(this.BtnOk_Click);
			// 
			// cbModel
			// 
			this.cbModel.Dock = System.Windows.Forms.DockStyle.Left;
			this.cbModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbModel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.cbModel.FormattingEnabled = true;
			this.cbModel.Location = new System.Drawing.Point(3, 3);
			this.cbModel.Name = "cbModel";
			this.cbModel.Size = new System.Drawing.Size(260, 25);
			this.cbModel.TabIndex = 0;
			this.cbModel.SelectedIndexChanged += new System.EventHandler(this.cbModel_SelectedIndexChanged);
			// 
			// tableLayout_Settings
			// 
			this.tableLayout_Settings.ColumnCount = 2;
			this.tableLayout_Settings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
			this.tableLayout_Settings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayout_Settings.Controls.Add(this.labelPromptTemplate, 0, 1);
			this.tableLayout_Settings.Controls.Add(this.labelPenaltyTokens, 0, 8);
			this.tableLayout_Settings.Controls.Add(this.labelRepeatPenalty, 0, 7);
			this.tableLayout_Settings.Controls.Add(this.labelTopK, 0, 6);
			this.tableLayout_Settings.Controls.Add(this.labelTopP, 0, 5);
			this.tableLayout_Settings.Controls.Add(this.labelMinP, 0, 4);
			this.tableLayout_Settings.Controls.Add(this.labelSampling, 0, 2);
			this.tableLayout_Settings.Controls.Add(this.labelTemperature, 0, 3);
			this.tableLayout_Settings.Controls.Add(this.labelModel, 0, 0);
			this.tableLayout_Settings.Controls.Add(this.panelModel, 1, 0);
			this.tableLayout_Settings.Controls.Add(this.panelPromptTemplate, 1, 1);
			this.tableLayout_Settings.Controls.Add(this.panelSampling, 1, 2);
			this.tableLayout_Settings.Controls.Add(this.panelTemperature, 1, 3);
			this.tableLayout_Settings.Controls.Add(this.panelMinP, 1, 4);
			this.tableLayout_Settings.Controls.Add(this.panelTopP, 1, 5);
			this.tableLayout_Settings.Controls.Add(this.panelTopK, 1, 6);
			this.tableLayout_Settings.Controls.Add(this.panelRepeatPenalty, 1, 7);
			this.tableLayout_Settings.Controls.Add(this.panelPenaltyTokens, 1, 8);
			this.tableLayout_Settings.Dock = System.Windows.Forms.DockStyle.Top;
			this.tableLayout_Settings.Location = new System.Drawing.Point(4, 53);
			this.tableLayout_Settings.Name = "tableLayout_Settings";
			this.tableLayout_Settings.RowCount = 9;
			this.tableLayout_Settings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout_Settings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout_Settings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout_Settings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout_Settings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout_Settings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout_Settings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout_Settings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout_Settings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout_Settings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayout_Settings.Size = new System.Drawing.Size(552, 255);
			this.tableLayout_Settings.TabIndex = 0;
			// 
			// labelTopK
			// 
			this.labelTopK.AutoSize = true;
			this.labelTopK.Dock = System.Windows.Forms.DockStyle.Top;
			this.labelTopK.Location = new System.Drawing.Point(3, 168);
			this.labelTopK.Name = "labelTopK";
			this.labelTopK.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			this.labelTopK.Size = new System.Drawing.Size(114, 21);
			this.labelTopK.TabIndex = 4;
			this.labelTopK.Text = "Top-K";
			// 
			// labelTopP
			// 
			this.labelTopP.AutoSize = true;
			this.labelTopP.Dock = System.Windows.Forms.DockStyle.Top;
			this.labelTopP.Location = new System.Drawing.Point(3, 140);
			this.labelTopP.Name = "labelTopP";
			this.labelTopP.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			this.labelTopP.Size = new System.Drawing.Size(114, 21);
			this.labelTopP.TabIndex = 5;
			this.labelTopP.Text = "Top-P";
			// 
			// labelMinP
			// 
			this.labelMinP.AutoSize = true;
			this.labelMinP.Location = new System.Drawing.Point(3, 112);
			this.labelMinP.Name = "labelMinP";
			this.labelMinP.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			this.labelMinP.Size = new System.Drawing.Size(42, 21);
			this.labelMinP.TabIndex = 25;
			this.labelMinP.Text = "Min-P";
			// 
			// panelModel
			// 
			this.panelModel.Controls.Add(this.cbModel);
			this.panelModel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelModel.Location = new System.Drawing.Point(120, 0);
			this.panelModel.Margin = new System.Windows.Forms.Padding(0);
			this.panelModel.Name = "panelModel";
			this.panelModel.Padding = new System.Windows.Forms.Padding(3);
			this.panelModel.Size = new System.Drawing.Size(432, 28);
			this.panelModel.TabIndex = 1;
			// 
			// panelPromptTemplate
			// 
			this.panelPromptTemplate.Controls.Add(this.cbSavePromptTemplate);
			this.panelPromptTemplate.Controls.Add(this.cbPromptTemplate);
			this.panelPromptTemplate.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelPromptTemplate.Location = new System.Drawing.Point(120, 28);
			this.panelPromptTemplate.Margin = new System.Windows.Forms.Padding(0);
			this.panelPromptTemplate.Name = "panelPromptTemplate";
			this.panelPromptTemplate.Padding = new System.Windows.Forms.Padding(3);
			this.panelPromptTemplate.Size = new System.Drawing.Size(432, 28);
			this.panelPromptTemplate.TabIndex = 2;
			// 
			// cbPromptTemplate
			// 
			this.cbPromptTemplate.Dock = System.Windows.Forms.DockStyle.Top;
			this.cbPromptTemplate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbPromptTemplate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.cbPromptTemplate.FormattingEnabled = true;
			this.cbPromptTemplate.Location = new System.Drawing.Point(3, 3);
			this.cbPromptTemplate.MaximumSize = new System.Drawing.Size(200, 0);
			this.cbPromptTemplate.Name = "cbPromptTemplate";
			this.cbPromptTemplate.Size = new System.Drawing.Size(200, 25);
			this.cbPromptTemplate.TabIndex = 0;
			this.cbPromptTemplate.SelectedIndexChanged += new System.EventHandler(this.cbPromptTemplate_SelectedIndexChanged);
			// 
			// panelSampling
			// 
			this.panelSampling.Controls.Add(this.cbSampling);
			this.panelSampling.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelSampling.Location = new System.Drawing.Point(120, 59);
			this.panelSampling.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
			this.panelSampling.Name = "panelSampling";
			this.panelSampling.Padding = new System.Windows.Forms.Padding(3);
			this.panelSampling.Size = new System.Drawing.Size(432, 25);
			this.panelSampling.TabIndex = 3;
			// 
			// cbSampling
			// 
			this.cbSampling.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbSampling.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.cbSampling.Location = new System.Drawing.Point(3, 0);
			this.cbSampling.Name = "cbSampling";
			this.cbSampling.Size = new System.Drawing.Size(100, 25);
			this.cbSampling.TabIndex = 0;
			this.cbSampling.SelectedIndexChanged += new System.EventHandler(this.CbSampling_SelectedIndexChanged);
			// 
			// panelTemperature
			// 
			this.panelTemperature.Controls.Add(this.trackBar_Temperature);
			this.panelTemperature.Controls.Add(this.textBox_Temperature);
			this.panelTemperature.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelTemperature.Location = new System.Drawing.Point(120, 84);
			this.panelTemperature.Margin = new System.Windows.Forms.Padding(0);
			this.panelTemperature.Name = "panelTemperature";
			this.panelTemperature.Padding = new System.Windows.Forms.Padding(3);
			this.panelTemperature.Size = new System.Drawing.Size(432, 28);
			this.panelTemperature.TabIndex = 4;
			// 
			// trackBar_Temperature
			// 
			this.trackBar_Temperature.Dock = System.Windows.Forms.DockStyle.Fill;
			this.trackBar_Temperature.Location = new System.Drawing.Point(103, 3);
			this.trackBar_Temperature.Maximum = 50;
			this.trackBar_Temperature.Name = "trackBar_Temperature";
			this.trackBar_Temperature.Size = new System.Drawing.Size(326, 22);
			this.trackBar_Temperature.TabIndex = 1;
			this.trackBar_Temperature.TickFrequency = 10;
			this.trackBar_Temperature.Value = 12;
			this.trackBar_Temperature.ValueChanged += new System.EventHandler(this.trackBar_Temperature_ValueChanged);
			// 
			// textBox_Temperature
			// 
			this.textBox_Temperature.Dock = System.Windows.Forms.DockStyle.Left;
			this.textBox_Temperature.Location = new System.Drawing.Point(3, 3);
			this.textBox_Temperature.Name = "textBox_Temperature";
			this.textBox_Temperature.Placeholder = null;
			this.textBox_Temperature.Size = new System.Drawing.Size(100, 25);
			this.textBox_Temperature.TabIndex = 0;
			this.textBox_Temperature.TextChanged += new System.EventHandler(this.textBox_Temperature_TextChanged);
			// 
			// panelMinP
			// 
			this.panelMinP.Controls.Add(this.trackBar_MinP);
			this.panelMinP.Controls.Add(this.textBox_MinP);
			this.panelMinP.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelMinP.Location = new System.Drawing.Point(120, 112);
			this.panelMinP.Margin = new System.Windows.Forms.Padding(0);
			this.panelMinP.Name = "panelMinP";
			this.panelMinP.Padding = new System.Windows.Forms.Padding(3);
			this.panelMinP.Size = new System.Drawing.Size(432, 28);
			this.panelMinP.TabIndex = 5;
			// 
			// trackBar_MinP
			// 
			this.trackBar_MinP.Dock = System.Windows.Forms.DockStyle.Fill;
			this.trackBar_MinP.Location = new System.Drawing.Point(103, 3);
			this.trackBar_MinP.Maximum = 100;
			this.trackBar_MinP.Name = "trackBar_MinP";
			this.trackBar_MinP.Size = new System.Drawing.Size(326, 22);
			this.trackBar_MinP.SmallChange = 5;
			this.trackBar_MinP.TabIndex = 1;
			this.trackBar_MinP.TickFrequency = 10;
			this.trackBar_MinP.Value = 10;
			this.trackBar_MinP.ValueChanged += new System.EventHandler(this.trackBar_MinP_ValueChanged);
			// 
			// textBox_MinP
			// 
			this.textBox_MinP.Dock = System.Windows.Forms.DockStyle.Left;
			this.textBox_MinP.Location = new System.Drawing.Point(3, 3);
			this.textBox_MinP.Name = "textBox_MinP";
			this.textBox_MinP.Placeholder = null;
			this.textBox_MinP.Size = new System.Drawing.Size(100, 25);
			this.textBox_MinP.TabIndex = 0;
			this.textBox_MinP.TextChanged += new System.EventHandler(this.textBox_MinP_TextChanged);
			// 
			// panelTopP
			// 
			this.panelTopP.Controls.Add(this.trackBar_TopP);
			this.panelTopP.Controls.Add(this.textBox_TopP);
			this.panelTopP.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelTopP.Location = new System.Drawing.Point(120, 140);
			this.panelTopP.Margin = new System.Windows.Forms.Padding(0);
			this.panelTopP.Name = "panelTopP";
			this.panelTopP.Padding = new System.Windows.Forms.Padding(3);
			this.panelTopP.Size = new System.Drawing.Size(432, 28);
			this.panelTopP.TabIndex = 6;
			// 
			// trackBar_TopP
			// 
			this.trackBar_TopP.Dock = System.Windows.Forms.DockStyle.Fill;
			this.trackBar_TopP.Location = new System.Drawing.Point(103, 3);
			this.trackBar_TopP.Maximum = 100;
			this.trackBar_TopP.Name = "trackBar_TopP";
			this.trackBar_TopP.Size = new System.Drawing.Size(326, 22);
			this.trackBar_TopP.TabIndex = 1;
			this.trackBar_TopP.TickFrequency = 10;
			this.trackBar_TopP.Value = 90;
			this.trackBar_TopP.ValueChanged += new System.EventHandler(this.trackBar_TopP_ValueChanged);
			// 
			// textBox_TopP
			// 
			this.textBox_TopP.Dock = System.Windows.Forms.DockStyle.Left;
			this.textBox_TopP.Location = new System.Drawing.Point(3, 3);
			this.textBox_TopP.Name = "textBox_TopP";
			this.textBox_TopP.Placeholder = null;
			this.textBox_TopP.Size = new System.Drawing.Size(100, 25);
			this.textBox_TopP.TabIndex = 0;
			this.textBox_TopP.TextChanged += new System.EventHandler(this.textBox_TopP_TextChanged);
			// 
			// panelTopK
			// 
			this.panelTopK.Controls.Add(this.trackBar_TopK);
			this.panelTopK.Controls.Add(this.textBox_TopK);
			this.panelTopK.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelTopK.Location = new System.Drawing.Point(120, 168);
			this.panelTopK.Margin = new System.Windows.Forms.Padding(0);
			this.panelTopK.Name = "panelTopK";
			this.panelTopK.Padding = new System.Windows.Forms.Padding(3);
			this.panelTopK.Size = new System.Drawing.Size(432, 28);
			this.panelTopK.TabIndex = 7;
			// 
			// trackBar_TopK
			// 
			this.trackBar_TopK.Dock = System.Windows.Forms.DockStyle.Fill;
			this.trackBar_TopK.Location = new System.Drawing.Point(103, 3);
			this.trackBar_TopK.Maximum = 100;
			this.trackBar_TopK.Name = "trackBar_TopK";
			this.trackBar_TopK.Size = new System.Drawing.Size(326, 22);
			this.trackBar_TopK.SmallChange = 5;
			this.trackBar_TopK.TabIndex = 1;
			this.trackBar_TopK.TickFrequency = 10;
			this.trackBar_TopK.Value = 30;
			this.trackBar_TopK.ValueChanged += new System.EventHandler(this.trackBar_TopK_ValueChanged);
			// 
			// textBox_TopK
			// 
			this.textBox_TopK.Dock = System.Windows.Forms.DockStyle.Left;
			this.textBox_TopK.Location = new System.Drawing.Point(3, 3);
			this.textBox_TopK.Name = "textBox_TopK";
			this.textBox_TopK.Placeholder = null;
			this.textBox_TopK.Size = new System.Drawing.Size(100, 25);
			this.textBox_TopK.TabIndex = 0;
			this.textBox_TopK.TextChanged += new System.EventHandler(this.textBox_TopK_TextChanged);
			// 
			// panelRepeatPenalty
			// 
			this.panelRepeatPenalty.Controls.Add(this.trackBar_RepeatPenalty);
			this.panelRepeatPenalty.Controls.Add(this.textBox_RepeatPenalty);
			this.panelRepeatPenalty.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelRepeatPenalty.Location = new System.Drawing.Point(120, 196);
			this.panelRepeatPenalty.Margin = new System.Windows.Forms.Padding(0);
			this.panelRepeatPenalty.Name = "panelRepeatPenalty";
			this.panelRepeatPenalty.Padding = new System.Windows.Forms.Padding(3);
			this.panelRepeatPenalty.Size = new System.Drawing.Size(432, 28);
			this.panelRepeatPenalty.TabIndex = 8;
			// 
			// trackBar_RepeatPenalty
			// 
			this.trackBar_RepeatPenalty.Dock = System.Windows.Forms.DockStyle.Fill;
			this.trackBar_RepeatPenalty.Location = new System.Drawing.Point(103, 3);
			this.trackBar_RepeatPenalty.Maximum = 200;
			this.trackBar_RepeatPenalty.Minimum = 50;
			this.trackBar_RepeatPenalty.Name = "trackBar_RepeatPenalty";
			this.trackBar_RepeatPenalty.Size = new System.Drawing.Size(326, 22);
			this.trackBar_RepeatPenalty.SmallChange = 5;
			this.trackBar_RepeatPenalty.TabIndex = 1;
			this.trackBar_RepeatPenalty.TickFrequency = 10;
			this.trackBar_RepeatPenalty.Value = 105;
			this.trackBar_RepeatPenalty.ValueChanged += new System.EventHandler(this.trackBar_RepeatPenalty_ValueChanged);
			// 
			// textBox_RepeatPenalty
			// 
			this.textBox_RepeatPenalty.Dock = System.Windows.Forms.DockStyle.Left;
			this.textBox_RepeatPenalty.Location = new System.Drawing.Point(3, 3);
			this.textBox_RepeatPenalty.Name = "textBox_RepeatPenalty";
			this.textBox_RepeatPenalty.Placeholder = null;
			this.textBox_RepeatPenalty.Size = new System.Drawing.Size(100, 25);
			this.textBox_RepeatPenalty.TabIndex = 0;
			this.textBox_RepeatPenalty.TextChanged += new System.EventHandler(this.textBox_RepeatPenalty_TextChanged);
			// 
			// panelPenaltyTokens
			// 
			this.panelPenaltyTokens.Controls.Add(this.trackBar_PenaltyTokens);
			this.panelPenaltyTokens.Controls.Add(this.textBox_RepeatTokens);
			this.panelPenaltyTokens.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelPenaltyTokens.Location = new System.Drawing.Point(120, 224);
			this.panelPenaltyTokens.Margin = new System.Windows.Forms.Padding(0);
			this.panelPenaltyTokens.Name = "panelPenaltyTokens";
			this.panelPenaltyTokens.Padding = new System.Windows.Forms.Padding(3);
			this.panelPenaltyTokens.Size = new System.Drawing.Size(432, 31);
			this.panelPenaltyTokens.TabIndex = 9;
			// 
			// trackBar_PenaltyTokens
			// 
			this.trackBar_PenaltyTokens.Dock = System.Windows.Forms.DockStyle.Fill;
			this.trackBar_PenaltyTokens.LargeChange = 8;
			this.trackBar_PenaltyTokens.Location = new System.Drawing.Point(103, 3);
			this.trackBar_PenaltyTokens.Maximum = 512;
			this.trackBar_PenaltyTokens.Minimum = 16;
			this.trackBar_PenaltyTokens.Name = "trackBar_PenaltyTokens";
			this.trackBar_PenaltyTokens.Size = new System.Drawing.Size(326, 25);
			this.trackBar_PenaltyTokens.TabIndex = 1;
			this.trackBar_PenaltyTokens.TickFrequency = 64;
			this.trackBar_PenaltyTokens.Value = 256;
			this.trackBar_PenaltyTokens.ValueChanged += new System.EventHandler(this.trackBar_RepeatTokens_ValueChanged);
			// 
			// textBox_RepeatTokens
			// 
			this.textBox_RepeatTokens.Dock = System.Windows.Forms.DockStyle.Left;
			this.textBox_RepeatTokens.Location = new System.Drawing.Point(3, 3);
			this.textBox_RepeatTokens.Name = "textBox_RepeatTokens";
			this.textBox_RepeatTokens.Placeholder = null;
			this.textBox_RepeatTokens.Size = new System.Drawing.Size(100, 25);
			this.textBox_RepeatTokens.TabIndex = 0;
			this.textBox_RepeatTokens.TextChanged += new System.EventHandler(this.textBox_RepeatTokens_TextChanged);
			// 
			// panelPresets
			// 
			this.panelPresets.Controls.Add(this.btnNewPreset);
			this.panelPresets.Controls.Add(this.btnSavePreset);
			this.panelPresets.Controls.Add(this.btnRemovePreset);
			this.panelPresets.Controls.Add(this.cbPresets);
			this.panelPresets.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelPresets.Location = new System.Drawing.Point(120, 4);
			this.panelPresets.Margin = new System.Windows.Forms.Padding(0);
			this.panelPresets.Name = "panelPresets";
			this.panelPresets.Padding = new System.Windows.Forms.Padding(3);
			this.panelPresets.Size = new System.Drawing.Size(432, 34);
			this.panelPresets.TabIndex = 0;
			// 
			// btnNewPreset
			// 
			this.btnNewPreset.Dock = System.Windows.Forms.DockStyle.Right;
			this.btnNewPreset.Location = new System.Drawing.Point(279, 3);
			this.btnNewPreset.Name = "btnNewPreset";
			this.btnNewPreset.Size = new System.Drawing.Size(50, 28);
			this.btnNewPreset.TabIndex = 1;
			this.btnNewPreset.Text = "New";
			this.btnNewPreset.UseVisualStyleBackColor = true;
			this.btnNewPreset.Click += new System.EventHandler(this.btnNewPreset_Click);
			// 
			// btnSavePreset
			// 
			this.btnSavePreset.Dock = System.Windows.Forms.DockStyle.Right;
			this.btnSavePreset.Location = new System.Drawing.Point(329, 3);
			this.btnSavePreset.Name = "btnSavePreset";
			this.btnSavePreset.Size = new System.Drawing.Size(50, 28);
			this.btnSavePreset.TabIndex = 2;
			this.btnSavePreset.Text = "Save";
			this.btnSavePreset.UseVisualStyleBackColor = true;
			this.btnSavePreset.Click += new System.EventHandler(this.btnSavePreset_Click);
			// 
			// btnRemovePreset
			// 
			this.btnRemovePreset.Dock = System.Windows.Forms.DockStyle.Right;
			this.btnRemovePreset.Location = new System.Drawing.Point(379, 3);
			this.btnRemovePreset.Name = "btnRemovePreset";
			this.btnRemovePreset.Size = new System.Drawing.Size(50, 28);
			this.btnRemovePreset.TabIndex = 3;
			this.btnRemovePreset.Text = "Del";
			this.btnRemovePreset.UseVisualStyleBackColor = true;
			this.btnRemovePreset.Click += new System.EventHandler(this.btnRemovePreset_Click);
			// 
			// cbPresets
			// 
			this.cbPresets.Dock = System.Windows.Forms.DockStyle.Left;
			this.cbPresets.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbPresets.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.cbPresets.FormattingEnabled = true;
			this.cbPresets.Location = new System.Drawing.Point(3, 3);
			this.cbPresets.Name = "cbPresets";
			this.cbPresets.Size = new System.Drawing.Size(260, 25);
			this.cbPresets.TabIndex = 0;
			this.cbPresets.SelectedIndexChanged += new System.EventHandler(this.cbPresets_SelectedIndexChanged);
			// 
			// tableLayout_Buttons
			// 
			this.tableLayout_Buttons.ColumnCount = 5;
			this.tableLayout_Buttons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
			this.tableLayout_Buttons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
			this.tableLayout_Buttons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayout_Buttons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
			this.tableLayout_Buttons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
			this.tableLayout_Buttons.Controls.Add(this.btnCopy, 0, 0);
			this.tableLayout_Buttons.Controls.Add(this.btnPaste, 1, 0);
			this.tableLayout_Buttons.Controls.Add(this.btnConfirm, 3, 0);
			this.tableLayout_Buttons.Controls.Add(this.btnCancel, 4, 0);
			this.tableLayout_Buttons.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.tableLayout_Buttons.Location = new System.Drawing.Point(4, 319);
			this.tableLayout_Buttons.Name = "tableLayout_Buttons";
			this.tableLayout_Buttons.RowCount = 1;
			this.tableLayout_Buttons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayout_Buttons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
			this.tableLayout_Buttons.Size = new System.Drawing.Size(552, 38);
			this.tableLayout_Buttons.TabIndex = 1;
			// 
			// btnCopy
			// 
			this.btnCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCopy.Location = new System.Drawing.Point(3, 4);
			this.btnCopy.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnCopy.Name = "btnCopy";
			this.btnCopy.Size = new System.Drawing.Size(98, 30);
			this.btnCopy.TabIndex = 0;
			this.btnCopy.Text = "Copy";
			this.btnCopy.UseVisualStyleBackColor = true;
			this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
			// 
			// btnPaste
			// 
			this.btnPaste.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnPaste.Location = new System.Drawing.Point(107, 4);
			this.btnPaste.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnPaste.Name = "btnPaste";
			this.btnPaste.Size = new System.Drawing.Size(98, 30);
			this.btnPaste.TabIndex = 1;
			this.btnPaste.Text = "Paste";
			this.btnPaste.UseVisualStyleBackColor = true;
			this.btnPaste.Click += new System.EventHandler(this.btnPaste_Click);
			// 
			// tableLayout_Presets
			// 
			this.tableLayout_Presets.ColumnCount = 2;
			this.tableLayout_Presets.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
			this.tableLayout_Presets.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayout_Presets.Controls.Add(labelPresets, 0, 0);
			this.tableLayout_Presets.Controls.Add(this.panelPresets, 1, 0);
			this.tableLayout_Presets.Dock = System.Windows.Forms.DockStyle.Top;
			this.tableLayout_Presets.Location = new System.Drawing.Point(4, 3);
			this.tableLayout_Presets.Name = "tableLayout_Presets";
			this.tableLayout_Presets.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			this.tableLayout_Presets.RowCount = 1;
			this.tableLayout_Presets.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
			this.tableLayout_Presets.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
			this.tableLayout_Presets.Size = new System.Drawing.Size(552, 38);
			this.tableLayout_Presets.TabIndex = 4;
			// 
			// separator
			// 
			this.separator.Controls.Add(this.line);
			this.separator.Dock = System.Windows.Forms.DockStyle.Top;
			this.separator.Location = new System.Drawing.Point(4, 41);
			this.separator.Name = "separator";
			this.separator.Padding = new System.Windows.Forms.Padding(4);
			this.separator.Size = new System.Drawing.Size(552, 12);
			this.separator.TabIndex = 4;
			// 
			// line
			// 
			this.line.Dock = System.Windows.Forms.DockStyle.Top;
			this.line.Location = new System.Drawing.Point(4, 4);
			this.line.Name = "line";
			this.line.Size = new System.Drawing.Size(544, 2);
			this.line.TabIndex = 0;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.horizontalLine1);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel1.Location = new System.Drawing.Point(4, 307);
			this.panel1.Name = "panel1";
			this.panel1.Padding = new System.Windows.Forms.Padding(4);
			this.panel1.Size = new System.Drawing.Size(552, 12);
			this.panel1.TabIndex = 5;
			// 
			// horizontalLine1
			// 
			this.horizontalLine1.Dock = System.Windows.Forms.DockStyle.Top;
			this.horizontalLine1.Location = new System.Drawing.Point(4, 4);
			this.horizontalLine1.Name = "horizontalLine1";
			this.horizontalLine1.Size = new System.Drawing.Size(544, 2);
			this.horizontalLine1.TabIndex = 0;
			// 
			// cbSavePromptTemplate
			// 
			this.cbSavePromptTemplate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.cbSavePromptTemplate.AutoSize = true;
			this.cbSavePromptTemplate.Checked = true;
			this.cbSavePromptTemplate.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbSavePromptTemplate.Location = new System.Drawing.Point(274, 5);
			this.cbSavePromptTemplate.Name = "cbSavePromptTemplate";
			this.cbSavePromptTemplate.Size = new System.Drawing.Size(158, 21);
			this.cbSavePromptTemplate.TabIndex = 1;
			this.cbSavePromptTemplate.Text = "Associated with model";
			this.cbSavePromptTemplate.UseVisualStyleBackColor = true;
			this.cbSavePromptTemplate.CheckedChanged += new System.EventHandler(this.cbSavePromptTemplate_CheckedChanged);
			// 
			// EditModelSettingsDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.ClientSize = new System.Drawing.Size(560, 360);
			this.Controls.Add(this.tableLayout_Settings);
			this.Controls.Add(this.separator);
			this.Controls.Add(this.tableLayout_Presets);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.tableLayout_Buttons);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "EditModelSettingsDialog";
			this.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Edit model settings";
			this.tableLayout_Settings.ResumeLayout(false);
			this.tableLayout_Settings.PerformLayout();
			this.panelModel.ResumeLayout(false);
			this.panelPromptTemplate.ResumeLayout(false);
			this.panelPromptTemplate.PerformLayout();
			this.panelSampling.ResumeLayout(false);
			this.panelTemperature.ResumeLayout(false);
			this.panelTemperature.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_Temperature)).EndInit();
			this.panelMinP.ResumeLayout(false);
			this.panelMinP.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_MinP)).EndInit();
			this.panelTopP.ResumeLayout(false);
			this.panelTopP.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_TopP)).EndInit();
			this.panelTopK.ResumeLayout(false);
			this.panelTopK.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_TopK)).EndInit();
			this.panelRepeatPenalty.ResumeLayout(false);
			this.panelRepeatPenalty.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_RepeatPenalty)).EndInit();
			this.panelPenaltyTokens.ResumeLayout(false);
			this.panelPenaltyTokens.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_PenaltyTokens)).EndInit();
			this.panelPresets.ResumeLayout(false);
			this.tableLayout_Buttons.ResumeLayout(false);
			this.tableLayout_Presets.ResumeLayout(false);
			this.tableLayout_Presets.PerformLayout();
			this.separator.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private ButtonEx btnCancel;
		private ButtonEx btnConfirm;
		private ComboBoxEx cbModel;
		private System.Windows.Forms.TableLayoutPanel tableLayout_Settings;
		private ComboBoxEx cbPromptTemplate;
		private System.Windows.Forms.Panel panelTemperature;
		private TrackBarEx trackBar_Temperature;
		private TextBoxEx textBox_Temperature;
		private System.Windows.Forms.Panel panelSampling;
		private System.Windows.Forms.Panel panelPresets;
		private ButtonEx btnSavePreset;
		private ButtonEx btnRemovePreset;
		private ComboBoxEx cbPresets;
		private System.Windows.Forms.Panel panelTopP;
		private TrackBarEx trackBar_TopP;
		private TextBoxEx textBox_TopP;
		private System.Windows.Forms.Panel panelTopK;
		private TrackBarEx trackBar_TopK;
		private TextBoxEx textBox_TopK;
		private System.Windows.Forms.Panel panelRepeatPenalty;
		private TrackBarEx trackBar_RepeatPenalty;
		private TextBoxEx textBox_RepeatPenalty;
		private System.Windows.Forms.Panel panelPenaltyTokens;
		private TrackBarEx trackBar_PenaltyTokens;
		private TextBoxEx textBox_RepeatTokens;
		private ComboBoxEx cbSampling;
		private System.Windows.Forms.Panel panelMinP;
		private TrackBarEx trackBar_MinP;
		private TextBoxEx textBox_MinP;
		private System.Windows.Forms.Label labelMinP;
		private System.Windows.Forms.Label labelTopK;
		private System.Windows.Forms.Label labelTopP;
		private System.Windows.Forms.TableLayoutPanel tableLayout_Buttons;
		private ButtonEx btnCopy;
		private ButtonEx btnPaste;
		private System.Windows.Forms.Panel panelPromptTemplate;
		private System.Windows.Forms.Label labelModel;
		private System.Windows.Forms.Label labelTemperature;
		private System.Windows.Forms.Label labelSampling;
		private System.Windows.Forms.Label labelPenaltyTokens;
		private System.Windows.Forms.Label labelRepeatPenalty;
		private System.Windows.Forms.Label labelPromptTemplate;
		private System.Windows.Forms.Panel panelModel;
		private ButtonEx btnNewPreset;
		private System.Windows.Forms.TableLayoutPanel tableLayout_Presets;
		private System.Windows.Forms.Panel separator;
		private HorizontalLine line;
		private System.Windows.Forms.Panel panel1;
		private HorizontalLine horizontalLine1;
		private System.Windows.Forms.CheckBox cbSavePromptTemplate;
	}
}