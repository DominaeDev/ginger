using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Ginger.Properties;

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
				options |= Generator.Option.SillyTavern;

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
						parameter.value = Utility.ReplaceWholeWord(parameter.value, GingerString.CharacterMarker, characterPlaceholder, true);
					if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
						parameter.value = Utility.ReplaceWholeWord(parameter.value, GingerString.UserMarker, userPlaceholder, true);

					parameter.value = Utility.ReplaceWholeWord(parameter.value, oldName, newName, false);
				}

				// List parameters
				foreach (var parameter in character.recipes.SelectMany(r => r.parameters.OfType<ListParameter>()))
				{
					string sValue = Utility.ListToDelimitedString(parameter.value, ",");
					if (sValue.Contains(oldName))
					{ 
						sValue = Utility.ReplaceWholeWord(sValue, oldName, newName, false);
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
							entry.value = Utility.ReplaceWholeWord(entry.value, GingerString.CharacterMarker, characterPlaceholder, true);
						if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
							entry.value = Utility.ReplaceWholeWord(entry.value, GingerString.UserMarker, userPlaceholder, true);

						entry.value = Utility.ReplaceWholeWord(entry.value, oldName, newName, false);
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
		}

		private bool ImportCharacterJson()
		{
			// Open file...
			importFileDialog.Title = Resources.cap_import_character;
			importFileDialog.Filter = "Character Json|*.json|SillyTavern card|*.png|Backyard AI card|*.png";
			importFileDialog.FilterIndex = AppSettings.User.LastImportCharacterFilter;
			importFileDialog.InitialDirectory = AppSettings.Paths.LastImportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			var result = importFileDialog.ShowDialog();
			if (result != DialogResult.OK)
				return false;

			AppSettings.Paths.LastImportPath = Path.GetDirectoryName(importFileDialog.FileName);
			AppSettings.User.LastImportCharacterFilter = importFileDialog.FilterIndex;

			if (ConfirmSave(Resources.cap_import_character) == false)
				return false;

			int jsonErrors;
			FileUtil.Error error;
			switch (importFileDialog.FilterIndex)
			{
			case 1: // Json
				error = FileUtil.ImportCharacterJson(importFileDialog.FileName, out jsonErrors);
				break;
			case 2: // Tavern png
				error = FileUtil.ImportCharacterFromPNG(importFileDialog.FileName, out jsonErrors, FileUtil.Format.SillyTavern);
				break;
			case 3: // Faraday png
				error = FileUtil.ImportCharacterFromPNG(importFileDialog.FileName, out jsonErrors, FileUtil.Format.Faraday);
				break;
			default:
				return false;
			}

			if (error == FileUtil.Error.FileNotFound)
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

			if (jsonErrors > 0)
				MessageBox.Show(string.Format(Resources.msg_load_with_error, jsonErrors), Resources.cap_load_with_error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return true;
		}

		private bool ImportLorebook(bool bCopyToFolder)
		{
			// Open file...
			importFileDialog.Title = Resources.cap_import_lorebook;
			importFileDialog.Filter = "Lorebook json|*.json|Lorebook csv|*.csv|SillyTavern card|*.png|Backyard AI card|*.png";
			importFileDialog.FilterIndex = AppSettings.User.LastImportLorebookFilter;
			importFileDialog.InitialDirectory = AppSettings.Paths.LastImportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.ContentPath("Lorebooks");
			var result = importFileDialog.ShowDialog();
			if (result != DialogResult.OK)
				return false;

			AppSettings.User.LastImportLorebookFilter = importFileDialog.FilterIndex;
			AppSettings.Paths.LastImportPath = Path.GetDirectoryName(importFileDialog.FileName);

			Lorebook lorebook = null;
			if (importFileDialog.FilterIndex == 1) // Json
			{
				lorebook = new Lorebook();
				if (lorebook.LoadFromJson(importFileDialog.FileName) == false)
				{
					MessageBox.Show(Resources.error_unrecognized_lorebook_format, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}
			}
			else if (importFileDialog.FilterIndex == 2) // CSV
			{
				lorebook = new Lorebook();
				if (lorebook.LoadFromCsv(importFileDialog.FileName) == false)
				{
					MessageBox.Show(Resources.error_unrecognized_lorebook_format, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}
			}
			else // PNG
			{
				int jsonErrors;
				FileUtil.ImportResult importResult;
				var error = FileUtil.Import(importFileDialog.FileName, out importResult);
				if (error == FileUtil.Error.FallbackError)
					error = FileUtil.Error.NoError; // We don't care about this error in this context

				if (error == FileUtil.Error.NoError)
				{
					if (importFileDialog.FilterIndex == 3 // SillyTavern
						&& importResult.tavernData != null
						&& importResult.tavernData.data.character_book != null)
					{
						lorebook = Lorebook.FromTavernBook(importResult.tavernData.data.character_book);
						lorebook.name = string.Concat(importResult.tavernData.data.name, " lorebook");
					}
					else if (importFileDialog.FilterIndex == 4 // Faraday
						&& importResult.faradayData != null
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

					if (FileUtil.ExportTavernLorebook(lorebook, destFilename) == false)
					{
						MessageBox.Show(Resources.error_save_character_card, Resources.cap_import_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
						return false;
					}
				}

				Lorebooks.LoadLorebooks();
			}

			if (lorebook.entries.Count >= 40)
				SetStatusBarMessage(string.Format("Building lorebook with {0} entries. Please wait...", lorebook.entries.Count));
			else
				SetStatusBarMessage("Refreshing recipe list...");
			Cursor = Cursors.WaitCursor;

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

		private void ExportCharacterJson()
		{
			ConfirmName(); 
			
			string filename = null;
			if (string.IsNullOrWhiteSpace(Current.Card.name) == false)
				filename = Current.Card.name;
			else if (string.IsNullOrWhiteSpace(Current.Character.spokenName) == false)
				filename = Current.Character.spokenName;

			if (string.IsNullOrEmpty(filename) == false)
			{
				if (AppSettings.User.LastExportCharacterFilter == 4 || AppSettings.User.LastExportCharacterFilter == 5) // png
					filename = string.Concat(filename, ".png");
				else // json
					filename = string.Concat(filename, ".json");
			}

			// Save as...
			exportFileDialog.Title = Resources.cap_export_character;
			exportFileDialog.Filter = "SillyTavern character json|*.json|Agnaistic character json|*.json|PygmalionAI character json|*.json|SillyTavern card|*.png|Backyard AI card|*.png";
			exportFileDialog.FileName = Utility.ValidFilename(filename);
			exportFileDialog.InitialDirectory = AppSettings.Paths.LastImportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			exportFileDialog.FilterIndex = AppSettings.User.LastExportCharacterFilter;
			var result = exportFileDialog.ShowDialog();
			if (result != DialogResult.OK || string.IsNullOrWhiteSpace(exportFileDialog.FileName))
				return;

			AppSettings.Paths.LastImportPath = Path.GetDirectoryName(exportFileDialog.FileName);
			AppSettings.User.LastExportCharacterFilter = exportFileDialog.FilterIndex;

			var output = Generator.Generate(Generator.Option.Export);
			var gingerExt = GingerExtensionData.FromOutput(Generator.Generate(Generator.Option.Snippet));


			if (exportFileDialog.FilterIndex == 1) // Tavern
			{
				var card = TavernCardV2.FromOutput(output);
				card.data.extensions.ginger = gingerExt;

				string tavernJson = card.ToJson();
				if (tavernJson != null && FileUtil.ExportTextFile(exportFileDialog.FileName, tavernJson))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == 2) // Agnaistic
			{
				var card = AgnaisticCard.FromOutput(output);
				card.extensions.ginger = gingerExt;

				// Avatar image
				if (Current.Card.portraitImage != null)
					card.avatar = FileUtil.ImageToBase64(Current.Card.portraitImage);

				string agnaisticJson = card.ToJson();
				if (agnaisticJson != null && FileUtil.ExportTextFile(exportFileDialog.FileName, agnaisticJson))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == 3) // Pygmalion
			{
				var card = PygmalionCard.FromOutput(output);
				string json = card.ToJson();
				if (json != null && FileUtil.ExportTextFile(exportFileDialog.FileName, json))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == 4) // SillyTavern PNG
			{
				// Open in another instance?
				if (FileMutex.CanAcquire(exportFileDialog.FileName) == false)
				{
					MessageBox.Show(string.Format(Resources.error_already_open, Path.GetFileName(exportFileDialog.FileName)), Resources.cap_export_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				if (FileUtil.Export(exportFileDialog.FileName, (Image)Current.Card.portraitImage ?? DefaultPortrait.Image, FileUtil.Format.SillyTavern))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == 5) // Faraday PNG
			{
				// Open in another instance?
				if (FileMutex.CanAcquire(exportFileDialog.FileName) == false)
				{
					MessageBox.Show(string.Format(Resources.error_already_open, Path.GetFileName(exportFileDialog.FileName)), Resources.cap_export_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				if (FileUtil.Export(exportFileDialog.FileName, (Image)Current.Card.portraitImage ?? DefaultPortrait.Image, FileUtil.Format.Faraday))
					return; // Success
			}
			MessageBox.Show(Resources.error_write_json, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void ExportLorebookJson(Generator.Output output, bool saveLocal)
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
				if (AppSettings.User.LastExportLorebookFilter == 3) // csv
					filename = string.Concat(filename, ".csv");
				else // json
					filename = string.Concat(filename, ".json");
			}

			exportFileDialog.Title = Resources.cap_export_lorebook;
			exportFileDialog.Filter = "SillyTavern lorebook json|*.json|Agnaistic lorebook json|*.json|CSV file|*.csv";
			exportFileDialog.FileName = Utility.ValidFilename(filename);
			if (saveLocal)
				exportFileDialog.InitialDirectory = Utility.ContentPath("Lorebooks");
			else
				exportFileDialog.InitialDirectory = AppSettings.Paths.LastImportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.ContentPath("Lorebooks");
			exportFileDialog.FilterIndex = AppSettings.User.LastExportLorebookFilter;

			var result = exportFileDialog.ShowDialog();
			if (result != DialogResult.OK || string.IsNullOrWhiteSpace(exportFileDialog.FileName))
				return;

			AppSettings.Paths.LastImportPath = Path.GetDirectoryName(exportFileDialog.FileName);
			AppSettings.User.LastExportLorebookFilter = exportFileDialog.FilterIndex;

			if (string.IsNullOrEmpty(lorebook.name))
				lorebook.name = Path.GetFileNameWithoutExtension(exportFileDialog.FileName);

			if (exportFileDialog.FilterIndex == 1) // Tavern
			{
				if (FileUtil.ExportTavernLorebook(lorebook, exportFileDialog.FileName))
					return; // Success
			} 
			else if (exportFileDialog.FilterIndex == 2) // Agnaistic
			{
				if (FileUtil.ExportAgnaisticLorebook(lorebook, exportFileDialog.FileName))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == 3) // CSV
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
						Utility.ReplaceWholeWord(sb, GingerString.CharacterMarker, characterPlaceholder, true);
					if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
						Utility.ReplaceWholeWord(sb, GingerString.UserMarker, userPlaceholder, true);
					parameter.value = sb.ToString();
				}

				foreach (var parameter in Current.Character.recipes.SelectMany(r => r.parameters).OfType<LorebookParameter>())
				{
					var lorebook = parameter.value;
					foreach (var entry in lorebook.entries)
					{
						StringBuilder sb = new StringBuilder(entry.value);
						if (string.IsNullOrWhiteSpace(characterPlaceholder) == false)
							Utility.ReplaceWholeWord(sb, GingerString.CharacterMarker, characterPlaceholder, true);
						if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
							Utility.ReplaceWholeWord(sb, GingerString.UserMarker, userPlaceholder, true);
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
						Utility.ReplaceWholeWord(sb, characterPlaceholder, GingerString.CharacterMarker, false);
					if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
						Utility.ReplaceWholeWord(sb, userPlaceholder, GingerString.UserMarker, false);
					parameter.value = sb.ToString();
				}

				foreach (var parameter in Current.Character.recipes.SelectMany(r => r.parameters).OfType<LorebookParameter>())
				{
					var lorebook = parameter.value;
					foreach (var entry in lorebook.entries)
					{
						StringBuilder sb = new StringBuilder(entry.value);
						if (string.IsNullOrWhiteSpace(characterPlaceholder) == false)
							Utility.ReplaceWholeWord(sb, characterPlaceholder, GingerString.CharacterMarker, false);
						if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
							Utility.ReplaceWholeWord(sb, userPlaceholder, GingerString.UserMarker, false);
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
					RecipeMaker.CreateSnippet(dlg.FileName, dlg.SnippetName, dlg.Texts);
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

			LaunchTextEditor.OpenFile(recipe.filename);
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
			ExportLorebookJson(output, true);
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

			if (FileUtil.Export(filename, (Image)Current.Card.portraitImage ?? DefaultPortrait.Image))
			{
				SaveNotes(filename);

				FileMutex.Acquire(filename);

				Current.IsDirty = false;
				Current.IsFileDirty = false;
				Current.Filename = filename;
				RefreshTitle();

				MRUList.AddToMRU(filename, Current.Card.name);
				return true;
			}
			else
			{
				MessageBox.Show(Resources.error_save_character_card, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}

		private bool SaveAs()
		{
			string filename = string.Concat(Utility.FirstNonEmpty(Current.Card.name, Current.Character.spokenName, Constants.DefaultName), ".png");

			// Save as...
			saveFileDialog.Filter = "Ginger character card|*.png";
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

#if DEBUG
			if (Current.AllRecipes.IsEmpty())
				return true; // Nothing to save
#endif

			var mr = MessageBox.Show(Resources.msg_save_changes, caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
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

			IEnumerable<Searchable> lsSearchables = recipeList.GetSearchables().Where(s => s.control.Enabled);
			if (e.reverse)
				lsSearchables = lsSearchables.Reverse();

			var searchables = lsSearchables.ToArray();

			if (searchables.Length == 0)
			{
				// Nothing to search
				MessageBox.Show(Resources.msg_no_match, Resources.cap_find, MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			TextBoxBase focused = GetFocusedControl() as TextBoxBase;
			int offset = 0;

			int idxFocused = Array.FindIndex(searchables, s => s.control == focused);
			if (idxFocused == -1)
				idxFocused = 0;

			if (focused != null)
			{
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
				int find = searchable.control.Find(e.match, e.matchCase, e.wholeWord, e.reverse, i == 0 ? offset : -1);
				if (find != -1)
				{
					if (searchable.panel.Collapsed && searchable.panel.CanExpand)
					{
						searchable.panel.Collapsed = false;
						recipeList.RefreshLayout();
						recipeList.ScrollToPanel(searchable.panel);
					}
					searchable.control.Select(find, e.match.Length);
					(searchable.control as Control).Focus();
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

		public static void RefreshSpellChecking()
		{
			if (SpellChecker.IsInitialized && AppSettings.Settings.SpellChecking)
			{
				var spellChecked = instance.recipeList.FindAllControlsOfType<TextBoxBase>().OfType<ISpellChecked>();
				foreach (var control in spellChecked)
					control.SpellCheck(true, true);
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

			var panels = instance.recipeList.FindAllControlsOfType<MultiTextParameterPanel>();
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

		public static void SetStatusBarMessage(string message)
		{
			instance.statusBarMessage.Text = message;
			instance.statusBar.Refresh();
		}

		public static void ClearStatusBarMessage()
		{
			instance.statusBarMessage.Text = null;
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
	}
}
