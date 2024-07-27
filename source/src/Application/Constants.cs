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
			{ Recipe.Drawer.Appearance,			ColorTranslator.FromHtml("#fffef0") },
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
			{ Recipe.Category.Job,				Color.Honeydew },

			{ Recipe.Category.Appearance,	    ColorTranslator.FromHtml("#fffef0") },
			{ Recipe.Category.Speech,		    ColorTranslator.FromHtml("#fffef0") },
			{ Recipe.Category.Trait,		    ColorTranslator.FromHtml("#fffef0") },
			{ Recipe.Category.Special,		    ColorTranslator.FromHtml("#fffef0") },
			{ Recipe.Category.Body,				ColorTranslator.FromHtml("#fffef0") },
			{ Recipe.Category.Feature,			ColorTranslator.FromHtml("#fffef0") },

			{ Recipe.Category.Personality,	    ColorTranslator.FromHtml("#d2f0f0") },
			{ Recipe.Category.Mind,				Color.Azure },
			{ Recipe.Category.Behavior,			Color.Azure },
			{ Recipe.Category.Quirk,			Color.Azure },
			{ Recipe.Category.Emotion,          Color.Azure },
			{ Recipe.Category.Sexual,           ColorTranslator.FromHtml("#fff0f8") },

			{ Recipe.Category.User,			    ColorTranslator.FromHtml("#ddf5ef") },
			{ Recipe.Category.Relationship,	    ColorTranslator.FromHtml("#ddf5ef") },

			{ Recipe.Category.Story,			Color.Linen },
			{ Recipe.Category.Role,				Color.Linen },
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
			{ Recipe.Category.Undefined,		Recipe.Drawer.Character },
			{ Recipe.Category.Base,				Recipe.Drawer.Model },
			{ Recipe.Category.Model,			Recipe.Drawer.Model },
			{ Recipe.Category.Modifier,			Recipe.Drawer.Model },
			{ Recipe.Category.Archetype,		Recipe.Drawer.Character },
			{ Recipe.Category.Character,		Recipe.Drawer.Character },
			{ Recipe.Category.Job,				Recipe.Drawer.Character },
			{ Recipe.Category.Relationship,		Recipe.Drawer.Character },
			{ Recipe.Category.Speech,			Recipe.Drawer.Character },
			{ Recipe.Category.Custom,			Recipe.Drawer.Character },
			{ Recipe.Category.Chat,				Recipe.Drawer.Character },
			{ Recipe.Category.Trait,			Recipe.Drawer.Character },
			{ Recipe.Category.Special,			Recipe.Drawer.Character },
			{ Recipe.Category.Appearance,		Recipe.Drawer.Appearance },
			{ Recipe.Category.Body,				Recipe.Drawer.Appearance },
			{ Recipe.Category.Feature,			Recipe.Drawer.Appearance },
			{ Recipe.Category.Personality,		Recipe.Drawer.Mind },
			{ Recipe.Category.Mind,				Recipe.Drawer.Mind },
			{ Recipe.Category.Behavior,			Recipe.Drawer.Mind },
			{ Recipe.Category.Quirk,			Recipe.Drawer.Mind },
			{ Recipe.Category.Emotion,			Recipe.Drawer.Mind },
			{ Recipe.Category.Sexual,			Recipe.Drawer.Mind },
			{ Recipe.Category.Role,             Recipe.Drawer.Story },
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
			public static readonly string OverrideGender = "override-gender";
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
			public static readonly string ToggleFormatting = "__formatting";
		}

		public static readonly string DefaultName = "Unnamed";
		public static readonly float AutoWrapWidth = 54;

		public static readonly string DefaultFontFace = "Segoe UI";
		public static readonly float DefaultFontSize = 9.75f;
		public static readonly float ReferenceFontSize = 8.25f;
		public static readonly float LineHeight = 1.15f;

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

		public static class Colors
		{
			public static class Light
			{
				public static readonly Color Foreground	= ColorTranslator.FromHtml("#202020");
				public static readonly Color Background	= ColorTranslator.FromHtml("#FFFFFF");
				public static readonly Color Dialogue	= ColorTranslator.FromHtml("#C06000");
				public static readonly Color Narration	= ColorTranslator.FromHtml("#406080");
				public static readonly Color Number		= ColorTranslator.FromHtml("#800080");
				public static readonly Color Name		= ColorTranslator.FromHtml("#0000C0");
				public static readonly Color Command	= ColorTranslator.FromHtml("#800000");
				public static readonly Color Pronoun	= ColorTranslator.FromHtml("#C000C0");
				public static readonly Color Comment	= ColorTranslator.FromHtml("#969696");
				public static readonly Color Code		= ColorTranslator.FromHtml("#606060");
				public static readonly Color Error		= ColorTranslator.FromHtml("#C00000");
				public static readonly Color Wildcard	= ColorTranslator.FromHtml("#008080");
				public static readonly Color Decorator	= ColorTranslator.FromHtml("#800080");
			}

			public static class Dark
			{
				public static readonly Color Foreground	= ColorTranslator.FromHtml("#DEDEDE");
				public static readonly Color Background	= ColorTranslator.FromHtml("#222222");
				public static readonly Color Dialogue	= ColorTranslator.FromHtml("#F0A400");
				public static readonly Color Narration	= ColorTranslator.FromHtml("#93ACC4");
				public static readonly Color Number		= ColorTranslator.FromHtml("#BE7BE3");
				public static readonly Color Name		= ColorTranslator.FromHtml("#4FB0FF");
				public static readonly Color Command	= ColorTranslator.FromHtml("#C06000");
				public static readonly Color Pronoun	= ColorTranslator.FromHtml("#D000D0");
				public static readonly Color Comment	= ColorTranslator.FromHtml("#636363");
				public static readonly Color Code		= ColorTranslator.FromHtml("#969696");
				public static readonly Color Error		= ColorTranslator.FromHtml("#D12640");
				public static readonly Color Wildcard	= ColorTranslator.FromHtml("#00A0A0");
				public static readonly Color Decorator	= ColorTranslator.FromHtml("#A000A0");
			}
		}
	}

	public static class ShortcutKeys
	{
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
		public const Keys SaveIncremental = Keys.Control | Keys.Shift | Keys.Alt | Keys.S;
	}
}
