using Ginger.Properties;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public static class DarkTheme
	{
		private class ColorTable
		{
			// Interface
			public static readonly Color ControlForeground = ColorTranslator.FromHtml("#f0f0f0");
			public static readonly Color ControlBackground = ColorTranslator.FromHtml("#404040");
			public static readonly Color TextBoxForeground = ColorTranslator.FromHtml("#DEDEDE");
			public static readonly Color TextBoxBackground = ColorTranslator.FromHtml("#262626");
			public static readonly Color TextBoxBorder = Color.Gray;
			public static readonly Color TextBoxPlaceholder = ColorTranslator.FromHtml("#606060");
			public static readonly Color TextBoxDisabledBackground = ColorTranslator.FromHtml("#363636");
			public static readonly Color TextBoxDisabledBorder = Color.Gray;
			public static readonly Color MenuForeground = Color.White;
			public static readonly Color MenuBackground = ColorTranslator.FromHtml("#404040");
			public static readonly Color MenuBorder = ColorTranslator.FromHtml("#202020");
			public static readonly Color MenuGradientBegin = ColorTranslator.FromHtml("#404040");
			public static readonly Color MenuGradientMiddle = ColorTranslator.FromHtml("#3c3c3c");
			public static readonly Color MenuGradientEnd = ColorTranslator.FromHtml("#343434");
			public static readonly Color MenuSeparator = ColorTranslator.FromHtml("#202020");
			public static readonly Color Highlight = SystemColors.Highlight;
			public static readonly Color HighlightInactive = ColorTranslator.FromHtml("#505050");
			public static readonly Color HighlightText = SystemColors.HighlightText;
			public static readonly Color MenuItemSelectedBorder = Color.FromArgb(48, 78, 102);
			public static readonly Color MenuItemSelected = Color.FromArgb(32, 92, 140);
			public static readonly Color Button = ColorTranslator.FromHtml("#363636");
			public static readonly Color ButtonHover = ColorTranslator.FromHtml("#2d2d2d");
			public static readonly Color ButtonPressed = ColorTranslator.FromHtml("#2d2d2d");
			public static readonly Color ButtonText = ControlForeground;
			public static readonly Color ButtonDisabled = ColorTranslator.FromHtml("#484848");
			public static readonly Color ButtonDisabledText = ColorTranslator.FromHtml("#606060");
			public static readonly Color ImageButton = ColorTranslator.FromHtml("#363636");
			public static readonly Color ImageButtonHover = ColorTranslator.FromHtml("#2d2d2d");
			public static readonly Color ImageButtonPressed = ColorTranslator.FromHtml("#2d2d2d");
			public static readonly Color ButtonBorder = ColorTranslator.FromHtml("#adadad");
			public static readonly Color ButtonDisabledBorder = ColorTranslator.FromHtml("#808080");
			public static readonly Color Border = Color.Gray;
			public static readonly Color GroupBoxBorder = ColorTranslator.FromHtml("#606060");
			public static readonly Color TreeViewForeground = TextBoxForeground;
			public static readonly Color TreeViewBackground = ColorTranslator.FromHtml("#303030");
			public static readonly Color WarningRed = ColorTranslator.FromHtml("#ff4040");
			public static readonly Color HighlightBorder = Color.LightGray;
			public static readonly Color GrayText = SystemColors.GrayText;
			public static readonly Color TabInactiveText = ColorTranslator.FromHtml("#c0c0c0");
			public static readonly Color TabBorder = ColorTranslator.FromHtml("#808080");
			public static readonly Color TabEdgeBorder = ColorTranslator.FromHtml("#606060");
			public static readonly Color SelectedTabBorder = ColorTranslator.FromHtml("#c0c0c0");
			public static readonly Color SeletedTabButtonLight = ColorTranslator.FromHtml("#808080");
			public static readonly Color SeletedTabButtonDark = ColorTranslator.FromHtml("#606060");
			public static readonly Color RecipeListBackground = ColorTranslator.FromHtml("#282828");
			public static readonly Color RecipeListGradient = ColorTranslator.FromHtml("#3a3a3a");
			public static readonly Color OutputForeground = ColorTranslator.FromHtml("#f9eeb9");
			public static readonly Color OutputBackground = TextBoxBackground;
			public static readonly Color NotesForeground = TextBoxForeground;
			public static readonly Color NotesBackground = TextBoxBackground;
			public static readonly Color Grid = Border;
			public static readonly Color Workspace = ColorTranslator.FromHtml("#202020");

			// Text
			public static readonly Color Dialogue = ColorTranslator.FromHtml("#ffac60");
			public static readonly Color Narration = ColorTranslator.FromHtml("#a7b7c5");
			public static readonly Color Number = ColorTranslator.FromHtml("#BE7BE3");
			public static readonly Color Name = ColorTranslator.FromHtml("#4FB0FF");
			public static readonly Color Command = ColorTranslator.FromHtml("#c2814c");
			public static readonly Color Pronoun = ColorTranslator.FromHtml("#D000D0");
			public static readonly Color Comment = ColorTranslator.FromHtml("#636363");
			public static readonly Color Code = ColorTranslator.FromHtml("#969696");
			public static readonly Color Error = ColorTranslator.FromHtml("#ff5569");
			public static readonly Color Wildcard = ColorTranslator.FromHtml("#00A0A0");
			public static readonly Color Decorator = ColorTranslator.FromHtml("#A000A0");
			public static readonly Color Variable = ColorTranslator.FromHtml("#BE7BE3");
		}

		public class ThemeColors : IVisualTheme
		{
			// Interface
			public Color ControlForeground => ColorTable.ControlForeground;
			public Color ControlBackground => ColorTable.ControlBackground;
			public Color TextBoxForeground => ColorTable.TextBoxForeground;
			public Color TextBoxBackground => ColorTable.TextBoxBackground;
			public Color TextBoxBorder => ColorTable.TextBoxBorder;
			public Color TextBoxPlaceholder => ColorTable.TextBoxPlaceholder;
			public Color TextBoxDisabledBackground => ColorTable.TextBoxDisabledBackground;
			public Color TextBoxDisabledBorder => ColorTable.TextBoxDisabledBorder;
			public Color MenuForeground => ColorTable.MenuForeground;
			public Color MenuBackground => ColorTable.MenuBackground;
			public Color MenuBorder => ColorTable.MenuBorder;
			public Color MenuGradientBegin => ColorTable.MenuGradientBegin;
			public Color MenuGradientMiddle => ColorTable.MenuGradientMiddle;
			public Color MenuGradientEnd => ColorTable.MenuGradientEnd;
			public Color MenuSeparator => ColorTable.MenuSeparator;
			public Color Highlight => ColorTable.Highlight;
			public Color HighlightInactive => ColorTable.HighlightInactive;
			public Color HighlightText => ColorTable.HighlightText;
			
			public Color Button => ColorTable.Button;
			public Color ButtonBorder => ColorTable.ButtonBorder;
			public Color ButtonDisabledBorder => ColorTable.ButtonDisabledBorder;
			public Color ButtonDisabled => ColorTable.ButtonDisabled;
			public Color ButtonDisabledText => ColorTable.ButtonDisabledText;
			public Color ButtonText => ColorTable.ButtonText;
			public Color ButtonHover => ColorTable.ButtonHover;
			public Color ButtonPressed => ColorTable.ButtonPressed;

			public Color ImageButton => ColorTable.ImageButton;
			public Color ImageButtonHover => ColorTable.ImageButtonHover;
			public Color ImageButtonPressed => ColorTable.ImageButtonPressed;
			public Color Border => ColorTable.Border;
			public Color GroupBoxBorder => ColorTable.GroupBoxBorder;
			public Color TreeViewForeground => ColorTable.TreeViewForeground;
			public Color TreeViewBackground => ColorTable.TreeViewBackground;
			public Color WarningRed => ColorTable.WarningRed;
			public Color HighlightBorder => ColorTable.HighlightBorder;
			public Color GrayText => ColorTable.GrayText;
			public Color TabInactiveText => ColorTable.TabInactiveText;
			public Color TabBorder => ColorTable.TabBorder;
			public Color TabEdgeBorder => ColorTable.TabEdgeBorder;
			public Color SelectedTabBorder => ColorTable.SelectedTabBorder;
			public Color SeletedTabButtonLight => ColorTable.SeletedTabButtonLight;
			public Color SeletedTabButtonDark => ColorTable.SeletedTabButtonDark;
			public Color RecipeListBackground => ColorTable.RecipeListBackground;
			public Color RecipeListGradient => ColorTable.RecipeListGradient;
			public Color OutputForeground => ColorTable.OutputForeground;
			public Color OutputBackground => ColorTable.OutputBackground;
			public Color NotesForeground => ColorTable.NotesForeground;
			public Color NotesBackground => ColorTable.NotesBackground;
			public Color Grid => ColorTable.Grid;
			public Color Workspace => ColorTable.Workspace;

			// Text
			public Color Dialogue => ColorTable.Dialogue;
			public Color Narration => ColorTable.Narration;
			public Color Number => ColorTable.Number;
			public Color Name => ColorTable.Name;
			public Color Command => ColorTable.Command;
			public Color Pronoun => ColorTable.Pronoun;
			public Color Comment => ColorTable.Comment;
			public Color Code => ColorTable.Code;
			public Color Error => ColorTable.Error;
			public Color Wildcard => ColorTable.Wildcard;
			public Color Decorator => ColorTable.Decorator;
			public Color Variable => ColorTable.Variable;

			// Icons
			public Image MenuIcon => Resources.dark_menu;
			public Image MenuEditIcon => Resources.dark_menu_edit;
			public Image MenuFolder => Resources.dark_folder;
			public Image MenuRedDot => Resources.dark_red_dot;
			public Image MenuSnippet => Resources.dark_snippet_small;
			public Image MenuLore => Resources.dark_lore_small;

			public Image ButtonModel => Resources.dark_model;
			public Image ButtonCharacter => Resources.dark_persona;
			public Image ButtonTraits => Resources.dark_traits;
			public Image ButtonMind => Resources.dark_personality;
			public Image ButtonStory => Resources.dark_story;
			public Image ButtonComponents => Resources.dark_component;
			public Image ButtonSnippets => Resources.dark_snippet;
			public Image ButtonLore => Resources.dark_lore;

			public Image Checker => Resources.dark_checker;
			public Image Write => Resources.dark_write;
			public Image MoveLoreUp => Resources.dark_lore_up;
			public Image MoveLoreDown => Resources.dark_lore_down;
			public Image RemoveLore => Resources.dark_delete;
			public Image ArrowLeft => Resources.dark_arrow_left;
			public Image ArrowRight => Resources.dark_arrow_right;

			public Image LinkConnected => Resources.dark_link_connected;
			public Image LinkActive => Resources.dark_link_active;
			public Image LinkActiveDirty => Resources.dark_link_dirty;
			public Image LinkInactive => Resources.dark_link_inactive;
			public Image LinkBroken => Resources.dark_link_broken;

			public Image EmbeddedAssets => Resources.dark_embedded_assets;
			public Image MenuEmbeddedAssets => Resources.dark_menu_embedded_assets;
			public Image DeleteCharacters => Resources.dark_delete_character;
			public Image CleanIcon => Resources.dark_clean;
			public Image RepairIcon => Resources.dark_repair;
			public Image CreateBackupIcon => Resources.dark_create_backup;
			public Image RestoreBackupIcon => Resources.dark_restore_backup;

			public Image Collapsed => Resources.dark_collapsed;
			public Image Expanded => Resources.dark_expanded;

			public Image PortraitOverrideAsset => Resources.dark_asset_override;
			public Image ActorPortraitAsset => Resources.dark_asset_actor_portrait;
		}

		public class ToolStripColorTable : ProfessionalColorTable
		{
			public override Color ToolStripDropDownBackground => ColorTable.MenuBackground;
			public override Color MenuStripGradientBegin => ColorTable.MenuBackground;
			public override Color MenuStripGradientEnd => ColorTable.MenuBackground;
			public override Color ImageMarginGradientBegin => ColorTable.MenuGradientBegin;
			public override Color ImageMarginGradientMiddle => ColorTable.MenuGradientMiddle;
			public override Color ImageMarginGradientEnd => ColorTable.MenuGradientEnd;
			public override Color MenuBorder => ColorTable.MenuBorder;
			public override Color MenuItemBorder => ColorTable.Highlight;
			public override Color MenuItemPressedGradientBegin => ColorTable.MenuBackground;
			public override Color MenuItemPressedGradientEnd => ColorTable.MenuBackground;
			public override Color ButtonSelectedBorder => ColorTable.Highlight;
			public override Color SeparatorLight => ColorTable.MenuSeparator;
			public override Color SeparatorDark => ColorTable.MenuSeparator;
			public override Color MenuItemSelected => ColorTable.MenuItemSelectedBorder;
			public override Color ButtonSelectedHighlight => ColorTable.MenuItemSelectedBorder;
			public override Color CheckBackground => ColorTable.MenuItemSelected;
			public override Color CheckSelectedBackground => ColorTable.MenuItemSelected;
			public override Color MenuItemSelectedGradientBegin => ColorTable.MenuItemSelected;
			public override Color MenuItemSelectedGradientEnd =>ColorTable.MenuItemSelected;
		}
	}
}
