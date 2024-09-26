using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public class ChatListView : ListView
	{
		public class ContextMenuEventArgs : EventArgs
		{
			public int Index { get; set; }
			public Point Location { get; set; }
		}
		public event EventHandler<ContextMenuEventArgs> OnContextMenu;

		private Point _rightDownLocation;
		private bool _bRightDown = false;

		private static readonly int LeftMargin = 19;
		private static readonly int RightMargin = 2;

		public ChatListView()
		{
			OwnerDraw = true;
			DoubleBuffered = true;
			SetStyle(ControlStyles.Opaque | ControlStyles.OptimizedDoubleBuffer, true);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				_rightDownLocation = new Point(e.X, e.Y);
				_bRightDown = true;
			}

			base.OnMouseClick(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				if (_bRightDown && (Math.Abs(e.X - _rightDownLocation.X) < 10 && Math.Abs(e.Y - _rightDownLocation.Y) < 10))
				{
					ListViewHitTestInfo hitTest = HitTest(e.Location);
					if (hitTest.Item != null)
					{
						OnContextMenu?.Invoke(this, new ContextMenuEventArgs() {
							Index = hitTest.Item.Index,
							Location = new Point(e.X, e.Y),
						});
					}
					else
					{
						OnContextMenu?.Invoke(this, new ContextMenuEventArgs() {
							Index = -1,
							Location = new Point(e.X, e.Y),
						});
					}
					return;
				}
			}
			base.OnMouseUp(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			_bRightDown = false;
			base.OnMouseLeave(e);
		}

		protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
		{
			if (e.ColumnIndex != 0)
				return;

			bool bSelected = e.Item.Selected;
			bool bFocused  = e.Item.Focused;

			// Draw icon
			if (e.Item.ImageIndex != -1)
				e.Graphics.DrawImage(e.Item.ImageList.Images[e.Item.ImageIndex], e.Bounds.Left + 3, e.Bounds.Top + 1);

			var item = e.Item;

			e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

			for (int i = 0; i < item.SubItems.Count; ++i)
			{
				ListViewItem.ListViewSubItem subItem = item.SubItems[i];
				TextFormatFlags tf = TextFormatFlags.WordEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.ModifyString | TextFormatFlags.VerticalCenter;
				switch (this.Columns[i].TextAlign)
				{
				case HorizontalAlignment.Center:
					tf |= TextFormatFlags.HorizontalCenter;
					break;
				case HorizontalAlignment.Right:
					tf |= TextFormatFlags.Right;
					break;
				}

				Rectangle bounds = subItem.Bounds;
				if (i == 0)
					bounds = new Rectangle(bounds.Left + LeftMargin, bounds.Top, Columns[0].Width - LeftMargin, bounds.Height);

				// Background
				using (var brush = new SolidBrush(bSelected ? Theme.Current.MenuHighlight : BackColor))
				{
					e.Graphics.FillRectangle(brush, bounds);
				}

				// Text
				if (i == 1)
					bounds = new Rectangle(bounds.Left, bounds.Top, bounds.Width - RightMargin, bounds.Height);
				TextRenderer.DrawText(e.Graphics, subItem.Text, this.Font, bounds, bSelected ? Color.White : ForeColor, tf);
			}

			// Draw focus rectangle
			if (bFocused)
			{
				Rectangle rowBounds = new Rectangle(e.Bounds.Left, e.Bounds.Top, this.ClientSize.Width, e.Bounds.Height);

				ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(
					rowBounds.Left + LeftMargin,
					rowBounds.Top,
					rowBounds.Width - LeftMargin,
					rowBounds.Height
					), Color.White, bSelected ? Theme.Current.MenuHighlight : BackColor);
			}

		}

		protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
		{
			e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

			using (StringFormat sf = new StringFormat())
			{
				// Store the column text alignment, letting it default
				// to Left if it has not been set to Center or Right.
				switch (e.Header.TextAlign)
				{
				case HorizontalAlignment.Center:
					sf.Alignment = StringAlignment.Center;
					break;
				case HorizontalAlignment.Right:
					sf.Alignment = StringAlignment.Far;
					break;
				}

				// Background
				using (var brush = new SolidBrush(Theme.Current.MenuBackground))
				{
					e.Graphics.FillRectangle(brush, e.Bounds);
				}
			
				// Text
				TextFormatFlags tf = TextFormatFlags.WordEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.ModifyString | TextFormatFlags.VerticalCenter;
				switch (e.Header.TextAlign)
				{
				case HorizontalAlignment.Center:
					tf |= TextFormatFlags.HorizontalCenter;
					break;
				case HorizontalAlignment.Right:
					tf |= TextFormatFlags.Right;
					break;
				}

				TextRenderer.DrawText(e.Graphics, e.Header.Text, this.Font, e.Bounds, Theme.Current.MenuForeground, tf);
			}
		}
	}
}
