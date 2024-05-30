using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Inner margin code by Uwe Keim
// https://stackoverflow.com/questions/2914004/rich-text-box-padding-between-text-and-border

namespace Ginger
{
	public static class RichTextBoxExtensions
	{
		public static void SetInnerMargins(this TextBoxBase textBox, int left, int top, int right, int bottom)
		{
			var newRect = new Rectangle(left, top, textBox.Width - left - right, textBox.Height - top - bottom);
			textBox.SetFormattingRect(newRect);
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
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
		}

		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		private static extern int SendMessageRefRect(IntPtr hWnd, uint msg, int wParam, ref RECT rect);

		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		private static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, ref Rectangle lParam);

		private const int EM_GETRECT = 0xB2;
		private const int EM_SETRECT = 0xB3;

		private static void SetFormattingRect(this TextBoxBase textbox, Rectangle rect)
		{
			var rc = new RECT(rect);
			SendMessageRefRect(textbox.Handle, EM_SETRECT, 0, ref rc);
		}

		private static Rectangle GetFormattingRect(this TextBoxBase textbox)
		{
			var rect = new Rectangle();
			SendMessage(textbox.Handle, EM_GETRECT, (IntPtr)0, ref rect);
			return rect;
		}
	}
}
