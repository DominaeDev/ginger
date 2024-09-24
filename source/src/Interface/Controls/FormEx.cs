using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ginger
{
	public class FormEx : Form
	{
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			VisualTheme.ApplyTheme(this);
		}

	}
}
