using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using Bridge = Ginger.BackyardBridge;

namespace Ginger
{
	public partial class LinkEditChatDialog : Form
	{
		public Bridge.GroupInstance Group { get { return _groupInstance; } set { _groupInstance = value; } }
		private Bridge.GroupInstance _groupInstance;
		private Dictionary<string, Bridge.CharacterInstance> _charactersById;
		private Bridge.ChatInstance _selectedChatInstance = null;

		private static readonly Color[] NameColors = new Color[] {
			ColorTranslator.FromHtml("#0185b6"),
			ColorTranslator.FromHtml("#b68e01"),
			ColorTranslator.FromHtml("#c837c7"),
			ColorTranslator.FromHtml("#2c3397"),
			ColorTranslator.FromHtml("#76d244"),
			ColorTranslator.FromHtml("#44d2af"),
			ColorTranslator.FromHtml("#972c2c"),
			ColorTranslator.FromHtml("#2aa02d"),
		};

		private FormWindowState _lastWindowState;
		private System.Timers.Timer _statusbarTimer = new System.Timers.Timer();

		#region Win32 stuff
		public const uint LVM_SETHOTCURSOR = 4158;

		[DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
		#endregion

		public LinkEditChatDialog()
		{
			InitializeComponent();

			this.Load += OnLoad;
			this.Shown += LinkEditChatDialog_Shown;
			this.ResizeEnd += LinkEditChatDialog_ResizeEnd;
			chatInstanceList.Resize += ChatInstanceList_Resize;
			chatInstanceList.ItemSelectionChanged += ChatInstanceList_ItemSelectionChanged;
			chatView.ShowChat(false);

			_charactersById = Bridge.Characters.ToDictionary(c => c.instanceId, c => c);

			_statusbarTimer.Interval = 1000;
			_statusbarTimer.Elapsed += OnStatusBarTimerElapsed;
			_statusbarTimer.AutoReset = false;
			_statusbarTimer.SynchronizingObject = this;

			importMenuItem.ToolTipText = Resources.tooltip_link_import_chat;
			exportMenuItem.ToolTipText = Resources.tooltip_link_export_chat;
			duplicateMenuItem.ToolTipText = Resources.tooltip_link_duplicate_chat;
			repairChatsMenuItem.ToolTipText = Resources.tooltip_link_repair_chat;
			purgeMenuItem.ToolTipText = Resources.tooltip_link_purge_chat;
		}

		private void OnLoad(object sender, EventArgs e)
		{
			chatInstanceList.Columns[0].Width = chatInstanceList.Width - chatInstanceList.Columns[1].Width - 4;
			chatInstanceList.Items.Clear();

			// Fix for flickering cursor
			SendMessage(chatInstanceList.Handle, LVM_SETHOTCURSOR, IntPtr.Zero, Cursors.Arrow.Handle);

			RefreshTitle();
		}
		
		private void LinkEditChatDialog_Shown(object sender, EventArgs e)
		{
			PopulateChatList(true);
		}

		private void RefreshTitle()
		{
			if (_groupInstance.isEmpty == false)
				Text = string.Format("Chat history - {1}{0}", GetGroupTitle(_groupInstance), _groupInstance.members.Length > 2 ? "(Group) " : "");
			else
				Text = "Chat history";
		}

		private string GetGroupTitle(Bridge.GroupInstance group)
		{
			if (group.isEmpty)
			{
				return "Undefined";
			}
			else if (string.IsNullOrEmpty(group.name) == false)
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

		protected override void OnClientSizeChanged(EventArgs e)
		{
			base.OnClientSizeChanged(e);

			if (WindowState != _lastWindowState)
			{
				_lastWindowState = WindowState;

				chatView.ResizeItems();
			}
		}

		private void LinkEditChatDialog_ResizeEnd(object sender, EventArgs e)
		{
			chatView.ResizeItems();
		}

		private void chatList_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
		{
			e.NewWidth = chatInstanceList.Columns[e.ColumnIndex].Width;
			e.Cancel = true;
		}

		private void ChatInstanceList_Resize(object sender, EventArgs e)
		{
			chatInstanceList.Columns[0].Width = chatInstanceList.Width - chatInstanceList.Columns[1].Width - 4;
		}

		private void ChatInstanceList_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{
			if (e.IsSelected)
			{
				ViewChat(e.Item.Tag as Bridge.ChatInstance);
			}
			else
			{
				ViewChat(null);
			}
		}

		private void PopulateChatList(bool bSelectFirst)
		{
			// List chats for group
			Bridge.ChatInstance[] chats = null;
			if (_groupInstance.isEmpty == false && Bridge.GetChats(_groupInstance, out chats) != Bridge.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_unknown, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}

			_selectedChatInstance = null;
			chatInstanceList.BeginUpdate();
			chatInstanceList.Items.Clear();
			chatInstanceList.Groups.Clear();

			if (chats != null && chats.Length > 0)
			{
				var now = DateTime.Now;
				ListViewGroup groupToday = null;
				ListViewGroup groupYesterday = null;
				ListViewGroup groupLastWeek = null;
				ListViewGroup groupLastMonth = null;
				ListViewGroup groupOlder = null;

				for (int i = 0; i < chats.Length; ++i)
				{
					var item = chatInstanceList.Items.Add(chats[i].name);
					item.Tag = chats[i];

					var updateDate = DateTimeExtensions.Max(chats[i].history.lastMessageTime, chats[i].creationDate);

					if (chats[i].history.count >= 20)
						item.ImageIndex = 2;	// Long chat icon
					else if (chats[i].history.count >= 2)
						item.ImageIndex = 1;	// Short chat icon
					else 
						item.ImageIndex = 0;	// Empty chat icon

					if (updateDate.Date == now.Date) // Today
					{
						item.SubItems.Add(updateDate.ToString("t", CultureInfo.InvariantCulture));
						if (groupToday == null)
							groupToday = chatInstanceList.Groups.Add("today", "Today");
						item.Group = groupToday;
					}
					else if (updateDate.Date == now.Date - TimeSpan.FromDays(1)) // Yesterday
					{
						item.SubItems.Add(updateDate.ToString("t"));
						if (groupYesterday == null)
							groupYesterday = chatInstanceList.Groups.Add("yesterday", "Yesterday");
						item.Group = groupYesterday;
					}
					else if ((now.Date - updateDate.Date) < TimeSpan.FromDays(7)) // Last week
					{
						item.SubItems.Add(updateDate.ToString("m", CultureInfo.InvariantCulture));
						if (groupLastWeek == null)
							groupLastWeek = chatInstanceList.Groups.Add("week", "Last week");
						item.Group = groupLastWeek;
					}		
					else if ((now.Date - updateDate.Date) < TimeSpan.FromDays(31)) // Last month
					{
						item.SubItems.Add(updateDate.ToString("m", CultureInfo.InvariantCulture));
						if (groupLastMonth == null)
							groupLastMonth = chatInstanceList.Groups.Add("month", "Last month");
						item.Group = groupLastMonth;
					}
					else // Older
					{
						item.SubItems.Add(updateDate.ToString("d"));
						if (groupOlder == null)
							groupOlder = chatInstanceList.Groups.Add("year", "Older chats");
						item.Group = groupOlder;
					}
				}
			}

			chatInstanceList.EndUpdate();

			if (bSelectFirst)
				SelectChat(0);
			else
				Unselect();
			
			ResetStatusBarMessage();
			RefreshTitle();
		}

		private void Unselect()
		{
			if (chatInstanceList.SelectedIndices.Count > 0)
				chatInstanceList.Items[chatInstanceList.SelectedIndices[0]].Selected = false;
			_selectedChatInstance = null;
			chatView.Items.Clear();
		}

		private void ViewChat(Bridge.ChatInstance chatInstance)
		{
			chatView.Items.Clear();
			if (chatInstance == null)
			{
				_selectedChatInstance = null;
				statusChatLabel.Text = "";
				chatView.ShowChat(false);
				return;
			}

			_selectedChatInstance = chatInstance;
			chatView.ShowChat(true);

			List<Bridge.CharacterInstance> participants = new List<Bridge.CharacterInstance>(4);
			participants.Add(chatInstance.participants.Select(id => Bridge.GetCharacter(id)).FirstOrDefault(c => c.isUser)); // User first
			participants.AddRange(chatInstance.participants.Select(id => Bridge.GetCharacter(id)).Where(c => c.isUser == false));

			var namesById = participants
				.Select(c => c.name ?? "Unknown")
				.ToArray();

			var lines = new List<ChatListBox.Entry>();
			DateTime currDate = DateTime.MinValue;
			for (int i = 0; i < chatInstance.history.messages.Length; ++i)
			{
				var entry = chatInstance.history.messages[i];

				string timestamp;
				if (entry.updateDate.Date > currDate)
				{
					currDate = entry.updateDate;
					timestamp = string.Concat(entry.updateDate.ToString("d"), " ", entry.updateDate.ToString("T"));
				}
				else
				{
					timestamp = entry.updateDate.ToString("T");
				}

				lines.Add(new ChatListBox.Entry() {
					characterIndex = entry.speaker,
					color = NameColors[entry.speaker % NameColors.Length],
					name = namesById[entry.speaker],
					message = entry.text,
					timestamp = timestamp,
				});
			}
			chatView.Items.AddRange(lines.ToArray());
			chatView.listBox.TopIndex = 0; // chatView.Items.Count - 1;

			var sbStatus = new StringBuilder();
			int messageCount = chatInstance.history.count;
			sbStatus.Append(string.Format("{0} {1}", messageCount, messageCount == 1 ? "message" : "messages"));
			if (chatInstance.hasGreeting)
				sbStatus.Append(" (incl. greeting)");
			statusChatLabel.Text = sbStatus.ToString();
		}

		private void btnExport_Click(object sender, EventArgs e)
		{
			ExportChat(_selectedChatInstance);
		}

		private void btnImport_Click(object sender, EventArgs e)
		{
			ImportChat();
		}

		private void ExportChat(Bridge.ChatInstance chatInstance)
		{
			if (chatInstance == null)
				return; // Error

			if (chatInstance.history.messagesWithoutGreeting.Count() == 0)
			{
				MessageBox.Show(Resources.error_empty_chat, Resources.cap_export_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return ;
			}

			string filename = chatInstance.name;
//			if (AppSettings.User.LastExportChatFilter == 1) // json
//				filename = string.Concat(filename, ".csv");
//			else // json
				filename = string.Concat(filename, ".json");
			
			exportFileDialog.Title = "Export chat";
			exportFileDialog.Filter = "C.AI JSON|*.json";
			exportFileDialog.FileName = Utility.ValidFilename(filename);
			exportFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			exportFileDialog.FilterIndex = AppSettings.User.LastExportChatFilter;

			var result = exportFileDialog.ShowDialog();
			if (result != DialogResult.OK || string.IsNullOrWhiteSpace(exportFileDialog.FileName))
				return;

			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(exportFileDialog.FileName);
			AppSettings.User.LastExportChatFilter = exportFileDialog.FilterIndex;

			if (exportFileDialog.FilterIndex == 1) // C.AI
			{
				if (FileUtil.ExportCaiChat(chatInstance.history, exportFileDialog.FileName))
					return; // Success
			} 
			
			MessageBox.Show(Resources.error_write_json, Resources.cap_export_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private bool ImportChat()
		{
			// Open file...
			importFileDialog.Title = Resources.cap_import_chat;
			importFileDialog.Filter = "All supported file types|*.json";
			importFileDialog.FilterIndex = AppSettings.User.LastImportChatFilter;
			importFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			var result = importFileDialog.ShowDialog();
			if (result != DialogResult.OK)
				return false;

			AppSettings.User.LastImportChatFilter = importFileDialog.FilterIndex;
			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(importFileDialog.FileName);

			ChatHistory importedChat = FileUtil.ImportChat(importFileDialog.FileName);
			if (importedChat == null)
			{
				MessageBox.Show(Resources.error_unrecognized_chat_format, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			else if (importedChat.isEmpty)
			{
				MessageBox.Show(Resources.error_empty_chat, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			// Write to db...
			Bridge.ChatInstance chatInstance;
			var error = Bridge.CreateNewChat("Imported chat", importedChat, _groupInstance.instanceId, out chatInstance);
			if (error == Bridge.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_unknown, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return false;
			}
			else if (error != Bridge.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_import_chat, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			else
			{
				PopulateChatList(false);
				SelectChat(chatInstance.instanceId);
				return true;
			}
		}

		private void SelectChat(int index)
		{
			if (index >= 0 && index < chatInstanceList.Items.Count)
			{
				chatInstanceList.Items[index].Focused = true;
				chatInstanceList.Items[index].Selected = true;
				chatInstanceList.Select();
				chatInstanceList.EnsureVisible(index);
			}
		}

		private void SelectChat(string instanceId)
		{
			for (int i = 0; i < chatInstanceList.Items.Count; ++i)
			{
				if (((Bridge.ChatInstance)chatInstanceList.Items[i].Tag).instanceId == instanceId)
				{
					SelectChat(i);
					return;
				}
			}
		}

		private void closeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void selectCharacterMenuItem_Click(object sender, EventArgs e)
		{
			// Refresh character list
			if (Bridge.RefreshCharacters() != Bridge.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_unknown, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}

			var groupDlg = new LinkSelectGroupDialog();
			groupDlg.Characters = Bridge.Characters.ToArray();
			groupDlg.Groups = Bridge.Groups.ToArray();
			groupDlg.Folders = Bridge.Folders.ToArray();
			if (groupDlg.ShowDialog() == DialogResult.OK)
			{
				_groupInstance = groupDlg.SelectedGroup;
			}

			chatView.Items.Clear();
			PopulateChatList(true);
		}

		private void chatInstanceList_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == Keys.F2 && chatInstanceList.SelectedItems.Count > 0)
			{
				chatInstanceList.SelectedItems[0].BeginEdit();
				e.Handled = true;
			}

			if (e.KeyData == Keys.Delete && chatInstanceList.SelectedItems.Count > 0)
			{
				var chatInstance = chatInstanceList.SelectedItems[0].Tag as Bridge.ChatInstance;
				if (chatInstance != null)
					DeleteChat(chatInstance);
				e.Handled = true;
			}
		}

		private void chatInstanceList_AfterLabelEdit(object sender, LabelEditEventArgs e)
		{
			if (e.Label == null) // Cancelled by user
			{
				e.CancelEdit = false;
				return;
			}

			string newName = e.Label;
			if (string.IsNullOrWhiteSpace(newName))
				newName = Bridge.DefaultChatTitle;
			if (newName.Length > 100)
				newName = newName.Substring(0, 100);
			newName = newName.Trim();

			var item = chatInstanceList.Items[e.Item];
			if (item.Text == newName) // No change
			{
				e.CancelEdit = false;
				return;
			}

			// Rename
			if (Bridge.RenameChat(_selectedChatInstance, newName) == Bridge.Error.NoError)
			{
				SetStatusBarMessage(Resources.status_link_renamed_chat, Constants.StatusBarMessageInterval);
				e.CancelEdit = false;
			}
			else
			{
				MessageBox.Show(Resources.error_link_rename_chat, Resources.cap_link_rename_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				e.CancelEdit = true;
			}
		}

		private void DeleteChat(Bridge.ChatInstance chatInstance)
		{
			if (chatInstance == null)
				return;

			// Delete
			int chatCounts;
			if (Bridge.ConfirmDeleteChat(chatInstance, _groupInstance, out chatCounts) != Bridge.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_unknown, Resources.cap_link_delete_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			Bridge.Error error;
			if (chatCounts == 1) // Last chat
			{
				var mr = MessageBox.Show(string.Format(Resources.msg_link_delete_confirm_last, chatInstance.name), Resources.cap_link_delete_chat, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
				if (mr != DialogResult.Yes)
					return;

				// Update chat
				chatInstance.name = Bridge.DefaultChatTitle;
				chatInstance.updateDate = DateTime.Now;
				chatInstance.history = new ChatHistory();
				error = Bridge.UpdateChat(chatInstance, _groupInstance);
			}
			else
			{
				var mr = MessageBox.Show(string.Format(Resources.msg_link_delete_confirm, chatInstance.name), Resources.cap_link_delete_chat, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
				if (mr != DialogResult.Yes)
					return;

				error = Bridge.DeleteChat(chatInstance);
			}

			if (error == Bridge.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_unknown, Resources.cap_link_delete_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
			else if (error != Bridge.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_delete_chat, Resources.cap_link_delete_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				PopulateChatList(chatCounts == 1);
				SetStatusBarMessage(Resources.status_link_deleted_chat, Constants.StatusBarMessageInterval);
			}
		}

		public void SetStatusBarMessage(string message, int durationMS = 0)
		{
			statusLabel.Text = message;
			statusBar.Refresh();

			if (durationMS > 0)
			{
				_statusbarTimer.Stop();
				_statusbarTimer.Interval = durationMS;
				_statusbarTimer.Start();
			}
		}

		public void ResetStatusBarMessage()
		{
			int count = chatInstanceList.Items.Count;
			if (count > 0)
				statusLabel.Text = string.Format("{0} {1}", count, count == 1 ? "chat" : "chats");
			else
				statusLabel.Text = "No chats";
		}

		private void OnStatusBarTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			ResetStatusBarMessage();
		}

		private void menuBar_MenuActivate(object sender, EventArgs e)
		{
			bool hasGroup = _groupInstance.isEmpty == false;
			bool hasChat = _selectedChatInstance != null;

			importMenuItem.Enabled = hasGroup;
			exportMenuItem.Enabled = hasGroup && hasChat;
			duplicateMenuItem.Enabled = hasGroup && hasChat;
			purgeMenuItem.Enabled = hasGroup;
			refreshMenuItem.Enabled = hasGroup;
			repairChatsMenuItem.Enabled = hasGroup;
			createBackupMenuItem.Enabled = hasGroup;
		}

		private void duplicateMenuItem_Click(object sender, EventArgs e)
		{
			DuplicateChat(_selectedChatInstance);
		}

		public void DuplicateChat(Bridge.ChatInstance chatInstance)
		{
			if (chatInstance == null || _groupInstance.isEmpty)
				return;

			string chatTitle = string.Concat(chatInstance.name, " (copy)");
			Bridge.ChatInstance duplicate;
			var error = Bridge.CreateNewChat(chatTitle, chatInstance.history, _groupInstance.instanceId, out duplicate);

			if (error == Bridge.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_unknown, Resources.cap_link_duplicate_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
			else if (error != Bridge.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_duplicate_chat, Resources.cap_link_duplicate_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				PopulateChatList(true);
				SetStatusBarMessage(Resources.status_link_duplicated_chat, Constants.StatusBarMessageInterval);
			}
		}

		private void purgeMenuItem_Click(object sender, EventArgs e)
		{
			PurgeAllChats();
		}

		private void PurgeAllChats()
		{
			if (_groupInstance.isEmpty)
				return; // Error

			var mr = MessageBox.Show(string.Format(Resources.msg_link_purge_chat, GetGroupTitle(_groupInstance)), Resources.cap_link_purge_chat, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
			if (mr != DialogResult.Yes)
				return;

			var error = Bridge.PurgeChats(_groupInstance);
			if (error == Bridge.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_unknown, Resources.cap_link_purge_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
			else if (error != Bridge.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_purge_chat, Resources.cap_link_purge_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				PopulateChatList(true);
				SetStatusBarMessage(Resources.status_link_purged_chat, Constants.StatusBarMessageInterval);
			}
		}

		private void repairChatsMenuItem_Click(object sender, EventArgs e)
		{
			if (_groupInstance.isEmpty)
				return; // Error

			var mr = MessageBox.Show(string.Format(Resources.msg_link_repair_chat, GetGroupTitle(_groupInstance)), Resources.cap_link_repair_chat, MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
			if (mr != DialogResult.Yes)
				return;

			int modified = 0;
			var error = Bridge.RepairChats(_groupInstance, out modified);
			if (error == Bridge.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_unknown, Resources.cap_link_repair_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
			else if (error != Bridge.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_repair_chat, Resources.cap_link_repair_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				PopulateChatList(true);
				MessageBox.Show(string.Format(Resources.msg_link_repaired_chat, modified), Resources.cap_link_repair_chat, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void chatView_OnContextMenu(object sender, ChatListBox.ContextMenuEventArgs args)
		{
			ShowChatContextMenu(args.Index, args.Location);
		}

		private void ShowChatContextMenu(int messageIndex, Point location)
		{
			if (_selectedChatInstance == null || _selectedChatInstance.history == null || _selectedChatInstance.history.messages == null)
				return; // No chat

			string instanceId = null;
			if (messageIndex >= 0 && messageIndex < _selectedChatInstance.history.messages.Length)
				instanceId = _selectedChatInstance.history.messages[messageIndex].instanceId;

			int messageCount = _selectedChatInstance.history.count;
			bool bUserMessage = messageIndex >= 0 && messageIndex < messageCount && _selectedChatInstance.history.messages[messageIndex].speaker == 0;
			bool bCanBranch = !bUserMessage && messageIndex < messageCount - 1;
			bool bCanScrub = bUserMessage;

			if ((messageIndex == 0 && _selectedChatInstance.hasGreeting) // Greeting
				|| (string.IsNullOrEmpty(instanceId))) // No message id
			{
				bCanBranch = false;
				bCanScrub = false;
			}

			ContextMenuStrip menu = new ContextMenuStrip();
			menu.Items.Add(new ToolStripMenuItem("Copy selection", null, (s, e) => {
				chatView.CopySelected();
			}) {
				Enabled = chatView.listBox.SelectedItems.Count > 0,
				ToolTipText = "Copy selected chat to clipboard.",
			});

			menu.Items.Add(new ToolStripSeparator());

			menu.Items.Add(new ToolStripMenuItem("Branch from here", null, (s, e) => {
				BranchChat(_selectedChatInstance, instanceId);
			}) {
				Enabled = bCanBranch,
				ToolTipText = bCanBranch ? Resources.tooltip_link_branch_chat : Resources.tooltip_link_cannot_branch_chat,
			});

			menu.Items.Add(new ToolStripMenuItem("Scrub from here", null, (s, e) => {
				ScrubChat(_selectedChatInstance, instanceId);
			}) {
				Enabled = bCanScrub,
				ToolTipText = bCanScrub ? Resources.tooltip_link_scrub_chat : Resources.tooltip_link_cannot_scrub_chat,
			});
			menu.Show(chatView.listBox, location);
		}

		private void BranchChat(Bridge.ChatInstance chatInstance, string messageId)
		{
			if (chatInstance == null || string.IsNullOrEmpty(messageId) || _groupInstance.isEmpty)
				return; // Error

			string chatTitle = string.Concat(chatInstance.name, " (branch)");
			var branchedChatHistory = (ChatHistory)chatInstance.history.Clone();
			int messageIndex = Array.FindIndex(branchedChatHistory.messages, m => m.instanceId == messageId);
			if (messageIndex == -1)
			{
				MessageBox.Show(Resources.error_link_generic, Resources.cap_link_branch_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			Array.Resize(ref branchedChatHistory.messages, messageIndex + 1);

			Bridge.ChatInstance branchedChat;
			var error = Bridge.CreateNewChat(chatTitle, branchedChatHistory, _groupInstance.instanceId, out branchedChat);
			if (error == Bridge.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_unknown, Resources.cap_link_branch_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
			else if (error != Bridge.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_branch_chat, Resources.cap_link_branch_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				PopulateChatList(true);
				SetStatusBarMessage(Resources.status_link_branched_chat, Constants.StatusBarMessageInterval);
			}
		}

		private void ScrubChat(Bridge.ChatInstance chatInstance, string messageId)
		{
			if (chatInstance == null || string.IsNullOrEmpty(messageId))
				return; // Error

			var mr = MessageBox.Show(string.Format(Resources.msg_link_scrub_confirm, chatInstance.name), Resources.cap_link_scrub_chat, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
			if (mr != DialogResult.Yes)
				return;

			chatInstance.updateDate = DateTime.Now;
			int messageIndex = Array.FindIndex(chatInstance.history.messages, m => m.instanceId == messageId);
			if (messageIndex == -1)
			{
				MessageBox.Show(Resources.error_link_generic, Resources.cap_link_branch_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			Array.Resize(ref chatInstance.history.messages, messageIndex);
		
			var error = Bridge.UpdateChat(chatInstance, _groupInstance);
			if (error == Bridge.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_unknown, Resources.cap_link_scrub_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
			else if (error != Bridge.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_scrub_chat, Resources.cap_link_scrub_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				PopulateChatList(true);
				SetStatusBarMessage(Resources.status_link_scrubbed_chat, Constants.StatusBarMessageInterval);
			}
		}

		private void chatInstanceList_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				var hitTest = chatInstanceList.HitTest(e.X, e.Y);
				if (hitTest.Item == null)
					return;

				ShowChatListContextMenu(hitTest.Item, new Point(e.X, e.Y));
			}
		}

		private void ShowChatListContextMenu(ListViewItem item, Point location)
		{
			ContextMenuStrip menu = new ContextMenuStrip();
			menu.Items.Add(new ToolStripMenuItem("Rename", null, (s, e) => {
				item.BeginEdit();
			}));			

			menu.Items.Add(new ToolStripMenuItem("Duplicate", null, (s, e) => {
				DuplicateChat(item.Tag as Bridge.ChatInstance);
			}) {
				ToolTipText = Resources.tooltip_link_duplicate_chat,
			});

			menu.Items.Add(new ToolStripMenuItem("Export...", null, (s, e) => {
				ExportChat(item.Tag as Bridge.ChatInstance);
			}) {
				ToolTipText = Resources.tooltip_link_export_chat,
			});

			menu.Items.Add(new ToolStripSeparator());

			menu.Items.Add(new ToolStripMenuItem("Delete", null, (s, e) => {
				DeleteChat(item.Tag as Bridge.ChatInstance);
			}) {
				ToolTipText = Resources.tooltip_link_delete_chat,
			});

			menu.Show(chatInstanceList, location);
		}

		private void refreshMenuItem_Click(object sender, EventArgs e)
		{
			// Refresh character list
			if (Bridge.RefreshCharacters() != Bridge.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_unknown, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}

			PopulateChatList(true);
		}

		private void splitter_SplitterMoved(object sender, SplitterEventArgs e)
		{
			chatView.ResizeItems();
		}
	}
}
