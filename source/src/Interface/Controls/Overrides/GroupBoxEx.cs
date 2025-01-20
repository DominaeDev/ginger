using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public class GroupBoxEx : GroupBox
	{
		[Browsable(true)]
		public bool Collapsible { get; set; }

		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool Collapsed { get; set; }

		public int CollapsedHeight = 22;
		private int _height;

		public GroupBoxEx()
		{
			MouseClick += GroupBoxEx_MouseClick;
			MouseDoubleClick += GroupBoxEx_MouseClick;
		}

		private void GroupBoxEx_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (Collapsible && e.Y <= CollapsedHeight)
					SetCollapsed(!Collapsed);
			}
		}

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
				const int iconWidth = 8;
				Size textSize = g.MeasureString(this.Text, this.Font).ToSize();
				textSize.Width += iconWidth + 2;

				int textHalfHeight = textSize.Height / 2;
				Rectangle rect = this.ClientRectangle;

				rect = new Rectangle(rect.X, rect.Y + textHalfHeight, rect.Width - 1, rect.Height - textHalfHeight - 1);

				if (!Collapsed)
				{
					// Left border
					g.DrawLine(borderPen, rect.Location, new Point(rect.X, rect.Y + rect.Height));
					// Right border
					g.DrawLine(borderPen, new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height));
					// Bottom border
					g.DrawLine(borderPen, new Point(rect.X, rect.Y + rect.Height), new Point(rect.X + rect.Width, rect.Y + rect.Height));
					// Top left border
					g.DrawLine(borderPen, new Point(rect.X, rect.Y), new Point(rect.X + this.Padding.Left - 4, rect.Y));
					// Top right border
					g.DrawLine(borderPen, new Point(rect.X + this.Padding.Left + textSize.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y));
				}
				else
				{
					using (Pen stipplePen = new Pen(Theme.Current.GroupBoxBorder, 1))
					{
						stipplePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
						g.DrawLine(borderPen, new Point(rect.X, rect.Y - 1), new Point(rect.X + this.Padding.Left - 4, rect.Y - 1));
						g.DrawLine(borderPen, new Point(rect.X, rect.Y + 1), new Point(rect.X + this.Padding.Left - 4, rect.Y + 1));

						g.DrawLine(stipplePen, new Point(rect.X + this.Padding.Left + textSize.Width, rect.Y - 1), new Point(rect.X + rect.Width, rect.Y - 1));
						g.DrawLine(stipplePen, new Point(rect.X + this.Padding.Left + textSize.Width, rect.Y + 1), new Point(rect.X + rect.Width, rect.Y + 1));
					}
				}

				// Draw text
				g.DrawString(this.Text, this.Font, textBrush, this.Padding.Left + iconWidth, 0);

				// Draw arrow
				if (Collapsed)
					g.DrawImageUnscaled(Theme.Current.Collapsed, -5, -6);
				else
					g.DrawImageUnscaled(Theme.Current.Expanded, -4, -7);

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

		public bool Collapse()
		{
			SetCollapsed(true);
			return true;
		}

		public bool Expand()
		{
			SetCollapsed(false);
			return true;
		}

		private void SetCollapsed(bool bCollapsed)
		{
			if (Collapsible == false)
				return;

			this.Collapsed = bCollapsed;
			TabStop = !bCollapsed;

			if (bCollapsed)
			{
				foreach (Control childControl in this.Controls)
					childControl.Visible = false;
				
				_height = this.Size.Height;
				this.Size = new Size(this.Size.Width, CollapsedHeight);
			}
			else
			{
				foreach (Control childControl in this.Controls)
					childControl.Visible = true;
				this.Size = new Size(this.Size.Width, _height);
			}
						
		}
	}
}
