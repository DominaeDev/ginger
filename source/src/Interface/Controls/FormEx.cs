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

		protected virtual void ApplyTheme()
		{
			VisualTheme.ApplyTheme(this);
		}
	}
}
