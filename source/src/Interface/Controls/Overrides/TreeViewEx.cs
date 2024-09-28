using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public class TreeViewEx : TreeView
	{
		public event MouseEventHandler OnRightClick;

		private Point _rightDownLocation;
		private bool _bRightDown = false;

		protected override void OnMouseLeave(EventArgs e)
		{
			_bRightDown = false;
			base.OnMouseLeave(e);
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == Win32.WM_RBUTTONUP)
			{
				int x = Win32.LoWord(m.LParam);
				int y = Win32.HiWord(m.LParam);
				if (_bRightDown && (Math.Abs(x - _rightDownLocation.X) < 10 && Math.Abs(y - _rightDownLocation.Y) < 10))
					OnRightClick?.Invoke(this, new MouseEventArgs(MouseButtons.Right, 1, x, y, 0));
				_bRightDown = false;
				return;
			}
			if (m.Msg == Win32.WM_RBUTTONDOWN)
			{
				int x = Win32.LoWord(m.LParam);
				int y = Win32.HiWord(m.LParam);
				_rightDownLocation = new Point(x, y);
				_bRightDown = true;
				return;
			}

			base.WndProc(ref m);
		}

		public IEnumerable<TreeNode> AllNodes()
		{
			for (int i = 0; i < this.Nodes.Count; ++i)
			{
				foreach (var node in AllNodes(this.Nodes[i]))
				{
					yield return node;
				}
			}
		}

		private static IEnumerable<TreeNode> AllNodes(TreeNode node)
		{
			for (int i = 0; i < node.Nodes.Count; ++i)
			{
				foreach (var child in AllNodes(node.Nodes[i]))
					yield return child;
			}
			yield return node;
		}
	}
}
