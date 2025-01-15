using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Ginger.Properties;
using Ginger.Integration;

namespace Ginger
{
	public partial class RecipePanel : UserControl, IThemedControl
	{
		public event EventHandler OnParameterChanged;
		public event EventHandler OnPanelSizeChanged;
		public event EventHandler OnRemove;
		public event EventHandler OnMoveUp;
		public event EventHandler OnMoveDown;
		public event EventHandler OnMoveToTop;
		public event EventHandler OnMoveToBottom;
		public event EventHandler OnCollapse;
		public event EventHandler OnExpand;
		public event EventHandler OnBake;
		public event EventHandler OnEnable;
		public event EventHandler OnToggleRaw;
		public event EventHandler OnToggleNSFW;
		public event EventHandler<Recipe.DetailLevel> OnChangeDetailLevel;
		public event EventHandler OnCopy;
		public event EventHandler OnPaste;
		public event EventHandler OnSaveAsSnippet;
		public event EventHandler OnSaveAsRecipe;
		public event EventHandler OnSaveLorebook;
		public event EventHandler OnMakePrimaryGreeting;

		public Recipe recipe { get; set; }

		private Color BorderColor { get; set; }
		
		public string Title 
		{
			get { return labelTitle.Text; }
			set { labelTitle.Text = value; }
		}

		public Control parameterContainer { get { return parameters; } }
		public bool Collapsed 
		{ 
			get { return _bCollapsed; } 
			set { SetCollapsed(value); } 
		}

		private bool _bCollapsed = false;

		public bool CanExpand { get { return parameterContainer.Controls.Count > 0; } }

		public IList<IParameterPanel> parameterPanels { get { return _parameterPanels; } }
		private List<IParameterPanel> _parameterPanels = new List<IParameterPanel>();

		public bool InvalidSize { get; set; }

		public RecipePanel()
		{
			InitializeComponent();

			this.DoubleBuffered = true;
			this.SuspendLayout();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			header.Bounds = new Rectangle(0, 0, this.Size.Width, this.header.Size.Height);

			if (parameters.Visible)
			{
				int parameterY = Constants.ParameterPanel.TopMargin;
				int index = 0;
				for (int i = 0; i < parameterPanels.Count; ++i)
				{
					var parameterPanel = parameterPanels[i];
					if (parameterPanel.isActive == false)
						continue;
					if (index++ > 0)
						parameterY += Constants.ParameterPanel.Spacing;
					var panel = parameterPanel as Control;
					panel.Bounds = new Rectangle(0, parameterY, this.Size.Width, parameterPanel.GetParameterHeight());
					parameterY += parameterPanel.GetParameterHeight();
				}

				parameters.Bounds = new Rectangle(0, this.header.Size.Height + 1, this.Size.Width, parameterY + Constants.RecipePanel.BottomMargin);
			}
		}

		public void SetColor(Color color)
		{
			const int kDarken = 20;
			var darkColor = Color.FromArgb(Math.Max(color.R - kDarken, 0), Math.Max(color.G - kDarken, 0), Math.Max(color.B - kDarken, 0));
			
			if (Theme.IsDarkModeEnabled)
			{
				darkColor = Utility.GetDarkColor(color, 0.30f);
				color = Utility.GetDarkColor(color, 0.65f);
				color = Utility.BoostSaturation(color);
			}

			var contrastColor = Utility.GetContrastColor(color, true);
			labelTitle.ForeColor = Utility.GetContrastColor(color, false);

			header.BackColor = color;
			header.ForeColor = contrastColor;
			parameters.BackColor = darkColor;
			parameters.ForeColor = contrastColor;

			BorderColor = Utility.GetDarkerColor(darkColor, 0.25f);
		}

		public bool Collapse()
		{
			if (CanExpand == false)
				btnExpand.Image = null; // Properties.Resources.arrow_none;
			else
				btnExpand.Image = Properties.Resources.collapsed;

			Collapsed = true;
			recipe.isCollapsed = true;
			return true;
		}

		public bool Expand()
		{
			if (CanExpand == false)
			{
				Collapse();
				return false;
			}

			btnExpand.Image = Properties.Resources.expanded;
			Collapsed = false;
			recipe.isCollapsed = false;
			return true;
		}

		public void InitRecipe(Recipe recipe)
		{
			this.recipe = recipe;

			MainForm.SuspendGeneration();

			TextParameterPanelBase.AllowFlexibleHeight = false; // Don't resize during instantiation / layout
			LorebookEntryPanel.AllowFlexibleHeight = false;

			List<IParameterPanel> addedPanels = new List<IParameterPanel>();

			foreach (var parameter in recipe.parameters)
			{
				IParameterPanel panel;
				if (parameter is TextParameter)
				{
					var textParameter = parameter as TextParameter;
					switch (textParameter.mode)
					{
					default:
					case TextParameter.Mode.Single:
						panel = CreateParameterPanel(new TextParameterPanel(), textParameter);
						break;
					case TextParameter.Mode.Short:
						panel = CreateParameterPanel(new MultiTextParameterPanel(MultiTextParameterPanel.TextBoxSize.Short), textParameter);
						break;
					case TextParameter.Mode.Flexible:
						panel = CreateParameterPanel(new MultiTextParameterPanel(MultiTextParameterPanel.TextBoxSize.Short) { FlexibleHeight = true }, textParameter);
						break;
					case TextParameter.Mode.Brief:
						panel = CreateParameterPanel(new MultiTextParameterPanel(MultiTextParameterPanel.TextBoxSize.Brief), textParameter);
						break;
					case TextParameter.Mode.Component:
						panel = CreateParameterPanel(new MultiTextParameterPanel(MultiTextParameterPanel.TextBoxSize.Component) { FlexibleHeight = true }, textParameter);
						break;
					case TextParameter.Mode.Code:
						panel = CreateParameterPanel(new CodeParameterPanel(), textParameter);
						break;
					case TextParameter.Mode.Chat:
						panel = CreateParameterPanel(new ChatParameterPanel() { FlexibleHeight = true }, textParameter);
						break;
					}
				}
				else if (parameter is BooleanParameter)
				{
					panel = CreateParameterPanel(new ToggleParameterPanel(), parameter as BooleanParameter);
				}
				else if (parameter is NumberParameter)
				{
					panel = CreateParameterPanel(new NumberParameterPanel(), parameter as NumberParameter);
				}
				else if (parameter is RangeParameter)
				{
					panel = CreateParameterPanel(new SliderParameterPanel(), parameter as RangeParameter);
				}
				else if (parameter is MeasurementParameter)
				{
					panel = CreateParameterPanel(new MeasurementParameterPanel(), parameter as MeasurementParameter);
				}
				else if (parameter is ChoiceParameter)
				{
					switch ((parameter as ChoiceParameter).style)
					{
					default:
					case ChoiceParameter.Style.List:
						panel = CreateParameterPanel(new ChoiceParameterPanel(), parameter as ChoiceParameter);
						break;
					case ChoiceParameter.Style.Radio:
						panel = CreateParameterPanel(new RadioParameterPanel(), parameter as ChoiceParameter);
						break;
					case ChoiceParameter.Style.Multiple:
						panel = CreateParameterPanel(new MultiChoiceParameterPanel(), parameter as MultiChoiceParameter);
						break;
					case ChoiceParameter.Style.Slider:
						panel = CreateParameterPanel(new ChoiceSliderParameterPanel(), parameter as ChoiceParameter);
						break;
					}
				}
				else if (parameter is MultiChoiceParameter)
				{
					panel = CreateParameterPanel(new MultiChoiceParameterPanel(), parameter as MultiChoiceParameter);
				}
				else if (parameter is ListParameter)
				{
					panel = CreateParameterPanel(new ListParameterPanel(), parameter as ListParameter);
				}
				else if (parameter is HintParameter)
				{
					panel = CreateParameterPanel(new HintParameterPanel(), parameter as HintParameter);
				}
				else if (parameter is LorebookParameter)
				{
					panel = CreateParameterPanel(new LoreBookParameterPanel(), parameter as LorebookParameter);
					(panel as LoreBookParameterPanel).EntriesChanged += OnLoreEntriesChanged;
				}
				else
					continue;

				_parameterPanels.Add(panel);
				addedPanels.Add(panel);
			}

			parameterContainer.Controls.AddRange(addedPanels.Reverse<IParameterPanel>().OfType<Control>().ToArray());

			foreach (var panel in addedPanels)
				panel.RefreshValue(); // Set value

			TextParameterPanelBase.AllowFlexibleHeight = true;
			LorebookEntryPanel.AllowFlexibleHeight = true;

			foreach (var panel in addedPanels.OfType<IFlexibleParameterPanel>())
				panel.RefreshFlexibleSize();

			RefreshParameterLayout();

			for (int i = 0; i < parameterPanels.Count; ++i)
				(parameterPanels[i] as Control).TabIndex = i;

			ApplyVisualTheme();

//			RefreshTitle();
			toolTip.SetToolTip(labelTitle, recipe.GetTooltip());
			btnUp.Visible = !recipe.isBase;
			btnDown.Visible = !recipe.isBase;
			SetEnabled(recipe.isEnabled);


			MainForm.ResumeGeneration();
		}

		public void ResetRecipeReference(Recipe recipe)
		{
			this.recipe = recipe;

			// Reset parameter references
			var panelParameters = recipe.parameters.Where(p => !(p is IInvisibleParameter)).ToArray();
			for (int i = _parameterPanels.Count - 1; i >= 0; --i)
				_parameterPanels[i].ResetParameterReference(panelParameters[i]);

			if (recipe.isEnabled != parameterContainer.Enabled)
				SetEnabled(recipe.isEnabled);

			if (recipe.isCollapsed && !Collapsed)
				Collapse();
			else if (!recipe.isCollapsed && Collapsed)
				Expand();
		}

		private void OnLoreEntriesChanged(object sender, EventArgs e)
		{
			RefreshParameterLayout();
		}

		private ParameterPanel<T> CreateParameterPanel<T>(ParameterPanel<T> panel, T parameter) where T : class, IParameter
		{
			panel.Name = parameter.id.ToString();
			panel.Size = new Size(parameterContainer.Width - 2, panel.Height);
			panel.Font = this.Font;
			panel.ParameterValueChanged += OnParameterValueChanged;
			panel.ParameterEnabledChanged += OnParameterEnabledChanged;
			panel.ParameterResized += OnParameterResized;
			panel.OnRightClick += OnRightClick;
			panel.SetParameter(parameter);
			return panel;
		}

		private void OnParameterValueChanged(object sender, ParameterEventArgs e)
		{
			var panel = sender as IParameterPanel;
			if (panel != null)
			{
				// Push undo
				StringHandle id = string.Format("parameter_{0}_{1}_{2}", 
					recipe.instanceID.ToString(), 
					panel.GetParameter().id,
					e.subId.ToString() ?? "");
				Undo.Push(Undo.Kind.Parameter, "Change parameter", id);
			}

			// Refresh parameter visibility
			OnParameterChanged?.Invoke(sender, e);
		}

		private void OnParameterEnabledChanged(object sender, EventArgs e)
		{
			var panel = sender as IParameterPanel;
			if (panel != null)
			{
				StringHandle id = string.Format("enable_parameter_{0}_{1}",
					recipe.instanceID.ToString(),
					panel.GetParameter().id);
				Undo.Push(Undo.Kind.Parameter, "Toggled parameter", id);
			}

			OnParameterChanged?.Invoke(sender, e);
		}

		private void BtnExpand_Click(object sender, EventArgs e)
		{
			MainForm.EnableFormLevelDoubleBuffering(true);
			if (Collapsed)
			{
				if (Expand())
				{
					MainForm.StealFocus();

					OnExpand?.Invoke(this, EventArgs.Empty);
					Undo.Push(Undo.Kind.Parameter, "Expand recipe");
				}
			}
			else
			{
				if (Collapse())
				{
					MainForm.StealFocus();

					OnCollapse?.Invoke(this, EventArgs.Empty);
					Undo.Push(Undo.Kind.Parameter, "Collapse recipe");
				}
			}
		}

		private void OnClickHeader(object sender, MouseEventArgs args)
		{
			if (args.Button == MouseButtons.Left)
				BtnExpand_Click(sender, args);
			else if (args.Button == MouseButtons.Right)
				ShowContextMenu(sender as Control, new Point(args.X, args.Y));
		}

		private void ShowContextMenu(Control control, Point location)
		{
			ContextMenuStrip menu = new ContextMenuStrip();

			menu.Items.Add(new ToolStripMenuItem("Enabled", null, (s, e) => {
				CommitChange();
				OnEnable?.Invoke(this, EventArgs.Empty);
			}) {
				Checked = recipe.isEnabled,
			});

			var optionsMenu = new ToolStripMenuItem("Options");

			if (recipe.isComponent) // Components
			{
				optionsMenu.DropDownItems.Add(new ToolStripMenuItem("Format text", null, (s, e) => {
					CommitChange();
					OnToggleRaw?.Invoke(this, EventArgs.Empty);
				}) {
					Checked = recipe.enableTextFormatting,
					Enabled = recipe.canToggleTextFormatting,
					ToolTipText = Resources.tooltip_recipe_formatting,
				});
			}
			else // Recipes
			{
				if (recipe.flags.Contains(Constants.Flag.LevelOfDetail))
				{
					var lodMenu = new ToolStripMenuItem("Detail level");
					if (recipe.flags.Contains(Constants.Flag.LevelOfDetail))
					{
						lodMenu.DropDownItems.Add(new ToolStripMenuItem("(Default)", null, (s, e) => {
							CommitChange();
							OnChangeDetailLevel?.Invoke(this, Recipe.DetailLevel.Default);
						}) {
							Checked = recipe.levelOfDetail == Recipe.DetailLevel.Default,
							ToolTipText = "Use the default detail setting for this recipe.",
						});

						lodMenu.DropDownItems.Add(new ToolStripSeparator());

						lodMenu.DropDownItems.Add(new ToolStripMenuItem("Less detail", null, (s, e) => {
							CommitChange();
							OnChangeDetailLevel?.Invoke(this, Recipe.DetailLevel.Less);
						}) {
							Checked = recipe.levelOfDetail == Recipe.DetailLevel.Less,
						});

						lodMenu.DropDownItems.Add(new ToolStripMenuItem("Normal detail", null, (s, e) => {
							CommitChange();
							OnChangeDetailLevel?.Invoke(this, Recipe.DetailLevel.Normal);
						}) {
							Checked = recipe.levelOfDetail == Recipe.DetailLevel.Normal,
						});

						lodMenu.DropDownItems.Add(new ToolStripMenuItem("More detail", null, (s, e) => {
							CommitChange();
							OnChangeDetailLevel?.Invoke(this, Recipe.DetailLevel.More);
						}) {
							Checked = recipe.levelOfDetail == Recipe.DetailLevel.More,
						});
					}
					else
						lodMenu.Enabled = false;
					optionsMenu.DropDownItems.Add(lodMenu);
				}

				if (AppSettings.Settings.AllowNSFW)
				{
					if (optionsMenu.DropDownItems.Count > 0)
						optionsMenu.DropDownItems.Add(new ToolStripSeparator());

					bool bCanToggleNSFW = recipe.flags.Contains(Constants.Flag.ToggleNSFW);
					optionsMenu.DropDownItems.Add(new ToolStripMenuItem("Allow NSFW content", null, (s, e) => {
						CommitChange();
						OnToggleNSFW?.Invoke(this, EventArgs.Empty);
					}) {
						Checked = (bCanToggleNSFW && recipe.enableNSFWContent) || (!bCanToggleNSFW && recipe.flags.Contains(Constants.Flag.NSFW)),
						Enabled = bCanToggleNSFW,
						ToolTipText = "Allow this recipe to include explicit text in its output.",
					});
				}
				
			}
			optionsMenu.Enabled = optionsMenu.DropDownItems.Count > 0;

			menu.Items.Add(optionsMenu);

			menu.Items.Add("-");

			if (recipe.isLorebook && recipe.parameters.Count == 1 && recipe.parameters[0] is LorebookParameter)
			{
				var lorebookParameter = recipe.parameters[0] as LorebookParameter;
				bool isEmpty = lorebookParameter.value.isEmpty;

				menu.Items.Add(new ToolStripMenuItem("Save lorebook...", null, (s, e) => {
					OnSaveLorebook?.Invoke(this, EventArgs.Empty);
				}) {
					Enabled = !isEmpty,
				});

				var lorebookPanel = parameterPanels[0] as LoreBookParameterPanel;
				if (lorebookPanel != null)
				{
					var sortMenu = new ToolStripMenuItem("Sort all entries");
					sortMenu.DropDownItems.Add(new ToolStripMenuItem("By key", null, (s, e) => {
						lorebookPanel.Sort(Lorebook.Sorting.ByKey);
					}) { Enabled = !isEmpty });
					sortMenu.DropDownItems.Add(new ToolStripMenuItem("By order", null, (s, e) => {
						lorebookPanel.Sort(Lorebook.Sorting.ByOrder);
					}) { Enabled = !isEmpty });
					sortMenu.DropDownItems.Add(new ToolStripMenuItem("By creation", null, (s, e) => {
						lorebookPanel.Sort(Lorebook.Sorting.ByIndex);
					}) { Enabled = !isEmpty });
					menu.Items.Add(sortMenu);

					var resetOrderMenu = new ToolStripMenuItem("Set order (all)");
					menu.Items.Add(resetOrderMenu);
					resetOrderMenu.DropDownItems.Add(new ToolStripMenuItem("Default", null, (s, e) => { 
						CommitChange(); 
						lorebookPanel.OnResetOrder(s, new LorebookEntryPanel.ResetOrderEventArgs() { Ordering = LorebookEntryPanel.ResetOrderEventArgs.Order.Default }); 
					}) {
						Enabled = lorebookPanel.lorebook.entries.Count > 0,
					});
					resetOrderMenu.DropDownItems.Add(new ToolStripMenuItem("Zero", null, (s, e) => { 
						CommitChange(); 
						lorebookPanel.OnResetOrder(s, new LorebookEntryPanel.ResetOrderEventArgs() { Ordering = LorebookEntryPanel.ResetOrderEventArgs.Order.Zero }); 
					}) {
						Enabled = lorebookPanel.lorebook.entries.Count > 0,
					});
					resetOrderMenu.DropDownItems.Add(new ToolStripMenuItem("One hundred", null, (s, e) => { 
						CommitChange(); 
						lorebookPanel.OnResetOrder(s, new LorebookEntryPanel.ResetOrderEventArgs() { Ordering = LorebookEntryPanel.ResetOrderEventArgs.Order.OneHundred }); 
					}) {
						Enabled = lorebookPanel.lorebook.entries.Count > 0,
					});
					resetOrderMenu.DropDownItems.Add(new ToolStripMenuItem("By row", null, (s, e) => { 
						CommitChange(); 
						lorebookPanel.OnResetOrder(s, new LorebookEntryPanel.ResetOrderEventArgs() { Ordering = LorebookEntryPanel.ResetOrderEventArgs.Order.ByRow }); 
					}) {
						Enabled = lorebookPanel.lorebook.entries.Count > 0,
					});
					menu.Items.Add(new ToolStripMenuItem("Rearrange lore", null, (s, e) => {
						CommitChange();
						MainForm.instance.rearrangeLoreMenuItem_Click(s, e);
					}) {
						Checked = AppSettings.Settings.EnableRearrangeLoreMode,
						ToolTipText = Resources.tooltip_rearrange_lore,
					});

					int numEntries = lorebookPanel.lorebook.entries.Count;
					if (numEntries > AppSettings.Settings.LoreEntriesPerPage)
					{
						menu.Items.Add(new ToolStripSeparator());
						int pageIndex = (lorebookPanel.GetParameter() as LorebookParameter).pageIndex;
						menu.Items.Add(new ToolStripMenuItem("Next page", null, (s, e) => { CommitChange(); lorebookPanel.OnNextPage(s, e); }) {
							Enabled = pageIndex < numEntries / AppSettings.Settings.LoreEntriesPerPage,
						});
						menu.Items.Add(new ToolStripMenuItem("Previous page", null, (s, e) => { CommitChange(); lorebookPanel.OnPreviousPage(s, e); }) {
							Enabled = pageIndex > 0,
						});
					}

				}

			}
			else
			{
				menu.Items.Add(new ToolStripMenuItem("Save as snippet...", null, (s, e) => {
					OnSaveAsSnippet?.Invoke(this, EventArgs.Empty);
				}) {
					Enabled = recipe.isEnabled && recipe.flags.Contains(Constants.Flag.DontBake) == false,
					ToolTipText = Resources.tooltip_recipe_create_snippet,
				});

				menu.Items.Add(new ToolStripMenuItem("Save as new recipe...", null, (s, e) => {
					OnSaveAsRecipe?.Invoke(this, EventArgs.Empty);
				}) {
					Enabled = recipe.isEnabled && recipe.flags.Contains(Constants.Flag.DontBake) == false && parameterPanels.Count > 0, // Should check for visible parameters here, really.
					ToolTipText = Resources.tooltip_recipe_create_recipe,
				});

				menu.Items.Add(new ToolStripMenuItem("Bake", null, (s, e) => {
					OnBake?.Invoke(this, EventArgs.Empty);
				}) {
					Enabled = recipe.isEnabled && recipe.canBake,
					ToolTipText = Resources.tooltip_recipe_bake,
				});
			}

			menu.Items.Add("-");

			if (recipe.isGreeting)
			{
				var greetings = Current.Character.recipes.Where(r => r.isEnabled && r.isGreeting).ToList();
				int index = greetings.IndexOf(recipe);
				if (greetings.Count > 1)
				{
					menu.Items.Add(new ToolStripMenuItem("Make primary greeting", null, (s, e) => {
						CommitChange();
						OnMakePrimaryGreeting?.Invoke(this, EventArgs.Empty);
					}) {
						Enabled = index != 0,
					});
					menu.Items.Add("-");
				}
			}

			menu.Items.Add(new ToolStripMenuItem("Copy", null, (s, e) => { OnCopy?.Invoke(this, e); }));
			menu.Items.Add(new ToolStripMenuItem("Paste", null, (s, e) => { OnPaste?.Invoke(this, e); }) {
				Enabled = Clipboard.ContainsData(RecipeClipboard.Format) 
					|| Clipboard.ContainsData(LoreClipboard.Format)
					|| Clipboard.ContainsData(ChatClipboard.Format)
					|| Clipboard.ContainsData(ChatStagingClipboard.Format),
			});

			menu.Items.Add("-");

			if (Collapsed)
			{
				menu.Items.Add(new ToolStripMenuItem("Expand", null, (s, e) => {
					CommitChange();
					if (Expand())
					{
						OnExpand?.Invoke(this, EventArgs.Empty);
						Undo.Push(Undo.Kind.Parameter, "Expand recipe");
					}
				}) {
					Enabled = CanExpand,
				}); ;
			}
			else
			{
				menu.Items.Add(new ToolStripMenuItem("Collapse", null, (s, e) => {
					MainForm.StealFocus();

					if (Collapse())
					{
						CommitChange();
						OnCollapse?.Invoke(this, EventArgs.Empty);
						Undo.Push(Undo.Kind.Parameter, "Collapse recipe");
					}
				}));
			}
			menu.Items.Add(new ToolStripMenuItem("Move up", null, (s, e) => { CommitChange(); OnMoveUp?.Invoke(this, e); }) {
				Enabled = recipe.isBase == false,
			});
			menu.Items.Add(new ToolStripMenuItem("Move down", null, (s, e) => { CommitChange(); OnMoveDown?.Invoke(this, e); }) {
				Enabled = recipe.isBase == false,
			});
			menu.Items.Add(new ToolStripMenuItem("Move to top", null, (s, e) => { CommitChange(); OnMoveToTop?.Invoke(this, e); }) {
				Enabled = recipe.isBase == false,
			});
			menu.Items.Add(new ToolStripMenuItem("Move to bottom", null, (s, e) => { CommitChange(); OnMoveToBottom?.Invoke(this, e); }) {
				Enabled = recipe.isBase == false,
			});

			menu.Items.Add("-");
			menu.Items.Add(new ToolStripMenuItem("Remove", null, (s, e) => { OnRemove?.Invoke(this, e); }));

			if (recipe.isExternal && !recipe.isComponent)
			{
				menu.Items.Add("-");
				var similar = RecipeBook.GetSimilarRecipe(recipe);
				if (similar != null)
				{
					menu.Items.Add(new ToolStripMenuItem("Reload recipe", null, (s, e) => {
						MainForm.instance.ReplaceExternalRecipe(recipe, similar);
					}) {
						ToolTipText = Resources.tooltip_recipe_load_local,
					});
				}

				menu.Items.Add(new ToolStripMenuItem("Save recipe...", null, (s, e) => {
					MainForm.instance.ImportExternalRecipe(recipe);
				}) {
					Enabled = RecipeBook.GetRecipeByUID(recipe.uid) == null,
					ToolTipText = Resources.tooltip_recipe_save_local,
				});
			}
			else if (!recipe.isExternal && string.IsNullOrEmpty(recipe.filename) == false)
			{
				menu.Items.Add("-");
				menu.Items.Add(new ToolStripMenuItem("Edit source...", null, (s, e) => { MainForm.EditRecipeSource(recipe); }));
			}

			Theme.Apply(menu);
			menu.Show(control, location);
		}

		public void RefreshAllParameters()
		{
			for (int i = _parameterPanels.Count - 1; i >= 0; --i)
				_parameterPanels[i].RefreshValue();
		}

		public bool RefreshParameterVisibility()
		{
			if (recipe.parameters.IsEmpty())
				return false;

			bool bChanged = false;

			var recipes = Current.Character.recipes.ToArray();
			int recipeIdx = Array.IndexOf(recipes, this.recipe);
			if (recipeIdx == -1)
				return false;

			Context outerContext = Current.Character.GetContext(CharacterData.ContextType.None);
			ParameterStates parameterStates = ParameterResolver.ResolveParameters(recipes, outerContext);

			// Resolve parameters
			var parameterState = parameterStates[recipeIdx];

			foreach (var parameter in recipe.parameters.OrderByDescending(p => p.isImmediate))
			{
				var parameterPanel = _parameterPanels.Find(p => p.GetParameter() == parameter);
				if (parameterPanel == null)
					continue;

				if (parameter.isGlobal) // Is shared (reserve?)
				{
					StringHandle uid;
					string reservedValue = default(string);
					bool isReserved = parameterStates.TryGetReservedValue(parameter.id, out uid, out reservedValue)
						&& uid != parameter.uid;
					parameterPanel.SetReserved(isReserved, reservedValue);

					if (isReserved)
						continue;
				}

				if (parameter.isConditional && parameterPanel.isReserved == false) // Has condition
				{
					bool wasVisible = parameterPanel.isActive;
					parameterPanel.isActive = parameterState.IsInactive(parameter.uid) == false;
					bChanged |= wasVisible != parameterPanel.isActive;
				}
								
				// Apply parameter to context
				parameter.Apply(parameterState);
			}

			if (bChanged)
			{
				RefreshParameterLayout();
				OnPanelSizeChanged?.Invoke(this, EventArgs.Empty);
				return true;
			}
			return false;
		}

		private void BtnDown_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && ModifierKeys == Keys.Shift)
			{
				CommitChange();
				OnMoveToBottom?.Invoke(this, e);
			}
			else if (e.Button == MouseButtons.Left)
			{
				CommitChange();
				OnMoveDown?.Invoke(this, e);
			}
		}

		private void BtnUp_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && ModifierKeys == Keys.Shift)
			{
				CommitChange();
				OnMoveToTop?.Invoke(this, e);
			}
			else if (e.Button == MouseButtons.Left)
			{
				CommitChange();
				OnMoveUp?.Invoke(this, e);
			}
		}

		private void Btn_Remove_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				CommitChange();
				OnRemove?.Invoke(this, e);
			}
		}

		private void OnRightClick(object sender, MouseEventArgs args)
		{
			ShowContextMenu(sender as Control, new Point(args.X, args.Y));
		}

		public void SetEnabled(bool enabled)
		{
			parameterContainer.Enabled = enabled;

			if (enabled)
			{
				SetColor(recipe.color);
			}
			else // Disabled
			{
				SetColor(Color.Gainsboro);
				if (Theme.IsDarkModeEnabled)
					labelTitle.ForeColor = Color.Black;
				else
					labelTitle.ForeColor = Color.Gray;
			}
			TabStop = !Collapsed && recipe.isEnabled;
			RefreshTitle();
		}

		public void RefreshTitle()
		{
			// Greeting(s)
			if (recipe.isGreeting && recipe.isEnabled)
			{
				var greetings = Current.Character.recipes.Where(r => r.isEnabled && r.isGreeting).ToList();
				int index = greetings.IndexOf(recipe);
				if (index == 0)
				{
					if (greetings.Count == 1)
						Title = recipe.GetTitle();
					else
						Title = string.Concat(recipe.GetTitle(), " (Primary)");
				}
				else if (index > 0)
				{
					if (greetings.Count == 2)
						Title = string.Concat(recipe.GetTitle(), " (Alternate)");
					else
						Title = string.Format("{0} (Alternate #{1})", recipe.GetTitle(), index);
				}
			}
			else
				Title = recipe.GetTitle();

			if (recipe.isExternal && !recipe.isComponent)
				Title += "*";
			if (recipe.isEnabled == false)
				Title += " (Disabled)";
#if false
			Title += string.Format(" (0x{0:X8})", recipe.uid);
#endif
		}

		public void RefreshLoreTokenCounts(Dictionary<string, int> loreTokens)
		{
			for (int i = _parameterPanels.Count - 1; i >= 0; --i)
			{
				if (_parameterPanels[i] is LoreBookParameterPanel)
					(_parameterPanels[i] as LoreBookParameterPanel).RefreshLoreTokenCounts(loreTokens);
			}
		}

		private void OnParameterResized(object sender, EventArgs e)
		{
			RefreshSize();
			OnPanelSizeChanged?.Invoke(this, EventArgs.Empty);
		}

		public void RefreshSize()
		{
			RefreshParameterLayout(true);
		}

		private void SetCollapsed(bool collapsed)
		{
			_bCollapsed = collapsed;
			TabStop = !collapsed && recipe.isEnabled;

			MainForm.LockRecipeList();
			if (collapsed)
			{
				parameters.Visible = false;
				this.Size = new Size(this.Size.Width, header.Size.Height + 1);
			}
			else
			{
				header.Visible = true;
				parameters.Visible = true;
				this.Size = new Size(this.Size.Width, parameters.Location.Y + parameters.Size.Height + 1);
			}
			MainForm.ReleaseRecipeList();
		}

		public void RefreshParameterLayout(bool bResize = true)
		{
			int parameterY = Constants.ParameterPanel.TopMargin;
			int index = 0;
			for (int i = 0; i < parameterPanels.Count; ++i)
			{
				var parameterPanel = parameterPanels[i];
				if (parameterPanel.isActive == false)
					continue;
				if (index++ > 0) // Spacing
					parameterY += Constants.ParameterPanel.Spacing;
				var panel = parameterPanel as Control;
				panel.Bounds = new Rectangle(0, parameterY, this.Size.Width, parameterPanel.GetParameterHeight());
				parameterY += parameterPanel.GetParameterHeight();
			}
			parameters.Bounds = new Rectangle(0, this.header.Size.Height + 1, this.Size.Width, parameterY + Constants.RecipePanel.BottomMargin);

			if (bResize && Collapsed == false)
				this.Size = new Size(this.Size.Width, parameters.Location.Y + parameters.Size.Height + 1);
		}

		public void CommitChange()
		{
			if (ActiveControl != null)
			{
				var focused = MainForm.instance.GetFocusedControl();
				if (focused != null)
					focused.KillFocus(); // Send WM_KILLFOCUS, which triggers the value to be saved, if changed.
			}
		}

		private Win32.PAINTSTRUCT _ps;

		protected override void WndProc(ref System.Windows.Forms.Message m)
		{
			IntPtr hdc = IntPtr.Zero;

			if (m.Msg == Win32.WM_PAINT)
			{
				hdc = Win32.BeginPaint(Handle, out _ps);
			}

			base.WndProc(ref m);

			if (hdc != IntPtr.Zero)
			{
				using (Graphics graphic = Graphics.FromHwnd(Handle))
				{
					OnPaint(new PaintEventArgs(graphic, _ps.rcPaint.ToRectangle()));
				}

				Win32.EndPaint(Handle, ref _ps);
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			int mid = header.Size.Height;
			int bottom = this.Size.Height - 1;
			using (var pen = new Pen(BorderColor))
			{
				e.Graphics.DrawLine(pen, new Point(0, mid), new Point(Width - 1, mid));
				e.Graphics.DrawLine(pen, new Point(0, bottom), new Point(Width - 1, bottom));
			}
		}

		public void RefreshSyntaxHighlighting(bool immediate)
		{
			foreach (var parameter in parameterPanels.OfType<ISyntaxHighlighted>())
				parameter.RefreshSyntaxHighlight(immediate);
		}

		public void ApplyVisualTheme()
		{
			if (Enabled == false || recipe.isEnabled == false)
			{
				SetColor(Color.Gainsboro);
				if (Theme.IsDarkModeEnabled)
					labelTitle.ForeColor = Color.Black;
				else
					labelTitle.ForeColor = Color.Gray;
				return;
			}

			SetColor(recipe.color);

			var textBoxes = parameterContainer.FindAllControlsOfType<TextBoxBase>();
			foreach (var control in textBoxes)
				Theme.Apply(control);

			var comboBoxes = parameterContainer.FindAllControlsOfType<ComboBox>();
			foreach (var control in comboBoxes)
				Theme.Apply(control);

			var themedControls = parameterContainer.FindAllControlsOfType<Control>().OfType<IThemedControl>();
			foreach (var control in themedControls)
				control.ApplyVisualTheme();
		}
	}
}
