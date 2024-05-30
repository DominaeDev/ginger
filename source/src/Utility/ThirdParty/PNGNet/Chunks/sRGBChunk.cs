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
	/// <summary>
	/// Color rendering intent as defined by the International Color Consortium [ICC-1] and [ICC-1A].
	/// </summary>
	public enum RenderingIntent : byte
    {
        /// <summary>
        /// For images preferring good adaptation to the output device gamut at the expense of colorimetric accuracy, such as photographs.
        /// </summary>
        Perceptual = 0,

        /// <summary>
        /// For images requiring colour appearance matching (relative to the output device white point), such as logos.
        /// </summary>
        RelativeColorimetric,

        /// <summary>
        /// For images preferring preservation of saturation at the expense of hue and lightness, such as charts and graphs.
        /// </summary>
        Saturation,

        /// <summary>
        /// For images requiring preservation of absolute colorimetry, such as previews of images destined for a different output device (proofs).
        /// </summary>
        AbsoluteColorimetric
    }

    /// <summary>
    /// Specifies that the PNG image conforms to the sRGB color space [IEC 61966-2-1] and should be displayed using the specified rendering intent.
    /// </summary>
    [Chunk("sRGB", AllowMultiple = false)]
    public class sRGBChunk : Chunk
    {
        private RenderingIntent _ri;

        public sRGBChunk(PNGImage image)
            : base("sRGB", image)
        {
            _ri = RenderingIntent.Perceptual;
        }

        public sRGBChunk(byte[] data, PNGImage image)
            : base(data, image)
        {
            this.AssertDataLength(data, 1);

            this.RenderingIntent = (RenderingIntent)data[4];
        }

        protected override void WriteChunkData(MemoryStream ms)
        {
            ms.WriteByte((byte)this.RenderingIntent);
        }

        /// <summary>
        /// The color rendering intent as defined by the International Color Consortium [ICC-1] and [ICC-1A].
        /// </summary>
        public RenderingIntent RenderingIntent
        {
            get { return _ri; }
            set
            {
                this.AssertEnumValue(typeof(RenderingIntent), value);

                _ri = value;
            }
        }
    }
}
