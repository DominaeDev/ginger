using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Ginger
{
	public partial class LinkImportDialog : Form
	{
		public BackyardBridge.CharacterInstance[] Characters;
		public BackyardBridge.FolderInstance[] Folders;
		public BackyardBridge.CharacterInstance SelectedCharacter { get; private set; }

		public bool ShouldLink { get { return cbCreateLink.Checked; } }

		public LinkImportDialog()
		{
			InitializeComponent();

			this.Load += OnLoad;
		}

		private void OnLoad(object sender, EventArgs e)
		{
			PopulateTree();

			treeView.SelectedNode = null;
			SelectedCharacter = default(BackyardBridge.CharacterInstance);

			btnOk.Enabled = false;
			cbCreateLink.Checked = AppSettings.BackyardLink.LinkOnImport;
			
			// Tooltips
			toolTip.SetToolTip(cbCreateLink, Resources.tooltip_link_about_linking);
		}

		private void PopulateTree()
		{
			treeView.Suspend();
			treeView.Nodes.Clear();

			if (Folders == null || Characters == null)
				return; // Nothing to show

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
					CreateFolderNode(folder, nodesById, Characters.Count(c => c.folderId == folder.instanceId));
				
				openList.Remove(parentId);
			}

			// Create characters
			foreach (var character in Characters
				.OrderBy(c => c.displayName)
				.ThenByDescending(c => c.creationDate))
			{
				int similarCount = Characters.Count(c => c.displayName == character.displayName && c.folderId == character.folderId);
				CreateCharacterNode(character, nodesById, similarCount > 1);
			}
			treeView.Resume();
		}

		private TreeNode CreateFolderNode(BackyardBridge.FolderInstance folder, Dictionary<string, TreeNode> nodes, int count)
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

		private TreeNode CreateCharacterNode(BackyardBridge.CharacterInstance character, Dictionary<string, TreeNode> nodes, bool showTimeStamp)
		{
			TreeNode parentNode;
			nodes.TryGetValue(character.folderId, out parentNode);

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
			if (e.Node == null || e.Node.Tag == null)
			{
				SelectedCharacter = default(BackyardBridge.CharacterInstance);
				btnOk.Enabled = false;
			}
			else
			{
				SelectedCharacter = (BackyardBridge.CharacterInstance)e.Node.Tag;
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
				SelectedCharacter = (BackyardBridge.CharacterInstance)e.Node.Tag;
				BtnOk_Click(this, EventArgs.Empty);
			}
		}
	}
}
