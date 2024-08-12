using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Ginger.Properties;

namespace Ginger
{
	public static class Current
	{
		public static CardData Card = new CardData();
		public static List<CharacterData> Characters = null;
		public static CharacterData MainCharacter { get { return Characters[0]; } }

		public static CharacterData Character 
		{ 
			get { return Characters[SelectedCharacter]; } 
			private set
			{
				Characters = new List<CharacterData>() {
					value ?? new CharacterData(),
				};
				SelectedCharacter = 0;
			}
		}

		public static bool IsLoading = false;

		public static string CardName { get { return Utility.FirstNonEmpty(Card.name, MainCharacter.spokenName, Constants.DefaultCharacterName); } }
		public static string Name { get { return Utility.FirstNonEmpty(Character.spokenName, Card.name, Constants.DefaultCharacterName); } }

		public static int SelectedCharacter { get; set; }

		public static StringBank Strings = new StringBank();
		public static string Filename;

		public static bool IsDirty
		{
			get { return _bDirty; }
			set { _bDirty = value; IsFileDirty |= value; }
		}
		public static bool IsFileDirty { get; set; }
		private static bool _bDirty = false;

		public static IEnumerable<Recipe> AllRecipes { get { return Characters.SelectMany(c => c.recipes); } }
		public static IRuleSupplier[] RuleSuppliers { get { return new IRuleSupplier[] { Strings }; } }

		public static bool IsNSFW
		{
			get
			{
				return AllRecipes.ContainsAny(r => r.isEnabled && r.isNSFW)
					|| Card.tags.ContainsAny(t => string.Compare(t, "nsfw", true) == 0);
			}
		}
		
		public static bool ContainsV3Data
		{
			get
			{
				return (Card.assets != null && Card.assets.ContainsAny(a => a.isDefaultAsset == false))
					|| AllRecipes.ContainsAny(r => r.flags.Contains("__ccv3"));
			}
		}

		public static EventHandler OnLoadCharacter;

		public static int seed
		{
			get
			{
				return string.Concat(MainCharacter.namePlaceholder, MainCharacter.gender ?? "Gender").GetHashCode();
			}
		}

		public static void LoadMacros()
		{
			Strings.Clear();

			// Load internal macros from resources
			byte[] recipeXml = Encoding.UTF8.GetBytes(Resources.internal_macros);
			var xmlDoc = Utility.LoadXmlDocumentFromMemory(recipeXml);
			Strings.LoadFromXml(xmlDoc.DocumentElement);

			// Load external macros
			xmlDoc = Utility.LoadXmlDocument(Utility.ContentPath("Internal", Constants.InternalFiles.GlobalMacros));
			if (xmlDoc != null)
				Strings.LoadFromXml(xmlDoc.DocumentElement);
		}

		public static void Reset()
		{
			Card = new CardData();
			Character = new CharacterData();
			SelectedCharacter = 0;
			Filename = null;
			IsDirty = false;
			IsFileDirty = false;
		}

		private struct RecipeHandle
		{
			public int uid;
			public StringHandle id;
			public VersionNumber version;
		}

		public static void ReloadRecipes(bool bForceRefresh = false)
		{
			foreach (var character in Characters)
			{
				List<RecipeHandle> prevRecipeIds = new List<RecipeHandle>();
				Dictionary<StringHandle, Recipe> sourceData = new Dictionary<StringHandle, Recipe>();
				Dictionary<StringHandle, int> counters = new Dictionary<StringHandle, int>();
				for (int i = 0; i < character.recipes.Count; ++i)
				{
					var sourceRecipe = character.recipes[i];
					prevRecipeIds.Add(new RecipeHandle() {
						uid = sourceRecipe.uid,
						id = sourceRecipe.id,
						version = sourceRecipe.version,
					});
					int index;
					if (counters.ContainsKey(sourceRecipe.id))
						index = ++counters[sourceRecipe.id];
					else
					{
						counters.Add(sourceRecipe.id, 0);
						index = 0;
					}

					StringHandle instanceID = sourceRecipe.id + string.Format("_{0:00}", index);
					sourceData.Add(instanceID, sourceRecipe);
				}

				counters.Clear();
				character.recipes.Clear();

				for (int i = 0; i < prevRecipeIds.Count; ++i)
				{
					var recipeID = prevRecipeIds[i].id;
					var recipeUID = prevRecipeIds[i].uid;
					var version = prevRecipeIds[i].version;

					int index;
					if (counters.ContainsKey(recipeID))
						index = ++counters[recipeID];
					else
					{
						counters.Add(recipeID, 0);
						index = 0;
					}

					StringHandle instanceID = recipeID + string.Format("_{0:00}", index);
					Recipe sourceRecipe;
					sourceData.TryGetValue(instanceID, out sourceRecipe);

					RecipeTemplate recipeTemplate;
					if (bForceRefresh)
					{
						recipeTemplate = RecipeBook.GetRecipeByID(recipeID);
					}
					else
					{
						recipeTemplate = RecipeBook.GetRecipeByUID(recipeUID);
						if (recipeTemplate == null)
						{
							var equivalent = RecipeBook.GetRecipeByID(recipeID);
							if (equivalent != null && equivalent.version >= version)
								recipeTemplate = equivalent;
						}
					}

					if (recipeTemplate == null)
					{
						// Keep current recipe
						character.AddRecipe(sourceRecipe);
						continue;
					}

					// Instantiate and copy parameters
					var addedRecipe = character.AddRecipe(recipeTemplate);
					if (sourceRecipe != null)
					{
						if (sourceRecipe.enableTextFormatting == false)
							addedRecipe.EnableTextFormatting(false);
						Recipe.CopyParameterValues(sourceRecipe, addedRecipe);
					}
				}
			}
		}

		public static Recipe AddLorebook(Lorebook book)
		{
			if (book == null || book.entries.Count == 0)
				return null;

			book = book.Clone();

			foreach (var entry in book.entries)
				entry.value = GingerString.FromString(entry.value).ToParameter();

			var recipe = RecipeBook.AddRecipeFromResource(Resources.lorebook_recipe);
			(recipe.parameters[0] as LorebookParameter).value = book;
			return recipe;
		}

		public static void NewCharacter()
		{
			Reset();
			Character = new CharacterData() {
				spokenName = null,
			};

			OnLoadCharacter?.Invoke(null, EventArgs.Empty);
			IsDirty = false;
			IsFileDirty = false;
		}

		public static bool LoadPortraitImage(string filename, out Image image)
		{
			if (string.IsNullOrEmpty(filename))
			{
				image = default(Image);
				return false;
			}

			// Load image first
			try
			{
				byte[] bytes = File.ReadAllBytes(filename);
				using (var stream = new MemoryStream(bytes))
				{
					image = Image.FromStream(stream);
				}
			}
			catch
			{
				image = default(Image);
				return false;
			}

			return true;
		}

		public static void ReadGingerCard(GingerCardV1 card, Image portrait)
		{
			if (card == null)
				return;

			Reset();
			Card = new CardData() {
				uuid = card.id,
				name = card.name,
				creator = card.creator,
				comment = card.comment,
				userGender = card.userGender,
				versionString = card.versionString,
				portraitImage = ImageRef.FromImage(portrait),
				creationDate = card.creationDate,
				detailLevel = EnumHelper.FromInt(card.detailLevel, CardData.DetailLevel.Normal),
				textStyle = EnumHelper.FromInt(card.textStyle, CardData.TextStyle.None),
				extraFlags = EnumHelper.FromInt(card.flags, CardData.Flag.None),
				lastTokenCounts = card.tokens ?? new int[] { 0, 0, 0 },
			};

			if (card.tags != null)
				Card.tags = new HashSet<string>(card.tags);

			if (card.sources != null && card.sources.Length > 0)
				Card.sources = new List<string>(card.sources);

			if (card.characters.Count > 0)
			{
				Characters = new List<CharacterData>();
				foreach (var character in card.characters)
				{
					var characterData = new CharacterData() {
						spokenName = character.spokenName,
						gender = character.gender,
					};
					if (character.recipes != null)
					{
						foreach (var recipe in character.recipes)
							characterData.AddRecipe(recipe);
					}
					Characters.Add(characterData);
				}
			}
			
		}

		public static void ReadFaradayCard(FaradayCardV4 card, Image portrait)
		{
			if (card == null)
				return;

			Reset();
			Card = new CardData() {
				name = card.data.displayName,
				userGender = null,
				portraitImage = ImageRef.FromImage(portrait),
			};

			DateTime creationDate;
			if (DateTime.TryParse(card.data.creationDate, out creationDate) == false)
				creationDate = DateTime.UtcNow;
			Card.creationDate = creationDate;

			Character = new CharacterData() {
				spokenName = card.data.name,
			};

			InferGender(GingerString.FromFaraday(card.data.persona), out Character.gender);
			Card.textStyle = DetectTextStyle(card.data.example, card.data.greeting);

			AddChannel(GingerString.FromFaraday(card.data.system).ToParameter(), Resources.system_recipe);
			AddChannel(GingerString.FromFaraday(card.data.persona).ToParameter(), Resources.persona_recipe);
			AddChannel(GingerString.FromFaraday(card.data.scenario).ToParameter(), Resources.scenario_recipe);
			AddChannel(GingerString.FromFaraday(card.data.greeting).ToParameter(), Resources.greeting_recipe);
			AddChannel(GingerString.FromFaraday(card.data.example).ToParameter(), Resources.example_recipe);
			AddChannel(card.data.grammar, Resources.grammar_recipe);

			if (card.data.loreItems != null && card.data.loreItems.Length > 0)
			{
				var loreBook = Lorebook.FromFaradayBook(card.data.loreItems);
				var loreBookRecipe = RecipeBook.AddRecipeFromResource(Resources.lorebook_recipe);
				(loreBookRecipe.parameters[0] as LorebookParameter).value = loreBook;
			}
		}

		public static bool ReadTavernCard(TavernCardV2 card, Image portrait)
		{
			if (card == null)
				return false;

			Reset();
			Card = new CardData() {
				name = card.data.name,
				userGender = null,
				creator = card.data.creator,
				comment = card.data.creator_notes.ConvertLinebreaks(Linebreak.Default),
				versionString = card.data.character_version,
				portraitImage = ImageRef.FromImage(portrait),
				extensionData = card.data.extensions,
				creationDate = DateTime.UtcNow,
			};
			Character = new CharacterData() {
				spokenName = null,
			};

			if (card.data.tags != null)
				Card.tags = new HashSet<string>(card.data.tags);

			InferGender(GingerString.FromTavern(card.data.persona), out Character.gender);
			Card.textStyle = DetectTextStyle(card.data.example, card.data.greeting);

			AddChannel(GingerString.FromTavern(card.data.system).ToParameter(), Resources.system_recipe);
			AddChannel(GingerString.FromTavern(card.data.post_history_instructions).ToParameter(), Resources.post_history_recipe);
			AddChannel(GingerString.FromTavern(card.data.persona).ToParameter(), Resources.persona_recipe);
			if (string.IsNullOrEmpty(card.data.personality) == false)
				AddChannel(GingerString.FromTavern(card.data.personality).ToParameter(), Resources.personality_recipe);

			AddChannel(GingerString.FromTavern(card.data.scenario).ToParameter(), Resources.scenario_recipe);
			AddChannel(GingerString.FromTavern(card.data.greeting).ToParameter(), Resources.greeting_recipe);
			if (card.data.alternate_greetings != null)
			{
				for (int i = 0; i < card.data.alternate_greetings.Length; ++i)
					AddChannel(GingerString.FromTavern(card.data.alternate_greetings[i]).ToParameter(), Resources.greeting_recipe);
			}


			if (string.IsNullOrEmpty(card.data.example) == false)
				AddChannel(GingerString.FromTavernChat(card.data.example).ToParameter(), Resources.example_recipe);

			if (card.data.character_book != null && card.data.character_book.entries.Length > 0)
			{
				var loreBook = Lorebook.FromTavernBook(card.data.character_book);
				var loreBookRecipe = RecipeBook.AddRecipeFromResource(Resources.lorebook_recipe);
				(loreBookRecipe.parameters[0] as LorebookParameter).value = loreBook;
			}

			return true;
		}

		public static bool ReadTavernCard(TavernCardV3 card, Image portrait)
		{
			if (card == null)
				return false;

			Reset();
			Card = new CardData() {
				name = card.data.name,
				userGender = null,
				creator = card.data.creator,
				comment = card.data.creator_notes.ConvertLinebreaks(Linebreak.Default),
				versionString = card.data.character_version,
				portraitImage = ImageRef.FromImage(portrait),
				extensionData = card.data.extensions,
				creationDate = DateTimeExtensions.FromUnixTime(card.data.creationDate ?? 0L),
			};

			if (card.data.source != null && card.data.source.Length > 0)
				Card.sources = new List<string>(card.data.source);

			Character = new CharacterData() {
				spokenName = null,
			};

			if (card.data.tags != null)
				Card.tags = new HashSet<string>(card.data.tags);

			InferGender(GingerString.FromTavern(card.data.persona), out Character.gender);
			Card.textStyle = DetectTextStyle(card.data.example, card.data.greeting);

			AddChannel(GingerString.FromTavern(card.data.system).ToParameter(), Resources.system_recipe);
			AddChannel(GingerString.FromTavern(card.data.post_history_instructions).ToParameter(), Resources.post_history_recipe);
			AddChannel(GingerString.FromTavern(card.data.persona).ToParameter(), Resources.persona_recipe);
			if (string.IsNullOrEmpty(card.data.personality) == false)
				AddChannel(GingerString.FromTavern(card.data.personality).ToParameter(), Resources.personality_recipe);

			AddChannel(GingerString.FromTavern(card.data.scenario).ToParameter(), Resources.scenario_recipe);
			AddChannel(GingerString.FromTavern(card.data.greeting).ToParameter(), Resources.greeting_recipe);
			if (card.data.alternate_greetings != null)
			{
				for (int i = 0; i < card.data.alternate_greetings.Length; ++i)
					AddChannel(GingerString.FromTavern(card.data.alternate_greetings[i]).ToParameter(), Resources.greeting_recipe);
			}
			if (card.data.group_greetings != null)
			{
				for (int i = 0; i < card.data.group_greetings.Length; ++i)
					AddChannel(GingerString.FromTavern(card.data.group_greetings[i]).ToParameter(), Resources.group_greeting_recipe);
			}

			if (string.IsNullOrEmpty(card.data.example) == false)
				AddChannel(GingerString.FromTavernChat(card.data.example).ToParameter(), Resources.example_recipe);

			if (card.data.character_book != null && card.data.character_book.entries.Length > 0)
			{
				var loreBook = Lorebook.FromTavernBook(card.data.character_book);
				var loreBookRecipe = RecipeBook.AddRecipeFromResource(Resources.lorebook_recipe);
				(loreBookRecipe.parameters[0] as LorebookParameter).value = loreBook;
			}

			return true;
		}

		public static bool ReadAgnaisticCard(AgnaisticCard card)
		{
			if (card == null)
				return false;

			Reset();
			Card = new CardData() {
				name = card.name,
				userGender = null,
				creator = card.creator,
				comment = card.description.ConvertLinebreaks(Linebreak.Default),
				versionString = card.character_version,
			};

			DateTime creationDate;
			if (DateTime.TryParse(card.creationDate, out creationDate) == false)
				creationDate = DateTime.UtcNow;
			Card.creationDate = creationDate;

			Character = new CharacterData() {
				spokenName = null,
			};

			if (card.tags != null)
				Card.tags = new HashSet<string>(card.tags);

			InferGender(GingerString.FromTavern(card.persona), out Character.gender);
			Card.textStyle = DetectTextStyle(card.example, card.greeting);

			if (string.IsNullOrEmpty(card.avatar) == false)
				Card.portraitImage = ImageRef.FromImage(FileUtil.ImageFromBase64(card.avatar));

			AddChannel(GingerString.FromTavern(card.system_prompt).ToParameter(), Resources.system_recipe);
			AddChannel(GingerString.FromTavern(card.postHistoryInstructions).ToParameter(), Resources.post_history_recipe);
			AddChannel(GingerString.FromTavern(card.persona).ToParameter(), Resources.persona_recipe);
			AddChannel(GingerString.FromTavern(card.scenario).ToParameter(), Resources.scenario_recipe);
			AddChannel(GingerString.FromTavern(card.greeting).ToParameter(), Resources.greeting_recipe);
			if (card.alternateGreetings != null)
			{ 
				for (int i = 0; i < card.alternateGreetings.Length; ++i)
					AddChannel(GingerString.FromTavern(card.alternateGreetings[i]).ToParameter(), Resources.greeting_recipe);
			}

			if (string.IsNullOrEmpty(card.example) == false)
				AddChannel(GingerString.FromTavernChat(card.example).ToParameter(), Resources.example_recipe);

			if (card.character_book != null && card.character_book.entries.Length > 0)
			{
				var loreBook = Lorebook.FromAgnaisticBook(card.character_book);
				var loreBookRecipe = RecipeBook.AddRecipeFromResource(Resources.lorebook_recipe);
				(loreBookRecipe.parameters[0] as LorebookParameter).value = loreBook;
			}

			return true;
		}

		public static bool LoadCharacter(PygmalionCard card)
		{
			if (card == null)
				return false;

			Reset();
			Card = new CardData() {
				name = card.name,
				userGender = null,
				creator = card.metaData != null ? (card.metaData.creator ?? "") : "",
				comment = card.metaData != null ? (card.metaData.comment ?? "").ConvertLinebreaks(Linebreak.Default) : "",
				creationDate = card.metaData != null ? DateTimeExtensions.FromUnixTime(card.metaData.creationDate) : DateTime.UtcNow,
			};

			if (card.metaData != null && card.metaData.source != null)
				Card.sources = new List<string>() { card.metaData.source };

			Character = new CharacterData() {
				spokenName = null,
			};

			InferGender(GingerString.FromTavern(card.persona), out Character.gender);
			Card.textStyle = DetectTextStyle(card.example, card.greeting);

			AddChannel(GingerString.FromTavern(card.persona).ToParameter(), Resources.persona_recipe);
			AddChannel(GingerString.FromTavern(card.scenario).ToParameter(), Resources.scenario_recipe);
			AddChannel(GingerString.FromTavern(card.greeting).ToParameter(), Resources.greeting_recipe);

			if (string.IsNullOrEmpty(card.example) == false)
				AddChannel(GingerString.FromTavernChat(card.example).ToParameter(), Resources.example_recipe);

			return true;
		}

		public static bool LoadCharacter(TextGenWebUICard card)
		{
			if (card == null)
				return false;

			Reset();
			Card = new CardData() {
				name = card.name,
				userGender = null,
				creationDate = DateTime.UtcNow,
			};

			Character = new CharacterData() {
				spokenName = null,
			};

			InferGender(GingerString.FromTavern(card.context), out Character.gender);
			Card.textStyle = DetectTextStyle(card.example, card.greeting);

			AddChannel(GingerString.FromTavern(card.context).ToParameter(), Resources.persona_recipe);
			AddChannel(GingerString.FromTavern(card.greeting).ToParameter(), Resources.greeting_recipe);

			if (string.IsNullOrEmpty(card.example) == false)
				AddChannel(GingerString.FromTavernChat(card.example).ToParameter(), Resources.example_recipe);

			return true;
		}

		private static Recipe AddChannel(string text, string xmlSource)
		{
			if (string.IsNullOrWhiteSpace(text) == false)
			{
				Text.ReplaceDecorativeQuotes(ref text);

				var recipe = RecipeBook.AddRecipeFromResource(xmlSource);
				(recipe.parameters[0] as TextParameter).value = text;
				return recipe;
			}
			return null;
		}

		private static void InferGender(GingerString persona, out string gender)
		{
			string text = persona.ToString();
			if (string.IsNullOrEmpty(text))
			{
				gender = null;
				return;
			}

			// Gender indicators:
			// Primary
			int iFemale = Utility.FindFirstWholeWord(text, new string[] { "female", "woman", "girl" }, true);
			int iMale = Utility.FindFirstWholeWord(text, new string[] { "male", "man", "boy"}, true);
			int iFuta = Utility.FindFirstWholeWord(text, new string[] { "futanari", "futa", "dickgirl", "shemale", "dick-girl", "she-male" }, true);
			int iTrans = Utility.FindFirstWholeWord(text, new string[] { "trans", "transgender", "transsexual" }, true);
			int iNonBinary = Utility.FindFirstWholeWord(text, new string[] { "non-binary", "nonbinary" }, true);
			// Secondary
			int iFeminine = Utility.FindFirstWholeWord(text, new string[] { "she", "her", "herself", "hers" }, true);
			int iMasculine = Utility.FindFirstWholeWord(text, new string[] { "he", "him", "himself", "his" }, true);
			int iNeutral = Utility.FindFirstWholeWord(text, new string[] { "they", "them", "themselves", "their", "theirs" }, true);
			// Tertiary
			int iFemaleRole = Utility.FindFirstWholeWord(text, new string[] { "girlfriend", "wife", "waifu", "mother", "mom", "milf", "daughter", "matron", "matriarch" }, true);
			int iMaleRole = Utility.FindFirstWholeWord(text, new string[] { "boyfriend", "husband", "father", "dad", "son", "patriarch" }, true);

			if (iFuta != -1) // Special case: Futanari mostly use feminine pronouns
			{
				gender = "Futanari";
				return;
			}
			if (iNonBinary != -1) // Special case: Non-binary may use any pronouns.
			{
				gender = "Non-binary";
				return;
			}
			if (iTrans != -1 && (iMale == -1 || iTrans < iMale) && (iFemale == -1 || iTrans < iFemale))
			{
				gender = "Transgender";
				return;
			}
			if (iFemale != -1 && (iMale == -1 || iFemale < iMale))
			{
				gender = "Female";
				return;
			}
			if (iMale != -1 && (iFemale == -1 || iMale < iFemale))
			{
				gender = "Male";
				return;
			}
			if (iFeminine != -1 && (iMasculine == -1 || iFeminine < iMasculine))
			{
				gender = "Female";
				return;
			}
			if (iMasculine != -1 && (iFeminine == -1 || iMasculine < iFeminine))
			{
				gender = "Male";
				return;
			}
			if (iFemaleRole != -1 && (iMaleRole == -1 || iFemaleRole < iMaleRole))
			{
				gender = "Female";
				return;
			}
			if (iMaleRole != -1 && (iFemaleRole == -1 || iMaleRole < iFemaleRole))
			{
				gender = "Male";
				return;
			}
			if (iNeutral != -1 && (iMale == -1 || iNeutral < iMale) && (iFemale == -1 || iNeutral < iFemale))
			{
				gender = null; // Neutral or undetermined
				return;
			}

			gender = null;
		}

		public static void AddCharacter()
		{
			var character = new CharacterData();
			Characters.Add(character);
			SelectedCharacter = Characters.Count - 1;

			IsDirty = true;
		}

		private static CardData.TextStyle DetectTextStyle(string example, string greeting)
		{
			bool hasQuotes;
			bool hasAsterisks;
			bool isDecorative;
			bool isJapanese;

			if (example != null)
				example = example.Remove("<START>", true);

			if (string.IsNullOrWhiteSpace(greeting) == false)
			{
				hasQuotes = greeting.IndexOf('"') != -1;
				isDecorative = greeting.IndexOfAny(new char[] { '\u201C', '\u201D', '\u201E', '\u201F' }) != -1;
				isJapanese = greeting.IndexOfAny(new char[] { '\u300C', '\u300D' }) != -1;
				hasAsterisks = greeting.IndexOf('*') != -1;
			}
			else if (string.IsNullOrWhiteSpace(example) == false)
			{
				hasQuotes = example.IndexOf('"') != -1;
				isDecorative = example.IndexOfAny(new char[] { '\u201C', '\u201D', '\u201E', '\u201F' }) != -1;
				isJapanese = example.IndexOfAny(new char[] { '\u300C', '\u300D' }) != -1;
				hasAsterisks = example.IndexOf('*') != -1;
			}
			else
				return CardData.TextStyle.None;

			if (hasQuotes && hasAsterisks)
				return CardData.TextStyle.Mixed;
			else if (hasQuotes)
				return isDecorative ? CardData.TextStyle.Decorative : CardData.TextStyle.Novel;
			else if (isDecorative)
				return CardData.TextStyle.Decorative;
			else if (isJapanese)
				return CardData.TextStyle.None;
			else if (hasAsterisks)
				return CardData.TextStyle.Chat;
			return CardData.TextStyle.None;
		}

	}
}
