using IniParser;
using IniParser.Model;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;

namespace Ginger
{
	public static class AppSettings
	{
		public static class Settings
		{
			public static bool AutoConvertNames = true;
			public static string UserPlaceholder = "User";
			public static bool ShowNSFW = false;
			public static bool ConfirmNSFW = true;
			public static int UndoSteps = 80;
			public static int TokenBudget = 0;
			public static bool AutoBreakLine = true;
			public static string Locale = Locales.DefaultLocale;
			public static bool EnableFormLevelBuffering = true;

			public static string FontFace = null;
			private static Font _font = new Font(Constants.DefaultFontFace, Constants.DefaultFontSize, FontStyle.Regular, GraphicsUnit.Point, 0);
			public static Font Font
			{
				get { return _font; }
				set { _font = value; }
			}

			public static string FontSerialize
			{
				get { return FontFace; }
				set 
				{ 
					FontFace = value;
					if (string.IsNullOrEmpty(FontFace) == false)
						_font = new Font(FontFace, Constants.DefaultFontSize, FontStyle.Regular, GraphicsUnit.Point, 0);
					else
						_font = new Font(Constants.DefaultFontFace, Constants.DefaultFontSize, FontStyle.Regular, GraphicsUnit.Point, 0);
				}
			}

			public enum OutputPreviewFormat
			{
				Default,
				SillyTavern,
				Faraday,
				PlainText, // Rendered
			}
			public static OutputPreviewFormat PreviewFormat = OutputPreviewFormat.Default;

			public static bool SpellChecking = true;
			public static string Dictionary = "en_US";

			public static bool ShowRecipeCategory = false;
		}

		public static class User
		{
			public static int LastImportCharacterFilter = 0;
			public static int LastImportLorebookFilter = 0;

			public static int LastExportCharacterFilter = 0;
			public static int LastExportLorebookFilter = 0;

			public static bool LaunchTextEditor = true;

			public static string FindMatch = "";
			public static bool FindMatchCase = false;
			public static bool FindWholeWords = false;
			public static Point FindLocation = default(Point);

			public static string ReplaceLastFind = "";
			public static string ReplaceLastReplace = "";
			public static bool ReplaceMatchCase = false;
			public static bool ReplaceWholeWords = false;
			public static bool ReplaceLorebooks = true;

			public static bool SnippetSwapPronouns = false;
		}
		
		public static class Paths
		{
			public static string LastCharacterPath = null;
			public static string LastImagePath = null;
			public static string LastImportPath = null;
		}

		public static class WriteDialog
		{
			public static bool WordWrap = true;
			public static bool Highlight = true;
			public static bool HighlightNames = true;
			public static bool HighlightNumbers = true;
			public static bool HighlightPronouns = false;
			public static bool AutoBreakLine = true;
			public static Point WindowSize = new Point(820, 660);
			public static Point WindowLocation = default(Point);
			public static bool DarkMode = false;

			private static string DefaultFontFace = "Segoe UI";
			private static Font _font = new Font(DefaultFontFace, 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
			public static Font Font
			{
				get { return _font; }
				set { _font = value; }
			}

			public static string FontSerialize
			{
				get { return TypeDescriptor.GetConverter(typeof(Font)).ConvertToInvariantString(_font); }
				set 
				{ 
					var font = TypeDescriptor.GetConverter(typeof(Font)).ConvertFromInvariantString(value) as Font;
					if (font != null)
						_font = font;
					else
						_font = new Font(DefaultFontFace, 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
				}
			}
		}

		public static class Faraday
		{
			public static int RepeatPenaltyTokens = 256;
			public static decimal RepeatPenalty = 1.05m;
			public static decimal Temperature = 1.2m;
			public static int TopK = 30;
			public static decimal MinP = 0.1m;
			public static bool MinPEnabled = true;
			public static decimal TopP = 0.9m;
			public static int PromptTemplate = 0;
			public static bool PruneExampleChat = true;
		}

		public static class FileFormat
		{
			public static bool EnableBackyardAI = true;
			public static bool EnableCCV2 = true;
			public static bool EnableCCV3 = true;
		}

		public static bool LoadFromIni(string filePath)
		{
			// Load
			var parser = new FileIniDataParser();
			parser.Parser.Configuration.CommentString = "#";
			parser.Parser.Configuration.SkipInvalidLines = true;
			parser.Parser.Configuration.OverrideDuplicateKeys = true;
			parser.Parser.Configuration.AllowKeysWithoutSection = true;

			try
			{
				var iniData = parser.ReadFile(filePath, Encoding.UTF8);
				if (iniData == null)
					return false;

				LoadFromIni(iniData);
			}
			catch
			{
				return false;
			}

			return true;
		}

		private static void LoadFromIni(IniData iniData)
		{
			var settingsSection = iniData.Sections["Settings"];
			if (settingsSection != null)
			{
				ReadBool(ref Settings.AutoConvertNames, settingsSection, "AutoConvertNames");
				ReadString(ref Settings.UserPlaceholder, settingsSection, "UserPlaceholder");
				ReadBool(ref Settings.ShowNSFW, settingsSection, "ShowNSFW");
				ReadBool(ref Settings.ConfirmNSFW, settingsSection, "ConfirmNSFW");
				ReadInt(ref Settings.UndoSteps, settingsSection, "UndoSteps");
				ReadInt(ref Settings.TokenBudget, settingsSection, "TokenBudget", 0, 32768);
				ReadEnum(ref Settings.PreviewFormat, settingsSection, "PreviewFormat");
				ReadBool(ref Settings.AutoBreakLine, settingsSection, "AutoBreakLine");
				ReadBool(ref Settings.SpellChecking, settingsSection, "SpellChecking");
				ReadString(ref Settings.Dictionary, settingsSection, "Dictionary");
				ReadString(ref Settings.Locale, settingsSection, "Locale");
				ReadBool(ref Settings.ShowRecipeCategory, settingsSection, "ShowRecipeCategory");
				ReadBool(ref Settings.EnableFormLevelBuffering, settingsSection, "FormBuffering");
				
				try
				{
					Settings.FontSerialize = settingsSection["Font"];
				}
				catch
				{ }

				if (Locales.IsValidLocale(Settings.Locale) == false)
					Settings.Locale = Locales.DefaultLocale;
			}

			var userSection = iniData.Sections["User"];
			if (userSection != null)
			{
				ReadInt(ref User.LastImportCharacterFilter, userSection, "LastImportCharacterFilter");
				ReadInt(ref User.LastImportLorebookFilter, userSection, "LastImportLorebookFilter");
				ReadInt(ref User.LastExportCharacterFilter, userSection, "LastExportCharacterFilter");
				ReadInt(ref User.LastExportLorebookFilter, userSection, "LastExportLorebookFilter");
				ReadBool(ref User.LaunchTextEditor, userSection, "LaunchTextEditor");
				ReadBool(ref User.FindMatchCase, userSection, "FindMatchCase");
				ReadBool(ref User.FindWholeWords, userSection, "FindWholeWords");
				ReadPoint(ref User.FindLocation, userSection, "FindLocation");
				ReadBool(ref User.ReplaceMatchCase, userSection, "ReplaceMatchCase");
				ReadBool(ref User.ReplaceWholeWords, userSection, "ReplaceWholeWords");
				ReadBool(ref User.ReplaceLorebooks, userSection, "ReplaceLorebooks");
				ReadBool(ref User.SnippetSwapPronouns, userSection, "SnippetSwapPronouns");
			}

			var writeSection = iniData.Sections["Write"];
			if (writeSection != null)
			{
				ReadBool(ref WriteDialog.WordWrap, writeSection, "WordWrap");
				ReadBool(ref WriteDialog.Highlight, writeSection, "Highlight");
				ReadBool(ref WriteDialog.HighlightNames, writeSection, "HighlightNames");
				ReadBool(ref WriteDialog.HighlightNumbers, writeSection, "HighlightNumbers");
				ReadBool(ref WriteDialog.HighlightPronouns, writeSection, "HighlightPronouns");
				ReadBool(ref WriteDialog.AutoBreakLine, writeSection, "AutoBreakLine");
				ReadPoint(ref WriteDialog.WindowLocation, userSection, "WindowLocation");
				ReadPoint(ref WriteDialog.WindowSize, userSection, "WindowSize");
				ReadBool(ref WriteDialog.DarkMode, writeSection, "DarkMode");

				try
				{
					WriteDialog.FontSerialize = writeSection["Font"];
				}
				catch
				{ }
			}

			var pathsSection = iniData.Sections["Paths"];
			if (pathsSection != null)
			{
				ReadString(ref Paths.LastCharacterPath, pathsSection, "LastCharacterPath");
				ReadString(ref Paths.LastImagePath, pathsSection, "LastImagePath");
				ReadString(ref Paths.LastImportPath, pathsSection, "LastImportPath");

				if (Paths.LastCharacterPath == "") Paths.LastCharacterPath = null;
				if (Paths.LastImagePath == "") Paths.LastImagePath = null;
				if (Paths.LastImportPath == "") Paths.LastImportPath = null;
			}

			var faradaySection = iniData.Sections["BackyardAI"];
			if (faradaySection != null)
			{
				ReadInt(ref Faraday.RepeatPenaltyTokens, faradaySection, "RepeatPenaltyTokens", 16, 512);
				ReadDecimal(ref Faraday.RepeatPenalty, faradaySection, "RepeatPenalty", 0.5m, 2.0m);
				ReadDecimal(ref Faraday.Temperature, faradaySection, "Temperature", 0m, 5m);
				ReadInt(ref Faraday.TopK, faradaySection, "TopK", 0, 100);
				ReadDecimal(ref Faraday.TopP, faradaySection, "TopP", 0m, 1m);
				ReadDecimal(ref Faraday.MinP, faradaySection, "MinP", 0m, 1m);
				ReadBool(ref Faraday.MinPEnabled, faradaySection, "MinPEnabled");
				ReadInt(ref Faraday.PromptTemplate, faradaySection, "PromptTemplate", 0, 3);
				ReadBool(ref Faraday.PruneExampleChat, faradaySection, "PruneExampleChat");
			}

			var fileFormatSection = iniData.Sections["FileFormat"];
			if (fileFormatSection != null)
			{
				ReadBool(ref FileFormat.EnableBackyardAI, fileFormatSection, "EnableBackyardAI");
				ReadBool(ref FileFormat.EnableCCV2, fileFormatSection, "EnableCCV2");
				ReadBool(ref FileFormat.EnableCCV3, fileFormatSection, "EnableCCV3");
			}

			var mruSection = iniData.Sections["MRU"];
			if (mruSection != null)
			{
				for (int mruIndex = 0; mruIndex < MRUList.MaxCount; ++mruIndex)
				{
					string value = mruSection[string.Format("File{0:00}", mruIndex)];
					if (string.IsNullOrWhiteSpace(value))
						break;

					int pos_split = value.IndexOf('|', 0);
					string filename;
					string characterName;

					if (pos_split != -1)
					{
						filename = value.Substring(0, pos_split);
						characterName = value.Substring(pos_split + 1);
					}
					else
					{
						filename = value;
						characterName = "";
					}
					if (string.IsNullOrWhiteSpace(filename))
						break;

					MRUList.AddToMRU(filename, characterName);
				}
			}
		}

		public static void SaveToIni(string filePath)
		{
			try
			{
				using (StreamWriter outputFile = new StreamWriter(filePath))
				{
					// Settings
					WriteSection(outputFile, "Settings", false);
					if (Settings.FontFace != null)
						Write(outputFile, "Font", Settings.FontSerialize);
					if (Settings.EnableFormLevelBuffering == false)
						Write(outputFile, "FormBuffering", Settings.EnableFormLevelBuffering);
					Write(outputFile, "AutoConvertNames", Settings.AutoConvertNames);
					Write(outputFile, "UserPlaceholder", Settings.UserPlaceholder);
					Write(outputFile, "ShowNSFW", Settings.ShowNSFW);
					Write(outputFile, "ConfirmNSFW", Settings.ConfirmNSFW);
					Write(outputFile, "UndoSteps", Settings.UndoSteps);
					Write(outputFile, "TokenBudget", Settings.TokenBudget);
					Write(outputFile, "PreviewFormat", Settings.PreviewFormat);
					Write(outputFile, "AutoBreakLine", Settings.AutoBreakLine);
					Write(outputFile, "SpellChecking", Settings.SpellChecking);
					Write(outputFile, "Dictionary", Settings.Dictionary);
					Write(outputFile, "Locale", Settings.Locale);
					Write(outputFile, "ShowRecipeCategory", Settings.ShowRecipeCategory);

					// User
					WriteSection(outputFile, "User");
					Write(outputFile, "LastImportCharacterFilter", User.LastImportCharacterFilter);
					Write(outputFile, "LastImportLorebookFilter", User.LastImportLorebookFilter);
					Write(outputFile, "LastExportCharacterFilter", User.LastExportCharacterFilter);
					Write(outputFile, "LastExportLorebookFilter", User.LastExportLorebookFilter);
					Write(outputFile, "LaunchTextEditor", User.LaunchTextEditor);
					Write(outputFile, "FindMatchCase", User.FindMatchCase);
					Write(outputFile, "FindWholeWords", User.FindWholeWords);
					Write(outputFile, "FindLocation", User.FindLocation);
					Write(outputFile, "ReplaceMatchCase", User.ReplaceMatchCase);
					Write(outputFile, "ReplaceWholeWords", User.ReplaceWholeWords);
					Write(outputFile, "ReplaceLorebooks", User.ReplaceLorebooks);
					Write(outputFile, "SnippetSwapPronouns", User.SnippetSwapPronouns);

					// Write
					WriteSection(outputFile, "Write");
					Write(outputFile, "Font", WriteDialog.FontSerialize);
					Write(outputFile, "WordWrap", WriteDialog.WordWrap);
					Write(outputFile, "Highlight", WriteDialog.Highlight);
					Write(outputFile, "HighlightNames", WriteDialog.HighlightNames);
					Write(outputFile, "HighlightNumbers", WriteDialog.HighlightNumbers);
					Write(outputFile, "HighlightPronouns", WriteDialog.HighlightPronouns);
					Write(outputFile, "AutoBreakLine", WriteDialog.AutoBreakLine);
					Write(outputFile, "WindowLocation", WriteDialog.WindowLocation);
					Write(outputFile, "WindowSize", WriteDialog.WindowSize);
					Write(outputFile, "DarkMode", WriteDialog.DarkMode);

					// Paths
					WriteSection(outputFile, "Paths");
					Write(outputFile, "LastCharacterPath", Paths.LastCharacterPath);
					Write(outputFile, "LastImagePath", Paths.LastImagePath);
					Write(outputFile, "LastImportPath", Paths.LastImportPath);

					// Faraday
					WriteSection(outputFile, "BackyardAI");
					Write(outputFile, "RepeatPenaltyTokens", Faraday.RepeatPenaltyTokens);
					Write(outputFile, "RepeatPenalty", Faraday.RepeatPenalty);
					Write(outputFile, "Temperature", Faraday.Temperature);
					Write(outputFile, "TopK", Faraday.TopK);
					Write(outputFile, "MinPEnabled", Faraday.MinPEnabled);
					Write(outputFile, "MinP", Faraday.MinP);
					Write(outputFile, "TopP", Faraday.TopP);
					Write(outputFile, "PromptTemplate", Faraday.PromptTemplate);
					Write(outputFile, "PruneExampleChat", Faraday.PruneExampleChat);
					
					// CCV3
					WriteSection(outputFile, "FileFormat");
					Write(outputFile, "EnableBackyardAI", FileFormat.EnableBackyardAI);
					Write(outputFile, "EnableCCV2", FileFormat.EnableCCV2);
					Write(outputFile, "EnableCCV3", FileFormat.EnableCCV3);

					// MRU list
					WriteSection(outputFile, "MRU");
					int mruIndex = 0;
					foreach (var mruItem in MRUList.mruItems)
						outputFile.WriteLine(string.Format("File{0:00} = {1}|{2}", mruIndex++, mruItem.filename ?? "", mruItem.characterName ?? ""));

				}
			}
			catch
			{
				// Do nothing
			}
		}

		private static void ReadBool(ref bool value, KeyDataCollection ini, string name)
		{
			value = Utility.StringToBool(ini[name], value);
		}

		private static void ReadInt(ref int value, KeyDataCollection ini, string name, int min = int.MinValue, int max = int.MaxValue)
		{
			value = Utility.StringToInt(ini[name], min, max, value);
		}

		private static void ReadDecimal(ref decimal value, KeyDataCollection ini, string name, decimal min = decimal.MinValue, decimal max = decimal.MaxValue)
		{
			value = Utility.StringToDecimal(ini[name], min, max, value);
		}

		private static void ReadString(ref string value, KeyDataCollection ini, string name)
		{
			if (ini.ContainsKey(name))
				value = ini[name]?.Trim();
		}

		private static void ReadEnum<T>(ref T value, KeyDataCollection ini, string name) where T : struct, IConvertible
		{
			value = EnumHelper.FromString(ini[name], value);
		}

		private static void ReadPoint(ref Point value, KeyDataCollection ini, string name)
		{
			value = Utility.StringToPoint(ini[name], value);
		}

		private static void WriteSection(StreamWriter w, string name, bool spacing = true)
		{
			if (spacing)
				w.WriteLine("");
			w.WriteLine(string.Concat("[", name, "]"));
		}

		private static void Write(StreamWriter w, string name, bool value)
		{
			w.WriteLine(string.Format("{0} = {1}", name, value ? "Yes" : "No"));
		}

		private static void Write(StreamWriter w, string name, int value)
		{
			w.WriteLine(string.Format("{0} = {1}", name, value));
		}

		private static void Write(StreamWriter w, string name, decimal value)
		{
			w.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} = {1:0.##}", name, value));
		}

		private static void Write(StreamWriter w, string name, string value)
		{
			w.WriteLine(string.Format("{0} = {1}", name, value ?? ""));
		}

		private static void Write<T>(StreamWriter w, string name, T value) where T : struct, IConvertible
		{
			w.WriteLine(string.Format("{0} = {1}", name, EnumHelper.ToString(value)));
		}

		private static void Write(StreamWriter w, string name, Point value)
		{
			w.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} = {1},{2}", name, value.X, value.Y));
		}
	}
}
