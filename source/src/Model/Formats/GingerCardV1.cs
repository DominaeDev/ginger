using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Ginger
{
	public class GingerCardV1 : IXmlLoadable, IXmlSaveable
	{
		public static int Version = 1;

		public string id = null;	// Unique card id
		public string name = "";
		public string userGender = "";
		public string creator = "";
		public string comment = "";
		public string versionString = "";
		public string[] tags = null;
		public int detailLevel = 0;
		public int textStyle = 0;
		public int flags = (int)CardData.Flag.Default;
		public int[] tokens = new int[3] { 0, 0, 0 };
		public DateTime creationDate = DateTime.UtcNow;
		public int missingRecipes = 0;
		public string[] sources = null;

		public class Character
		{
			public string spokenName;
			public string gender = "";
			public List<Recipe> recipes = new List<Recipe>();
		}
		public List<Character> characters = new List<Character>();

		public bool LoadFromXml(XmlNode xmlNode)
		{
			id = xmlNode.GetValueElement("ID", null);
			name = xmlNode.GetValueElement("Name");
			userGender = xmlNode.GetValueElement("UserGender");
			creator = xmlNode.GetValueElement("Creator");
			comment = xmlNode.GetValueElement("Comment");
			tags = Utility.ListFromCommaSeparatedString(xmlNode.GetValueElement("Tags")).ToArray();
			versionString = xmlNode.GetValueElement("Version");
			detailLevel = xmlNode.GetValueElementInt("Detail");
			textStyle = xmlNode.GetValueElementInt("TextStyle");
			flags = xmlNode.GetValueElementInt("Flags");
			sources = Utility.ListFromCommaSeparatedString(xmlNode.GetValueElement("Sources")).ToArray();

			tokens[0] = 0;
			tokens[1] = 0;
			tokens[2] = 0;
			missingRecipes = 0;

			string[] sTokens = xmlNode.GetValueElement("TokenCount").Split(new char[] { ',' }, StringSplitOptions.None);
			if (sTokens.Length > 0)
				int.TryParse(sTokens[0], out tokens[0]);
			if (sTokens.Length > 1)
				int.TryParse(sTokens[1], out tokens[1]);
			if (sTokens.Length > 2)
				int.TryParse(sTokens[2], out tokens[2]);

			string sCreationDate = xmlNode.GetValueElement("Created");
			if (DateTime.TryParse(sCreationDate, out creationDate) == false)
				creationDate = DateTime.UtcNow;

			// Read recipes
			Dictionary<StringHandle, Recipe> recipesByID = new Dictionary<StringHandle, Recipe>();
			var recipesNode = xmlNode.GetFirstElement("Recipes");
			if (recipesNode != null)
			{
				var recipeNode = recipesNode.GetFirstElement("Recipe");
				while (recipeNode != null)
				{
					var recipe = new Recipe();

					if (recipe.LoadFromXml(recipeNode))
						recipesByID.Add(recipe.id, recipe);

					recipeNode = recipeNode.GetNextSibling();
				}
			}

			// Read characters
			var characterNode = xmlNode.GetFirstElement("Character");
			while (characterNode != null)
			{
				var character = new Character();
				character.spokenName = characterNode.GetValueElement("SpokenName", null);
				character.gender = characterNode.GetValueElement("Gender");
				characters.Add(character);

				string characterNamePlaceholder;
				if (string.IsNullOrWhiteSpace(character.spokenName) == false)
					characterNamePlaceholder = character.spokenName;
				else if (string.IsNullOrWhiteSpace(name) == false)
					characterNamePlaceholder = name;
				else
					characterNamePlaceholder = Constants.DefaultCharacterName;

				var characterRecipesNode = characterNode.GetFirstElement("Recipes");
				if (characterRecipesNode != null)
				{
					var instanceNode = characterRecipesNode.GetFirstElement("Instance");
					while (instanceNode != null)
					{
						StringHandle recipeID = instanceNode.GetAttribute("id", null);
						bool isActive = instanceNode.GetAttributeBool("active", true);
						bool isCollapsed = instanceNode.GetAttributeBool("collapse", false);
						bool enableTextFormatting = instanceNode.GetAttributeBool("format", true);

						Recipe recipe;
						Recipe recipeTemplate;
						if (recipesByID.TryGetValue(recipeID, out recipeTemplate) == false)
						{
							// Recipe was not stored in card (probably an internal recipe)
							var template = RecipeBook.GetRecipeByID(recipeID);
							if (template != null)
							{
								recipe = template.Instantiate();
								if (recipe.canToggleTextFormatting && !enableTextFormatting)
									recipe.EnableTextFormatting(false);
							}
							else
							{
								instanceNode = instanceNode.GetNextSibling();
								missingRecipes++; // Loss of information
								continue;
							}
						}
						else
						{
							// Use a local recipe if one exists
							RecipeTemplate equivalentTemplate = null;
							if (recipeTemplate.isInternal)
								equivalentTemplate = RecipeBook.GetRecipeByID(recipeTemplate.id); // Get internal recipe
							if (equivalentTemplate == null)
								equivalentTemplate = RecipeBook.GetEquivalentRecipe(recipeTemplate);

							if (equivalentTemplate != null)
								recipe = equivalentTemplate.Instantiate();
							else
							{
								recipe = (Recipe)recipeTemplate.Clone();
								recipe.filename = Constants.Flag.External;
							}
						}
						recipe.isEnabled = isActive;
						recipe.isCollapsed = isCollapsed;

						// Read parameters
						var parameterNode = instanceNode.GetFirstElement("Parameter");
						int parameterIndex = 0;
						while (parameterNode != null)
						{
							StringHandle parameterID = parameterNode.GetAttribute("id", null);
							IParameter parameter;
							if (parameterIndex < recipe.parameters.Count && recipe.parameters[parameterIndex].id == parameterID)
								parameter = recipe.parameters[parameterIndex];
							else
								parameter = recipe.parameters.Find(p => p.id == parameterID);
							if (parameter != null)
								parameter.LoadValueFromXml(parameterNode, Utility.FirstNonEmpty(characters[0].spokenName, name, Constants.DefaultCharacterName), Current.Card.userPlaceholder);

							++parameterIndex;
							parameterNode = parameterNode.GetNextSibling();
						}

						character.recipes.Add(recipe);

						instanceNode = instanceNode.GetNextSibling();
					}
				}

				characterNode = characterNode.GetNextSibling();
			}

			return true;
		}

		public void SaveToXml(XmlNode xmlNode)
		{
			// Card info
			if (string.IsNullOrEmpty(id) == false)
				xmlNode.AddValueElement("ID", id);
			xmlNode.AddValueElement("Name", name);
			if (string.IsNullOrWhiteSpace(userGender) == false)
				xmlNode.AddValueElement("UserGender", userGender);
			if (string.IsNullOrWhiteSpace(creator) == false)
				xmlNode.AddValueElement("Creator", creator);
			if (string.IsNullOrWhiteSpace(comment) == false)
				xmlNode.AddValueElement("Comment", comment);
			if (tags != null && tags.Length > 0)
				xmlNode.AddValueElement("Tags", Utility.ListToCommaSeparatedString(tags.Where(t => !string.IsNullOrWhiteSpace(t))));
			if (string.IsNullOrWhiteSpace(versionString) == false)
				xmlNode.AddValueElement("Version", versionString);
			xmlNode.AddValueElement("Detail", detailLevel);
			xmlNode.AddValueElement("TextStyle", textStyle);
			xmlNode.AddValueElement("Flags", flags);
			xmlNode.AddValueElement("TokenCount", string.Format("{0}, {1}, {2}", tokens[0], tokens[1], tokens[2]));
			xmlNode.AddValueElement("Created", creationDate.ToString("yyyy-MM-ddTHH:mm:ss.fffK"));
			if (sources != null && sources.Length > 0)
				xmlNode.AddValueElement("Sources", Utility.ListToCommaSeparatedString(sources.Where(s => !string.IsNullOrWhiteSpace(s))));

			// Recipes
			var allRecipes = characters.SelectMany(c => c.recipes);
			if (allRecipes.IsEmpty() == false)
			{
				// Store recipe templates
				var recipesNode = xmlNode.AddElement("Recipes");
				foreach (var recipe in allRecipes.Where(r => r.isInternal == false).DistinctBy(r => r.id))
				{
					var recipeNode = recipesNode.AddElement("Recipe");
					recipeNode.AddAttribute("id", recipe.id.ToString());
					recipe.SaveToXml(recipeNode);
				}
			}

			// Characters
			foreach (var character in characters)
			{
				var characterNode = xmlNode.AddElement("Character");

				// Name
				if (string.IsNullOrWhiteSpace(character.spokenName) == false)
					characterNode.AddValueElement("SpokenName", character.spokenName);

				if (string.IsNullOrWhiteSpace(character.gender) == false)
					characterNode.AddValueElement("Gender", character.gender);

				// Recipe instances
				if (character.recipes.Count > 0)
				{
					var characterRecipesNode = characterNode.AddElement("Recipes");

					// Assign unique id to each instance
					for (int i = 0; i < character.recipes.Count; ++i)
					{
						var recipe = character.recipes[i];

						var instanceNode = characterRecipesNode.AddElement("Instance");
						instanceNode.AddAttribute("id", recipe.id.ToString());
						instanceNode.AddAttribute("active", recipe.isEnabled);
						instanceNode.AddAttribute("collapse", recipe.isCollapsed);
						if (recipe.canToggleTextFormatting)
							instanceNode.AddAttribute("format", recipe.enableTextFormatting);

						// Parameters
						foreach (var parameter in recipe.parameters)
							parameter.SaveValueToXml(instanceNode);
					}
				}
			}
		}

		public string ToXml()
		{
			XmlDocument xmlDoc = new XmlDocument();
			XmlNode rootNode = xmlDoc.CreateElement("Ginger");
			rootNode.AddAttribute("version", Version);
			xmlDoc.AppendChild(rootNode);

			XmlDeclaration xmlDecl = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", "no");
			xmlDecl.Encoding = "UTF-8";
			xmlDoc.InsertBefore(xmlDecl, rootNode);

			SaveToXml(rootNode);

			// Save to file
			try
			{
				XmlWriterSettings settings = new XmlWriterSettings();
				settings.Indent = false;

				StringBuilder sbXml = new StringBuilder();
				using (var stringWriter = new StringWriterUTF8(sbXml))
				{
					using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
					{
						xmlDoc.Save(xmlWriter);
					}
				}

				string xml = sbXml.ToString();
				return xml;
			}
			catch
			{
				return null;
			}
		}

		public static GingerCardV1 FromXml(string xmlSource)
		{
			XmlDocument xmlDoc = new XmlDocument();
			try
			{
				byte[] payload = Encoding.UTF8.GetBytes(xmlSource);
				using (var stream = new MemoryStream(payload))
				{
					xmlDoc.Load(stream);
				}
			}
			catch
			{
				return null;
			}

			if (xmlDoc.DocumentElement.Name != "Ginger")
				return null; // Unexpected root element
			int version = xmlDoc.DocumentElement.GetAttributeInt("version", 1);
			if (version > Version)
				return null; // Unsupported version

			GingerCardV1 card = new GingerCardV1();
			if (card.LoadFromXml(xmlDoc.DocumentElement))
				return card;
			return null;
		}

		public static GingerCardV1 Create()
		{
			GingerCardV1 card = new GingerCardV1() {
				id = Current.Card.uuid,
				name = Current.Card.name,
				creator = Current.Card.creator,
				comment = Current.Card.comment,
				versionString = Current.Card.versionString,
				userGender = Current.Card.userGender,
				detailLevel = EnumHelper.ToInt(Current.Card.detailLevel),
				textStyle = EnumHelper.ToInt(Current.Card.textStyle),
				flags = EnumHelper.ToInt(Current.Card.extraFlags),
				tags = Current.Card.tags.ToArray(),
				creationDate = Current.Card.creationDate ?? DateTime.UtcNow,
				tokens = Current.Card.lastTokenCounts,
				sources = Current.Card.sources != null ? Current.Card.sources.ToArray() : null,
			};

			card.characters = Current.Characters.Select(c => new Character() {
				spokenName = c._spokenName,
				gender = c.gender,
				recipes = new List<Recipe>(c.recipes),
			}).ToList();

			return card;
		}
	}
}
