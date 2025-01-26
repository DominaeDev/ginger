using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public class GroupBoxEx : GroupBox
	{
		[Browsable(true)]
		public int BottomMargin { get; set; }

		protected override void OnPaint(PaintEventArgs e)
		{
			Brush textBrush = new SolidBrush(Theme.Current.ControlForeground);
			Brush borderBrush = new SolidBrush(Theme.Current.GroupBoxBorder);
			Pen borderPen = new Pen(borderBrush);
			Graphics g = e.Graphics;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

			// Clear text and border
			g.Clear(this.BackColor);

			if (string.IsNullOrEmpty(this.Text) == false)
			{
				SizeF strSize = g.MeasureString(this.Text, this.Font);
				Rectangle rect = new Rectangle(this.ClientRectangle.X,
					this.ClientRectangle.Y + (int)(strSize.Height / 2),
					this.ClientRectangle.Width - 1,
					this.ClientRectangle.Height - (int)(strSize.Height / 2) - BottomMargin - 1);

				// Draw text
				g.DrawString(this.Text, this.Font, textBrush, this.Padding.Left, 0);

				// Drawing Border
				//Left
				g.DrawLine(borderPen, rect.Location, new Point(rect.X, rect.Y + rect.Height));
				//Right
				g.DrawLine(borderPen, new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height));
				//Bottom
				g.DrawLine(borderPen, new Point(rect.X, rect.Y + rect.Height), new Point(rect.X + rect.Width, rect.Y + rect.Height));
				//Top1
				g.DrawLine(borderPen, new Point(rect.X, rect.Y), new Point(rect.X + this.Padding.Left, rect.Y));
				//Top2
				g.DrawLine(borderPen, new Point(rect.X + this.Padding.Left + (int)(strSize.Width), rect.Y), new Point(rect.X + rect.Width, rect.Y));
			}
			else
			{
				SizeF strSize = g.MeasureString(" ", this.Font);
				Rectangle rect = new Rectangle(this.ClientRectangle.X,
					this.ClientRectangle.Y + (int)(strSize.Height / 2),
					this.ClientRectangle.Width - 1,
					this.ClientRectangle.Height - (int)(strSize.Height / 2) - BottomMargin - 1);
				g.DrawRectangle(borderPen, rect);
			}

			textBrush.Dispose();
			borderBrush.Dispose();
			borderPen.Dispose();
		}
	}
}
