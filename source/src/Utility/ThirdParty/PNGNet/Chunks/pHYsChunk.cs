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
	public enum PhysicalUnit : byte
    {
        Unknown = 0,
        Metre = 1
    }

    [Chunk("pHYs", AllowMultiple = false)]
    public class pHYsChunk : Chunk
    {
        private uint _ppuX, _ppuY;
        private PhysicalUnit _unit;

        public pHYsChunk(PNGImage image)
            : base("pHYs", image)
        { }

        public pHYsChunk(byte[] data, PNGImage image)
            : base(data, image)             
        {
            this.AssertDataLength(data, 9);

            this.PPUX = Utils.BytesToUInt(data, 4, Utils.Endianness.Big);
            this.PPUY = Utils.BytesToUInt(data, 8, Utils.Endianness.Big);
            this.Unit = (PhysicalUnit)data[9];
        }

        protected override void WriteChunkData(MemoryStream ms)
        {
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(Utils.UIntToBytes(this.PPUX, Utils.Endianness.Big));
            bw.Write(Utils.UIntToBytes(this.PPUY, Utils.Endianness.Big));
            bw.Write((byte)this.Unit);
        }

        public uint PPUX
        {
            get { return _ppuX; }
            set { _ppuX = value; }
        }

        public uint PPUY
        {
            get { return _ppuY; }
            set { _ppuY = value; }
        }

        public PhysicalUnit Unit
        {
            get { return _unit; }
            set
            {
                this.AssertEnumValue(typeof(PhysicalUnit), value);

                _unit = value;
            }
        }
    }
}
