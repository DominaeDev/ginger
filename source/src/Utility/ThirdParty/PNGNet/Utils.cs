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
using System.Drawing;

namespace PNGNet
{
	public static class Utils
    {
        public enum Endianness
        {
            Little, Big
        }

        public static bool ArrayContains<T>(T[] array, T element)
        {
            foreach (T e in array)
                if (e.Equals(element))
                    return true;

            return false;
        }

        public static bool BytesEqual(byte[] b1, byte[] b2)
        {
            for (int i = 0; i < b1.Length; i++)
                if (b1[i] != b2[i])
                    return false;

            return true;
        }

        public static int BytesToInt(byte[] data, Endianness type)
        {
            if (type == Endianness.Little)
            {
                return (data[0]) | (data[1] << 8) | (data[2] << 16) | (data[3] << 24);
            }
            else if (type == Endianness.Big)
            {
                return (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | (data[3]);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public static uint BytesToUInt(byte[] data, Endianness type) 
        {
            return BytesToUInt(data, 0, type);
        }

        public static uint BytesToUInt(byte[] data, int offset, Endianness type)
        {
            if (type == Endianness.Little)
            {
                return (uint)(data[offset]) | (uint)(data[offset + 1] << 8) |
                    (uint)(data[offset + 2] << 16) | (uint)(data[offset + 3] << 24);
            }
            else if (type == Endianness.Big)
            {
                return (uint)(data[offset] << 24) | (uint)(data[offset + 1] << 16) |
                    (uint)(data[offset + 2] << 8) | (uint)(data[offset + 3]);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public static ushort BytesToUShort(byte[] data, Endianness type)
        {
            return BytesToUShort(data, 0, type);
        }

        public static ushort BytesToUShort(byte[] data, int offset, Endianness type)
        {
            if (type == Endianness.Little)
            {
                return (ushort)(data[offset] | (data[offset + 1] << 8));
            }
            else if (type == Endianness.Big)
            {
                return (ushort)((data[offset] << 8) | data[offset + 1]);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public static bool ColorsEqual(Color a, Color b)
        {
            return (a.R == b.R) && (a.G == b.G) && (a.B == b.B) && (a.A == b.A);
        }

        public static int IntCeilDiv(int a, int b)
        {
            return (int)Math.Ceiling(((double)a / b));
        }

        public static byte[] IntToBytes(int n, Endianness type)
        {
            byte[] data = new byte[4];

            if (type == Endianness.Little)
            {
                data[0] = (byte)(n & 0xff);
                data[1] = (byte)((n >> 8) & 0xff);
                data[2] = (byte)((n >> 16) & 0xff);
                data[3] = (byte)((n >> 24) & 0xff);
            }
            else if (type == Endianness.Big)
            {
                data[0] = (byte)((n >> 24) & 0xff);
                data[1] = (byte)((n >> 16) & 0xff);
                data[2] = (byte)((n >> 8) & 0xff);
                data[3] = (byte)(n & 0xff);
            }
            else
            {
                throw new ArgumentException();
            }

            return data;
        }

        public static int GetBitsPerPixel(ColorType ct, int bpp)
        {
            switch (ct)
            {
                case ColorType.Grayscale:
                    return bpp;
                case ColorType.GrayscaleWithAlpha:
                    return bpp * 2;
                case ColorType.IndexedColor:
                    return bpp;
                case ColorType.Truecolor:
                    return bpp * 3;
                case ColorType.TruecolorWithAlpha:
                    return bpp * 4;
                default:
                    throw new Exception("Invalid color type.");
            }
        }

        public static int GetBytesPerPixel(ColorType ct, int bpp)
        {
            return GetBitsPerPixel(ct, bpp) / 8;
        }

        public static Color MakeGray(int value)
        {
            return Color.FromArgb(value, value, value);
        }

        public static Color MakeGray(int alpha, int value)
        {
            return Color.FromArgb(alpha, value, value, value);
        }

        public static byte[] ReverseBytes(byte[] data)
        {
            byte[] newdata = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
                newdata[i] = data[data.Length - i - 1];

            return newdata;
        }

        public static uint ReverseEndian(uint n)
        {
            uint b0 = n & 0xff;
            uint b1 = (n >> 8) & 0xff;
            uint b2 = (n >> 16) & 0xff;
            uint b3 = (n >> 24) & 0xff;

            b0 <<= 24;
            b1 <<= 16;
            b2 <<= 8;

            return b0 | b1 | b2 | b3;
        }

        public static int ReadInt(Stream s, Utils.Endianness type)
        {
            byte[] buffer = new byte[4];

            if (s.Read(buffer, 0, 4) == 0)
                throw new EndOfStreamException();

            return BytesToInt(buffer, type);
        }

        public static string ReadString(Stream s, int length)
        {
            byte[] buffer = new byte[length];

            if (s.Read(buffer, 0, length) == 0)
                throw new EndOfStreamException();

            return System.Text.ASCIIEncoding.ASCII.GetString(buffer);
        }

        public static uint ReadUInt(Stream s, Utils.Endianness type)
        {
            byte[] buffer = new byte[4];

            if (s.Read(buffer, 0, 4) == 0)
                throw new EndOfStreamException();

            return BytesToUInt(buffer, type);
        }

        public static byte[] UIntToBytes(uint n, Endianness type)
        {
            byte[] data = new byte[4];

            if (type == Endianness.Little)
            {
                data[0] = (byte)(n & 0xff);
                data[1] = (byte)((n >> 8) & 0xff);
                data[2] = (byte)((n >> 16) & 0xff);
                data[3] = (byte)((n >> 24) & 0xff);
            }
            else if (type == Endianness.Big)
            {
                data[0] = (byte)((n >> 24) & 0xff);
                data[1] = (byte)((n >> 16) & 0xff);
                data[2] = (byte)((n >> 8) & 0xff);
                data[3] = (byte)(n & 0xff);
            }
            else
            {
                throw new ArgumentException();
            }

            return data;
        }

        public static byte[] UShortToBytes(ushort n, Endianness type)
        {
            byte[] data = new byte[2];

            if (type == Endianness.Little)
            {
                data[0] = (byte)(n & 0xff);
                data[1] = (byte)((n >> 8) & 0xff);
            }
            else if (type == Endianness.Big)
            {
                data[0] = (byte)((n >> 8) & 0xff);
                data[1] = (byte)(n & 0xff);
            }
            else
            {
                throw new ArgumentException();
            }

            return data;  
        }
    }
}
