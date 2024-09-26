using Ginger.Properties;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public static class LightTheme
	{
		private class ColorTable
		{
			// Interface
			public static readonly Color ControlBackground = SystemColors.Control;
			public static readonly Color ControlForeground = SystemColors.ControlText;
			public static readonly Color TextBoxBackground = SystemColors.Window;
			public static readonly Color TextBoxForeground = SystemColors.WindowText;
			public static readonly Color TextBoxBorder = Color.Gray;
			public static readonly Color TextBoxPlaceholder = Color.FromArgb(144, 144, 144);
			public static readonly Color TextBoxDisabledBackground = SystemColors.ControlLight;
			public static readonly Color TextBoxDisabledBorder = SystemColors.InactiveBorder;
			public static readonly Color MenuBackground = SystemColors.MenuBar;
			public static readonly Color MenuForeground = SystemColors.MenuText;
			public static readonly Color MenuBorder = SystemColors.ControlDark;
			public static readonly Color MenuGradientBegin = SystemColors.MenuBar;
			public static readonly Color MenuGradientMiddle = SystemColors.MenuBar;
			public static readonly Color MenuGradientEnd = SystemColors.MenuBar;
			public static readonly Color MenuSeparator = SystemColors.ControlDark;
			public static readonly Color Highlight = SystemColors.Highlight;
			public static readonly Color HighlightInactive = ColorTranslator.FromHtml("#f0f0f0");
			public static readonly Color HighlightText = SystemColors.HighlightText;
			public static readonly Color Border = Color.Gray;
			public static readonly Color GroupBoxBorder = ColorTranslator.FromHtml("#d3d3d3");
			public static readonly Color TreeViewForeground = SystemColors.WindowText;
			public static readonly Color TreeViewBackground = SystemColors.Window;
			public static readonly Color Button = ColorTranslator.FromHtml("#e1e1e1");
			public static readonly Color ButtonText = SystemColors.ControlText;
			public static readonly Color ButtonDisabled = ColorTranslator.FromHtml("#cccccc");
			public static readonly Color ButtonDisabledText = ColorTranslator.FromHtml("#a0a0a0");
			public static readonly Color ButtonHover = ColorTranslator.FromHtml("#cacaca");
			public static readonly Color ButtonPressed = ColorTranslator.FromHtml("#d8d8d8");
			public static readonly Color ImageButton = Color.WhiteSmoke;
			public static readonly Color ButtonBorder = ColorTranslator.FromHtml("#adadad");
			public static readonly Color ButtonDisabledBorder = ColorTranslator.FromHtml("#bfbfbf");
			public static readonly Color ImageButtonHover = Color.White;
			public static readonly Color ImageButtonPressed = Color.White;
			public static readonly Color WarningRed = Color.Red;
			public static readonly Color HighlightBorder = SystemColors.Highlight;
			public static readonly Color GrayText = SystemColors.GrayText;
			public static readonly Color SeletedTabButtonLight = Color.FromArgb(242, 242, 242);
			public static readonly Color SeletedTabButtonDark = Color.FromArgb(207, 207, 207);
			public static readonly Color TabInactiveText = SystemColors.GrayText;
			public static readonly Color TabBorder = Color.Silver;
			public static readonly Color TabEdgeBorder = SystemColors.ControlDark;
			public static readonly Color SelectedTabBorder = SystemColors.ControlDark;
			public static readonly Color RecipeListBackground = Color.Gray;
			public static readonly Color RecipeListGradient = Color.DarkGray;
			public static readonly Color OutputForeground = Color.Beige;
			public static readonly Color OutputBackground = Color.FromArgb(64, 64, 64);
			public static readonly Color NotesForeground = Color.Black;
			public static readonly Color NotesBackground = Color.FromArgb(255, 255, 248);

			// Text
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

		public class ThemeColors : IColorTheme
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

			// Icons
			public Image MenuIcon => Resources.menu;
			public Image MenuEditIcon => Resources.menu_edit;
			public Image MenuFolder => Resources.folder;
			public Image MenuRedDot => Resources.red_dot;
			public Image MenuSnippet => Resources.snippet_small;
			public Image MenuLore => Resources.lore_small;
		
			public Image ButtonModel => Resources.model;
			public Image ButtonCharacter => Resources.persona;
			public Image ButtonTraits => Resources.characteristic;
			public Image ButtonMind => Resources.personality;
			public Image ButtonStory => Resources.story;
			public Image ButtonComponents => Resources.component;
			public Image ButtonSnippets => Resources.snippet;
			public Image ButtonLore => Resources.lore;
			public Image Checker => Resources.checker;
			public Image Write => Resources.write;
			public Image MoveLoreUp => Resources.lore_up;
			public Image MoveLoreDown => Resources.lore_down;
			public Image RemoveLore => Resources.delete;
			public Image ArrowLeft => Resources.arrow_left;
			public Image ArrowRight => Resources.arrow_right;
		}

	}
}
