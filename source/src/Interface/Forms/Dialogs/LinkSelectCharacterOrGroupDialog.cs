using Ginger.Integration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Ginger
{
	using CharacterInstance = Backyard.CharacterInstance;
	using FolderInstance = Backyard.FolderInstance;
	using GroupInstance = Backyard.GroupInstance;
	using Timer = System.Timers.Timer;

	public partial class LinkSelectCharacterOrGroupDialog : FormEx
	{
		[Flags]
		public enum Option
		{
			None = 0,
			Orphans = 1 << 0,
			Solo = 1 << 1,
			Parties = 1 << 2,
		}

		public CharacterInstance SelectedCharacter { get; private set; }
		public GroupInstance SelectedGroup { get; private set; }
		public Option Options { get; set; }

		public string ConfirmButton { set { btnOk.Text = value; } }

		private CharacterInstance[] Characters;
		private CharacterInstance[] Orphans;
		private GroupInstance[] Groups;
		private FolderInstance[] Folders;
		private Dictionary<string, CharacterInstance> _charactersById;
		private Dictionary<string, int> _folderCounts = new Dictionary<string, int>();
		private Dictionary<string, Backyard.ChatCount> _chatCounts;

		private bool _bIgnoreEvents = false;
		private string _filterString;
		private Timer _timer = new Timer();

		public LinkSelectCharacterOrGroupDialog()
		{
			InitializeComponent();

			this.Load += OnLoad;
		}

		~LinkSelectCharacterOrGroupDialog()
		{
			_timer.Dispose();
		}

		private void OnLoad(object sender, EventArgs e)
		{
			filterTextBox.KeyDown += FilterTextBox_KeyDown;

			this.Characters = Backyard.Database.Characters.ToArray();
			this.Orphans = this.Characters.Where(c => c.groupId == null).ToArray();
			this.Folders = Backyard.Database.Folders.ToArray();
			if (Options.Contains(Option.Parties))
			{
				this.Groups = Backyard.Database.Groups.ToArray();
			}
			else if (Options.Contains(Option.Solo))
			{
				this.Groups = Backyard.Database.Groups
					.Where(g => g.GetGroupType() == GroupInstance.GroupType.Solo)
					.ToArray();
			}
			else
				this.Groups = new GroupInstance[0];

			_charactersById = Characters.ToDictionary(c => c.instanceId, c => c);

			treeView.SelectedNode = null;
			SelectedGroup = default(GroupInstance);
			SelectedCharacter = default(CharacterInstance);
						
			BackyardUtil.GetChatCounts(out _chatCounts);

			if (Groups.ContainsAny(g => g.Count > 2))
				this.Text = "Select character or party";

			_bIgnoreEvents = false;

			PopulateTree(false);
			filterTextBox.Text = "";
			btnOk.Enabled = false;

			_timer.Interval = 250;
			_timer.Elapsed += OnTimerElapsed;
			_timer.AutoReset = false;
			_timer.SynchronizingObject = this;

			if (Characters.Length > 0 || Groups.Length > 0)
			{
				var characterIds = Characters.Select(c => c.instanceId)
					.Union(Groups.Select(g => g.instanceId));
				AppSettings.BackyardSettings.StarredCharacters.IntersectWith(characterIds);
			}

			_bIgnoreEvents = false;
		}

		private void FilterTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Down)
				treeView.Focus();
		}

		private DateTime GetLatestMessageTime(GroupInstance group)
		{
			Backyard.ChatCount count;
			if (_chatCounts.TryGetValue(group.instanceId, out count))
				return count.lastMessage;
			return DateTime.MinValue;
		}

		private DateTime GetLatestMessageTime(CharacterInstance characterInstance)
		{
			if (string.IsNullOrEmpty(characterInstance.groupId))
				return DateTime.MinValue;
			Backyard.ChatCount count;
			if (_chatCounts.TryGetValue(characterInstance.groupId, out count))
				return count.lastMessage;
			return DateTime.MinValue;
		}

		private void PopulateTree(bool bRefresh)
		{
			HashSet<string> expandedFolders = new HashSet<string>();
			HashSet<string> emptyFolders = new HashSet<string>();
			foreach (var node in treeView.AllNodes())
			{
				if (node.IsExpanded && node.Tag is string)
					expandedFolders.Add(node.Tag as string);
			}

			HashSet<string> starredIds = AppSettings.BackyardSettings.StarredCharacters;

			treeView.BeginUpdate();
			treeView.Nodes.Clear();

			if (Folders == null || Groups == null)
				return; // Nothing to show

			IEnumerable<GroupInstance> sortedGroups = Groups;
			IEnumerable<CharacterInstance> sortedOrphans = Orphans;

			// Filter
			bool bFiltered = string.IsNullOrWhiteSpace(_filterString) == false;

			if (bFiltered)
			{
				var filteredCharacters = new HashSet<string>(this.Characters
					.Where(c => {
						return c.isCharacter
							&& (c.displayName.ContainsPhrase(_filterString)
								|| c.name.ContainsPhrase(_filterString)
								|| c.creator.ContainsPhrase(_filterString)
								|| c.persona.ContainsWholeWord(_filterString));
					})
					.Select(c => c.instanceId));

				sortedGroups = sortedGroups.Where(g => filteredCharacters.ContainsAnyIn(g.members));
				sortedOrphans = sortedOrphans.Where(c => filteredCharacters.Contains(c.instanceId));
			}

			if (bRefresh == false || bFiltered)
			{
				_folderCounts.Clear();

				// Sum character counts
				for (int i = 0; i < Folders.Length; ++i)
					_folderCounts.Add(Folders[i].instanceId, sortedGroups.Count(c => c.folderId == Folders[i].instanceId));

				for (int i = Folders.Length - 1; i >= 0; --i)
				{
					if (string.IsNullOrEmpty(Folders[i].parentId) == false)
						_folderCounts[Folders[i].parentId] += _folderCounts[Folders[i].instanceId];
				}
			}

			// Create folder nodes
			var nodesById = new Dictionary<string, TreeNode>();
			string rootId = Folders.FirstOrDefault(f => f.isRoot).instanceId;
			nodesById.Add(rootId, null);

			var openList = new List<string>(Folders
				.Select(f => f.instanceId)
				.Distinct());

			var expandedNodes = new List<TreeNode>();
			while (openList.Count > 0)
			{
				string parentId = openList[0];
				var subfolders = Folders
					.Where(f => f.parentId == parentId)
					.OrderBy(f => f.name);

				foreach (var folder in subfolders)
				{
					int count = _folderCounts[folder.instanceId];
					if (count == 0)
						continue;

					var folderNode = CreateFolderNode(folder, nodesById, count);
					if (bFiltered || expandedFolders.Contains(folder.instanceId))
						expandedNodes.Add(folderNode);
				}
				
				openList.Remove(parentId);
			}

			if (AppSettings.User.SortGroups == AppSettings.CharacterSortOrder.ByName)
			{
				sortedGroups = sortedGroups
					.OrderBy(g => g.GetDisplayName())
					.ThenByDescending(c => c.creationDate);
				sortedOrphans = sortedOrphans
					.OrderBy(g => g.displayName)
					.ThenByDescending(c => c.creationDate);
			}
			else if (AppSettings.User.SortGroups == AppSettings.CharacterSortOrder.ByCreation)
			{
				sortedGroups = sortedGroups
					.OrderByDescending(g => g.creationDate);
				sortedOrphans = sortedOrphans
					.OrderByDescending(c => c.creationDate);
			}
			else if (AppSettings.User.SortGroups == AppSettings.CharacterSortOrder.ByLastMessage)
			{
				sortedGroups = sortedGroups
					.OrderByDescending(g => GetLatestMessageTime(g));
				sortedOrphans = sortedOrphans
					.OrderByDescending(c => GetLatestMessageTime(c));
			}
			else if (AppSettings.User.SortGroups == AppSettings.CharacterSortOrder.ByCustom)
			{
				sortedGroups = sortedGroups
					.OrderBy(g => g.folderSortPosition);
				sortedOrphans = sortedOrphans
					.OrderByDescending(c => c.creationDate); // Not applicable
			}

			// Starred folder
			TreeNode starredFolder = null;
			if (bFiltered == false)
			{
				int nStarred = sortedGroups.Select(g => g.instanceId).Count(id => starredIds.Contains(id));
				if (Options.ContainsAny(Option.Orphans))
					nStarred += sortedOrphans.Select(c => c.instanceId).Count(id => starredIds.Contains(id));
				if (nStarred > 0)
				{
					starredFolder = new TreeNode(string.Format("Starred ({0})", nStarred), 2, 2);
					starredFolder.Tag = "Starred";
					treeView.Nodes.Insert(0, starredFolder);
					if (expandedFolders.Contains(starredFolder.Tag))
						expandedNodes.Add(starredFolder);
				}
			}

			// Create orphan nodes
			int nOrphans = sortedOrphans.Count();
			if (nOrphans > 0 && Options.ContainsAny(Option.Orphans) )
			{
				TreeNode folderNode = null;

				if (sortedGroups.Count() > 0)
				{
					folderNode = new TreeNode(string.Format("Stand-alone characters ({0})", nOrphans), 1, 1);
					folderNode.Tag = "Orphans";
					treeView.Nodes.Insert(starredFolder != null ? 1 : 0, folderNode);
					if (expandedFolders.Contains(folderNode.Tag))
						expandedNodes.Add(folderNode);
				}

				foreach (var character in sortedOrphans)
				{
					bool bStarred = starredIds.Contains(character.instanceId);
					CreateCharacterNode(character, folderNode, true, bStarred);

					if (starredFolder != null && bStarred)
					{
						Backyard.ChatCount chatCount;
						_chatCounts.TryGetValue(character.instanceId, out chatCount);
						CreateCharacterNode(character, starredFolder, chatCount.hasMessages == false, true);
					}
				}
			}

			// Create group nodes
			foreach (var group in sortedGroups)
			{
				bool bStarred = starredIds.Contains(group.instanceId);
				CreateGroupNode(group, nodesById, null, bStarred);

				if (starredFolder != null && bStarred)
					CreateGroupNode(group, nodesById, starredFolder, true);
			}

			if (bRefresh || bFiltered)
			{
				for (int i = expandedNodes.Count - 1; i >= 0; --i)
					expandedNodes[i].Expand();
			}

			treeView.EndUpdate();
		}

		private TreeNode CreateFolderNode(FolderInstance folder, Dictionary<string, TreeNode> nodes, int count)
		{
			TreeNode parentNode;
			nodes.TryGetValue(folder.parentId, out parentNode);

			var node = new TreeNode(string.Format("{0} ({1})", folder.name, count), 0, 0);
			if (parentNode != null)
				parentNode.Nodes.Add(node);
			else
				treeView.Nodes.Add(node);
			node.Tag = folder.instanceId;
			nodes.Add(folder.instanceId, node);
			return node;
		}

		private TreeNode CreateGroupNode(GroupInstance group, Dictionary<string, TreeNode> nodes, TreeNode parentNode, bool isStarred)
		{
			if (group.Count < 2)
				return null;

			if (parentNode == null)
				nodes.TryGetValue(group.folderId, out parentNode);

			string groupLabel = group.GetDisplayName();
			var sbTooltip = new StringBuilder();

			CharacterInstance[] characters = group.members
				.Select(id => _charactersById.GetOrDefault(id))
				.Where(c => c.isCharacter)
				.OrderBy(c => c.creationDate)
				.ToArray();

			if (characters.Length >= 2)
			{
				string[] characterNames = characters
					.Select(c => Utility.FirstNonEmpty(c.name, Constants.DefaultCharacterName))
					.ToArray();

				sbTooltip.Append("Group chat with ");
				sbTooltip.Append(Utility.CommaSeparatedList(characterNames));
			}
			else if (characters.Length == 1)
			{
				var character = characters[0];
				sbTooltip.Append("Name: ");
				sbTooltip.Append(character.displayName);
				if (string.Compare(character.name, character.displayName, StringComparison.OrdinalIgnoreCase) != 0)
				{
					sbTooltip.Append(" (goes by '");
					sbTooltip.Append(character.name);
					sbTooltip.Append("')");
				}

				if (string.IsNullOrEmpty(character.creator) == false)
				{
					sbTooltip.NewLine();
					sbTooltip.Append("By: ");
					sbTooltip.Append(characters[0].creator);
					sbTooltip.AppendLine();
				}
				if (string.IsNullOrEmpty(character.inferredGender) == false)
				{
					sbTooltip.NewLine();
					sbTooltip.AppendFormat("Gender: {0} (Inferred)", character.inferredGender);
				}
				sbTooltip.NewLine();
				sbTooltip.AppendFormat("Lorebook: {0}", character.hasLorebook ? "Yes" : "No");
			}

			sbTooltip.NewLine();
			sbTooltip.Append("Chats: ");
			Backyard.ChatCount chatCount;
			if (_chatCounts.TryGetValue(group.instanceId, out chatCount))
			{
				if (chatCount.hasMessages)
					sbTooltip.Append(chatCount.count);
				else
					sbTooltip.Append("None");
			}
			else
				sbTooltip.Append("None");

			sbTooltip.NewParagraph();
			sbTooltip.AppendLine($"Created: {group.creationDate.ToShortDateString()}");
			sbTooltip.AppendLine($"Last modified: {group.updateDate.ToShortDateString()}");

			// Icon
			int icon;
			if (characters.Length >= 2)
			{
				icon = 33; // Group
				if (isStarred)
					icon += 1;
			}
			else if (characters.Length == 1)
			{
				string inferredGender = characters[0].inferredGender?.ToLowerInvariant();
				if (inferredGender == "male")
					icon = 9; // Blue
				else if (inferredGender == "female")
					icon = 15; // Pink
				else if (inferredGender == "transgender")
					icon = 21; // Green
				else if (inferredGender == "futanari" || inferredGender == "hermaphrodite")
					icon = 27; // Yellow
				else
					icon = 3; // White

				if (isStarred)
					icon += 2;
				else if (characters[0].hasLorebook)
					icon += 1; // Lore
			}
			else
			{
				icon = 3; // White
				if (isStarred)
					icon += 1;
			}

			var node = new TreeNode(groupLabel, icon, icon);
			node.Tag = group;
			node.ToolTipText = sbTooltip.ToString();
			if (parentNode != null)
				parentNode.Nodes.Add(node);
			else
				treeView.Nodes.Add(node);

			if (characters.Length > 1 && Options.Contains(Option.Orphans))
			{
				foreach (var character in characters)
					CreateCharacterNode(character, node, chatCount.hasMessages == false, false);
			}
			return node;
		}
				
		private TreeNode CreateCharacterNode(CharacterInstance character, TreeNode parentNode, bool bGrayed, bool isStarred)
		{
			string label = character.displayName;
			if (string.Compare(character.name, label, StringComparison.OrdinalIgnoreCase) != 0)
				label = string.Concat(label, " \"", character.name, "\"");

			var sbTooltip = new StringBuilder();
			sbTooltip.Append("Name: ");
			sbTooltip.Append(character.displayName);
			if (string.Compare(character.name, label, StringComparison.OrdinalIgnoreCase) != 0)
			{
				sbTooltip.Append(" (goes by '");
				sbTooltip.Append(character.name);
				sbTooltip.Append("')");
			}
			if (string.IsNullOrEmpty(character.creator) == false)
			{
				sbTooltip.NewLine();
				sbTooltip.Append("By: ");
				sbTooltip.Append(character.creator);
				sbTooltip.AppendLine();
			}
			if (string.IsNullOrEmpty(character.inferredGender) == false)
			{
				sbTooltip.NewLine();
				sbTooltip.AppendFormat("Gender: {0} (Inferred)", character.inferredGender);
			}

			string inferredGender = character.inferredGender?.ToLowerInvariant();
			sbTooltip.NewParagraph();
			sbTooltip.AppendLine($"Created: {character.creationDate.ToShortDateString()}");
			sbTooltip.AppendLine($"Last modified: {character.updateDate.ToShortDateString()}");

			// Icon
			int icon;
			if (inferredGender == "male")
				icon = 9; // Blue
			else if (inferredGender == "female")
				icon = 15; // Pink
			else if (inferredGender == "transgender")
				icon = 21; // Green
			else if (inferredGender == "futanari" || inferredGender == "hermaphrodite")
				icon = 27; // Yellow
			else
				icon = 3; // White

			if (isStarred)
				icon += 2;
			else if (character.hasLorebook)
				icon += 1; // Lore
			if (bGrayed)
				icon += 3; // Grayed out

			var node = new TreeNode(label, icon, icon);
			node.Tag = character;
			node.ToolTipText = sbTooltip.ToString();
			if (parentNode != null)
				parentNode.Nodes.Add(node);
			else
				treeView.Nodes.Add(node);
			return node;
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			OnSelectedNode(e.Node);
		}

		private void OnSelectedNode(TreeNode node)
		{
			if (node != null && node.Tag is GroupInstance)
			{
				GroupInstance group = (GroupInstance)node.Tag;
				CharacterInstance[] characters = group.members
					.Select(id => _charactersById.GetOrDefault(id))
					.Where(c => c.isCharacter)
					.ToArray();

				if (characters.Length > 1)
				{
					SelectedGroup = (GroupInstance)node.Tag;
					SelectedCharacter = default(CharacterInstance);
				}
				else if (characters.Length == 1)
				{
					SelectedCharacter = characters[0];
					SelectedGroup = (GroupInstance)node.Tag;
				}
				else
				{
					SelectedGroup = default(GroupInstance);
					SelectedCharacter = default(CharacterInstance);
				}
			}
			else if (node != null && node.Tag is CharacterInstance)
			{
				SelectedCharacter = (CharacterInstance)node.Tag;
				SelectedGroup = default(GroupInstance);
			}
			else
			{
				SelectedGroup = default(GroupInstance);
				SelectedCharacter = default(CharacterInstance);
			}

			btnOk.Enabled = SelectedGroup.isDefined || SelectedCharacter.isDefined;
		}

		private void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Node != null && e.Node == treeView.SelectedNode
				&& (e.Node.Tag is GroupInstance || e.Node.Tag is CharacterInstance))
			{
				OnSelectedNode(e.Node);
				BtnOk_Click(this, EventArgs.Empty);
			}
		}

		private void treeView_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				ShowContextMenu(sender as Control, new Point(e.X, e.Y));
			}
		}

		private void ShowContextMenu(Control control, Point location)
		{
			ContextMenuStrip menu = new ContextMenuStrip();

			// Starred?
			var node = treeView.GetNodeAt(location);
			if (node != null)
			{
				string id;
				if (node.Tag is GroupInstance)
					id = ((GroupInstance)node.Tag).instanceId;
				else if (node.Tag is CharacterInstance)
					id = ((CharacterInstance)node.Tag).instanceId;
				else 
					id = null;

				if (id != null)
				{
					menu.Items.Add(new ToolStripMenuItem("Starred", null, (s, e) => {
						if (AppSettings.BackyardSettings.StarredCharacters.Contains(id) == false)
							AppSettings.BackyardSettings.StarredCharacters.Add(id);
						else
							AppSettings.BackyardSettings.StarredCharacters.Remove(id);

						treeView.LockScrollAndDo(() => {
							PopulateTree(true);
						});
					}) {
						Checked = AppSettings.BackyardSettings.StarredCharacters.Contains(id),
					});

					menu.Items.Add(new ToolStripSeparator());
				};
			}

			menu.Items.Add(new ToolStripMenuItem("Sort by name", null, (s, e) => {
				if (AppSettings.User.SortGroups != AppSettings.CharacterSortOrder.ByName)
				{
					AppSettings.User.SortGroups = AppSettings.CharacterSortOrder.ByName;
					PopulateTree(true);
				}
			}) 
			{
				Checked = AppSettings.User.SortGroups == AppSettings.CharacterSortOrder.ByName,
			});	
			
			menu.Items.Add(new ToolStripMenuItem("Sort by date", null, (s, e) => {
				if (AppSettings.User.SortGroups != AppSettings.CharacterSortOrder.ByCreation)
				{
					AppSettings.User.SortGroups = AppSettings.CharacterSortOrder.ByCreation;
					PopulateTree(true);
				}
			}) 
			{
				Checked = AppSettings.User.SortGroups == AppSettings.CharacterSortOrder.ByCreation,
			});	

			menu.Items.Add(new ToolStripMenuItem("Sort by last message", null, (s, e) => {
				if (AppSettings.User.SortGroups != AppSettings.CharacterSortOrder.ByLastMessage)
				{
					AppSettings.User.SortGroups = AppSettings.CharacterSortOrder.ByLastMessage;
					PopulateTree(true);
				}
			}) 
			{
				Checked = AppSettings.User.SortGroups == AppSettings.CharacterSortOrder.ByLastMessage,
			});

			menu.Items.Add(new ToolStripMenuItem("Sort by custom order", null, (s, e) => {
				if (AppSettings.User.SortGroups != AppSettings.CharacterSortOrder.ByCustom)
				{
					AppSettings.User.SortGroups = AppSettings.CharacterSortOrder.ByCustom;
					PopulateTree(true);
				}
			}) 
			{
				Checked = AppSettings.User.SortGroups == AppSettings.CharacterSortOrder.ByCustom,
			});

			Theme.Apply(menu);
			menu.Show(control, location);
		}

		public override void ApplyTheme()
		{
			base.ApplyTheme();
			
			treeView.ForeColor = Theme.Current.TreeViewForeground;
			treeView.BackColor = Theme.Current.TreeViewBackground;
			treeView.ImageList = Theme.IsDarkModeEnabled ? imageList_Dark : imageList_Light;

			listPanel.BackColor = Theme.Current.TreeViewBackground;
		}

		private void filterTextBox_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			_timer.Stop();
			_timer.Start();
		}

		private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			_filterString = filterTextBox.Text.Trim().ToLowerInvariant();
			PopulateTree(false);
		}	
	}
}
