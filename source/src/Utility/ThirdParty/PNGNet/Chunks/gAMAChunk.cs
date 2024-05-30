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
	[Chunk("gAMA", AllowMultiple = false)]
    public class gAMAChunk : Chunk
    {
        private float _gamma;

        public gAMAChunk(PNGImage image)
            : base("gAMA", image)
        {
            _gamma = 1;
        }

        public gAMAChunk(byte[] data, PNGImage image)
            : base(data, image)
        {
            this.AssertDataLength(data, 4);

            this.Gamma = ((float)Utils.BytesToUInt(data, 4, Utils.Endianness.Big)) / 100000;
        }

        protected override void WriteChunkData(MemoryStream ms)
        {
            BinaryWriter bw = new BinaryWriter(ms);
                
            bw.Write(Utils.UIntToBytes((uint)(_gamma * 100000), Utils.Endianness.Big));
        }

        public float Gamma
        {
            get { return _gamma; }
            set
            {
                if (value == 0)
                    throw new InvalidChunkDataException("Gamma value cannot be 0.");

                _gamma = value;
            }
        }
    }
}
