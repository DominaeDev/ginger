using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Ginger.Integration;

namespace Ginger
{
	public partial class ChatListBox : UserControl
	{
		public class Entry
		{
			public int characterIndex;
			public Color color;
			public string name;
			public string message;
			public string timestamp;
		}

		private static readonly int NameLeft = 4;
		private static readonly int NameTop = 2;
		private static readonly int MessageLeft = 4;
		private static readonly int MessageRight = 0;
		private static readonly int MessageTop = 19;
		private static readonly int MessageBottom = 6;
		private static readonly int MinRowHeight = 22;

		public static readonly Color[] NameColors_Light = new Color[] {
			ColorTranslator.FromHtml("#0185b6"),
			ColorTranslator.FromHtml("#b68e01"),
			ColorTranslator.FromHtml("#c837c7"),
			ColorTranslator.FromHtml("#76d244"),
			ColorTranslator.FromHtml("#2c3397"),
			ColorTranslator.FromHtml("#b62929"),
			ColorTranslator.FromHtml("#44d2af"),
			ColorTranslator.FromHtml("#f2aa39"),
		};

		public static readonly Color[] NameColors_Dark = new Color[] {
			ColorTranslator.FromHtml("#41a4c8"),
			ColorTranslator.FromHtml("#c8aa41"),
			ColorTranslator.FromHtml("#d669d5"),
			ColorTranslator.FromHtml("#98dd73"),
			ColorTranslator.FromHtml("#6166b1"),
			ColorTranslator.FromHtml("#c85f5f"),
			ColorTranslator.FromHtml("#73ddc3"),
			ColorTranslator.FromHtml("#f5bf6b"),
		};

		public ListBox.ObjectCollection Items { get { return listBox.Items; } }
		
		public class ContextMenuEventArgs : EventArgs
		{
			public int Index { get; set; }
			public Point Location { get; set; }
		}
		public event EventHandler<ContextMenuEventArgs> OnContextMenu;

		private Font _nameFont;
		private Font _timeFont;
		private static StringFormat RightAligned = new StringFormat() { Alignment = StringAlignment.Far };
		private Point _rightDownLocation;
		private bool _bRightDown = false;

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

			_nameFont = new Font(this.Font.FontFamily, 8.5F, FontStyle.Bold);
			_timeFont = new Font(this.Font.FontFamily, 8.5F, FontStyle.Regular);
		}

		private void listBox_DrawItem(object sender, DrawItemEventArgs e)
		{
			if (e.Index >= 0)
			{
				var entry = (Entry)listBox.Items[e.Index];

				bool selected = e.State.Contains(DrawItemState.Selected);
				
				e.DrawBackground();
				e.DrawFocusRectangle();

				e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

				// Name
				using (var brush = new SolidBrush(selected ? e.ForeColor : entry.color))
				{
					e.Graphics.DrawString(
						entry.name,
						_nameFont,
						brush,
						new Point(e.Bounds.Left + NameLeft, e.Bounds.Top + NameTop));
				}

				// Timestamp
				using (var brush = new SolidBrush(selected ? e.ForeColor : Theme.Current.GrayText))
				{
					e.Graphics.DrawString(
						entry.timestamp,
						_timeFont,
						brush,
						new Rectangle(e.Bounds.Left, e.Bounds.Top + NameTop, e.Bounds.Width - 2, e.Bounds.Height),
						RightAligned);
				}

				// Message
				using (var brush = new SolidBrush(e.ForeColor))
				{
					e.Graphics.DrawString(
						entry.message,
						e.Font,
						brush,
						new Rectangle(
							new Point(MessageLeft, e.Bounds.Top + MessageTop),
							new Size(listBox.ClientSize.Width - MessageLeft - MessageRight - SystemInformation.VerticalScrollBarWidth, 0))
						);
				}
			}
		}

		private void listBox_MeasureItem(object sender, MeasureItemEventArgs e)
		{
			if (e.Index >= 0)
			{
				var entry = (Entry)listBox.Items[e.Index];
				string text = entry.message;

				using (Graphics g = listBox.CreateGraphics())
				{
					SizeF size = g.MeasureString(text, this.Font, listBox.ClientSize.Width - MessageLeft - MessageRight - SystemInformation.VerticalScrollBarWidth);
					e.ItemHeight = Math.Max((int)Math.Ceiling(size.Height), MinRowHeight) + MessageTop + MessageBottom;
				}
			}
		}

		private void ChatListView_Resize(object sender, EventArgs e)
		{
			listBox.Location = new Point(1, 0);
			listBox.Size = new Size(this.ClientSize.Width - 1, this.ClientSize.Height);
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

		private void listBox_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				if (_bRightDown && (Math.Abs(e.X - _rightDownLocation.X) < 10 && Math.Abs(e.Y - _rightDownLocation.Y) < 10))
				{
					int index = HitTest(new Point(e.X, e.Y));
					if (index != -1 && listBox.GetSelected(index) == false)
						SelectLine(index);

					OnContextMenu?.Invoke(listBox, new ContextMenuEventArgs() {
						Index = index,
						Location = new Point(e.X, e.Y),
					});
				}
			}
		}

		private int HitTest(Point location)
		{
			if (Items.Count == 0)
				return -1;

			Rectangle lastRect = listBox.GetItemRectangle(Items.Count - 1);
			if (lastRect.Bottom < location.Y)
				return -1;
			return listBox.IndexFromPoint(location);
		}

		private void listBox_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				_rightDownLocation = new Point(e.X, e.Y);
				_bRightDown = true;
			}
		}

		private void listBox_MouseLeave(object sender, EventArgs e)
		{
			_bRightDown = false;
		}

		private void SelectLine(int index)
		{
			listBox.BeginUpdate();
			for (int i = 0; i < listBox.Items.Count; ++i)
				listBox.SetSelected(i, i == index);
			listBox.EndUpdate();
			listBox.Refresh();
		}

		public void ShowChat(bool bShow)
		{
			listBox.Visible = bShow;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == ShortcutKeys.Copy)
			{
				CopySelected();
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		public void CopySelected()
		{
			if (listBox.SelectedItems.Count == 0)
				return;

			var messages = new List<ChatClipboard.Message>();
			int lastIndex = -1;
			for (int i = 0; i < listBox.Items.Count; ++i)
			{
				if (listBox.GetSelected(i) == false)
					continue;
				if (lastIndex >= 0 && i - lastIndex > 1)
				{
					// Insert gap
					messages.Add(new ChatClipboard.Message());
				}
				lastIndex = i;
				var entry = (Entry)listBox.Items[i];
				messages.Add(new ChatClipboard.Message() {
					name = entry.name,
					characterIndex = entry.characterIndex,
					text = entry.message,
				});
			}

			ChatClipboard chatClipboard = ChatClipboard.FromMessages(messages);
			DataObject dataObject = new DataObject();
			dataObject.SetData(ChatClipboard.Format, chatClipboard);
			dataObject.SetData(DataFormats.UnicodeText, chatClipboard.rawText);
			Clipboard.SetDataObject(dataObject);
		}
	}
}
