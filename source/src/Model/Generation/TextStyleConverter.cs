using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ginger
{
	public static class TextStyleConverter
	{
		private class Spans
		{
			public enum Mode
			{
				Undefined,
				None,
				Dialogue,   // "..."
				NonVerbal,  // *...*
				Name,
			}

			public struct Span
			{
				public int startPos;
				public int endPos;
				public Mode mode;
				public int length { get { return endPos - startPos; } }
			}

			public List<Span> spans = new List<Span>();
			public int Count { get { return spans.Count; } }

			public void Add(Mode mode, int pos, int length)
			{
				spans.Add(new Span() {
					mode = mode,
					startPos = pos,
					endPos = pos + length,
				});
			}

			public void Sort()
			{
				spans = spans.OrderBy(s => s.startPos).ToList();
			}

			public void FillGaps(Mode mode, string text)
			{
				Func<int, int, bool> CheckAndAdd = (int pos, int len) => {
					if (len == 0)
						return false;

					if (mode == Mode.Dialogue || mode == Mode.NonVerbal)
					{
						var word = text.Substring(pos, len);
						if (word.ContainsNoneOf(c => !(char.IsPunctuation(c) || char.IsWhiteSpace(c))))
						{
							Add(Mode.None, pos, len);
							return true;
						}
					}
					Add(mode, pos, len);
					return true;
				};

				Sort();
				int length = text.Length;

				if (spans.Count == 0)
					CheckAndAdd(0, length);
				else
				{
					int n = spans.Count - 1;
					CheckAndAdd(spans[spans.Count - 1].endPos, length - spans[spans.Count - 1].endPos); // Tail
					CheckAndAdd(0, spans[0].startPos); // Head

					for (int i = 0; i < n; ++i)
					{
						var first = spans[i];
						var second = spans[i + 1];
						if (first.endPos < second.startPos)
							CheckAndAdd(first.endPos, second.startPos - first.endPos);
					}
				}
			}

			public void Trim(string text)
			{
				for (int i = 0; i < spans.Count; ++i)
				{
					var span = spans[i];

					for (int pos = span.endPos - 1; pos > span.startPos; --pos)
					{
						if (char.IsWhiteSpace(text[pos]))
							span.endPos = pos;
						else
							break;
					}
					for (int pos = span.startPos; pos < span.endPos; ++pos)
					{
						span.startPos = pos;
						if (!char.IsWhiteSpace(text[pos]))
							break;
					}
					spans[i] = span;
				}

				spans = spans
					.Where(s => s.length > 0 && string.IsNullOrWhiteSpace(text.Substring(s.startPos, s.length)) == false)
					.OrderBy(s => s.startPos)
					.ToList();
			}

			public Span GetSpanAt(int position)
			{
				foreach (var span in spans)
				{
					if (position >= span.startPos && position < span.endPos)
						return span;
				}
				return default(Span);
			}

			public bool CheckSpan(int startPos, int endPos)
			{
				foreach (var span in spans)
				{
					if (startPos > span.startPos && startPos < span.endPos)
						return false;
					if (endPos > span.startPos && endPos < span.endPos)
						return false;
					if (span.startPos > startPos && span.startPos < endPos)
						return false;
					if (span.endPos > startPos && span.endPos < endPos)
						return false;
				}
				return true;
			}
		}

		public static string Convert(string value, CardData.TextStyle textStyle)
		{
			if (string.IsNullOrEmpty(value) || textStyle == CardData.TextStyle.None)
				return value;

			value = MarkStyles(value);
			return ApplyStyle(value, textStyle);
		}

		public static string MarkStyles(string value, bool bNamesOnly = false)
		{
			if (string.IsNullOrEmpty(value))
				return value;

			StringBuilder sbReplace = new StringBuilder(value);
			sbReplace.Replace(GingerString.CharacterMarker, "__CCCC__");
			sbReplace.Replace(GingerString.UserMarker, "__UUUU__");
			string text = sbReplace.ToString();

			value = text;

			// Convert unicode quotation marks
			if (value.IndexOfAny(new char[] { '\u201C', '\u201D', '\u201E', '\u201F', '\u2018', '\u2019' }) != -1)
			{
				StringBuilder sbQuote = new StringBuilder(value);
				sbQuote.Replace('\u201C', '"'); // “
				sbQuote.Replace('\u201D', '"'); // ”
				sbQuote.Replace('\u201E', '"'); // „
				sbQuote.Replace('\u201F', '"'); // ‟
				sbQuote.Replace('\u2018', '\''); // ‘ ’
				sbQuote.Replace('\u2019', '\'');
				value = sbQuote.ToString();
			}

			// Convert linebreaks
			value = value.ConvertLinebreaks(Linebreak.LF);

			// Trim lines
			value = string.Join("\n", value.Split(new char[] { '\n' }, StringSplitOptions.None).Select(s => s.Trim()));

			// Process each paragraph separately
			var paragraphs = value.Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

			for (int par = 0; par < paragraphs.Length; ++par)
			{
				var paragraph = paragraphs[par].Trim();
				if (string.IsNullOrWhiteSpace(paragraph))
				{
					paragraphs[par] = null;
					continue;
				}

				bool hasQuotes = paragraph.IndexOfAny(new char[] { '"', '\u300C' }) != -1;
				bool hasAsterisks = paragraph.IndexOf('*') != -1;
				bool hasParentheses = paragraph.IndexOf('(') != -1 && paragraph.Count(c => c == '(')  == paragraph.Count(c => c == ')');
				bool canFill = true;

				var spans = new Spans();

				// Exclude names at the beginning of a line ("Name: ...")
				int pos_begin = 0;
				int pos_colon = paragraph.IndexOf(':', pos_begin);
				while (pos_colon != -1)
				{
					int pos_line = paragraph.IndexOfReverse('\n', pos_colon - 1);
					if (pos_line >= 0 && pos_line < pos_begin)
						pos_line = pos_begin;
					if (pos_line == -1)
						pos_line = 0;

					string possibleName = paragraph.Substring(pos_line, pos_colon - pos_line).Trim();
					if (possibleName == "__CCCC__"
						|| possibleName == "__UUUU__"
						|| string.Compare(Current.Card.name, possibleName, true) == 0
						|| possibleName.BeginsWith("<##CHAR")
						|| Current.Characters.ContainsAny(c => string.Compare(c.name, possibleName, true) == 0) // Known name
						|| (possibleName.IndexOfAny(new char[] { ',', '.', ':', ';', '!', '?', '<', '>' }, 0) == -1 // No punctuation
							&& possibleName.Count(c => char.IsWhiteSpace(c)) < 4)) // Less than 4 words
					{
						// Don't format
						spans.Add(Spans.Mode.Name, pos_line, pos_colon - pos_line + 1);

						pos_line = paragraph.IndexOf('\n', pos_colon);
						if (pos_line == -1)
							break;
						pos_begin = pos_line + 1;
						pos_colon = paragraph.IndexOf(':', pos_begin);
						continue;
					}

					pos_begin = pos_colon + 1;
					pos_colon = paragraph.IndexOf(':', pos_begin);
				}

				if (!hasQuotes && !hasAsterisks && hasParentheses) // Only parentheses: Assume dialogue
					hasAsterisks = true;
				else if (spans.Count > 0 && !hasQuotes && !hasAsterisks) // Found names: Assume dialogue
					hasAsterisks = true;
				else if (!hasQuotes && !hasAsterisks && hasParentheses)
					hasQuotes = false; // Found parentheses
				else if (!hasQuotes && !hasAsterisks) // Found no names: Assume non-verbal
					hasQuotes = true;
				else if (hasQuotes && hasAsterisks)
					canFill = false; // Can't disambiguate

				// Identify code blocks
				MarkSpan('`', Spans.Mode.None, paragraph, spans);

				// Identify non-verbal actions
				MarkSpan('*', Spans.Mode.NonVerbal, paragraph, spans);

				// Identify dialogue
				MarkSpan('"', Spans.Mode.Dialogue, paragraph, spans);
				MarkSpan('\u300C', '\u300D', Spans.Mode.Dialogue, paragraph, spans);
				
				// Identify non-verbal actions in parentheses
				if (hasParentheses)
					MarkSpan('(', ')', Spans.Mode.NonVerbal, paragraph, spans);

				if (canFill)
					spans.FillGaps(hasQuotes ? Spans.Mode.NonVerbal : Spans.Mode.Dialogue, paragraph);
				spans.Trim(paragraph);

				// Apply style
				bool bApplyStyle = Current.Card.textStyle != CardData.TextStyle.None;
				StringBuilder sb = new StringBuilder(paragraph);

				for (int i = spans.spans.Count - 1; i >= 0; --i)
				{
					var span = spans.spans[i];

					if (bApplyStyle && span.mode == Spans.Mode.Dialogue && !bNamesOnly)
						ReplaceEncapsulationMarks(sb, span, "<__DIALOGUE>", "</__DIALOGUE>");
					else if (bApplyStyle && span.mode == Spans.Mode.NonVerbal && !bNamesOnly)
						ReplaceEncapsulationMarks(sb, span, "<__ACTION>", "</__ACTION>");
					else if (span.mode == Spans.Mode.Name)
					{
						sb.Insert(span.endPos, "</__NAME>");
						sb.Insert(span.startPos, "<__NAME>");
					}
				}

				paragraphs[par] = sb.ToString();
			}

			StringBuilder sbNewValue = new StringBuilder(string.Join("\n\n", paragraphs.Where(s => string.IsNullOrWhiteSpace(s) == false)));
			sbNewValue.Replace("__CCCC__", GingerString.CharacterMarker);
			sbNewValue.Replace("__UUUU__", GingerString.UserMarker);
			sbNewValue.ConvertLinebreaks(Linebreak.CRLF);
			return sbNewValue.ToString();
		}

		public static string ApplyStyle(string value, CardData.TextStyle textStyle)
		{
			if (string.IsNullOrEmpty(value))
				return value;

			string[] Dialogue;
			string[] Action;
			switch (textStyle)
			{
			case CardData.TextStyle.Chat:
				Dialogue	= new string[] { "", "" };
				Action		= new string[] { "*", "*" };
				break;
			case CardData.TextStyle.Novel:
				Dialogue	= new string[] { "\"", "\"" };
				Action		= new string[] { "", "" };
				break;
			default:
			case CardData.TextStyle.Mixed:
				Dialogue	= new string[] { "\"", "\"" };
				Action		= new string[] { "*", "*" };
				break;
			case CardData.TextStyle.Decorative:
				Dialogue	= new string[] { "\u201C", "\u201D" };
				Action		= new string[] { "", "" };
				break;
			case CardData.TextStyle.Bold:
				Dialogue	= new string[] { "", "" };
				Action		= new string[] { "**", "**" };
				break;
			case CardData.TextStyle.Parentheses:
				Dialogue	= new string[] { "", "" };
				Action		= new string[] { "(", ")" };
				break;
			case CardData.TextStyle.Japanese:
				Dialogue	= new string[] { "\u300C", "\u300D" };
				Action		= new string[] { "", "" };
				break;
			}

			StringBuilder sb = new StringBuilder(value);
			sb.Replace("<__DIALOGUE>", Dialogue[0]);
			sb.Replace("</__DIALOGUE>", Dialogue[1]);
			sb.Replace("<__ACTION>", Action[0]);
			sb.Replace("</__ACTION>", Action[1]);
			sb.Replace("<__NAME>", "");
			sb.Replace("</__NAME>", "");
			return sb.ToString();
		}

		private static bool SkipSpans(ref int pos, Spans spans)
		{
			var span = spans.GetSpanAt(pos);
			if (span.mode == Spans.Mode.Undefined)
				return false;

			while (span.mode != Spans.Mode.Undefined)
			{
				pos = span.endPos;
				span = spans.GetSpanAt(pos);
			}
			return true;
		}

		private static string GetEncapsulationMark(string text, int pos)
		{
			char ch = text[pos];
			if (ch == '"')
				return "\"";
			if (pos + 1 >= text.Length || text[pos + 1] != ch)
				return char.ToString(ch);
			var sbSymbol = new StringBuilder();
			for (; pos < text.Length; ++pos)
			{
				if (text[pos] != ch)
					break;
				sbSymbol.Append(ch);
			}
			return sbSymbol.ToString();
		}

		private static void ReplaceEncapsulationMarks(StringBuilder sb, Spans.Span span, char chOpen = '\0', char chClose = '\0')
		{
			ReplaceEncapsulationMarks(sb, span, chOpen != '\0' ? char.ToString(chOpen) : null, chClose != '\0' ? char.ToString(chClose) : null);
		}

		private static void ReplaceEncapsulationMarks(StringBuilder sb, Spans.Span span, string toOpen, string toClose = null)
		{
			int start = span.startPos;
			int end = span.endPos;
			string fromOpen = char.ToString(sb[start]);
			string fromClose = char.ToString(sb[end - 1]);
			char[] marks = new char[] { '"', '*', '\u201C', '\u300C', '(' };
			bool bRemove = marks.Contains(sb[span.startPos]);

			if (toOpen != null && toClose == null)
				toClose = toOpen;

			if (fromOpen == fromClose)
				fromOpen = fromClose = GetEncapsulationMark(sb.ToString(), start);

			if (fromClose != toClose)
			{
				if (bRemove)
				{
					sb.Remove(end - fromClose.Length, fromClose.Length);
					if (toClose != null)
						sb.Insert(end - fromClose.Length, toClose);
				}
				else
				{
					if (toClose != null)
						sb.Insert(end, toClose);
				}
			}
			if (fromOpen != toOpen)
			{
				if (bRemove)
				{
					sb.Remove(start, fromOpen.Length);
					if (toOpen != null)
						sb.Insert(start, toOpen);
				}
				else
				{
					if (toOpen != null)
						sb.Insert(start, toOpen);
				}
			}
		}

		private static void MarkSpan(char ch, Spans.Mode mode, string text, Spans spans)
		{
			int pos = 0;
			SkipSpans(ref pos, spans);
			int pos_next = text.IndexOf(ch, pos);
			while (pos_next > 0 && text[pos_next - 1] == '\\') // Escape
				pos_next = text.IndexOf(ch, pos_next + 1);

			while (pos_next != -1)
			{
				if (SkipSpans(ref pos_next, spans))
				{
					pos_next = text.IndexOf(ch, pos_next);
					continue;
				}

				string symbol = GetEncapsulationMark(text, pos_next);

				int pos_end = text.IndexOf(symbol, pos_next + symbol.Length);
				while (pos_end > 0 && text[pos_end - 1] == '\\') // Escape
					pos_end = text.IndexOf(symbol, pos_end + 1);
				if (pos_end != -1)
				{
					if (spans.CheckSpan(pos_next, pos_end)) // Not overlapping
						spans.Add(mode, pos_next, pos_end - pos_next + symbol.Length);
					pos_next = text.IndexOf(symbol, pos_end + symbol.Length);
				}
				else
					break;
			}
		}

		private static void MarkSpan(char chBegin, char chEnd, Spans.Mode mode, string text, Spans spans)
		{
			int pos = 0;
			SkipSpans(ref pos, spans);
			int pos_next = text.IndexOf(chBegin, pos);
			while (pos_next > 0 && text[pos_next - 1] == '\\') // Escape
				pos_next = text.IndexOf(chBegin, pos_next + 1);
			while (pos_next != -1)
			{
				if (SkipSpans(ref pos_next, spans))
				{
					pos_next = text.IndexOf(chBegin, pos_next);
					continue;
				}

				int pos_end = text.IndexOf(chEnd, pos_next + 1);
				while (pos_end > 0 && text[pos_end - 1] == '\\') // Escape
					pos_end = text.IndexOf(chEnd, pos_end + 1);
				if (pos_end != -1)
				{
					if (spans.CheckSpan(pos_next, pos_end)) // Not overlapping
						spans.Add(mode, pos_next, pos_end - pos_next + 1);
					pos_next = text.IndexOf(chBegin, pos_end + 1);
				}
				else
					break;
			}
		}
	}
}
