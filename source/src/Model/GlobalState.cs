using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Ginger.Properties;
using Ginger.Integration;

using Backyard = Ginger.Integration.Backyard;

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
			set { _bDirty = value; if (value) IsFileDirty = true; }
		}
		private static bool _bDirty = false;

		public static bool IsFileDirty 
		{ 
			get { return _bFileDirty; }
			set { 
				_bFileDirty = value; 
				if (value) IsLinkDirty = true; }
		}
		private static bool _bFileDirty = false;

		public static Backyard.Link Link = null;
		public static bool HasLink { get { return Link != null; } }
		public static bool HasActiveLink { get { return Link != null && Link.isActive; } }
		public static bool HasStaleLink { get { return Link != null && !Link.isActive; } }

		public static bool IsLinkDirty
		{
			get { return Link != null && Link.isActive && Link.isDirty; }
			set 
			{ 
				if (Link != null) 
					Link.isDirty = value;
			}
		}

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
			Link = null;
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

			if (Card.portraitImage != null && string.IsNullOrEmpty(card.portraitUID) == false)
				Card.portraitImage.uid = card.portraitUID;

			Link = card.backyardLinkInfo;
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
				creator = card.hubAuthorUsername,
				comment = card.comment,
			};

			DateTime creationDate;
			if (DateTime.TryParse(card.data.creationDate, out creationDate) == false)
				creationDate = DateTime.UtcNow;
			Card.creationDate = creationDate;

			Character = new CharacterData() {
				spokenName = card.data.name,
			};

			Character.gender = Utility.InferGender(GingerString.FromFaraday(card.data.persona).ToString());
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

			Character.gender = Utility.InferGender(GingerString.FromTavern(card.data.persona).ToString());
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

			Character.gender = Utility.InferGender(GingerString.FromTavern(card.data.persona).ToString());
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

			// Creator notes (multi-language)
			var creator_notes_by_language = card.data.creator_notes_multilingual ?? new Dictionary<string, string>();
			if (string.IsNullOrWhiteSpace(card.data.creator_notes) == false)
			{
				if (!(card.data.creator_notes[0] == '#' && creator_notes_by_language.Count > 0 && card.data.creator_notes.IndexOf(':') == -1)) // Might be repeated multilingual
					creator_notes_by_language.TryAdd("en", card.data.creator_notes);
			}
			
			if (creator_notes_by_language.ContainsKey("en") && creator_notes_by_language.Count == 1)
				Card.comment = creator_notes_by_language["en"].ConvertLinebreaks(Linebreak.Default);
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

			Character.gender = Utility.InferGender(GingerString.FromTavern(card.persona).ToString());
			Card.textStyle = DetectTextStyle(card.example, card.greeting);

			if (string.IsNullOrEmpty(card.avatar) == false)
				Card.portraitImage = ImageRef.FromImage(Utility.ImageFromBase64(card.avatar));

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

			Character.gender = Utility.InferGender(GingerString.FromTavern(card.persona).ToString());
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

			Character.gender = Utility.InferGender(GingerString.FromTavern(card.context).ToString());
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

		public static void LinkWith(CharacterInstance characterInstance, Backyard.Link.Image[] imageLinks)
		{
			Link = new Backyard.Link() {
				characterId = characterInstance.instanceId,
				updateDate = characterInstance.updateDate,
				imageLinks = imageLinks,
				filename = Current.Filename,
				isActive = true,
			};
			IsFileDirty = true;
		}

		public static void Unlink()
		{
			if (Link != null)
			{
				Link.isActive = false;
				IsFileDirty = true;
			}
		}

		public static void ImportImages(string[] images, out Backyard.Link.Image[] imageLinks) // Backyard import
		{
			if (images == null || images.Length == 0)
			{
				imageLinks = null;
				return;
			}

			var lsImageLinks = new List<Backyard.Link.Image>();

			Image image;
			if (Utility.LoadImageFromFile(images[0], out image))
			{
				Card.portraitImage = ImageRef.FromImage(image);
				lsImageLinks.Add(new Backyard.Link.Image() {
					filename = Path.GetFileName(images[0]),
					uid = Card.portraitImage.uid,
				});
			}

			for (int i = 1; i < images.Length; ++i)
			{
				string name = Path.GetFileNameWithoutExtension(images[i]);
				string ext = Path.GetExtension(images[i]);
				if (ext.BeginsWith("."))
					ext = ext.Substring(1);

				var bytes = Utility.LoadFile(images[i]);
				if (bytes != null)
				{
					var asset = new AssetFile() {
						name = name,
						assetType = AssetFile.AssetType.Icon,
						data = AssetData.FromBytes(bytes),
						ext = ext,
						uriType = AssetFile.UriType.Embedded,
					};
					Card.assets.Add(asset);
					lsImageLinks.Add(new Backyard.Link.Image() {
						filename = Path.GetFileName(images[i]),
						uid = asset.uid,
					});
				}
			}

			imageLinks = lsImageLinks.ToArray();
		}
	}
}
