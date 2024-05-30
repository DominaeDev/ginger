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
	/// <summary>
	/// Specifies primary chromaticities and white point
	/// </summary>
	[Chunk("cHRM", AllowMultiple = false)]
    public class cHRMChunk : Chunk
    {
        private float _whiteX, _whiteY, _redX, _redY, _greenX, _greenY, _blueX, _blueY;

        public cHRMChunk(PNGImage image) : base("cHRM", image) { }

        public cHRMChunk(byte[] data, PNGImage image)
            : base(data, image)
        {
            this.AssertDataLength(data, 32);

            _whiteX = ((float)Utils.BytesToUInt(data, 4, Utils.Endianness.Big)) / 100000;
            _whiteY = ((float)Utils.BytesToUInt(data, 8, Utils.Endianness.Big)) / 100000;
            _redX = ((float)Utils.BytesToUInt(data, 12, Utils.Endianness.Big)) / 100000;
            _redY = ((float)Utils.BytesToUInt(data, 16, Utils.Endianness.Big)) / 100000;
            _greenX = ((float)Utils.BytesToUInt(data, 20, Utils.Endianness.Big)) / 100000;
            _greenY = ((float)Utils.BytesToUInt(data, 24, Utils.Endianness.Big)) / 100000;
            _blueX = ((float)Utils.BytesToUInt(data, 28, Utils.Endianness.Big)) / 100000;
            _blueY = ((float)Utils.BytesToUInt(data, 32, Utils.Endianness.Big)) / 100000;
        }

        protected override void WriteChunkData(MemoryStream ms)
        {
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(Utils.UIntToBytes((uint)(_whiteX * 100000), Utils.Endianness.Big));
            bw.Write(Utils.UIntToBytes((uint)(_whiteY * 100000), Utils.Endianness.Big));
            bw.Write(Utils.UIntToBytes((uint)(_redX * 100000), Utils.Endianness.Big));
            bw.Write(Utils.UIntToBytes((uint)(_redY * 100000), Utils.Endianness.Big));
            bw.Write(Utils.UIntToBytes((uint)(_greenX * 100000), Utils.Endianness.Big));
            bw.Write(Utils.UIntToBytes((uint)(_greenY * 100000), Utils.Endianness.Big));
            bw.Write(Utils.UIntToBytes((uint)(_blueX * 100000), Utils.Endianness.Big));
            bw.Write(Utils.UIntToBytes((uint)(_blueY * 100000), Utils.Endianness.Big));
        }

        public float WhiteX
        {
            get { return _whiteX; }
            set { _whiteX = value; }
        }

        public float WhiteY
        {
            get { return _whiteY; }
            set { _whiteY = value; }
        }

        public float RedX
        {
            get { return _redX; }
            set { _redX = value; }
        }

        public float RedY
        {
            get { return _redY; }
            set { _redY = value; }
        }

        public float GreenX
        {
            get { return _greenX; }
            set { _greenX = value; }
        }

        public float GreenY
        {
            get { return _greenY; }
            set { _greenY = value; }
        }

        public float BlueX
        {
            get { return _blueX; }
            set { _blueX = value; }
        }

        public float BlueY
        {
            get { return _blueY; }
            set { _blueY = value; }
        }
    }
}
