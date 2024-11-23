using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Ginger
{
	public static class Win32
	{
		#region Constants
		public const int WM_LBUTTONDOWN = 0x0201;
		public const int WM_LBUTTONUP = 0x0202;
		public const int WM_LBUTTONDBLCLK = 0x0203;
		public const int WM_RBUTTONDOWN = 0x0204;
		public const int WM_RBUTTONUP = 0x0205;
		public const int WM_RBUTTONDBLCLK = 0x0206;
		
		public const int WM_PAINT = 0x0F;
		public const int WM_NCPAINT = 0x85;
		public const int WM_MOUSEWHEEL = 0x20A;
		public const int WM_MOUSEMOVE = 0x0200;
		public const int WM_MOUSELEAVE = 0x02A3;
		public const int WM_MOUSEHOVER = 0x02A1;
		public const int WM_ENABLE = 0x0A;

		public const int WM_SETCURSOR	= 0x20;
		public const int WM_HSCROLL = 0x114;
		public const int WM_VSCROLL = 0x115;
		public const int WM_HSCROLL_TRACK = 0x2114;
		public const int WM_VSCROLL_TRACK = 0x2115;
		public const int WM_SETREDRAW = 0x000B;
		public const int WM_USER = 0x400;
		public const int WM_SETFOCUS = 0x07;
		public const int WM_KILLFOCUS = 0x08;

		public const uint RDW_INVALIDATE = 0x01;
		public const uint RDW_IUPDATENOW = 0x100;
		public const uint RDW_FRAME = 0x400;

		public const int EM_GETEVENTMASK = (WM_USER + 59);
		public const int EM_SETEVENTMASK = (WM_USER + 69);
		public const int EM_GETSCROLLPOS = WM_USER + 221;
		public const int SB_PAGEBOTTOM = 7;
		public const int EM_GETPARAFORMAT = 1085;
		public const int EM_SETPARAFORMAT = 1095;
		public const int EM_SETTYPOGRAPHYOPTIONS = 1226;
		public const int EM_SETTABSTOPS = 0x00CB;
		public const int TO_ADVANCEDTYPOGRAPHY = 1;
		public const int PFM_ALIGNMENT = 0x8;
		public const int PFM_LINESPACING = 0x100;
		public const int SCF_DEFAULT = 0x0;
		public const int SCF_SELECTION = 0x1;
		public const int SB_LINEUP = 0;
		public const int SB_LINEDOWN = 1;
		public const int SB_THUMBPOSITION = 4;
		public const int SB_THUMBTRACK = 5;
		public const int SB_TOP = 6;
		public const int SB_BOTTOM = 7;
		public const int SB_ENDSCROLL = 8;
		#endregion

		#region Structs
		internal struct SCROLLINFO
		{
			public uint cbSize;
			public uint fMask;
			public int nMin;
			public int nMax;
			public uint nPage;
			public int nPos;
			public int nTrackPos;
		}

		internal enum ScrollBarDirection
		{
			SB_HORZ = 0,
			SB_VERT = 1,
			SB_CTL = 2,
			SB_BOTH = 3
		}

		internal enum ScrollInfoMask
		{
			SIF_RANGE = 0x1,
			SIF_PAGE = 0x2,
			SIF_POS = 0x4,
			SIF_DISABLENOSCROLL = 0x8,
			SIF_TRACKPOS = 0x10,
			SIF_ALL = SIF_RANGE + SIF_PAGE + SIF_POS + SIF_TRACKPOS
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct PARAFORMAT
		{
			public int cbSize;
			public uint dwMask;
			public short wNumbering;
			public short wReserved;
			public int dxStartIndent;
			public int dxRightIndent;
			public int dxOffset;
			public short wAlignment;
			public short cTabCount;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public int[] rgxTabs;

			// PARAFORMAT2 from here onwards.
			public int dySpaceBefore;
			public int dySpaceAfter;
			public int dyLineSpacing;
			public short sStyle;
			public byte bLineSpacingRule;
			public byte bOutlineLevel;
			public short wShadingWeight;
			public short wShadingStyle;
			public short wNumberingStart;
			public short wNumberingStyle;
			public short wNumberingTab;
			public short wBorderSpace;
			public short wBorderWidth;
			public short wBorders;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct RECT
		{
			public readonly int Left;
			public readonly int Top;
			public readonly int Right;
			public readonly int Bottom;

			private RECT(int left, int top, int right, int bottom)
			{
				Left = left;
				Top = top;
				Right = right;
				Bottom = bottom;
			}

			public RECT(Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom)
			{
			}

			public Rectangle ToRectangle()
			{
				return new Rectangle(this.Left, this.Top, this.Right - this.Left, this.Bottom - this.Top);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct PAINTSTRUCT
		{
			public IntPtr hdc;
			public bool fErase;
			public RECT rcPaint;
			public bool fRestore;
			public bool fIncUpdate;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] rgbReserved;
		}
		#endregion

		#region DLL Imports
		[DllImport("user32.dll")]
		internal extern static int SendMessage(HandleRef hWnd, int msg, int wParam, int lParam);

		[DllImport("user32.dll")]
		internal extern static int SendMessage(HandleRef hWnd, int msg, int wParam, ref PARAFORMAT lp);

		[DllImport("user32.dll")]
		internal extern static Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

		[DllImport("user32.dll")]
		internal extern static int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		internal extern static IntPtr SendMessage(IntPtr hWnd, Int32 wMsg, Int32 wParam, ref Point lParam);

		[DllImport("user32.dll")]
		internal extern static IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

		[DllImport("User32.dll")]
		internal extern static IntPtr SendMessage(IntPtr handle, int msg, int wParam, int[] lParam);

		[DllImport("user32.dll")]
		internal extern static int SetCursor(IntPtr cursor);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal extern static bool GetScrollInfo(IntPtr hwnd, int fnBar, ref SCROLLINFO lpsi);

		[DllImport("user32.dll")]
		internal extern static int SetScrollInfo(IntPtr hwnd, int fnBar, [In] ref SCROLLINFO lpsi, bool fRedraw);

		[DllImport("user32.dll")]
		internal extern static IntPtr GetWindowDC(IntPtr hWnd);

		[DllImport("user32.dll")]
		internal extern static int ReleaseDC(IntPtr hWnd, IntPtr hDC);

		[DllImport("user32.dll")]
		internal extern static bool RedrawWindow(IntPtr hWnd, IntPtr lprc, IntPtr hrgn, uint flags);
		
		[DllImport("user32.dll")]
		internal extern static bool LockWindowUpdate(IntPtr hWndLock);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal extern static bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

		[DllImport("user32.dll")]
		internal extern static IntPtr BeginPaint(IntPtr hwnd, out PAINTSTRUCT lpPaint);

		[DllImport("user32.dll")]
		internal extern static bool EndPaint(IntPtr hWnd, [In] ref PAINTSTRUCT lpPaint);
		#endregion

		#region Methods

		public static bool GetVerticalScrollbarPosition(Control control, out int position)
		{
			int max;
			return GetVerticalScrollbarPosition(control, out position, out max);
		}

		public static bool GetVerticalScrollbarPosition(Control control, out int position, out int max)
		{
			IntPtr handle = control.Handle;
			SCROLLINFO si = new SCROLLINFO();
			si.cbSize = (uint)Marshal.SizeOf(si);
			si.fMask = (uint)ScrollInfoMask.SIF_ALL;
			if (GetScrollInfo(handle, (int)ScrollBarDirection.SB_VERT, ref si))
			{
				position = (int)si.nPage + si.nPos;
				max = si.nMax;
				return true;
			}
			else
			{
				position = default(int);
				max = default(int);
				return false;
			}
		}

		public static void ScrollToBottom(TextBoxBase textBox)
		{
			SendMessage(textBox.Handle, WM_VSCROLL, (IntPtr)SB_PAGEBOTTOM, IntPtr.Zero);
		}

		public static void SetLineHeight(TextBoxBase textBox, float lineHeight)
		{
			var fmt = new PARAFORMAT();
			fmt.cbSize = Marshal.SizeOf(fmt);
			fmt.dwMask = PFM_LINESPACING;
			fmt.dyLineSpacing = Convert.ToInt32(Math.Max(lineHeight * 240, 0));
			fmt.bLineSpacingRule = 0x3;
			SendMessage(new HandleRef(textBox, textBox.Handle), EM_SETPARAFORMAT, SCF_DEFAULT, ref fmt);
		}

		public static float GetLineHeight(TextBoxBase textBox)
		{
			var fmt = new PARAFORMAT();
			fmt.cbSize = Marshal.SizeOf(fmt);
			SendMessage(new HandleRef(textBox, textBox.Handle), EM_GETPARAFORMAT, SCF_DEFAULT, ref fmt);
			return fmt.dyLineSpacing / 240.0f;
		}

		public static int LoWord(IntPtr dWord)
		{
			return dWord.ToInt32() & 0xffff;
		}

		public static int HiWord(IntPtr dWord)
		{
			if ((dWord.ToInt32() & 0x80000000) == 0x80000000)
				return dWord.ToInt32() >> 16;
			else
				return (dWord.ToInt32() >> 16) & 0xffff;
		}
		#endregion
	}
}
