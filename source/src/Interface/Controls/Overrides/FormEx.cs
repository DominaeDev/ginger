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
			Theme.BeginTheming();
			Theme.Apply(this);
			Theme.EndTheming();
		}

		// Reduce flickering
		protected override CreateParams CreateParams
		{
			get
			{
				if (Utility.InDesignMode)
					return base.CreateParams;

				CreateParams cp = base.CreateParams;
				cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
				return cp;
			}
		}

	}
}
