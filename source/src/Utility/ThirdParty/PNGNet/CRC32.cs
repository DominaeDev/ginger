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

namespace PNGNet
{
	/// <summary>
	/// Provides functions to calculate CRC32 hashes of data.
	/// </summary>
	public static class CRC32
    {
        private const uint Polynomial = 0xedb88320;
        private static uint[] Table;

        /// <summary>
        /// Initializes the CRC32 table.
        /// </summary>
        public static void Initialize()
        {
            uint h = 1;

            Table = new uint[256];
            Table[0] = 0;
            
            for (int i = 128; i != 0; i >>= 1)
            {
                h = (h >> 1) ^ (((h & 1) != 0) ? Polynomial : 0);
                
                for (int j = 0; j < 256; j += 2 * i)
                    Table[i + j] = Table[j] ^ h;
            }
        }

        /// <summary>
        /// Performs CRC32 on a sequence of bytes. Initializes the table if not already intialized.
        /// </summary>
        /// <param name="crc32">Specify the value of the previous hash, or specify 0.</param>
        /// <param name="data">The data to be hashed.</param>
        /// <returns></returns>
        public static uint Hash(uint crc32, byte[] data, int offset, int length)
        {
            if (Table == null)
                Initialize();

            crc32 ^= 0xffffffff;

            for (int i = offset; i < offset + length; i++)
                crc32 = (crc32 >> 8) ^ Table[(crc32 ^ data[i]) & 0xff];

            return crc32 ^ 0xffffffff;
        }
    }
}
