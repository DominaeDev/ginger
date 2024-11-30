using Ginger.Integration;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
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
			public static bool AllowNSFW = false;
			public static bool ConfirmNSFW = true;
			public static int UndoSteps = 80;
			public static int TokenBudget = 0;
			public static bool AutoBreakLine = true;
			public static string Locale = Locales.DefaultLocale;
			public static bool EnableFormLevelBuffering = true;
			public static bool DarkTheme = false;

			public static int LoreEntriesPerPage { get { return _loreEntriesPerPage > 0 ? Math.Max(_loreEntriesPerPage, 10) : int.MaxValue; } }
			public static string LoreEntriesPerPageSerialize
			{
				get { return _loreEntriesPerPage.ToString(CultureInfo.InvariantCulture); }
				set 
				{
					if (int.TryParse(value, out _loreEntriesPerPage) == false)
						_loreEntriesPerPage = 10;
				}
			}
			private static int _loreEntriesPerPage = 10;
			public static bool EnableRearrangeLoreMode = false;


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

		public enum CharacterSortOrder
		{
			ByName,
			ByCreation,
			ByLastMessage,

			Default = ByName,
		}

		public static class User
		{
			public static int LastImportCharacterFilter = 0;
			public static int LastImportLorebookFilter = 0;
			public static int LastImportChatFilter = 0;

			public static int LastExportCharacterFilter = 0;
			public static int LastExportLorebookFilter = 0;
			public static int LastExportChatFilter = 0;

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

			public static CharacterSortOrder SortCharacters = CharacterSortOrder.Default;
			public static CharacterSortOrder SortGroups = CharacterSortOrder.Default;
		}
		
		public static class Paths
		{
			public static string LastCharacterPath = null;
			public static string LastImagePath = null;
			public static string LastImportExportPath = null;
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

		public static class BackyardSettings
		{
			public static ChatParameters DefaultSettings
			{
				get
				{
					if (chatSettings.Count > 0)
						return chatSettings[0];
					return ChatParameters.Default;
				}
				set
				{
					if (chatSettings.Count > 0)
						chatSettings[0] = value;
					else
						chatSettings.Add(value);
				}
			}

			public static List<ChatParameters> chatSettings = new List<ChatParameters>();
			
		}

		public static class BackyardLink
		{
			public static bool Enabled = false;
			public static bool Strict = true;
			public static string Location = null;
			public static bool Autosave = true;
			public static bool AlwaysLinkOnImport = false;
			public static VersionNumber LastVersion;
			public static string BulkImportFolderName = "Imported from Ginger";

			public enum ActiveChatSetting { First, Last, All }
			public static ActiveChatSetting ApplyChatSettings = ActiveChatSetting.Last;
			public static bool UsePortraitAsBackground = false;
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
				ReadBool(ref Settings.AllowNSFW, settingsSection, "ShowNSFW");
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
				Settings.LoreEntriesPerPageSerialize = settingsSection["LoreEntriesPerPage"];
				ReadBool(ref Settings.EnableRearrangeLoreMode, settingsSection, "EnableRearrangeLoreMode");
				ReadBool(ref Settings.DarkTheme, settingsSection, "DarkTheme");
				
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
				ReadInt(ref User.LastImportChatFilter, userSection, "LastImportChatFilter");
				ReadInt(ref User.LastExportCharacterFilter, userSection, "LastExportCharacterFilter");
				ReadInt(ref User.LastExportLorebookFilter, userSection, "LastExportLorebookFilter");
				ReadInt(ref User.LastExportChatFilter, userSection, "LastExportChatFilter");
				ReadBool(ref User.LaunchTextEditor, userSection, "LaunchTextEditor");
				ReadBool(ref User.FindMatchCase, userSection, "FindMatchCase");
				ReadBool(ref User.FindWholeWords, userSection, "FindWholeWords");
				ReadPoint(ref User.FindLocation, userSection, "FindLocation");
				ReadBool(ref User.ReplaceMatchCase, userSection, "ReplaceMatchCase");
				ReadBool(ref User.ReplaceWholeWords, userSection, "ReplaceWholeWords");
				ReadBool(ref User.ReplaceLorebooks, userSection, "ReplaceLorebooks");
				ReadBool(ref User.SnippetSwapPronouns, userSection, "SnippetSwapPronouns");
				ReadEnum(ref User.SortCharacters, userSection, "SortCharacters");
				ReadEnum(ref User.SortGroups, userSection, "SortGroups");
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
				ReadString(ref Paths.LastImportExportPath, pathsSection, "LastImportPath");

				if (Paths.LastCharacterPath == "") Paths.LastCharacterPath = null;
				if (Paths.LastImagePath == "") Paths.LastImagePath = null;
				if (Paths.LastImportExportPath == "") Paths.LastImportExportPath = null;
			}

			var linkSection = iniData.Sections["BackyardAI.Link"];
			if (linkSection != null)
			{
				ReadBool(ref BackyardLink.Enabled, linkSection, "Enabled");
				ReadBool(ref BackyardLink.Strict, linkSection, "Strict");
				BackyardLink.LastVersion = VersionNumber.Parse(linkSection["LastVersion"]);
				ReadBool(ref BackyardLink.Autosave, linkSection, "Autosave");
				ReadBool(ref BackyardLink.AlwaysLinkOnImport, linkSection, "AlwaysLinkOnImport");
				ReadString(ref BackyardLink.Location, linkSection, "Location");
				ReadEnum(ref BackyardLink.ApplyChatSettings, linkSection, "ApplyChatSettings");
				ReadBool(ref BackyardLink.UsePortraitAsBackground, linkSection, "UsePortraitAsBackground");
				ReadString(ref BackyardLink.BulkImportFolderName, linkSection, "BulkImportFolderName");
			}
			
			var presetsSection = iniData.Sections["BackyardAI.Presets"];
			if (presetsSection != null)
			{
				for (int index = 0; index < 100; ++index)
				{
					string value = presetsSection[string.Format("Preset{0:00}.Model", index)];
					if (string.IsNullOrWhiteSpace(value))
						break;

					ChatParameters chatSettings = new ChatParameters();

					ReadString(ref chatSettings.model, presetsSection, string.Format("Preset{0:00}.Model", index));
					if (string.Compare(chatSettings.model, "default", StringComparison.OrdinalIgnoreCase) == 0)
						chatSettings.model = null;
					ReadInt(ref chatSettings.repeatPenaltyTokens, presetsSection, string.Format("Preset{0:00}.RepeatPenaltyTokens", index), 16, 512);
					ReadDecimal(ref chatSettings.repeatPenalty, presetsSection, string.Format("Preset{0:00}.RepeatPenalty", index), 0.5m, 2.0m);
					ReadDecimal(ref chatSettings.temperature, presetsSection, string.Format("Preset{0:00}.Temperature", index), 0m, 5m);
					ReadInt(ref chatSettings.topK, presetsSection, string.Format("Preset{0:00}.TopK", index), 0, 100);
					ReadDecimal(ref chatSettings.topP, presetsSection, string.Format("Preset{0:00}.TopP", index), 0m, 1m);
					ReadDecimal(ref chatSettings.minP, presetsSection, string.Format("Preset{0:00}.MinP", index), 0m, 1m);
					ReadBool(ref chatSettings.minPEnabled, presetsSection, string.Format("Preset{0:00}.MinPEnabled", index));
					ReadBool(ref chatSettings.pruneExampleChat, presetsSection, string.Format("Preset{0:00}.PruneExampleChat", index));
					ReadString(ref chatSettings.authorNote, presetsSection, string.Format("Preset{0:00}.AuthorNote", index));
					string promptTemplate = null;
					ReadString(ref promptTemplate, presetsSection, string.Format("Preset{0:00}.PromptTemplate", index));
					chatSettings.promptTemplate = promptTemplate;

					BackyardSettings.chatSettings.Add(chatSettings);
				}
			}
			else
			{
				var backyardSection = iniData.Sections["BackyardAI"];
				if (backyardSection != null)
				{
					if (backyardSection.ContainsKey("Model"))	// Legacy
					{
						ChatParameters chatSettings = new ChatParameters();

						ReadString(ref chatSettings.model, backyardSection, "Model");
						if (string.Compare(chatSettings.model, "default", StringComparison.OrdinalIgnoreCase) == 0)
							chatSettings.model = null;
						ReadInt(ref chatSettings.repeatPenaltyTokens, backyardSection, "RepeatPenaltyTokens", 16, 512);
						ReadDecimal(ref chatSettings.repeatPenalty, backyardSection, "RepeatPenalty", 0.5m, 2.0m);
						ReadDecimal(ref chatSettings.temperature, backyardSection, "Temperature", 0m, 5m);
						ReadInt(ref chatSettings.topK, backyardSection, "TopK", 0, 100);
						ReadDecimal(ref chatSettings.topP, backyardSection, "TopP", 0m, 1m);
						ReadDecimal(ref chatSettings.minP, backyardSection, "MinP", 0m, 1m);
						ReadBool(ref chatSettings.minPEnabled, backyardSection, "MinPEnabled");
						ReadBool(ref chatSettings.pruneExampleChat, backyardSection, "PruneExampleChat");
						ReadString(ref chatSettings.authorNote, backyardSection, "AuthorNote");
						int iPromptTemplate = 0;
						ReadInt(ref iPromptTemplate, backyardSection, "PromptTemplate");
						chatSettings.iPromptTemplate = iPromptTemplate;

						BackyardSettings.chatSettings.Add(chatSettings);
					}
				}
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
					Write(outputFile, "ShowNSFW", Settings.AllowNSFW);
					Write(outputFile, "ConfirmNSFW", Settings.ConfirmNSFW);
					Write(outputFile, "UndoSteps", Settings.UndoSteps);
					Write(outputFile, "TokenBudget", Settings.TokenBudget);
					Write(outputFile, "PreviewFormat", Settings.PreviewFormat);
					Write(outputFile, "AutoBreakLine", Settings.AutoBreakLine);
					Write(outputFile, "SpellChecking", Settings.SpellChecking);
					Write(outputFile, "Dictionary", Settings.Dictionary);
					Write(outputFile, "Locale", Settings.Locale);
					Write(outputFile, "ShowRecipeCategory", Settings.ShowRecipeCategory);
					Write(outputFile, "LoreEntriesPerPage", Settings.LoreEntriesPerPageSerialize);
					Write(outputFile, "EnableRearrangeLoreMode", Settings.EnableRearrangeLoreMode);
					Write(outputFile, "DarkTheme", Settings.DarkTheme);

					// User
					WriteSection(outputFile, "User");
					Write(outputFile, "LastImportCharacterFilter", User.LastImportCharacterFilter);
					Write(outputFile, "LastImportLorebookFilter", User.LastImportLorebookFilter);
					Write(outputFile, "LastImportChatFilter", User.LastImportChatFilter);
					Write(outputFile, "LastExportCharacterFilter", User.LastExportCharacterFilter);
					Write(outputFile, "LastExportLorebookFilter", User.LastExportLorebookFilter);
					Write(outputFile, "LastExportChatFilter", User.LastExportChatFilter);
					Write(outputFile, "LaunchTextEditor", User.LaunchTextEditor);
					Write(outputFile, "FindMatchCase", User.FindMatchCase);
					Write(outputFile, "FindWholeWords", User.FindWholeWords);
					Write(outputFile, "FindLocation", User.FindLocation);
					Write(outputFile, "ReplaceMatchCase", User.ReplaceMatchCase);
					Write(outputFile, "ReplaceWholeWords", User.ReplaceWholeWords);
					Write(outputFile, "ReplaceLorebooks", User.ReplaceLorebooks);
					Write(outputFile, "SnippetSwapPronouns", User.SnippetSwapPronouns);
					Write(outputFile, "SortCharacters", User.SortCharacters);
					Write(outputFile, "SortGroups", User.SortGroups);

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

					// Paths
					WriteSection(outputFile, "Paths");
					Write(outputFile, "LastCharacterPath", Paths.LastCharacterPath);
					Write(outputFile, "LastImagePath", Paths.LastImagePath);
					Write(outputFile, "LastImportPath", Paths.LastImportExportPath);

					// Backyard link settings
					WriteSection(outputFile, "BackyardAI.Link");
					Write(outputFile, "Location", BackyardLink.Location);
					if (BackyardLink.LastVersion.isDefined)
						Write(outputFile, "LastVersion", BackyardLink.LastVersion.ToFullString());
					Write(outputFile, "Enabled", BackyardLink.Enabled);
					Write(outputFile, "Strict", BackyardLink.Strict);
					Write(outputFile, "Autosave", BackyardLink.Autosave);
					Write(outputFile, "AlwaysLinkOnImport", BackyardLink.AlwaysLinkOnImport);
					Write(outputFile, "ApplyChatSettings", BackyardLink.ApplyChatSettings);
					Write(outputFile, "UsePortraitAsBackground", BackyardLink.UsePortraitAsBackground);
					Write(outputFile, "BulkImportFolderName", BackyardLink.BulkImportFolderName);

					// Backyard model settings
					WriteSection(outputFile, "BackyardAI.Presets");
					if (BackyardSettings.chatSettings != null)
					{
						IEnumerable<ChatParameters> chatSettings = BackyardSettings.chatSettings;
						if (chatSettings.IsEmpty())
							chatSettings = new ChatParameters[] { BackyardSettings.DefaultSettings };

						int index = 0;
						foreach (var settings in chatSettings)
						{
							if (string.IsNullOrEmpty(settings.model) == false)
								Write(outputFile, string.Format("Preset{0:00}.Model", index), settings.model);
							else
								Write(outputFile, string.Format("Preset{0:00}.Model", index), "Default");
							Write(outputFile, string.Format("Preset{0:00}.PromptTemplate", index), settings.iPromptTemplate);
							Write(outputFile, string.Format("Preset{0:00}.Temperature", index), settings.temperature);
							Write(outputFile, string.Format("Preset{0:00}.MinPEnabled", index), settings.minPEnabled);
							Write(outputFile, string.Format("Preset{0:00}.MinP", index), settings.minP);
							Write(outputFile, string.Format("Preset{0:00}.TopP", index), settings.topP);
							Write(outputFile, string.Format("Preset{0:00}.TopK", index), settings.topK);
							Write(outputFile, string.Format("Preset{0:00}.RepeatPenalty", index), settings.repeatPenalty);
							Write(outputFile, string.Format("Preset{0:00}.RepeatPenaltyTokens", index), settings.repeatPenaltyTokens);
							Write(outputFile, string.Format("Preset{0:00}.PruneExampleChat", index), settings.pruneExampleChat);
							Write(outputFile, string.Format("Preset{0:00}.AuthorNote", index), settings.authorNote);
							++index;
						}
					}
					
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
