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
			System.Windows.Forms.Label labelModel;
			System.Windows.Forms.Label labelTemperature;
			System.Windows.Forms.Label labelSampling;
			System.Windows.Forms.Label labelPenaltyTokens;
			System.Windows.Forms.Label labelRepeatPenalty;
			System.Windows.Forms.Label labelPromptTemplate;
			System.Windows.Forms.Label labelPresets;
			this.btnCancel = new Ginger.ButtonEx();
			this.btnConfirm = new Ginger.ButtonEx();
			this.cbModel = new Ginger.ComboBoxEx();
			this.tableLayout = new System.Windows.Forms.TableLayoutPanel();
			this.labelTopK = new System.Windows.Forms.Label();
			this.labelTopP = new System.Windows.Forms.Label();
			this.labelMinP = new System.Windows.Forms.Label();
			this.panelPresets = new System.Windows.Forms.Panel();
			this.btnNewPreset = new System.Windows.Forms.Button();
			this.btnRemovePreset = new System.Windows.Forms.Button();
			this.cbPresets = new Ginger.ComboBoxEx();
			this.panelTemperature = new System.Windows.Forms.Panel();
			this.trackBar_Temperature = new Ginger.TrackBarEx();
			this.textBox_Temperature = new System.Windows.Forms.TextBox();
			this.panelSampling = new System.Windows.Forms.Panel();
			this.cbSampling = new Ginger.ComboBoxEx();
			this.panelMinP = new System.Windows.Forms.Panel();
			this.trackBar_MinP = new Ginger.TrackBarEx();
			this.textBox_MinP = new System.Windows.Forms.TextBox();
			this.panelTopP = new System.Windows.Forms.Panel();
			this.trackBar_TopP = new Ginger.TrackBarEx();
			this.textBox_TopP = new System.Windows.Forms.TextBox();
			this.panelTopK = new System.Windows.Forms.Panel();
			this.trackBar_TopK = new Ginger.TrackBarEx();
			this.textBox_TopK = new System.Windows.Forms.TextBox();
			this.panelRepeatPenalty = new System.Windows.Forms.Panel();
			this.trackBar_RepeatPenalty = new Ginger.TrackBarEx();
			this.textBox_RepeatPenalty = new System.Windows.Forms.TextBox();
			this.panelRepeatTokens = new System.Windows.Forms.Panel();
			this.trackBar_RepeatTokens = new Ginger.TrackBarEx();
			this.textBox_RepeatTokens = new System.Windows.Forms.TextBox();
			this.panelPromptTemplate = new System.Windows.Forms.Panel();
			this.cbAssociate = new System.Windows.Forms.CheckBox();
			this.cbPromptTemplate = new Ginger.ComboBoxEx();
			this.tableLayout_Buttons = new System.Windows.Forms.TableLayoutPanel();
			this.btnCopy = new Ginger.ButtonEx();
			this.btnPaste = new Ginger.ButtonEx();
			labelModel = new System.Windows.Forms.Label();
			labelTemperature = new System.Windows.Forms.Label();
			labelSampling = new System.Windows.Forms.Label();
			labelPenaltyTokens = new System.Windows.Forms.Label();
			labelRepeatPenalty = new System.Windows.Forms.Label();
			labelPromptTemplate = new System.Windows.Forms.Label();
			labelPresets = new System.Windows.Forms.Label();
			this.tableLayout.SuspendLayout();
			this.panelPresets.SuspendLayout();
			this.panelTemperature.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_Temperature)).BeginInit();
			this.panelSampling.SuspendLayout();
			this.panelMinP.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_MinP)).BeginInit();
			this.panelTopP.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_TopP)).BeginInit();
			this.panelTopK.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_TopK)).BeginInit();
			this.panelRepeatPenalty.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_RepeatPenalty)).BeginInit();
			this.panelRepeatTokens.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_RepeatTokens)).BeginInit();
			this.panelPromptTemplate.SuspendLayout();
			this.tableLayout_Buttons.SuspendLayout();
			this.SuspendLayout();
			// 
			// labelModel
			// 
			labelModel.AutoSize = true;
			labelModel.Dock = System.Windows.Forms.DockStyle.Top;
			labelModel.Location = new System.Drawing.Point(3, 44);
			labelModel.Name = "labelModel";
			labelModel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			labelModel.Size = new System.Drawing.Size(114, 21);
			labelModel.TabIndex = 0;
			labelModel.Text = "Model";
			// 
			// labelTemperature
			// 
			labelTemperature.AutoSize = true;
			labelTemperature.Dock = System.Windows.Forms.DockStyle.Top;
			labelTemperature.Location = new System.Drawing.Point(3, 128);
			labelTemperature.Name = "labelTemperature";
			labelTemperature.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			labelTemperature.Size = new System.Drawing.Size(114, 21);
			labelTemperature.TabIndex = 2;
			labelTemperature.Text = "Temperature";
			// 
			// labelSampling
			// 
			labelSampling.AutoSize = true;
			labelSampling.Dock = System.Windows.Forms.DockStyle.Top;
			labelSampling.Location = new System.Drawing.Point(3, 100);
			labelSampling.Name = "labelSampling";
			labelSampling.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			labelSampling.Size = new System.Drawing.Size(114, 21);
			labelSampling.TabIndex = 3;
			labelSampling.Text = "Sampler";
			// 
			// labelPenaltyTokens
			// 
			labelPenaltyTokens.AutoSize = true;
			labelPenaltyTokens.Dock = System.Windows.Forms.DockStyle.Top;
			labelPenaltyTokens.Location = new System.Drawing.Point(3, 268);
			labelPenaltyTokens.Name = "labelPenaltyTokens";
			labelPenaltyTokens.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			labelPenaltyTokens.Size = new System.Drawing.Size(114, 21);
			labelPenaltyTokens.TabIndex = 7;
			labelPenaltyTokens.Text = "Penalty tokens";
			// 
			// labelRepeatPenalty
			// 
			labelRepeatPenalty.AutoSize = true;
			labelRepeatPenalty.Dock = System.Windows.Forms.DockStyle.Top;
			labelRepeatPenalty.Location = new System.Drawing.Point(3, 240);
			labelRepeatPenalty.Name = "labelRepeatPenalty";
			labelRepeatPenalty.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			labelRepeatPenalty.Size = new System.Drawing.Size(114, 21);
			labelRepeatPenalty.TabIndex = 7;
			labelRepeatPenalty.Text = "Repeat penalty";
			// 
			// labelPromptTemplate
			// 
			labelPromptTemplate.AutoSize = true;
			labelPromptTemplate.Dock = System.Windows.Forms.DockStyle.Top;
			labelPromptTemplate.Location = new System.Drawing.Point(3, 72);
			labelPromptTemplate.Name = "labelPromptTemplate";
			labelPromptTemplate.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			labelPromptTemplate.Size = new System.Drawing.Size(114, 21);
			labelPromptTemplate.TabIndex = 8;
			labelPromptTemplate.Text = "Prompt template";
			// 
			// labelPresets
			// 
			labelPresets.AutoSize = true;
			labelPresets.Location = new System.Drawing.Point(3, 0);
			labelPresets.Name = "labelPresets";
			labelPresets.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			labelPresets.Size = new System.Drawing.Size(50, 21);
			labelPresets.TabIndex = 23;
			labelPresets.Text = "Presets";
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.Location = new System.Drawing.Point(451, 4);
			this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(98, 30);
			this.btnCancel.TabIndex = 2;
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
			this.btnConfirm.TabIndex = 1;
			this.btnConfirm.Text = "Apply";
			this.btnConfirm.UseVisualStyleBackColor = true;
			this.btnConfirm.Click += new System.EventHandler(this.BtnOk_Click);
			// 
			// cbModel
			// 
			this.cbModel.Dock = System.Windows.Forms.DockStyle.Top;
			this.cbModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbModel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.cbModel.FormattingEnabled = true;
			this.cbModel.Location = new System.Drawing.Point(123, 47);
			this.cbModel.Name = "cbModel";
			this.cbModel.Size = new System.Drawing.Size(426, 25);
			this.cbModel.TabIndex = 1;
			// 
			// tableLayout
			// 
			this.tableLayout.ColumnCount = 2;
			this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
			this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayout.Controls.Add(labelPromptTemplate, 0, 2);
			this.tableLayout.Controls.Add(labelPenaltyTokens, 0, 9);
			this.tableLayout.Controls.Add(labelRepeatPenalty, 0, 8);
			this.tableLayout.Controls.Add(this.labelTopK, 0, 7);
			this.tableLayout.Controls.Add(this.labelTopP, 0, 6);
			this.tableLayout.Controls.Add(this.labelMinP, 0, 5);
			this.tableLayout.Controls.Add(labelSampling, 0, 3);
			this.tableLayout.Controls.Add(labelTemperature, 0, 4);
			this.tableLayout.Controls.Add(labelModel, 0, 1);
			this.tableLayout.Controls.Add(labelPresets, 0, 0);
			this.tableLayout.Controls.Add(this.panelPresets, 1, 0);
			this.tableLayout.Controls.Add(this.cbModel, 1, 1);
			this.tableLayout.Controls.Add(this.panelTemperature, 1, 4);
			this.tableLayout.Controls.Add(this.panelSampling, 1, 3);
			this.tableLayout.Controls.Add(this.panelMinP, 1, 5);
			this.tableLayout.Controls.Add(this.panelTopP, 1, 6);
			this.tableLayout.Controls.Add(this.panelTopK, 1, 7);
			this.tableLayout.Controls.Add(this.panelRepeatPenalty, 1, 8);
			this.tableLayout.Controls.Add(this.panelRepeatTokens, 1, 9);
			this.tableLayout.Controls.Add(this.panelPromptTemplate, 1, 2);
			this.tableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayout.Location = new System.Drawing.Point(4, 3);
			this.tableLayout.Name = "tableLayout";
			this.tableLayout.RowCount = 11;
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayout.Size = new System.Drawing.Size(552, 349);
			this.tableLayout.TabIndex = 7;
			// 
			// labelTopK
			// 
			this.labelTopK.AutoSize = true;
			this.labelTopK.Dock = System.Windows.Forms.DockStyle.Top;
			this.labelTopK.Location = new System.Drawing.Point(3, 212);
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
			this.labelTopP.Location = new System.Drawing.Point(3, 184);
			this.labelTopP.Name = "labelTopP";
			this.labelTopP.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			this.labelTopP.Size = new System.Drawing.Size(114, 21);
			this.labelTopP.TabIndex = 5;
			this.labelTopP.Text = "Top-P";
			// 
			// labelMinP
			// 
			this.labelMinP.AutoSize = true;
			this.labelMinP.Location = new System.Drawing.Point(3, 156);
			this.labelMinP.Name = "labelMinP";
			this.labelMinP.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
			this.labelMinP.Size = new System.Drawing.Size(42, 21);
			this.labelMinP.TabIndex = 25;
			this.labelMinP.Text = "Min-P";
			// 
			// panelPresets
			// 
			this.panelPresets.Controls.Add(this.btnNewPreset);
			this.panelPresets.Controls.Add(this.btnRemovePreset);
			this.panelPresets.Controls.Add(this.cbPresets);
			this.panelPresets.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelPresets.Location = new System.Drawing.Point(120, 0);
			this.panelPresets.Margin = new System.Windows.Forms.Padding(0, 0, 0, 15);
			this.panelPresets.Name = "panelPresets";
			this.panelPresets.Padding = new System.Windows.Forms.Padding(3, 3, 0, 0);
			this.panelPresets.Size = new System.Drawing.Size(432, 29);
			this.panelPresets.TabIndex = 24;
			// 
			// btnNewPreset
			// 
			this.btnNewPreset.Dock = System.Windows.Forms.DockStyle.Right;
			this.btnNewPreset.Location = new System.Drawing.Point(282, 3);
			this.btnNewPreset.Name = "btnNewPreset";
			this.btnNewPreset.Size = new System.Drawing.Size(75, 26);
			this.btnNewPreset.TabIndex = 1;
			this.btnNewPreset.Text = "Save";
			this.btnNewPreset.UseVisualStyleBackColor = true;
			// 
			// btnRemovePreset
			// 
			this.btnRemovePreset.Dock = System.Windows.Forms.DockStyle.Right;
			this.btnRemovePreset.Location = new System.Drawing.Point(357, 3);
			this.btnRemovePreset.Name = "btnRemovePreset";
			this.btnRemovePreset.Size = new System.Drawing.Size(75, 26);
			this.btnRemovePreset.TabIndex = 2;
			this.btnRemovePreset.Text = "Delete";
			this.btnRemovePreset.UseVisualStyleBackColor = true;
			// 
			// cbPresets
			// 
			this.cbPresets.Dock = System.Windows.Forms.DockStyle.Left;
			this.cbPresets.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbPresets.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.cbPresets.FormattingEnabled = true;
			this.cbPresets.Items.AddRange(new object[] {
            "(Default)"});
			this.cbPresets.Location = new System.Drawing.Point(3, 3);
			this.cbPresets.Name = "cbPresets";
			this.cbPresets.Size = new System.Drawing.Size(260, 25);
			this.cbPresets.TabIndex = 0;
			// 
			// panelTemperature
			// 
			this.panelTemperature.Controls.Add(this.trackBar_Temperature);
			this.panelTemperature.Controls.Add(this.textBox_Temperature);
			this.panelTemperature.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelTemperature.Location = new System.Drawing.Point(120, 128);
			this.panelTemperature.Margin = new System.Windows.Forms.Padding(0);
			this.panelTemperature.Name = "panelTemperature";
			this.panelTemperature.Padding = new System.Windows.Forms.Padding(3);
			this.panelTemperature.Size = new System.Drawing.Size(432, 28);
			this.panelTemperature.TabIndex = 12;
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
			this.textBox_Temperature.Size = new System.Drawing.Size(100, 25);
			this.textBox_Temperature.TabIndex = 0;
			// 
			// panelSampling
			// 
			this.panelSampling.Controls.Add(this.cbSampling);
			this.panelSampling.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelSampling.Location = new System.Drawing.Point(120, 103);
			this.panelSampling.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
			this.panelSampling.Name = "panelSampling";
			this.panelSampling.Padding = new System.Windows.Forms.Padding(3);
			this.panelSampling.Size = new System.Drawing.Size(432, 25);
			this.panelSampling.TabIndex = 14;
			// 
			// cbSampling
			// 
			this.cbSampling.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbSampling.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.cbSampling.Items.AddRange(new object[] {
            "Min-P",
            "Top-P"});
			this.cbSampling.Location = new System.Drawing.Point(3, 0);
			this.cbSampling.Name = "cbSampling";
			this.cbSampling.Size = new System.Drawing.Size(200, 25);
			this.cbSampling.TabIndex = 0;
			// 
			// panelMinP
			// 
			this.panelMinP.Controls.Add(this.trackBar_MinP);
			this.panelMinP.Controls.Add(this.textBox_MinP);
			this.panelMinP.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelMinP.Location = new System.Drawing.Point(120, 156);
			this.panelMinP.Margin = new System.Windows.Forms.Padding(0);
			this.panelMinP.Name = "panelMinP";
			this.panelMinP.Padding = new System.Windows.Forms.Padding(3);
			this.panelMinP.Size = new System.Drawing.Size(432, 28);
			this.panelMinP.TabIndex = 26;
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
			this.textBox_MinP.Size = new System.Drawing.Size(100, 25);
			this.textBox_MinP.TabIndex = 0;
			// 
			// panelTopP
			// 
			this.panelTopP.Controls.Add(this.trackBar_TopP);
			this.panelTopP.Controls.Add(this.textBox_TopP);
			this.panelTopP.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelTopP.Location = new System.Drawing.Point(120, 184);
			this.panelTopP.Margin = new System.Windows.Forms.Padding(0);
			this.panelTopP.Name = "panelTopP";
			this.panelTopP.Padding = new System.Windows.Forms.Padding(3);
			this.panelTopP.Size = new System.Drawing.Size(432, 28);
			this.panelTopP.TabIndex = 19;
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
			this.textBox_TopP.Size = new System.Drawing.Size(100, 25);
			this.textBox_TopP.TabIndex = 0;
			// 
			// panelTopK
			// 
			this.panelTopK.Controls.Add(this.trackBar_TopK);
			this.panelTopK.Controls.Add(this.textBox_TopK);
			this.panelTopK.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelTopK.Location = new System.Drawing.Point(120, 212);
			this.panelTopK.Margin = new System.Windows.Forms.Padding(0);
			this.panelTopK.Name = "panelTopK";
			this.panelTopK.Padding = new System.Windows.Forms.Padding(3);
			this.panelTopK.Size = new System.Drawing.Size(432, 28);
			this.panelTopK.TabIndex = 20;
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
			this.textBox_TopK.Size = new System.Drawing.Size(100, 25);
			this.textBox_TopK.TabIndex = 0;
			// 
			// panelRepeatPenalty
			// 
			this.panelRepeatPenalty.Controls.Add(this.trackBar_RepeatPenalty);
			this.panelRepeatPenalty.Controls.Add(this.textBox_RepeatPenalty);
			this.panelRepeatPenalty.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelRepeatPenalty.Location = new System.Drawing.Point(120, 240);
			this.panelRepeatPenalty.Margin = new System.Windows.Forms.Padding(0);
			this.panelRepeatPenalty.Name = "panelRepeatPenalty";
			this.panelRepeatPenalty.Padding = new System.Windows.Forms.Padding(3);
			this.panelRepeatPenalty.Size = new System.Drawing.Size(432, 28);
			this.panelRepeatPenalty.TabIndex = 21;
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
			this.textBox_RepeatPenalty.Size = new System.Drawing.Size(100, 25);
			this.textBox_RepeatPenalty.TabIndex = 0;
			// 
			// panelRepeatTokens
			// 
			this.panelRepeatTokens.Controls.Add(this.trackBar_RepeatTokens);
			this.panelRepeatTokens.Controls.Add(this.textBox_RepeatTokens);
			this.panelRepeatTokens.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelRepeatTokens.Location = new System.Drawing.Point(120, 268);
			this.panelRepeatTokens.Margin = new System.Windows.Forms.Padding(0);
			this.panelRepeatTokens.Name = "panelRepeatTokens";
			this.panelRepeatTokens.Padding = new System.Windows.Forms.Padding(3);
			this.panelRepeatTokens.Size = new System.Drawing.Size(432, 28);
			this.panelRepeatTokens.TabIndex = 22;
			// 
			// trackBar_RepeatTokens
			// 
			this.trackBar_RepeatTokens.Dock = System.Windows.Forms.DockStyle.Fill;
			this.trackBar_RepeatTokens.LargeChange = 8;
			this.trackBar_RepeatTokens.Location = new System.Drawing.Point(103, 3);
			this.trackBar_RepeatTokens.Maximum = 512;
			this.trackBar_RepeatTokens.Minimum = 16;
			this.trackBar_RepeatTokens.Name = "trackBar_RepeatTokens";
			this.trackBar_RepeatTokens.Size = new System.Drawing.Size(326, 22);
			this.trackBar_RepeatTokens.TabIndex = 1;
			this.trackBar_RepeatTokens.TickFrequency = 64;
			this.trackBar_RepeatTokens.Value = 256;
			this.trackBar_RepeatTokens.ValueChanged += new System.EventHandler(this.trackBar_RepeatTokens_ValueChanged);
			// 
			// textBox_RepeatTokens
			// 
			this.textBox_RepeatTokens.Dock = System.Windows.Forms.DockStyle.Left;
			this.textBox_RepeatTokens.Location = new System.Drawing.Point(3, 3);
			this.textBox_RepeatTokens.Name = "textBox_RepeatTokens";
			this.textBox_RepeatTokens.Size = new System.Drawing.Size(100, 25);
			this.textBox_RepeatTokens.TabIndex = 0;
			// 
			// panelPromptTemplate
			// 
			this.panelPromptTemplate.Controls.Add(this.cbAssociate);
			this.panelPromptTemplate.Controls.Add(this.cbPromptTemplate);
			this.panelPromptTemplate.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelPromptTemplate.Location = new System.Drawing.Point(120, 72);
			this.panelPromptTemplate.Margin = new System.Windows.Forms.Padding(0);
			this.panelPromptTemplate.Name = "panelPromptTemplate";
			this.panelPromptTemplate.Padding = new System.Windows.Forms.Padding(3);
			this.panelPromptTemplate.Size = new System.Drawing.Size(432, 28);
			this.panelPromptTemplate.TabIndex = 27;
			// 
			// cbAssociate
			// 
			this.cbAssociate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.cbAssociate.AutoSize = true;
			this.cbAssociate.Checked = true;
			this.cbAssociate.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbAssociate.Location = new System.Drawing.Point(279, 3);
			this.cbAssociate.Name = "cbAssociate";
			this.cbAssociate.Size = new System.Drawing.Size(144, 21);
			this.cbAssociate.TabIndex = 10;
			this.cbAssociate.Text = "Make model default";
			this.cbAssociate.UseVisualStyleBackColor = true;
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
			this.cbPromptTemplate.TabIndex = 9;
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
			this.tableLayout_Buttons.Location = new System.Drawing.Point(4, 314);
			this.tableLayout_Buttons.Name = "tableLayout_Buttons";
			this.tableLayout_Buttons.RowCount = 1;
			this.tableLayout_Buttons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayout_Buttons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
			this.tableLayout_Buttons.Size = new System.Drawing.Size(552, 38);
			this.tableLayout_Buttons.TabIndex = 3;
			// 
			// btnCopy
			// 
			this.btnCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCopy.Location = new System.Drawing.Point(3, 4);
			this.btnCopy.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnCopy.Name = "btnCopy";
			this.btnCopy.Size = new System.Drawing.Size(98, 30);
			this.btnCopy.TabIndex = 3;
			this.btnCopy.Text = "Copy";
			this.btnCopy.UseVisualStyleBackColor = true;
			// 
			// btnPaste
			// 
			this.btnPaste.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnPaste.Location = new System.Drawing.Point(107, 4);
			this.btnPaste.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnPaste.Name = "btnPaste";
			this.btnPaste.Size = new System.Drawing.Size(98, 30);
			this.btnPaste.TabIndex = 2;
			this.btnPaste.Text = "Paste";
			this.btnPaste.UseVisualStyleBackColor = true;
			// 
			// EditModelSettingsDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.ClientSize = new System.Drawing.Size(560, 355);
			this.Controls.Add(this.tableLayout_Buttons);
			this.Controls.Add(this.tableLayout);
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
			this.tableLayout.ResumeLayout(false);
			this.tableLayout.PerformLayout();
			this.panelPresets.ResumeLayout(false);
			this.panelTemperature.ResumeLayout(false);
			this.panelTemperature.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_Temperature)).EndInit();
			this.panelSampling.ResumeLayout(false);
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
			this.panelRepeatTokens.ResumeLayout(false);
			this.panelRepeatTokens.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_RepeatTokens)).EndInit();
			this.panelPromptTemplate.ResumeLayout(false);
			this.panelPromptTemplate.PerformLayout();
			this.tableLayout_Buttons.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private ButtonEx btnCancel;
		private ButtonEx btnConfirm;
		private ComboBoxEx cbModel;
		private System.Windows.Forms.TableLayoutPanel tableLayout;
		private ComboBoxEx cbPromptTemplate;
		private System.Windows.Forms.Panel panelTemperature;
		private TrackBarEx trackBar_Temperature;
		private System.Windows.Forms.TextBox textBox_Temperature;
		private System.Windows.Forms.Panel panelSampling;
		private System.Windows.Forms.Panel panelPresets;
		private System.Windows.Forms.Button btnNewPreset;
		private System.Windows.Forms.Button btnRemovePreset;
		private ComboBoxEx cbPresets;
		private System.Windows.Forms.Panel panelTopP;
		private TrackBarEx trackBar_TopP;
		private System.Windows.Forms.TextBox textBox_TopP;
		private System.Windows.Forms.Panel panelTopK;
		private TrackBarEx trackBar_TopK;
		private System.Windows.Forms.TextBox textBox_TopK;
		private System.Windows.Forms.Panel panelRepeatPenalty;
		private TrackBarEx trackBar_RepeatPenalty;
		private System.Windows.Forms.TextBox textBox_RepeatPenalty;
		private System.Windows.Forms.Panel panelRepeatTokens;
		private TrackBarEx trackBar_RepeatTokens;
		private System.Windows.Forms.TextBox textBox_RepeatTokens;
		private ComboBoxEx cbSampling;
		private System.Windows.Forms.Panel panelMinP;
		private TrackBarEx trackBar_MinP;
		private System.Windows.Forms.TextBox textBox_MinP;
		private System.Windows.Forms.Label labelMinP;
		private System.Windows.Forms.Label labelTopK;
		private System.Windows.Forms.Label labelTopP;
		private System.Windows.Forms.TableLayoutPanel tableLayout_Buttons;
		private ButtonEx btnCopy;
		private ButtonEx btnPaste;
		private System.Windows.Forms.Panel panelPromptTemplate;
		private System.Windows.Forms.CheckBox cbAssociate;
	}
}