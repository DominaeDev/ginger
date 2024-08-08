using System;
using System.Windows.Forms;

namespace Ginger
{
	partial class AboutBox : Form
	{
		public AboutBox()
		{
			InitializeComponent();
			this.Text = string.Format("About {0}", AppVersion.AssemblyTitle);
			this.labelProductName.Text = AppVersion.AssemblyProduct;
			this.labelVersion.Text = string.Format("Version {0}", AppVersion.AssemblyVersion);
		}
	}
}
