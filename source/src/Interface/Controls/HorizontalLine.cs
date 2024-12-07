using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public partial class HorizontalLine : UserControl
	{
		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;

			// Draw line
			using (var Pen = new Pen(Theme.Current.MenuSeparator, 1))
			{
				g.DrawLine(Pen, new Point(0, Height / 2), new Point(Width - 1, Height / 2));
			}
		}
	}
}