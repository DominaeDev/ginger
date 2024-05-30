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
	[Chunk("tRNS", AllowMultiple = false)]
    public class tRNSChunk : Chunk
    {
        private ushort _grayValue;
        private ushort _redValue;
        private ushort _greenValue;
        private ushort _blueValue;
        private byte[] _paletteAlpha;

        public tRNSChunk(PNGImage image)
            : base("tRNS", image)
        {
            _paletteAlpha = new byte[0];
        }

        public tRNSChunk(byte[] data, PNGImage image)
            : base(data, image)
        {
            IHDRChunk hdr = this.GetAssertIHDRChunk();

            switch (hdr.ColorType)
            {
                case ColorType.Grayscale:
                    this.AssertDataLength(data, 2);
                    _grayValue = Utils.BytesToUShort(data, 4, Utils.Endianness.Big);
                    break;

                case ColorType.GrayscaleWithAlpha:
                    throw new InvalidChunkDataException("The tRNS chunk cannot appear for the grayscale with alpha color type.");

                case ColorType.Truecolor:
                    this.AssertDataLength(data, 6);
                    _redValue = Utils.BytesToUShort(data, 4, Utils.Endianness.Big);
                    _greenValue = Utils.BytesToUShort(data, 6, Utils.Endianness.Big);
                    _blueValue = Utils.BytesToUShort(data, 8, Utils.Endianness.Big);
                    break;

                case ColorType.TruecolorWithAlpha:
                    throw new InvalidChunkDataException("The tRNS chunk cannot appear for the truecolor with alpha color type.");

                case ColorType.IndexedColor:
                    if (data.Length - 4 > this.GetAssertPLTEChunk().Entries.Length)
                        throw new InvalidChunkLengthException("tRNS chunk must contain the same amount of entries as the palette chunk or less.");

                    _paletteAlpha = new byte[data.Length - 4];

                    for (int i = 0; i < data.Length - 4; i++)
                        _paletteAlpha[i] = data[i + 4];

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
                    bw.Write(Utils.UShortToBytes(_grayValue, Utils.Endianness.Big));
                    break;

                case ColorType.GrayscaleWithAlpha:
                    throw new InvalidChunkDataException("The tRNS chunk cannot appear for the grayscale with alpha color type.");

                case ColorType.Truecolor:
                    bw.Write(Utils.UShortToBytes(_redValue, Utils.Endianness.Big));
                    bw.Write(Utils.UShortToBytes(_greenValue, Utils.Endianness.Big));
                    bw.Write(Utils.UShortToBytes(_blueValue, Utils.Endianness.Big));
                    break;

                case ColorType.TruecolorWithAlpha:
                    throw new InvalidChunkDataException("The tRNS chunk cannot appear for the truecolor with alpha color type.");

                case ColorType.IndexedColor:
                    if (_paletteAlpha.Length > this.GetAssertPLTEChunk().Entries.Length)
                        throw new InvalidChunkLengthException("tRNS chunk must contain the same amount of entries as the palette chunk or less.");

                    bw.Write(_paletteAlpha);

                    break;

                default:
                    throw new InvalidChunkDataException("Invalid color type.");
            }  
        }

        public byte[] PaletteAlpha
        {
            get { return _paletteAlpha; }
            set { _paletteAlpha = value; }
        }

        public ushort Gray
        {
            get { return _grayValue; }
            set { _grayValue = value; }
        }

        public ushort Red
        {
            get { return _redValue; }
            set { _redValue = value; }
        }

        public ushort Green
        {
            get { return _greenValue; }
            set { _greenValue = value; }
        }

        public ushort Blue
        {
            get { return _blueValue; }
            set { _blueValue = value; }
        }
    }
}
