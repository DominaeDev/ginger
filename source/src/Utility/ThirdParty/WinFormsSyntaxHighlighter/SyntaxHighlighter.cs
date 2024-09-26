/*
 *  WinFormsSyntaxHighlighter
 * 
 * Copyright (C) 2014 sinairv
 * https://github.com/sinairv/WinFormsSyntaxHighlighter/
 * 
 * License: MIT
 * 
 * Modified by DominaeDev in 2024, adding unicode support, line spacing, overlapping styles, spell checking.
 */

using Ginger;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WinFormsSyntaxHighlighter
{
	public class SyntaxHighlighter
	{
		/// <summary>
		/// Reference to the RichTextBox instance, for which 
		/// the syntax highlighting is going to occur.
		/// </summary>
		private readonly RichTextBoxEx _richTextBox;

		private int _fontSizeFactor;
		private string _fontName;
		private static readonly string _emojiFontName = "Segoe UI Emoji";
		private static readonly string _japaneseFontName = "Meiryo UI";
		private static readonly string _generalPunctuationFontName = "Lucida Sans Unicode";
		private static readonly string _monoFontName = "Consolas";
		private int _lineSpacingTwips = 0;

		public float LineHeight
		{
			get { return _lineHeight; }
			set 
			{ 
				_lineHeight = value;
				_lineSpacingTwips = Convert.ToInt32(Math.Max(_lineHeight * 240, 0));
			}
		}
		private float _lineHeight = 1.0f;

		public System.Drawing.Font Font
		{ 
			set
			{
				_fontSizeFactor = Convert.ToInt32(value.Size * 2);
				_fontName = value.Name;
			}
		}

		/// <summary>
		/// Determines whether the program is busy creating rtf for the previous
		/// modification of the text-box. It is necessary to avoid blinks when the 
		/// user is typing fast.
		/// </summary>
		private bool _isDuringHighlight;
		public bool isHighlighting { get { return _isDuringHighlight; } }

		private List<StyleGroupPair> _styleGroupPairs;
		private readonly List<PatternStyleMap> _patternStyles = new List<PatternStyleMap>();

		private static class CharacterSet
		{
			public const int Default = 0;
			public const int Emoji = 1;
			public const int Japanese = 2;
			public const int GeneralPunctuation = 2;
		}

		public static readonly char ErrorStart = '\uFFF0';
		public static readonly char ErrorEnd = '\uFFF1';

		private TextSpans _textSpans; // Spell checking

		public SyntaxHighlighter(RichTextBoxEx richTextBox)
		{
			if (richTextBox == null)
				throw new ArgumentNullException("richTextBox");

			_richTextBox = richTextBox;

			_fontSizeFactor = Convert.ToInt32(_richTextBox.Font.Size * 2);
			_fontName = _richTextBox.Font.Name;
			_lineSpacingTwips = Convert.ToInt32(Math.Max(Math.Floor(LineHeight * 240), 0));

			EnableHighlighting = true;
			EnableColoredStyles = true;
		}

		/// <summary>
		/// Gets or sets a value indicating whether highlighting should be disabled or not.
		/// If true, the user input will remain intact. If false, the rich content will be
		/// modified to match the syntax of the currently selected language.
		/// </summary>
		public bool EnableHighlighting { get; set; }
		public bool EnableColoredStyles { get; set; }

		public void SetCharacterNames(string[] names, SyntaxStyle syntaxStyle, int Order = -1)
		{
			int index = _patternStyles.FindIndex(p => p.Name == "names");
			if (index != -1)
				_patternStyles.RemoveAt(index);
			AddPattern("names", new PatternDefinition(names), syntaxStyle, Order);
		}

		public void AddPattern(PatternDefinition patternDefinition, SyntaxStyle syntaxStyle, int Order = -1)
		{
			AddPattern((_patternStyles.Count + 1).ToString(CultureInfo.InvariantCulture), patternDefinition, syntaxStyle, Order);
		}

		public void AddPattern(string name, PatternDefinition patternDefinition, SyntaxStyle syntaxStyle, int Order = -1)
		{
			if (patternDefinition == null)
				throw new ArgumentNullException("patternDefinition");
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("name must not be null or empty", "name");

			var existingPatternStyle = FindPatternStyle(name);

			if (existingPatternStyle != null)
				throw new ArgumentException("A pattern style pair with the same name already exists");

			_patternStyles.Add(new PatternStyleMap(name, patternDefinition, syntaxStyle, Order));
		}

		public void ClearPatterns()
		{
			_patternStyles.Clear();
			_styleGroupPairs = null;
		}

		protected SyntaxStyle GetDefaultStyle()
		{
			 return new SyntaxStyle(_richTextBox.ForeColor, _richTextBox.Font.Bold, _richTextBox.Font.Italic);
		}

		protected SyntaxStyle GetErrorStyle()
		{
			return new SyntaxStyle(Theme.Current.Error, false, false);
		}

		private PatternStyleMap FindPatternStyle(string name)
		{
			return _patternStyles.FirstOrDefault(p => String.Equals(p.Name, name, StringComparison.Ordinal));
		}

		/// <summary>
		/// Rehighlights the text-box content.
		/// </summary>
		public void ReHighlight(bool bForce = false)
		{
			if (EnableHighlighting || bForce)
			{
				_fontSizeFactor = Convert.ToInt32(_richTextBox.Font.Size * 2);
				_fontName = _richTextBox.Font.Name;
				_lineSpacingTwips = Convert.ToInt32(Math.Max(LineHeight * 240, 0));

				if (_isDuringHighlight)
					return;

				HighlighTextBase();
			}
		}

		internal IEnumerable<Expression> Parse(string text)
		{
			text = text.NormalizeLineBreaks("\n");
			var parsedExpressions = new List<Expression> 
			{ 
				new Expression(text, ExpressionType.None, String.Empty, -1),
			};
			var patternStyles = new List<PatternStyleMap>(_patternStyles); // Copy for async
			foreach (var patternStyleMap in patternStyles)
			{
				parsedExpressions = ParsePattern(patternStyleMap, parsedExpressions);
			}

			parsedExpressions = ProcessLineBreaks(parsedExpressions);
			return parsedExpressions;
		}

		private Regex _lineBreakRegex;

		private Regex GetLineBreakRegex()
		{
			if (_lineBreakRegex == null)
				_lineBreakRegex = new Regex(Regex.Escape("\n"), RegexOptions.Compiled);

			return _lineBreakRegex;
		}

		private List<Expression> ProcessLineBreaks(List<Expression> expressions)
		{
			var parsedExpressions = new List<Expression>();

			var regex = GetLineBreakRegex();

			foreach (var inputExpression in expressions)
			{
				int lastProcessedIndex = -1;

				foreach (Match match in regex.Matches(inputExpression.Content).Cast<Match>().OrderBy(m => m.Index))
				{
					if (match.Success)
					{
						if (match.Index > lastProcessedIndex + 1)
						{
							string nonMatchedContent = inputExpression.Content.Substring(lastProcessedIndex + 1,
								match.Index - lastProcessedIndex - 1);
							var nonMatchedExpression = new Expression(nonMatchedContent, inputExpression.Type,
								inputExpression.Group, -1);
							parsedExpressions.Add(nonMatchedExpression);
						}

						string matchedContent = inputExpression.Content.Substring(match.Index, match.Length);
						var matchedExpression = new Expression(matchedContent, ExpressionType.Newline, "line-break", -1);
						parsedExpressions.Add(matchedExpression);
						lastProcessedIndex = match.Index + match.Length - 1;
					}
				}

				if (lastProcessedIndex < inputExpression.Content.Length - 1)
				{
					string nonMatchedContent = inputExpression.Content.Substring(lastProcessedIndex + 1,
						inputExpression.Content.Length - lastProcessedIndex - 1);
					var nonMatchedExpression = new Expression(nonMatchedContent, inputExpression.Type, inputExpression.Group, inputExpression.Order);
					parsedExpressions.Add(nonMatchedExpression);
				}
			}

			return parsedExpressions;
		}

		private List<Expression> ParsePattern(PatternStyleMap patternStyleMap, List<Expression> expressions)
		{
			var parsedExpressions = new List<Expression>();

			foreach (var inputExpression in expressions)
			{
				if (inputExpression.Type != ExpressionType.None && !(inputExpression.Order >= 0 && patternStyleMap.Order >= 0 && inputExpression.Order < patternStyleMap.Order))
				{
					parsedExpressions.Add(inputExpression);
				}
				else
				{
					var regex = patternStyleMap.PatternDefinition.Regex;

					int lastProcessedIndex = -1;

					foreach (var match in regex.Matches(inputExpression.Content).Cast<Match>().OrderBy(m => m.Index))
					{
						if (match.Success)
						{
							if (match.Index > lastProcessedIndex + 1)
							{
								string nonMatchedContent = inputExpression.Content.Substring(lastProcessedIndex + 1, match.Index - lastProcessedIndex - 1);
								var nonMatchedExpression = new Expression(nonMatchedContent, inputExpression.Type, inputExpression.Group, inputExpression.Order);
								parsedExpressions.Add(nonMatchedExpression);
							}

							string matchedContent = inputExpression.Content.Substring(match.Index, match.Length);
							var matchedExpression = new Expression(matchedContent, patternStyleMap.PatternDefinition.ExpressionType, patternStyleMap.Name, patternStyleMap.Order);
							parsedExpressions.Add(matchedExpression);
							lastProcessedIndex = match.Index + match.Length - 1;
						}
					}

					// Tail
					if (lastProcessedIndex < inputExpression.Content.Length - 1)
					{
						string nonMatchedContent = inputExpression.Content.Substring(lastProcessedIndex + 1, inputExpression.Content.Length - lastProcessedIndex - 1);
						var nonMatchedExpression = new Expression(nonMatchedContent, inputExpression.Type, inputExpression.Group, inputExpression.Order);
						parsedExpressions.Add(nonMatchedExpression);
					}
				}
			}

			return parsedExpressions;
		}

		internal IEnumerable<StyleGroupPair> GetStyles()
		{
			yield return new StyleGroupPair(GetDefaultStyle(), String.Empty);
			yield return new StyleGroupPair(GetErrorStyle(), String.Empty);

			foreach (var patternStyle in _patternStyles)
			{
				var style = patternStyle.SyntaxStyle;
				yield return new StyleGroupPair(style, patternStyle.Name);
			}
		}

		internal virtual string GetGroupName(Expression expression)
		{
			return expression.Group;
		}

		private List<StyleGroupPair> GetStyleGroupPairs()
		{
			if (_styleGroupPairs == null)
			{
				_styleGroupPairs = GetStyles().ToList();

				for (int i = 0; i < _styleGroupPairs.Count; i++)
				{
					_styleGroupPairs[i].Index = i + 1;
				}
			}

			return _styleGroupPairs;
		}

		#region RTF Stuff

		private static string HandleFonts(string s, ref int currentFont)
		{
			bool bSkipOne = false;

			var sb = new StringBuilder(s.Length);
			foreach (var c in s)
			{
				int range;
				if (bSkipOne)
				{
					range = currentFont;
					bSkipOne = false;
				}
				else if (c == ' ' || c == ErrorStart || c == ErrorEnd)
					range = currentFont;
				else if ((c >= '\u3041' && c <= '\u3096')	// Hiragana
					|| (c >= '\u30A0' && c <= '\u30ff')		// Katakana
					|| (c >= '\u3400' && c <= '\u4db5')		// Kanji
					|| (c >= '\u4e00' && c <= '\u9fcb')		// Kanji
					|| (c >= '\uf900' && c <= '\ufa6a')		// Kanji
					|| (c >= '\u2e80' && c <= '\u2fd5')		// Kanji radicals
					|| (c >= '\uff5f' && c <= '\uff9f')		// Halfwidth katakana & punctuation
					|| (c >= '\u3000' && c <= '\u303f')		// Symbols and punctuation
					|| (c >= '\u31f0' && c <= '\u31ff')		// Miscellaneous
					|| (c >= '\u3220' && c <= '\u3243')		// Miscellaneous
					|| (c >= '\u3280' && c <= '\u337F')		// Miscellaneous
					|| (c >= '\uff01' && c <= '\uff5e')		// Alphanumeric and punctuation
					) range = CharacterSet.Japanese;
				else if (c >= '\ud800' && c <= '\udbff')	// High surrogates
				{
					range = CharacterSet.Emoji;
					bSkipOne = true;
				}
				else if (c >= '\u2700' && c <= '\u27bf')    // Dingbats (Emoji)
				{
					range = CharacterSet.Emoji;
				}
				else if (c == '\ufe0e'						// Variant selector-15
					|| c == '\ufe0f'						// Variant selector-16
					) range = CharacterSet.Emoji;
				else if ((c >= '\u2010' && c <= '\u2027')	// General punctuation
					|| (c >= '\u2030' && c <= '\u205f')		// General punctuation
					) range = CharacterSet.GeneralPunctuation;
				else if (c >= '\u2000' && c <= '\u206f')	// General punctuation (format and spaces)
					range = currentFont;
				else
					range = CharacterSet.Default;

				// Switch font by range
				if (range != currentFont)
				{
					sb.Append("\\f");
					sb.Append(range);
					sb.Append(' ');
					currentFont = range;
				}

				if (c == '\\')
					sb.Append("\\\\");
				else if (c == '{')
					sb.Append("\\{");
				else if (c == '}')
					sb.Append("\\}");
				else if (c <= 0xff)
					sb.Append(c);
				else if (c == ErrorStart || c == ErrorEnd)
					sb.Append(c);
				else
				{
					unchecked
					{
						if (c < 0x8000)
							sb.Append("\\u" + Convert.ToInt16(c) + "?");
						else
							sb.Append("\\u" + Convert.ToInt16(c - 65536) + "?");
					}
				}
			}
			return sb.ToString();
		}

		private void HighlighTextBase()
		{
			_isDuringHighlight = true;
			_richTextBox.Rtf = Highlight(_richTextBox.Text);
			_isDuringHighlight = false;
		}

		/// <summary>
		/// The base method that highlights the text-box content.
		/// </summary>
		public string Highlight(string text)
		{
			if (text.Length == 0)
				return text;

			try
			{
				var sb = new StringBuilder(32768);

				sb.AppendLine(RTFHeader());
				sb.AppendLine(RTFColorTable());
				sb.Append(@"\viewkind4\deftab480\uc1\pard\f0\fs").Append(_fontSizeFactor).Append(" ");
				sb.AppendFormat("\\sl{0}\\slmult0", _lineSpacingTwips);
				int currentFont = CharacterSet.Default;
				int contentPos = 0;
				string contentFull = text;
				foreach (var expression in Parse(contentFull))
				{
					if (expression.Type == ExpressionType.Whitespace)
					{
						string content = expression.Content;
						sb.Append(content);
						contentPos += content.Length;
					}
					else if (expression.Type == ExpressionType.Newline)
					{
						sb.Append(@"\par");
						contentPos += 1;
					}
					else
					{
						string content = expression.Content;

						// Spelling errors
						StringBuilder sbErrors = null;
						if (_textSpans != null)
						{
							try
							{
								foreach (var word in _textSpans.GetWordsAt(contentPos, content.Length).Where(w => w.tag == TextSpan.Word.Tag.Misspelled))
								{
									if (sbErrors == null)
										sbErrors = new StringBuilder(content);

									sbErrors.Insert(word.start + word.length - contentPos, ErrorEnd);
									sbErrors.Insert(word.start - contentPos, ErrorStart);
								}
							}
							catch
							{
								// If _textSpan is out of sync, we could easily crash here.
								sbErrors = null;
							}
						}
						contentPos += content.Length;
						if (sbErrors != null)
							content = sbErrors.ToString();
						content = HandleFonts(content, ref currentFont);

						var styleGroups = GetStyleGroupPairs();

						string groupName = GetGroupName(expression);

						var styleToApply = styleGroups.FirstOrDefault(s => String.Equals(s.GroupName, groupName, StringComparison.Ordinal));

						if (styleToApply != null)
						{
							string opening = String.Empty, closing = String.Empty;

							if (styleToApply.SyntaxStyle.Bold)
							{
								opening += @"\b";
								closing += @"\b0";
							}

							if (styleToApply.SyntaxStyle.Italic)
							{
								opening += @"\i";
								closing += @"\i0";
							}
							if (styleToApply.SyntaxStyle.Underline)
							{
								opening += @"\ulwave";
								closing += @"\ul0";
							}
							
							if (styleToApply.SyntaxStyle.Monospace)
							{
								opening += @"\f4";
								closing += @"\f0";
							}

							sb.AppendFormat(@"\cf{0}{2} {1}\cf0{3} ", EnableColoredStyles ? styleToApply.Index : 0, content, opening, closing);
						}
						else
						{
							sb.AppendFormat(@"\cf{0} {1}\cf0 ", 1, content);
						}
					}
				}

				sb.Append(@"\par }");

				sb.Replace("\uFFF0", string.Format("{{\\cf{0}\\ulwave ", EnableColoredStyles ? 2 : 0));
				sb.Replace("\uFFF1", @"\ul0 }");


				return sb.ToString();
			}
			finally
			{
			}
		}

		public void SetSpellChecking(TextSpans textSpan)
		{
			_textSpans = textSpan;
		}

		private string RTFColorTable()
		{
			var styleGroupPairs = GetStyleGroupPairs();

			if (styleGroupPairs.Count <= 0)
				styleGroupPairs.Add(new StyleGroupPair(GetDefaultStyle(), String.Empty));

			var sbRtfColorTable = new StringBuilder();
			sbRtfColorTable.Append(@"{\colortbl ;");

			foreach (var styleGroup in styleGroupPairs)
			{
				sbRtfColorTable.AppendFormat("{0};", ColorUtils.ColorToRtfTableEntry(styleGroup.SyntaxStyle.Color));
			}

			sbRtfColorTable.Append("}");

			return sbRtfColorTable.ToString();
		}

		private string RTFHeader()
		{
			return String.Concat(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 ", _fontName, @";\f1\fnil\fcharset0 ", _emojiFontName, @";\f2\fnil\fcharset0 ", _japaneseFontName, @";\f3\fnil\fcharset0 ", _generalPunctuationFontName, @";\f4\fnil\fcharset0 ", _monoFontName, @";}}");
		}

		#endregion

	}
}
