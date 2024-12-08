using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public class ButtonEx : Button
	{
		[Browsable(true)]
		public bool Highlighted
		{
			get { return _bHighlighted; }
			set { _bHighlighted = value; Invalidate(); }
		}
		private bool _bHighlighted = false;

		private bool _bHover = false;

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			_bHover = true;
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseEnter(e);
			_bHover = false;
		}

		protected override void OnMouseHover(EventArgs e)
		{
			base.OnMouseHover(e);
			_bHover = true;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;

			bool bPressed = this.Capture;
			bool bFocused = this.ShowFocusCues && this.ContainsFocus;

			// Background
			Color background = Theme.Current.Button;
			if (Enabled == false)
				background = Theme.Current.ButtonDisabled;
			else if (bPressed)
				background = Theme.Current.ButtonPressed;
			else if (_bHover)
				background = Theme.Current.ButtonHover;
			g.Clear(background);

			Rectangle bounds = new Rectangle(e.ClipRectangle.Left, e.ClipRectangle.Top, e.ClipRectangle.Width - 1, e.ClipRectangle.Height - 1);
			SizeF strSize = g.MeasureString(this.Text, this.Font);

			// Draw text
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
			using (var brush = new SolidBrush(Enabled ? this.ForeColor : Theme.Current.ButtonDisabledText))
			{
				g.DrawString(this.Text, this.Font, brush, 
					this.Padding.Left + (e.ClipRectangle.Width - (int)strSize.Width) / 2, 
					this.Padding.Top + (e.ClipRectangle.Height - (int)strSize.Height) / 2);
			}

			// Draw border
			using (var pen = new Pen(Enabled ? Theme.Current.ButtonBorder : Theme.Current.ButtonDisabledBorder))
			{
				g.DrawRectangle(pen, bounds);
				if (bPressed || bFocused) // Thick border
					g.DrawRectangle(pen, new Rectangle(bounds.Left + 1, bounds.Top + 1, bounds.Width - 2, bounds.Height - 2));
			}

			if (Enabled && Highlighted)
			{
				using (var pen = new Pen(Theme.Current.HighlightBorder, 1))
				{
					g.DrawRectangle(pen, new Rectangle(bounds.Left + 2, bounds.Top + 2, bounds.Width - 4, bounds.Height - 4));
				}
			}
			
			// Draw focus
			if (bFocused)
			{
				using (var pen = new Pen(Theme.Current.ControlForeground))
				{
					g.DrawRectangle(pen, new Rectangle(bounds.Left + 4, bounds.Top + 4, bounds.Width - 8, bounds.Height - 8));
				}
			}
		}
	}
}
