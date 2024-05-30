using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Ginger
{
	public class RecipePreset : IXmlLoadable
	{
		public StringHandle id;
		public string filename;
		public string name;
		public string[] path;

		public string cardName;
		public string characterName;
		public string characterGender;

		public struct RecipeInfo
		{
			public StringHandle id;
			public List<ParameterInfo> parameters;
			public bool collapsed;
		}

		public struct ParameterInfo
		{
			public StringHandle id;
			public string value;
			public bool enabled;
		}

		public List<RecipeInfo> recipes = new List<RecipeInfo>();

		public bool LoadFromXml(XmlNode xmlNode)
		{
			name = xmlNode.GetValueElement("Name", null);
			if (name == null)
				return false;
			name = name.Trim();
			if (name.Length == 0)
				return false;

			id = name;

			// Path
			name = name.Replace("//", "%%SLASH%%");
			var lsPath = name.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.Select(s => s.Replace("%%SLASH%%", "/"))
				.ToList();

			name = lsPath[lsPath.Count - 1];
			path = lsPath.Take(lsPath.Count - 1).ToArray();

			// Character info
			cardName = xmlNode.GetValueElement("CardName").SingleLine();
			characterName = xmlNode.GetValueElement("CharacterName").SingleLine();
			characterGender = xmlNode.GetValueElement("Gender").SingleLine();

			// Parameters
			recipes.Clear();
			var recipeNode = xmlNode.GetFirstElement("Recipe"); 
			while (recipeNode != null)
			{
				string name = recipeNode.GetAttribute("name");
				bool bCollapsed = recipeNode.GetAttributeBool("collapsed");
				var recipe = new RecipeInfo() {
					id = name,
					parameters = new List<ParameterInfo>(),
					collapsed = bCollapsed,
				};
				recipes.Add(recipe);

				var parameterNode = recipeNode.GetFirstElement("Parameter");
				while (parameterNode != null)
				{
					ParameterInfo parameter = new ParameterInfo();
					parameter.id = parameterNode.GetAttribute("id", "");
					parameter.value = parameterNode.GetTextValue().Trim();
					parameter.enabled = parameterNode.GetAttributeBool("enabled", true);
					recipe.parameters.Add(parameter);

					parameterNode = parameterNode.GetNextSibling();
				}

				recipeNode = recipeNode.GetNextSibling();
			}

			return recipes.Count > 0;
		}
	}
}
