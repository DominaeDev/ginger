using System.Text;

namespace Ginger
{
	public static class NormalizeSpecialChars
	{
		private static char[] SpecialChars = new char[50]
		{ 
			'\u0060',	// [00] Grave accent
			'\u00B4',	// [01] Acute accent
			'\u2018',	// [02] Left single quotation mark
			'\u2019',	// [03] Right single quotation mark
			'\u201A',	// [04] Single low-9 quotation mark
			'\u201B',	// [05] Single high-reversed-9 quotation mark
			'\u2032',	// [06] Prime
			'\u2035',	// [07] Reversed prime
			'\u201C',	// [08] Left double quotation mark
			'\u201D',	// [09] Right double quotation mark
			'\u201E',	// [10] Double low-0 quotation mark
			'\u201F',	// [11] Double high-reversed-9 quotation mark
			'\u2033',	// [12] Double prime
			'\u2034',	// [13] Triple prime
			'\u2057',	// [14] Quadruple prime
			'\u2036',	// [15] Reversed double prime
			'\u2037',	// [16] Reversed triple prime
			'\u2024',	// [17] One dot leader
			'\u2025',	// [18] Two dot leader
			'\u2026',	// [19] Horizontal ellipsis
			'\u203C',	// [20] Double exclamation mark
			'\u2047',	// [21] Double question mark
			'\u2048',	// [22] Question exclamation mark
			'\u2049',	// [23] Exclamation question mark
			'\u2010',	// [24] Hyphen
			'\u2027',	// [25] Hyphenation point
			'\u2043',	// [26] Hyphen bullet
			'\u2028',	// [27] Line separator
			'\u2029',	// [28] Paragraph separator
			'\u2039',	// [29] Single left-pointing angle quotation mark
			'\u203A',	// [30] Single right-pointing angle quotation mark
			'\u204F',	// [31] Reversed semicolon
			'\u2055',	// [32] Flower punctuation mark
			'\u204E',	// [33] Low asterisk
			'\u20F0',	// [34] Combining asterisk above
			'\u2217',	// [35] Asterisk operator
			'\u2731',	// [36] Heavy asterisk
			'\u273B',	// [37] Teardrop-spoked asterisk
			'\u273C',	// [38] Open center teardrop-spoked asterisk
			'\u273D',	// [39] Heavy teardrop-spoked asterisk
			'\uFE61',	// [40] Small asterisk
			'\uFF0A',	// [41] Fullwidth asterisk
			'\u2240',	// [42] Fraction slash
			'\u275B',	// [43] Heavy single turned comma quotation mark ornament
			'\u275C',	// [44] Heavy single comma quotation mark ornament
			'\u275D',	// [45] Heavy double turned comma quotation mark ornament
			'\u275E',	// [46] Heavy double comma quotation mark ornament
			'\uFF02',	// [47] Fullwidth quotation mark
			'\uFF07',	// [48] Fullwidth apostrophe
			'\u2053',	// [49] Swung dash
		};

		private static string[] Normalized = new string[50]
		{
			"'",		// [00] Grave accent
			"'",		// [01] Acute accent
			"'",		// [02] Left single quotation mark
			"'",		// [03] Right single quotation mark
			",",		// [04] Single low-9 quotation mark
			"'",		// [05] Single high-reversed-9 quotation mark
			"'",		// [06] Prime
			"'",		// [07] Reversed prime
			"\"",		// [08] Left double quotation mark
			"\"",		// [09] Right double quotation mark
			"\"",		// [10] Double low-0 quotation mark
			"\"",		// [11] Double high-reversed-9 quotation mark
			"\"",		// [12] Double prime
			"'\"",		// [13] Triple prime
			"\"\"",		// [14] Quadruple prime
			"\"",		// [15] Reversed double prime
			"\"'",		// [16] Reversed triple prime
			".",		// [17] One dot leader
			"..",		// [18] Two dot leader
			"...",		// [19] Horizontal ellipsis
			"!!",		// [20] Double exclamation mark
			"??",		// [21] Double question mark
			"?!",		// [22] Question exclamation mark
			"!?",		// [23] Exclamation question mark
			"-",		// [24] Hyphen
			"-",		// [25] Hyphenation point
			"-",		// [26] Hyphen bullet
			"\n",		// [27] Line separator
			"\n\n",		// [28] Paragraph separator
			"<",		// [29] Single left-pointing angle quotation mark
			">",		// [30] Single right-pointing angle quotation mark
			";",		// [31] Reversed semicolon
			"*",		// [32] Flower punctuation mark
			"*",		// [33] Low asterisk
			"*",		// [34] Combining asterisk above
			"*",		// [35] Asterisk operator
			"*",		// [36] Heavy asterisk
			"*",		// [37] Teardrop-spoked asterisk
			"*",		// [38] Open center teardrop-spoked asterisk
			"*",		// [39] Heavy teardrop-spoked asterisk
			"*",		// [40] Small asterisk
			"*",		// [41] Fullwidth asterisk
			"/",		// [42] Fraction slash
			"'",		// [43] Heavy single turned comma quotation mark ornament
			"'",		// [44] Heavy single comma quotation mark ornament
			"\"",		// [45] Heavy double turned comma quotation mark ornament
			"\"",		// [46] Heavy double comma quotation mark ornament
			"\"",		// [47] Fullwidth quotation mark
			"'",		// [48] Fullwidth apostrophe
			"~",		// [49] Swung dash
		};

		public static void Normalize(StringBuilder sb)
		{
			for (int pos = sb.Length - 1; pos >= 0; --pos)
			{
				char ch = sb[pos];
				for (int j = 0; j < SpecialChars.Length; ++j)
				{
					if (ch == SpecialChars[j])
					{
						sb.Remove(pos, 1);
						sb.Insert(pos, Normalized[j]);
						break;
					}
				}
			}

			sb.Replace("\'\"", "\"");
			sb.Replace("\"\'", "\"");
		}
	}
}
