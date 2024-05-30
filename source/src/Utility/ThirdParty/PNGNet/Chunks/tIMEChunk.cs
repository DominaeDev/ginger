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
	[Chunk("tIME", AllowMultiple = false)]
    public class tIMEChunk : Chunk
    {
        DateTime _time;

        public tIMEChunk(PNGImage image)
            : base("tIME", image)
        { }

        public tIMEChunk(byte[] data, PNGImage image)
            : base(data, image)
        {             
            this.AssertDataLength(data, 7);

            _time = new DateTime(
                Utils.BytesToUShort(data, 4, Utils.Endianness.Big),
                data[6],
                data[7],
                data[8],
                data[9],
                data[10]);
        }

        protected override void WriteChunkData(MemoryStream ms)
        {
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(Utils.UShortToBytes((ushort)_time.Year, Utils.Endianness.Big));
            bw.Write((byte)_time.Month);
            bw.Write((byte)_time.Day);
            bw.Write((byte)_time.Hour);
            bw.Write((byte)_time.Minute);
            bw.Write((byte)_time.Second);
        }

        public DateTime Time
        {
            get { return _time; }
            set { _time = value; }
        }
    }
}
