using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Ginger
{
	public partial class PageChanger : UserControl, IThemedControl
	{
		public int currentPage	{ get; private set; }	// zero-based
		public int maxPages		{ get; private set; }	// one-based

		private bool _bIgnoreEvents;

		public class PageChangedEventArgs : EventArgs
		{
			public int page;
		}
		public event EventHandler<PageChangedEventArgs> PageChanged;

		public PageChanger()
		{
			InitializeComponent();

			Resize += PageChanger_Resize;
			textBox_Page.KeyPress += textBox_Page_KeyPress;
			textBox_Page.GotFocus += textBox_Page_GotFocus;
			textBox_Page.LostFocus += textBox_Page_LostFocus;
		}

		private void btnPrev_Click(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			if (currentPage > 0)
			{
				if (ModifierKeys == Keys.Shift)
					SetPage(0, maxPages);
				else
					SetPage(currentPage - 1, maxPages);

				PageChanged?.Invoke(this, new PageChangedEventArgs() {
					page = currentPage,
				});
			}
		}

		private void btnNext_Click(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			if (currentPage < maxPages - 1)
			{
				if (ModifierKeys == Keys.Shift)
					SetPage(maxPages - 1, maxPages);
				else
					SetPage(currentPage + 1, maxPages);

				PageChanged?.Invoke(this, new PageChangedEventArgs() {
					page = currentPage,
				});
			}
		}

		private void textBox_Page_EnterPressed(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int page;
			if (int.TryParse(textBox_Page.Text, out page))
			{
				page = Math.Min(Math.Max(page - 1, 0), maxPages - 1);
				SetPage(page, this.maxPages);

				PageChanged?.Invoke(this, new PageChangedEventArgs() {
					page = currentPage,
				});

				textBox_Page.SelectAll();
			}
		}

		private void textBox_Page_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar))
			{
				e.Handled = false; //Do not reject the input
			}
			else
			{
				e.Handled = true; //Reject the input
			}
		}

		private void textBox_Page_GotFocus(object sender, EventArgs e)
		{
			textBox_Page.SelectAll();
		}

		private void textBox_Page_LostFocus(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			SetPage(currentPage, this.maxPages);
		}

		public void SetPage(int page, int maxPages)
		{
			this.currentPage = Math.Min(Math.Max(page, 0), maxPages - 1);
			this.maxPages = maxPages;

			_bIgnoreEvents = true;
			textBox_Page.SetText((this.currentPage + 1).ToString(CultureInfo.InvariantCulture));
			label_Pages.Text = string.Format(CultureInfo.InvariantCulture, "/ {0}", this.maxPages);
			_bIgnoreEvents = false;
		}

		private void PageChanger_Resize(object sender, EventArgs e)
		{
			var scaleFactor = this.Font.SizeInPoints / Constants.ReferenceFontSize;
			int clientWidth = (int)(this.Width / scaleFactor);
			int clientHeight = (int)(this.Height / scaleFactor);
			textBox_Page.Bounds = new Rectangle(clientWidth / 2 - textBox_Page.Width, (int)((this.Height - textBox_Page.Height) / (2 * scaleFactor)), textBox_Page.Width, textBox_Page.Height);
			label_Pages.Bounds = new Rectangle(clientWidth / 2, textBox_Page.Top, label_Pages.Width, textBox_Page.Height);
		}

		public void ApplyVisualTheme()
		{
			btnNext.Image = Theme.Current.ArrowRight;
			btnPrev.Image = Theme.Current.ArrowLeft;
		}
	}
}
