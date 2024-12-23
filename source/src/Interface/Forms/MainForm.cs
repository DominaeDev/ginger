﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Ginger.Properties;
using Ginger.Integration;

using Backyard = Ginger.Integration.Backyard;

namespace Ginger
{
	public partial class MainForm : Form, IThemedControl
	{
		public static readonly string AppTitle = "Ginger";

		private TokenizerQueue tokenQueue = new TokenizerQueue();
		private int _inputHash = 0;

		private bool _bShouldRefreshSidePanel = false;
		private bool _bShouldRefreshTokenCount = false;
		private bool _bShouldRecreatePanels = false;
		private int _originalExStyle = -1;
		private static bool _bEnableFormLevelDoubleBuffering = true;

		private static bool _bCanRegenerate = true;
		private static bool _bCanIdle = true;
		private bool _bWasFileDirty = false;

		public static MainForm instance { get; private set; }

		private string _shouldLoadFilename = null;
		private FindDialog _findDialog;
		private LinkEditChatDialog _editChatDialog;

		private Dictionary<string, ToolStripMenuItem> _spellCheckLangMenuItems = new Dictionary<string, ToolStripMenuItem>();
		private Dictionary<string, ToolStripMenuItem> _changeLanguageMenuItems = new Dictionary<string, ToolStripMenuItem>();
		private List<IIdleHandler> _idleHandlers = new List<IIdleHandler>();

		private System.Timers.Timer _statusbarTimer = new System.Timers.Timer();

		public MainForm()
		{
			instance = this;
			InitializeComponent();

			Icon = Resources.icon;

			Application.Idle += OnIdle;
			Application.ApplicationExit += OnClose;
			DragEnter += OnDragEnter;
			DragDrop += OnDragDrop;
			FormClosing += OnClosing;

			Current.OnLoadCharacter += (s, e) => {
				OnLoadedFile();
			};
			tokenQueue.onTokenCount += TokenQueue_onTokenCount;
			sidePanel.EditName += OnRenamedCharacter;
			sidePanel.ChangedGender += OnChangedGender;
			recipeList.SaveAsSnippet += OnSaveAsSnippet;
			recipeList.SaveAsRecipe += OnSaveAsRecipe;
			recipeList.SaveLorebook += OnSaveLorebook;

			// Initialize
			Current.Reset();

			// Initialize spell checker
			if (AppSettings.Settings.SpellChecking)
			{
				if (SpellChecker.Initialize(AppSettings.Settings.Dictionary) == false)
					AppSettings.Settings.SpellChecking = false;
			}

			// Load recipes & lorebooks
			BlockStyles.LoadStyles();
			RecipeBook.LoadRecipes();
			Lorebooks.LoadLorebooks();

			sidePanel.SetLoreCount(0, false);
			sidePanel.RefreshValues();
			RefreshTitle();

			// Undo history
			Undo.Initialize();
			Undo.OnUndoRedo += OnUndoState;

			_statusbarTimer.Interval = 1000;
			_statusbarTimer.Elapsed += OnStatusBarTimerElapsed;
			_statusbarTimer.AutoReset = false;
			_statusbarTimer.SynchronizingObject = this;
		}

		public void SetFirstLoad(string filename)
		{
			_shouldLoadFilename = filename;
		}

		private void OnLoadedFile()
		{
			EnableFormLevelDoubleBuffering(true);

			tabControl.SelectedIndex = 0;

#if DEBUG
			Stopwatch stopWatch = new Stopwatch();
			stopWatch.Start();
#endif
			RefreshTitle();

			this.Suspend();
			sidePanel.Reset();
			sidePanel.SetLoreCount(0, false);
			sidePanel.RefreshValues();
			sidePanel.Refresh();
			recipeList.RemoveAllPanels();
			this.Resume();
			recipeList.Refresh();

			this.Suspend();
			recipeList.RecreatePanels(true);
			recipeList.RefreshScrollbar();

			RefreshSpellChecking();

			Regenerate();
			Current.IsFileDirty = false;

#if DEBUG
			stopWatch.Stop();
			Debug.WriteLine(string.Format("Total time: {0} ms", stopWatch.ElapsedMilliseconds));
#endif
			this.Resume();
			Undo.Clear();
			GC.Collect();
		}

		private void OnClosing(object sender, FormClosingEventArgs e)
		{
			StealFocus();
			if (ConfirmSave(Resources.cap_exit_app) == false)
				e.Cancel = true;
		}

		private void OnClose(object sender, EventArgs e)
		{
			FileMutex.Release();
			AppSettings.SaveToIni(Utility.AppPath("Settings.ini"));
		}

		/// Hack to reduce intolerable flickering in WinForms.
		/// We have to turn this on and off occasionally or else the RichTextBox scrollbar breaks.
		protected override CreateParams CreateParams
		{
			get
			{
				if (_originalExStyle == -1)
					_originalExStyle = base.CreateParams.ExStyle;

				CreateParams cp = base.CreateParams;
				if (_bEnableFormLevelDoubleBuffering && AppSettings.Settings.EnableFormLevelBuffering)
					cp.ExStyle |= 0x02000000;   // WS_EX_COMPOSITED
				else
					cp.ExStyle = _originalExStyle;

				return cp;
			}
		}

		public static void EnableFormLevelDoubleBuffering(bool bEnable)
		{
			if (AppSettings.Settings.EnableFormLevelBuffering == false)
				return;

			if (_bEnableFormLevelDoubleBuffering != bEnable)
			{
				_bEnableFormLevelDoubleBuffering = bEnable;
				instance.MaximizeBox = true;
#if DEBUG
				instance.RefreshTitle();
#endif
			}
		}

		private void OnIdle(object sender, EventArgs e)
		{
			if (_bCanIdle == false)
				return;

			foreach (var idleHandler in _idleHandlers)
				idleHandler.OnIdle();

			if (_bShouldRefreshSidePanel)
			{
				sidePanel.RefreshValues();
				_bShouldRefreshSidePanel = false;
				_bShouldRefreshTokenCount = false;
			}
			else if (_bShouldRefreshTokenCount)
			{
				sidePanel.RefreshTokenCount();
				_bShouldRefreshTokenCount = false;
			}

			if (Current.IsDirty && _bCanRegenerate)
			{
				Current.IsDirty = false;
				Regenerate();
			}
			if (_bWasFileDirty != Current.IsFileDirty)
			{
				_bWasFileDirty = Current.IsFileDirty;
				RefreshTitle();
			}
		}

		private void MainForm_OnLoad(object sender, EventArgs e)
		{
			sidePanel.ChangePortraitImage += OnChangePortraitImage;
			sidePanel.ResizePortraitImage += OnResizePortraitImage;
			sidePanel.PastePortraitImage += OnPastePortraitImage;
			sidePanel.RemovePortraitImage += OnRemovePortraitImage;

			SetToolTip(btnAdd_Model, "Bot instructions");
			SetToolTip(btnAdd_Character, "Character");
			SetToolTip(btnAdd_Traits, "Appearance & Traits");
			SetToolTip(btnAdd_Mind, "Personality & Behavior");
			SetToolTip(btnAdd_World, "Story & World");
			SetToolTip(btnAdd_Other, "Components");
			SetToolTip(btnAdd_Snippets, "Snippets");
			SetToolTip(btnAdd_Lore, "Lorebooks");

			rearrangeLoreMenuItem.ToolTipText = Resources.tooltip_rearrange_lore;
			enableLinkMenuItem.ToolTipText = Resources.tooltip_link_connect;
			enableAutosaveMenuItem.ToolTipText = Resources.tooltip_link_autosave;
			reestablishLinkMenuItem.ToolTipText = Resources.tooltip_link_reestablish;
			breakLinkMenuItem.ToolTipText = Resources.tooltip_link_break;
			importLinkedMenuItem.ToolTipText = Resources.tooltip_link_open;
			saveLinkedMenuItem.ToolTipText = Resources.tooltip_link_save;
			saveNewLinkedMenuItem.ToolTipText = Resources.tooltip_link_save_as_new;
			revertLinkedMenuItem.ToolTipText = Resources.tooltip_link_revert;
			applyToFirstChatMenuItem.ToolTipText = Resources.tooltip_link_apply_to_first;
			applyToLastChatMenuItem.ToolTipText = Resources.tooltip_link_apply_to_last;
			applyToAllChatsMenuItem.ToolTipText = Resources.tooltip_link_apply_to_all;
			alwaysLinkMenuItem.ToolTipText = Resources.tooltip_link_always_create;
			usePortraitAsBackgroundMenuItem.ToolTipText = Resources.tooltip_link_use_portrait_as_background;
			bulkExportMenuItem.ToolTipText = Resources.tooltip_link_export_many;
			bulkImportMenuItem.ToolTipText = Resources.tooltip_link_import_many;
			bulkEditModelSettingsMenuItem.ToolTipText = Resources.tooltip_link_model_settings_many;
			editExportModelSettingsMenuItem.ToolTipText = Resources.tooltip_link_model_settings_default;
			importAltGreetingsMenuItem.ToolTipText = Resources.tooltip_link_alt_greetings;

			createBackupMenuItem.ToolTipText = Resources.tooltip_link_create_backup;
			restoreBackupMenuItem.ToolTipText = Resources.tooltip_link_restore_backup;
			repairBrokenImagesMenuItem.ToolTipText = Resources.tooltip_link_repair_images;
			purgeUnusedImagesMenuItem.ToolTipText = Resources.tooltip_link_purge_images;

			RegisterIdleHandler(recipeList);

			outputBox.SetTabWidth(4);

#if DEBUG && false
			group_Debug.Visible = true;
#endif

			// Create dictionary menu items
			if (Dictionaries.IsOk())
			{
				foreach (var langKvp in Dictionaries.dictionaries)
				{
					var menuItem = new ToolStripMenuItem(langKvp.Value);
					menuItem.Click += delegate {
						ChangeSpellingLanguage(langKvp.Key);
					};
					checkSpellingMenuItem.DropDownItems.Add(menuItem);
					_spellCheckLangMenuItems.Add(langKvp.Key, menuItem);
				}
				checkSpellingMenuItem.DropDownItems.Add(new ToolStripSeparator());
				checkSpellingMenuItem.DropDownItems.Add(new ToolStripMenuItem("Add dictionary...", null, (s, x) => {
					Utility.OpenUrl(Constants.DictionariesURL);
				}));
			}
			else
			{
				checkSpellingMenuItem.Enabled = false;
				AppSettings.Settings.SpellChecking = false;
			}

			// Create language menu items
			foreach (var langKvp in Locales.AppLocales)
			{
				var menuItem = new ToolStripMenuItem(langKvp.Value);
				menuItem.Click += delegate {
					ChangeAppLanguage(langKvp.Key);
				};
				changeLanguageMenuItem.DropDownItems.Add(menuItem);
				_changeLanguageMenuItems.Add(langKvp.Key, menuItem);
			}
			if (changeLanguageMenuItem.DropDownItems.Count <= 1)
			{
				changeLanguageMenuItem.Enabled = false;
				changeLanguageMenuItem.DropDownItems.Clear();
//				changeLanguageSeparator.Visible = false;
			}

			ApplyVisualTheme();

			if (_shouldLoadFilename != null) // Command-line argument
			{
				if (FileMutex.CanAcquire(_shouldLoadFilename) == false)
				{
					MessageBox.Show(string.Format(Resources.error_already_open, Path.GetFileName(_shouldLoadFilename)), Resources.cap_load_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				OpenFile(_shouldLoadFilename);
				_shouldLoadFilename = null;
			}
		}

		private void OnDragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (files.Length == 1 && FileUtil.CheckFileType(files[0]) != FileUtil.FileType.Unknown)
				{
					e.Effect = DragDropEffects.Copy;
					return;
				}
			}
			e.Effect = DragDropEffects.None;
		}

		private void OnDragDrop(object sender, DragEventArgs e)
		{
			Activate();

			if (e.Data.GetDataPresent(DataFormats.FileDrop) == false)
				return;

			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (files.Length != 1)
				return; // Error

			string filename = files[0];

			var fileType = FileUtil.CheckFileType(filename);
			if (fileType == FileUtil.FileType.Unknown)
				return;

			// Dropped lorebook?
			if (fileType.Contains(FileUtil.FileType.Lorebook))
			{
				Lorebook lorebook = null;
				if (fileType.Contains(FileUtil.FileType.Json))
				{
					lorebook = new Lorebook();
					int nErrors;
					var loadError = lorebook.LoadFromJson(filename, out nErrors);
					switch (loadError)
					{
					case Lorebook.LoadError.NoError:
						if (nErrors > 0)
							MessageBox.Show(string.Format(Resources.msg_import_lorebook_with_errors, nErrors), Resources.cap_import_lorebook, MessageBoxButtons.OK, MessageBoxIcon.Warning);
						break;
					case Lorebook.LoadError.UnknownFormat:
						MessageBox.Show(Resources.error_unrecognized_lorebook_format, Resources.cap_import_lorebook, MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					case Lorebook.LoadError.NoData:
						MessageBox.Show(Resources.error_no_lorebook, Resources.cap_import_lorebook, MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					case Lorebook.LoadError.FileError:
						MessageBox.Show(Resources.error_load_lorebook, Resources.cap_import_lorebook, MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					case Lorebook.LoadError.InvalidJson:
						MessageBox.Show(Resources.error_invalid_json_file, Resources.cap_import_lorebook, MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
				}
				else if (fileType.Contains(FileUtil.FileType.Csv))
				{
					return; // No drag & drop for csv files
				}

				if (lorebook != null)
				{
					Cursor = Cursors.WaitCursor;
					SetStatusBarMessage(Resources.status_refreshing_list);

					// Add to recipe list
					var instance = Current.AddLorebook(lorebook);
					recipeList.Suspend();
					recipeList.DisableRedrawAndDo(() => {
						var pastedPanel = recipeList.AddRecipePanel(instance, false);
						pastedPanel.Focus();
						recipeList.ScrollToPanel(pastedPanel);
					});
					recipeList.Resume();
					recipeList.Invalidate(true);

					Undo.Push(Undo.Kind.RecipeAddRemove, "Add lorebook");

					Cursor = Cursors.Default;
					ClearStatusBarMessage();
				}
				else
				{
					MessageBox.Show(Resources.error_unrecognized_lorebook_format, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				return;
			}

			// Dropped character file
			if (ConfirmSave(Resources.cap_open_character_card) == false)
				return;

			if (FileMutex.CanAcquire(openFileDialog.FileName) == false)
			{
				MessageBox.Show(string.Format(Resources.error_already_open, Path.GetFileName(openFileDialog.FileName)), Resources.cap_load_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			OpenFile(filename);
		}

		private void OnChangePortraitImage(object sender, PortraitPreview.ChangePortraitImageEventArgs e)
		{
			string filename = e.Filename;
			if (filename == null)
			{
				// Open file...
				openFileDialog.Title = Resources.cap_open_image;
				openFileDialog.Filter = "Image files|*.png;*.jpeg;*.jpg;*.webp";
				openFileDialog.InitialDirectory = AppSettings.Paths.LastImagePath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
				var result = openFileDialog.ShowDialog();
				if (result != DialogResult.OK)
					return;

				AppSettings.Paths.LastImagePath = Path.GetDirectoryName(openFileDialog.FileName);

				filename = openFileDialog.FileName;
			}

			Image portraitImage;
			if (Utility.LoadImageFromFile(filename, out portraitImage) == false)
			{
				MessageBox.Show(Resources.error_load_image, Resources.cap_open_image, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}

			// Is animated image?
			var ext = Utility.GetFileExt(filename);
			bool bAnimated;
			if (ext == "apng" || ext == "png")
				bAnimated = Utility.IsAnimatedPNG(filename);
			else if (ext == "webp")
				bAnimated = Utility.IsAnimatedWebP(filename);
			else if (ext == "gif")
				bAnimated = Utility.IsAnimatedImage(portraitImage);
			else
				bAnimated = false;
						
			if (portraitImage.Width > Constants.MaxImageDimension || portraitImage.Height > Constants.MaxImageDimension)
			{
				int srcWidth = portraitImage.Width;
				int srcHeight = portraitImage.Height;
				float scale = Math.Min((float)Constants.MaxImageDimension / srcWidth, (float)Constants.MaxImageDimension / srcHeight);
				int newWidth = Math.Max((int)Math.Round(srcWidth * scale), 1);
				int newHeight = Math.Max((int)Math.Round(srcHeight * scale), 1);

				if (MessageBox.Show(string.Format(Resources.msg_rescale_portrait, portraitImage.Width, portraitImage.Height, newWidth, newHeight), Resources.cap_change_portrait, MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
				{
					var bmpNewImage = new Bitmap(newWidth, newHeight);
					using (Graphics gfxNewImage = Graphics.FromImage(bmpNewImage))
					{
						gfxNewImage.DrawImage(portraitImage,
							new Rectangle(0, 0, newWidth, newHeight),
								0, 0, srcWidth, srcHeight,
								GraphicsUnit.Pixel);
					}
					portraitImage = Image.FromHbitmap(bmpNewImage.GetHbitmap());
				}
			}

			Current.Card.portraitImage = ImageRef.FromImage(portraitImage);
			Current.IsDirty = true;

			if (bAnimated)
				Current.Card.ReplaceMainPortraitAsset(filename);
			else
				Current.Card.RemoveMainPortraitAsset();

			sidePanel.RefreshValues();
			Undo.Push(Undo.Kind.Parameter, "Change portrait image");
		}

		private void OnPastePortraitImage(object sender, EventArgs e)
		{
			Image image;
			try
			{
				image = Clipboard.GetImage();
			}
			catch
			{
				return; // Error
			}

			Current.Card.portraitImage = ImageRef.FromImage(image);
			Current.IsDirty = true;
			sidePanel.RefreshValues();

			Undo.Push(Undo.Kind.Parameter, "Change portrait image");
		}

		private void OnRemovePortraitImage(object sender, EventArgs e)
		{
			Current.Card.portraitImage = null;
			Current.IsDirty = true;
			sidePanel.RefreshValues();
			Undo.Push(Undo.Kind.Parameter, "Clear portrait");
		}

		private void OnResizePortraitImage(object sender, EventArgs e)
		{
			Image image = Current.Card.portraitImage;
			if (image == null || (image.Width <= Constants.MaxImageDimension && image.Height <= Constants.MaxImageDimension))
				return; // No change

			int srcWidth = image.Width;
			int srcHeight = image.Height;
			float scale = Math.Min((float)Constants.MaxImageDimension / srcWidth, (float)Constants.MaxImageDimension / srcHeight);
			int newWidth = Math.Max((int)Math.Round(srcWidth * scale), 1);
			int newHeight = Math.Max((int)Math.Round(srcHeight * scale), 1);

			Image bmpNewImage = new Bitmap(newWidth, newHeight);
			
			using (Graphics gfxNewImage = Graphics.FromImage(bmpNewImage))
			{
				gfxNewImage.DrawImage(image,
					new Rectangle(0, 0, newWidth, newHeight),
						0, 0, srcWidth, srcHeight,
						GraphicsUnit.Pixel);
			}

			Current.Card.portraitImage = ImageRef.FromImage(bmpNewImage);
			Current.IsDirty = true;
			sidePanel.RefreshValues();

			Undo.Push(Undo.Kind.Parameter, "Resize portrait image");
			SetStatusBarMessage("Resized portrait image", Constants.StatusBarMessageInterval);
		}		

		private void OnExitApplication(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private struct RecipeMenuItem
		{
			public RecipeMenuItem(ToolStripMenuItem menuItem, int order = 100)
			{
				this.menuItem = menuItem;
				this.order = order;
			}

			public string label { get { return menuItem.Text; } }

			public ToolStripMenuItem menuItem;
			public int order;
		}

		public void PopulateRecipeMenu(Recipe.Drawer drawer, ToolStripItemCollection items, Context context, string root = "")
		{
			var folders = RecipeBook.GetFolders(root, drawer);
			var recipesByUID = RecipeBook.GetRecipes(root, drawer);

			var lsFolders = new List<RecipeMenuItem>();
			var lsRecipes = new List<RecipeMenuItem>();

			if (drawer == Recipe.Drawer.Components)
			{
				PopulateComponentMenu("", items, context);

				items.Add(new ToolStripSeparator());
				var otherFolder = new ToolStripMenuItem("Other");
				PopulateComponentMenu("Other", otherFolder.DropDownItems, context);
				items.Add(otherFolder);
				return;
			}

			if (drawer == Recipe.Drawer.Snippets && string.IsNullOrEmpty(root))
			{
				var newSnippetItem = new ToolStripMenuItem("New snippet...");
				newSnippetItem.Click += (s, e) => {
					CreateSnippetMenuItem_Click(s, e);
				};
				items.Add(newSnippetItem);
				if (folders.Length > 0 || recipesByUID.Length > 0)
					items.Add(new ToolStripSeparator());
			}

			// Folders
			foreach (var folder in folders)
			{
				var menuItem = new ToolStripMenuItem();
				menuItem.Image = Theme.Current.MenuFolder;
				menuItem.Text = Utility.EscapeMenu(folder);

				if (string.IsNullOrEmpty(root))
					PopulateRecipeMenu(drawer, menuItem.DropDownItems, context, folder);
				else
					PopulateRecipeMenu(drawer, menuItem.DropDownItems, context, string.Concat(root, "/", folder));

				lsFolders.Add(new RecipeMenuItem(menuItem));
			}

			foreach (var recipeUID in recipesByUID)
			{
				var recipeTemplate = RecipeBook.GetRecipeByUID(recipeUID);
				if (recipeTemplate == null)
					continue;

				var menuItem = new ToolStripMenuItem();
				menuItem.Text = recipeTemplate.label;
				string tooltip = recipeTemplate.tooltip;
				if (recipeTemplate.includes > 0 || recipeTemplate.hasDetached)
				{
					menuItem.Text += "\u2026";
					var sbTooltip = new StringBuilder(tooltip);
					sbTooltip.NewParagraph();
					sbTooltip.AppendFormat(Resources.tooltip_no_includes);
					tooltip = sbTooltip.ToString();
				}
				menuItem.ToolTipText = tooltip;

				if (recipeTemplate.type == Recipe.Type.Snippet)
					menuItem.Image = Theme.Current.MenuSnippet;
				if (Current.Character.recipes.ContainsAny(r => r.uid == recipeTemplate.uid) && recipeTemplate.allowMultiple == false)
				{
					menuItem.Checked = true;
				}
				else if (recipeTemplate.requires != null
					&& recipeTemplate.requires.Evaluate(context, new EvaluationCookie() { ruleSuppliers = Current.RuleSuppliers }) == false)
				{
					menuItem.Enabled = false;
					menuItem.ToolTipText = string.Concat(tooltip.TrimEnd(), "\r\n\r\n(", Resources.tooltip_cant_add, ")");
				}
				else
				{
					menuItem.Click += (s, e) => {
						AddRecipe(recipeTemplate);
					};
				}

				lsRecipes.Add(new RecipeMenuItem(menuItem, recipeTemplate.order ?? 100));
			}

			lsRecipes = lsRecipes.OrderBy(r => r.order).ToList();

			// Lorebooks
			if (drawer == Recipe.Drawer.Lore && string.IsNullOrEmpty(root))
			{
				var newLorebookItem = new ToolStripMenuItem("New lorebook");
				newLorebookItem.Click += (s, e) => {
					var book = new Lorebook();
					book.entries.Add(new Lorebook.Entry());
					recipeList.AddLorebook(book);
				};
				items.Add(newLorebookItem);

				var importItem = new ToolStripMenuItem("Load from file...");
				importItem.Click += (s, e) => {
					ImportLorebook(false);
				};
				items.Add(importItem);
				if (Lorebooks.books.Count > 0)
				{
					items.Add(new ToolStripSeparator());

					foreach (var book in Lorebooks.books.OrderBy(b => b.name))
					{
						var menuItem = new ToolStripMenuItem();
						menuItem.Text = Utility.EscapeMenu(book.name);
						menuItem.Image = Theme.Current.MenuLore;
						menuItem.Click += (s, e) => {
							recipeList.AddLorebook(book);
						};
						lsRecipes.Add(new RecipeMenuItem(menuItem));
					}
				}
			}

			// Add category items
			items.AddRange(lsFolders.Select(c => c.menuItem).ToArray());
			if (folders.Length > 0 && recipesByUID.Length > 0)
				items.Add("-");

			// Split into groups?
			if (lsRecipes.Count >= Constants.Drawer.SplitMenuAfter)
			{
				int numPartitions = lsRecipes.Count / Constants.Drawer.RecipesPerSplit;
				if (lsRecipes.Count % Constants.Drawer.RecipesPerSplit != 0)
					numPartitions++;

				int perPartition = Constants.Drawer.RecipesPerSplit;

				var partitions = new List<List<RecipeMenuItem>>();
				int n = 0;
				for (int i = 0; i < numPartitions && n < lsRecipes.Count; ++i)
				{
					partitions.Add(new List<RecipeMenuItem>());
					for (int j = 0; j < perPartition && n < lsRecipes.Count; ++j)
						partitions[i].Add(lsRecipes[n++]);
				}

				for (int i = 0; i < partitions.Count; ++i)
				{
					var partition = partitions[i];
					if (partition.Count == 0)
						continue;

					char letterA = partition[0].label[0];
					char letterZ = partition[partition.Count - 1].label[0];
					bool isAlpha = (letterA >= 'A' && letterA <= 'z') && (letterZ >= 'A' && letterZ <= 'z');

					if (i == 0 && isAlpha)
						letterA = 'A';
					else if (i == partitions.Count - 1 && isAlpha)
						letterZ = 'Z';

					var menuItem = new ToolStripMenuItem(string.Format("{0} - {1}", char.ToUpper(letterA), char.ToUpper(letterZ)));
				
					var recipeGroups = partition
						.GroupBy(r => r.order / 10)
						.Select(g => new {
							group = g.Key,
							entries = new List<RecipeMenuItem>(g),
						})
						.ToList();

					for (int j = 0; j < recipeGroups.Count; ++j)
					{
						if (j > 0)
							menuItem.DropDownItems.Add(new ToolStripSeparator());
						menuItem.DropDownItems.AddRange(recipeGroups[j].entries.Select(r => r.menuItem).ToArray());
					}
					items.Add(menuItem);
				}
			}
			else
			{
				var recipeGroups = lsRecipes
					.GroupBy(r => r.order / 10)
					.Select(g => new {
						group = g.Key,
						entries = new List<RecipeMenuItem>(g),
					})
					.ToList();
				for (int i = 0; i < recipeGroups.Count; ++i)
				{
					if (i > 0)
						items.Add(new ToolStripSeparator());
					items.AddRange(recipeGroups[i].entries.Select(r => r.menuItem).ToArray());
				}
			}
		}

		public void PopulateComponentMenu(string root, ToolStripItemCollection items, Context context)
		{
			var recipesByUID = RecipeBook.GetRecipes(root, Recipe.Drawer.Components);
			var recipes = recipesByUID.Select(uid => RecipeBook.GetRecipeByUID(uid))
				.NotNull()
				.OrderBy(r => r.order);

			bool bSeparator = false;
			foreach (var recipeTemplate in recipes)
			{
				var menuItem = new ToolStripMenuItem();
				menuItem.Text = recipeTemplate.label;
				menuItem.ToolTipText = recipeTemplate.tooltip;
				if (recipeTemplate.requires != null
					&& recipeTemplate.requires.Evaluate(context, new EvaluationCookie() { ruleSuppliers = Current.RuleSuppliers }) == false)
				{
					menuItem.Enabled = false;
					menuItem.ToolTipText = menuItem.ToolTipText = string.Concat(recipeTemplate.tooltip.TrimEnd(), "\r\n\r\n(", Resources.tooltip_cant_add, ")");
				}
				menuItem.Click += (s, e) => {
					AddRecipe(recipeTemplate);
				};

				if (recipeTemplate.order >= 10 && bSeparator == false)
				{
					items.Add("-");
					bSeparator = true;
				}

				items.Add(menuItem);
			}
		}

		private void AddRecipe(RecipeTemplate recipeTemplate)
		{
			StealFocus();

			bool bIncludes = ModifierKeys != Keys.Shift;

			if (bIncludes && recipeTemplate.includes > 0 && Current.Character.recipes.Count >= 2)
			{
				var mr = MessageBox.Show(string.Format(recipeTemplate.includes == 1 ? Resources.msg_include_recipe : Resources.msg_include_recipes, recipeTemplate.includes), Resources.cap_add_recipe, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
				if (mr == DialogResult.No)
					bIncludes = false;
				else if (mr == DialogResult.Cancel)
					return;
			}

			var instances = Current.Character.AddRecipe(recipeTemplate, bIncludes);
			
			recipeList.AddRecipePanels(instances, true);

			Undo.Push(Undo.Kind.RecipeAddRemove, "Add recipe");
		}

		private void NewFromPreset(RecipePreset preset)
		{
			if (ConfirmSave(Resources.cap_new_file))
			{
				FileMutex.Release();

				Current.NewCharacter();
				Current.Card.name = preset.cardName;
				Current.MainCharacter.spokenName = preset.characterName;
				Current.MainCharacter.gender = preset.characterGender;

				var instances = Current.MainCharacter.AddRecipePreset(preset);
				recipeList.AddRecipePanels(instances, false);
				recipeList.ScrollToTop();

				RefreshTitle();
				sidePanel.RefreshValues();
				Current.IsDirty = false;
				Current.IsFileDirty = false;
				userNotes.Clear();
				Undo.Clear();
				Regenerate();
			}
		}

		private void BtnAddModel_MouseClick(object sender, MouseEventArgs e)
		{
			ShowRecipeMenu(Recipe.Drawer.Model, btnAdd_Model, new Point(e.X, e.Y));
		}

		private void BtnAdd_Character_MouseClick(object sender, MouseEventArgs e)
		{
			ShowRecipeMenu(Recipe.Drawer.Character, btnAdd_Character, new Point(e.X, e.Y));
		}

		private void btnAdd_Trait_Click(object sender, MouseEventArgs e)
		{
			ShowRecipeMenu(Recipe.Drawer.Appearance, btnAdd_Traits, new Point(e.X, e.Y));
		}

		private void BtnAdd_Mind_MouseClick(object sender, MouseEventArgs e)
		{
			ShowRecipeMenu(Recipe.Drawer.Mind, btnAdd_Mind, new Point(e.X, e.Y));
		}

		private void BtnAdd_Scenario_MouseClick(object sender, MouseEventArgs e)
		{
			ShowRecipeMenu(Recipe.Drawer.Story, btnAdd_World, new Point(e.X, e.Y));
		}

		private void BtnAdd_Lore_MouseClick(object sender, MouseEventArgs e)
		{
			ShowRecipeMenu(Recipe.Drawer.Lore, btnAdd_Lore, new Point(e.X, e.Y));
		}

		private void BtnAdd_Snippets_MouseClick(object sender, MouseEventArgs e)
		{
			ShowRecipeMenu(Recipe.Drawer.Snippets, btnAdd_Snippets, new Point(e.X, e.Y));
		}
		
		private void BtnAdd_Other_MouseClick(object sender, MouseEventArgs e)
		{
			ShowRecipeMenu(Recipe.Drawer.Components, btnAdd_Other, new Point(e.X, e.Y));
		}

		private void ShowRecipeMenu(Recipe.Drawer drawer, Control control, Point point)
		{
			Context context = Current.Character.GetContext(CharacterData.ContextType.FlagsOnly, true);

			ContextMenuStrip menu = new ContextMenuStrip();

			PopulateRecipeMenu(drawer, menu.Items, context);

			if (menu.Items.Count == 0)
				menu.Items.Add("(Empty)").Enabled = false;

			point.Offset(-16, 16);
			
			Theme.Apply(menu);
			menu.Show(control, point);
			
			StealFocus();
		}

		private void BtnBakeAll_Click(object sender, EventArgs e)
		{
			if (Current.AllRecipes.IsEmpty())
				return;

			if (BakeAll() == false)
				MessageBox.Show(Resources.error_bake_no_result, Resources.cap_bake_no_result, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnBakeActor_Click(object sender, EventArgs e)
		{
			if (Current.Character.recipes.IsEmpty())
				return;

			if (BakeActor() == false)
				MessageBox.Show(Resources.error_bake_no_result, Resources.cap_bake_no_result, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Save(Current.Filename);
		}

		private void SaveasToolStripMenuItem_Click(object sender, EventArgs args)
		{
			SaveAs();
		}

		private void SaveIncrementalMenuItem_Click(object sender, EventArgs e)
		{
			SaveIncremental();
		}

		private void OpenFileMenuItem_Click(object sender, EventArgs e)
		{
			// Open file...
			openFileDialog.Title = Resources.cap_open_character_card;
			openFileDialog.Filter = "Supported card types|*.png;*.json;*.charx|PNG files|*.png|JSON files|*.json|CHARX files|*.charx";
			openFileDialog.InitialDirectory = AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			var result = openFileDialog.ShowDialog();
			if (result != DialogResult.OK)
				return;

			AppSettings.Paths.LastCharacterPath = Path.GetDirectoryName(openFileDialog.FileName);

			if (ConfirmSave(Resources.cap_open_character_card) == false)
				return;

			if (FileMutex.CanAcquire(openFileDialog.FileName) == false)
			{
				MessageBox.Show(string.Format(Resources.error_already_open, Path.GetFileName(openFileDialog.FileName)), Resources.cap_load_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			OpenFile(openFileDialog.FileName);
		}

		public bool OpenFile(string filename)
		{
			if (File.Exists(filename) == false)
			{
				MessageBox.Show(Resources.error_file_not_found, Resources.cap_open_character_card, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			SetStatusBarMessage(Resources.status_open_character); 

			string ext = Path.GetExtension(filename).ToLowerInvariant();
			if (ext == ".json" || ext == ".yaml" || ext == ".charx")
			{
				if (ImportCharacter(filename) == false)
				{
					MessageBox.Show(Resources.error_open_character_card, Resources.cap_import_character, MessageBoxButtons.OK, MessageBoxIcon.Error);
					ClearStatusBarMessage();
					return false;
				}
			}
			else if (ext == ".png")
			{
				int errors;
				var loadError = FileUtil.ImportCharacterFromPNG(filename, out errors);
				if (loadError == FileUtil.Error.NoDataFound)
				{
					MessageBox.Show(string.Format(Resources.error_no_data, Path.GetFileName(filename)), Resources.cap_open_character_card, MessageBoxButtons.OK, MessageBoxIcon.Error);
					ClearStatusBarMessage();
					return false;
				}
				else if (loadError == FileUtil.Error.FileReadError || loadError == FileUtil.Error.FileNotFound || loadError == FileUtil.Error.InvalidData)
				{
					MessageBox.Show(Resources.error_open_character_card, Resources.cap_open_character_card, MessageBoxButtons.OK, MessageBoxIcon.Error);
					ClearStatusBarMessage();
					return false;
				}
				else if (loadError == FileUtil.Error.FallbackError)
				{
					MessageBox.Show(Resources.error_fallback, Resources.cap_open_character_card, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				else if (errors > 0)
				{
					MessageBox.Show(string.Format(Resources.msg_load_with_errors, errors), Resources.cap_open_character_card, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}

				if (FileMutex.Acquire(filename) == false)
				{
					MessageBox.Show(string.Format(Resources.error_already_open, Path.GetFileName(filename)), Resources.cap_open_character_card, MessageBoxButtons.OK, MessageBoxIcon.Error);
					ClearStatusBarMessage();
					return false;
				}

				Current.Filename = filename;

				MRUList.AddToMRU(filename, Current.Card.name);
			}
			else
			{
				MessageBox.Show(string.Format(Resources.error_open_wrong_file_type, Path.GetFileName(filename)), Resources.cap_open_character_card, MessageBoxButtons.OK, MessageBoxIcon.Error);
				ClearStatusBarMessage();
				return false;
			}

			Cursor = Cursors.WaitCursor;

			SetStatusBarMessage(Resources.status_refreshing_list);

			sidePanel.Enabled = true;
			userNotes.Clear();
			LoadNotes(filename);

			Current.IsLoading = true;
			Current.IsDirty = false;
			Current.IsFileDirty = false;
			if (Current.HasLink)
				Current.Link.RefreshState();

			Current.OnLoadCharacter?.Invoke(null, EventArgs.Empty);


			Current.IsLoading = false;
			Cursor = Cursors.Default;
			ClearStatusBarMessage();
			return true;
		}

		private void NewToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (ConfirmSave(Resources.cap_new_file))
			{
				FileMutex.Release();
				userNotes.Clear();
				Current.NewCharacter();
			}
		}

		private void ReloadRecipesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			BlockStyles.LoadStyles();
			RecipeBook.LoadRecipes();
			Lorebooks.LoadLorebooks();
			bool hasOutdatedRecipes = Current.AllRecipes.ContainsAny(r => {
				var localRecipe = RecipeBook.GetRecipeByID(r.id);
				return localRecipe != null && r.uid != localRecipe.uid && r.version >= localRecipe.version;
			});

			if (ModifierKeys == Keys.Shift || (hasOutdatedRecipes && MessageBox.Show(Resources.msg_reload_recipes, Resources.cap_reload_recipes, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes))
			{
				this.Suspend();
				Current.ReloadRecipes(true);
				RefreshRecipeList();
				Undo.Push(Undo.Kind.RecipeList, "Reload recipes");
				this.Resume();
			}
#if DEBUG
			else
				Regenerate(); // For testing
#endif
			SetStatusBarMessage(Resources.status_reload_recipes, Constants.StatusBarMessageInterval);
		}

		private void OnRenamedCharacter(object sender, SidePanel.EditNameEventArgs e)
		{
			if (e.OldName != e.NewName && AppSettings.Settings.AutoConvertNames)
			{
				ReplaceNamePlaceholders(e.OldName, e.NewName);

				recipeList.RefreshAllParameters();
				recipeList.RefreshParameterVisibility();
				recipeList.RefreshSyntaxHighlighting(true);
				RefreshSpellChecking();
			}

			RefreshTitle();
		}

		private void OnChangedGender(object sender, EventArgs e)
		{
			Undo.Push(Undo.Kind.Parameter, "Change gender", "change-gender");
			recipeList.RefreshParameterVisibility();
		}

		public void RefreshTitle()
		{
			// Character name
			string title = Utility.FirstNonEmpty(Current.Card.name, Current.MainCharacter.spokenName) ?? "";
			if (title.Length > 0)
				title = string.Concat(title, " ");

			// Filename
			if (string.IsNullOrEmpty(Current.Filename) == false)
				title = string.Concat(title, "[", Path.GetFileName(Current.Filename), "] ");

			// App title
			if (title.Length > 0)
				title = string.Concat(title, "- ", AppTitle);
			else
				title = AppTitle;

			// Is dirty?
			if (Current.IsFileDirty || (Current.IsLinkDirty && Backyard.ConnectionEstablished))
				title = string.Concat("*", title);

			this.Text = title;

			_bWasFileDirty = Current.IsFileDirty;

			// Status bar
			if (Current.Characters.Count > 1)
				statusBarActor.Text = string.Format("Actor {0} / {1}", Current.SelectedCharacter + 1, Current.Characters.Count);
			else
				statusBarActor.Text = string.Empty;

#if DEBUG && false // Show form level buffering
			statusBarActor.Text = statusBarActor.Text + (_bEnableFormLevelDoubleBuffering && AppSettings.Settings.EnableFormLevelBuffering ? " ON" : " OFF");
#endif

			// Show Backyard menu
			backyardMenuItem.Visible = Backyard.ConnectionEstablished;

			// Connection status icon
			if (Backyard.ConnectionEstablished)
			{
				if (Current.HasActiveLink)
				{
					statusConnectionIcon.Image = Theme.Current.LinkActive;
					statusConnectionIcon.ToolTipText = "Connected; Linked";
				}
				else if (Current.HasLink)
				{
					if (Backyard.HasCharacter(Current.Link.characterId))
					{
						statusConnectionIcon.Image = Theme.Current.LinkInactive;
						statusConnectionIcon.ToolTipText = "Connected; Link broken";
					}
					else
					{
						statusConnectionIcon.Image = Theme.Current.LinkBroken;
						statusConnectionIcon.ToolTipText = "Connected; Link broken";
					}
				}
				else
				{
					statusConnectionIcon.Image = Theme.Current.LinkConnected;
					statusConnectionIcon.ToolTipText = "Connected; Not linked";
				}
			}
			else
			{
				if (Current.HasLink)
				{
					statusConnectionIcon.Image = Theme.Current.LinkDisconnected;
					statusConnectionIcon.ToolTipText = "Not connected";
				}
				else
				{
					statusConnectionIcon.Image = null;
					statusConnectionIcon.ToolTipText = null;
				}
			}

			// Menu items
			RefreshMenuItems();
		}

		private void PopulatePresetMenu(ToolStripItemCollection items, string root = "")
		{
			var folders = RecipeBook.GetPresetFolders(root);
			var presets = RecipeBook.GetPresets(root);

			// Recipes
			foreach (var presetName in presets)
			{
				var preset = RecipeBook.GetPresetByName(root, presetName);
				if (preset != null)
				{
					var item = new ToolStripMenuItem(preset.name);
					item.Click += (s, e) => {
						NewFromPreset(preset);
					};
					items.Add(item);
				}
			}

			// Separator
			if (folders.Length > 0 && presets.Length > 0)
				items.Add("-");

			// Categories
			foreach (var folder in folders)
			{
				var item = new ToolStripMenuItem(folder);
				items.Add(item);

				if (string.IsNullOrEmpty(root))
					PopulatePresetMenu(item.DropDownItems, folder);
				else
					PopulatePresetMenu(item.DropDownItems, string.Concat(root, "/", folder));
			}
		}

		private void MainMenuActivate(object sender, EventArgs e)
		{
			// Steal focus from text boxes
			StealFocus();

			// New from template
			newFromTemplateMenuItem.DropDownItems.Clear();
			PopulatePresetMenu(newFromTemplateMenuItem.DropDownItems);
			newFromTemplateMenuItem.Enabled = newFromTemplateMenuItem.DropDownItems.Count > 0;

			// Languages
			foreach (var kvp in _changeLanguageMenuItems)
				kvp.Value.Checked = string.Compare(AppSettings.Settings.Locale, kvp.Key, StringComparison.OrdinalIgnoreCase) == 0;

			// Save incremental
			saveIncrementalMenuItem.Enabled = string.IsNullOrEmpty(Current.Filename) == false;

			// MRU
			PopulateMRUMenu(openRecentMenuItem.DropDownItems);
			openRecentMenuItem.Enabled = openRecentMenuItem.DropDownItems.Count > 0;

			// Undo / Redo
			RefreshMenuItems();

			// Supporting characters
			additionalCharactersMenuItem.DropDownItems.Clear();
			PopulateSupportingCharactersMenu(additionalCharactersMenuItem.DropDownItems);

			// Token budget
			tokenBudgetNone.Checked = AppSettings.Settings.TokenBudget == 0;
			tokenBudget1K.Checked = AppSettings.Settings.TokenBudget == 1024;
			tokenBudget2K.Checked = AppSettings.Settings.TokenBudget == 2048;
			tokenBudget3K.Checked = AppSettings.Settings.TokenBudget == 3072;
			tokenBudget4K.Checked = AppSettings.Settings.TokenBudget == 4096;
			tokenBudget5K.Checked = AppSettings.Settings.TokenBudget == 5120;
			tokenBudget6K.Checked = AppSettings.Settings.TokenBudget == 6144;
			tokenBudget8K.Checked = AppSettings.Settings.TokenBudget == 8192;
			tokenBudget10K.Checked = AppSettings.Settings.TokenBudget == 10240;
			tokenBudget12K.Checked = AppSettings.Settings.TokenBudget == 12288;
			tokenBudget16K.Checked = AppSettings.Settings.TokenBudget == 16384;
			tokenBudget24K.Checked = AppSettings.Settings.TokenBudget == 24576;
			tokenBudget32K.Checked = AppSettings.Settings.TokenBudget == 32768;

			// Settings
			autoConvertNameMenuItem.Checked = AppSettings.Settings.AutoConvertNames;
			showNSFWRecipesMenuItem.Checked = AppSettings.Settings.AllowNSFW;
			autoBreakMenuItem.Checked = AppSettings.Settings.AutoBreakLine;
			enableSpellCheckingMenuItem.Checked = AppSettings.Settings.SpellChecking;
			rearrangeLoreMenuItem.Checked = AppSettings.Settings.EnableRearrangeLoreMode;
			lightThemeMenuItem.Checked = !AppSettings.Settings.DarkTheme;
			darkThemeMenuItem.Checked = AppSettings.Settings.DarkTheme;

			// Spell checking
			foreach (var kvp in _spellCheckLangMenuItems)
			{
				kvp.Value.Checked = string.Compare(AppSettings.Settings.Dictionary, kvp.Key, StringComparison.OrdinalIgnoreCase) == 0;
				kvp.Value.Enabled = AppSettings.Settings.SpellChecking;
			}

			// Tools
			mergeLoreMenuItem.Enabled = Current.Character.recipes.Count(r => r.isLorebook) > 1;

			copyMenuItem.Enabled = !Current.Character.recipes.IsEmpty();
			if (Clipboard.ContainsData(RecipeClipboard.Format))
			{
				pasteMenuItem.Text = "Paste recipe(s)";
				pasteMenuItem.Enabled = true;
			}
			else if (Clipboard.ContainsData(LoreClipboard.Format))
			{
				pasteMenuItem.Text = "Paste lorebook";
				pasteMenuItem.Enabled = true;
			}
			else if (Clipboard.ContainsData(ChatClipboard.Format))
			{
				pasteMenuItem.Text = "Paste chat";
				pasteMenuItem.Enabled = true;
			} 
			else if (Clipboard.ContainsData(ChatStagingClipboard.Format))
			{
				pasteMenuItem.Text = "Paste chat settings";
				pasteMenuItem.Enabled = true;
			} 
			else if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
			{
				pasteMenuItem.Text = "Paste text";
				pasteMenuItem.Enabled = true;
			}
			else
			{
				pasteMenuItem.Text = "Paste";
				pasteMenuItem.Enabled = false;
			}

			// Link menu
			enableLinkMenuItem.Text = Backyard.ConnectionEstablished ? "Connected to Backyard AI" : "Connect to Backyard AI";
			enableLinkMenuItem.Checked = Backyard.ConnectionEstablished;
			backyardMenuItem.Visible = Backyard.ConnectionEstablished;

			importLinkedMenuItem.Enabled = Backyard.ConnectionEstablished;
			saveLinkedMenuItem.Enabled = Backyard.ConnectionEstablished && Current.HasActiveLink;
			saveNewLinkedMenuItem.Enabled = Backyard.ConnectionEstablished && Current.HasActiveLink == false;
			revertLinkedMenuItem.Enabled = Backyard.ConnectionEstablished && Current.HasActiveLink;

			breakRestoreLinkSeparator.Visible = Backyard.ConnectionEstablished;
			breakLinkMenuItem.Enabled = Backyard.ConnectionEstablished && Current.HasActiveLink;
			breakLinkMenuItem.Visible = Backyard.ConnectionEstablished && Current.HasStaleLink == false;
			reestablishLinkMenuItem.Enabled = Backyard.ConnectionEstablished && Current.HasStaleLink;
			reestablishLinkMenuItem.Visible = Backyard.ConnectionEstablished && Current.HasStaleLink;
			editCurrentModelSettingsMenuItem.Enabled = Current.HasActiveLink;
						
			// Link options
			applyToFirstChatMenuItem.Checked = AppSettings.BackyardLink.ApplyChatSettings == AppSettings.BackyardLink.ActiveChatSetting.First;
			applyToLastChatMenuItem.Checked = AppSettings.BackyardLink.ApplyChatSettings == AppSettings.BackyardLink.ActiveChatSetting.Last;
			applyToAllChatsMenuItem.Checked = AppSettings.BackyardLink.ApplyChatSettings == AppSettings.BackyardLink.ActiveChatSetting.All;
			alwaysLinkMenuItem.Checked = AppSettings.BackyardLink.AlwaysLinkOnImport;
			enableAutosaveMenuItem.Checked = AppSettings.BackyardLink.Autosave;
			usePortraitAsBackgroundMenuItem.Checked = AppSettings.BackyardLink.UsePortraitAsBackground;
			importAltGreetingsMenuItem.Checked = AppSettings.BackyardLink.ImportAlternateGreetings;


			Theme.Apply(menuStrip);
		}

		private void PopulateMRUMenu(ToolStripItemCollection items)
		{
			openRecentMenuItem.DropDownItems.Clear();
			foreach (var entry in MRUList.mruItems.Reverse())
			{
				var menuItem = new ToolStripMenuItem(Utility.EscapeMenu(string.Format("{0} [{1}]", entry.characterName, Path.GetFileName(entry.filename))));
				menuItem.ToolTipText = entry.filename;
				menuItem.Click += (s, e) => {
					OpenFromMRU(entry.filename);
				};
				items.Add(menuItem);
			}
			if (MRUList.mruItems.Count > 0)
			{
				items.Add("-");

				var menuItem = new ToolStripMenuItem("Clear list");
				menuItem.ToolTipText = Resources.tooltip_clear_mru;
				menuItem.Click += (s, e) => {
					MRUList.Clear();
				};
				items.Add(menuItem);
			}
		}

		private void OpenFromMRU(string filename)
		{
			if (ConfirmSave(Resources.cap_open_character_card) == false)
				return;

			if (File.Exists(filename) == false)
			{
				MessageBox.Show(Resources.error_file_not_found, Resources.cap_load_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				MRUList.RemoveFromMRU(filename);
				return;
			}

			OpenFile(filename);
		}

		private void PopulateSupportingCharactersMenu(ToolStripItemCollection items)
		{
			// Primary character
			var primary = new ToolStripMenuItem() {
				Text = Current.MainCharacter.namePlaceholder,
				Checked = Current.SelectedCharacter == 0,
				ShortcutKeyDisplayString = "Alt+1",
			};
			primary.Click += (s, e) => {
				SelectCharacter(0);
			};
			items.Add(primary);

			// Secondary characters
			for (int i = 1; i < Current.Characters.Count; ++i)
			{
				string subName = Utility.FirstNonEmpty(Current.Characters[i].spokenName, Constants.DefaultCharacterName);

				var menuItem = new ToolStripMenuItem() {
					Text = subName,
					Checked = Current.SelectedCharacter == i,
				};
				int index = i;
				menuItem.Click += (s, e) => {
					SelectCharacter(index);
				};
				if (i < 9)
					menuItem.ShortcutKeyDisplayString = string.Format("Alt+{0}", i + 1);
				else if (i == 9)
					menuItem.ShortcutKeyDisplayString = "Alt+0";
				items.Add(menuItem);
			}

			// Add new
			items.Add("-");
			var addActor = new ToolStripMenuItem("Add actor");
			addActor.Click += AddSupportingCharacterMenuItem_Click;
			items.Add(addActor);
			var removeActor = new ToolStripMenuItem(string.Format("Remove {0}", string.IsNullOrEmpty(Current.Character.spokenName) ? "actor" : Current.Character.spokenName));
			removeActor.Enabled = Current.SelectedCharacter > 0;
			removeActor.Click += RemoveSupportingCharacterMenuItem_Click;
			items.Add(removeActor);
		}

		private void ExpandAllMenuItem_Click(object sender, EventArgs e)
		{
			if (!panelRecipe.Visible)
				return;

			recipeList.ExpandAll();
		}

		private void CollapseAllMenuItem_Click(object sender, EventArgs e)
		{
			if (!panelRecipe.Visible)
				return;

			recipeList.CollapseAll();
		}

		private void CreateRecipeMenuItem_Click(object sender, EventArgs e)
		{
			var context = Current.Character.GetContext(CharacterData.ContextType.None);
			var output = Generator.Generate(RecipeBook.WithInternal(Current.Character.recipes), Current.SelectedCharacter, context, Generator.Option.Snippet);

			using (var dlg = new CreateRecipeDialog())
			{
				if (dlg.ShowDialog() == DialogResult.OK)
					RecipeMaker.CreateRecipe(dlg.FileName, dlg.RecipeName, dlg.RecipeTitle, dlg.Category, dlg.RecipeXml, output, null);
			}
		}

		private void ImportCharacterMenuItem_Click(object sender, EventArgs e)
		{
			if (ImportCharacterJson())
			{
				Current.IsDirty = false;
				Current.IsFileDirty = false;
				Current.OnLoadCharacter?.Invoke(this, EventArgs.Empty);
			}
		}

		private void ExportCharacterMenuItem_Click(object sender, EventArgs e)
		{
			ExportCharacter();
		}
		
		private void UndoMenuItem_Click(object sender, EventArgs e)
		{
			StealFocus();
			Undo.DoUndo();
		}

		private void RedoMenuItem_Click(object sender, EventArgs e)
		{
			StealFocus();
			Undo.DoRedo();
		}

		private void OnUndoState(object sender, Undo.UndoRedoEventArgs e)
		{
			bool bOk = true;
			if (tabControl.SelectedIndex == 0)
			{
				if (e.kind == Undo.Kind.Parameter)
					bOk = recipeList.RefreshReferences();
				else if (e.kind == Undo.Kind.RecipeAddRemove)
					bOk = recipeList.RefreshReferencesDelta();
				else if (e.kind == Undo.Kind.RecipeOrder)
					bOk = recipeList.RefreshReferencesUnordered();
				else // Full refresh
					RefreshRecipeList();
			}
			else
				_bShouldRecreatePanels = true; // Full refresh later

			if (!bOk || (!_bShouldRecreatePanels && !recipeList.ValidateState()))
			{
#if DEBUG
				throw new Exception("Invalid state");
#else
				// Full refreash
				RefreshRecipeList();
#endif
			}

			sidePanel.RefreshValues();

			RefreshSpellChecking(false);
			recipeList.RefreshSyntaxHighlighting(false);
			Regenerate();
			RefreshTitle();

			Current.IsFileDirty = true;
			Refresh(); // Immediate repaint (Provides feedback between each undo if holding down Ctrl+Z)
		}

		private void RefreshMenuItems()
		{
			// Expand / Contract menu items
			bool bViewingRecipe = tabControl.SelectedIndex == 0;
			bool bHasRecipes = recipeList.Controls.Count > 0;
			expandAllMenuItem.Enabled = bViewingRecipe && bHasRecipes;
			collapseAllMenuItem.Enabled = bViewingRecipe && bHasRecipes;

			// Find
			bool bCanFind = Current.Character.recipes.Count > 0;
			bool bCanFindNext = bCanFind && string.IsNullOrEmpty(AppSettings.User.FindMatch) == false;
			findMenuItem.Enabled = bCanFind;
			findNextMenuItem.Enabled = bCanFindNext;
			findPreviousMenuItem.Enabled = bCanFindNext;
			findAndReplaceMenuItem.Enabled = bCanFind;
			swapGenderMenuItem.Enabled = bCanFind;

			// View menu
			viewRecipeMenuItem.Checked = tabControl.SelectedIndex == 0;
			viewOutputMenuItem.Checked = tabControl.SelectedIndex == 1;
			viewNotesMenuItem.Checked = tabControl.SelectedIndex == 2;

			expandAllMenuItem.Enabled = bViewingRecipe && bHasRecipes;
			collapseAllMenuItem.Enabled = bViewingRecipe && bHasRecipes;

			outputPreviewDefaultMenuItem.Checked = AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Default;
			outputPreviewSillyTavernMenuItem.Checked = AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.SillyTavern;
			outputPreviewFaradayMenuItem.Checked = AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday;
			outputPreviewPlainTextMenuItem.Checked = AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.PlainText;

			// Tools
			bakeActorMenuItem.Visible = Current.Characters.Count > 1;

			// Undo / Redo
			var undo = Undo.PeekUndo();
			if (undo.IsEmpty())
				undoMenuItem.Text = "Undo";
			else
				undoMenuItem.Text = string.Format("Undo {0}", undo.actionName ?? "");

			var redo = Undo.PeekRedo();
			if (redo.IsEmpty())
				redoMenuItem.Text = "Redo";
			else
				redoMenuItem.Text = string.Format("Redo {0}", redo.actionName ?? "");
			undoMenuItem.Enabled = Undo.canUndo;
			redoMenuItem.Enabled = Undo.canRedo;

			// View categories
			showRecipeCategoryMenuItem.Checked = AppSettings.Settings.ShowRecipeCategory;
			showRecipeCategoryMenuItem.Enabled = bViewingRecipe && bHasRecipes;
			sortRecipesMenuItem.Enabled = bViewingRecipe && bHasRecipes;
		}

		private void ImportLorebookJsonMenuItem_Click(object sender, EventArgs e)
		{
			ImportLorebook(true);
		}

		public static void StealFocus()
		{
			// Steal away focus from the active control.
			// This is done to allow for double buffering to be re-enabled, which significantly
			// reduces flickering. Double buffering breaks RichTextBox in WinForms, so
			// we have to play this stupid game of turning buffering on and off based
			// on the currently focused control.
			instance.ActiveControl = null;
			instance.buttonRow.Focus();

			// Ensure double buffering is enabled
			EnableFormLevelDoubleBuffering(true);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == (Keys.Control | Keys.Alt | Keys.Z))
			{
				UndoMenuItem_Click(this, EventArgs.Empty);
				return true;
			}
			else if (keyData == (Keys.Control | Keys.Alt | Keys.Y))
			{
				RedoMenuItem_Click(this, EventArgs.Empty);
				return true;
			}
			if (keyData == ShortcutKeys.Cancel)
			{
				StealFocus();
				return true;
			}
			else if (keyData == ShortcutKeys.NextActor)
			{
				SelectCharacter(Current.SelectedCharacter + 1);
				return true;
			}
			else if (keyData == ShortcutKeys.PreviousActor)
			{
				SelectCharacter(Current.SelectedCharacter - 1);
				return true;
			}
			else if (keyData == (Keys.Shift | Keys.F5))
			{
				ReloadRecipesToolStripMenuItem_Click(this, EventArgs.Empty);
				return true;
			}
			else if (keyData == ShortcutKeys.SwitchView)
			{
				tabControl.SelectedIndex = tabControl.SelectedIndex == 1 ? 0 : 1;
				return true;
			}		
			else if (keyData == (ShortcutKeys.SwitchView | Keys.Shift))
			{
				tabControl.SelectedIndex = tabControl.SelectedIndex == 2 ? 0 : 2;
				return true;
			}
			else if (keyData == (Keys.Control | Keys.Home) && tabControl.SelectedIndex == 0 && !FocusedOnTextBox())
			{
				recipeList.ScrollToTop();
				return true;
			}
			else if (keyData == (Keys.Control | Keys.End) && tabControl.SelectedIndex == 0 && !FocusedOnTextBox())
			{
				recipeList.ScrollToBottom();
				return true;
			}
			else if (keyData == (Keys.Control | Keys.Up) && tabControl.SelectedIndex == 0 && !FocusedOnTextBox())
			{
				recipeList.ScrollUp(false);
				return true;
			}
			else if (keyData == (Keys.Control | Keys.Down) && tabControl.SelectedIndex == 0 && !FocusedOnTextBox())
			{
				recipeList.ScrollDown(false);
				return true;
			}		
			else if (keyData == (Keys.PageUp) && tabControl.SelectedIndex == 0 && !FocusedOnTextBox())
			{
				recipeList.ScrollUp(true);
				return true;
			}
			else if (keyData == (Keys.PageDown) && tabControl.SelectedIndex == 0 && !FocusedOnTextBox())
			{
				recipeList.ScrollDown(true);
				return true;
			}
			else if (keyData == ShortcutKeys.SaveIncremental)
			{
				SaveIncremental();
				return true;
			}
			else if (keyData == ShortcutKeys.LinkedOpen)
			{
				if (Backyard.ConnectionEstablished)
					importLinkedMenuItem_Click(this, EventArgs.Empty);
				else
					MessageBox.Show(Resources.error_link_not_connected, Resources.cap_import_character, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return true;
			}
			else if (keyData == ShortcutKeys.LinkedSave && Backyard.ConnectionEstablished && Current.HasActiveLink)
			{
				if (Backyard.ConnectionEstablished)
					saveLinkedMenuItem_Click(this, EventArgs.Empty);
				return true;
			}
			else if (keyData == ShortcutKeys.LinkedSaveAsNew && Current.HasActiveLink == false)
			{
				if (Backyard.ConnectionEstablished)
					saveNewLinkedMenuItem_Click(this, EventArgs.Empty);
				else
					MessageBox.Show(Resources.error_link_not_connected, Resources.cap_link_save_character, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return true;
			}
			else if (keyData == ShortcutKeys.LinkedChatHistory && Backyard.ConnectionEstablished)
			{
				OpenChatHistory();
				return true;
			}

			else if (keyData == (Keys.Alt | Keys.D1))
			{
				SelectCharacter(0);
				return true;
			}
			else if (keyData == (Keys.Alt | Keys.D2))
			{
				SelectCharacter(1);
				return true;
			}
			else if (keyData == (Keys.Alt | Keys.D3))
			{
				SelectCharacter(2);
				return true;
			}
			else if (keyData == (Keys.Alt | Keys.D4))
			{
				SelectCharacter(3);
				return true;
			}
			else if (keyData == (Keys.Alt | Keys.D5))
			{
				SelectCharacter(4);
				return true;
			}
			else if (keyData == (Keys.Alt | Keys.D6))
			{
				SelectCharacter(5);
				return true;
			}
			else if (keyData == (Keys.Alt | Keys.D7))
			{
				SelectCharacter(6);
				return true;
			}
			else if (keyData == (Keys.Alt | Keys.D8))
			{
				SelectCharacter(7);
				return true;
			}
			else if (keyData == (Keys.Alt | Keys.D9))
			{
				SelectCharacter(8);
				return true;
			}
			else if (keyData == (Keys.Alt | Keys.D0))
			{
				SelectCharacter(9);
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void NewWindowMenuItem_Click(object sender, EventArgs e)
		{
			OpenFileInNewWindow("");
		}

		private void AddSupportingCharacterMenuItem_Click(object sender, EventArgs e)
		{
			if (Current.Characters.Count >= 32) // This number is arbitrary. I just wanted an upper bound.
			{
				MessageBox.Show(Resources.error_max_characters, Resources.cap_save_snippet, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}

			Current.AddCharacter();
			tabControl.SelectedIndex = 0;
			recipeList.RecreatePanels();
			sidePanel.RefreshValues();
			sidePanel.OnActorChanged();
			RefreshTitle();
			sidePanel.SetSpokenName(Constants.DefaultCharacterName);

			Undo.Push(Undo.Kind.RecipeList, "Add actor");
		}

		private void RemoveSupportingCharacterMenuItem_Click(object sender, EventArgs e)
		{
			if (Current.SelectedCharacter <= 0 || Current.SelectedCharacter >= Current.Characters.Count)
				return;

			Current.Characters.RemoveAt(Current.SelectedCharacter);
			Current.SelectedCharacter = Math.Min(Math.Max(Current.SelectedCharacter, 0), Current.Characters.Count - 1);
			Current.IsDirty = true;

			tabControl.SelectedIndex = 0;
			recipeList.RecreatePanels();
			sidePanel.RefreshValues();
			sidePanel.OnActorChanged();
			RefreshTitle();
			StealFocus();

			Undo.Push(Undo.Kind.RecipeList, "Remove actor");
		}

		private void SelectCharacter(int characterIndex)
		{
			if (characterIndex < 0 || characterIndex >= Current.Characters.Count)
				return;

			if (Current.SelectedCharacter == characterIndex)
				return;

			Current.SelectedCharacter = characterIndex;
			tabControl.SelectedIndex = 0;
			recipeList.RecreatePanels();
			sidePanel.RefreshValues();
			sidePanel.OnActorChanged();
			RefreshTitle();

			Undo.Push(Undo.Kind.RecipeList, "Select actor", "select-actor");
		}

		private void CreateSnippetMenuItem_Click(object sender, EventArgs e)
		{
			var context = Current.Character.GetContext(CharacterData.ContextType.None);
			var output = Generator.Generate(RecipeBook.WithInternal(Current.Character.recipes), Current.SelectedCharacter, context, Generator.Option.Snippet);

			using (var dlg = new CreateSnippetDialog())
			{
				dlg.SetOutput(output);
				if (dlg.ShowDialog() == DialogResult.OK)
					RecipeMaker.CreateSnippet(dlg.FileName, dlg.SnippetName, dlg.Output);
			}
		}

		private void ExportLorebookMenuItem_Click(object sender, EventArgs e)
		{
			var output = Generator.Generate();
			ExportLorebook(output, false); 
			Lorebooks.LoadLorebooks();
		}

		private void TokenBudgetMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
			if (menuItem == null || menuItem.Checked == false)
				return;

			if (menuItem.Text == "None")
			{
				AppSettings.Settings.TokenBudget = 0;
			}
			else
			{
				int budget;
				if (int.TryParse(menuItem.Text, out budget))
				{
					AppSettings.Settings.TokenBudget = budget;
				}
			}

			sidePanel.RefreshValues();
		}

		private void swapGenderMenuItem_Click(object sender, EventArgs e)
		{
			using (var dlg = new GenderSwapDialog())
			{
				if (dlg.ShowDialog() == DialogResult.OK && dlg.Valid)
				{
					int replacements = GenderSwap.SwapGenders(Current.Character.recipes, dlg.CharacterFrom, dlg.CharacterTo, dlg.UserFrom, dlg.UserTo, dlg.SwapCharacter, dlg.SwapUser);

					if (replacements > 0)
					{
						Undo.Push(Undo.Kind.Parameter, "Replace pronouns");
						recipeList.RefreshAllParameters();
						recipeList.RefreshSyntaxHighlighting(false);
						RefreshSpellChecking();
					}
					if (replacements == 1)
						MessageBox.Show(string.Format(Resources.msg_replace_single, replacements), Resources.cap_swap_pronouns, MessageBoxButtons.OK, MessageBoxIcon.Information);
					else
						MessageBox.Show(string.Format(Resources.msg_replace_plural, replacements), Resources.cap_swap_pronouns, MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
		}

		private void replaceMenuItem_Click(object sender, EventArgs e)
		{
			using (var dlg = new FindReplaceDialog())
			{
				dlg.context = FindReplaceDialog.Context.Main;

				var focused = GetFocusedControl();
				if (focused is TextBoxBase)
				{
					var textBox = focused as TextBoxBase;
					if (textBox.SelectionLength > 0)
						dlg.Match = textBox.SelectedText;
				}

				if (dlg.ShowDialog() == DialogResult.OK)
				{
					int replacements = FindReplace.Replace(Current.Character.recipes.Where(r => r.isEnabled), dlg.Match, dlg.Replace, dlg.MatchWholeWord, dlg.IgnoreCase, dlg.IncludeLorebooks);

					if (replacements > 0)
					{
						Undo.Push(Undo.Kind.Parameter, "Replace text");
						recipeList.RefreshAllParameters();
						RefreshSpellChecking(false);
						recipeList.RefreshSyntaxHighlighting(true);
					}

					if (replacements == 1)
						MessageBox.Show(string.Format(Resources.msg_replace_single, replacements), Resources.cap_replace, MessageBoxButtons.OK, MessageBoxIcon.Information);
					else
						MessageBox.Show(string.Format(Resources.msg_replace_plural, replacements), Resources.cap_replace, MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
		}

		private void AutoconvertCharacterMarkersMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			bool bEnabled = autoConvertNameMenuItem.Checked;
			AppSettings.Settings.AutoConvertNames = bEnabled;
			Current.IsDirty = true;

			if (bEnabled == false && Current.AllRecipes.IsEmpty() == false)
			{
				var mr = MessageBox.Show("Replace names with placeholders?", Resources.cap_confirm, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
				if (mr == DialogResult.Yes)
					ConvertCharacterNameMarkers(bEnabled);
			}
			else
				ConvertCharacterNameMarkers(bEnabled);
			
			RefreshSpellChecking(false);
			recipeList.RefreshSyntaxHighlighting(true);

			Undo.Push(Undo.Kind.Parameter, "Change auto-convert option");
		}

		private void MainForm_ResizeBegin(object sender, EventArgs e)
		{
			// Turn off layout during resizing
			splitContainer.SuspendLayout();
			EnableFormLevelDoubleBuffering(false);
		}

		private void MainForm_ResizeEnd(object sender, EventArgs e)
		{
			// Turn it back on again once the resizing is over
			EnableFormLevelDoubleBuffering(true);
			splitContainer.ResumeLayout();
		}

		private void viewHelpToolStripMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				var argument = Utility.AppPath("Docs\\index.html");
				if (File.Exists(argument) == false) // Can't find docs
				{
					MessageBox.Show(Resources.error_file_not_found, Resources.cap_load_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				argument = argument.Replace("/", "\\");
				if (argument.Contains(" "))
					argument = string.Concat("\"", argument, "\"");

				var processInfo = new ProcessStartInfo() {
					FileName = "explorer",
					Arguments = argument,
					UseShellExecute = true,

				};
				Process.Start(processInfo);
			}
			catch
			{
				MessageBox.Show(Resources.error_help, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		}

		private void aboutGingerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (var dlg = new AboutBox())
			{
				dlg.ShowDialog();
			}
		}

		private void showNSFWRecipesMenuItem_Click(object sender, EventArgs e)
		{
			if (showNSFWRecipesMenuItem.Checked 
				&& AppSettings.Settings.ConfirmNSFW
				&& MessageBox.Show(Resources.msg_confirm_nsfw, Resources.cap_confirm, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
			{
				showNSFWRecipesMenuItem.Checked = false;
				return;
			}

			AppSettings.Settings.AllowNSFW = showNSFWRecipesMenuItem.Checked;
			AppSettings.Settings.ConfirmNSFW = false;
			recipeList.RefreshParameterVisibility();
			Current.IsDirty = true;
		}

		private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (tabControl.SelectedIndex == 1) // Output
			{
				StealFocus();
				Regenerate();
			}
			else if (tabControl.SelectedIndex == 2) // Notes
				userNotes.Focus();
			else
				StealFocus();

			if (tabControl.SelectedIndex == 0 && _bShouldRecreatePanels)
			{
				_bShouldRecreatePanels = false;
				recipeList.RecreatePanels();
			}

			RefreshMenuItems();
		}

		public bool FocusedOnTextBox(bool isMultiline = false)
		{
			var focused = GetFocusedControl();
			if (focused is TextBoxBase)
				return !isMultiline || (focused as TextBoxBase).Multiline;
			return false;
		}

		public Control GetFocusedControl()
		{
			return FindFocusedControl(this.ActiveControl);
		}

		private static Control FindFocusedControl(Control control)
		{
			var container = control as IContainerControl;
			while (container != null)
			{
				control = container.ActiveControl;
				container = control as IContainerControl;
			}
			return control;
		}

		private void RefreshRecipeList()
		{
			if (tabControl.SelectedIndex == 0)
				recipeList.RecreatePanels();
			else
				_bShouldRecreatePanels = true;
		}

		private void visitGitHubPageMenuItem_Click(object sender, EventArgs e)
		{
			Utility.OpenUrl(Constants.GitHubURL);
		}

		public static void SuspendGeneration()
		{
			_bCanRegenerate = false;
		}

		public static void ResumeGeneration()
		{
			_bCanRegenerate = true;
		}

		private void findMenuItem_Click(object sender, EventArgs e)
		{
			if (_findDialog != null && !_findDialog.IsDisposed)
				_findDialog.Close(); // Close existing

			_findDialog = new FindDialog();
			_findDialog.Find += OnFind;

			var focused = GetFocusedControl();
			if (focused is TextBoxBase)
			{
				var textBox = focused as TextBoxBase;
				if (textBox.SelectionLength > 0)
					_findDialog.Match = textBox.SelectedText;
			}

			_findDialog.Show(this);
		}

		private void findNextMenuItem_Click(object sender, EventArgs e)
		{
			OnFind(this, new FindDialog.FindEventArgs() {
				match = AppSettings.User.FindMatch ?? "",
				matchCase = AppSettings.User.FindMatchCase,
				wholeWord = AppSettings.User.FindWholeWords,
				reverse = false,
			});
		}

		private void findPreviousMenuItem_Click(object sender, EventArgs e)
		{
			OnFind(this, new FindDialog.FindEventArgs() {
				match = AppSettings.User.FindMatch ?? "",
				matchCase = AppSettings.User.FindMatchCase,
				wholeWord = AppSettings.User.FindWholeWords,
				reverse = true,
			});
		}

		private void outputPreviewDefaultMenuItem_Click(object sender, EventArgs e)
		{
			AppSettings.Settings.PreviewFormat = AppSettings.Settings.OutputPreviewFormat.Default;
			Regenerate();
			_bShouldRefreshTokenCount = true;
		}

		private void outputPreviewSillyTavernMenuItem_Click(object sender, EventArgs e)
		{
			AppSettings.Settings.PreviewFormat = AppSettings.Settings.OutputPreviewFormat.SillyTavern;
			Regenerate();
			_bShouldRefreshTokenCount = true;
		}

		private void outputPreviewFaradayMenuItem_Click(object sender, EventArgs e)
		{
			AppSettings.Settings.PreviewFormat = AppSettings.Settings.OutputPreviewFormat.Faraday;
			Regenerate();
			_bShouldRefreshTokenCount = true;
		}

		private void outputPreviewPlainTextMenuItem_Click(object sender, EventArgs e)
		{
			AppSettings.Settings.PreviewFormat = AppSettings.Settings.OutputPreviewFormat.PlainText;
			Regenerate();
		}


		private void ViewRecipeMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			tabControl.SelectedIndex = 0;
		}

		private void ViewOutputMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			tabControl.SelectedIndex = 1;
		}

		private void ViewNotesMenuItem_Click(object sender, EventArgs e)
		{
			tabControl.SelectedIndex = 2;
		}

		private void MergeLoreMenuItem_Click(object sender, EventArgs e)
		{
			MergeLore();
		}

		private void EnableSpellCheckingMenuItem_Click(object sender, EventArgs e)
		{
			EnableSpellChecking(!enableSpellCheckingMenuItem.Checked);
		}

		private void AutoBreakMenuItem_Click(object sender, EventArgs e)
		{
			EnableAutoWrap(!AppSettings.Settings.AutoBreakLine);
		}

		private void sortRecipesMenuItem_Click(object sender, EventArgs e)
		{
			recipeList.Sort();
			Current.IsDirty = true;
		}

		public void RegisterIdleHandler(IIdleHandler idleHandler)
		{
			if (_idleHandlers.Contains(idleHandler))
				return;
			_idleHandlers.Add(idleHandler);
		}

		private void showRecipeCategoryMenuItem_Click(object sender, EventArgs e)
		{
			AppSettings.Settings.ShowRecipeCategory = !AppSettings.Settings.ShowRecipeCategory;
			recipeList.RefreshTitles();
		}

		private void copyMenuItem_Click(object sender, EventArgs e)
		{
			Clipboard.SetDataObject(RecipeClipboard.FromRecipes(Current.Character.recipes), true);
		}

		private void pasteMenuItem_Click(object sender, EventArgs e)
		{
			recipeList.PasteFromClipboard(null);
		}

		private void embeddedAssetsMenuItem_Click(object sender, EventArgs e)
		{
			AssetViewDialog dlg = new AssetViewDialog();
			if (dlg.ShowDialog() == DialogResult.OK && dlg.Changed)
			{
				Current.Card.assets = (AssetCollection)dlg.Assets.Clone();
				Undo.Push(Undo.Kind.Parameter, "Changed embedded assets");

				Current.IsFileDirty = true;
			}
		}

		private void enableLinkMenuItem_Click(object sender, EventArgs e)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				var error = Backyard.EstablishConnection();
				if (error == Backyard.Error.ValidationFailed)
				{
					MessageBox.Show(Resources.error_link_unsupported, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					AppSettings.BackyardLink.Enabled = false;
				}
				else if (error == Backyard.Error.NotConnected)
				{
					MessageBox.Show(Resources.error_link_not_found, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					AppSettings.BackyardLink.Enabled = false;
				}
				else if (error != Backyard.Error.NoError)
				{
					MessageBox.Show(string.Format(Resources.error_link_failed_with_reason, Backyard.LastError ?? ""), Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					AppSettings.BackyardLink.Enabled = false;
				}
				else
				{
					// Fetch characters
					if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
					{
						// Error
						MessageBox.Show(string.Format(Resources.error_link_read_characters, Backyard.LastError ?? ""), Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
						AppSettings.BackyardLink.Enabled = false;
					}
					else
					{
						if (Current.HasLink)
							Current.Link.RefreshState();

						MessageBox.Show(Resources.msg_link_connected, Resources.cap_link_connect, MessageBoxButtons.OK, MessageBoxIcon.Information);
						SetStatusBarMessage(Resources.status_link_connect, Constants.StatusBarMessageInterval);
						AppSettings.BackyardLink.Enabled = true;

						VersionNumber appVersion;
						if (Backyard.GetAppVersion(out appVersion))
							AppSettings.BackyardLink.LastVersion = appVersion;
					}
				}
			}
			else
			{
				SetStatusBarMessage(Resources.status_link_disconnect, Constants.StatusBarMessageInterval);
				AppSettings.BackyardLink.Enabled = false;
				Backyard.Disconnect();
			}
			RefreshTitle();
		}

		private void importLinkedMenuItem_Click(object sender, EventArgs e)
		{
			ImportCharacterFromBackyard();
		}

		private void saveLinkedMenuItem_Click(object sender, EventArgs e)
		{
			var error = UpdateCharacterInBackyard();
			if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_failed, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else if (error == Backyard.Error.NotFound)
			{
				MessageBox.Show(Resources.error_link_save_character, Resources.cap_link_save_character, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else if (error == Backyard.Error.CancelledByUser || error == Backyard.Error.DismissedByUser)
			{
				// User clicked cancel
				return;
			}
			else if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_save, Resources.cap_link_save_character, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				SetStatusBarMessage(Resources.status_link_saved, Constants.StatusBarMessageInterval);
				//MessageBox.Show(Resources.msg_link_saved, Resources.cap_link_save_character, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}
		
		private void saveNewLinkedMenuItem_Click(object sender, EventArgs e)
		{
			CharacterInstance createdCharacter;
			Backyard.Link.Image[] images;

			var error = CreateNewCharacterInBackyard(out createdCharacter, out images);
			if (error == Backyard.Error.NotConnected)
				MessageBox.Show(Resources.error_link_failed, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
			else if (error == Backyard.Error.NotFound)
				MessageBox.Show(Resources.error_link_save_character, Resources.cap_link_save_character, MessageBoxButtons.OK, MessageBoxIcon.Error);
			else if (error != Backyard.Error.NoError)
				MessageBox.Show(Resources.error_link_save, Resources.cap_link_save_character, MessageBoxButtons.OK, MessageBoxIcon.Error);
			else
			{
				if (AppSettings.BackyardLink.AlwaysLinkOnImport || MessageBox.Show(Resources.msg_link_create_link, Resources.cap_link_character, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
				{
					Current.LinkWith(createdCharacter, images);
					Current.IsLinkDirty = false;
					SetStatusBarMessage(Resources.status_link_save_and_link_new, Constants.StatusBarMessageInterval);
					RefreshTitle();
					MessageBox.Show(Resources.msg_link_save_and_link_new, Resources.cap_link_save_character, MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
					MessageBox.Show(Resources.msg_link_saved, Resources.cap_link_save_character, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void reestablishLinkMenuItem_Click(object sender, EventArgs e)
		{
			ReestablishLink();
		}

		private void breakLinkMenuItem_Click(object sender, EventArgs e)
		{
			BreakLink();
		}

		private void enableAutosaveMenuItem_Click(object sender, EventArgs e)
		{
			AppSettings.BackyardLink.Autosave = !AppSettings.BackyardLink.Autosave;
		}

		private void revertLinkedMenuItem_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show(Resources.msg_link_revert, Resources.cap_link_revert, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
				return;

			var error = RevertCharacterFromBackyard();
			if (error == Backyard.Error.NotConnected)
				MessageBox.Show(Resources.error_link_failed, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
			else if (error != Backyard.Error.NoError)
				MessageBox.Show(Resources.error_link_open_character, Resources.cap_link_revert, MessageBoxButtons.OK, MessageBoxIcon.Error);
			else
			{
				Current.IsLinkDirty = false;
				SetStatusBarMessage(Resources.status_link_reverted, Constants.StatusBarMessageInterval);
				RefreshTitle();
			}
		}

		public void rearrangeLoreMenuItem_Click(object sender, EventArgs e)
		{
			AppSettings.Settings.EnableRearrangeLoreMode = !AppSettings.Settings.EnableRearrangeLoreMode;
			recipeList.RefreshAllParameters();
			recipeList.RefreshSyntaxHighlighting(true);
		}

		private void chatHistoryMenuItem_Click(object sender, EventArgs e)
		{
			OpenChatHistory();
		}

		private void applyToFirstChatMenuItem_Click(object sender, EventArgs e)
		{
			AppSettings.BackyardLink.ApplyChatSettings = AppSettings.BackyardLink.ActiveChatSetting.First;
		}

		private void applyToLastChatMenuItem_Click(object sender, EventArgs e)
		{
			AppSettings.BackyardLink.ApplyChatSettings = AppSettings.BackyardLink.ActiveChatSetting.Last;
		}

		private void applyToAllChatsMenuItem_Click(object sender, EventArgs e)
		{
			AppSettings.BackyardLink.ApplyChatSettings = AppSettings.BackyardLink.ActiveChatSetting.All;
		}

		private void alwaysLinkMenuItem_Click(object sender, EventArgs e)
		{
			AppSettings.BackyardLink.AlwaysLinkOnImport = !AppSettings.BackyardLink.AlwaysLinkOnImport;
		}
		
		private void usePortraitAsBackgroundMenuItem_Click(object sender, EventArgs e)
		{
			AppSettings.BackyardLink.UsePortraitAsBackground = !AppSettings.BackyardLink.UsePortraitAsBackground;
		}

		private void lightThemeMenuItem_Click(object sender, EventArgs e)
		{
			AppSettings.Settings.DarkTheme = false;
			ApplyVisualTheme();
		}

		private void darkThemeMenuItem_Click(object sender, EventArgs e)
		{
			AppSettings.Settings.DarkTheme = true;
			ApplyVisualTheme();
		}
		
		public void ApplyVisualTheme()
		{
			if (Utility.InDesignMode)
				return;

			Theme.BeginTheming();

			Theme.Apply(menuStrip);

			this.BackColor = Theme.Current.ControlBackground;
			this.ForeColor = Theme.Current.ControlForeground;

			this.splitContainer.Panel1.BackColor = Theme.Current.ControlBackground;
			this.splitContainer.Panel2.BackColor = Theme.Current.ControlBackground;

			Theme.Apply(btnAdd_Model);
			Theme.Apply(btnAdd_Character);
			Theme.Apply(btnAdd_Mind);
			Theme.Apply(btnAdd_World);
			Theme.Apply(btnAdd_Traits);
			Theme.Apply(btnAdd_Other);
			Theme.Apply(btnAdd_Snippets);
			Theme.Apply(btnAdd_Lore);
			btnAdd_Model.Image = Theme.Current.ButtonModel;
			btnAdd_Character.Image = Theme.Current.ButtonCharacter;
			btnAdd_Mind.Image = Theme.Current.ButtonMind;
			btnAdd_World.Image = Theme.Current.ButtonStory;
			btnAdd_Traits.Image = Theme.Current.ButtonTraits;
			btnAdd_Other.Image = Theme.Current.ButtonComponents;
			btnAdd_Snippets.Image = Theme.Current.ButtonSnippets;
			btnAdd_Lore.Image = Theme.Current.ButtonLore;

			statusBar.BackColor = Theme.Current.ControlBackground;
			statusBar.ForeColor = Theme.Current.ControlForeground;
			sidePanel.ApplyVisualTheme();
			recipeList.ApplyVisualTheme();
			tabControl.ApplyVisualTheme();

			outputBox.ForeColor = Theme.Current.OutputForeground;
			outputBox.BackColor = Theme.Current.OutputBackground;

			userNotes.richTextBox.ForeColor = Theme.Current.NotesForeground;
			userNotes.richTextBox.BackColor = Theme.Current.NotesBackground;

			if (_findDialog != null && _findDialog.IsDisposed == false)
				_findDialog.ApplyTheme();
			if (_editChatDialog != null && _editChatDialog.IsDisposed == false)
				_editChatDialog.ApplyTheme();

			Theme.ApplyToTitleBar(this, true);
			Theme.EndTheming();

			RefreshTitle();
		}

		private void customVariablesMenuItem_Click(object sender, EventArgs e)
		{
			ShowCustomVariablesDialog();
		}

		private void bulkExportMenuItem_Click(object sender, EventArgs e)
		{
			ExportManyFromBackyard();
		}

		private void bulkImportMenuItem_Click(object sender, EventArgs e)
		{
			ImportManyToBackyard();
		}

		private void bulkChangeModelSettingsMenuItem_Click(object sender, EventArgs e)
		{
			EditManyModelSettings();			
		}

		private void editExportModelSettingsMenuItem_Click(object sender, EventArgs e)
		{
			var dlg = new EditModelSettingsDialog();
			dlg.EditingDefaults = true;
			if (dlg.ShowDialog() == DialogResult.OK)
				AppSettings.BackyardSettings.UserSettings = dlg.Parameters.Copy();
		}

		private void importAltGreetingsMenuItem_Click(object sender, EventArgs e)
		{
			AppSettings.BackyardLink.ImportAlternateGreetings = !AppSettings.BackyardLink.ImportAlternateGreetings;
		}

		private void createBackupMenuItem_Click(object sender, EventArgs e)
		{
			CreateBackyardBackup();
		}

		private void restoreBackupMenuItem_Click(object sender, EventArgs e)
		{
			CharacterInstance characterInstance;
			RestoreBackyardBackup(out characterInstance);
		}

		private void repairBrokenImagesMenuItem_Click(object sender, EventArgs e)
		{
			RepairBrokenImages();
		}

		private void purgeUnusedImages_Click(object sender, EventArgs e)
		{
			PurgeUnusedImages();
		}

		private void editCurrentModelSettingsMenuItem_Click(object sender, EventArgs e)
		{
			EditCurrentModelSettings();
		}
	}

	public interface IIdleHandler
	{
		void OnIdle();
	}
}