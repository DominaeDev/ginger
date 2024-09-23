using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;

namespace Ginger
{
	public class ComboBoxEx : ComboBox
	{
		public ComboBoxEx()
		{
			BorderColor = SystemColors.WindowFrame;
		}

		[Browsable(true)]
		[Category("Appearance")]
		[DefaultValue(typeof(Color), "WindowFrame")]
		public Color BorderColor { get; set; }

		private const int WM_PAINT = 0xF;
		private const int WM_MOUSEWHEEL = 0x20A;

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_PAINT)
			{
				base.WndProc(ref m);

				Color borderColor = Focused ? VisualTheme.Theme.Highlight : VisualTheme.Theme.TextBoxBorder;

				using (var g = Graphics.FromHwnd(Handle))
				{
					using (var p = new Pen(borderColor, 2))
					{
						g.DrawRectangle(p, 0, 0, Width, Height);
					}
				}
				return;
			}

			else if (m.Msg == WM_MOUSEWHEEL)
			{
				// Eat scroll wheel events, because casually scrolling down the recipe list 
				// with the mouse wheel can cause unwanted parameter changes otherwise.
				return;
			}

			base.WndProc(ref m);
		}
	}
}
