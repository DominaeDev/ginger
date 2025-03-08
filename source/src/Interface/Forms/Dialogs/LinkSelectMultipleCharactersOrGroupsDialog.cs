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

	public partial class LinkSelectMultipleCharactersOrGroupsDialog : FormEx
	{
		[Flags]
		public enum Option
		{
			None = 0,
			Orphans = 1 << 0,
			Solo = 1 << 1,
			Parties = 1 << 2,
		}

		public CharacterInstance[] SelectedCharacters { get; private set; }
		public GroupInstance[] SelectedGroups { get; private set; }
		public Option Options { get; set; }

		private CharacterInstance[] Characters;
		private CharacterInstance[] Orphans;
		private GroupInstance[] Groups;
		private FolderInstance[] Folders;

		private struct NodeState {
			public CharacterInstance character;
			public GroupInstance group;
			public bool bChecked;
		}
		private NodeState[] Nodes;

		private Dictionary<string, CharacterInstance> _charactersById;
		private Dictionary<string, int> _folderCounts = new Dictionary<string, int>();
		private Dictionary<string, Backyard.ChatCount> _chatCounts;

		private bool _bIgnoreEvents = false;

		public LinkSelectMultipleCharactersOrGroupsDialog()
		{
			InitializeComponent();

			this.Load += OnLoad;
		}

		private void OnLoad(object sender, EventArgs e)
		{
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
			{
				this.Groups = new GroupInstance[0];
			}

			_charactersById = Characters.ToDictionary(c => c.instanceId, c => c);

			BackyardUtil.GetChatCounts(out _chatCounts);

			if (Groups.ContainsAny(g => g.Count > 2))
				this.Text = "Select character or group";

			Nodes = Groups
				.Select(g => new NodeState() {
					bChecked = false,
					group = g,
				})
				.Union(
					Orphans.Select(c => new NodeState() {
						bChecked = false,
						character = c,
					})
				)
				.ToArray();

			PopulateTree(false);

			treeView.SelectedNode = null;

			RefreshSelectAllCheckbox();
			RefreshConfirmButton();
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
			var expandedFolders = new HashSet<string>();
			var expandingNodes = new List<TreeNode>();
			var checkedFolders = new HashSet<string>();
			foreach (var node in treeView.AllNodes())
			{
				if (node.Tag is string) // Folder
				{
					if (node.IsExpanded)
						expandedFolders.Add(node.Tag as string);
					if (node.Checked)
						checkedFolders.Add(node.Tag as string);
				}
			}

			treeView.BeginUpdate();
			treeView.Nodes.Clear();

			if (Folders == null || Groups == null)
				return; // Nothing to show

			if (bRefresh == false)
			{
				_folderCounts.Clear();
				// Sum character counts
				for (int i = 0; i < Folders.Length; ++i)
					_folderCounts.Add(Folders[i].instanceId, Groups.Count(c => c.folderId == Folders[i].instanceId));
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

			_bIgnoreEvents = true;
			while (openList.Count > 0)
			{
				string parentId = openList[0];
				var subfolders = Folders
					.Where(f => f.parentId == parentId)
					.OrderBy(c => c.name);

				foreach (var folder in subfolders)
				{
					var folderNode = CreateFolderNode(folder, nodesById, _folderCounts[folder.instanceId]);
					if (expandedFolders.Contains(folder.instanceId))
						expandingNodes.Add(folderNode);
					if (checkedFolders.Contains(folder.instanceId))
						folderNode.Checked = true;
				}
				
				openList.Remove(parentId);
			}

			// Create group nodes
			IEnumerable<NodeState> sortedGroups = Nodes.Where(n => n.group.isDefined);
			IEnumerable<NodeState> sortedOrphans = Nodes.Where(n => n.character.isDefined);
			if (AppSettings.User.SortGroups == AppSettings.CharacterSortOrder.ByName)
			{
				sortedGroups = sortedGroups
					.OrderBy(n => n.group.GetDisplayName())
					.ThenByDescending(n => n.group.creationDate);
				sortedOrphans = sortedOrphans
					.OrderBy(n => n.character.displayName)
					.ThenByDescending(n => n.character.creationDate);
			}
			else if (AppSettings.User.SortGroups == AppSettings.CharacterSortOrder.ByCreation)
			{
				sortedGroups = sortedGroups
					.OrderByDescending(n => n.group.creationDate);
				sortedOrphans = sortedOrphans
					.OrderByDescending(n => n.character.creationDate);
			}
			else if (AppSettings.User.SortGroups == AppSettings.CharacterSortOrder.ByLastMessage)
			{
				sortedGroups = sortedGroups
					.OrderByDescending(n => GetLatestMessageTime(n.group));
				sortedOrphans = sortedOrphans
					.OrderByDescending(n => GetLatestMessageTime(n.character));
			}
			else if (AppSettings.User.SortGroups == AppSettings.CharacterSortOrder.ByCustom)
			{
				sortedGroups = sortedGroups
					.OrderBy(n => n.group.folderSortPosition);
				sortedOrphans = sortedOrphans
					.OrderByDescending(n => n.character.creationDate); // Not applicable
			}
						
			int nOrphans = sortedOrphans.Count();
			if (nOrphans > 0 && Options.ContainsAny(Option.Orphans) )
			{
				TreeNode folderNode = null;

				if (sortedGroups.Count() > 0)
				{
					folderNode = new TreeNode(string.Format("Single characters ({0})", nOrphans), 1, 1);
					folderNode.Tag = "Orphans";
					treeView.Nodes.Insert(0, folderNode);
					if (expandedFolders.Contains(folderNode.Tag))
						expandingNodes.Add(folderNode);
				}
				
				foreach (var character in sortedOrphans)
					CreateCharacterNode(character, folderNode, nodesById, true);
			}

			foreach (var node in sortedGroups)
				CreateGroupNode(node, nodesById);

			if (bRefresh)
			{
				for (int i = expandingNodes.Count - 1; i >= 0; --i)
					expandingNodes[i].Expand();
			}

			treeView.EndUpdate();
			_bIgnoreEvents = false;
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

		private TreeNode CreateGroupNode(NodeState nodeState, Dictionary<string, TreeNode> nodes)
		{
			TreeNode parentNode;
			var group = nodeState.group;
			if (group.Count < 2)
				return null;

			nodes.TryGetValue(group.folderId, out parentNode);

			string groupLabel = group.GetDisplayName();
			var sbTooltip = new StringBuilder();

			CharacterInstance[] characters = group.members
				.Select(id => _charactersById.GetOrDefault(id))
				.OrderBy(c => c.creationDate)
				.Where(c => c.isCharacter)
				.ToArray();

			string[] characterNames = characters
				.Select(c => Utility.FirstNonEmpty(c.name, Constants.DefaultCharacterName))
				.ToArray();

			if (characterNames.Length >= 2)
			{
				groupLabel = string.Concat("(Group chat) ", groupLabel);

				sbTooltip.Append("Group chat with ");
				sbTooltip.Append(Utility.CommaSeparatedList(characterNames));
			}
			else if (characterNames.Length == 1)
			{
				sbTooltip.Append("Name: ");
				sbTooltip.Append(characterNames[0]);
			}

			Backyard.ChatCount chatCount;
			if (_chatCounts.TryGetValue(group.instanceId, out chatCount))
			{
				if (chatCount.hasMessages)
					sbTooltip.AppendFormat(" ({0} {1})",
						chatCount.count,
						chatCount.count == 1 ? "chat" : "chats");
				else
					sbTooltip.Append(" (No messages)");
			}
			else
				sbTooltip.Append(" (No chats found)");

			sbTooltip.NewParagraph();
			sbTooltip.AppendLine($"Created: {group.creationDate.ToShortDateString()}");
			sbTooltip.AppendLine($"Last modified: {group.updateDate.ToShortDateString()}");

			// Icon
			int icon = 2;
			if (characters.Length >= 2)
				icon = 10; // Group
			else if (characters.Length == 1)
			{
				string inferredGender = characters[0].inferredGender;
				if (string.IsNullOrEmpty(inferredGender))
					icon = 2;
				else if (string.Compare(inferredGender, "male", StringComparison.OrdinalIgnoreCase) == 0)
					icon = 3;
				else if (string.Compare(inferredGender, "female", StringComparison.OrdinalIgnoreCase) == 0)
					icon = 4;
				else if (string.Compare(inferredGender, "transgender", StringComparison.OrdinalIgnoreCase) == 0)
					icon = 2;
				else if (string.Compare(inferredGender, "non-binary", StringComparison.OrdinalIgnoreCase) == 0)
					icon = 2;
				else // Other, futa
					icon = 5;

				if (characters[0].hasLorebook)
					icon += 4; // Lore
			}

			var node = new TreeNode(groupLabel, icon, icon);
			node.Tag = group;
			node.ToolTipText = sbTooltip.ToString();
			node.Checked = nodeState.bChecked;
			if (parentNode != null)
				parentNode.Nodes.Add(node);
			else
				treeView.Nodes.Add(node);
			return node;
		}

		private TreeNode CreateCharacterNode(NodeState nodeState, TreeNode parentNode, Dictionary<string, TreeNode> nodes, bool bGrayed)
		{
			var character = nodeState.character;

			string label = character.displayName;
			if (string.Compare(character.name, label, StringComparison.OrdinalIgnoreCase) != 0)
				label = string.Concat(label, " (", character.name, ")");

			string inferredGender = character.inferredGender;
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
			if (string.IsNullOrEmpty(inferredGender) == false)
			{
				sbTooltip.NewLine();
				sbTooltip.AppendFormat("Gender: {0} (Inferred)", inferredGender);
			}

			sbTooltip.NewParagraph();
			sbTooltip.AppendLine($"Created: {character.creationDate.ToShortDateString()}");
			sbTooltip.AppendLine($"Last modified: {character.updateDate.ToShortDateString()}");

			// Set icon
			int icon;
			if (string.IsNullOrEmpty(inferredGender))
				icon = 2;
			else if (string.Compare(inferredGender, "male", StringComparison.OrdinalIgnoreCase) == 0)
				icon = 3;
			else if (string.Compare(inferredGender, "female", StringComparison.OrdinalIgnoreCase) == 0)
				icon = 4;
			else if (string.Compare(inferredGender, "transgender", StringComparison.OrdinalIgnoreCase) == 0)
				icon = 2;
			else if (string.Compare(inferredGender, "non-binary", StringComparison.OrdinalIgnoreCase) == 0)
				icon = 2;
			else 
				icon = 5;
			if (character.hasLorebook)
				icon += 4;
			if (bGrayed)
				icon += 9; // Grayed out

			var node = new TreeNode(label, icon, icon);
			node.Tag = character;
			node.ToolTipText = sbTooltip.ToString();
			node.Checked = nodeState.bChecked;
			if (parentNode != null)
				parentNode.Nodes.Add(node);
			else
				treeView.Nodes.Add(node);
			return node;
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			SelectedGroups = Nodes
				.Where(n => n.bChecked && n.group.isDefined)
				.Select(n => n.group)
				.ToArray();
			SelectedCharacters = Nodes
				.Where(n => n.bChecked&& n.character.isDefined)
				.Select(n => n.character)
				.ToArray();
			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Button == MouseButtons.Left
				&& e.Node != null
				&& e.Node == treeView.SelectedNode
				&& (e.Node.Tag is GroupInstance || e.Node.Tag is CharacterInstance))
			{
				e.Node.Checked = !e.Node.Checked;
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

		private void treeView_AfterCheck(object sender, TreeViewEventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			_bIgnoreEvents = true;
			if (e.Node.Tag is string) // Is folder
			{
				foreach (TreeNode node in e.Node.Nodes)
					node.Checked = e.Node.Checked;
				CheckChildren(e.Node, e.Node.Checked);
			}
			else
			{
				OnCheckNode(e.Node);
			}

			RefreshParentNode(e.Node);

			_bIgnoreEvents = false;

			RefreshSelectAllCheckbox();
			RefreshConfirmButton();
		}

		private void OnCheckNode(TreeNode node)
		{
			if (node.Tag is GroupInstance) // Group
			{
				var groupId = ((GroupInstance)node.Tag).instanceId;
				int index = Array.FindIndex(Nodes, n => n.group.instanceId == groupId);
				if (index != -1)
					Nodes[index].bChecked = node.Checked;
			}
			else if (node.Tag is CharacterInstance) // Group
			{
				var characterId = ((CharacterInstance)node.Tag).instanceId;
				int index = Array.FindIndex(Nodes, n => n.character.instanceId == characterId);
				if (index != -1)
					Nodes[index].bChecked = node.Checked;
			}
		}

		private static void RefreshParentNode(TreeNode node)
		{
			if (node == null || node.Parent == null)
				return;

			bool bAllChecked = true;
			foreach (TreeNode sibling in node.Parent.Nodes)
			{
				if (sibling.Checked == false)
				{
					bAllChecked = false;
					break;
				}
			}

			node.Parent.Checked = bAllChecked;
			RefreshParentNode(node.Parent);
		}

		private void CheckChildren(TreeNode parent, bool bChecked)
		{
			foreach (TreeNode node in parent.Nodes)
			{
				node.Checked = bChecked;
				CheckChildren(node, bChecked);
				OnCheckNode(node);
			}
		}

		private void cbSelectAll_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			bool bChecked = cbSelectAll.Checked;
			foreach (TreeNode node in treeView.AllNodes())
				node.Checked = bChecked;

			for (int i = 0; i < Nodes.Length; ++i)
				Nodes[i].bChecked = bChecked;

			this.Invalidate();
		}

		private void RefreshSelectAllCheckbox()
		{
			if (_bIgnoreEvents)
				return;

			var checkState = CheckState.Unchecked;

			foreach (TreeNode node in treeView.AllNodes())
			{
				if (node.Checked == false)
				{
					if (checkState == CheckState.Checked)
					{
						checkState = CheckState.Indeterminate;
						break;
					}
				}
				else
					checkState = CheckState.Checked;
			}

			_bIgnoreEvents = true;
			cbSelectAll.CheckState = checkState;
			_bIgnoreEvents = false;
		}

		private void RefreshConfirmButton()
		{
			int count = treeView.AllNodes().Count(n => n.Checked && (n.Tag is GroupInstance || n.Tag is CharacterInstance));

			btnOk.Text = count > 0 ? $"Select ({count})" : "Select";
			btnOk.Enabled = count > 0;
		}
	}
}
