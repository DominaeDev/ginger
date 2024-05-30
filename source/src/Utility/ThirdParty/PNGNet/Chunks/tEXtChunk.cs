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
using System.Text;

namespace PNGNet
{
	[Chunk("tEXt", AllowMultiple = true)]
    public class tEXtChunk : Chunk
    {
        private string _keyword;
        private string _text;

        public tEXtChunk(PNGImage image)
            : base("tEXt", image)
        { }

        public tEXtChunk(byte[] data, PNGImage image)
            : base(data, image)
        {
            int i = 0;

            while (data[i + 4] != 0)
                i++;

            this.Keyword = ASCIIEncoding.ASCII.GetString(data, 4, i);
            this.Text = ASCIIEncoding.ASCII.GetString(data, i + 5, data.Length - 4 - i - 1);
        }

        protected override void WriteChunkData(MemoryStream ms)
        {
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(ASCIIEncoding.ASCII.GetBytes(this.Keyword));
            bw.Write((byte)0);
            bw.Write(ASCIIEncoding.ASCII.GetBytes(this.Text));
        }

        public string Keyword
        {
            get { return _keyword; }
            set
            {
                if (value.Length > 79)
                    throw new InvalidChunkDataException("Keyword string must be less than 80 characters long.");

                _keyword = value;
            }
        }

        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }
    }
}
