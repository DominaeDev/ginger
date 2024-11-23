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
		public CardData Data = new CardData();
		public List<CharacterData> Characters = null;

		public ImageRef PortraitImage { get { return Data.portraitImage; } }

		public void Reset()
		{
			Data = new CardData();
			Characters = new List<CharacterData> { new CharacterData() };
		}

		public void ReadGingerCard(GingerCardV1 card, Image portrait)
		{
			if (card == null)
				return;

			Reset();
			Data = new CardData() {
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
				Data.tags = new HashSet<string>(card.tags);

			if (card.sources != null && card.sources.Length > 0)
				Data.sources = new List<string>(card.sources);

			if (card.customVariables != null && card.customVariables.Count > 0)
				Data.customVariables = new List<CustomVariable>(card.customVariables);

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

			if (Data.portraitImage != null && string.IsNullOrEmpty(card.portraitUID) == false)
				Data.portraitImage.uid = card.portraitUID;
		}

		public void ReadFaradayCard(FaradayCardV4 card, Image portrait)
		{
			if (card == null)
				return;

			Reset();
			Data = new CardData() {
				name = card.data.displayName,
				userGender = null,
				portraitImage = ImageRef.FromImage(portrait),
				creator = card.hubAuthorUsername ?? "",
				comment = card.comment ?? "",
			};

			DateTime creationDate;
			if (DateTime.TryParse(card.data.creationDate, out creationDate) == false)
				creationDate = DateTime.UtcNow;
			Data.creationDate = creationDate;

			var character = new CharacterData() {
				spokenName = card.data.name,
			};
			Characters = new List<CharacterData> { character };

			character.gender = Utility.InferGender(GingerString.FromFaraday(card.data.persona).ToString());
			Data.textStyle = DetectTextStyle(card.data.example, card.data.greeting);

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

		public bool ReadTavernCard(TavernCardV2 card, Image portrait)
		{
			if (card == null)
				return false;

			Reset();
			Data = new CardData() {
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
				Data.tags = new HashSet<string>(card.data.tags);

			character.gender = Utility.InferGender(GingerString.FromTavern(card.data.persona).ToString());
			Data.textStyle = DetectTextStyle(card.data.example, card.data.greeting);

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

		public bool ReadTavernCard(TavernCardV3 card, Image portrait)
		{
			if (card == null)
				return false;

			Reset();
			Data = new CardData() {
				name = card.data.name ?? Constants.DefaultCharacterName,
				userGender = null,
				creator = card.data.creator ?? "",
				versionString = card.data.character_version ?? "",
				portraitImage = ImageRef.FromImage(portrait),
				extensionData = card.data.extensions,
				creationDate = DateTimeExtensions.FromUnixTime(card.data.creationDate ?? 0L),
			};

			if (card.data.source != null && card.data.source.Length > 0)
				Data.sources = new List<string>(card.data.source);

			var character = new CharacterData() {
				spokenName = null,
			};
			Characters = new List<CharacterData> { character };

			if (card.data.tags != null)
				Data.tags = new HashSet<string>(card.data.tags);

			character.gender = Utility.InferGender(GingerString.FromTavern(card.data.persona).ToString());
			Data.textStyle = DetectTextStyle(card.data.example, card.data.greeting);

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

			// Creator notes (multi-language)
			var creator_notes_by_language = card.data.creator_notes_multilingual ?? new Dictionary<string, string>();
			if (string.IsNullOrWhiteSpace(card.data.creator_notes) == false)
			{
				if (!(card.data.creator_notes[0] == '#' && creator_notes_by_language.Count > 0 && card.data.creator_notes.IndexOf(':') == -1)) // Might be repeated multilingual
					creator_notes_by_language.TryAdd("en", card.data.creator_notes);
			}
			
			if (creator_notes_by_language.ContainsKey("en") && creator_notes_by_language.Count == 1)
				Data.comment = (creator_notes_by_language["en"] ?? "").ConvertLinebreaks(Linebreak.Default);
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
				Data.comment = sbComment.ToString();
			}

			return true;
		}

		public bool ReadAgnaisticCard(AgnaisticCard card)
		{
			if (card == null)
				return false;

			Reset();
			Data = new CardData() {
				name = card.name ?? Constants.DefaultCharacterName,
				userGender = null,
				creator = card.creator ?? "",
				comment = (card.description ?? "").ConvertLinebreaks(Linebreak.Default),
				versionString = card.character_version ?? "",
			};

			DateTime creationDate;
			if (DateTime.TryParse(card.creationDate, out creationDate) == false)
				creationDate = DateTime.UtcNow;
			Data.creationDate = creationDate;

			var character = new CharacterData() {
				spokenName = null,
			};
			Characters = new List<CharacterData> { character };

			if (card.tags != null)
				Data.tags = new HashSet<string>(card.tags);

			character.gender = Utility.InferGender(GingerString.FromTavern(card.persona).ToString());
			Data.textStyle = DetectTextStyle(card.example, card.greeting);

			if (string.IsNullOrEmpty(card.avatar) == false)
				Data.portraitImage = ImageRef.FromImage(Utility.ImageFromBase64(card.avatar));

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

		public bool ReadPygmalionCard(PygmalionCard card)
		{
			if (card == null)
				return false;

			Reset();
			Data = new CardData() {
				name = card.name ?? Constants.DefaultCharacterName,
				userGender = null,
				creator = card.metaData != null ? (card.metaData.creator ?? "") : "",
				comment = card.metaData != null ? (card.metaData.comment ?? "").ConvertLinebreaks(Linebreak.Default) : "",
				creationDate = card.metaData != null ? DateTimeExtensions.FromUnixTime(card.metaData.creationDate) : DateTime.UtcNow,
			};

			if (card.metaData != null && card.metaData.source != null)
				Data.sources = new List<string>() { card.metaData.source };

			var character = new CharacterData() {
				spokenName = null,
			};
			Characters = new List<CharacterData> { character };

			character.gender = Utility.InferGender(GingerString.FromTavern(card.persona).ToString());
			Data.textStyle = DetectTextStyle(card.example, card.greeting);

			AddChannel(GingerString.FromTavern(card.persona).ToParameter(), Resources.persona_recipe);
			AddChannel(GingerString.FromTavern(card.scenario).ToParameter(), Resources.scenario_recipe);
			AddChannel(GingerString.FromTavern(card.greeting).ToParameter(), Resources.greeting_recipe);

			if (string.IsNullOrEmpty(card.example) == false)
				AddChannel(GingerString.FromTavernChat(card.example).ToParameter(), Resources.example_recipe);

			return true;
		}

		public bool ReadTextGenWebUICard(TextGenWebUICard card)
		{
			if (card == null)
				return false;

			Reset();
			Data = new CardData() {
				name = card.name,
				userGender = null,
				creationDate = DateTime.UtcNow,
			};

			var character = new CharacterData() {
				spokenName = null,
			};
			Characters = new List<CharacterData> { character };

			character.gender = Utility.InferGender(GingerString.FromTavern(card.context).ToString());
			Data.textStyle = DetectTextStyle(card.example, card.greeting);

			AddChannel(GingerString.FromTavern(card.context).ToParameter(), Resources.persona_recipe);
			AddChannel(GingerString.FromTavern(card.greeting).ToParameter(), Resources.greeting_recipe);

			if (string.IsNullOrEmpty(card.example) == false)
				AddChannel(GingerString.FromTavernChat(card.example).ToParameter(), Resources.example_recipe);

			return true;
		}

		private Recipe AddChannel(string text, string xmlSource)
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
