using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Ginger.Properties;

namespace Ginger
{
	public class GingerCharacter
	{
		public CardData Card = new CardData();
		public List<CharacterData> Characters = null;

		public void Reset()
		{
			Card = new CardData();
			Characters = new List<CharacterData> { new CharacterData() };
		}

		public void ReadGingerCard(GingerCardV1 card, Image portrait)
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
				useStyleGrammar = card.useStyleGrammar,
				extraFlags = EnumHelper.FromInt(card.flags, CardData.Flag.None),
				lastTokenCounts = card.tokens ?? new int[] { 0, 0, 0 },
			};

			if (card.tags != null)
				Card.tags = new HashSet<string>(card.tags);

			if (card.sources != null && card.sources.Length > 0)
				Card.sources = new List<string>(card.sources);

			if (card.customVariables != null && card.customVariables.Count > 0)
				Card.customVariables = new List<CustomVariable>(card.customVariables);

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

			if (Card.portraitImage != null && string.IsNullOrEmpty(card.portraitUID) == false)
				Card.portraitImage.uid = card.portraitUID;
		}

		public void ReadFaradayCard(FaradayCardV4 card, Image portrait)
		{
			if (card == null)
				return;

			Reset();
			Card = new CardData() {
				name = card.data.displayName,
				userGender = null,
				portraitImage = ImageRef.FromImage(portrait),
				creator = card.hubAuthorUsername ?? "",
				comment = card.comment ?? "",
			};

			DateTime creationDate;
			if (DateTime.TryParse(card.data.creationDate, out creationDate) == false)
				creationDate = DateTime.UtcNow;
			Card.creationDate = creationDate;

			var character = new CharacterData() {
				spokenName = card.data.name,
			};
			Characters = new List<CharacterData> { character };

			character.gender = Utility.InferGender(GingerString.FromFaraday(card.data.persona).ToString());
			Card.textStyle = DetectTextStyle(card.data.example, card.data.greeting);

			AddChannel(character, GingerString.FromFaraday(card.data.system).ToParameter(), Resources.system_recipe);
			AddChannel(character, GingerString.FromFaraday(card.authorNote).ToParameter(), Resources.post_history_recipe);
			AddChannel(character, GingerString.FromFaraday(card.data.persona).ToParameter(), Resources.persona_recipe);
			AddChannel(character, GingerString.FromFaraday(card.data.scenario).ToParameter(), Resources.scenario_recipe);
			AddChannel(character, GingerString.FromFaraday(card.data.greeting).ToParameter(), Resources.greeting_recipe);
			AddChannel(character, GingerString.FromFaraday(card.data.example).ToParameter(), Resources.example_recipe);
			AddChannel(character, card.data.grammar, Resources.grammar_recipe);

			if (card.data.loreItems != null && card.data.loreItems.Length > 0)
			{
				var loreBook = Lorebook.FromFaradayBook(card.data.loreItems);
				var loreBookRecipe = RecipeBook.AddRecipeFromResource(Resources.lorebook_recipe);
				(loreBookRecipe.parameters[0] as LorebookParameter).value = loreBook;
			}
		}

		public bool ReadTavernCard(TavernCardV2 card, Image portrait)
		{
			if (card == null)
				return false;

			Reset();
			Card = new CardData() {
				name = card.data.name ?? Constants.DefaultCharacterName,
				userGender = null,
				creator = card.data.creator ?? "",
				comment = (card.data.creator_notes ?? "").ConvertLinebreaks(Linebreak.Default),
				versionString = card.data.character_version ?? "",
				portraitImage = ImageRef.FromImage(portrait),
				extensionData = card.data.extensions,
				creationDate = DateTime.UtcNow,
			};
			var character = new CharacterData() {
				spokenName = null,
			};
			Characters = new List<CharacterData> { character };

			if (card.data.tags != null)
				Card.tags = new HashSet<string>(card.data.tags);

			character.gender = Utility.InferGender(GingerString.FromTavern(card.data.persona).ToString());
			Card.textStyle = DetectTextStyle(card.data.example, card.data.greeting);

			AddChannel(character, GingerString.FromTavern(card.data.system).ToParameter(), Resources.system_recipe);
			AddChannel(character, GingerString.FromTavern(card.data.post_history_instructions).ToParameter(), Resources.post_history_recipe);
			AddChannel(character, GingerString.FromTavern(card.data.persona).ToParameter(), Resources.persona_recipe);
			if (string.IsNullOrEmpty(card.data.personality) == false)
				AddChannel(character, GingerString.FromTavern(card.data.personality).ToParameter(), Resources.personality_recipe);

			AddChannel(character, GingerString.FromTavern(card.data.scenario).ToParameter(), Resources.scenario_recipe);
			AddChannel(character, GingerString.FromTavern(card.data.greeting).ToParameter(), Resources.greeting_recipe);
			if (card.data.alternate_greetings != null)
			{
				for (int i = 0; i < card.data.alternate_greetings.Length; ++i)
				{
					var altGreeting = AddChannel(character, GingerString.FromTavern(card.data.alternate_greetings[i]).ToParameter(), Resources.greeting_recipe);
					if (altGreeting != null)
						altGreeting.isCollapsed = true; // Collapse alt greetings
				}
			}


			if (string.IsNullOrEmpty(card.data.example) == false)
				AddChannel(character, GingerString.FromTavernChat(card.data.example).ToParameter(), Resources.example_recipe);

			if (card.data.character_book != null && card.data.character_book.entries.Length > 0)
			{
				var loreBook = Lorebook.FromTavernBook(card.data.character_book);
				var loreBookRecipe = RecipeBook.AddRecipeFromResource(Resources.lorebook_recipe);
				(loreBookRecipe.parameters[0] as LorebookParameter).value = loreBook;
			}

			return true;
		}

		public bool ReadTavernCard(TavernCardV3 card, Image portrait)
		{
			if (card == null)
				return false;

			Reset();
			Card = new CardData() {
				name = card.data.name ?? Constants.DefaultCharacterName,
				userGender = null,
				creator = card.data.creator ?? "",
				versionString = card.data.character_version ?? "",
				portraitImage = ImageRef.FromImage(portrait),
				extensionData = card.data.extensions,
				creationDate = DateTimeExtensions.FromUnixTime(card.data.creationDate ?? 0L),
			};

			if (card.data.source != null && card.data.source.Length > 0)
				Card.sources = new List<string>(card.data.source);

			var character = new CharacterData() {
				spokenName = null,
			};
			Characters = new List<CharacterData> { character };

			if (card.data.tags != null)
				Card.tags = new HashSet<string>(card.data.tags);

			character.gender = Utility.InferGender(GingerString.FromTavern(card.data.persona).ToString());
			Card.textStyle = DetectTextStyle(card.data.example, card.data.greeting);

			AddChannel(character, GingerString.FromTavern(card.data.system).ToParameter(), Resources.system_recipe);
			AddChannel(character, GingerString.FromTavern(card.data.post_history_instructions).ToParameter(), Resources.post_history_recipe);
			AddChannel(character, GingerString.FromTavern(card.data.persona).ToParameter(), Resources.persona_recipe);
			if (string.IsNullOrEmpty(card.data.personality) == false)
				AddChannel(character, GingerString.FromTavern(card.data.personality).ToParameter(), Resources.personality_recipe);

			AddChannel(character, GingerString.FromTavern(card.data.scenario).ToParameter(), Resources.scenario_recipe);
			AddChannel(character, GingerString.FromTavern(card.data.greeting).ToParameter(), Resources.greeting_recipe);
			if (card.data.alternate_greetings != null)
			{
				for (int i = 0; i < card.data.alternate_greetings.Length; ++i)
				{
					var altGreeting = AddChannel(character, GingerString.FromTavern(card.data.alternate_greetings[i]).ToParameter(), Resources.greeting_recipe);
					if (altGreeting != null)
						altGreeting.isCollapsed = true; // Collapse alt greetings
				}
			}
			if (card.data.group_greetings != null)
			{
				for (int i = 0; i < card.data.group_greetings.Length; ++i)
					AddChannel(character, GingerString.FromTavern(card.data.group_greetings[i]).ToParameter(), Resources.group_greeting_recipe);
			}

			if (string.IsNullOrEmpty(card.data.example) == false)
				AddChannel(character, GingerString.FromTavernChat(card.data.example).ToParameter(), Resources.example_recipe);

			if (card.data.character_book != null && card.data.character_book.entries.Length > 0)
			{
				var loreBook = Lorebook.FromTavernBook(card.data.character_book);
				var loreBookRecipe = RecipeBook.AddRecipeFromResource(Resources.lorebook_recipe);
				(loreBookRecipe.parameters[0] as LorebookParameter).value = loreBook;
			}

			// Creator notes (multi-language)
			var creator_notes_by_language = card.data.creator_notes_multilingual ?? new Dictionary<string, string>();
			if (string.IsNullOrWhiteSpace(card.data.creator_notes) == false)
			{
				if (!(card.data.creator_notes[0] == '#' && creator_notes_by_language.Count > 0 && card.data.creator_notes.IndexOf(':') == -1)) // Might be repeated multilingual
					creator_notes_by_language.TryAdd("en", card.data.creator_notes);
			}
			
			if (creator_notes_by_language.ContainsKey("en") && creator_notes_by_language.Count == 1)
				Card.comment = (creator_notes_by_language["en"] ?? "").ConvertLinebreaks(Linebreak.Default);
			else
			{
				var sbComment = new StringBuilder();
				foreach (var kvp in creator_notes_by_language
					.Where(kvp => string.IsNullOrWhiteSpace(kvp.Key) == false && string.IsNullOrWhiteSpace(kvp.Value) == false)
					.OrderBy(kvp => kvp.Key))
				{
					sbComment.NewParagraph();
					sbComment.AppendLine(string.Format("#{0}:", kvp.Key.Trim().ToLowerInvariant()));
					sbComment.Append(kvp.Value.Trim());
				}

				sbComment.ConvertLinebreaks(Linebreak.Default);
				Card.comment = sbComment.ToString();
			}

			return true;
		}

		public bool ReadAgnaisticCard(AgnaisticCard card)
		{
			if (card == null)
				return false;

			Reset();
			Card = new CardData() {
				name = card.name ?? Constants.DefaultCharacterName,
				userGender = null,
				creator = card.creator ?? "",
				comment = (card.description ?? "").ConvertLinebreaks(Linebreak.Default),
				versionString = card.character_version ?? "",
			};

			DateTime creationDate;
			if (DateTime.TryParse(card.creationDate, out creationDate) == false)
				creationDate = DateTime.UtcNow;
			Card.creationDate = creationDate;

			var character = new CharacterData() {
				spokenName = null,
			};
			Characters = new List<CharacterData> { character };

			if (card.tags != null)
				Card.tags = new HashSet<string>(card.tags);

			character.gender = Utility.InferGender(GingerString.FromTavern(card.persona).ToString());
			Card.textStyle = DetectTextStyle(card.example, card.greeting);

			if (string.IsNullOrEmpty(card.avatar) == false)
				Card.portraitImage = ImageRef.FromImage(Utility.ImageFromBase64(card.avatar));

			AddChannel(character, GingerString.FromTavern(card.system_prompt).ToParameter(), Resources.system_recipe);
			AddChannel(character, GingerString.FromTavern(card.postHistoryInstructions).ToParameter(), Resources.post_history_recipe);
			AddChannel(character, GingerString.FromTavern(card.persona).ToParameter(), Resources.persona_recipe);
			AddChannel(character, GingerString.FromTavern(card.scenario).ToParameter(), Resources.scenario_recipe);
			AddChannel(character, GingerString.FromTavern(card.greeting).ToParameter(), Resources.greeting_recipe);
			if (card.alternateGreetings != null)
			{
				for (int i = 0; i < card.alternateGreetings.Length; ++i)
				{
					var altGreeting = AddChannel(character, GingerString.FromTavern(card.alternateGreetings[i]).ToParameter(), Resources.greeting_recipe);
					if (altGreeting != null)
						altGreeting.isCollapsed = true; // Collapse alt greetings
				}
			}

			if (string.IsNullOrEmpty(card.example) == false)
				AddChannel(character, GingerString.FromTavernChat(card.example).ToParameter(), Resources.example_recipe);

			if (card.character_book != null && card.character_book.entries.Length > 0)
			{
				var loreBook = Lorebook.FromAgnaisticBook(card.character_book);
				var loreBookRecipe = RecipeBook.AddRecipeFromResource(Resources.lorebook_recipe);
				(loreBookRecipe.parameters[0] as LorebookParameter).value = loreBook;
			}

			return true;
		}

		public bool ReadPygmalionCard(PygmalionCard card)
		{
			if (card == null)
				return false;

			Reset();
			Card = new CardData() {
				name = card.name ?? Constants.DefaultCharacterName,
				userGender = null,
				creator = card.metaData != null ? (card.metaData.creator ?? "") : "",
				comment = card.metaData != null ? (card.metaData.comment ?? "").ConvertLinebreaks(Linebreak.Default) : "",
				creationDate = card.metaData != null ? DateTimeExtensions.FromUnixTime(card.metaData.creationDate) : DateTime.UtcNow,
			};

			if (card.metaData != null && card.metaData.source != null)
				Card.sources = new List<string>() { card.metaData.source };

			var character = new CharacterData() {
				spokenName = null,
			};
			Characters = new List<CharacterData> { character };

			character.gender = Utility.InferGender(GingerString.FromTavern(card.persona).ToString());
			Card.textStyle = DetectTextStyle(card.example, card.greeting);

			AddChannel(character, GingerString.FromTavern(card.persona).ToParameter(), Resources.persona_recipe);
			AddChannel(character, GingerString.FromTavern(card.scenario).ToParameter(), Resources.scenario_recipe);
			AddChannel(character, GingerString.FromTavern(card.greeting).ToParameter(), Resources.greeting_recipe);

			if (string.IsNullOrEmpty(card.example) == false)
				AddChannel(character, GingerString.FromTavernChat(card.example).ToParameter(), Resources.example_recipe);

			return true;
		}

		public bool ReadTextGenWebUICard(TextGenWebUICard card)
		{
			if (card == null)
				return false;

			Reset();
			Card = new CardData() {
				name = card.name,
				userGender = null,
				creationDate = DateTime.UtcNow,
			};

			var character = new CharacterData() {
				spokenName = null,
			};
			Characters = new List<CharacterData> { character };

			character.gender = Utility.InferGender(GingerString.FromTavern(card.context).ToString());
			Card.textStyle = DetectTextStyle(card.example, card.greeting);

			AddChannel(character, GingerString.FromTavern(card.context).ToParameter(), Resources.persona_recipe);
			AddChannel(character, GingerString.FromTavern(card.greeting).ToParameter(), Resources.greeting_recipe);

			if (string.IsNullOrEmpty(card.example) == false)
				AddChannel(character, GingerString.FromTavernChat(card.example).ToParameter(), Resources.example_recipe);

			return true;
		}

		public bool ReadUserData(UserData userInfo)
		{
			if (userInfo == null)
				return false;

			Card.volatileUserPlaceholder = userInfo.name;
			if (string.IsNullOrEmpty(userInfo.persona) == false)
			{
				GingerString userPersona = GingerString.FromFaraday(userInfo.persona);
				AddChannel(Characters[0], userPersona.ToParameter(), Resources.user_recipe);
				Card.userGender = Utility.InferGender(userPersona.ToString(), true);
			}
			return true;
		}

		private Recipe AddChannel(CharacterData character, string text, string xmlSource)
		{
			if (string.IsNullOrWhiteSpace(text) == false)
			{
				Text.ReplaceDecorativeQuotes(ref text);

				var recipe = CreateComponent(xmlSource);
				(recipe.parameters[0] as TextParameter).value = text;
				character.AddRecipe(recipe);
				return recipe;
			}
			return null;
		}

		private static Recipe CreateComponent(string xmlSource)
		{
			byte[] recipeXml = Encoding.UTF8.GetBytes(xmlSource);
			var xmlDoc = Utility.LoadXmlDocumentFromMemory(recipeXml);
			Recipe recipe = new Recipe();
			if (recipe.LoadFromXml(xmlDoc.DocumentElement))
			{
				recipe.type = Recipe.Type.Component;
				recipe.ResetParameters();
				return recipe;
			}
			return default(Recipe);
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

		public void ConvertCharacterMarkers(string characterName, string userName)
		{
			if (Characters == null)
				return;

			foreach (var recipe in Characters.SelectMany(c => c.recipes))
				recipe.ConvertCharacterMarkers(characterName, userName);
		}
	}
}
