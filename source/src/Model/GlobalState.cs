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
		public static GingerCharacter Instance = new GingerCharacter();

		public static CardData Card { 
			get { return Instance.Card; } 
			set { Instance.Card = value; }
		}
		public static List<CharacterData> Characters { 
			get { return Instance.Characters; } 
			set { Instance.Characters = value; }
		}

		public static CharacterData MainCharacter { get { return Characters[0]; } }

		public static CharacterData Character 
		{ 
			get { return Characters[SelectedCharacter]; } 
			private set
			{
				Instance.Characters = new List<CharacterData>() {
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
			Instance.Reset();
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

			recipe.ConvertCharacterMarkers(Current.Name, Card.userPlaceholder);
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

		public static bool ReadGingerCard(GingerCardV1 card, Image portrait)
		{
			if (card == null)
				return false;

			Reset();
			Instance.ReadGingerCard(card, portrait);
			Link = card.backyardLinkInfo;
			return true;
		}

		public static bool ReadFaradayCard(FaradayCardV4 card, Image portrait)
		{
			if (card == null)
				return false;

			Reset();
			Instance.ReadFaradayCard(card, portrait);
			Instance.ConvertCharacterMarkers(Name, Card.userPlaceholder);
			return true;
		}

		public static bool ReadTavernCard(TavernCardV2 card, Image portrait)
		{
			if (card == null)
				return false;

			Reset();
			Instance.ReadTavernCard(card, portrait);
			Instance.ConvertCharacterMarkers(Name, Card.userPlaceholder);
			return true;
		}

		public static bool ReadTavernCard(TavernCardV3 card, Image portrait)
		{
			if (card == null)
				return false;

			Reset();
			Instance.ReadTavernCard(card, portrait);
			Instance.ConvertCharacterMarkers(Name, Card.userPlaceholder);
			return true;
		}

		public static bool ReadAgnaisticCard(AgnaisticCard card)
		{
			if (card == null)
				return false;

			Reset();
			Instance.ReadAgnaisticCard(card);
			Instance.ConvertCharacterMarkers(Name, Card.userPlaceholder);
			return true;
		}

		public static bool ReadPygmalionCard(PygmalionCard card)
		{
			if (card == null)
				return false;

			Reset();
			Instance.ReadPygmalionCard(card);
			Instance.ConvertCharacterMarkers(Name, Card.userPlaceholder);
			return true;
		}

		public static bool ReadTextGenWebUICard(TextGenWebUICard card)
		{
			if (card == null)
				return false;

			Reset();
			Instance.ReadTextGenWebUICard(card);
			Instance.ConvertCharacterMarkers(Name, Card.userPlaceholder);
			return true;
		}

		public static void AddCharacter()
		{
			var character = new CharacterData();
			Characters.Add(character);
			SelectedCharacter = Characters.Count - 1;

			IsDirty = true;
		}

		public static void LinkWith(CharacterInstance characterInstance, Backyard.Link.Image[] imageLinks)
		{
			Link = new Backyard.Link() {
				characterId = characterInstance.instanceId,
				updateDate = characterInstance.updateDate,
				imageLinks = imageLinks,
				filenameHash = Current.Filename,
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

		public static void ImportImages(string[] images, out Backyard.Link.Image[] imageLinks, AssetFile.AssetType imageType = AssetFile.AssetType.Icon) // Backyard import
		{
			if (images == null || images.Length == 0)
			{
				imageLinks = null;
				return;
			}

			var lsImageLinks = new List<Backyard.Link.Image>();

			int i = 0;

			if (imageType == AssetFile.AssetType.Icon)
			{
				Image portraitImage;
				if (Card.LoadPortraitImageFromFile(images[0], out portraitImage))
				{
					lsImageLinks.Add(new Backyard.Link.Image() {
						filename = Path.GetFileName(images[0]),
						uid = Card.portraitImage.uid,
					});
				}
				++i;
			}

			for (; i < images.Length; ++i)
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
						assetType = imageType,
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

		public struct StashInfo
		{
			public GingerCharacter instance;
			public Backyard.Link link;
			public bool isDirty;
			public bool isFileDirty;
		}

		public static StashInfo Stash()
		{
			var stash = new StashInfo() {
				instance = Instance,
				link = Link,
				isDirty = _bDirty,
				isFileDirty = _bFileDirty,
			};
			Link = null;
			return stash;
		}

		public static void Restore(StashInfo stash)
		{
			Instance = stash.instance;
			Link = stash.link;
			_bDirty = stash.isDirty;
			_bFileDirty = stash.isFileDirty;
		}
	}

}
