using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ginger
{
	public static class Generator
	{
		public struct Output
		{
			public Output(Output other)
			{
				system = other.system;
				system_post_history = other.system_post_history;
				persona = other.persona;
				scenario = other.scenario;
				example = other.example;
				userPersona = other.userPersona;
				grammar = other.grammar;
				personality = other.personality;

				if (other.greetings != null)
				{
					this.greetings = new GingerString[other.greetings.Length];
					Array.Copy(other.greetings, this.greetings, this.greetings.Length);
				}
				else
					this.greetings = null;

				if (other.group_greetings != null)
				{
					this.group_greetings = new GingerString[other.group_greetings.Length];
					Array.Copy(other.group_greetings, this.group_greetings, this.group_greetings.Length);
				}
				else
					this.group_greetings = null;

				if (other.lorebook != null)
					lorebook = other.lorebook.Clone();
				else
					lorebook = null;
			}

			public GingerString system;
			public GingerString system_post_history;
			public GingerString persona;		// Tavern: Description
			public GingerString personality;	// Tavern: Personality
			public GingerString userPersona;
			public GingerString scenario;
			public GingerString example;
			public GingerString grammar;
			public GingerString greeting { get { return greetings != null && greetings.Length > 0 ? greetings[0] : default(GingerString); } }
			public GingerString[] greetings;
			public GingerString[] group_greetings;
			public GingerString[] alternativeGreetings
			{ 
				get 
				{
					if (greetings == null || greetings.Length < 2)
						return new GingerString[0];

					var alts = new GingerString[greetings.Length - 1];
					Array.Copy(greetings, 1, alts, 0, greetings.Length - 1);
					return alts;
				} 
			}
			public Lorebook lorebook;

			public bool isEmpty
			{
				get
				{
					return system.IsNullOrEmpty()
						&& system_post_history.IsNullOrEmpty()
						&& persona.IsNullOrEmpty()
						&& userPersona.IsNullOrEmpty()
						&& scenario.IsNullOrEmpty()
						&& example.IsNullOrEmpty()
						&& grammar.IsNullOrEmpty()
						&& greeting.IsNullOrEmpty()
						&& !hasLore; 
				}
			}

			public bool hasLore
			{
				get
				{
					return lorebook != null && lorebook.isEmpty == false;
				}
			}

			public GingerString GetText(Recipe.Component channel)
			{
				switch (channel)
				{
				case Recipe.Component.System:
					return system;
				case Recipe.Component.System_PostHistory:
					return system_post_history;
				case Recipe.Component.Persona:
					return persona;
				case Recipe.Component.UserPersona:
					return userPersona;
				case Recipe.Component.Scenario:
					return scenario;
				case Recipe.Component.Greeting:
					return greeting;
				case Recipe.Component.Example:
					return example;
				case Recipe.Component.Grammar:
					return grammar;
				default:
					return default(GingerString);
				}
			}

			public void SetText(Recipe.Component channel, GingerString value)
			{
				switch (channel)
				{
				case Recipe.Component.System:
					system = value;
					break;
				case Recipe.Component.System_PostHistory:
					system_post_history = value;
					break;
				case Recipe.Component.Persona:
					persona = value;
					break;
				case Recipe.Component.UserPersona:
					userPersona = value;
					break;
				case Recipe.Component.Scenario:
					scenario = value;
					break;
				case Recipe.Component.Greeting:
					if (greetings == null || greetings.Length == 0)
						greetings = new GingerString[1] { value };
					else
						greetings[0] = value; // First greeting
					break;
				case Recipe.Component.Example:
					example = value;
					break;
				case Recipe.Component.Grammar:
					grammar = value;
					break;
				}
			}

			public bool HasText(Recipe.Component channel)
			{
				return !GetText(channel).IsNullOrEmpty();
			}

			public override int GetHashCode()
			{
				int hash = 13;
				hash ^= Utility.MakeHashCode(
					system,
					system_post_history,
					persona,
					userPersona,
					scenario,
					example,
					grammar);
				hash ^= Utility.MakeHashCode(greetings, Utility.HashOption.Default);
				hash ^= Utility.MakeHashCode(group_greetings, Utility.HashOption.Default);
				hash ^= Utility.MakeHashCode(lorebook);
				return hash;
			}
		}

		[Flags]
		public enum Option
		{
			None	= 0,
			Export	= 1 << 0,
			Bake	= 1 << 1,
			Snippet = 1 << 2,
			Linked	= 1 << 3,

			Single	= 1 << 10,
			All		= 1 << 11,
			Group	= 1 << 12,

			Preview = 1 << 20,
			Faraday = 1 << 21,
			SillyTavernV2 = 1 << 22,
			SillyTavernV3 = 1 << 23,
		}

		public static Output Generate(Option option = Option.Export)
		{
			option |= Option.All;
			List<Output> outputPerCharacter = GenerateAllCharacters(option);

			// Single character
			if (outputPerCharacter.Count == 1)
			{
#if DEBUG
				if (option.Contains(Option.Preview) == false)
					System.Diagnostics.Debug.WriteLine(string.Format("Generating text. Hash=0x{0:X8}", outputPerCharacter[0].GetHashCode()), "Debug");
#endif
				return outputPerCharacter[0];
			}

			// Combine character outputs
			int numChannels = EnumHelper.ToInt(Recipe.Component.Count);

			var outputByChannel = new GingerString[numChannels];
			GingerString[] greetings = null;
			GingerString[] group_greetings = null;
			for (int iChannel = 0; iChannel < numChannels; ++iChannel)
			{
				var eChannel = EnumHelper.FromInt(iChannel, Recipe.Component.Invalid);
			
				if (eChannel == Recipe.Component.Greeting)
				{
					greetings = outputPerCharacter
						.Where(o => o.greetings != null)
						.SelectMany(o => o.greetings)
						.Where(g => g.IsNullOrEmpty() == false)
						.ToArray(); 
				}
				else if (eChannel == Recipe.Component.Greeting_Group)
				{
					group_greetings = outputPerCharacter
						.Where(o => o.group_greetings != null)
						.SelectMany(o => o.group_greetings)
						.Where(g => g.IsNullOrEmpty() == false)
						.ToArray(); 
				}
				else
				{
					// Concatenate channels
					var texts = outputPerCharacter
						.Select(o => o.GetText(eChannel))
						.Where(g => g.IsNullOrEmpty() == false)
						.ToArray();

					if (texts.Length == 1)
						outputByChannel[iChannel] = texts[0];
					else if (texts.Length > 1)
						outputByChannel[iChannel] = GingerString.Join(Text.ParagraphBreak, texts);
				}
			}

			var lorebook = Lorebook.Merge(outputPerCharacter
				.Select(o => o.lorebook)
				.NotNull()
				.ToList());

			var output = new Output() 
			{
				system = outputByChannel[0],
				system_post_history = outputByChannel[7],
				persona = outputByChannel[1],
				userPersona = outputByChannel[2],
				scenario = outputByChannel[3],
				example = outputByChannel[4],
				grammar = outputByChannel[5],
				greetings = greetings,
				group_greetings = group_greetings,
				lorebook = lorebook,
			};
#if DEBUG
			if (option.Contains(Option.Preview) == false)
				System.Diagnostics.Debug.WriteLine(string.Format("Generating text. Hash=0x{0:X8}", output.GetHashCode()), "Debug");
#endif
			return output;
		}

		private static List<Output> GenerateAllCharacters(Option option)
		{
			List<Output> outputPerCharacter = new List<Output>();

			Recipe internalGlobalRecipe = RecipeBook.GetRecipeByID(RecipeBook.GlobalInternal)?.Instantiate();
			Recipe externalGlobalRecipe = RecipeBook.GetRecipeByID(RecipeBook.GlobalExternal)?.Instantiate();
			Recipe pruneScenarioRecipe = RecipeBook.GetRecipeByID(RecipeBook.PruneScenario)?.Instantiate();

			for (int index = 0; index < Current.Characters.Count; ++index)
			{
				var character = Current.Characters[index];

				var recipes = new List<Recipe>(character.recipes.Count + 1);
				if (externalGlobalRecipe != null)
					recipes.Insert(0, externalGlobalRecipe);
				if (internalGlobalRecipe != null)
					recipes.Insert(0, internalGlobalRecipe);
				if (pruneScenarioRecipe != null && Current.Card.extraFlags.Contains(CardData.Flag.PruneScenario))
					recipes.Insert(0, pruneScenarioRecipe);
				recipes.AddRange(character.recipes);

				var context = character.GetContext(CharacterData.ContextType.None, option, false);
				if (option.Contains(Option.Faraday))
				{
					context.SetFlag("__faraday");
					context.SetFlag("__backyard");
					if (option.Contains(Option.Linked))
					{
						context.SetFlag("__link");
						if (AppSettings.BackyardLink.WriteUserPersona)
							context.SetFlag("__write-user");
						if (AppSettings.BackyardLink.WriteAuthorNote)
							context.SetFlag("__author-note");
					}
				}
				else if (option.ContainsAny(Option.SillyTavernV2 | Option.SillyTavernV3))
					context.SetFlag("__tavern");
				if (option.ContainsAny(Option.SillyTavernV2))
					context.SetFlag("__ccv2");
				else if (option.ContainsAny(Option.SillyTavernV3))
					context.SetFlag("__ccv3");

				if (option.Contains(Option.Preview))
				{
					context.SetFlag("__preview");

					switch (AppSettings.Settings.PreviewFormat)
					{
					case AppSettings.Settings.OutputPreviewFormat.Default:
						context.SetFlag("__ginger");
						break;
					case AppSettings.Settings.OutputPreviewFormat.Faraday:
					case AppSettings.Settings.OutputPreviewFormat.FaradayParty:
						context.SetFlag("__backyard");
						 context.SetFlag("__faraday");
						break;
					case AppSettings.Settings.OutputPreviewFormat.SillyTavern:
						context.SetFlag("__tavern");
						break;
					case AppSettings.Settings.OutputPreviewFormat.PlainText:
						context.SetFlag("__plain");
						break;
					}
				}

				var characterOutput = Generate(recipes, index, context, option);
				outputPerCharacter.Add(characterOutput);
			}
			return outputPerCharacter;
		}

		public static Output Generate(Recipe recipe, Option option)
		{
			var recipes = RecipeBook.WithInternal(new Recipe[] { recipe });

			var context = Current.Character.GetContextForRecipe(recipe, option);
			return Generate(recipes, Current.SelectedCharacter, context, option | Option.Single);
		}

		public static Output Generate(List<Recipe> recipes, int characterIndex, Context context, Option options)
		{
			var partialOutput = BuildGraph(recipes, characterIndex, context, options);
			var blockBuilder = partialOutput.blockBuilder;

			// (Silly tavern) Build important block separately
			GingerString postHistory = GingerString.Empty;
			if (options.ContainsAny(Option.SillyTavernV2 | Option.SillyTavernV3 | Option.Snippet | Option.Bake) // SillyTavern
				|| ((options.Contains(Option.Export | Option.Linked) || options.Contains(Option.Preview | Option.Linked)) 
				&& AppSettings.BackyardLink.WriteAuthorNote)) // Backyard, linked (author's note)
				postHistory = GingerString.FromOutput(blockBuilder.Build("system/important"), characterIndex, options, Text.EvalOption.OutputFormatting);

			// (Silly tavern) Build personality block
			GingerString personality = GingerString.Empty;
			if (options.ContainsAny(Option.SillyTavernV2 | Option.SillyTavernV3) && options.ContainsAny(Option.Snippet | Option.Bake) == false)
				personality = GingerString.FromOutput(blockBuilder.Build("persona/output/personality"), characterIndex, options, Text.EvalOption.OutputFormatting);

			// Option: Prune scenario
			if (context.HasFlag(Constants.Flag.PruneScenario) && options.ContainsAny(Option.Snippet | Option.Bake) == false)
			{
				GingerString scenario = GingerString.FromOutput(blockBuilder.Build("scenario/output"), characterIndex, options, Text.EvalOption.OutputFormatting);
				blockBuilder.Add(new Block() {
					id = "example/__scenario/text",
					style = Block.Style.Undefined,
					formatting = Block.Formatting.None,
				}, scenario.ToString());
			}

			// Should insert original model instructions?
			bool bPrependOriginal = options.ContainsAny(Option.Export | Option.Preview)
				&& !(context.HasFlag(Constants.Flag.System) || context.HasFlag(Constants.Flag.Base) || context.HasFlag("system-prompt")
					|| blockBuilder.BlockHasChildren("system/output", true));

			// Omit attributes block
			if (Current.Card.extraFlags.Contains(CardData.Flag.OmitAttributes))
				blockBuilder.RemoveBlock("persona/attributes");

			// Build blocks
			blockBuilder.Build();

			var systemOutput = GingerString.FromOutput(blockBuilder.GetFinishedBlock("system"), characterIndex, options, Text.EvalOption.OutputFormatting);
			var personaOutput = GingerString.FromOutput(blockBuilder.GetFinishedBlock("persona"), characterIndex, options, Text.EvalOption.OutputFormatting);
			var userPersonaOutput = GingerString.FromOutput(blockBuilder.GetFinishedBlock("user"), characterIndex, options, Text.EvalOption.OutputFormatting);
			var scenarioOutput = GingerString.FromOutput(blockBuilder.GetFinishedBlock("scenario"), characterIndex, options, Text.EvalOption.OutputFormatting);
			var exampleOutput = GingerString.FromOutput(blockBuilder.GetFinishedBlock("example"), characterIndex, options, Text.EvalOption.ExampleFormatting);
			var greetings = partialOutput.greetings;
			var group_greetings = partialOutput.group_greetings;
			var grammar = partialOutput.grammarOutput;
			var lore = partialOutput.lore;

			// Insert original model instructions
			if (bPrependOriginal 
				&& systemOutput.IsNullOrEmpty() == false 
				&& systemOutput.ToGinger().Contains(GingerString.OriginalMarker) == false)
			{
				systemOutput = GingerString.FromString(string.Concat(GingerString.OriginalMarker, "\n\n", systemOutput.ToString()));
			}

			// Omit outputs
			if (Current.Card.extraFlags.Contains(CardData.Flag.OmitSystemPrompt))
			{
				systemOutput = GingerString.Empty;
				postHistory = GingerString.Empty;
			}
			if (Current.Card.extraFlags.Contains(CardData.Flag.OmitUserPersona))
				userPersonaOutput = GingerString.Empty;
			if (Current.Card.extraFlags.Contains(CardData.Flag.OmitScenario))
				scenarioOutput = GingerString.Empty;
			if (Current.Card.extraFlags.Contains(CardData.Flag.OmitExample))
				exampleOutput = GingerString.Empty;
			if (Current.Card.extraFlags.Contains(CardData.Flag.OmitGrammar))
				grammar = GingerString.Empty;
			if (Current.Card.extraFlags.Contains(CardData.Flag.OmitGreeting))
			{
				greetings = null;
				group_greetings = null;
			}
			if (Current.Card.extraFlags.Contains(CardData.Flag.OmitLore))
				lore = null;

			// Strip lorebook decorators
			if (lore != null)
			{
				if (!options.Contains(Option.SillyTavernV3 | Option.Export))
					lore.StripDecorators();
				lore.SortEntries(Lorebook.Sorting.ByOrder, false);
			}

			return new Output() {
				system = systemOutput,
				system_post_history = postHistory,
				persona = personaOutput,
				personality = personality,
				userPersona = userPersonaOutput,
				scenario = scenarioOutput,
				example = exampleOutput,
				grammar = grammar,
				greetings = greetings,
				group_greetings = group_greetings,
				lorebook = lore,
			};
		}

		public static Output[] GenerateMany(Option option = Option.Export)
		{
			option |= Option.Group;
			List<Output> outputPerCharacter = GenerateAllCharacters(option);

			// Combine character outputs
			int numChannels = EnumHelper.ToInt(Recipe.Component.Count);

			var outputByChannel = new GingerString[numChannels];
			GingerString[] greetings = null;
			GingerString[] group_greetings = null;
			for (int iChannel = 0; iChannel < numChannels; ++iChannel)
			{
				var eChannel = EnumHelper.FromInt(iChannel, Recipe.Component.Invalid);
			
				if (eChannel == Recipe.Component.Greeting)
				{
					greetings = outputPerCharacter
						.Where(o => o.greetings != null)
						.SelectMany(o => o.greetings)
						.Where(g => g.IsNullOrEmpty() == false)
						.ToArray(); 
				}
				else if (eChannel == Recipe.Component.Greeting_Group)
				{
					group_greetings = outputPerCharacter
						.Where(o => o.group_greetings != null)
						.SelectMany(o => o.group_greetings)
						.Where(g => g.IsNullOrEmpty() == false)
						.ToArray(); 
				}
				else if (eChannel != Recipe.Component.Persona)
				{
					// Concatenate channels
					var texts = outputPerCharacter
						.Select(o => o.GetText(eChannel))
						.Where(g => g.IsNullOrEmpty() == false)
						.ToArray();

					if (texts.Length == 1)
						outputByChannel[iChannel] = texts[0];
					else if (texts.Length > 1)
						outputByChannel[iChannel] = GingerString.Join(Text.ParagraphBreak, texts);
				}
			}

			Output[] outputs = new Output[outputPerCharacter.Count];

			// Primary output
			outputs[0] = new Output() {
				persona = outputPerCharacter[0].persona,
				lorebook = outputPerCharacter[0].lorebook,
				system = outputByChannel[0],
				system_post_history = outputByChannel[7],
				userPersona = outputByChannel[2],
				scenario = outputByChannel[3],
				example = outputByChannel[4],
				grammar = outputByChannel[5],
				greetings = greetings,
				group_greetings = group_greetings,
			};

			// Actor outputs
			for (int i = 1; i < outputPerCharacter.Count; ++i)
			{
				outputs[i] = new Output() {
					persona = outputPerCharacter[i].persona,
					lorebook = outputPerCharacter[i].lorebook,
				};
			}

			return outputs;
		}

		private struct PartialOutput
		{
			public BlockBuilder blockBuilder;
			public GingerString[] greetings;
			public GingerString[] group_greetings;
			public GingerString grammarOutput;
			public Lorebook lore;
		}

		private static PartialOutput BuildGraph(List<Recipe> recipes, int characterIndex, Context context, Option options)
		{
			BlockBuilder blockBuilder = new BlockBuilder();
			List<Lorebook> loreEntries = new List<Lorebook>();

			var randomizer = new RandomNoise(Current.seed);
			int numChannels = EnumHelper.ToInt(Recipe.Component.Count);
			List<string>[] lsOutputsByChannel = new List<string>[numChannels];
			for (int i = 0; i < numChannels; ++i)
				lsOutputsByChannel[i] = new List<string>();

			var globalContext = Context.Copy(context);
			if (options.Contains(Option.Bake) || options.Contains(Option.Snippet))
				globalContext.SetFlag("__bake");
			if (options.Contains(Option.Snippet))
				globalContext.SetFlag("__snippet");
			if (options.Contains(Option.Single))
				globalContext.SetFlag("__single");

			// Prepare contexts
			Context[] recipeContexts = ParameterResolver.GetLocalContexts(recipes.ToArray(), globalContext);

			// Adjectives & nouns
			CompileAdjectivesAndNoun(recipes, recipeContexts, randomizer);

			// Evaluate lorebooks, blocks, and attributes
			for (int i = 0; i < recipes.Count; ++i)
			{
				var recipe = recipes[i];
				if (recipe.isEnabled == false)
					continue; // Skip

				var localContext = recipeContexts[i];

				// Lorebooks
				foreach (var parameter in recipe.parameters.OfType<LorebookParameter>())
				{
					if (parameter.isEnabled == false)
						continue;

					Lorebook lorebook = new Lorebook();
					foreach (var entry in parameter.value.entries.Where(e => e.isEnabled))
					{
						Lorebook.Entry newEntry = entry.Clone();
						string key = GingerString.BakeNames(GingerString.EvaluateParameter(newEntry.key, localContext), characterIndex);
						string value = GingerString.FromParameter(GingerString.EvaluateParameter(newEntry.value, localContext)).ToString();
						value = GingerString.FromOutput(value, characterIndex, options, Text.EvalOption.LoreFormatting).ToString();

						if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
							continue;

						newEntry.key = key;
						newEntry.value = value;
						lorebook.entries.Add(newEntry);
					}

					if (lorebook.isEmpty == false)
						loreEntries.Add(lorebook);
				}

				// Blocks
				foreach (var block in recipe.blocks)
				{
					// Condition
					if (block.condition != null && block.condition.Evaluate(localContext,
							new EvaluationCookie() {
								randomizer = randomizer,
								ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
							}) == false)
						continue;

					string text = Text.Eval(block.value, localContext,
						new ContextString.EvaluationConfig() {
							macroSuppliers = new IMacroSupplier[] { recipe.strings, Current.Strings },
							referenceSuppliers = new IStringReferenceSupplier[] { recipe.strings, Current.Strings },
							ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
							randomizer = randomizer,
						},
						Text.EvalOption.None)
						.Trim();

					if (text.Length == 0)
						continue;

					// Is attribute?
					if (block is AttributeBlock)
					{
						var attribute = block as AttributeBlock;
						var evalConfig = new ContextString.EvaluationConfig() {
							macroSuppliers = new IMacroSupplier[] { recipe.strings, Current.Strings },
							referenceSuppliers = new IStringReferenceSupplier[] { recipe.strings, Current.Strings },
							ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
							randomizer = randomizer,
						};

						string attributeName = Text.Eval(attribute.name, localContext, evalConfig,
							Text.EvalOption.Capitalization | Text.EvalOption.Whitespace | Text.EvalOption.NoInternal)
							.Trim();

						string attributeLabel = Text.Eval(string.Format("[!__attrib:{0}]", attributeName), localContext, evalConfig,
							Text.EvalOption.None);

						blockBuilder.AddAttribute(attribute, attributeName, attributeLabel, text);
					}
					else
					{
						blockBuilder.Add(block, text);
					}
				}
			}

			// Evaluate recipe components
			for (int iRecipe = 0; iRecipe < recipes.Count; ++iRecipe)
			{
				var recipe = recipes[iRecipe];
				if (recipe.isEnabled == false)
					continue;

				var localContext = recipeContexts[iRecipe];

				foreach (var template in recipe.templates)
				{
					if (string.IsNullOrEmpty(template.text)
						|| template.channel == Recipe.Component.Invalid
						|| template.isDetached)
						continue;

					if (template.condition != null
						&& template.condition.Evaluate(localContext,
							new EvaluationCookie() {
								randomizer = randomizer,
								ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
							}
						) == false)
						continue;

					string text = Text.Eval(template.text, localContext,
						new ContextString.EvaluationConfig() {
							macroSuppliers = new IMacroSupplier[] { recipe.strings, Current.Strings },
							referenceSuppliers = new IStringReferenceSupplier[] { recipe.strings, Current.Strings },
							ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
							randomizer = randomizer,
						}, Text.EvalOption.None);

					if (text.Length > 0)
					{
						var channel = template.channel;
						if (channel == Recipe.Component.System && template.isImportant)
							channel = Recipe.Component.System_PostHistory;
						else if (channel == Recipe.Component.Greeting && template.isGroupOnly)
							channel = Recipe.Component.Greeting_Group;

						if (channel == Recipe.Component.Grammar)
						{
							if (options.Contains(Option.Snippet) == false)
								text = GingerString.RemoveComments(text);
							lsOutputsByChannel[(int)channel].Add(Text.DontProcess(Utility.Unindent(Text.Process(text, Text.EvalOption.Minimal))));
						}
						else if (channel == Recipe.Component.Example || channel == Recipe.Component.Greeting || channel == Recipe.Component.Greeting_Group)
							lsOutputsByChannel[(int)channel].Add(Text.DontProcess(Utility.Unindent(Text.Process(text, Text.EvalOption.ExampleFormatting))));
						else if (template.isRaw)
							lsOutputsByChannel[(int)channel].Add(Text.DontProcess(text));
						else
							lsOutputsByChannel[(int)channel].Add(text);
					}
				}

				// Lore items <Lore>
				if (recipe.loreItems.Count > 0)
				{
					var lorebook = new Lorebook();
					var usedLoreKeys = new HashSet<string>();
					foreach (var loreItem in recipe.loreItems)
					{
						if (string.IsNullOrEmpty(loreItem.key) || string.IsNullOrEmpty(loreItem.text))
							continue;

						if (loreItem.condition != null
							&& loreItem.condition.Evaluate(localContext,
								new EvaluationCookie() {
									randomizer = randomizer,
									ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
								}
							) == false)
							continue;

						string key = Text.Eval(loreItem.key, localContext,
							new ContextString.EvaluationConfig() {
								macroSuppliers = new IMacroSupplier[] { recipe.strings, Current.Strings },
								referenceSuppliers = new IStringReferenceSupplier[] { recipe.strings, Current.Strings },
								ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
								randomizer = randomizer,
							}, Text.EvalOption.Whitespace | Text.EvalOption.Minimal)
							.SingleLine()
							.Trim();

						key = GingerString.BakeNames(key, characterIndex);

						string value = GingerString.EvaluateParameter(loreItem.text, localContext);

						value = Text.Eval(value, localContext,
							new ContextString.EvaluationConfig() {
								macroSuppliers = new IMacroSupplier[] { recipe.strings, Current.Strings },
								referenceSuppliers = new IStringReferenceSupplier[] { recipe.strings, Current.Strings },
								ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
								randomizer = randomizer,
							}, Text.EvalOption.LoreFormatting)
							.Trim();

						if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
							continue;

						if (usedLoreKeys.Contains(key) == false)
						{
							value = GingerString.FromOutput(value, characterIndex, options).ToString();
							usedLoreKeys.Add(key);
							lorebook.entries.Add(new Lorebook.Entry() {
								key = key,
								value = value,
								sortOrder = loreItem.order,
							});
						}
					}

					if (lorebook.entries.Count > 0)
						loreEntries.Add(lorebook);
				}
			}

			// Combine and finalize outputs
			GingerString[] greetings = null;
			GingerString[] group_greetings = null;
			GingerString grammarOutput = new GingerString();

			for (int iChannel = 0; iChannel < numChannels; ++iChannel)
			{
				Recipe.Component channel = EnumHelper.FromInt(iChannel, Recipe.Component.Invalid);

				// Process outputs
				for (int i = 0; i < lsOutputsByChannel[iChannel].Count; ++i)
				{
					if (channel == Recipe.Component.Grammar)
						continue; // Grammar mustn't be processed

					Text.EvalOption evalOptions;
					if (channel == Recipe.Component.Persona
						|| channel == Recipe.Component.System
						|| channel == Recipe.Component.System_PostHistory
						|| channel == Recipe.Component.Scenario
						|| channel == Recipe.Component.UserPersona)
						evalOptions = Text.EvalOption.StandardOutputFormatting;
					else
					{
						evalOptions = Text.EvalOption.LimitedOutputFormatting;
					}

					string text = lsOutputsByChannel[iChannel][i];
					text = Text.Process(text, evalOptions);
					lsOutputsByChannel[iChannel][i] = text;
				}

				if (channel == Recipe.Component.Greeting || channel == Recipe.Component.Greeting_Group) // Separate greetings
				{
					StringBuilder sbGreeting = new StringBuilder();
					var lsGreetings = new List<GingerString>();
					for (int i = 0; i < lsOutputsByChannel[iChannel].Count; ++i)
					{
						string text = GingerString.FromOutput(lsOutputsByChannel[iChannel][i], characterIndex, options).ToString();
						if (sbGreeting.Length == 0)
						{
							sbGreeting.Append(text);
						}
						else
						{
							if (sbGreeting.Contains(GingerString.InternalContinueMarker, false))
							{
								sbGreeting.Replace(GingerString.InternalContinueMarker, text);
							}
							else
							{
								lsGreetings.Add(GingerString.FromString(sbGreeting.ToString()).ApplyTextStyle(Current.Card.textStyle));
								sbGreeting.Clear();
								sbGreeting.Append(text);
							}
						}
					}

					if (sbGreeting.Length > 0)
					{
						sbGreeting.Replace(GingerString.InternalContinueMarker, "");
						lsGreetings.Add(GingerString.FromString(sbGreeting.ToString()).ApplyTextStyle(Current.Card.textStyle));
					}

					if (channel == Recipe.Component.Greeting)
						greetings = lsGreetings.ToArray();
					else if (channel == Recipe.Component.Greeting_Group)
						group_greetings = lsGreetings.ToArray();
				}
				else if (channel == Recipe.Component.Example)
				{
					string text = string.Join(Text.ParagraphBreak, lsOutputsByChannel[iChannel]);
					text = GingerString.FromOutput(text, characterIndex, options, Text.EvalOption.Minimal)
						.ApplyTextStyle(Current.Card.textStyle)
						.ToString();

					blockBuilder.Add(new Block() {
						id = "example/output/value",
						style = Block.Style.Undefined,
						formatting = Block.Formatting.None,
					}, text);
				}
				else if (channel == Recipe.Component.Grammar)
				{
					string text = string.Join(Text.Separator, lsOutputsByChannel[iChannel]);
					grammarOutput = GingerString.FromOutput(text, characterIndex, options);
				}
				else
				{
					string blockID;
					switch (channel)
					{
					case Recipe.Component.System:
						blockID = "system/output/value";
						break;
					case Recipe.Component.System_PostHistory:
						blockID = "system/important/output/value";
						break;
					case Recipe.Component.UserPersona:
						blockID = "user/output/value";
						break;
					case Recipe.Component.Scenario:
						blockID = "scenario/output/value";
						break;
					default:
						blockID = "persona/output/value";
						break;
					}
					for (int i = 0; i < lsOutputsByChannel[iChannel].Count; ++i)
					{
						string text = Text.Process(lsOutputsByChannel[iChannel][i], Text.EvalOption.Minimal);
						blockBuilder.Add(new Block() {
							id = blockID,
							style = Block.Style.Undefined,
							formatting = Block.Formatting.None,
						}, text);
					}
				}
			}

			return new PartialOutput() {
				blockBuilder = blockBuilder,
				grammarOutput = grammarOutput,
				greetings = greetings,
				group_greetings = group_greetings,
				lore = Lorebook.Merge(loreEntries),
			};

		}

		private struct AdjectiveNoun
		{
			public string value;
			public int order;
			public int priority;
		}

		private static void CompileAdjectivesAndNoun(List<Recipe> recipes, Context[] recipeContexts, IRandom randomizer)
		{
			List<AdjectiveNoun> lsAdjectives = new List<AdjectiveNoun>();
			List<AdjectiveNoun> lsNouns = new List<AdjectiveNoun>();

			for (int iRecipe = 0; iRecipe < recipes.Count; ++iRecipe)
			{
				var recipe = recipes[iRecipe];
				if (recipe.isEnabled == false)
					continue;

				var localContext = recipeContexts[iRecipe];

				foreach (var adjective in recipe.adjectives)
				{
					if (adjective.order < 0)
						continue;

					if (adjective.condition != null
						&& adjective.condition.Evaluate(localContext,
							new EvaluationCookie() {
								randomizer = randomizer,
								ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
							}
						) == false)
						continue;

					string text = Text.Eval(adjective.value, localContext,
						new ContextString.EvaluationConfig() {
							macroSuppliers = new IMacroSupplier[] { recipe.strings, Current.Strings },
							referenceSuppliers = new IStringReferenceSupplier[] { recipe.strings, Current.Strings },
							ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
							randomizer = randomizer,
						}, Text.EvalOption.Minimal);

					if (string.IsNullOrEmpty(text))
						continue;

					var words = Utility.ListFromCommaSeparatedString(text);
					lsAdjectives.Add(new AdjectiveNoun() {
						value = randomizer.Item(words),
						order = adjective.order,
						priority = adjective.priority,
					});
				}

				foreach (var noun in recipe.nouns)
				{
					if (noun.condition != null
						&& noun.condition.Evaluate(localContext,
							new EvaluationCookie() {
								randomizer = randomizer,
								ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
							}
						) == false)
						continue;

					string text = Text.Eval(noun.value, localContext,
						new ContextString.EvaluationConfig() {
							macroSuppliers = new IMacroSupplier[] { recipe.strings, Current.Strings },
							referenceSuppliers = new IStringReferenceSupplier[] { recipe.strings, Current.Strings },
							ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
							randomizer = randomizer,
						}, Text.EvalOption.Minimal);

					if (string.IsNullOrEmpty(text))
						continue;

					int order;
					if (noun.affix == CharacterNoun.Affix.Prefix)
						order = -1;
					else if (noun.affix == CharacterNoun.Affix.Suffix)
						order = 1;
					else
						order = 0;

					var words = Utility.ListFromCommaSeparatedString(text);
					lsNouns.Add(new AdjectiveNoun() {
						value = randomizer.Item(words),
						order = order,
						priority = noun.priority,
					});
				}
			}

			// Select 
			var adjByOrder = lsAdjectives
				.DistinctBy(a => a.value.ToLowerInvariant())
				.GroupBy(a => a.order)
				.ToDictionary(g => g.Key, g => {
					int max = CharacterAdjective.CountByOrder[g.Key];
					return new Queue<AdjectiveNoun>(
						g.ToList()
							.Shuffle(randomizer)
							.OrderByDescending(gg => gg.priority)
							.Take(max));
				});

			const int MaxAdjectives = 8;
			var selectedAdjectives = new List<AdjectiveNoun>();
			for (;;)
			{
				bool bAdded = false;
				foreach (var key in adjByOrder.Keys)
				{
					if (adjByOrder[key].Count > 0)
					{
						selectedAdjectives.Add(adjByOrder[key].Dequeue());
						bAdded = true;
					}
				}
				if (!bAdded || selectedAdjectives.Count >= MaxAdjectives)
					break;
			}

			var adjectives = selectedAdjectives
				.GroupBy(a => a.order)
				.Select(g => new {
					order = g.First().order,
					values = g.Select(gg => gg.value),
				})
				.OrderBy(x => x.order)
				.SelectMany(x => x.values);

			string sAdjectives = string.Join(Text.Delimiter, adjectives);
			if (sAdjectives.Length > 0)
			{
				for (int i = 0; i < recipeContexts.Length; ++i)
					recipeContexts[i].SetValue(Constants.Variables.Adjectives, sAdjectives);
			}

			lsNouns = lsNouns.DistinctBy(n => n.value.ToLowerInvariant()).ToList();

			// Choose noun
			var nouns = lsNouns.Where(n => n.order == 0);
			var prefixes = lsNouns.Where(n => n.order == -1);
			var suffixes = lsNouns.Where(n => n.order == 1);
			var sNoun = SelectOne(nouns);
			var sPrefix = SelectOne(prefixes);
			var sSuffix = SelectOne(suffixes);

			if (string.IsNullOrEmpty(sNoun) == false)
			{
				// Affixes
				if (string.IsNullOrEmpty(sPrefix) == false)
				{
					if (sPrefix.EndsWith("-"))
						sNoun = string.Concat(sPrefix, sNoun);
					else
						sNoun = string.Concat(sPrefix, " ", sNoun);
				}
				if (string.IsNullOrEmpty(sSuffix) == false)
				{
					if (sSuffix.BeginsWith("-"))
						sNoun = string.Concat(sNoun, sSuffix);
					else
						sNoun =string.Concat(sNoun, " ", sSuffix);
				}

				for (int i = 0; i < recipeContexts.Length; ++i)
					recipeContexts[i].SetValue(Constants.Variables.Noun, sNoun);
			}

			string SelectOne(IEnumerable<AdjectiveNoun> adjNoun)
			{
				return adjNoun.GroupBy(n => n.priority)
				.Select(g => new {
					priority = g.First().priority,
					value = g.Select(gg => gg.value).ToList(),
				})
				.OrderByDescending(x => x.priority)
				.Take(1)
				.Select(x => x.value.Shuffle(randomizer).FirstOrDefault())
				.FirstOrDefault();
			}
		}

		public struct OutputWithNodes
		{
			public GingerString system;
			public GingerString system_post_history;
			public GingerString persona;
			public GingerString userPersona;
			public GingerString scenario;
			public GingerString example;
			public GingerString grammar;
			public GingerString[] greetings;
			public GingerString[] group_greetings;
			public Lorebook lorebook;
			public Dictionary<BlockID, string> nodes;
			public List<AttributeBlock> attributes;
		}

		public static OutputWithNodes GenerateSeparately(List<Recipe> recipes, int characterIndex, Context context, Option options)
		{
			var partialOutput = BuildGraph(recipes, characterIndex, context, options);
			var blockBuilder = partialOutput.blockBuilder;
			
			var nodes = new Dictionary<BlockID, string>();
			var attributes = new List<AttributeBlock>();

			// Post-history instructions
			GingerString postHistory = GingerString.Empty;
			if (options.ContainsAny(Option.SillyTavernV2 | Option.SillyTavernV3 | Option.Snippet | Option.Bake) // SillyTavern
				|| ((options.Contains(Option.Export | Option.Linked) || options.Contains(Option.Preview | Option.Linked)) 
				&& AppSettings.BackyardLink.WriteAuthorNote)) // Backyard, linked (author's note)
				postHistory = GingerString.FromOutput(blockBuilder.Build("system/important"), characterIndex, options, Text.EvalOption.OutputFormatting);

			// Standard output:
			var systemOutput = GingerString.FromOutput(blockBuilder.Build("system/output"), characterIndex, options, Text.EvalOption.OutputFormatting);
			var personaOutput = GingerString.FromOutput(blockBuilder.Build("persona/output"), characterIndex, options, Text.EvalOption.OutputFormatting);
			var userPersonaOutput = GingerString.FromOutput(blockBuilder.Build("user/output"), characterIndex, options, Text.EvalOption.OutputFormatting);
			var scenarioOutput = GingerString.FromOutput(blockBuilder.Build("scenario/output"), characterIndex, options, Text.EvalOption.OutputFormatting);
			var exampleOutput = GingerString.FromOutput(blockBuilder.Build("example/output"), characterIndex, options, Text.EvalOption.ExampleFormatting);

			blockBuilder.RemoveBlock("system", false);
			blockBuilder.RemoveBlock("system/important", false);
			blockBuilder.RemoveBlock("persona", false);
			blockBuilder.RemoveBlock("user", false);
			blockBuilder.RemoveBlock("scenario", false);
			blockBuilder.RemoveBlock("example", false);
			blockBuilder.ClearFinishedBlocks();

			// Attributes:
			foreach (var attribute in blockBuilder.attributes)
			{
				var attributeOutput = GingerString.FromOutput(blockBuilder.Build(string.Concat(attribute.id.ToString(), "/value")), characterIndex, options, Text.EvalOption.OutputFormatting);
				if (attributeOutput.IsNullOrEmpty())
					continue;

				attributes.Add(new AttributeBlock() {
					id = new BlockID(attribute.name.Replace("/", "_")),
					name = attribute.name,
					style = attribute.style,
					mode = attribute.mode,
					formatting = attribute.formatting,
					order = attribute.order,
					value = attributeOutput.ToString(),
				});
			}

			blockBuilder.RemoveBlock("persona/attributes", false);
			blockBuilder.ClearFinishedBlocks();

			// Nodes:
			blockBuilder.Build();
			foreach (var nodeID in blockBuilder.finishedBlocks)
			{
				string text = blockBuilder.GetFinishedBlock(nodeID);
				if (string.IsNullOrWhiteSpace(text) == false)
					nodes.TryAdd(nodeID, text);
			}

			return new OutputWithNodes() {
				system = systemOutput,
				system_post_history = postHistory,
				persona = personaOutput,
				userPersona = userPersonaOutput,
				scenario = scenarioOutput,
				example = exampleOutput,
				grammar = partialOutput.grammarOutput,
				greetings = partialOutput.greetings,
				group_greetings = partialOutput.group_greetings,
				lorebook = partialOutput.lore,
				nodes = nodes,
				attributes = attributes,
			};
		}

	}
}
