using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FastBlur
{
	public static class ImageTools
	{
		public class ExposedBitmap
		{
			public PinnedByteArray pinnedArray;
			public Bitmap exBitmap;

			public readonly PixelFormat pixelFormat;
			public readonly int bytesPerPixel;
			public readonly int stride;
			public readonly int Height;
			public readonly int Width;

			private int horizontalCoords = -1;
			private int verticalCoords = -1;
			private int horizontalLoc = 0;
			private int verticalLoc = 0;
			private int location = 0;

			public void GetPixel(int x, int y, out byte red, out byte green, out byte blue)
			{
				if (x == horizontalCoords && y == verticalCoords)
				{
					blue = pinnedArray.bytes[location];
					green = pinnedArray.bytes[location + 1];
					red = pinnedArray.bytes[location + 2];
					return;
				}
				else
				{
					if (x != horizontalCoords)
					{
						horizontalCoords = x;
						horizontalLoc = horizontalCoords * bytesPerPixel;
					}

					if (y != verticalCoords)
					{
						verticalCoords = y;
						verticalLoc = verticalCoords * stride;
					}

					location = verticalLoc + horizontalLoc;
				}

				blue = pinnedArray.bytes[location];
				green = pinnedArray.bytes[location + 1];
				red = pinnedArray.bytes[location + 2];
			}

			public void SetPixel(int x, int y, byte red, byte green, byte blue)
			{
				if (x == horizontalCoords && y == verticalCoords)
				{
					pinnedArray.bytes[location] = blue;
					pinnedArray.bytes[location + 1] = green;
					pinnedArray.bytes[location + 2] = red;
					return;
				}
				else
				{
					if (x != horizontalCoords)
					{
						horizontalCoords = x;
						horizontalLoc = horizontalCoords * bytesPerPixel;
					}

					if (y != verticalCoords)
					{
						verticalCoords = y;
						verticalLoc = verticalCoords * stride;
					}

					location = verticalLoc + horizontalLoc;
				}

				pinnedArray.bytes[location] = blue;
				pinnedArray.bytes[location + 1] = green;
				pinnedArray.bytes[location + 2] = red;
			}

			public byte GetRed(int x, int y)
			{
				if (x == horizontalCoords && y == verticalCoords)
				{
					return pinnedArray.bytes[location + 2];
				}
				else
				{
					if (x != horizontalCoords)
					{
						horizontalCoords = x;
						horizontalLoc = horizontalCoords * bytesPerPixel;
					}

					if (y != verticalCoords)
					{
						verticalCoords = y;
						verticalLoc = verticalCoords * stride;
					}

					location = verticalLoc + horizontalLoc;
				}

				return pinnedArray.bytes[location + 2];
			}

			public byte GetGreen(int x, int y)
			{
				if (x == horizontalCoords && y == verticalCoords)
				{
					return pinnedArray.bytes[location + 1];
				}
				else
				{
					if (x != horizontalCoords)
					{
						horizontalCoords = x;
						horizontalLoc = horizontalCoords * bytesPerPixel;
					}

					if (y != verticalCoords)
					{
						verticalCoords = y;
						verticalLoc = verticalCoords * stride;
					}

					location = verticalLoc + horizontalLoc;
				}

				return pinnedArray.bytes[location + 1];
			}

			public byte GetBlue(int x, int y)
			{
				if (x == horizontalCoords && y == verticalCoords)
				{
					return pinnedArray.bytes[location];
				}
				else
				{
					if (x != horizontalCoords)
					{
						horizontalCoords = x;
						horizontalLoc = horizontalCoords * bytesPerPixel;
					}

					if (y != verticalCoords)
					{
						verticalCoords = y;
						verticalLoc = verticalCoords * stride;
					}

					location = verticalLoc + horizontalLoc;
				}

				return pinnedArray.bytes[location];
			}

			public void SetRed(int x, int y, Byte byt)
			{
				if (x == horizontalCoords && y == verticalCoords)
				{
					pinnedArray.bytes[location + 2] = byt;
					return;
				}
				else
				{
					if (x != horizontalCoords)
					{
						horizontalCoords = x;
						horizontalLoc = horizontalCoords * bytesPerPixel;
					}

					if (y != verticalCoords)
					{
						verticalCoords = y;
						verticalLoc = verticalCoords * stride;
					}

					location = verticalLoc + horizontalLoc;
				}

				pinnedArray.bytes[location + 2] = byt;
			}

			public void SetGreen(int x, int y, Byte byt)
			{
				if (x == horizontalCoords && y == verticalCoords)
				{
					pinnedArray.bytes[location + 1] = byt;
					return;
				}
				else
				{
					if (x != horizontalCoords)
					{
						horizontalCoords = x;
						horizontalLoc = horizontalCoords * bytesPerPixel;
					}

					if (y != verticalCoords)
					{
						verticalCoords = y;
						verticalLoc = verticalCoords * stride;
					}

					location = verticalLoc + horizontalLoc;
				}

				pinnedArray.bytes[location + 1] = byt;
			}

			public void SetBlue(int x, int y, Byte byt)
			{
				if (x == horizontalCoords && y == verticalCoords)
				{
					pinnedArray.bytes[location] = byt;
					return;
				}
				else
				{
					if (x != horizontalCoords)
					{
						horizontalCoords = x;
						horizontalLoc = horizontalCoords * bytesPerPixel;
					}

					if (y != verticalCoords)
					{
						verticalCoords = y;
						verticalLoc = verticalCoords * stride;
					}

					location = verticalLoc + horizontalLoc;
				}

				pinnedArray.bytes[location] = byt;
			}

			public class PinnedByteArray
			{
				public byte[] bytes;
				internal GCHandle handle;
				internal IntPtr ptr;
				private int referenceCount;
				private bool destroyed;

				public PinnedByteArray(int length)
				{
					bytes = new byte[length];
					handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
					ptr = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
					referenceCount++;
				}

				internal void AddReference()
				{
					referenceCount++;
				}

				internal void ReleaseReference()
				{
					referenceCount--;
					if (referenceCount <= 0) Destroy();
				}

				private void Destroy()
				{
					if (!destroyed)
					{
						handle.Free();
						bytes = null;
						destroyed = true;
					}
				}

				~PinnedByteArray()
				{
					Destroy();
				}
			}

			private int GetStride(int width, int bytesPerPixel)
			{
				int stride = width * bytesPerPixel;

				// Correct for the 4 byte boundary requirement:
				stride += stride % 4 == 0 ? 0 : 4 - (stride % 4);

				return stride;
			}

			public ExposedBitmap(ref Bitmap sourceBmp)
			{
				// Get the basic info from sourceBmp and store it locally (improves performance)
				Height = sourceBmp.Height;
				Width = sourceBmp.Width;
				pixelFormat = sourceBmp.PixelFormat;

				// Create exBitmap, associating it with our pinned array so we can access the bitmap bits directly:
				bytesPerPixel = Image.GetPixelFormatSize(pixelFormat) / 8;
				stride = GetStride(Width, bytesPerPixel);
				pinnedArray = new PinnedByteArray(stride * Height);
				exBitmap = new Bitmap(Width, Height, stride, pixelFormat, pinnedArray.ptr);

				// Copy the image from sourceBmp to exBitmap:
				Graphics g = Graphics.FromImage(exBitmap);
				g.DrawImage(sourceBmp, 0, 0, Width, Height);
				g.Dispose();
			}
		}

		public static void Blur(ref Bitmap image)
		{
			Blur(ref image, new Rectangle(0, 0, image.Width, image.Height), 2);
		}

		public static void Blur(ref Bitmap image, Int32 blurSize)
		{
			Blur(ref image, new Rectangle(0, 0, image.Width, image.Height), blurSize);
		}

		private static void Blur(ref Bitmap image, Rectangle rectangle, Int32 blurSize)
		{
			ExposedBitmap blurred = new ExposedBitmap(ref image);

			// Store height & width locally (improives performance)
			int height = blurred.Height;
			int width = blurred.Width;

			for (int xx = rectangle.X; xx < rectangle.X + rectangle.Width; xx++)
			{
				for (int yy = rectangle.Y; yy < rectangle.Y + rectangle.Height; yy++)
				{
					//byte red, green, blue;
					int avgR = 0, avgG = 0, avgB = 0;
					int blurPixelCount = 0;
					int horizontalLocation;
					int verticalLocation;
					int pixelPointer;

					// Average the color of the red, green and blue for each pixel in the
					// blur size while making sure you don't go outside the image bounds:
					for (int x = xx; (x < xx + blurSize && x < width); x++)
					{
						horizontalLocation = x * blurred.bytesPerPixel;
						for (int y = yy; (y < yy + blurSize && y < height); y++)
						{
							verticalLocation = y * blurred.stride;
							pixelPointer = verticalLocation + horizontalLocation;

							avgB += blurred.pinnedArray.bytes[pixelPointer];
							avgG += blurred.pinnedArray.bytes[pixelPointer + 1];
							avgR += blurred.pinnedArray.bytes[pixelPointer + 2];

							blurPixelCount++;
						}
					}

					byte bavgr = (byte)(avgR / blurPixelCount);
					byte bavgg = (byte)(avgG / blurPixelCount);
					byte bavgb = (byte)(avgB / blurPixelCount);

					// Now that we know the average for the blur size, set each pixel to that color
					for (int x = xx; x < xx + blurSize && x < width && x < rectangle.Width; x++)
					{
						horizontalLocation = x * blurred.bytesPerPixel;
						for (int y = yy; y < yy + blurSize && y < height && y < rectangle.Height; y++)
						{
							verticalLocation = y * blurred.stride;
							pixelPointer = verticalLocation + horizontalLocation;

							blurred.pinnedArray.bytes[pixelPointer] = bavgb;
							blurred.pinnedArray.bytes[pixelPointer + 1] = bavgg;
							blurred.pinnedArray.bytes[pixelPointer + 2] = bavgr;
						}
					}
				}
			}

			image = blurred.exBitmap;
		}
	}
}
