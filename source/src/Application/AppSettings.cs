using Ginger.Integration;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
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
			public struct Preset
			{
				public Preset(string name, ChatParameters parameters)
				{
					this.name = name;
					this.parameters = parameters.Copy();
				}

				public static implicit operator ChatParameters(Preset preset)
				{
					return preset.parameters.Copy();
				}

				public string name;
				private ChatParameters parameters;
			}

			public static ChatParameters UserSettings = new ChatParameters();
			public static List<Preset> Presets = new List<Preset>();
			public static List<KeyValuePair<string, int>> ModelPromptTemplates = new List<KeyValuePair<string, int>>();

			public static int GetPromptTemplateForModel(string model)
			{
				if (string.IsNullOrEmpty(model))
					return -1;

				int idxModel = ModelPromptTemplates.FindIndex(kvp => string.Compare(kvp.Key, model, StringComparison.OrdinalIgnoreCase) == 0);
				if (idxModel == -1)
					return -1;

				int promptTemplate = ModelPromptTemplates[idxModel].Value;
				if (promptTemplate >= 0 && promptTemplate <= ChatParameters.MaxPromptTemplate)
					return promptTemplate;
				return -1;
			}

			public static void SetPromptTemplateForModel(string model, int promptTemplate)
			{
				if (string.IsNullOrEmpty(model))
					return;

				int idxModel = ModelPromptTemplates.FindIndex(kvp => string.Compare(kvp.Key, model, StringComparison.OrdinalIgnoreCase) == 0);
				if (idxModel != -1)
				{
					if (promptTemplate >= 0)
						ModelPromptTemplates[idxModel] = new KeyValuePair<string, int>(model, promptTemplate);
					else
						ModelPromptTemplates.RemoveAt(idxModel);
				}
				else
					ModelPromptTemplates.Add(new KeyValuePair<string, int>(model, promptTemplate));
			}
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
			public static bool ImportAlternateGreetings = false;

			public static bool PruneExampleChat = true;
			public static bool MarkNSFW = true;
			public static bool WriteAuthorNote = true;
			public static bool WriteUserPersona = false;
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
			BackyardSettings.Presets.Clear();
			BackyardSettings.ModelPromptTemplates.Clear();

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

			var backyardSection = iniData.Sections["BackyardAI"]; // Since v1.5.0
			if (backyardSection != null && backyardSection.ContainsKey("Model") == false)
				ReadBackyardSettings(backyardSection);
			else
			{
				var linkSection = iniData.Sections["BackyardAI.Link"]; // Prior to v1.5.0
				if (linkSection != null) // Legacy
					ReadBackyardSettings(linkSection);
			}

			BackyardSettings.Presets.Clear();
			var modelSettingsSection = iniData.Sections["BackyardAI.ModelSettings.Default"]; // Since v1.5.0
			if (modelSettingsSection != null)
				BackyardSettings.UserSettings = ReadModelSettings(modelSettingsSection);
			else if (backyardSection != null && backyardSection.ContainsKey("Model")) // Prior to v1.5.0 
				BackyardSettings.UserSettings = ReadModelSettings(backyardSection);

			// Model setting presets
			int unnamedPresetCounter = 1;
			for (int i = 0; i < 1000; ++i)
			{
				var presetsSection = iniData.Sections[string.Format("BackyardAI.ModelSettings.Preset{0:00}", i)];
				if (presetsSection != null)
				{
					ChatParameters chatParameters = ReadModelSettings(presetsSection);
					string presetName = null;
					ReadString(ref presetName, presetsSection, "Name");
					if (string.IsNullOrEmpty(presetName))
						presetName = string.Format("Preset #{0}", unnamedPresetCounter++);
					BackyardSettings.Presets.Add(new BackyardSettings.Preset(presetName, chatParameters));
					continue;
				}
				break;
			}

			// Prompt templates
			var promptTemplateSection = iniData.Sections["BackyardAI.PromptTemplate"];
			if (promptTemplateSection != null)
			{
				foreach (KeyData item in promptTemplateSection)
				{
					int iPromptTemplate = Utility.StringToInt(item.Value, -1);
					if (iPromptTemplate >= 0 && iPromptTemplate <= ChatParameters.MaxPromptTemplate)
						BackyardSettings.SetPromptTemplateForModel(item.KeyName, iPromptTemplate);
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

		private static void ReadBackyardSettings(KeyDataCollection section)
		{
			ReadBool(ref BackyardLink.Enabled, section, "Enabled");
			ReadBool(ref BackyardLink.Strict, section, "Strict");
			BackyardLink.LastVersion = VersionNumber.Parse(section["LastVersion"]);
			ReadBool(ref BackyardLink.Autosave, section, "Autosave");
			ReadBool(ref BackyardLink.AlwaysLinkOnImport, section, "AlwaysLinkOnImport");
			ReadString(ref BackyardLink.Location, section, "Location");
			BackyardLink.Location = BackyardLink.Location.Replace('/', '\\');
			ReadEnum(ref BackyardLink.ApplyChatSettings, section, "ApplyChatSettings");
			ReadBool(ref BackyardLink.UsePortraitAsBackground, section, "UsePortraitAsBackground");
			ReadString(ref BackyardLink.BulkImportFolderName, section, "BulkImportFolderName");
			ReadBool(ref BackyardLink.PruneExampleChat, section, "PruneExampleChat");
			ReadBool(ref BackyardLink.MarkNSFW, section, "MarkNSFW");
			ReadBool(ref BackyardLink.WriteAuthorNote, section, "WriteAuthorNote");
			ReadBool(ref BackyardLink.WriteUserPersona, section, "WriteUserPersona");
			ReadBool(ref BackyardLink.ImportAlternateGreetings, section, "ImportAlternateGreetings");
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

					// MRU list
					WriteSection(outputFile, "MRU");
					int mruIndex = 0;
					foreach (var mruItem in MRUList.mruItems)
						outputFile.WriteLine(string.Format("File{0:00} = {1}|{2}", mruIndex++, mruItem.filename ?? "", mruItem.characterName ?? ""));

					// Backyard settings
					WriteSection(outputFile, "BackyardAI");
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
					Write(outputFile, "PruneExampleChat", BackyardLink.PruneExampleChat);
					Write(outputFile, "MarkNSFW", BackyardLink.MarkNSFW);
					Write(outputFile, "WriteAuthorNote", BackyardLink.WriteAuthorNote);
					Write(outputFile, "WriteUserPersona", BackyardLink.WriteUserPersona);
					Write(outputFile, "ImportAlternateGreetings", BackyardLink.ImportAlternateGreetings);

					// Backyard model settings
					WriteSection(outputFile, "BackyardAI.ModelSettings.Default");
					WriteModelSettings(outputFile, BackyardSettings.UserSettings);

					// Presets
					for (int i = 0; i < BackyardSettings.Presets.Count; ++i)
					{
						var preset = BackyardSettings.Presets[i];
							
						WriteSection(outputFile, string.Format("BackyardAI.ModelSettings.Preset{0:00}", i));
						Write(outputFile, "Name", preset.name);
						WriteModelSettings(outputFile, preset);
					}

					// Model prompt templates
					if (BackyardSettings.ModelPromptTemplates.Count > 0)
					{
						WriteSection(outputFile, "BackyardAI.PromptTemplate");
						foreach (var kvp in BackyardSettings.ModelPromptTemplates.OrderBy(kvp => kvp.Key).Where(kvp => kvp.Value >= 0))
							Write(outputFile, kvp.Key, kvp.Value);
					}

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

		private static ChatParameters ReadModelSettings(KeyDataCollection section)
		{
			ChatParameters chatSettings = new ChatParameters();

			string model = null;
			ReadString(ref model, section, "Model");
			chatSettings.model = model;

			ReadInt(ref chatSettings.repeatLastN, section, "RepeatPenaltyTokens", 16, 512);
			ReadDecimal(ref chatSettings.repeatPenalty, section, "RepeatPenalty", 0.5m, 2.0m);
			ReadDecimal(ref chatSettings.temperature, section, "Temperature", 0m, 5m);
			ReadInt(ref chatSettings.topK, section, "TopK", 0, 100);
			ReadDecimal(ref chatSettings.topP, section, "TopP", 0m, 1m);
			ReadDecimal(ref chatSettings.minP, section, "MinP", 0m, 1m);
			ReadBool(ref chatSettings.minPEnabled, section, "MinPEnabled");
			string promptTemplate = null;
			ReadString(ref promptTemplate, section, "PromptTemplate");
			chatSettings.promptTemplate = promptTemplate;

			return chatSettings;
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
			w.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} = {1:0.###}", name, value));
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

		private static void WriteModelSettings(StreamWriter w, ChatParameters settings)
		{
			Write(w, "Model", string.IsNullOrEmpty(settings.model) ? "Default" : settings.model);
			Write(w, "PromptTemplate", settings.iPromptTemplate);
			Write(w, "Temperature", settings.temperature);
			Write(w, "MinPEnabled", settings.minPEnabled);
			Write(w, "MinP", settings.minP);
			Write(w, "TopP", settings.topP);
			Write(w, "TopK", settings.topK);
			Write(w, "RepeatPenalty", settings.repeatPenalty);
			Write(w, "RepeatPenaltyTokens", settings.repeatLastN);
		}
	}
}
