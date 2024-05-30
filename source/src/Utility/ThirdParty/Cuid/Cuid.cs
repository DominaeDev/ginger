/* 
 * MIT License

   Copyright (c) 2023 Visus Development Team
   
   Permission is hereby granted, free of charge, to any person obtaining a copy
   of this software and associated documentation files (the "Software"), to deal
   in the Software without restriction, including without limitation the rights
   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
   copies of the Software, and to permit persons to whom the Software is
   furnished to do so, subject to the following conditions:
   
   The above copyright notice and this permission notice shall be included in all
   copies or substantial portions of the Software.
   
   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
   SOFTWARE.
*/

/*
	Heavily modified and stripped down by DominaeDev (2024) for compatibility 
	with .NET Framework 4. Removed everything except NewCuid()
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Ginger
{
	public readonly struct Cuid
	{
		/// <summary>
		///     A read-only instance of <see cref="Cuid" /> structure whose values are all zeros.
		/// </summary>
		public static readonly Cuid Empty;

		private const int Base = 36;
		private const int BlockSize = 4;
		private const string Prefix = "c";
		private static ulong _synchronizedCounter;

		/// <summary>
		///     Initializes a new instance of the <see cref="Cuid" /> structure.
		/// </summary>
		/// <returns>A new CUID object.</returns>
		public static string NewCuid()
		{
			string[] result = new string[5];

			result[0] = Prefix;
			result[1] = Encode((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
			result[2] = Pad(Encode(SafeCounter()), BlockSize);
			result[3] = Context.Fingerprint;
			result[4] = Pad(Encode(PseudoRandom()), BlockSize * 2);

			return string.Join(string.Empty, result);
		}

		private static string Encode(ulong value)
		{
			if (value is 0)
			{
				return "0";
			}

			Stack<char> result = new Stack<char>();

			while (value > 0)
			{
				ulong c = value % Base;
				result.Push((char)(c >= 0 && c <= 9 ? c + 48 : c + 'a' - 10));
				value /= Base;
			}

			return new string(result.ToArray());
		}

		private static string Pad(string value, int size)
		{
			string result = $"000000000{value}";

			return result.Substring(result.Length - size, size);
		}

		private static ulong SafeCounter()
		{
			_synchronizedCounter = _synchronizedCounter < Context.DiscreteValues ? _synchronizedCounter : 0;
			_synchronizedCounter++;
			return _synchronizedCounter - 1;
		}

		private static ulong PseudoRandom()
		{
			const int size = BlockSize * 2;

			var random = new RandomNoise();
			byte[] bytes = new byte[size];
			for (int i = 0; i < size; ++i)
				bytes[i] = random.Byte();

			if (BitConverter.IsLittleEndian)
				bytes = bytes.Reverse().ToArray();

			ulong item = BitConverter.ToUInt64(bytes, 0);
			item *= Context.DiscreteValues;

			return item;
		}

		private static class Context
		{
			public static readonly ulong DiscreteValues = (ulong)Math.Pow(Base, BlockSize);

			public static readonly string Fingerprint = GenerateFingerprint();

			private static string GenerateFingerprint()
			{
				string machineName = Environment.MachineName;
				int processIdentifier = System.Diagnostics.Process.GetCurrentProcess().Id;

				int machineIdentifier = machineName.Length + Base;
				machineIdentifier = machineName.Aggregate(machineIdentifier, (i, c) => i + c);

				string id = Pad(processIdentifier.ToString(CultureInfo.InvariantCulture), 2);
				string name = Pad(machineIdentifier.ToString(CultureInfo.InvariantCulture), 2);

				return $"{id}{name}";
			}
		}
	}
}