using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Ginger
{
	public class CollectionTextBox : TextBoxEx
	{
		public CollectionTextBox() : base()
		{
			this.LostFocus += OnValidate;
			this.PreviewKeyDown += OnPreviewKeyDown;
		}

		public HashSet<string> Collection
		{
			get
			{
				var list = Utility.ListFromCommaSeparatedString(this.Text)
					.Distinct(StringComparer.Create(CultureInfo.InvariantCulture, true));
				return new HashSet<string>(list);
			}
			set
			{
				this.Text = Utility.ListToCommaSeparatedString(value);
			}
		}

		private void OnValidate(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(this.Text))
				return;

			// Clean up
			this.Text = Utility.ListToCommaSeparatedString(Collection);
		}

		private void OnPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyData == Keys.Return || e.KeyData == Keys.Enter)
			{
				if (Collection.Count > 0)
				{
					this.Text = Utility.ListToCommaSeparatedString(Collection) + ", ";
					this.SelectionStart = this.Text.Length;
				}
				e.IsInputKey = false;
			}
		}
	}
}
