using System.Collections.Generic;
using System.Windows.Forms;

namespace Ginger
{
	public static class ReplaceWord
	{
		#region Tables
		public static Dictionary<string, string> AutoReplacePairs_Character = new Dictionary<string, string>() {
			{"they",        "{they}" },
			{"they'd",      "{they'd}" },
			{"they're",     "{they're}" },
			{"they've",     "{they've}" },
			{"they'll",     "{they'll}" },
			{"them",        "{them}" },
			{"their",       "{their}" },
			{"theirs",      "{theirs}" },
			{"themselves",  "{themselves}" },
			{"she",         "{she}" },
			{"she'd",       "{she'd}" },
			{"she's",       "{she's}" },
			{"she'll",      "{she'll}" },
			{"her",         "{her}" },
			{"hers",        "{hers}" },
			{"herself",     "{herself}" },
			{"he",          "{he}" },
			{"he'd",        "{he'd}" },
			{"he's",        "{he's}" },
			{"he'll",       "{he'll}" },
			{"him",         "{him}" },
			{"his",         "{his}" },
			{"himself",     "{himself}" },

			{"{#they}",          "{they}" },
			{"{#they'd}",        "{they'd}" },
			{"{#they're}",       "{they're}" },
			{"{#they've}",       "{they've}" },
			{"{#they'll}",       "{they'll}" },
			{"{#them}",          "{them}" },
			{"{#their}",         "{their}" },
			{"{#theirs}",        "{theirs}" },
			{"{#themselves}",    "{themselves}" },
			{"{#she}",           "{she}" },
			{"{#she'd}",         "{she'd}" },
			{"{#she's}",         "{she's}" },
			{"{#she'll}",        "{she'll}" },
			{"{#her}",           "{her}" },
			{"{#hers}",          "{hers}" },
			{"{#herself}",       "{herself}" },
			{"{#he}",            "{he}" },
			{"{#he'd}",          "{he'd}" },
			{"{#he's}",          "{he's}" },
			{"{#he'll}",         "{he'll}" },
			{"{#him}",           "{him}" },
			{"{#his}",           "{his}" },
			{"{#himself}",       "{himself}" },

			// Cycle her/them/their
			{"{her}",       "{their}" },
			{"{his}",       "{their}" },
			{"{their}",     "{them}" },
			{"{them}",      "{theirs}" },
			{"{theirs}",    "{their}" },


			// Macros
			{"is",          "{is}" },
			{"are",         "{are}" },
			{"{is}",        "{are}" },
			{"{are}",       "{is}" },
			{"isn't",       "{isn't}" },
			{"aren't",      "{aren't}" },
			{"{isn't}",     "{aren't}" },
			{"{aren't}",    "{isn't}" },
			{"has",          "{has}" },
			{"have",         "{have}" },
			{"{has}",        "{have}" },
			{"{have}",       "{has}" },
			{"hasn't",       "{hasn't}" },
			{"haven't",      "{haven't}" },
			{"{hasn't}",     "{haven't}" },
			{"{haven't}",    "{hasn't}" },
			{"was",          "{was}" },
			{"were",         "{were}" },
			{"{was}",        "{were}" },
			{"{were}",       "{was}" },	
			{"wasn't",       "{wasn't}" },
			{"weren't",      "{weren't}" },
			{"{wasn't}",     "{weren't}" },
			{"{weren't}",    "{wasn't}" },	
			{"do",			 "{do}" },
			{"does",         "{does}" },
			{"{do}",		 "{does}" },
			{"{does}",       "{do}" },
			{"don't",        "{don't}" },
			{"doesn't",      "{doesn't}" },
			{"{don't}",      "{doesn't}" },
			{"{doesn't}",    "{don't}" },
			{"{#is}",		"{is}" },
			{"{#are}",		"{are}" },
			{"{#isn't}",	"{isn't}" },
			{"{#aren't}",	"{aren't}" },
			{"{#has}",		"{has}" },
			{"{#have}",		"{have}" },
			{"{#hasn't}",	"{hasn't}" },
			{"{#haven't}",	"{haven't}" },
			{"{#was}",		"{was}" },
			{"{#were}",		"{were}" },
			{"{#wasn't}",	"{wasn't}" },
			{"{#weren't}",	"{weren't}" },
			{"{#do}",		"{do}" },
			{"{#does}",		"{does}" },
			{"{#don't}",	"{don't}" },
			{"{#doesn't}",	"{doesn't}" },
		};
		public static Dictionary<string, string> AutoReplacePairs_User = new Dictionary<string, string>() {
			{"they",        "{#they}" },
			{"they'd",      "{#they'd}" },
			{"they're",     "{#they're}" },
			{"they've",     "{#they've}" },
			{"they'll",     "{#they'll}" },
			{"them",        "{#them}" },
			{"their",       "{#their}" },
			{"theirs",      "{#theirs}" },
			{"themselves",  "{#themselves}" },
			{"she",         "{#she}" },
			{"she'd",       "{#she'd}" },
			{"she's",       "{#she's}" },
			{"she'll",      "{#she'll}" },
			{"her",         "{#her}" },
			{"hers",        "{#hers}" },
			{"herself",     "{#herself}" },
			{"he",          "{#he}" },
			{"he'd",        "{#he'd}" },
			{"he's",        "{#he's}" },
			{"he'll",       "{#he'll}" },
			{"him",         "{#him}" },
			{"his",         "{#his}" },
			{"himself",		"{#himself}" },

			{"{they}",			"{#they}" },
			{"{they'd}",		"{#they'd}" },
			{"{they're}",		"{#they're}" },
			{"{they've}",		"{#they've}" },
			{"{they'll}",		"{#they'll}" },
			{"{them}",			"{#them}" },
			{"{their}",			"{#their}" },
			{"{theirs}",		"{#theirs}" },
			{"{themselves}",	"{#themselves}" },
			{"{she}",			"{#she}" },
			{"{she'd}",			"{#she'd}" },
			{"{she's}",			"{#she's}" },
			{"{she'll}",		"{#she'll}" },
			{"{her}",			"{#her}" },
			{"{hers}",			"{#hers}" },
			{"{herself}",		"{#herself}" },
			{"{he}",			"{#he}" },
			{"{he'd}",			"{#he'd}" },
			{"{he's}",			"{#he's}" },
			{"{he'll}",			"{#he'll}" },
			{"{him}",			"{#him}" },
			{"{his}",			"{#his}" },
			{"{himself}",		"{#himself}" },

			// Cycle her/them/their
			{"{#her}",		"{#their}" },
			{"{#his}",      "{#their}" },
			{"{#their}",    "{#them}" },
			{"{#them}",		"{#theirs}" },
			{"{#theirs}",	"{#their}" },

			// Macros
			{"is",          "{#is}" },
			{"are",         "{#are}" },
			{"{#is}",        "{#are}" },
			{"{#are}",       "{#is}" },
			{"isn't",       "{#isn't}" },
			{"aren't",      "{#aren't}" },
			{"{#isn't}",     "{#aren't}" },
			{"{#aren't}",    "{#isn't}" },
			{"has",          "{#has}" },
			{"have",         "{#have}" },
			{"{#has}",        "{#have}" },
			{"{#have}",       "{#has}" },
			{"hasn't",       "{#hasn't}" },
			{"haven't",      "{#haven't}" },
			{"{#hasn't}",     "{#haven't}" },
			{"{#haven't}",    "{#hasn't}" },
			{"was",          "{#was}" },
			{"were",         "{#were}" },
			{"{#was}",        "{#were}" },
			{"{#were}",       "{#was}" },
			{"wasn't",       "{#wasn't}" },
			{"weren't",      "{#weren't}" },
			{"{#wasn't}",     "{#weren't}" },
			{"{#weren't}",    "{#wasn't}" },
			{"do",           "{#do}" },
			{"does",         "{#does}" },
			{"{#do}",         "{#does}" },
			{"{#does}",       "{#do}" },
			{"don't",        "{#don't}" },
			{"doesn't",      "{#doesn't}" },
			{"{#don't}",      "{#doesn't}" },
			{"{#doesn't}",    "{#don't}" },

			{"{is}",       "{#is}" },
			{"{are}",      "{#are}" },
			{"{isn't}",    "{#isn't}" },
			{"{aren't}",   "{#aren't}" },
			{"{has}",      "{#has}" },
			{"{have}",     "{#have}" },
			{"{hasn't}",   "{#hasn't}" },
			{"{haven't}",  "{#haven't}" },
			{"{was}",      "{#was}" },
			{"{were}",     "{#were}" },
			{"{wasn't}",   "{#wasn't}" },
			{"{weren't}",  "{#weren't}" },
			{"{do}",       "{#do}" },
			{"{does}",     "{#does}" },
			{"{don't}",    "{#don't}" },
			{"{doesn't}",  "{#doesn't}" },
		};
		public static Dictionary<string, string> AutoReplacePairs_Erase = new Dictionary<string, string>() {
			{"{they}",        "they" },
			{"{they'd}",      "they'd"},
			{"{they're}",     "they're"},
			{"{they've}",     "they've"},
			{"{they'll}",     "they'll"},
			{"{them}",        "them"},
			{"{their}",       "their"},
			{"{theirs}",      "theirs"},
			{"{themselves}",  "themselves"},
			{"{she}",         "she"},
			{"{she'd}",       "she'd"},
			{"{she's}",       "she's"},
			{"{she'll}",      "she'll"},
			{"{her}",         "her"},
			{"{hers}",        "hers"},
			{"{herself}",     "herself"},
			{"{he}",          "he"},
			{"{he'd}",        "he'd"},
			{"{he's}",        "he's"},
			{"{he'll}",       "he'll"},
			{"{him}",         "him"},
			{"{his}",         "his"},
			{"{himself}",     "himself"},
			{"{#they}",        "they" },
			{"{#they'd}",      "they'd"},
			{"{#they're}",     "they're"},
			{"{#they've}",     "they've"},
			{"{#they'll}",     "they'll"},
			{"{#them}",        "them"},
			{"{#their}",       "their"},
			{"{#theirs}",      "theirs"},
			{"{#themselves}",  "themselves"},
			{"{#she}",         "she"},
			{"{#she'd}",       "she'd"},
			{"{#she's}",       "she's"},
			{"{#she'll}",      "she'll"},
			{"{#her}",         "her"},
			{"{#hers}",        "hers"},
			{"{#herself}",     "herself"},
			{"{#he}",          "he"},
			{"{#he'd}",        "he'd"},
			{"{#he's}",        "he's"},
			{"{#he'll}",       "he'll"},
			{"{#him}",         "him"},
			{"{#his}",         "his"},
			{"{#himself}",     "himself"},

			// Macros
			{"{is}",			"is" },
			{"{are}",			"are" },
			{"{isn't}",			"isn't" },
			{"{aren't}",		"aren't" },
			{"{has}",			"has" },
			{"{have}",			"have" },
			{"{hasn't}",		"hasn't" },
			{"{haven't}",		"haven't" },
			{"{was}",			"was" },
			{"{were}",			"were" },
			{"{wasn't}",		"wasn't" },
			{"{weren't}",		"weren't" },
			{"{do}",			"do" },
			{"{does}",			"does" },
			{"{don't}",			"don't" },
			{"{doesn't}",		"doesn't" },
			{"{#is}",			"is" },
			{"{#are}",			"are" },
			{"{#isn't}",		"isn't" },
			{"{#aren't}",		"aren't" },
			{"{#has}",			"has" },
			{"{#have}",			"have" },
			{"{#hasn't}",		"hasn't" },
			{"{#haven't}",		"haven't" },
			{"{#was}",			"was" },
			{"{#were}",			"were" },
			{"{#wasn't}",		"wasn't" },
			{"{#weren't}",		"weren't" },
			{"{#do}",			"do" },
			{"{#does}",			"does" },
			{"{#don't}",		"don't" },
			{"{#doesn't}",		"doesn't" },
		};
		#endregion

		public static string GetWordAtCursor(string text, ref int pos)
		{
			if (pos > 0 && pos < text.Length && !(char.IsLetterOrDigit(text, pos) || text[pos] == '\'' || text[pos] == '}' || text[pos] == '#'))
				pos--;

			int pos_begin = pos;
			int pos_end = pos;
			for (int i = pos; i >= 0; --i)
			{
				if (i == text.Length)
					continue;
				if (!(char.IsLetterOrDigit(text, i) || text[i] == '\'' || text[i] == '{' || text[i] == '}' || text[i] == '#'))
					break;
				pos_begin = i;
			}
			for (int i = pos; i <= text.Length; ++i)
			{
				pos_end = i;
				if (i == text.Length)
					break;
				if (!(char.IsLetterOrDigit(text, i) || text[i] == '\'' || text[i] == '{' || text[i] == '}' || text[i] == '#'))
					break;
			}
			pos = pos_begin;
			return text.Substring(pos_begin, pos_end - pos_begin);
		}

		public enum Option
		{
			Default,
			User,
			Erase,
		}

		public static bool FindReplacement(TextBoxBase textbox, Option option, out int startPos, out int length, out string replacement)
		{
			int pos = textbox.SelectionStart;
			startPos = pos;
			string word = GetWordAtCursor(textbox.Text, ref startPos);

			Dictionary<string, string> table;
			switch (option)
			{
			default:
			case Option.Default:
				table = AutoReplacePairs_Character;
				break;
			case Option.User:
				table = AutoReplacePairs_User;
				break;
			case Option.Erase:
				table = AutoReplacePairs_Erase;
				break;
			}

			if (table.TryGetValue(word.ToLowerInvariant(), out replacement) == false)
			{
				startPos = -1;
				length = 0;
				replacement = default(string);
				return false;
			}

			int posCap = 0;
			if (word[0] == '{')
			{
				posCap += 1;
				if (word[1] == '#')
					posCap += 1;
			}
			bool bUpper0 = char.IsUpper(word, posCap + 0);
			bool bUpper1 = char.IsUpper(word, posCap + 1);

			if (bUpper0 && bUpper1)
				replacement = replacement.ToUpperInvariant();
			else if (bUpper0)
			{
				if (replacement[0] == '{' && replacement[1] == '#')
					replacement = string.Concat(replacement.Substring(0, 2), char.ToUpperInvariant(replacement[2]), replacement.Substring(3));
				else if (replacement[0] == '{')
					replacement = string.Concat(replacement[0], char.ToUpperInvariant(replacement[1]), replacement.Substring(2));
				else
					replacement = string.Concat(char.ToUpperInvariant(replacement[0]), replacement.Substring(1));
			}
			if (pos == textbox.Text.Length)
				replacement = string.Concat(replacement, " ");

			length = word.Length;
			return true;
		}

	}
}
