using System.Drawing;

namespace Ginger
{
	public interface IColorTheme
	{
		// Interface
		Color ControlForeground { get; }
		Color ControlBackground { get; }
		Color TextBoxForeground { get; }
		Color TextBoxBackground { get; }
		Color TextBoxBorder { get; }
		Color TextBoxPlaceholder { get; }
		Color TextBoxDisabledBorder { get; }
		Color TextBoxDisabledBackground { get; }
		Color MenuBackground { get; }
		Color MenuForeground { get; }
		Color MenuBorder { get; }
		Color MenuGradientBegin { get; }
		Color MenuGradientMiddle { get; }
		Color MenuGradientEnd { get; }
		Color MenuSeparator { get; }
		Color MenuHighlight { get; }
		Color Border { get; }
		Color GroupBoxBorder { get; }
		Color TreeViewForeground { get; }
		Color TreeViewBackground { get; }
		Color WarningRed { get; }
		Color Highlight { get; }
		Color GrayText { get; }
		Color SeletedTabButtonLight { get; }
		Color SeletedTabButtonDark{ get; }
		Color TabBorder { get; }
		Color TabInactiveText { get; }
		Color TabEdgeBorder { get; }
		Color SelectedTabBorder { get; }
		Color Button { get; }
		Color ButtonBorder { get; }
		Color ButtonHover { get; }
		Color ButtonPressed { get; }
		Color RecipeListBackground { get; }
		Color RecipeListGradient { get; }
		Color OutputForeground { get; }
		Color OutputBackground { get; }
		Color NotesForeground { get; }
		Color NotesBackground { get; }

		// Text
		Color Dialogue	 { get; }
		Color Narration	 { get; }
		Color Number	 { get; }
		Color Name		 { get; }
		Color Command	 { get; }
		Color Pronoun	 { get; }
		Color Comment	 { get; }
		Color Code		 { get; }
		Color Error		 { get; }
		Color Wildcard	 { get; }
		Color Decorator	 { get; }

		// Icons
		Image MenuIcon { get; }
		Image MenuEditIcon { get; }
		Image MenuFolder { get; }
		Image MenuRedDot { get; }
		Image MenuSnippet { get; }
		Image MenuLore { get; }
		Image ButtonModel { get; }
		Image ButtonCharacter { get; }
		Image ButtonTraits { get; }
		Image ButtonMind { get; }
		Image ButtonStory { get; }
		Image ButtonComponents { get; }
		Image ButtonSnippets { get; }
		Image ButtonLore { get; }
		Image Checker { get; }
		Image Write { get; }
		Image MoveLoreUp { get; }
		Image MoveLoreDown { get; }
		Image RemoveLore { get; }
		Image ArrowLeft { get; }
		Image ArrowRight { get; }
	}
}
