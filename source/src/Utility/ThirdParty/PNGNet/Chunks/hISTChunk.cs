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
	[Chunk("hIST", AllowMultiple = false)]
    public class hISTChunk : Chunk
    {
        private ushort[] _histogram;

        public hISTChunk(PNGImage image)
            : base("hIST", image)
        {
            _histogram = new ushort[this.GetAssertPLTEChunk().Entries.Length];
        }

        public hISTChunk(byte[] data, PNGImage image)
            : base(data, image)
        {
            if (data.Length - 4 != this.GetAssertPLTEChunk().Entries.Length * 2)
                throw new InvalidChunkLengthException("Chunk length must equal to the number of palette entries times 2.");

            _histogram = new ushort[(data.Length - 4) / 2];

            for (int i = 0; i < (data.Length - 4) / 2; i++)
                _histogram[i] = Utils.BytesToUShort(data, i * 2 + 4, Utils.Endianness.Big);
        }

        protected override void WriteChunkData(MemoryStream ms)
        {
            BinaryWriter bw = new BinaryWriter(ms);

            for (int i = 0; i < _histogram.Length; i++)
                bw.Write(Utils.UShortToBytes(_histogram[i], Utils.Endianness.Big));
        }

        public ushort[] Histogram
        {
            get { return _histogram; }
            set { _histogram = value; }
        }
    }
}
