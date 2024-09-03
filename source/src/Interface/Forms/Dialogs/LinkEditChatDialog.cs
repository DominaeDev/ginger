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
		public Bridge.GroupInstance Group { get; set; }
		
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

			_charactersById = Bridge.Characters.ToDictionary(c => c.instanceId, c => c);

			_statusbarTimer.Interval = 1000;
			_statusbarTimer.Elapsed += OnStatusBarTimerElapsed;
			_statusbarTimer.AutoReset = false;
			_statusbarTimer.SynchronizingObject = this;
		}

		private void OnLoad(object sender, EventArgs e)
		{
			chatInstanceList.Columns[0].Width = chatInstanceList.Width - chatInstanceList.Columns[1].Width - 4;
			chatInstanceList.Items.Clear();

			// Fix for flickering cursor
			SendMessage(chatInstanceList.Handle, LVM_SETHOTCURSOR, IntPtr.Zero, Cursors.Arrow.Handle);

			RefreshTitle();
		}

		private void RefreshTitle()
		{
			if (Group.isEmpty == false)
				Text = string.Format("Chat history - {1}{0}", GetGroupTitle(Group), Group.members.Length > 2 ? "(Group) " : "");
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

		private void LinkEditChatDialog_Shown(object sender, EventArgs e)
		{
			// Refresh character list
			if (Group.isEmpty)
			{
				if (Bridge.RefreshCharacters() != Bridge.Error.NoError)
				{
					MessageBox.Show(Resources.error_read_data, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					Close();
					return;
				}

				var groupDlg = new LinkSelectGroupDialog();
				groupDlg.Characters = Bridge.Characters.ToArray();
				groupDlg.Groups = Bridge.Groups.ToArray();
				groupDlg.Folders = Bridge.Folders.ToArray();
				if (groupDlg.ShowDialog() != DialogResult.OK)
				{
					Close();
					return;
				}
				Group = groupDlg.SelectedGroup;
			}

			PopulateChatList(true);
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
				_selectedChatInstance = null;
			}
		}

		private void PopulateChatList(bool bSelectFirst)
		{
			// List chats for group
			Bridge.ChatInstance[] chats;
			Bridge.GetChats(Group, out chats);

			_selectedChatInstance = null;
			chatInstanceList.BeginUpdate();
			chatInstanceList.Items.Clear();
			chatInstanceList.Groups.Clear();

			if (chats != null && chats.Length > 0)
			{
				var now = DateTime.Now;
				ListViewGroup groupToday = null;
				ListViewGroup groupYesterday = null;
				ListViewGroup groupOlder = null;
				ListViewGroup groupLastYear = null;

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
					else if (updateDate.Year == now.Year) // Older than yesterday
					{
						item.SubItems.Add(updateDate.ToString("m", CultureInfo.InvariantCulture));
						if (groupOlder == null)
							groupOlder = chatInstanceList.Groups.Add("older", "Older than yesterday");
						item.Group = groupOlder;
					}
					else if (updateDate.Year < now.Year) // Last year
					{
						item.SubItems.Add(updateDate.ToString("d"));
						if (groupLastYear == null)
							groupLastYear = chatInstanceList.Groups.Add("year", "Last year");
						item.Group = groupLastYear;
					}
				}
			}

			chatInstanceList.EndUpdate();

			if (bSelectFirst)
				SelectChat(0);

			ResetStatusBarMessage();
			RefreshTitle();
		}


		private void ViewChat(Bridge.ChatInstance chatInstance)
		{
			chatView.Items.Clear();
			if (chatInstance == null)
			{
				_selectedChatInstance = null;
				statusChatLabel.Text = "";
				return;
			}
			_selectedChatInstance = chatInstance;

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

			string ext = (Path.GetExtension(importFileDialog.FileName) ?? "").ToLowerInvariant();

			ChatHistory importedChat = null;
			if (ext == ".json")
			{
				importedChat = LoadChatFromJson(importFileDialog.FileName);
				if (importedChat == null)
				{
					MessageBox.Show(Resources.error_unrecognized_chat_format, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}
			}
			else
			{
				MessageBox.Show(Resources.error_unrecognized_chat_format, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			if (importedChat == null || importedChat.isEmpty)
			{
				MessageBox.Show(Resources.error_empty_chat, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			// Write to db...
			Bridge.ChatInstance chatInstance;
			var error = Bridge.CreateNewChat("Imported chat", importedChat, Group.instanceId, out chatInstance);
			if (error == Bridge.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_failed, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return false;
			}
			else if (error != Bridge.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_import_chat, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			
			PopulateChatList(false);
			SelectChat(chatInstance.instanceId);

			return true;
		}

		private void SelectChat(int index)
		{
			if (index >= 0 && index < chatInstanceList.Items.Count)
			{
				chatInstanceList.Items[index].Focused = true;
				chatInstanceList.Items[index].Selected = true;
//				chatInstanceList.Select();
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
					break;
				}
			}
		}

		private static ChatHistory LoadChatFromJson(string filename)
		{
			string json;
			if (FileUtil.ReadTextFile(filename, out json) == false)
				return null;

			// Try to read Tavern format (World book)
			if (CAIChat.Validate(json))
			{
				var caiChat = CAIChat.FromJson(json);
				if (caiChat != null)
					return caiChat.ToChat();
			}

			return null;
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
				MessageBox.Show(Resources.error_read_data, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}

			var groupDlg = new LinkSelectGroupDialog();
			groupDlg.Characters = Bridge.Characters.ToArray();
			groupDlg.Groups = Bridge.Groups.ToArray();
			groupDlg.Folders = Bridge.Folders.ToArray();
			if (groupDlg.ShowDialog() == DialogResult.OK)
			{
				Group = groupDlg.SelectedGroup;
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
			if (Bridge.ConfirmDeleteChat(chatInstance, Group, out chatCounts) != Bridge.Error.NoError)
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
				error = Bridge.UpdateChat(chatInstance);
			}
			else
			{
				var mr = MessageBox.Show(string.Format(Resources.msg_link_delete_confirm, chatInstance.name), Resources.cap_link_delete_chat, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
				if (mr != DialogResult.Yes)
					return;

				error = Bridge.DeleteChat(chatInstance);
			}

			if (error != Bridge.Error.NoError)
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
			bool hasGroup = Group.isEmpty == false;
			bool hasSelection = _selectedChatInstance != null;
			importMenuItem.Enabled = hasGroup;
			exportMenuItem.Enabled = hasGroup && hasSelection;
			duplicateMenuItem.Enabled = hasGroup && hasSelection;
			purgeMenuItem.Enabled = hasGroup;
		}

		private void duplicateMenuItem_Click(object sender, EventArgs e)
		{
			DuplicateChat(_selectedChatInstance);
		}

		public void DuplicateChat(Bridge.ChatInstance chatInstance)
		{
			if (chatInstance == null || Group.isEmpty)
				return;

			Bridge.ChatInstance duplicate;
			var error = Bridge.CreateNewChat(string.Concat(chatInstance.name, " (copy)"), chatInstance.history, Group.instanceId, out duplicate);

			if (error != Bridge.Error.NoError)
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
			if (Group.isEmpty)
				return; // Error

			var mr = MessageBox.Show(string.Format(Resources.msg_link_purge_chat, GetGroupTitle(Group)), Resources.cap_link_purge_chat, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
			if (mr != DialogResult.Yes)
				return;

			var error = Bridge.PurgeChats(Group.instanceId);
			if (error != Bridge.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_purge_chat, Resources.cap_link_purge_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				PopulateChatList(true);
				SetStatusBarMessage(Resources.status_link_purged_chat, Constants.StatusBarMessageInterval);
			}
		}
	}
}
