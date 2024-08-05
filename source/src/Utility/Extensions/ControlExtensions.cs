using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Ginger
{
	public static class ControlExtensions
	{
		// Windows APIs
		[DllImport("user32.dll")]
		public static extern bool LockWindowUpdate(IntPtr hWndLock);
		[DllImport("user32.dll")]
		private extern static IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

		private enum ScrollBarDirection
		{
			SB_HORZ = 0,
			SB_VERT = 1,
			SB_CTL = 2,
			SB_BOTH = 3
		}

		private const int WM_SETREDRAW = 0x000B;
		private const int WM_USER = 0x400;
		private const int EM_GETEVENTMASK = (WM_USER + 59);
		private const int EM_SETEVENTMASK = (WM_USER + 69);
		private const int EM_GETSCROLLPOS = WM_USER + 221;

		private static IntPtr _lockedControl = IntPtr.Zero;
		public static void Suspend(this Control control)
		{
			if (_lockedControl == IntPtr.Zero)
			{
				if (LockWindowUpdate(control.Handle))
					_lockedControl = control.Handle;
			}
		}

		public static void Resume(this Control control)
		{
			if (_lockedControl == IntPtr.Zero || _lockedControl == control.Handle)
			{
				if (LockWindowUpdate(IntPtr.Zero))
					_lockedControl = IntPtr.Zero;
			}
		}

		public static void DisableRedrawAndDo(this Control control, Action action)
		{
			IntPtr stateLocked = IntPtr.Zero;
			Lock(control.Handle, ref stateLocked);
			action();
			Unlock(control.Handle, ref stateLocked);
			control.Invalidate();
		}

		private static void Lock(IntPtr handle, ref IntPtr stateLocked)
		{
			// Stop redrawing:  
			SendMessage(handle, WM_SETREDRAW, 0, IntPtr.Zero);
			// Stop sending of events:  
			stateLocked = SendMessage(handle, EM_GETEVENTMASK, 0, IntPtr.Zero);
			// change colors and stuff in the RichTextBox 
		}

		private static void Unlock(IntPtr handle, ref IntPtr stateLocked)
		{
			// turn on events  
			SendMessage(handle, EM_SETEVENTMASK, 0, stateLocked);
			// turn on redrawing  
			SendMessage(handle, WM_SETREDRAW, 1, IntPtr.Zero);
			stateLocked = IntPtr.Zero;
		}

		public static int Find(this TextBoxBase textBox, string match, bool matchCase, bool matchWord, bool reverse, int startIndex = -1)
		{
			if (reverse == false)
			{
				if (startIndex >= 0)
					startIndex = Math.Min(startIndex, textBox.Text.Length);
				else
					startIndex = 0;

				if (matchWord)
					return Utility.FindWholeWord(textBox.Text, match, startIndex, !matchCase);
				else
					return textBox.Text.IndexOf(match, startIndex, matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
			}
			else
			{
				if (startIndex >= 0)
					startIndex = Math.Min(startIndex, textBox.Text.Length);

				if (matchWord)
					return Utility.FindWholeWordReverse(textBox.Text, match, startIndex, !matchCase);
				else
					return textBox.Text.IndexOfReverse(match, startIndex, !matchCase);
			}
		}


		// set tab stops to a width of 4
		private const int EM_SETTABSTOPS = 0x00CB;

		[DllImport("User32.dll")]
		public static extern IntPtr SendMessage(IntPtr handle, int msg, int wParam, int[] lParam);

		public static void SetTabWidth(this TextBoxBase textBox, int tabWidth)
		{
			SendMessage(textBox.Handle, EM_SETTABSTOPS, 1, new int[] { tabWidth * 4 });
		}


		const int WM_KILLFOCUS = 0x0008;

		public static void KillFocus(this Control control)
		{
			SendMessage(control.Handle, WM_KILLFOCUS, 0, null);
		}

		public static void HideHorizontalScrollbar(this Control control)
		{
			ShowScrollBar(control.Handle, (int)ScrollBarDirection.SB_HORZ, false); // Never draw horizontal scrollbar
		}

		public static void HideVerticalScrollbar(this Control control)
		{
			ShowScrollBar(control.Handle, (int)ScrollBarDirection.SB_VERT, false); // Never draw horizontal scrollbar
		}
	}
}
