using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger.src.Interface.Controls
{
	public partial class ChatListBox : UserControl
	{
		public ChatListBox()
		{
			InitializeComponent();

			this.FontChanged += ChatListBox_FontChanged;

			listBox.BeginUpdate();
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.Items.Add("Hahahaha");
			listBox.Items.Add("Hur hur hur");
			listBox.EndUpdate();
		}

		private void ChatListBox_FontChanged(object sender, EventArgs e)
		{
			listBox.Font = this.Font;
		}

		private void listBox_DrawItem(object sender, DrawItemEventArgs e)
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

		private void listBox_MeasureItem(object sender, MeasureItemEventArgs e)
		{
			e.ItemHeight = 30;
		}

		private void ChatListView_Resize(object sender, EventArgs e)
		{
			listBox.Location = new Point(0, 0);
			listBox.Size = new Size(this.Width, this.Height);
		}
	}
}
