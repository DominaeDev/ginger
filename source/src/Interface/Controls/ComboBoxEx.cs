using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Ginger
{
	public class ComboBoxEx : ComboBox
	{
		[Browsable(true)]
		[Category("Appearance")]
		[DefaultValue(typeof(Color), "WindowFrame")]
		public Color BorderColor { get; set; }

		private const int WM_PAINT = 0xF;
		private const int WM_NCPAINT = 0x85;
		private const int WM_MOUSEWHEEL = 0x20A;

		public ComboBoxEx()
		{
			BorderColor = SystemColors.WindowFrame;
			DropDownStyle = ComboBoxStyle.DropDownList;
			DoubleBuffered = false;
		}

		public const int WM_MOUSEFIRST = 0x0200;
        public const int WM_MOUSELEAVE = 0x02A3;
        public const int WM_MOUSEHOVER = 0x02A1;

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_PAINT)
			{
				if (Enabled == false && VisualTheme.DarkModeEnabled)
				{
					base.WndProc(ref m);
					DrawDisabled();
					return;
				}
				else
				{
					base.WndProc(ref m);
					DrawBorder();
				}
				return;
			}
			else if (m.Msg == WM_NCPAINT)
			{
				base.WndProc(ref m);

				Color borderColor = (Focused ? VisualTheme.Theme.MenuHighlight : VisualTheme.Theme.TextBoxBorder);

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

			base.WndProc(ref m);
		}

		protected override void OnEnabledChanged(EventArgs e)
		{
			this.ForeColor = Enabled ? VisualTheme.Theme.TextBoxForeground : SystemColors.GrayText;
			this.BackColor = Enabled ? VisualTheme.Theme.TextBoxBackground : SystemColors.Control;
			base.OnEnabledChanged(e);
		}
		
		private void DrawBorder()
		{
			Color borderColor = Enabled ?
				(Focused ? VisualTheme.Theme.MenuHighlight : VisualTheme.Theme.TextBoxBorder)
				: VisualTheme.Theme.TextBoxDisabledBorder;

			using (var g = Graphics.FromHwnd(Handle))
			{
				using (var p = new Pen(borderColor, 2))
				{
					g.DrawRectangle(p, 0, 0, Width, Height);
				}
			}
		}

		private void DrawDisabled()
		{
			using (var g = Graphics.FromHwnd(Handle))
			{
				// Background
				using (var brush = new SolidBrush(VisualTheme.Theme.TextBoxDisabledBackground))
				{
					g.FillRectangle(brush, new Rectangle(0, 0, Width, Height));
				}

				// Text
				using (var brush = new SolidBrush(VisualTheme.Theme.GrayText))
				{
					g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

					g.DrawString(
						this.Text,
						this.Font,
						brush,
						new Point(2, 4));
				}

				// Border
				using (var pen = new Pen(VisualTheme.Theme.TextBoxDisabledBorder, 2))
				{
					g.DrawRectangle(pen, 0, 0, Width, Height);
				}

				// Inner rim
				using (var pen = new Pen(VisualTheme.Theme.TextBoxDisabledBackground))
				{
					g.DrawRectangle(pen, new Rectangle(1, 1, Width - 20, Height - 3));
				}

				// Combo box button
				ComboBoxRenderer.DrawDropDownButton(g, new Rectangle(Width - 18, 1, 17, Height - 2), System.Windows.Forms.VisualStyles.ComboBoxState.Disabled);
			}
		}
		
	}
}
