﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Ginger.Properties;
using Ginger.Integration;

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
			// Open file...
			importFileDialog.Title = Resources.cap_import_character;
			importFileDialog.Filter = "All supported types|*.png;*.json;*.charx;*.yaml|PNG files|*.png|JSON files|*.json|CHARX files|*.charx|YAML files|*.yaml";
			importFileDialog.FilterIndex = AppSettings.User.LastImportCharacterFilter;
			importFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
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

					Image image;
					if (Utility.LoadImageFromFile(pngFilename, out image))
						Current.Card.portraitImage = ImageRef.FromImage(image);
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

			if (string.IsNullOrEmpty(filename) == false)
			{
				if (AppSettings.User.LastExportCharacterFilter == 5 
					|| AppSettings.User.LastExportCharacterFilter == 6
					|| AppSettings.User.LastExportCharacterFilter == 7) // png
					filename = string.Concat(filename, ".png");
				else if (AppSettings.User.LastExportCharacterFilter == 8) // charx
					filename = string.Concat(filename, ".charx");
				else if (AppSettings.User.LastExportCharacterFilter == 9) // yaml
					filename = string.Concat(filename, ".yaml");
				else // json
					filename = string.Concat(filename, ".json");
			}

			// Save as...
			exportFileDialog.Title = Resources.cap_export_character;
			exportFileDialog.Filter = "Character Card V2 JSON|*.json|Character Card V3 JSON|*.json|Agnai Character JSON|*.json|PygmalionAI Character JSON|*.json|Character Card V2 PNG|*.png|Character Card V3 PNG|*.png|Backyard AI PNG|*.png|CharX file|*.charx|Text generation web ui YAML|*.yaml";
			exportFileDialog.FileName = Utility.ValidFilename(filename);
			exportFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			exportFileDialog.FilterIndex = AppSettings.User.LastExportCharacterFilter;
			var result = exportFileDialog.ShowDialog();
			if (result != DialogResult.OK || string.IsNullOrWhiteSpace(exportFileDialog.FileName))
				return;

			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(exportFileDialog.FileName);
			AppSettings.User.LastExportCharacterFilter = exportFileDialog.FilterIndex;

			var output = Generator.Generate(Generator.Option.Export);
			var gingerExt = GingerExtensionData.FromOutput(Generator.Generate(Generator.Option.Snippet));


			if (exportFileDialog.FilterIndex == 1) // Tavern V2
			{
				var card = TavernCardV2.FromOutput(output);
				card.data.extensions.ginger = gingerExt;

				string tavernJson = card.ToJson();
				if (tavernJson != null && FileUtil.ExportTextFile(exportFileDialog.FileName, tavernJson))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == 2) // Tavern V3
			{
				var card = TavernCardV3.FromOutput(output);
				card.data.extensions.ginger = gingerExt;

				var assets = (AssetCollection)Current.Card.assets.Clone();
				
				assets.AddPortraitImage(FileUtil.FileType.Json);
				assets.Validate();

				card.data.assets = assets
					.Select(a => a.ToV3Asset(AssetFile.UriFormat.Data))
					.ToArray();

				string tavernJson = card.ToJson();
				if (tavernJson != null && FileUtil.ExportTextFile(exportFileDialog.FileName, tavernJson))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == 3) // Agnaistic
			{
				var card = AgnaisticCard.FromOutput(output);
				card.extensions.ginger = gingerExt;

				// Avatar image
				if (Current.Card.portraitImage != null)
					card.avatar = Utility.ImageToBase64(Current.Card.portraitImage);

				string agnaisticJson = card.ToJson();
				if (agnaisticJson != null && FileUtil.ExportTextFile(exportFileDialog.FileName, agnaisticJson))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == 4) // Pygmalion / CAI
			{
				var card = PygmalionCard.FromOutput(output);
				string json = card.ToJson();
				if (json != null && FileUtil.ExportTextFile(exportFileDialog.FileName, json))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == 5) // PNGv2
			{
				// Open in another instance?
				if (FileMutex.CanAcquire(exportFileDialog.FileName) == false)
				{
					MessageBox.Show(string.Format(Resources.error_already_open, Path.GetFileName(exportFileDialog.FileName)), Resources.cap_export_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				if (FileUtil.Export(exportFileDialog.FileName, (Image)Current.Card.portraitImage ?? DefaultPortrait.Image, FileUtil.Format.SillyTavernV2))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == 6) // PNGv3
			{
				// Open in another instance?
				if (FileMutex.CanAcquire(exportFileDialog.FileName) == false)
				{
					MessageBox.Show(string.Format(Resources.error_already_open, Path.GetFileName(exportFileDialog.FileName)), Resources.cap_export_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				if (FileUtil.Export(exportFileDialog.FileName, (Image)Current.Card.portraitImage ?? DefaultPortrait.Image, FileUtil.Format.SillyTavernV3))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == 7) // Faraday PNG
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
			else if (exportFileDialog.FilterIndex == 8) // CharX
			{
				if (FileUtil.ExportToCharX(exportFileDialog.FileName))
					return; // Success
			}
			else if (exportFileDialog.FilterIndex == 9) // Text Generation WebUI Yaml
			{
				var card = TextGenWebUICard.FromOutput(output);
				string yaml = card.ToYaml();
				if (yaml != null && FileUtil.ExportTextFile(exportFileDialog.FileName, yaml))
				{
					// Save portrait
					if (Current.Card.portraitImage != null)
					{
						var pngFilename = Path.Combine(Path.GetDirectoryName(exportFileDialog.FileName), string.Concat(Path.GetFileNameWithoutExtension(exportFileDialog.FileName), ".png"));
						FileUtil.ExportPNG(pngFilename, Current.Card.portraitImage, false);
					}
					return; // Success
				}
			}
			MessageBox.Show(Resources.error_write_json, Resources.cap_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

				MRUList.AddToMRU(filename, Current.Card.name);

				SetStatusBarMessage(Resources.status_file_save, Constants.StatusBarMessageInterval);

				if (bShouldAutosave)
				{
					if (autosaveError == Backyard.Error.NoError)
						SetStatusBarMessage(Resources.status_link_saved, Constants.StatusBarMessageInterval);
					else if (autosaveError == Backyard.Error.NotFound)
					{
						MessageBox.Show(Resources.error_link_save_character, Resources.cap_link_save_character, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MessageBox.Show(string.Format(Resources.error_link_read_characters, Backyard.LastError ?? ""), Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				AppSettings.BackyardLink.Enabled = false;
			}

			dlg.Characters = Backyard.Characters.ToArray();
			dlg.Folders = Backyard.Folders.ToArray();
			if (dlg.ShowDialog() != DialogResult.OK)
				return false;

			if (ConfirmSave(Resources.cap_import_character) == false)
				return false;

			SetStatusBarMessage(Resources.status_open_character);

			// Import...
			FaradayCardV4 faradayData;
			string[] images;
			var importError = Backyard.ImportCharacter(dlg.SelectedCharacter, out faradayData, out images);
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
			Current.ReadFaradayCard(faradayData, null);

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

			FaradayCardV4 card = FaradayCardV4.FromOutput(Generator.Generate(Generator.Option.Export | Generator.Option.Faraday));

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
				var mr = MessageBox.Show(Resources.msg_link_confirm_overwrite, Resources.cap_link_overwrite, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
				if (mr == DialogResult.Cancel)
					return Backyard.Error.CancelledByUser;
				else if (mr == DialogResult.No)
					return Backyard.Error.DismissedByUser;
			}

			DateTime updateDate;
			Backyard.Link.Image[] imageLinks;
			error = Backyard.UpdateCharacter(card, Current.Link, out updateDate, out imageLinks);
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
			
			FaradayCardV4 card = FaradayCardV4.FromOutput(Generator.Generate(Generator.Option.Export | Generator.Option.Faraday));
			Backyard.ImageInput[] imageInput = GatherImages();
			var error = Backyard.CreateNewCharacter(card, imageInput, null, out createdCharacter, out images);
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

		private Backyard.ImageInput[] GatherImages()
		{
			var lsImages = new List<Backyard.ImageInput>();

			// Main portrait
			if (Current.Card.portraitImage != null)
			{
				lsImages.Add(new Backyard.ImageInput() {
					image = Current.Card.portraitImage,
					fileExt = "png",
				});
			}
			else
			{
				lsImages.Add(new Backyard.ImageInput() {
					image = DefaultPortrait.Image,
					fileExt = "png",
				});
			}

			if (Current.Card.assets != null)
			{
				foreach (var asset in Current.Card.assets
					.Where(a => a.isEmbeddedAsset
						&& (a.assetType == AssetFile.AssetType.Icon || a.assetType == AssetFile.AssetType.Expression)
						&& a.data.length > 0))
				{
					lsImages.Add(new Backyard.ImageInput() {
						asset = asset,
						fileExt = asset.ext,
					});
				}
			}
			return lsImages.ToArray();
		}

		private bool ReestablishLink(bool bSilent = false)
		{
			// Refresh character information
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				if (bSilent == false)
				{
					MessageBox.Show(Resources.error_link_failed, Resources.cap_link_error, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
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

					if (bSilent == false)
						SetStatusBarMessage(Resources.status_link_reestablished, Constants.StatusBarMessageInterval);
					return true;
				}
				else if (bSilent == false)
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
			string[] images;
			var importError = Backyard.ImportCharacter(characterInstance, out faradayData, out images);
			if (importError != Backyard.Error.NoError)
				return importError;

			// Success
			Current.ReadFaradayCard(faradayData, null);

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
	}
}
