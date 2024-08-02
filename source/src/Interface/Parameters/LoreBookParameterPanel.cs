using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Ginger
{
	public partial class LoreBookParameterPanel : LoreBookParameterPanelDummy, ISyntaxHighlighted, ISearchableContainer, IFlexibleParameterPanel
	{
		protected override CheckBox parameterCheckBox { get { return null; } }
		protected override Label parameterLabel { get { return null; } }

		public Lorebook lorebook { get { return parameter.value; } }
		private List<LorebookEntryPanel> _entryPanels = new List<LorebookEntryPanel>();

		public event EventHandler EntriesChanged;

		private static readonly int EntrySpacing = 6;
		private static readonly int ScrollStep = 150; // Scroll only

		private int FirstEntryIndex { get { return parameter.pageIndex * AppSettings.Settings.LoreEntriesPerPage; } }
		private static int EntriesPerPage { get { return AppSettings.Settings.LoreEntriesPerPage; } }
		
		public LoreBookParameterPanel()
		{
			InitializeComponent();

			this.SuspendLayout(); // Manual layout
			centerPanel.SuspendLayout(); // Manual layout
			centerPanel.VerticalScroll.SmallChange = ScrollStep;
			centerPanel.VerticalScroll.LargeChange = ScrollStep * 5;

			pageChanger.PageChanged += PageChanger_PageChanged;
		}

		protected override void OnSetParameter()
		{
			centerPanel.Enabled = parameter.isEnabled || !parameter.isOptional;
		}

		protected override void OnSetEnabled(bool bEnabled)
		{
			centerPanel.Enabled = bEnabled;
		}

		private LorebookEntryPanel CreateEntryPanel()
		{
			var entryPanel = new LorebookEntryPanel();
			entryPanel.RemoveClicked += OnRemoveEntry;
			entryPanel.OnMoveUp += OnMoveUpEntry;
			entryPanel.OnMoveDown += OnMoveDownEntry;
			entryPanel.OnMoveToTop += OnMoveToTop;
			entryPanel.OnMoveToBottom += OnMoveToBottom;
			entryPanel.Changed += OnChangedEntry;
			entryPanel.OnCopy += OnCopy;
			entryPanel.OnPaste += OnPaste;
			entryPanel.OnInsert += OnInsertAt;
			entryPanel.OnDuplicate += OnDuplicate;
			entryPanel.OnAddEntry += OnAddEntry;
			entryPanel.TextSizeChanged += EntryPanel_TextSizeChanged;
			return entryPanel;
		}

		private void OnAddEntry(object sender, EventArgs e)
		{
			MainForm.EnableFormLevelDoubleBuffering(true);

			var newEntry = new Lorebook.Entry() {
				addition_index = lorebook.GetNextIndex(),
			};
			lorebook.entries.Add(newEntry);

			if (lorebook.entries.Count - FirstEntryIndex <= EntriesPerPage)
			{
				LorebookEntryPanel entryPanel = null;
				this.DisableRedrawAndDo(() => {
					entryPanel = CreateEntryPanel();
					entryPanel.SetContent(newEntry);

					centerPanel.Controls.Add(entryPanel);
					_entryPanels.Add(entryPanel);

					// Tab order
					for (int i = 0; i < _entryPanels.Count; ++i)
						_entryPanels[i].TabIndex = i;

					centerPanel.Invalidate();

					ResizeCenterPanel();
					RefreshLayout();
					centerPanel.ScrollControlIntoView(_entryPanels[_entryPanels.Count - 1]);
				});
				entryPanel.textBox_Keys.Focus();
			}
			else
			{
				ChangePage(1 + lorebook.entries.Count / EntriesPerPage);

				var entryPanel = _entryPanels[_entryPanels.Count - 1];
				entryPanel.textBox_Keys.Focus();
			}


			Undo.Suspend();
//			EntriesChanged?.Invoke(this, EventArgs.Empty);
			NotifyValueChanged(string.Format("entry-{0}-{1}", _entryPanels.Count - 1, _entryPanels.Count));
			Undo.Resume();
			Undo.Push(Undo.Kind.Parameter, "Add lore entry");
		}

		private void OnChangedEntry(object sender, LorebookEntryPanel.LorebookChangedEventArgs e)
		{
			if (isIgnoringEvents)
				return;

			LorebookEntryPanel panel = (LorebookEntryPanel)sender;
			int index = lorebook.entries.IndexOf(panel.lorebookEntry);
			if (index == -1)
				return;

			if (e.Keys != null)
				lorebook.entries[index].keys = e.Keys;
			if (e.Text != null)
				lorebook.entries[index].value = e.Text;
			if (e.SortOrder.HasValue)
				lorebook.entries[index].sortOrder = e.SortOrder.Value;
			lorebook.entries[index].isEnabled = e.Enabled;

			NotifyValueChanged(string.Format("entry-{0}-{1}-{2}", index, _entryPanels.Count, e.Text != null ? e.Text.GetHashCode() : 0));
		}

		private void OnRemoveEntry(object sender, EventArgs e)
		{
			MainForm.StealFocus();

			LorebookEntryPanel panel = (LorebookEntryPanel)sender;
			int index = lorebook.entries.IndexOf(panel.lorebookEntry);
			if (index == -1)
				return;

			Undo.Suspend();
			lorebook.entries.RemoveAt(index);

			if (FirstEntryIndex < lorebook.entries.Count)
			{
				centerPanel.Suspend();
				WhileIgnoringEvents(() => {
					centerPanel.Controls.Remove(panel);
					_entryPanels.Remove(panel);
					panel.Dispose();
				});

				ResizeCenterPanel();
				centerPanel.Resume();
			}
			else
			{
				ChangePage(parameter.pageIndex); // will get clamped
			}

			EntriesChanged?.Invoke(this, EventArgs.Empty);
			NotifyValueChanged(string.Format("entry-{0}-{1}", index, _entryPanels.Count));

			Undo.Resume();
			Undo.Push(Undo.Kind.Parameter, "Remove lore entry");
		}

		private void OnMoveUpEntry(object sender, EventArgs e)
		{
			LorebookEntryPanel panel = (LorebookEntryPanel)sender;
			int index = lorebook.entries.IndexOf(panel.lorebookEntry);
			if (index == -1)
				return;

			if (MoveEntry(index, index - 1))
			{
//				SetDirty(string.Format("entry-{0}-{1}", index, _entryPanels.Count));
				Undo.Push(Undo.Kind.Parameter, "Move lore entry");
			}
		}

		private void OnMoveDownEntry(object sender, EventArgs e)
		{
			LorebookEntryPanel panel = (LorebookEntryPanel)sender;
			int index = lorebook.entries.IndexOf(panel.lorebookEntry);
			if (index == -1)
				return;

			if (MoveEntry(index, index + 1))
			{
//				SetDirty(string.Format("entry-{0}-{1}", index, _entryPanels.Count));
				Undo.Push(Undo.Kind.Parameter, "Move lore entry");
			}
		}

		private void OnMoveToTop(object sender, EventArgs e)
		{
			LorebookEntryPanel panel = (LorebookEntryPanel)sender;
			int index = lorebook.entries.IndexOf(panel.lorebookEntry);
			if (index == -1)
				return;

			if (MoveEntry(index, 0))
			{
//				SetDirty(string.Format("entry-{0}-{1}", index, _entryPanels.Count));
				Undo.Push(Undo.Kind.Parameter, "Move lore entry");
			}
		}

		private void OnMoveToBottom(object sender, EventArgs e)
		{
			LorebookEntryPanel panel = (LorebookEntryPanel)sender;
			int index = lorebook.entries.IndexOf(panel.lorebookEntry);
			if (index == -1)
				return;

			if (MoveEntry(index, lorebook.entries.Count - 1))
			{
//				SetDirty(string.Format("entry-{0}-{1}", index, _entryPanels.Count));
				Undo.Push(Undo.Kind.Parameter, "Move lore entry");
			}
		}

		private bool MoveEntry(int index, int newIndex)
		{
			MainForm.StealFocus();

			if (newIndex < 0)
				newIndex = 0;
			if (newIndex >= lorebook.entries.Count)
				newIndex = lorebook.entries.Count - 1;

			var panelIndex		= index - parameter.pageIndex * EntriesPerPage;
			var newPanelIndex	= newIndex - parameter.pageIndex * EntriesPerPage;

			if (panelIndex < 0 || panelIndex >= _entryPanels.Count)
				return false;

			// Move panel
			var panel = _entryPanels[panelIndex];
			_entryPanels.RemoveAt(panelIndex);
			_entryPanels.Insert(newPanelIndex, panel);

			// Move lore entry
			var loreEntry = lorebook.entries[index];
			lorebook.entries.RemoveAt(index);
			lorebook.entries.Insert(newIndex, loreEntry);

			RefreshLayout();

			centerPanel.ScrollControlIntoView(_entryPanels[newPanelIndex]);
			centerPanel.Invalidate();

			// Tab order
			for (int i = 0; i < _entryPanels.Count; ++i)
				_entryPanels[i].TabIndex = i;

			return true;
		}

		private void EntryPanel_TextSizeChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			ResizeCenterPanel();
			RefreshLayout();
		}

		public void RefreshLoreTokenCounts(Dictionary<string, int> loreTokens)
		{
			// Refresh entries
			foreach (var entry in lorebook.entries)
			{
				int count;
				if (loreTokens != null && loreTokens.TryGetValue(entry.GetUID(), out count))
					entry.tokenCount = count;
				else
					entry.tokenCount = 0;
			}

			// Refresh panels
			for (int i = 0; i < _entryPanels.Count; ++i)
				_entryPanels[i].RefreshTokenCount();
		}

		public ISearchable[] GetSearchables()
		{
			return _entryPanels.SelectMany(p => new ISearchable[] {
				p.textBox_Keys.richTextBox,
				p.textBox_Text.richTextBox,
			}).ToArray();
		}

		private void OnCopyAll(object sender, EventArgs e)
		{
			OnCopy(null, e);
		}

		private void OnCopy(object sender, EventArgs e)
		{
			if (sender != null) // Copy single
			{
				var panel = sender as LorebookEntryPanel;
				var entry = panel.lorebookEntry;
				Clipboard.SetDataObject(LoreClipboard.FromLoreEntries(new Lorebook.Entry[] { entry }), true);
			}
			else // Copy all
			{
				Clipboard.SetDataObject(LoreClipboard.FromLoreEntries(lorebook.entries), true);
			}
		}

		private void OnPaste(object sender, EventArgs e)
		{
			if (Clipboard.ContainsData(LoreClipboard.Format) == false)
				return;

			int insertionIndex = _entryPanels.Count;

			LoreClipboard data = Clipboard.GetData(LoreClipboard.Format) as LoreClipboard;
			if (data == null)
				return;

			List<Lorebook.Entry> entries = data.ToEntries();
			if (entries == null || entries.Count == 0)
				return;

			MainForm.StealFocus();

			centerPanel.Suspend();

			foreach (var entry in entries)
			{
				lorebook.entries.Insert(insertionIndex, entry);

				var entryPanel = CreateEntryPanel();
				_entryPanels.Insert(insertionIndex, entryPanel);
				centerPanel.Controls.Add(entryPanel);

				entryPanel.SetContent(entry);
				entryPanel.textBox_Keys.Focus();

				insertionIndex++;
			}

			ResizeCenterPanel();

			// Tab order
			for (int i = 0; i < _entryPanels.Count; ++i)
				_entryPanels[i].TabIndex = i;

			centerPanel.Resume();

			if (centerPanel.Controls.Count > 0)
				centerPanel.ScrollControlIntoView(centerPanel.Controls[0]);

			Undo.Suspend();
			EntriesChanged?.Invoke(this, EventArgs.Empty);
			NotifyValueChanged(string.Format("entry-{0}-{1}", _entryPanels.Count - 1, _entryPanels.Count));
			Undo.Resume();
			Undo.Push(Undo.Kind.Parameter, "Paste lore");
		}

		private void OnInsertAt(object sender, EventArgs e)
		{
			var panel = sender as LorebookEntryPanel;
			var newEntry = new Lorebook.Entry() {
				addition_index = lorebook.GetNextIndex(),
				sortOrder = panel.lorebookEntry.sortOrder,
			};

			int insertionIndex = lorebook.entries.IndexOf(panel.lorebookEntry);
			if (insertionIndex == -1)
				return;

			int panelInsertionIndex = _entryPanels.IndexOf(panel);

			MainForm.StealFocus();

			centerPanel.Suspend();

			lorebook.entries.Insert(insertionIndex, newEntry);

			var entryPanel = CreateEntryPanel();
			_entryPanels.Insert(panelInsertionIndex, entryPanel);
			centerPanel.Controls.Add(entryPanel);

			entryPanel.SetContent(newEntry);
			entryPanel.textBox_Keys.Focus();

			ResizeCenterPanel();
			RefreshLayout();

			// Tab order
			for (int i = 0; i < _entryPanels.Count; ++i)
			{
				_entryPanels[i].TabIndex = i;
			}
			
			centerPanel.Resume();

			if (centerPanel.Controls.Count > 0)
				centerPanel.ScrollControlIntoView(centerPanel.Controls[0]);

			Undo.Suspend();
			EntriesChanged?.Invoke(this, EventArgs.Empty);
			NotifyValueChanged(string.Format("entry-{0}-{1}", _entryPanels.Count - 1, _entryPanels.Count));
			Undo.Resume();
			Undo.Push(Undo.Kind.Parameter, "Insert lore");
		}

		private void OnDuplicate(object sender, EventArgs e)
		{
			var panel = sender as LorebookEntryPanel;
			var duplicateEntry = panel.lorebookEntry.Clone();
			duplicateEntry.addition_index = lorebook.GetNextIndex();

			int insertionIndex = lorebook.entries.IndexOf(panel.lorebookEntry);
			if (insertionIndex == -1)
				return;

			int panelInsertionIndex = _entryPanels.IndexOf(panel);
			duplicateEntry.key = string.Concat("Copy of ", duplicateEntry.key);

			MainForm.StealFocus();

			centerPanel.Suspend();

			lorebook.entries.Insert(insertionIndex + 1, duplicateEntry);

			var entryPanel = CreateEntryPanel();
			_entryPanels.Insert(panelInsertionIndex + 1, entryPanel);
			centerPanel.Controls.Add(entryPanel);

			entryPanel.SetContent(duplicateEntry);
			entryPanel.textBox_Keys.Focus();

			ResizeCenterPanel();

			// Tab order
			for (int i = 0; i < _entryPanels.Count; ++i)
			{
				_entryPanels[i].TabIndex = i;
			}
			
			centerPanel.Resume();

			if (centerPanel.Controls.Count > 0)
				centerPanel.ScrollControlIntoView(centerPanel.Controls[0]);

			Undo.Suspend();
			EntriesChanged?.Invoke(this, EventArgs.Empty);
			NotifyValueChanged(string.Format("entry-{0}-{1}", _entryPanels.Count - 1, _entryPanels.Count));
			Undo.Resume();
			Undo.Push(Undo.Kind.Parameter, "Duplicate lore");
		}

		protected override void OnRefreshValue()
		{
			int numEntries = parameter.value.entries.Count;
			int entryFrom = parameter.pageIndex * EntriesPerPage;
			int entryTo = Math.Min(entryFrom + EntriesPerPage - 1, numEntries - 1);
			int entriesInPage = (entryTo - entryFrom) + 1;

			// Remove
			while (_entryPanels.Count > entriesInPage)
			{
				var panel = _entryPanels[_entryPanels.Count - 1];
				centerPanel.Controls.Remove(panel);
				_entryPanels.RemoveAt(_entryPanels.Count - 1);
				panel.Dispose();
			}

			bool bSyntaxHighlightingWasEnabled = RichTextBoxEx.AllowSyntaxHighlighting;
			if (bSyntaxHighlightingWasEnabled)
				RichTextBoxEx.AllowSyntaxHighlighting = false;

			// Add
			var addedPanels = new List<LorebookEntryPanel>();
			while (_entryPanels.Count < entriesInPage)
			{
				var entryPanel = CreateEntryPanel();
				_entryPanels.Add(entryPanel);
				addedPanels.Add(entryPanel);
			}
			centerPanel.Controls.AddRange(addedPanels.ToArray());

			for (int i = 0; i < _entryPanels.Count; ++i)
			{
				_entryPanels[i].TabIndex = i;
				_entryPanels[i].SetContent(parameter.value.entries[entryFrom + i]);
			}

			pageChanger.SetPage(parameter.pageIndex + 1, Math.Max(lorebook.entries.Count / EntriesPerPage + 1, 1));
			pageChanger.Visible = lorebook.entries.Count > EntriesPerPage;

			RefreshFlexibleSize();
			ResizeCenterPanel();
			RefreshLayout();

			RichTextBoxEx.AllowSyntaxHighlighting = bSyntaxHighlightingWasEnabled;
			RefreshSyntaxHighlight(true, true);
		}

		public override int GetParameterHeight()
		{
			return centerPanel.Location.Y + centerPanel.Height + bottomPanel.Height;
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			int clientWidth = this.Width - 4;
			centerPanel.Bounds = new Rectangle(0, 0, clientWidth, centerPanel.Height);
			bottomPanel.Bounds = new Rectangle(0, centerPanel.Location.Y + centerPanel.Size.Height + 4, clientWidth, bottomPanel.Height);

			RefreshLayout();
		}

		private void RefreshLayout()
		{
			int clientWidth = this.Width - 4;

			if (_entryPanels.Count > 0)
			{
				int parameterY = Constants.ParameterPanel.TopMargin;
				for (int i = 0; i < _entryPanels.Count; ++i)
				{
					int entryHeight = _entryPanels[i].Height;
					if (i > 0)
						parameterY += EntrySpacing;
					var entryPanel = _entryPanels[i] as Control;
					entryPanel.Bounds = new Rectangle(0, parameterY - centerPanel.VerticalScroll.Value, clientWidth, entryPanel.Height);
					parameterY += entryHeight;
				}
			}

			this.HideHorizontalScrollbar();
		}

		private void ResizeCenterPanel()
		{
			if (centerPanel.Controls.Count > 0)
			{
				int totalHeight = 0;
				int count = centerPanel.Controls.Count;
				for (int i = 0; i < count; ++i)
					totalHeight += centerPanel.Controls[i].Size.Height;
				totalHeight += (EntrySpacing * count - 1) + 2;
				centerPanel.Size = new Size(centerPanel.Size.Width, totalHeight);
			}
			else
				centerPanel.Size = new Size();

//			this.HideHorizontalScrollbar();
			NotifySizeChanged(); // Notify parent the size has changed
		}

		public void RefreshSyntaxHighlight(bool immediate, bool invalidate)
		{
			foreach (var panel in _entryPanels)
			{
				if (invalidate)
				{
					panel.textBox_Keys.richTextBox.InvalidateSyntaxHighlighting();
					panel.textBox_Text.richTextBox.InvalidateSyntaxHighlighting();
				}
				panel.textBox_Keys.richTextBox.RefreshSyntaxHighlight(immediate);
				panel.textBox_Text.richTextBox.RefreshSyntaxHighlight(immediate);
			}
		}

		public void Sort(Lorebook.Sorting sorting)
		{
			MainForm.StealFocus();
			lorebook.SortEntries(sorting, false);

			parameter.pageIndex = 0;

			OnRefreshValue();

			Undo.Suspend();
			NotifyValueChanged();
			Undo.Resume();
			Undo.Push(Undo.Kind.Parameter, "Sort lorebook");
		}

		public void RefreshFlexibleSize()
		{
			WhileIgnoringEvents(() => {
				foreach (var panel in _entryPanels)
				{
					panel.RefreshFlexibleSize();
				}
			});
			ResizeCenterPanel();
		}

		private void PageChanger_PageChanged(object sender, PageChanger.PageChangedEventArgs e)
		{
			ChangePage(e.page);
		}

		public void ChangePage(int page)
		{
			this.DisableRedrawAndDo(() => {
				foreach (var panel in _entryPanels)
					panel.CommitChange();

				int pageIndex = page - 1;
				parameter.pageIndex = pageIndex;

				OnRefreshValue();

				RefreshSyntaxHighlight(true, true);
			});
		}

	}
}
