using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ginger
{
	public static class Text
	{
		public static string Eval(string text, IContextual context = null, EvalOption options = EvalOption.Default, IRandom randomizer = null)
		{
			Context ctx = context != null ? context.context : null;
			return Process(ContextString.EvaluateString(text, ctx, randomizer), options);
		}

		public static string Eval(string text, IContextual contextual, ContextString.EvaluationConfig evalConfig, EvalOption options = EvalOption.Default)
		{
			Context ctx = contextual != null ? contextual.context : null;
			return Process(ContextString.Evaluate(text, ctx, evalConfig), options);
		}

		[Flags]
		public enum EvalOption
		{
			None			= 0,
			Capitalization	= 1 << 0,
			Whitespace		= 1 << 1,
			Punctuation		= 1 << 2,
			Quotation		= 1 << 3,
			Minimal			= 1 << 4,
			Linebreaks		= 1 << 5,
			NoInternal		= 1 << 6,
			Default			= Capitalization | Whitespace | Punctuation,

			StandardOutputFormatting	= Default | Linebreaks | NoInternal, // System, Persona, Scenario, User
			LimitedOutputFormatting		= Default | NoInternal, // Example, Greeting, Grammar
			StandardBlockFormatting		= Capitalization | Whitespace | Punctuation | NoInternal, // Nodes
			LimitedBlockFormatting		= Whitespace | Punctuation | NoInternal, // Nodes
			LoreFormatting				= Default | Linebreaks,
			OutputFormatting			= Whitespace | Linebreaks | Punctuation,
			ExampleFormatting			= Whitespace | Punctuation | Minimal,
		}

		public static readonly string Space				= "\uFFF0\uFFF1";
		public static readonly string Tab				= "\uFFF0\uFFF2";
		public static readonly string Break				= "\uFFF0\uFFF3";
		public static readonly string ParagraphBreak	= "\uFFF0\uFFF4";
		public static readonly string Separator			= "\uFFF0\uFFF5";
		public static readonly string SoftBreak			= "\uFFF0\uFFF6";
		public static readonly string Delimiter			= "\uFFF0\uFFF7";

		public static StringBuilder Process(StringBuilder sb, EvalOption options = EvalOption.Default)
		{
			if (options == EvalOption.None)
				return sb;

			ProcessMarkDown(sb);

			if (EnumHelper.Contains(options, EvalOption.Punctuation))
				ProcessPunctuation(sb);
			if (EnumHelper.Contains(options, EvalOption.Capitalization))
				ProcessCapitalization(sb);
			if (EnumHelper.Contains(options, EvalOption.Linebreaks))
				ProcessLinebreaks(sb);
			if (EnumHelper.Contains(options, EvalOption.Whitespace))
				ProcessWhitespace(sb, EnumHelper.Contains(options, EvalOption.Linebreaks));
			if (EnumHelper.Contains(options, EvalOption.Quotation))
				ProcessQuotation(sb);
			if (EnumHelper.Contains(options, EvalOption.NoInternal) == false)
				ProcessInternal(sb);
			return sb;
		}

		public static string Process(string s, EvalOption options = EvalOption.Default)
		{
			if (options == EvalOption.None)
				return s;

			var sbOutput = new StringBuilder(s);
			Process(sbOutput, options);
			return sbOutput.ToString();
		}

		public static string DontProcess(string text)
		{
			if (text == null || text.BeginsWith("<!np+>"))
				return text;
			return string.Concat("<!np+>", text, "<!np->");
		}
		
		private static bool SkipNoParse(StringBuilder sb, ref int pos, bool checkInternal = false)
		{
			if (pos < 0 || pos >= sb.Length)
				return false;

			// No parsing <!np+>...<!np->
			if (sb[pos] == '<' && pos < sb.Length - 2 && sb[pos + 1] == '!' && sb.CompareAt(pos, "<!np+>", true))
			{
				int pos_end = sb.IndexOf("<!np->", pos + 6);
				if (pos_end != -1)
				{
					pos = pos_end + 6;
					return true;
				}
			}

			// Internal markers <##...##>
			if (checkInternal)
			{
				if (sb[pos] == '<' && pos < sb.Length - 2 && sb[pos + 1] == '#' && sb.CompareAt(pos, "<##", false))
				{
					int pos_end = sb.IndexOf("##>", pos + 3);
					if (pos_end != -1)
					{
						pos = pos_end + 3;
						return true;
					}
				}
			}
			return false;
		}

		private static string ProcessCapitalization(StringBuilder sbOutput)
		{
			bool bCapitalize = true;
			bool bAbbreviation = false;

			for (int pos = 0; pos < sbOutput.Length;)
			{
				if (SkipNoParse(sbOutput, ref pos)) 
					continue;

				char ch = sbOutput[pos];

				if (ch == '<' && pos < sbOutput.Length - 2 && sbOutput[pos + 1] == '!')
				{
					if (sbOutput.CompareAt(pos, "<!cap>", true))
					{
						bCapitalize = true;
						sbOutput.Remove(pos, 6);
						continue;
					}
					if (sbOutput.CompareAt(pos, "<!nocap>", true))
					{
						bCapitalize = false;
						sbOutput.Remove(pos, 8);
						continue;
					}
				}

				if (ch == '\\' && pos < sbOutput.Length - 2 && sbOutput[pos+1] == '.') // \. Period. Not full stop.
				{
					bCapitalize = false;
					sbOutput.Remove(pos, 1);
					pos += 2;
					continue;
				}

				// Don't capitalize {{...}}
				if (ch == '{' && pos < sbOutput.Length - 2 && sbOutput[pos + 1] == '{')
				{
					int pos_next = Utility.FindEndOfScope(sbOutput.ToString(), pos, '{', '}');
					if (pos_next != -1)
					{
						pos = pos_next + 1;
						continue;
					}
				}

#if false
				// Don't capitalize *actions*
				if (ch == '*') 
				{
					int pos_next = sbOutput.IndexOf('*', pos + 1);
					if (pos_next != -1)
					{
						pos = pos_next + 1;
						continue;
					}
				}
#endif
				// {char}, {user}
				if ((ch == '<'
					&& (sbOutput.CompareAt(pos, "<##CHAR", false) || sbOutput.CompareAt(pos, "<##USER>", false))) )
				{
					bCapitalize = false;
					++pos;
					continue;
				}

				// Capitalize within brackets
			//	if (ch == '{' && CapitalizeInner(sbOutput, ref pos, '{', '}'))
			//		continue;
			//	if (ch == '[' && CapitalizeInner(sbOutput, ref pos, '[', ']'))
			//		continue;
			//	else if (ch == '<' && CapitalizeInner(sbOutput, ref pos, '<', '>'))
			//		continue;

				if (ch == ' ' || ch == '\n' || ch == '\r' || ch == '\t' || ch == '\u00a0' || ch == '\u3000'
					|| ch == '(' || ch == ')' || ch == '\'' || ch == '"' || ch == '\u201C' || ch == '\u201D'
					|| ch == '@' || ch == '$' || ch == '^' || ch == 'µ' || ch == '{' || ch == '}' || ch == '*') // Skip whitespace, some punctuation
				{
					++pos;
					continue;
				}

				if (ch == '.' || ch == '?')
				{
					bCapitalize = true;
					++pos;
					continue;
				}

				if (ch == ',' || ch == ';')
				{
					bCapitalize = false;
					++pos;
					continue;
				}

				if (ch == '!' && (pos == 0 || sbOutput[pos - 1] != '<'))
				{
					bCapitalize = true;
					++pos;
					continue;
				}

				// Capitalize 'i'
				if (ch == 'i' && pos > 0 && pos < sbOutput.Length - 2 
					&& char.IsLetterOrDigit(sbOutput[pos + 1]) == false && char.IsLetterOrDigit(sbOutput[pos - 1]) == false
					&& sbOutput[pos + 1] != '>')
					bCapitalize = true;

				if (char.IsLetterOrDigit(sbOutput[pos]) == false)
				{
					++pos;
					continue;
				}

				// Don't capitalize abbreviations
				if (bCapitalize && pos < sbOutput.Length - 1 && sbOutput[pos + 1] == '.')
				{
					bCapitalize = false;
					bAbbreviation = true;
					++pos;
					continue;
				}

				if (bAbbreviation)
				{
					bCapitalize = false;
					bAbbreviation = false;
					++pos;
					continue;
				}

				if (bCapitalize)
				{
					sbOutput[pos] = char.ToUpper(sbOutput[pos]);
					bCapitalize = false;
				}
				++pos;
			}

			return sbOutput.ToString();
		}

		private static bool CapitalizeInner(StringBuilder sbOutput, ref int pos, char open, char close)
		{
			int pos_end = Utility.FindEndOfScope(sbOutput, pos, open, close);
			if (pos_end != -1)
			{
				StringBuilder inner = new StringBuilder(sbOutput.SubstringToFrom(pos + 1, pos_end - 1));
				sbOutput.RemoveFromTo(pos, pos_end);
				sbOutput.Insert(pos, close);
				sbOutput.Insert(pos, open);
				ProcessCapitalization(inner);
				sbOutput.Insert(pos + 1, inner);
				pos = pos + inner.Length + 2;
				return true;
			}

			return false;
		}

		private static void ProcessWhitespace(StringBuilder sbOutput, bool includeLinebreaks)
		{
			if (sbOutput.Length == 0)
				return;
			
			Func<char, bool> IsWhiteSpace;
			if (includeLinebreaks)
				IsWhiteSpace = (c) => (c == ' ' || c == '\t' || c == '\u00a0' || c == '\u3000' || c == '\r' || c == '\n');
			else
				IsWhiteSpace = (c) => (c == ' ' || c == '\t' || c == '\u00a0' || c == '\u3000');

			// Protect eol whitespace
			sbOutput.Replace("  \r\n", Text.Break);
			sbOutput.Replace("  \n", Text.Break);

			// Collapse whitespace
			int pos_ws = 0;
			for (; pos_ws < sbOutput.Length;)
			{
				if (SkipNoParse(sbOutput, ref pos_ws))
					continue;

				if (sbOutput[pos_ws] >= '\uFFF0')
				{
					pos_ws += 2; // Skip this and next
					continue;
				}

				if (IsWhiteSpace(sbOutput[pos_ws]))
				{
					int next_non_ws = pos_ws;
					for (; next_non_ws < sbOutput.Length; ++next_non_ws)
					{
						if (IsWhiteSpace(sbOutput[next_non_ws]) == false)
							break;
					}

					if (next_non_ws >= sbOutput.Length)
					{
						sbOutput.Length = pos_ws;
						break;
					}

					if (next_non_ws == pos_ws + 1 && sbOutput[pos_ws] == ' ')
					{
						++pos_ws;
						continue; // No change
					}

					sbOutput.ReplaceFromTo(pos_ws, next_non_ws - 1, " ");
				}

				++pos_ws;
			}
		}

		static char[] s_punctuation = new char[] { '.', ',', '!', '?', ':', ';' };
		private static void ProcessPunctuation(StringBuilder sbOutput)
		{
			if (sbOutput.Length == 0)
				return;

			Func<char, bool> IsPunctuation = (char c) =>
			{
				for (int i = 0; i < s_punctuation.Length; ++i)
					if (s_punctuation[i] == c)
						return true;
				return false;
			};

#if false   // Punctuation at end of quotation
			char tmp;
			for (int i = 0; i < sbOutput.Length - 1; ++i)
			{
				if (SkipNoParse(sbOutput, ref i))
					continue;

				if (sbOutput[i] == '"' && (sbOutput[i + 1] == '.' || sbOutput[i + 1] == ',' ))
				{
					// Swap quote and punctuation
					tmp = sbOutput[i];
					sbOutput[i] = sbOutput[i+1];
					sbOutput[i+1] = tmp;
				}
			}
#endif
			sbOutput.Replace("\u2026", "..."); // Ellipsis

			// Periods '.' or '...'
			for (int i = 0; i < sbOutput.Length - 1;)
			{
				if (SkipNoParse(sbOutput, ref i))
					continue;

				if (sbOutput[i] != '.')
				{
					++i;
					continue;
				}

				int count = 1;
				int pos_last = i;
				int pos = i + 1;
				while (pos < sbOutput.Length)
				{
					if (sbOutput[pos] == '.')
					{
						pos_last = pos;
						++pos;
						++count;
						continue;
					}
					if (char.IsWhiteSpace(sbOutput[pos]))
					{
						++pos;
						continue;
					}
					break;
				}

				if (count == 1) // No action needed
				{
					i = i + 1;
				}
				else if (count >= 3) // Ellipsis
				{
					sbOutput.Remove(i, pos_last - i + 1);
					sbOutput.Insert(i, "...");
					i = i + 3;
				}
				else // Not ellipsis
				{
					sbOutput.Remove(i, pos_last - i + 1);
					sbOutput.Insert(i, ".");
					i = i + 1;
				}
			}

			// Only one
			var only_ones = new char[] { ',', ';', ':' };
			foreach (var ch in only_ones)
			{
				for (int i = 0; i < sbOutput.Length - 1;)
				{
					if (SkipNoParse(sbOutput, ref i))
						continue;

					if (sbOutput[i] != ch)
					{
						++i;
						continue;
					}

					int count = 1;
					int pos_last = i;
					int pos = i + 1;
					while (pos < sbOutput.Length)
					{
						if (sbOutput[pos] == ch)
						{
							pos_last = pos;
							++pos;
							++count;
							continue;
						}
						if (char.IsWhiteSpace(sbOutput[pos]))
						{
							++pos;
							continue;
						}
						break;
					}

					if (count == 1) // No action needed
					{
						i = i + 1;
					}
					else
					{
						sbOutput.Remove(i, pos_last - i + 1);
						sbOutput.Insert(i, ch);
						i = i + 1;
					}
				}
			}

			// No other punctuation after ! and ?
			for (int i = 0; i < sbOutput.Length - 1; ++i)
			{

				if (sbOutput[i] != '!' && sbOutput[i] != '?')
					continue;
				
				int pos = i + 1;
				while (pos < sbOutput.Length)
				{
					if (sbOutput[pos] == ',' || sbOutput[pos] == '.')
					{
						sbOutput.Remove(pos, 1);
						continue;
					}
					if (char.IsWhiteSpace(sbOutput[pos++]))
						continue;
					break;
				}
			}

			// Explicit linebreak
			for (int i = sbOutput.Length - 2; i >= 0; --i)
			{
				if (sbOutput[i] != '<' || sbOutput[i + 1] != '!')
					continue;

				int pos_end = sbOutput.IndexOf('>', i + 2);
				if (pos_end == -1)
					continue;

				string word = sbOutput.SubstringToFrom(i + 2, pos_end - 1).ToLowerInvariant();
				if (string.IsNullOrEmpty(word))
					continue;

				// Explicit full stop
				if (word == "fs")
				{
					sbOutput.ReplaceFromTo(i, pos_end, ".");
					continue;
				}

				// Explicit comma
				if (word == "com")
				{
					sbOutput.ReplaceFromTo(i, pos_end, ",");
					continue;
				}
			}

			// Remove whitespace preceding punctuation or line break
			for (int i = 0; i < sbOutput.Length;)
			{
				if (SkipNoParse(sbOutput, ref i))
					continue;

				if (i > 0 && IsPunctuation(sbOutput[i]))
				{
					int pos = i;
					while (pos > 0 && char.IsWhiteSpace(sbOutput[pos - 1]))
					{
						sbOutput.Remove(pos - 1, 1);
						--pos;
					}
					i = pos;
				}
				++i;
			}

			// Ensure there's a blankspace after each punctuation
			for (int i = 0; i < sbOutput.Length - 1;)
			{
				if (SkipNoParse(sbOutput, ref i))
					continue;

				char ch = sbOutput[i];
				if (ch == '!' && i > 0 && sbOutput[i - 1] == '<') // Ignore <!
				{
					i += 1;
					continue;
				}

				char nextCh = sbOutput[i + 1];
				if (ch == ',' && (char.IsWhiteSpace(nextCh) || char.IsPunctuation(nextCh)) == false)
				{
					sbOutput.Insert(i + 1, ' ');
					i += 2;
					continue;
				}
				else if (IsPunctuation(ch) && (char.IsLetter(nextCh) || nextCh == '{' || nextCh == '[' || nextCh == '<' || nextCh == '('))
				{
					sbOutput.Insert(i + 1, ' ');
					i += 2;
					continue;
				}
				++i;
			}

			// Remove superfluous full stops
			for (int i = 0; i < sbOutput.Length;)
			{
				if (SkipNoParse(sbOutput, ref i))
					continue;

				if (sbOutput[i] == '.' && i < sbOutput.Length - 1 && sbOutput[i + 1] == '.')
				{
					if (i < sbOutput.Length - 2 && sbOutput[i + 2] == '.') // Ellipsis?
					{
						i += 3;
						continue;
					}

					sbOutput.Remove(i, 1); // Remove second .
					i += 1;
					continue;
				}
				++i;
			}


			sbOutput.TrimEnd();
		}

		private static void ProcessQuotation(StringBuilder sbOutput)
		{
			if (sbOutput.Length == 0)
				return;

			int pos_quote_begin = sbOutput.IndexOf('"', 0);
			if (pos_quote_begin == -1)
				return;

			char[] inner_marks = new char[] { ',', '.', '"' };

			List<char> marks = new List<char>(8);

			while (pos_quote_begin != -1)
			{
				if (SkipNoParse(sbOutput, ref pos_quote_begin))
					continue;

				int pos_quote_end = sbOutput.IndexOf('"', pos_quote_begin + 1);
				if (pos_quote_end == -1)
					return;

				// Move marks into quotation
				marks.Clear();
				for (int i = pos_quote_end + 1; i < sbOutput.Length; ++i)
				{
					char ch = sbOutput[i];
					if (char.IsWhiteSpace(ch))
						continue;
					if (char.IsPunctuation(ch) == false)
						break;

					if (Array.IndexOf(inner_marks, ch) != -1)
					{
						marks.Add(ch);
						sbOutput.Remove(i--, 1);
						continue;
					}
				}

				for (int i = 0; i < marks.Count; ++i)
					sbOutput.Insert(pos_quote_end++, marks[i]);

				pos_quote_begin = sbOutput.IndexOf('"', pos_quote_begin + 1);
			}
		}

		private static void ProcessInternal(StringBuilder sbOutput)
		{
			for (int i = sbOutput.Length - 2; i >= 0; --i)
			{
				if (sbOutput[i] != '<' || sbOutput[i + 1] != '!')
					continue;

				int pos_end = sbOutput.IndexOf('>', i + 2);
				if (pos_end == -1)
					continue;

				string word = sbOutput.SubstringToFrom(i + 2, pos_end - 1).ToLowerInvariant();
				if (string.IsNullOrEmpty(word))
					continue;

				// Explicit quote
				if (word == "qt")
				{
					sbOutput.ReplaceFromTo(i, pos_end, "\"");
					continue;
				}

				// Unicode literal
				if (word[0] == 'u')
				{
					int begin = i + 3;

					string value = sbOutput.SubstringToFrom(begin, pos_end - 1);
					int iValue;
					if (Int32.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out iValue))
					{
						string replacement;
						try
						{
							replacement = char.ConvertFromUtf32(iValue);
						}
						catch (ArgumentOutOfRangeException)
						{
							replacement = "";
						}
						sbOutput.ReplaceFromTo(i, pos_end, replacement);
					}
					continue;
				}

				sbOutput.ReplaceFromTo(i, pos_end, "");
			}

			// Replace markers
			SubstituteMarkers(sbOutput);

			CleanParagraphs(sbOutput);
		}

		private static void SubstituteMarkers(StringBuilder sbOutput)
		{
			if (sbOutput.IndexOf('\uFFF0', 0) == -1)
				return;

			// Break
			int pos_break = sbOutput.IndexOf(Break, 0);
			while (pos_break != -1)
			{
				int pos_end = pos_break + 2;
				while (pos_end < sbOutput.Length && char.IsWhiteSpace(sbOutput[pos_end]))
					pos_end++;

				sbOutput.Remove(pos_break, pos_end - pos_break);
				sbOutput.Insert(pos_break, "\n");
				pos_break = sbOutput.IndexOf(Break, pos_break);
			}

			// Paragraph
			int pos_pbreak = sbOutput.IndexOf(ParagraphBreak, 0);
			while (pos_pbreak != -1)
			{
				int pos_end = pos_pbreak + 2;
				while (pos_end < sbOutput.Length && char.IsWhiteSpace(sbOutput[pos_end]))
					pos_end++;

				sbOutput.Remove(pos_pbreak, pos_end - pos_pbreak);
				sbOutput.Insert(pos_pbreak, "\n\n");
				pos_pbreak = sbOutput.IndexOf(ParagraphBreak, pos_pbreak);
			}

			// Separator (Space)
			int pos_separator = sbOutput.IndexOf(Separator, 0);
			while (pos_separator != -1)
			{
				if (pos_separator > 0 && char.IsWhiteSpace(sbOutput[pos_separator - 1]))
				{
					sbOutput.Remove(pos_separator, 2);
				}
				else
				{
					sbOutput.Remove(pos_separator, 2);
					sbOutput.Insert(pos_separator, " ");
				}
				pos_separator = sbOutput.IndexOf(Separator, pos_separator);
			}

			// Softbreak (New line)
			int pos_sbreak = sbOutput.IndexOf(SoftBreak, 0);
			while (pos_sbreak != -1)
			{
				if (pos_sbreak > 0 && sbOutput[pos_sbreak - 1] == '\n')
				{
					sbOutput.Remove(pos_sbreak, 2);
				}
				else
				{
					sbOutput.Remove(pos_sbreak, 2);
					sbOutput.Insert(pos_sbreak, "\n");
				}
				pos_sbreak = sbOutput.IndexOf(SoftBreak, pos_sbreak);
			}

			sbOutput.Replace(Space, " ");
			sbOutput.Replace(Tab, "\t");
			sbOutput.Replace(Delimiter, ", ");
		}

		private static class HumanFriendly
		{
			public static readonly string Zero = "zero";
			public static readonly string Negative = "negative";
			public static readonly string Hundred = "hundred";
			public static string[] Ones = new string[] { "", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };
			public static string[] Teens = new string[] { "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
			public static string[] Tens = new string[] { "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
			public static string[] ThousandsGroups = { "", "thousand", "million", "billion" };
		}

		private static string FriendlyInteger(int n, string leftDigits, int thousands)
		{
			if (n == 0)
				return leftDigits;

			string friendlyInt = leftDigits;
			if (friendlyInt.Length > 0)
				friendlyInt += " ";

			if (n < 10)
				friendlyInt += HumanFriendly.Ones[n];
			else if (n < 20)
				friendlyInt += HumanFriendly.Teens[n - 10];
			else if (n < 100)
				friendlyInt += FriendlyInteger(n % 10, HumanFriendly.Tens[n / 10 - 2], 0);
			else if (n < 1000)
				friendlyInt += FriendlyInteger(n % 100, string.Concat(HumanFriendly.Ones[n / 100], " ", HumanFriendly.Hundred), 0);
			else
			{
				friendlyInt += FriendlyInteger(n % 1000, FriendlyInteger(n / 1000, "", thousands+1), 0);
				if (n % 1000 == 0)
					return friendlyInt;
			}

			return string.Concat(friendlyInt, " ", HumanFriendly.ThousandsGroups[thousands]);
		}

		public static string NumeralFromInt(int n)
		{
			if (n == 0)
				return HumanFriendly.Zero;
			else if (n < 0)
				return string.Concat(HumanFriendly.Negative, " ", NumeralFromInt(-n));
			return FriendlyInteger(n, "", 0).TrimEnd();
		}

		private static void ProcessMarkDown(StringBuilder sb)
		{
			int pos_excl = sb.IndexOf("![", 0);
			while (pos_excl != -1)
			{
				int pos_mid = sb.IndexOf("](", pos_excl + 2);
				if (pos_mid == -1)
					break;
				int pos_end = sb.IndexOf(')', pos_mid + 1);
				if (pos_end == -1)
					break;

				sb.Insert(pos_end + 1, "<!np->");
				sb.Insert(pos_excl, "<!np+>");
				pos_excl = sb.IndexOf("![", pos_end + 12);
			}
		}

		public static string AOrAn(string s)
		{
			if (string.IsNullOrEmpty(s))
				return "";

			var ignore_chars = new char[] { ' ', '\t', '\r', '\n', '.', ',', '!', '?', ';', ':', '\'', '"', '/', '\\', '&', '\u00a0', '\u3000', '*', '^', '@', '[', ']', '(', ')', '<', '>' };
			int idx = 0;
			char open = '\0';
			char close = '\0';
			for (; idx < s.Length; ++idx)
			{
				if (s[idx] == '<')
				{
					open = '<';
					close = '>';
				}
				else if (s[idx] == '[')
				{
					open = '[';
					close = ']';
				}
				else if (s[idx] == '{')
				{
					open = '{';
					close = '}';
				}
				else
				{
					open = '\0';
					close = '\0';
				}

				// Filter by scope
				while (open != '\0' && s[idx] == open)
				{
					int endScope = Utility.FindEndOfScope(s, idx, open, close);
					if (endScope != -1)
					{
						idx = endScope;
						continue;
					}
					break;
				}

				if (ignore_chars.Contains(s[idx]) == false)
					break;
			}
			if (idx >= s.Length)
				return "";

			if (idx > 0)
				s = s.Substring(idx);

			// Not sure if this is necessary...
			int endOfWord = s.IndexOfAny(new char[] { ' ', '\t', '\r', '\n', '.', ',', '!', '?', ';', ':', '\'', '"', '/', '\\', '&', '\u00a0', '\u3000', '*', '^', '[', ']', '(', ')', '@' });
			if (endOfWord > 0)
				s = s.Substring(0, endOfWord);

			// Bug fix
			s = s.Replace('-', ' ');
			
			return AvsAnLib.AvsAn.Query(s).Article;
		}

		private static void ProcessLinebreaks(StringBuilder sb)
		{
			// Place temp markers where we want linebreaks to go.
			// These get replaced with real linebreaks in ProcessInternal
			sb.Replace("\r\n\r\n", ParagraphBreak);
			sb.Replace("\n\n", ParagraphBreak);
			sb.Replace("  \n", Break);
			sb.Replace("  \r\n", Break);
		}

		public static void CleanParagraphs(StringBuilder sb)
		{
			var rows = sb.ToString().Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
			if (rows.Length == 1)
				return;

			sb.Clear();
			bool bAllowEmptyRow = false;
			for (int i = 0; i < rows.Length; ++i)
			{
				if (string.IsNullOrWhiteSpace(rows[i]) == false)
				{
					sb.AppendLine(rows[i]);
					bAllowEmptyRow = true;
				}
				else if (bAllowEmptyRow)
				{
					sb.AppendLine();
					bAllowEmptyRow = false;
				}
			}
			sb.TrimEnd();
		}

		public static string Lower(string text)
		{
			StringBuilder sbOutput = new StringBuilder(text);
			int pos_begin = 0;
			int pos_end = text.IndexOf('<', pos_begin);
			if (pos_end == -1)
				return text.ToLowerInvariant();

			while (pos_begin < text.Length)
			{
				if (pos_end == -1)
				{
					sbOutput.Replace(pos_begin, text.Length - pos_begin, sbOutput.Substring(pos_begin, text.Length - pos_begin).ToLowerInvariant());
					break;
				}

				if (sbOutput[pos_end] == '<')
				{
					int pos = pos_end;
					if (SkipNoParse(sbOutput, ref pos, true))
					{
						sbOutput.Replace(pos_begin, pos_end - pos_begin, sbOutput.Substring(pos_begin, pos_end - pos_begin).ToLowerInvariant());
						pos_begin = pos;
						pos_end = text.IndexOf('<', pos_begin);
						continue;
					}
					else
					{
						pos_end = text.IndexOf('<', pos_end + 1);
						continue;
					}
				}

				sbOutput.Replace(pos_begin, pos_end - pos_begin, sbOutput.Substring(pos_begin, pos_end - pos_begin).ToLowerInvariant());
				pos_begin = pos_end;
				pos_end = text.IndexOf('<', pos_begin);
			}

			return sbOutput.ToString();
		}

		public static string Upper(string text)
		{
			StringBuilder sbOutput = new StringBuilder(text);
			int pos_begin = 0;
			int pos_end = text.IndexOf('<', pos_begin);
			if (pos_end == -1)
				return text.ToUpperInvariant();

			while (pos_begin < text.Length)
			{
				if (pos_end == -1)
				{
					sbOutput.Replace(pos_begin, text.Length - pos_begin, sbOutput.Substring(pos_begin, text.Length - pos_begin).ToUpperInvariant());
					break;
				}

				if (sbOutput[pos_end] == '<')
				{
					int pos = pos_end;
					if (SkipNoParse(sbOutput, ref pos, true))
					{
						sbOutput.Replace(pos_begin, pos_end - pos_begin, sbOutput.Substring(pos_begin, pos_end - pos_begin).ToUpperInvariant());
						pos_begin = pos;
						pos_end = text.IndexOf('<', pos_begin);
						continue;
					}
					else
					{
						pos_end = text.IndexOf('<', pos_end + 1);
						continue;
					}
				}

				sbOutput.Replace(pos_begin, pos_end - pos_begin, sbOutput.Substring(pos_begin, pos_end - pos_begin).ToUpperInvariant());
				pos_begin = pos_end;
				pos_end = text.IndexOf('<', pos_begin);
			}

			return sbOutput.ToString();
		}

		public static void ReplaceDecorativeQuotes(ref string text)
		{
			if (string.IsNullOrEmpty(text))
				return;

			if (text.IndexOfAny(new char[] { '\u201C', '\u201D', '\u201E', '\u201F', '\u2019', '\u2032' }) != -1)
			{
				StringBuilder sb = new StringBuilder(text);
				sb.Replace('\u201C', '"');
				sb.Replace('\u201D', '"');
				sb.Replace('\u201E', '"');
				sb.Replace('\u201F', '"');

				// Replace single quotation mark / prime used as apostrophes
				sb.Replace("n\u2019t", "n't");
				sb.Replace("n\u2032t", "n't");
				sb.Replace("\u2019s", "'s");
				sb.Replace("\u2032s", "'s");
				sb.Replace("\u2019d", "'d");
				sb.Replace("\u2032d", "'d");
				sb.Replace("\u2019ve", "'ve");
				sb.Replace("\u2032ve", "'ve");


				text = sb.ToString();
			}
		}
	}

}
