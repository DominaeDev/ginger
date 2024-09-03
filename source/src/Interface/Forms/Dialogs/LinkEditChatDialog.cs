using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Bridge = Ginger.BackyardBridge;

namespace Ginger
{
	public partial class LinkEditChatDialog : Form
	{
		public Bridge.GroupInstance Group { get; set; }

		private FormWindowState _lastWindowState;

		#region Win32 stuff
		public const uint LVM_SETHOTCURSOR = 4158;

		[DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
		#endregion

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

		public LinkEditChatDialog()
		{
			InitializeComponent();

			this.Load += OnLoad;
			this.Shown += LinkEditChatDialog_Shown;
			this.ResizeEnd += LinkEditChatDialog_ResizeEnd;
			chatInstanceList.Resize += ChatInstanceList_Resize;
			chatInstanceList.ItemSelectionChanged += ChatInstanceList_ItemSelectionChanged;
		}

		private void OnLoad(object sender, EventArgs e)
		{
			chatInstanceList.Columns[0].Width = chatInstanceList.Width - chatInstanceList.Columns[1].Width - 4;
			chatInstanceList.Items.Clear();

			// Fix for flickering cursor
			SendMessage(chatInstanceList.Handle, LVM_SETHOTCURSOR, IntPtr.Zero, Cursors.Arrow.Handle);

			// Refresh character list
			if (Bridge.RefreshCharacters() != Bridge.Error.NoError)
			{
				MessageBox.Show(Resources.error_read_data, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}
		}

		private void LinkEditChatDialog_Shown(object sender, EventArgs e)
		{
			var groupDlg = new LinkSelectGroupDialog();
			groupDlg.Characters = Bridge.Characters.ToArray();
			groupDlg.Groups = Bridge.Groups.ToArray();
			groupDlg.Folders = Bridge.Folders.ToArray();
			if (groupDlg.ShowDialog() != DialogResult.OK)
			{
				Close();
				return;
			}

			this.Group = groupDlg.SelectedGroup;

			PopulateChatList();
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
		}

		private void PopulateChatList()
		{
			// List chats for group
			Bridge.ChatInstance[] chats;
			Bridge.GetChats(Group, out chats);

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

					if (chats[i].history.Count >= 2)
						item.ImageIndex = 0;	// Chat icon
					else
						item.ImageIndex = 1;	// Empty chat icon

					if (chats[i].updateDate.Date == now.Date) // Today
					{
						item.SubItems.Add(chats[i].updateDate.ToString("t", CultureInfo.InvariantCulture));
						if (groupToday == null)
							groupToday = chatInstanceList.Groups.Add("today", "Today");
						item.Group = groupToday;
					}
					else if (chats[i].updateDate.Date == now.Date - TimeSpan.FromDays(1)) // Yesterday
					{
						item.SubItems.Add(chats[i].updateDate.ToString("t"));
						if (groupYesterday == null)
							groupYesterday = chatInstanceList.Groups.Add("yesterday", "Yesterday");
						item.Group = groupYesterday;
					}
					else if (chats[i].updateDate.Year == now.Year) // Older than yesterday
					{
						item.SubItems.Add(chats[i].updateDate.ToString("m", CultureInfo.InvariantCulture));
						if (groupOlder == null)
							groupOlder = chatInstanceList.Groups.Add("older", "Older than yesterday");
						item.Group = groupOlder;
					}
					else if (chats[i].updateDate.Year < now.Year) // Last year
					{
						item.SubItems.Add(chats[i].updateDate.ToString("d"));
						if (groupLastYear == null)
							groupLastYear = chatInstanceList.Groups.Add("year", "Last year");
						item.Group = groupLastYear;
					}
				}
			}

			chatInstanceList.EndUpdate();
		}

		private void ViewChat(Bridge.ChatInstance chatInstance)
		{
			chatView.Items.Clear();

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
					message = entry.message,
					timestamp = timestamp,
				});
			}
			chatView.Items.AddRange(lines.ToArray());
			chatView.listBox.TopIndex = 0; // chatView.Items.Count - 1;

		}

		private void btnExport_Click(object sender, EventArgs e)
		{
			ExportChat();
		}

		private void btnImport_Click(object sender, EventArgs e)
		{
			ImportChat();
		}

		private void ExportChat()
		{
			if (chatInstanceList.SelectedItems.Count == 0)
				return; // No selection

			var chatInstance = chatInstanceList.SelectedItems[0].Tag as Bridge.ChatInstance;
			if (chatInstance == null)
				return; // Error

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
			
			PopulateChatList();
			SelectChat(chatInstance.instanceId);

			return true;
		}

		private void SelectChat(string instanceId)
		{
			for (int i = 0; i < chatInstanceList.Items.Count; ++i)
			{
				if (((Bridge.ChatInstance)chatInstanceList.Items[i].Tag).instanceId == instanceId)
				{
					chatInstanceList.Items[i].Focused = true;
					chatInstanceList.Items[i].Selected = true;
					chatInstanceList.Select();
					chatInstanceList.EnsureVisible(i);
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
	}
}
