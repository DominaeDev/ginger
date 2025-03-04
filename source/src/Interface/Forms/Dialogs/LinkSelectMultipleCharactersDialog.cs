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

	public partial class LinkSelectMultipleCharactersDialog : FormEx
	{
		public CharacterInstance[] Characters;
		public FolderInstance[] Folders;

		private struct NodeState {
			public CharacterInstance character;
			public bool bChecked;
		}
		private NodeState[] Nodes;

		public Dictionary<string, int> _folderCounts = new Dictionary<string, int>();
		private Dictionary<string, Backyard.ChatCount> _chatCounts;

		private bool _bIgnoreEvents = false;

		public LinkSelectMultipleCharactersDialog()
		{
			InitializeComponent();

			this.Load += OnLoad;
		}

		private void OnLoad(object sender, EventArgs e)
		{
			if (Backyard.ConnectionEstablished == false || Backyard.Database.GetChatCounts(out _chatCounts) != Backyard.Error.NoError)
				_chatCounts = new Dictionary<string, Backyard.ChatCount>(); // Empty

			Nodes = Characters.Select(c => new NodeState() {
				bChecked = false,
				character = c,
			}).ToArray();

			PopulateTree(false);

			treeView.SelectedNode = null;

			RefreshSelectAllCheckbox();
			RefreshConfirmButton();
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

			if (Folders == null || Characters == null)
				return; // Nothing to show

			if (bRefresh == false)
			{
				_folderCounts.Clear();
				// Sum character counts
				for (int i = 0; i < Folders.Length; ++i)
				{
					_folderCounts.Add(Folders[i].instanceId, Characters.Count(c => c.folderId == Folders[i].instanceId));
				}

				for (int i = Folders.Length - 1; i >= 0; --i)
				{
					if (string.IsNullOrEmpty(Folders[i].parentId) == false)
						_folderCounts[Folders[i].parentId] += _folderCounts[Folders[i].instanceId];
				}
			}

			// Create folders
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

			// Create characters
			IEnumerable<NodeState> sortedNodes = Nodes;
			if (AppSettings.User.SortCharacters == AppSettings.CharacterSortOrder.ByName)
			{
				sortedNodes = sortedNodes
					.OrderBy(n => n.character.displayName)
					.ThenByDescending(n => n.character.creationDate);
			}
			else if (AppSettings.User.SortCharacters == AppSettings.CharacterSortOrder.ByCreation)
			{
				sortedNodes = sortedNodes
					.OrderByDescending(n => n.character.creationDate)
					.ThenBy(n => n.character.displayName);
			}
			else if (AppSettings.User.SortCharacters == AppSettings.CharacterSortOrder.ByLastMessage)
			{
				sortedNodes = sortedNodes
					.OrderByDescending(n => GetLatestMessageTime(n.character))
					.ThenBy(n => n.character.displayName);
			}
			else if (AppSettings.User.SortCharacters == AppSettings.CharacterSortOrder.ByCustom)
			{
				sortedNodes = sortedNodes
					.OrderBy(n => n.character.folderSortPosition);
			}

			foreach (var node in sortedNodes)
				CreateCharacterNode(node, nodesById);

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
			node.Tag = folder.instanceId;
			if (parentNode != null)
				parentNode.Nodes.Add(node);
			else
				treeView.Nodes.Add(node);
			nodes.Add(folder.instanceId, node);
			return node;
		}

		private TreeNode CreateCharacterNode(NodeState nodeState, Dictionary<string, TreeNode> nodes)
		{
			TreeNode parentNode;
			var character = nodeState.character;
			if (character.folderId != null)
				nodes.TryGetValue(character.folderId, out parentNode);
			else
				parentNode = default(TreeNode);

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

			int icon;
			if (string.IsNullOrEmpty(inferredGender))
				icon = 1;
			else if (string.Compare(inferredGender, "male", StringComparison.OrdinalIgnoreCase) == 0)
				icon = 2;
			else if (string.Compare(inferredGender, "female", StringComparison.OrdinalIgnoreCase) == 0)
				icon = 3;
			else if (string.Compare(inferredGender, "transgender", StringComparison.OrdinalIgnoreCase) == 0)
				icon = 1;
			else if (string.Compare(inferredGender, "non-binary", StringComparison.OrdinalIgnoreCase) == 0)
				icon = 1;
			else 
				icon = 4;
			if (character.hasLorebook)
				icon += 4;

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

		private DateTime GetLatestMessageTime(CharacterInstance characterInstance)
		{
			if (string.IsNullOrEmpty(characterInstance.groupId))
				return DateTime.MinValue;
			Backyard.ChatCount count;
			if (_chatCounts.TryGetValue(characterInstance.groupId, out count))
				return count.lastMessage;
			return DateTime.MinValue;
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			Characters = Nodes
				.Where(n => n.bChecked)
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
				&& e.Node != null && e.Node.Tag is CharacterInstance && e.Node == treeView.SelectedNode)
				e.Node.Checked = !e.Node.Checked;
		}

		private void treeView_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
				ShowContextMenu(sender as Control, new Point(e.X, e.Y));
		}

		private void ShowContextMenu(Control control, Point location)
		{
			ContextMenuStrip menu = new ContextMenuStrip();
			menu.Items.Add(new ToolStripMenuItem("Sort by name", null, (s, e) => {
				if (AppSettings.User.SortCharacters != AppSettings.CharacterSortOrder.ByName)
				{
					AppSettings.User.SortCharacters = AppSettings.CharacterSortOrder.ByName;
					PopulateTree(true);
				}
			}) 
			{
				Checked = AppSettings.User.SortCharacters == AppSettings.CharacterSortOrder.ByName,
			});	
			
			menu.Items.Add(new ToolStripMenuItem("Sort by date", null, (s, e) => {
				if (AppSettings.User.SortCharacters != AppSettings.CharacterSortOrder.ByCreation)
				{
					AppSettings.User.SortCharacters = AppSettings.CharacterSortOrder.ByCreation;
					PopulateTree(true);
				}
			}) 
			{
				Checked = AppSettings.User.SortCharacters == AppSettings.CharacterSortOrder.ByCreation,
			});	

			menu.Items.Add(new ToolStripMenuItem("Sort by last message", null, (s, e) => {
				if (AppSettings.User.SortCharacters != AppSettings.CharacterSortOrder.ByLastMessage)
				{
					AppSettings.User.SortCharacters = AppSettings.CharacterSortOrder.ByLastMessage;
					PopulateTree(true);
				}
			}) 
			{
				Checked = AppSettings.User.SortCharacters == AppSettings.CharacterSortOrder.ByLastMessage,
			});

			menu.Items.Add(new ToolStripMenuItem("Sort by custom order", null, (s, e) => {
				if (AppSettings.User.SortCharacters != AppSettings.CharacterSortOrder.ByCustom)
				{
					AppSettings.User.SortCharacters = AppSettings.CharacterSortOrder.ByCustom;
					PopulateTree(true);
				}
			}) 
			{
				Checked = AppSettings.User.SortCharacters == AppSettings.CharacterSortOrder.ByCustom,
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
			else if (e.Node.Tag is CharacterInstance) // Character
			{
				var characterId = ((CharacterInstance)e.Node.Tag).instanceId;
				int index = Array.FindIndex(Nodes, n => n.character.instanceId == characterId);
				if (index != -1)
					Nodes[index].bChecked = e.Node.Checked;
			}

			RefreshParentNode(e.Node);

			_bIgnoreEvents = false;

			RefreshSelectAllCheckbox();
			RefreshConfirmButton();
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

				if (node.Tag is CharacterInstance)
				{
					var characterId = ((CharacterInstance)node.Tag).instanceId;
					int index = Array.FindIndex(Nodes, n => n.character.instanceId == characterId);
					if (index != -1)
						Nodes[index].bChecked = bChecked;
				}
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
			int count = treeView.AllNodes().Count(n => n.Checked && n.Tag is CharacterInstance);

			btnOk.Text = count > 0 ? $"Select ({count})" : "Select";
			btnOk.Enabled = count > 0;
		}
	}
}