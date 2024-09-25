using System;
using System.Windows.Forms;

namespace Ginger
{
	public class FormEx : Form
	{
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			ApplyTheme();
		}

		public virtual void ApplyTheme()
		{
			VisualTheme.ApplyTheme(this);
		}
	}
}
