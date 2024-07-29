using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Ginger
{
	public partial class LoreBookParameterPanel : LoreBookParameterPanelDummy, ISyntaxHighlighted, ISearchableContainer
	{
		protected override CheckBox parameterCheckBox { get { return null; } }
		protected override Label parameterLabel { get { return null; } }

		public Lorebook lorebook { get { return parameter.value; } }
		private List<LorebookEntryPanel> _entryPanels = new List<LorebookEntryPanel>();

		public event EventHandler EntriesChanged;

		private static readonly int EntrySpacing = 6;
		private static readonly int ScrollStep = 150; // Scroll only
		private static readonly int MaxEntriesInView = 5;

		public LoreBookParameterPanel()
		{
			InitializeComponent();

			centerPanel.VerticalScroll.SmallChange = ScrollStep;
			centerPanel.VerticalScroll.LargeChange = ScrollStep * MaxEntriesInView;
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
			entryPanel.MoveUpClicked += OnMoveUpEntry;
			entryPanel.MoveDownClicked += OnMoveDownEntry;
			entryPanel.Changed += OnChangedEntry;
			entryPanel.OnCopy += OnCopy;
			entryPanel.OnPaste += OnPaste;
			entryPanel.OnInsert += OnInsertAt;
			entryPanel.OnDuplicate += OnDuplicate;
			entryPanel.OnAddEntry += BtnAddEntry_Click;
			return entryPanel;
		}

		private void BtnAddEntry_Click(object sender, EventArgs e)
		{
			MainForm.EnableFormLevelDoubleBuffering(true);

//			centerPanel.Suspend();
//			centerPanel.SuspendLayout();

			LorebookEntryPanel entryPanel = null;

			this.DisableRedrawAndDo(() => {
				var newEntry = new Lorebook.Entry() {
					addition_index = lorebook.GetNextIndex(),
				};
				lorebook.entries.Add(newEntry);

				entryPanel = CreateEntryPanel();
				entryPanel.SetContent(newEntry);

				centerPanel.Controls.Add(entryPanel);
				_entryPanels.Add(entryPanel);

				// Tab order
				for (int i = 0; i < _entryPanels.Count; ++i)
					_entryPanels[i].TabIndex = i;

				centerPanel.Invalidate();
			});


			ResizeCenterPanel();
			RefreshLayout();

			this.DisableRedrawAndDo(() => {
				centerPanel.ScrollControlIntoView(_entryPanels[_entryPanels.Count - 1]);
			});
			entryPanel.textBox_Keys.Focus();

			Undo.Suspend();
			EntriesChanged?.Invoke(this, EventArgs.Empty);
			NotifyValueChanged(string.Format("entry-{0}-{1}", _entryPanels.Count - 1, _entryPanels.Count));
			Undo.Resume();
			Undo.Push(Undo.Kind.Parameter, "Add lore entry");
		}

		private void OnChangedEntry(object sender, LorebookEntryPanel.LorebookChangedEventArgs e)
		{
			if (isIgnoringEvents)
				return;

			LorebookEntryPanel panel = (LorebookEntryPanel)sender;
			int index = _entryPanels.IndexOf(panel);

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
			int index = _entryPanels.IndexOf(panel);

			Undo.Suspend();
			centerPanel.Suspend();
			WhileIgnoringEvents(() => {
				centerPanel.Controls.Remove(panel);
			});
			_entryPanels.Remove(panel);
			panel.Dispose();

			lorebook.entries.RemoveAt(index);

			ResizeCenterPanel();

			centerPanel.Resume();

			EntriesChanged?.Invoke(this, EventArgs.Empty);
			NotifyValueChanged(string.Format("entry-{0}-{1}", index, _entryPanels.Count));

			Undo.Resume();
			Undo.Push(Undo.Kind.Parameter, "Remove lore entry");
		}

		private void OnMoveUpEntry(object sender, EventArgs e)
		{
			LorebookEntryPanel panel = (LorebookEntryPanel)sender;
			int index = _entryPanels.IndexOf(panel);
			int newIndex = index - 1;

			if (ModifierKeys == Keys.Shift)
				newIndex = 0;

			if (MoveEntry(index, newIndex))
			{
//				SetDirty(string.Format("entry-{0}-{1}", index, _entryPanels.Count));
				Undo.Push(Undo.Kind.Parameter, "Move lore entry");
			}
		}

		private void OnMoveDownEntry(object sender, EventArgs e)
		{
			LorebookEntryPanel panel = (LorebookEntryPanel)sender;
			int index = _entryPanels.IndexOf(panel);
			int newIndex = index + 1;

			if (ModifierKeys == Keys.Shift)
				newIndex = lorebook.entries.Count - 1;

			if (MoveEntry(index, newIndex))
			{
//				SetDirty(string.Format("entry-{0}-{1}", index, _entryPanels.Count));
				Undo.Push(Undo.Kind.Parameter, "Move lore entry");
			}
		}

		private bool MoveEntry(int position, int newPosition)
		{
			MainForm.StealFocus();

			if (newPosition < 0)
				newPosition = 0;
			if (newPosition > _entryPanels.Count - 1)
				newPosition = _entryPanels.Count - 1;
			if (newPosition == position)
				return false;

			var panel = _entryPanels[position];
//			centerPanel.Suspend();
//			centerPanel.SuspendLayout();

			_entryPanels.RemoveAt(position);
			_entryPanels.Insert(newPosition, panel);
			var loreEntry = lorebook.entries[position];
			lorebook.entries.RemoveAt(position);
			lorebook.entries.Insert(newPosition, loreEntry);

			RefreshLayout();

			centerPanel.ScrollControlIntoView(_entryPanels[newPosition]);
			centerPanel.Invalidate();
			// Tab order
			for (int i = 0; i < _entryPanels.Count; ++i)
				_entryPanels[i].TabIndex = i;

//			centerPanel.ResumeLayout(true);
//			centerPanel.Resume();
			return true;
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
//			centerPanel.SuspendLayout();

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

//			centerPanel.ResumeLayout(true);
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

			int insertionIndex = _entryPanels.IndexOf(panel);

			MainForm.StealFocus();

			centerPanel.Suspend();
//			centerPanel.SuspendLayout();

			lorebook.entries.Insert(insertionIndex, newEntry);

			var entryPanel = CreateEntryPanel();
			_entryPanels.Insert(insertionIndex, entryPanel);
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
			
//			centerPanel.ResumeLayout(true);
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

			int insertionIndex = _entryPanels.IndexOf(panel) + 1;
			duplicateEntry.key = string.Concat("Copy of ", duplicateEntry.key);

			MainForm.StealFocus();

			centerPanel.Suspend();
//			centerPanel.SuspendLayout();

			lorebook.entries.Insert(insertionIndex, duplicateEntry);

			var entryPanel = CreateEntryPanel();
			_entryPanels.Insert(insertionIndex, entryPanel);
			centerPanel.Controls.Add(entryPanel);

			entryPanel.SetContent(duplicateEntry);
			entryPanel.textBox_Keys.Focus();

			ResizeCenterPanel();

			// Tab order
			for (int i = 0; i < _entryPanels.Count; ++i)
			{
				_entryPanels[i].TabIndex = i;
			}
			
//			centerPanel.ResumeLayout(true);
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
			// Remove
			while (_entryPanels.Count > parameter.value.entries.Count)
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
			while (_entryPanels.Count < parameter.value.entries.Count)
			{
				var entryPanel = CreateEntryPanel();
				_entryPanels.Add(entryPanel);
				addedPanels.Add(entryPanel);
			}
			centerPanel.Controls.AddRange(addedPanels.ToArray());

			for (int i = 0; i < _entryPanels.Count; ++i)
			{
				_entryPanels[i].TabIndex = i;
				_entryPanels[i].SetContent(parameter.value.entries[i]);
			}

			ResizeCenterPanel();
			RefreshLayout();

			RefreshSyntaxHighlight(true, true);

			RichTextBoxEx.AllowSyntaxHighlighting = bSyntaxHighlightingWasEnabled;
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

			if (_entryPanels.Count > MaxEntriesInView)
				clientWidth -= 24;

			if (_entryPanels.Count > 0)
			{
				int entryHeight = _entryPanels[0].Height;
				int parameterY = Constants.ParameterPanel.TopMargin;
				for (int i = 0; i < _entryPanels.Count; ++i)
				{
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
				int entryHeight = centerPanel.Controls[0].Size.Height;
				centerPanel.Size = new Size(centerPanel.Size.Width,
					Math.Min(entryHeight * MaxEntriesInView + (EntrySpacing * MaxEntriesInView - 1),
					entryHeight * _entryPanels.Count + (EntrySpacing * _entryPanels.Count - 1)) + 2);
			}
			else
				centerPanel.Size = new Size();
			
			this.HideHorizontalScrollbar();
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

			bool bSyntaxHighlightingWasEnabled = RichTextBoxEx.AllowSyntaxHighlighting;
			if (bSyntaxHighlightingWasEnabled)
				RichTextBoxEx.AllowSyntaxHighlighting = false;

			for (int i = 0; i < _entryPanels.Count; ++i)
			{
				_entryPanels[i].TabIndex = i;
				_entryPanels[i].SetContent(lorebook.entries[i]);
			}
			RichTextBoxEx.AllowSyntaxHighlighting = bSyntaxHighlightingWasEnabled;
			RefreshSyntaxHighlight(true, true);

			Undo.Suspend();
			NotifyValueChanged();
			Undo.Resume();
			Undo.Push(Undo.Kind.Parameter, "Sort lorebook");
		}
	}
}
