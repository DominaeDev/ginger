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

using System.IO;

namespace PNGNet
{
	public struct SignificantBits
    {
        public SignificantBits(byte gr, byte a, byte r, byte g, byte b)
        {
            Gray = gr;
            Alpha = a;
            Red = r;
            Green = g;
            Blue = b;
        }

        public byte Gray;   
        public byte Alpha;
        public byte Red;
        public byte Green;
        public byte Blue;
    }

    [Chunk("sBIT", AllowMultiple = false)]
    public class sBITChunk : Chunk
    {
        private SignificantBits _sb;

        public sBITChunk(PNGImage image)
            : base("sBIT", image)
        {
            _sb = new SignificantBits();
        }

        public sBITChunk(byte[] data, PNGImage image)
            : base(data, image)
        {
            IHDRChunk hdr = this.GetAssertIHDRChunk();

            switch (hdr.ColorType)
            {
                case ColorType.Grayscale:
                    _sb = new SignificantBits(data[4], 0, 0, 0, 0);
                    break;

                case ColorType.Truecolor:
                case ColorType.IndexedColor:
                    _sb = new SignificantBits(0, 0, data[4], data[5], data[6]);
                    break;

                case ColorType.GrayscaleWithAlpha:
                    _sb = new SignificantBits(data[4], data[5], 0, 0, 0);
                    break;

                case ColorType.TruecolorWithAlpha:
                    _sb = new SignificantBits(0, data[7], data[4], data[5], data[6]);
                    break;

                default:
                    throw new InvalidChunkDataException("Invalid color type.");
            }
        }

        protected override void WriteChunkData(MemoryStream ms)
        {
            IHDRChunk hdr = this.GetAssertIHDRChunk();

            switch (hdr.ColorType)
            {
                case ColorType.Grayscale:
                    ms.WriteByte(_sb.Gray);
                    break;

                case ColorType.Truecolor:
                case ColorType.IndexedColor:
                    ms.WriteByte(_sb.Red);
                    ms.WriteByte(_sb.Green);
                    ms.WriteByte(_sb.Blue);
                    break;

                case ColorType.GrayscaleWithAlpha:
                    ms.WriteByte(_sb.Gray);
                    ms.WriteByte(_sb.Alpha);
                    break;

                case ColorType.TruecolorWithAlpha:
                    ms.WriteByte(_sb.Red);
                    ms.WriteByte(_sb.Green);
                    ms.WriteByte(_sb.Blue);
                    ms.WriteByte(_sb.Alpha);
                    break;

                default:
                    throw new InvalidChunkDataException("Invalid color type.");
            } 
        }

        public SignificantBits SignificantBits
        {
            get { return _sb; }
        }
    }
}
