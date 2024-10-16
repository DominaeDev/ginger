﻿using System;
using System.Collections.Generic;
using System.Linq;
using Ginger.Properties;

namespace Ginger
{
	public class CharacterData
	{
		public string spokenName
		{
			get
			{
				if (string.IsNullOrWhiteSpace(_spokenName) == false)
					return _spokenName.Trim();
				else if (isMainCharacter && Current.Card.name != null)
					return Current.Card.name.Trim();
				return "";
			}
			set { _spokenName = value; }
		}

		public string namePlaceholder
		{
			get { return string.IsNullOrWhiteSpace(spokenName) ? Constants.DefaultCharacterName : spokenName.Trim(); }
		}

		public string _spokenName = null;
		public string gender = "";

		public List<Recipe> recipes = new List<Recipe>();
		public bool isMainCharacter { get { return this == Current.MainCharacter; } }

		public CharacterData Clone()
		{
			CharacterData clone = (CharacterData)this.MemberwiseClone();
			clone.recipes = new List<Recipe>(recipes.Count);
			foreach (var recipe in recipes)
				clone.recipes.Add((Recipe)recipe.Clone());
			return clone;
		}

		public enum ContextType
		{
			None,		// Card info
			FlagsOnly,	// + Recipe flags
			Full,		// + Parameters
		}

		public Context GetContext(ContextType type, bool includeInactive = false)
		{
			Context context = Context.CreateEmpty();
			// Name(s)
			context.SetValue("card", Utility.FirstNonEmpty(Current.Card.name, Current.Name, Constants.DefaultCharacterName));
			context.SetValue("name", Utility.FirstNonEmpty(this.spokenName, Current.Card.name, Constants.DefaultCharacterName));
			context.SetValue("#name", GingerString.InternalUserMarker);
			context.SetValue("names", 
				string.Join(Text.Delimiter,
					Current.Characters.Select(c => c.spokenName)
					.Where(s => string.IsNullOrEmpty(s) == false)));
			context.SetValue("actors",
				string.Join(Text.Delimiter,
				Current.Characters
					.Except(new CharacterData[] { Current.MainCharacter })
					.Select(c => c.spokenName)
					.Where(s => string.IsNullOrEmpty(s) == false)));

			// Gender
			if (string.IsNullOrWhiteSpace(this.gender) == false)
			{
				context.SetValue("gender", this.gender.ToLowerInvariant());
				context.AddTag(this.gender);

				bool bFuta = GenderSwap.IsFutanari(this.gender);
				if (bFuta)
					context.AddTag("futanari");

				// Custom gender?
				if (!(string.Compare(this.gender, "male", true) == 0 
					|| string.Compare(this.gender, "female", true) == 0 
					|| bFuta))
				{
					context.AddTag("custom-gender");
				}
			}

			// Gender (user)
			if (Current.Card.userGender != null)
			{
				var userGender = Current.Card.userGender.ToLowerInvariant();
				context.SetValue("user-gender", userGender);
				context.SetValue("#gender", userGender);
			}

			// Is actor?
			if (isMainCharacter == false)
				context.AddTag(Constants.Flag.Actor);
			if (Current.Characters.Count > 1)
			{
				context.AddTag("__multi");
				context.AddTag("multi-character");
			}

			// Allow nsfw?
			if (AppSettings.Settings.AllowNSFW)
				context.AddTag("allow-nsfw");

			// Level of detail
			switch (Current.Card.detailLevel)
			{
			case CardData.DetailLevel.Low: 
				context.AddTag("less-detail");
				break;
			case CardData.DetailLevel.High:
				context.AddTag("more-detail");
				break;
			}
			context.SetValue("detail", EnumHelper.ToInt(Current.Card.detailLevel));

			// Text style
			context.SetValue("text-style", EnumHelper.ToInt(Current.Card.textStyle));

			switch (Current.Card.textStyle)
			{
			case CardData.TextStyle.None:
				break;
			case CardData.TextStyle.Novel:
				context.AddTag("__style-quotes");
				break;
			case CardData.TextStyle.Chat:
				context.AddTag("__style-action-asterisks");
				break;
			case CardData.TextStyle.Mixed:
				context.AddTag("__style-quotes");
				context.AddTag("__style-action-asterisks");
				break;
			case CardData.TextStyle.Decorative:
				context.AddTag("__style-quotes-decorative");
				break;
			case CardData.TextStyle.Japanese:
				context.AddTag("__style-quotes-cjk");
				break;
			case CardData.TextStyle.Parentheses:
				context.AddTag("__style-action-brackets");
				break;
			case CardData.TextStyle.Bold:
				context.AddTag("__style-action-bold");
				break;
			}

			// Flags
			if (Current.Card.extraFlags.Contains(CardData.Flag.PruneScenario))
				context.AddTag(Constants.Flag.PruneScenario);

			if (type == ContextType.Full)
			{
				ParameterStates parameterStates = new ParameterStates(recipes);
				Context evalContext = Context.Copy(context);

				// Collect global flags
				var globalFlags = new HashSet<StringHandle>();
				for (int i = 0; i < recipes.Count; ++i)
				{
					if (recipes[i].isEnabled)
						globalFlags.UnionWith(recipes[i].flags);
				}

				// Create parameter states
				for (int i = 0; i < recipes.Count; ++i)
				{
					var state = new ParameterState();
					state.evalContext = evalContext;
					state.SetFlags(globalFlags, ParameterScope.Global);
					parameterStates[i] = state;
				}

				for (int i = 0; i < recipes.Count; ++i)
				{
					var recipe = recipes[i];
					if (recipe.isEnabled == false && includeInactive == false)
						continue; // Skip

					var state = new ParameterState();
					parameterStates[i] = state;

					state.evalContext = evalContext;
					state.evalConfig = new ContextString.EvaluationConfig() {
						macroSuppliers = new IMacroSupplier[] { recipe.strings, Current.Strings },
						referenceSuppliers = new IStringReferenceSupplier[] { recipe.strings, Current.Strings },
						ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
						valueSuppliers = new IValueSupplier[] { parameterStates },
					};
					foreach (var parameter in recipe.parameters.OrderByDescending(p => p.isImmediate))
						parameter.Apply(state);
					state.SetFlags(recipe.flags, ParameterScope.Global);
				}

				return evalContext;
			}
			else if (type == ContextType.FlagsOnly)
			{
				foreach (var recipe in recipes)
				{
					if (recipe.isEnabled == false && includeInactive == false)
						continue;

					context.AddTags(recipe.flags);
				}
			}			

			return context;
		}

		public Context GetContextForRecipe(Recipe targetRecipe)
		{
			int index = recipes.IndexOf(targetRecipe);
			if (index == -1)
				return GetContext(ContextType.None);

			// Prepare contexts
			var localContexts = Generator.GetRecipeContexts(recipes.ToArray(), GetContext(ContextType.None));
			return localContexts[index];
		}

		public Recipe AddRecipe(RecipeTemplate recipeTemplate)
		{
			var instances = AddRecipe(recipeTemplate, false);
			if (instances.Length > 0)
				return instances[0];
			return null;
		}

		public Recipe[] AddRecipe(RecipeTemplate recipeTemplate, bool bIncludes)
		{
			var recipe = recipeTemplate.Instantiate();

			List<Recipe> instances = new List<Recipe>();
			if (AddRecipe(recipe))
				instances.Add(recipe);

			if (bIncludes)
			{
				// Add includes
				foreach (var include in recipe.includes)
				{
					var includeRecipeTemplate = RecipeBook.GetRecipeByID(include);
					if (includeRecipeTemplate != null)
					{
						if (includeRecipeTemplate.requires != null)
						{
							Context context = GetContext(ContextType.FlagsOnly);

							if (includeRecipeTemplate.requires.Evaluate(context,
								new EvaluationCookie() { ruleSuppliers = Current.RuleSuppliers }) == false)
								continue;
						}

						instances.AddRange(AddRecipe(includeRecipeTemplate, false));
					}
				}
			}

			if (bIncludes || recipe.isSnippet)
			{
				// Detached templates
				IEnumerable<Recipe.Template> detachedTemplates;
				if (recipe.isSnippet)
					detachedTemplates = recipe.templates;
				else
					detachedTemplates = recipe.templates.Where(t => t.isDetached);

				foreach (var template in detachedTemplates)
				{
					string xmlSource;
					switch (template.channel)
					{
					case Recipe.Component.System: 
						if (template.isImportant)
							xmlSource = Resources.post_history_recipe; 
						else
							xmlSource = Resources.system_recipe;
						break;
					case Recipe.Component.Persona: xmlSource = Resources.persona_recipe; break;
					case Recipe.Component.Scenario: xmlSource = Resources.scenario_recipe; break;
					case Recipe.Component.Greeting: 
						if (template.isGroupOnly)
							xmlSource = Resources.group_greeting_recipe; 
						else
							xmlSource = Resources.greeting_recipe; 
						break;
					case Recipe.Component.Example: xmlSource = Resources.example_recipe; break;
					case Recipe.Component.Grammar: xmlSource = Resources.grammar_recipe; break;
					case Recipe.Component.UserPersona: xmlSource = Resources.user_recipe; break;
					default:
						continue;
					}

					Context context = GetContext(ContextType.FlagsOnly);

					if (template.condition != null && template.condition.Evaluate(context, new EvaluationCookie() { ruleSuppliers = Current.RuleSuppliers }) == false)
						continue;

					var editRecipe = RecipeBook.AddRecipeFromResource(xmlSource);
					if (editRecipe != null)
					{
						string text = template.text;
						GenderSwap.FromNeutralMarkers(ref text); // {them} -> him

						text = GingerString.FromString(
							Text.Eval(text, context,
								new ContextString.EvaluationConfig() {
									macroSuppliers = new IMacroSupplier[] { recipe.strings, Current.Strings },
									referenceSuppliers = new IStringReferenceSupplier[] { recipe.strings, Current.Strings },
									ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
								},
							Text.EvalOption.Minimal))
						.ToBaked();

						(editRecipe.parameters[0] as TextParameter).value = text;

						instances.Add(editRecipe);
					}
				}
			}
			return instances.ToArray();
		}

		public bool AddRecipe(Recipe recipe)
		{
			if (recipe.allowMultiple == false && recipes.ContainsAny(r => r.uid == recipe.uid))
				return false; // Already added

			if (recipe.isSnippet)
				return false; // Don't add snippets directly

			int index = 0;
			foreach (var existing in recipes.Where(r => r.id == recipe.id))
				index = Math.Max(index, recipe.instanceIndex + 1);
			recipe.instanceIndex = index;

			if (recipe.isBase)
				recipes.Insert(0, recipe);
			else
				recipes.Add(recipe);

			Current.IsDirty = true;
			return true;
		}

		public Recipe[] AddRecipePreset(RecipePreset preset)
		{
			List<Recipe> instances = new List<Recipe>();

			// Add recipes
			foreach (var include in preset.recipes)
			{
				var includedRecipe = RecipeBook.GetRecipeByID(include.id);
				if (includedRecipe == null)
					continue;

				var instance = AddRecipe(includedRecipe);
				if (instance != null)
				{
					Context context = Current.Character.GetContext(ContextType.FlagsOnly);
					var evalConfig = new ContextString.EvaluationConfig() {
						macroSuppliers = new IMacroSupplier[] { Current.Strings },
						referenceSuppliers = new IStringReferenceSupplier[] { Current.Strings },
						ruleSuppliers = new IRuleSupplier[] { Current.Strings },
					};

					foreach (var parameterInfo in include.parameters)
					{
						var parameter = instance.GetParameter(parameterInfo.id);
						if (parameter == null)
							continue;

						string value = parameterInfo.value;
						if (string.IsNullOrEmpty(value) == false)
							value = GingerString.FromString(Text.Eval(value, context, evalConfig, Text.EvalOption.Minimal)).ToBaked();
						
						instance.SetParameterValue(parameterInfo.id, value, parameterInfo.enabled);
					}
					instance.isCollapsed = include.collapsed;

					instances.Add(instance);
				}
			}

			Current.IsDirty = true;
			return instances.ToArray();
		}

		public bool RemoveRecipe(Recipe recipe)
		{
			if (recipes.Remove(recipe))
			{
				Current.IsDirty = true;
				return true;
			}
			return false;
		}
	}

}
