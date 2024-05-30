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
	public enum ColorType : byte
    {
        /// <summary>
        /// Grayscale
        /// </summary>
        Grayscale = 0,

        /// <summary>
        /// Truecolor
        /// </summary>
        Truecolor = 2,
        
        /// <summary>
        /// Indexed-color
        /// </summary>
        IndexedColor = 3,

        /// <summary>
        /// Grayscale with alpha
        /// </summary>
        GrayscaleWithAlpha = 4,

        /// <summary>
        /// Truecolor with alpha
        /// </summary>
        TruecolorWithAlpha = 6
    }

    public enum CompressionMethod : byte
    {
        Deflate = 0
    }

    public enum FilterMethod : byte
    {
        Adaptive = 0
    }

    public enum InterlaceMethod : byte
    {
        None = 0,
        Adam7 = 1
    }

    /// <summary>
    /// The first chunk in a PNG image, containing metadata such as image width, height, color type, compression method, filter method and interlace method.
    /// </summary>
    [Chunk("IHDR", AllowMultiple = false)]
    public class IHDRChunk : Chunk
    {
        private uint _width, _height;
        private byte _bitDepth;
        private ColorType _colorType;
        private CompressionMethod _compressionMethod;
        private FilterMethod _filterMethod;
        private InterlaceMethod _interlaceMethod;

        /// <summary>
        /// Initializes a new IHDR chunk with 1x1 resolution, truecolor with alpha color type, 
        /// deflate compression method, adaptive filter method and no interlacing.
        /// </summary>
        public IHDRChunk(PNGImage image)
            : base("IHDR", image)
        {
            this.Width = 1;
            this.Height = 1;
            this.ColorType = ColorType.TruecolorWithAlpha;
            this.BitDepth = 8;
            this.CompressionMethod = CompressionMethod.Deflate;
            this.FilterMethod = FilterMethod.Adaptive;
            this.InterlaceMethod = InterlaceMethod.None;
        }

        public IHDRChunk(byte[] data, PNGImage image)
            : base(data, image)
        {
            this.AssertDataLength(data, 13);

            this.Width = Utils.BytesToUInt(data, 4, Utils.Endianness.Big);
            this.Height = Utils.BytesToUInt(data, 8, Utils.Endianness.Big);
            this.ColorType = (ColorType)data[13];
            this.BitDepth = data[12];
            this.CompressionMethod = (CompressionMethod)data[14];
            this.FilterMethod = (FilterMethod)data[15];
            this.InterlaceMethod = (InterlaceMethod)data[16];
        }

        protected override void WriteChunkData(MemoryStream ms)
        {
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(Utils.UIntToBytes(this.Width, Utils.Endianness.Big));
            bw.Write(Utils.UIntToBytes(this.Height, Utils.Endianness.Big));
            bw.Write(this.BitDepth);
            bw.Write((byte)this.ColorType);
            bw.Write((byte)this.CompressionMethod);
            bw.Write((byte)this.FilterMethod);
            bw.Write((byte)this.InterlaceMethod);
        }

        public uint Width
        {
            get { return _width; }
            set
            {
                if (value == 0)
                    throw new InvalidChunkDataException("Width cannot be 0.");

                this.AssertNumber31Bits(value, "Width");

                _width = value;
            }
        }

        public uint Height
        {
            get { return _height; }
            set
            {
                if (value == 0)
                    throw new InvalidChunkDataException("Height cannot be 0.");

                this.AssertNumber31Bits(value, "Height");

                _height = value;
            }
        }

        public byte BitDepth
        {
            get { return _bitDepth; }

            set
            {
                if (value == 0)
                    throw new InvalidChunkDataException("Bit depth cannot be 0.");

                switch (this.ColorType)
                {
                    case ColorType.Grayscale:
                        // 1, 2, 4, 8, 16
                        if (value != 1 && value != 2 && value != 4 && value != 8 && value != 16)
                            throw new InvalidChunkDataException("Bit depth for grayscale color type must be 1, 2, 4, 8 or 16.");

                        break;

                    case ColorType.Truecolor:
                        // 8, 16
                        if (value != 8 && value != 16)
                            throw new InvalidChunkDataException("Bit depth for truecolor color type must be 8 or 16.");

                        break;

                    case ColorType.IndexedColor:
                        // 1, 2, 4, 8
                        if (value != 1 && value != 2 && value != 4 && value != 8)
                            throw new InvalidChunkDataException("Bit depth for indexed-color color type must be 1, 2, 4 or 8.");

                        break;

                    case ColorType.GrayscaleWithAlpha:
                        // 8, 16  
                        if (value != 8 && value != 16)
                            throw new InvalidChunkDataException("Bit depth for grayscale with alpha color type must be 8 or 16.");

                        break;

                    case ColorType.TruecolorWithAlpha:
                        // 8, 16  
                        if (value != 8 && value != 16)
                            throw new InvalidChunkDataException("Bit depth for truecolor with alpha color type must be 8 or 16.");

                        break;

                    default:
                        throw new InvalidChunkDataException("Invalid color type.");
                }

                _bitDepth = value;
            }
        }

        public ColorType ColorType
        {
            get { return _colorType; }

            set
            {
                this.AssertEnumValue(typeof(ColorType), value);

                _colorType = value;
            }
        }

        public CompressionMethod CompressionMethod
        {
            get { return _compressionMethod; }
            set
            {
                this.AssertEnumValue(typeof(CompressionMethod), value);

                _compressionMethod = value;
            }
        }

        public FilterMethod FilterMethod
        {
            get { return _filterMethod; }
            set
            {
                this.AssertEnumValue(typeof(FilterMethod), value);

                _filterMethod = value;
            }
        }

        public InterlaceMethod InterlaceMethod
        {
            get { return _interlaceMethod; }
            set
            {
                this.AssertEnumValue(typeof(InterlaceMethod), value);

                _interlaceMethod = value;
            }
        }
    }
}
