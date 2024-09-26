using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public class CheckBoxEx : CheckBox
	{
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// Dark mode: Gray text
			if (Enabled == false && Theme.IsDarkModeEnabled)
			{
				Graphics g = e.Graphics;

				// Erase text
				Rectangle bounds = new Rectangle(e.ClipRectangle.Left + 16, e.ClipRectangle.Top, e.ClipRectangle.Width - 16, e.ClipRectangle.Height);
				using (var brush = new SolidBrush(this.BackColor))
				{
					g.FillRectangle(brush, bounds);
				}

				// Draw text
				g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
				using (var brush = new SolidBrush(Theme.Current.GrayText))
				{
					g.DrawString(this.Text, this.Font, brush, 17, 1);
				}
			}
		}
	}
}
