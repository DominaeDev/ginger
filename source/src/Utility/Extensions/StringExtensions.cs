// String extension methods

using System;
using System.Text;

namespace Ginger
{
	public static class StringExtensions
	{
		public static bool BeginsWith(this string str, string match, bool bIgnoreCase = false)
		{
			if (str == null || match == null || str.Length < match.Length || match.Length == 0)
				return false;

			if (bIgnoreCase)
			{
				if (str.Length == match.Length)
					return string.Compare(str, match, StringComparison.Ordinal) == 0;

				for (int i = 0; i < str.Length && i < match.Length; ++i)
				{
					if (char.ToUpperInvariant(match[i]) != char.ToUpperInvariant(str[i]))
						return false;
				}
			}
			else
			{
				if (str.Length == match.Length)
					return string.Compare(str, match, StringComparison.OrdinalIgnoreCase) == 0;

				for (int i = 0; i < str.Length && i < match.Length; ++i)
				{
					if (match[i] != str[i])
						return false;
				}
			}
			return true;
		}

		public static bool BeginsWith(this string str, char ch)
		{
			return str.Length > 0 && str[0] == ch;
		}

		public static bool EndsWith(this string str, char ch)
		{
			return str.Length > 0 && str[str.Length - 1] == ch;
		}

		public static bool EndsWith(this string str, string match, bool bIgnoreCase)
		{
			if (str == null || match == null || str.Length < match.Length || match.Length == 0)
				return false;

			if (bIgnoreCase)
			{
				int i = str.Length - match.Length;
				int j = 0;
				for (; i < str.Length && j < match.Length; ++i, ++j)
				{
					if (char.ToUpperInvariant(match[j]) != char.ToUpperInvariant(str[i]))
						return false;
				}
			}
			else
			{
				int i = str.Length - match.Length;
				int j = 0;
				for (; i < str.Length && j < match.Length; ++i, ++j)
				{
					if (match[j] != str[i])
						return false;
				}
			}
			return true;
		}

		public static bool IsNullOrWhiteSpace(this string value)
		{
			if (string.IsNullOrEmpty(value))
				return true;

			for (int i = 0; i < value.Length; ++i)
			{
				if (!char.IsWhiteSpace(value[i]))
					return false;
			}
			return true;
		}

		public static string ConvertLinebreaks(this string text, Linebreak eol = Linebreak.Default)
		{
			var sb = new StringBuilder(text);
			sb.ConvertLinebreaks(eol);
			return sb.ToString();
		}

		public static string Replace(this string text, string oldValue, string newValue, bool ignoreCase)
		{
			if (!ignoreCase)
				return text.Replace(oldValue, newValue);
			var sb = new StringBuilder(text);
			sb.Replace(oldValue, newValue, true);
			return sb.ToString();
		}

		public static string Remove(this string text, string value, bool ignoreCase = false)
		{
			if (!ignoreCase)
				return text.Replace(value, "");
			var sb = new StringBuilder(text);
			sb.Replace(value, "", true);
			return sb.ToString();
		}

		public static string SingleLine(this string value)
		{
			if (value == null || value.Length == 0)
				return value;

			StringBuilder sb = new StringBuilder(value);
			sb.Replace("\n", " ");
			sb.Replace("\r", " ");
			sb.Replace("\t", " ");
			int pos = sb.IndexOf("  ", 0);
			while (pos != -1)
			{
				sb.Remove(pos, 1);
				pos = sb.IndexOf("  ", pos);
			}
			return sb.ToString();
		}

		public static int IndexOfReverse(this string text, string match, int startIndex, bool ignoreCase)
		{
			if (string.IsNullOrEmpty(match))
				return -1;

			int index;
			int length = match.Length;
			if (startIndex < 0 || startIndex >= text.Length)
				startIndex = text.Length - match.Length;

			int maxLength = text.Length;

			Func<char, char, bool> fnComp;
			if (ignoreCase)
				fnComp = (a, b) => char.ToUpperInvariant(a) == char.ToUpperInvariant(b);
			else
				fnComp = (a, b) => a == b;

			for (int i = startIndex; i >= 0 && i < text.Length; --i)
			{
				if (fnComp(text[i], match[0]))
				{
					index = 1;
					while (index < length && (i + index) < maxLength && fnComp(text[i + index], match[index]))
						++index;

					if (index == length)
						return i;
				}
			}

			return -1;
		}

		public static int IndexOfReverse(this string text, char ch, int startIndex)
		{
			int index;
			if (startIndex < 0 || startIndex >= text.Length)
				return -1;

			for (int i = startIndex; i >= 0; --i)
			{
				if (text[i] == ch)
					return i;
			}

			return -1;
		}

		public static int FindIndex(this string text, int pos, Predicate<char> pred)
		{
			for (int i = pos; i < text.Length; ++i)
				if (pred.Invoke(text[i]))
					return i;
			return -1;
		}

		public static int IndexOfAny(this string text, string[] words, int startIndex, StringComparison comparisonType)
		{
			if (words == null)
				return -1;

			foreach (var word in words)
			{
				int idx = text.IndexOf(word, startIndex, comparisonType);
				if (idx != -1)
					return idx;
			}

			return -1;
		}

		public static bool ContainsPhrase(this string text, string phrase, bool ignoreCase = true)
		{
			if (text == null)
				return false;

			return text.IndexOf(phrase, 0, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) != -1;
		}

		public static bool ContainsWholeWord(this string text, string phrase, bool ignoreCase = true)
		{
			if (text == null)
				return false;

			return Utility.FindWholeWord(text, phrase, 0, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal, Utility.WholeWordOptions.None) != -1;
		}
	}
}