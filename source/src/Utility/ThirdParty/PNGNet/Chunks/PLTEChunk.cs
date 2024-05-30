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
	[Chunk("PLTE", AllowMultiple = false)]
    public class PLTEChunk : Chunk
    {
        private Color[] _entries;

        public PLTEChunk(PNGImage image)
            : base("PLTE", image)
        {
            _entries = new Color[0];
        }

        public PLTEChunk(byte[] data, PNGImage image)
            : base(data, image)
        {
            if (data.Length - 4 < 3)
                throw new InvalidChunkLengthException("There must be at least one palette entry.");
            if ((data.Length - 4) % 3 != 0)
                throw new InvalidChunkLengthException("Chunk length must be divisible by 3.");
            if ((data.Length - 4) > 3 * 256)
                throw new InvalidChunkLengthException("There must be 256 or less palette entries.");

            _entries = new Color[(data.Length - 4) / 3];

            for (int i = 0; i < (data.Length - 4) / 3; i++)
                _entries[i] = Color.FromArgb(
                    data[i * 3 + 4], data[i * 3 + 5], data[i * 3 + 6]);
        }

        protected override void WriteChunkData(MemoryStream ms)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                ms.WriteByte(_entries[i].R);
                ms.WriteByte(_entries[i].G);
                ms.WriteByte(_entries[i].B);
            }
        }

        public Color[] Entries
        {
            get { return _entries; }
            set { _entries = value; }
        }
    }
}
