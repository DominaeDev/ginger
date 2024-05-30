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
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace PNGNet
{
	[Chunk("zTXt", AllowMultiple = true)]
    public class zTXtChunk : Chunk
    {
        private string _keyword;
        private CompressionMethod _compressionMethod;
        private string _text;

        public zTXtChunk(PNGImage image)
            : base("zTXt", image)
        { }

        public zTXtChunk(byte[] data, PNGImage image)
            : base(data, image)
        {
            int i = 0;

            while (data[i + 4] != 0)
                i++;

            this.Keyword = ASCIIEncoding.ASCII.GetString(data, 4, i);
            this.CompressionMethod = (CompressionMethod)data[i + 5];

            ByteStreamReader bs = new ByteStreamReader(data);

            bs.Seek(i + 6, SeekOrigin.Begin);
                      
            InflaterInputStream iis = new InflaterInputStream(bs, new Inflater(), data.Length - 4 - i - 2);
            StringBuilder sb = new StringBuilder();

            while (iis.Available == 1)
            {
                int b = iis.ReadByte();

                if (b == -1)
                    break;
                
                sb.Append(System.Text.ASCIIEncoding.ASCII.GetChars(new byte[] { (byte)b })[0]);
            }

            this.Text = sb.ToString();

            iis.Close();
            bs.Close();
        }

        protected override void WriteChunkData(MemoryStream ms)
        {
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(ASCIIEncoding.ASCII.GetBytes(this.Keyword));
            bw.Write((byte)0);
            bw.Write((byte)this.CompressionMethod);

            DeflaterOutputStream dos = new DeflaterOutputStream(ms, new Deflater(CompressionLevel.Level));
            BinaryWriter dosBw = new BinaryWriter(dos);

            dosBw.Write(ASCIIEncoding.ASCII.GetBytes(this.Text));
            dos.Finish();
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

        public CompressionMethod CompressionMethod
        {
            get { return _compressionMethod; }
            set
            {
                this.AssertEnumValue(typeof(CompressionMethod), value);

                _compressionMethod = value;
            }
        }

        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }
    }
}
