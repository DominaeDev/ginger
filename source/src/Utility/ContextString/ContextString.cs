using Ginger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Ginger
{
	public class ContextString
	{
		public static IRandom DefaultRandomizer = new RandomDefault((int)DateTime.Now.Ticks ^ 911249839);

		public string value;

		public ContextString(string value)
		{
			this.value = value;
		}

		public class EvaluationConfig
		{
			public IMacroSupplier[] macroSuppliers;
			public IStringReferenceSupplier[] referenceSuppliers;
			public IRuleSupplier[] ruleSuppliers;
			public IValueSupplier[] valueSuppliers;
			public IRandom randomizer;
		}

		public string Evaluate(Context ctx = null, IRandom randomizer = null)
		{
			if (randomizer != null)
				return __Evaluate(value, ctx, new EvaluationConfig() { randomizer = randomizer });
			return __Evaluate(value, ctx, null);
		}

		public string Evaluate(Context ctx, EvaluationConfig config)
		{
			return __Evaluate(value, ctx, config);
		}

		public static string Evaluate(string value, Context ctx, EvaluationConfig config)
		{
			return new ContextString(value).Evaluate(ctx, config);
		}

		private string __Evaluate(string s, Context ctx, EvaluationConfig evalConfig)
		{
			Internal_Context internalContext;

			if (ctx != null)
				internalContext = new Internal_Context(ctx);
			else
				internalContext = new Internal_Context(Context.CreateEmpty());

			if (evalConfig != null)
			{
				internalContext.referenceStringBanks = evalConfig.referenceSuppliers;
				internalContext.macroSuppliers = evalConfig.macroSuppliers;
				internalContext.ruleSuppliers = evalConfig.ruleSuppliers;
				internalContext.valueSuppliers = evalConfig.valueSuppliers;
				internalContext.randomizer = evalConfig.randomizer ?? DefaultRandomizer;
			}
			else
			{
				internalContext.referenceStringBanks = null;
				internalContext.macroSuppliers = null;
				internalContext.ruleSuppliers = null;
				internalContext.valueSuppliers = null;
				internalContext.randomizer = DefaultRandomizer;
			}

			var sb = Evaluate(new StringBuilder(s), internalContext);
			__PostProcess(sb, internalContext);
			return sb.ToString();
		}

		private string Evaluate(string input, Internal_Context internalContext)
		{
			var sb = Evaluate(new StringBuilder(input), internalContext);
			__PostProcess(sb, internalContext);
			return sb.ToString();
		}

		private StringBuilder Evaluate(StringBuilder sbInput, Internal_Context internalContext)
		{
			if (internalContext == null)
				return sbInput;

			int pos = 0;
			int loop_count = 0;
			while (pos < sbInput.Length)
			{
				int beginBracket = sbInput.IndexOfAny(new char[] { '[', '{', ']', '}' }, pos);
				if (beginBracket < 0)
					break;

				char ch = sbInput[beginBracket];
				
				// Escaped bracket 
				if (beginBracket < sbInput.Length - 1)
				{
					if (ch == '[' || ch == ']' || ch == '{' || ch == '}')
					{
						if (sbInput[beginBracket + 1] == ch) // Double backet: Escaped
						{ 
							sbInput.Remove(beginBracket, 1);
							pos = beginBracket + 1;
							continue;
						}
					}
				}
				
				if (ch == '}' || ch == ']')
				{
					// Must have opening tag or be escaped
					sbInput.Remove(beginBracket, 1);
					pos = beginBracket;
					continue;
				}



				// Curly bracket?
				bool bCurly = sbInput[beginBracket] == '{' && beginBracket + 1 < sbInput.Length;

				int endBracket = FindNextInScope(sbInput, bCurly ? '}' : ']', beginBracket + 1);
				if (endBracket == -1)
				{
					// Must have closing tag or be escaped
					sbInput.Remove(beginBracket, 1);
					pos = beginBracket;
					continue;
				}

				if (loop_count > 500)
					return StackOverflowError(sbInput, beginBracket, endBracket);

				pos = endBracket + 1;

				// Full stop [.]
				if (endBracket - beginBracket == 2 && sbInput[beginBracket + 1] == '.')
				{
					sbInput.ReplaceFromTo(beginBracket, endBracket, "[erase][fs]");
					pos = beginBracket;
					continue;
				}
				// Comma [,]
				if (endBracket - beginBracket == 2 && sbInput[beginBracket + 1] == ',')
				{
					sbInput.ReplaceFromTo(beginBracket, endBracket, "[erase][comma]");
					pos = beginBracket;
					continue;
				}

				var operators = FindNextInScopeOf(sbInput, ":?/|!.@", beginBracket + 1, endBracket - 1);
				int colon		= operators[0];	// ':'
				int question	= operators[1];	// '?'
				int slash		= operators[2];	// '/'
				int pipe		= operators[3];	// '|'
				int excl		= operators[4];	// '!'
				int period		= operators[5];	// '.'
				int at			= operators[6];	// '@'

				// Ignore function marker (.) before selector (:)
				if (colon != -1 && period < colon)
					period = FindNextInScope(sbInput, '.', colon + 1, endBracket - 1);

				int beginOfWord = beginBracket + 1;
				int endOfWord = period != -1 ? period : endBracket;
				int firstChar = beginBracket + 1;
				if (beginOfWord > endOfWord)
					endOfWord = beginOfWord;

				bool isCondition = false;
				string word = null;

				// Disallow function marker (.) after conditional marker (?)
				if (question != -1 && period > question)
					period = -1;
				
				// Is parsing?
				if (internalContext.haltParsing)
				{
					// Resume parsing
					if (beginOfWord < endOfWord && string.Compare(sbInput.ToString(beginOfWord, endOfWord - beginOfWord), "parse", true) == 0)
					{
						sbInput.ReplaceFromTo(beginBracket, endBracket, "<!np->");
						pos = beginBracket + 6;
						internalContext.haltParsing = false;
						continue;
					}

					pos = endBracket + 1;
					continue;
				}

				bool hasFunction = false;
				string function = null;

				// Function [!a:...]
				if (!hasFunction && (excl == firstChar) && (colon != -1) && (slash == -1 || excl < slash))
				{
					function = sbInput.ToString(excl + 1, colon - excl - 1).Trim();
					string input = sbInput.ToString(colon + 1, endBracket - colon - 1).Trim();

					if (internalContext.PushStack(() =>
					{
						input = Evaluate(input, internalContext);
					}))
					{ 
						string replacement = string.Format("<%%{0}|{1}%%>", function, input); // Mark late function <%%lower|...%%>
						sbInput.ReplaceFromTo(beginBracket, endBracket, replacement);
						pos = beginBracket + replacement.Length;
						continue;
					}
					else // Break cycle
						return StackOverflowError(sbInput, beginBracket, endBracket);
				}
				else
				{
					colon = -1;
				}

				// Random word [../..]
				if (slash != -1 && (at == -1 || at > slash) && (question == -1 || question > slash))
				{
					string options = sbInput.ToString(beginBracket + 1, endBracket - (beginBracket + 1));
					sbInput.ReplaceFromTo(beginBracket, endBracket, RandomWord(options, internalContext.randomizer));
					pos = beginBracket;
					continue;
				}

				// First of [..|..]
				if (question == -1 && pipe != -1 && (at == -1 || at > pipe))
				{
					string options = sbInput.ToString(beginBracket + 1, endBracket - (beginBracket + 1));
					sbInput.ReplaceFromTo(beginBracket, endBracket, ChooseFirstOf(options, internalContext));
					pos = beginBracket;
					continue;
				}

				// Function name [name.lower]
				if (period != -1)
				{ 
					function = sbInput.ToString(period + 1, endBracket - period - 1).Trim().ToLowerInvariant();
					hasFunction = period != -1 && function.Length > 0;
				}

				// Function on plain text [[...].lower]
				if (hasFunction)
				{
					string argument = sbInput.ToString(beginOfWord, endOfWord - beginOfWord);

					if (argument.Length > 2
						&& ((argument[0] == '[' && argument[argument.Length - 1] == ']')
						|| (argument[0] == '{' && argument[argument.Length - 1] == '}')))
					{
						string replacement = string.Format("<%%{0}|{1}%%>", function, argument.Substring(1, argument.Length - 2)); // Mark late function <%%lower|...%%>
						sbInput.ReplaceFromTo(beginBracket, endBracket, replacement);
						pos = beginBracket;
						continue;
					}
				}

				// Condition [...?a] or [...?a|b]
				if (question != -1 && (pipe == -1 || pipe > question))
				{
					word = sbInput.ToString(beginBracket + 1, question - beginBracket - 1);
					isCondition = true;
				}
				
				// Default
				if (!isCondition)
					word = sbInput.ToString(beginBracket + 1, endOfWord - beginBracket - 1);

				word = word.Trim();

				if (string.IsNullOrEmpty(word)) // [] ?
				{
					sbInput.ReplaceFromTo(beginBracket, endBracket, "");
					pos = beginBracket;
					continue;
				}


				// String references
				if (at == beginBracket + 1 && endOfWord - beginOfWord > 1)
				{
					string reference = sbInput.ToString(beginOfWord + 1, endOfWord - beginOfWord - 1);

					// Is reference?
					string sReferencedString;
					if (GetReferencedString(reference, internalContext, out sReferencedString))
					{
						if (internalContext.PushStack( () =>
						{
							var refContext = internalContext;
							string refString = Evaluate(new StringBuilder(sReferencedString), refContext).ToString().Trim();

							if (hasFunction)
								ApplyFunction(ref refString, function, internalContext);
							sbInput.ReplaceFromTo(beginBracket, endBracket, refString);
							pos = beginBracket + refString.Length;
						}))
						{ 
							++loop_count;
							continue;
						}
						else // Break cycle
							return StackOverflowError(sbInput, beginBracket, endBracket);
					}
					else
					{
						sbInput.ReplaceFromTo(beginBracket, endBracket, "");
						++loop_count;
						pos = beginBracket;
						continue;
					}
				}

				// [#if ...] ?
				if (word.Length >= 3 && word.BeginsWith("if", true))
				{
					// Find else/elif/endif
					var ifScope = FindIfScope(sbInput, beginBracket);

					if (ifScope.pos_endif != -1)
					{
						endBracket = ifScope.pos_endif + 6; // [endif]
															//       ^
						bool bSuccess = false;
						if (ifScope.error == false)
						{
							// Evaluate if condition
							for (int i = 0; i < ifScope.locations.Count; ++i)
							{
								bool result = true;
								if (ifScope.locations[i].condition != null)
									result = EvaluateCondition(ifScope.locations[i].condition, internalContext);

								int block_from;
								int block_to;
								if (result && ifScope.GetInnerBlock(i, out block_from, out block_to))
								{
									StringBuilder sbInner = new StringBuilder(block_to - block_from);
									sbInner.Append(sbInput.SubstringToFrom(block_from, block_to - 1));

									sbInner.TrimLinebreaks();

									// Replace with inner block
									sbInput.ReplaceFromTo(beginBracket, endBracket, sbInner.ToString());

									bSuccess = true;
									break;
								}
							}
						}

						if (!bSuccess)
						{
							// Clear entire block
							sbInput.ReplaceFromTo(beginBracket, endBracket, "");
						}

						++loop_count;
						pos = beginBracket;
						continue;
					}
				}

				// Evaluate condition
				if (isCondition)
				{
					string condition = sbInput.ToString(beginBracket + 1, question - beginBracket - 1).Trim();
					string[] options = SplitOptions(sbInput.ToString(question + 1, endBracket - (question + 1)));

					// Preceding word condition? ?..
					if (condition.EndsWith(".."))
					{
						condition = condition.Substring(0, condition.Length - 2);
						options = options
							.Select(s => Evaluate(new StringBuilder(s), internalContext).ToString())
							.ToArray();

						string replacement = string.Format("<%%?..{0}|{1}%%>", condition, string.Join("|", options)); // Late evaluation
						sbInput.ReplaceFromTo(beginBracket, endBracket, replacement);
						pos = beginBracket + replacement.Length;
						continue;
					}
					// Subsequent word condition? ..?
					if (condition.BeginsWith(".."))
					{
						condition = condition.Substring(2);
						options = options
							.Select(s => Evaluate(new StringBuilder(s), internalContext).ToString())
							.ToArray();

						string replacement = string.Format("<%%..?{0}|{1}%%>", condition, string.Join("|", options)); // Late evaluation
						sbInput.ReplaceFromTo(beginBracket, endBracket, replacement);
						pos = beginBracket + replacement.Length;
						continue;
					}

					// Expression
					Context ruleContext = Context.Copy(internalContext.context, true);
					
					ICondition rule = Rule.Parse(condition);
					bool bResult = rule != null && rule.Evaluate(ruleContext, new EvaluationCookie() { 
						ruleSuppliers = internalContext.ruleSuppliers,
						randomizer = internalContext.randomizer 
						});
					string resultWord = ConditionalWord(options, bResult);
					sbInput.ReplaceFromTo(beginBracket, endBracket, resultWord);
					pos = beginBracket;
					continue;
				}
			
				// Perform word replacement
				bool isMacro;
				string substitution;
				if (ContextStringSubstitutor.Resolve(word, internalContext, out substitution, out isMacro))
				{
					if (hasFunction)
					{
						if (isMacro) // Functions on macros need to be resolved after the macro has been resolved.							
							DeferFunction(ref substitution, function);
						else
							ApplyFunction(ref substitution, function, internalContext);
					}

					sbInput.ReplaceFromTo(beginBracket, endBracket, substitution);

					if (string.Compare(substitution, "<!np+>") == 0)
						internalContext.haltParsing = true;

					++loop_count;
					if (isMacro)
						pos = beginBracket;
					else
						pos = beginBracket + substitution.Length;
					continue;
				}

				// Unicode character [#u2014]
				if (word.Length > 1 && word[0] == '#' && (word[1] == 'u' || word[1] == 'U'))
				{
					string value = word.Substring(2);
					int iValue;
					if (Int32.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out iValue))
					{
						string replacement = string.Format("<!u{0}>", value);
						sbInput.ReplaceFromTo(beginBracket, endBracket, replacement);
						pos = beginBracket;
						continue;
					}
				}

				// Unable to resolve
				sbInput.RemoveFromTo(beginBracket, endBracket);
				pos = beginBracket;
				++loop_count;

				// Skip next
				continue;
			}

			return sbInput;
		}

		private void __PostProcess(StringBuilder sb, Internal_Context internalContext)
		{
			// Resolve latent functions <%%lower|...%%>
			ResolvePostFunctions(sb, internalContext);

			// Articles post function
			ResolvePostProcedures(sb);
		}

		private static StringBuilder StackOverflowError(StringBuilder sbInput, int beginBracket, int endBracket)
		{
			// There was a log here
			sbInput.ReplaceFromTo(beginBracket, endBracket, "");
			return sbInput;
		}
		
		public static bool EvaluateCondition(ICondition condition, Internal_Context internalContext)
		{
			if (condition == null)
				return false;

			Context ruleContext = Context.Copy(internalContext.context, true);
			return condition.Evaluate(ruleContext, new EvaluationCookie() {
				randomizer = internalContext.randomizer,
				ruleSuppliers = internalContext.ruleSuppliers,
			});
		}

		public static string EvaluateString(string text, Context context, IRandom randomizer = null)
		{
			return new ContextString(text).Evaluate(context, randomizer);
		}

		private static string RandomWord(string words, IRandom randomizer)
		{
			List<string> lsWords = ScopedSplit(words, '/');
			if (lsWords.Count == 0)
				return string.Empty;
			if (lsWords.Count == 1)
				return lsWords[0];

			return lsWords[randomizer.Int(0, lsWords.Count, RandomOption.Exclusive)];
		}

		private string ChooseFirstOf(string words, Internal_Context internalContext)
		{
			List<string> lsWords = ScopedSplit(words, '|');
			if (lsWords.Count == 0)
				return string.Empty;
			if (lsWords.Count == 1)
				return lsWords[0];

			for (int i = 0; i < lsWords.Count; ++i)
			{
				var refContext = internalContext.Clone();
				string value = Evaluate(lsWords[i].Trim(), refContext);
				if (value.IsNullOrWhiteSpace() == false)
					return value;
			}
			return string.Empty;
		}

		private static List<string> ScopedSplit(string s, char delimiter)
		{
			List<string> words = new List<string>();

			var sb = new StringBuilder(s);

			int pos = 0;
			int pipe = FindNextInScope(sb, delimiter, pos);
			while (pipe != -1)
			{
				words.Add(s.Substring(pos, pipe - pos));
				pos = pipe + 1;
				pipe = FindNextInScope(sb, delimiter, pos);
			}

			if (pos <= s.Length)
				words.Add(s.Substring(pos, s.Length - pos));
			return words;
		}

		private static string[] SplitOptions(string s)
		{
			List<string> words = ScopedSplit(s, '|');
			if (words.Count >= 2)
				return new string[] { words[0], words[1] };
			if (words.Count == 1)
				return new string[] { words[0], "" };
			return new string[] { "", "" };
		}

		private static string ConditionalWord(string[] options, bool condition)
		{
			if (options == null || options.Length == 0)
				return string.Empty;
			if (condition == true)
				return options[0];
			else if (options.Length > 1)
				return options[1];
			return string.Empty;
		}

		public static int FindFirst(string s, char ch, int from = 0, int to = -1)
		{
			if (to < from)
			{
				for (int i = from; i < s.Length; ++i)
					if (s[i] == ch)
						return i;
			}
			else
			{
				for (int i = from; i <= to && i < s.Length; ++i)
					if (s[i] == ch)
						return i;
			}
			return -1;
		}

		private static int FindNextInScope(StringBuilder sb, char ch, int from, int to = -1)
		{
			if (sb == null || sb.Length == 0)
				return -1;

			Stack<char> stack = new Stack<char>();
			
			if (to < 0)
				to = sb.Length - 1;

			for (int i = from; i <= to && i < sb.Length;)
			{
				char c = sb[i];

				// Escaped?
				/*if ((c == '[' || c == ']' || c == '{' || c == '}') 
					&& i < sb.Length - 1 && sb[i + 1] == c)
				{
					i += 2;
					continue;
				}*/

				if (c == ch && stack.Count == 0)
					return i;

				if (c == '[')
					stack.Push(']');
				else if (c == '{')
					stack.Push('}');
				else if (stack.Count > 0 && c == stack.Peek())
					stack.Pop();
				++i;
			}
			return -1;
		}

		// Returns an array of (all) the earliest instances of characters in a set
		private static int[] FindNextInScopeOf(StringBuilder sb, string chars, int from, int to = -1)
		{
			if (string.IsNullOrEmpty(chars))
				return new int[0];

			int[] result = new int[chars.Length];
			for (int i = 0; i < chars.Length; ++i)
				result[i] = -1;

			if (sb == null || sb.Length == 0 || string.IsNullOrEmpty(chars))
				return result;

			Stack<char> stack = new Stack<char>();
			int found = 0;
			
			if (to < 0)
				to = sb.Length - 1;

			for (int i = from; i <= to && i < sb.Length && found < chars.Length;)
			{
				char c = sb[i];

				// Escaped?
				/*if ((c == '[' || c == ']' || c == '{' || c == '}')
					&& i < sb.Length - 1 && sb[i + 1] == c)
				{
					i += 2;
					continue;
				}*/

				if (c == '[')
				{
					stack.Push(']');
				}
				else if (c == '{')
				{
					stack.Push('}');
				}
				else if (stack.Count > 0 && c == stack.Peek())
				{
					stack.Pop();
				}
				else if (stack.Count == 0)
				{
					int idx = chars.IndexOf(c);
					if (idx != -1 && result[idx] == -1)
					{
						result[idx] = i;
						found++;
					}
				}
				++i;
			}
			return result;
		}

		private struct IfScope
		{
			public struct Location
			{
				public int position;
				public int innerPosition;
				public ICondition condition;
			}

			public List<Location> locations;
			public int pos_endif;
			public bool error;

			public void Append(int position, int innerPosition, string condition = null)
			{
				if (locations == null)
					locations = new List<Location>(1);

				locations.Add(new Location()
				{
					position = position,
					innerPosition = innerPosition,
					condition = condition != null ? Rule.Parse(condition) : Condition.Always(),
				});
			}

			public bool GetInnerBlock(int index, out int begin, out int end)
			{				
				if (index >= 0 && index < locations.Count)
				{
					begin = locations[index].innerPosition;

					if (index < locations.Count - 1)
						end = locations[index + 1].position;
					else
						end = pos_endif;
					return true;
				}

				begin = -1;
				end = -1;
				return false;
			}
		}

		private static bool CompareChars(StringBuilder sb, string value, int position)
		{	
			for (int i = 0; i < value.Length; ++i)
			{
				if (position + i >= sb.Length)
					return false;

				char cA = sb[position + i];
				char cB = value[i];
				if (char.ToUpperInvariant(cA) != char.ToUpperInvariant(cB))
					return false;
			}
			return true;
		}

		private static IfScope FindIfScope(StringBuilder sb, int from)
		{
			if (sb == null || sb.Length == 0)
			{
				return new IfScope() {
					pos_endif = -1,
					error = true,
				};
			}

			IfScope result = new IfScope()
			{
				locations = null,
				pos_endif = -1
			};

			int currScope = 0;
			bool bElseFound = false;

			int i = from;
			while (i < sb.Length - 1)
			{
				if (sb[i] != '[')
				{
					++i;
					continue;
				}
				
				// [if ...]
				if (CompareChars(sb, "[if", i))
				{
					// Expect [if.. bracket to end with ']'.
					if (i + 3 < sb.Length && char.IsWhiteSpace(sb[i + 3]))
					{
						int pos_bracket = sb.IndexOfAny(new char[] { '[', ']', '{', '}' }, i + 4);
						if (pos_bracket != -1 && sb[pos_bracket] == ']')
						{
							if (currScope == 0)
							{
								var condition = sb.ToString().Substring(i + 4, pos_bracket - i - 4).Trim();
								if (condition.IsNullOrWhiteSpace()) // No condition is an error
									result.error = true;
								result.Append(i, pos_bracket + 1, condition);
							}

							currScope++;
							i = pos_bracket + 1;
							continue;
						}
					}
					else // No condition
					{
						int pos_bracket = sb.IndexOfAny(new char[] { '[', ']', '{', '}' }, i + 3);
						if (pos_bracket != -1)
						{
							result.error = true;
							currScope++;
							i = pos_bracket + 1;
							continue;
						}
						else // Error
						{
							result.pos_endif = -1;
							return result;
						}
					}
				}

				// [elif ...]
				if (CompareChars(sb, "[elif", i))
				{
					// Expect [elif.. to end with ']'.
					if (i + 5 < sb.Length && char.IsWhiteSpace(sb[i + 5]))
					{
						int pos_bracket = sb.IndexOfAny(new char[] { '[', ']', '{', '}' }, i + 5);
						if (pos_bracket != -1 && sb[pos_bracket] == ']')
						{
							if (currScope == 1)
							{
								if (bElseFound) // [elif] after [else] is an error
									result.error = true;

								var condition = sb.ToString().Substring(i + 6, pos_bracket - i - 6).Trim();
								if (condition.IsNullOrWhiteSpace()) // No condition is an error
									result.error = true;

								result.Append(i, pos_bracket + 1, condition);
							}

							i = pos_bracket + 1;
							continue;
						}
					}
					else // No condition
					{
						int pos_bracket = sb.IndexOfAny(new char[] { '[', ']', '{', '}' }, i + 5);
						if (pos_bracket != -1 && sb[pos_bracket] == ']')
						{
							if (bElseFound == false)  // [elif] -> [else]
							{
								bElseFound = true;
								result.Append(i, pos_bracket + 1);
								i = pos_bracket + 1;
								continue;
							}
							else
							{
								result.error = true;
								i = pos_bracket + 1;
								continue;
							}
						}
						else // Error
						{
							result.pos_endif = -1;
							return result;
						}
					}
				}
				// [else]
				else if (CompareChars(sb, "[else]", i))
				{
					if (currScope == 1)
					{
						if (bElseFound) // [else] after [else] is an error
							result.error = true;

						bElseFound = true;
						result.Append(i, i + 6);
					}

					i = i + 6;
					continue;
				}

				// [endif]
				else if (CompareChars(sb, "[endif]", i))
				{
					if (currScope == 1)
					{
						result.pos_endif = i;
						break;
					}

					currScope--;
					i = i + 7;
					continue;
				}

				++i;
			}
			return result;
		}
		
		private void DeferFunction(ref string text, string functionName)
		{
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(functionName))
				return;

			text = string.Format("<%%{0}|{1}%%>", functionName, text);
		}

		private void ApplyFunction(ref string text, string functionName, Internal_Context internalContext)
		{
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(functionName))
				return;

			string[] functions;

			if (functionName.Contains('.'))
				functions = functionName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			else
				functions = new string[] { functionName };

			Func<string, string> fnFix = (string s) => {
				s = Utility.RemoveBrackets(s, "<", ">");
				s = Utility.RemoveBrackets(s, "[", "]");
				return s;
			};

			var evalConfig = internalContext.evalConfig;

			for (int i = 0; i < functions.Length; ++i)
			{
				var fn = functions[i];
				switch (fn)
				{
				case "lower":
					text = Text.Lower(text);
					break;
				case "upper":
					text = Text.Upper(text);
					break;
				case "cap":
				case "capital":
					if (text.Length >= 1)
						text = string.Concat(text.Substring(0, 1).ToUpper(), text.Substring(1).ToLower());
					break;
				case "article":
					text = Text.AOrAn(fnFix(text));
					break;
				case "possessive":
					text = fnFix(text).EndsWith("s", false) ? string.Concat(text, "'") : string.Concat(text, "<!nocap>", "'s");
					break;
				case "quote":
					text = string.Concat("\"", text, "\"");
					break;
				case "singlequote":
					text = string.Concat("'", text, "'");
					break;
				case "collapse":
					text = text.Replace('\n', ' ');
					break;
				case "list":
				case "andlist":
				case "and-list":
				{
					var words = ListFromDelimitedString(text);
					text = Text.Eval(Utility.CommaSeparatedList(words, Text.Eval("[{@conjugates/and}|and]", internalContext.context, evalConfig, Text.EvalOption.None)), null, Text.EvalOption.None);
					break;
				}
				case "orlist":
				case "or-list":
				{
					var words = ListFromDelimitedString(text);
					text = Text.Eval(Utility.CommaSeparatedList(words, Text.Eval("[{@conjugates/or}|or]", internalContext.context, evalConfig, Text.EvalOption.None)), null, Text.EvalOption.None);
					break;
				}
				case "norlist":
				case "nor-list":
				{
					var words = ListFromDelimitedString(text);
					text = Text.Eval(Utility.CommaSeparatedList(words, Text.Eval("[{@conjugates/nor}|nor]", internalContext.context, evalConfig, Text.EvalOption.None)), null, Text.EvalOption.None);
					break;
				}
				case "pluslist":
				case "plus-list":
				{
					var words = ListFromDelimitedString(text);
					text = Text.Eval(Utility.ListToDelimitedString(words, " + "), null, Text.EvalOption.None);
					break;
				}
				case "commalist":
				case "comma-list":
				{
					var words = ListFromDelimitedString(text);
					text = Text.Eval(Utility.ListToDelimitedString(words, ", "), null, Text.EvalOption.None);
					break;
				}
				case "semilist":
				case "semi-list":
				case "semicolonlist":
				case "semicolon-list":
				{
					var words = ListFromDelimitedString(text);
					text = Text.Eval(Utility.ListToDelimitedString(words, "; "), null, Text.EvalOption.None);
					break;
				}
				case "quotelist":
				case "quote-list":
				{
					var words = ListFromDelimitedString(text);
					StringBuilder sb = new StringBuilder();
					for (int n = 0; n < words.Count; ++n)
					{
						if (n > 0)
							sb.Append(", ");
						sb.Append("\"");
						sb.Append(Text.Eval(words[n], null, Text.EvalOption.Default));
						sb.Append("\"");
					}
					text = sb.ToString();
					break;
				}	
				case "numlist":
				case "numberlist":
				case "num-list":
				case "number-list":
				{
					var words = ListFromDelimitedString(text);
					StringBuilder sb = new StringBuilder();
					for (int n = 0; n < words.Count; ++n)
					{
						sb.Append(Text.SoftBreak);
						sb.AppendFormat("{0}. ", n + 1);
						sb.Append(Text.Eval(words[n], null, Text.EvalOption.None));
					}
					text = sb.ToString();
					break;
				}	
				case "bulletlist":
				case "bullet-list":
				{
					var words = ListFromDelimitedString(text);
					StringBuilder sb = new StringBuilder();
					for (int n = 0; n < words.Count; ++n)
					{
						sb.Append(Text.SoftBreak);
						sb.AppendFormat("- ", n + 1);
						//sb.AppendFormat("\u2022 ", n + 1);
						sb.Append(Text.Eval(words[n], null, Text.EvalOption.None));
					}
					text = sb.ToString();
					break;
				}
				case "count":
				{
					var words = ListFromDelimitedString(text);
					text = words.Count.ToString();
					break;
				}
				case "numeral":
				{
					int iValue;
					if (Utility.StringToInt(text, out iValue))
						text = Text.NumeralFromInt(iValue);
					else
						text = "";
					break;
				}
				case "percent":
				{
					double value;
					if (Utility.StringToDouble(text, out value))
						text = string.Format("{0}%", (int)(value * 100.0), CultureInfo.InvariantCulture);
					else
						text = "";
					break;
				}
				case "floor":
				{
					float fValue;
					if (Utility.StringToFloat(text, out fValue))
						text = Math.Floor(fValue).ToString("0", CultureInfo.InvariantCulture);
					else
						text = "";
					break;
				}
				case "ceil":
				case "ceiling":
				{
					float fValue;
					if (Utility.StringToFloat(text, out fValue))
						text = Math.Ceiling(fValue).ToString("0", CultureInfo.InvariantCulture);
					else
						text = "";
					break;
				}
				case "round":
				{
					float fValue;
					if (Utility.StringToFloat(text, out fValue))
						text = Math.Round(fValue).ToString("0", CultureInfo.InvariantCulture);
					else
						text = "";
					break;
				}

				case "imperial":
				{
					text = Measurement.ConvertToImperial(text);
					break;
				}
				case "metric":
				{
					text = Measurement.ConvertToImperial(text);
					break;
				}
				case "value":
				{
					decimal value;
					if (Utility.StringToDecimal(text, out value))
						text = value.ToString("g", CultureInfo.InvariantCulture);
					else
						text = "";
					break;
				}
				case "m":
				case "dm":
				case "cm":
				case "mm":
				case "g":
				case "hg":
				case "kg":
				case "yd":
				case "ton":
				case "oz":
				case "lb":
				case "l":
				case "dl":
				case "cl":
				case "cc":
				case "ml":
				case "floz":
				case "gal":
					ConvertMeasurement(ref text, fn);
					break;
				case "in":
				case "inch":
					ConvertMeasurement(ref text, "in");
					break;
				case "ft":
					ConvertMeasurement(ref text, "ft");
					break;
				case "ftin":
					ConvertMeasurement(ref text, "ftin");
					break;

				case "plural":
				{
					if (text.EndsWith("y", true))
						text = Text.Eval(string.Concat("[use-plural?", text.Substring(0, text.Length - 1), "ies|", text, "]"), internalContext.context, Text.EvalOption.None);
					else
						text = Text.Eval(string.Concat("[use-plural?", text.Substring(0, text.Length - 1), "s|", text, "]"), internalContext.context, Text.EvalOption.None);
					break;
				}
				default:
					{ 
						// Wrapper?
						string wrapper;
						if (ContextStringSubstitutor.macros.TryGetWrapper(fn, out wrapper))
						{
							string value = wrapper.Replace("_", text ?? "");// string.Concat(prefix, text, suffix);
							if (internalContext.PushStack(() => { value = Evaluate(value, internalContext); }) == false)
								value = "";
							text = value;
						}
						else if (internalContext.macroSuppliers != null)
						{
							foreach (var macroSupplier in internalContext.macroSuppliers)
							{
								if (macroSupplier.GetMacroBank().TryGetWrapper(fn, out wrapper))
								{
									string value = wrapper.Replace("_", text ?? "");// string.Concat(prefix, text, suffix);
									if (internalContext.PushStack(() => { value = Evaluate(value, internalContext); }) == false)
										value = "";
									text = value;
								}
							}
						}
						break;
					}
				} // switch
			}

		}

		private static string ConvertMeasurement(ref string text, string unit)
		{
			decimal magnitude;
			if (Measurement.Parse(text, NumberParameter.Mode.Unknown, out magnitude, out var _, out var unitSystem))
				text = Measurement.ToString(magnitude, unit);
			else
				text = "";
			return text;
		}

		private void ResolvePostFunctions(StringBuilder sb, Internal_Context internalContext)
		{
			const string Open = "<%%";
			const string Close = "%%>";

			int pos = sb.IndexOf(Open, 0);
			while (pos != -1)
			{
				int beginScope = pos;

				int endScope = Utility.ScopedIndexOf(sb, Close, beginScope, Open, Close);
				if (endScope == -1)
				{
					pos = sb.IndexOf(Open, pos + 1);
					continue;
				}

				int pipe =  Utility.ScopedIndexOf(sb, "|", beginScope, Open, Close, endScope);
				if (pipe == -1)
				{
					pos = sb.IndexOf(Open, pos + 1);
					continue;
				}

				string function = sb.ToString(beginScope + 3, pipe - beginScope - 3).Trim();
				var sbValue = new StringBuilder(sb.ToString(pipe + 1, endScope - pipe - 1));
				ResolvePostFunctions(sbValue, internalContext);
				string value = sbValue.ToString();

				if (function.Length > 3 && function.BeginsWith("?.."))
					IfPrecedingWord(sb, beginScope, function.Substring(3), value, ref value);
				else if (function.Length > 3 && function.BeginsWith("..?"))
					IfSubsequentWord(sb, endScope + 3, function.Substring(3), value, ref value);
				else
					ApplyFunction(ref value, function, internalContext);
				sb.ReplaceFromTo(beginScope, endScope + 2, value);

				pos = sb.IndexOf(Open, pos + 1);
			}
		}

		private static void ResolvePostProcedures(StringBuilder sb)
		{
			const string Open = "<%%";
			const string Close = "%%>";

			int pos = sb.IndexOf(Open, 0);
			while (pos != -1)
			{
				int beginScope = pos;

				int endScope = Utility.ScopedIndexOf(sb, Close, beginScope, Open, Close);
				if (endScope == -1)
				{
					pos = sb.IndexOf(Open, pos + 1);
					continue;
				}

				string function = sb.ToString(beginScope + 3, endScope - beginScope - 3).Trim();

				switch (function)
				{
				case "a_or_an":
					AOrAn(sb, beginScope);
					break;
				case "unp":
					Unparagraph(sb, beginScope, "\n");
					pos = sb.IndexOf(Open, 0);
					continue;
				case "clr":
					Unparagraph(sb, beginScope, "");
					pos = sb.IndexOf(Open, 0);
					continue;
				case "the":
					ConditionalThe(sb, beginScope);
					continue;
				}

				pos = sb.IndexOf(Open, pos);
			}

		}

		private static void AOrAn(StringBuilder sb, int beginScope)
		{
			int len = ("<%%a_or_an%%>").Length;
			int idxArticle = beginScope;

			string subsequentWord = sb.ToString()
				.Substring(idxArticle + len)
				.TrimStart();

			if (string.IsNullOrEmpty(subsequentWord) == false)
				sb.ReplaceFromTo(idxArticle, idxArticle + len - 1, Text.AOrAn(subsequentWord));
			else
				sb.ReplaceFromTo(idxArticle, idxArticle + len - 1, "");
		}


		private static void ConditionalThe(StringBuilder sb, int pos)
		{
			int len = ("<%%the%%>").Length;
			int idxThe = pos;

			string subsequentWord = sb.ToString()
				.Substring(idxThe + len)
				.TrimStart()
				.ToLowerInvariant();

			if (string.IsNullOrEmpty(subsequentWord) == false && subsequentWord.Length >= 3)
			{
				if (subsequentWord.BeginsWith("the") && subsequentWord.Length > 3 && char.IsWhiteSpace(subsequentWord[3]))
					sb.ReplaceFromTo(idxThe, idxThe + len - 1, "");
				else
					sb.ReplaceFromTo(idxThe, idxThe + len - 1, "the");
			}
			else
				sb.ReplaceFromTo(idxThe, idxThe + len - 1, "the");
		}

		private static void IfPrecedingWord(StringBuilder sb, int posBegin, string word, string options, ref string value)
		{
			string prevWord = sb.ToString()
				.Substring(0, posBegin)
				.TrimEnd()
				.ToLowerInvariant();

			string[] arrOptions = SplitOptions(options);

			if (prevWord.Length == 0)
			{
				value = ConditionalWord(arrOptions, false);
				return;
			}

			word = word.Trim().ToLowerInvariant();
			if (prevWord.EndsWith(word) == false)
			{
				value = ConditionalWord(arrOptions, false);
				return;
			}
			if (prevWord.Length > word.Length) // Word must end
			{
				char ch = prevWord[prevWord.Length - (word.Length + 1)];
				value = ConditionalWord(arrOptions, char.IsWhiteSpace(ch) || char.IsPunctuation(ch));
				return;
			}
			value = ConditionalWord(arrOptions, true);
		}

		private static void IfSubsequentWord(StringBuilder sb, int posEnd, string word, string options, ref string value)
		{
			string nextWord = sb.ToString()
				.Substring(posEnd)
				.TrimStart()
				.ToLowerInvariant();

			string[] arrOptions = SplitOptions(options);

			if (nextWord.Length == 0)
			{
				value = ConditionalWord(arrOptions, false);
				return;
			}

			word = word.Trim().ToLowerInvariant();
			if (nextWord.BeginsWith(word) == false)
			{
				value = ConditionalWord(arrOptions, false);
					return;
				}
			if (nextWord.Length > word.Length) // Word must end
				{
				char ch = nextWord[word.Length];
				value = ConditionalWord(arrOptions, char.IsWhiteSpace(ch) || char.IsPunctuation(ch));
						return;
					}
			value = ConditionalWord(arrOptions, true);
		}

		private static void Unparagraph(StringBuilder sb, int pos, string replace = " ")
		{
			int len = ("<%%xxx%%>").Length;
			int from = pos;
			int to = pos + len - 1;
			while (from > 0 && char.IsWhiteSpace(sb[from - 1]))
				from--;
			while (to < sb.Length - 1 && char.IsWhiteSpace(sb[to + 1]))
				to++;
			sb.ReplaceFromTo(from, to, replace);
		}

		private static bool GetReferencedString(string referenceID, Internal_Context internalContext, out string sResult)
		{
			if (string.IsNullOrEmpty(referenceID))
			{
				sResult = null;
				return false;
			}

			StringID[] stringIDs;
			if (referenceID.IndexOf('|') != -1) // [@ref1 | ref2 | ref3]
			{
				stringIDs = referenceID.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(s => StringID.Make(s))
					.ToArray();
			}
			else
			{
				stringIDs = new StringID[] { StringID.Make(referenceID) };
			}

			List<StringBank.Entry> foundStrings = new List<StringBank.Entry>();

			var refContext = Context.Copy(internalContext.context, true);

			bool bFound = false;

			if (internalContext.referenceStringBanks != null)
			{
				foreach (var referenceStringBank in internalContext.referenceStringBanks)
				{
					for (int i = 0; i < stringIDs.Length; ++i)
					{
						if (referenceStringBank != null)
						{
							var search = referenceStringBank.FindStrings(stringIDs[i], refContext, internalContext.ruleSuppliers, false);
							foundStrings.AddRange(search);
							bFound |= search.Length > 0;
						}
					}
				}
			}

			if (bFound)
			{
				if (foundStrings.IsEmpty() == false)
					foundStrings = StringBank.PrioritizeEntries(foundStrings).ToList();

				if (foundStrings.Count > 0)
					sResult = internalContext.randomizer.Item(foundStrings).value;
				else // Reference exists but is ineligible
					sResult = "";
				return true;
			}

			// Invalid reference
			sResult = null;
			return false;
		}

		public static List<string> ListFromDelimitedString(string source)
		{
			if (string.IsNullOrEmpty(source))
				return new List<string>(0);

			return source.Split(new string[] { Text.Delimiter, ";", "\r\n", "\n" }, StringSplitOptions.None)
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.ToList();
		}

	} // class
	
} // namespace
