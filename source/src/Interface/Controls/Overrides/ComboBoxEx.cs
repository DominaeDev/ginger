using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System;

namespace Ginger
{
	public class ComboBoxEx : ComboBox
	{
		[Browsable(true)]
		[Category("Appearance")]
		[DefaultValue(typeof(Color), "WindowFrame")]
		public Color BorderColor { get; set; }

		public ComboBoxEx()
		{
			BorderColor = SystemColors.WindowFrame;
			DropDownStyle = ComboBoxStyle.DropDownList;
			DoubleBuffered = false;
		}

		private const int WM_PAINT = 0xF;
		private const int WM_NCPAINT = 0x85;
		private const int WM_MOUSEWHEEL = 0x20A;
		public const int WM_MOUSEFIRST = 0x0200;
        public const int WM_MOUSELEAVE = 0x02A3;
        public const int WM_MOUSEHOVER = 0x02A1;
		private const int WM_ENABLE     = 0x0A;

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_PAINT)
			{
				base.WndProc(ref m);
				DrawBorder();
				return;
			}
			else if (m.Msg == WM_NCPAINT)
			{
				base.WndProc(ref m);

				Color borderColor = (Focused ? Theme.Current.Highlight : Theme.Current.TextBoxBorder);

				using (var g = Graphics.FromHwnd(Handle))
				{
					using (var p = new Pen(borderColor, 2))
					{
						g.DrawRectangle(p, 0, 0, Width, Height);
					}
				}
			}
			else if (m.Msg == WM_MOUSEWHEEL)
			{
				// Eat scroll wheel events, because casually scrolling down the recipe list 
				// with the mouse wheel can cause unwanted parameter changes otherwise.
				return;
			}
			else if (m.Msg == WM_ENABLE)
			{
				return;
			}

			base.WndProc(ref m);
		}

		protected override void OnEnabledChanged(EventArgs e)
		{
			base.OnEnabledChanged(e);

			this.ForeColor = Enabled ? Theme.Current.TextBoxForeground : SystemColors.GrayText;
			this.BackColor = Enabled ? Theme.Current.TextBoxBackground : Theme.Current.TextBoxDisabledBackground;
		}
		
		private void DrawBorder()
		{
			Color borderColor = Focused ? Theme.Current.Highlight : Theme.Current.TextBoxBorder;

			using (var g = Graphics.FromHwnd(Handle))
			{
				using (var p = new Pen(borderColor, 2))
				{
					g.DrawRectangle(p, 0, 0, Width, Height);
				}

				if (Enabled == false)
				{
					// Cover inner gray rim
					using (var p = new Pen(this.BackColor, 2))
					{
						g.DrawRectangle(p, 2, 2, Width - 21, Height - 4);
					}
				}
			}
		}
	}
}
