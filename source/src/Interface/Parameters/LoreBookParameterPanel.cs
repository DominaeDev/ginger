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

		private int FirstEntryIndex { get { return parameter.pageIndex * EntriesPerPage; } }
		private int numEntries { get { return lorebook.entries.Count; } }
		private int numPages { get { return Math.Max((int)Math.Ceiling((double)numEntries / EntriesPerPage), 1); } }
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
			entryPanel.OnRemove += OnRemoveEntry;
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
			entryPanel.OnNextPage += OnNextPage;
			entryPanel.OnPreviousPage += OnPreviousPage;
			entryPanel.TextSizeChanged += EntryPanel_TextSizeChanged;
			entryPanel.OnSortEntries += EntryPanel_OnSortEntries;
			return entryPanel;
		}

		private void EntryPanel_OnSortEntries(object sender, LorebookEntryPanel.LorebookSortEventArgs e)
		{
			Sort(e.Sorting);
		}

		public void OnNextPage(object sender, EventArgs e)
		{
			ChangePage(parameter.pageIndex + 1);
		}

		public void OnPreviousPage(object sender, EventArgs e)
		{
			ChangePage(parameter.pageIndex - 1);
		}

		private void OnAddEntry(object sender, EventArgs e)
		{
			MainForm.EnableFormLevelDoubleBuffering(true);

			var newEntry = new Lorebook.Entry() {
				addition_index = lorebook.GetNextIndex(),
			};
			lorebook.entries.Add(newEntry);

			if (numEntries - FirstEntryIndex <= EntriesPerPage)
			{
				LorebookEntryPanel entryPanel = null;

				this.Suspend();
				entryPanel = CreateEntryPanel();
				entryPanel.SetContent(newEntry, lorebook);

				centerPanel.Controls.Add(entryPanel);
				_entryPanels.Add(entryPanel);

				// Tab order
				for (int i = 0; i < _entryPanels.Count; ++i)
					_entryPanels[i].TabIndex = i;

				centerPanel.Invalidate();

				RefreshPageChanger();
				ResizeCenterPanel();
				RefreshLayout();
				this.Resume();

				entryPanel.textBox_Keys.Focus();
			}
			else
			{
				ChangePage(numPages - 1);

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
			if (numEntries <= EntriesPerPage && !(index == FirstEntryIndex && index == numEntries))
			{
				centerPanel.Suspend();
				WhileIgnoringEvents(() => {
					centerPanel.Controls.Remove(panel);
					_entryPanels.Remove(panel);
					panel.Dispose();
				});

				centerPanel.Resume();
				RefreshPageChanger();
				ResizeCenterPanel();
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
				Undo.Push(Undo.Kind.Parameter, "Move lore entry");
		}

		private void OnMoveDownEntry(object sender, EventArgs e)
		{
			LorebookEntryPanel panel = (LorebookEntryPanel)sender;
			int index = lorebook.entries.IndexOf(panel.lorebookEntry);
			if (index == -1)
				return;

			if (MoveEntry(index, index + 1))
				Undo.Push(Undo.Kind.Parameter, "Move lore entry");
		}

		private void OnMoveToTop(object sender, EventArgs e)
		{
			LorebookEntryPanel panel = (LorebookEntryPanel)sender;
			int index = lorebook.entries.IndexOf(panel.lorebookEntry);
			if (index == -1)
				return;

			if (MoveEntry(index, 0))
				Undo.Push(Undo.Kind.Parameter, "Move lore entry");
		}

		private void OnMoveToBottom(object sender, EventArgs e)
		{
			LorebookEntryPanel panel = (LorebookEntryPanel)sender;
			int index = lorebook.entries.IndexOf(panel.lorebookEntry);
			if (index == -1)
				return;

			if (MoveEntry(index, numEntries - 1))
				Undo.Push(Undo.Kind.Parameter, "Move lore entry");
		}

		private bool MoveEntry(int index, int newIndex)
		{
			MainForm.StealFocus();

			if (newIndex < 0)
				newIndex = 0;
			if (newIndex >= numEntries)
				newIndex = numEntries - 1;

			// Move lore entry
			var loreEntry = lorebook.entries[index];
			lorebook.entries.RemoveAt(index);
			lorebook.entries.Insert(newIndex, loreEntry);

			ChangePage(parameter.pageIndex); // Refresh
	
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
			LoreClipboard data = Clipboard.GetData(LoreClipboard.Format) as LoreClipboard;
			if (data == null)
				return;

			var panel = sender as LorebookEntryPanel;

			int insertionIndex = lorebook.entries.IndexOf(panel.lorebookEntry);
			if (insertionIndex == -1)
				insertionIndex = lorebook.entries.Count;

			List<Lorebook.Entry> entries = data.ToEntries();
			if (entries == null || entries.Count == 0)
				return;

			foreach (var entry in entries)
			{
				lorebook.entries.Insert(insertionIndex, entry);
				insertionIndex++;
			}

			ChangePage(parameter.pageIndex);

			var entryPanel = _entryPanels.FirstOrDefault(ee => ee.lorebookEntry == entries[0]);
			if (entryPanel != null)
				entryPanel.textBox_Keys.Focus();

			Undo.Suspend();
			EntriesChanged?.Invoke(this, EventArgs.Empty);
			NotifyValueChanged(string.Format("entry-{0}-{1}", insertionIndex, _entryPanels.Count));
			Undo.Resume();
			Undo.Push(Undo.Kind.Parameter, "Paste lore");
		}

		private void OnInsertAt(object sender, EventArgs e)
		{
			var panel = sender as LorebookEntryPanel;

			int insertionIndex = lorebook.entries.IndexOf(panel.lorebookEntry);
			if (insertionIndex == -1)
				insertionIndex = lorebook.entries.Count;

			var newEntry = new Lorebook.Entry() {
				addition_index = lorebook.GetNextIndex(),
				sortOrder = panel.lorebookEntry.sortOrder,
			};

			lorebook.entries.Insert(insertionIndex, newEntry);

			ChangePage(parameter.pageIndex);

			var entryPanel = _entryPanels.FirstOrDefault(ee => ee.lorebookEntry == newEntry);
			if (entryPanel != null)
				entryPanel.textBox_Keys.Focus();

			Undo.Suspend();
			EntriesChanged?.Invoke(this, EventArgs.Empty);
			NotifyValueChanged(string.Format("entry-{0}-{1}", insertionIndex, _entryPanels.Count));
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
				insertionIndex = lorebook.entries.Count - 1;
			insertionIndex++;

			int panelInsertionIndex = _entryPanels.IndexOf(panel);
			duplicateEntry.key = string.Concat("Copy of ", duplicateEntry.key);

			lorebook.entries.Insert(insertionIndex, duplicateEntry);

			ChangePage(insertionIndex / EntriesPerPage); // Refresh

			var entryPanel = _entryPanels.FirstOrDefault(ee => ee.lorebookEntry == duplicateEntry);
			if (entryPanel != null)
				entryPanel.textBox_Keys.Focus();

			Undo.Suspend();
			EntriesChanged?.Invoke(this, EventArgs.Empty);
			NotifyValueChanged(string.Format("entry-{0}-{1}", insertionIndex, _entryPanels.Count));
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
				_entryPanels[i].SetContent(parameter.value.entries[entryFrom + i], lorebook);
			}

			RefreshPageChanger();
			RefreshFlexibleSize();
			RefreshLayout();
			NotifySizeChanged();

			RichTextBoxEx.AllowSyntaxHighlighting = bSyntaxHighlightingWasEnabled;
			RefreshSyntaxHighlight(true, true);
		}

		private void RefreshPageChanger()
		{
			pageChanger.SetPage(parameter.pageIndex, Math.Max(numEntries / EntriesPerPage, 0));
			pageChanger.Visible = numEntries > EntriesPerPage;
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

		private void ResizeCenterPanel(bool bNotifyParent = true)
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
			if (bNotifyParent)
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
			if (LorebookEntryPanel.AllowFlexibleHeight == false)
				return;

			WhileIgnoringEvents(() => {
				foreach (var panel in _entryPanels)
					panel.RefreshFlexibleSize();
			});
			ResizeCenterPanel(false);
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

				int pageIndex = Math.Min(Math.Max(page, 0), numPages - 1);
				parameter.pageIndex = pageIndex;

				OnRefreshValue();

				RefreshSyntaxHighlight(true, true);
			});
		}

		public void RefreshLineWidth()
		{
			WhileIgnoringEvents(() => {
				foreach (var panel in _entryPanels)
					panel.RefreshLineWidth();
			});

			RefreshLayout();
			ResizeCenterPanel();
		}
	}
}
