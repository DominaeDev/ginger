using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Bridge = Ginger.BackyardBridge;

namespace Ginger
{
	public partial class LinkSelectCharacterDialog : Form
	{
		public Bridge.CharacterInstance[] Characters;
		public Bridge.FolderInstance[] Folders;
		public Bridge.CharacterInstance SelectedCharacter { get; private set; }

		public Dictionary<string, int> _folderCounts = new Dictionary<string, int>();
		private Dictionary<string, Bridge.ChatCount> _chatCounts;

		public bool ShouldLink { get { return cbCreateLink.Checked; } }

		public LinkSelectCharacterDialog()
		{
			InitializeComponent();

			this.Load += OnLoad;
		}

		private void OnLoad(object sender, EventArgs e)
		{
			if (Bridge.GetChatCounts(out _chatCounts) != Bridge.Error.NoError)
				_chatCounts = new Dictionary<string, Bridge.ChatCount>(); // Empty

			PopulateTree(false);

			treeView.SelectedNode = null;
			SelectedCharacter = default(Bridge.CharacterInstance);

			btnOk.Enabled = false;
			cbCreateLink.Checked = AppSettings.BackyardLink.LinkOnImport;
			
			// Tooltips
			toolTip.SetToolTip(cbCreateLink, Resources.tooltip_link_about_linking);
		}

		private void PopulateTree(bool bRefresh)
		{
			var expandedFolders = new HashSet<string>();
			var expandingNodes = new List<TreeNode>();
			foreach (var node in treeView.AllNodes())
			{
				if (node.IsExpanded && node.Tag is string)
					expandedFolders.Add(node.Tag as string);
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
					_folderCounts.Add(Folders[i].instanceId, Characters.Count(c => c.folderId == Folders[i].instanceId));
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
				}
				
				openList.Remove(parentId);
			}

			// Create characters
			IEnumerable<Bridge.CharacterInstance> sortedCharacters = Characters.Where(c => c.isUser == false);
			if (AppSettings.User.SortCharactersAlphabetically)
			{
				sortedCharacters = sortedCharacters
					.OrderBy(c => c.displayName)
					.ThenByDescending(c => c.creationDate);
			}
			else
			{
				sortedCharacters = sortedCharacters
					.OrderByDescending(c => GetLatestMessageTime(c))
					.ThenBy(c => c.displayName);
			}
			foreach (var character in sortedCharacters)
				CreateCharacterNode(character, nodesById);

			if (bRefresh)
			{
				for (int i = expandingNodes.Count - 1; i >= 0; --i)
					expandingNodes[i].Expand();
			}
			treeView.EndUpdate();
		}

		private TreeNode CreateFolderNode(Bridge.FolderInstance folder, Dictionary<string, TreeNode> nodes, int count)
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

		private TreeNode CreateCharacterNode(Bridge.CharacterInstance character, Dictionary<string, TreeNode> nodes)
		{
			TreeNode parentNode;
			if (character.folderId != null)
				nodes.TryGetValue(character.folderId, out parentNode);
			else
				parentNode = default(TreeNode);

			string label = character.displayName;
			if (string.Compare(character.name, label, StringComparison.OrdinalIgnoreCase) != 0)
				label = string.Concat(label, " (", character.name, ")");

			var sbTooltip = new StringBuilder();
			sbTooltip.Append(character.displayName);
			if (string.Compare(character.name, label, StringComparison.OrdinalIgnoreCase) != 0)
			{
				sbTooltip.Append(" (aka ");
				sbTooltip.Append(character.name);
				sbTooltip.Append(")");
			}
			sbTooltip.NewParagraph();
			if (character.creator != null)
			{
				sbTooltip.Append("By: ");
				sbTooltip.Append(character.creator);
				sbTooltip.AppendLine();
			}
			sbTooltip.AppendLine($"Created: {character.creationDate.ToShortDateString()}");
			sbTooltip.AppendLine($"Last modified: {character.updateDate.ToShortDateString()}");

			var node = new TreeNode(label, 1, 1);
			node.Tag = character;
			node.ToolTipText = sbTooltip.ToString();
			if (parentNode != null)
				parentNode.Nodes.Add(node);
			else
				treeView.Nodes.Add(node);
			return node;
		}

		private DateTime GetLatestMessageTime(Bridge.CharacterInstance characterInstance)
		{
			Bridge.ChatCount count;
			if (_chatCounts.TryGetValue(characterInstance.groupId, out count))
				return count.lastMessage;
			return DateTime.MinValue;
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			AppSettings.BackyardLink.LinkOnImport = cbCreateLink.Checked;
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
			if (e.Node != null && e.Node.Tag is Bridge.CharacterInstance)
			{
				SelectedCharacter = (Bridge.CharacterInstance)e.Node.Tag;
				btnOk.Enabled = true;
			}
			else
			{
				SelectedCharacter = default(Bridge.CharacterInstance);
				btnOk.Enabled = false;
			}
		}

		private void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Node != null && e.Node.Tag is Bridge.CharacterInstance && e.Node == treeView.SelectedNode)
			{
				SelectedCharacter = (Bridge.CharacterInstance)e.Node.Tag;
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
			menu.Items.Add(new ToolStripMenuItem("Sort alphabetically", null, (s, e) => {
				if (AppSettings.User.SortCharactersAlphabetically == false)
				{
					AppSettings.User.SortCharactersAlphabetically = true;
					PopulateTree(true);
				}
			}) 
			{
				Checked = AppSettings.User.SortCharactersAlphabetically,
			});
				
			menu.Items.Add(new ToolStripMenuItem("Sort by last message", null, (s, e) => {
				if (AppSettings.User.SortCharactersAlphabetically)
				{
					AppSettings.User.SortCharactersAlphabetically = false;
					PopulateTree(true);
				}
			}) 
			{
				Checked = !AppSettings.User.SortCharactersAlphabetically,
			});
			menu.Show(control, location);
		}
	}
}
