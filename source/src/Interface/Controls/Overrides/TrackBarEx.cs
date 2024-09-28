using System;
using System.Windows.Forms;

namespace Ginger
{
	public class TrackBarEx : TrackBar
	{
		private const int margin = 10;
		private bool isDragging = false;

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

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == Win32.WM_MOUSEWHEEL) // Override mouse wheel scroll to change slider
				return;
			else if (m.Msg == Win32.WM_HSCROLL_TRACK)
				return;
			else if (m.Msg == Win32.WM_VSCROLL_TRACK)
				return;
			else if (m.Msg == Win32.WM_LBUTTONDOWN)
			{
				int x = Win32.LoWord(m.LParam);
				Capture = true;
				SetTrackBar((double)(x - margin) / GetWidth());
				isDragging = true;
				return;
			}
			else if (m.Msg == Win32.WM_LBUTTONUP)
			{
				isDragging = false;
				base.WndProc(ref m);
				return;
			}
			else if (m.Msg == Win32.WM_MOUSEMOVE)
			{
				unchecked
				{
					bool bLeftDown = ((uint)m.WParam & 0x0001) == 0x0001;
					short x = (short)Win32.LoWord(m.LParam);
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
