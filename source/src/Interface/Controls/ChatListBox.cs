using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Ginger.src.Interface.Controls
{
	public partial class ChatListBox : UserControl
	{
		public System.Windows.Forms.ListBox.ObjectCollection Items { get { return listBox.Items; } }

		public ChatListBox()
		{
			InitializeComponent();

			this.FontChanged += ChatListBox_FontChanged;
			this.Load += ChatListBox_Load;
		}

		private void ChatListBox_Load(object sender, EventArgs e)
		{

		}

		private void ChatListBox_FontChanged(object sender, EventArgs e)
		{
			listBox.Font = this.Font;
		}

		private void listBox_DrawItem(object sender, DrawItemEventArgs e)
		{
			if (e.Index >= 0)
			{
				e.DrawBackground();
				e.DrawFocusRectangle();
				e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
				e.Graphics.DrawString(
					 (string)listBox.Items[e.Index],
					 e.Font,
					 new SolidBrush(e.ForeColor),
					 e.Bounds);
			}
		}

		private void listBox_MeasureItem(object sender, MeasureItemEventArgs e)
		{
			if (e.Index >= 0)
			{
				string text = (string)listBox.Items[e.Index];
				Size size = TextRenderer.MeasureText(text, this.Font, new Size(this.Size.Width - 37, 0), TextFormatFlags.WordBreak);
				e.ItemHeight = size.Height + 8;
			}
		}

		private void ChatListView_Resize(object sender, EventArgs e)
		{
			listBox.Location = new Point(0, 0);
			listBox.Size = new Size(this.Width, this.Height);
			listBox.Invalidate();
		}
		
		public void ResizeItems()
		{
			this.DisableRedrawAndDo(() => {
				ForceMeasureItems(listBox, listBox_MeasureItem);
			});
			listBox.Invalidate();
		}

		[DllImport("user32.dll")]
		private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

		private const int LB_SETITEMHEIGHT = 0x01A0;

		private static void ForceMeasureItems(ListBox listBox, Action<object, MeasureItemEventArgs> onMeasureEvent)
		{
			for (int i = 0; i < listBox.Items.Count; i++)
			{
				MeasureItemEventArgs eArgs = new MeasureItemEventArgs(listBox.CreateGraphics(), i);
				onMeasureEvent(listBox, eArgs);
				SendMessage(listBox.Handle, LB_SETITEMHEIGHT, i, eArgs.ItemHeight);
			}
			listBox.Refresh();
		}
	}
}
