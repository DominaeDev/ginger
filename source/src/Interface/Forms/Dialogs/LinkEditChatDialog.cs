﻿using System;
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


namespace Ginger
{
	using CharacterInstance = Backyard.CharacterInstance;
	using GroupInstance = Backyard.GroupInstance;
	using ChatInstance = Backyard.ChatInstance;
	using ChatStaging = Backyard.ChatStaging;
	using CharacterMessage = Backyard.CharacterMessage;

	public partial class LinkEditChatDialog : FormEx
	{
		public GroupInstance Group { set { _groupInstance = value; } }
		private GroupInstance _groupInstance;
		private Dictionary<string, CharacterInstance> _charactersById;
		private ChatInstance _selectedChatInstance = null;

		private FormWindowState _lastWindowState;
		private System.Timers.Timer _statusbarTimer = new System.Timers.Timer();
		private bool _bEditing = false;

		// Find
		private ChatSearchable[] _chatSearchables = null;
		private FindDialog _findDialog;

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
			editModelSettingsMenuItem.ToolTipText = Resources.tooltip_link_model_settings_all;
		}

		private void OnLoad(object sender, EventArgs e)
		{
			chatInstanceList.Columns[0].Width = chatInstanceList.Width - chatInstanceList.Columns[1].Width - 4;
			chatInstanceList.Items.Clear();

			RefreshPortraitPosition();

			// Fix for flickering cursor
			SendMessage(chatInstanceList.Handle, LVM_SETHOTCURSOR, IntPtr.Zero, Cursors.Arrow.Handle);

			_charactersById = Backyard.Everyone.ToDictionary(c => c.instanceId, c => c);

			RefreshTitle();
		}
		
		private void LinkEditChatDialog_Shown(object sender, EventArgs e)
		{
			RefreshChats();
			RefreshPortrait();
		}

		private void RefreshTitle()
		{
			var groupType = _groupInstance.GetGroupType();

			if (groupType != GroupInstance.GroupType.Unknown)
			{
				Text = string.Format("{0} - Chat history", GetGroupTitle(_groupInstance));
				if (groupType == GroupInstance.GroupType.Party)
					Text = "(Party) ";
			}
			else
				Text = "Chat history";
		}

		private string GetGroupTitle(GroupInstance group)
		{
			if (group.isDefined == false)
			{
				return "Undefined";
			}
			else if (string.IsNullOrEmpty(group.displayName) == false)
			{
				return group.displayName;
			}
			else
			{
				var characters = group.activeMembers
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
			if (_groupInstance.isDefined == false)
				return Constants.DefaultCharacterName;
			else if (string.IsNullOrEmpty(_groupInstance.displayName) == false)
				return _groupInstance.displayName;
			else
			{
				var characters = _groupInstance.activeMembers
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
			if (Backyard.ConnectionEstablished == false
				|| (_groupInstance.isDefined && RunTask(() => Backyard.Database.GetChats(_groupInstance.instanceId, out chats)) != Backyard.Error.NoError))
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_error, this);
				Close();
				return;
			}

			_chatSearchables = null;
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

			HideFindDialog();
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

		private void ViewChat(ChatInstance chatInstance, int select = -1)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_error, this);
				Close();
				return;
			}

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
			participants.AddRange(chatInstance.participants.Select(id => Backyard.Database.GetCharacter(id)).Where(c => c.isUser)); // User(s) first
			participants.AddRange(chatInstance.participants.Select(id => Backyard.Database.GetCharacter(id)).Where(c => c.isUser == false));

			var speakerNames = participants
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
					color = nameColors[entry.speaker % nameColors.Length],
					name = speakerNames[entry.speaker],
					message = entry.text,
					timestamp = timestamp,
				});
			}
			chatView.Items.AddRange(lines.ToArray());
			chatView.listBox.TopIndex = 0; // chatView.Items.Count - 1;

			if (select >= 0)
			{
				chatView.listBox.SelectedIndex = select;
			}

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
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_create_chat, this);
				Close();
				return;
			}

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
			if (GetLatestChat(_groupInstance.instanceId, out latestChat))
			{
				args.parameters = latestChat.parameters;
				args.staging = latestChat.staging;
			}
			else
				args.parameters = AppSettings.BackyardSettings.UserSettings;

			ChatInstance chatInstance = null;
			var error = RunTask(() => Backyard.Database.CreateNewChat(args, _groupInstance.instanceId, out chatInstance), "Creating chat...");
			if (error == Backyard.Error.NotConnected)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_create_chat, this);
				Close();
				return;
			}
			else if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_create_chat, Resources.cap_link_create_chat, this);
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
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_import_chat, this);
				Close();
				return;
			}

			// Open file...
			importFileDialog.FileName = "";
			importFileDialog.Title = Resources.cap_import_chat;
			importFileDialog.Filter = "All supported file types|*.json;*.jsonl;*.log;*.txt";
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
				MsgBox.Error(Resources.error_unrecognized_chat_format, Resources.cap_import_chat, this);
				return;
			}

			int numSpeakers = chatHistory.numSpeakers;
			if (numSpeakers > _groupInstance.Count)
			{
				if (MsgBox.Ask(Resources.msg_link_chat_too_many_speakers, Resources.cap_import_chat, this) == false)
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
			if (GetLatestChat(_groupInstance.instanceId, out latestChat))
			{
				args.parameters = latestChat.parameters;
				args.staging = latestChat.staging;
			}
			else
				args.parameters = AppSettings.BackyardSettings.UserSettings;

			var error = RunTask(() => Backyard.Database.CreateNewChat(args, _groupInstance.instanceId, out chatInstance), "Importing chat...");
			if (error == Backyard.Error.NotConnected)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_import_chat, this);
				Close();
				return;
			}
			else if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_import_chat, Resources.cap_import_chat, this);
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

			// User
			var userName = _groupInstance.activeMembers
				.Select(id => _charactersById.GetOrDefault(id))
				.Where(c => c.isUser)
				.FirstOrDefault()
				.name ?? GingerString.BackyardUserMarker;

			// Character (first)
			var characterName = _groupInstance.activeMembers
				.Select(id => _charactersById.GetOrDefault(id))
				.Where(c => c.isCharacter)
				.FirstOrDefault()
				.name ?? GingerString.BackyardCharacterMarker;

			for (int i = 0; i < chatHistory.messages.Length; ++i)
			{
				for (int j = 0; j < chatHistory.messages[i].swipes.Length; ++j)
				{
					StringBuilder sb = new StringBuilder(chatHistory.messages[i].swipes[j]);
					Utility.ReplaceWholeWord(sb, GingerString.TavernUserMarker, "__UUUU__", StringComparison.OrdinalIgnoreCase);
					Utility.ReplaceWholeWord(sb, GingerString.UserMarker, "__UUUU__", StringComparison.OrdinalIgnoreCase);
					Utility.ReplaceWholeWord(sb, GingerString.CharacterMarker, "__CCCC__", StringComparison.OrdinalIgnoreCase);
					Utility.ReplaceWholeWord(sb, GingerString.TavernCharacterMarker, "__CCCC__", StringComparison.OrdinalIgnoreCase);
					Utility.ReplaceWholeWord(sb, GingerString.BackyardCharacterMarker, "__CCCC__", StringComparison.OrdinalIgnoreCase);
					Utility.ReplaceWholeWord(sb, "__UUUU__", userName, StringComparison.Ordinal);
					Utility.ReplaceWholeWord(sb, "__CCCC__", characterName, StringComparison.Ordinal);
					chatHistory.messages[i].swipes[j] = sb.ToString();
				}
			}
		}

		private void ExportChat(ChatInstance chatInstance)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_export_error, this);
				Close();
				return;
			}

			if (chatInstance == null)
				return; // Error

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
				exportFileDialog.Filter = "Ginger chat log|*.log|Text file|*.txt";
				if (filterIndex == SoloTextFile) // Text
					filterIndex = GroupTextFile;
				else
					filterIndex = GroupGinger;

				if (filterIndex == GroupTextFile) // Text
					filename = string.Concat(filename, ".txt");
				else if (filterIndex == GroupGinger)
					filename = string.Concat(filename, ".log");
				else // json
					filename = string.Concat(filename, ".json");
			}
			else
			{
				exportFileDialog.Filter = "Backyard chat log|*.json|Ginger chat log|*.log|SillyTavern chat log|*.jsonl|Text generation web ui chat log|*.json|Text file|*.txt";
				
				if (filterIndex == SoloSillyTavern)
					filename = string.Concat(filename, ".jsonl");
				else if (filterIndex == SoloTextFile)
					filename = string.Concat(filename, ".txt");
				else if (filterIndex == SoloGinger)
					filename = string.Concat(filename, ".log");
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
				.Select(id => Backyard.Database.GetCharacter(id).name)
				.ToArray();

			if (exportFileDialog.FilterIndex == SoloBackyard) // Backyard
			{
				if (FileUtil.ExportBackyardChat(chatInstance.history, exportFileDialog.FileName))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == SoloGinger) // Ginger
			{
				var speakers = new GingerChatV2.SpeakerList();
				for (int i = 0; i < chatInstance.participants.Length; ++i)
				{
					string name = null;
					CharacterInstance instance;
					if (_charactersById.TryGetValue(chatInstance.participants[i], out instance))
						name = instance.name;

					speakers.Add(new GingerChatV2.Speaker() {
						id = i.ToString(),
						name = name ?? Constants.UnknownCharacter,
					});
				}

				var chat = GingerChatV2.FromChat(chatInstance, speakers);
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
			MsgBox.Error(Resources.error_write_json, Resources.cap_export_error, this);
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
				MsgBox.LinkError.Disconnected(Resources.cap_link_error, this);
				Close();
				return;
			}
			
			_charactersById = Backyard.Everyone.ToDictionary(c => c.instanceId, c => c);

			var dlg = new LinkSelectCharacterOrGroupDialog();
			dlg.Options = LinkSelectCharacterOrGroupDialog.Option.Solo;
			if (BackyardValidation.CheckFeature(BackyardValidation.Feature.GroupChat))
				dlg.Options |= LinkSelectCharacterOrGroupDialog.Option.Parties;
			if (dlg.ShowDialog() == DialogResult.OK)
				_groupInstance = dlg.SelectedGroup;

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
			if (Backyard.ConnectionEstablished == false)
			{
				e.CancelEdit = true;
				return;
			}

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
			var error = RunTask(() => Backyard.Database.RenameChat(_selectedChatInstance.instanceId, newName), "Renaming chat...");
			if (error == Backyard.Error.NoError)
			{
				SetStatusBarMessage(Resources.status_link_renamed_chat, Constants.StatusBarMessageInterval);
				e.CancelEdit = false;
			}
			else if (error == Backyard.Error.NotFound)
			{
				MsgBox.Error(Resources.error_link_chat_not_found, Resources.cap_link_rename_chat, this);
				e.CancelEdit = true;
				RefreshChats();
			}
			else
			{
				MsgBox.Error(Resources.error_link_rename_chat, Resources.cap_link_rename_chat, this);
				e.CancelEdit = true;
			}
		}

		private void DeleteChat(ChatInstance chatInstance)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_delete_chat, this);
				Close();
				return;
			}

			if (chatInstance == null)
				return;

			if (ConfirmChatExists(chatInstance.instanceId, out var _, Resources.cap_link_delete_chat) == false)
				return;

			// Fetch chat counts
			int chatCounts;
			if (Backyard.Database.ConfirmDeleteChat(chatInstance.instanceId, _groupInstance.instanceId, out chatCounts) != Backyard.Error.NoError)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_delete_chat, this);
				return;
			}

			// Confirm
			if (MsgBox.Confirm(string.Format(Resources.msg_link_delete_confirm, chatInstance.name), Resources.cap_link_delete_chat, this) == false)
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
				error = RunTask(() => Backyard.Database.UpdateChat(chatInstance.instanceId, chatInstance, _groupInstance.instanceId), "Deleting chat...");
			}
			else
			{
				// Delete chat
				error = RunTask(() => Backyard.Database.DeleteChat(chatInstance.instanceId), "Deleting chat...");
			}

			if (error == Backyard.Error.NotConnected)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_delete_chat, this);
				Close();
			}
			else if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_delete_chat, Resources.cap_link_delete_chat, this);
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
			bool hasGroup = _groupInstance.isDefined;
			bool hasChat = _selectedChatInstance != null;

			importMenuItem.Enabled = hasGroup;
			exportMenuItem.Enabled = hasGroup && hasChat;
			duplicateMenuItem.Enabled = hasGroup && hasChat;
			refreshMenuItem.Enabled = hasGroup;
			editModelSettingsMenuItem.Enabled = hasGroup;

			setBackgroundMenuItem.Visible = BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds);
			setBackgroundMenuItem.Enabled = hasGroup;
			
			bool bCanFindNext = hasGroup && string.IsNullOrEmpty(AppSettings.User.FindMatch) == false;
			findMenuItem.Enabled = hasGroup;
			findNextMenuItem.Enabled = bCanFindNext;
			findPreviousMenuItem.Enabled = bCanFindNext;	
		}

		private void duplicateMenuItem_Click(object sender, EventArgs e)
		{
			DuplicateChat(_selectedChatInstance);
		}

		public void DuplicateChat(ChatInstance chatInstance)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_duplicate_chat, this);
				Close();
				return;
			}

			if (chatInstance == null || _groupInstance.isDefined == false)
				return;

			// Fetch latest version of this chat
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
			var error = RunTask(() => Backyard.Database.CreateNewChat(args, _groupInstance.instanceId, out duplicate), "Duplicating chat...");

			if (error == Backyard.Error.NotConnected)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_duplicate_chat, this);
				Close();
			}
			else if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_duplicate_chat, Resources.cap_link_duplicate_chat, this);
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
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_purge_chat, this);
				Close();
				return;
			}

			if (_groupInstance.isDefined == false)
				return; // Error

			if (MsgBox.Confirm(string.Format(Resources.msg_link_purge_chat, GetGroupTitle(_groupInstance)), Resources.cap_link_purge_chat, this) == false)
				return;

			var error = RunTask(() => Backyard.Database.DeleteAllChats(_groupInstance.instanceId), "Deleting chat history...");
			if (error == Backyard.Error.NotConnected)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_purge_chat, this);
				Close();
			}
			else if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_purge_chat, Resources.cap_link_purge_chat, this);
			}
			else
			{
				PopulateChatList(true);
				SetStatusBarMessage(Resources.status_link_purged_chat, Constants.StatusBarMessageInterval);
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
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_branch_chat, this);
				Close();
				return;
			}

			if (chatInstance == null || string.IsNullOrEmpty(messageId) || _groupInstance.isDefined == false)
				return; // Error

			// Fetch latest version of this chat
			ChatInstance latestChat;
			if (ConfirmChatExists(chatInstance.instanceId, out latestChat) == false)
				latestChat = chatInstance;

			var chatHistory = (ChatHistory)latestChat.history.Clone();
			chatHistory.name = string.Concat(latestChat.name, " (branch)");

			int messageIndex = Array.FindIndex(chatHistory.messages, m => m.instanceId == messageId);
			if (messageIndex == -1)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_branch_chat, this);
				return;
			}
			Array.Resize(ref chatHistory.messages, messageIndex + 1);

			ChatInstance branchedChat;
			var args = new Backyard.CreateChatArguments() {
				history = chatHistory,
				staging = latestChat.staging,
				parameters = latestChat.parameters,
			};

			var error = RunTask(() => Backyard.Database.CreateNewChat(args, _groupInstance.instanceId, out branchedChat), "Branching chat...");

			if (error == Backyard.Error.NotConnected)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_branch_chat, this);
				Close();
			}
			else if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_branch_chat, Resources.cap_link_branch_chat, this);
			}
			else
			{
				PopulateChatList(true);
				SetStatusBarMessage(Resources.status_link_branched_chat, Constants.StatusBarMessageInterval);
			}
		}

		private void RestartChat(ChatInstance chatInstance, string messageId)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_restart_chat, this);
				Close();
				return;
			}

			if (chatInstance == null || string.IsNullOrEmpty(messageId))
				return; // Error

			// Confirm
			if (MsgBox.Confirm(string.Format(Resources.msg_link_restart_confirm, chatInstance.name), Resources.cap_link_restart_chat, this) == false)
				return;

			// Fetch latest version of this chat
			ChatInstance latestChat;
			if (ConfirmChatExists(chatInstance.instanceId, out latestChat) == false)
				latestChat = chatInstance;

			latestChat.updateDate = DateTime.Now;
			int messageIndex = Array.FindIndex(latestChat.history.messages, m => m.instanceId == messageId);
			if (messageIndex == -1)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_restart_chat, this);
				return;
			}

			var messages = new ChatHistory.Message[latestChat.history.messages.Length - messageIndex];
			Array.Copy(latestChat.history.messages, messageIndex, messages, 0, latestChat.history.messages.Length - messageIndex);
			latestChat.history.messages = messages;

			var error = RunTask(() => Backyard.Database.UpdateChat(chatInstance.instanceId, latestChat, _groupInstance.instanceId), "Updating chat...");
			if (error == Backyard.Error.NotConnected)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_restart_chat, this);
				Close();
			}
			else if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_restart_chat, this);
			}
			else
			{
				PopulateChatList(true);
				SetStatusBarMessage(Resources.status_link_restarted_chat, Constants.StatusBarMessageInterval);
			}
		}

		private void ScrubChat(ChatInstance chatInstance, string messageId)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_scrub_chat, this);
				Close();
				return;
			}

			if (chatInstance == null || string.IsNullOrEmpty(messageId))
				return; // Error

			// Confirm
			if (MsgBox.Confirm(string.Format(Resources.msg_link_scrub_confirm, chatInstance.name), Resources.cap_link_scrub_chat, this) == false)
				return;

			// Fetch latest version of this chat
			ChatInstance latestChat;
			if (ConfirmChatExists(chatInstance.instanceId, out latestChat) == false)
				latestChat = chatInstance;

			latestChat.updateDate = DateTime.Now;
			int messageIndex = Array.FindIndex(latestChat.history.messages, m => m.instanceId == messageId);
			if (messageIndex == -1)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_scrub_chat, this);
				return;
			}
			Array.Resize(ref latestChat.history.messages, messageIndex);
		
			var error = RunTask(() => Backyard.Database.UpdateChat(chatInstance.instanceId, latestChat, _groupInstance.instanceId), "Scrubbing chat...");
			if (error == Backyard.Error.NotConnected)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_scrub_chat, this);
				Close();
			}
			else if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_scrub_chat, this);
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

				if (BackyardValidation.CheckFeature(BackyardValidation.Feature.ChatBackgrounds))
				{
					var backgroundSubmenu = new ToolStripMenuItem("Set background");

					backgroundSubmenu.DropDownItems.Add(new ToolStripMenuItem("Load from file...", null, (s, e) => {
						SetChatBackgroundFromFile(new string[] { (item.Tag as ChatInstance).instanceId });
					}));

					backgroundSubmenu.DropDownItems.Add(new ToolStripMenuItem("Same as portrait", null, (s, e) => {
						SetChatBackgroundFromPortrait(new string[] { (item.Tag as ChatInstance).instanceId });
					}));

					backgroundSubmenu.DropDownItems.Add(new ToolStripSeparator());

					backgroundSubmenu.DropDownItems.Add(new ToolStripMenuItem("Clear background", null, (s, e) => {
						ClearChatBackground(new string[] { (item.Tag as ChatInstance).instanceId });
					}));

					menu.Items.Add(backgroundSubmenu);
				}

				menu.Items.Add(new ToolStripSeparator());

				menu.Items.Add(new ToolStripMenuItem("Delete chat", null, (s, e) => {
					DeleteChat(item.Tag as ChatInstance);
				}) {
					ToolTipText = Resources.tooltip_link_delete_chat,
				});
			}
			else
			{
				bool hasGroup = _groupInstance.isDefined;
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
				MsgBox.LinkError.Disconnected(Resources.cap_link_error, this);
				Close();
				return;
			}

			_charactersById = Backyard.Everyone.ToDictionary(c => c.instanceId, c => c);

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
				MsgBox.LinkError.Disconnected(Resources.cap_link_copy_staging, this);
				Close();
				return;
			}

			// Fetch latest version of this chat
			ChatInstance latestChat;
			if (ConfirmChatExists(chatInstance.instanceId, out latestChat) == false)
				latestChat = chatInstance;

			if (latestChat.staging == null)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_copy_staging, this);
				return;
			}

			Clipboard.SetDataObject(ChatStagingClipboard.FromStaging(latestChat.staging), false);

			SetStatusBarMessage(Resources.status_link_copy_staging, Constants.StatusBarMessageInterval);
		}
		
		private void PasteStaging(ChatInstance chatInstance)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_paste_staging, this);
				return;
			}

			if (Clipboard.ContainsData(ChatStagingClipboard.Format) == false)
				return;

			ChatStagingClipboard clip = Clipboard.GetData(ChatStagingClipboard.Format) as ChatStagingClipboard;
			if (clip == null)
				return;

			ChatStaging staging = clip.staging;
			if (staging == null)
				return;

			staging.ClearCharacterIds();

			var error = RunTask(() => Backyard.Database.UpdateChatParameters(chatInstance.instanceId, staging, null), "Updating chat...");
			if (error == Backyard.Error.NotFound)
			{
				MsgBox.Error(Resources.error_link_chat_not_found, Resources.cap_link_paste_staging, this);
				RefreshChats();
				return;
			}
			if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_paste_staging, this);
				return;
			}

			RefreshChats();
			SetStatusBarMessage(Resources.status_link_paste_staging, Constants.StatusBarMessageInterval);
		}

		private void EditSettings(ChatInstance chatInstance)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_edit_chat_settings, this);
				return;
			}

			// Model settings dialog
			var dlg = new EditModelSettingsDialog();
			dlg.Editing = chatInstance.parameters;
			if (dlg.ShowDialog() != DialogResult.OK)
				return;

			var error = RunTask(() => Backyard.Database.UpdateChatParameters(chatInstance.instanceId, null, dlg.Parameters), "Updating model settings...");
			if (error == Backyard.Error.NotFound)
			{
				MsgBox.Error(Resources.error_link_chat_not_found, Resources.cap_link_edit_chat_settings, this);
				RefreshChats();
				return;
			}
			if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_edit_chat_settings, this);
				return;
			}

			RefreshChats();
			SetStatusBarMessage(Resources.status_link_reset_settings, Constants.StatusBarMessageInterval);
		}
		
		private void EditAllSettings()
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_edit_chat_settings, this);
				return;
			}

			ChatInstance[] chats = null;
			if (_groupInstance.isDefined && RunTask(() => Backyard.Database.GetChats(_groupInstance.instanceId, out chats)) != Backyard.Error.NoError)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_edit_chat_settings, this);
				Close();
				return;
			}

			if (chats == null || chats.Length == 0)
			{
				MsgBox.Error("No chats.", Resources.cap_link_edit_chat_settings, this);
				return;
			}

			// Model settings dialog
			var dlg = new EditModelSettingsDialog();
			dlg.Editing = chats[0].parameters;
			if (dlg.ShowDialog() != DialogResult.OK)
				return;

			string[] chatIds = chats.Select(c => c.instanceId).ToArray();
			var error = RunTask(() => Backyard.Database.UpdateChatParameters(chatIds, null, dlg.Parameters), "Updating model settings...");
			if (error == Backyard.Error.NotFound)
			{
				MsgBox.Error(Resources.error_link_chat_not_found, Resources.cap_link_edit_chat_settings, this);
				RefreshChats();
				return;
			}
			if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_edit_chat_settings, this);
				return;
			}

			RefreshChats();
			SetStatusBarMessage(Resources.status_link_update_model_settings, Constants.StatusBarMessageInterval);
		}

		private bool ConfirmChatExists(string chatId, out ChatInstance chatInstance, string errorCaption = null)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				chatInstance = default(ChatInstance);
				return false;
			}

			ChatInstance[] chats = null;
			var error = RunTask(() => Backyard.Database.GetChats(_groupInstance.instanceId, out chats));
			if (error == Backyard.Error.NoError)
			{
				chatInstance = Array.Find(chats, c => c.instanceId == chatId);
				return chatInstance != null;
			}

			chatInstance = default(ChatInstance);

			if (errorCaption == null)
				return chatInstance != null; // Silent

			if (error == Backyard.Error.NotConnected)
			{
				MsgBox.LinkError.Disconnected(errorCaption, this);
				Close();
				return false;
			}
			else if (error == Backyard.Error.NotFound)
			{
				MsgBox.Error(Resources.error_link_chat_not_found, errorCaption, this);
				RefreshChats();
				return false;
			}
			else if (error == Backyard.Error.Unknown)
			{
				MsgBox.Error(Resources.error_link_general, errorCaption, this);
				return false;
			}

			return chatInstance != null;
		}

		private bool GetLatestChat(string groupId, out ChatInstance chatInstance)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				chatInstance = default(ChatInstance);
				return false;
			}

			return Backyard.Database.FetchLatestChat(groupId, out chatInstance);
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
			else if (keyData == Keys.F5)
			{
				refreshMenuItem_Click(this, EventArgs.Empty);
				return true;
			}
			else if (keyData == ShortcutKeys.Find)
			{
				findMenuItem_Click(this, EventArgs.Empty);
				return true;
			}
			else if (keyData == ShortcutKeys.FindNext)
			{
				findNextMenuItem_Click(this, EventArgs.Empty);
				return true;
			}
			else if (keyData == ShortcutKeys.FindPrevious)
			{
				findPreviousMenuItem_Click(this, EventArgs.Empty);
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void RefreshPortrait()
		{
			if (_groupInstance.isDefined == false)
			{
				portraitImage.SetImage(null);
				portraitPanel.Visible = false;
				return;
			}

			// First non-user
			var character = _groupInstance.activeMembers
				.Select(id => _charactersById.GetOrDefault(id))
				.Where(c => c.isCharacter)
				.FirstOrDefault();

			if (Backyard.ConnectionEstablished)
			{
				string[] imageUrls;
				var error = Backyard.Database.GetImageUrls(character.configId, out imageUrls);
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

		private void setBackgroundFromFileMenuItem_Click(object sender, EventArgs e)
		{
			if (_groupInstance.isDefined == false)
				return;

			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_update_chat_background, this);
				Close();
				return;
			}

			ChatInstance[] chats = null;
			if (RunTask(() => Backyard.Database.GetChats(_groupInstance.instanceId, out chats)) != Backyard.Error.NoError)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_update_chat_background, this);
				Close();
				return;
			}

			if (chats == null || chats.Length == 0)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_update_chat_background, this);
				return;
			}

			SetChatBackgroundFromFile(chats.Select(c => c.instanceId).ToArray());
		}

		private void setBackgroundFromPortraitMenuItem_Click(object sender, EventArgs e)
		{
			if (_groupInstance.isDefined == false)
				return;

			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_update_chat_background, this);
				Close();
				return;
			}

			ChatInstance[] chats = null;
			if (RunTask(() => Backyard.Database.GetChats(_groupInstance.instanceId, out chats)) != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_update_chat_background, this);
				return;
			}

			if (chats == null || chats.Length == 0)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_update_chat_background, this);
				return;
			}

			SetChatBackgroundFromPortrait(chats.Select(c => c.instanceId).ToArray());
		}

		private void clearBackgroundMenuItem_Click(object sender, EventArgs e)
		{
			if (_groupInstance.isDefined == false)
				return;

			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_update_chat_background, this);
				Close();
				return;
			}

			ChatInstance[] chats = null;
			if (RunTask(() => Backyard.Database.GetChats(_groupInstance.instanceId, out chats)) != Backyard.Error.NoError)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_update_chat_background, this);
				Close();
				return;
			}

			if (chats == null || chats.Length == 0)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_update_chat_background, this);
				Close();
				return;
			}

			// Update chats
			ClearChatBackground(chats.Select(c => c.instanceId).ToArray());
		}

		
		private void SetChatBackgroundFromFile(string[] chatIds)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_update_chat_background, this);
				Close();
				return;
			}

			// Open file...
			importFileDialog.Title = Resources.cap_open_image;
			importFileDialog.Filter = "Supported image formats|*.png;*.jpeg;*.jpg;*.gif;*.apng;*.webp|PNG images|*.png;*.apng|JPEG images|*.jpg;*.jpeg|GIF images|*.gif|WEBP images|*.webp";
			importFileDialog.InitialDirectory = AppSettings.Paths.LastImagePath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			var result = importFileDialog.ShowDialog();
			if (result != DialogResult.OK)
				return;

			AppSettings.Paths.LastImagePath = Path.GetDirectoryName(importFileDialog.FileName);

			string sourceFilename = importFileDialog.FileName;
			var imageData = Utility.LoadFile(sourceFilename);
			int width, height;
			if (imageData == null || Utility.GetImageDimensions(imageData, out width, out height) == false)
			{
				MsgBox.Error(Resources.error_load_image, Resources.cap_link_update_chat_background, this);
				return;
			}

			// Copy image
			string destPath = Path.Combine(AppSettings.BackyardLink.Location, "images");
			string destFilename;
			try
			{
				destFilename = Path.Combine(destPath, Utility.CreateRandomFilename(Utility.GetFileExt(sourceFilename)));
				File.Copy(sourceFilename, destFilename, true);
			}
			catch
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_update_chat_background, this);
				return;
			}

			// Update chats
			var error = RunTask(() => Backyard.Database.UpdateChatBackground(chatIds, destFilename, width, height), "Updating chats...");
			if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_update_chat_background, this);
				return;
			}

			RefreshChats();
			SetStatusBarMessage(Resources.status_link_update_chats, Constants.StatusBarMessageInterval);
		}
						
		private void SetChatBackgroundFromPortrait(string[] chatIds)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_update_chat_background, this);
				Close();
				return;
			}

			if (_groupInstance.isDefined == false)
				return;

			var character = _groupInstance.activeMembers
				.Select(id => _charactersById.GetOrDefault(id))
				.Where(c => c.isCharacter)
				.FirstOrDefault();

			string[] imageUrls;
			var error = Backyard.Database.GetImageUrls(character.configId, out imageUrls);
			if (error != Backyard.Error.NoError || imageUrls.Length == 0)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_update_chat_background, this);
				Close();
				return;
			}

			string sourceFilename = imageUrls[0];
			var imageData = Utility.LoadFile(sourceFilename);
			int width, height;
			if (imageData == null || Utility.GetImageDimensions(imageData, out width, out height) == false)
			{
				MsgBox.Error(Resources.error_load_image, Resources.cap_link_update_chat_background, this);
				return;
			}

			// Copy image
			string destPath = Path.Combine(AppSettings.BackyardLink.Location, "images");
			string destFilename;
			try
			{
				destFilename = Path.Combine(destPath, Utility.CreateRandomFilename(Utility.GetFileExt(sourceFilename)));
				File.Copy(sourceFilename, destFilename, true);
			}
			catch
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_update_chat_background, this);
				return;
			}

			// Update chats
			error = RunTask(() => Backyard.Database.UpdateChatBackground(chatIds, destFilename, width, height), "Updating chats...");
			if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_update_chat_background, this);
				return;
			}

			RefreshChats();
			SetStatusBarMessage(Resources.status_link_update_chats, Constants.StatusBarMessageInterval);
		}
		
		private void ClearChatBackground(string[] chatIds)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_update_chat_background, this);
				Close();
				return;
			}

			// Update chats
			var error = RunTask(() => Backyard.Database.UpdateChatBackground(chatIds, null, 0, 0), "Updating chats...");
			if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_general, Resources.cap_link_update_chat_background, this);
				return;
			}

			RefreshChats();
			SetStatusBarMessage(Resources.status_link_update_chats, Constants.StatusBarMessageInterval);
		}

		private void findMenuItem_Click(object sender, EventArgs e)
		{
			if (_findDialog != null && !_findDialog.IsDisposed)
				_findDialog.Close(); // Close existing

			if (_groupInstance.isDefined == false)
				return;

			_findDialog = new FindDialog();
			_findDialog.Find += OnFind;
			_findDialog.Show(this);
		}

		private void findNextMenuItem_Click(object sender, EventArgs e)
		{
			if (_groupInstance.isDefined == false)
				return;

			OnFind(this, new FindDialog.FindEventArgs() {
				match = AppSettings.User.FindMatch ?? "",
				matchCase = AppSettings.User.FindMatchCase,
				wholeWord = AppSettings.User.FindWholeWords,
				reverse = false,
			});
		}

		private void findPreviousMenuItem_Click(object sender, EventArgs e)
		{
			if (_groupInstance.isDefined == false)
				return;

			OnFind(this, new FindDialog.FindEventArgs() {
				match = AppSettings.User.FindMatch ?? "",
				matchCase = AppSettings.User.FindMatchCase,
				wholeWord = AppSettings.User.FindWholeWords,
				reverse = true,
			});
		}

		private void OnFind(object sender, FindDialog.FindEventArgs e)
		{
			if (string.IsNullOrEmpty(e.match) || _groupInstance.isDefined == false)
				return;

			if (_chatSearchables == null)
			{
				ChatInstance[] chats = null;
				if (Backyard.ConnectionEstablished == false
					|| RunTask(() => Backyard.Database.GetChats(_groupInstance.instanceId, out chats)) != Backyard.Error.NoError)
				{
					MsgBox.LinkError.Disconnected(Resources.cap_link_edit_chat_settings, this);
					Close();
					return;
				}

				if (chats == null || chats.Length == 0)
				{
					MsgBox.Error("No chats.", Resources.cap_link_edit_chat_settings, this);
					return;
				}

				_chatSearchables = chats.Select(c => new ChatSearchable(c)).ToArray();
			}

			if (_chatSearchables.Length == 0)
			{
				// Nothing to search
				MsgBox.Message(Resources.msg_no_match, Resources.cap_find, this);
				return;
			}

			ChatSearchable[] searchables;
			if (e.reverse)
				searchables = _chatSearchables.Reverse().ToArray();
			else
				searchables = _chatSearchables;

			int offset = 0;
			int idxFocused = 0;
			if (_selectedChatInstance != null)
			{
				idxFocused = Array.FindIndex(searchables, s => s.chat.instanceId == _selectedChatInstance.instanceId);
				if (idxFocused == -1)
					idxFocused = 0;

				if (chatView.listBox.SelectedIndex >= 0)
				{
					if (!e.reverse)
						offset = chatView.listBox.SelectedIndex + 1;
					else
					{
						offset = chatView.listBox.SelectedIndex - 1;
						if (offset < 0)
							idxFocused++;
					}
				}
			}

			for (int i = 0; i < searchables.Length + 1; ++i) // +1 Search the first textbox again as we wrap around.
			{
				int index = (idxFocused + i) % searchables.Length;
				var searchable = searchables[index];
				int find = searchable.Find(e.match, e.matchCase, e.wholeWord, e.reverse, i == 0 ? offset : -1);
				if (find != -1)
				{
					SelectChat(searchable.chat.instanceId, false);
					ViewChat(searchable.chat, find);
					return; // Success
				}
			}

			MsgBox.Message(Resources.msg_no_match, Resources.cap_find, this);
		}

		private void HideFindDialog()
		{
			if (_findDialog != null)
				_findDialog.Hide();
		}
	}

	public class ChatSearchable : ISearchable
	{
		public bool Enabled { get { return true; } }
		public TextBoxBase SearchableControl { get { return null; } }

		public ChatInstance chat;
		private string[] _messages;

		public ChatSearchable(ChatInstance chat)
		{
			this.chat = chat;
			_messages = chat.history.messages.Select(m => m.text ?? "").ToArray();
		}

		public int Find(string match, bool matchCase, bool matchWord, bool reverse, int startIndex = -1)
		{
			var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

			if (!reverse)
			{
				if (startIndex >= 0)
					startIndex = Math.Min(startIndex, _messages.Length);
				else
					startIndex = 0;

				for (int idxMsg = startIndex; idxMsg < _messages.Length; ++idxMsg)
				{
					int idx;
					if (matchWord)
						idx = Utility.FindWholeWord(_messages[idxMsg], match, 0, comparison);
					else
						idx = _messages[idxMsg].IndexOf(match, 0, comparison);
					if (idx != -1)
						return idxMsg;
				}
			}
			else
			{
				if (startIndex >= 0)
					startIndex = Math.Min(startIndex, _messages.Length);
				else
					startIndex = _messages.Length - 1;

				for (int idxMsg = startIndex; idxMsg >= 0; --idxMsg)
				{
					int idx;
					if (matchWord)
						idx = Utility.FindWholeWord(_messages[idxMsg], match, 0, comparison);
					else
						idx = _messages[idxMsg].IndexOf(match, 0, comparison);
					if (idx != -1)
						return idxMsg;
				}
			}
			return -1;
		}

		public void FocusAndSelect(int start, int length)
		{
			return;
		}
	}

}
