using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Ginger
{
	public static class ControlExtensions
	{
		// Windows APIs

		private static IntPtr _lockedControl = IntPtr.Zero;
		public static void Suspend(this Control control)
		{
			if (_lockedControl == IntPtr.Zero)
			{
				if (Win32.LockWindowUpdate(control.Handle))
					_lockedControl = control.Handle;
			}
		}

		public static void Resume(this Control control)
		{
			if (_lockedControl == IntPtr.Zero || _lockedControl == control.Handle)
			{
				Win32.LockWindowUpdate(IntPtr.Zero);
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
			Win32.SendMessage(handle, Win32.WM_SETREDRAW, 0, IntPtr.Zero);
			// Stop sending of events:  
			stateLocked = Win32.SendMessage(handle, Win32.EM_GETEVENTMASK, 0, IntPtr.Zero);
		}

		private static void Unlock(IntPtr handle, ref IntPtr stateLocked)
		{
			// turn on events  
			Win32.SendMessage(handle, Win32.EM_SETEVENTMASK, 0, stateLocked);
			// turn on redrawing  
			Win32.SendMessage(handle, Win32.WM_SETREDRAW, 1, IntPtr.Zero);
			stateLocked = IntPtr.Zero;
		}

		public static void SuspendRedraw(this Control control)
		{
			_SuspendRedraw(control.Handle);
		}

		public static void ResumeRedraw(this Control control)
		{
			_ResumeRedraw(control.Handle);
		}

		private static void _SuspendRedraw(IntPtr handle)
		{
			// Stop redrawing:  
			Win32.SendMessage(handle, Win32.WM_SETREDRAW, 0, IntPtr.Zero);
		}

		private static void _ResumeRedraw(IntPtr handle)
		{
			// turn on redrawing  
			Win32.SendMessage(handle, Win32.WM_SETREDRAW, 1, IntPtr.Zero);
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
					return Utility.FindWholeWord(textBox.Text, match, startIndex, matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
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

		public static void SetTabWidth(this TextBoxBase textBox, int tabWidth)
		{
			Win32.SendMessage(textBox.Handle, Win32.EM_SETTABSTOPS, 1, new int[] { tabWidth * 4 });
		}

		public static void KillFocus(this Control control)
		{
			Win32.SendMessage(control.Handle, Win32.WM_KILLFOCUS, 0, (IntPtr)null);
		}

		public static void HideHorizontalScrollbar(this Control control)
		{
			Win32.ShowScrollBar(control.Handle, (int)Win32.ScrollBarDirection.SB_HORZ, false); // Never draw horizontal scrollbar
		}

		public static void HideVerticalScrollbar(this Control control)
		{
			Win32.ShowScrollBar(control.Handle, (int)Win32.ScrollBarDirection.SB_VERT, false); // Never draw horizontal scrollbar
		}

		public static List<T> FindAllControlsOfType<T>(this Control parent) where T : Control
		{
			var controls = new List<T>();
			if (parent is T)
				controls.Add(parent as T);
			for (int i = 0; i < parent.Controls.Count; ++i)
				controls.AddRange(FindAllControlsOfType<T>(parent.Controls[i]));
			return controls;
		}

		public static List<T> FindAllControlsOfType<T>(this Form form) where T : Control
		{
			var controls = new List<T>();
			for (int i = 0; i < form.Controls.Count; ++i)
				controls.AddRange(FindAllControlsOfType<T>(form.Controls[i]));
			return controls;
		}


	}
}
