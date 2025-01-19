using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public class GroupBoxEx : GroupBox
	{
		public bool Collapsed { get; set; }

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
				Size textSize = g.MeasureString(this.Text, this.Font).ToSize();
				int textHalfHeight = textSize.Height / 2;
				Rectangle rect = this.ClientRectangle;
				const int iconWidth = 7;

				rect = new Rectangle(rect.X, rect.Y + textHalfHeight, rect.Width - 1, rect.Height - textHalfHeight - 1);

				// Left border
				g.DrawLine(borderPen, rect.Location, new Point(rect.X, rect.Y + rect.Height));
				// Right border
				g.DrawLine(borderPen, new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height));
				// Bottom border
				g.DrawLine(borderPen, new Point(rect.X, rect.Y + rect.Height), new Point(rect.X + rect.Width, rect.Y + rect.Height));
				// Top left border
				g.DrawLine(borderPen, new Point(rect.X, rect.Y), new Point(rect.X + this.Padding.Left - 4, rect.Y));
				// Top right border
				g.DrawLine(borderPen, new Point(rect.X + this.Padding.Left + textSize.Width + iconWidth, rect.Y), new Point(rect.X + rect.Width, rect.Y));

				// Draw text
				g.DrawString(this.Text, this.Font, textBrush, this.Padding.Left + iconWidth, 0);

				// Draw arrow
				if (Collapsed)
					g.DrawImageUnscaled(Theme.Current.Collapsed, -5, -6);
				else
					g.DrawImageUnscaled(Theme.Current.Expanded, -5, -7);

			}
			else
			{
				SizeF strSize = g.MeasureString(" ", this.Font);
				Rectangle rect = new Rectangle(this.ClientRectangle.X,
					this.ClientRectangle.Y + (int)(strSize.Height / 2),
					this.ClientRectangle.Width - 1,
					this.ClientRectangle.Height - (int)(strSize.Height / 2) - 1);
				g.DrawRectangle(borderPen, rect);
			}

			textBrush.Dispose();
			borderBrush.Dispose();
			borderPen.Dispose();
		}
	}
}
