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
				context = other.context;

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
			public GingerString persona;        // Tavern: Description
			public GingerString personality;    // Tavern: Personality
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
			public Context context;

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

			public Output WithText(Recipe.Component channel, GingerString value)
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

				return (Output)this.MemberwiseClone();
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
			None = 0,
			Export = 1 << 0,
			Bake = 1 << 1,
			Snippet = 1 << 2,
			Linked = 1 << 3,

			Single = 1 << 10,
			All = 1 << 11,
			Group = 1 << 12,

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
			var context = outputPerCharacter[0].context;
			for (int i = 1; i < outputPerCharacter.Count; ++i)
				context = Context.Merge(context, outputPerCharacter[i].context);

			var output = new Output() {
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
				context = context,
			};
#if DEBUG
			if (option.Contains(Option.Preview) == false)
				System.Diagnostics.Debug.WriteLine(string.Format("Generating text. Hash=0x{0:X8}", output.GetHashCode()), "Debug");
#endif
			return output;
		}

		private static List<Output> GenerateAllCharacters(Option options)
		{
			Recipe internalGlobalRecipe = RecipeBook.GetRecipeByID(RecipeBook.GlobalInternal)?.Instantiate();
			Recipe externalGlobalRecipe = RecipeBook.GetRecipeByID(RecipeBook.GlobalExternal)?.Instantiate();
			Recipe pruneScenarioRecipe = RecipeBook.GetRecipeByID(RecipeBook.PruneScenario)?.Instantiate();

			List<PartialOutput> partialOutputPerCharacter = new List<PartialOutput>();
			List<Recipe> perActorRecipes = new List<Recipe>();

			for (int index = 0; index < Current.Characters.Count; ++index)
			{
				var character = Current.Characters[index];

				var perActorRecipe = GetPerActorRecipe(character);
				perActorRecipes.Add(perActorRecipe);

				var recipes = new List<Recipe>(character.recipes.Count + 4);
				if (internalGlobalRecipe != null) // First
					recipes.Add(internalGlobalRecipe);
				if (externalGlobalRecipe != null)
					recipes.Add(externalGlobalRecipe);
				if (pruneScenarioRecipe != null && Current.Card.extraFlags.Contains(CardData.Flag.PruneScenario))
					recipes.Add(pruneScenarioRecipe);

				if (perActorRecipe != null)
				{
					recipes.AddRange(character.recipes.Select(r => (Recipe)r.Clone()));
					foreach (var recipe in recipes)
					{
						recipe.templates.RemoveAll(t => t.isPerActor);
						recipe.blocks.RemoveAll(b => b.isPerActor);
					}
				}
				else
					recipes.AddRange(character.recipes);

				var context = character.GetContext(CharacterData.ContextType.None, options, false);
				if (options.Contains(Option.Faraday))
				{
					context.SetFlag("__faraday");
					context.SetFlag("__backyard");
					if (options.Contains(Option.Linked))
					{
						context.SetFlag("__link");
						if (AppSettings.BackyardLink.WriteUserPersona)
							context.SetFlag("__write-user");
						if (AppSettings.BackyardLink.WriteAuthorNote)
							context.SetFlag("__author-note");
					}
				}
				else if (options.ContainsAny(Option.SillyTavernV2 | Option.SillyTavernV3))
					context.SetFlag("__tavern");
				if (options.ContainsAny(Option.SillyTavernV2))
					context.SetFlag("__ccv2");
				else if (options.ContainsAny(Option.SillyTavernV3))
					context.SetFlag("__ccv3");

				if (options.Contains(Option.Preview))
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

				partialOutputPerCharacter.Add(BuildGraph(recipes, index, context, options));
			}

			for (int index = 0; index < Current.Characters.Count; ++index)
			{
				var character = Current.Characters[index];
				if (perActorRecipes[index] == null)
					continue;

				var perActorRecipe = perActorRecipes[index];

				for (int i = 0; i < Current.Characters.Count; ++i)
				{
					var perActorOutput = BuildGraph(new List<Recipe> { perActorRecipe }, i, partialOutputPerCharacter[i].context, options);
					partialOutputPerCharacter[index] = PartialOutput.Merge(partialOutputPerCharacter[index], perActorOutput);
				}
			}

			// Interleave example chats
			var exampleChats = Interleave(partialOutputPerCharacter
				.Where(o => o.exampleMessages != null)
				.SelectMany(o => o.exampleMessages));

			for (int i = 0; i < partialOutputPerCharacter.Count; ++i)
				partialOutputPerCharacter[i].exampleMessages = null;
			partialOutputPerCharacter[0].exampleMessages = exampleChats.ToArray();

			List<Output> outputPerCharacter = new List<Output>();
			for (int index = 0; index < Current.Characters.Count; ++index)
				outputPerCharacter.Add(FinalizeOutput(partialOutputPerCharacter[index], index, options));

			// Add style grammar?
			if (Current.Card.useStyleGrammar
				&& outputPerCharacter.Count > 0
				&& outputPerCharacter.ContainsNoneOf(o => o.grammar.IsNullOrEmpty() == false))
			{
				int textStyle = Math.Min(Math.Max(EnumHelper.ToInt(Current.Card.textStyle) - 1, 0), 7);
				Recipe grammarStyleRecipe = RecipeBook.GetRecipeByID(RecipeBook.StyleGrammar[textStyle])?.Instantiate();
				if (grammarStyleRecipe != null)
				{
					var grammarOutput = Generate(new List<Recipe> { grammarStyleRecipe }, 0, outputPerCharacter[0].context, options);
					outputPerCharacter[0] = outputPerCharacter[0].WithText(Recipe.Component.Grammar, grammarOutput.grammar);
					outputPerCharacter[0].context.SetFlags(grammarOutput.context.GetFlags());
				}
			}

			return outputPerCharacter;
		}

		private static PartialOutput.ExampleChatMessage[] Interleave(IEnumerable<PartialOutput.ExampleChatMessage> messages)
		{
			var table = new HashTable<string, PartialOutput.ExampleChatMessage[]>();
			List<PartialOutput.ExampleChatMessage> tmp = null;
			foreach (var message in messages)
			{
				if (tmp == null)
					tmp = new List<PartialOutput.ExampleChatMessage>();

				tmp.Add(message);
				if (message.name != GingerString.UserMarker)
				{
					table.Add(message.name ?? "Unknown", tmp.ToArray());
					tmp = null;
				}
			}

			if (tmp != null)
				table.Add(GingerString.UserMarker, tmp.ToArray());

			var keys = table.Keys.ToArray();

			List<PartialOutput.ExampleChatMessage> result = new List<PartialOutput.ExampleChatMessage>();
			while (true)
			{
				bool bFound = false;
				for (int i = 0; i < keys.Length; ++i)
				{
					var key = keys[i];
					if (table[key].Count > 0)
					{
						bFound = true;
						result.AddRange(table[key][0]);
						table[key].RemoveAt(0);
					}
				}
				if (!bFound)
					break;
			}

			return result.ToArray();
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
			return FinalizeOutput(partialOutput, characterIndex, options);
		}

		private static Output FinalizeOutput(PartialOutput partialOutput, int characterIndex, Option options)
		{
			var blockBuilder = partialOutput.blockBuilder;

			Context context = partialOutput.context;

			// (Silly tavern) Build important block separately
			GingerString postHistory = GingerString.Empty;
			if (options.ContainsAny(Option.SillyTavernV2 | Option.SillyTavernV3 | Option.Snippet | Option.Bake) // SillyTavern
				|| ((options.Contains(Option.Export | Option.Linked) || options.Contains(Option.Preview | Option.Linked))
				&& AppSettings.BackyardLink.WriteAuthorNote)) // Backyard, linked (author's note)
				postHistory = GingerString.FromOutput(blockBuilder.Build("system/important"), characterIndex, options, Text.EvalOption.OutputFormatting);

			// (Silly tavern) Build personality block
			GingerString personality = GingerString.Empty;
			if (options.ContainsAny(Option.SillyTavernV2 | Option.SillyTavernV3) && options.ContainsAny(Option.Snippet | Option.Bake) == false)
				personality = GingerString.FromOutput(blockBuilder.Build("persona/summary"), characterIndex, options, Text.EvalOption.OutputFormatting);

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

			// Example chat
			if (partialOutput.exampleMessages != null && partialOutput.exampleMessages.Length > 0)
			{
				foreach (var message in partialOutput.exampleMessages)
				{
					blockBuilder.Add(new Block() {
						id = "example/output/value",
						style = Block.Style.Undefined,
						formatting = Block.Formatting.None,
					}, message.ToString(Current.Card.textStyle));
				}
			}

			// Should insert original model instructions?
			bool bPrependOriginal = characterIndex == 0
				&& options.ContainsAny(Option.Export | Option.Preview)
				&& !(context.HasFlag(Constants.Flag.System) || context.HasFlag(Constants.Flag.Base) || context.HasFlag("system-prompt")
					|| blockBuilder.BlockHasChildren("system/output", true));

			// Omit attributes block
			if (Current.Card.extraFlags.Contains(CardData.Flag.OmitAttributes))
				blockBuilder.RemoveBlock("persona/attributes");
			// Omit personality block
			if (Current.Card.extraFlags.Contains(CardData.Flag.OmitPersonality))
				blockBuilder.RemoveBlock("persona/summary");

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
			if (Current.Card.extraFlags.Contains(CardData.Flag.OmitPersonality))
				personality = GingerString.Empty;
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
				context = context,
			};
		}

		public static Output[] GenerateMany(Option option = Option.Export)
		{
			List<Output> outputPerCharacter = GenerateAllCharacters(option | Option.Group);

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
				context = outputPerCharacter[0].context,
			};

			// Actor outputs
			for (int i = 1; i < outputPerCharacter.Count; ++i)
			{
				outputs[i] = new Output() {
					persona = outputPerCharacter[i].persona,
					lorebook = outputPerCharacter[i].lorebook,
					context = outputPerCharacter[i].context,
				};
			}

			return outputs;
		}

		private class PartialOutput
		{
			public BlockBuilder blockBuilder;
			public GingerString[] greetings;
			public GingerString[] group_greetings;
			public GingerString grammarOutput;
			public ExampleChatMessage[] exampleMessages;
			public Lorebook lore;
			public Context context;

			public struct ExampleChatMessage
			{
				public string name;
				public string message;
				public string userMessage;

				public string ToString(CardData.TextStyle textStyle)
				{
					StringBuilder sb = new StringBuilder();
					if (userMessage != null)
					{
						sb.Append(GingerString.UserMarker);
						sb.Append(": ");
						sb.Append(TextStyleConverter.ApplyStyle(userMessage, textStyle));
						sb.Append('\n');
					}

					if (name != null)
					{
						sb.Append(name);
						sb.Append(": ");
					}
					sb.Append(TextStyleConverter.ApplyStyle(message, textStyle));
					return sb.ToString();
				}
			}

			public static PartialOutput Merge(PartialOutput a, PartialOutput b)
			{
				BlockBuilder mergedBlocks;
				if (a.blockBuilder != null && b.blockBuilder != null)
					mergedBlocks = BlockBuilder.Merge(a.blockBuilder, b.blockBuilder);
				else if (a.blockBuilder == null && b.blockBuilder != null)
					mergedBlocks = b.blockBuilder;
				else if (a.blockBuilder != null && b.blockBuilder == null)
					mergedBlocks = a.blockBuilder;
				else
					mergedBlocks = new BlockBuilder();

				Lorebook mergedLorebook;
				if (a.lore != null && b.lore != null)
					mergedLorebook = Lorebook.Merge(new List<Lorebook> { a.lore, b.lore });
				else if (a.lore == null && b.lore != null)
					mergedLorebook = b.lore;
				else if (a.lore != null && b.lore == null)
					mergedLorebook = a.lore;
				else
					mergedLorebook = null;

				GingerString mergedGrammar;
				if (!a.grammarOutput.IsNullOrEmpty() && !b.grammarOutput.IsNullOrEmpty())
					mergedGrammar = GingerString.FromString(string.Concat(a.grammarOutput.ToString(), "\n", b.grammarOutput.ToString()));
				else if (!a.grammarOutput.IsNullOrEmpty())
					mergedGrammar = a.grammarOutput;
				else if (!b.grammarOutput.IsNullOrEmpty())
					mergedGrammar = b.grammarOutput;
				else
					mergedGrammar = GingerString.Empty;

				var mergedGreetings = Utility.ConcatenateArrays(a.greetings, b.greetings);
				var mergedGreetingsGroup = Utility.ConcatenateArrays(a.group_greetings, b.group_greetings);
				var mergedExampleChat = Utility.ConcatenateArrays(a.exampleMessages, b.exampleMessages);

				return new PartialOutput() {
					blockBuilder = mergedBlocks,
					greetings = mergedGreetings,
					group_greetings = mergedGreetingsGroup,
					grammarOutput = mergedGrammar,
					exampleMessages = mergedExampleChat,
					lore = mergedLorebook,
					context = a.context, //!
				};
			}
		}

		private static PartialOutput BuildGraph(List<Recipe> recipes, int characterIndex, Context context, Option options)
		{
			BlockBuilder graph = new BlockBuilder();
			List<Lorebook> loreEntries = new List<Lorebook>();

			var randomizer = new RandomNoise(Current.seed);
			int numChannels = EnumHelper.ToInt(Recipe.Component.Count);
			List<string> greetings_text = new List<string>();
			List<string> example_chat_text = new List<string>();
			List<string> greetings_group_text = new List<string>();
			List<string> grammar_text = new List<string>();

			var globalContext = Context.Copy(context);
			if (options.Contains(Option.Bake) || options.Contains(Option.Snippet))
				globalContext.SetFlag("__bake");
			if (options.Contains(Option.Snippet))
				globalContext.SetFlag("__snippet");
			if (options.Contains(Option.Single))
				globalContext.SetFlag("__single");

			// Prepare contexts
			Context finalContext;
			Context[] recipeContexts = ParameterResolver.GetLocalContexts(recipes.ToArray(), globalContext, out finalContext);

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

						graph.AddAttribute(attribute, attributeName, attributeLabel, text);
					}
					else
					{
						graph.Add(block, text);
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

					if (string.IsNullOrWhiteSpace(text))
						continue;

					var channel = template.channel;
					if (channel == Recipe.Component.System && template.isImportant)
						channel = Recipe.Component.System_PostHistory;
					else if (channel == Recipe.Component.Greeting && template.isGroupOnly)
						channel = Recipe.Component.Greeting_Group;

					// Process text
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

					if (channel == Recipe.Component.Grammar)
					{
						if (options.Contains(Option.Snippet) == false)
							text = GingerString.RemoveComments(text);
						text = Text.DontProcess(Utility.Unindent(Text.Process(text, Text.EvalOption.Minimal)));
					}
					else if (channel == Recipe.Component.Greeting || channel == Recipe.Component.Greeting_Group)
						text = Text.DontProcess(Utility.Unindent(Text.Process(text, Text.EvalOption.ExampleFormatting)));
					else if (channel == Recipe.Component.Example || channel == Recipe.Component.Greeting || channel == Recipe.Component.Greeting_Group)
						text = Utility.Unindent(Text.Process(text, Text.EvalOption.ExampleFormatting));
					else if (template.isRaw)
						text = Text.DontProcess(text);
					else
						text = Text.Process(text, evalOptions);

					string blockID = GetOutputNodePath(channel);

					if (blockID != null)
					{
						graph.Add(new Block() {
							id = blockID,
							style = Block.Style.Undefined,
							formatting = Block.Formatting.None,
						}, text);
					}
					else if (channel == Recipe.Component.Greeting)
						greetings_text.Add(text);
					else if (channel == Recipe.Component.Greeting_Group)
						greetings_group_text.Add(text);
					else if (channel == Recipe.Component.Example)
						example_chat_text.Add(text);
					else if (channel == Recipe.Component.Grammar)
						grammar_text.Add(text);
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
			GingerString[] greetingsOutput = null;
			GingerString[] groupGreetingsOutput = null;
			GingerString grammarOutput = new GingerString();

			if (greetings_text.Count > 0) // Compile greetings
			{
				StringBuilder sbGreeting = new StringBuilder();
				var lsOutput = new List<GingerString>();
				for (int i = 0; i < greetings_text.Count; ++i)
				{
					string text = GingerString.FromOutput(greetings_text[i], characterIndex, options).ToString();
					if (sbGreeting.Length == 0)
						sbGreeting.Append(text);
					else
					{
						if (sbGreeting.Contains(GingerString.InternalContinueMarker, false))
							sbGreeting.Replace(GingerString.InternalContinueMarker, text);
						else
						{
							lsOutput.Add(GingerString.FromString(sbGreeting.ToString()).ApplyTextStyle(Current.Card.textStyle));
							sbGreeting.Clear();
							sbGreeting.Append(text);
						}
					}
				}
				if (sbGreeting.Length > 0)
				{
					sbGreeting.Replace(GingerString.InternalContinueMarker, "");
					lsOutput.Add(GingerString.FromString(sbGreeting.ToString()).ApplyTextStyle(Current.Card.textStyle));
				}

				greetingsOutput = lsOutput.ToArray();
			}
			if (greetings_group_text.Count > 0) // Compile greetings (group-only)
			{
				StringBuilder sbGreeting = new StringBuilder();
				var lsOutput = new List<GingerString>();
				for (int i = 0; i < greetings_group_text.Count; ++i)
				{
					string text = GingerString.FromOutput(greetings_group_text[i], characterIndex, options).ToString();
					if (sbGreeting.Length == 0)
						sbGreeting.Append(text);
					else
					{
						if (sbGreeting.Contains(GingerString.InternalContinueMarker, false))
							sbGreeting.Replace(GingerString.InternalContinueMarker, text);
						else
						{
							lsOutput.Add(GingerString.FromString(sbGreeting.ToString()).ApplyTextStyle(Current.Card.textStyle));
							sbGreeting.Clear();
							sbGreeting.Append(text);
						}
					}
				}
				if (sbGreeting.Length > 0)
				{
					sbGreeting.Replace(GingerString.InternalContinueMarker, "");
					lsOutput.Add(GingerString.FromString(sbGreeting.ToString()).ApplyTextStyle(Current.Card.textStyle));
				}

				groupGreetingsOutput = lsOutput.ToArray();
			}

			List<PartialOutput.ExampleChatMessage> lsExampleChatMessages = new List<PartialOutput.ExampleChatMessage>();
			if (example_chat_text.Count > 0)
			{
				// Split the example chat into individual messages so they can be interleaved later
				foreach (var message in example_chat_text)
				{
					string text = GingerString.FromOutput(message, characterIndex, options, Text.EvalOption.Minimal).ToString();
					text = TextStyleConverter.MarkStyles(text);
					lsExampleChatMessages.AddRange(SplitChatMessage(text));
				}
			}
			if (grammar_text.Count > 0) // Grammar
			{
				string grammarText = string.Join(Text.Separator, grammar_text);
				grammarOutput = GingerString.FromOutput(grammarText, characterIndex, options);
			}

			return new PartialOutput() {
				blockBuilder = graph,
				greetings = greetingsOutput ?? new GingerString[0],
				group_greetings = groupGreetingsOutput ?? new GingerString[0],
				exampleMessages = lsExampleChatMessages.ToArray(),
				lore = Lorebook.Merge(loreEntries),
				grammarOutput = grammarOutput,
				context = finalContext,
			};

		}

		private static PartialOutput.ExampleChatMessage[] SplitChatMessage(string text)
		{
			var messages = new List<PartialOutput.ExampleChatMessage>();

			var paragraphs = text
				.Split(new string[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var paragraph in paragraphs)
			{
				var lines = paragraph.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				int row = 0;
				int firstRow = -1;
				string name = null;
				string userMessage = null;
				while (row < lines.Length)
				{
					if (lines[row].Length == 0)
					{
						++row;
						continue;
					}

					int pos_colon = lines[row].IndexOf(":</__NAME>");
					if (pos_colon != -1)
					{
						if (firstRow != -1) // New name?
						{
							if (name == GingerString.UserMarker)
							{
								userMessage = string.Join("\n", lines.Skip(firstRow).Take(row - firstRow));
							}
							else
							{
								string message = string.Join("\n", lines.Skip(firstRow).Take(row - firstRow));
								messages.Add(new PartialOutput.ExampleChatMessage() {
									name = name,
									message = message,
									userMessage = userMessage,
								});
								userMessage = null;
							}
						}

						name = lines[row].Substring(8, pos_colon - 8).Trim();
						lines[row] = lines[row].Substring(pos_colon + 10).TrimStart();
						firstRow = row;
					}
					++row;
				}

				if (firstRow != -1) // New name?
				{
					string message = string.Join("\n", lines.Skip(firstRow).Take(row - firstRow));
					messages.Add(new PartialOutput.ExampleChatMessage() {
						name = name,
						message = message,
						userMessage = userMessage,
					});
				}
				else
				{
					messages.Add(new PartialOutput.ExampleChatMessage() {
						name = null, // Unknown
						message = paragraph,
						userMessage = userMessage,
					});
				}
			}

			return messages.ToArray();
		}

		private static string GetOutputNodePath(Recipe.Component channel)
		{
			switch (channel)
			{
			case Recipe.Component.System:
				return "system/output/value";
			case Recipe.Component.System_PostHistory:
				return "system/important/output/value";
			case Recipe.Component.UserPersona:
				return "user/output/value";
			case Recipe.Component.Scenario:
				return "scenario/output/value";
			case Recipe.Component.Persona:
				return "persona/output/value";
			default:
				return null;
			}
		}

		private struct AdjectiveNoun
		{
			public string value;
			public int order;
			public int priority;
		}

		private static void CompileAdjectivesAndNoun(IList<Recipe> recipes, Context[] recipeContexts, IRandom randomizer)
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
					List<string> words;
					if (noun.affix == CharacterNoun.Affix.Addendum)
						words = new List<string> { text }; // Include commas
					else
						words = Utility.ListFromCommaSeparatedString(text); // Separate by comma

					if (noun.affix == CharacterNoun.Affix.Prefix)
						order = -1;
					else if (noun.affix == CharacterNoun.Affix.Suffix)
						order = 1;
					else if (noun.affix == CharacterNoun.Affix.Addendum)
						order = 2;
					else
						order = 0;

					lsNouns.Add(new AdjectiveNoun() {
						value = randomizer.Item(words),
						order = order,
						priority = noun.priority,
					});
				}
			}

			// Select 
			var adjByOrder = lsAdjectives
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
			for (; ; )
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
					values = g.Select(gg => gg.value)
						.DistinctBy(s => s.ToLowerInvariant()),
				})
				.OrderBy(x => x.order)
				.SelectMany(x => x.values);

			string sAdjectivesAll = string.Join(Text.Delimiter, adjectives);
			string sAdjectivesOpinion = string.Join(Text.Delimiter, selectedAdjectives.Where(a => a.order == 0).Select(a => a.value).DistinctBy(a => a.ToLowerInvariant()));
			string sAdjectivesSize = string.Join(Text.Delimiter, selectedAdjectives.Where(a => a.order == 1).Select(a => a.value).DistinctBy(a => a.ToLowerInvariant()));
			string sAdjectivesQuality = string.Join(Text.Delimiter, selectedAdjectives.Where(a => a.order == 2).Select(a => a.value).DistinctBy(a => a.ToLowerInvariant()));
			string sAdjectivesAge = string.Join(Text.Delimiter, selectedAdjectives.Where(a => a.order == 3).Select(a => a.value).DistinctBy(a => a.ToLowerInvariant()));
			string sAdjectivesShape = string.Join(Text.Delimiter, selectedAdjectives.Where(a => a.order == 4).Select(a => a.value).DistinctBy(a => a.ToLowerInvariant()));
			string sAdjectivesColor = string.Join(Text.Delimiter, selectedAdjectives.Where(a => a.order == 5).Select(a => a.value).DistinctBy(a => a.ToLowerInvariant()));
			string sAdjectivesPattern = string.Join(Text.Delimiter, selectedAdjectives.Where(a => a.order == 6).Select(a => a.value).DistinctBy(a => a.ToLowerInvariant()));
			string sAdjectivesOrigin = string.Join(Text.Delimiter, selectedAdjectives.Where(a => a.order == 6).Select(a => a.value).DistinctBy(a => a.ToLowerInvariant()));
			string sAdjectivesMaterial = string.Join(Text.Delimiter, selectedAdjectives.Where(a => a.order == 7).Select(a => a.value).DistinctBy(a => a.ToLowerInvariant()));
			string sAdjectivesQualifier = string.Join(Text.Delimiter, selectedAdjectives.Where(a => a.order == 8).Select(a => a.value).DistinctBy(a => a.ToLowerInvariant()));


			if (sAdjectivesAll.Length > 0)
			{
				for (int i = 0; i < recipeContexts.Length; ++i)
				{
					recipeContexts[i].SetValue(Constants.Variables.Adjectives, sAdjectivesAll);
					recipeContexts[i].SetValue(string.Concat(Constants.Variables.Adjectives, ":opinion"), sAdjectivesOpinion);
					recipeContexts[i].SetValue(string.Concat(Constants.Variables.Adjectives, ":size"), sAdjectivesSize);
					recipeContexts[i].SetValue(string.Concat(Constants.Variables.Adjectives, ":quality"), sAdjectivesQuality);
					recipeContexts[i].SetValue(string.Concat(Constants.Variables.Adjectives, ":age"), sAdjectivesAge);
					recipeContexts[i].SetValue(string.Concat(Constants.Variables.Adjectives, ":shape"), sAdjectivesShape);
					recipeContexts[i].SetValue(string.Concat(Constants.Variables.Adjectives, ":color"), sAdjectivesColor);
					recipeContexts[i].SetValue(string.Concat(Constants.Variables.Adjectives, ":pattern"), sAdjectivesPattern);
					recipeContexts[i].SetValue(string.Concat(Constants.Variables.Adjectives, ":origin"), sAdjectivesOrigin);
					recipeContexts[i].SetValue(string.Concat(Constants.Variables.Adjectives, ":material"), sAdjectivesMaterial);
					recipeContexts[i].SetValue(string.Concat(Constants.Variables.Adjectives, ":qualifier"), sAdjectivesQualifier);
				}
			}

			// Choose noun
			var nouns = lsNouns.Where(n => n.order == 0).ToList();
			var prefixes = lsNouns.Where(n => n.order == -1).ToList();
			var suffixes = lsNouns.Where(n => n.order == 1).ToList();
			var addendums = lsNouns.Where(n => n.order == 2).ToList();
			var sNoun = SelectOne(nouns);
			var sPrefix = SelectOne(prefixes);
			var sSuffix = SelectOne(suffixes);
			var sAddendum = SelectOne(addendums);

			if (string.IsNullOrEmpty(sNoun) == false)
			{
				string origNoun = sNoun;

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
						sNoun = string.Concat(sNoun, " ", sSuffix);
				}

				for (int i = 0; i < recipeContexts.Length; ++i)
				{
					var context = recipeContexts[i];
					context.SetValue(Constants.Variables.Noun, sNoun);
					context.SetValue(Constants.Variables.Addendum, sAddendum);
					context.SetValue(Constants.Variables.NoAffix, origNoun);
					context.SetValue(Constants.Variables.Prefix, sPrefix);
					context.SetValue(Constants.Variables.Suffix, sSuffix);
				}
			}

			string SelectOne(IEnumerable<AdjectiveNoun> adjNoun)
			{
				return adjNoun.GroupBy(n => n.priority)
					.Select(g => new {
						priority = g.First().priority,
						value = g.Select(gg => gg.value)
							.DistinctBy(s => s.ToLowerInvariant())
							.ToList(),
					})
					.OrderByDescending(x => x.priority)
					.Take(1)
					.Select(x => x.value.Shuffle(randomizer).FirstOrDefault())
					.FirstOrDefault();
			}

		}

		public static void GetAdjectivesAndNoun(IList<Recipe> recipes, Context targetContext)
		{
			if (recipes == null || recipes.Count() == 0)
				return;

			Context[] recipeContexts = ParameterResolver.GetLocalContexts(recipes.ToArray(), targetContext);
			CompileAdjectivesAndNoun(recipes, recipeContexts, new RandomDefault());

			string adjectives;
			if (recipeContexts[0].TryGetValue(Constants.Variables.Adjectives, out adjectives))
				targetContext.SetValue(Constants.Variables.Adjectives, adjectives);

			string noun;
			if (recipeContexts[0].TryGetValue(Constants.Variables.Noun, out noun))
				targetContext.SetValue(Constants.Variables.Noun, noun);
		}

		private static Recipe GetPerActorRecipe(CharacterData character)
		{
			// Get per-actor templates, nodes
			var templates = new List<Recipe.Template>();
			var blocks = new List<Block>();
			foreach (var recipe in character.recipes.Where(r => r.isEnabled))
			{
				for (int i = recipe.templates.Count - 1; i >= 0; i--)
				{
					if (recipe.templates[i].isPerActor)
						templates.Add(recipe.templates[i]);
				}

				for (int i = recipe.blocks.Count - 1; i >= 0; i--)
				{
					if (recipe.blocks[i].isPerActor)
						blocks.Add(recipe.blocks[i]);
				}
			}
			if (templates.Count > 0 || blocks.Count > 0)
			{
				return new Recipe() {
					id = "__per-actor",
					templates = templates,
					blocks = blocks,
				};
			}
			return null;
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

			// Lore 

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
