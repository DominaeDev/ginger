/*
 * PNG.Net
 * 
 * Copyright (C) 2008 wj32
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace PNGNet
{
	/// <summary>
	/// Provides high-level methods to read and write PNG bitmaps.
	/// </summary>
	public sealed class PNGBitmap
    {
        /// <summary>
        /// The type of filter, applied to each scanline.
        /// </summary>
        public enum FilterType : byte
        {
            None = 0,
            Sub,
            Up,
            Average,
            Paeth
        }

        #region Adam7 Sequence

        /*   0_1_2_3_4_5_6_7
         * 0|1 6 4 6 2 6 4 6
         * 1|7 7 7 7 7 7 7 7
         * 2|5 6 5 6 5 6 5 6
         * 3|7 7 7 7 7 7 7 7
         * 4|3 6 4 6 3 6 4 6
         * 5|7 7 7 7 7 7 7 7
         * 6|5 6 5 6 5 6 5 6
         * 7|7 7 7 7 7 7 7 7
         */
        private byte[][] _adam7 = new byte[][]
        {
            new byte[] { 0, 5, 3, 5, 1, 5, 3, 5 },
            new byte[] { 6, 6, 6, 6, 6, 6, 6, 6 },
            new byte[] { 4, 5, 4, 5, 4, 5, 4, 5 },
            new byte[] { 6, 6, 6, 6, 6, 6, 6, 6 },
            new byte[] { 2, 5, 3, 5, 2, 5, 3, 5 },
            new byte[] { 6, 6, 6, 6, 6, 6, 6, 6 },
            new byte[] { 4, 5, 4, 5, 4, 5, 4, 5 },
            new byte[] { 6, 6, 6, 6, 6, 6, 6, 6 }
        };

#pragma warning disable 0169 // Field never used
        int[] _adam7Pixels, _adam7Lines;

        // stored as lines, x, y, x, y, ...
        private byte[][] _adam7Lookup = new byte[][]
        {
            new byte[] { 1, 0, 0 }, // 1
            new byte[] { 1, 4, 0 }, // 2
            new byte[] { 1, 0, 4, 4, 4}, // 3
            new byte[]
            {
                2,
                2, 0, 6, 0, 
                2, 4, 6, 4
            }, // 4
            new byte[]
            {
                2,
                0, 2, 2, 2, 4, 2, 6, 2, 
                0, 6, 2, 6, 4, 6, 6, 6
            }, // 5
            new byte[]
            {
                4,
                1, 0, 3, 0, 5, 0, 7, 0, 
                1, 2, 3, 2, 5, 2, 7, 2,
                1, 4, 3, 4, 5, 4, 7, 4,
                1, 6, 3, 6, 5, 6, 7, 6
            }, // 6 
            new byte[]
            {
                4,
                0, 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1, 6, 1, 7, 1,
                0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3, 6, 3, 7, 3,
                0, 5, 1, 5, 2, 5, 3, 5, 4, 5, 5, 5, 6, 5, 7, 5,
                0, 7, 1, 7, 2, 7, 3, 7, 4, 7, 5, 7, 6, 7, 7, 7
            } // 7
        };
#pragma warning restore 0169 // Field never used

        #endregion

        private PNGImage _image;
        private Bitmap _bitmap;

        // state
        int _scanlineLength;
        int _bpp;

        /// <summary>
        /// Creates a new PNG bitmap.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <param name="ct">The color type.</param>
        /// <param name="bitDepth">The bit depth.</param>
        public PNGBitmap(Size size, ColorType ct, byte bitDepth) : this(size.Width, size.Height, ct, bitDepth) { }

        /// <summary>
        /// Creates a new PNG bitmap.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="ct">The color type.</param>
        /// <param name="bitDepth">The bit depth.</param>
        public PNGBitmap(int width, int height, ColorType ct, byte bitDepth)
        {          
            _bitmap = CreateBitmap(ct, bitDepth, width, height);      
        }

        /// <summary>
        /// Reads a PNG file from the specified stream.
        /// </summary>
        /// <param name="s">The stream to read from.</param>
        public PNGBitmap(Stream s)
        {
            _image = new PNGImage(s);

            this.Read(this.Decompress());
        }

        /// <summary>
        /// Creates a Bitmap from the given information.
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="bitDepth"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private Bitmap CreateBitmap(ColorType ct, byte bitDepth, int width, int height)
        {
            // don't need to use the extra information for now

            return new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        }

        /// <summary>
        /// Decompresses the compressed data stored in one or more IDAT chunks.
        /// </summary>
        private byte[] Decompress()
        {
            Inflater inf = new Inflater();
            MemoryStream decompressed = new MemoryStream();
            MemoryStream compressed = new MemoryStream();
            byte[] buf = new byte[1];

            // decompress the image data
            foreach (Chunk c in _image.Chunks)
            {
                if (c.Type == "IDAT")
                {
                    IDATChunk idat = c as IDATChunk;

                    compressed.Write(idat.Data, 0, idat.Data.Length);

                    if (compressed.Length > 15)
                    {
                        inf.SetInput(compressed.ToArray());

                        while (!inf.IsNeedingInput)
                        {
                            if (inf.Inflate(buf) == -1)
                                break;

                            decompressed.WriteByte(buf[0]);
                        }

                        compressed = new MemoryStream();
                    }
                }
            }

            inf.SetInput(compressed.ToArray());

            while (!inf.IsNeedingInput)
            {
                if (inf.Inflate(buf) == -1)
                    break;

                decompressed.WriteByte(buf[0]);
            }

            if (!inf.IsFinished)
                throw new InvalidCompressedDataException("Inflater is not finished but there are no more IDAT chunks.");

            byte[] arr = decompressed.ToArray();

            decompressed.Close();

            return arr;
        }

        /// <summary>
        /// Returns the paeth predictor of the three values.
        /// </summary>
        /// <param name="a">The left value.</param>
        /// <param name="b">The above value.</param>
        /// <param name="c">The upper left value.</param>
        /// <returns>The closest neighbouring value.</returns>
        private byte PaethPredictor(byte a, byte b, byte c)
        {
            int p = (int)a + (int)b - (int)c;
            int pa = (int)Math.Abs((int)p - a);
            int pb = (int)Math.Abs((int)p - b);
            int pc = (int)Math.Abs((int)p - c);

            if ((pa <= pb) && (pa <= pc))
                return a;
            else if (pb <= pc)
                return b;
            else
                return c;
        }

        /// <summary>
        /// Defilters a scanline.
        /// </summary>
        /// <param name="data">The raw image data.</param>
        /// <param name="line">The scanline number.</param>
        /// <param name="priorLine">The previous defiltered line.</param>
        /// <returns>The defiltered data.</returns>
        private byte[] Defilter(byte[] data, int line, byte[] priorLine)
        {
            FilterType ft = (FilterType)data[line * _scanlineLength];
            byte[] lineData = new byte[_scanlineLength - 1];
            int lds = line * _scanlineLength + 1;

            switch (ft)
            {
                case FilterType.None:
                    // no filtering, just copy the line over, skipping the filter type byte.
                    Array.Copy(data, lds, lineData, 0, lineData.Length);
                    break;

                case FilterType.Sub:
                    for (int i = 0; i < lineData.Length; i++)
                    {
                        byte orig = data[lds + i];
                        byte rawxbpp = (i - _bpp < 0) ? (byte)0 : lineData[i - _bpp];

                        lineData[i] = (byte)((
                            orig + rawxbpp
                            ) % 256);
                    }

                    break;

                case FilterType.Up:
                    if (line == 0)
                    {
                        Array.Copy(data, lds, lineData, 0, lineData.Length);
                    }
                    else
                    {
                        for (int i = 0; i < lineData.Length; i++)
                        {           
                            byte orig = data[lds + i];
                            byte priorx = priorLine[i];

                            lineData[i] = (byte)((
                                orig + priorx
                                ) % 256);
                        }
                    }

                    break;

                case FilterType.Average:
                    if (line == 0)
                    {
                        for (int i = 0; i < lineData.Length; i++)
                        {
                            byte orig = data[lds + i];
                            byte rawxbpp = (i - _bpp < 0) ? (byte)0 : lineData[i - _bpp];

                            lineData[i] = (byte)((
                                orig + (int)Math.Floor((double)rawxbpp / 2)
                                ) % 256);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < lineData.Length; i++)
                        {
                            byte orig = data[lds + i];
                            byte rawxbpp = (i - _bpp < 0) ? (byte)0 : lineData[i - _bpp];
                            byte priorx = priorLine[i];

                            lineData[i] = (byte)((
                                orig + (int)Math.Floor((double)(rawxbpp + priorx) / 2)
                                ) % 256);
                        }
                    }

                    break;

                case FilterType.Paeth:
                    if (line == 0)
                    {
                        for (int i = 0; i < lineData.Length; i++)
                        {
                            byte orig = data[lds + i];
                            byte rawxbpp = (i - _bpp < 0) ? (byte)0 : lineData[i - _bpp];

                            lineData[i] = (byte)((
                                orig + this.PaethPredictor(rawxbpp, 0, 0)
                                ) % 256);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < lineData.Length; i++)
                        {
                            byte orig = data[lds + i];
                            byte rawxbpp = (i - _bpp < 0) ? (byte)0 : lineData[i - _bpp];
                            byte priorx = priorLine[i];
                            byte priorxbpp = (i - _bpp < 0) ? (byte)0 : priorLine[i - _bpp];

                            lineData[i] = (byte)((
                                orig + this.PaethPredictor(rawxbpp, priorx, priorxbpp)
                                ) % 256);
                        }
                    }

                    break;

                default:
                    throw new Exception(string.Format("Unknown filter type {0}.", ft));
            }

            return lineData;
        }

        /// <summary>
        /// Filters a scanline.
        /// </summary>
        /// <param name="data">A single line of data.</param>
        /// <param name="line">The line number.</param>
        /// <param name="ft">The filter type to use.</param>
        /// <param name="priorLine">The previous unfiltered line.</param>
        /// <returns>The filtered bytes.</returns>
        private byte[] Filter(byte[] data, int line, FilterType ft, byte[] priorLine)
        {
            byte[] lineData = new byte[_scanlineLength];

            lineData[0] = (byte)ft;

            switch (ft)
            {
                case FilterType.None:
                    // no filtering, just copy the line over, skipping the filter type byte.
                    Array.Copy(data, 0, lineData, 1, data.Length);
                    break;

                case FilterType.Sub:
                    for (int i = 0; i < data.Length; i++)
                    {
                        byte orig = data[i];
                        byte rawxbpp = (i - _bpp < 0) ? (byte)0 : data[i - _bpp];

                        lineData[i + 1] = (byte)((
                            orig - rawxbpp
                            ) % 256);
                    }

                    break;

                case FilterType.Up:
                    if (line == 0)
                    {
                        Array.Copy(data, 0, lineData, 1, data.Length);
                    }
                    else
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            byte orig = data[i];
                            byte priorx = priorLine[i];

                            lineData[i + 1] = (byte)((
                                orig - priorx
                                ) % 256);
                        }
                    }

                    break;

                case FilterType.Average:
                    if (line == 0)
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            byte orig = data[i];
                            byte rawxbpp = (i - _bpp < 0) ? (byte)0 : data[i - _bpp];

                            lineData[i + 1] = (byte)((
                                orig - (int)Math.Floor((double)rawxbpp / 2)
                                ) % 256);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            byte orig = data[i];
                            byte rawxbpp = (i - _bpp < 0) ? (byte)0 : data[i - _bpp];
                            byte priorx = priorLine[i];

                            lineData[i + 1] = (byte)((
                                orig - (int)Math.Floor((double)(rawxbpp + priorx) / 2)
                                ) % 256);
                        }
                    }

                    break;

                case FilterType.Paeth:
                    if (line == 0)
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            byte orig = data[i];
                            byte rawxbpp = (i - _bpp < 0) ? (byte)0 : lineData[i - _bpp];

                            lineData[i + 1] = (byte)((
                                orig - this.PaethPredictor(rawxbpp, 0, 0)
                                ) % 256);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            byte orig = data[i];
                            byte rawxbpp = (i - _bpp < 0) ? (byte)0 : data[i - _bpp];
                            byte priorx = priorLine[i];
                            byte priorxbpp = (i - _bpp < 0) ? (byte)0 : priorLine[i - _bpp];

                            lineData[i + 1] = (byte)((
                                orig - this.PaethPredictor(rawxbpp, priorx, priorxbpp)
                                ) % 256);
                        }
                    }

                    break;

                default:
                    throw new Exception(string.Format("Unknown filter type {0}.", ft));
            }

            return lineData;
        }

        /// <summary>
        /// Calculates the best filter method for the given scanline by applying each filter to the 
        /// given scanline, computing the sum of each filtered byte (as a signed value), then returning 
        /// the filter which produces the lowest absolute value of the sum.
        /// </summary>
        /// <param name="data">The scanline to test.</param>
        /// <param name="line">The line number.</param>
        /// <param name="priorLine">The previous unfiltered line.</param>
        /// <returns></returns>
        private FilterType GetBestFilterMethod(byte[] data, int line, byte[] priorLine)
        {
            Dictionary<FilterType, long> values = new Dictionary<FilterType,long>();

            foreach (FilterType ft in Enum.GetValues(typeof(FilterType)))
            {
                byte[] f = this.Filter(data, line, ft, priorLine);
                long sum = 0;

                foreach (byte b in f)
                {
                    if (b > 127)
                        sum -= 256 - b;
                    else
                        sum += b;
                }

                values.Add(ft, (long)Math.Abs(sum));
            }

            FilterType minType = FilterType.None;
            long minValue = (1 << 62) - 1;

            foreach (FilterType ft in values.Keys)
            {
                if (values[ft] < minValue)
                {
                    minType = ft;
                    minValue = values[ft];
                }
            }

            return minType;  
        }

        private void FillAdam7Tables(int width, int height)
        {
            // yCounts[pass] = count of lines in pass  
            // xCounts[line][pass] = count of pass in each line
            int[] yCounts = new int[7];           
            int[][] xCounts = new int[8][];       

            for (int i = 0; i < 7; i++)
                xCounts[i] = new int[7];
        }

        /// <summary>
        /// Reads the image pixels and stores them in Bitmap.
        /// </summary>
        /// <param name="data">The image bytes.</param>
        private void Read(byte[] data)
        {
            // optimized for speed (it's very slow already)

            IHDRChunk hdr = _image.Chunks["IHDR"] as IHDRChunk;
            int width = (int)hdr.Width;
            int height = (int)hdr.Height;
            byte bitDepth = hdr.BitDepth;

            _bitmap = CreateBitmap(hdr.ColorType, hdr.BitDepth, width, height);
            _scanlineLength = Utils.IntCeilDiv(Utils.GetBitsPerPixel(hdr.ColorType, hdr.BitDepth) * width, 8) + 1;
            _bpp = Utils.GetBytesPerPixel(hdr.ColorType, hdr.BitDepth); // bytes per pixel

            byte[] decoded = null;

            // palette
            PLTEChunk palette = _image.Chunks["PLTE"] as PLTEChunk;

            #region tRNS Chunk

            tRNSChunk trns = _image.Chunks["tRNS"] as tRNSChunk;
            int tGray1 = -1;
            int tGray2 = -1;
            int tGray4 = -1;
            int tGray8 = -1;
            int tGray16 = -1;
            Color tRgb8 = Color.FromArgb(0, 0, 0, 0);
            Color tRgb16 = Color.FromArgb(0, 0, 0, 0);
            int[] tPalette = null;

            if (trns != null)
            {
                tGray1 = trns.Gray & 0x1;
                tGray2 = trns.Gray & 0x3;
                tGray4 = trns.Gray & 0xf;
                tGray8 = trns.Gray & 0xff;
                tGray16 = trns.Gray;
                tRgb8 = Color.FromArgb(trns.Red & 0xff, trns.Green & 0xff, trns.Blue & 0xff);
                tRgb16 = Color.FromArgb(trns.Red * 255 / 65535, trns.Green * 255 / 65535, trns.Blue * 255 / 65535);

                if (palette != null)
                {
                    tPalette = new int[palette.Entries.Length];

                    for (int i = 0; i < tPalette.Length; i++)
                    {
                        if (i < trns.PaletteAlpha.Length)
                            tPalette[i] = trns.PaletteAlpha[i];
                        else
                            tPalette[i] = 255;
                    }
                }
            }
            else
            {
                if (palette != null)
                {
                    tPalette = new int[palette.Entries.Length];

                    for (int i = 0; i < tPalette.Length; i++)
                        tPalette[i] = 255;
                }
            }

            #endregion

            if (hdr.InterlaceMethod == InterlaceMethod.Adam7)
            {
                #region Adam7

                for (int pass = 0; pass < _adam7.Length; pass++)
                {
                        
                }

                #endregion
            }
            else
            {
                #region Normal

                for (int line = 0; line < data.Length / _scanlineLength; line++)
                {
                    decoded = this.Defilter(data, line, decoded);

                    switch (hdr.ColorType)
                    {
                        case ColorType.Grayscale:
                            if (bitDepth == 1)
                            {
                                for (int i = 0; i < Utils.IntCeilDiv(width, 8); i++)
                                {
                                    int[] pixels = new int[8];

                                    for (int j = 0; j < 8; j++)
                                        pixels[j] = (decoded[i] >> (7 - j)) & 0x1;

                                    for (int j = 0; j < 8; j++)
                                        if (i * 8 + j < hdr.Width)
                                            _bitmap.SetPixel(i * 8 + j, line, 
                                                Utils.MakeGray(pixels[j] == tGray1 ? 0 : 255, pixels[j] * 255));
                                }
                            }
                            else if (bitDepth == 2)
                            {
                                for (int i = 0; i < Utils.IntCeilDiv(width, 4); i++)
                                {
                                    int[] pixels = new int[4];

                                    for (int j = 0; j < 4; j++)
                                        pixels[j] = (decoded[i] >> ((3 - j) * 2)) & 0x3;

                                    for (int j = 0; j < 4; j++)
                                        if (i * 4 + j < hdr.Width)
                                            _bitmap.SetPixel(i * 4 + j, line, 
                                                Utils.MakeGray(pixels[j] == tGray2 ? 0 : 255, pixels[j] * 255 / 3));
                                }
                            }
                            else if (bitDepth == 4)
                            {
                                for (int i = 0; i < Utils.IntCeilDiv(width, 2); i++)
                                {
                                    int pixel1 = decoded[i] >> 4; // upper four bits
                                    int pixel2 = decoded[i] & 0xf; // lower two bits

                                    _bitmap.SetPixel(i * 2, line, Utils.MakeGray(
                                        pixel1 == tGray4 ? 0 : 255, pixel1 * 255 / 15));

                                    if (i * 2 + 1 < hdr.Width)
                                        _bitmap.SetPixel(i * 2 + 1, line, Utils.MakeGray(
                                            pixel2 == tGray4 ? 0 : 255, pixel2 * 255 / 15));
                                }
                            }
                            else if (bitDepth == 8)
                            {
                                for (int i = 0; i < hdr.Width; i++)
                                    _bitmap.SetPixel(i, line,
                                        Utils.MakeGray(decoded[i] == tGray8 ? 0 : 255, decoded[i]));
                            }
                            else if (bitDepth == 16)
                            {
                                for (int i = 0; i < hdr.Width; i++)
                                {
                                    int value = Utils.BytesToUShort(decoded, i * 2, Utils.Endianness.Big);

                                    _bitmap.SetPixel(i, line, Utils.MakeGray(
                                        value == tGray16 ? 0 : 255, value * 255 / 65535));
                                }
                            }

                            break;

                        case ColorType.GrayscaleWithAlpha:
                            if (bitDepth == 8)
                            {
                                for (int i = 0; i < hdr.Width; i++)
                                    _bitmap.SetPixel(i, line, Color.FromArgb(
                                        decoded[i * 2 + 1],
                                        decoded[i * 2], decoded[i * 2], decoded[i * 2]
                                        ));
                            }
                            else if (bitDepth == 16)
                            {
                                for (int i = 0; i < hdr.Width; i++)
                                    _bitmap.SetPixel(i, line, Color.FromArgb(
                                        Utils.BytesToUShort(decoded, i * 4 + 2, Utils.Endianness.Big) * 255 / 65535,
                                        Utils.BytesToUShort(decoded, i * 4, Utils.Endianness.Big) * 255 / 65535,
                                        Utils.BytesToUShort(decoded, i * 4, Utils.Endianness.Big) * 255 / 65535,
                                        Utils.BytesToUShort(decoded, i * 4, Utils.Endianness.Big) * 255 / 65535
                                        ));
                            }

                            break;

                        case ColorType.IndexedColor:
                            if (bitDepth == 1)
                            {
                                for (int i = 0; i < Utils.IntCeilDiv(width, 8); i++)
                                {
                                    int[] pixels = new int[8];

                                    for (int j = 0; j < 8; j++)
                                        pixels[j] = (decoded[i] >> (7 - j)) & 0x1;

                                    for (int j = 0; j < 8; j++)
                                        if (i * 8 + j < hdr.Width)
                                            _bitmap.SetPixel(i * 8 + j, line, 
                                                Color.FromArgb(tPalette[pixels[j]], palette.Entries[pixels[j]]));
                                }
                            }
                            else if (bitDepth == 2)
                            {
                                for (int i = 0; i < Utils.IntCeilDiv(width, 4); i++)
                                {
                                    int[] pixels = new int[4];

                                    for (int j = 0; j < 4; j++)
                                        pixels[j] = (decoded[i] >> ((3 - j) * 2)) & 0x3;

                                    for (int j = 0; j < 4; j++)
                                        if (i * 4 + j < hdr.Width)
                                            _bitmap.SetPixel(i * 4 + j, line, 
                                                Color.FromArgb(tPalette[pixels[j]], palette.Entries[pixels[j]]));
                                }
                            }
                            else if (bitDepth == 4)
                            {
                                for (int i = 0; i < Utils.IntCeilDiv(width, 2); i++)
                                {
                                    int pixel1 = decoded[i] >> 4; // upper four bits
                                    int pixel2 = decoded[i] & 0xf; // lower two bits

                                    _bitmap.SetPixel(i * 2, line, 
                                        Color.FromArgb(tPalette[pixel1], palette.Entries[pixel1]));

                                    if (i * 2 + 1 < hdr.Width)
                                        _bitmap.SetPixel(i * 2 + 1, line, 
                                            Color.FromArgb(tPalette[pixel2], palette.Entries[pixel2]));
                                }
                            }
                            else if (bitDepth == 8)
                            {
                                for (int i = 0; i < hdr.Width; i++)
                                    _bitmap.SetPixel(i, line, 
                                        Color.FromArgb(tPalette[decoded[i]], palette.Entries[decoded[i]]));
                            }

                            break;

                        case ColorType.Truecolor:
                            if (bitDepth == 8)
                            {
                                for (int i = 0; i < hdr.Width; i++)
                                {
                                    Color c = Color.FromArgb(
                                        decoded[i * 3],
                                        decoded[i * 3 + 1],
                                        decoded[i * 3 + 2]
                                        );

                                    _bitmap.SetPixel(i, line, Color.FromArgb(
                                        Utils.ColorsEqual(c, tRgb8) ? 0 : 255, c));
                                }
                            }
                            else if (bitDepth == 16)
                            {
                                // .NET doesn't support 16-bit bit depths
                                for (int i = 0; i < hdr.Width; i++)
                                {
                                    Color c = Color.FromArgb(
                                        Utils.BytesToUShort(decoded, i * 6, Utils.Endianness.Big) * 255 / 65535,
                                        Utils.BytesToUShort(decoded, i * 6 + 2, Utils.Endianness.Big) * 255 / 65535,
                                        Utils.BytesToUShort(decoded, i * 6 + 4, Utils.Endianness.Big) * 255 / 65535
                                        );

                                    _bitmap.SetPixel(i, line, Color.FromArgb(
                                        Utils.ColorsEqual(c, tRgb16) ? 0 : 255, c));
                                }
                            }

                            break;

                        case ColorType.TruecolorWithAlpha:
                            if (bitDepth == 8)
                            {
                                for (int i = 0; i < hdr.Width; i++)
                                    _bitmap.SetPixel(i, line, Color.FromArgb(
                                        decoded[i * 4 + 3],
                                        decoded[i * 4],
                                        decoded[i * 4 + 1],
                                        decoded[i * 4 + 2]));
                            }
                            else if (bitDepth == 16)
                            {
                                for (int i = 0; i < hdr.Width; i++)
                                    _bitmap.SetPixel(i, line, Color.FromArgb(
                                        Utils.BytesToUShort(decoded, i * 8 + 6, Utils.Endianness.Big) * 255 / 65535,
                                        Utils.BytesToUShort(decoded, i * 8, Utils.Endianness.Big) * 255 / 65535,
                                        Utils.BytesToUShort(decoded, i * 8 + 2, Utils.Endianness.Big) * 255 / 65535,
                                        Utils.BytesToUShort(decoded, i * 8 + 4, Utils.Endianness.Big) * 255 / 65535
                                        ));
                            }

                            break;

                        default:
                            throw new Exception("Invalid color type.");
                    }
                }

                #endregion
            }
        }

        /// <summary>
        /// Saves the PNG bitmap to the specified file.
        /// </summary>
        /// <param name="path">The file name.</param>
        public void Save(string path)
        {
            this.Save(path, FileMode.OpenOrCreate);
        }

        /// <summary>
        /// Saves the PNG bitmap to the specified file.
        /// </summary>
        /// <param name="path">The file name.</param>
        /// <param name="mode">The file mode.</param>
        public void Save(string path, FileMode mode)
        {
            FileStream fs = new FileStream(path, mode);

            this.Save(fs);

            fs.Close();
        }

        /// <summary>
        /// Saves the PNG image to a stream.
        /// </summary>
        /// <param name="s">The stream to write to.</param>
        public void Save(Stream s)
        {
            IHDRChunk hdr = _image.Chunks["IHDR"] as IHDRChunk;
            int width = (int)hdr.Width;
            int height = (int)hdr.Height;

            _scanlineLength = Utils.IntCeilDiv(Utils.GetBitsPerPixel(hdr.ColorType, hdr.BitDepth) * width, 8) + 1;
            _bpp = Utils.GetBytesPerPixel(hdr.ColorType, hdr.BitDepth); // bytes per pixel

            byte bitDepth = hdr.BitDepth;

            MemoryStream data = new MemoryStream();
            BinaryWriter bwdata = new BinaryWriter(data);
            byte[] scanline = null;

            for (int line = 0; line < height; line++)
            {
                byte[] prevLine = scanline;

                scanline = new byte[_scanlineLength - 1];

                switch (hdr.ColorType)
                {
                    case ColorType.Grayscale:
                        if (bitDepth == 1)
                        {
                            for (int i = 0; i < Utils.IntCeilDiv(width, 8); i++)
                            {
                                byte b = 0;

                                for (int j = 0; j < 8; j++)
                                {
                                    if (i * 8 + j < hdr.Width)
                                    {
                                        Color c = _bitmap.GetPixel(i * 8 + j, line);
                                        byte value = (byte)((c.R + c.G + c.B) / 3);

                                        b |= (byte)(((value / 255) >> (7 - j)) & 0x1);
                                    }
                                }

                                scanline[i] = b;
                            }
                        }
                        else if (bitDepth == 2)
                        {
                            for (int i = 0; i < Utils.IntCeilDiv(width, 4); i++)
                            {
                                byte b = 0;

                                for (int j = 0; j < 4; j++)
                                {
                                    if (i * 4 + j < hdr.Width)
                                    {
                                        Color c = _bitmap.GetPixel(i * 4 + j, line);
                                        byte value = (byte)((c.R + c.G + c.B) / 3);

                                        b |= (byte)(((value * 3 / 255) >> ((3 - j) * 2)) & 0x3);
                                    }
                                }

                                scanline[i] = b;
                            }
                        }
                        else if (bitDepth == 4)
                        {
                            for (int i = 0; i < Utils.IntCeilDiv(width, 2); i++)
                            {
                                byte b = 0;

                                Color c = _bitmap.GetPixel(i * 2, line);
                                byte value = (byte)((c.R + c.G + c.B) / 3);
                                
                                b |= (byte)((value * 15 / 255) << 4);

                                if (i * 2 + 1 < width)
                                {
                                    c = _bitmap.GetPixel(i * 2 + 1, line);
                                    value = (byte)((c.R + c.G + c.B) / 3);

                                    b |= (byte)(value * 15 / 255);
                                }
                                
                                scanline[i] = b;
                            }
                        }
                        else if (bitDepth == 8)
                        {
                            for (int i = 0; i < width; i++)
                            {
                                Color c = _bitmap.GetPixel(i, line);

                                scanline[i] = (byte)((c.R + c.G + c.B) / 3);
                            }
                        }
                        else if (bitDepth == 16)
                        {
                            for (int i = 0; i < width; i++)
                            {
                                Color c = _bitmap.GetPixel(i, line);
                                byte value = (byte)((c.R + c.G + c.B) / 3);

                                scanline[i * 2] = (byte)((value * 65535 / 255) >> 8);
                                scanline[i * 2 + 1] = (byte)((value * 65535 / 255) & 0xff);
                            }
                        }

                        break;

                    case ColorType.GrayscaleWithAlpha:
                        if (bitDepth == 8)
                        {
                            for (int i = 0; i < width; i++)
                            {
                                Color c = _bitmap.GetPixel(i, line);

                                scanline[i * 2] = (byte)((c.R + c.G + c.B) / 3);
                                scanline[i * 2 + 1] = c.A;
                            }
                        }
                        else if (bitDepth == 16)
                        {
                            for (int i = 0; i < width; i++)
                            {
                                Color c = _bitmap.GetPixel(i, line);
                                byte value = (byte)((c.R + c.G + c.B) / 3);

                                scanline[i * 4] = (byte)((value * 65535 / 255) >> 8);
                                scanline[i * 4 + 1] = (byte)((value * 65535 / 255) & 0xff);
                                scanline[i * 4 + 2] = (byte)((c.A * 65535 / 255) >> 8);
                                scanline[i * 4 + 3] = (byte)((c.A * 65535 / 255) & 0xff);
                            }
                        }

                        break;

                    case ColorType.IndexedColor:


                        break;

                    case ColorType.Truecolor:
                        if (bitDepth == 8)
                        {
                            for (int i = 0; i < width; i++)
                            {
                                Color c = _bitmap.GetPixel(i, line);

                                scanline[i * 3] = c.R;
                                scanline[i * 3 + 1] = c.G;
                                scanline[i * 3 + 2] = c.B;
                            }
                        }
                        else if (bitDepth == 16)
                        {
                            for (int i = 0; i < width; i++)
                            {
                                Color c = _bitmap.GetPixel(i, line);

                                scanline[i * 6] = (byte)((c.R * 65535 / 255) >> 8);
                                scanline[i * 6 + 1] = (byte)((c.R * 65535 / 255) & 0xff);
                                scanline[i * 6 + 2] = (byte)((c.G * 65535 / 255) >> 8);
                                scanline[i * 6 + 3] = (byte)((c.G * 65535 / 255) & 0xff);
                                scanline[i * 6 + 4] = (byte)((c.B * 65535 / 255) >> 8);
                                scanline[i * 6 + 5] = (byte)((c.B * 65535 / 255) & 0xff);
                            }
                        }

                        break;

                    case ColorType.TruecolorWithAlpha:
                        if (bitDepth == 8)
                        {
                            for (int i = 0; i < width; i++)
                            {
                                Color c = _bitmap.GetPixel(i, line);

                                scanline[i * 4] = c.R;
                                scanline[i * 4 + 1] = c.G;
                                scanline[i * 4 + 2] = c.B;
                                scanline[i * 4 + 3] = c.A;
                            }
                        }
                        else if (bitDepth == 16)
                        {
                            for (int i = 0; i < width; i++)
                            {
                                Color c = _bitmap.GetPixel(i, line);

                                scanline[i * 8] = (byte)((c.R * 65535 / 255) >> 8);
                                scanline[i * 8 + 1] = (byte)((c.R * 65535 / 255) & 0xff);
                                scanline[i * 8 + 2] = (byte)((c.G * 65535 / 255) >> 8);
                                scanline[i * 8 + 3] = (byte)((c.G * 65535 / 255) & 0xff);
                                scanline[i * 8 + 4] = (byte)((c.B * 65535 / 255) >> 8);
                                scanline[i * 8 + 5] = (byte)((c.B * 65535 / 255) & 0xff);
                                scanline[i * 8 + 6] = (byte)((c.A * 65535 / 255) >> 8);
                                scanline[i * 8 + 7] = (byte)((c.A * 65535 / 255) & 0xff);
                            } 
                        } 

                        break;

                    default:
                        throw new Exception("Invalid color type.");
                }

                byte[] filtered = this.Filter(scanline, line, this.GetBestFilterMethod(scanline, line, prevLine), prevLine);

                bwdata.Write(filtered);
            }

            this.SaveChunks(data.ToArray());
            _image.Write(s, true);

            bwdata.Close();
        }

        /// <summary>
        /// Modifies the underlying image's chunks to save the image.
        /// </summary>
        /// <param name="data">The uncompressed image data.</param>
        private void SaveChunks(byte[] data)
        {
            // remove all existing data chunks
            _image.Chunks.RemoveAll(new Predicate<Chunk>(
                delegate(Chunk c)
                {
                    if (c.Type == "IDAT") return true; else return false;
                }));

            // insert our new data chunk just before the end
            for (int i = 0; i < _image.Chunks.Count; i++)
            {
                if (_image.Chunks[i].Type == "IEND")
                {
                    IDATChunk idat = new IDATChunk(_image);
                    MemoryStream ms = new MemoryStream();
                    DeflaterOutputStream dos = new DeflaterOutputStream(ms, new Deflater(CompressionLevel.Level));
                    BinaryWriter bwdata = new BinaryWriter(dos);

                    bwdata.Write(data);
                    dos.Finish();
                    idat.Data = ms.ToArray();
                    bwdata.Close();
                    ms.Close();

                    _image.Chunks.Insert(i, idat);

                    break;
                }
            }
        }

        /// <summary>
        /// Gets the underlying PNG image.
        /// </summary>
        public PNGImage Image
        {
            get { return _image; }
        }

        /// <summary>
        /// Gets the bitmap.
        /// </summary>
        public Bitmap Bitmap
        {
            get { return _bitmap; }
        }
    }
}
