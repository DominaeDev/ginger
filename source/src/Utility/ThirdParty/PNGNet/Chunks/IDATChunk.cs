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
using System.IO;

namespace PNGNet
{
	[Chunk("IDAT", AllowMultiple = true)]
    public class IDATChunk : Chunk
    {
        private byte[] _data = null;

        public IDATChunk(PNGImage image)
            : base("IDAT", image)
        { }

        public IDATChunk(byte[] data, PNGImage image)
            : base(data, image)
        {
            _data = new byte[data.Length - 4];
            Array.Copy(data, 4, _data, 0, data.Length - 4);
        }

        protected override void WriteChunkData(MemoryStream ms)
        {
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(_data);
        }
         
        public byte[] Data
        {
            get { return _data; }
            set { _data = value; }
        }
    }
}
