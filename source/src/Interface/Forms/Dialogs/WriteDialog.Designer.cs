namespace Ginger
{
	partial class WriteDialog
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WriteDialog));
			this.btnOk = new System.Windows.Forms.Button();
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.fileMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.saveAndCloseMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.cancelAndCloseMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.copyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.cutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.selectAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.findMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.findNextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.findPreviousMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.replaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.replacePronounsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.autoReplacePronounMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoReplaceUserPronounMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.formatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.wordWrapMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoBreakMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.changeFontMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.highlightingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.enableHighlightingMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
			this.highlightNamesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.highlightPronounsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.spellCheckingMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.enableSpellCheckingMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
			this.fontDialog = new System.Windows.Forms.FontDialog();
			this.labelTokens = new System.Windows.Forms.Label();
			this.textBox = new Ginger.RichTextBoxEx();
			this.highlightNumbersMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			buttonLayout = new System.Windows.Forms.FlowLayoutPanel();
			buttonLayout.SuspendLayout();
			this.menuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonLayout
			// 
			buttonLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			buttonLayout.Controls.Add(this.btnOk);
			buttonLayout.Dock = System.Windows.Forms.DockStyle.Bottom;
			buttonLayout.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			buttonLayout.Location = new System.Drawing.Point(0, 446);
			buttonLayout.Margin = new System.Windows.Forms.Padding(0);
			buttonLayout.Name = "buttonLayout";
			buttonLayout.Padding = new System.Windows.Forms.Padding(0, 0, 9, 0);
			buttonLayout.Size = new System.Drawing.Size(713, 40);
			buttonLayout.TabIndex = 1;
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(603, 6);
			this.btnOk.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(98, 28);
			this.btnOk.TabIndex = 0;
			this.btnOk.Text = "Close";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
			// 
			// menuStrip
			// 
			this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenu,
            this.editMenu,
            this.formatToolStripMenuItem,
            this.viewMenu});
			this.menuStrip.Location = new System.Drawing.Point(0, 0);
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
			this.menuStrip.Size = new System.Drawing.Size(713, 24);
			this.menuStrip.TabIndex = 2;
			this.menuStrip.Text = "menuStrip";
			this.menuStrip.MenuActivate += new System.EventHandler(this.MenuStrip_MenuActivate);
			// 
			// fileMenu
			// 
			this.fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveAndCloseMenuItem,
            this.cancelAndCloseMenuItem});
			this.fileMenu.Name = "fileMenu";
			this.fileMenu.Size = new System.Drawing.Size(37, 20);
			this.fileMenu.Text = "&File";
			// 
			// saveAndCloseMenuItem
			// 
			this.saveAndCloseMenuItem.Name = "saveAndCloseMenuItem";
			this.saveAndCloseMenuItem.ShortcutKeyDisplayString = "Ctrl+Enter";
			this.saveAndCloseMenuItem.Size = new System.Drawing.Size(219, 22);
			this.saveAndCloseMenuItem.Text = "&Apply and close";
			this.saveAndCloseMenuItem.Click += new System.EventHandler(this.BtnOk_Click);
			// 
			// cancelAndCloseMenuItem
			// 
			this.cancelAndCloseMenuItem.Name = "cancelAndCloseMenuItem";
			this.cancelAndCloseMenuItem.ShortcutKeyDisplayString = "Escape";
			this.cancelAndCloseMenuItem.Size = new System.Drawing.Size(219, 22);
			this.cancelAndCloseMenuItem.Text = "&Cancel";
			this.cancelAndCloseMenuItem.Click += new System.EventHandler(this.BtnCancel_Click);
			// 
			// editMenu
			// 
			this.editMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyMenuItem,
            this.cutMenuItem,
            this.pasteMenuItem,
            this.selectAllMenuItem,
            this.toolStripMenuItem2,
            this.findMenuItem,
            this.findNextMenuItem,
            this.findPreviousMenuItem,
            this.replaceToolStripMenuItem,
            this.replacePronounsMenuItem,
            this.toolStripMenuItem1,
            this.autoReplacePronounMenuItem,
            this.autoReplaceUserPronounMenuItem});
			this.editMenu.Name = "editMenu";
			this.editMenu.Size = new System.Drawing.Size(39, 20);
			this.editMenu.Text = "&Edit";
			// 
			// copyMenuItem
			// 
			this.copyMenuItem.Name = "copyMenuItem";
			this.copyMenuItem.ShortcutKeyDisplayString = "Ctrl+C";
			this.copyMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.copyMenuItem.Size = new System.Drawing.Size(314, 22);
			this.copyMenuItem.Text = "&Copy";
			this.copyMenuItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
			// 
			// cutMenuItem
			// 
			this.cutMenuItem.Name = "cutMenuItem";
			this.cutMenuItem.ShortcutKeyDisplayString = "Ctrl+X";
			this.cutMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
			this.cutMenuItem.Size = new System.Drawing.Size(314, 22);
			this.cutMenuItem.Text = "C&ut";
			this.cutMenuItem.Click += new System.EventHandler(this.CutMenuItem_Click);
			// 
			// pasteMenuItem
			// 
			this.pasteMenuItem.Name = "pasteMenuItem";
			this.pasteMenuItem.ShortcutKeyDisplayString = "Ctrl+V";
			this.pasteMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.pasteMenuItem.Size = new System.Drawing.Size(314, 22);
			this.pasteMenuItem.Text = "&Paste";
			this.pasteMenuItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
			// 
			// selectAllMenuItem
			// 
			this.selectAllMenuItem.Name = "selectAllMenuItem";
			this.selectAllMenuItem.ShortcutKeyDisplayString = "Ctrl+A";
			this.selectAllMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.selectAllMenuItem.Size = new System.Drawing.Size(314, 22);
			this.selectAllMenuItem.Text = "Select &all";
			this.selectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(311, 6);
			// 
			// findMenuItem
			// 
			this.findMenuItem.Name = "findMenuItem";
			this.findMenuItem.ShortcutKeyDisplayString = "Ctrl+F";
			this.findMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
			this.findMenuItem.Size = new System.Drawing.Size(314, 22);
			this.findMenuItem.Text = "&Find...";
			this.findMenuItem.Click += new System.EventHandler(this.findMenuItem_Click);
			// 
			// findNextMenuItem
			// 
			this.findNextMenuItem.Name = "findNextMenuItem";
			this.findNextMenuItem.ShortcutKeyDisplayString = "F3";
			this.findNextMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F3;
			this.findNextMenuItem.Size = new System.Drawing.Size(314, 22);
			this.findNextMenuItem.Text = "Find &next";
			this.findNextMenuItem.Click += new System.EventHandler(this.findNextMenuItem_Click);
			// 
			// findPreviousMenuItem
			// 
			this.findPreviousMenuItem.Name = "findPreviousMenuItem";
			this.findPreviousMenuItem.ShortcutKeyDisplayString = "Shift+F3";
			this.findPreviousMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F3)));
			this.findPreviousMenuItem.Size = new System.Drawing.Size(314, 22);
			this.findPreviousMenuItem.Text = "Find pre&vious";
			this.findPreviousMenuItem.Click += new System.EventHandler(this.findPreviousMenuItem_Click);
			// 
			// replaceToolStripMenuItem
			// 
			this.replaceToolStripMenuItem.Name = "replaceToolStripMenuItem";
			this.replaceToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+F";
			this.replaceToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.F)));
			this.replaceToolStripMenuItem.Size = new System.Drawing.Size(314, 22);
			this.replaceToolStripMenuItem.Text = "Find and r&eplace...";
			this.replaceToolStripMenuItem.Click += new System.EventHandler(this.replaceMenuItem_Click);
			// 
			// replacePronounsMenuItem
			// 
			this.replacePronounsMenuItem.Name = "replacePronounsMenuItem";
			this.replacePronounsMenuItem.Size = new System.Drawing.Size(314, 22);
			this.replacePronounsMenuItem.Text = "Replace pronouns...";
			this.replacePronounsMenuItem.Click += new System.EventHandler(this.swapGenderToolStripMenuItem_Click);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(311, 6);
			// 
			// autoReplacePronounMenuItem
			// 
			this.autoReplacePronounMenuItem.Name = "autoReplacePronounMenuItem";
			this.autoReplacePronounMenuItem.ShortcutKeyDisplayString = "Ctrl+Space";
			this.autoReplacePronounMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Space)));
			this.autoReplacePronounMenuItem.Size = new System.Drawing.Size(314, 22);
			this.autoReplacePronounMenuItem.Text = "Auto-replace pronoun";
			this.autoReplacePronounMenuItem.Click += new System.EventHandler(this.autoReplacePronounMenuItem_Click);
			// 
			// autoReplaceUserPronounMenuItem
			// 
			this.autoReplaceUserPronounMenuItem.Name = "autoReplaceUserPronounMenuItem";
			this.autoReplaceUserPronounMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+Space";
			this.autoReplaceUserPronounMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.Space)));
			this.autoReplaceUserPronounMenuItem.Size = new System.Drawing.Size(314, 22);
			this.autoReplaceUserPronounMenuItem.Text = "Auto-replace user pronoun";
			this.autoReplaceUserPronounMenuItem.Click += new System.EventHandler(this.autoReplaceUserPronounMenuItem_Click);
			// 
			// formatToolStripMenuItem
			// 
			this.formatToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.wordWrapMenuItem,
            this.autoBreakMenuItem,
            this.changeFontMenuItem});
			this.formatToolStripMenuItem.Name = "formatToolStripMenuItem";
			this.formatToolStripMenuItem.Size = new System.Drawing.Size(57, 20);
			this.formatToolStripMenuItem.Text = "F&ormat";
			// 
			// wordWrapMenuItem
			// 
			this.wordWrapMenuItem.Checked = true;
			this.wordWrapMenuItem.CheckOnClick = true;
			this.wordWrapMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.wordWrapMenuItem.Name = "wordWrapMenuItem";
			this.wordWrapMenuItem.Size = new System.Drawing.Size(161, 22);
			this.wordWrapMenuItem.Text = "Word wrap";
			this.wordWrapMenuItem.CheckStateChanged += new System.EventHandler(this.WordWrapMenuItem_CheckedChanged);
			// 
			// autoBreakMenuItem
			// 
			this.autoBreakMenuItem.Checked = true;
			this.autoBreakMenuItem.CheckOnClick = true;
			this.autoBreakMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.autoBreakMenuItem.Name = "autoBreakMenuItem";
			this.autoBreakMenuItem.Size = new System.Drawing.Size(161, 22);
			this.autoBreakMenuItem.Text = "Limit line width";
			this.autoBreakMenuItem.CheckStateChanged += new System.EventHandler(this.AutoBreakLinesMenuItem_CheckStateChanged);
			// 
			// changeFontMenuItem
			// 
			this.changeFontMenuItem.Name = "changeFontMenuItem";
			this.changeFontMenuItem.Size = new System.Drawing.Size(161, 22);
			this.changeFontMenuItem.Text = "Font...";
			this.changeFontMenuItem.Click += new System.EventHandler(this.ChangeFontMenuItem_Click);
			// 
			// viewMenu
			// 
			this.viewMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.highlightingToolStripMenuItem,
            this.spellCheckingMenuItem});
			this.viewMenu.Name = "viewMenu";
			this.viewMenu.Size = new System.Drawing.Size(44, 20);
			this.viewMenu.Text = "&View";
			// 
			// highlightingToolStripMenuItem
			// 
			this.highlightingToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.enableHighlightingMenuItem,
            this.toolStripMenuItem5,
            this.highlightNamesMenuItem,
            this.highlightNumbersMenuItem,
            this.highlightPronounsMenuItem});
			this.highlightingToolStripMenuItem.Name = "highlightingToolStripMenuItem";
			this.highlightingToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.highlightingToolStripMenuItem.Text = "Highlighting";
			// 
			// enableHighlightingMenuItem
			// 
			this.enableHighlightingMenuItem.CheckOnClick = true;
			this.enableHighlightingMenuItem.Name = "enableHighlightingMenuItem";
			this.enableHighlightingMenuItem.Size = new System.Drawing.Size(180, 22);
			this.enableHighlightingMenuItem.Text = "Enabled";
			this.enableHighlightingMenuItem.CheckedChanged += new System.EventHandler(this.enableHighlightingMenuItem_CheckedChanged);
			// 
			// toolStripMenuItem5
			// 
			this.toolStripMenuItem5.Name = "toolStripMenuItem5";
			this.toolStripMenuItem5.Size = new System.Drawing.Size(177, 6);
			// 
			// highlightNamesMenuItem
			// 
			this.highlightNamesMenuItem.CheckOnClick = true;
			this.highlightNamesMenuItem.Name = "highlightNamesMenuItem";
			this.highlightNamesMenuItem.Size = new System.Drawing.Size(180, 22);
			this.highlightNamesMenuItem.Text = "Names";
			this.highlightNamesMenuItem.CheckedChanged += new System.EventHandler(this.highlightNamesMenuItem_CheckedChanged);
			// 
			// highlightPronounsMenuItem
			// 
			this.highlightPronounsMenuItem.CheckOnClick = true;
			this.highlightPronounsMenuItem.Name = "highlightPronounsMenuItem";
			this.highlightPronounsMenuItem.Size = new System.Drawing.Size(180, 22);
			this.highlightPronounsMenuItem.Text = "Pronouns";
			this.highlightPronounsMenuItem.CheckedChanged += new System.EventHandler(this.highlightPronounsMenuItem_CheckedChanged);
			// 
			// spellCheckingMenuItem
			// 
			this.spellCheckingMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.enableSpellCheckingMenuItem,
            this.toolStripMenuItem4});
			this.spellCheckingMenuItem.Name = "spellCheckingMenuItem";
			this.spellCheckingMenuItem.Size = new System.Drawing.Size(180, 22);
			this.spellCheckingMenuItem.Text = "Spell checking";
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
			// fontDialog
			// 
			this.fontDialog.FontMustExist = true;
			this.fontDialog.ShowApply = true;
			this.fontDialog.ShowEffects = false;
			// 
			// labelTokens
			// 
			this.labelTokens.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelTokens.AutoSize = true;
			this.labelTokens.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelTokens.Location = new System.Drawing.Point(5, 457);
			this.labelTokens.Name = "labelTokens";
			this.labelTokens.Size = new System.Drawing.Size(81, 17);
			this.labelTokens.TabIndex = 2;
			this.labelTokens.Text = "Token count:";
			// 
			// textBox
			// 
			this.textBox.BackColor = System.Drawing.SystemColors.Window;
			this.textBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBox.DetectUrls = false;
			this.textBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBox.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBox.Location = new System.Drawing.Point(0, 24);
			this.textBox.Name = "textBox";
			this.textBox.Placeholder = null;
			this.textBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
			this.textBox.Size = new System.Drawing.Size(713, 422);
			this.textBox.SpellChecking = true;
			this.textBox.syntaxFlags = Ginger.RichTextBoxEx.SyntaxFlags.None;
			this.textBox.SyntaxHighlighting = true;
			this.textBox.TabIndex = 0;
			this.textBox.Text = "";
			// 
			// highlightNumbersMenuItem
			// 
			this.highlightNumbersMenuItem.CheckOnClick = true;
			this.highlightNumbersMenuItem.Name = "highlightNumbersMenuItem";
			this.highlightNumbersMenuItem.Size = new System.Drawing.Size(180, 22);
			this.highlightNumbersMenuItem.Text = "Numbers";
			this.highlightNumbersMenuItem.CheckedChanged += new System.EventHandler(this.highlightNumbersMenuItem_CheckedChanged);
			// 
			// WriteDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(713, 486);
			this.Controls.Add(this.labelTokens);
			this.Controls.Add(this.textBox);
			this.Controls.Add(buttonLayout);
			this.Controls.Add(this.menuStrip);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(347, 380);
			this.Name = "WriteDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Edit text";
			this.ResizeEnd += new System.EventHandler(this.WriteDialog_ResizeEnd);
			this.Resize += new System.EventHandler(this.WriteDialog_Resize);
			buttonLayout.ResumeLayout(false);
			this.menuStrip.ResumeLayout(false);
			this.menuStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.MenuStrip menuStrip;
		private System.Windows.Forms.ToolStripMenuItem fileMenu;
		private System.Windows.Forms.ToolStripMenuItem editMenu;
		private System.Windows.Forms.ToolStripMenuItem copyMenuItem;
		private System.Windows.Forms.ToolStripMenuItem cutMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteMenuItem;
		private System.Windows.Forms.ToolStripMenuItem selectAllMenuItem;
		private System.Windows.Forms.ToolStripMenuItem viewMenu;
		private System.Windows.Forms.FontDialog fontDialog;
		private RichTextBoxEx textBox;
		private System.Windows.Forms.ToolStripMenuItem saveAndCloseMenuItem;
		private System.Windows.Forms.ToolStripMenuItem formatToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem wordWrapMenuItem;
		private System.Windows.Forms.ToolStripMenuItem changeFontMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autoBreakMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem replaceToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem replacePronounsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autoReplacePronounMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autoReplaceUserPronounMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem findMenuItem;
		private System.Windows.Forms.ToolStripMenuItem findNextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem findPreviousMenuItem;
		private System.Windows.Forms.ToolStripMenuItem spellCheckingMenuItem;
		private System.Windows.Forms.ToolStripMenuItem enableSpellCheckingMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
		private System.Windows.Forms.Label labelTokens;
		private System.Windows.Forms.ToolStripMenuItem cancelAndCloseMenuItem;
		private System.Windows.Forms.ToolStripMenuItem highlightingToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem enableHighlightingMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
		private System.Windows.Forms.ToolStripMenuItem highlightNamesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem highlightPronounsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem highlightNumbersMenuItem;
	}
}