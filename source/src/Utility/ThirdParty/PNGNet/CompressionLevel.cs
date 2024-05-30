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

namespace PNGNet
{
	/// <summary>
	/// Provides methods for changing the global compression level.
	/// </summary>
	public sealed class CompressionLevel : IDisposable
    {
        #region Static Members

        /// <summary>
        /// Indicates that the compressor should store blocks uncompressed.
        /// </summary>
        public const int NoCompression = 0;

        /// <summary>
        /// Indicates that the compressor should compress at the fastest speed possible.
        /// </summary>
        public const int BestSpeed = 1;

        /// <summary>
        /// Indicates that the compressor should compress to the smallest size possible.
        /// </summary>
        public const int BestCompression = 9;

        private static int _level = 6;

        /// <summary>
        /// Gets or sets the compression level for compressing PNG data.
        /// </summary>
        public static int Level
        {
            get { return _level; }
            set
            {
                if (value < CompressionLevel.NoCompression)
                    throw new ArgumentException("Compression level must be equal to or above 0.");
                if (value > CompressionLevel.BestCompression)
                    throw new ArgumentException("Compression level must be equal to or below 9.");

                _level = value;
            }
        }

        #endregion

        #region Class

        private int _oldLevel;

        /// <summary>
        /// Temporarily sets the compression level.
        /// </summary>
        /// <param name="newLevel">The new level.</param>
        public CompressionLevel(int newLevel)
        {
            _oldLevel = CompressionLevel.Level;
            CompressionLevel.Level = newLevel;
        }

        #region IDisposable Members

        /// <summary>
        /// Sets the compression level to the original value.
        /// </summary>
        public void Dispose()
        {
            CompressionLevel.Level = _oldLevel;
        }

        #endregion

        #endregion
    }
}
