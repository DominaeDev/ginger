using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Ginger.Properties;
using Ginger.Integration;

using Backyard = Ginger.Integration.Backyard;

namespace Ginger
{
	public partial class LinkEditChatDialog : Form
	{
		public GroupInstance Group { get { return _groupInstance; } set { _groupInstance = value; } }
		private GroupInstance _groupInstance;
		private Dictionary<string, CharacterInstance> _charactersById;
		private ChatInstance _selectedChatInstance = null;

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

			_statusbarTimer.Interval = 1000;
			_statusbarTimer.Elapsed += OnStatusBarTimerElapsed;
			_statusbarTimer.AutoReset = false;
			_statusbarTimer.SynchronizingObject = this;

			importMenuItem.ToolTipText = Resources.tooltip_link_import_chat;
			exportMenuItem.ToolTipText = Resources.tooltip_link_export_chat;
			duplicateMenuItem.ToolTipText = Resources.tooltip_link_duplicate_chat;
			repairChatsMenuItem.ToolTipText = Resources.tooltip_link_repair_chat;
			purgeMenuItem.ToolTipText = Resources.tooltip_link_purge_chat;
			createBackupMenuItem.ToolTipText = Resources.tooltip_link_create_backup;
			restoreBackupMenuItem.ToolTipText = Resources.tooltip_link_restore_backup;
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
			RefreshChats();
		}

		private void RefreshTitle()
		{
			if (_groupInstance.isEmpty == false)
				Text = string.Format("{1}{0} - Chat history", GetGroupTitle(_groupInstance), _groupInstance.members.Length > 2 ? "(Group) " : "");
			else
				Text = "Chat history";
		}

		private string GetGroupTitle(GroupInstance group)
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
						.Select(c => Utility.FirstNonEmpty(c.name, Constants.DefaultCharacterName))
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

		private string GetCharacterName()
		{
			if (_groupInstance.isEmpty)
				return Constants.DefaultCharacterName;
			else if (string.IsNullOrEmpty(_groupInstance.name) == false)
				return _groupInstance.name;
			else
			{
				var characters = _groupInstance.members
					.Select(id => _charactersById.GetOrDefault(id))
					.Where(c => c.isUser == false)
					.Select(c => Utility.FirstNonEmpty(c.name, Constants.DefaultCharacterName))
					.ToArray();

				if (characters.Count() > 1)
					return string.Concat(characters[0], " et al");
				else if (characters.Count() == 1)
					return characters[0];
				else
					return Constants.DefaultCharacterName;
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
				ViewChat(e.Item.Tag as ChatInstance);
			}
			else
			{
				ViewChat(null);
			}
		}

		private void PopulateChatList(bool bSelectFirst)
		{
			// List chats for group
			ChatInstance[] chats = null;
			if (_groupInstance.isEmpty == false && Backyard.GetChats(_groupInstance.instanceId, out chats) != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
					var chat = chats[i];
					var item = chatInstanceList.Items.Add(chat.name);
					item.Tag = chat;

					var lastMessageTime = chat.history.lastMessageTime;

					var sbTooltip = new StringBuilder();
					sbTooltip.AppendLine($"Created: {chats[i].creationDate.ToString("g")}");
					sbTooltip.AppendLine($"Updated: {chats[i].updateDate.ToString("g")}");
					sbTooltip.AppendLine($"Last messaged: {lastMessageTime.ToString("g")}");
					if (chat.parameters != null)
					{
						sbTooltip.NewParagraph();
						sbTooltip.AppendLine($"Model: {chat.parameters.model ?? Backyard.DefaultModel }");
					}

					item.ToolTipText = sbTooltip.ToString();

					var updateDate = DateTimeExtensions.Max(lastMessageTime, chats[i].creationDate);

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

		private void ViewChat(ChatInstance chatInstance)
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

			List<CharacterInstance> participants = new List<CharacterInstance>(4);
			participants.Add(chatInstance.participants.Select(id => Backyard.GetCharacter(id)).FirstOrDefault(c => c.isUser)); // User first
			participants.AddRange(chatInstance.participants.Select(id => Backyard.GetCharacter(id)).Where(c => c.isUser == false));

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
			// Open file...
			importFileDialog.Title = Resources.cap_import_chat;
			importFileDialog.Filter = "All supported file types|*.json;*.jsonl";
			importFileDialog.FilterIndex = AppSettings.User.LastImportChatFilter;
			importFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			var result = importFileDialog.ShowDialog();
			if (result != DialogResult.OK)
				return;

			AppSettings.User.LastImportChatFilter = importFileDialog.FilterIndex;
			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(importFileDialog.FileName);

			ChatHistory chatHistory = FileUtil.ImportChat(importFileDialog.FileName);
			if (chatHistory == null)
			{
				MessageBox.Show(Resources.error_unrecognized_chat_format, Resources.cap_import_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			else if (chatHistory.isEmpty)
			{
				MessageBox.Show(Resources.error_empty_chat, Resources.cap_import_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			int numSpeakers = chatHistory.numSpeakers;
			if (numSpeakers > _groupInstance.members.Length)
			{
				if (MessageBox.Show(Resources.msg_link_chat_too_many_speakers, Resources.cap_import_chat, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
					return;

				// Remove other characters' messages
				chatHistory.messages = chatHistory.messages.Where(m => m.speaker < numSpeakers - 1).ToArray();
			}

			// Apply names (2 character only)
			var characters = _groupInstance.members
				.Select(id => _charactersById.GetOrDefault(id))
				.Where(c => c.isUser == false)
				.ToArray();
			var user = _groupInstance.members
				.Select(id => _charactersById.GetOrDefault(id))
				.Where(c => c.isUser)
				.FirstOrDefault();
			string characterName = Utility.FirstNonEmpty(characters.Length > 0 ? characters[0].name : "", Constants.DefaultCharacterName);
			string userName = Utility.FirstNonEmpty(user.name, Constants.DefaultUserName);
			chatHistory.ApplyNames(new string[] { userName, characterName });

			if (string.IsNullOrEmpty(chatHistory.name))
				chatHistory.name = "Imported chat";

			// Write to db...
			ChatInstance chatInstance;
			var args = new Backyard.CreateChatArguments() {
				history = chatHistory,
			};
			var error = Backyard.CreateNewChat(args, _groupInstance.instanceId, out chatInstance);
			if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_import_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}
			else if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_import_chat, Resources.cap_import_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			else
			{
				PopulateChatList(false);
				SelectChat(chatInstance.instanceId);
				SetStatusBarMessage("Imported chat", Constants.StatusBarMessageInterval);
				return;
			}
		}

		private void ExportChat(ChatInstance chatInstance)
		{
			if (chatInstance == null)
				return; // Error

			if (chatInstance.history.messagesWithoutGreeting.Count() == 0)
			{
				MessageBox.Show(Resources.error_empty_chat, Resources.cap_export_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			string filename = string.Format("chatLog_{0}_{1}.json", GetCharacterName(), chatInstance.creationDate.ToUnixTimeSeconds()).Replace(" ", "_");

			bool bGroup = chatInstance.history.numSpeakers > 2;

			exportFileDialog.Title = "Export chat";
			if (bGroup)
			{
				exportFileDialog.Filter = "Ginger chat format|*.json";
				exportFileDialog.FileName = Utility.ValidFilename(filename);
				exportFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
				exportFileDialog.FilterIndex = 1;

				var result = exportFileDialog.ShowDialog();
				if (result != DialogResult.OK || string.IsNullOrWhiteSpace(exportFileDialog.FileName))
					return;
			}
			else
			{
				exportFileDialog.Filter = "Backyard chat|*.json|Text generation web ui chat|*.json|SillyTavern chat|*.jsonl|Ginger chat format|*.json";
				exportFileDialog.FileName = Utility.ValidFilename(filename);
				exportFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
				exportFileDialog.FilterIndex = AppSettings.User.LastExportChatFilter;

				var result = exportFileDialog.ShowDialog();
				if (result != DialogResult.OK || string.IsNullOrWhiteSpace(exportFileDialog.FileName))
					return;

				AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(exportFileDialog.FileName);
				AppSettings.User.LastExportChatFilter = exportFileDialog.FilterIndex;
			}

			if (!bGroup && exportFileDialog.FilterIndex == 1) // Backyard
			{
				if (FileUtil.ExportBackyardChat(chatInstance.history, exportFileDialog.FileName))
					return; // Success
			}
			else if (!bGroup && exportFileDialog.FilterIndex == 2) // TextGenWebUI
			{
				if (FileUtil.ExportTextGenWebUI(chatInstance.history, exportFileDialog.FileName))
					return; // Success
			}
			else if (!bGroup && exportFileDialog.FilterIndex == 3) // SillyTavern
			{
				string userName = Utility.FirstNonEmpty(chatInstance.participants.Select(id => Backyard.GetCharacter(id)).FirstOrDefault(c => c.isUser).name, Constants.DefaultUserName);
				string characterName = Utility.FirstNonEmpty(chatInstance.participants.Select(id => Backyard.GetCharacter(id)).FirstOrDefault(c => !c.isUser).name, Constants.DefaultCharacterName);

				if (FileUtil.ExportTavernChat(chatInstance.history, exportFileDialog.FileName, characterName, userName))
					return; // Success
			} 
			else if ((!bGroup && exportFileDialog.FilterIndex == 4) || exportFileDialog.FilterIndex == 1) // Ginger
			{
				var speakers = new List<GingerChat.Speaker>();
				for (int i = 0; i < chatInstance.participants.Length; ++i)
				{
					speakers.Add(new GingerChat.Speaker() {
						id = i.ToString(),
						name = i > 0 ? _charactersById[chatInstance.participants[i]].name : "You",
					});
				}

				var chat = GingerChat.FromChat(chatInstance, speakers);
				string json = chat.ToJson();
				if (json != null && FileUtil.ExportTextFile(exportFileDialog.FileName, json))
				{
					SetStatusBarMessage("Exported chat", Constants.StatusBarMessageInterval);
					return; // Success
				}
			}
			MessageBox.Show(Resources.error_write_json, Resources.cap_export_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
				if (((ChatInstance)chatInstanceList.Items[i].Tag).instanceId == instanceId)
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
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}

			_charactersById = Backyard.Characters.ToDictionary(c => c.instanceId, c => c);

			var groupDlg = new LinkSelectGroupDialog();
			groupDlg.Characters = Backyard.Characters.ToArray();
			groupDlg.Groups = Backyard.Groups.ToArray();
			groupDlg.Folders = Backyard.Folders.ToArray();
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
				var chatInstance = chatInstanceList.SelectedItems[0].Tag as ChatInstance;
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
				newName = ChatInstance.DefaultName;
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
			var error = Backyard.RenameChat(_selectedChatInstance, newName);
			if (error == Backyard.Error.NoError)
			{
				SetStatusBarMessage(Resources.status_link_renamed_chat, Constants.StatusBarMessageInterval);
				e.CancelEdit = false;
			}
			else if (error == Backyard.Error.NotFound)
			{
				MessageBox.Show(Resources.error_link_chat_not_found, Resources.cap_link_rename_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				e.CancelEdit = true;
				RefreshChats();
			}
			else
			{
				MessageBox.Show(Resources.error_link_rename_chat, Resources.cap_link_rename_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				e.CancelEdit = true;
			}
		}

		private void DeleteChat(ChatInstance chatInstance)
		{
			if (chatInstance == null)
				return;

			if (ConfirmChatExists(chatInstance.instanceId, Resources.cap_link_delete_chat) == false)
				return;

			// Chat chat counts
			int chatCounts;
			if (Backyard.ConfirmDeleteChat(chatInstance, _groupInstance, out chatCounts) != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_delete_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Confirm
			if (MessageBox.Show(string.Format(Resources.msg_link_delete_confirm, chatInstance.name), Resources.cap_link_delete_chat, MessageBoxButtons.YesNo, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
				return;

			Backyard.Error error;
			if (chatCounts == 1) // Last chat mustn't be deleted
			{
				// Clear messages
				chatInstance.updateDate = DateTime.Now;
				chatInstance.history = new ChatHistory();
				error = Backyard.UpdateChat(chatInstance, _groupInstance.instanceId);
			}
			else
			{
				// Delete chat
				error = Backyard.DeleteChat(chatInstance);
			}

			if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_delete_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
			else if (error != Backyard.Error.NoError)
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

		public void DuplicateChat(ChatInstance chatInstance)
		{
			if (chatInstance == null || _groupInstance.isEmpty)
				return;

			// Fetch latest
			ChatInstance latestChat;
			if (ConfirmChatExists(chatInstance.instanceId, out latestChat) == false)
				latestChat = chatInstance;

			var chatHistory = (ChatHistory)latestChat.history.Clone();
			chatHistory.name = string.Concat(latestChat.name, " (copy)");

			ChatInstance duplicate;
			var args = new Backyard.CreateChatArguments() {
				history = chatHistory,
				staging = latestChat.staging,
				parameters = latestChat.parameters,
			};
			var error = Backyard.CreateNewChat(args, _groupInstance.instanceId, out duplicate);

			if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_duplicate_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
			else if (error != Backyard.Error.NoError)
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

			var mr = MessageBox.Show(string.Format(Resources.msg_link_purge_chat, GetGroupTitle(_groupInstance)), Resources.cap_link_purge_chat, MessageBoxButtons.YesNo, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button2);
			if (mr != DialogResult.Yes)
				return;

			var error = Backyard.PurgeChats(_groupInstance);
			if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_purge_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
			else if (error != Backyard.Error.NoError)
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

			int modified;
			var error = Backyard.RepairChats(_groupInstance, out modified);
			if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_repair_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
			else if (error != Backyard.Error.NoError)
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

		private void BranchChat(ChatInstance chatInstance, string messageId)
		{
			if (chatInstance == null || string.IsNullOrEmpty(messageId) || _groupInstance.isEmpty)
				return; // Error

			// Fetch latest
			ChatInstance latestChat;
			if (ConfirmChatExists(chatInstance.instanceId, out latestChat) == false)
				latestChat = chatInstance;

			var chatHistory = (ChatHistory)latestChat.history.Clone();
			chatHistory.name = string.Concat(latestChat.name, " (branch)");

			int messageIndex = Array.FindIndex(chatHistory.messages, m => m.instanceId == messageId);
			if (messageIndex == -1)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_branch_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			Array.Resize(ref chatHistory.messages, messageIndex + 1);

			ChatInstance branchedChat;
			var args = new Backyard.CreateChatArguments() {
				history = chatHistory,
				staging = latestChat.staging,
				parameters = latestChat.parameters,
			};
			var error = Backyard.CreateNewChat(args, _groupInstance.instanceId, out branchedChat);
			if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_branch_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
			else if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_branch_chat, Resources.cap_link_branch_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				PopulateChatList(true);
				SetStatusBarMessage(Resources.status_link_branched_chat, Constants.StatusBarMessageInterval);
			}
		}

		private void ScrubChat(ChatInstance chatInstance, string messageId)
		{
			if (chatInstance == null || string.IsNullOrEmpty(messageId))
				return; // Error

			// Confirm
			if (MessageBox.Show(string.Format(Resources.msg_link_scrub_confirm, chatInstance.name), Resources.cap_link_scrub_chat, MessageBoxButtons.YesNo, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
				return;

			// Fetch latest
			ChatInstance latestChat;
			if (ConfirmChatExists(chatInstance.instanceId, out latestChat, Resources.cap_link_branch_chat) == false)
				return;

			latestChat.updateDate = DateTime.Now;
			int messageIndex = Array.FindIndex(latestChat.history.messages, m => m.instanceId == messageId);
			if (messageIndex == -1)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_branch_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			Array.Resize(ref latestChat.history.messages, messageIndex);
		
			var error = Backyard.UpdateChat(latestChat, _groupInstance.instanceId);
			if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_scrub_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
			else if (error != Backyard.Error.NoError)
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
				DuplicateChat(item.Tag as ChatInstance);
			}) {
				ToolTipText = Resources.tooltip_link_duplicate_chat,
			});

			menu.Items.Add(new ToolStripMenuItem("Export...", null, (s, e) => {
				ExportChat(item.Tag as ChatInstance);
			}) {
				ToolTipText = Resources.tooltip_link_export_chat,
			});

			menu.Items.Add(new ToolStripSeparator());

			menu.Items.Add(new ToolStripMenuItem("Copy chat settings", null, (s, e) => {
				CopyStaging(item.Tag as ChatInstance);
			}) {
				ToolTipText = Resources.tooltip_link_copy_staging,
			});

			menu.Items.Add(new ToolStripMenuItem("Copy model settings", null, (s, e) => {
				CopySettings(item.Tag as ChatInstance);
			}) {
				ToolTipText = Resources.tooltip_link_copy_settings,
			});

			if (Clipboard.ContainsData(ChatParametersClipboard.Format))
			{
				menu.Items.Add(new ToolStripMenuItem("Paste model settings", null, (s, e) => {
					PasteSettings(item.Tag as ChatInstance);
				}) {
					ToolTipText = Resources.tooltip_link_paste_settings,
				});
			}
			else if (Clipboard.ContainsData(ChatStagingClipboard.Format))
			{
				menu.Items.Add(new ToolStripMenuItem("Paste chat settings", null, (s, e) => {
					PasteStaging(item.Tag as ChatInstance);
				}) {
					ToolTipText = Resources.tooltip_link_paste_settings,
				});
			}
			else
			{
				menu.Items.Add(new ToolStripMenuItem("Paste", null, (EventHandler)null) { Enabled = false });
			}

			menu.Items.Add(new ToolStripMenuItem("Reset model settings", null, (s, e) => {
				ResetSettings(item.Tag as ChatInstance);
			}) {
				ToolTipText = Resources.tooltip_link_reset_settings,
			});

			menu.Items.Add(new ToolStripSeparator());

			menu.Items.Add(new ToolStripMenuItem("Delete chat", null, (s, e) => {
				DeleteChat(item.Tag as ChatInstance);
			}) {
				ToolTipText = Resources.tooltip_link_delete_chat,
			});

			menu.Show(chatInstanceList, location);
		}

		private void refreshMenuItem_Click(object sender, EventArgs e)
		{
			RefreshChats();
		}

		private void RefreshChats()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}
			_charactersById = Backyard.Characters.ToDictionary(c => c.instanceId, c => c);

			PopulateChatList(true);
		}

		private void splitter_SplitterMoved(object sender, SplitterEventArgs e)
		{
			chatView.ResizeItems();
		}

		private void createBackupMenuItem_Click(object sender, EventArgs e)
		{
			if (_groupInstance.isEmpty)
				return;

			CreateBackup();
		}
		
		private void restoreBackupMenuItem_Click(object sender, EventArgs e)
		{
			CharacterInstance characterInstance;
			if (RestoreBackup(out characterInstance))
			{
				// Refresh and select newly created character
				if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
				{
					MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					Close();
					return;
				}

				var group = Backyard.GetGroupForCharacter(characterInstance.instanceId);
				if (group.isEmpty == false)
				{
					_groupInstance = group;
					_charactersById = Backyard.Characters.ToDictionary(c => c.instanceId, c => c);

					PopulateChatList(true);
				}
			}
		}

		private bool CreateBackup()
		{
			CharacterInstance characterInstance;
			characterInstance = Group.members
				.Select(id => Backyard.GetCharacter(id))
				.FirstOrDefault(c => c.isUser == false);

			if (string.IsNullOrEmpty(characterInstance.instanceId))
				return false; // Error

			BackupData backup;
			var error = BackupUtil.CreateBackup(characterInstance, out backup);
			if (error == Backyard.Error.NotFound)
			{
				MessageBox.Show(Resources.error_link_create_backup, Resources.cap_link_create_backup, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			else if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_create_backup, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return false;
			}
			else if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_create_backup, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			string filename = string.Concat(GetCharacterName(), ".backup.", DateTime.Now.ToString("yyyy-MM-dd"), ".zip").Replace(" ", "_");

			importFileDialog.Title = Resources.cap_link_create_backup;
			exportFileDialog.Filter = "Character backup file|*.zip";
			exportFileDialog.FileName = Utility.ValidFilename(filename);
			exportFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			exportFileDialog.FilterIndex = AppSettings.User.LastExportChatFilter;

			var result = exportFileDialog.ShowDialog();
			if (result != DialogResult.OK || string.IsNullOrWhiteSpace(exportFileDialog.FileName))
				return false;

			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(exportFileDialog.FileName);
			AppSettings.User.LastExportChatFilter = exportFileDialog.FilterIndex;

			if (BackupUtil.WriteBackup(exportFileDialog.FileName, backup) == false)
			{
				MessageBox.Show(Resources.error_export_file, Resources.cap_link_create_backup, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			MessageBox.Show(Resources.msg_link_create_backup, Resources.cap_link_create_backup, MessageBoxButtons.OK, MessageBoxIcon.Information);
			return true;
		}

		private bool RestoreBackup(out CharacterInstance characterInstance)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_restore_backup, MessageBoxButtons.OK, MessageBoxIcon.Error);
				characterInstance = default(CharacterInstance);
				return false;
			}

			importFileDialog.Title = Resources.cap_link_restore_backup;
			importFileDialog.Filter = "Character backup file|*.zip";
			importFileDialog.FilterIndex = AppSettings.User.LastImportChatFilter;
			importFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			var result = importFileDialog.ShowDialog();
			if (result != DialogResult.OK)
			{
				characterInstance = default(CharacterInstance);
				return false;
			}

			AppSettings.User.LastImportChatFilter = importFileDialog.FilterIndex;
			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(importFileDialog.FileName);

			BackupData backup;
			FileUtil.Error readError = BackupUtil.ReadBackup(importFileDialog.FileName, out backup);
			if (readError != FileUtil.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_restore_backup_invalid, Resources.cap_link_restore_backup, MessageBoxButtons.OK, MessageBoxIcon.Error);
				characterInstance = default(CharacterInstance);
				return false;
			}

			// Confirmation
			if (MessageBox.Show(string.Format(Resources.msg_link_restore_backup, backup.characterCard.data.displayName, backup.chats.Count), Resources.cap_link_restore_backup, MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1) == DialogResult.No)
			{
				characterInstance = default(CharacterInstance);
				return false;
			}

			// Import chat parameters?
			if (backup.hasParameters && MessageBox.Show(Resources.msg_link_restore_backup_settings, Resources.cap_link_restore_backup, MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1) == DialogResult.No)
			{
				// Strip parameters
				foreach (var chat in backup.chats)
					chat.parameters = null;
			}

			Backyard.Link.Image[] imageLinks; // Ignored
			Backyard.ImageInput[] images = backup.images
				.Select(i => new Backyard.ImageInput {
					asset = new AssetFile() {
						name = i.filename,
						data = AssetData.FromBytes(i.data),
						ext = i.ext,
						assetType = AssetFile.AssetType.Icon,
					},
					fileExt = i.ext,
				})
				.ToArray();

			Backyard.Error error = Backyard.CreateNewCharacter(backup.characterCard, images, backup.chats.ToArray(), out characterInstance, out imageLinks);
			if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_restore_backup, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
						
			MessageBox.Show(Resources.msg_link_restore_backup_success, Resources.cap_link_restore_backup, MessageBoxButtons.OK, MessageBoxIcon.Information);
			return true;
		}

		private void CopySettings(ChatInstance chatInstance)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_copy_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Fetch latest
			ChatInstance latestChat;
			if (ConfirmChatExists(chatInstance.instanceId, out latestChat) == false)
				latestChat = chatInstance;

			if (latestChat.parameters == null)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_copy_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			Clipboard.SetDataObject(ChatParametersClipboard.FromParameters(latestChat.parameters), false);

			SetStatusBarMessage(Resources.status_link_copy_settings, Constants.StatusBarMessageInterval);
		}

		private void PasteSettings(ChatInstance chatInstance)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_paste_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (Clipboard.ContainsData(ChatParametersClipboard.Format) == false)
				return;

			ChatParametersClipboard clip = Clipboard.GetData(ChatParametersClipboard.Format) as ChatParametersClipboard;
			if (clip == null)
				return;

			var error = Backyard.UpdateChatParameters(chatInstance.instanceId, clip.parameters, null);
			if (error == Backyard.Error.NotFound)
			{
				MessageBox.Show(Resources.error_link_chat_not_found, Resources.cap_link_reset_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				RefreshChats();
				return;
			}
			if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_paste_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			SetStatusBarMessage(Resources.status_link_paste_settings, Constants.StatusBarMessageInterval);
		}

		private void CopyStaging(ChatInstance chatInstance)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_copy_staging, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Fetch latest
			ChatInstance latestChat;
			if (ConfirmChatExists(chatInstance.instanceId, out latestChat) == false)
				latestChat = chatInstance;

			if (latestChat.staging == null)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_copy_staging, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			Clipboard.SetDataObject(ChatStagingClipboard.FromStaging(latestChat.staging), false);

			SetStatusBarMessage(Resources.status_link_copy_staging, Constants.StatusBarMessageInterval);
		}
		
		private void PasteStaging(ChatInstance chatInstance)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_paste_staging, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (Clipboard.ContainsData(ChatStagingClipboard.Format) == false)
				return;

			ChatStagingClipboard clip = Clipboard.GetData(ChatStagingClipboard.Format) as ChatStagingClipboard;
			if (clip == null)
				return;

			var error = Backyard.UpdateChatParameters(chatInstance.instanceId, null, clip.staging);
			if (error == Backyard.Error.NotFound)
			{
				MessageBox.Show(Resources.error_link_chat_not_found, Resources.cap_link_paste_staging, MessageBoxButtons.OK, MessageBoxIcon.Error);
				RefreshChats();
				return;
			}
			if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_paste_staging, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			SetStatusBarMessage(Resources.status_link_paste_staging, Constants.StatusBarMessageInterval);

			RefreshChats();
		}

		private void ResetSettings(ChatInstance chatInstance)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_reset_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			var error = Backyard.UpdateChatParameters(chatInstance.instanceId, new ChatParameters(), null);
			if (error == Backyard.Error.NotFound)
			{
				MessageBox.Show(Resources.error_link_chat_not_found, Resources.cap_link_reset_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				RefreshChats();
				return;
			}
			if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_reset_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			SetStatusBarMessage(Resources.status_link_reset_settings, Constants.StatusBarMessageInterval);
		}

		private bool ConfirmChatExists(string chatId, string errorCaption)
		{
			ChatInstance tmp;
			return ConfirmChatExists(chatId, out tmp, errorCaption);
		}

		private bool ConfirmChatExists(string chatId, out ChatInstance chatInstance, string errorCaption = null)
		{
			var error = Backyard.GetChat(chatId, _groupInstance.instanceId, out chatInstance);
			if (error == Backyard.Error.NoError)
				return true;

			if (errorCaption == null)
				return chatInstance != null; // Silent

			if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_disconnected, errorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return false;
			}
			else if (error == Backyard.Error.NotFound)
			{
				MessageBox.Show(Resources.error_link_chat_not_found, errorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
				RefreshChats();
				return false;
			}
			else if (error == Backyard.Error.Unknown)
			{
				MessageBox.Show(Resources.error_link_general, errorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			return chatInstance != null;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				Close();
				return true;
			}
			if (keyData == Keys.F5)
			{
				refreshMenuItem_Click(this, EventArgs.Empty);
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}
	}
}
