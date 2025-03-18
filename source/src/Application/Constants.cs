using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public static class Constants
	{
		public static readonly string WebsiteURL = "https://github.com/DominaeDev/ginger";
		public static readonly string GitHubURL = "https://github.com/DominaeDev/ginger";
		public static readonly string DictionariesURL = "https://github.com/LibreOffice/dictionaries";

		public static readonly int GingerExtensionVersion = 1;
		public static readonly Color DefaultColor = Color.Gainsboro;

		public static Dictionary<Recipe.Drawer, Color> RecipeColorByDrawer = new Dictionary<Recipe.Drawer, Color>
		{
			{ Recipe.Drawer.Undefined,			Color.Gainsboro },
			{ Recipe.Drawer.Components,			Color.Gainsboro },
			{ Recipe.Drawer.Snippets,			ColorTranslator.FromHtml("#f0f0f0") },
			{ Recipe.Drawer.Lore,				ColorTranslator.FromHtml("#f2e6f2") },
			{ Recipe.Drawer.Model,				ColorTranslator.FromHtml("#bfd0db") },
			{ Recipe.Drawer.Character,			Color.Honeydew },
			{ Recipe.Drawer.Traits,				ColorTranslator.FromHtml("#fffef0") },
			{ Recipe.Drawer.Mind,				Color.Azure },
			{ Recipe.Drawer.Story,				Color.Linen  },
		};

		public static Dictionary<Recipe.Category, Color> RecipeColorByCategory = new Dictionary<Recipe.Category, Color>
		{
			{ Recipe.Category.Undefined,		Color.Gainsboro },
			{ Recipe.Category.Base,			    ColorTranslator.FromHtml("#98acb9") },
			{ Recipe.Category.Model,			ColorTranslator.FromHtml("#bfd0db") },
			{ Recipe.Category.Modifier,			ColorTranslator.FromHtml("#bfd0db") },
	   
			{ Recipe.Category.Archetype,		Color.Honeydew },
			{ Recipe.Category.Character,		Color.Honeydew },
			{ Recipe.Category.Special,		    Color.Honeydew },

			{ Recipe.Category.Appearance,	    ColorTranslator.FromHtml("#fffef0") },
			{ Recipe.Category.Body,				ColorTranslator.FromHtml("#fffef0") },
			{ Recipe.Category.Trait,		    ColorTranslator.FromHtml("#fffef0") },
			{ Recipe.Category.Feature,			ColorTranslator.FromHtml("#fffef0") },
			{ Recipe.Category.Speech,		    ColorTranslator.FromHtml("#fffef0") },

			{ Recipe.Category.Relationship,	    ColorTranslator.FromHtml("#fffef0") },
			{ Recipe.Category.Job,				ColorTranslator.FromHtml("#fffef0") },
			{ Recipe.Category.Role,				ColorTranslator.FromHtml("#fffef0") },

			{ Recipe.Category.Personality,	    ColorTranslator.FromHtml("#d2f0f0") },
			{ Recipe.Category.Mind,				Color.Azure },
			{ Recipe.Category.Behavior,			Color.Azure },
			{ Recipe.Category.Quirk,			Color.Azure },
			{ Recipe.Category.Emotion,          Color.Azure },
			{ Recipe.Category.Sexual,           ColorTranslator.FromHtml("#fff0f8") },

			{ Recipe.Category.User,			    ColorTranslator.FromHtml("#ddf5ef") },

			{ Recipe.Category.Story,			Color.Linen },
			{ Recipe.Category.World,			Color.Linen },
			{ Recipe.Category.Location,			Color.Linen },
			{ Recipe.Category.Scenario,			Color.Linen },
			{ Recipe.Category.Cast,				Color.Linen },
			{ Recipe.Category.Concept,			Color.Linen },

			{ Recipe.Category.Chat,			    Color.FloralWhite },
			{ Recipe.Category.Lore,			    ColorTranslator.FromHtml("#f2e6f2") },
			{ Recipe.Category.Custom,		    Color.WhiteSmoke },
		};

		public static Dictionary<Recipe.Category, Recipe.Drawer> DrawerFromCategory = new Dictionary<Recipe.Category, Recipe.Drawer>
		{
			{ Recipe.Category.Undefined,		Recipe.Drawer.Traits },
			{ Recipe.Category.Base,				Recipe.Drawer.Model },
			{ Recipe.Category.Model,			Recipe.Drawer.Model },
			{ Recipe.Category.Modifier,			Recipe.Drawer.Model },
			
			{ Recipe.Category.Archetype,		Recipe.Drawer.Character },
			{ Recipe.Category.Character,		Recipe.Drawer.Character },
			{ Recipe.Category.Body,				Recipe.Drawer.Character },
			{ Recipe.Category.Appearance,		Recipe.Drawer.Character },
			
			{ Recipe.Category.Trait,			Recipe.Drawer.Traits },
			{ Recipe.Category.Feature,			Recipe.Drawer.Traits },
			{ Recipe.Category.Speech,			Recipe.Drawer.Traits },
			{ Recipe.Category.Special,			Recipe.Drawer.Traits },
			{ Recipe.Category.Custom,			Recipe.Drawer.Traits },
			
			{ Recipe.Category.Personality,		Recipe.Drawer.Mind },
			{ Recipe.Category.Mind,				Recipe.Drawer.Mind },
			{ Recipe.Category.Behavior,			Recipe.Drawer.Mind },
			{ Recipe.Category.Quirk,			Recipe.Drawer.Mind },
			{ Recipe.Category.Emotion,			Recipe.Drawer.Mind },
			{ Recipe.Category.Sexual,			Recipe.Drawer.Mind },
			{ Recipe.Category.Job,				Recipe.Drawer.Mind },
			{ Recipe.Category.Role,             Recipe.Drawer.Mind },
			{ Recipe.Category.Relationship,		Recipe.Drawer.Mind },

			{ Recipe.Category.Chat,				Recipe.Drawer.Story },
			{ Recipe.Category.Story,			Recipe.Drawer.Story },
			{ Recipe.Category.World,			Recipe.Drawer.Story },
			{ Recipe.Category.User,				Recipe.Drawer.Story },
			{ Recipe.Category.Location,			Recipe.Drawer.Story },
			{ Recipe.Category.Scenario,			Recipe.Drawer.Story },
			{ Recipe.Category.Cast,				Recipe.Drawer.Story },
			{ Recipe.Category.Concept,			Recipe.Drawer.Story },
			{ Recipe.Category.Lore,				Recipe.Drawer.Story },
		};

		public static class Flag
		{
			public static readonly string Base = "base";
			public static readonly string NSFW = "nsfw";
			public static readonly string Actor = "__actor";
			public static readonly string Internal = "__internal";
			public static readonly string External = "__external";
			public static readonly string Component = "__component";
			public static readonly string System = "__system";
			public static readonly string Lorebook = "__lorebook";
			public static readonly string Greeting = "__greeting";
			public static readonly string Grammar = "__grammar";
			public static readonly string DontBake = "__nobake";
			public static readonly string Hidden = "__hidden";
			public static readonly string PruneScenario = "__prune-scenario";
			public static readonly string ToggleFormatting = "__formatting";
			public static readonly string NSFWOptional = "__nsfw-optional";
			public static readonly string LevelOfDetail = "__detail-optional";
		}

		public static class Variables
		{
			public static readonly string Adjectives	= "summary:adjectives";
			public static readonly string Noun			= "summary:noun";
			public static readonly string Addendum		= "summary:addendum";
			public static readonly string NoAffix		= "summary:noun:noaffix";
			public static readonly string Prefix		= "summary:noun:prefix";
			public static readonly string Suffix		= "summary:noun:suffix";
			
			public static readonly string CardName = "card"; //! todo
			public static readonly string CharacterName = "name";
			public static readonly string CharacterGender = "gender";
			public static readonly string UserName = "#name";
			public static readonly string UserGender = "#gender";
		}

		public static readonly string DefaultCharacterName = "Unnamed";
		public static readonly string UnknownCharacter = "{unknown}";
		public static readonly string DefaultUserName = "User";
		public static readonly float AutoWrapWidth = 54;
		public static readonly int DefaultPortraitWidth = 256;
		public static readonly int DefaultPortraitHeight = 256;

		public static readonly string DefaultFontFace = "Segoe UI";
		public static readonly float DefaultFontSize = 9.75f;
		public static readonly float ReferenceFontSize = 8.25f;
		public static readonly float LineHeight = 1.15f;

		public static readonly int StatusBarMessageInterval = 3500;

		public static readonly int MaxImageDimension = 1800;
		public static readonly int MaxActorCount = 8;

		public static class ParameterPanel
		{
			public static readonly int TopMargin = 2;
			public static readonly int Spacing = 4;
			public static readonly int LabelWidth = 140;
			public static readonly int CheckboxWidth = 30;

		}

		public static class RecipePanel
		{
			public static readonly int BottomMargin = 8;
		}

		public static class InternalFiles
		{
			public static readonly string GlobalMacros = "global_macros.xml";
			public static readonly string GlobalRecipe = "global_recipe.xml";
		}

		public static class Drawer
		{
			public static readonly int SplitMenuAfter = 28;
			public static readonly int RecipesPerSplit = 20;
		}

	}

	public static class ShortcutKeys
	{
		public const Keys Copy = Keys.Control | Keys.C;
		public const Keys Paste = Keys.Control | Keys.V;
		public const Keys Undo = Keys.Control | Keys.Z;
		public const Keys Redo = Keys.Control | Keys.Y;
		public const Keys EraseWord = Keys.Control | Keys.Back;
		public const Keys DeleteWord = Keys.Control | Keys.Delete;
		public const Keys AutoReplace = Keys.Control | Keys.Space;
		public const Keys AutoReplaceUser = Keys.Control | Keys.Shift | Keys.Space;
		public const Keys AutoReplaceErase = Keys.Control | Keys.Alt | Keys.Space;
		public const Keys Confirm = Keys.Enter;
		public const Keys Cancel = Keys.Escape;
		public const Keys Find = Keys.Control | Keys.F;
		public const Keys FindNext = Keys.F3;
		public const Keys FindPrevious = Keys.Shift | Keys.F3;
		public const Keys Replace = Keys.Control | Keys.Shift | Keys.F;
		public const Keys NextActor = Keys.Alt | Keys.Right;
		public const Keys PreviousActor = Keys.Alt | Keys.Left;
		public const Keys SwitchView = Keys.Control | Keys.Tab;
		public const Keys SaveIncremental = Keys.Control | Keys.Alt | Keys.S;
		public const Keys LinkedOpen = Keys.Control | Keys.Shift | Keys.O;
		public const Keys LinkedSave = Keys.Control | Keys.U;
		public const Keys LinkedSaveAsNew = Keys.Control | Keys.Shift | Keys.U;
		public const Keys LinkedChatHistory = Keys.Control | Keys.H;
		public const Keys ViewEmbeddedAssets = Keys.Control | Keys.Alt | Keys.A;
		public const Keys ViewUserVariables = Keys.Control | Keys.Alt | Keys.V;
	}
}
