using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
	}
}
