﻿using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Ginger
{
	public partial class RecipeList : UserControl, IIdleHandler
	{
		[Category("Appearance"), Description("Gradient color")]
		public Color GradientColor { get; set; }

		private List<RecipePanel> recipePanels = new List<RecipePanel>();

		public event EventHandler SaveAsSnippet;
		public event EventHandler SaveAsRecipe;
		public event EventHandler SaveLorebook;

		private bool _bShouldUpdateScrollbars;
		private bool _bShouldUpdateLayout;
		private bool _bIgnoreEvents;
		private Size _lastSize;

		protected override CreateParams CreateParams
		{
			get
			{
				// Always show vertical scrollbar
				var cp = base.CreateParams;
				cp.Style |= 0x00200000; // WS_VSCROLL
				return cp;
			}
		}

		public RecipeList()
		{
			InitializeComponent();

			this.Paint += OnPaint;

			this.DoubleBuffered = true;
			this.SuspendLayout(); // Manual layout
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			if (_lastSize != this.Size)
			{
				_lastSize = this.Size;
				if (recipePanels.Count > 0)
					ResizeRecipePanels();
			}
			this.HideHorizontalScrollbar();
		}

		private void OnPaint(object sender, PaintEventArgs e)
		{
			var panel = sender as RecipeList;
			var graphics = e.Graphics;

			// Solid bg
			using (var bgBrush = new SolidBrush(this.BackColor))
			{
				graphics.FillRectangle(bgBrush, panel.DisplayRectangle);
			}

			// Gradient
			RectangleF rectBottom = new RectangleF(new PointF(0, panel.Height - panel.Height / 2), new SizeF(panel.Width, panel.Height / 2));
			RectangleF drawBottom = new RectangleF(rectBottom.X, rectBottom.Y + 1, rectBottom.Width, rectBottom.Height - 1);
			using (var bgGradient = new LinearGradientBrush(rectBottom, this.BackColor, this.GradientColor, LinearGradientMode.Vertical))
			{
				graphics.FillRectangle(bgGradient, drawBottom);
			}

			// Shadow
			if (Controls.Count > 0)
			{
				int bottomY = Controls[0].Location.Y + Controls[0].Size.Height - 2;
				RectangleF rectShadow = new RectangleF(new PointF(0, bottomY), new SizeF(panel.Width, 40));
				using (Brush brush = new LinearGradientBrush(rectShadow, Color.FromArgb(40, Color.Black), Color.FromArgb(0, Color.Black), LinearGradientMode.Vertical))
				{
					RectangleF rect = new RectangleF(rectShadow.X, rectShadow.Y, rectShadow.Width, rectShadow.Height);
					graphics.FillRectangle(brush, rect);
				}
			}
		}

		public RecipePanel AddRecipePanel(Recipe recipe, bool bLayout, int insertAt = -1)
		{
			if (recipe == null)
				return null;

			_bIgnoreEvents = true;

			if (bLayout)
			{
				Undo.Suspend();
				this.Suspend();
				RichTextBoxEx.AllowSyntaxHighlighting = false;
			}

			var panel = new RecipePanel();
			Controls.Add(panel);
			panel.Name = "recipe_" + recipePanels.Count.ToString("00");
			panel.Size = new Size(this.ClientSize.Width, 100);
			panel.OnRemove += OnRemoveRecipe;
			panel.OnMoveUp += OnMoveRecipeUp;
			panel.OnMoveDown += OnMoveRecipeDown;
			panel.OnMoveToTop += OnMoveRecipeToTop;
			panel.OnMoveToBottom += OnMoveRecipeToBottom;
			panel.OnParameterChanged += OnParameterChanged;
			panel.OnCollapse += OnExpandCollapse;
			panel.OnExpand += OnExpandCollapse;
			panel.OnBake += OnBakeSingle;
			panel.OnEnable += OnEnableRecipe;
			panel.OnToggleRaw += OnToggleRaw;
			panel.OnSaveAsSnippet += OnSaveAsSnippet;
			panel.OnSaveAsRecipe += OnSaveAsRecipe;
			panel.OnSaveLorebook += OnSaveLorebook;
			panel.OnMakePrimaryGreeting += OnMakePrimaryGreeting;
			panel.OnPanelSizeChanged += OnPanelSizeChanged;
			panel.OnCopy += OnCopy;
			panel.OnPaste += OnPaste;
			panel.InitRecipe(recipe);

			if (recipe.isBase)
				insertAt = 0;

			if (insertAt < 0)
			{
				recipePanels.Add(panel);
			}
			else
			{
				insertAt = Math.Min(insertAt, recipePanels.Count);
				recipePanels.Insert(insertAt, panel);
			}

			// Tab stop
			for (int i = 0; i < recipePanels.Count; ++i)
			{
				recipePanels[i].TabIndex = i;
				recipePanels[i].RefreshTitle(); // Greeting titles depend on order and count
			}

			panel.Visible = true;
			if (!recipe.isCollapsed && panel.CanExpand)
				panel.Expand(); // Resize
			else
				panel.Collapse();

			if (bLayout)
			{
				RichTextBoxEx.AllowSyntaxHighlighting = true;
				panel.RefreshSyntaxHighlighting(false);

				RefreshParameterVisibility();
				RefreshLayout();

				this.Resume();
				panel.Focus();

				ScrollToPanel(panel);
				Invalidate(false);
				Undo.Resume();
			}

			_bIgnoreEvents = false;
			_bShouldUpdateLayout = true;

			return panel;
		}

		public void AddRecipePanels(IEnumerable<Recipe> recipes, bool scrollToPanel = false)
		{
			if (recipes == null || recipes.Count() == 0)
				return;

			_bIgnoreEvents = true;

			RichTextBoxEx.AllowSyntaxHighlighting = false; // Performance
			if (recipes.Count() > 10)
				MainForm.EnableFormLevelDoubleBuffering(false); // Prefer partial drawing over blank screen (for large characters)

			Undo.Suspend();

			List<RecipePanel> addedPanels = new List<RecipePanel>();
			this.Suspend();
			this.DisableRedrawAndDo(() => {
				foreach (var recipe in recipes)
				{
					var panel = new RecipePanel();
					panel.Dock = DockStyle.None;
					panel.Name = "recipe_" + recipePanels.Count.ToString("00");
					panel.Size = new Size(this.ClientSize.Width, 100);
					panel.Font = this.Font;
					panel.OnRemove += OnRemoveRecipe;
					panel.OnMoveUp += OnMoveRecipeUp;
					panel.OnMoveDown += OnMoveRecipeDown;
					panel.OnMoveToTop += OnMoveRecipeToTop;
					panel.OnMoveToBottom += OnMoveRecipeToBottom;
					panel.OnParameterChanged += OnParameterChanged;
					panel.OnCollapse += OnExpandCollapse;
					panel.OnExpand += OnExpandCollapse;
					panel.OnBake += OnBakeSingle;
					panel.OnEnable += OnEnableRecipe;
					panel.OnToggleRaw += OnToggleRaw;
					panel.OnSaveAsSnippet += OnSaveAsSnippet;
					panel.OnSaveAsRecipe += OnSaveAsRecipe;
					panel.OnSaveLorebook += OnSaveLorebook;
					panel.OnMakePrimaryGreeting += OnMakePrimaryGreeting;
					panel.OnPanelSizeChanged += OnPanelSizeChanged;
					panel.OnCopy += OnCopy;
					panel.OnPaste += OnPaste;
					panel.recipe = recipe;
					addedPanels.Add(panel);
				}

				// Base recipes goes on top.
				// There should only be one, but we can't know that for sure.
				int insert = 0;
				foreach (var baseRecipe in addedPanels.Where(p => p.recipe.isBase))
					recipePanels.Insert(insert++, baseRecipe);

				recipePanels.AddRange(addedPanels.Where(p => p.recipe.isBase == false));
				Controls.AddRange(addedPanels.ToArray());

				// Tab stop
				for (int i = 0; i < addedPanels.Count; ++i)
				{
					var panel = addedPanels[i];
					panel.InitRecipe(panel.recipe);
					if (!panel.recipe.isCollapsed && panel.CanExpand)
						panel.Expand(); // Resize
					else
						panel.Collapse();

					panel.TabIndex = i;
				}

				RefreshParameterVisibility();
				RefreshLayout();
			});

			MainForm.EnableFormLevelDoubleBuffering(true); // Restore
			RichTextBoxEx.AllowSyntaxHighlighting = true; // Restore
			RefreshSyntaxHighlighting(false); 

			if (scrollToPanel && addedPanels.Count > 0)
				ScrollToPanel(addedPanels[0]);

			this.Resume();
			Invalidate(true);
			Undo.Resume();

			_bIgnoreEvents = false;
		}

		private void OnPanelSizeChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			RefreshLayout();
			Invalidate(false);
		}

		public void RemoveAllPanels()
		{
			foreach (var panel in recipePanels)
				panel.Dispose();
			this.Controls.Clear();
			recipePanels.Clear();
		}

		public void RemoveRecipe(Recipe recipe, bool bLayout)
		{
			MainForm.StealFocus();

			var panel = recipePanels.Find(p => p.recipe == recipe);
			if (panel == null)
				return;

			if (bLayout)
				this.Suspend();

			this.Controls.Remove(panel);
			recipePanels.Remove(panel);
			panel.Dispose();

			Current.Character.RemoveRecipe(recipe);
			RefreshParameterVisibility();

			// Tab stop
			for (int i = 0; i < recipePanels.Count; ++i)
				recipePanels[i].RefreshTitle(); // Greeting titles depend on order and count

			if (bLayout)
			{
				RefreshLayout();
				this.Resume();
				Invalidate(false);
			}
			_bShouldUpdateScrollbars = true;
		}

		private void OnParameterChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			RefreshParameterVisibility();
		}
		
		private void OnExpandCollapse(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			var panel = sender as RecipePanel;
			if (panel.Collapsed == false)
				ScrollToPanel(panel);

			RefreshLayout();
		}

		public void RecreatePanels(bool bScrollToTop = false)
		{
			if (ActiveControl != null)
				MainForm.StealFocus();

			if (!bScrollToTop)
				MainForm.SetStatusBarMessage("Refreshing recipe list...");

			this.Suspend();
			this.DisableRedrawAndDo(() => {
				int vscroll = VerticalScroll.Value;
				RemoveAllPanels();
				AddRecipePanels(Current.Character.recipes);

				if (bScrollToTop)
					ScrollToTop();
				else
				{
					VerticalScroll.Value = vscroll;
					HorizontalScroll.Value = 0;
					AdjustFormScrollbars(true);
					Invalidate(false); // Repaint
				}
			});
			this.Resume();

			MainForm.ClearStatusBarMessage();

		}

		public void RefreshAllParameters()
		{
			foreach (var panel in recipePanels)
				panel.RefreshAllParameters();
			Invalidate(false);
		}

		private void OnRemoveRecipe(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			var panel = sender as RecipePanel;
			RemoveRecipe(panel.recipe, true);
			Undo.Push(Undo.Kind.RecipeAddRemove, "Remove recipe");
		}

		public void RefreshParameterVisibility()
		{
			bool bChanged = false;
			foreach (var panel in recipePanels)
			{
				if (panel.recipe.isEnabled == false)
					continue;

				Context localContext = Current.Character.GetContextForRecipe(panel.recipe);
				bChanged |= panel.RefreshParameterVisibility(localContext);
			}
			if (bChanged)
				Invalidate(false);
		}

		private void OnMoveRecipeUp(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			var panel = sender as RecipePanel;
			int position = recipePanels.IndexOf(panel);

			if (MoveRecipe(position, position - 1, true))
			{
				Current.IsDirty = true;
				Undo.Push(Undo.Kind.RecipeOrder, "Move recipe up");
			}
		}

		private void OnMoveRecipeDown(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			var panel = sender as RecipePanel;
			int position = recipePanels.IndexOf(panel);

			if (MoveRecipe(position, position + 1, true))
			{
				Current.IsDirty = true;
				Undo.Push(Undo.Kind.RecipeOrder, "Move recipe down");
			}
		}

		private void OnMoveRecipeToTop(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			var panel = sender as RecipePanel;
			int position = recipePanels.IndexOf(panel);
			
			if (MoveRecipe(position, 0, true))
			{
				Current.IsDirty = true;
				Undo.Push(Undo.Kind.RecipeOrder, "Move recipe to top");
			}
		}

		private void OnMoveRecipeToBottom(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			var panel = sender as RecipePanel;
			int position = recipePanels.IndexOf(panel);
			
			if (MoveRecipe(position, recipePanels.Count - 1, true))
			{
				Current.IsDirty = true;
				Undo.Push(Undo.Kind.RecipeOrder, "Move recipe to bottom");
			}
		}

		private bool MoveRecipe(int position, int newPosition, bool bLayout = false)
		{
			if (newPosition < 0)
				newPosition = 0;
			if (newPosition > recipePanels.Count - 1)
				newPosition = recipePanels.Count - 1;
			if (newPosition == 0 && recipePanels[0].recipe.isBase)
				newPosition = 1;

			if (newPosition == position)
				return false;

			var panel = recipePanels[position];

			recipePanels.RemoveAt(position);
			recipePanels.Insert(newPosition, panel);
			var instance = Current.Character.recipes[position];
			Current.Character.recipes.RemoveAt(position);
			Current.Character.recipes.Insert(newPosition, instance);

			if (bLayout)
			{
				RefreshParameterVisibility();
				RefreshLayout();

				// Tab stop
				for (int i = 0; i < recipePanels.Count; ++i)
				{
					recipePanels[i].TabIndex = i;
					recipePanels[i].RefreshTitle(); // Greeting titles depend on order and count
				}

				ScrollToPanel(panel);
				Invalidate(false);
			}

			_bShouldUpdateLayout = true;
			return true;
		}

		public void ExpandAll()
		{
			MainForm.EnableFormLevelDoubleBuffering(true);

			LockAndDo(() => {
				foreach (var panel in recipePanels)
					panel.Expand();
				RefreshLayout();
			});
			
			Undo.Push(Undo.Kind.Parameter, "Expand all");
			Refresh();
		}

		public void CollapseAll()
		{
			// Drop focus
			if (this.ActiveControl != null)
				MainForm.StealFocus();

			LockAndDo(() => {
				foreach (var panel in recipePanels)
					panel.Collapse();
				RefreshLayout();
			});

			Undo.Push(Undo.Kind.Parameter, "Collapse all");
			Refresh();
		}

		private void OnBakeSingle(object sender, EventArgs e)
		{
			var panel = sender as RecipePanel;
			var recipe = panel.recipe;

			if (BakeSingle(recipe) == false)
				MessageBox.Show(Resources.error_bake_no_result, Resources.cap_bake_no_result, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private bool BakeSingle(Recipe recipe)
		{
			var output = Generator.Generate(recipe, Generator.Option.Bake | Generator.Option.Single);

			List<Recipe> recipes = new List<Recipe>();
			recipes.Add(BakeChannel(output, Recipe.Component.System, Resources.system_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.System_PostHistory, Resources.post_history_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.Persona, Resources.persona_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.UserPersona, Resources.user_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.Scenario, Resources.scenario_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.Greeting, Resources.greeting_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.Example, Resources.example_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.Grammar, Resources.grammar_recipe));

			if (output.hasLore)
			{
				var loreRecipe = RecipeBook.CreateRecipeFromResource(Resources.lorebook_recipe);
				output.lorebook.Bake();

				(loreRecipe.parameters[0] as LorebookParameter).value = output.lorebook;
				recipes.Add(loreRecipe);
			}

			recipes.RemoveAll(r => r == null);
			if (recipes.Count == 0)
				return false;

			int insertionIndex = Current.Character.recipes.IndexOf(recipe);
			RemoveRecipe(recipe, false);

			for (int i = 0; i < recipes.Count; ++i)
			{
				Current.Character.recipes.Insert(insertionIndex, recipes[i]);
				AddRecipePanel(recipes[i], false, insertionIndex++);
			}

			RefreshParameterVisibility();
			RefreshLayout();
			RefreshSyntaxHighlighting(false);
			Invalidate(false);

			Undo.Push(Undo.Kind.RecipeList, "Bake recipe");
			return true;
		}

		private Recipe BakeChannel(Generator.Output output, Recipe.Component channel, string xmlSource)
		{
			string text = output.GetText(channel).ToBaked();
			if (string.IsNullOrWhiteSpace(text))
				return null;

			Recipe recipe = RecipeBook.CreateRecipeFromResource(xmlSource);
			(recipe.parameters[0] as TextParameter).value = text;
			return recipe;
		}

		private void OnSaveAsSnippet(object sender, EventArgs e)
		{
			SaveAsSnippet?.Invoke(sender, e);
		}

		private void OnSaveAsRecipe(object sender, EventArgs e)
		{
			SaveAsRecipe?.Invoke(sender, e);
		}

		private void OnSaveLorebook(object sender, EventArgs e)
		{
			SaveLorebook?.Invoke(sender, e);
		}

		private void OnCopy(object sender, EventArgs e)
		{
			if (sender != null) // Copy single
			{
				var panel = sender as RecipePanel;
				var recipe = panel.recipe;
				Clipboard.SetDataObject(RecipeClipboard.FromRecipes(new Recipe[] { recipe }), true);
			}
			else // Copy all
			{
				Clipboard.SetDataObject(RecipeClipboard.FromRecipes(Current.Character.recipes), true);
			}
		}

		private void OnPaste(object sender, EventArgs e)
		{
			PasteFromClipboard(sender as RecipePanel);
		}

		public void PasteFromClipboard(RecipePanel targetPanel)
		{ 
			if (Clipboard.ContainsData(LoreClipboard.Format))
			{
				OnPasteLore(targetPanel);
				return;
			}
			else if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
			{
				OnPasteText(targetPanel);
				return;
			}

			if (Clipboard.ContainsData(RecipeClipboard.Format) == false)
				return;

			int insertionIndex = -1;
			if (targetPanel != null)
				insertionIndex = Current.Character.recipes.IndexOf(targetPanel.recipe);
			if (insertionIndex == -1)
				insertionIndex = Current.Character.recipes.Count;

			if (insertionIndex == 0
				&& Current.Character.recipes.Count > 0
				&& Current.Character.recipes[0].isBase)
				insertionIndex++; // Can't move base

			RecipeClipboard data = Clipboard.GetData(RecipeClipboard.Format) as RecipeClipboard;
			if (data == null)
				return;

			List<Recipe> recipes = data.ToRecipes();
			if (recipes == null || recipes.Count == 0)
				return;

			MainForm.StealFocus();
			RichTextBoxEx.AllowSyntaxHighlighting = false;

			int failedCounter = 0;
			bool bAdded = false;
			RecipePanel pastedPanel = null;
			foreach (var recipe in recipes)
			{
				if (recipe.allowMultiple == false && Current.Character.recipes.ContainsAny(r => r.id == recipe.id))
				{
					failedCounter++;
					continue;
				}

				if (recipe.requires != null && recipe.requires.Evaluate(
					Current.Character.GetContext(CharacterData.ContextType.WithFlags, true),
					new EvaluationCookie() { ruleSuppliers = Current.RuleSuppliers }) == false)
				{
					failedCounter++;
					continue;
				}

				Current.Character.recipes.Insert(insertionIndex, recipe);
				var newPanel = AddRecipePanel(recipe, false, insertionIndex++);
				if (pastedPanel == null)
					pastedPanel = newPanel;
				bAdded = true;
			}

			if (bAdded)
			{
				RichTextBoxEx.AllowSyntaxHighlighting = true;
				RefreshSyntaxHighlighting(false);

				RefreshParameterVisibility();
				RefreshLayout();
				ScrollToPanel(pastedPanel);
				Undo.Push(Undo.Kind.RecipeAddRemove, "Paste recipe");
				Current.IsDirty = true;
			}

			if (failedCounter > 0)
				MessageBox.Show(string.Format(Resources.error_pasted, failedCounter), Resources.cap_pasted, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			Invalidate(false);
		}

		private void OnPasteLore(RecipePanel targetPanel)
		{
			if (Clipboard.ContainsData(LoreClipboard.Format) == false)
				return;

			int insertionIndex = -1;
			if (targetPanel != null)
				insertionIndex = recipePanels.FindIndex(p => p.recipe == targetPanel.recipe);
			if (insertionIndex == -1)
				insertionIndex = recipePanels.Count;

			if (insertionIndex == 0
				&& Current.Character.recipes.Count > 0
				&& Current.Character.recipes[0].isBase)
				insertionIndex++; // Can't move base

			LoreClipboard data = Clipboard.GetData(LoreClipboard.Format) as LoreClipboard;
			if (data == null)
				return;

			var entries = data.ToEntries();
			if (entries == null || entries.Count == 0)
				return;

			MainForm.StealFocus();

			RecipePanel pastedPanel = null;
			var lorebook = new Lorebook();
			lorebook.entries.AddRange(entries);
			var instance = Current.AddLorebook(lorebook);
			AddRecipePanel(instance, true, insertionIndex);

			ScrollToPanel(pastedPanel);
			Invalidate(false);

			Undo.Push(Undo.Kind.RecipeAddRemove, "Paste lorebook");
			Current.IsDirty = true;
		}

		private void OnPasteText(RecipePanel targetPanel)
		{
			if (Clipboard.ContainsText(TextDataFormat.UnicodeText) == false)
				return;

			string recipeXml;
			using (var dlg = new PasteTextDialog())
			{
				if (dlg.ShowDialog() != DialogResult.OK)
					return;
				recipeXml = dlg.RecipeXml;
			}

			int insertionIndex = -1;
			if (targetPanel != null)
				insertionIndex = recipePanels.FindIndex(p => p.recipe == targetPanel.recipe);
			if (insertionIndex == -1)
				insertionIndex = recipePanels.Count;

			if (insertionIndex == 0
				&& Current.Character.recipes.Count > 0
				&& Current.Character.recipes[0].isBase)
				insertionIndex++; // Can't move base

			var formats = Clipboard.GetDataObject().GetFormats();
			string content;
			if (Array.IndexOf(formats, "System.String") != -1)
				content = (string)Clipboard.GetDataObject().GetData("System.String");
			else if (Array.IndexOf(formats, "UnicodeText") != -1)
				content = (string)Clipboard.GetDataObject().GetData("UnicodeText");
			else
				content = Clipboard.GetText();

			if (content == null)
				return;

			var recipe = RecipeBook.CreateRecipeFromResource(recipeXml, Recipe.Type.Component);
			if (recipe == null)
				return; // Error

			(recipe.parameters[0] as TextParameter).value = content;

			this.Suspend();
			this.DisableRedrawAndDo(() => {
				Current.Character.recipes.Insert(insertionIndex, recipe);
				var pastedPanel = AddRecipePanel(recipe, false, insertionIndex);
				pastedPanel.Focus();
				ScrollToPanel(pastedPanel);
			});
			this.Resume();
			

			Undo.Push(Undo.Kind.RecipeAddRemove, "Paste");
			Current.IsDirty = true;
		}

		private void OnMakePrimaryGreeting(object sender, EventArgs e)
		{
			var panel = sender as RecipePanel;
			int position = recipePanels.IndexOf(panel);
			if (panel.recipe.isGreeting == false)
				return; // Not a greeting

			int from = recipePanels.IndexOf(panel);
			int to = recipePanels.FindIndex(p => p.recipe.isGreeting);
			if (from == -1 || to == -1 || from == to)
				return; // Can't move

			// Swap
			this.Suspend();
			MoveRecipe(to, from - 1, false);
			MoveRecipe(from, to, false);

			// Tab stop
			for (int i = 0; i < recipePanels.Count; ++i)
			{
				recipePanels[i].TabIndex = i;
				recipePanels[i].RefreshTitle(); // Greeting titles depend on order and count
			}

//			ScrollToPanel(panel);
			RefreshParameterVisibility();
			RefreshLayout();
			this.Resume();

			Invalidate(false);

			Current.IsDirty = true;
			Undo.Push(Undo.Kind.RecipeOrder, "Change primary greeting");
		}


		protected override void OnMouseDown(MouseEventArgs e)
		{
			MainForm.StealFocus(); // Prevents annoying scroll to top

			if (e.Button == MouseButtons.Right)
				RecipeList_MouseDown(this, e);
		}

		private void RecipeList_MouseDown(object sender, MouseEventArgs args)
		{
			if (args.Button == MouseButtons.Right)
			{
				Context context = Current.Character.GetContext(CharacterData.ContextType.WithFlags, true);

				// Create recipe context menu
				var menu = new ContextMenuStrip();
				MainForm.instance.PopulateComponentMenu("", menu.Items, context);
				menu.Items.Add("-");

				var model = new ToolStripMenuItem("Model", Resources.folder);
				var character = new ToolStripMenuItem("Character", Resources.folder);
				var mind = new ToolStripMenuItem("Mind", Resources.folder);
				var traits = new ToolStripMenuItem("Traits", Resources.folder);
				var story = new ToolStripMenuItem("Story", Resources.folder);
				var snippets = new ToolStripMenuItem("Snippets", Resources.folder);
				var lore = new ToolStripMenuItem("Lorebooks", Resources.folder);
				var otherComponents = new ToolStripMenuItem("Other components", Resources.folder);

				MainForm.instance.PopulateRecipeMenu(Recipe.Drawer.Model, model.DropDownItems, context);
				MainForm.instance.PopulateRecipeMenu(Recipe.Drawer.Character, character.DropDownItems, context);
				MainForm.instance.PopulateRecipeMenu(Recipe.Drawer.Appearance, traits.DropDownItems, context);
				MainForm.instance.PopulateRecipeMenu(Recipe.Drawer.Story, story.DropDownItems, context);
				MainForm.instance.PopulateRecipeMenu(Recipe.Drawer.Mind, mind.DropDownItems, context);
				MainForm.instance.PopulateRecipeMenu(Recipe.Drawer.Snippets, snippets.DropDownItems, context);
				MainForm.instance.PopulateRecipeMenu(Recipe.Drawer.Lore, lore.DropDownItems, context);
				MainForm.instance.PopulateComponentMenu("Other", otherComponents.DropDownItems, context);
				menu.Items.Add(model);
				menu.Items.Add(character);
				menu.Items.Add(traits);
				menu.Items.Add(mind);
				menu.Items.Add(story);
				menu.Items.Add(snippets);
				menu.Items.Add(lore);
				menu.Items.Add(otherComponents);

				if (RecipeBook.allRecipes.ContainsAny(r => r.type == Recipe.Type.Recipe
					&& r.drawer == Recipe.Drawer.Undefined
					&& r.isHidden == false))
				{
					var unknown = new ToolStripMenuItem("Uncategorized", Resources.folder);
					MainForm.instance.PopulateRecipeMenu(Recipe.Drawer.Undefined, unknown.DropDownItems, context);
					menu.Items.Add(unknown);
				}

				menu.Items.Add("-");

				menu.Items.Add(new ToolStripMenuItem("Copy recipes", null, (s, e) => { OnCopy(null, e); }) {
					Enabled = !Current.Character.recipes.IsEmpty(),
				});
				if (Clipboard.ContainsData(RecipeClipboard.Format))
				{
					menu.Items.Add(new ToolStripMenuItem("Paste recipes", null, (s, e) => { OnPaste(null, e); }));
				} 
				else if (Clipboard.ContainsData(LoreClipboard.Format))
				{
					menu.Items.Add(new ToolStripMenuItem("Paste lorebook", null, (s, e) => { OnPaste(null, e); }));
				} 
				else if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
				{
					menu.Items.Add(new ToolStripMenuItem("Paste text", null, (s, e) => { OnPaste(null, e); }));
				}
				else
				{
					menu.Items.Add(new ToolStripMenuItem("Paste", null, (EventHandler)null) { Enabled = false });
				}

				menu.Show(this, new Point(args.X, args.Y));
			}
		}

		public bool ReplaceRecipe(Recipe recipe, Recipe newRecipe)
		{
			int position = recipePanels.FindIndex(r => r.recipe == recipe);
			if (position == -1)
				return false;

			this.Suspend();
			RemoveRecipe(recipe, false);

			Current.Character.recipes.Insert(position, newRecipe);
			var panel = AddRecipePanel(newRecipe, false, position);

			RefreshParameterVisibility();
			RefreshLayout();
			this.Resume();

			Invalidate(false);

			return true;
		}

		private void OnEnableRecipe(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			var panel = sender as RecipePanel;
			var recipe = panel.recipe;
			recipe.isEnabled = !recipe.isEnabled;
			panel.SetEnabled(recipe.isEnabled);

			RefreshParameterVisibility();
			for (int i = 0; i < recipePanels.Count; ++i)
				recipePanels[i].RefreshTitle(); // Greeting titles depend on this

			Current.IsDirty = true;
			Undo.Push(Undo.Kind.Parameter, recipe.isEnabled ? "Enabled recipe" : "Disabled recipe");
		}

		private void OnToggleRaw(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			var panel = sender as RecipePanel;
			var recipe = panel.recipe;

			if (!recipe.canToggleTextFormatting)
				return;

			recipe.EnableTextFormatting(!recipe.enableTextFormatting);

			Current.IsDirty = true;
			Undo.Push(Undo.Kind.Parameter, "Toggle raw");
		}

		public void RefreshLoreTokenCounts(Dictionary<string, int> loreTokens)
		{
			foreach (var panel in recipePanels)
				panel.RefreshLoreTokenCounts(loreTokens);
		}

		public void ScrollToRecipe(Recipe recipe)
		{
			ScrollToPanel(recipePanels.FirstOrDefault(p => p.recipe == recipe));
		}

		public void ScrollToPanel(RecipePanel panel)
		{
			if (panel == null)
				return;

			var point = AutoScrollOffset.Y - ScrollToControl(panel).Y;
			VerticalScroll.Value = Math.Min(Math.Max(point, VerticalScroll.Minimum), VerticalScroll.Maximum);
			HorizontalScroll.Value = 0;
			AdjustFormScrollbars(true);
		}

		public Searchable[] GetSearchables()
		{
			var temp = recipePanels.Select(p => new {
				container = p,
				controls = p.parameterPanels
					.OfType<ISearchableContainer>()
					.Where(s => s.Enabled)
					.SelectMany(pp => pp.GetSearchables())
			});
			var list = new List<Searchable>();
			foreach (var x in temp)
			{
				list.AddRange(x.controls.Select(c => new Searchable() {
					panel = x.container,
					control = c,
				}));
			}
			return list.ToArray();
		}

		public void ScrollToTop()
		{
			VerticalScroll.Value = VerticalScroll.Minimum;
			HorizontalScroll.Value = 0;
			AdjustFormScrollbars(true);
			Invalidate(false); // Repaint
		}

		public void ScrollToBottom()
		{
			VerticalScroll.Value = VerticalScroll.Maximum;
			HorizontalScroll.Value = 0;
			AdjustFormScrollbars(true);
			Invalidate(false); // Repaint
		}

		public void ScrollUp(bool page)
		{
			VerticalScroll.Value = Math.Max(VerticalScroll.Value - (page ? 700 : 100), VerticalScroll.Minimum);
			HorizontalScroll.Value = 0;
			AdjustFormScrollbars(true);
			Invalidate(false); // Repaint
		}

		public void ScrollDown(bool page)
		{
			VerticalScroll.Value = Math.Min(VerticalScroll.Value + (page ? 700 : 100), VerticalScroll.Maximum);
			HorizontalScroll.Value = 0;
			AdjustFormScrollbars(true);
			Invalidate(false); // Repaint
		}

		public RecipePanel GetRecipePanel(Recipe recipe)
		{
			return recipePanels.FirstOrDefault(p => p.recipe == recipe);
		}

		private static List<T> FindAllControlsOfType<T>(Control parent) where T : Control
		{
			var controls = new List<T>();
			if (parent is T)
				controls.Add(parent as T);
			for (int i = 0; i < parent.Controls.Count; ++i)
				controls.AddRange(FindAllControlsOfType<T>(parent.Controls[i]));
			return controls;
		}

		public T[] FindAllControlsOfType<T>() where T : Control
		{
			var controls = new List<T>();
			foreach (var panel in recipePanels)
				controls.AddRange(FindAllControlsOfType<T>(panel));
			return controls.ToArray();
		}

		public bool RefreshReferences()
		{
			if (Current.Character.recipes.Count != recipePanels.Count)
				return false;

			this.Suspend();
			RichTextBoxEx.AllowSyntaxHighlighting = false;

			// Validate
			for (int i = 0; i < recipePanels.Count; ++i)
			{
				if (recipePanels[i].recipe.id != Current.Character.recipes[i].id)
					return false;
			}

			for (int i = 0; i < recipePanels.Count; ++i)
				recipePanels[i].ResetRecipeReference(Current.Character.recipes[i]);

			RefreshParameterVisibility();
			RefreshFlexibleTextBoxes();
			RefreshLayout();
			this.Resume();

			RichTextBoxEx.AllowSyntaxHighlighting = true;
			RefreshSyntaxHighlighting(false);

			return true;
		}

		public bool RefreshReferencesDelta()
		{
			try
			{
				this.Suspend();
				RichTextBoxEx.AllowSyntaxHighlighting = false;

				if (Current.Character.recipes.Count > recipePanels.Count) // Recipe added
				{
					var oldRecipes = recipePanels.Select(r => r.recipe).ToList();
					var newRecipes = Current.Character.recipes;

					var added = new HashSet<int>();
					int oldIndex = 0;
					for (int i = 0; i < newRecipes.Count; ++i)
					{
						if (oldIndex < oldRecipes.Count && newRecipes[i].CompareTo(oldRecipes[oldIndex]))
						{
							oldIndex++;
							continue;
						}
						added.Add(i);
					}

					for (int i = 0; i < newRecipes.Count; ++i)
					{
						if (added.Contains(i))
							AddRecipePanel(newRecipes[i], false, i);
						else
							recipePanels[i].ResetRecipeReference(newRecipes[i]);
					}

					return true;
				}
				else if (Current.Character.recipes.Count < recipePanels.Count) // Recipe removed
				{
					var oldRecipes = recipePanels.Select(r => r.recipe).ToList();
					var newRecipes = Current.Character.recipes;

					var removed = new List<int>();
					int newIndex = 0;
					for (int i = 0; i < oldRecipes.Count; ++i)
					{
						if (newIndex < newRecipes.Count && newRecipes[newIndex].CompareTo(oldRecipes[i]))
						{
							newIndex++;
							continue;
						}
						removed.Add(i);
					}

					// Remove panels
					for (int i = removed.Count - 1; i >= 0; --i)
					{
						var panel = recipePanels[removed[i]];
						this.Controls.Remove(panel);
						recipePanels.Remove(panel);
						panel.Dispose();
					}

					// Update the rest
					for (int i = 0; i < Current.Character.recipes.Count; ++i)
						recipePanels[i].ResetRecipeReference(Current.Character.recipes[i]);

					return true;
				}

				return false;
			}
			finally
			{
				RefreshParameterVisibility();
				RefreshFlexibleTextBoxes();
				RefreshLayout();

				for (int i = 0; i < recipePanels.Count; ++i)
				{
					recipePanels[i].TabIndex = i;
					recipePanels[i].RefreshTitle(); // Greeting titles depend on order and count
				}


				this.Resume();

				RichTextBoxEx.AllowSyntaxHighlighting = true;
				RefreshSyntaxHighlighting(false);
			}
		}

		public bool RefreshReferencesUnordered()
		{
			if (Current.Character.recipes.Count != recipePanels.Count)
				return false;

			var oldRecipes = recipePanels.Select(r => r.recipe).ToList();
			var newRecipes = Current.Character.recipes;

			var order = new Dictionary<int, int>();
			var taken = new HashSet<Recipe>();
			for (int i = 0; i < oldRecipes.Count; ++i)
			{
				var oldRecipe = oldRecipes[i];
				int index = newRecipes.FindIndex(r => r.CompareTo(oldRecipe) && taken.Contains(r) == false);
				if (index == -1)
					return false;

				order.Add(i, index);
				taken.Add(newRecipes[index]);
			}

			// Reorder
			var reordered = new List<RecipePanel>();
			foreach (var kvp in order.OrderBy(kvp => kvp.Value))
			{
				int from = kvp.Key;
				int to = kvp.Value;
				reordered.Add(recipePanels[from]);
			}
			recipePanels = reordered;

			this.Suspend();
			RichTextBoxEx.AllowSyntaxHighlighting = false;

			for (int i = 0; i < recipePanels.Count; ++i)
			{
				recipePanels[i].ResetRecipeReference(Current.Character.recipes[i]);
				recipePanels[i].TabIndex = i;
				recipePanels[i].RefreshTitle(); // Greeting titles depend on order and count
			}

			RefreshParameterVisibility();
			RefreshFlexibleTextBoxes();
			RefreshLayout();
			this.Resume();

			RichTextBoxEx.AllowSyntaxHighlighting = true;
			RefreshSyntaxHighlighting(false);

			Invalidate(false);
			return true;
		}

		public bool ValidateState()
		{
			if (Current.Character.recipes.Count != recipePanels.Count)
				return false;
			for (int i = 0; i < recipePanels.Count; ++i)
			{
				if (Current.Character.recipes[i] != recipePanels[i].recipe)
					return false;
			}
			return true;
		}

		public void Sort()
		{
			recipePanels = recipePanels
				.OrderBy(r => r.recipe.GetSortingOrder())
				.ToList();

			Current.Character.recipes = recipePanels.Select(r => r.recipe).ToList();

			for (int i = 0; i < recipePanels.Count; ++i)
			{
				recipePanels[i].TabIndex = i;
				recipePanels[i].RefreshTitle(); // Greeting titles depend on order and count
			}
			RefreshParameterVisibility();
			RefreshLayout();

			Undo.Push(Undo.Kind.RecipeOrder, "Sort recipes", "sort");
		}

		public void RefreshScrollbar()
		{
			int scrollHeight = 0;
			if (recipePanels.Count > 0)
				scrollHeight = this.VerticalScroll.Value + recipePanels[recipePanels.Count - 1].Location.Y + recipePanels[recipePanels.Count - 1].Size.Height + AutoScrollMargin.Height;
			bool bEnableScrollbar = scrollHeight > this.ClientSize.Height;
			if (bEnableScrollbar != VerticalScroll.Enabled)
			{
				this.AutoScroll = false; // Must be disabled or Enabled does nothing
				this.VerticalScroll.Enabled = bEnableScrollbar;
				this.AutoScroll = true;
				if (!bEnableScrollbar) // Scrollbar was disabled. Reset scroll.
					ScrollToTop();
			}

			AdjustFormScrollbars(true); // Resize
			_bShouldUpdateScrollbars = false;
		}

		public void OnIdle()
		{
			if (_bShouldUpdateLayout)
			{
				_bShouldUpdateLayout = false;
				RefreshLayout();
			}
			else if (_bShouldUpdateScrollbars)
			{
				_bShouldUpdateScrollbars = false;
				RefreshScrollbar();
			}
		}

		private void ResizeRecipePanels()
		{
			this.Suspend();

			int clientWidth = ClientSize.Width;

			// Resize flexible textboxes first
			TextParameterPanelBase.AllowFlexibleHeight = false;
			for (int i = 0; i < recipePanels.Count; ++i)
			{
				var panel = recipePanels[i];
				if (panel.parameterPanels.ContainsNoneOf(pp => pp is IFlexibleParameterPanel))
					continue;

				panel.Size = new Size(clientWidth, panel.Size.Height);
			}
			TextParameterPanelBase.AllowFlexibleHeight = true;
			for (int i = 0; i < recipePanels.Count; ++i)
			{
				var panel = recipePanels[i];
				var flexibleParameters = panel.parameterPanels.OfType<IFlexibleParameterPanel>().ToArray();
				if (flexibleParameters.Length == 0)
					continue;
				foreach (var flexibleParameter in flexibleParameters)
					flexibleParameter.RefreshFlexibleSize();
				panel.RefreshSize();
			}
			
			// Resize the rest
			int panelY = 0;
			for (int i = 0; i < recipePanels.Count; ++i)
			{
				var panel = recipePanels[i];
				panel.Bounds = new Rectangle(-HorizontalScroll.Value, panelY - VerticalScroll.Value, clientWidth, panel.Size.Height);
				panel.RefreshSize();
				panelY += panel.Size.Height;
			}

			this.Resume();

			_bShouldUpdateScrollbars = true;
		}

		public void RefreshLayout()
		{
			int parameterY = 0;
			for (int i = 0; i < recipePanels.Count; ++i)
			{
				var panel = recipePanels[i];
				if (panel.Visible == false)
					continue;
				panel.Location = new Point(-HorizontalScroll.Value, parameterY - VerticalScroll.Value);
				parameterY += panel.Size.Height;
			}

			_bShouldUpdateLayout = false;
			_bShouldUpdateScrollbars = true;
			Invalidate();
		}

		public void RefreshSyntaxHighlighting(bool immediate)
		{
			foreach (var panel in recipePanels)
				panel.RefreshSyntaxHighlighting(immediate);
		}

		public void AddLorebook(Lorebook lorebook)
		{
			if (lorebook == null)
				return;

			if (lorebook.entries.Count >= 40)
				MainForm.SetStatusBarMessage(string.Format("Building lorebook with {0} entries. Please wait...", lorebook.entries.Count));
			else
				MainForm.SetStatusBarMessage("Refreshing recipe list...");
			MainForm.instance.Cursor = Cursors.WaitCursor;

			this.Suspend();
			var instance = Current.AddLorebook(lorebook);

			MainForm.StealFocus();

			RecipePanel pastedPanel = null;

			this.DisableRedrawAndDo(() => {
				RichTextBoxEx.AllowSyntaxHighlighting = false;
				pastedPanel = AddRecipePanel(instance, false);
				RichTextBoxEx.AllowSyntaxHighlighting = true;
				pastedPanel.RefreshSyntaxHighlighting(false);
				RefreshParameterVisibility();
				RefreshLayout();
			});

			this.Resume();
			Invalidate(true);
			Undo.Push(Undo.Kind.RecipeAddRemove, "Add lorebook");

			MainForm.instance.Cursor = Cursors.Default;
			MainForm.ClearStatusBarMessage();
			Current.IsDirty = true;
		}

		public void RefreshTitles()
		{
			foreach (var panel in recipePanels)
				panel.RefreshTitle();
		}

		public void RefreshFlexibleTextBoxes()
		{
			foreach (var panel in recipePanels)
			{
				foreach (var parameter in panel.parameterPanels.OfType<IFlexibleParameterPanel>())
					parameter.RefreshFlexibleSize();
			}
		}

		public void LockAndDo(Action action)
		{
			int vscroll = VerticalScroll.Value;
			this.DisableRedrawAndDo(() => {
				action?.Invoke();

				VerticalScroll.Value = Math.Min(Math.Max(vscroll, VerticalScroll.Minimum), VerticalScroll.Maximum);
				HorizontalScroll.Value = 0;
				AdjustFormScrollbars(true);
				Invalidate(false); // Repaint
			});
		}

	}
}
