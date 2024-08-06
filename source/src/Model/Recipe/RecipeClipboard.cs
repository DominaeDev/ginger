using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Ginger
{
	[Serializable]
	public class RecipeClipboard
	{
		public static readonly string Format = "Ginger.RecipeClipboard";

		public int version;
		public string data;

		public static RecipeClipboard FromRecipes(IEnumerable<Recipe> recipes)
		{
			XmlDocument xmlDoc = new XmlDocument();
			XmlNode xmlNode = xmlDoc.CreateElement("Ginger");
			xmlNode.AddAttribute("version", GingerCardV1.Version);
			xmlDoc.AppendChild(xmlNode);
			
			XmlDeclaration xmlDecl = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", "no");
			xmlDecl.Encoding = "UTF-8";
			xmlDoc.InsertBefore(xmlDecl, xmlNode);

			// Store recipe templates
			var recipesNode = xmlNode.AddElement("Recipes");
			foreach (var recipe in recipes.DistinctBy(r => r.id))
			{
				var recipeNode = recipesNode.AddElement("Recipe");
				recipeNode.AddAttribute("id", recipe.id.ToString());
				recipe.SaveToXml(recipeNode);
			}

			foreach (var recipe in recipes)
			{
				var instanceNode = xmlNode.AddElement("Instance");
				instanceNode.AddAttribute("id", recipe.id.ToString());
				instanceNode.AddAttribute("active", recipe.isEnabled);
				instanceNode.AddAttribute("collapse", recipe.isCollapsed);
				if (recipe.canToggleTextFormatting)
					instanceNode.AddAttribute("format", recipe.enableTextFormatting);

				// Parameters
				foreach (var parameter in recipe.parameters)
					parameter.SaveValueToXml(instanceNode);
			}

			StringBuilder sbXml = new StringBuilder();
			using (var stringWriter = new StringWriterUTF8(sbXml))
			{
				XmlWriterSettings settings = new XmlWriterSettings();
				settings.Indent = false;

				using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
				{
					xmlDoc.Save(xmlWriter);
				}
			}

			return new RecipeClipboard() {
				data = sbXml.ToString(),
				version = 1,
			};
		}

		public List<Recipe> ToRecipes()
		{
			if (string.IsNullOrEmpty(data))
				return null;

			List<Recipe> recipes = new List<Recipe>();

			XmlDocument xmlDoc = new XmlDocument();
			try
			{
				byte[] payload = Encoding.UTF8.GetBytes(data);
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
			int version = xmlDoc.DocumentElement.GetAttributeInt("version", 0);
			if (version > GingerCardV1.Version)
				return null; // Unsupported version

			var xmlNode = xmlDoc.DocumentElement;

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
			var instanceNode = xmlNode.GetFirstElement("Instance");
			while (instanceNode != null)
			{
				StringHandle recipeID = instanceNode.GetAttribute("id", null);
				bool isActive = instanceNode.GetAttributeBool("active", true);
				bool isCollapsed = instanceNode.GetAttributeBool("collapse", false);

				Recipe recipe;
				Recipe recipeTemplate;
				if (recipesByID.TryGetValue(recipeID, out recipeTemplate) == false)
					continue; // Loss of information

				// Use a local recipe if one exists
				RecipeTemplate equivalentTemplate = RecipeBook.GetEquivalentRecipe(recipeTemplate);
				if (equivalentTemplate != null)
					recipe = equivalentTemplate.Instantiate();
				else
				{
					recipe = (Recipe)recipeTemplate.Clone();
					recipe.filename = Constants.Flag.External;
				}
				recipe.isEnabled = isActive;
				recipe.isCollapsed = isCollapsed;

				// Read parameters
				var parameterNode = instanceNode.GetFirstElement("Parameter");
				while (parameterNode != null)
				{
					StringHandle parameterID = parameterNode.GetAttribute("id", null);
					var parameter = recipe.parameters.Find(p => p.id == parameterID);
					if (parameter != null)
						parameter.LoadValueFromXml(parameterNode, Utility.FirstNonEmpty(Current.MainCharacter.spokenName, Constants.DefaultCharacterName), Current.Card.userPlaceholder);
					parameterNode = parameterNode.GetNextSibling();
				}

				recipes.Add(recipe);

				instanceNode = instanceNode.GetNextSibling();
			}

			return recipes;
		}

		
	}
}
