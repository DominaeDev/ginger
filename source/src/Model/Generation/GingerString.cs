using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ginger
{
	/// <summary>
	/// Internal string representation used to handle conversion between formats.
	/// </summary>
	public struct GingerString
	{
		public string value { get; private set; }

		// Markers (user's representation)
		public static readonly string CharacterMarker = "{char}";
		public static readonly string UserMarker = "{user}";
		public static readonly string OriginalMarker = "{original}";
		public static readonly string NameMarker = "{name}";
		public static readonly string ContinueMarker = "{continue}"; // Greetings

		// Markers (internal representation)
		public static readonly string InternalCharacterMarker = "<##CHAR##>";
		public static readonly string InternalUserMarker = "<##USER##>";
		public static readonly string InternalOriginalMarker = "<##ORIGINAL##>";
		public static readonly string InternalNameMarker = "<##NAME##>";
		public static readonly string InternalContinueMarker = "<##CONTINUE##>";
		public static string MakeInternalCharacterMarker(int index) => string.Format("<##CHAR{0:D2}##>", index);

		// Backyard markers
		public static readonly string BackyardCharacterMarker = "{character}";
		public static readonly string BackyardUserMarker = "{user}";
		public static readonly string BackyardOriginalMarker = "{original}";
		
		// SillyTavern markers
		public static readonly string TavernCharacterMarker = "{{char}}";
		public static readonly string TavernUserMarker = "{{user}}";
		public static readonly string TavernOriginalMarker = "{{original}}";

		private GingerString(string value)
		{
			this.value = value;
		}

		public static GingerString FromTavern(string value)
		{
			StringBuilder sb = new StringBuilder(value);
			sb.Replace("<bot>", CharacterMarker, true);
			sb.Replace("<user>", UserMarker, true);
			sb.Replace(TavernCharacterMarker, CharacterMarker, true);
			sb.Replace(TavernUserMarker, UserMarker, true);
			sb.Replace(TavernOriginalMarker, OriginalMarker, true);
			sb.Replace(BackyardCharacterMarker, CharacterMarker, true);
			sb.Replace(CharacterMarker, CharacterMarker, true);
			sb.Replace(UserMarker, UserMarker, true);
			sb.ConvertLinebreaks(Linebreak.Default);
			NormalizeSpecialChars.Normalize(sb);
			sb.Trim();
			return new GingerString(sb.ToString());
		}

		public string ToTavern()
		{
			StringBuilder sb = new StringBuilder(value);
			sb.Replace(CharacterMarker, TavernCharacterMarker, true);
			sb.Replace(UserMarker, TavernUserMarker, true);
			sb.Replace(OriginalMarker, TavernOriginalMarker, true);
			sb.Replace(NameMarker, Current.Name, true);
			sb.Replace(ContinueMarker, "", true);
			sb.ConvertLinebreaks(Linebreak.LF);
			sb.TrimTrailingSpaces();
			return sb.ToString();
		}

		public static GingerString FromTavernChat(string value)
		{
			StringBuilder sb = new StringBuilder(FromTavern(value).ToString());
			sb.Replace("<START>", "__CHAT__", true);
			sb.Replace("END_OF_DIALOG", "__CHAT__", true);
			sb.Replace("\r\n\r\n", "__CHAT__", true);

			// Identify chat separators. Tavern cards can get creative with these
			var lines = sb.ToString().Split(new string[] { "\r\n" }, StringSplitOptions.None);
			for (int i = 0; i < lines.Length; ++i)
			{
				string line = lines[i];
				if (line.Length >= 3 && line.Length <= 16 && char.IsPunctuation(line[0]))
				{
					char ch = line[0];
					bool bSeparator = true;
					for (int j = 1; j < line.Length; ++j)
					{
						if (line[j] != ch)
						{
							bSeparator = false;
							break;
						}
					}
					if (bSeparator)
						lines[i] = "__CHAT__";
				}
			}
			
			var paragraphs = string.Join("\r\n", lines)
				.Split(new string[] { "__CHAT__" }, StringSplitOptions.RemoveEmptyEntries);

			StringBuilder sbChat = new StringBuilder();
			foreach (var paragraph in paragraphs)
			{
				if (string.IsNullOrWhiteSpace(paragraph) == false)
				{
					sbChat.AppendLine(paragraph.Trim());
					sbChat.AppendLine();
				}
			}
			sbChat.TrimEnd();

			return new GingerString(sbChat.ToString());
		}

		public string ToTavernChat()
		{
			// Split by paragraph
			string text = ToTavern();
			var paragraphs = text.Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0);
			StringBuilder sb = new StringBuilder();
			foreach (var paragraph in paragraphs)
			{
				sb.AppendLine("<START>");
				sb.AppendLine(paragraph);
			}
			sb.TrimEnd();
			sb.ConvertLinebreaks(Linebreak.LF);
			return sb.ToString();
		}

		public static GingerString FromFaraday(string value)
		{
			StringBuilder sb = new StringBuilder(value);
			sb.Replace("<bot>", CharacterMarker, true);
			sb.Replace("<user>", UserMarker, true);
			sb.Replace("#{character}", CharacterMarker, true);
			sb.Replace("#{user}", UserMarker, true);
			sb.Replace(TavernCharacterMarker, CharacterMarker, true);
			sb.Replace(TavernUserMarker, UserMarker, true);
			sb.Replace(TavernOriginalMarker, OriginalMarker, true);
			sb.Replace(BackyardCharacterMarker, CharacterMarker, true);
			sb.Replace(BackyardUserMarker, UserMarker, true);
			sb.ConvertLinebreaks(Linebreak.Default);
			sb.Trim();
			return new GingerString(sb.ToString());
		}

		public string ToFaraday()
		{
			StringBuilder sb = new StringBuilder(value);

			var names = new HashSet<string>(
				Current.Characters.Select(c => c.spokenName.ToLowerInvariant())
				.Where(s => string.IsNullOrWhiteSpace(s) == false));
			
			sb.Replace(CharacterMarker, BackyardCharacterMarker, true);
			sb.Replace(UserMarker, BackyardUserMarker, true);
			sb.Replace(OriginalMarker, BackyardOriginalMarker, true);
			sb.Replace(NameMarker, Current.Name, true);
			sb.Replace(ContinueMarker, "", true);

			sb.ConvertLinebreaks(Linebreak.LF);
			sb.Trim();
			sb.TrimTrailingSpaces();
			return sb.ToString();
		}

		public string ToFaradayGreeting()
		{
			string text = ToFaraday();

			// Trim lines
			text = string.Join("\n", text.Split(new char[] { '\n' }, StringSplitOptions.None).Select(s => s.Trim()));
			
			// Split paragraphs
			var paragraphs = text.Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.Where(s => s.Length > 0);

			// Join paragraphs
			return string.Join("\n", paragraphs);
		}

		public string ToFaradayChat()
		{
			string text = ToFaraday();

			var names = new HashSet<string>(
				Current.Characters.Select(c => c.spokenName.ToLowerInvariant())
				.Where(s => string.IsNullOrWhiteSpace(s) == false));

			var paragraphs = text.Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
			for (int i = 0; i < paragraphs.Length; ++i)
			{
				// {char}: -> #{char}:
				string[] lines = paragraphs[i].Split(new char[] { '\n' }, StringSplitOptions.None);
				for (int line = 0; line < lines.Length; ++line)
				{
					if (lines[line].BeginsWith("{character}:", false) 
						|| lines[line].BeginsWith("{user}:", false)
						|| lines[line].BeginsWith("{character}\uFF1A", false)
						|| lines[line].BeginsWith("{user}\uFF1A", false))
						lines[line] = string.Concat("#", lines[line]);
					else
					{
						int pos_colon = Utility.ScopedIndexOf(lines[line], 0, ':', '\"', '\"');
						int pos_colon_full_width = Utility.ScopedIndexOf(lines[line], 0, '\uFF1A', '\"', '\"'); // full width colon
						if (pos_colon_full_width != -1 && (pos_colon == -1 || pos_colon_full_width < pos_colon))
							pos_colon = pos_colon_full_width;
						if (pos_colon != -1)
						{
							string name = lines[line].Substring(0, pos_colon).Trim().ToLowerInvariant();
							if (names.Contains(name))
								lines[line] = string.Concat("#", lines[line]);
						}
					}
				}
				paragraphs[i] = string.Join("\n", lines);
			}
			return string.Join("\n\n", paragraphs);
		}

		public static GingerString FromParameter(string value)
		{
			StringBuilder sb = new StringBuilder(value);
			sb.ConvertLinebreaks(Linebreak.LF);
			sb.Replace("\n", Text.Break);

			// Back tick
			int pos_tick = sb.IndexOf('`', 0);
			while (pos_tick != -1)
			{
				int pos_end = sb.IndexOf('`', pos_tick + 1);
				if (pos_end == -1)
					break;
				if (pos_end - pos_tick > 1)
				{
					sb.Insert(pos_end, "<!np->");
					sb.Insert(pos_tick + 1, "<!np+>");
					pos_tick = sb.IndexOf('`', pos_end + 13);
					continue;
				}
				pos_tick = sb.IndexOf('`', pos_tick + 2);
			}

			// Remove comments
			RemoveComments(sb);

			// Replace names
			Utility.ReplaceWholeWord(sb, CharacterMarker, "__CCCC__", StringComparison.OrdinalIgnoreCase);
			Utility.ReplaceWholeWord(sb, UserMarker, "__UUUU__", StringComparison.OrdinalIgnoreCase);
			Utility.ReplaceWholeWord(sb, OriginalMarker, "__OOOO__", StringComparison.OrdinalIgnoreCase);
			Utility.ReplaceWholeWord(sb, NameMarker, "__NNNN__", StringComparison.OrdinalIgnoreCase);
			Utility.ReplaceWholeWord(sb, ContinueMarker, "", StringComparison.OrdinalIgnoreCase);

			if (AppSettings.Settings.AutoConvertNames)
			{
				string userPlaceholder = (Current.Card.userPlaceholder ?? "").Trim();
				if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
					Utility.ReplaceWholeWord(sb, userPlaceholder, "__UUUU__", StringComparison.Ordinal);

				for (int i = 0; i < Current.Characters.Count; ++i)
				{
					string characterPlaceholder = (Current.Characters[i].namePlaceholder ?? "").Trim();
					if (string.IsNullOrWhiteSpace(characterPlaceholder) == false)
						Utility.ReplaceWholeWord(sb, characterPlaceholder, MakeInternalCharacterMarker(i), StringComparison.Ordinal);
				}
			}

			sb.Replace("__CCCC__", InternalCharacterMarker);
			sb.Replace("__UUUU__", InternalUserMarker);
			sb.Replace("__OOOO__", InternalOriginalMarker);
			sb.Replace("__NNNN__", InternalNameMarker);

			return FromString(sb.ToString());
		}
		
		public static string RemoveComments(string text)
		{
			StringBuilder sb = new StringBuilder(text);
			RemoveComments(sb);
			return sb.ToString();
		}

		public static void RemoveComments(StringBuilder sb)
		{
			// Remove comments
			int pos_comment = sb.IndexOf("/*", 0);
			while (pos_comment != -1)
			{
				int pos_comment_end = Utility.ScopedIndexOf(sb, "*/", pos_comment, "/*", "*/", -1, '\0');
				if (pos_comment_end == -1)
					break;

				sb.Remove(pos_comment, pos_comment_end - pos_comment + 2);
				pos_comment = sb.IndexOf("/*", pos_comment);
			}

			// Remove comments (Html)
			pos_comment = sb.IndexOf("<!--", 0);
			while (pos_comment != -1)
			{
				int pos_comment_end = sb.IndexOf("-->", pos_comment + 4);
				if (pos_comment_end == -1)
					break;

				sb.Remove(pos_comment, pos_comment_end - pos_comment + 3);
				pos_comment = sb.IndexOf("<!--", pos_comment);
			}
		}

		public static GingerString Join(string separator, IEnumerable<GingerString> texts)
		{
			return FromString(Text.Process(string.Join(separator, texts.Select(g => g.ToString())), Text.EvalOption.Minimal));
		}

		public string ToParameter()
		{
			StringBuilder sb = new StringBuilder(value);

			sb.Replace(InternalCharacterMarker, CharacterMarker);
			sb.Replace(InternalUserMarker, UserMarker);
			sb.Replace(InternalOriginalMarker, OriginalMarker);
			sb.Replace(InternalNameMarker, NameMarker);
			sb.Replace(InternalContinueMarker, ContinueMarker);

			return sb.TrimStart().ToString();
		}

		public string WithNames(string[] characterNames, string userName)
		{
			return WithNames(value, characterNames, userName);
		}

		public static string WithNames(string value, string[] characterNames, string userName)
		{
			StringBuilder sb = new StringBuilder(value);
			for (int i = 0; i < characterNames.Length; ++i)
			{
				string marker = MakeInternalCharacterMarker(i);
				sb.Replace(marker, characterNames[i] ?? marker);
			}

			if (AppSettings.Settings.AutoConvertNames && characterNames.Length > 0)
				sb.Replace(CharacterMarker, characterNames[0] ?? CharacterMarker, true);
			if (AppSettings.Settings.AutoConvertNames)
				sb.Replace(UserMarker, userName ?? UserMarker, true);

			return sb.ToString();
		}

		public string ToBaked()
		{
			string[] characterNames = Current.Characters.Select(c => c.namePlaceholder ?? "").ToArray();
			string userPlaceholder = Current.Card.userPlaceholder;

			return WithNames(ToParameter(), characterNames, userPlaceholder);
		}
		
		public static GingerString FromString(string value)
		{
			return new GingerString() { value = value };
		}

		public static GingerString FromOutput(string value, int characterIndex, Generator.Option options, Text.EvalOption evalOption = Text.EvalOption.Minimal)
		{
			if (value == null)
				return new GingerString(value);

			var sb = new StringBuilder(value);

			string[] characterNames = Current.Characters.Select(c => c.spokenName).ToArray();
			bool bUseCharacterPlaceholder = (characterIndex == 0 && Current.Characters.Count == 1) || options.Contains(Generator.Option.Single);
			bool bGroup = options.Contains(Generator.Option.Group);

			if (bGroup)
			{
				sb.Replace(InternalCharacterMarker, MakeInternalCharacterMarker(characterIndex));
			}
			else
			{
				if (bUseCharacterPlaceholder)
				{
					sb.Replace(InternalCharacterMarker, CharacterMarker);
					sb.Replace(MakeInternalCharacterMarker(characterIndex), CharacterMarker);
				}
				else if (characterIndex >= 0 && characterIndex < characterNames.Length)
				{
					sb.Replace(MakeInternalCharacterMarker(0), CharacterMarker);
					sb.Replace(InternalCharacterMarker, characterNames[characterIndex]);
				}
				else
					sb.Replace(InternalCharacterMarker, CharacterMarker); // Error
			}

			sb.Replace(InternalUserMarker, UserMarker);
			sb.Replace(InternalOriginalMarker, OriginalMarker);
			sb.Replace(InternalNameMarker, NameMarker);
			sb.Replace(InternalContinueMarker, ContinueMarker);

			if (!bGroup)
			{
				for (int i = 0; i < characterNames.Length; ++i)
					sb.Replace(MakeInternalCharacterMarker(i), characterNames[i]);
			}

			string text = Text.Process(sb.ToString(), evalOption)
				.ConvertLinebreaks(Linebreak.LF);
			return new GingerString(text);
		}

		public string ToGinger()
		{
			if (value == null)
				return null;

			StringBuilder sb = new StringBuilder(value);

			sb.Replace(CharacterMarker, CharacterMarker, true); // Case insensitive
			sb.Replace(UserMarker, UserMarker, true); // Case insensitive
			sb.Replace(OriginalMarker, OriginalMarker, true); // Case insensitive
			sb.Replace(NameMarker, NameMarker, true); // Case insensitive

			sb.Trim();
			sb.ConvertLinebreaks(Linebreak.Default);

			return sb.ToString();
		}

		public string ToOutputPreview(Recipe.Component channel = Recipe.Component.Invalid)
		{
			if (value == null)
				return null;

			StringBuilder sb;

			switch (AppSettings.Settings.PreviewFormat)
			{
			default:
			case AppSettings.Settings.OutputPreviewFormat.Default:
				return ToGinger();
			case AppSettings.Settings.OutputPreviewFormat.SillyTavern:
				if (channel == Recipe.Component.Example)
					sb = new StringBuilder(ToTavernChat());
				else
					sb = new StringBuilder(ToTavern());
				break;
			case AppSettings.Settings.OutputPreviewFormat.Faraday:
				if (channel == Recipe.Component.Example)
					sb = new StringBuilder(ToFaradayChat());
				else
					sb = new StringBuilder(ToFaraday());
				break;
			case AppSettings.Settings.OutputPreviewFormat.FaradayParty:
				sb = new StringBuilder(ToFaraday());
				string[] characterNames = Current.Characters.Select(c => c.spokenName).ToArray();
				for (int i = 0; i < characterNames.Length; ++i)
					sb.Replace(MakeInternalCharacterMarker(i), characterNames[i]);
				break;
			case AppSettings.Settings.OutputPreviewFormat.PlainText:
				sb = new StringBuilder(ToGinger());
				sb.Replace(CharacterMarker, Current.Name, true);
				sb.Replace(UserMarker, Current.Card.userPlaceholder, true);
				sb.Replace(OriginalMarker, "", true);
				sb.Replace(ContinueMarker, "", true);
				sb.Replace(NameMarker, Current.Name, true);
				break;
			}
			
			sb.Trim();
			sb.ConvertLinebreaks(Linebreak.CRLF);
			return sb.ToString();
		}

		public static string BakeNames(string value, int characterIndex)
		{
			string text = FromOutput(value, characterIndex, Generator.Option.None).ToString();

			var sb = new StringBuilder(text);

			string userPlaceholder = Current.Card.userPlaceholder;
			if (string.IsNullOrEmpty(userPlaceholder) == false)
				sb.Replace(UserMarker, userPlaceholder, false);

			if (characterIndex >= 0 && characterIndex < Current.Characters.Count)
			{
				string characterPlaceholder = Current.Characters[characterIndex].namePlaceholder;
				if (string.IsNullOrEmpty(characterPlaceholder) == false)
					sb.Replace(CharacterMarker, characterPlaceholder, false);
			}
			else
			{
				string characterPlaceholder = Current.MainCharacter.namePlaceholder;
				if (string.IsNullOrEmpty(characterPlaceholder) == false)
					sb.Replace(CharacterMarker, characterPlaceholder, false);
			}

			return sb.ToString();
		}

		public override string ToString()
		{
			return value;
		}

		public override int GetHashCode()
		{
			if (value == null)
				return 0;
			return value.GetHashCode();
		}

		private struct Replacement
		{
			public Replacement(Func<Context, ContextString.EvaluationConfig, string> fn, params string[] matches)
			{
				this.fn = fn;
				this.matches = matches;
			}
			public string[] matches;
			public Func<Context, ContextString.EvaluationConfig, string> fn;
		}

		private static Replacement[] replacements = new Replacement[]
		{
			new Replacement((ctx, cfg) => NameMarker, "{name}"),
			new Replacement((ctx, cfg) => UserMarker, "{#name}"),
			new Replacement((ctx, cfg) => Text.Eval("[card]", ctx, cfg, Text.EvalOption.None), "{card}"),
			new Replacement((ctx, cfg) => Text.Eval("[gender]", ctx, cfg, Text.EvalOption.None), "{gender}"),
			new Replacement((ctx, cfg) => Text.Eval("[#gender]", ctx, cfg, Text.EvalOption.None), "{#gender}"),
			new Replacement((ctx, cfg) => "", "{unknown}"),

			// Character grammar macros
			new Replacement((ctx, cfg) => Text.Eval("[they]", ctx, cfg, Text.EvalOption.None), "{they}", "{he}", "{she}"),
			new Replacement((ctx, cfg) => Text.Eval("[they've]", ctx, cfg, Text.EvalOption.None), "{they've}"),
			new Replacement((ctx, cfg) => Text.Eval("[they'd]", ctx, cfg, Text.EvalOption.None), "{they'd}", "{he'd}", "{she'd}"),
			new Replacement((ctx, cfg) => Text.Eval("[they'll]", ctx, cfg, Text.EvalOption.None), "{they'll}", "{he'll}", "{she'll}"),
			new Replacement((ctx, cfg) => Text.Eval("[they're]", ctx, cfg, Text.EvalOption.None), "{they're}", "{he's}", "{she's}"),
			new Replacement((ctx, cfg) => Text.Eval("[them]", ctx, cfg, Text.EvalOption.None), "{them}", "{him}"),
			new Replacement((ctx, cfg) => Text.Eval("[their]", ctx, cfg, Text.EvalOption.None), "{their}", "{his}", "{her}"),
			new Replacement((ctx, cfg) => Text.Eval("[theirs]", ctx, cfg, Text.EvalOption.None), "{theirs}", "{hers}"),
			new Replacement((ctx, cfg) => Text.Eval("[themselves]", ctx, cfg, Text.EvalOption.None), "{themselves}", "{himself}", "{herself}"),
			new Replacement((ctx, cfg) => Text.Eval("[is]", ctx, cfg, Text.EvalOption.None), "{is}", "{are}"),
			new Replacement((ctx, cfg) => Text.Eval("[isn't]", ctx, cfg, Text.EvalOption.None), "{isn't}", "{aren't}"),
			new Replacement((ctx, cfg) => Text.Eval("[has]", ctx, cfg, Text.EvalOption.None), "{has}", "{have}"),
			new Replacement((ctx, cfg) => Text.Eval("[hasn't]", ctx, cfg, Text.EvalOption.None), "{hasn't}", "{haven't}"),
			new Replacement((ctx, cfg) => Text.Eval("[was]", ctx, cfg, Text.EvalOption.None), "{was}", "{were}"),
			new Replacement((ctx, cfg) => Text.Eval("[wasn't]", ctx, cfg, Text.EvalOption.None), "{wasn't}", "{weren't}"),
			new Replacement((ctx, cfg) => Text.Eval("[do]", ctx, cfg, Text.EvalOption.None), "{does}", "{do}"),
			new Replacement((ctx, cfg) => Text.Eval("[don't]", ctx, cfg, Text.EvalOption.None), "{doesn't}", "{don't}"),
			new Replacement((ctx, cfg) => Text.Eval("[s]", ctx, cfg, Text.EvalOption.None), "{s}"),
			new Replacement((ctx, cfg) => Text.Eval("[es]", ctx, cfg, Text.EvalOption.None), "{es}"),
			new Replacement((ctx, cfg) => Text.Eval("[ies]", ctx, cfg, Text.EvalOption.None), "{y}", "{ies}"),
			
			// User grammar macros
			new Replacement((ctx, cfg) => Text.Eval("[#they]", ctx, cfg, Text.EvalOption.None), "{#they}", "{#he}", "{#she}"),
			new Replacement((ctx, cfg) => Text.Eval("[#they've]", ctx, cfg, Text.EvalOption.None), "{#they've}"),
			new Replacement((ctx, cfg) => Text.Eval("[#they'd]", ctx, cfg, Text.EvalOption.None), "{#they'd}", "{#he'd}", "{#she'd}"),
			new Replacement((ctx, cfg) => Text.Eval("[#they'll]", ctx, cfg, Text.EvalOption.None), "{#they'll}", "{#he'll}", "{#she'll}"),
			new Replacement((ctx, cfg) => Text.Eval("[#they're]", ctx, cfg, Text.EvalOption.None), "{#they're}", "{#he's}", "{#she's}"),
			new Replacement((ctx, cfg) => Text.Eval("[#them]", ctx, cfg, Text.EvalOption.None), "{#them}", "{#him}"),
			new Replacement((ctx, cfg) => Text.Eval("[#their]", ctx, cfg, Text.EvalOption.None), "{#their}", "{#his}", "{#her}"),
			new Replacement((ctx, cfg) => Text.Eval("[#theirs]", ctx, cfg, Text.EvalOption.None), "{#theirs}", "{#hers}"),
			new Replacement((ctx, cfg) => Text.Eval("[#themselves]", ctx, cfg, Text.EvalOption.None), "{#themselves}", "{#himself}", "{#herself}"),
			new Replacement((ctx, cfg) => Text.Eval("[#is]", ctx, cfg, Text.EvalOption.None), "{#is}", "{#are}"),
			new Replacement((ctx, cfg) => Text.Eval("[#isn't]", ctx, cfg, Text.EvalOption.None), "{#isn't}", "{#aren't}"),
			new Replacement((ctx, cfg) => Text.Eval("[#has]", ctx, cfg, Text.EvalOption.None), "{#has}", "{#have}"),
			new Replacement((ctx, cfg) => Text.Eval("[#hasn't]", ctx, cfg, Text.EvalOption.None), "{#hasn't}", "{#haven't}"),
			new Replacement((ctx, cfg) => Text.Eval("[#was]", ctx, cfg, Text.EvalOption.None), "{#was}", "{#were}"),
			new Replacement((ctx, cfg) => Text.Eval("[#wasn't]", ctx, cfg, Text.EvalOption.None), "{#wasn't}", "{#weren't}"),
			new Replacement((ctx, cfg) => Text.Eval("[#do]", ctx, cfg, Text.EvalOption.None), "{#does}", "{#do}"),
			new Replacement((ctx, cfg) => Text.Eval("[#don't]", ctx, cfg, Text.EvalOption.None), "{#doesn't}", "{#don't}"),
			new Replacement((ctx, cfg) => Text.Eval("[#s]", ctx, cfg, Text.EvalOption.None), "{#s}"),
			new Replacement((ctx, cfg) => Text.Eval("[#es]", ctx, cfg, Text.EvalOption.None), "{#es}"),
			new Replacement((ctx, cfg) => Text.Eval("[#ies]", ctx, cfg, Text.EvalOption.None), "{#y}", "{#ies}"),
		};

		public static string EvaluateParameter(string text, Context context)
		{
			var evalConfig = new ContextString.EvaluationConfig() {
				macroSuppliers = new IMacroSupplier[] { Current.Strings },
				referenceSuppliers = new IStringReferenceSupplier[] { Current.Strings },
				ruleSuppliers = new IRuleSupplier[] { Current.Strings },
			};

			return EvaluateParameter(text, context, evalConfig);
		}

		public static string EvaluateParameter(string text, Context context, ContextString.EvaluationConfig evalConfig)
		{
			if (text == null)
				return null;

			if (context.HasFlag("__snippet"))
				return text; // Don't evaluate if we're generating a snippet

			if (text.Contains('{') == false)
				return text; // No possible replacement

			// Replace variables
			StringBuilder sb = new StringBuilder(text);
			var pos_var = sb.IndexOf("{$", 0);
			while (pos_var != -1)
			{
				int pos_var_end = sb.IndexOfAny(new char[] { '}', ' ', '\r', '\n', '\t' }, pos_var + 2);
				if (pos_var_end == -1 || char.IsWhiteSpace(sb[pos_var_end]))
					break;

				string varName = sb.Substring(pos_var + 2, pos_var_end - pos_var - 2).ToLowerInvariant();
				string varValue;

				if (Current.Card.TryGetVariable(varName, out varValue))
				{
					sb.Remove(pos_var, pos_var_end - pos_var + 1);
					sb.Insert(pos_var, varValue ?? "");
					pos_var = sb.IndexOf("{$", pos_var + (varValue ?? "").Length);
				}
				else
				{
					sb.Remove(pos_var, pos_var_end - pos_var + 1);
					pos_var = sb.IndexOf("{$", pos_var);
				}
			}

			// Find possible replacements
			string findText = sb.ToString().ToLowerInvariant();
			List<Replacement> foundReplacements = new List<Replacement>(4);
			for (int i = 0; i < replacements.Length; ++i)
			{
				foreach (var match in replacements[i].matches)
				{
					if (findText.Contains(match))
					{
						foundReplacements.Add(replacements[i]);
						break;
					}
				}
			}

			if (foundReplacements.Count == 0)
				return sb.ToString();

			// Apply replacements
			for (int i = 0; i < foundReplacements.Count; ++i)
			{
				string evaluatedText = foundReplacements[i].fn.Invoke(context, evalConfig);
				foreach (var match in foundReplacements[i].matches)
					sb.Replace(match, evaluatedText, true);
			}
			return sb.ToString();
		}

		public static string FromCode(string text)
		{
			var sb = new StringBuilder(text);
			sb.Replace("\t", Text.Tab);
			sb.Insert(0, "<!np+>");
			sb.Insert(sb.Length, "<!np->");
			return sb.ToString();
		}

		public static string Escape(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text;

			StringBuilder sb = new StringBuilder(text);
			Escape(sb);
			text = sb.ToString();
			return text;
		}

		private static HashSet<string> _commands = new HashSet<string>() {
			"{char}", "{user}", "{card}", "{name}", "{gender}", "{unknown}", "{they}", "{he}", "{she}", "{they've}", "{they'd}", "{he'd}", "{she'd}", "{they'll}", "{he'll}", "{she'll}", "{they're}", "{he's}", "{she's}", "{them}", "{him}", "{their}", "{his}", "{her}", "{theirs}", "{hers}", "{themselves}", "{himself}", "{herself}", "{is}", "{are}", "{isn't}", "{aren't}", "{has}", "{have}", "{hasn't}", "{haven't}", "{was}", "{were}", "{wasn't}", "{weren't}", "{does}", "{do}", "{doesn't}", "{don't}", "{s}", "{es}", "{y}", "{ies}", "{#name}", "{#gender}", "{#they}", "{#he}", "{#she}", "{#they've}", "{#they'd}", "{#he'd}", "{#she'd}", "{#they'll}", "{#he'll}", "{#she'll}", "{#they're}", "{#he's}", "{#she's}", "{#them}", "{#him}", "{#their}", "{#his}", "{#hers}", "{#theirs}", "{#hers}", "{#themselves}", "{#himself}", "{#herself}", "{#is}", "{#are}", "{#isn't}", "{#aren't}", "{#has}", "{#have}", "{#hasn't}", "{#haven't}", "{#was}", "{#were}", "{#wasn't}", "{#weren't}", "{#does}", "{#do}", "{#doesn't}", "{#don't}", "{#s}", "{#es}", "{#y}", "{#ies}",
		};

		public static void Escape(StringBuilder sb)
		{
			// Escape curly brackets (except for macros)
			int pos = sb.IndexOfAny(new char[] { '{', '}' }, 0);
			while (pos != -1)
			{
				char ch = sb[pos];
				int pos_end = -1;
				if (ch == '{')
					pos_end = Utility.FindEndOfScope(sb, pos, '{', '}');
				
				if (pos_end == -1)
				{
					sb.Insert(pos, ch);
					pos = sb.IndexOfAny(new char[] { '{', '}' }, pos + 2);
					continue;
				}

				string word = sb.SubstringToFrom(pos, pos_end).ToLowerInvariant();
				if (_commands.Contains(word))
				{
					// Don't escape
					pos = sb.IndexOfAny(new char[] { '{', '}' }, pos_end + 1);
					continue;
				}

				// Escape
				sb.Insert(pos_end, '}');
				sb.Insert(pos, '{');
				pos = sb.IndexOfAny(new char[] { '{', '}' }, pos_end + 3);
			}

			sb.Replace("[", "[[");
			sb.Replace("]", "]]");
		}

		public static string Unescape(string text)
		{
			StringBuilder sb = new StringBuilder(text);
			Unescape(sb);
			text = sb.ToString();
			return text;
		}

		public static void Unescape(StringBuilder sb)
		{
			sb.Replace("{{", "{");
			sb.Replace("}}", "}");
			sb.Replace("[[", "[");
			sb.Replace("]]", "]");
		}

		public static GingerString Empty
		{
			get { return new GingerString(null); }
		}

		public bool IsNullOrEmpty()
		{
			return string.IsNullOrWhiteSpace(value);
		}

		public GingerString ApplyTextStyle(CardData.TextStyle style)
		{
			if (style != CardData.TextStyle.None)
				value = TextStyleConverter.Convert(value, style);
			return this;
		}

		public GingerString ApplyStandardTextStyle()
		{
			if (string.IsNullOrEmpty(value) == false)
				value = TextStyleConverter.Convert(value, CardData.TextStyle.Mixed);
			return this;
		}

	}

}
