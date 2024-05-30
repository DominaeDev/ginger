using Ginger.Properties;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public partial class PortraitPreview : PictureBox
	{
		public class ChangePortraitImageEventArgs : EventArgs
		{
			public string Filename { get; set; }
		}
		public event EventHandler<ChangePortraitImageEventArgs> ChangePortraitImage;

		private ImageRef _image = null;

		public PortraitPreview()
		{
			InitializeComponent();

			AllowDrop = true;
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			base.OnPaint(pe);

			if (Image == null)
			{
				StringFormat stringFormat = new StringFormat();
				stringFormat.Alignment = StringAlignment.Center;
				stringFormat.LineAlignment = StringAlignment.Center;

				pe.Graphics.DrawString(Resources.msg_drop_image, Font, Brushes.White, ClientRectangle, stringFormat);
			}
		}

		private void PortraitPreview_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (files.Length == 1)
				{
					string fn = files[0].ToLowerInvariant();
					if (fn.EndsWith(".png") || fn.EndsWith(".jpg") || fn.EndsWith(".jpeg"))
					{
						e.Effect = DragDropEffects.Copy;
						return;
					}
				}
			}
			e.Effect = DragDropEffects.None;
		}

		private void PortraitPreview_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop) == false)
				return;

			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (files.Length != 1)
				return; // Error
			string fn = files[0].ToLowerInvariant();
			if (!(fn.EndsWith(".png") || fn.EndsWith(".jpg") || fn.EndsWith(".jpeg")))
				return; // Error

			string filename = files[0];

			ChangePortraitImage?.Invoke(this, new ChangePortraitImageEventArgs() {
				Filename = filename,
			});
		}

		public void SetImage(ImageRef image)
		{
			if (image == _image)
				return;
			_image = image;

			// Clear existing image
			if (this.Image != null)
			{
				this.Image.Dispose();
				this.Image = null;
			}

			if (image == null)
			{
				this.Image = null;
				return;
			}

			if (image.Width > this.Width || image.Height > this.Height)
			{
				Image bmpNewImage = new Bitmap(this.Width, this.Height);
				int srcWidth = image.Width;
				int srcHeight = image.Height;
				int fitWidth = this.Width;
				int fitHeight = this.Height;
				using (Graphics gfxNewImage = Graphics.FromImage(bmpNewImage))
				{
					float scale = Math.Max((float)fitWidth / srcWidth, (float)fitHeight / srcHeight);
					int newWidth = (int)Math.Round(srcWidth * scale);
					int newHeight = (int)Math.Round(srcHeight * scale);
					gfxNewImage.DrawImage(image,
						new Rectangle(-(newWidth - fitWidth)/2, 0, newWidth, newHeight),
							0, 0, srcWidth, srcHeight,
							GraphicsUnit.Pixel);
				}
				this.Image = bmpNewImage;

			}
			else
			{
				this.Image = image.Clone();
			}
		}

		private void PortraitPreview_MouseClick(object sender, MouseEventArgs e)
		{
			// No image: Single click
			if (e.Button == MouseButtons.Left && this.Image == null)
			{
				ChangePortraitImage(this, new ChangePortraitImageEventArgs() {
					Filename = null,
				});
			}
		}

		private void PortraitPreview_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			// Has image: Double click
			if (e.Button == MouseButtons.Left && this.Image != null)
			{
				ChangePortraitImage(this, new ChangePortraitImageEventArgs() {
					Filename = null,
				});
			}
		}
	}
}
