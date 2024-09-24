using System;
using System.Windows.Forms;

namespace Ginger
{
	partial class AboutBox : FormEx
	{
		public AboutBox()
		{
			InitializeComponent();
			this.Text = string.Format("About {0}", AppVersion.ProductTitle);
			this.labelProductName.Text = AppVersion.ProductName;
			this.labelVersion.Text = string.Format("Version {0}", AppVersion.ProductVersion);
		}
	}
}
