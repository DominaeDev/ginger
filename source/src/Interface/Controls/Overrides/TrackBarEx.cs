using System;
using System.Windows.Forms;

namespace Ginger
{
	public class TrackBarEx : TrackBar
	{
		private const int WM_MOUSEWHEEL = 0x20A;
		private const int WM_HSCROLL = 0x2114;
		private const int WM_VSCROLL = 0x2115;
		private const int WM_MOUSEMOVE = 0x0200;
		private const int WM_LBUTTONDOWN = 0x0201;
		private const int WM_LBUTTONUP = 0x0202;

		private const int margin = 10;
		private bool isDragging = false;

		public TrackBarEx()
		{
		}

		private void SetTrackBar(double p)
		{
			int newValue = this.Minimum + (int)(p * (this.Maximum - this.Minimum + 1));
			this.Value = Math.Min(Math.Max(newValue, this.Minimum), this.Maximum);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			isDragging = false;
			base.OnMouseUp(e);
		}

		protected override void OnMouseCaptureChanged(EventArgs e)
		{
			if (this.Capture == false)
				isDragging = false;
			base.OnMouseCaptureChanged(e);
		}

		private int GetWidth()
		{
			return Math.Max(this.Width - margin * 2, 1);
		}

		private static int LoWord(IntPtr dWord)
		{
			return dWord.ToInt32() & 0xffff;
		}

		private static int HiWord(IntPtr dWord)
		{
			if ((dWord.ToInt32() & 0x80000000) == 0x80000000)
				return dWord.ToInt32() >> 16;
			else
				return (dWord.ToInt32() >> 16) & 0xffff;
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_MOUSEWHEEL) // Override mouse wheel scroll to change slider
				return;
			else if (m.Msg == WM_HSCROLL)
				return;
			else if (m.Msg == WM_VSCROLL)
				return;
			else if (m.Msg == WM_LBUTTONDOWN)
			{
				int x = LoWord(m.LParam);
				Capture = true;
				SetTrackBar((double)(x - margin) / GetWidth());
				isDragging = true;
				return;
			}
			else if (m.Msg == WM_LBUTTONUP)
			{
				isDragging = false;
				base.WndProc(ref m);
				return;
			}
			else if (m.Msg == WM_MOUSEMOVE)
			{
				unchecked
				{
					bool bLeftDown = ((uint)m.WParam & 0x0001) == 0x0001;
					short x = (short)LoWord(m.LParam);
					if (x == (short)0xFFFF) // Is negative
						x = 0;
					if (bLeftDown && isDragging)
						SetTrackBar((double)(x - margin) / GetWidth());
					return;
				}
			}
			base.WndProc(ref m);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Next)
			{
				return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		protected override void OnScroll(EventArgs e)
		{
			return; // base.OnScroll(e);
		}
	}
}
