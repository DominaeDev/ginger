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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace PNGNet
{
	/// <summary>
	/// This exception indicates that the PNG image has an invalid header.
	/// </summary>
	public class InvalidHeaderException : Exception 
    {
        public InvalidHeaderException(string message) : base(message) { }
    }

    /// <summary>
    /// Indicates that the PNG image is invalid.
    /// </summary>
    public class InvalidImageException : Exception   
    {
        public InvalidImageException(string message) : base(message) { }
    }

    /// <summary>
    /// Indicates that the PNG image has invalid compressed data.
    /// </summary>
    public class InvalidCompressedDataException : Exception
    {
        public InvalidCompressedDataException(string message) : base(message) { }
    }

    /// <summary>
    /// Represents a PNG image and provides methods to read and write chunks.
    /// </summary>
    public sealed class PNGImage
    {
        [Flags]
        public enum PNGImageOptions
        {
            /// <summary>
            /// Specifies that errors in ancillary chunks should be fatal.
            /// </summary>
            AncillaryChunkErrors
        }

        public const uint MaxLength = 2147483647;
        public static PNGImageOptions Options;

        private byte[] _header = { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        private ChunkCollection _chunks = new ChunkCollection();
        private Dictionary<string, Type> _classes = new Dictionary<string, Type>();
        private bool _ccMod = false;

        /// <summary>
        /// Creates a PNG image with no chunks.
        /// </summary>
        public PNGImage() { }

        /// <summary>
        /// Creates a PNG image from a stream.
        /// </summary>
        /// <param name="s">The stream to read from.</param>
        public PNGImage(Stream s) : this(s, new Type[0]) { }

        /// <summary>
        /// Creates a PNG image from a stream.
        /// </summary>
        /// <param name="s">The stream to read from.</param>
        /// <param name="customHandlers">Any custom chunk classes you may wish to specify.</param>
        public PNGImage(Stream s, Type[] customHandlers)
        {
            this.LoadClasses(customHandlers);

            byte[] header = new byte[8];

            s.Read(header, 0, 8);

            if (!Utils.BytesEqual(header, _header))
                throw new InvalidHeaderException("Invalid PNG image header.");

            uint chunkNumber = 0;

            while (s.Position < s.Length)
            {
                uint goodcrc, crc32;
                int length;
                string type;
                byte[] data;

                length = Utils.ReadInt(s, Utils.Endianness.Big);

                // this decoder does NOT support chunk lengths greater than 2^31-5 because of limitations with uint.
                if (length > MaxLength - 4)
                    throw new InvalidChunkLengthException(
                        string.Format("Length {0} for chunk {1} exceeds 2^31-5.", length, chunkNumber));

                type = Utils.ReadString(s, 4);

                // terminate on critical unrecognized chunk
                if (char.IsUpper(type[0]) && !_classes.ContainsKey(type))
                    throw new InvalidChunkException(string.Format("Critical chunk {0} was not recognized.", type));

                data = new byte[length + 4];
                ASCIIEncoding.ASCII.GetBytes(type).CopyTo(data, 0);

                s.Read(data, 4, length);

                goodcrc = Utils.ReadUInt(s, Utils.Endianness.Big);
                crc32 = CRC32.Hash(0, data, 0, length + 4);

                if (goodcrc != crc32)
                    throw new CorruptChunkDataException(
                        string.Format("Invalid CRC32 (actual value is 0x{0:x8}, specified value is 0x{1:x8}) for {2} chunk.", crc32, goodcrc, type));

                Chunk newChunk = null;

                try
                {
                    newChunk = Activator.CreateInstance(GetChunkHandler(type), data, this) as Chunk;
                }
                catch (Exception ex)
                {
                    if (char.IsUpper(type[0]))
                        throw ex;
                    else if ((Options & PNGImageOptions.AncillaryChunkErrors) != 0)
                        throw ex;
                }

                _chunks.Add(newChunk);

                if (type == "IEND")
                    break;

                chunkNumber++;
            }

            this.Verify();
        }

        /// <summary>
        /// Checks the image's chunks for conformance to the PNG specification.
        /// </summary>
        public void Verify()
        {
            // verify the critical chunks
            // IHDR and IEND
            if (_chunks["IHDR"] == null)
                throw new InvalidImageException("Critical chunk IHDR was not found.");
            if (_chunks[0].Type != "IHDR")
                throw new InvalidImageException("IHDR chunk must appear first.");

            if (_chunks["IEND"] == null)
                throw new InvalidImageException("Critical chunk IEND was not found.");
            if (_chunks[_chunks.Count - 1].Type != "IEND")
                throw new InvalidImageException("IEND chunk must appear last.");

            // PLTE
            IHDRChunk hdr = _chunks["IHDR"] as IHDRChunk;

            if (hdr.ColorType == ColorType.IndexedColor)
                if (_chunks["PLTE"] == null)
                    throw new InvalidImageException(
                        "Critical chunk PLTE required for color type 3 (indexed color) was not found.");

            if (hdr.ColorType == ColorType.Grayscale || hdr.ColorType == ColorType.GrayscaleWithAlpha)
                if (_chunks["PLTE"] != null)
                    throw new InvalidImageException(
                        "PLTE chunk cannot appear for color types 0 and 4 (grayscale and grayscale with alpha).");

            // IDAT
            if (_chunks["IDAT"] == null)
                throw new InvalidImageException("Critical chunk IDAT was not found.");

            // verify chunk counts
            Dictionary<Type, int> counts = new Dictionary<Type, int>();

            foreach (Chunk c in _chunks)
                if (!counts.ContainsKey(c.GetType()))
                    counts.Add(c.GetType(), 1);
                else
                    counts[c.GetType()]++;

            foreach (Type t in counts.Keys)
            {
                if (_classes.ContainsValue(t))
                {
                    ChunkAttribute a = t.GetCustomAttributes(typeof(ChunkAttribute), true)[0] as ChunkAttribute;

                    if (a.AllowMultiple == false && counts[t] > 1)
                        throw new InvalidImageException(
                            string.Format("Multiple instances ({0}) of {1} chunk are not allowed.", counts[t], a.Type));
                }
            }

            // verify ancillary chunks
            if (_chunks["iCCP"] != null && _chunks["sRGB"] != null)
                throw new InvalidImageException("iCCP chunk and sRGB chunk cannot be both present.");

            // verify chunk ordering
            int plteLoc, idatLoc;

            plteLoc = _chunks.IndexOf("PLTE");
            idatLoc = _chunks.IndexOf("IDAT");

            // verify that IDAT chunks are consecutive
            bool idatEnded = false;

            for (int i = idatLoc; i < _chunks.Count; i++)
            {
                if (_chunks[i].Type == "IDAT" && idatEnded)
                    throw new InvalidImageException("IDAT chunks must be consecutive.");
                else if (_chunks[i].Type != "IDAT" && !idatEnded)
                    idatEnded = true;
            }

            // PLTE must be before IDAT
            if (plteLoc >= idatLoc)
                throw new InvalidImageException("PLTE chunk must be before first IDAT chunk.");

            // verify other chunks' ordering
            string[] beforePLTEbeforeIDAT = new string[] { "cHRM", "gAMA", "iCCP", "sBIT", "sRGB" };
            string[] afterPLTEbeforeIDAT = new string[] { "bKGD", "hIST", "tRNS" };
            string[] beforeIDAT = new string[] { "pHYs", "sPLT" };

            for (int i = 0; i < _chunks.Count; i++)
            {
                if (Utils.ArrayContains<string>(beforePLTEbeforeIDAT, _chunks[i].Type))
                {
                    if (((plteLoc == -1) ? false : (i >= plteLoc)) || i >= idatLoc)
                        throw new InvalidImageException(
                            string.Format("{0} chunk must be before PLTE chunk and first IDAT chunk.", _chunks[i].Type));
                }

                if (Utils.ArrayContains<string>(afterPLTEbeforeIDAT, _chunks[i].Type))
                {
                    if (((plteLoc == -1) ? false : (i <= plteLoc)) || i >= idatLoc)
                        throw new InvalidImageException(
                            string.Format("{0} must be after PLTE chunk and before first IDAT chunk.", _chunks[i].Type));
                }

                if (Utils.ArrayContains<string>(beforeIDAT, _chunks[i].Type))
                {
                    if (i >= idatLoc)
                        throw new InvalidImageException(
                            string.Format("{0} must be before first IDAT chunk.", _chunks[i].Type));
                }
            }
        }

        /// <summary>
        /// Writes the PNG image to a file.
        /// </summary>
        /// <param name="path">The filename.</param>
        public void Write(string path)
        {
            Write(path, FileMode.OpenOrCreate);
        }

        /// <summary>
        /// Writes the PNG image to a file.
        /// </summary>
        /// <param name="path">The filename.</param>
        /// <param name="mode">The file mode.</param>
        public void Write(string path, FileMode mode)
        {
            FileStream fs = new FileStream(path, mode);

            Write(fs, true);
            fs.Close();
        }

        /// <summary>
        /// Writes the PNG image to a stream.
        /// </summary>
        /// <param name="s">The stream to which the image is written.</param>
        public void Write(Stream s, bool verify)
        {
            if (verify)
                this.Verify();

            BinaryWriter bw = new BinaryWriter(s);

            bw.Write(_header);

            foreach (Chunk c in _chunks)
                if (!(c.Private && this.CriticalChunksModified))
                    c.Write(s);

            this.CriticalChunksModified = false;
        }
                                  
        /// <summary>
        /// Gets a copy of the standard PNG file header.
        /// </summary>
        public byte[] Header
        {
            get { return (byte[])_header.Clone(); }
        }

        /// <summary>
        /// Gets the chunk handler classes.
        /// </summary>
        internal Dictionary<string, Type> Classes
        {
            get { return _classes; }
        }

        /// <summary>
        /// Specifies whether any critial chunks have been modified. If this property is set to true, 
        /// any unrecognized chunks will not be saved.
        /// </summary>
        public bool CriticalChunksModified
        {
            get { return _ccMod; }
            set { _ccMod = value; }
        }
                   
        /// <summary>
        /// Gets the chunk handler class for the specified type.
        /// </summary>
        /// <param name="type">The PNG chunk type.</param>
        /// <returns>The handler class.</returns>
        public Type GetChunkHandler(string type)
        {
            Type classType = typeof(Chunk);

            if (_classes.ContainsKey(type))
                classType = _classes[type];

            return classType;
        }

        /// <summary>
        /// Loads the list of chunk handler classes from both this assembly and any specified handlers.
        /// </summary>
        /// <param name="customHandlers">The list of custom chunk handlers.</param>
        private void LoadClasses(Type[] customHandlers)
        {
            List<Type> types = new List<Type>();

            foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
                types.Add(t);

            foreach (Type t in customHandlers)
                types.Add(t);

            foreach (Type t in types)
            {
                object[] attributes = t.GetCustomAttributes(typeof(ChunkAttribute), false);

                foreach (ChunkAttribute a in attributes)
                {
                    _classes.Add(a.Type, t);
                }
            }
        }

        /// <summary>
        /// Gets the list chunks in this PNG image.
        /// </summary>
        public ChunkCollection Chunks
        {
            get { return _chunks; }
        }
    }
}
