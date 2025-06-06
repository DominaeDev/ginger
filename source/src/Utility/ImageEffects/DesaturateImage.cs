﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Ginger
{
	public class DesaturateImage
	{
		private readonly int[] _alpha;
		private readonly int[] _red;
		private readonly int[] _green;
		private readonly int[] _blue;

		private readonly int _width;
		private readonly int _height;

		private readonly ParallelOptions _pOptions = new ParallelOptions { MaxDegreeOfParallelism = 16 };

		public DesaturateImage(Bitmap image)
		{
			var rct = new Rectangle(0, 0, image.Width, image.Height);
			var source = new int[rct.Width * rct.Height];
			var bits = image.LockBits(rct, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
			Marshal.Copy(bits.Scan0, source, 0, source.Length);
			image.UnlockBits(bits);

			_width = image.Width;
			_height = image.Height;

			_alpha = new int[_width * _height];
			_red = new int[_width * _height];
			_green = new int[_width * _height];
			_blue = new int[_width * _height];

			Parallel.For(0, source.Length, _pOptions, i => {
				_alpha[i] = (int)((source[i] & 0xff000000) >> 24);
				_red[i] = (source[i] & 0xff0000) >> 16;
				_green[i] = (source[i] & 0x00ff00) >> 8;
				_blue[i] = (source[i] & 0x0000ff);
			});
		}

		public Bitmap Process(double amount)
		{
			var newRed = new int[_width * _height];
			var newGreen = new int[_width * _height];
			var newBlue = new int[_width * _height];
			var dest = new int[_width * _height];

			desaturate(_red, _green, _blue, newRed, newGreen, newBlue, amount);

			Parallel.For(0, dest.Length, _pOptions, i => {
				if (newRed[i] > 255) newRed[i] = 255;
				if (newGreen[i] > 255) newGreen[i] = 255;
				if (newBlue[i] > 255) newBlue[i] = 255;

				if (newRed[i] < 0) newRed[i] = 0;
				if (newGreen[i] < 0) newGreen[i] = 0;
				if (newBlue[i] < 0) newBlue[i] = 0;

				dest[i] = (int)((uint)(_alpha[i] << 24) | (uint)(newRed[i] << 16) | (uint)(newGreen[i] << 8) | (uint)newBlue[i]);
			});

			var image = new Bitmap(_width, _height);
			var rct = new Rectangle(0, 0, image.Width, image.Height);
			var bits2 = image.LockBits(rct, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
			Marshal.Copy(dest, 0, bits2.Scan0, dest.Length);
			image.UnlockBits(bits2);
			return image;
		}

		private void desaturate(int[] srcR, int[] srcG, int[] srcB, int[] dstR, int[] dstG, int[] dstB, double amount)
		{
			for (var i = 0; i < srcR.Length; i++)
			{
				double value = 0.3 * srcR[i] + 0.6 * srcG[i] + 0.1 * srcB[i];
				dstR[i] = Convert.ToInt32(srcR[i] + amount * (value - srcR[i]));
				dstG[i] = Convert.ToInt32(srcG[i] + amount * (value - srcG[i]));
				dstB[i] = Convert.ToInt32(srcB[i] + amount * (value - srcB[i]));
			}
		}

	}
}
