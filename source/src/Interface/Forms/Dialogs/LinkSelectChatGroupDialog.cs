using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Bridge = Ginger.BackyardBridge;

namespace Ginger
{
	public partial class LinkSelectChatGroupDialog : Form
	{
		public Bridge.CharacterInstance[] Characters;
		public Bridge.GroupInstance[] Groups;
		public Bridge.FolderInstance[] Folders;
		public Bridge.GroupInstance SelectedGroup { get; private set; }

		private Dictionary<string, Bridge.CharacterInstance> _charactersById;
		public Dictionary<string, int> _folderCounts = new Dictionary<string, int>();
		public Dictionary<string, int> _chatCounts;

		public LinkSelectChatGroupDialog()
		{
			InitializeComponent();

			this.Load += OnLoad;
		}

		private void OnLoad(object sender, EventArgs e)
		{
			_charactersById = Characters.ToDictionary(c => c.instanceId, c => c);
			if (Bridge.GetChatCounts(out _chatCounts) != Bridge.Error.NoError)
				_chatCounts = new Dictionary<string, int>(); // Empty

			PopulateTree();

			treeView.SelectedNode = null;
			SelectedGroup = default(Bridge.GroupInstance);

			btnOk.Enabled = false;
		}

		private string GetGroupTitle(Bridge.GroupInstance group)
		{
			if (string.IsNullOrEmpty(group.name) == false)
				return group.name;
			else
			{
				var characters = group.members
					.Select(id => _charactersById.GetOrDefault(id))
					.Where(c => c.isUser == false);

				if (characters.Count() > 1)
				{
					string[] memberNames = characters
						.Select(c => c.name ?? "Unnamed")
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
						.FirstOrDefault() ?? "Unnamed";
				}
			}
		}

		private void PopulateTree()
		{
			treeView.BeginUpdate();
			treeView.Nodes.Clear();

			if (Folders == null || Groups == null)
				return; // Nothing to show

			// Sum character counts
			for (int i = 0; i < Folders.Length; ++i)
				_folderCounts.Add(Folders[i].instanceId, Groups.Count(c => c.folderId == Folders[i].instanceId));
			for (int i = Folders.Length - 1; i >= 0; --i)
			{
				if (string.IsNullOrEmpty(Folders[i].parentId) == false)
					_folderCounts[Folders[i].parentId] += _folderCounts[Folders[i].instanceId];
			}

			// Create folder nodes
			var nodesById = new Dictionary<string, TreeNode>();
			string rootId = Folders.FirstOrDefault(f => f.isRoot).instanceId;
			nodesById.Add(rootId, null);

			var openList = new List<string>(Folders
				.Select(f => f.instanceId)
				.Distinct());

			while (openList.Count > 0)
			{
				string parentId = openList[0];
				var subfolders = Folders
					.Where(f => f.parentId == parentId)
					.OrderBy(f => f.name);

				foreach (var folder in subfolders)
					CreateFolderNode(folder, nodesById, _folderCounts[folder.instanceId]);
				
				openList.Remove(parentId);
			}

			// Create group nodes

			// First group chats...
			foreach (var group in Groups
				.Where(g => g.members.Length > 2)
				.OrderByDescending(c => c.creationDate))
			{
				CreateGroupNode(group, nodesById);
			}
			// Then one-on-ones
			foreach (var group in Groups
				.Where(g => g.members.Length <= 2)
				.OrderBy(g => GetGroupTitle(g))
				.ThenByDescending(c => c.creationDate))
			{
				CreateGroupNode(group, nodesById);
			}
			treeView.EndUpdate();
		}

		private TreeNode CreateFolderNode(Bridge.FolderInstance folder, Dictionary<string, TreeNode> nodes, int count)
		{
			TreeNode parentNode;
			nodes.TryGetValue(folder.parentId, out parentNode);

			var node = new TreeNode(string.Format("{0} ({1})", folder.name, count), 0, 0);
			if (parentNode != null)
				parentNode.Nodes.Add(node);
			else
				treeView.Nodes.Add(node);
			nodes.Add(folder.instanceId, node);
			return node;
		}

		private TreeNode CreateGroupNode(Bridge.GroupInstance group, Dictionary<string, TreeNode> nodes)
		{
			if (group.members.Length < 2)
				return null;

			TreeNode parentNode;
			nodes.TryGetValue(group.folderId, out parentNode);

			string groupLabel = GetGroupTitle(group);
			var sbTooltip = new StringBuilder();

			string[] characterNames = group.members
				.Select(id => _charactersById.GetOrDefault(id))
				.Where(c => c.isUser == false)
				.Select(c => c.name ?? "Unnamed")
				.ToArray();

			if (characterNames.Length >= 2)
			{
				sbTooltip.Append("Group including ");
				sbTooltip.Append(Utility.CommaSeparatedList(characterNames));
			}
			else if (characterNames.Length == 1)
			{
				sbTooltip.Append(characterNames[0]);
			}

			int chatCount;
			if (_chatCounts.TryGetValue(group.instanceId, out chatCount))
				sbTooltip.AppendFormat(" ({0} {1})", chatCount, chatCount == 1 ? "chat" : "chats");

			sbTooltip.NewParagraph();
			sbTooltip.AppendLine($"Created: {group.creationDate.ToShortDateString()}");
			sbTooltip.AppendLine($"Last modified: {group.updateDate.ToShortDateString()}");

			int icon = group.members.Length > 2 ? 2 : 1;
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
			if (e.Node == null || e.Node.Tag == null)
			{
				SelectedGroup = default(Bridge.GroupInstance);
				btnOk.Enabled = false;
			}
			else
			{
				SelectedGroup = (Bridge.GroupInstance)e.Node.Tag;
				btnOk.Enabled = true;
			}
		}

		private void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Node == null || e.Node.Tag == null || e.Node != treeView.SelectedNode)
			{
				return; // Double-clicked folder: Do nothing
			}
			else
			{
				SelectedGroup = (Bridge.GroupInstance)e.Node.Tag;
				BtnOk_Click(this, EventArgs.Empty);
			}
		}
	}
}
