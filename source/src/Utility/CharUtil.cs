
namespace Ginger
{
	public static class CharUtil
	{
		public enum CharacterSet
		{
			Undefined	= -1,
			Default		= 0,
			CJK			= 1,
			Emoji		= 2,

			GeneralPunctuation,
		}

		public static CharacterSet GetCharacterSet(string s, int pos)
		{
			if (s == null || pos < 0 || pos >= s.Length)
				return CharacterSet.Undefined;
			return GetCharacterSet(s[pos]);
		}

		public static CharacterSet GetCharacterSet(char c)
		{
			if (char.IsPunctuation(c))
				return CharacterSet.GeneralPunctuation;

			if (c <= 0xff)
				return CharacterSet.Default;

			if ((c >= '\u3041' && c <= '\u3096')		// Hiragana
				|| (c >= '\u30A0' && c <= '\u30ff')		// Katakana
				|| (c >= '\u4e00' && c <= '\u9fff')		// CJK Unified Ideographs
				|| (c >= '\u3400' && c <= '\u4dbf')		// CJK Unified Ideographs Extension A
				|| (c >= '\uf900' && c <= '\ufa6a')		// Kanji
				|| (c >= '\u2e80' && c <= '\u2eff')		// CJK Radicals Supplement
				|| (c >= '\u2f00' && c <= '\u2fdf')		// CJK Radicals / Kangxi Radicals
				|| (c >= '\uff5f' && c <= '\uff9f')		// Halfwidth katakana & punctuation
				|| (c >= '\u3000' && c <= '\u303f')		// Symbols and punctuation
				|| (c >= '\u31f0' && c <= '\u31ff')		// Miscellaneous
				|| (c >= '\u3220' && c <= '\u3243')		// Miscellaneous
				|| (c >= '\u3280' && c <= '\u337F')		// Miscellaneous
				|| (c >= '\uff01' && c <= '\uff5e')		// Alphanumeric and punctuation
				) 
				return CharacterSet.CJK;

			if ((c >= '\uac00' && c <= '\ud7a3')		// Hangul syllables
				|| (c >= '\u1100' && c <= '\u11ff')		// Hangul Jamo
				|| (c >= '\u3130' && c <= '\u318f')		// Hangul Compatibility Jamo
				|| (c >= '\ua960' && c <= '\ua97f')		// Hangul Jamo Extended-A
				|| (c >= '\ud7b0' && c <= '\ud7ff'))	// Hangul Jamo Extended-B
				return CharacterSet.CJK;

			if ((c >= '\ud800' && c <= '\udbff')		// High surrogates
				|| (c >= '\u2700' && c <= '\u27bf')		// Dingbats (Emoji)
				|| (c == '\ufe0e' || c == '\ufe0f'))	// Variant selector-15, Variant selector-16
				return CharacterSet.Emoji;
			
			if ((c >= '\u2010' && c <= '\u2027')		// General punctuation
				|| (c >= '\u2030' && c <= '\u205f'))
				return CharacterSet.GeneralPunctuation;

			return CharacterSet.Default;
		}
	}
}
