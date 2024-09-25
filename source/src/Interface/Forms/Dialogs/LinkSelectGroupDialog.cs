using Ginger.Integration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Backyard = Ginger.Integration.Backyard;

namespace Ginger
{
	public partial class LinkSelectGroupDialog : FormEx
	{
		public CharacterInstance[] Characters;
		public GroupInstance[] Groups;
		public FolderInstance[] Folders;
		public GroupInstance SelectedGroup { get; private set; }

		private Dictionary<string, CharacterInstance> _charactersById;
		private Dictionary<string, int> _folderCounts = new Dictionary<string, int>();
		private Dictionary<string, Backyard.ChatCount> _chatCounts;

		public LinkSelectGroupDialog()
		{
			InitializeComponent();

			this.Load += OnLoad;
		}

		private void OnLoad(object sender, EventArgs e)
		{
			_charactersById = Characters.ToDictionary(c => c.instanceId, c => c);
			if (Backyard.GetChatCounts(out _chatCounts) != Backyard.Error.NoError)
				_chatCounts = new Dictionary<string, Backyard.ChatCount>(); // Empty

			if (Groups.ContainsAny(g => g.members.Length > 2))
				this.Text = "Select a character or group";

			PopulateTree(false);

			treeView.SelectedNode = null;
			SelectedGroup = default(GroupInstance);

			btnOk.Enabled = false;
		}

		private string GetGroupName(GroupInstance group)
		{
			if (string.IsNullOrEmpty(group.name) == false)
			{
				return group.name;
			}
			else
			{
				var characters = group.members
					.Select(id => _charactersById.GetOrDefault(id))
					.Where(c => c.isUser == false);

				if (characters.Count() > 1)
				{
					string[] memberNames = characters
						.Select(c => c.name ?? Constants.DefaultCharacterName)
						.OrderBy(c => c)
						.ToArray();
					string groupTitle = string.Join(", ", memberNames.Take(3));
					if (memberNames.Length > 3)
						groupTitle += ", ...";
					
					return groupTitle;
				}
				else
				{
					return characters
						.Select(c => c.displayName)
						.FirstOrDefault() ?? Constants.DefaultCharacterName;
				}
			}
		}

		private DateTime GetLatestMessageTime(GroupInstance group)
		{
			Backyard.ChatCount count;
			if (_chatCounts.TryGetValue(group.instanceId, out count))
				return count.lastMessage;
			return DateTime.MinValue;
		}

		private void PopulateTree(bool bRefresh)
		{
			HashSet<string> openedFolders = new HashSet<string>();
			foreach (var node in treeView.AllNodes())
			{
				if (node.IsExpanded && node.Tag is string)
					openedFolders.Add(node.Tag as string);
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

			var expandedNodes = new List<TreeNode>();
			while (openList.Count > 0)
			{
				string parentId = openList[0];
				var subfolders = Folders
					.Where(f => f.parentId == parentId)
					.OrderBy(f => f.name);

				foreach (var folder in subfolders)
				{
					var folderNode = CreateFolderNode(folder, nodesById, _folderCounts[folder.instanceId]);
					if (openedFolders.Contains(folder.instanceId))
						expandedNodes.Add(folderNode);
				}
				
				openList.Remove(parentId);
			}

			// Create group nodes
			IEnumerable<GroupInstance> sortedGroups = Groups;
			if (AppSettings.User.SortGroups == AppSettings.CharacterSortOrder.ByName)
			{
				sortedGroups = sortedGroups
					.OrderBy(g => GetGroupName(g))
					.ThenByDescending(c => c.creationDate);
			}
			else if (AppSettings.User.SortGroups == AppSettings.CharacterSortOrder.ByCreation)
			{
				sortedGroups = sortedGroups
					.OrderByDescending(g => g.creationDate);
			}
			else if (AppSettings.User.SortGroups == AppSettings.CharacterSortOrder.ByLastMessage)
			{
				sortedGroups = sortedGroups
					.OrderByDescending(g => GetLatestMessageTime(g));
			}

			foreach (var group in sortedGroups)
				CreateGroupNode(group, nodesById);

			if (bRefresh)
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

		private TreeNode CreateGroupNode(GroupInstance group, Dictionary<string, TreeNode> nodes)
		{
			if (group.members.Length < 2)
				return null;

			TreeNode parentNode;
			nodes.TryGetValue(group.folderId, out parentNode);

			string groupLabel = GetGroupName(group);
			var sbTooltip = new StringBuilder();

			CharacterInstance[] characters = group.members
				.Select(id => _charactersById.GetOrDefault(id))
				.Where(c => c.isUser == false)
				.ToArray();

			string[] characterNames = characters
				.Select(c => Utility.FirstNonEmpty(c.name, Constants.DefaultCharacterName))
				.ToArray();

			if (characterNames.Length >= 2)
			{
				sbTooltip.Append("Group including ");
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
			int icon = 1;
			if (characters.Length >= 2)
				icon = 5; // Group
			else if (characters.Length == 1)
			{
				string inferredGender = characters[0].inferredGender;
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
			}

			if (chatCount.count == 0)
				icon = 11; // Error icon
			else if (chatCount.hasMessages == false)
				icon += 5; // Empty chat

			var node = new TreeNode(groupLabel, icon, icon);
			node.Tag = group;
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
			if (e.Node != null && e.Node.Tag is GroupInstance)
			{
				SelectedGroup = (GroupInstance)e.Node.Tag;
				btnOk.Enabled = true;
			}
			else
			{
				SelectedGroup = default(GroupInstance);
				btnOk.Enabled = false;
			}
		}

		private void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Node != null && e.Node.Tag is GroupInstance && e.Node == treeView.SelectedNode)
			{
				SelectedGroup = (GroupInstance)e.Node.Tag;
				BtnOk_Click(this, EventArgs.Empty);
			}
			else
			{
				return; // Double-clicked folder: Do nothing
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

			VisualTheme.ApplyTheme(menu);
			menu.Show(control, location);
		}

		public override void ApplyTheme()
		{
			base.ApplyTheme();
			
			treeView.ForeColor = VisualTheme.Theme.TreeViewForeground;
			treeView.BackColor = VisualTheme.Theme.TreeViewBackground;
			treeView.ImageList = VisualTheme.DarkModeEnabled ? imageList_Dark : imageList_Light;

			listPanel.BackColor = VisualTheme.Theme.TextBoxBackground;
		}
	}
}
