using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ginger
{
	public static class FindReplace
	{
		public static int Replace(ref string text, string word, string replacement, bool bWholeWord, bool bIgnoreCase)
		{
			int[] matches;
			if (bWholeWord)
				matches = Utility.FindWholeWords(text, word, bIgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
			else
				matches = Utility.FindWords(text, word, bIgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

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
