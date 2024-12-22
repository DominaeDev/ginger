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
	public partial class LinkEditChatDialog : FormEx
	{
		public GroupInstance Group { set { _groupInstance = value; } }
		private GroupInstance _groupInstance;
		private Dictionary<string, CharacterInstance> _charactersById;
		private ChatInstance _selectedChatInstance = null;

		private FormWindowState _lastWindowState;
		private System.Timers.Timer _statusbarTimer = new System.Timers.Timer();
		private bool _bEditing = false;

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
			chatInstanceList.BeforeLabelEdit += ChatInstanceList_BeforeLabelEdit;
			chatView.ShowChat(false);

			_statusbarTimer.Interval = 1000;
			_statusbarTimer.Elapsed += OnStatusBarTimerElapsed;
			_statusbarTimer.AutoReset = false;
			_statusbarTimer.SynchronizingObject = this;

			importMenuItem.ToolTipText = Resources.tooltip_link_import_chat;
			exportMenuItem.ToolTipText = Resources.tooltip_link_export_chat;
			duplicateMenuItem.ToolTipText = Resources.tooltip_link_duplicate_chat;
			repairChatsMenuItem.ToolTipText = Resources.tooltip_link_repair_chat;
			editModelSettingsMenuItem.ToolTipText = Resources.tooltip_link_model_settings_all;
		}

		private void OnLoad(object sender, EventArgs e)
		{
			chatInstanceList.Columns[0].Width = chatInstanceList.Width - chatInstanceList.Columns[1].Width - 4;
			chatInstanceList.Items.Clear();

			RefreshPortraitPosition();

			// Fix for flickering cursor
			SendMessage(chatInstanceList.Handle, LVM_SETHOTCURSOR, IntPtr.Zero, Cursors.Arrow.Handle);

			_charactersById = Backyard.Characters.ToDictionary(c => c.instanceId, c => c);

			RefreshTitle();
		}
		
		private void LinkEditChatDialog_Shown(object sender, EventArgs e)
		{
			RefreshChats();
			RefreshPortrait();
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
					.OrderBy(c => c.creationDate)
					.Where(c => c.isUser == false)
					.ToArray();

				if (characters.Length > 1)
				{
					return string.Concat(characters[0].displayName ?? Constants.DefaultCharacterName, " (and others)");

/*					string[] memberNames = characters
						.Select(c => c.name ?? Constants.DefaultCharacterName)
						.OrderBy(c => c)
						.ToArray();
					string groupTitle = string.Join(", ", memberNames.Take(3));
					if (memberNames.Length > 3)
						groupTitle += ", ...";
					
					return groupTitle; */
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
				RefreshPortraitPosition();
			}
		}

		private void LinkEditChatDialog_ResizeEnd(object sender, EventArgs e)
		{
			chatView.ResizeItems();
			RefreshPortraitPosition();
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
			if (_groupInstance.isEmpty == false && RunTask(() => Backyard.GetChats(_groupInstance.instanceId, out chats)) != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}

			_selectedChatInstance = null;
			_bEditing = false;
			chatInstanceList.BeginUpdate();
			chatInstanceList.Items.Clear();
			chatInstanceList.Groups.Clear();

			if (chats != null && chats.Length > 0)
			{
				var now = DateTime.Now;
				bool bAddGroups = Theme.IsDarkModeEnabled == false; // Group header color is unchangeable
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
					if (lastMessageTime != default(DateTime))
						sbTooltip.AppendLine($"Last message: {lastMessageTime.ToString("g")}");
					if (chat.parameters != null)
					{
						sbTooltip.NewParagraph();
						var model = BackyardModelDatabase.GetModel(chat.parameters.model);
						sbTooltip.AppendLine($"Model: {Utility.FirstNonEmpty(model.displayName, chat.parameters.model, "(Default model)") }");
					}

					sbTooltip.AppendLine($"Background: {(chat.hasBackground ? "Yes" : "No")}");

					item.ToolTipText = sbTooltip.ToString();

					var updateDate = DateTimeExtensions.Max(lastMessageTime, chats[i].creationDate);

					if (chats[i].history.count >= 20)
						item.ImageIndex = 2;	// Long chat icon
					else if (chats[i].history.count >= 4)
						item.ImageIndex = 1;	// Short chat icon
					else 
						item.ImageIndex = 0;	// Empty chat icon

					if (updateDate.Date == now.Date) // Today
					{
						item.SubItems.Add(updateDate.ToString("t", CultureInfo.InvariantCulture));
						if (bAddGroups)
						{
							if (groupToday == null)
								groupToday = chatInstanceList.Groups.Add("today", "Today");
							item.Group = groupToday;
						}
					}
					else if (updateDate.Date == now.Date - TimeSpan.FromDays(1)) // Yesterday
					{
						item.SubItems.Add(updateDate.ToString("t"));
						if (bAddGroups)
						{
							if (groupYesterday == null)
								groupYesterday = chatInstanceList.Groups.Add("yesterday", "Yesterday");
							item.Group = groupYesterday;
						}
					}
					else if ((now.Date - updateDate.Date) < TimeSpan.FromDays(7)) // Last week
					{
						item.SubItems.Add(updateDate.ToString("m", CultureInfo.InvariantCulture));
						if (bAddGroups)
						{
							if (groupLastWeek == null)
								groupLastWeek = chatInstanceList.Groups.Add("week", "Last week");
							item.Group = groupLastWeek;
						}
					}		
					else if ((now.Date - updateDate.Date) < TimeSpan.FromDays(31)) // Last month
					{
						item.SubItems.Add(updateDate.ToString("m", CultureInfo.InvariantCulture));
						if (bAddGroups)
						{
							if (groupLastMonth == null)
								groupLastMonth = chatInstanceList.Groups.Add("month", "Last month");
							item.Group = groupLastMonth;
						}
					}
					else // Older
					{
						item.SubItems.Add(updateDate.ToString("d"));
						if (bAddGroups)
						{
							if (groupOlder == null)
								groupOlder = chatInstanceList.Groups.Add("year", "Older chats");
							item.Group = groupOlder;
						}
					}
				}
			}

			chatInstanceList.EndUpdate();

			if (bSelectFirst)
				SelectChat(0);
			else
				Unselect();
			
			RefreshStatusBarMessage();
			RefreshTitle();
		}

		private void Unselect()
		{
			if (chatInstanceList.SelectedIndices.Count > 0)
				chatInstanceList.Items[chatInstanceList.SelectedIndices[0]].Selected = false;
			_selectedChatInstance = null;
			_bEditing = false;
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

				Color[] nameColors;
				if (Theme.IsDarkModeEnabled)
					nameColors = ChatListBox.NameColors_Dark;
				else
					nameColors = ChatListBox.NameColors_Light;

				lines.Add(new ChatListBox.Entry() {
					characterIndex = entry.speaker,
					color =nameColors[entry.speaker % nameColors.Length],
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

		private void NewChat()
		{
			ChatHistory chatHistory = new ChatHistory() {
				messages = new ChatHistory.Message[0],
			};

			if (string.IsNullOrEmpty(chatHistory.name) || chatHistory.name == ChatInstance.DefaultName)
				chatHistory.name = ChatInstance.DefaultName;

			var args = new Backyard.CreateChatArguments() {
				history = chatHistory,
			};

			// Fetch latest chat settings
			ChatInstance latestChat;
			if (ConfirmChatExists(_groupInstance.instanceId, out latestChat))
				args.parameters = latestChat.parameters;
			else
				args.parameters = AppSettings.BackyardSettings.UserSettings;

			ChatInstance chatInstance = null;
			var error = RunTask(() => Backyard.CreateNewChat(args, _groupInstance.instanceId, out chatInstance), "Creating chat...");
			if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_create_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}
			else if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_create_chat, Resources.cap_link_create_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			else
			{
				PopulateChatList(false);
				SelectChat(chatInstance.instanceId, true);

				SetStatusBarMessage("New chat created", Constants.StatusBarMessageInterval);
				return;
			}
		}

		private void ImportChat()
		{
			// Open file...
			importFileDialog.FileName = "";
			importFileDialog.Title = Resources.cap_import_chat;
			importFileDialog.Filter = "All supported file types|*.json;*.jsonl;*.txt";
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

			Unanonymize(chatHistory);

			if (string.IsNullOrEmpty(chatHistory.name) || chatHistory.name == ChatInstance.DefaultName)
				chatHistory.name = "Imported chat";

			// Write to db...
			ChatInstance chatInstance = null;
			var args = new Backyard.CreateChatArguments() {
				history = chatHistory,
				isImport = true,
			};

			// Fetch latest chat settings
			ChatInstance latestChat;
			if (ConfirmChatExists(_groupInstance.instanceId, out latestChat))
				args.parameters = latestChat.parameters;
			else
				args.parameters = AppSettings.BackyardSettings.UserSettings;

			var error = RunTask(() => Backyard.CreateNewChat(args, _groupInstance.instanceId, out chatInstance), "Importing chat...");
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

		private void Unanonymize(ChatHistory chatHistory)
		{
			if (chatHistory.isEmpty)
				return;

			var userName = _groupInstance.members
				.Select(id => _charactersById.GetOrDefault(id))
				.Where(c => c.isUser)
				.FirstOrDefault()
				.name ?? "{user}";
			var characterName = _groupInstance.members
				.Select(id => _charactersById.GetOrDefault(id))
				.Where(c => c.isUser == false)
				.FirstOrDefault()
				.name ?? "{character}";

			for (int i = 0; i < chatHistory.messages.Length; ++i)
			{
				for (int j = 0; j < chatHistory.messages[i].swipes.Length; ++j)
				{
					StringBuilder sb = new StringBuilder(chatHistory.messages[i].swipes[j]);
					Utility.ReplaceWholeWord(sb, "{{user}}", "__UUUU__", StringComparison.OrdinalIgnoreCase);
					Utility.ReplaceWholeWord(sb, GingerString.UserMarker, "__UUUU__", StringComparison.OrdinalIgnoreCase);
					Utility.ReplaceWholeWord(sb, "{{char}}", "__CCCC__", StringComparison.OrdinalIgnoreCase);
					Utility.ReplaceWholeWord(sb, "{character}", "__CCCC__", StringComparison.OrdinalIgnoreCase);
					Utility.ReplaceWholeWord(sb, GingerString.CharacterMarker, "__CCCC__", StringComparison.OrdinalIgnoreCase);
					Utility.ReplaceWholeWord(sb, "__UUUU__", userName, StringComparison.Ordinal);
					Utility.ReplaceWholeWord(sb, "__CCCC__", characterName, StringComparison.Ordinal);
					chatHistory.messages[i].swipes[j] = sb.ToString();
				}
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

			string filename = string.Format("chatLog_{0}_{1}", GetCharacterName(), chatInstance.creationDate.ToUnixTimeSeconds()).Replace(" ", "_");

			bool bGroupChat = chatInstance.history.numSpeakers > 2;

			const int SoloBackyard = 1;
			const int SoloGinger = 2;
			const int SoloSillyTavern = 3;
			const int SoloTextGenWebUI = 4;
			const int SoloTextFile = 5;
			const int GroupGinger = 1;
			const int GroupTextFile = 2;

			int filterIndex = AppSettings.User.LastExportChatFilter;
			exportFileDialog.Title = "Export chat";
			if (bGroupChat)
			{
				exportFileDialog.Filter = "Ginger chat log|*.json|Text file|*.txt";
				if (filterIndex == SoloTextFile) // Text
					filterIndex = GroupTextFile;
				else
					filterIndex = GroupGinger;

				if (filterIndex == GroupTextFile) // Text
					filename = string.Concat(filename, ".txt");
				else // json
					filename = string.Concat(filename, ".json");	
			}
			else
			{
				exportFileDialog.Filter = "Backyard chat log|*.json|Ginger chat log|*.json|SillyTavern chat log|*.jsonl|Text generation web ui chat log|*.json|Text file|*.txt";
				
				if (filterIndex == SoloSillyTavern)
					filename = string.Concat(filename, ".jsonl");
				else if (filterIndex == SoloTextFile)
					filename = string.Concat(filename, ".txt");
				else // json
					filename = string.Concat(filename, ".json");		
			}

			exportFileDialog.FileName = Utility.ValidFilename(filename);
			exportFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			exportFileDialog.FilterIndex = filterIndex;

			var result = exportFileDialog.ShowDialog();
			if (result != DialogResult.OK || string.IsNullOrWhiteSpace(exportFileDialog.FileName))
				return;

			if (bGroupChat)
			{
				if (exportFileDialog.FilterIndex == GroupGinger)
					exportFileDialog.FilterIndex = SoloGinger; // Ginger
				else if (exportFileDialog.FilterIndex == GroupTextFile)
					exportFileDialog.FilterIndex = SoloTextFile; // Text
			}

			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(exportFileDialog.FileName);
			AppSettings.User.LastExportChatFilter = exportFileDialog.FilterIndex;

			string[] names = chatInstance.participants
				.Select(id => Backyard.GetCharacter(id).name)
				.ToArray();

			if (exportFileDialog.FilterIndex == SoloBackyard) // Backyard
			{
				if (FileUtil.ExportBackyardChat(chatInstance.history, exportFileDialog.FileName))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == SoloGinger) // Ginger
			{
				var speakers = new GingerChat.SpeakerList();
				for (int i = 0; i < chatInstance.participants.Length; ++i)
				{
					speakers.Add(new GingerChat.Speaker() {
						id = i.ToString(),
						name = _charactersById[chatInstance.participants[i]].name,
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
			else if (exportFileDialog.FilterIndex == SoloSillyTavern) // SillyTavern
			{
				if (FileUtil.ExportTavernChat(chatInstance.history, exportFileDialog.FileName, names))
					return; // Success
			} 
			else if (exportFileDialog.FilterIndex == SoloTextGenWebUI) // TextGenWebUI
			{
				if (FileUtil.ExportTextGenWebUI(chatInstance.history, exportFileDialog.FileName))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == SoloTextFile) // Text file
			{
				var chat = TextFileChat.FromChat(chatInstance, names.ToArray());
				string textData = chat.ToString();
				if (textData != null && FileUtil.ExportTextFile(exportFileDialog.FileName, textData))
				{
					SetStatusBarMessage("Exported chat", Constants.StatusBarMessageInterval);
					return; // Success
				}
			}
			MessageBox.Show(Resources.error_write_json, Resources.cap_export_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void SelectChat(int index, bool bBeginEdit = false)
		{
			if (index >= 0 && index < chatInstanceList.Items.Count)
			{
				chatInstanceList.Items[index].Focused = true;
				chatInstanceList.Items[index].Selected = true;
				chatInstanceList.Select();
				chatInstanceList.EnsureVisible(index);
				if (bBeginEdit)
					chatInstanceList.Items[index].BeginEdit();
			}
		}

		private void SelectChat(string instanceId, bool bBeginEdit = false)
		{
			for (int i = 0; i < chatInstanceList.Items.Count; ++i)
			{
				if (((ChatInstance)chatInstanceList.Items[i].Tag).instanceId == instanceId)
				{
					SelectChat(i, bBeginEdit);
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
			if (RunTask(() => Backyard.RefreshCharacters()) != Backyard.Error.NoError)
			{
				this.Cursor = Cursors.Default;
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
			RefreshPortrait();
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
		
		private void ChatInstanceList_BeforeLabelEdit(object sender, LabelEditEventArgs e)
		{
			_bEditing = true;
		}

		private void chatInstanceList_AfterLabelEdit(object sender, LabelEditEventArgs e)
		{
			_bEditing = false;
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
			var error = RunTask(() => Backyard.RenameChat(_selectedChatInstance, newName), "Renaming chat...");
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

			// Fetch chat counts
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
				string greeting = chatInstance.history.greeting;
				chatInstance.history = new ChatHistory();
				if (greeting != null)
				{
					chatInstance.history.messages = new ChatHistory.Message[] {
						new ChatHistory.Message() {
							speaker = 1,
							swipes = new string[] { greeting },
						}
					};
				}
				error = RunTask(() => Backyard.UpdateChat(chatInstance, _groupInstance.instanceId), "Deleting chat...");
			}
			else
			{
				// Delete chat
				error = RunTask(() => Backyard.DeleteChat(chatInstance), "Deleting chat...");
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

		public void RefreshStatusBarMessage()
		{
			int count = chatInstanceList.Items.Count;
			if (count > 0)
				statusLabel.Text = string.Format("{0} {1}", count, count == 1 ? "chat" : "chats");
			else
				statusLabel.Text = "No chats";
		}

		private void OnStatusBarTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			RefreshStatusBarMessage();
		}

		private void menuBar_MenuActivate(object sender, EventArgs e)
		{
			bool hasGroup = _groupInstance.isEmpty == false;
			bool hasChat = _selectedChatInstance != null;

			importMenuItem.Enabled = hasGroup;
			exportMenuItem.Enabled = hasGroup && hasChat;
			duplicateMenuItem.Enabled = hasGroup && hasChat;
			refreshMenuItem.Enabled = hasGroup;
			repairChatsMenuItem.Enabled = hasGroup;
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
			var error = RunTask(() => Backyard.CreateNewChat(args, _groupInstance.instanceId, out duplicate), "Duplicating chat...");

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
			DeleteAllChats();
		}

		private void DeleteAllChats()
		{
			if (_groupInstance.isEmpty)
				return; // Error

			var mr = MessageBox.Show(string.Format(Resources.msg_link_purge_chat, GetGroupTitle(_groupInstance)), Resources.cap_link_purge_chat, MessageBoxButtons.YesNo, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button2);
			if (mr != DialogResult.Yes)
				return;

			var error = RunTask(() => Backyard.PurgeChats(_groupInstance), "Deleting chat history...");
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

			var mr = MessageBox.Show(string.Format(Resources.msg_link_repair_chat, GetGroupTitle(_groupInstance)), Resources.cap_link_repair_chat, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
			if (mr != DialogResult.Yes)
				return;

			int modified = 0;
			var error = RunTask(() => Backyard.RepairChats(_groupInstance, out modified), "Repairing chats...");
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
			bool bCanRestart = !bUserMessage && messageIndex > 0 && messageIndex < messageCount - 1;
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

			menu.Items.Add(new ToolStripMenuItem("Set as first message...", null, (s, e) => {
				RestartChat(_selectedChatInstance, instanceId);
			}) {
				Enabled = bCanRestart,
				ToolTipText = bCanRestart ? Resources.tooltip_link_restart_chat : Resources.tooltip_link_cannot_restart_chat,
			});

			menu.Items.Add(new ToolStripMenuItem("Scrub from here...", null, (s, e) => {
				ScrubChat(_selectedChatInstance, instanceId);
			}) {
				Enabled = bCanScrub,
				ToolTipText = bCanScrub ? Resources.tooltip_link_scrub_chat : Resources.tooltip_link_cannot_scrub_chat,
			});
			Theme.Apply(menu);
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

			var error = RunTask(() => Backyard.CreateNewChat(args, _groupInstance.instanceId, out branchedChat), "Branching chat...");

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

		private void RestartChat(ChatInstance chatInstance, string messageId)
		{
			if (chatInstance == null || string.IsNullOrEmpty(messageId))
				return; // Error

			// Confirm
			if (MessageBox.Show(string.Format(Resources.msg_link_restart_confirm, chatInstance.name), Resources.cap_link_restart_chat, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
				return;

			// Fetch latest
			ChatInstance latestChat;
			if (ConfirmChatExists(chatInstance.instanceId, out latestChat, Resources.cap_link_restart_chat) == false)
				return;

			latestChat.updateDate = DateTime.Now;
			int messageIndex = Array.FindIndex(latestChat.history.messages, m => m.instanceId == messageId);
			if (messageIndex == -1)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_restart_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			var messages = new ChatHistory.Message[latestChat.history.messages.Length - messageIndex];
			Array.Copy(latestChat.history.messages, messageIndex, messages, 0, latestChat.history.messages.Length - messageIndex);
			latestChat.history.messages = messages;

			var error = RunTask(() => Backyard.UpdateChat(latestChat, _groupInstance.instanceId), "Updating chat...");
			if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_restart_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
			else if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_restart_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				PopulateChatList(true);
				SetStatusBarMessage(Resources.status_link_restarted_chat, Constants.StatusBarMessageInterval);
			}
		}


		private void ScrubChat(ChatInstance chatInstance, string messageId)
		{
			if (chatInstance == null || string.IsNullOrEmpty(messageId))
				return; // Error

			// Confirm
			if (MessageBox.Show(string.Format(Resources.msg_link_scrub_confirm, chatInstance.name), Resources.cap_link_scrub_chat, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
				return;

			// Fetch latest
			ChatInstance latestChat;
			if (ConfirmChatExists(chatInstance.instanceId, out latestChat, Resources.cap_link_scrub_chat) == false)
				return;

			latestChat.updateDate = DateTime.Now;
			int messageIndex = Array.FindIndex(latestChat.history.messages, m => m.instanceId == messageId);
			if (messageIndex == -1)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_scrub_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			Array.Resize(ref latestChat.history.messages, messageIndex);
		
			var error = RunTask(() => Backyard.UpdateChat(latestChat, _groupInstance.instanceId), "Scrubbing chat...");
			if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_scrub_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
			else if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_scrub_chat, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				PopulateChatList(true);
				SetStatusBarMessage(Resources.status_link_scrubbed_chat, Constants.StatusBarMessageInterval);
			}
		}

		private void chatInstanceList_OnContextMenu(object sender, ChatListView.ContextMenuEventArgs e)
		{
			if (e.Index >= 0)
				ShowChatListContextMenu(chatInstanceList.Items[e.Index], e.Location);
			else
				ShowChatListContextMenu(null, e.Location);
		}

		private void ShowChatListContextMenu(ListViewItem item, Point location)
		{
			ContextMenuStrip menu = new ContextMenuStrip();

			if (item != null)
			{
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

				if (Clipboard.ContainsData(ChatStagingClipboard.Format))
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

				menu.Items.Add(new ToolStripMenuItem("Edit model settings", null, (s, e) => {
					EditSettings(item.Tag as ChatInstance);
				}) {
					ToolTipText = Resources.tooltip_link_model_settings_one,
				});

				menu.Items.Add(new ToolStripSeparator());

				menu.Items.Add(new ToolStripMenuItem("Delete chat", null, (s, e) => {
					DeleteChat(item.Tag as ChatInstance);
				}) {
					ToolTipText = Resources.tooltip_link_delete_chat,
				});
			}
			else
			{
				bool hasGroup = _groupInstance.isEmpty == false;
				menu.Items.Add(new ToolStripMenuItem("New chat", null, (s, e) => {
					NewChat();
				}) { 
					Enabled = hasGroup,	
				});

				menu.Items.Add(new ToolStripMenuItem("Import chat...", null, (s, e) => {
					ImportChat();
				}) {
					Enabled = hasGroup,	
					ToolTipText = Resources.tooltip_link_import_chat,
				});

				menu.Items.Add(new ToolStripSeparator());

				menu.Items.Add(new ToolStripMenuItem("Delete all chats...", null, (s, e) => {
					DeleteAllChats();
				}) {
					Enabled = hasGroup,	
					ToolTipText = Resources.tooltip_link_purge_chat,
				});
			}

			Theme.Apply(menu);
			menu.Show(chatInstanceList, location);
		}

		private void refreshMenuItem_Click(object sender, EventArgs e)
		{
			RefreshChats();
			RefreshPortrait();
		}

		private void RefreshChats()
		{
			// Refresh character list
			if (RunTask(() => Backyard.RefreshCharacters()) != Backyard.Error.NoError)
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
			RefreshPortraitPosition();
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

			var error = RunTask(() => Backyard.UpdateChatParameters(chatInstance.instanceId, null, clip.staging), "Updating chat...");
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

		private void EditSettings(ChatInstance chatInstance)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_edit_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Model settings dialog
			var dlg = new EditModelSettingsDialog();
			dlg.Editing = chatInstance.parameters;
			if (dlg.ShowDialog() != DialogResult.OK)
				return;

			var error = RunTask(() => Backyard.UpdateChatParameters(chatInstance.instanceId, dlg.Parameters, null), "Updating model settings...");
			if (error == Backyard.Error.NotFound)
			{
				MessageBox.Show(Resources.error_link_chat_not_found, Resources.cap_link_edit_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				RefreshChats();
				return;
			}
			if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_edit_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			SetStatusBarMessage(Resources.status_link_reset_settings, Constants.StatusBarMessageInterval);

			RefreshChats();
		}
		
		private void EditAllSettings()
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_edit_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			ChatInstance[] chats = null;
			if (_groupInstance.isEmpty == false && RunTask(() => Backyard.GetChats(_groupInstance.instanceId, out chats)) != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_edit_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}

			if (chats == null || chats.Length == 0)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_edit_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}

			// Model settings dialog
			var dlg = new EditModelSettingsDialog();
			dlg.Editing = chats[0].parameters;
			if (dlg.ShowDialog() != DialogResult.OK)
				return;

			string[] chatIds = chats.Select(c => c.instanceId).ToArray();

			var error = RunTask(() => Backyard.UpdateChatParameters(chatIds, dlg.Parameters, null), "Updating model settings...");
			if (error == Backyard.Error.NotFound)
			{
				MessageBox.Show(Resources.error_link_chat_not_found, Resources.cap_link_edit_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				RefreshChats();
				return;
			}
			if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_edit_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			SetStatusBarMessage(Resources.status_link_update_model_settings, Constants.StatusBarMessageInterval);

			RefreshChats();
		}

		private bool ConfirmChatExists(string chatId, string errorCaption)
		{
			ChatInstance tmp;
			return ConfirmChatExists(chatId, out tmp, errorCaption);
		}

		private bool ConfirmChatExists(string chatId, out ChatInstance chatInstance, string errorCaption = null)
		{
			ChatInstance returnedChat = default(ChatInstance);
			var error = RunTask(() => Backyard.GetChat(chatId, _groupInstance.instanceId, out returnedChat));
			chatInstance = returnedChat;
			
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
				if (_bEditing == false)
				{
					Close();
					return true;
				}
			}
			if (keyData == Keys.F5)
			{
				refreshMenuItem_Click(this, EventArgs.Empty);
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void RefreshPortrait()
		{
			if (_groupInstance.isEmpty)
			{
				portraitImage.SetImage(null);
				portraitPanel.Visible = false;
				return;
			}

			var character = _groupInstance.members
				.Select(id => _charactersById.GetOrDefault(id))
				.Where(c => c.isUser == false)
				.FirstOrDefault();

			string[] imageUrls;
			var error = Backyard.GetImageUrls(character, out imageUrls);
			if (error == Backyard.Error.NoError && imageUrls.Length > 0)
			{
				Image image;
				if (Utility.LoadImageFromFile(imageUrls[0], out image))
				{
					portraitImage.SetImage(ImageRef.FromImage(image));
					portraitPanel.Visible = true;
					return;
				}
			}
			portraitImage.SetImage(null);
			portraitPanel.Visible = false;
		}

		private void RefreshPortraitPosition()
		{
			portraitImage.Left = (portraitPanel.Width - portraitImage.Width) / 2;
		}

		private Backyard.Error RunTask(Func<Backyard.Error> action, string statusText = null)
		{
			if (statusText != null)
			{
				this.statusLabel.Text = statusText;
				statusBar.Refresh();
			}

			this.Cursor = Cursors.WaitCursor;
			var error = action.Invoke();
			this.Cursor = Cursors.Default;

			if (statusText != null)
				this.statusLabel.Text = "";
			return error;
		}

		public override void ApplyTheme()
		{
			base.ApplyTheme();

			this.Suspend();
			chatInstanceList.ForeColor = Theme.Current.TreeViewForeground;
			chatInstanceList.BackColor = Theme.Current.TreeViewBackground;
			chatInstanceList.Invalidate();

			chatView.ForeColor = Theme.Current.TextBoxForeground;
			chatView.BackColor = Theme.IsDarkModeEnabled ? Theme.Current.TextBoxBackground : Color.WhiteSmoke;
			chatView.listBox.ForeColor = Theme.Current.TextBoxForeground;
			chatView.listBox.BackColor = Theme.IsDarkModeEnabled ? Theme.Current.TextBoxBackground : Color.WhiteSmoke;
			ViewChat(_selectedChatInstance);
			chatView.Invalidate();
			this.Resume();
		}

		private void editModelSettingsMenuItem_Click(object sender, EventArgs e)
		{
			EditAllSettings();
		}

	}
}
