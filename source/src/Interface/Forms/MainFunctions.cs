using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Ginger.Properties;
using Ginger.Integration;

using WinAPICodePack = Microsoft.WindowsAPICodePack.Dialogs;

using Backyard = Ginger.Integration.Backyard;

namespace Ginger
{
	public partial class MainForm
	{
		private void Regenerate()
		{
			Generator.Option options = Generator.Option.Preview;
			if (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday)
				options |= Generator.Option.Faraday;
			else if (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.SillyTavern)
				options |= Generator.Option.SillyTavernV2;
			if (Backyard.ConnectionEstablished && Current.HasActiveLink)
				options |= Generator.Option.Linked;

			Generator.Output output = Generator.Generate(options);

			outputBox.SetOutput(output);

#if DEBUG && false
			string faradayJson;
			Generator.Output faradayOutput = Generator.Generate(Generator.Option.Export | Generator.Option.Faraday);
			faradayJson = FaradayCardV4.FromOutput(faradayOutput).ToJson();
			outputBox_Raw.Text = faradayJson;

			string tavernJson;
			Generator.Output tavernOutput = Generator.Generate(Generator.Option.Export | Generator.Option.SillyTavern);
			var tavernCard = TavernCardV2_Export.FromOutput(output);
			tavernCard.data.extensions.ginger = GingerExtensionData.FromOutput(Generator.Generate(Generator.Option.Snippet | Generator.Option.Preview));
			tavernJson = tavernCard.ToJson();
			outputBox_Raw2.Text = tavernJson;
#endif

			sidePanel.SetLoreCount(output.hasLore ? output.lorebook.entries.Count : 0, true);
			sidePanel.OnRegenerate();

			// Recalculate token count
			CalculateTokens(output);
		}

		private void CalculateTokens(Generator.Output output)
		{
			// Recalculate token count
			_inputHash = output.GetHashCode();
			tokenQueue.Schedule(output, _inputHash, this);
		}

		private void TokenQueue_onTokenCount(TokenizerQueue.Result result)
		{
			if (result.hash != _inputHash)
				return;

			Current.Card.lastTokenCounts = new int[] {
				result.tokens_total,
				result.tokens_permanent_faraday,
				result.tokens_permanent_silly,
			};
			_bShouldRefreshTokenCount = true;

			recipeList.RefreshLoreTokenCounts(result.loreTokens);
		}

		private bool BakeAll()
		{
			StealFocus();

			var output = Generator.Generate(Generator.Option.Bake);
			Current.Character.recipes.Clear();
			Current.SelectedCharacter = 0;

			List<Recipe> recipes = new List<Recipe>();
			recipes.Add(BakeChannel(output, Recipe.Component.System, Resources.system_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.System_PostHistory, Resources.post_history_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.Persona, Resources.persona_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.UserPersona, Resources.user_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.Scenario, Resources.scenario_recipe));
			recipes.AddRange(BakeGreetings(output));
			recipes.Add(BakeChannel(output, Recipe.Component.Example, Resources.example_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.Grammar, Resources.grammar_recipe));

			if (output.hasLore)
			{
				var loreRecipe = RecipeBook.CreateRecipeFromResource(Resources.lorebook_recipe, Recipe.Type.Lore, Recipe.Drawer.Lore);
				output.lorebook.Bake();
				(loreRecipe.parameters[0] as LorebookParameter).value = output.lorebook;
				recipes.Add(loreRecipe);
			}

			recipes.RemoveAll(r => r == null);
			if (recipes.Count == 0)
				return false;

			// Remove all but the main character
			if (Current.Characters.Count > 1)
				Current.Characters.RemoveRange(1, Current.Characters.Count - 1);

			Undo.Suspend();

			recipeList.RemoveAllPanels();

			for (int i = 0; i < recipes.Count; ++i)
			{
				Current.Character.AddRecipe(recipes[i]);
				recipeList.AddRecipePanel(recipes[i], false);
			}

			recipeList.ScrollToTop();
			recipeList.RefreshParameterVisibility();
			recipeList.RefreshLayout();
			recipeList.RefreshSyntaxHighlighting(false);
			Undo.Resume();
			Undo.Push(Undo.Kind.RecipeList, "Bake all");
			return true;
		}

		private bool BakeActor()
		{
			StealFocus();

			var context = Current.Character.GetContext(CharacterData.ContextType.None);
			var output = Generator.Generate(RecipeBook.WithInternal(Current.Character.recipes), Current.SelectedCharacter, context, Generator.Option.Bake | Generator.Option.Single);
			List<Recipe> recipes = new List<Recipe>();
			
			Current.Character.recipes.Clear();
			
			recipes.Add(BakeChannel(output, Recipe.Component.System, Resources.system_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.System_PostHistory, Resources.post_history_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.Persona, Resources.persona_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.UserPersona, Resources.user_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.Scenario, Resources.scenario_recipe));
			recipes.AddRange(BakeGreetings(output));
			recipes.Add(BakeChannel(output, Recipe.Component.Example, Resources.example_recipe));
			recipes.Add(BakeChannel(output, Recipe.Component.Grammar, Resources.grammar_recipe));

			if (output.hasLore)
			{
				var loreRecipe = RecipeBook.CreateRecipeFromResource(Resources.lorebook_recipe, Recipe.Type.Lore, Recipe.Drawer.Lore);
				output.lorebook.Bake();
				(loreRecipe.parameters[0] as LorebookParameter).value = output.lorebook;
				recipes.Add(loreRecipe);
			}

			recipes.RemoveAll(r => r == null);
			if (recipes.Count == 0)
				return false;

			recipeList.SuspendLayout();
			recipeList.RemoveAllPanels();

			for (int i = 0; i < recipes.Count; ++i)
			{
				Current.Character.AddRecipe(recipes[i]);
				recipeList.AddRecipePanel(recipes[i], false);
			}

			recipeList.ScrollToTop();
			recipeList.RefreshParameterVisibility();
			recipeList.ResumeLayout();
			recipeList.RefreshSyntaxHighlighting(false);

			Undo.Push(Undo.Kind.RecipeList, "Bake actor");
			return true;
		}

		private Recipe BakeChannel(Generator.Output output, Recipe.Component channel, string xmlSource)
		{
			string text = output.GetText(channel).ToBaked();
			if (string.IsNullOrWhiteSpace(text))
				return null;

			Recipe.Drawer drawer;
			switch (channel)
			{
			case Recipe.Component.System:
			case Recipe.Component.System_PostHistory:
				drawer = Recipe.Drawer.Model;
				break;
			case Recipe.Component.Persona:
			case Recipe.Component.UserPersona:
				drawer = Recipe.Drawer.Character;
				break;
			case Recipe.Component.Scenario:
				drawer = Recipe.Drawer.Story;
				break;
			case Recipe.Component.Example:
			case Recipe.Component.Grammar:
			case Recipe.Component.Greeting:
			default:
				drawer = Recipe.Drawer.Components;
				break;
			}

			Recipe recipe = RecipeBook.CreateRecipeFromResource(xmlSource, Recipe.Type.Component, drawer);
			(recipe.parameters[0] as TextParameter).value = text;
			return recipe;
		}

		private List<Recipe> BakeGreetings(Generator.Output output)
		{
			var greetings = new List<Recipe>();
			if (output.greetings != null)
			{
				foreach (var greeting in output.greetings)
				{
					string text = greeting.ToBaked();
					if (string.IsNullOrWhiteSpace(text))
						continue;

					Recipe recipe = RecipeBook.CreateRecipeFromResource(Resources.greeting_recipe, Recipe.Type.Component, Recipe.Drawer.Components);
					(recipe.parameters[0] as TextParameter).value = text;
					greetings.Add(recipe);
				}

				foreach (var greeting in output.group_greetings)
				{
					string text = greeting.ToBaked();
					if (string.IsNullOrWhiteSpace(text))
						continue;

					Recipe recipe = RecipeBook.CreateRecipeFromResource(Resources.group_greeting_recipe, Recipe.Type.Component, Recipe.Drawer.Components);
					(recipe.parameters[0] as TextParameter).value = text;
					greetings.Add(recipe);
				}
			}
			return greetings;
		}
		
		private void ReplaceNamePlaceholders(string oldName, string newName)
		{
			if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName) || oldName == newName)
				return;

			string userPlaceholder = (Current.Card.userPlaceholder ?? "").Trim();
			string characterPlaceholder = (Current.MainCharacter.spokenName ?? "").Trim();

			foreach (var character in Current.Characters)
			{
				// Text parameters
				foreach (var parameter in character.recipes.SelectMany(r => r.parameters.OfType<TextParameter>()))
				{
					if (string.IsNullOrEmpty(parameter.value) || parameter.isRaw)
						continue;

					if (string.IsNullOrWhiteSpace(characterPlaceholder) == false)
						parameter.value = Utility.ReplaceWholeWord(parameter.value, GingerString.CharacterMarker, characterPlaceholder, StringComparison.OrdinalIgnoreCase);
					if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
						parameter.value = Utility.ReplaceWholeWord(parameter.value, GingerString.UserMarker, userPlaceholder, StringComparison.OrdinalIgnoreCase);

					parameter.value = Utility.ReplaceWholeWord(parameter.value, oldName, newName, StringComparison.Ordinal);
				}

				// List parameters
				foreach (var parameter in character.recipes.SelectMany(r => r.parameters.OfType<ListParameter>()))
				{
					string sValue = Utility.ListToDelimitedString(parameter.value, ",");
					if (sValue.Contains(oldName))
					{ 
						sValue = Utility.ReplaceWholeWord(sValue, oldName, newName, StringComparison.Ordinal);
						parameter.value = new HashSet<string>(Utility.ListFromCommaSeparatedString(sValue));
					}
				}

				// Lorebooks
				foreach (var parameter in character.recipes.SelectMany(r => r.parameters.OfType<LorebookParameter>()))
				{
					var lorebook = parameter.value;
					foreach (var entry in lorebook.entries)
					{
						if (string.IsNullOrEmpty(entry.value))
							continue;

						if (string.IsNullOrWhiteSpace(characterPlaceholder) == false)
							entry.value = Utility.ReplaceWholeWord(entry.value, GingerString.CharacterMarker, characterPlaceholder, StringComparison.OrdinalIgnoreCase);
						if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
							entry.value = Utility.ReplaceWholeWord(entry.value, GingerString.UserMarker, userPlaceholder, StringComparison.OrdinalIgnoreCase);

						entry.value = Utility.ReplaceWholeWord(entry.value, oldName, newName, StringComparison.Ordinal);
					}
				}
			}
		}

		private void SetToolTip(Control control, string text)
		{
			if (this.components == null)
				this.components = new System.ComponentModel.Container();
			
			var toolTip = new ToolTip(this.components);
			toolTip.SetToolTip(control, text);
			toolTip.UseFading = false;
			toolTip.UseAnimation = false;
			toolTip.AutomaticDelay = 250;
			toolTip.AutoPopDelay = 3500;
		}

		private bool ImportCharacterJson()
		{
			int filter = AppSettings.User.LastImportCharacterFilter;
			if (filter < 0 || filter > 5)
				filter = 0;

			// Open file...
			importFileDialog.Title = Resources.cap_import_character;
			importFileDialog.Filter = "All supported types|*.png;*.json;*.charx;*.yaml|PNG files|*.png|JSON files|*.json|CHARX files|*.charx|YAML files|*.yaml";
			importFileDialog.FilterIndex = filter;
			importFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			importFileDialog.FileName = "";
			var result = importFileDialog.ShowDialog();
			if (result != DialogResult.OK)
				return false;

			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(importFileDialog.FileName);
			AppSettings.User.LastImportCharacterFilter = importFileDialog.FilterIndex;

			if (ConfirmSave(Resources.cap_import_character) == false)
				return false;

			return ImportCharacter(importFileDialog.FileName);
		}

		private bool ImportCharacter(string filename)
		{
			string ext = (Path.GetExtension(filename) ?? "").ToLowerInvariant();

			int jsonErrors = 0;
			FileUtil.Error error;
			if (ext == ".png")
				error = FileUtil.ImportCharacterFromPNG(filename, out jsonErrors, FileUtil.Format.SillyTavernV2 | FileUtil.Format.SillyTavernV3 | FileUtil.Format.Faraday);
			else if (ext == ".json")
				error = FileUtil.ImportCharacterJson(filename, out jsonErrors);
			else if (ext == ".charx")
				error = FileUtil.ImportCharacterFromPNG(filename, out jsonErrors, FileUtil.Format.SillyTavernV3);
			else if (ext == ".yaml")
			{
				error = FileUtil.ImportCharacterJson(filename, out jsonErrors);
					
				// Load portrait image (if any)
				if (error == FileUtil.Error.NoError)
				{
					var pngFilename = Path.Combine(Path.GetDirectoryName(filename), string.Concat(Path.GetFileNameWithoutExtension(filename), ".png"));

					Image portraitImage;
					Current.Card.LoadPortraitImageFromFile(pngFilename, out portraitImage);
				}
			}
			else
				error = FileUtil.Error.UnrecognizedFormat;

			if (error == FileUtil.Error.FileNotFound)
			{
				MessageBox.Show(Resources.error_file_not_found, Resources.cap_import_character, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			else if (error == FileUtil.Error.FileReadError)
			{
				MessageBox.Show(Resources.error_read_json, Resources.cap_import_character, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			else if (error == FileUtil.Error.InvalidJson)
			{
				MessageBox.Show(Resources.error_invalid_json_file, Resources.cap_import_character, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			else if (error == FileUtil.Error.UnrecognizedFormat)
			{
				MessageBox.Show(Resources.error_unrecognized_character_format, Resources.cap_import_character, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			FileMutex.Release();

			if (jsonErrors > 0)
				MessageBox.Show(string.Format(Resources.msg_import_character_with_errors, jsonErrors), Resources.cap_import_character, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return true;
		}

		private bool ImportLorebook(bool bCopyToFolder)
		{
			// Open file...
			importFileDialog.Title = Resources.cap_import_lorebook;
			importFileDialog.Filter = "All supported types|*.json;*.png;*.csv;*.charx|JSON files|*.json|PNG files|*.png|CHARX files|*.charx|CSV files|*.csv";
			importFileDialog.FilterIndex = AppSettings.User.LastImportLorebookFilter;
			importFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.ContentPath("Lorebooks");
			var result = importFileDialog.ShowDialog();
			if (result != DialogResult.OK)
				return false;

			AppSettings.User.LastImportLorebookFilter = importFileDialog.FilterIndex;
			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(importFileDialog.FileName);

			string ext = (Path.GetExtension(importFileDialog.FileName) ?? "").ToLowerInvariant();

			Lorebook lorebook = null;
			if (ext == ".json")
			{
				lorebook = new Lorebook();
				int nErrors;
				var loadError = lorebook.LoadFromJson(importFileDialog.FileName, out nErrors);
				switch (loadError)
				{
				case Lorebook.LoadError.NoError:
					if (nErrors > 0)
						MessageBox.Show(string.Format(Resources.msg_import_lorebook_with_errors, nErrors), Resources.cap_import_lorebook, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					break;
				case Lorebook.LoadError.UnknownFormat:
					MessageBox.Show(Resources.error_unrecognized_lorebook_format, Resources.cap_import_lorebook, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				case Lorebook.LoadError.NoData:
					MessageBox.Show(Resources.error_no_lorebook, Resources.cap_import_lorebook, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				case Lorebook.LoadError.FileError:
					MessageBox.Show(Resources.error_load_lorebook, Resources.cap_import_lorebook, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				case Lorebook.LoadError.InvalidJson:
					MessageBox.Show(Resources.error_invalid_json_file, Resources.cap_import_lorebook, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}
			}
			else if (ext == ".png" || ext == ".charx")
			{
				int jsonErrors;
				FileUtil.ImportResult importResult;
				var error = FileUtil.Import(importFileDialog.FileName, out importResult);
				if (error == FileUtil.Error.FallbackError)
					error = FileUtil.Error.NoError; // We don't care about this error in this context

				if (error == FileUtil.Error.NoError)
				{
					if (importResult.tavernDataV3 != null // SillyTavern v3
						&& importResult.tavernDataV3.data.character_book != null)
					{
						lorebook = Lorebook.FromTavernBook(importResult.tavernDataV3.data.character_book);
						lorebook.name = string.Concat(importResult.tavernDataV3.data.name, " lorebook");
					}
					else if (importResult.tavernDataV2 != null // SillyTavern v2
						&& importResult.tavernDataV2.data.character_book != null)
					{
						lorebook = Lorebook.FromTavernBook(importResult.tavernDataV2.data.character_book);
						lorebook.name = string.Concat(importResult.tavernDataV2.data.name, " lorebook");
					}
					else if (importResult.faradayData != null // Faraday
						&& importResult.faradayData.data.loreItems != null)
					{
						lorebook = Lorebook.FromFaradayBook(importResult.faradayData.data.loreItems);
						lorebook.name = string.Concat(importResult.faradayData.data.displayName, " lorebook");
					}
				}
				else if (error == FileUtil.Error.FileNotFound)
				{
					MessageBox.Show(Resources.error_file_not_found, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}
				else if (error == FileUtil.Error.FileReadError)
				{
					MessageBox.Show(Resources.error_read_json, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}
				else if (error == FileUtil.Error.UnrecognizedFormat)
				{
					MessageBox.Show(Resources.error_unrecognized_character_format, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}
				else
				{
					MessageBox.Show(Resources.error_read_data, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}
			}
			else if (ext == ".csv")
			{
				lorebook = new Lorebook();
				if (lorebook.LoadFromCsv(importFileDialog.FileName) == false)
				{
					MessageBox.Show(Resources.error_unrecognized_lorebook_format, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}
			} else
			{
				MessageBox.Show(Resources.error_unrecognized_lorebook_format, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			if (lorebook == null || lorebook.isEmpty)
			{
				MessageBox.Show(Resources.error_no_lorebook, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			string srcFilename = importFileDialog.FileName;
			string destFilename = Utility.ContentPath("Lorebooks", Path.GetFileName(srcFilename));

			if (string.Compare(srcFilename, destFilename, true) == 0)
				bCopyToFolder = false;

			if (bCopyToFolder)
			{

				if (importFileDialog.FilterIndex == 3 || importFileDialog.FilterIndex == 4)
					destFilename = Utility.ChangeFileExtension(destFilename, "json");

				if ((importFileDialog.FilterIndex == 1 || importFileDialog.FilterIndex == 2) // json or csv
					&& (File.Exists(destFilename) == false
						|| MessageBox.Show(Resources.msg_overwrite_lorebook, Resources.cap_overwrite_lorebook, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes))
				{ 
					try
					{
						if (Directory.Exists(Utility.ContentPath("Lorebooks")) == false)
							Directory.CreateDirectory(Utility.ContentPath("Lorebooks"));

						File.Copy(srcFilename, destFilename, true);
					}
					catch (Exception e)
					{
						MessageBox.Show(e.Message, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
						return false;
					}
				}
				else if ((importFileDialog.FilterIndex == 3 || importFileDialog.FilterIndex == 4) // png
					&& (File.Exists(destFilename) == false
						|| MessageBox.Show(Resources.msg_overwrite_lorebook, Resources.cap_overwrite_lorebook, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes))
				{
					try
					{
						if (Directory.Exists(Utility.ContentPath("Lorebooks")) == false)
							Directory.CreateDirectory(Utility.ContentPath("Lorebooks"));
					}
					catch (Exception e)
					{
						MessageBox.Show(e.Message, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
						return false;
					}

					// Save to Lorebooks folder
					if (FileUtil.ExportTavernV2Lorebook(lorebook, destFilename) == false)
					{
						MessageBox.Show(Resources.error_save_character_card, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
						return false;
					}
				}

				Lorebooks.LoadLorebooks();
			}

			Cursor = Cursors.WaitCursor;
			SetStatusBarMessage(Resources.status_refreshing_list);

			// Add to recipe list
			var instance = Current.AddLorebook(lorebook);
			recipeList.Suspend();
			recipeList.DisableRedrawAndDo(() => {
				var pastedPanel = recipeList.AddRecipePanel(instance, false);
				pastedPanel.Focus();
				recipeList.ScrollToPanel(pastedPanel);
			});
			recipeList.Resume();
			recipeList.Invalidate(true);
			Undo.Push(Undo.Kind.RecipeAddRemove, "Add lorebook");

			Cursor = Cursors.Default;
			ClearStatusBarMessage();
			return true;
		}

		private void ExportCharacter()
		{
			ConfirmName();

			string filename = null;
			if (string.IsNullOrWhiteSpace(Current.Card.name) == false)
				filename = Current.Card.name;
			else if (string.IsNullOrWhiteSpace(Current.Character.spokenName) == false)
				filename = Current.Character.spokenName;

			int filter = AppSettings.User.LastExportCharacterFilter;
			if (filter < 0 || filter > 9)
				filter = 0; 

			if (string.IsNullOrEmpty(filename) == false)
			{
				if (filter == 5
					|| filter == 6
					|| filter == 7) // png
					filename = string.Concat(filename, ".png");
				else if (filter == 8) // charx
					filename = string.Concat(filename, ".charx");
				else if (filter == 9) // yaml
					filename = string.Concat(filename, ".yaml");
				else // json
					filename = string.Concat(filename, ".json");
			}

			// Save as...
			exportFileDialog.Title = Resources.cap_export_character;
			exportFileDialog.Filter = "Character Card V2 JSON|*.json|Character Card V3 JSON|*.json|Agnai Character JSON|*.json|PygmalionAI Character JSON|*.json|Character Card V2 PNG|*.png|Character Card V3 PNG|*.png|Backyard AI PNG|*.png|CharX file|*.charx|Text generation web ui YAML|*.yaml";
			exportFileDialog.FileName = Utility.ValidFilename(filename);
			exportFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			exportFileDialog.FilterIndex = filter;
			exportFileDialog.OverwritePrompt = true;
			var result = exportFileDialog.ShowDialog();
			if (result != DialogResult.OK || string.IsNullOrWhiteSpace(exportFileDialog.FileName))
				return;

			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(exportFileDialog.FileName);
			AppSettings.User.LastExportCharacterFilter = exportFileDialog.FilterIndex;

			FileUtil.FileType fileType;
			switch (exportFileDialog.FilterIndex)
			{
			case 1: // Tavern V2
				fileType = FileUtil.FileType.TavernV2 | FileUtil.FileType.Json;
				break;
			case 2: // Tavern V3
				fileType = FileUtil.FileType.TavernV3 | FileUtil.FileType.Json;
				break;
			case 3: // Agnaistic
				fileType = FileUtil.FileType.Agnaistic | FileUtil.FileType.Json;
				break;
			case 4: // Pygmalion / CAI
				fileType = FileUtil.FileType.Pygmalion | FileUtil.FileType.Json;
				break;
			case 5: // PNGv2
				fileType = FileUtil.FileType.TavernV2 | FileUtil.FileType.Png;
				break;
			case 6: // PNGv3
				fileType = FileUtil.FileType.TavernV3 | FileUtil.FileType.Png;
				break;
			case 7: // Faraday PNG
				fileType = FileUtil.FileType.Faraday | FileUtil.FileType.Png;
				break;
			case 8: // CharX
				fileType = FileUtil.FileType.TavernV3 | FileUtil.FileType.CharX;
				break;
			case 9: // Text Generation WebUI Yaml
				fileType = FileUtil.FileType.TextGenWebUI | FileUtil.FileType.Yaml;
				break;
			default:
				fileType = FileUtil.FileType.Unknown;
				break;
			}

			// Open in another instance?
			if (fileType.Contains(FileUtil.FileType.Png) && FileMutex.CanAcquire(exportFileDialog.FileName) == false)
			{
				MessageBox.Show(string.Format(Resources.error_already_open, Path.GetFileName(exportFileDialog.FileName)), Resources.cap_export_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (FileUtil.Export(exportFileDialog.FileName, fileType) == false)
			{
				MessageBox.Show(Resources.error_write_json, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ExportLorebook(Generator.Output output, bool saveLocal)
		{
			var lorebook = output.lorebook;
			if (output.hasLore == false)
			{
				MessageBox.Show(Resources.error_empty_lore, Resources.cap_export_lorebook, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			string filename = null;
			if (string.IsNullOrWhiteSpace(Current.Card.name) == false)
				filename = string.Concat(Current.Card.name, " Lorebook");
			else if (string.IsNullOrWhiteSpace(Current.Character.spokenName) == false)
				filename = string.Concat(Current.Character.spokenName, " Lorebook");

			if (string.IsNullOrEmpty(filename) == false)
			{
				if (AppSettings.User.LastExportLorebookFilter == 4) // csv
					filename = string.Concat(filename, ".csv");
				else // json
					filename = string.Concat(filename, ".json");
			}

			exportFileDialog.Title = Resources.cap_export_lorebook;
			exportFileDialog.Filter = "SillyTavern World Book|*.json|Agnai Character Book|*.json|Character Card V3 Lorebook|*.json|Comma separated values|*.csv";
			exportFileDialog.FileName = Utility.ValidFilename(filename);
			if (saveLocal)
				exportFileDialog.InitialDirectory = Utility.ContentPath("Lorebooks");
			else
				exportFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.ContentPath("Lorebooks");
			exportFileDialog.FilterIndex = AppSettings.User.LastExportLorebookFilter;
			exportFileDialog.OverwritePrompt = true;

			var result = exportFileDialog.ShowDialog();
			if (result != DialogResult.OK || string.IsNullOrWhiteSpace(exportFileDialog.FileName))
				return;

			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(exportFileDialog.FileName);
			AppSettings.User.LastExportLorebookFilter = exportFileDialog.FilterIndex;

			if (string.IsNullOrEmpty(lorebook.name))
				lorebook.name = Path.GetFileNameWithoutExtension(exportFileDialog.FileName);

			if (exportFileDialog.FilterIndex == 1) // V2
			{
				if (FileUtil.ExportTavernV2Lorebook(lorebook, exportFileDialog.FileName))
					return; // Success
			} 
			else if (exportFileDialog.FilterIndex == 2) // Agnai lorebook
			{
				if (FileUtil.ExportAgnaisticLorebook(lorebook, exportFileDialog.FileName))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == 3) // V3
			{
				if (FileUtil.ExportTavernV3Lorebook(lorebook, exportFileDialog.FileName))
					return; // Success
			} 
			else if (exportFileDialog.FilterIndex == 4) // CSV
			{
				if (FileUtil.ExportLorebookCsv(lorebook, exportFileDialog.FileName))
					return; // Success
			}
			MessageBox.Show(Resources.error_write_json, Resources.cap_export_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void ConvertCharacterNameMarkers(bool bEnabled)
		{
			string characterPlaceholder = (Current.Character.namePlaceholder ?? "").Trim();
			string userPlaceholder = (Current.Card.userPlaceholder ?? "").Trim();

			if (bEnabled)
			{
				foreach (var parameter in Current.Character.recipes.SelectMany(r => r.parameters).OfType<TextParameter>())
				{
					StringBuilder sb = new StringBuilder(parameter.value);
					if (string.IsNullOrWhiteSpace(characterPlaceholder) == false)
						Utility.ReplaceWholeWord(sb, GingerString.CharacterMarker, characterPlaceholder, StringComparison.OrdinalIgnoreCase);
					if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
						Utility.ReplaceWholeWord(sb, GingerString.UserMarker, userPlaceholder, StringComparison.OrdinalIgnoreCase);
					parameter.value = sb.ToString();
				}

				foreach (var parameter in Current.Character.recipes.SelectMany(r => r.parameters).OfType<LorebookParameter>())
				{
					var lorebook = parameter.value;
					foreach (var entry in lorebook.entries)
					{
						StringBuilder sb = new StringBuilder(entry.value);
						if (string.IsNullOrWhiteSpace(characterPlaceholder) == false)
							Utility.ReplaceWholeWord(sb, GingerString.CharacterMarker, characterPlaceholder, StringComparison.OrdinalIgnoreCase);
						if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
							Utility.ReplaceWholeWord(sb, GingerString.UserMarker, userPlaceholder, StringComparison.OrdinalIgnoreCase);
						entry.value = sb.ToString();
					}
				}
			}
			else
			{
				foreach (var parameter in Current.Character.recipes.SelectMany(r => r.parameters).OfType<TextParameter>())
				{
					StringBuilder sb = new StringBuilder(parameter.value);
					if (string.IsNullOrWhiteSpace(characterPlaceholder) == false)
						Utility.ReplaceWholeWord(sb, characterPlaceholder, GingerString.CharacterMarker, StringComparison.Ordinal);
					if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
						Utility.ReplaceWholeWord(sb, userPlaceholder, GingerString.UserMarker, StringComparison.Ordinal);
					parameter.value = sb.ToString();
				}

				foreach (var parameter in Current.Character.recipes.SelectMany(r => r.parameters).OfType<LorebookParameter>())
				{
					var lorebook = parameter.value;
					foreach (var entry in lorebook.entries)
					{
						StringBuilder sb = new StringBuilder(entry.value);
						if (string.IsNullOrWhiteSpace(characterPlaceholder) == false)
							Utility.ReplaceWholeWord(sb, characterPlaceholder, GingerString.CharacterMarker, StringComparison.Ordinal);
						if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
							Utility.ReplaceWholeWord(sb, userPlaceholder, GingerString.UserMarker, StringComparison.Ordinal);
						entry.value = sb.ToString();
					}
				}
			}
			recipeList.RefreshAllParameters();
			recipeList.RefreshSyntaxHighlighting(true);
			sidePanel.RefreshValues();

			Current.IsDirty = true;
		}

		private void OpenFileInNewWindow(string fileName)
		{
			try
			{
				var location = System.Reflection.Assembly.GetEntryAssembly().Location;
				var directory = Path.GetDirectoryName(location);
				var executable = Path.Combine(directory,
				  System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe");

				var argument = fileName;
				argument = argument.Replace("/", "\\");
				if (argument.Contains(" "))
					argument = string.Concat("\"", argument, "\"");

				var processInfo = new System.Diagnostics.ProcessStartInfo() {
					FileName = executable,
					Arguments = argument,
				};
				System.Diagnostics.Process.Start(processInfo);
			}
			catch
			{
				MessageBox.Show(Resources.error_launch_text_editor, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		}

		private void OnSaveAsSnippet(object sender, EventArgs e)
		{
			var panel = sender as RecipePanel;
			if (panel == null)
				return;

			var recipe = panel.recipe;
			var output = Generator.Generate(recipe, Generator.Option.Snippet);

			using (var dlg = new CreateSnippetDialog())
			{
				dlg.SetOutput(output);
				if (dlg.ShowDialog() == DialogResult.OK)
					RecipeMaker.CreateSnippet(dlg.FileName, dlg.SnippetName, dlg.Output);
			}
		}

		private void OnSaveAsRecipe(object sender, EventArgs e)
		{
			var panel = sender as RecipePanel;
			if (panel == null)
				return;

			var recipe = panel.recipe;

			var context = Current.Character.GetContextForRecipe(recipe);
			var output = Generator.GenerateSeparately(RecipeBook.WithInternal(recipe), Current.SelectedCharacter, context, Generator.Option.Snippet | Generator.Option.Single);

			IEnumerable<StringHandle> flags = recipe.flags;
			if (recipe.isComponent || recipe.isInternal)
				flags = null;

			using (var dlg = new CreateRecipeDialog())
			{
				dlg.RecipeName = string.Concat(recipe.origName.Trim(), " (Copy)");
				dlg.Category = recipe.category;
				dlg.FromRecipe = true;
				if (dlg.ShowDialog() == DialogResult.OK)
					RecipeMaker.CreateRecipe(dlg.FileName, dlg.RecipeName, dlg.RecipeTitle, dlg.Category, dlg.RecipeXml, output, flags);
			}
		}

		public static void EditRecipeSource(Recipe recipe)
		{
			if (string.IsNullOrEmpty(recipe.filename))
				return;

			LaunchTextEditor.OpenTextFile(recipe.filename);
		}

		public bool ImportExternalRecipe(Recipe recipe)
		{
			// Path
			string recipeName = recipe.name;
			string recipeFilename = recipeName.Replace("//", "%%SLASH%%");
			var lsPath = recipeFilename.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.Select(s => s.Replace("%%SLASH%%", "/"))
				.ToList();

			recipeFilename = lsPath[lsPath.Count - 1];
			var path = lsPath.Take(lsPath.Count - 1).ToArray();

			if (path.Length > 0)
				recipeName = string.Concat(Utility.ListToDelimitedString(path, "/"), "/", recipeFilename);
			else
				recipeName = recipeFilename;

			recipeFilename = Utility.ValidFilename(recipeFilename).ToLowerInvariant();

			saveFileDialog.Title = Resources.cap_import_external_recipe;
			saveFileDialog.Filter = "Ginger recipe|*.xml";
			saveFileDialog.InitialDirectory = Utility.ContentPath("Recipes");
			saveFileDialog.FileName = string.Format("{0}.recipe.xml", recipeFilename);
			if (saveFileDialog.ShowDialog() != DialogResult.OK)
				return false;

			string destFilename = saveFileDialog.FileName;

			XmlDocument xmlDoc = new XmlDocument();
			XmlNode rootNode = xmlDoc.CreateElement("Ginger");
			rootNode.AddAttribute("version", 1);
			xmlDoc.AppendChild(rootNode);

			recipe.SaveToXml(rootNode);

			// Save to file
			try
			{
				XmlWriterSettings settings = new XmlWriterSettings();
				settings.OmitXmlDeclaration = true;
				settings.Indent = true;
				settings.IndentChars = "\t";
				settings.NewLineChars = "\r\n";

				StringBuilder sbXml = new StringBuilder();
				using (var stringWriter = new StringWriterUTF8(sbXml))
				{
					using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
					{
						xmlDoc.Save(xmlWriter);
					}
				}

				File.WriteAllText(destFilename, sbXml.ToString(), Encoding.UTF8);
			}
			catch
			{
				return false;
			}
			finally
			{
				RecipeBook.LoadRecipes();
				Current.ReloadRecipes();
				RefreshRecipeList();
			}
			return true;
		}

		public void ReplaceExternalRecipe(Recipe recipe, RecipeTemplate replacement)
		{
			var newRecipe = replacement.Instantiate();
			Recipe.CopyParameterValues(recipe, newRecipe);
			if (recipeList.ReplaceRecipe(recipe, newRecipe))
			{
				Current.IsDirty = true;
				Undo.Push(Undo.Kind.RecipeList, "Replace recipe");
			}
		}

		private bool LoadNotes(string cardFilename)
		{
			string notesFilename = Path.Combine(Path.GetDirectoryName(cardFilename), Path.GetFileNameWithoutExtension(cardFilename) + ".txt");

			if (File.Exists(notesFilename) == false)
			{
				userNotes.Clear();
				return false;
			}

			var notes = Utility.LoadTextFile(notesFilename);
			if (notes == null)
			{
				userNotes.Clear();
				return false;
			}

			userNotes.Text = notes;
			return true;
		}

		private void SaveNotes(string cardFilename)
		{
			string notesFilename = Path.Combine(Path.GetDirectoryName(cardFilename), Path.GetFileNameWithoutExtension(cardFilename) + ".txt");

			if (string.IsNullOrWhiteSpace(userNotes.Text))
			{
				// Erase notes file
				try
				{
					if (File.Exists(notesFilename))
						File.Delete(notesFilename);
				}
				catch
				{}
				return;
			}

			try
			{
				var intermediateFilename = Path.GetTempFileName();
				File.WriteAllText(intermediateFilename, userNotes.Text, Encoding.UTF8);

				// Rename Temporaty file to Target file
				if (File.Exists(notesFilename))
					File.Delete(notesFilename);
				File.Move(intermediateFilename, notesFilename);
			}
			catch
			{
				MessageBox.Show(Resources.error_save_notes, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		private void OnSaveLorebook(object sender, EventArgs e)
		{
			var panel = sender as RecipePanel;
			if (panel == null)
				return;

			var recipe = panel.recipe;
			var output = Generator.Generate(recipe, Generator.Option.Export);
			ExportLorebook(output, true);
			Lorebooks.LoadLorebooks();
		}

		private bool Save(string filename = null)
		{
			if (string.IsNullOrEmpty(filename))
				return SaveAs();
 
			// Ensure text parameters get an opportunity to save
			var focused = GetFocusedControl();
			if (focused is TextBoxBase)
			{
				StealFocus();
				focused.Focus();
			}

			ConfirmName();

			// Opened in other instance?
			if (FileMutex.CanAcquire(filename) == false)
			{
				MessageBox.Show(string.Format(Resources.error_already_open, Path.GetFileName(filename)), Resources.cap_save_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			if (Current.Card.creationDate == null)
				Current.Card.creationDate = DateTime.UtcNow;

			// Only write ccv3 if necessary
			var formats = FileUtil.Format.Ginger | FileUtil.Format.Faraday | FileUtil.Format.SillyTavernV2;
			if (Current.ContainsV3Data)
				formats |= FileUtil.Format.SillyTavernV3;

			// Linking: Check filename
			if (Current.HasLink)
			{
				if (string.IsNullOrEmpty(Current.Link.filenameHash))
					Current.Link.filenameHash = filename;
				else if (Current.Link.CompareFilename(filename) == false)
					Current.Unlink(); // Saving as new
			}

			// Linking: Write to Backyard database?
			bool bShouldAutosave =
				AppSettings.BackyardLink.Autosave
				&& Backyard.ConnectionEstablished
				&& Current.HasActiveLink
				&& Current.IsLinkDirty;
			
			Backyard.Error autosaveError = Backyard.Error.NoError;

			if (bShouldAutosave)
			{
				// Save changes to Backyard
				autosaveError = UpdateCharacterInBackyard();

				if (autosaveError == Backyard.Error.CancelledByUser)
					return false; // User clicked 'cancel'
				else if (autosaveError == Backyard.Error.DismissedByUser)
					bShouldAutosave = false; // User clicked 'no'
				else if (autosaveError == Backyard.Error.NoError)
					Current.IsLinkDirty = false;
			}

			if (FileUtil.Export(filename, (Image)(Current.Card.portraitImage ?? DefaultPortrait.Image), formats))
			{
				SaveNotes(filename);

				FileMutex.Acquire(filename);

				Current.IsDirty = false;
				Current.IsFileDirty = false;
				Current.Filename = filename;
				RefreshTitle();

				MRUList.AddCurrent();

				SetStatusBarMessage(Resources.status_file_save, Constants.StatusBarMessageInterval);

				if (bShouldAutosave)
				{
					if (autosaveError == Backyard.Error.NoError)
						SetStatusBarMessage(Resources.status_link_save_file_and_link, Constants.StatusBarMessageInterval);
					else if (autosaveError == Backyard.Error.NotFound)
					{
						MessageBox.Show(Resources.error_link_update_character_not_found, Resources.cap_link_save_character, MessageBoxButtons.OK, MessageBoxIcon.Warning);
						BreakLink(true);
					}
					else if (autosaveError != Backyard.Error.NoError)
					{
						MessageBox.Show(Resources.error_link_autosave, Resources.cap_link_save_character, MessageBoxButtons.OK, MessageBoxIcon.Warning);
						Backyard.Disconnect();
					}
				}
				return true;
			}
			else
			{
				if (bShouldAutosave && autosaveError == Backyard.Error.NoError) // Notify user the auto save worked
					SetStatusBarMessage(Resources.status_link_saved, Constants.StatusBarMessageInterval);

				MessageBox.Show(Resources.error_save_character_card, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}

		private bool SaveAs()
		{
			string filename = string.Concat(Utility.FirstNonEmpty(Current.Card.name, Current.Character.spokenName, Constants.DefaultCharacterName), ".png");

			// Save as...
			saveFileDialog.Filter = "Ginger Character Card|*.png";
			saveFileDialog.InitialDirectory = AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			saveFileDialog.FileName = Utility.ValidFilename(filename);
			var result = saveFileDialog.ShowDialog();
			if (result != DialogResult.OK || string.IsNullOrWhiteSpace(saveFileDialog.FileName))
				return false;

			AppSettings.Paths.LastCharacterPath = Path.GetDirectoryName(saveFileDialog.FileName);
			return Save(saveFileDialog.FileName);
		}

		private bool SaveIncremental()
		{
			if (string.IsNullOrEmpty(Current.Filename))
				return SaveAs();

			string prevVersionString = Current.Card.versionString;

			try
			{
				// Increment filename number	
				string filename = Path.GetFileNameWithoutExtension(Current.Filename);
				if (filename.Length > 0)
				{
					int pos = filename.Length - 1;
					while (pos > 0 && char.IsDigit(filename[pos]))
						--pos;
					pos++;
					int number;
					if (int.TryParse(filename.Substring(pos), out number))
					{
						// Card 2.png -> Card 3.png
						filename = string.Concat(filename.Remove(pos), (number + 1).ToString());
					}
					else
					{
						// Card.png -> Card 2.png
						filename = string.Concat(filename, " 2");
					}
				}
				else
				{
					// .png -> 1.png
					filename = "1";
				}

				filename = Path.Combine(Path.GetDirectoryName(Current.Filename), string.Concat(filename, ".png"));
				
				// Confirm overwrite
				if (File.Exists(filename))
				{
					if (MessageBox.Show(string.Format(Resources.msg_incremental_file_exists, Path.GetFileName(filename)), Resources.cap_confirm, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
						return false;
				}

				// Increment version number
				if (string.IsNullOrWhiteSpace(Current.Card.versionString))
				{
					Current.Card.versionString = "1.0";
				}
				else
				{
					VersionNumber version;
					if (VersionNumber.TryParse(Current.Card.versionString, out version))
					{
						version.Minor += 1;
						Current.Card.versionString = version.ToString();
					}
				}

				if (Save(filename))
				{
					_bShouldRefreshSidePanel = true;
					return true;
				}
				else
				{
					// Restore
					Current.Card.versionString = prevVersionString;
					return false;
				}
			}
			catch
			{
				// Restore
				Current.Card.versionString = prevVersionString;

				MessageBox.Show(Resources.error_save_character_card, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}

		private bool ConfirmSave(string caption)
		{
			if (Current.IsFileDirty == false)
				return true; // No changes

			var mr = MessageBox.Show(string.Format(Resources.msg_save_changes, Current.CardName), caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
			if (mr == DialogResult.Cancel)
				return false;
			if (mr == DialogResult.No)
				return true;

			return Save(Current.Filename);
		}

		private bool ConfirmSaveBeforeExit()
		{
			if (Current.IsFileDirty == false)
				return true; // No changes

			var mr = MessageBox.Show(string.Format(Resources.msg_save_before_quit, Current.CardName), Resources.cap_exit_app, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
			if (mr == DialogResult.Cancel)
				return false;
			if (mr == DialogResult.No)
				return true;

			return Save(Current.Filename);
		}

		private void ConfirmName()
		{
			// If the player is in the middle of editing the name, ensure it gets stored
			sidePanel.ForceCommitName();
		}

		private void OnFind(object sender, FindDialog.FindEventArgs e)
		{
			if (string.IsNullOrEmpty(e.match))
				return;

			if (tabControl.SelectedIndex != 0)
				tabControl.SelectedIndex = 0;

			IEnumerable<Searchable> lsSearchables = recipeList.GetSearchables().Where(s => s.instance.Enabled);
			if (e.reverse)
				lsSearchables = lsSearchables.Reverse();

			var searchables = lsSearchables.ToArray();

			if (searchables.Length == 0)
			{
				// Nothing to search
				MessageBox.Show(Resources.msg_no_match, Resources.cap_find, MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			int offset = 0;
			int idxFocused = 0;

			TextBoxBase focused = GetFocusedControl() as TextBoxBase;
			if (focused != null)
			{
				idxFocused = Array.FindIndex(searchables, s => s.instance.SearchableControl == focused);
				if (idxFocused == -1)
					idxFocused = 0;

				if (focused.SelectionStart >= 0 && focused.Text.Length >= focused.SelectionStart + e.match.Length)
				{
					var selectedText = focused.Text.Substring(focused.SelectionStart, e.match.Length);
					if (string.Compare(e.match, selectedText, !e.matchCase) == 0)
					{
						if (!e.reverse)
							offset = focused.SelectionStart + 1;
						else
						{
							offset = focused.SelectionStart - 1;
							if (offset < 0)
								idxFocused++;
						}
					}
					else
						offset = focused.SelectionStart;
				}
			}

			for (int i = 0; i < searchables.Length + 1; ++i) // +1 Search the first textbox again as we wrap around.
			{
				int index = (idxFocused + i) % searchables.Length;
				var searchable = searchables[index];
				int find = searchable.instance.Find(e.match, e.matchCase, e.wholeWord, e.reverse, i == 0 ? offset : -1);
				if (find != -1)
				{
					if (searchable.panel.Collapsed && searchable.panel.CanExpand)
					{
						searchable.panel.Collapsed = false;
						recipeList.RefreshLayout();
						recipeList.ScrollToPanel(searchable.panel);
					}

					searchable.instance.FocusAndSelect(find, e.match.Length);
					return; // Success
				}
			}

			MessageBox.Show(Resources.msg_no_match, Resources.cap_find, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		public static void HideFindDialog()
		{
			if (instance._findDialog != null)
				instance._findDialog.Hide();
		}

		public void MergeLore()
		{
			Recipe primaryRecipe = null;
			var remove = new List<Recipe>();

			foreach (var recipe in Current.Character.recipes)
			{
				if (recipe.isLorebook == false)
					continue;

				if (primaryRecipe == null)
					primaryRecipe = recipe;
				else
					remove.Add(recipe);
			}

			var lorebookParameter = primaryRecipe.parameters.OfType<LorebookParameter>().FirstOrDefault();
			if (lorebookParameter == null)
				return; // Error

			var lorebook = lorebookParameter.value;

			recipeList.Suspend();
			recipeList.SuspendLayout();

			foreach (var recipe in remove)
			{
				// Copy entries
				lorebook.entries.AddRange(recipe.parameters.OfType<LorebookParameter>().SelectMany(p => p.value.entries));
				Current.Character.RemoveRecipe(recipe);
				recipeList.RemoveRecipe(recipe, false);
			}

			// Clear out empties
			for (int i = lorebook.entries.Count - 1; i >= 0; --i)
			{
				if (lorebook.entries[i].isEmpty)
					lorebook.entries.RemoveAt(i);
			}

			lorebook.Reindex(true);

			recipeList.ResumeLayout(false);
			recipeList.Resume();
			recipeList.ScrollToRecipe(primaryRecipe);
			
			var panel = recipeList.GetRecipePanel(primaryRecipe);
			if (panel != null)
			{
				foreach (var lorebookPanel in panel.parameterPanels.OfType<LoreBookParameterPanel>())
					lorebookPanel.RefreshValue();
				panel.RefreshParameterLayout();
			}
			recipeList.PerformLayout();

			Undo.Push(Undo.Kind.RecipeList, "Merge lorebooks");
		}

		public static bool EnableSpellChecking(bool enable)
		{
			AppSettings.Settings.SpellChecking = enable;

			if (AppSettings.Settings.SpellChecking && ChangeSpellingLanguage(AppSettings.Settings.Dictionary))
			{
				var spellChecked = instance.recipeList.FindAllControlsOfType<TextBoxBase>().OfType<ISpellChecked>();
				foreach (var control in spellChecked)
					control.EnableSpellCheck(true);
				return true;
			}
			else
			{
				var spellChecked = instance.recipeList.FindAllControlsOfType<TextBoxBase>().OfType<ISpellChecked>();
				foreach (var control in spellChecked)
					control.EnableSpellCheck(false);
				return false;
			}
		}

		public static void RefreshSpellChecking(bool bRehighlight = true)
		{
			if (SpellChecker.IsInitialized && AppSettings.Settings.SpellChecking)
			{
				var spellChecked = instance.recipeList.FindAllControlsOfType<TextBoxBase>().OfType<ISpellChecked>();
				foreach (var control in spellChecked)
					control.SpellCheck(true, bRehighlight);
			}
		}

		public static bool ChangeSpellingLanguage(string lang)
		{
			if (SpellChecker.Initialize(lang))
			{
				AppSettings.Settings.Dictionary = lang;
			}
			else
			{
				MessageBox.Show(Resources.error_load_dictionary, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			// Re-check spelling
			if (AppSettings.Settings.SpellChecking)
			{
				RefreshSpellChecking();
			}
			return true;
		}

		public static void EnableAutoWrap(bool autoWrap)
		{
			AppSettings.Settings.AutoBreakLine = autoWrap;

			var panels = instance.recipeList.FindAllControlsOfType<Control>().OfType<IFlexibleParameterPanel>();
			foreach (var panel in panels)
				panel.RefreshLineWidth();
		}

		public static void LockRecipeList()
		{
			instance.recipeList.SuspendLayout();
			instance.recipeList.Suspend();
		}

		public static void ReleaseRecipeList()
		{
			instance.recipeList.Resume();
			instance.recipeList.ResumeLayout(false);
		}

		public static void SetStatusBarMessage(string message, int durationMS = 0)
		{
			instance.statusBarMessage.Text = message;
			instance.statusBar.Refresh();

			if (durationMS > 0)
			{
				instance._statusbarTimer.Stop();
				instance._statusbarTimer.Interval = durationMS;
				instance._statusbarTimer.Start();
			}
		}

		public static void ClearStatusBarMessage()
		{
			instance.statusBarMessage.Text = null;
		}

		private void OnStatusBarTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			statusBarMessage.Text = null;
		}

		private bool ChangeAppLanguage(string lang)
		{
			AppSettings.Settings.Locale = lang;

			// Reload everything
			RecipeBook.LoadRecipes();
			Lorebooks.LoadLorebooks();
			bool hasOutdatedRecipes = Current.AllRecipes.ContainsAny(r => {
				var localRecipe = RecipeBook.GetRecipeByID(r.id);
				return localRecipe != null && r.uid != localRecipe.uid && r.version >= localRecipe.version;
			});

			if (hasOutdatedRecipes && MessageBox.Show(Resources.msg_reload_recipes, Resources.cap_reload_recipes, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
			{
				Current.ReloadRecipes(true);
				RefreshRecipeList();
				Undo.Push(Undo.Kind.RecipeList, "Reload recipes");
			}
			else
			{
				Current.ReloadRecipes(false);
				RefreshRecipeList();
			}
			return true;
		}

		private bool ImportCharacterFromBackyard()
		{
			var dlg = new LinkSelectCharacterDialog();
			dlg.Text = "Open Backyard AI character";

			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MessageBox.Show(string.Format(Resources.error_link_read_characters, Backyard.LastError ?? ""), Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				AppSettings.BackyardLink.Enabled = false;
			}

			dlg.Characters = Backyard.CharactersNoUser.ToArray();
			dlg.Folders = Backyard.Folders.ToArray();
			if (dlg.ShowDialog() != DialogResult.OK)
				return false;

			if (ConfirmSave(Resources.cap_import_character) == false)
				return false;

			SetStatusBarMessage(Resources.status_open_character);

			// Import...
			FaradayCardV4 faradayData;
			ImageInstance[] images;
			UserData userInfo;
			var importError = Backyard.ImportCharacter(dlg.SelectedCharacter, out faradayData, out images, out userInfo);
			if (importError == Backyard.Error.NotFound)
			{
				MessageBox.Show(Resources.error_link_open_character, Resources.cap_import_character, MessageBoxButtons.OK, MessageBoxIcon.Error);
				ClearStatusBarMessage();
				return false;
			}
			else if (importError != Backyard.Error.NoError || faradayData == null)
			{
				MessageBox.Show(Resources.error_link_open_character, Resources.cap_import_character, MessageBoxButtons.OK, MessageBoxIcon.Error);
				ClearStatusBarMessage();
				return false;
			}
			
			// Success
			Current.ReadFaradayCard(faradayData, null, userInfo);

			Backyard.Link.Image[] imageLinks;
			Current.ImportImages(images, out imageLinks);

			ClearStatusBarMessage();

			FileMutex.Release();

			Current.Filename = null;
			Current.IsDirty = false;
			Current.IsFileDirty = false;
			Current.OnLoadCharacter?.Invoke(this, EventArgs.Empty);

			if (AppSettings.BackyardLink.AlwaysLinkOnImport || MessageBox.Show(Resources.msg_link_create_link, Resources.cap_link_character, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
			{
				Current.LinkWith(dlg.SelectedCharacter, imageLinks);
				SetStatusBarMessage(Resources.status_link_create, Constants.StatusBarMessageInterval);
				Current.IsFileDirty = false;
				Current.IsLinkDirty = false;
				RefreshTitle();
			}
			return true;
		}

		private Backyard.Error UpdateCharacterInBackyard()
		{
			if (Backyard.ConnectionEstablished == false)
				return Backyard.Error.NotConnected;
			else if (Current.HasLink == false)
				return Backyard.Error.NotFound;

			var output = Generator.Generate(Generator.Option.Export | Generator.Option.Faraday | Generator.Option.Linked);

			// User persona
			UserData userInfo = null;
			if (AppSettings.BackyardLink.WriteUserPersona)
			{
				string userPersona = output.userPersona.ToFaraday();
				if (string.IsNullOrEmpty(userPersona) == false)
				{
					userInfo = new UserData() {
						name = Current.Card.userPlaceholder,
						persona = userPersona,
					};
					output.userPersona = GingerString.Empty;
				}
			}

			FaradayCardV4 card = FaradayCardV4.FromOutput(output);

			// Check if character exists, has newer changes
			bool hasChanges;
			var error = Backyard.ConfirmSaveCharacter(card, Current.Link, out hasChanges);
			if (error != Backyard.Error.NoError)
			{
				Current.Unlink();
				return error;
			}

			if (hasChanges)
			{
				// Overwrite prompt
				var mr = MessageBox.Show(Resources.msg_link_confirm_overwrite, Resources.cap_link_overwrite, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
				if (mr == DialogResult.Cancel)
					return Backyard.Error.CancelledByUser;
				else if (mr == DialogResult.No)
					return Backyard.Error.DismissedByUser;
			}

			DateTime updateDate;
			Backyard.Link.Image[] imageLinks;
			error = Backyard.UpdateCharacter(card, Current.Link, out updateDate, out imageLinks, userInfo);
			if (error != Backyard.Error.NoError)
			{
				return error;
			}
			else
			{
				Current.Link.updateDate = updateDate;
				Current.Link.imageLinks = imageLinks;
				Current.IsFileDirty = true;
				Current.IsLinkDirty = false;
				RefreshTitle();

				// Refresh character information
				Backyard.RefreshCharacters();
				return Backyard.Error.NoError;
			}
		}

		private Backyard.Error CreateNewCharacterInBackyard(out CharacterInstance createdCharacter, out Backyard.Link.Image[] images)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				createdCharacter = default(CharacterInstance);
				images = null;
				return Backyard.Error.NotConnected;
			}

			var output = Generator.Generate(Generator.Option.Export | Generator.Option.Faraday | Generator.Option.Linked);
			
			// User persona
			UserData userInfo = null;
			if (AppSettings.BackyardLink.WriteUserPersona)
			{
				string userPersona = output.userPersona.ToFaraday();
				if (string.IsNullOrEmpty(userPersona) == false)
				{
					userInfo = new UserData() {
						name = Current.Card.userPlaceholder,
						persona = userPersona,
					};
					output.userPersona = GingerString.Empty;
				}
			}

			FaradayCardV4 card = FaradayCardV4.FromOutput(output);

			Backyard.ImageInput[] imageInput = Backyard.GatherImages();
			BackupData.Chat[] chats = null;
			if (AppSettings.BackyardLink.ImportAlternateGreetings && output.greetings.Length > 1)
				chats = Backyard.GatherChats(card, output, imageInput);

			var error = Backyard.CreateNewCharacter(card, imageInput, chats, out createdCharacter, out images, userInfo);
			if (error != Backyard.Error.NoError)
			{
				return error;
			}
			else
			{
				Current.IsFileDirty = true;
				Current.IsLinkDirty = false;
				RefreshTitle();

				// Refresh character information
				Backyard.RefreshCharacters();
				return Backyard.Error.NoError;
			}
		}

		private bool ReestablishLink()
		{
			// Refresh character information
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_failed, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			if (Current.Link != null)
			{
				CharacterInstance characterInstance;
				if (Backyard.GetCharacter(Current.Link.characterId, out characterInstance))
				{
					Current.Link.filenameHash = Current.Filename;
					Current.Link.isActive = true;
					Current.IsFileDirty = true;
					Current.Link.RefreshState();
					RefreshTitle();

					MessageBox.Show(Resources.msg_link_reestablished, Resources.cap_link_reestablish, MessageBoxButtons.OK, MessageBoxIcon.Information);
					// SetStatusBarMessage(Resources.status_link_reestablished, Constants.StatusBarMessageInterval);
					return true;
				}
				else
				{
					if (MessageBox.Show(Resources.error_link_reestablish, Resources.cap_link_reestablish, MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
					{
						Current.Link = null;
						Current.IsFileDirty = true;
						RefreshTitle();
					}
				}
			}
			return false;
		}

		private bool BreakLink(bool bSilent = false)
		{
			if (Current.HasActiveLink)
			{
				Current.IsFileDirty = true;
				Current.Link.isActive = false;
				RefreshTitle();

				if (bSilent == false)
					SetStatusBarMessage(Resources.status_link_break, Constants.StatusBarMessageInterval);
				return true;
			}
			return false;
		}

		private Backyard.Error RevertCharacterFromBackyard()
		{
			if (Backyard.ConnectionEstablished == false)
				return Backyard.Error.NotConnected;
			else if (Current.HasLink == false)
				return Backyard.Error.NotFound;

			// Refresh character list
			var refreshError = Backyard.RefreshCharacters();
			if (refreshError != Backyard.Error.NoError)
				return refreshError;

			// Get character instance
			CharacterInstance characterInstance;
			if (Backyard.GetCharacter(Current.Link.characterId, out characterInstance) == false)
				return Backyard.Error.NotFound;

			// Import data
			FaradayCardV4 faradayData;
			ImageInstance[] images;
			UserData userInfo;
			var importError = Backyard.ImportCharacter(characterInstance, out faradayData, out images, out userInfo);
			if (importError != Backyard.Error.NoError)
				return importError;

			// Success
			Current.ReadFaradayCard(faradayData, null, userInfo);

			Backyard.Link.Image[] imageLinks;
			Current.ImportImages(images, out imageLinks);

			Current.LinkWith(characterInstance, imageLinks);
			Current.IsDirty = true;
			Current.IsLinkDirty = false;
			
			// Refresh sidepanel and recipe list
			this.Suspend();
			sidePanel.Reset();
			sidePanel.SetLoreCount(0, false);
			sidePanel.RefreshValues();
			sidePanel.Refresh();
			recipeList.RemoveAllPanels();
			this.Resume();
			recipeList.Refresh();

			this.Suspend();
			recipeList.RecreatePanels(true);
			recipeList.RefreshScrollbar();
			RefreshSpellChecking();

			Regenerate();
			RefreshTitle();
			this.Resume();

			Undo.Push(Undo.Kind.RecipeList, "Reimport character");
			
			return Backyard.Error.NoError;
		}

		private bool OpenChatHistory()
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MessageBox.Show(Resources.error_link_failed, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			if (_editChatDialog != null && !_editChatDialog.IsDisposed)
				_editChatDialog.Close(); // Close existing

			_editChatDialog = new LinkEditChatDialog();
			if (Current.HasActiveLink)
			{
				var group = Backyard.GetGroup(Backyard.GetCharacter(Current.Link.characterId).groupId);
				if (string.IsNullOrEmpty(group.instanceId) == false)
					_editChatDialog.Group = group;
			}

			_editChatDialog.Show();
			return true;
		}

		public void ShowCustomVariablesDialog(string editVariable = null)
		{
			VariablesDialog dlg = new VariablesDialog();
			var variables = new List<CustomVariable>(Current.Card.customVariables);

			// Find unknown variables and add them
			var foundNames = new HashSet<CustomVariableName>();

			foreach (var parameter in Current.AllRecipes.SelectMany(r => r.parameters).OfType<TextParameter>())
			{
				var text = parameter.value;
				if (text == null)
					continue;
				ExtractVariableNames(ref foundNames, text);
			}

			foreach (var parameter in Current.AllRecipes.SelectMany(r => r.parameters).OfType<LorebookParameter>())
			{
				var lorebook = parameter.value;
				if (lorebook == null)
					continue;
				foreach (var entry in lorebook.entries)
					ExtractVariableNames(ref foundNames, entry.value);
			}

			foreach (var varName in foundNames.OrderBy(s => s))
			{
				string tmp;
				if (Current.Card.TryGetVariable(varName, out tmp) == false)
					variables.Add(new CustomVariable(varName));
			}

			dlg.Variables = variables;
			dlg.EditOnLoad = editVariable;

			if (dlg.ShowDialog() == DialogResult.OK && dlg.Changed)
			{
				Current.Card.customVariables = new List<CustomVariable>(dlg.Variables);
				Undo.Push(Undo.Kind.Parameter, "Changed user-defined variables");

				var textBoxes = recipeList.FindAllControlsOfType<RichTextBoxEx>();
				foreach (var textBox in textBoxes)
					textBox.RefreshPatterns();

				recipeList.RefreshSyntaxHighlighting(false);
				Current.IsDirty = true;
			}

			void ExtractVariableNames(ref HashSet<CustomVariableName> names, string text)
			{
				var pos_var = text.IndexOf("{$", 0);
				while (pos_var != -1)
				{
					int pos_var_end = text.IndexOfAny(new char[] { '}', ' ', '\r', '\n', '\t' }, pos_var + 2);
					if (pos_var_end == -1 || char.IsWhiteSpace(text[pos_var_end]))
						break;

					CustomVariableName varName = text.Substring(pos_var + 1, pos_var_end - pos_var - 1);
					if (string.IsNullOrWhiteSpace(varName.ToString()) == false)
						names.Add(varName);

					pos_var = text.IndexOf("{$", pos_var + 2);
				}
			}
		}

		public bool ExportManyFromBackyard()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MessageBox.Show(string.Format(Resources.error_link_read_characters, Backyard.LastError ?? ""), Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}

			// Choose character(s)
			var dlg = new LinkSelectMultipleCharactersDialog();
			dlg.Text = "Select characters to export";
			dlg.Characters = Backyard.CharactersNoUser.ToArray();
			dlg.Folders = Backyard.Folders.ToArray();
			if (dlg.ShowDialog() != DialogResult.OK || dlg.Characters.Length == 0)
				return false;

			// Export format
			var formatDialog = new FileFormatDialog();
			if (formatDialog.ShowDialog() != DialogResult.OK)
				return false;

			string ext;
			if (formatDialog.FileFormat.Contains(FileUtil.FileType.Json))
				ext = "json";
			else if (formatDialog.FileFormat.Contains(FileUtil.FileType.Png))
				ext = "png";
			else if (formatDialog.FileFormat.Contains(FileUtil.FileType.CharX))
				ext = "charx";
			else if (formatDialog.FileFormat.Contains(FileUtil.FileType.Yaml))
				ext = "yaml";
			else if (formatDialog.FileFormat.Contains(FileUtil.FileType.Backup))
				ext = "zip";
			else
				return false; // Error

			var folderDialog = new WinAPICodePack.CommonOpenFileDialog();
			folderDialog.Title = Resources.cap_export_folder;
			folderDialog.IsFolderPicker = true;
			folderDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			folderDialog.EnsurePathExists = true;
			folderDialog.AllowNonFileSystemItems = false;
			folderDialog.EnsureFileExists = true;
			folderDialog.EnsureReadOnly = false;
			folderDialog.EnsureValidNames = true;
			folderDialog.Multiselect = false;
			folderDialog.AddToMostRecentlyUsedList = false;
			folderDialog.ShowPlacesList = true;

			if (folderDialog.ShowDialog() != WinAPICodePack.CommonFileDialogResult.Ok)
				return false;

			var outputDirectory = folderDialog.FileName;
			if (Directory.Exists(outputDirectory) == false)
				return false;

			AppSettings.Paths.LastImportExportPath = outputDirectory;
		
			var filenames = new List<string>(dlg.Characters.Length);
			if (formatDialog.FileFormat.Contains(FileUtil.FileType.Backup))
			{
				string now = DateTime.Now.ToString("yyyy-MM-dd");
				foreach (var character in dlg.Characters)
				{
					filenames.Add(Path.Combine(outputDirectory,
						Utility.MakeUniqueFilename(string.Concat(character.displayName.Replace(" ", "_"), " - ", now, ".backup.zip"))
					));
				}
			}
			else
			{
				foreach (var character in dlg.Characters)
				{
					filenames.Add(Path.Combine(outputDirectory,
						Utility.MakeUniqueFilename(string.Format("{0}_{1}.{2}",
							character.displayName,
							character.creationDate.ToFileTimeUtc() / 1000L,
							ext))
					));
				}
			}

			// Confirm overwrite?
			bool bFileExists = filenames.ContainsAny(fn => File.Exists(fn));
			if (bFileExists && MessageBox.Show(Resources.msg_link_export_overwrite_files, Resources.cap_overwrite_files, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
			{
				return false;
			}

			var exporter = new BulkExporter();

			var progressDlg = new ProgressBarDialog();
			progressDlg.Message = "Exporting...";

			progressDlg.onCancel += (s, e) => {
				exporter.Cancel();
				progressDlg.Close();
			};
			exporter.onProgress += (value) => {
				progressDlg.Percentage = value;
			};
			exporter.onComplete += (result) => {
				progressDlg.Percentage = 100;
				progressDlg.TopMost = false;
				progressDlg.Close();

				CompleteExport(result, filenames);
				_bCanRegenerate = true;
				_bCanIdle = true;
			};

			for (int i = 0; i < filenames.Count; ++i)
				exporter.Enqueue(dlg.Characters[i]);

			_bCanRegenerate = false;
			_bCanIdle = false;
			exporter.Start(formatDialog.FileFormat);
			progressDlg.ShowDialog(this);

			return true;
		}

		private void CompleteExport(BulkExporter.Result result, List<string> filenames)
		{
			int skipped = 0;
			List<string> removeFilenames = new List<string>();
			if (result.error == BulkExporter.Error.NoError)
			{
				// Move intermediate files
				for (int i = 0; i < result.filenames.Length && i < filenames.Count; ++i)
				{
					try
					{
						string tempFilename = result.filenames[i];
						if (string.IsNullOrEmpty(tempFilename) == false && File.Exists(tempFilename))
						{
							if (File.Exists(filenames[i]))
								File.Delete(filenames[i]);
							File.Move(tempFilename, filenames[i]);
						}
					}
					catch (IOException e)
					{
						if (e.HResult == Win32.HR_ERROR_DISK_FULL 
							|| e.HResult == Win32.HR_ERROR_HANDLE_DISK_FULL)
						{
							removeFilenames.AddRange(result.filenames);
							result.error = BulkExporter.Error.DiskFullError;
							break;
						}
						else if (e.HResult == Win32.HR_ERROR_ACCESS_DENIED 
							|| e.HResult == Win32.HR_ERROR_WRITE_PROTECT
							|| e.HResult == Win32.HR_ERROR_FILE_EXISTS)
						{
							skipped++;
							removeFilenames.Add(result.filenames[i]); // Skip
						}
						else
						{
							removeFilenames.AddRange(result.filenames);
							result.error = BulkExporter.Error.FileError;
							break;
						}
					}
					catch
					{
						skipped++;
						removeFilenames.Add(result.filenames[i]); // Skip
					}
				}
			}
			else
			{
				removeFilenames.AddRange(result.filenames);
			}

			// Delete temp files
			foreach (var filename in removeFilenames)
			{
				try
				{
					if (string.IsNullOrEmpty(filename) == false && File.Exists(filename))
						File.Delete(filename);
				}
				catch
				{
					// Do nothing
				}
			}

			if (result.error == BulkExporter.Error.NoError)
			{
				if (skipped > 0)
				{
					MessageBox.Show(this, string.Format(Resources.msg_link_export_some_characters, NumCharacters(result.succeeded - skipped), skipped), Resources.cap_link_export_many_characters, MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					MessageBox.Show(this, string.Format(Resources.msg_link_export_many_characters, NumCharacters(result.succeeded)), Resources.cap_link_export_many_characters, MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			else if (result.error == BulkExporter.Error.Cancelled)
			{
				MessageBox.Show(this, Resources.error_canceled, Resources.cap_link_export_many_characters, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else if (result.error == BulkExporter.Error.FileError)
			{
				MessageBox.Show(this, Resources.error_write_file, Resources.cap_link_export_many_characters, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else if (result.error == BulkExporter.Error.DiskFullError)
			{
				MessageBox.Show(this, Resources.error_disk_full, Resources.cap_link_export_many_characters, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				MessageBox.Show(this, Resources.error_link_export_many_characters, Resources.cap_link_export_many_characters, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		public bool ImportManyToBackyard()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MessageBox.Show(string.Format(Resources.error_link_read_characters, Backyard.LastError ?? ""), Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}

			// Select files...
			try
			{
				importFileDialog.Title = Resources.cap_import_character;
				importFileDialog.Filter = "All supported types|*.png;*.json;*.charx;*.yaml;*.zip|PNG files|*.png|JSON files|*.json|CHARX files|*.charx|YAML files|*.yaml|Character backup files|*.zip";
				importFileDialog.FilterIndex = AppSettings.User.LastImportCharacterFilter;
				importFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
				importFileDialog.Multiselect = true;
				importFileDialog.FileName = "";

				var mr = importFileDialog.ShowDialog();
				if (mr != DialogResult.OK || importFileDialog.FileNames.Length == 0)
					return false;

				AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(importFileDialog.FileNames[0]);
				AppSettings.User.LastImportCharacterFilter = importFileDialog.FilterIndex;
			}
			finally
			{
				importFileDialog.Multiselect = false;
			}

			// Identify file types and import (no progress bar)
			var filenames = importFileDialog.FileNames.ToArray();
			if (filenames.Length < 10)
			{ 
				filenames = filenames
					.Where(fn => FileUtil.CheckFileType(fn) != FileUtil.FileType.Unknown)
					.OrderBy(fn => new FileInfo(fn).LastWriteTime)
					.ToArray();
				return BeginImport(filenames);
			}

			// Identify file types (with progress bar)
			var checker = new AsyncFileTypeChecker();

			var progressDlg = new ProgressBarDialog();
			progressDlg.Message = "Identifying file types...";

			progressDlg.onCancel += (s, e) => {
				checker.Cancel();
				progressDlg.Close();
				MessageBox.Show(Resources.error_canceled, Resources.cap_link_import_many_characters, MessageBoxButtons.OK, MessageBoxIcon.Error);
			};
			checker.onProgress += (value) => {
				progressDlg.Percentage = value;
			};
			checker.onComplete += (result) => {
				progressDlg.Percentage = 100;
				progressDlg.TopMost = false;
				progressDlg.Close();

				if (result.error == AsyncFileTypeChecker.Error.NoError)
				{
					BeginImport(result.filenames);
				}
				else if (result.error == AsyncFileTypeChecker.Error.Cancelled)
				{
					MessageBox.Show(Resources.error_canceled, Resources.cap_link_import_many_characters, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			};

			checker.Enqueue(filenames);
			checker.Start();
			progressDlg.ShowDialog(this);

			return true;
		}

		private bool BeginImport(string[] filenames)
		{
			if (filenames.Length == 0)
			{
				MessageBox.Show(Resources.error_link_import_many_unsupported, Resources.cap_link_import_many_characters, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			// Confirm
			if (MessageBox.Show(string.Format(Resources.msg_link_confirm_import_many, NumCharacters(filenames.Length)), Resources.cap_link_import_many_characters, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) != DialogResult.Yes)
				return false;

			// Create Ginger import folder
			FolderInstance importFolder;
			if (string.IsNullOrEmpty(AppSettings.BackyardLink.BulkImportFolderName) == false)
			{
				string folderName = AppSettings.BackyardLink.BulkImportFolderName.Trim();
				string folderUrl = Backyard.ToFolderUrl(folderName);
				importFolder = Backyard.Folders
					.Where(f => string.Compare(f.name, folderName, StringComparison.OrdinalIgnoreCase) == 0
						|| string.Compare(f.url, folderUrl, StringComparison.OrdinalIgnoreCase) == 0)
					.FirstOrDefault();

				if (importFolder.isEmpty)
					Backyard.CreateNewFolder(folderName, out importFolder); // It's ok if this fails.
			}
			else
				importFolder = default(FolderInstance);

			var importer = new BulkImporter();
			var progressDlg = new ProgressBarDialog();
			progressDlg.Message = "Importing...";

			progressDlg.onCancel += (s, e) => {
				importer.Cancel();
				progressDlg.Close();
			};
			importer.onProgress += (value) => {
				progressDlg.Percentage = value;
			};
			importer.onComplete += (result) => {
				progressDlg.Percentage = 100;
				progressDlg.TopMost = false;
				progressDlg.Close();

				CompleteImport(result);
				_bCanRegenerate = true;
				_bCanIdle = true;
			};

			for (int i = 0; i < filenames.Length; ++i)
				importer.Enqueue(filenames[i]);

			_bCanRegenerate = false;
			_bCanIdle = false;
			importer.Start(importFolder);
			progressDlg.ShowDialog(this);
			return true;
		}

		private void CompleteImport(BulkImporter.Result result)
		{
			if (result.error == BulkImporter.Error.NoError)
			{
				MessageBox.Show(this, string.Format(result.skipped == 0 ? Resources.msg_link_import_many_characters : Resources.msg_link_import_some_characters, NumCharacters(result.succeeded), result.skipped), Resources.cap_link_import_many_characters, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			else if (result.error == BulkImporter.Error.Cancelled)
			{
				MessageBox.Show(this, Resources.error_canceled, Resources.cap_link_import_many_characters, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				MessageBox.Show(this, Resources.error_link_import_many_characters, Resources.cap_link_import_many_characters, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private string NumCharacters(int n)
		{
			return n == 1 ? string.Concat(n.ToString(), " character") : string.Concat(n.ToString(), " characters");
		}

		private bool EditManyModelSettings()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MessageBox.Show(string.Format(Resources.error_link_read_characters, Backyard.LastError ?? ""), Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}

			// Choose character(s)
			var dlg = new LinkSelectMultipleGroupsDialog();
			dlg.Text = "Select Backyard AI characters";
			dlg.Characters = Backyard.Characters.ToArray();
			dlg.Groups = Backyard.Groups.ToArray();
			dlg.Folders = Backyard.Folders.ToArray();
			if (dlg.ShowDialog() != DialogResult.OK || dlg.Groups.Length == 0)
				return false;

			// Model settings
			var dlgSettings = new EditModelSettingsDialog();
			if (dlgSettings.ShowDialog() != DialogResult.OK)
				return false;

			// Confirm
			if (MessageBox.Show(string.Format(Resources.msg_link_confirm_update_many, NumCharacters(dlg.Groups.Length)), Resources.cap_link_update_many_characters, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) != DialogResult.Yes)
				return false;

			var updater = new BulkUpdateModelSettings();

			var progressDlg = new ProgressBarDialog();
			progressDlg.Message = "Updating...";

			progressDlg.onCancel += (s, e) => {
				updater.Cancel();
				progressDlg.Close();
			};
			updater.onProgress += (value) => {
				progressDlg.Percentage = value;
			};
			updater.onComplete += (result) => {
				progressDlg.Percentage = 100;
				progressDlg.TopMost = false;
				progressDlg.Close();

				CompleteUpdateSettings(result);
				_bCanRegenerate = true;
				_bCanIdle = true;
			};

			for (int i = 0; i < dlg.Groups.Length; ++i)
				updater.Enqueue(dlg.Groups[i]);

			_bCanRegenerate = false;
			_bCanIdle = false;
			updater.Start(dlgSettings.Parameters);
			progressDlg.ShowDialog(this);

			return true;
		}

		private void CompleteUpdateSettings(BulkUpdateModelSettings.Result result)
		{
			if (result.error == BulkUpdateModelSettings.Error.NoError)
			{
				MessageBox.Show(this, string.Format(result.skipped == 0 ? Resources.msg_link_update_many_characters : Resources.msg_link_update_some_characters, NumCharacters(result.succeeded), result.skipped), Resources.cap_link_update_many_characters, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			else if (result.error == BulkUpdateModelSettings.Error.Cancelled)
			{
				MessageBox.Show(this, Resources.error_canceled, Resources.cap_link_update_many_characters, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				MessageBox.Show(this, Resources.error_link_update_many_characters, Resources.cap_link_update_many_characters, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private bool CreateBackyardBackup()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MessageBox.Show(string.Format(Resources.error_link_read_characters, Backyard.LastError ?? ""), Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}

			var groupDlg = new LinkSelectGroupDialog();
			groupDlg.Text = Resources.cap_link_create_backup;
			groupDlg.Characters = Backyard.Characters.ToArray();
			groupDlg.Groups = Backyard.Groups.ToArray();
			groupDlg.Folders = Backyard.Folders.ToArray();

			GroupInstance groupInstance;
			if (groupDlg.ShowDialog() == DialogResult.OK)
				groupInstance = groupDlg.SelectedGroup;
			else
				return false;

			if (groupInstance.isEmpty)
				return false;

			if (groupInstance.members.Length > 2)
			{
				MessageBox.Show(Resources.error_link_create_backup_not_character, Resources.cap_link_create_backup, MessageBoxButtons.OK, MessageBoxIcon.Information);
				return false;
			}

			CharacterInstance characterInstance;
			characterInstance = groupInstance.members
				.Select(id => Backyard.GetCharacter(id))
				.FirstOrDefault(c => c.isUser == false);

			if (string.IsNullOrEmpty(characterInstance.instanceId))
				return false; // Error

			string groupName = Utility.FirstNonEmpty(groupInstance.name, characterInstance.displayName, Constants.DefaultCharacterName);
			if (groupInstance.members.Length > 2)
				groupName += " et al";

			BackupData backup = null;
			var error = RunTask(() => BackupUtil.CreateBackup(characterInstance, out backup), "Creating backup...");
			if (error == Backyard.Error.NotFound)
			{
				MessageBox.Show(Resources.error_link_create_backup, Resources.cap_link_create_backup, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			else if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_create_backup, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return false;
			}
			else if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_create_backup, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			string filename = string.Concat(groupName.Replace(" ", "_"), " - ", DateTime.Now.ToString("yyyy-MM-dd"), ".backup.zip");

			importFileDialog.Title = Resources.cap_link_create_backup;
			exportFileDialog.Filter = "Character backup file|*.zip";
			exportFileDialog.FileName = Utility.ValidFilename(filename);
			exportFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			exportFileDialog.FilterIndex = AppSettings.User.LastExportChatFilter;

			var result = exportFileDialog.ShowDialog();
			if (result != DialogResult.OK || string.IsNullOrWhiteSpace(exportFileDialog.FileName))
				return false;

			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(exportFileDialog.FileName);
			AppSettings.User.LastExportChatFilter = exportFileDialog.FilterIndex;

			if (BackupUtil.WriteBackup(exportFileDialog.FileName, backup) != FileUtil.Error.NoError)
			{
				MessageBox.Show(Resources.error_write_file, Resources.cap_link_create_backup, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			MessageBox.Show(Resources.msg_link_create_backup, Resources.cap_link_create_backup, MessageBoxButtons.OK, MessageBoxIcon.Information);
			return true;
		}

		private bool RestoreBackyardBackup(out CharacterInstance characterInstance)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_restore_backup, MessageBoxButtons.OK, MessageBoxIcon.Error);
				characterInstance = default(CharacterInstance);
				return false;
			}

			importFileDialog.Title = Resources.cap_link_restore_backup;
			importFileDialog.Filter = "Character backup file|*.zip";
			importFileDialog.FilterIndex = AppSettings.User.LastImportChatFilter;
			importFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			var result = importFileDialog.ShowDialog();
			if (result != DialogResult.OK)
			{
				characterInstance = default(CharacterInstance);
				return false;
			}

			AppSettings.User.LastImportChatFilter = importFileDialog.FilterIndex;
			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(importFileDialog.FileName);

			BackupData backup;
			FileUtil.Error readError = BackupUtil.ReadBackup(importFileDialog.FileName, out backup);
			if (readError != FileUtil.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_restore_backup_invalid, Resources.cap_link_restore_backup, MessageBoxButtons.OK, MessageBoxIcon.Error);
				characterInstance = default(CharacterInstance);
				return false;
			}

			// Confirmation
			if (MessageBox.Show(string.Format(Resources.msg_link_restore_backup, backup.characterCard.data.displayName, backup.chats.Count), Resources.cap_link_restore_backup, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.No)
			{
				characterInstance = default(CharacterInstance);
				return false;
			}

			// Import model settings?
			if (backup.hasParameters && MessageBox.Show(Resources.msg_link_restore_backup_settings, Resources.cap_link_restore_backup, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.No)
			{
				// Use default model settings
				foreach (var chat in backup.chats)
					chat.parameters = AppSettings.BackyardSettings.UserSettings;
			}

			List<Backyard.ImageInput> images = new List<Backyard.ImageInput>();
			images.AddRange(backup.images
				.Select(i => new Backyard.ImageInput {
					asset = new AssetFile() {
						name = i.filename,
						data = AssetData.FromBytes(i.data),
						ext = i.ext,
						assetType = AssetFile.AssetType.Icon,
					},
					fileExt = i.ext,
				}));

			images.AddRange(backup.backgrounds
				.Select(i => new Backyard.ImageInput {
					asset = new AssetFile() {
						name = i.filename,
						data = AssetData.FromBytes(i.data),
						ext = i.ext,
						assetType = AssetFile.AssetType.Background,
					},
					fileExt = i.ext,
				}));

			if (backup.userPortrait != null)
			{
				images.Add(new Backyard.ImageInput {
					asset = new AssetFile() {
						name = backup.userPortrait.filename,
						data = AssetData.FromBytes(backup.userPortrait.data),
						ext = backup.userPortrait.ext,
						assetType = AssetFile.AssetType.UserIcon,
					},
					fileExt = backup.userPortrait.ext,
				});
			}

			// Create Ginger import folder
			FolderInstance backupFolder;
			if (string.IsNullOrEmpty(AppSettings.BackyardLink.BulkImportFolderName) == false)
			{
				string folderName = "Restored from backup";
				string folderUrl = Backyard.ToFolderUrl(folderName);
				backupFolder = Backyard.Folders
					.Where(f => string.Compare(f.name, folderName, StringComparison.OrdinalIgnoreCase) == 0
						|| string.Compare(f.url, folderUrl, StringComparison.OrdinalIgnoreCase) == 0)
					.FirstOrDefault();

				if (backupFolder.isEmpty)
					Backyard.CreateNewFolder(folderName, out backupFolder); // It's ok if this fails.
			}
			else
				backupFolder = default(FolderInstance);

			// Write character
			Backyard.Link.Image[] imageLinks; // Ignored
			CharacterInstance returnedCharacter = default(CharacterInstance);
			Backyard.Error error = RunTask(() => Backyard.CreateNewCharacter(backup.characterCard, images.ToArray(), backup.chats.ToArray(), out returnedCharacter, out imageLinks, backup.userInfo, backupFolder), "Restoring backup...");
			characterInstance = returnedCharacter;
			if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_restore_backup, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
						
			MessageBox.Show(Resources.msg_link_restore_backup_success, Resources.cap_link_restore_backup, MessageBoxButtons.OK, MessageBoxIcon.Information);
			return true;
		}

		private Backyard.Error RunTask(Func<Backyard.Error> action, string statusText = null)
		{
			if (statusText != null)
				SetStatusBarMessage(statusText);
			
			this.Cursor = Cursors.WaitCursor;
			var error = action.Invoke();
			this.Cursor = Cursors.Default;

			if (statusText != null)
				ClearStatusBarMessage();
			return error;
		}

		private void RepairBrokenImages()
		{
			var mr = MessageBox.Show(Resources.msg_link_repair_images, Resources.cap_link_repair_images, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
			if (mr != DialogResult.Yes)
				return;

			int modified = 0;
			int skipped = 0;
			var error = RunTask(() => Backyard.RepairImages(out modified, out skipped), "Repairing broken images...");

			if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_repair_images, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}
			if (error == Backyard.Error.NotFound)
			{
				MessageBox.Show(Resources.error_link_images_folder_not_found, Resources.cap_link_repair_images, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_repair_images, Resources.cap_link_repair_images, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Success
			if (skipped > 0)
			{
				MessageBox.Show(string.Format(Resources.msg_link_repaired_images_skipped, modified, skipped), Resources.cap_link_repair_images, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			else if (modified > 0)
			{
				MessageBox.Show(string.Format(Resources.msg_link_repaired_images, modified), Resources.cap_link_repair_images, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			else
			{
				MessageBox.Show(string.Format(Resources.msg_link_no_images_repaired, modified), Resources.cap_link_repair_images, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void PurgeUnusedImages()
		{
			var imagesFolder = Path.Combine(AppSettings.BackyardLink.Location, "images");
			if (Directory.Exists(imagesFolder) == false)
			{
				MessageBox.Show(Resources.error_link_images_folder_not_found, Resources.cap_link_purge_images, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			string[] imageUrls = new string[0];

			var error = RunTask(() => Backyard.GetAllImageUrls(out imageUrls));

			if (error == Backyard.Error.NotConnected)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_purge_images, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}
			if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_repair_images, Resources.cap_link_purge_images, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if (imageUrls == null || imageUrls.Length == 0)
			{
				MessageBox.Show(Resources.msg_link_purge_images_not_found, Resources.cap_link_purge_images, MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			var images = new HashSet<string>(imageUrls
				.Select(fn => Path.GetFileName(fn).ToLowerInvariant())
				.Where(fn => string.IsNullOrEmpty(fn) == false));
			
			var foundImageFilenames = new HashSet<string>(Utility.FindFilesInFolder(imagesFolder)
				.Select(fn => Path.GetFileName(fn).ToLowerInvariant())
				.Where(fn => {
					return Utility.IsSupportedImageFilename(fn);
				}));

			var unknownImages = foundImageFilenames.Except(images)
				.Select(fn => Path.Combine(imagesFolder, fn))
				.ToList();

			if (unknownImages.Count > 0)
			{
				var mr = MessageBox.Show(string.Format(Resources.msg_link_purge_images_confirm, unknownImages.Count), Resources.cap_link_purge_images, MessageBoxButtons.YesNo, MessageBoxIcon.Stop,  MessageBoxDefaultButton.Button2);
				
				if (mr == DialogResult.Yes)
				{
					Win32.SendToRecycleBin(unknownImages, Win32.FileOperationFlags.FOF_WANTNUKEWARNING | Win32.FileOperationFlags.FOF_NOCONFIRMATION);
				}
			}
			else
			{
				MessageBox.Show(Resources.msg_link_purge_images_not_found, Resources.cap_link_purge_images, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private bool EditCurrentModelSettings()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MessageBox.Show(string.Format(Resources.error_link_read_characters, Backyard.LastError ?? ""), Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}

			var groupInstance = Backyard.GetGroupForCharacter(Current.Link.characterId);
			if (groupInstance.isEmpty)
			{
				MessageBox.Show(string.Format(Resources.error_link_character_not_found, Backyard.LastError ?? ""), Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}


			ChatInstance[] chats = null;
			if (groupInstance.isEmpty == false && RunTask(() => Backyard.GetChats(groupInstance.instanceId, out chats)) != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_disconnected, Resources.cap_link_edit_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			if (chats == null || chats.Length == 0)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_edit_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			// Model settings dialog
			var dlg = new EditModelSettingsDialog();
			dlg.Editing = chats[0].parameters;
			if (dlg.ShowDialog() != DialogResult.OK)
				return false;

			string[] chatIds = chats.Select(c => c.instanceId).ToArray();

			var error = RunTask(() => Backyard.UpdateChatParameters(chatIds, dlg.Parameters, null), "Updating model settings...");
			if (error == Backyard.Error.NotFound)
			{
				MessageBox.Show(Resources.error_link_chat_not_found, Resources.cap_link_edit_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;;
			}
			if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_edit_settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			SetStatusBarMessage(Resources.status_link_update_model_settings, Constants.StatusBarMessageInterval);
			return true;
		}

		private bool RepairLegacyChats()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MessageBox.Show(string.Format(Resources.error_link_read_characters, Backyard.LastError ?? ""), Resources.cap_link_bulk_repair_chats, MessageBoxButtons.OK, MessageBoxIcon.Error);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}

			var groups = Backyard.Groups.ToArray();

			// Confirm
			if (MessageBox.Show(Resources.msg_link_bulk_repair_chats_confirm, Resources.cap_link_bulk_repair_chats, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) != DialogResult.Yes)
				return false;

			var updater = new LegacyChatUpdater();

			var progressDlg = new ProgressBarDialog();
			progressDlg.Message = "Repairing...";

			progressDlg.onCancel += (s, e) => {
				updater.Cancel();
				progressDlg.Close();
			};
			updater.onProgress += (value) => {
				progressDlg.Percentage = value;
			};
			updater.onComplete += (result) => {
				progressDlg.Percentage = 100;
				progressDlg.TopMost = false;
				progressDlg.Close();

				CompleteRepairLegacyChats(result);
				_bCanRegenerate = true;
				_bCanIdle = true;
			};

			updater.Enqueue(groups);

			_bCanRegenerate = false;
			_bCanIdle = false;
			updater.Start();
			progressDlg.ShowDialog(this);
			return true;
		}

		private void CompleteRepairLegacyChats(LegacyChatUpdater.Result result)
		{
			if (result.error == LegacyChatUpdater.Error.NoError)
			{
				if (result.numCharacters > 0)
				{
					MessageBox.Show(this, string.Format(Resources.msg_link_bulk_repair_chats, result.numChats, result.numCharacters), Resources.cap_link_bulk_repair_chats, MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					MessageBox.Show(this, Resources.msg_link_bulk_repair_chats_none, Resources.cap_link_bulk_repair_chats, MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				
			}
			else if (result.error == LegacyChatUpdater.Error.Cancelled)
			{
				MessageBox.Show(this, Resources.error_canceled, Resources.cap_link_bulk_repair_chats, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				MessageBox.Show(this, Resources.error_link_bulk_repair_chats, Resources.cap_link_bulk_repair_chats, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private bool DeleteBackyardCharacters()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MessageBox.Show(string.Format(Resources.error_link_read_characters, Backyard.LastError ?? ""), Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}

			// Choose character(s)
			var dlg = new LinkSelectMultipleCharactersDialog();
			dlg.Text = "Select characters to delete";
			dlg.Characters = Backyard.CharactersNoUser.ToArray();
			dlg.Folders = Backyard.Folders.ToArray();
			if (dlg.ShowDialog() != DialogResult.OK || dlg.Characters.Length == 0)
				return false;

			var characters = dlg.Characters;

			string characterNames;

			if (characters.Length > 1)
			{
				string[] names = characters
					.Select(c => c.name ?? Constants.DefaultCharacterName)
					.OrderBy(c => c)
					.ToArray();
				if (names.Length <= 4)
					characterNames = Utility.CommaSeparatedList(names);
				else
					characterNames = Utility.ListToCommaSeparatedString(names.Take(3)) + string.Format(", and {0} others", names.Length - 3);
			}
			else
			{
				characterNames = characters
					.Select(c => c.displayName)
					.FirstOrDefault() ?? Constants.DefaultCharacterName;
			}

			// Get affected character ids and group ids.
			Backyard.ConfirmDeleteResult result;
			Backyard.Error error = Backyard.ConfirmDeleteCharacters(characters, out result);
			if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_delete_characters, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			// Confirm delete
			if (MessageBox.Show(string.Format(result.characterIds.Length != result.groupIds.Length ? Resources.msg_link_delete_characters_and_group_chats_confirm : Resources.msg_link_delete_characters_confirm, characterNames), Resources.cap_link_delete_characters, MessageBoxButtons.YesNo, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
				return false;

			error = RunTask(() => Backyard.DeleteCharacters(result.characterIds, result.groupIds, result.imageIds), "Deleting characters...");
			if (error != Backyard.Error.NoError)
			{
				MessageBox.Show(Resources.error_link_general, Resources.cap_link_delete_characters, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			// Delete orphaned users
			string[] imageUrls;
			Backyard.DeleteOrphanedUsers(out imageUrls);
			imageUrls = Utility.ConcatenateArrays(imageUrls, result.imageUrls);

			// Delete image files
			try
			{
				foreach (var imageUrl in imageUrls)
				{
					if (File.Exists(imageUrl))
						File.Delete(imageUrl);
				}
			}
			catch 
			{ 
			}

			MessageBox.Show(this, string.Format(Resources.msg_link_deleted_characters, NumCharacters(characters.Length)), Resources.cap_link_delete_characters, MessageBoxButtons.OK, MessageBoxIcon.Information);
			
			return true;
		}
	}
}
