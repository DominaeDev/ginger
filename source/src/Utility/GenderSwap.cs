using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ginger
{
	public static class GenderSwap
	{
		public enum Pronouns
		{
			Neutral = 0,
			Masculine = 1,
			Feminine = 2,
			Mixed = 3,
			VariableNeutral = 4,
			VariableMasculine = 5,
			VariableFeminine = 6,
			VariableUserNeutral = 7,
			VariableUserMasculine = 8,
			VariableUserFeminine = 9,
		}

		private static string[][] PronounTable	= new string[][] 
		{
			/* Neutral               */	new string[] { "they'll", "they're", "they've", "they'd", "they", "them", "theirs", "their", "themselves", },
			/* Masculine             */	new string[] { "he'll", "he's", "he's", "he'd", "he", "him", "his", "his", "himself", },
			/* Feminine              */	new string[] { "she'll", "she's", "she's", "she'd", "she", "her", "hers", "her", "herself", },
			/* Mixed                 */	new string[] { "he/she'll", "he/she's", "he/she's", "he/she'd", "he/she", "him/her", "his/hers", "his/her", "himself/herself", },
			/* VariableNeutral       */	new string[] { "{they'll}", "{they're}", "{they've}", "{they'd}", "{they}", "{them}", "{theirs}", "{their}", "{themselves}", },
			/* VariableMasculine     */	new string[] { "{he'll}", "{he's}", "{he's}", "{he'd}", "{he}", "{him}", "{theirs}", "{his}", "{himself}", },
			/* VariableFeminine      */	new string[] { "{she'll}", "{she's}", "{she's}", "{she'd}", "{she}", "{them}", "{hers}", "{her}", "{herself}", },
			/* VariableUserNeutral   */	new string[] { "{#they'll}", "{#they're}", "{#they've}", "{#they'd}", "{#they}", "{#them}", "{#theirs}", "{#their}", "{#themselves}", },
			/* VariableUserMasculine */	new string[] { "{#he'll}", "{#he's}", "{#he's}", "{#he'd}", "{#he}", "{#him}", "{#theirs}", "{#his}", "{#himself}", },
			/* VariableUserFeminine  */	new string[] { "{#she'll}", "{#she's}", "{#she's}", "{#she'd}", "{#she}", "{#them}", "{#hers}", "{#her}", "{#herself}", },
		};

		private static bool[][] MaskTable = new bool[][]
		{
			/* Neutral               */	new bool[] { true, true, true, true, true, true, true, true, true, },
			/* Masculine             */	new bool[] { true, true, true, true, true, true, false, true, true, },
			/* Feminine              */	new bool[] { true, true, true, true, true, false, true, true, true, },
			/* Mixed                 */	new bool[] { true, true, true, true, true, true, true, true, true, },
			/* VariableNeutral       */	new bool[] { true, true, true, true, true, true, true, true, true, },
			/* VariableMasculine     */	new bool[] { true, true, true, true, true, true, false, true, true, },
			/* VariableFeminine      */	new bool[] { true, true, true, true, true, false, true, true, true, },
			/* VariableUserNeutral   */	new bool[] { true, true, true, true, true, true, true, true, true, },
			/* VariableUserMasculine */	new bool[] { true, true, true, true, true, true, false, true, true, },
			/* VariableUserFeminine  */	new bool[] { true, true, true, true, true, false, true, true, true, },
		};

		private static string[] CharacterIntermediate = new string[] { "{ey'll__}", "{ey's__}", "{ey've__}", "{ey'd__}", "{ey__}", "{em__}", "{eirs__}", "{eir__}", "{emself__}", };
		private static string[] UserIntermediate = new string[] { "{ey'll__u}", "{ey's__u}", "{ey've__u}", "{ey'd__u}", "{ey__u}", "{em__u}", "{eirs__u}", "{eir__u}", "{emself__u}", };

		public static int SwapGenders(IEnumerable<Recipe> recipes, Pronouns characterFrom, Pronouns characterTo, Pronouns userFrom, Pronouns userTo, bool swapCharacter, bool swapUser)
		{
			int replacements = 0;

			string[] fromCharacterPronouns = PronounTable[EnumHelper.ToInt(characterFrom)];
			bool[] fromCharacterMask = MaskTable[EnumHelper.ToInt(characterFrom)];
			string[] targetCharacterPronouns = PronounTable[EnumHelper.ToInt(characterTo)];
			string[] fromUserPronouns = PronounTable[EnumHelper.ToInt(userFrom)];
			bool[] fromUserMask = MaskTable[EnumHelper.ToInt(userFrom)];
			string[] targetUserPronouns = PronounTable[EnumHelper.ToInt(userTo)];

			foreach (var recipe in recipes)
			{
				foreach (var parameter in recipe.parameters.OfType<TextParameter>())
				{
					if (string.IsNullOrEmpty(parameter.value))
						continue;

					if (swapCharacter)
					{
						if (characterFrom == Pronouns.VariableNeutral) // {he}, {she} = {they}
						{
							string[] masculinePronouns = PronounTable[EnumHelper.ToInt(Pronouns.VariableMasculine)];
							string[] femininePronouns = PronounTable[EnumHelper.ToInt(Pronouns.VariableFeminine)];
							for (int i = 0; i < masculinePronouns.Length; ++i)
							{
								replacements += ReplacePronoun(ref parameter.value, masculinePronouns[i], CharacterIntermediate[i], false);
								replacements += ReplacePronoun(ref parameter.value, femininePronouns[i], CharacterIntermediate[i], false);
							}
						}

						for (int i = 0; i < fromCharacterPronouns.Length && i < fromCharacterMask.Length; ++i)
						{
							if (fromCharacterMask[i])
								replacements += ReplacePronoun(ref parameter.value, fromCharacterPronouns[i], CharacterIntermediate[i], false);
						}
					}

					if (swapUser)
					{
						if (userFrom == Pronouns.VariableUserNeutral) // {#he}, {#she} = {#they}
						{
							string[] masculinePronouns = PronounTable[EnumHelper.ToInt(Pronouns.VariableUserMasculine)];
							string[] femininePronouns = PronounTable[EnumHelper.ToInt(Pronouns.VariableUserFeminine)];
							for (int i = 0; i < masculinePronouns.Length; ++i)
							{
								replacements += ReplacePronoun(ref parameter.value, masculinePronouns[i], UserIntermediate[i], true);
								replacements += ReplacePronoun(ref parameter.value, femininePronouns[i], UserIntermediate[i], true);
							}
						}

						for (int i = 0; i < fromUserPronouns.Length && i < fromUserMask.Length; ++i)
						{
							if (fromUserMask[i])
								replacements += ReplacePronoun(ref parameter.value, fromUserPronouns[i], UserIntermediate[i], true);
						}
					}

					if (swapCharacter)
					{
						for (int i = 0; i < CharacterIntermediate.Length && i < targetCharacterPronouns.Length; ++i)
							ReplacePronoun(ref parameter.value, CharacterIntermediate[i], targetCharacterPronouns[i], false);
					}

					if (swapUser)
					{
						for (int i = 0; i < UserIntermediate.Length && i < targetUserPronouns.Length; ++i)
							ReplacePronoun(ref parameter.value, UserIntermediate[i], targetUserPronouns[i], true);
					}
				}
			}

			return replacements;
		}

		public static int SwapGenders(ref string text, Pronouns characterFrom, Pronouns characterTo, Pronouns userFrom, Pronouns userTo, bool swapCharacter, bool swapUser)
		{
			if (string.IsNullOrEmpty(text))
				return 0;

			int replacements = 0;

			string[] fromCharacterPronouns = PronounTable[EnumHelper.ToInt(characterFrom)];
			bool[] fromCharacterMask = MaskTable[EnumHelper.ToInt(characterFrom)];
			string[] targetCharacterPronouns = PronounTable[EnumHelper.ToInt(characterTo)];
			string[] fromUserPronouns = PronounTable[EnumHelper.ToInt(userFrom)];
			bool[] fromUserMask = MaskTable[EnumHelper.ToInt(userFrom)];
			string[] targetUserPronouns = PronounTable[EnumHelper.ToInt(userTo)];

			if (swapCharacter)
			{
				if (characterFrom == Pronouns.VariableNeutral) // {he}, {she} = {they}
				{
					string[] masculinePronouns = PronounTable[EnumHelper.ToInt(Pronouns.VariableMasculine)];
					string[] femininePronouns = PronounTable[EnumHelper.ToInt(Pronouns.VariableFeminine)];
					for (int i = 0; i < masculinePronouns.Length; ++i)
					{
						replacements += ReplacePronoun(ref text, masculinePronouns[i], CharacterIntermediate[i], false);
						replacements += ReplacePronoun(ref text, femininePronouns[i], CharacterIntermediate[i], false);
					}
				}

				for (int i = 0; i < fromCharacterPronouns.Length && i < fromCharacterMask.Length; ++i)
				{
					if (fromCharacterMask[i])
						replacements += ReplacePronoun(ref text, fromCharacterPronouns[i], CharacterIntermediate[i], false);
				}
			}

			if (swapUser)
			{
				if (userFrom == Pronouns.VariableUserNeutral) // {#he}, {#she} = {#they}
				{
					string[] masculinePronouns = PronounTable[EnumHelper.ToInt(Pronouns.VariableUserMasculine)];
					string[] femininePronouns = PronounTable[EnumHelper.ToInt(Pronouns.VariableUserFeminine)];
					for (int i = 0; i < masculinePronouns.Length; ++i)
					{
						replacements += ReplacePronoun(ref text, masculinePronouns[i], UserIntermediate[i], true);
						replacements += ReplacePronoun(ref text, femininePronouns[i], UserIntermediate[i], true);
					}
				}

				for (int i = 0; i < fromUserPronouns.Length && i < fromUserMask.Length; ++i)
				{
					if (fromUserMask[i])
						replacements += ReplacePronoun(ref text, fromUserPronouns[i], UserIntermediate[i], true);
				}
			}

			if (swapCharacter)
			{
				for (int i = 0; i < CharacterIntermediate.Length && i < targetCharacterPronouns.Length; ++i)
					ReplacePronoun(ref text, CharacterIntermediate[i], targetCharacterPronouns[i], false);
			}

			if (swapUser)
			{
				for (int i = 0; i < UserIntermediate.Length && i < targetUserPronouns.Length; ++i)
					ReplacePronoun(ref text, UserIntermediate[i], targetUserPronouns[i], true);
			}

			return replacements;
		}

		private static int ReplacePronoun(ref string text, string pronoun, string replacement, bool user)
		{
			// Disambiguate her -> their / them
			int count = ReplaceHerThem(ref text, pronoun, user);
			// Disambiguate his -> their / theirs
			count += ReplaceHisTheirs(ref text, pronoun, user);

			int[] matches = Utility.FindWholeWords(text, pronoun, true);
			if (matches == null || matches.Length == 0)
				return 0;

			int len = pronoun.Length;
			StringBuilder sb = new StringBuilder(text);
			for (int i = matches.Length - 1; i >= 0; --i)
			{
				int pos = matches[i];

				var capitalization = GetCapitalization(text, pos);
				sb.Remove(pos, len);
				sb.Insert(pos, Capitalize(replacement, capitalization));
			}
			text = sb.ToString();
			count += matches.Length;
			return count;
		}

		private static int ReplaceHerThem(ref string text, string pronoun, bool user)
		{
			string replacement;
			if (string.Compare(pronoun, "her", StringComparison.OrdinalIgnoreCase) == 0)
				replacement = user ? "{em__u}" : "{em__}";
			else
				return 0; // Skip

			int[] matches = Utility.FindWholeWords(text, pronoun, true);
			if (matches == null || matches.Length == 0)
				return 0;

			int len = pronoun.Length;
			StringBuilder sb = new StringBuilder(text);
			for (int i = matches.Length - 1; i >= 0; --i)
			{
				int pos = matches[i];
				if (IsThem(text, pos + 3, true) == false)
					continue;

				var capitalization = GetCapitalization(text, pos);
				sb.Remove(pos, len);
				sb.Insert(pos, Capitalize(replacement, capitalization));
			}
			text = sb.ToString();
			return matches.Length;
		}

		private static int ReplaceHisTheirs(ref string text, string pronoun, bool user)
		{
			string replacement;
			if (string.Compare(pronoun, "his", StringComparison.OrdinalIgnoreCase) == 0)
				replacement = user ? "{eirs__u}" : "{eirs__}";
			else
				return 0; // Skip

			int[] matches = Utility.FindWholeWords(text, pronoun, true);
			if (matches == null || matches.Length == 0)
				return 0;

			int len = pronoun.Length;
			StringBuilder sb = new StringBuilder(text);
			for (int i = matches.Length - 1; i >= 0; --i)
			{
				int pos = matches[i];
				if (IsThem(text, pos + 3, false) == false)
					continue;

				var capitalization = GetCapitalization(text, pos);
				sb.Remove(pos, len);
				sb.Insert(pos, Capitalize(replacement, capitalization));
			}
			text = sb.ToString();
			return matches.Length;
		}

		private static HashSet<string> ThemWords = new HashSet<string>() {
			// Articles
			"a", "an", "the",
			// Prepropositions
			"aboard", "about", "above", "across", "after", "against", "along", "amid",
			"among", "anti", "around", "as", "at", "before", "behind", "below", "beneath",
			"beside", "besides", "between", "beyond", "but", "by", "concerning", "considering",
			"despite", "down", "during", "except", "excepting", "excluding", "following",
			"for", "from", "in", "inside", "into", "like", "minus", "near", "of", "so",
			"off", "on", "onto", "opposite", "outside", "over", "past", "per",
			"plus", "regarding", "round", "save", "since", "than", "through", "to", "toward", "towards",
			"under", "underneath", "unlike", "until", "up", "upon", "versus", "via",
			"with", "within", "without",
			// Demonstrative determiners
			"this", "that", "these", "those",
			// Possessive determiners
			"my", "his", "her", "its", "our", "their", "your",
			// Interrogative determiners
			"whose", "what", "which",
			// Quantifiers
			"all", "every", "many", "much", "some", "few", "any", "no",
			"each", "either", "neither", "more", "less", "fewer", "fewest",
			// Adverbs
			"afterwards", "almost", "already", "also", "always", "anyway", "away", "close", "downstairs", "enough", 
			"ever", "everywhere", "far", "hence", "here", "however", "instead", "just", "later", "likewise", "lots", 
			"moreover", "never", "nevertheless", "now", "often", "once", "overseas", "sometimes", "then", "there", 
			"today", "tomorrow", "tonight", "twice", "upstairs", "yesterday", "yet",
		};

		private static bool IsThem(string text, int pos, bool feminine)
		{
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
					if (char.IsWhiteSpace(s[i]) || char.IsPunctuation(s, i))
						return i;
				}
				return s.Length;
			};

			int pos_start = fnFindNextWord(pos, text);
			if (pos_start == -1 || char.IsPunctuation(text, pos_start))
				return true;

			int pos_end = fnFindEndOfWord(pos_start, text);
			string word = text.Substring(pos_start, pos_end - pos_start).Trim();

			// Special case "Everyone calls her [Name]"
			if (feminine 
				&& (word == Current.Character.namePlaceholder 
					|| word == Current.Card.userPlaceholder 
					|| GetCapitalization(word) == Capitalization.Capitalized))
				return true;

			word = word.ToLowerInvariant();

			if (word.Length == 0)
				return true;
			if (ThemWords.Contains(word))
				return true;
			if (word.EndsWith("ly")) // Adverb?
			{
				// ..at the end of the sentence
				int pos_next = fnFindNextWord(pos_end, text);
				if (pos_next == -1 || char.IsPunctuation(text, pos_next))
					return true;
			}
			if (word.EndsWith("ing")) //[verb]ing?
				return true;

			return false;
		}

		private enum Capitalization
		{
			None,
			AllCaps,
			Capitalized,
		}

		private static Capitalization GetCapitalization(string text)
		{
			return GetCapitalization(text, 0);
		}

		private static Capitalization GetCapitalization(string text, int pos)
		{
			if (text == null || text.Length < 2)
				return Capitalization.None;

			if (text[pos] == '{') // {them}
			{
				pos += 1;
				if (text[pos] == '#') // {#them}
					pos += 1;
			}
			if (pos >= text.Length - 1)
				return Capitalization.None;

			bool bUpper0 = char.IsUpper(text, pos);
			bool bUpper1 = char.IsUpper(text, pos + 1);

			if (!bUpper0 && !bUpper1)
				return Capitalization.None;
			else if (!bUpper1)
				return Capitalization.Capitalized;
			return Capitalization.AllCaps;
		}

		private static string Capitalize(string replacement, Capitalization capitalization)
		{
			int posCap = 0;
			if (replacement[0] == '{') // {them}
			{
				posCap += 1;
				if (replacement[1] == '#') // {#them}
					posCap += 1;
			}

			switch (capitalization)
			{
			default:
				return replacement;
			case Capitalization.AllCaps:
				return replacement.ToUpperInvariant();
			case Capitalization.Capitalized:
				StringBuilder sb = new StringBuilder(replacement);
				sb[posCap] = char.ToUpperInvariant(replacement[posCap]);
				return sb.ToString();
			}
		}

		public static bool IsFutanari(string gender)
		{
			if (string.IsNullOrEmpty(gender))
				return false;

			string[] synonyms = new string[] {
				"futanari", "futa", "shemale", "she-male", 
				"hermaphrodite", "herm",
				"dickgirl", "dick girl", "dick-girl", 
				"ladyboy", "lady boy", "lady-boy",
			};

			return synonyms.Contains(gender.ToLowerInvariant());
		}

		public static Pronouns PronounsFromGender(string gender)
		{
			if (string.IsNullOrEmpty(gender))
				return Pronouns.Neutral;
			if (string.Compare(gender, "male", true) == 0)
				return Pronouns.Masculine;
			if (string.Compare(gender, "female", true) == 0 || IsFutanari(gender))
				return Pronouns.Feminine;
			if (string.Compare(gender, "any", true) == 0)
				return Pronouns.Mixed;
			return Pronouns.Neutral;
		}

		public static Pronouns PronounsFromGender(CharacterData character)
		{
			if (string.IsNullOrEmpty(character.gender)) // Not set
				return Pronouns.Neutral;
			if (string.Compare(character.gender, "male", true) == 0)
				return Pronouns.Masculine;
			if (string.Compare(character.gender, "female", true) == 0 || IsFutanari(character.gender))
				return Pronouns.Feminine;
			return Pronouns.Neutral;
		}

		public static bool ToNeutralMarkers(ref string text)
		{
			Pronouns characterFrom = PronounsFromGender(Current.Character);
			Pronouns userFrom = PronounsFromGender(Current.Card.userGender);

			bool bSwapCharacter = string.IsNullOrEmpty(Current.Character.gender) == false;
			bool bSwapUser = string.IsNullOrEmpty(Current.Card.userGender) == false;
			bSwapUser &= characterFrom != userFrom;

			return SwapGenders(ref text, characterFrom, Pronouns.VariableNeutral, userFrom, Pronouns.VariableUserNeutral, bSwapCharacter, bSwapUser) > 0;
		}

		public static bool FromNeutralMarkers(ref string text)
		{
			Pronouns characterTo = PronounsFromGender(Current.Character);
			Pronouns userTo = PronounsFromGender(Current.Card.userGender);

			bool bSwapCharacter = string.IsNullOrEmpty(Current.Character.gender) == false;
			bool bSwapUser = string.IsNullOrEmpty(Current.Card.userGender) == false;
			bSwapUser &= characterTo != userTo;

			return SwapGenders(ref text, Pronouns.VariableNeutral, characterTo, Pronouns.VariableUserNeutral, userTo, bSwapCharacter, bSwapUser) > 0;
		}
	}
}
