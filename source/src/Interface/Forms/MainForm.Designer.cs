using Ginger.Properties;

namespace Ginger {
	partial class MainForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
			System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
			System.Windows.Forms.ToolStripSeparator toolStripMenuItem8;
			System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
			System.Windows.Forms.ToolStripSeparator toolStripMenuItem7;
			System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
			System.Windows.Forms.ToolStripSeparator toolStripMenuItem6;
			System.Windows.Forms.ToolStripMenuItem linkMenuItem;
			System.Windows.Forms.ToolStripSeparator toolStripMenuItem10;
			System.Windows.Forms.ToolStripMenuItem writeChatSettingsMenuItem;
			System.Windows.Forms.ToolStripMenuItem themeMenuItem;
			this.enableLinkMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.importLinkedMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveLinkedMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveNewLinkedMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.revertLinkedMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.reestablishLinkMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.breakLinkMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.bulkSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.bulkOperationsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.bulkEditModelSettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem12 = new System.Windows.Forms.ToolStripSeparator();
			this.bulkImportMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.bulkExportMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.applyToLastChatMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.applyToFirstChatMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.applyToAllChatsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.lightThemeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.darkThemeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.splitContainer = new System.Windows.Forms.SplitContainer();
			this.sidePanel = new Ginger.SidePanel();
			this.tabControl = new System.Windows.Forms.CustomTabControl();
			this.tabRecipe = new System.Windows.Forms.TabPage();
			this.panelRecipe = new System.Windows.Forms.Panel();
			this.recipeList = new Ginger.RecipeList();
			this.buttonRow = new System.Windows.Forms.TableLayoutPanel();
			this.btnAdd_Lore = new System.Windows.Forms.Button();
			this.btnAdd_Snippets = new System.Windows.Forms.Button();
			this.btnAdd_Other = new System.Windows.Forms.Button();
			this.btnAdd_World = new System.Windows.Forms.Button();
			this.btnAdd_Mind = new System.Windows.Forms.Button();
			this.btnAdd_Traits = new System.Windows.Forms.Button();
			this.btnAdd_Character = new System.Windows.Forms.Button();
			this.btnAdd_Model = new System.Windows.Forms.Button();
			this.tabOutput = new System.Windows.Forms.TabPage();
			this.panelOutput = new System.Windows.Forms.Panel();
			this.outputBox = new Ginger.OutputPreview();
			this.group_Debug = new System.Windows.Forms.GroupBox();
			this.outputBox_Raw2 = new Ginger.TextBoxEx();
			this.outputBox_Raw = new Ginger.TextBoxEx();
			this.tabNotes = new System.Windows.Forms.TabPage();
			this.userNotes = new Ginger.Interface.Controls.UserNotes();
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newFromTemplateMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openRecentMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveasToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveIncrementalMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.importToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.importCharacterMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.importLorebookMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exportCharacterMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exportLorebookMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.changeLanguageMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.changeLanguageSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.undoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.redoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.copyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.findMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.findNextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.findPreviousMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.findAndReplaceMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.swapGenderMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewRecipeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewOutputMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewNotesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.embeddedAssetsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.customVariablesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.chatHistoryMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.collapseAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.expandAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showRecipeCategoryMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sortRecipesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.additionalCharactersMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.tokenBudgetMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.tokenBudgetNone = new System.Windows.Forms.ToolStripMenuItem();
			this.tokenBudget1K = new System.Windows.Forms.ToolStripMenuItem();
			this.tokenBudget2K = new System.Windows.Forms.ToolStripMenuItem();
			this.tokenBudget3K = new System.Windows.Forms.ToolStripMenuItem();
			this.tokenBudget4K = new System.Windows.Forms.ToolStripMenuItem();
			this.tokenBudget5K = new System.Windows.Forms.ToolStripMenuItem();
			this.tokenBudget6K = new System.Windows.Forms.ToolStripMenuItem();
			this.tokenBudget8K = new System.Windows.Forms.ToolStripMenuItem();
			this.tokenBudget10K = new System.Windows.Forms.ToolStripMenuItem();
			this.tokenBudget12K = new System.Windows.Forms.ToolStripMenuItem();
			this.tokenBudget16K = new System.Windows.Forms.ToolStripMenuItem();
			this.tokenBudget24K = new System.Windows.Forms.ToolStripMenuItem();
			this.tokenBudget32K = new System.Windows.Forms.ToolStripMenuItem();
			this.outputPreviewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.outputPreviewDefaultMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.outputPreviewSillyTavernMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.outputPreviewFaradayMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.outputPreviewPlainTextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.checkSpellingMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.enableSpellCheckingMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
			this.linkOptionsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.enableAutosaveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.alwaysLinkMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.usePortraitAsBackgroundMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editExportModelSettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem11 = new System.Windows.Forms.ToolStripSeparator();
			this.autoConvertNameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoBreakMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.rearrangeLoreMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showNSFWRecipesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.createRecipeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.createSnippetMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.reloadRecipesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.bakeAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.bakeActorMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mergeLoreMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewHelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.visitGitHubPageMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.aboutGingerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.exportFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.importFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.statusBar = new System.Windows.Forms.StatusStrip();
			this.statusBarMessage = new System.Windows.Forms.ToolStripStatusLabel();
			this.statusBarActor = new System.Windows.Forms.ToolStripStatusLabel();
			this.statusConnectionIcon = new System.Windows.Forms.ToolStripStatusLabel();
			toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			toolStripMenuItem8 = new System.Windows.Forms.ToolStripSeparator();
			toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
			toolStripMenuItem7 = new System.Windows.Forms.ToolStripSeparator();
			toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			toolStripMenuItem6 = new System.Windows.Forms.ToolStripSeparator();
			linkMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			toolStripMenuItem10 = new System.Windows.Forms.ToolStripSeparator();
			writeChatSettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			themeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
			this.splitContainer.Panel1.SuspendLayout();
			this.splitContainer.Panel2.SuspendLayout();
			this.splitContainer.SuspendLayout();
			this.tabControl.SuspendLayout();
			this.tabRecipe.SuspendLayout();
			this.panelRecipe.SuspendLayout();
			this.buttonRow.SuspendLayout();
			this.tabOutput.SuspendLayout();
			this.panelOutput.SuspendLayout();
			this.group_Debug.SuspendLayout();
			this.tabNotes.SuspendLayout();
			this.menuStrip.SuspendLayout();
			this.statusBar.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStripMenuItem3
			// 
			toolStripMenuItem3.Name = "toolStripMenuItem3";
			toolStripMenuItem3.Size = new System.Drawing.Size(256, 6);
			// 
			// toolStripMenuItem1
			// 
			toolStripMenuItem1.Name = "toolStripMenuItem1";
			toolStripMenuItem1.Size = new System.Drawing.Size(256, 6);
			// 
			// toolStripMenuItem8
			// 
			toolStripMenuItem8.Name = "toolStripMenuItem8";
			toolStripMenuItem8.Size = new System.Drawing.Size(239, 6);
			// 
			// toolStripMenuItem5
			// 
			toolStripMenuItem5.Name = "toolStripMenuItem5";
			toolStripMenuItem5.Size = new System.Drawing.Size(216, 6);
			// 
			// toolStripMenuItem7
			// 
			toolStripMenuItem7.Name = "toolStripMenuItem7";
			toolStripMenuItem7.Size = new System.Drawing.Size(216, 6);
			// 
			// toolStripSeparator1
			// 
			toolStripSeparator1.Name = "toolStripSeparator1";
			toolStripSeparator1.Size = new System.Drawing.Size(184, 6);
			// 
			// toolStripMenuItem6
			// 
			toolStripMenuItem6.Name = "toolStripMenuItem6";
			toolStripMenuItem6.Size = new System.Drawing.Size(163, 6);
			// 
			// linkMenuItem
			// 
			linkMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.enableLinkMenuItem,
            this.reestablishLinkMenuItem,
            this.breakLinkMenuItem,
            toolStripMenuItem10,
            this.importLinkedMenuItem,
            this.saveLinkedMenuItem,
            this.saveNewLinkedMenuItem,
            this.revertLinkedMenuItem,
            this.bulkSeparator,
            this.bulkOperationsMenuItem});
			linkMenuItem.Name = "linkMenuItem";
			linkMenuItem.Size = new System.Drawing.Size(259, 22);
			linkMenuItem.Text = "&Link";
			// 
			// enableLinkMenuItem
			// 
			this.enableLinkMenuItem.Name = "enableLinkMenuItem";
			this.enableLinkMenuItem.Size = new System.Drawing.Size(239, 22);
			this.enableLinkMenuItem.Text = "Connect to Backyard AI";
			this.enableLinkMenuItem.Click += new System.EventHandler(this.enableLinkMenuItem_Click);
			// 
			// toolStripMenuItem10
			// 
			toolStripMenuItem10.Name = "toolStripMenuItem10";
			toolStripMenuItem10.Size = new System.Drawing.Size(236, 6);
			// 
			// importLinkedMenuItem
			// 
			this.importLinkedMenuItem.Name = "importLinkedMenuItem";
			this.importLinkedMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+O";
			this.importLinkedMenuItem.Size = new System.Drawing.Size(239, 22);
			this.importLinkedMenuItem.Text = "Open character...";
			this.importLinkedMenuItem.Click += new System.EventHandler(this.importLinkedMenuItem_Click);
			// 
			// saveLinkedMenuItem
			// 
			this.saveLinkedMenuItem.Name = "saveLinkedMenuItem";
			this.saveLinkedMenuItem.ShortcutKeyDisplayString = "Ctrl+U";
			this.saveLinkedMenuItem.Size = new System.Drawing.Size(239, 22);
			this.saveLinkedMenuItem.Text = "Save changes";
			this.saveLinkedMenuItem.Click += new System.EventHandler(this.saveLinkedMenuItem_Click);
			// 
			// saveNewLinkedMenuItem
			// 
			this.saveNewLinkedMenuItem.Name = "saveNewLinkedMenuItem";
			this.saveNewLinkedMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+U";
			this.saveNewLinkedMenuItem.Size = new System.Drawing.Size(239, 22);
			this.saveNewLinkedMenuItem.Text = "Save as new";
			this.saveNewLinkedMenuItem.Click += new System.EventHandler(this.saveNewLinkedMenuItem_Click);
			// 
			// revertLinkedMenuItem
			// 
			this.revertLinkedMenuItem.Name = "revertLinkedMenuItem";
			this.revertLinkedMenuItem.Size = new System.Drawing.Size(239, 22);
			this.revertLinkedMenuItem.Text = "Revert...";
			this.revertLinkedMenuItem.Click += new System.EventHandler(this.revertLinkedMenuItem_Click);
			// 
			// reestablishLinkMenuItem
			// 
			this.reestablishLinkMenuItem.Name = "reestablishLinkMenuItem";
			this.reestablishLinkMenuItem.Size = new System.Drawing.Size(239, 22);
			this.reestablishLinkMenuItem.Text = "Restore character link";
			this.reestablishLinkMenuItem.Click += new System.EventHandler(this.reestablishLinkMenuItem_Click);
			// 
			// breakLinkMenuItem
			// 
			this.breakLinkMenuItem.Name = "breakLinkMenuItem";
			this.breakLinkMenuItem.Size = new System.Drawing.Size(239, 22);
			this.breakLinkMenuItem.Text = "Break character link";
			this.breakLinkMenuItem.Click += new System.EventHandler(this.breakLinkMenuItem_Click);
			// 
			// bulkSeparator
			// 
			this.bulkSeparator.Name = "bulkSeparator";
			this.bulkSeparator.Size = new System.Drawing.Size(236, 6);
			// 
			// bulkOperationsMenuItem
			// 
			this.bulkOperationsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bulkEditModelSettingsMenuItem,
            this.toolStripMenuItem12,
            this.bulkImportMenuItem,
            this.bulkExportMenuItem});
			this.bulkOperationsMenuItem.Name = "bulkOperationsMenuItem";
			this.bulkOperationsMenuItem.Size = new System.Drawing.Size(239, 22);
			this.bulkOperationsMenuItem.Text = "Bulk operations";
			// 
			// bulkEditModelSettingsMenuItem
			// 
			this.bulkEditModelSettingsMenuItem.Name = "bulkEditModelSettingsMenuItem";
			this.bulkEditModelSettingsMenuItem.Size = new System.Drawing.Size(209, 22);
			this.bulkEditModelSettingsMenuItem.Text = "Change model settings...";
			this.bulkEditModelSettingsMenuItem.Click += new System.EventHandler(this.bulkChangeModelSettingsMenuItem_Click);
			// 
			// toolStripMenuItem12
			// 
			this.toolStripMenuItem12.Name = "toolStripMenuItem12";
			this.toolStripMenuItem12.Size = new System.Drawing.Size(206, 6);
			// 
			// bulkImportMenuItem
			// 
			this.bulkImportMenuItem.Name = "bulkImportMenuItem";
			this.bulkImportMenuItem.Size = new System.Drawing.Size(209, 22);
			this.bulkImportMenuItem.Text = "Import many characters...";
			this.bulkImportMenuItem.Click += new System.EventHandler(this.bulkImportMenuItem_Click);
			// 
			// bulkExportMenuItem
			// 
			this.bulkExportMenuItem.Name = "bulkExportMenuItem";
			this.bulkExportMenuItem.Size = new System.Drawing.Size(209, 22);
			this.bulkExportMenuItem.Text = "Export many characters...";
			this.bulkExportMenuItem.Click += new System.EventHandler(this.bulkExportMenuItem_Click);
			// 
			// writeChatSettingsMenuItem
			// 
			writeChatSettingsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.applyToLastChatMenuItem,
            this.applyToFirstChatMenuItem,
            this.applyToAllChatsMenuItem});
			writeChatSettingsMenuItem.Name = "writeChatSettingsMenuItem";
			writeChatSettingsMenuItem.Size = new System.Drawing.Size(213, 22);
			writeChatSettingsMenuItem.Text = "Apply chat settings to";
			// 
			// applyToLastChatMenuItem
			// 
			this.applyToLastChatMenuItem.Name = "applyToLastChatMenuItem";
			this.applyToLastChatMenuItem.Size = new System.Drawing.Size(163, 22);
			this.applyToLastChatMenuItem.Text = "Most recent chat";
			this.applyToLastChatMenuItem.Click += new System.EventHandler(this.applyToLastChatMenuItem_Click);
			// 
			// applyToFirstChatMenuItem
			// 
			this.applyToFirstChatMenuItem.Name = "applyToFirstChatMenuItem";
			this.applyToFirstChatMenuItem.Size = new System.Drawing.Size(163, 22);
			this.applyToFirstChatMenuItem.Text = "Oldest chat";
			this.applyToFirstChatMenuItem.Click += new System.EventHandler(this.applyToFirstChatMenuItem_Click);
			// 
			// applyToAllChatsMenuItem
			// 
			this.applyToAllChatsMenuItem.Name = "applyToAllChatsMenuItem";
			this.applyToAllChatsMenuItem.Size = new System.Drawing.Size(163, 22);
			this.applyToAllChatsMenuItem.Text = "All chats";
			this.applyToAllChatsMenuItem.Click += new System.EventHandler(this.applyToAllChatsMenuItem_Click);
			// 
			// themeMenuItem
			// 
			themeMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lightThemeMenuItem,
            this.darkThemeMenuItem});
			themeMenuItem.Name = "themeMenuItem";
			themeMenuItem.Size = new System.Drawing.Size(183, 22);
			themeMenuItem.Text = "Theme";
			// 
			// lightThemeMenuItem
			// 
			this.lightThemeMenuItem.Name = "lightThemeMenuItem";
			this.lightThemeMenuItem.Size = new System.Drawing.Size(101, 22);
			this.lightThemeMenuItem.Text = "Light";
			this.lightThemeMenuItem.Click += new System.EventHandler(this.lightThemeMenuItem_Click);
			// 
			// darkThemeMenuItem
			// 
			this.darkThemeMenuItem.Name = "darkThemeMenuItem";
			this.darkThemeMenuItem.Size = new System.Drawing.Size(101, 22);
			this.darkThemeMenuItem.Text = "Dark";
			this.darkThemeMenuItem.Click += new System.EventHandler(this.darkThemeMenuItem_Click);
			// 
			// splitContainer
			// 
			this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer.Location = new System.Drawing.Point(0, 24);
			this.splitContainer.Margin = new System.Windows.Forms.Padding(0);
			this.splitContainer.Name = "splitContainer";
			// 
			// splitContainer.Panel1
			// 
			this.splitContainer.Panel1.Controls.Add(this.sidePanel);
			this.splitContainer.Panel1MinSize = 360;
			// 
			// splitContainer.Panel2
			// 
			this.splitContainer.Panel2.BackColor = System.Drawing.SystemColors.Control;
			this.splitContainer.Panel2.Controls.Add(this.tabControl);
			this.splitContainer.Panel2.Padding = new System.Windows.Forms.Padding(0, 0, 4, 0);
			this.splitContainer.Panel2MinSize = 500;
			this.splitContainer.Size = new System.Drawing.Size(1264, 775);
			this.splitContainer.SplitterDistance = 450;
			this.splitContainer.TabIndex = 0;
			this.splitContainer.TabStop = false;
			// 
			// sidePanel
			// 
			this.sidePanel.AutoSize = true;
			this.sidePanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.sidePanel.Location = new System.Drawing.Point(0, 0);
			this.sidePanel.Margin = new System.Windows.Forms.Padding(0);
			this.sidePanel.Name = "sidePanel";
			this.sidePanel.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.sidePanel.Size = new System.Drawing.Size(450, 772);
			this.sidePanel.TabIndex = 0;
			// 
			// tabControl
			// 
			this.tabControl.Alignment = System.Windows.Forms.TabAlignment.Right;
			this.tabControl.Controls.Add(this.tabRecipe);
			this.tabControl.Controls.Add(this.tabOutput);
			this.tabControl.Controls.Add(this.tabNotes);
			this.tabControl.DisplayStyleProvider.CloserColor = System.Drawing.Color.DarkGray;
			this.tabControl.DisplayStyleProvider.FocusTrack = false;
			this.tabControl.DisplayStyleProvider.HotTrack = true;
			this.tabControl.DisplayStyleProvider.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.tabControl.DisplayStyleProvider.Opacity = 1F;
			this.tabControl.DisplayStyleProvider.Overlap = 0;
			this.tabControl.DisplayStyleProvider.Padding = new System.Drawing.Point(6, 3);
			this.tabControl.DisplayStyleProvider.Radius = 10;
			this.tabControl.DisplayStyleProvider.ShowTabCloser = false;
			this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.tabControl.HotTrack = true;
			this.tabControl.Location = new System.Drawing.Point(0, 0);
			this.tabControl.Multiline = true;
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(806, 775);
			this.tabControl.TabIndex = 6;
			this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
			// 
			// tabRecipe
			// 
			this.tabRecipe.BackColor = System.Drawing.SystemColors.Control;
			this.tabRecipe.Controls.Add(this.panelRecipe);
			this.tabRecipe.Location = new System.Drawing.Point(4, 4);
			this.tabRecipe.Name = "tabRecipe";
			this.tabRecipe.Size = new System.Drawing.Size(773, 767);
			this.tabRecipe.TabIndex = 0;
			this.tabRecipe.Text = "Recipe";
			// 
			// panelRecipe
			// 
			this.panelRecipe.Controls.Add(this.recipeList);
			this.panelRecipe.Controls.Add(this.buttonRow);
			this.panelRecipe.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelRecipe.Location = new System.Drawing.Point(0, 0);
			this.panelRecipe.Name = "panelRecipe";
			this.panelRecipe.Padding = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.panelRecipe.Size = new System.Drawing.Size(773, 767);
			this.panelRecipe.TabIndex = 6;
			// 
			// recipeList
			// 
			this.recipeList.AutoScroll = true;
			this.recipeList.AutoScrollMargin = new System.Drawing.Size(0, 22);
			this.recipeList.BackColor = System.Drawing.Color.Gray;
			this.recipeList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.recipeList.Dock = System.Windows.Forms.DockStyle.Fill;
			this.recipeList.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.recipeList.GradientColor = System.Drawing.Color.DarkGray;
			this.recipeList.Location = new System.Drawing.Point(0, 68);
			this.recipeList.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.recipeList.Name = "recipeList";
			this.recipeList.Size = new System.Drawing.Size(770, 699);
			this.recipeList.TabIndex = 0;
			// 
			// buttonRow
			// 
			this.buttonRow.ColumnCount = 8;
			this.buttonRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
			this.buttonRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
			this.buttonRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
			this.buttonRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
			this.buttonRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
			this.buttonRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
			this.buttonRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
			this.buttonRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
			this.buttonRow.Controls.Add(this.btnAdd_Lore, 7, 0);
			this.buttonRow.Controls.Add(this.btnAdd_Snippets, 6, 0);
			this.buttonRow.Controls.Add(this.btnAdd_Other, 5, 0);
			this.buttonRow.Controls.Add(this.btnAdd_World, 4, 0);
			this.buttonRow.Controls.Add(this.btnAdd_Mind, 3, 0);
			this.buttonRow.Controls.Add(this.btnAdd_Traits, 2, 0);
			this.buttonRow.Controls.Add(this.btnAdd_Character, 1, 0);
			this.buttonRow.Controls.Add(this.btnAdd_Model, 0, 0);
			this.buttonRow.Dock = System.Windows.Forms.DockStyle.Top;
			this.buttonRow.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
			this.buttonRow.Location = new System.Drawing.Point(0, 0);
			this.buttonRow.Name = "buttonRow";
			this.buttonRow.Padding = new System.Windows.Forms.Padding(0, 0, 0, 4);
			this.buttonRow.RowCount = 1;
			this.buttonRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.buttonRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
			this.buttonRow.Size = new System.Drawing.Size(770, 68);
			this.buttonRow.TabIndex = 5;
			// 
			// btnAdd_Lore
			// 
			this.btnAdd_Lore.BackColor = System.Drawing.Color.WhiteSmoke;
			this.btnAdd_Lore.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnAdd_Lore.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
			this.btnAdd_Lore.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
			this.btnAdd_Lore.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
			this.btnAdd_Lore.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnAdd_Lore.Image = global::Ginger.Properties.Resources.lore;
			this.btnAdd_Lore.Location = new System.Drawing.Point(673, 0);
			this.btnAdd_Lore.Margin = new System.Windows.Forms.Padding(1, 0, 0, 0);
			this.btnAdd_Lore.Name = "btnAdd_Lore";
			this.btnAdd_Lore.Size = new System.Drawing.Size(97, 64);
			this.btnAdd_Lore.TabIndex = 7;
			this.btnAdd_Lore.UseVisualStyleBackColor = false;
			this.btnAdd_Lore.MouseClick += new System.Windows.Forms.MouseEventHandler(this.BtnAdd_Lore_MouseClick);
			// 
			// btnAdd_Snippets
			// 
			this.btnAdd_Snippets.BackColor = System.Drawing.Color.WhiteSmoke;
			this.btnAdd_Snippets.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnAdd_Snippets.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
			this.btnAdd_Snippets.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
			this.btnAdd_Snippets.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
			this.btnAdd_Snippets.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnAdd_Snippets.Image = global::Ginger.Properties.Resources.snippet;
			this.btnAdd_Snippets.Location = new System.Drawing.Point(577, 0);
			this.btnAdd_Snippets.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
			this.btnAdd_Snippets.Name = "btnAdd_Snippets";
			this.btnAdd_Snippets.Size = new System.Drawing.Size(94, 64);
			this.btnAdd_Snippets.TabIndex = 6;
			this.btnAdd_Snippets.UseVisualStyleBackColor = false;
			this.btnAdd_Snippets.MouseClick += new System.Windows.Forms.MouseEventHandler(this.BtnAdd_Snippets_MouseClick);
			// 
			// btnAdd_Other
			// 
			this.btnAdd_Other.BackColor = System.Drawing.Color.WhiteSmoke;
			this.btnAdd_Other.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnAdd_Other.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
			this.btnAdd_Other.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
			this.btnAdd_Other.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
			this.btnAdd_Other.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnAdd_Other.Image = global::Ginger.Properties.Resources.component;
			this.btnAdd_Other.Location = new System.Drawing.Point(481, 0);
			this.btnAdd_Other.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
			this.btnAdd_Other.Name = "btnAdd_Other";
			this.btnAdd_Other.Size = new System.Drawing.Size(94, 64);
			this.btnAdd_Other.TabIndex = 5;
			this.btnAdd_Other.UseVisualStyleBackColor = false;
			this.btnAdd_Other.MouseClick += new System.Windows.Forms.MouseEventHandler(this.BtnAdd_Other_MouseClick);
			// 
			// btnAdd_World
			// 
			this.btnAdd_World.BackColor = System.Drawing.Color.WhiteSmoke;
			this.btnAdd_World.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnAdd_World.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
			this.btnAdd_World.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
			this.btnAdd_World.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
			this.btnAdd_World.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnAdd_World.Image = global::Ginger.Properties.Resources.story;
			this.btnAdd_World.Location = new System.Drawing.Point(385, 0);
			this.btnAdd_World.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
			this.btnAdd_World.Name = "btnAdd_World";
			this.btnAdd_World.Size = new System.Drawing.Size(94, 64);
			this.btnAdd_World.TabIndex = 4;
			this.btnAdd_World.UseVisualStyleBackColor = false;
			this.btnAdd_World.MouseClick += new System.Windows.Forms.MouseEventHandler(this.BtnAdd_Scenario_MouseClick);
			// 
			// btnAdd_Mind
			// 
			this.btnAdd_Mind.BackColor = System.Drawing.Color.WhiteSmoke;
			this.btnAdd_Mind.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnAdd_Mind.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
			this.btnAdd_Mind.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
			this.btnAdd_Mind.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
			this.btnAdd_Mind.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnAdd_Mind.Image = global::Ginger.Properties.Resources.personality;
			this.btnAdd_Mind.Location = new System.Drawing.Point(289, 0);
			this.btnAdd_Mind.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
			this.btnAdd_Mind.Name = "btnAdd_Mind";
			this.btnAdd_Mind.Size = new System.Drawing.Size(94, 64);
			this.btnAdd_Mind.TabIndex = 3;
			this.btnAdd_Mind.UseVisualStyleBackColor = false;
			this.btnAdd_Mind.MouseClick += new System.Windows.Forms.MouseEventHandler(this.BtnAdd_Mind_MouseClick);
			// 
			// btnAdd_Traits
			// 
			this.btnAdd_Traits.BackColor = System.Drawing.Color.WhiteSmoke;
			this.btnAdd_Traits.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnAdd_Traits.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
			this.btnAdd_Traits.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
			this.btnAdd_Traits.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
			this.btnAdd_Traits.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnAdd_Traits.Image = global::Ginger.Properties.Resources.characteristic;
			this.btnAdd_Traits.Location = new System.Drawing.Point(193, 0);
			this.btnAdd_Traits.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
			this.btnAdd_Traits.Name = "btnAdd_Traits";
			this.btnAdd_Traits.Size = new System.Drawing.Size(94, 64);
			this.btnAdd_Traits.TabIndex = 2;
			this.btnAdd_Traits.UseVisualStyleBackColor = false;
			this.btnAdd_Traits.MouseClick += new System.Windows.Forms.MouseEventHandler(this.btnAdd_Trait_Click);
			// 
			// btnAdd_Character
			// 
			this.btnAdd_Character.BackColor = System.Drawing.Color.WhiteSmoke;
			this.btnAdd_Character.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnAdd_Character.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
			this.btnAdd_Character.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
			this.btnAdd_Character.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
			this.btnAdd_Character.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnAdd_Character.Image = global::Ginger.Properties.Resources.persona;
			this.btnAdd_Character.Location = new System.Drawing.Point(97, 0);
			this.btnAdd_Character.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
			this.btnAdd_Character.Name = "btnAdd_Character";
			this.btnAdd_Character.Size = new System.Drawing.Size(94, 64);
			this.btnAdd_Character.TabIndex = 1;
			this.btnAdd_Character.UseVisualStyleBackColor = false;
			this.btnAdd_Character.MouseClick += new System.Windows.Forms.MouseEventHandler(this.BtnAdd_Character_MouseClick);
			// 
			// btnAdd_Model
			// 
			this.btnAdd_Model.BackColor = System.Drawing.Color.WhiteSmoke;
			this.btnAdd_Model.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnAdd_Model.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
			this.btnAdd_Model.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
			this.btnAdd_Model.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
			this.btnAdd_Model.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnAdd_Model.Image = global::Ginger.Properties.Resources.model;
			this.btnAdd_Model.Location = new System.Drawing.Point(0, 0);
			this.btnAdd_Model.Margin = new System.Windows.Forms.Padding(0, 0, 1, 0);
			this.btnAdd_Model.Name = "btnAdd_Model";
			this.btnAdd_Model.Size = new System.Drawing.Size(95, 64);
			this.btnAdd_Model.TabIndex = 0;
			this.btnAdd_Model.UseVisualStyleBackColor = true;
			this.btnAdd_Model.MouseClick += new System.Windows.Forms.MouseEventHandler(this.BtnAddModel_MouseClick);
			// 
			// tabOutput
			// 
			this.tabOutput.BackColor = System.Drawing.SystemColors.Control;
			this.tabOutput.Controls.Add(this.panelOutput);
			this.tabOutput.Location = new System.Drawing.Point(4, 4);
			this.tabOutput.Name = "tabOutput";
			this.tabOutput.Size = new System.Drawing.Size(773, 767);
			this.tabOutput.TabIndex = 1;
			this.tabOutput.Text = "Output";
			// 
			// panelOutput
			// 
			this.panelOutput.AutoScroll = true;
			this.panelOutput.AutoSize = true;
			this.panelOutput.Controls.Add(this.outputBox);
			this.panelOutput.Controls.Add(this.group_Debug);
			this.panelOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelOutput.Location = new System.Drawing.Point(0, 0);
			this.panelOutput.Name = "panelOutput";
			this.panelOutput.Padding = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.panelOutput.Size = new System.Drawing.Size(773, 767);
			this.panelOutput.TabIndex = 4;
			// 
			// outputBox
			// 
			this.outputBox.AcceptsReturn = true;
			this.outputBox.AcceptsTab = true;
			this.outputBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.outputBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.outputBox.CausesValidation = false;
			this.outputBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.outputBox.Font = new System.Drawing.Font("Consolas", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.outputBox.ForeColor = System.Drawing.Color.Beige;
			this.outputBox.HighlightBorder = false;
			this.outputBox.InnerBorder = false;
			this.outputBox.Location = new System.Drawing.Point(0, 464);
			this.outputBox.Multiline = true;
			this.outputBox.Name = "outputBox";
			this.outputBox.Placeholder = null;
			this.outputBox.ReadOnly = true;
			this.outputBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.outputBox.Size = new System.Drawing.Size(770, 303);
			this.outputBox.TabIndex = 3;
			this.outputBox.TabStop = false;
			// 
			// group_Debug
			// 
			this.group_Debug.AutoSize = true;
			this.group_Debug.Controls.Add(this.outputBox_Raw2);
			this.group_Debug.Controls.Add(this.outputBox_Raw);
			this.group_Debug.Dock = System.Windows.Forms.DockStyle.Top;
			this.group_Debug.Location = new System.Drawing.Point(0, 0);
			this.group_Debug.Name = "group_Debug";
			this.group_Debug.Size = new System.Drawing.Size(770, 464);
			this.group_Debug.TabIndex = 8;
			this.group_Debug.TabStop = false;
			this.group_Debug.Text = "JSON";
			this.group_Debug.Visible = false;
			// 
			// outputBox_Raw2
			// 
			this.outputBox_Raw2.AcceptsReturn = true;
			this.outputBox_Raw2.AcceptsTab = true;
			this.outputBox_Raw2.BackColor = System.Drawing.SystemColors.Window;
			this.outputBox_Raw2.Dock = System.Windows.Forms.DockStyle.Top;
			this.outputBox_Raw2.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.outputBox_Raw2.Location = new System.Drawing.Point(3, 241);
			this.outputBox_Raw2.Margin = new System.Windows.Forms.Padding(0);
			this.outputBox_Raw2.Multiline = true;
			this.outputBox_Raw2.Name = "outputBox_Raw2";
			this.outputBox_Raw2.Placeholder = null;
			this.outputBox_Raw2.ReadOnly = true;
			this.outputBox_Raw2.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.outputBox_Raw2.Size = new System.Drawing.Size(764, 220);
			this.outputBox_Raw2.TabIndex = 3;
			this.outputBox_Raw2.TabStop = false;
			// 
			// outputBox_Raw
			// 
			this.outputBox_Raw.AcceptsReturn = true;
			this.outputBox_Raw.AcceptsTab = true;
			this.outputBox_Raw.BackColor = System.Drawing.SystemColors.Window;
			this.outputBox_Raw.Dock = System.Windows.Forms.DockStyle.Top;
			this.outputBox_Raw.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.outputBox_Raw.Location = new System.Drawing.Point(3, 21);
			this.outputBox_Raw.Margin = new System.Windows.Forms.Padding(0);
			this.outputBox_Raw.Multiline = true;
			this.outputBox_Raw.Name = "outputBox_Raw";
			this.outputBox_Raw.Placeholder = null;
			this.outputBox_Raw.ReadOnly = true;
			this.outputBox_Raw.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.outputBox_Raw.Size = new System.Drawing.Size(764, 220);
			this.outputBox_Raw.TabIndex = 2;
			this.outputBox_Raw.TabStop = false;
			// 
			// tabNotes
			// 
			this.tabNotes.BackColor = System.Drawing.SystemColors.Control;
			this.tabNotes.Controls.Add(this.userNotes);
			this.tabNotes.Location = new System.Drawing.Point(4, 4);
			this.tabNotes.Name = "tabNotes";
			this.tabNotes.Size = new System.Drawing.Size(773, 767);
			this.tabNotes.TabIndex = 2;
			this.tabNotes.Text = "Notes";
			// 
			// userNotes
			// 
			this.userNotes.Dock = System.Windows.Forms.DockStyle.Fill;
			this.userNotes.Location = new System.Drawing.Point(0, 0);
			this.userNotes.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.userNotes.Name = "userNotes";
			this.userNotes.Padding = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.userNotes.Size = new System.Drawing.Size(773, 767);
			this.userNotes.TabIndex = 0;
			// 
			// menuStrip
			// 
			this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editMenu,
            this.viewToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.helpToolStripMenuItem});
			this.menuStrip.Location = new System.Drawing.Point(0, 0);
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Size = new System.Drawing.Size(1264, 24);
			this.menuStrip.TabIndex = 0;
			this.menuStrip.Text = "menuStrip1";
			this.menuStrip.MenuActivate += new System.EventHandler(this.MainMenuActivate);
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newMenuItem,
            this.newWindowMenuItem,
            this.newFromTemplateMenuItem,
            this.openMenuItem,
            this.openRecentMenuItem,
            this.saveToolStripMenuItem,
            this.saveasToolStripMenuItem,
            this.saveIncrementalMenuItem,
            toolStripMenuItem3,
            this.importToolStripMenuItem,
            this.exportToolStripMenuItem,
            linkMenuItem,
            toolStripMenuItem1,
            this.changeLanguageMenuItem,
            this.changeLanguageSeparator,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// newMenuItem
			// 
			this.newMenuItem.Name = "newMenuItem";
			this.newMenuItem.ShortcutKeyDisplayString = "Ctrl+N";
			this.newMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.newMenuItem.Size = new System.Drawing.Size(259, 22);
			this.newMenuItem.Text = "&New";
			this.newMenuItem.Click += new System.EventHandler(this.NewToolStripMenuItem_Click);
			// 
			// newWindowMenuItem
			// 
			this.newWindowMenuItem.Name = "newWindowMenuItem";
			this.newWindowMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+N";
			this.newWindowMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.N)));
			this.newWindowMenuItem.Size = new System.Drawing.Size(259, 22);
			this.newWindowMenuItem.Text = "New &window";
			this.newWindowMenuItem.Click += new System.EventHandler(this.NewWindowMenuItem_Click);
			// 
			// newFromTemplateMenuItem
			// 
			this.newFromTemplateMenuItem.Name = "newFromTemplateMenuItem";
			this.newFromTemplateMenuItem.Size = new System.Drawing.Size(259, 22);
			this.newFromTemplateMenuItem.Text = "New from &template";
			// 
			// openMenuItem
			// 
			this.openMenuItem.Name = "openMenuItem";
			this.openMenuItem.ShortcutKeyDisplayString = "Ctrl+O";
			this.openMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.openMenuItem.Size = new System.Drawing.Size(259, 22);
			this.openMenuItem.Text = "&Open...";
			this.openMenuItem.Click += new System.EventHandler(this.OpenFileMenuItem_Click);
			// 
			// openRecentMenuItem
			// 
			this.openRecentMenuItem.Name = "openRecentMenuItem";
			this.openRecentMenuItem.Size = new System.Drawing.Size(259, 22);
			this.openRecentMenuItem.Text = "Open &recent";
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+S";
			this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(259, 22);
			this.saveToolStripMenuItem.Text = "&Save";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.SaveToolStripMenuItem_Click);
			// 
			// saveasToolStripMenuItem
			// 
			this.saveasToolStripMenuItem.Name = "saveasToolStripMenuItem";
			this.saveasToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+S";
			this.saveasToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
			this.saveasToolStripMenuItem.Size = new System.Drawing.Size(259, 22);
			this.saveasToolStripMenuItem.Text = "Save &as...";
			this.saveasToolStripMenuItem.Click += new System.EventHandler(this.SaveasToolStripMenuItem_Click);
			// 
			// saveIncrementalMenuItem
			// 
			this.saveIncrementalMenuItem.Name = "saveIncrementalMenuItem";
			this.saveIncrementalMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+Shift+S";
			this.saveIncrementalMenuItem.Size = new System.Drawing.Size(259, 22);
			this.saveIncrementalMenuItem.Text = "Save &incremental";
			this.saveIncrementalMenuItem.ToolTipText = global::Ginger.Properties.Resources.tooltip_save_incremental;
			this.saveIncrementalMenuItem.Click += new System.EventHandler(this.SaveIncrementalMenuItem_Click);
			// 
			// importToolStripMenuItem
			// 
			this.importToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.importCharacterMenuItem,
            this.importLorebookMenuItem});
			this.importToolStripMenuItem.Name = "importToolStripMenuItem";
			this.importToolStripMenuItem.Size = new System.Drawing.Size(259, 22);
			this.importToolStripMenuItem.Text = "&Import";
			// 
			// importCharacterMenuItem
			// 
			this.importCharacterMenuItem.Name = "importCharacterMenuItem";
			this.importCharacterMenuItem.Size = new System.Drawing.Size(197, 22);
			this.importCharacterMenuItem.Text = "Import character card...";
			this.importCharacterMenuItem.Click += new System.EventHandler(this.ImportCharacterMenuItem_Click);
			// 
			// importLorebookMenuItem
			// 
			this.importLorebookMenuItem.Name = "importLorebookMenuItem";
			this.importLorebookMenuItem.Size = new System.Drawing.Size(197, 22);
			this.importLorebookMenuItem.Text = "Import lorebook file...";
			this.importLorebookMenuItem.Click += new System.EventHandler(this.ImportLorebookJsonMenuItem_Click);
			// 
			// exportToolStripMenuItem
			// 
			this.exportToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exportCharacterMenuItem,
            this.exportLorebookMenuItem});
			this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
			this.exportToolStripMenuItem.Size = new System.Drawing.Size(259, 22);
			this.exportToolStripMenuItem.Text = "&Export";
			// 
			// exportCharacterMenuItem
			// 
			this.exportCharacterMenuItem.Name = "exportCharacterMenuItem";
			this.exportCharacterMenuItem.Size = new System.Drawing.Size(195, 22);
			this.exportCharacterMenuItem.Text = "Export character card...";
			this.exportCharacterMenuItem.Click += new System.EventHandler(this.ExportCharacterMenuItem_Click);
			// 
			// exportLorebookMenuItem
			// 
			this.exportLorebookMenuItem.Name = "exportLorebookMenuItem";
			this.exportLorebookMenuItem.Size = new System.Drawing.Size(195, 22);
			this.exportLorebookMenuItem.Text = "Export lorebook file...";
			this.exportLorebookMenuItem.Click += new System.EventHandler(this.ExportLorebookMenuItem_Click);
			// 
			// changeLanguageMenuItem
			// 
			this.changeLanguageMenuItem.Name = "changeLanguageMenuItem";
			this.changeLanguageMenuItem.Size = new System.Drawing.Size(259, 22);
			this.changeLanguageMenuItem.Text = "Change &language";
			// 
			// changeLanguageSeparator
			// 
			this.changeLanguageSeparator.Name = "changeLanguageSeparator";
			this.changeLanguageSeparator.Size = new System.Drawing.Size(256, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(259, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.OnExitApplication);
			// 
			// editMenu
			// 
			this.editMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoMenuItem,
            this.redoMenuItem,
            this.toolStripMenuItem2,
            this.copyMenuItem,
            this.pasteMenuItem,
            toolStripMenuItem8,
            this.findMenuItem,
            this.findNextMenuItem,
            this.findPreviousMenuItem,
            this.findAndReplaceMenuItem,
            this.swapGenderMenuItem});
			this.editMenu.Name = "editMenu";
			this.editMenu.Size = new System.Drawing.Size(39, 20);
			this.editMenu.Text = "&Edit";
			// 
			// undoMenuItem
			// 
			this.undoMenuItem.Name = "undoMenuItem";
			this.undoMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+Z";
			this.undoMenuItem.Size = new System.Drawing.Size(242, 22);
			this.undoMenuItem.Text = "&Undo";
			this.undoMenuItem.Click += new System.EventHandler(this.UndoMenuItem_Click);
			// 
			// redoMenuItem
			// 
			this.redoMenuItem.Name = "redoMenuItem";
			this.redoMenuItem.ShortcutKeyDisplayString = "Ctrl+Alt+Y";
			this.redoMenuItem.Size = new System.Drawing.Size(242, 22);
			this.redoMenuItem.Text = "&Redo";
			this.redoMenuItem.Click += new System.EventHandler(this.RedoMenuItem_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(239, 6);
			// 
			// copyMenuItem
			// 
			this.copyMenuItem.Name = "copyMenuItem";
			this.copyMenuItem.Size = new System.Drawing.Size(242, 22);
			this.copyMenuItem.Text = "Copy all";
			this.copyMenuItem.Click += new System.EventHandler(this.copyMenuItem_Click);
			// 
			// pasteMenuItem
			// 
			this.pasteMenuItem.Name = "pasteMenuItem";
			this.pasteMenuItem.Size = new System.Drawing.Size(242, 22);
			this.pasteMenuItem.Text = "Paste";
			this.pasteMenuItem.Click += new System.EventHandler(this.pasteMenuItem_Click);
			// 
			// findMenuItem
			// 
			this.findMenuItem.Name = "findMenuItem";
			this.findMenuItem.ShortcutKeyDisplayString = "Ctrl+F";
			this.findMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
			this.findMenuItem.Size = new System.Drawing.Size(242, 22);
			this.findMenuItem.Text = "Find...";
			this.findMenuItem.Click += new System.EventHandler(this.findMenuItem_Click);
			// 
			// findNextMenuItem
			// 
			this.findNextMenuItem.Name = "findNextMenuItem";
			this.findNextMenuItem.ShortcutKeyDisplayString = "F3";
			this.findNextMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F3;
			this.findNextMenuItem.Size = new System.Drawing.Size(242, 22);
			this.findNextMenuItem.Text = "Find next";
			this.findNextMenuItem.Click += new System.EventHandler(this.findNextMenuItem_Click);
			// 
			// findPreviousMenuItem
			// 
			this.findPreviousMenuItem.Name = "findPreviousMenuItem";
			this.findPreviousMenuItem.ShortcutKeyDisplayString = "Shift+F3";
			this.findPreviousMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F3)));
			this.findPreviousMenuItem.Size = new System.Drawing.Size(242, 22);
			this.findPreviousMenuItem.Text = "Find previous";
			this.findPreviousMenuItem.Click += new System.EventHandler(this.findPreviousMenuItem_Click);
			// 
			// findAndReplaceMenuItem
			// 
			this.findAndReplaceMenuItem.Name = "findAndReplaceMenuItem";
			this.findAndReplaceMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+F";
			this.findAndReplaceMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.F)));
			this.findAndReplaceMenuItem.Size = new System.Drawing.Size(242, 22);
			this.findAndReplaceMenuItem.Text = "Find and r&eplace...";
			this.findAndReplaceMenuItem.Click += new System.EventHandler(this.replaceMenuItem_Click);
			// 
			// swapGenderMenuItem
			// 
			this.swapGenderMenuItem.Name = "swapGenderMenuItem";
			this.swapGenderMenuItem.Size = new System.Drawing.Size(242, 22);
			this.swapGenderMenuItem.Text = "Replace &pronouns...";
			this.swapGenderMenuItem.Click += new System.EventHandler(this.swapGenderMenuItem_Click);
			// 
			// viewToolStripMenuItem
			// 
			this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.viewRecipeMenuItem,
            this.viewOutputMenuItem,
            this.viewNotesMenuItem,
            this.embeddedAssetsMenuItem,
            this.customVariablesMenuItem,
            this.chatHistoryMenuItem,
            toolStripMenuItem5,
            this.collapseAllMenuItem,
            this.expandAllMenuItem,
            this.showRecipeCategoryMenuItem,
            this.sortRecipesMenuItem,
            toolStripMenuItem7,
            this.additionalCharactersMenuItem});
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.viewToolStripMenuItem.Text = "V&iew";
			// 
			// viewRecipeMenuItem
			// 
			this.viewRecipeMenuItem.Name = "viewRecipeMenuItem";
			this.viewRecipeMenuItem.ShortcutKeyDisplayString = "Ctrl+Tab";
			this.viewRecipeMenuItem.Size = new System.Drawing.Size(219, 22);
			this.viewRecipeMenuItem.Text = "R&ecipe";
			this.viewRecipeMenuItem.Click += new System.EventHandler(this.ViewRecipeMenuItem_CheckedChanged);
			// 
			// viewOutputMenuItem
			// 
			this.viewOutputMenuItem.Name = "viewOutputMenuItem";
			this.viewOutputMenuItem.ShortcutKeyDisplayString = "Ctrl+Tab";
			this.viewOutputMenuItem.Size = new System.Drawing.Size(219, 22);
			this.viewOutputMenuItem.Text = "&Generated output";
			this.viewOutputMenuItem.Click += new System.EventHandler(this.ViewOutputMenuItem_CheckedChanged);
			// 
			// viewNotesMenuItem
			// 
			this.viewNotesMenuItem.Name = "viewNotesMenuItem";
			this.viewNotesMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+Tab";
			this.viewNotesMenuItem.Size = new System.Drawing.Size(219, 22);
			this.viewNotesMenuItem.Text = "&Notes";
			this.viewNotesMenuItem.Click += new System.EventHandler(this.ViewNotesMenuItem_Click);
			// 
			// embeddedAssetsMenuItem
			// 
			this.embeddedAssetsMenuItem.Name = "embeddedAssetsMenuItem";
			this.embeddedAssetsMenuItem.Size = new System.Drawing.Size(219, 22);
			this.embeddedAssetsMenuItem.Text = "Embedded &assets...";
			this.embeddedAssetsMenuItem.Click += new System.EventHandler(this.embeddedAssetsMenuItem_Click);
			// 
			// customVariablesMenuItem
			// 
			this.customVariablesMenuItem.Name = "customVariablesMenuItem";
			this.customVariablesMenuItem.Size = new System.Drawing.Size(219, 22);
			this.customVariablesMenuItem.Text = "User-defined &variables...";
			this.customVariablesMenuItem.Click += new System.EventHandler(this.customVariablesMenuItem_Click);
			// 
			// chatHistoryMenuItem
			// 
			this.chatHistoryMenuItem.Name = "chatHistoryMenuItem";
			this.chatHistoryMenuItem.ShortcutKeyDisplayString = "Ctrl+H";
			this.chatHistoryMenuItem.Size = new System.Drawing.Size(219, 22);
			this.chatHistoryMenuItem.Text = "Chat &history...";
			this.chatHistoryMenuItem.Click += new System.EventHandler(this.chatHistoryMenuItem_Click);
			// 
			// collapseAllMenuItem
			// 
			this.collapseAllMenuItem.Enabled = false;
			this.collapseAllMenuItem.Name = "collapseAllMenuItem";
			this.collapseAllMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+E";
			this.collapseAllMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.E)));
			this.collapseAllMenuItem.Size = new System.Drawing.Size(219, 22);
			this.collapseAllMenuItem.Text = "&Collapse all";
			this.collapseAllMenuItem.Click += new System.EventHandler(this.CollapseAllMenuItem_Click);
			// 
			// expandAllMenuItem
			// 
			this.expandAllMenuItem.Enabled = false;
			this.expandAllMenuItem.Name = "expandAllMenuItem";
			this.expandAllMenuItem.ShortcutKeyDisplayString = "Ctrl+E";
			this.expandAllMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
			this.expandAllMenuItem.Size = new System.Drawing.Size(219, 22);
			this.expandAllMenuItem.Text = "E&xpand all";
			this.expandAllMenuItem.Click += new System.EventHandler(this.ExpandAllMenuItem_Click);
			// 
			// showRecipeCategoryMenuItem
			// 
			this.showRecipeCategoryMenuItem.Name = "showRecipeCategoryMenuItem";
			this.showRecipeCategoryMenuItem.Size = new System.Drawing.Size(219, 22);
			this.showRecipeCategoryMenuItem.Text = "Show recipe category";
			this.showRecipeCategoryMenuItem.Click += new System.EventHandler(this.showRecipeCategoryMenuItem_Click);
			// 
			// sortRecipesMenuItem
			// 
			this.sortRecipesMenuItem.Name = "sortRecipesMenuItem";
			this.sortRecipesMenuItem.Size = new System.Drawing.Size(219, 22);
			this.sortRecipesMenuItem.Text = "&Sort recipes by type";
			this.sortRecipesMenuItem.Click += new System.EventHandler(this.sortRecipesMenuItem_Click);
			// 
			// additionalCharactersMenuItem
			// 
			this.additionalCharactersMenuItem.Name = "additionalCharactersMenuItem";
			this.additionalCharactersMenuItem.Size = new System.Drawing.Size(219, 22);
			this.additionalCharactersMenuItem.Text = "Actors";
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tokenBudgetMenuItem,
            this.outputPreviewToolStripMenuItem,
            this.checkSpellingMenuItem,
            themeMenuItem,
            this.linkOptionsMenuItem,
            this.toolStripMenuItem11,
            this.autoConvertNameMenuItem,
            this.autoBreakMenuItem,
            this.rearrangeLoreMenuItem,
            this.showNSFWRecipesMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.optionsToolStripMenuItem.Text = "&Options";
			// 
			// tokenBudgetMenuItem
			// 
			this.tokenBudgetMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tokenBudgetNone,
            this.tokenBudget1K,
            this.tokenBudget2K,
            this.tokenBudget3K,
            this.tokenBudget4K,
            this.tokenBudget5K,
            this.tokenBudget6K,
            this.tokenBudget8K,
            this.tokenBudget10K,
            this.tokenBudget12K,
            this.tokenBudget16K,
            this.tokenBudget24K,
            this.tokenBudget32K});
			this.tokenBudgetMenuItem.Name = "tokenBudgetMenuItem";
			this.tokenBudgetMenuItem.Size = new System.Drawing.Size(183, 22);
			this.tokenBudgetMenuItem.Text = "Token budget";
			// 
			// tokenBudgetNone
			// 
			this.tokenBudgetNone.CheckOnClick = true;
			this.tokenBudgetNone.Name = "tokenBudgetNone";
			this.tokenBudgetNone.Size = new System.Drawing.Size(104, 22);
			this.tokenBudgetNone.Text = "None";
			this.tokenBudgetNone.CheckedChanged += new System.EventHandler(this.TokenBudgetMenuItem_CheckedChanged);
			// 
			// tokenBudget1K
			// 
			this.tokenBudget1K.CheckOnClick = true;
			this.tokenBudget1K.Name = "tokenBudget1K";
			this.tokenBudget1K.Size = new System.Drawing.Size(104, 22);
			this.tokenBudget1K.Text = "1024";
			this.tokenBudget1K.CheckedChanged += new System.EventHandler(this.TokenBudgetMenuItem_CheckedChanged);
			// 
			// tokenBudget2K
			// 
			this.tokenBudget2K.CheckOnClick = true;
			this.tokenBudget2K.Name = "tokenBudget2K";
			this.tokenBudget2K.Size = new System.Drawing.Size(104, 22);
			this.tokenBudget2K.Text = "2048";
			this.tokenBudget2K.CheckedChanged += new System.EventHandler(this.TokenBudgetMenuItem_CheckedChanged);
			// 
			// tokenBudget3K
			// 
			this.tokenBudget3K.CheckOnClick = true;
			this.tokenBudget3K.Name = "tokenBudget3K";
			this.tokenBudget3K.Size = new System.Drawing.Size(104, 22);
			this.tokenBudget3K.Text = "3072";
			this.tokenBudget3K.CheckedChanged += new System.EventHandler(this.TokenBudgetMenuItem_CheckedChanged);
			// 
			// tokenBudget4K
			// 
			this.tokenBudget4K.CheckOnClick = true;
			this.tokenBudget4K.Name = "tokenBudget4K";
			this.tokenBudget4K.Size = new System.Drawing.Size(104, 22);
			this.tokenBudget4K.Text = "4096";
			this.tokenBudget4K.CheckedChanged += new System.EventHandler(this.TokenBudgetMenuItem_CheckedChanged);
			// 
			// tokenBudget5K
			// 
			this.tokenBudget5K.CheckOnClick = true;
			this.tokenBudget5K.Name = "tokenBudget5K";
			this.tokenBudget5K.Size = new System.Drawing.Size(104, 22);
			this.tokenBudget5K.Text = "5120";
			this.tokenBudget5K.CheckedChanged += new System.EventHandler(this.TokenBudgetMenuItem_CheckedChanged);
			// 
			// tokenBudget6K
			// 
			this.tokenBudget6K.CheckOnClick = true;
			this.tokenBudget6K.Name = "tokenBudget6K";
			this.tokenBudget6K.Size = new System.Drawing.Size(104, 22);
			this.tokenBudget6K.Text = "6144";
			this.tokenBudget6K.CheckedChanged += new System.EventHandler(this.TokenBudgetMenuItem_CheckedChanged);
			// 
			// tokenBudget8K
			// 
			this.tokenBudget8K.CheckOnClick = true;
			this.tokenBudget8K.Name = "tokenBudget8K";
			this.tokenBudget8K.Size = new System.Drawing.Size(104, 22);
			this.tokenBudget8K.Text = "8192";
			this.tokenBudget8K.CheckedChanged += new System.EventHandler(this.TokenBudgetMenuItem_CheckedChanged);
			// 
			// tokenBudget10K
			// 
			this.tokenBudget10K.CheckOnClick = true;
			this.tokenBudget10K.Name = "tokenBudget10K";
			this.tokenBudget10K.Size = new System.Drawing.Size(104, 22);
			this.tokenBudget10K.Text = "10240";
			this.tokenBudget10K.CheckedChanged += new System.EventHandler(this.TokenBudgetMenuItem_CheckedChanged);
			// 
			// tokenBudget12K
			// 
			this.tokenBudget12K.CheckOnClick = true;
			this.tokenBudget12K.Name = "tokenBudget12K";
			this.tokenBudget12K.Size = new System.Drawing.Size(104, 22);
			this.tokenBudget12K.Text = "12288";
			this.tokenBudget12K.CheckedChanged += new System.EventHandler(this.TokenBudgetMenuItem_CheckedChanged);
			// 
			// tokenBudget16K
			// 
			this.tokenBudget16K.CheckOnClick = true;
			this.tokenBudget16K.Name = "tokenBudget16K";
			this.tokenBudget16K.Size = new System.Drawing.Size(104, 22);
			this.tokenBudget16K.Text = "16384";
			this.tokenBudget16K.CheckedChanged += new System.EventHandler(this.TokenBudgetMenuItem_CheckedChanged);
			// 
			// tokenBudget24K
			// 
			this.tokenBudget24K.CheckOnClick = true;
			this.tokenBudget24K.Name = "tokenBudget24K";
			this.tokenBudget24K.Size = new System.Drawing.Size(104, 22);
			this.tokenBudget24K.Text = "24576";
			this.tokenBudget24K.CheckedChanged += new System.EventHandler(this.TokenBudgetMenuItem_CheckedChanged);
			// 
			// tokenBudget32K
			// 
			this.tokenBudget32K.CheckOnClick = true;
			this.tokenBudget32K.Name = "tokenBudget32K";
			this.tokenBudget32K.Size = new System.Drawing.Size(104, 22);
			this.tokenBudget32K.Text = "32768";
			this.tokenBudget32K.CheckedChanged += new System.EventHandler(this.TokenBudgetMenuItem_CheckedChanged);
			// 
			// outputPreviewToolStripMenuItem
			// 
			this.outputPreviewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.outputPreviewDefaultMenuItem,
            this.outputPreviewSillyTavernMenuItem,
            this.outputPreviewFaradayMenuItem,
            this.outputPreviewPlainTextMenuItem});
			this.outputPreviewToolStripMenuItem.Name = "outputPreviewToolStripMenuItem";
			this.outputPreviewToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.outputPreviewToolStripMenuItem.Text = "Preview format";
			// 
			// outputPreviewDefaultMenuItem
			// 
			this.outputPreviewDefaultMenuItem.Checked = true;
			this.outputPreviewDefaultMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.outputPreviewDefaultMenuItem.Name = "outputPreviewDefaultMenuItem";
			this.outputPreviewDefaultMenuItem.Size = new System.Drawing.Size(157, 22);
			this.outputPreviewDefaultMenuItem.Text = "Ginger (default)";
			this.outputPreviewDefaultMenuItem.Click += new System.EventHandler(this.outputPreviewDefaultMenuItem_Click);
			// 
			// outputPreviewSillyTavernMenuItem
			// 
			this.outputPreviewSillyTavernMenuItem.Name = "outputPreviewSillyTavernMenuItem";
			this.outputPreviewSillyTavernMenuItem.Size = new System.Drawing.Size(157, 22);
			this.outputPreviewSillyTavernMenuItem.Text = "SillyTavern";
			this.outputPreviewSillyTavernMenuItem.Click += new System.EventHandler(this.outputPreviewSillyTavernMenuItem_Click);
			// 
			// outputPreviewFaradayMenuItem
			// 
			this.outputPreviewFaradayMenuItem.Name = "outputPreviewFaradayMenuItem";
			this.outputPreviewFaradayMenuItem.Size = new System.Drawing.Size(157, 22);
			this.outputPreviewFaradayMenuItem.Text = "Backyard AI";
			this.outputPreviewFaradayMenuItem.Click += new System.EventHandler(this.outputPreviewFaradayMenuItem_Click);
			// 
			// outputPreviewPlainTextMenuItem
			// 
			this.outputPreviewPlainTextMenuItem.Name = "outputPreviewPlainTextMenuItem";
			this.outputPreviewPlainTextMenuItem.Size = new System.Drawing.Size(157, 22);
			this.outputPreviewPlainTextMenuItem.Text = "Plain text";
			this.outputPreviewPlainTextMenuItem.Click += new System.EventHandler(this.outputPreviewPlainTextMenuItem_Click);
			// 
			// checkSpellingMenuItem
			// 
			this.checkSpellingMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.enableSpellCheckingMenuItem,
            this.toolStripMenuItem4});
			this.checkSpellingMenuItem.Name = "checkSpellingMenuItem";
			this.checkSpellingMenuItem.Size = new System.Drawing.Size(183, 22);
			this.checkSpellingMenuItem.Text = "Spell checking";
			// 
			// enableSpellCheckingMenuItem
			// 
			this.enableSpellCheckingMenuItem.Name = "enableSpellCheckingMenuItem";
			this.enableSpellCheckingMenuItem.Size = new System.Drawing.Size(116, 22);
			this.enableSpellCheckingMenuItem.Text = "Enabled";
			this.enableSpellCheckingMenuItem.Click += new System.EventHandler(this.EnableSpellCheckingMenuItem_Click);
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new System.Drawing.Size(113, 6);
			// 
			// linkOptionsMenuItem
			// 
			this.linkOptionsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.enableAutosaveMenuItem,
            this.alwaysLinkMenuItem,
            this.usePortraitAsBackgroundMenuItem,
            this.editExportModelSettingsMenuItem,
            writeChatSettingsMenuItem});
			this.linkOptionsMenuItem.Name = "linkOptionsMenuItem";
			this.linkOptionsMenuItem.Size = new System.Drawing.Size(183, 22);
			this.linkOptionsMenuItem.Text = "Link options";
			// 
			// enableAutosaveMenuItem
			// 
			this.enableAutosaveMenuItem.Name = "enableAutosaveMenuItem";
			this.enableAutosaveMenuItem.Size = new System.Drawing.Size(213, 22);
			this.enableAutosaveMenuItem.Text = "Synchronized saving";
			this.enableAutosaveMenuItem.Click += new System.EventHandler(this.enableAutosaveMenuItem_Click);
			// 
			// alwaysLinkMenuItem
			// 
			this.alwaysLinkMenuItem.Name = "alwaysLinkMenuItem";
			this.alwaysLinkMenuItem.Size = new System.Drawing.Size(213, 22);
			this.alwaysLinkMenuItem.Text = "Always link characters";
			this.alwaysLinkMenuItem.Click += new System.EventHandler(this.alwaysLinkMenuItem_Click);
			// 
			// usePortraitAsBackgroundMenuItem
			// 
			this.usePortraitAsBackgroundMenuItem.Name = "usePortraitAsBackgroundMenuItem";
			this.usePortraitAsBackgroundMenuItem.Size = new System.Drawing.Size(213, 22);
			this.usePortraitAsBackgroundMenuItem.Text = "Set portrait as background";
			this.usePortraitAsBackgroundMenuItem.Click += new System.EventHandler(this.usePortraitAsBackgroundMenuItem_Click);
			// 
			// editExportModelSettingsMenuItem
			// 
			this.editExportModelSettingsMenuItem.Name = "editExportModelSettingsMenuItem";
			this.editExportModelSettingsMenuItem.Size = new System.Drawing.Size(213, 22);
			this.editExportModelSettingsMenuItem.Text = "Default model settings...";
			this.editExportModelSettingsMenuItem.Click += new System.EventHandler(this.editExportModelSettingsMenuItem_Click);
			// 
			// toolStripMenuItem11
			// 
			this.toolStripMenuItem11.Name = "toolStripMenuItem11";
			this.toolStripMenuItem11.Size = new System.Drawing.Size(180, 6);
			// 
			// autoConvertNameMenuItem
			// 
			this.autoConvertNameMenuItem.Checked = true;
			this.autoConvertNameMenuItem.CheckOnClick = true;
			this.autoConvertNameMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.autoConvertNameMenuItem.Name = "autoConvertNameMenuItem";
			this.autoConvertNameMenuItem.Size = new System.Drawing.Size(183, 22);
			this.autoConvertNameMenuItem.Text = "Auto-convert names";
			this.autoConvertNameMenuItem.ToolTipText = global::Ginger.Properties.Resources.tooltip_option_use_placeholders;
			this.autoConvertNameMenuItem.Click += new System.EventHandler(this.AutoconvertCharacterMarkersMenuItem_CheckedChanged);
			// 
			// autoBreakMenuItem
			// 
			this.autoBreakMenuItem.Name = "autoBreakMenuItem";
			this.autoBreakMenuItem.Size = new System.Drawing.Size(183, 22);
			this.autoBreakMenuItem.Text = "Limit line width";
			this.autoBreakMenuItem.Click += new System.EventHandler(this.AutoBreakMenuItem_Click);
			// 
			// rearrangeLoreMenuItem
			// 
			this.rearrangeLoreMenuItem.Name = "rearrangeLoreMenuItem";
			this.rearrangeLoreMenuItem.Size = new System.Drawing.Size(183, 22);
			this.rearrangeLoreMenuItem.Text = "Rearrange lore";
			this.rearrangeLoreMenuItem.Click += new System.EventHandler(this.rearrangeLoreMenuItem_Click);
			// 
			// showNSFWRecipesMenuItem
			// 
			this.showNSFWRecipesMenuItem.CheckOnClick = true;
			this.showNSFWRecipesMenuItem.Name = "showNSFWRecipesMenuItem";
			this.showNSFWRecipesMenuItem.Size = new System.Drawing.Size(183, 22);
			this.showNSFWRecipesMenuItem.Text = "Allow &NSFW content";
			this.showNSFWRecipesMenuItem.ToolTipText = global::Ginger.Properties.Resources.tooltip_option_nsfw;
			this.showNSFWRecipesMenuItem.Click += new System.EventHandler(this.showNSFWRecipesMenuItem_Click);
			// 
			// toolsToolStripMenuItem
			// 
			this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.createRecipeMenuItem,
            this.createSnippetMenuItem,
            this.reloadRecipesToolStripMenuItem,
            toolStripSeparator1,
            this.bakeAllMenuItem,
            this.bakeActorMenuItem,
            this.mergeLoreMenuItem});
			this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
			this.toolsToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
			this.toolsToolStripMenuItem.Text = "&Tools";
			// 
			// createRecipeMenuItem
			// 
			this.createRecipeMenuItem.Name = "createRecipeMenuItem";
			this.createRecipeMenuItem.Size = new System.Drawing.Size(187, 22);
			this.createRecipeMenuItem.Text = "New recipe...";
			this.createRecipeMenuItem.ToolTipText = global::Ginger.Properties.Resources.tooltip_create_recipe;
			this.createRecipeMenuItem.Click += new System.EventHandler(this.CreateRecipeMenuItem_Click);
			// 
			// createSnippetMenuItem
			// 
			this.createSnippetMenuItem.Name = "createSnippetMenuItem";
			this.createSnippetMenuItem.Size = new System.Drawing.Size(187, 22);
			this.createSnippetMenuItem.Text = "New snippet...";
			this.createSnippetMenuItem.ToolTipText = global::Ginger.Properties.Resources.tooltip_create_snippet;
			this.createSnippetMenuItem.Click += new System.EventHandler(this.CreateSnippetMenuItem_Click);
			// 
			// reloadRecipesToolStripMenuItem
			// 
			this.reloadRecipesToolStripMenuItem.Name = "reloadRecipesToolStripMenuItem";
			this.reloadRecipesToolStripMenuItem.ShortcutKeyDisplayString = "F5";
			this.reloadRecipesToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.reloadRecipesToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
			this.reloadRecipesToolStripMenuItem.Text = "&Reload recipes";
			this.reloadRecipesToolStripMenuItem.ToolTipText = global::Ginger.Properties.Resources.tooltip_reload_recipes;
			this.reloadRecipesToolStripMenuItem.Click += new System.EventHandler(this.ReloadRecipesToolStripMenuItem_Click);
			// 
			// bakeAllMenuItem
			// 
			this.bakeAllMenuItem.Name = "bakeAllMenuItem";
			this.bakeAllMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+B";
			this.bakeAllMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.B)));
			this.bakeAllMenuItem.Size = new System.Drawing.Size(187, 22);
			this.bakeAllMenuItem.Text = "&Bake all";
			this.bakeAllMenuItem.Click += new System.EventHandler(this.BtnBakeAll_Click);
			// 
			// bakeActorMenuItem
			// 
			this.bakeActorMenuItem.Name = "bakeActorMenuItem";
			this.bakeActorMenuItem.Size = new System.Drawing.Size(187, 22);
			this.bakeActorMenuItem.Text = "Bake &actor";
			this.bakeActorMenuItem.Click += new System.EventHandler(this.BtnBakeActor_Click);
			// 
			// mergeLoreMenuItem
			// 
			this.mergeLoreMenuItem.Name = "mergeLoreMenuItem";
			this.mergeLoreMenuItem.Size = new System.Drawing.Size(187, 22);
			this.mergeLoreMenuItem.Text = "Merge &lorebooks";
			this.mergeLoreMenuItem.ToolTipText = global::Ginger.Properties.Resources.tooltip_merge_lorebooks;
			this.mergeLoreMenuItem.Click += new System.EventHandler(this.MergeLoreMenuItem_Click);
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.viewHelpToolStripMenuItem,
            this.visitGitHubPageMenuItem,
            toolStripMenuItem6,
            this.aboutGingerToolStripMenuItem});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.helpToolStripMenuItem.Text = "&Help";
			// 
			// viewHelpToolStripMenuItem
			// 
			this.viewHelpToolStripMenuItem.Name = "viewHelpToolStripMenuItem";
			this.viewHelpToolStripMenuItem.ShortcutKeyDisplayString = "F1";
			this.viewHelpToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F1;
			this.viewHelpToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
			this.viewHelpToolStripMenuItem.Text = "Ginger &Help";
			this.viewHelpToolStripMenuItem.Click += new System.EventHandler(this.viewHelpToolStripMenuItem_Click);
			// 
			// visitGitHubPageMenuItem
			// 
			this.visitGitHubPageMenuItem.Name = "visitGitHubPageMenuItem";
			this.visitGitHubPageMenuItem.Size = new System.Drawing.Size(166, 22);
			this.visitGitHubPageMenuItem.Text = "Visit GitHub page";
			this.visitGitHubPageMenuItem.Click += new System.EventHandler(this.visitGitHubPageMenuItem_Click);
			// 
			// aboutGingerToolStripMenuItem
			// 
			this.aboutGingerToolStripMenuItem.Name = "aboutGingerToolStripMenuItem";
			this.aboutGingerToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
			this.aboutGingerToolStripMenuItem.Text = "About Ginger";
			this.aboutGingerToolStripMenuItem.Click += new System.EventHandler(this.aboutGingerToolStripMenuItem_Click);
			// 
			// openFileDialog
			// 
			this.openFileDialog.DefaultExt = "png";
			this.openFileDialog.Filter = "PNG files|*.png";
			this.openFileDialog.SupportMultiDottedExtensions = true;
			// 
			// saveFileDialog
			// 
			this.saveFileDialog.FileName = "Untitled.png";
			this.saveFileDialog.Filter = "PNG file|*.png";
			// 
			// exportFileDialog
			// 
			this.exportFileDialog.Filter = "Json file|*.json";
			this.exportFileDialog.Title = "Export";
			// 
			// importFileDialog
			// 
			this.importFileDialog.DefaultExt = "png";
			this.importFileDialog.Filter = "Json file|*.json";
			this.importFileDialog.SupportMultiDottedExtensions = true;
			this.importFileDialog.Title = "Import";
			// 
			// statusBar
			// 
			this.statusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusBarMessage,
            this.statusBarActor,
            this.statusConnectionIcon});
			this.statusBar.Location = new System.Drawing.Point(0, 799);
			this.statusBar.Name = "statusBar";
			this.statusBar.ShowItemToolTips = true;
			this.statusBar.Size = new System.Drawing.Size(1264, 22);
			this.statusBar.TabIndex = 1;
			// 
			// statusBarMessage
			// 
			this.statusBarMessage.Name = "statusBarMessage";
			this.statusBarMessage.Size = new System.Drawing.Size(0, 17);
			// 
			// statusBarActor
			// 
			this.statusBarActor.Margin = new System.Windows.Forms.Padding(0, 3, 18, 2);
			this.statusBarActor.Name = "statusBarActor";
			this.statusBarActor.Size = new System.Drawing.Size(1231, 17);
			this.statusBarActor.Spring = true;
			this.statusBarActor.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// statusConnectionIcon
			// 
			this.statusConnectionIcon.Name = "statusConnectionIcon";
			this.statusConnectionIcon.Size = new System.Drawing.Size(0, 17);
			// 
			// MainForm
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1264, 821);
			this.Controls.Add(this.splitContainer);
			this.Controls.Add(this.menuStrip);
			this.Controls.Add(this.statusBar);
			this.DoubleBuffered = true;
			this.MainMenuStrip = this.menuStrip;
			this.MinimumSize = new System.Drawing.Size(980, 650);
			this.Name = "MainForm";
			this.Text = "Ginger";
			this.Load += new System.EventHandler(this.MainForm_OnLoad);
			this.ResizeBegin += new System.EventHandler(this.MainForm_ResizeBegin);
			this.ResizeEnd += new System.EventHandler(this.MainForm_ResizeEnd);
			this.splitContainer.Panel1.ResumeLayout(false);
			this.splitContainer.Panel1.PerformLayout();
			this.splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
			this.splitContainer.ResumeLayout(false);
			this.tabControl.ResumeLayout(false);
			this.tabRecipe.ResumeLayout(false);
			this.panelRecipe.ResumeLayout(false);
			this.buttonRow.ResumeLayout(false);
			this.tabOutput.ResumeLayout(false);
			this.tabOutput.PerformLayout();
			this.panelOutput.ResumeLayout(false);
			this.panelOutput.PerformLayout();
			this.group_Debug.ResumeLayout(false);
			this.group_Debug.PerformLayout();
			this.tabNotes.ResumeLayout(false);
			this.menuStrip.ResumeLayout(false);
			this.menuStrip.PerformLayout();
			this.statusBar.ResumeLayout(false);
			this.statusBar.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.SplitContainer splitContainer;
		private System.Windows.Forms.MenuStrip menuStrip;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem newMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private TextBoxEx outputBox_Raw;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveasToolStripMenuItem;
		private SidePanel sidePanel;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private OutputPreview outputBox;
		private System.Windows.Forms.GroupBox group_Debug;
		private System.Windows.Forms.Panel panelOutput;
		private System.Windows.Forms.Button btnAdd_Model;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
		private System.Windows.Forms.TableLayoutPanel buttonRow;
		private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem reloadRecipesToolStripMenuItem;
		private TextBoxEx outputBox_Raw2;
		private System.Windows.Forms.ToolStripMenuItem importToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
		private System.Windows.Forms.Button btnAdd_Lore;
		private System.Windows.Forms.Button btnAdd_World;
		private System.Windows.Forms.Button btnAdd_Character;
		private System.Windows.Forms.Button btnAdd_Snippets;
		private System.Windows.Forms.ToolStripMenuItem newFromTemplateMenuItem;
		private System.Windows.Forms.Button btnAdd_Other;
		private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autoConvertNameMenuItem;
		private System.Windows.Forms.ToolStripMenuItem bakeAllMenuItem;
		private System.Windows.Forms.Panel panelRecipe;
		private System.Windows.Forms.ToolStripMenuItem exportCharacterMenuItem;
		private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem viewOutputMenuItem;
		private System.Windows.Forms.ToolStripMenuItem viewRecipeMenuItem;
		private System.Windows.Forms.ToolStripMenuItem expandAllMenuItem;
		private System.Windows.Forms.ToolStripMenuItem collapseAllMenuItem;
		private System.Windows.Forms.ToolStripMenuItem createRecipeMenuItem;
		private System.Windows.Forms.SaveFileDialog exportFileDialog;
		private System.Windows.Forms.OpenFileDialog importFileDialog;
		private System.Windows.Forms.ToolStripMenuItem editMenu;
		private System.Windows.Forms.ToolStripMenuItem undoMenuItem;
		private System.Windows.Forms.ToolStripMenuItem redoMenuItem;
		private System.Windows.Forms.Button btnAdd_Mind;
		private System.Windows.Forms.ToolStripMenuItem importCharacterMenuItem;
		private System.Windows.Forms.ToolStripMenuItem importLorebookMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exportLorebookMenuItem;
		private System.Windows.Forms.CustomTabControl tabControl;
		private System.Windows.Forms.TabPage tabRecipe;
		private System.Windows.Forms.TabPage tabOutput;
		private System.Windows.Forms.ToolStripMenuItem newWindowMenuItem;
		private System.Windows.Forms.StatusStrip statusBar;
		private System.Windows.Forms.ToolStripMenuItem additionalCharactersMenuItem;
		private System.Windows.Forms.ToolStripMenuItem createSnippetMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openRecentMenuItem;
		private System.Windows.Forms.ToolStripStatusLabel statusBarActor;
		private System.Windows.Forms.ToolStripMenuItem tokenBudgetMenuItem;
		private System.Windows.Forms.ToolStripMenuItem tokenBudgetNone;
		private System.Windows.Forms.ToolStripMenuItem tokenBudget1K;
		private System.Windows.Forms.ToolStripMenuItem tokenBudget2K;
		private System.Windows.Forms.ToolStripMenuItem tokenBudget3K;
		private System.Windows.Forms.ToolStripMenuItem tokenBudget4K;
		private System.Windows.Forms.ToolStripMenuItem tokenBudget6K;
		private System.Windows.Forms.ToolStripMenuItem tokenBudget8K;
		private System.Windows.Forms.ToolStripMenuItem tokenBudget12K;
		private System.Windows.Forms.ToolStripMenuItem tokenBudget16K;
		private System.Windows.Forms.ToolStripMenuItem tokenBudget24K;
		private System.Windows.Forms.ToolStripMenuItem tokenBudget32K;
		private System.Windows.Forms.ToolStripMenuItem swapGenderMenuItem;
		private System.Windows.Forms.ToolStripMenuItem findAndReplaceMenuItem;
		private System.Windows.Forms.TabPage tabNotes;
		private Interface.Controls.UserNotes userNotes;
		private System.Windows.Forms.Button btnAdd_Traits;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem viewHelpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem aboutGingerToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem showNSFWRecipesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem bakeActorMenuItem;
		private System.Windows.Forms.ToolStripMenuItem visitGitHubPageMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveIncrementalMenuItem;
		private System.Windows.Forms.ToolStripMenuItem findMenuItem;
		private System.Windows.Forms.ToolStripMenuItem findNextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem findPreviousMenuItem;
		private System.Windows.Forms.ToolStripMenuItem outputPreviewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem outputPreviewDefaultMenuItem;
		private System.Windows.Forms.ToolStripMenuItem outputPreviewSillyTavernMenuItem;
		private System.Windows.Forms.ToolStripMenuItem outputPreviewFaradayMenuItem;
		private System.Windows.Forms.ToolStripMenuItem outputPreviewPlainTextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem viewNotesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem mergeLoreMenuItem;
		private System.Windows.Forms.ToolStripMenuItem checkSpellingMenuItem;
		private System.Windows.Forms.ToolStripMenuItem enableSpellCheckingMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
		private System.Windows.Forms.ToolStripMenuItem autoBreakMenuItem;
		private System.Windows.Forms.ToolStripMenuItem sortRecipesMenuItem;
		public RecipeList recipeList;
		private System.Windows.Forms.ToolStripStatusLabel statusBarMessage;
		private System.Windows.Forms.ToolStripMenuItem changeLanguageMenuItem;
		private System.Windows.Forms.ToolStripSeparator changeLanguageSeparator;
		private System.Windows.Forms.ToolStripMenuItem showRecipeCategoryMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem copyMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteMenuItem;
		private System.Windows.Forms.ToolStripMenuItem tokenBudget5K;
		private System.Windows.Forms.ToolStripMenuItem tokenBudget10K;
		private System.Windows.Forms.ToolStripMenuItem embeddedAssetsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem enableLinkMenuItem;
		private System.Windows.Forms.ToolStripMenuItem importLinkedMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveNewLinkedMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveLinkedMenuItem;
		private System.Windows.Forms.ToolStripMenuItem reestablishLinkMenuItem;
		private System.Windows.Forms.ToolStripMenuItem breakLinkMenuItem;
		private System.Windows.Forms.ToolStripStatusLabel statusConnectionIcon;
		private System.Windows.Forms.ToolStripMenuItem revertLinkedMenuItem;
		private System.Windows.Forms.ToolStripMenuItem rearrangeLoreMenuItem;
		private System.Windows.Forms.ToolStripMenuItem chatHistoryMenuItem;
		private System.Windows.Forms.ToolStripMenuItem linkOptionsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem enableAutosaveMenuItem;
		private System.Windows.Forms.ToolStripMenuItem applyToFirstChatMenuItem;
		private System.Windows.Forms.ToolStripMenuItem applyToLastChatMenuItem;
		private System.Windows.Forms.ToolStripMenuItem applyToAllChatsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem alwaysLinkMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem11;
		private System.Windows.Forms.ToolStripMenuItem lightThemeMenuItem;
		private System.Windows.Forms.ToolStripMenuItem darkThemeMenuItem;
		private System.Windows.Forms.ToolStripMenuItem customVariablesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem usePortraitAsBackgroundMenuItem;
		private System.Windows.Forms.ToolStripMenuItem bulkOperationsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem bulkImportMenuItem;
		private System.Windows.Forms.ToolStripMenuItem bulkExportMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem12;
		private System.Windows.Forms.ToolStripMenuItem bulkEditModelSettingsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem editExportModelSettingsMenuItem;
		private System.Windows.Forms.ToolStripSeparator bulkSeparator;
	}
}

