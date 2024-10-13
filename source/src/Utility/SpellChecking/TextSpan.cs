using System;
using System.Collections.Generic;

namespace Ginger
{
	public class TextSpans
	{
		public string text;
		public TextSpan[] spans;
		public int Count { get { return spans != null ? spans.Length : 0; } }

		private static readonly char[] SpanBreakChars = new char[] { '\n', '/', '`', '<', '!', '@', '{' };

		private static readonly string[] HtmlTags = new string[] { "!doctype", "a", "abbr", "acronym", "address", "applet", "area", "article", "aside", "audio", "b", "base", "basefont", "bb", "bdo", "big", "blockquote", "body", "br", "button", "canvas", "caption", "center", "cite", "code", "col", "colgroup", "command", "datagrid", "datalist", "dd", "del", "details", "dfn", "dialog", "dir", "div", "dl", "dt", "em", "embed", "eventsource", "fieldset", "figcaption", "figure", "font", "footer", "form", "frame", "frameset", "h1", "h2", "h3", "h4", "h5", "h6", "head", "header", "hgroup", "hr", "html", "i", "iframe", "img", "input", "ins", "isindex", "kbd", "keygen", "label", "legend", "li", "link", "map", "mark", "menu", "meta", "meter", "nav", "noframes", "noscript", "object", "ol", "optgroup", "option", "output", "p", "param", "pre", "progress", "q", "rp", "rt", "ruby", "s", "samp", "script", "section", "select", "small", "source", "span", "strike", "strong", "style", "sub", "sup", "table", "tbody", "td", "textarea", "tfoot", "th", "thead", "time", "title", "tr", "track", "tt", "u", "ul", "var", "video", "wbr" };

		public static TextSpans FromString(string text)
		{
			List<TextSpan> spans = new List<TextSpan>();
			int pos = 0;
			int pos_end = text.IndexOfAny(SpanBreakChars, pos);
			while (pos_end != -1)
			{
				char ch = text[pos_end];

				if (ch == '/') // /* ... */
				{
					if (pos_end + 1 < text.Length && text[pos_end + 1] == '*')
					{
						int end_comment = text.IndexOf("*/", pos_end + 2);
						if (end_comment != -1)
						{
							spans.Add(TextSpan.FromString(text.Substring(pos, pos_end - pos), pos));
							spans.Add(TextSpan.Ignore(text.Substring(pos_end, end_comment - pos_end + 2), pos_end));
							pos = end_comment + 2;
							pos_end = text.IndexOfAny(SpanBreakChars, pos);
							continue;
						}
					}
					// Wasn't a comment. Continue...
					pos_end = text.IndexOfAny(SpanBreakChars, pos_end + 1);
					continue;
				}

				if (ch == '<')
				{
					// HTML comment <!-- ... -->
					if (pos_end + 3 < text.Length && text[pos_end + 1] == '!' && text[pos_end + 2] == '-' && text[pos_end + 3] == '-')
					{
						int end_comment = text.IndexOf("-->", pos_end + 4);
						if (end_comment != -1)
						{
							spans.Add(TextSpan.FromString(text.Substring(pos, pos_end - pos), pos));
							spans.Add(TextSpan.Ignore(text.Substring(pos_end, end_comment - pos_end + 3), pos_end));
							pos = end_comment + 3;
							pos_end = text.IndexOfAny(SpanBreakChars, pos);
							continue;
						}
					}

					// HTML tag <a href=""></a>
					int end_html = text.IndexOf('>', pos_end + 1);
					if (end_html != -1)
					{
						int begin_html = pos_end + 1;
						if (text[begin_html] == '/')
							begin_html += 1;

						string tag = text.Substring(begin_html, end_html - begin_html).ToLowerInvariant();
						if (HtmlTags.ContainsAny(t => tag.BeginsWith(t)))
						{
							spans.Add(TextSpan.FromString(text.Substring(pos, pos_end - pos), pos));
							spans.Add(TextSpan.Ignore(text.Substring(pos_end, end_html - pos_end + 1), pos_end));
							pos = end_html + 1;
							pos_end = text.IndexOfAny(SpanBreakChars, pos);
							continue;
						}
					}
					// Wasn't HTML. Continue...
					pos_end = text.IndexOfAny(SpanBreakChars, pos_end + 1);
					continue;
				}

				if (ch == '!') // ![]()
				{
					if (pos_end + 1 < text.Length && text[pos_end + 1] == '[')
					{
						int pos_endl = text.IndexOf('\n', pos_end + 1);
						if (pos_endl == -1)
							pos_endl = text.Length;
						int pos_mid = text.IndexOf("](", pos_end + 1);
						if (pos_mid != -1 && pos_mid < pos_endl)
						{
							int end_markdown = text.IndexOf(')', pos_mid + 2);
							if (end_markdown != -1 && end_markdown < pos_endl)
							{
								spans.Add(TextSpan.FromString(text.Substring(pos, pos_end - pos), pos));
								spans.Add(TextSpan.Ignore(text.Substring(pos_end, end_markdown - pos_end + 1), pos_end));
								pos = end_markdown + 1;
								pos_end = text.IndexOfAny(SpanBreakChars, pos);
								continue;
							}
						}
					}
					// Wasn't markdown. Continue...
					pos_end = text.IndexOfAny(SpanBreakChars, pos_end + 1);
					continue;
				}

				if (ch == '`') // ` ... `
				{
					int end_code = text.IndexOf('`', pos_end + 1);
					if (end_code != -1)
					{
						spans.Add(TextSpan.FromString(text.Substring(pos, pos_end - pos), pos));
						spans.Add(TextSpan.Ignore(text.Substring(pos_end, end_code - pos_end + 1), pos_end));
						pos = end_code + 1;
						pos_end = text.IndexOfAny(SpanBreakChars, pos);
						continue;
					}
					// Wasn't code. Continue...
					pos_end = text.IndexOfAny(SpanBreakChars, pos_end + 1);
					continue;
				}

				if (ch == '@') // @@ ... \n
				{
					if (pos_end + 1 < text.Length && text[pos_end + 1] == '@' && (pos_end == 0 || text[pos_end - 1] == '\n'))
					{
						int end_decorator = text.IndexOf('\n', pos_end + 2);
						if (end_decorator == -1)
							end_decorator = text.Length - 1;

						spans.Add(TextSpan.FromString(text.Substring(pos, pos_end - pos), pos));
						spans.Add(TextSpan.Ignore(text.Substring(pos_end, end_decorator - pos_end + 1), pos_end));
						pos = end_decorator;
						pos_end = text.IndexOfAny(SpanBreakChars, pos);
						continue;
					}
					// Wasn't a comment. Continue...
					pos_end = text.IndexOfAny(SpanBreakChars, pos_end + 1);
					continue;
				}

				if (ch == '{') // {$variable}
				{
					// Custom variable
					if (pos_end + 1 < text.Length && text[pos_end + 1] == '$')
					{
						int end_variable = text.IndexOf('}', pos_end + 2);
						if (end_variable != -1)
						{
							spans.Add(TextSpan.FromString(text.Substring(pos, pos_end - pos), pos));
							spans.Add(TextSpan.Variable(text.Substring(pos_end, end_variable - pos_end + 1), pos_end));
							pos = end_variable + 1;
							pos_end = text.IndexOfAny(SpanBreakChars, pos);
							continue;
						}
					}
					// Wasn't a variable. Continue...
					pos_end = text.IndexOfAny(SpanBreakChars, pos_end + 1);
					continue;
				}

				var span = TextSpan.FromString(text.Substring(pos, pos_end - pos), pos);
				spans.Add(span);

				if (ch == '\n')
				{
					span.length += 1;
					pos = pos_end + 1;
					pos_end = text.IndexOfAny(SpanBreakChars, pos);
				}
				else
				{
					pos = pos_end;
					pos_end = text.IndexOfAny(SpanBreakChars, pos + 1);
				}
			}

			// Last row
			var lastSpan = TextSpan.FromString(text.Substring(pos), pos);
			spans.Add(lastSpan);

			return new TextSpans() {
				text = text,
				spans = spans.ToArray(),
			};
		}

		public TextSpan.Word.Tag GetTagAt(int pos)
		{
			for (int i = 0; i < spans.Length; ++i)
			{
				int offset = spans[i].offset;
				var words = spans[i].words;
				for (int j = 0; j < words.Length; ++j)
				{
					if (pos - offset >= words[j].start && pos - offset < words[j].start + words[j].length)
						return words[j].tag;
				}
			}
			return default(TextSpan.Word.Tag);
		}

		public string GetWordAt(int pos, out int start, out int length)
		{
			for (int i = 0; i < spans.Length; ++i)
			{
				int offset = spans[i].offset;
				var words = spans[i].words;
				for (int j = 0; j < words.Length; ++j)
				{
					if (pos - offset >= words[j].start && pos - offset < words[j].start + words[j].length)
					{
						start = words[j].start + offset;
						length = words[j].length;
						return spans[i].text.Substring(start - offset, length);
					}
				}
			}
			start = default;
			length = default;
			return null;
		}

		public IEnumerable<TextSpan.Word> GetWordsAt(int position, int length)
		{
			for (int i = spans.Length - 1; i >= 0; --i)
			{
				int offset = spans[i].offset;
				var words = spans[i].words;
				for (int j = words.Length - 1; j >= 0; --j) // Reversed
				{
					if (words[j].start + offset >= position && words[j].end + offset <= position + length)
						yield return new TextSpan.Word()
						{
							start = words[j].start + offset,
							length = words[j].length,
							tag = words[j].tag,
						};
				}
			}
		}
	}

	public class TextSpan
	{
		public struct Word
		{
			public enum Tag
			{
				Undefined = 0,
				Correct,
				Misspelled,
				Incomplete,
				Variable,
			}

			public Word(int start, int length, Tag tag = Tag.Undefined)
			{
				this.start = start;
				this.length = length;
				this.tag = tag;
			}
			public int start;
			public int length;
			public int end { get { return start + length; } }
			public Tag tag;
		}

		public Word[] words;
		public int offset = 0;
		public int length;
		public string text;

		private TextSpan()
		{
		}

		public TextSpan(Word[] words)
		{
			this.words = words;
		}

		public static TextSpan FromString(string text, int offset)
		{
			var words = new List<Word>(64);
			int wordStart = 0;
			bool currIsWord = false;
			for (int i = 0; i <= text.Length; ++i)
			{
				if (i == text.Length)
				{
					if (currIsWord) // Last word
					{
						int wordEnd = i - 1;
						Trim(text, ref wordStart, ref wordEnd);
						if (Filter(text, wordStart, wordEnd))
							words.Add(new Word(wordStart, wordEnd - wordStart + 1));
					}
					break;
				}

				char ch = text[i];
				bool isPunctuation = ch == '.' || ch == ',' || ch == ':' || ch == ';' || ch == '!' || ch == '?'
					|| ch == '"' || (ch >= '\u2018' && ch <= '\u201f') || (ch >= '\u2032' && ch <= '\u2037') || ch == '\u02b9' || ch == '\u02ba'
					|| ch == '*' || ch == '#' || ch == '%' || ch == '&' || ch == '/' || ch == '\\' || ch == '~'
					|| ch == '\u2013' // –
					|| ch == '\u2014' // —
					|| ch == '\u2026' // …
					|| ch == '\u2047' // ⁇
					|| ch == '\u2048' // ⁈
					|| ch == '\u2049' // ⁉
					|| ch == '\u00a1' // ¡
					|| ch == '\u00bf'; // ¿

				bool isBracket = ch == '{' || ch == '}' || ch == '(' || ch == ')' 
					|| ch == '[' || ch == ']' || ch == '<' || ch == '>'
					|| ch == '\u300c' // 「
					|| ch == '\u300d' // 」
					|| ch == '\u300e' // 『
					|| ch == '\u300f' // 』
					|| ch == '\u3010' // 【
					|| ch == '\u3011'; // 】

				bool isWord = !(char.IsWhiteSpace(ch) || isPunctuation)
					|| ch == '-'; // Allow hyphens

				bool shouldBreak = isBracket || char.IsSymbol(ch);
				if (currIsWord != isWord || currIsWord && shouldBreak)
				{
					if (currIsWord)
					{
						// Trim
						int wordEnd = i - 1;
						Trim(text, ref wordStart, ref wordEnd);
						if (Filter(text, wordStart, wordEnd))
							words.Add(new Word(wordStart, wordEnd - wordStart + 1));
					}
					else
						wordStart = i;
					currIsWord = isWord && !shouldBreak;
				}
			}
			return new TextSpan() {
				text = text,
				offset = offset,
				words = words.ToArray(),
				length = text.Length,
			};
		}

		public static TextSpan Ignore(string text, int offset)
		{
			return new TextSpan() {
				text = text,
				offset = offset,
				words = new Word[0],
				length = text.Length,
			};
		}

		public static TextSpan Variable(string text, int offset)
		{
			return new TextSpan() {
				text = text,
				offset = offset,
				words = new Word[1] { new Word(0, text.Length, Word.Tag.Variable) },
				length = text.Length,
			};
		}

		private static void Trim(string text, ref int wordStart, ref int wordEnd)
		{
			while (wordEnd > wordStart && !char.IsLetterOrDigit(text[wordEnd]))
				wordEnd--;
			while (wordStart <= wordEnd && !char.IsLetterOrDigit(text[wordStart]))
				wordStart++;
			if (wordEnd - wordStart > 2 && text.Substring(wordEnd - 1, 2) == "'s") // possessive
				wordEnd -= 2;
		}

		private static bool Filter(string text, int wordStart, int wordEnd)
		{
			if (wordStart > wordEnd)
				return false;

			// Do not spell check known character names
			string word = text.Substring(wordStart, wordEnd - wordStart + 1);
			if (Array.IndexOf(Current.Card.name.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries), word) != -1)
				return false;

			// Don't spell check numbers and countables
			if (char.IsDigit(text[wordStart]))
				return false;

			foreach (var character in Current.Characters)
			{
				if (Array.IndexOf(character.namePlaceholder.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries), word) != -1)
					return false;
			}
			if (Array.IndexOf(Current.Card.userPlaceholder.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries), word) != -1)
				return false;

			return true;
		}

		public string GetWord(int n)
		{
			if (n >= 0 && n < words.Length)
				return text.Substring(words[n].start, words[n].length);
			return null;
		}

		public override int GetHashCode()
		{
			return text.GetHashCode();
		}

	}
}
