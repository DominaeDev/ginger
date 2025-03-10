using System;
using System.IO;
using System.Text;

namespace Ginger
{
	public enum Linebreak
	{
		CRLF,   // \r\n
		LF,     // \n
		CR,     // \r

		Windows = CRLF,
		Unix = LF,
		Mac = CR,
		Default = CRLF,
	}

	public static class StringBuilderExtensions
	{
		public static int IndexOfIgnoreCase(this StringBuilder sb, string value, int startIndex)
		{
			// It feels slightly wrong to call ToString here, but this is the fastest way
			// to search a StringBuilder while ignoring case. For-loop was way too slow.
			return sb.ToString().IndexOf(value, startIndex, StringComparison.OrdinalIgnoreCase);
		}

		public static int IndexOf(this StringBuilder sb, string value, int startIndex = 0)
		{
			int index;
			int length = value.Length;
			int sbLength = sb.Length;
			int maxSearchLength = (sb.Length - length) + 1;

			for (int i = startIndex; i < maxSearchLength; ++i)
			{
				if (sb[i] == value[0])
				{
					index = 1;
					while (index < length && i + index < sbLength && sb[i + index] == value[index])
						++index;

					if (index == length)
						return i;
				}
			}

			return -1;
		}

		public static int IndexOf(this StringBuilder sb, char value, int startIndex = 0)
		{
			for (int i = startIndex; i < sb.Length; ++i)
			{
				if (sb[i] == value)
					return i;
			}

			return -1;
		}

		public static int IndexOfAny(this StringBuilder sb, char[] chars, int startIndex = 0)
		{
			if (chars == null)
				return -1;

			for (int i = startIndex; i < sb.Length; ++i)
			{
				for (int j = 0; j < chars.Length; ++j)
					if (sb[i] == chars[j])
						return i;
			}

			return -1;
		}

		public static int IndexOfAny(this StringBuilder sb, string[] words, int startIndex = 0)
		{
			if (words == null)
				return -1;

			foreach (var word in words)
			{
				int idx = sb.IndexOf(word, startIndex);
				if (idx != -1)
					return idx;
			}

			return -1;
		}

		public static void RemoveFromTo(this StringBuilder sb, int from, int to)
		{
			if (from > to)
				return;
			sb.Remove(from, to - from + 1);
		}

		public static void Replace(this StringBuilder sb, int pos, int length, string replacement)
		{
			if (pos < 0 || pos >= sb.Length || length <= 0)
				return;
			
			sb.Remove(pos, length);
			sb.Insert(pos, replacement);
		}

		public static void ReplaceFromTo(this StringBuilder sb, int from, int to, string replacement)
		{
			if (from > to)
				return;
			sb.Remove(from, to - from + 1);
			sb.Insert(from, replacement);
		}

		public static string Substring(this StringBuilder sb, int pos, int length)
		{
			return sb.ToString().Substring(pos, length);
		}

		public static string SubstringToFrom(this StringBuilder sb, int from, int to)
		{
			return Substring(sb, from, to - from + 1);
		}

		public static bool CompareAt(this StringBuilder sb, int from, string value, bool ignoreCase = false)
		{
			if (value == null)
				return false;
			if (from < 0 || from >= sb.Length)
				return false;
			int to = from + value.Length;
			if (to < 0 || to > sb.Length)
				return false;

			if (ignoreCase)
			{
				for (int i = 0; i < to-from; ++i)
				{
					if (char.ToUpperInvariant(sb[from + i]) != char.ToUpperInvariant(value[i]))
						return false;
				}
				return true;
			}
			else
			{
				for (int i = 0; i < to-from; ++i)
				{
					if (sb[from + i] != value[i])
						return false;
				}
				return true;
			}
		}

		public static void Clear(this StringBuilder sb)
		{
			sb.Length = 0;
		}

		public static StringBuilder TrimStart(this StringBuilder sb)
		{
			if (sb == null || sb.Length == 0)
				return sb;

			int i = 0;

			for (; i < sb.Length; ++i)
			{
				if (!char.IsWhiteSpace(sb[i]))
					break;
			}

			if (i > 0)
				sb.Remove(0, i);

			return sb;
		}

		public static StringBuilder TrimEnd(this StringBuilder sb)
		{
			if (sb == null || sb.Length == 0)
				return sb;

			int i = sb.Length - 1;

			for (; i >= 0; i--)
				if (!char.IsWhiteSpace(sb[i]))
					break;

			if (i < sb.Length - 1)
				sb.Length = i + 1;

			return sb;
		}

		public static StringBuilder Trim(this StringBuilder sb)
		{
			sb.TrimEnd();
			sb.TrimStart();
			return sb;
		}

		public static StringBuilder TrimLinebreaks(this StringBuilder sb)
		{
			if (sb == null || sb.Length == 0)
				return sb;


			// Trim begin
			int i = 0;
			for (; i < sb.Length; ++i)
			{
				if (sb[i] != '\n' && sb[i] != '\r')
					break;
			}
			sb.Remove(0, i);

			// Trim end
			i = sb.Length - 1;
			for (; i >= 0; i--)
			{
				if (sb[i] != '\n' && sb[i] != '\r')
					break;
			}
			if (i < sb.Length - 1)
				sb.Length = i + 1;

			return sb;
		}

		public static void NewLine(this StringBuilder sb)
		{
			if (sb.Length == 0)
				return;

			int nLinebreaks = 0;
			int pos = -1;

			for (int i = sb.Length - 1; i >= 0; --i)
			{
				if (sb[i] == '\r')
					continue;
				else if (sb[i] == '\n')
				{
					++nLinebreaks;
					continue;
				}
				else if (char.IsWhiteSpace(sb[i]))
					continue;
				pos = i;
				break;
			}

			if (pos == -1) // All whitespace
			{
				sb.Clear();
				return;
			}

			if (nLinebreaks == 0)
				sb.Append('\n');
			else if (nLinebreaks == 1)
				return;
			else
			{
				sb.Remove(pos + 1, sb.Length - pos - 1);
				sb.Append('\n');
			}
		}

		public static void NewParagraph(this StringBuilder sb)
		{
			if (sb.Length == 0)
				return;

			int nLinebreaks = 0;
			int pos = -1;

			for (int i = sb.Length - 1; i >= 0; --i)
			{
				if (sb[i] == '\r')
					continue;
				else if (sb[i] == '\n')
				{
					++nLinebreaks;
					continue;
				}
				else if (char.IsWhiteSpace(sb[i]))
					continue;
				pos = i;
				break;
			}

			if (pos == -1) // All whitespace
			{
				sb.Clear();
				return;
			}

			if (nLinebreaks == 0)
			{
				sb.Append('\n');
				sb.Append('\n');
			}
			else if (nLinebreaks == 1)
				sb.Append('\n');
			else if (nLinebreaks == 2)
				return;
			else
			{
				sb.Remove(pos + 1, sb.Length - pos - 1);
				sb.Append('\n');
				sb.Append('\n');
			}
		}

		public static bool BeginsWith(this StringBuilder sb, string match, bool ignoreCase = false)
		{
			if (string.IsNullOrEmpty(match))
				return false;
			if (sb.Length < match.Length)
				return false;

			if (ignoreCase)
			{
				for (int i = 0; i < sb.Length && i < match.Length; ++i)
				{
					if (char.ToUpperInvariant(match[i]) != char.ToUpperInvariant(sb[i]))
						return false;
				}
			}
			else
			{
				for (int i = 0; i < sb.Length && i < match.Length; ++i)
				{
					if (match[i] != sb[i])
						return false;
				}
			}
			return true;
		}

		public static StringBuilder Replace(this StringBuilder sb, string oldValue, string newValue, bool ignoreCase)
		{
			if (!ignoreCase)
				return sb.Replace(oldValue, newValue);

			int i = sb.Length - oldValue.Length;
			for (; i >= 0;)
			{
				if (sb.CompareAt(i, oldValue, true))
				{
					sb.Remove(i, oldValue.Length);
					sb.Insert(i, newValue);
					i -= oldValue.Length;
					continue;
				}
				--i;
			}
			return sb;
		}

		public static bool Contains(this StringBuilder sb, string text, bool ignoreCase)
		{
			return (ignoreCase ? sb.IndexOfIgnoreCase(text, 0) : sb.IndexOf(text, 0)) != -1;
		}
		
		public static void ConvertLinebreaks(this StringBuilder sb, Linebreak eol = Linebreak.Default)
		{
			bool found = false;
			for (int i = 0; i < sb.Length; ++i)
			{
				if (sb[i] == '\r')
				{
					if (i < sb.Length - 1 && sb[i + 1] == '\n')
						sb.Remove(i + 1, 1);
					sb[i] = '\uFFFC';
					found = true;
				}
				else if (sb[i] == '\n')
				{
					sb[i] = '\uFFFC';
					found = true;
				}
			}
			if (found)
			{
				if (eol == Linebreak.CRLF)
					sb.Replace("\uFFFC", "\r\n");
				else if (eol == Linebreak.LF)
					sb.Replace('\uFFFC', '\n');
				else if (eol == Linebreak.CR)
					sb.Replace('\uFFFC', '\r');
			}
		}

		public static char Peek(this StringBuilder sb, int pos)
		{
			if (pos >= 0 && pos < sb.Length)
				return sb[pos];
			return '\0';
		}

		public static void TrimTrailingSpaces(this StringBuilder sb)
		{
			bool bTrim = true;
			int pos_end = sb.Length - 1;
			for (int i = sb.Length - 1; i >= 0; i--)
			{
				if (sb[i] == '\n' || sb[i] == '\r')
				{
					bTrim = true;
					pos_end = i - 1;
					continue;
				}
				if (bTrim && char.IsWhiteSpace(sb[i]) == false)
				{
					if (pos_end - i > 0)
						sb.Remove(i + 1, pos_end - i);
					bTrim = false;
				}
			}
		}
	}

	public class StringWriterUTF8 : StringWriter
	{
		public StringWriterUTF8(StringBuilder builder) : base(builder)
		{
		}

		public override Encoding Encoding
		{
			get { return Encoding.UTF8; }
		}
	}
}
