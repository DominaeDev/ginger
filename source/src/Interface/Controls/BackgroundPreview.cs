using Ginger.Properties;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public partial class BackgroundPreview : PictureBox
	{
		public class ChangeBackgroundImageEventArgs : EventArgs
		{
			public string Filename { get; set; }
		}
		public event EventHandler<ChangeBackgroundImageEventArgs> ChangeBackgroundImage;

		private ImageRef _image = null;

		public bool IsAnimation { get; set; }

		public BackgroundPreview()
		{
			InitializeComponent();

			AllowDrop = true;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (Image == null)
			{
				StringFormat stringFormat = new StringFormat();
				stringFormat.Alignment = StringAlignment.Center;
				stringFormat.LineAlignment = StringAlignment.Center;

				e.Graphics.DrawString("No background image", Font, Brushes.White, ClientRectangle, stringFormat);
			}

			if (IsAnimation)
				e.Graphics.DrawImageUnscaled(Resources.animation, Width - 30, Height - 30);
		}

		private void BackgroundPreview_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (files.Length == 1)
				{
					string ext = Utility.GetFileExt(files[0]);
					if (Utility.IsSupportedImageFileExt(ext))
					{
						e.Effect = DragDropEffects.Copy;
						return;
					}
				}
			}
			e.Effect = DragDropEffects.None;
		}

		private void BackgroundPreview_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop) == false)
				return;

			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (files.Length != 1)
				return; // Error
			if (Utility.IsSupportedImageFilename(files[0]) == false)
				return; // Error

			string filename = files[0];

			ChangeBackgroundImage?.Invoke(this, new ChangeBackgroundImageEventArgs() {
				Filename = filename,
			});
		}

		public void SetImage(ImageRef image, bool bAnimation = false)
		{
			if (image == _image)
				return;
			_image = image;
			IsAnimation = _image != null && bAnimation;

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

			ResizeImage();
		}

		public void ResizeImage()
		{
			if (_image == null)
				return;

			if (_image.Width > this.Width || _image.Height > this.Height)
			{
				Image bmpNewImage = new Bitmap(this.Width, this.Height);
				int srcWidth = _image.Width;
				int srcHeight = _image.Height;
				int fitWidth = this.Width;
				int fitHeight = this.Height;
				using (Graphics gfxNewImage = Graphics.FromImage(bmpNewImage))
				{
					float scale = Math.Max((float)fitWidth / srcWidth, (float)fitHeight / srcHeight);
					int newWidth = Math.Max((int)Math.Round(srcWidth * scale), 1);
					int newHeight = Math.Max((int)Math.Round(srcHeight * scale), 1);
					gfxNewImage.DrawImage(_image,
						new Rectangle(-(newWidth - fitWidth) / 2, -(newHeight - fitHeight) / 2, newWidth, newHeight),
							0, 0, srcWidth, srcHeight,
							GraphicsUnit.Pixel);
				}
				this.Image = bmpNewImage;
			}
			else
			{
				this.Image = _image.Clone();
			}
		}

		private void BackgroundPreview_MouseClick(object sender, MouseEventArgs e)
		{
			// No image: Single click
			if (e.Button == MouseButtons.Left && this.Image == null)
			{
				ChangeBackgroundImage?.Invoke(this, new ChangeBackgroundImageEventArgs() {
					Filename = null,
				});
			}
		}

		private void BackgroundPreview_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			// Has image: Double click
			if (e.Button == MouseButtons.Left && this.Image != null)
			{
				ChangeBackgroundImage?.Invoke(this, new ChangeBackgroundImageEventArgs() {
					Filename = null,
				});
			}
		}
	}
}
