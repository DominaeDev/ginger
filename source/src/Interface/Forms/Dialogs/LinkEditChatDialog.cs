using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
		}

		private void OnLoad(object sender, EventArgs e)
		{
			chatList.Columns[0].Width = chatList.Width - chatList.Columns[1].Width - 4;
		}

		private void chatList_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
		{
			e.NewWidth = chatList.Columns[e.ColumnIndex].Width;
			e.Cancel = true;
		}
	}
}
