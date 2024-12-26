using Ginger.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ginger
{
	public class RecipeTemplate
	{
		public int uid { get { return _template != null ? _template.uid : -1; } }
		public string label { get { return _template != null ? _template.GetMenuLabel() : string.Empty; } }
		public VersionNumber version { get { return _template != null ? _template.version : default(VersionNumber); } }
		public string tooltip { get { return _template != null ? _template.GetTooltip() : string.Empty; } }
		public Recipe.Type type { get { return _template != null ? _template.type : Recipe.Type.Recipe; } }
		public bool allowMultiple { get { return _template != null ? _template.allowMultiple : false; } }
		public ICondition requires { get { return _template != null ? _template.requires : null; } }
		public int? order { get { return _template != null ? _template.order : null; } }
		public int includes { get { return _template != null ? _template.includes.Count : 0; } }
		public bool hasDetached { get { return _template != null ? _template.templates.ContainsAny(t => t.isDetached) : false; } }

		public RecipeTemplate(Recipe template)
		{
			_template = template;
		}

		public Recipe Instantiate()
		{
			var clone = (Recipe)_template.Clone();
			clone.isEnabled = true;
			clone.ResetParameters();
			return clone;
		}

		private Recipe _template;
	}

	public static class RecipeBook
	{
		public static readonly string GlobalInternal = "__internal";
		public static readonly string GlobalExternal = "__global";
		public static readonly string PruneScenario = "__prune-scenario";

		public static void LoadRecipes()
		{
			recipes.Clear();
			presets.Clear();

			// Read recipes
			var recipeFiles = Utility.FindFilesInFolder(Utility.ContentPath("Recipes"), "*.xml", true);
			for (int i = 0; i < recipeFiles.Length; ++i)
			{
				var recipe = new Recipe(recipeFiles[i]);
				if (recipe.LoadFromXml(recipeFiles[i], "Ginger"))
					recipes.Add(recipe);
			}
			recipes = recipes.DistinctByVersion().ToList();

			// Read snippets
			var snippetFiles = Utility.FindFilesInFolder(Utility.ContentPath("Snippets"), "*.snippet", true);
			for (int i = 0; i < snippetFiles.Length; ++i)
			{
				var recipe = new Recipe(snippetFiles[i]);
				if (recipe.LoadFromXml(snippetFiles[i], "Ginger"))
				{
					recipe.type = Recipe.Type.Snippet;
					recipes.Add(recipe);
				}
			}

			// Presets
			var presetFiles = Utility.FindFilesInFolder(Utility.ContentPath("Templates"), "*.xml", true);
			for (int i = 0; i < presetFiles.Length; ++i)
			{
				var preset = new RecipePreset();
				if (preset.LoadFromXml(presetFiles[i], "GingerTemplate"))
					presets.Add(preset);
			}

			// Global recipe
			string fnGlobal = Utility.ContentPath("Internal", Constants.InternalFiles.GlobalRecipe);
			var global_recipe = new Recipe(fnGlobal);
			if (global_recipe.LoadFromXml(fnGlobal, "Ginger"))
				recipes.Add(global_recipe);

			// Load recipes from resources
			recipes.Add(CreateRecipeFromResource(Resources.internal_global_recipe,	Recipe.Type.Component,	Recipe.Drawer.Undefined));
			recipes.Add(CreateRecipeFromResource(Resources.system_recipe,			Recipe.Type.Component,	Recipe.Drawer.Model));
			recipes.Add(CreateRecipeFromResource(Resources.persona_recipe,			Recipe.Type.Component,	Recipe.Drawer.Character));
			recipes.Add(CreateRecipeFromResource(Resources.user_recipe,				Recipe.Type.Component,	Recipe.Drawer.Character));
			recipes.Add(CreateRecipeFromResource(Resources.scenario_recipe,			Recipe.Type.Component,	Recipe.Drawer.Story));
			recipes.Add(CreateRecipeFromResource(Resources.example_recipe,			Recipe.Type.Component,	Recipe.Drawer.Components));
			recipes.Add(CreateRecipeFromResource(Resources.greeting_recipe,			Recipe.Type.Component,	Recipe.Drawer.Components));
			recipes.Add(CreateRecipeFromResource(Resources.lorebook_recipe,			Recipe.Type.Lore,		Recipe.Drawer.Lore));

			// Other components
			recipes.Add(CreateRecipeFromResource(Resources.attribute_recipe,		Recipe.Type.Component,	Recipe.Drawer.Components));
			recipes.Add(CreateRecipeFromResource(Resources.personality_recipe,		Recipe.Type.Component,	Recipe.Drawer.Components));
			recipes.Add(CreateRecipeFromResource(Resources.grammar_recipe,			Recipe.Type.Component,	Recipe.Drawer.Components));
			recipes.Add(CreateRecipeFromResource(Resources.post_history_recipe,		Recipe.Type.Component,	Recipe.Drawer.Components));
			recipes.Add(CreateRecipeFromResource(Resources.group_greeting_recipe,	Recipe.Type.Component,	Recipe.Drawer.Components));

			// Hidden (Internal) components
			recipes.Add(CreateRecipeFromResource(Resources.prune_scenario_recipe,	Recipe.Type.Component,	Recipe.Drawer.Undefined));

			// Load macros
			Current.LoadMacros();
		}

		private static List<Recipe> recipes = new List<Recipe>();
		private static List<RecipePreset> presets = new List<RecipePreset>();

		public static IEnumerable<Recipe> allRecipes { get { return recipes; } }
		public static IEnumerable<RecipePreset> allPresets { get { return presets; } }

		private static string[] SplitPath(string path)
		{
			return path.Split(new char[] { '/' })
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.ToArray();
		}

		public static string[] GetFolders(string root, Recipe.Drawer drawer)
		{
			string[] path = SplitPath(root);

			Func<string[], string[], bool> fnBeginsWith = (r, sub) => {
				if (r.Length >= sub.Length)
					return false;
				for (int i = 0; i < r.Length; ++i)
					if (string.Compare(r[i], sub[i], true) != 0)
						return false;
				return true;
			};

			return recipes
				.Where(r => {
					if (r.path.Length == 0)
						return false;
					if (r.isNSFW && !AppSettings.Settings.AllowNSFW)
						return false; // Not allowed
					if (r.isHidden)
						return false; // Hidden
					if (!fnBeginsWith(path, r.path))
						return false;
					if (r.type == Recipe.Type.Snippet)
						return drawer == Recipe.Drawer.Snippets;
					else if (r.type == Recipe.Type.Component)
						return drawer == Recipe.Drawer.Components;
					else if (r.drawer != drawer)
						return false; // By drawer
					return true;
				})
				.Select(r => r.path[path.Length])
				.DistinctBy(p => p.ToLowerInvariant())
				.OrderBy(p => p)
				.ToArray();
		}

		public static int[] GetRecipes(string root, Recipe.Drawer drawer)
		{
			string[] path = SplitPath(root);

			Func<string[], string[], bool> fnExact = (a, b) => {
				if (a.Length != b.Length)
					return false;
				for (int i = 0; i < a.Length; ++i)
					if (string.Compare(a[i], b[i], true) != 0)
						return false;
				return true;
			};

			return recipes
				.Where(r => {
					if (r.isNSFW && !AppSettings.Settings.AllowNSFW)
						return false; // Not allowed
					if (r.isHidden)
						return false; // Hidden
					if (fnExact(path, r.path) == false)
						return false;
					if (r.type == Recipe.Type.Snippet)
						return drawer == Recipe.Drawer.Snippets;
					else if (r.type == Recipe.Type.Component)
						return drawer == Recipe.Drawer.Components;
					else if (r.drawer != drawer)
						return false; // By drawer
					return true;
				})
				.Select(r => new {
					name = r.name,
					uid = r.uid,
					order = r.order.HasValue ? r.order.Value : 100,
				})
				.OrderBy(x => x.name.ToLowerInvariant())
				.Select(x => x.uid)
				.ToArray();
		}

		public static string[] GetPresetFolders(string root)
		{
			string[] path = SplitPath(root);

			Func<string[], string[], bool> fnBeginsWith = (r, sub) => {
				if (r.Length >= sub.Length)
					return false;
				for (int i = 0; i < r.Length; ++i)
					if (string.Compare(r[i], sub[i], true) != 0)
						return false;
				return true;
			};

			return presets
				.Where(r => {
					if (!fnBeginsWith(path, r.path))
						return false;
					return true;
				})
				.Select(r => r.path[path.Length])
				.DistinctBy(r => r.ToLowerInvariant())
				.OrderBy(r => r)
				.ToArray();
		}

		public static string[] GetPresets(string root)
		{
			string[] path = SplitPath(root);

			Func<string[], string[], bool> fnExact = (a, b) => {
				if (a.Length != b.Length)
					return false;
				for (int i = 0; i < a.Length; ++i)
					if (string.Compare(a[i], b[i], true) != 0)
						return false;
				return true;
			};

			return presets
				.Where(r => {
					if (fnExact(path, r.path) == false)
						return false;
					return true; })
				.Select(r => r.name)
				.DistinctBy(r => r.ToLowerInvariant())
				.OrderBy(r => r)
				.ToArray();
		}

		public static List<Recipe> WithInternal(Recipe recipe)
		{
			return WithInternal(new Recipe[] { recipe });
		}

		public static List<Recipe> WithInternal(IEnumerable<Recipe> recipes)
		{
			List<Recipe> list = new List<Recipe>(recipes);
			Recipe externalGlobalRecipe = GetRecipeByID(GlobalExternal)?.Instantiate();
			Recipe internalGlobalRecipe = GetRecipeByID(GlobalInternal)?.Instantiate();
			if (externalGlobalRecipe != null)
				list.Insert(0, externalGlobalRecipe);
			if (internalGlobalRecipe != null)
				list.Insert(0, internalGlobalRecipe);
			return list;
		}

		public static RecipeTemplate GetRecipeByName(string path, string name)
		{
			if (string.IsNullOrEmpty(name))
				return null;
			name = name.Replace("/", "//");
			if (string.IsNullOrEmpty(path) == false)
				return GetRecipeByID(string.Concat(path, "/", name));
			else
				return GetRecipeByID(string.Concat(name));
		}

		public static RecipeTemplate GetRecipeByID(StringHandle id)
		{
			var recipe = recipes.FirstOrDefault(r => r.id == id);
			if (recipe != null)
				return new RecipeTemplate(recipe);
			return null;
		}

		public static RecipeTemplate GetRecipeByUID(int uid)
		{
			var recipe = recipes.FirstOrDefault(r => r.uid == uid);
			if (recipe != null)
				return new RecipeTemplate(recipe);
			return null;
		}

		public static RecipePreset GetPresetByName(string path, string name)
		{
			if (string.IsNullOrEmpty(name))
				return null;
			name = name.Replace("/", "//");
			if (string.IsNullOrEmpty(path) == false)
				return GetPresetByID(string.Concat(path, "/", name));
			else
				return GetPresetByID(string.Concat(name));
		}

		public static RecipePreset GetPresetByID(StringHandle id)
		{
			return presets.FirstOrDefault(r => r.id == id);
		}

		public static Recipe AddRecipeFromResource(string xml, Recipe.Type type = Recipe.Type.Recipe)
		{
			byte[] recipeXml = Encoding.UTF8.GetBytes(xml);
			var xmlDoc = Utility.LoadXmlDocumentFromMemory(recipeXml);
			Recipe recipe = new Recipe();
			if (recipe.LoadFromXml(xmlDoc.DocumentElement) && Current.Character.AddRecipe(recipe))
			{
				recipe.type = type;
				return recipe;
			}
			return default(Recipe);
		}

		public static Recipe CreateRecipeFromResource(string xml, Recipe.Type type = Recipe.Type.Recipe, Recipe.Drawer drawer = Recipe.Drawer.Default)
		{
			byte[] recipeXml = Encoding.UTF8.GetBytes(xml);
			var xmlDoc = Utility.LoadXmlDocumentFromMemory(recipeXml);
			Recipe recipe = new Recipe();
			if (recipe.LoadFromXml(xmlDoc.DocumentElement))
			{
				recipe.type = type;
				recipe.drawer = drawer;
				return recipe;
			}
			return default(Recipe);
		}

		public static RecipeTemplate GetEquivalentRecipe(Recipe other)
		{
			if (other == null)
				return null;

			if (other.id == "lorebook")
				return GetRecipeByID("__lorebook");

			var recipe = recipes.FirstOrDefault(r => 
				r.uid == other.uid 
				|| (r.id == other.id && r.version >= other.version)
				|| (r.id == other.id && r.isInternal));
			if (recipe == null)
				return null;

			return new RecipeTemplate(recipe);
		}

		public static RecipeTemplate GetSimilarRecipe(Recipe other)
		{
			var recipe = recipes.FirstOrDefault(r => r.id == other.id);
			if (recipe == null)
				return null;
			if (recipe.uid == other.uid)
				return null; // Same
			return new RecipeTemplate(recipe);
		}

		public static IEnumerable<Recipe> DistinctByVersion(this IEnumerable<Recipe> source)
		{
			var recipesByID = source
				.GroupBy(r => r.id)
				.Select(g => {
					StringHandle id = g.Key;
					IEnumerable<Recipe> recipes = g;
					return recipes.OrderByDescending(r => r.version).FirstOrDefault();
				});

			foreach (Recipe element in recipesByID)
				yield return element;
		}
	}

}
