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

using System.Drawing;
using System.IO;

namespace PNGNet
{
	public struct BackgroundColor
    {
        public BackgroundColor(ushort gr, ushort r, ushort g, ushort b, byte index)
        {
            Gray = gr;
            Red = r;
            Green = g;
            Blue = b;
            Index = index;
        }

        public ushort Gray;
        public ushort Red;
        public ushort Green;
        public ushort Blue;
        public byte Index;
    }

    [Chunk("bKGD", AllowMultiple = false)]
    public class bKGDChunk : Chunk
    {
        private BackgroundColor _backgroundColor;

        public bKGDChunk(PNGImage image)
            : base("bKGD", image)
        {
            _backgroundColor = new BackgroundColor();
        }

        public bKGDChunk(byte[] data, PNGImage image)
            : base(data, image)
        {
            IHDRChunk hdr = this.GetAssertIHDRChunk();

            switch (hdr.ColorType)
            {
                case ColorType.Grayscale:
                case ColorType.GrayscaleWithAlpha:
                    this.AssertDataLength(data, 2);
                    _backgroundColor = new BackgroundColor(
                        Utils.BytesToUShort(data, 4, Utils.Endianness.Big), 0, 0, 0, 0);
                    break;

                case ColorType.Truecolor:
                case ColorType.TruecolorWithAlpha:
                    this.AssertDataLength(data, 6);
                    _backgroundColor = new BackgroundColor(0,
                        Utils.BytesToUShort(data, 4, Utils.Endianness.Big),
                        Utils.BytesToUShort(data, 6, Utils.Endianness.Big),
                        Utils.BytesToUShort(data, 8, Utils.Endianness.Big), 0);
                    break;

                case ColorType.IndexedColor:
                    this.AssertDataLength(data, 1);
                    _backgroundColor = new BackgroundColor(0, 0, 0, 0, data[4]);
                    break;

                default:
                    throw new InvalidChunkDataException("Invalid color type.");
            }  
        }

        protected override void WriteChunkData(MemoryStream ms)
        {
            IHDRChunk hdr = this.GetAssertIHDRChunk();
            BinaryWriter bw = new BinaryWriter(ms);

            switch (hdr.ColorType)
            {
                case ColorType.Grayscale:
                case ColorType.GrayscaleWithAlpha:
                    bw.Write(Utils.UShortToBytes(_backgroundColor.Gray, Utils.Endianness.Big));
                    break;

                case ColorType.Truecolor:
                case ColorType.TruecolorWithAlpha:
                    bw.Write(Utils.UShortToBytes(_backgroundColor.Red, Utils.Endianness.Big));
                    bw.Write(Utils.UShortToBytes(_backgroundColor.Green, Utils.Endianness.Big));
                    bw.Write(Utils.UShortToBytes(_backgroundColor.Blue, Utils.Endianness.Big));
                    break;

                case ColorType.IndexedColor:
                    bw.Write(_backgroundColor.Index);
                    break;

                default:
                    throw new InvalidChunkDataException("Invalid color type.");
            }  
        }

        public Color GetEffectiveBackgroundColor()
        {
            IHDRChunk hdr = this.GetAssertIHDRChunk();

            switch (hdr.ColorType)
            {
                case ColorType.Grayscale:
                case ColorType.GrayscaleWithAlpha:
                    if (hdr.BitDepth == 1)
                        return Utils.MakeGray((_backgroundColor.Gray & 0x1) * 255);
                    else if (hdr.BitDepth == 2)
                        return Utils.MakeGray((_backgroundColor.Gray & 0x3) * 255 / 3);
                    else if (hdr.BitDepth == 4)
                        return Utils.MakeGray((_backgroundColor.Gray & 0xf) * 255 / 15);
                    else if (hdr.BitDepth == 8)
                        return Utils.MakeGray(_backgroundColor.Gray & 0xff);
                    else if (hdr.BitDepth == 16)
                        return Utils.MakeGray(_backgroundColor.Gray * 255 / 65535);
                    else
                        throw new InvalidChunkDataException("Invalid bit depth.");

                case ColorType.Truecolor:
                case ColorType.TruecolorWithAlpha:
                    if (hdr.BitDepth == 8)
                        return Color.FromArgb(
                            _backgroundColor.Red & 0xff,
                            _backgroundColor.Green & 0xff,
                            _backgroundColor.Blue & 0xff);
                    else if (hdr.BitDepth == 16)
                        return Color.FromArgb(
                            _backgroundColor.Red * 255 / 65535,
                            _backgroundColor.Green * 255 / 65535,
                            _backgroundColor.Blue * 255 / 65535);
                    else
                        throw new InvalidChunkDataException("Invalid bit depth.");

                case ColorType.IndexedColor:
                    PLTEChunk plte = this.GetAssertPLTEChunk();

                    return plte.Entries[_backgroundColor.Index];

                default:
                    throw new InvalidChunkDataException("Invalid color type.");
            }     
        }

        public BackgroundColor BackgroundColor
        {
            get { return _backgroundColor; }
        }
    }
}
