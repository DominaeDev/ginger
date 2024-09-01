using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Bridge = Ginger.BackyardBridge;

namespace Ginger
{
	public partial class LinkEditChatDialog : Form
	{
		public Bridge.GroupInstance Group { get; set; }

		public LinkEditChatDialog()
		{
			InitializeComponent();

			this.Load += OnLoad;
			this.ResizeEnd += LinkEditChatDialog_ResizeEnd;
			chatInstanceList.Resize += ChatInstanceList_Resize;
			chatInstanceList.ItemSelectionChanged += ChatInstanceList_ItemSelectionChanged;
		}

		private FormWindowState _lastWindowState;

		protected override void OnClientSizeChanged(EventArgs e)
		{
			base.OnClientSizeChanged(e);

			if (WindowState != _lastWindowState)
			{
				_lastWindowState = WindowState;

				chatListBox.ResizeItems();
			}
		}

		private void LinkEditChatDialog_ResizeEnd(object sender, EventArgs e)
		{
			chatListBox.ResizeItems();
		}

		private void OnLoad(object sender, EventArgs e)
		{
			chatInstanceList.Columns[0].Width = chatInstanceList.Width - chatInstanceList.Columns[1].Width - 4;
			chatInstanceList.Items.Clear();

			// Fix for flickering cursor
			SendMessage(chatInstanceList.Handle, LVM_SETHOTCURSOR, IntPtr.Zero, Cursors.Arrow.Handle);

			PopulateChatList();
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
			chatListBox.Items.Clear();

			var namesByConfigId = chatInstance.participants
				.Select(id => new {
					id = id,
					name = BackyardBridge.GetCharacter(id).name ?? "Unknown",
				})
				.ToDictionary(x => x.id, x => x.name);

			List<string> lines = new List<string>();
			for (int i = 0; i < chatInstance.entries.Length; ++i)
			{
				var entry = chatInstance.entries[i];
				lines.Add(string.Format("{0}:\n{1}", namesByConfigId[entry.configId], entry.message));
			}
			chatListBox.Items.AddRange(lines.ToArray());
		}

		public const uint LVM_SETHOTCURSOR = 4158;

		[DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
	}
}
