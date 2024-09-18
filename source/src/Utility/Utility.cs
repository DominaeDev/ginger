using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Ginger
{
	public static class Utility 
	{
		public static void Swap<T>(ref T a, ref T b)
		{
			T tmp = a;
			a = b;
			b = tmp;
		}

		public static byte[] LoadFile(string filename)
		{
			byte[] bytes = null;
			FileStream fs;
			try
			{
				fs = File.OpenRead(filename);
				bytes = new byte[fs.Length];
				fs.Read(bytes, 0, (int)fs.Length);
				fs.Close();
			}
			catch
			{
				return null;
			}

			return bytes;
		}


		public static string LoadTextFile(string filename)
		{
			byte[] bytes = LoadFile(filename);
			if (bytes != null)
			{
				try
				{
					return Encoding.UTF8.GetString(bytes);
				}
				catch
				{
					return null;
				}
			}
			return null;
		}

		public static bool LoadImageFromFile(string filename, out Image image)
		{
			if (string.IsNullOrEmpty(filename) || File.Exists(filename) == false)
			{
				image = default(Image);
				return false;
			}

			// Load image first
			try
			{
				byte[] bytes = File.ReadAllBytes(filename);
				using (var stream = new MemoryStream(bytes))
				{
					image = Image.FromStream(stream);
					return true;
				}
			}
			catch
			{
				image = default(Image);
				return false;
			}
		}		
		
		public static bool LoadImageFromMemory(byte[] bytes, out Image image)
		{
			if (bytes == null || bytes.Length == 0)
			{
				image = default(Image);
				return false;
			}

			// Load image first
			try
			{
				using (var stream = new MemoryStream(bytes))
				{
					image = Image.FromStream(stream);
					return true;
				}
			}
			catch
			{
				image = default(Image);
				return false;
			}
		}

		public static bool GetImageDimensions(byte[] bytes, out int width, out int height)
		{
			if (bytes == null || bytes.Length == 0)
			{
				width = default(int);
				height = default(int);
				return false;
			}

			// Load image first
			try
			{
				using (var stream = new MemoryStream(bytes))
				{
					var image = Image.FromStream(stream, false, false);
					width = image.Width;
					height = image.Height;
					return true;
				}
			}
			catch
			{
				width = default(int);
				height = default(int);
				return false;
			}
		}

		public static int StringToInt(string s, int default_value = 0)
		{
			int value;
			if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
				return value;
			return default_value;
		}

		public static bool StringToInt(string s, out int value)
		{
			if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
				return true;
			return false;
		}

		public static int StringToInt(string s, int min, int max, int default_value = 0)
		{
			int value;
			if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
				return Math.Min(Math.Max(value, min), max);
			return default_value;
		}

		public static float StringToFloat(string s, float default_value = 0.0f)
		{
			return Convert.ToSingle(StringToDouble(s, default_value));
		}

		public static float StringToFloat(string s, float min, float max, float default_value = 0.0f)
		{
			return Math.Min(Math.Max(Convert.ToSingle(StringToDouble(s, default_value)), min), max);
		}

		public static bool StringToFloat(string s, out float value)
		{
			int pos = 0;
			for (; pos < s.Length; ++pos)
			{
				if (char.IsNumber(s[pos]))
					continue;
				if (s[pos] == '+' || s[pos] == '-' || s[pos] == '.')
					continue;
				break;
			}
			if (pos < s.Length)
				return float.TryParse(s.Substring(0, pos), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

			return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}

		public static bool StringToDecimal(string s, out decimal value)
		{
			if (s == null || s.Length == 0)
			{
				value = default(decimal);
				return false;
			}
			int pos = 0;
			for (; pos < s.Length; ++pos)
			{
				if (char.IsNumber(s[pos]))
					continue;
				if (s[pos] == '+' || s[pos] == '-' || s[pos] == '.')
					continue;
				break;
			}
			if (pos < s.Length)
				return decimal.TryParse(s.Substring(0, pos), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
			return decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}

		public static decimal StringToDecimal(string s, decimal default_value = default(decimal))
		{
			decimal value;
			if (StringToDecimal(s, out value))
				return value;
			return default_value;
		}

		public static decimal StringToDecimal(string s, decimal min, decimal max, decimal default_value = default(decimal))
		{
			decimal value;
			if (StringToDecimal(s, out value))
				return Math.Min(Math.Max(value, min), max);
			return default_value;
		}

		public static double StringToDouble(string s, double default_value = 0.0f)
		{
			double outValue;
			if (s.EndsWith("%", false))
			{
				if (double.TryParse(s.Substring(0, s.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out outValue))
					return outValue / 100;
				return default_value;
			}

			if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out outValue))
				return outValue;
			return default_value;
		}

		public static bool StringToDouble(string s, out double value)
		{
			return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out value);
		}

		public static double StringToDouble(string s, double min, double max, double default_value = default(double))
		{
			double value;
			if (StringToDouble(s, out value))
				return Math.Min(Math.Max(value, min), max);
			return default_value;
		}

		public static Point StringToPoint(string value, Point default_value = default(Point))
		{
			if (string.IsNullOrEmpty(value))
				return default_value;

			int comma = value.IndexOf(',');
			if (comma == -1)
				return default_value;

			int x, y;
			if (int.TryParse(value.Substring(0, comma), out x)
				&& int.TryParse(value.Substring(comma + 1), out y))
				return new Point(x, y);
			return default_value;
		}

		public static int RoundNearest(float value, int integer)
		{
			if (integer <= 1)
				return Convert.ToInt32(Math.Round(value));
			return Convert.ToInt32(Math.Round(value / integer)) * integer;
		}

		public static float RoundNearest(float value, float partition)
		{
			if (partition <= 0.0f)
				return Convert.ToSingle(Math.Round(value));
			return Convert.ToSingle(Math.Round(value / partition)) * partition;
		}

		public static bool StringToBool(string value, bool defaultValue = false)
		{
			if (string.IsNullOrEmpty(value))
				return defaultValue;

			int i;
			if (int.TryParse(value, out i))
				return i != 0;

			return string.Compare(value, "true", true) == 0
				|| string.Compare(value, "yes", true) == 0
				|| string.Compare(value, "on", true) == 0;
		}

		public static XmlDocument LoadXmlDocument(string filename)
		{
			XmlDocument xmlDoc = new XmlDocument();
			try
			{
				xmlDoc.Load(filename);
			}
			catch
			{
				return null;
			}

			return xmlDoc;
		}

		public static XmlDocument LoadXmlDocumentFromMemory(byte[] buffer, string rootElement = null)
		{
			if (buffer == null || buffer.Length == 0)
				return null;

			XmlDocument xmlDoc = new XmlDocument();
			try
			{
				using (var stream = new MemoryStream(buffer))
				{
					xmlDoc.Load(stream);
					if (rootElement != null && string.Compare(xmlDoc.DocumentElement.Name, rootElement) != 0)
						return null;
					return xmlDoc;
				}
			}
			catch
			{
				return null;
			}
		}

		public static string[] FindFilesInFolder(string path, string searchPattern = "*", bool includeSubFolders = false)
		{
			try
			{
				return Directory.GetFiles(path, searchPattern, includeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
			}
			catch
			{
				return new string[0];
			}
		}

		public static string GetFilePathWithoutExt(string filename)
		{
			string filenameNoExt = Path.GetFileNameWithoutExtension(filename);

			string path = Path.GetDirectoryName(filename);
			if (string.IsNullOrEmpty(path) == false)
				return string.Concat(path, '/', filenameNoExt);
			else
				return filenameNoExt;
		}

		public static string CommaSeparatedList(IEnumerable<string> words, string conj = "and", bool oxfordComma = true)
		{
			int count = words.Count();

			if (words == null || count == 0)
				return "";
			else if (count == 1)
				return words.ElementAt(0);
			else if (count == 2)
			{
				if (string.IsNullOrEmpty(conj) == false)
					return string.Concat(words.ElementAt(0), " ", conj, " ", words.ElementAt(1));
				else
					return string.Concat(words.ElementAt(0), ", ", words.ElementAt(1));
			}
			else
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(words.ElementAt(0));
				for (int i = 1; i < count - 1; ++i)
				{
					sb.Append(", ");
					sb.Append(words.ElementAt(i));
				}
				if (string.IsNullOrEmpty(conj) == false)
				{
					sb.Append(oxfordComma && count > 2 ? ", " : " ");
					sb.Append(conj);
					sb.Append(" ");
				}
				else
					sb.Append(", ");
				sb.Append(words.ElementAt(count - 1));
				return sb.ToString();
			}
		}

		public static string ListToCommaSeparatedString<T>(IEnumerable<T> list)
		{
			return ListToDelimitedString(list, ", ");
		}

		public static string ListToDelimitedString<T>(IEnumerable<T> list, string delimeter)
		{
			if (list == null)
				return string.Empty;

			StringBuilder sb = new StringBuilder();
			int i = 0;
			foreach (var value in list)
			{
				if (0 < i++)
					sb.Append(delimeter ?? "");
				sb.Append(value.ToString());
			}
			return sb.ToString();
		}

		public static List<string> ListFromCommaSeparatedString(string commaSeparatedString, bool ignoreEmpty = true)
		{
			return ListFromDelimitedString(commaSeparatedString, ',', ignoreEmpty);
		}

		public static List<string> ListFromDelimitedString(string source, char delimiter = ',', bool ignoreEmpty = true)
		{
			if (string.IsNullOrEmpty(source))
				return new List<string>(0);

			int capacity = source.Count(c => c == delimiter) + 1;
			List<string> result = new List<string>(capacity);
			var values = new List<string>(source.Split(delimiter).Select(s => s.Trim()));
			foreach (var value in values)
			{
				if (ignoreEmpty && string.IsNullOrEmpty(value))
					continue;

				result.Add(value);
			}
			return result;
		}

		public static List<string> ListFromDelimitedString(string source, string[] delimiters, bool ignoreEmpty = true)
		{
			if (string.IsNullOrEmpty(source))
				return new List<string>(0);

			return source.Split(delimiters, ignoreEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None)
				.Select(s => s.Trim())
				.ToList();
		}

		public static List<string> ListFromDelimitedString(string source, string delimiter, bool ignoreEmpty = true)
		{
			if (string.IsNullOrEmpty(source))
				return new List<string>(0);

			return source.Split(new string[] { delimiter }, ignoreEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None)
				.Select(s => s.Trim())
				.ToList();
		}

		public static T[] ConcatenateArrays<T>(params T[][] arrays)
		{
			if (arrays == null || arrays.Length == 0)
				return null;

			int totalLength = 0;
			for (int i = 0; i < arrays.Length; ++i)
			{
				if (arrays[i] == null || arrays[i].Length == 0)
					continue;
				totalLength += arrays[i].Length;
			}

			var newArray = new T[totalLength];
			int pos = 0;
			for (int i = 0; i < arrays.Length; ++i)
			{
				if (arrays[i] == null || arrays[i].Length == 0)
					continue;
				Array.Copy(arrays[i], 0, newArray, pos, arrays[i].Length);
				pos += arrays[i].Length;
			}
			return newArray;
		}

		public static T[] AppendArray<T>(T[] a, params T[] b)
		{
			if (a == null || a.Length == 0)
				return b;
			if (b == null || b.Length == 0)
				return a;

			var arr = new T[a.Length + b.Length];
			a.CopyTo(arr, 0);
			b.CopyTo(arr, a.Length);
			return arr;
		}

		public static T[] ConcatenateArrays<T>(T[] a, T[] b, int maxLength)
		{
			if (maxLength == 0)
				return new T[0];

			if (a == null || a.Length == 0)
			{
				if (b != null && b.Length > maxLength)
				{
					var bb = new T[maxLength];
					Array.Copy(b, 0, bb, 0, maxLength);
					return bb;
				}
				return b;
			}
			if (b == null || b.Length == 0)
			{
				if (a != null && a.Length > maxLength)
				{
					var aa = new T[maxLength];
					Array.Copy(a, 0, aa, 0, maxLength);
					return aa;
				}
				return a;
			}

			var newArray = new T[Math.Min(a.Length + b.Length, maxLength)];
			int lenA = Math.Min(a.Length, newArray.Length);
			int lenB = Math.Max(newArray.Length - lenA, 0);
			Array.Copy(a, 0, newArray, 0, lenA);
			if (lenB > 0)
				Array.Copy(b, 0, newArray, lenA, lenB);

			return newArray;
		}

		public static T[] SubArray<T>(this T[] array, int pos, int length)
		{
			if (pos < 0 || pos >= array.Length || pos + length > array.Length)
				throw new ArgumentOutOfRangeException();

			var newArray = new T[length];
			Array.Copy(array, pos, newArray, 0, length);
			return newArray;
		}

		public static float Average(params float[] values)
		{
			if (values == null || values.Length == 0)
				return 0.0f;
			if (values.Length == 1)
				return values[0];

			float fSum = 0.0f;
			for (int i = 0; i < values.Length; ++i)
				fSum += values[i];
			return fSum / values.Length;
		}

		public static void ArrayRemoveAt<T>(ref T[] array, int index)
		{
			if (index < 0 && index >= array.Length)
				return;

			for (int i = index; i < array.Length - 1; ++i)
				array[i] = array[i + 1];

			Array.Resize(ref array, array.Length - 1);
		}

		public static string CleanExpression(string s)
		{
			if (s == null)
				return null;

			// Inject whitespace around parentheses
			StringBuilder sbWhitespace = new StringBuilder(s);
			for (int i = sbWhitespace.Length - 1; i >= 0; --i)
			{
				if (sbWhitespace[i] == '(' || sbWhitespace[i] == ')')
				{
					if (i < s.Length - 1)
						sbWhitespace.Insert(i + 1, ' ');
					if (i > 0)
					{
						sbWhitespace.Insert(i, ' ');
						--i;
					}
				}
			}

			var tokens = sbWhitespace.ToString().Split(new char[] { ' ', '\t', '\n', '\r', '\u00a0', '\u3000' }, StringSplitOptions.RemoveEmptyEntries);

			return string.Join(" ", tokens).TrimEnd().ToLowerInvariant();
		}

		public static int FindEndOfScope(string source, int pos, char chOpen, char chClose)
		{
			if (string.IsNullOrEmpty(source))
				return -1;

			int scope = 0;
			for (; pos < source.Length; ++pos)
			{
				char c = source[pos];
				if (c == chOpen)
					++scope;
				else if (c == chClose)
				{
					if (--scope <= 0)
						return pos;
				}
			}
			return -1;
		}

		public static int FindEndOfScope(StringBuilder source, int pos, char chOpen, char chClose)
		{
			if (source == null)
				return -1;

			int scope = 0;
			for (; pos < source.Length; ++pos)
			{
				char c = source[pos];
				if (c == chOpen)
					++scope;
				else if (c == chClose)
				{
					if (--scope <= 0)
						return pos;
				}
			}
			return -1;
		}

		public static int ScopedIndexOf(string text, int pos, char ch, char chOpen, char chClose, int to = -1, char escape = '\\')
		{
			if (string.IsNullOrEmpty(text))
				return -1;

			int currScope = 0;

			if (to < 0)
				to = text.Length - 1;

			for (int i = pos; i <= to && i < text.Length; ++i)
			{
				char c = text[i];

				// Escaped ?
				if (i > 0 && text[i - 1] == escape)
					continue;

				if (c == ch && currScope == 0)
					return i;

				if (c == chOpen)
					currScope++;
				else if (c == chClose)
					currScope--;
			}
			return -1;
		}

		private static bool __strcmp(IEnumerable<char> str1, int position, string str2)
		{
			if (str1 == null || str2 == null || str2.Length == 0)
				return false;
			int strLen = str1.Count();
			if (position < 0 || position >= strLen)
				return false;
			if (str2.Length > strLen)
				return false;

			for (int i = 0; i < str2.Length; ++i)
			{
				if (position + i >= strLen)
					return false;
				char c = str1.ElementAt(i + position);
				if (str2[i] != c)
					return false;
			}
			return true;
		}

		private static bool __strcmp(StringBuilder sb, int position, string str2)
		{
			if (sb == null || str2 == null || str2.Length == 0)
				return false;
			int strLen = sb.Length;
			if (position < 0 || position >= strLen)
				return false;
			if (str2.Length > strLen)
				return false;

			for (int i = 0; i < str2.Length; ++i)
			{
				if (position + i >= strLen)
					return false;
				char c = sb[i + position];
				if (str2[i] != c)
					return false;
			}
			return true;
		}

		public static int ScopedIndexOf(string source, string match, int from, string sOpen, string sClose, int to = -1, char escape = '\\')
		{
			if (string.IsNullOrEmpty(source))
				return -1;

			int currScope = -1;

			if (to < 0)
				to = source.Length - 1;

			for (int i = from; i <= to && i < source.Length; ++i)
			{
				// Escaped ?
				if (i > 0 && source[i - 1] == escape)
					continue;

				if (__strcmp(source, i, match) && currScope == 0)
					return i;

				if (__strcmp(source, i, sOpen))
					currScope++;
				else if (__strcmp(source, i, sClose))
					currScope--;
			}
			return -1;
		}

		public static int ScopedIndexOf(StringBuilder source, string match, int from, string sOpen, string sClose, int to = -1, char escape = '\\')
		{
			if (source == null)
				return -1;

			int currScope = -1;

			if (to < 0)
				to = source.Length - 1;

			for (int i = from; i <= to && i < source.Length; ++i)
			{
				// Escaped ?
				if (i > 0 && source[i - 1] == escape)
					continue;

				if (__strcmp(source, i, match) && currScope == 0)
					return i;

				if (__strcmp(source, i, sOpen))
					currScope++;
				else if (__strcmp(source, i, sClose))
					currScope--;
			}
			return -1;
		}

		public static int FloorToIntSigned(float value)
		{
			return value >= 0.0f ? (int)Math.Floor(value) : (int)Math.Ceiling(value);
		}

		public static bool FloatEquality(float a, float b)
		{
			return Math.Abs(a - b) < float.Epsilon;
		}

		public static bool DoubleEquality(double a, double b)
		{
			return Math.Abs(a - b) < double.Epsilon;
		}

		public static string[] SplitByWhiteSpaceWithQuotes(string command, bool ignoreEmpty = false)
		{
			if (string.IsNullOrEmpty(command))
				return new string[0];

			Func<int, string, int> fnFindQuoteEnd = (index, s) => {
				int quote_end = -1;
				for (int i = index + 1; i < s.Length; ++i)
				{
					if (s[i] != '"')
						continue;
					if (i < command.Length - 1 && s[i + 1] == '"') // Escaped
					{
						++i;
						continue;
					}
					quote_end = i;
					break;
				}
				return quote_end;
			};
			Func<int, string, int> fnFindNextWord = (index, s) => {
				for (int i = index; i < s.Length; ++i)
				{
					if (char.IsWhiteSpace(s[i]) == false)
						return i;
				}
				return -1;
			};
			Func<int, string, int> fnFindEndOfWord = (index, s) => {
				for (int i = index; i < s.Length; ++i)
				{
					if (char.IsWhiteSpace(s[i]))
						return i;
				}
				return s.Length;
			};

			int cursor = 0;
			var words = new List<string>();
			while (cursor < command.Length && cursor != -1)
			{
				// Skip preceding whitespace
				cursor = fnFindNextWord(cursor, command);
				if (cursor == -1)
					break;
				int pos_word = cursor;

				// Quoted string
				if (command[cursor] == '"')
				{
					int quote_end = fnFindQuoteEnd(cursor, command);
					if (quote_end != -1)
					{
						words.Add(command.Substring(pos_word + 1, quote_end - pos_word - 1).Replace("\"\"", "\"")); // Unescape
						cursor = quote_end + 1;
						continue;
					}
					else
					{
						words.Add(command.Substring(cursor)); // To end
						break;
					}
				}
				else
				{
					int word_end = fnFindEndOfWord(cursor, command);
					words.Add(command.Substring(pos_word, word_end - pos_word)); // To end
					cursor = word_end;
					continue;
				}
			}

			if (ignoreEmpty)
				return words.Where(w => string.IsNullOrEmpty(w) == false).ToArray();
			return words.ToArray();
		}

		public static string RemoveBrackets(string text, string open, string close, bool allowEscaping = true)
		{
			if (string.IsNullOrEmpty(open) || string.IsNullOrEmpty(close) || string.IsNullOrEmpty(text))
				return text;

			var sb = new StringBuilder(text);
			int pos = sb.IndexOf(open, 0);
			bool changed = false;
			while (pos != -1)
			{
				int beginScope = pos;

				if (allowEscaping && pos > 0 && sb[pos - 1] == '\\')
				{
					pos = sb.IndexOf(open, pos + 1);
					continue;
				}

				int endScope = ScopedIndexOf(sb, close, beginScope, open, close);
				if (endScope == -1)
				{
					pos = sb.IndexOf(open, pos + 1);
					continue;
				}

				changed = true;
				sb.RemoveFromTo(beginScope, endScope + close.Length - 1);

				pos = sb.IndexOf(open, beginScope);
			}
			if (changed)
				return sb.ToString();
			return text;
		}

		public struct ColorF
		{
			public float r;
			public float g;
			public float b;

			public ColorF(float r, float g, float b)
			{
				this.r = r;
				this.g = g;
				this.b = b;
			}

			public ColorF(Color color)
			{
				r = color.R / 255.0f;
				g = color.G / 255.0f;
				b = color.B / 255.0f;
			}

			public Color ToColor()
			{
				return Color.FromArgb(
					Convert.ToByte(Clamp01(r) * 255),
					Convert.ToByte(Clamp01(g) * 255),
					Convert.ToByte(Clamp01(b) * 255));
			}
		}

		private static ColorF RGBtoHCV(ColorF rgb)
		{
			const float Epsilon = 1e-10F;

			// Based on work by Sam Hocevar and Emil Persson
			float[] P = (rgb.g < rgb.b) ? new float[4] { rgb.b, rgb.g, -1.0f, 2.0f / 3.0f } : new float[4] { rgb.g, rgb.b, 0.0f, -1.0f / 3.0f };
			float[] Q = (rgb.r < P[0]) ? new float[4] { P[0], P[1], P[3], rgb.r } : new float[4] { rgb.r, P[1], P[2], P[0] };
			float C = Q[0] - Math.Min(Q[3], Q[1]);
			float H = Math.Abs((Q[3] - Q[1]) / (6 * C + Epsilon) + Q[2]);
			return new ColorF(H, C, Q[0]);
		}

		private static float Clamp01(float value)
		{
			return Math.Min(Math.Max(value, 0), 1);
		}

		public static ColorF HSVToRGB(ColorF hsv)
		{
			float h = hsv.r;
			float s = hsv.g;
			float v = hsv.b;

			float R = Clamp01(Math.Abs(h * 6 - 3) - 1);
			float G = Clamp01(2 - Math.Abs(h * 6 - 2));
			float B = Clamp01(2 - Math.Abs(h * 6 - 4));

			return new ColorF(
					Clamp01(((R - 1.0f) * s + 1) * v),
					Clamp01(((G - 1.0f) * s + 1) * v),
					Clamp01(((B - 1.0f) * s + 1) * v));
		}

		public static ColorF RGBToHSV(ColorF color)
		{
			const float Epsilon = 1e-10F;

			ColorF HCV = RGBtoHCV(color);
			float S = HCV.g / (HCV.b + Epsilon);
			return new ColorF(HCV.r, S, HCV.b);
		}

		public static Color GetContrastColor(Color background, bool soften)
		{
			float fR = background.R;
			float fG = background.G;
			float fB = background.B;

			var luminance = (0.299f * fR + 0.587f * fG + 0.114f * fB) / 255;
			Color contrastColor = luminance > 0.65 ? Color.Black : Color.White;

			if (!soften)
				return contrastColor;

			Func<Color, Color, float, Color> fnBlend = (Color x, Color y, float v) => {
				var a = new ColorF(x);
				var b = new ColorF(y);

				var c = new ColorF();
				c.r = a.r + v * (b.r - a.r);
				c.g = a.g + v * (b.g - a.g);
				c.b = a.b + v * (b.b - a.b);
				return c.ToColor();
			};

			const float kBlend = 0.45f;
			contrastColor = fnBlend(contrastColor, background, kBlend);

			// Boost saturation, brightness
			var hsv = RGBToHSV(new ColorF(contrastColor));
			if (luminance > 0.65)
				hsv.b -= 0.15f;
			else
				hsv.b += 0.15f;

			if (hsv.g > 0.05f)
				hsv.g += 0.15f;
			contrastColor = HSVToRGB(hsv).ToColor();

			return contrastColor;
		}

		public static Color GetDarkerColor(Color background, float fDarken = 0.5f)
		{
			var hsv = RGBToHSV(new ColorF(background));
			hsv.b -= fDarken;
			if (hsv.g > 0.05f)
				hsv.g += 0.12f;
			return HSVToRGB(hsv).ToColor();
		}

		public static string ReplaceWholeWord(string text, string word, string replace, StringComparison comparison = StringComparison.Ordinal, WholeWordOptions options = WholeWordOptions.Default)
		{
			if (string.IsNullOrEmpty(word))
				return text;

			StringBuilder sb = new StringBuilder(text);
			ReplaceWholeWord(sb, word, replace, comparison, options);
			return sb.ToString();
		}

		private struct WholeWord
		{
			public int start;
			public int length;
		}

		[Flags]
		public enum WholeWordOptions
		{
			None = 0,
			CharacterSetBoundaries = 1 << 0,

			Default = CharacterSetBoundaries,
		}

		public static bool IsWhole(string text, string word, int pos, WholeWordOptions options = WholeWordOptions.Default)
		{
			char? left = null;
			char? right = null;
			if (pos > 0) left = text[pos - 1];
			if (pos + word.Length < text.Length) right = text[pos + word.Length];

			char ch = text[pos];
			bool bTermLeft = !left.HasValue
				|| char.IsWhiteSpace(left.Value)
				|| char.IsPunctuation(left.Value)
				|| left.Value >= 0xfff0; // Special char
			bool bTermRight = !right.HasValue
				|| char.IsWhiteSpace(right.Value)
				|| char.IsPunctuation(right.Value)
				|| right.Value >= 0xfff0; // Special char

			if (options.Contains(WholeWordOptions.CharacterSetBoundaries) && !(bTermLeft || bTermRight))
			{ 
				var charSet = CharUtil.GetCharacterSet(ch);
				if (!bTermLeft)
				{
					var leftCharSet = left.HasValue ? CharUtil.GetCharacterSet(left.Value) : CharUtil.CharacterSet.Undefined;
					bTermLeft |= charSet == CharUtil.CharacterSet.CJK
						|| (charSet == CharUtil.CharacterSet.Default && !char.IsLetter(left.Value))
						|| charSet != leftCharSet;
				}
				if (!bTermRight)
				{
					var rightCharSet = right.HasValue ? CharUtil.GetCharacterSet(right.Value) : CharUtil.CharacterSet.Undefined;
					bTermRight |= charSet == CharUtil.CharacterSet.CJK
						|| (charSet == CharUtil.CharacterSet.Default && !char.IsLetter(right.Value))
						|| charSet != rightCharSet;
				}
			}

			return bTermLeft && bTermRight;
		}

		public static void ReplaceWholeWord(StringBuilder sb, string word, string replace, StringComparison comparison = StringComparison.Ordinal, WholeWordOptions options = WholeWordOptions.Default)
		{
			if (string.IsNullOrEmpty(word))
				return;

			List<WholeWord> replacements = new List<WholeWord>();

			string text = sb.ToString();

			int pos = text.IndexOf(word, 0, comparison);
			while (pos != -1)
			{
				bool whole = IsWhole(text, word, pos, options);
				if (whole)
				{
					replacements.Add(new WholeWord() {
						start = pos,
						length = word.Length,
					});
					pos = text.IndexOf(word, pos + word.Length, comparison);
					continue;
				}
				pos = text.IndexOf(word, pos + 1, comparison);
			}

			for (int i = replacements.Count - 1; i >= 0; --i)
			{
				var r = replacements[i];
				sb.Remove(r.start, r.length);
				sb.Insert(r.start, replace ?? "");
			}
		}

		public static int[] FindWords(string text, string word, StringComparison comparison = StringComparison.Ordinal)
		{
			if (string.IsNullOrEmpty(word))
				return null;

			List<int> found = new List<int>();
			int pos = text.IndexOf(word, 0, comparison);
			while (pos != -1)
			{
				found.Add(pos);
				pos = text.IndexOf(word, pos + word.Length, comparison);
			}
			return found.ToArray();
		}

		public static int FindWholeWord(string text, string word, int startIndex = 0, StringComparison comparison = StringComparison.Ordinal, WholeWordOptions options = WholeWordOptions.Default)
		{
			if (string.IsNullOrEmpty(word))
				return -1;

			int pos = text.IndexOf(word, startIndex, comparison);
			while (pos != -1)
			{
				bool whole = IsWhole(text, word, pos, options);

				if (whole)
					return pos;
				pos = text.IndexOf(word, pos + 1, comparison);
			}
			return -1;
		}

		public static int FindWholeWordReverse(string text, string word, int startIndex = -1, bool ignoreCase = false, WholeWordOptions options = WholeWordOptions.Default)
		{
			if (string.IsNullOrEmpty(word))
				return -1;
			if (startIndex == -1)
				startIndex = text.Length - 1;
			int pos = text.IndexOfReverse(word, startIndex, ignoreCase);
			while (pos != -1)
			{
				bool whole = IsWhole(text, word, pos, options);

				if (whole)
					return pos;
				pos = text.IndexOfReverse(word, pos - 1, ignoreCase);
			}
			return -1;
		}

		public static int[] FindWholeWords(string text, string word, StringComparison comparison = StringComparison.Ordinal, WholeWordOptions options = WholeWordOptions.Default)
		{
			if (string.IsNullOrEmpty(word))
				return null;

			List<int> found = new List<int>();
			int pos = text.IndexOf(word, 0, comparison);
			while (pos != -1)
			{
				bool whole = IsWhole(text, word, pos, options);

				if (whole)
				{
					found.Add(pos);
					pos = text.IndexOf(word, pos + word.Length, StringComparison.OrdinalIgnoreCase);
					continue;
				}
				pos = text.IndexOf(word, pos + 1, comparison);
			}
			return found.ToArray();
		}

		public static int FindFirstWholeWord(string text, string[] words, StringComparison comparison = StringComparison.Ordinal, WholeWordOptions options = WholeWordOptions.Default)
		{
			if (words == null || words.Length == 0)
				return -1;

			int found = int.MaxValue;
			for (int i = 0; i < words.Length; ++i)
			{
				int index = Math.Min(found, FindWholeWord(text, words[i], 0, comparison, options));
				if (index >= 0)
					found = Math.Min(found, index);
			}

			if (found != int.MaxValue)
				return found;
			return -1;
		}

		public static int FindAnyWord(string text, string[] words, StringComparison comparison = StringComparison.Ordinal)
		{
			if (words == null || words.Length == 0)
				return -1;

			for (int i = 0; i < words.Length; ++i)
			{
				int index = text.IndexOf(words[i], comparison);
				if (index != -1)
					return i;
			}
			return -1;
		}

		public static int FindAnyWholeWord(string text, string[] words, StringComparison comparison = StringComparison.Ordinal, WholeWordOptions options = WholeWordOptions.Default)
		{
			if (words == null || words.Length == 0)
				return -1;

			for (int i = 0; i < words.Length; ++i)
			{
				int index = FindWholeWord(text, words[i], 0, comparison, options);
				if (index != -1)
					return i;
			}
			return -1;
		}

		public static string AppPath(string pathName)
		{
			return Path.Combine(Directory.GetCurrentDirectory(), pathName);
		}

		public static string AppPath(string path, string filename)
		{
			return Path.Combine(Directory.GetCurrentDirectory(), path, filename);
		}

		public static string ContentPath(string pathName)
		{
			return Path.Combine(Directory.GetCurrentDirectory(), "Content", AppSettings.Settings.Locale, pathName);
		}

		public static string ContentPath(string pathName, string filename)
		{
			return Path.Combine(Directory.GetCurrentDirectory(), "Content", AppSettings.Settings.Locale, pathName, filename);
		}

		public static string ChangeFileExtension(string filename, string ext)
		{
			return Path.Combine(Path.GetDirectoryName(filename), string.Concat(Path.GetFileNameWithoutExtension(filename), ".", ext));
		}

		public enum HashOption
		{
			None = 0,
			Ordered = 1,
			Default = Ordered,
		}

		public static int MakeHashCode(params object[] objects)
		{
			return MakeHashCode(HashOption.Default, objects);
		}

		public static int MakeHashCode(HashOption option, params object[] objects)
		{
			if (objects == null || objects.Length == 0)
				return 0;

			int hash = 269;
			unchecked
			{
				if (option == HashOption.Ordered)
				{
					for (int i = 0; i < objects.Length; ++i)
						hash = (hash * 31) + (objects[i] != null ? (objects[i]).GetHashCode() : 0);
				}
				else
				{
					for (int i = 0; i < objects.Length; ++i)
						hash ^= (objects[i] != null ? (objects[i]).GetHashCode() : 0);
				}
			}
			return hash;
		}

		public static int MakeHashCode<T>(IEnumerable<T> objects, HashOption option)
		{
			if (objects == null)
				return 0;

			int hash = 269;
			unchecked
			{
				if (option == HashOption.Ordered)
				{
					foreach (var obj in objects)
						hash = (hash * 31) + (!ReferenceEquals(obj, null) ? obj.GetHashCode() : 0);
				}
				else
				{
					foreach (var obj in objects)
						hash ^= (!ReferenceEquals(obj, null) ? obj.GetHashCode() : 0);
				}
			}
			return hash;
		}

		public static string EscapeMenu(string text)
		{
			return text.Replace("&", "&&");
		}

		public static string FirstNonEmpty(params string[] texts)
		{
			if (texts == null || texts.Length == 0)
				return null;
			for (int i = 0; i < texts.Length; ++i)
				if (string.IsNullOrEmpty(texts[i]) == false)
					return texts[i];
			return null;
		}

		public static string ValidFilename(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				return filename;

			char[] invalidChars = new char[] { '/', '\\', ':', '*', '?', '"', ';' };

			filename = filename.Replace('/', '-');

			var sbFilename = new StringBuilder(filename);
			for (int i = 0; i < sbFilename.Length; ++i)
			{
				if (invalidChars.Contains(sbFilename[i]))
					sbFilename[i] = '_';
			}
			return sbFilename.ToString().Trim();
		}

		public static void OpenUrl(string url)
		{
			try
			{
				var processInfo = new ProcessStartInfo() {
					FileName = "explorer",
					Arguments = url,
					UseShellExecute = true,
				};
				Process.Start(processInfo);
			}
			catch
			{
			}
		}

		public static string Unindent(string s)
		{
			if (string.IsNullOrEmpty(s))
				return s;

			int posLn = s.IndexOf('\n');
			if (posLn == -1)
				return s.Trim();

			StringBuilder sb = new StringBuilder(s);
			int pos = 0;
			for (; pos < sb.Length;)
			{
				if (sb[pos] == '\n')
				{
					++pos;
					continue;
				}
				if (char.IsWhiteSpace(sb[pos]))
				{
					sb.Remove(pos, 1);
					continue;
				}
				pos = sb.IndexOf('\n', pos);
				if (pos == -1)
					break;
			}
			sb.TrimStart();
			sb.TrimEnd();
			return sb.ToString();
		}
		
		public enum ImageFormat { Png, Jpeg, Gif };
		public static byte[] ImageToMemory(Image image, ImageFormat format = ImageFormat.Png)
		{
			if (image == null)
				return null;

			try
			{
				System.Drawing.Imaging.ImageFormat imageFormat;
				switch (format)
				{
				default:
				case ImageFormat.Png:
					imageFormat = System.Drawing.Imaging.ImageFormat.Png;
					break;
				case ImageFormat.Jpeg:
					imageFormat = System.Drawing.Imaging.ImageFormat.Jpeg;
					break;
				case ImageFormat.Gif:
					imageFormat = System.Drawing.Imaging.ImageFormat.Gif;
					break;
				}

				using (var stream = new MemoryStream())
				{
					if (image.RawFormat.Equals(imageFormat) 
						&& format != ImageFormat.Jpeg) // Jpeg throws
					{
						image.Save(stream, imageFormat);
					}
					else
					{
						using (Image bmpNewImage = new Bitmap(image.Width, image.Height))
						{
							Graphics gfxNewImage = Graphics.FromImage(bmpNewImage);
							gfxNewImage.DrawImage(image, new Rectangle(0, 0, bmpNewImage.Width, bmpNewImage.Height),
													0, 0,
													image.Width, image.Height,
													GraphicsUnit.Pixel);
							gfxNewImage.Dispose();

							bmpNewImage.Save(stream, imageFormat);
						}
					}

					return stream.ToArray();
				}
			}
			catch (Exception e)
			{
				return null;
			}
		}

		public static string ImageToBase64(Image image)
		{
			if (image == null)
				return null;

			var bytes = ImageToMemory(image);
			if (bytes == null)
				return null;

			var imageBase64 = Convert.ToBase64String(bytes);
			return string.Concat("data:image/png;base64,", imageBase64);
		}

		public static Image ImageFromBase64(string base64)
		{
			if (string.IsNullOrEmpty(base64))
				return null;

			try
			{
				if (base64.BeginsWith("data:") == false)
					return null; // Not a valid string
				int pos_mime_end = base64.IndexOf(";base64,");
				if (pos_mime_end == -1)
					return null; // Not a valid string

				base64 = base64.Substring(pos_mime_end + 8); // Ignore mime type. Will fail downstream if unsupported.

				// Load image first
				Image image;
				try
				{
					byte[] bytes = Convert.FromBase64String(base64);
					using (var stream = new MemoryStream(bytes))
					{
						image = Image.FromStream(stream);
						return image;
					}
				}
				catch
				{
					return null;
				}
			}
			catch
			{
				return null;
			}
		}

		public static string InferGender(string persona)
		{
			if (string.IsNullOrEmpty(persona))
				return null;

			persona = persona.ToLowerInvariant();

			// Unambiguous indicators
			if (FindAnyWord(persona, new string[] { "hermaphrodite" }, StringComparison.Ordinal) != -1)
				return "Hermaphrodite";
			if (FindAnyWord(persona, new string[] { "futanari", "dickgirl", "shemale", "dick-girl", "she-male", "newhalf" }, StringComparison.Ordinal) != -1)
				return "Futanari";
			if (FindAnyWord(persona, new string[] { "transgender", "transsexual" }, StringComparison.Ordinal) != -1)
				return "Transgender";
			if (FindAnyWord(persona, new string[] { "non-binary", "nonbinary", "intersex" }, StringComparison.Ordinal) != -1)
				return "Non-binary";

			// Split text into lines/sentences
			string[] lines = persona
				.Split(new char[] { '\n', '.' }, StringSplitOptions.RemoveEmptyEntries)
				.Where(s => s.Contains("{user}") == false)
				.ToArray();

			string primaryGuess = null;
			string secondaryGuess = null;
			string tertiaryGuess = null;

			// Primary indicators
			for (int i = 0; i < lines.Length; ++i)
			{
				string line = lines[i];

				if (primaryGuess == null)
				{
					int index = -1;
					if (Scan(line, ref index, "futa"))
						return "Futanari";
					if (Scan(line, ref index, "male", "man", "boy"))
						primaryGuess = "Male";
					if (Scan(line, ref index, "female", "woman", "girl"))
						primaryGuess = "Female";
					if (Scan(line, ref index, "trans"))
						primaryGuess = "Transgender";
				}

				if (secondaryGuess == null)
				{
					int index = -1;
					if (Scan(line, ref index, "he", "him", "himself", "his"))
						secondaryGuess = "Male";
					if (Scan(line, ref index, "she", "her", "herself", "hers"))
						secondaryGuess = "Female";
				}

				if (tertiaryGuess == null)
				{
					int index = -1;
					if (Scan(line, ref index, "boyfriend", "husband", "father", "dad", "son", "patriarch", "incubus", "master", "gentleman"))
						tertiaryGuess = "Male";
					if (Scan(line, ref index, "girlfriend", "wife", "waifu", "mother", "mom", "milf", "daughter", "matron", "matriarch", "succubus", "mistress", "lady"))
						tertiaryGuess = "Female";
				}

				if (primaryGuess != null)
					return primaryGuess;
				if (secondaryGuess != null)
					return secondaryGuess;
				if (tertiaryGuess != null)
					return tertiaryGuess;
			}

			return null; // None (or Neutral)
		}

		private static bool Scan(string text, ref int index, params string[] words)
		{
			int found = FindFirstWholeWord(text, words, StringComparison.Ordinal, WholeWordOptions.None);
			if (found == -1)
				return false;

			if (index == -1 || found < index)
			{
				index = found;
				return true;
			}
			return false;
		}
	}

}
