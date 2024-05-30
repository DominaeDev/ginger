using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NHunspell;

namespace Ginger
{
	public static class SpellChecker
	{
		public static bool IsInitialized { get { return s_Hunspell != null; } }
		private static Hunspell s_Hunspell;
		private static string s_CurrentLanguage;

		private static HashSet<string> s_Whitelist = new HashSet<string>(); // Whitelist
		private static HashSet<string> s_UserList = new HashSet<string>(); // User word list (gets saved)

		public static bool Initialize(string language)
		{
			if (s_Hunspell != null)
				Release();

			try
			{
				s_Hunspell = new Hunspell(
					Utility.AppPath("Dictionaries", string.Concat(language, ".aff")), 
					Utility.AppPath("Dictionaries", string.Concat(language, ".dic")));

				s_CurrentLanguage = language;
				LoadWhitelist();
				return true;
			}
			catch
			{
				s_CurrentLanguage = null;
				s_Hunspell = null;
				return false;
			}
		}

		public static void Release()
		{
			if (s_Hunspell != null)
			{
				s_Hunspell.Dispose();
				s_Hunspell = null;
				s_CurrentLanguage = null;
			}
		}
		
		public static void SpellCheck(TextSpans textSpans)
		{
			if (textSpans == null || textSpans.spans == null || textSpans.spans.Length == 0)
				return;

			SpellCheck(textSpans.spans);
		}

		public static void SpellCheck(IEnumerable<TextSpan> textSpans)
		{
			if (textSpans == null || IsInitialized == false)
				return;

			foreach (var textSpan in textSpans)
			{
				for (int j = 0; j < textSpan.words.Length; ++j)
				{
					string word = textSpan.GetWord(j);
					if (CheckWhitelist(word, s_Whitelist))
						continue;

					if (s_Hunspell.Spell(word) == false)
					{
						textSpan.words[j] = new TextSpan.Word() {
							start = textSpan.words[j].start,
							length = textSpan.words[j].length,
							tag = TextSpan.Word.Tag.Misspelled,
						};
					}
				}
			}
		}

		private static bool CheckWhitelist(string word, HashSet<string> whitelist)
		{
			if (word.Length == 0)
				return true;
			if (word.ContainsAny(c => char.IsDigit(c))) // Skip numbers
				return true;
			if (word.Length > 2 && word[0] == '{' && word[word.Length - 1] == '}') // Skip commands
				return true;

			// Check whitelist
			if (whitelist != null && whitelist.Contains(word.ToLowerInvariant()))
				return true;

			return false;
		}

		public static List<string> Suggest(string word)
		{
			if (string.IsNullOrEmpty(word) || IsInitialized == false)
				return null;

			return s_Hunspell.Suggest(word);
		}

		public static void AddToDictionary(string word)
		{
			if (string.IsNullOrWhiteSpace(word) || IsInitialized == false)
				return;

			word = word.Trim().ToLowerInvariant();
			if (s_Whitelist.Contains(word))
				return;

			s_Whitelist.Add(word);
			s_UserList.Add(word);

			SaveWhitelist();
		}

		private static void LoadWhitelist()
		{
			s_UserList.Clear();

			string filename = Utility.AppPath("Dictionaries", string.Concat(s_CurrentLanguage, ".user.dic"));

			if (File.Exists(filename))
			{
				try
				{
					using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						using (StreamReader reader = new StreamReader(fs, Encoding.UTF8))
						{
							string line;
							while ((line = reader.ReadLine()) != null)
							{
								string word = line.Trim().ToLowerInvariant();
								if (word.Length > 0)
									s_UserList.Add(word);
							}
						}
					}
				}
				catch
				{
				}
			}

			s_Whitelist = new HashSet<string>(s_InternalTerms.Union(s_UserList));
		}

		private static void SaveWhitelist()
		{
			// Save
			if (s_UserList.Count == 0 || IsInitialized == false)
				return;

			string filename = Utility.AppPath("Dictionaries", string.Concat(s_CurrentLanguage, ".user.dic"));


			try
			{
				using (FileStream fs = File.Open(filename, FileMode.Create, FileAccess.Write))
				{
					using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
					{
						foreach (var word in s_UserList.OrderBy(w => w))
							writer.WriteLine(word);
					}
				}
			}
			catch
			{
			}
		}

		private static readonly string[] s_InternalTerms = {
			// Ginger commands
			"char", "user", "card", "name", "original", "gender", "they'll", "they're", "they've", "they'd", "they", "them", "theirs",
			"their", "themselves", "he'll", "he's", "he's", "he'd", "he", "him", "his", "his", "himself", "she'll", "she's", "she's",
			"she'd", "she", "her", "hers", "her", "herself", "is", "are", "isn't", "aren't", "has", "have", "hasn't", "haven't",
			"was", "were", "wasn't", "weren't", "does", "do", "doesn't", "don't", "s", "y", "ies", "es", 
			// Characteristics
			"futa", "futanari", "shemale", "trans", "transgender", "transsexual", "trans-gender", "trans-sexual",
			"nonbinary", "non-binary",
		};
	}
}
