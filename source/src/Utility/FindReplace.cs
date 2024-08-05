using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ginger
{
	public static class FindReplace
	{
		private static int[] FindWords(string text, string word, bool bIgnoreCase)
		{
			if (string.IsNullOrEmpty(word))
				return null;

			List<int> found = new List<int>();
			int pos = text.IndexOf(word, 0, bIgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
			while (pos != -1)
			{
				found.Add(pos);
				pos = text.IndexOf(word, pos + word.Length, bIgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
			}
			return found.ToArray();
		}

		private static int[] FindWholeWords(string text, string word, bool ignoreCase = false)
		{
			if (string.IsNullOrEmpty(word))
				return null;

			List<int> found = new List<int>();
			int pos = text.IndexOf(word, 0, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
			while (pos != -1)
			{
				char? left = null;
				char? right = null;
				if (pos > 0) left = text[pos - 1];
				if (pos + word.Length < text.Length) right = text[pos + word.Length];

				bool whole = (!left.HasValue || char.IsWhiteSpace(left.Value) || !(char.IsLetter(left.Value)))
					&& (!right.HasValue || char.IsWhiteSpace(right.Value) || !(char.IsLetter(right.Value)));

				if (whole)
				{
					found.Add(pos);
					pos = text.IndexOf(word, pos + word.Length, StringComparison.OrdinalIgnoreCase);
					continue;
				}
				pos = text.IndexOf(word, pos + 1, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
			}
			return found.ToArray();
		}

		public static int Replace(ref string text, string word, string replacement, bool bWholeWord, bool bIgnoreCase)
		{
			int[] matches;
			if (bWholeWord)
				matches = FindWholeWords(text, word, bIgnoreCase);
			else
				matches = FindWords(text, word, bIgnoreCase);

			if (matches == null || matches.Length == 0)
				return 0;

			int len = word.Length;
			StringBuilder sb = new StringBuilder(text);
			for (int i = matches.Length - 1; i >= 0; --i)
			{
				int pos = matches[i];
				sb.Remove(pos, len);
				sb.Insert(pos, replacement);
			}
			text = sb.ToString();
			return matches.Length;
		}

		public static int Replace(IEnumerable<Recipe> recipes, string word, string replacement, bool bWholeWord, bool bIgnoreCase, bool bIncludeLorebooks)
		{
			if (string.IsNullOrEmpty(word))
				return 0;

			int replacements = 0;

			foreach (var recipe in recipes)
			{
				// Text parameters
				foreach (var parameter in recipe.parameters.OfType<TextParameter>())
				{
					if (string.IsNullOrEmpty(parameter.value))
						continue;

					replacements += Replace(ref parameter.value, word, replacement, bWholeWord, bIgnoreCase);
				}

				// Lorebooks
				if (bIncludeLorebooks)
				{
					foreach (var entry in recipe.parameters.OfType<LorebookParameter>().SelectMany(p => p.value.entries))
					{
						// Key
						string value = entry.key;
						int n = Replace(ref value, word, replacement, bWholeWord, bIgnoreCase);
						if (n > 0) 
						{
							entry.key = value;
							replacements += n;
						}

						// Value
						value = entry.value;
						n = Replace(ref value, word, replacement, bWholeWord, bIgnoreCase);
						if (n > 0) 
						{
							entry.value = value;
							replacements += n;
						}
					}
				}
			}

			return replacements;
		}
	}
}
