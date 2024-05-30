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
using System.Text;
using System.IO;

namespace PNGNet
{
	/// <summary>
	/// Indicates that a critical or required chunk was not found.
	/// </summary>
	public class ChunkNotFoundException : Exception
    {
        public ChunkNotFoundException(string message) : base(message) { }
    }

    /// <summary>
    /// Indicates that a chunk failed the CRC32 check.
    /// </summary>
    public class CorruptChunkDataException : Exception
    {
        public CorruptChunkDataException(string message) : base(message) { }
    }

    /// <summary>
    /// Indicates that the chunk is invalid for the current image.
    /// </summary>
    public class InvalidChunkException : Exception
    {
        public InvalidChunkException(string message) : base(message) { }
    }

    /// <summary>
    /// Indicates that at least one of the chunk's values is invalid.
    /// </summary>
    public class InvalidChunkDataException : Exception
    {
        public InvalidChunkDataException(string message) : base(message) { }
    }

    /// <summary>
    /// Indicates that the chunk has an invalid name - non-alphabetic characters or lengths other than 4.
    /// </summary>
    public class InvalidChunkNameException : Exception
    {
        public InvalidChunkNameException(string message) : base(message) { }
    }

    /// <summary>
    /// Indicates that a chunk's length is invalid.
    /// </summary>
    public class InvalidChunkLengthException : Exception
    {
       public InvalidChunkLengthException(string message) : base(message) { }
    }

    /// <summary>
    /// Represents a PNG chunk. This class can be used to represent a generic chunk or can be 
    /// inherited to provide more specific methods and properties.
    /// </summary>
    public class Chunk
    {
        private string _type;
        private PNGImage _image;
        private byte[] _data = null;

        /// <summary>
        /// Creates a new chunk with the specified type.
        /// </summary>
        /// <param name="type">The type of chunk.</param>
        /// <param name="image">The parent image.</param>
        public Chunk(string type, PNGImage image)
        {
            if (image == null)
                throw new ArgumentNullException("The image must not be null.");

            _image = image;

            this.AssertChunkType(type);
            _type = type;
        }

        /// <summary>
        /// Creates a new chunk with the specified data (which must include the chunk type in 
        /// the first four bytes).
        /// </summary>
        /// <param name="data">The chunk data.</param>
        /// <param name="image">The parent image.</param>
        public Chunk(byte[] data, PNGImage image)
        {
            if (image == null)
                throw new ArgumentNullException("The image must not be null.");

            _image = image;

            ByteStreamReader bs = new ByteStreamReader(data);

            _type = Utils.ReadString(bs, 4);
            this.AssertChunkType(_type);

            if (!image.Classes.ContainsKey(_type))
                _data = data;
        }

        /// <summary>
        /// Writes the chunk to the specified stream, including length, type, data (if any) and CRC32 hash.
        /// </summary>
        /// <param name="s">The stream to write to.</param>
        public void Write(Stream s)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bwms = new BinaryWriter(ms);
            BinaryWriter bws = new BinaryWriter(s);

            bwms.Write(ASCIIEncoding.ASCII.GetBytes(_type));
            this.WriteChunkData(ms);
            bwms.Write(
                Utils.UIntToBytes(CRC32.Hash(0, ms.GetBuffer(), 0, (int)ms.Length), 
                Utils.Endianness.Big));

            bws.Write(Utils.UIntToBytes((uint)(ms.Length - 8), Utils.Endianness.Big));
            ms.WriteTo(s);

            bwms.Close();
        }

        /// <summary>
        /// Writes the chunk data (excluding type) to specified memory stream. This method must be overridden 
        /// in derived classes in order to write specific chunk data. If this chunk was created as a generic 
        /// chunk, this method will write the data that was loaded.
        /// </summary>
        /// <param name="ms">The memory stream to write to.</param>
        protected virtual void WriteChunkData(MemoryStream ms)
        {
            if (_data != null)
            {   
                ms.Write(_data, 4, _data.Length - 4);
            }
        }

        /// <summary>
        /// Gets the chunk type.
        /// </summary>
        public string Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Gets the parent image.
        /// </summary>
        protected PNGImage Image
        {
            get { return _image; }
        }

        /// <summary>
        /// Validates the specified chunk type and throws an exception if it is invalid.
        /// </summary>
        /// <param name="type">The chunk type.</param>
        /// <exception cref="InvalidChunkNameException" />
        protected void AssertChunkType(string type)
        {              
            if (type.Length != 4)   
                throw new InvalidChunkNameException("Chunk name must have 4 characters.");

            foreach (char c in type)
                if (!char.IsLetter(c))
                    throw new InvalidChunkNameException("Chunk name must consist entirely of alphabetic characters.");
        }

        /// <summary>
        /// Ensures that the length of the specified data is equal to the specified length, 
        /// and throws an exception if it is not.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="length">The correct length, excluding type.</param>
        /// <exception cref="InvalidChunkLengthException" />
        protected void AssertDataLength(byte[] data, int length)
        {
            if (data.Length - 4 != length)
                throw new InvalidChunkLengthException(string.Format(
                    "Chunk length for {0} chunk is {1} but should be {2}.", _type, data.Length - 4, length));
        }

        /// <summary>
        /// Ensures that the specified value is present in the specified enumeration. 
        /// Otherwise, it throws an exception.
        /// </summary>
        /// <param name="e">The enumeration.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="InvalidChunkDataException" />
        protected void AssertEnumValue(Type e, object value)
        {
            if (!Enum.IsDefined(e, value))
                throw new InvalidChunkDataException(string.Format("Invalid value {0} for {1}", value, e.Name));
        }

        /// <summary>
        /// Ensures that the specified number only uses the lower 31 bits.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="name">The purpose of the number. For example, "Width".</param>
        /// <exception cref="InvalidChunkDataException" />
        protected void AssertNumber31Bits(uint number, string name)
        {           
            if ((number >> 31) != 0)
                throw new InvalidChunkDataException(name + " is over 31 bits long.");
        }

        /// <summary>
        /// Ensures that the specified string does not have any null characters.
        /// </summary>
        /// <param name="text">The string.</param>  
        /// <exception cref="InvalidChunkDataException" />
        protected void AssertText(string text)
        {
            for (int i = 0; i < text.Length; i++)
                if (text[i] == 0)
                    throw new InvalidChunkDataException("Text cannot have a NUL character.");
        }

        /// <summary>
        /// Gets the IHDR chunk of the parent image and throws an exception if it is not present.
        /// </summary>
        /// <returns>The IHDR chunk of the parent image.</returns>
        /// <exception cref="ChunkNotFoundException" />
        protected IHDRChunk GetAssertIHDRChunk()
        {
            IHDRChunk chunk = _image.Chunks["IHDR"] as IHDRChunk;

            if (chunk == null)
                throw new ChunkNotFoundException("IHDR chunk not found.");

            return chunk;
        }

        /// <summary>
        /// Gets the PLTE chunk of the parent image and throws an exception if it is not present.
        /// </summary>
        /// <returns>The PLTE chunk of the parent image.</returns>       
        /// <exception cref="ChunkNotFoundException" />
        protected PLTEChunk GetAssertPLTEChunk()
        {
            PLTEChunk chunk = _image.Chunks["PLTE"] as PLTEChunk;

            if (chunk == null)
                throw new ChunkNotFoundException("PLTE chunk not found.");

            return chunk;
        }

        #region Property Bits

        /// <summary>
        /// Gets whether the chunk is ancillary.
        /// </summary>
        public bool Ancillary
        {
            get { return char.IsLower(_type[0]); }
        }

        /// <summary>
        /// Gets whether the chunk is private.
        /// </summary>
        public bool Private
        {
            get { return char.IsLower(_type[1]); }
        }

        /// <summary>
        /// Gets whether the chunk is reserved.
        /// </summary>
        public bool Reserved
        {
            get { return char.IsLower(_type[2]); }
        }

        /// <summary>
        /// Gets whether the chunk is safe to copy.
        /// </summary>
        public bool SafeToCopy
        {
            get { return char.IsLower(_type[3]); }
        }

        #endregion
    }
}
