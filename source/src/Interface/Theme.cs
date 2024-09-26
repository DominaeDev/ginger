using Ginger.Properties;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public static class Theme
	{
		public static bool IsDarkModeEnabled { get { return AppSettings.Settings.DarkTheme; } }

		public static void Apply(Form form)
		{
			if (Utility.InDesignMode)
				return;

			form.BackColor = Current.ControlBackground;
			form.ForeColor = Current.ControlForeground;

			var menuStrips = form.FindAllControlsOfType<MenuStrip>();
			foreach (var menuStrip in menuStrips)
				Apply(menuStrip);

			var statusStrips = form.FindAllControlsOfType<StatusStrip>();
			foreach (var statusStrip in statusStrips)
			{
				statusStrip.BackColor = Current.ControlBackground;
				statusStrip.ForeColor = Current.ControlForeground;
			}

			var textBoxes = form.FindAllControlsOfType<TextBoxBase>();
			foreach (var control in textBoxes)
			{
				if (control.Enabled)
				{
					control.ForeColor = Current.TextBoxForeground;
					control.BackColor = Current.TextBoxBackground;
				}
				else
				{
					control.ForeColor = Current.GrayText;
					control.BackColor = Current.TextBoxDisabledBackground;
				}
			}

			var comboBoxes = form.FindAllControlsOfType<ComboBox>();
			foreach (var control in comboBoxes)
			{
				if (control.Enabled)
				{
					control.ForeColor = Current.TextBoxForeground;
					control.BackColor = Current.TextBoxBackground;
				}
				else
				{
					control.ForeColor = Current.GrayText;
					control.BackColor = Current.TextBoxDisabledBackground;
				}
			}

			var buttons = form.FindAllControlsOfType<Button>();
			foreach (var button in buttons)
				Apply(button);

			global::Dark.Net.DarkNet.Instance.SetWindowThemeForms(form, IsDarkModeEnabled ? global::Dark.Net.Theme.Dark : global::Dark.Net.Theme.Light);
		}

		public static void Apply(Control parentControl)
		{
			var groupBoxes = parentControl.FindAllControlsOfType<GroupBox>();
			foreach (var control in groupBoxes)
			{
				control.ForeColor = Current.ControlForeground;
				control.BackColor = Current.ControlBackground;
			}

			var textBoxes = parentControl.FindAllControlsOfType<TextBoxBase>();
			foreach (var control in textBoxes)
			{
				if (control.Enabled)
				{
					control.ForeColor = Current.TextBoxForeground;
					control.BackColor = Current.TextBoxBackground;
				}
				else
				{
					control.ForeColor = Current.GrayText;
					control.BackColor = Current.TextBoxDisabledBackground;
				}
			}

			var comboBoxes = parentControl.FindAllControlsOfType<ComboBox>();
			foreach (var control in comboBoxes)
			{
				if (control.Enabled)
				{
					control.ForeColor = Current.TextBoxForeground;
					control.BackColor = Current.TextBoxBackground;
				}
				else
				{
					control.ForeColor = Current.GrayText;
					control.BackColor = Current.TextBoxDisabledBackground;
				}
			}

			var buttons = parentControl.FindAllControlsOfType<Button>();
			foreach (var button in buttons)
				Apply(button);

			foreach (Control control in parentControl.Controls)
			{
				control.ForeColor = Current.ControlForeground;
				control.BackColor = Current.ControlBackground;
			}
		}
		
		public static void Apply(Button button)
		{
			button.FlatStyle = FlatStyle.Flat;
			button.FlatAppearance.BorderColor = Current.ButtonBorder;
			button.FlatAppearance.MouseDownBackColor = Current.ButtonPressed;
			button.FlatAppearance.MouseOverBackColor = Current.ButtonHover;
			button.FlatAppearance.BorderSize = 1;
			button.ForeColor = Current.ControlForeground;
			button.BackColor = Current.Button;
		}

		public static void Apply(ToolStripMenuItem menuItem)
		{
			menuItem.ForeColor = Current.MenuForeground;
			if (menuItem.DropDownItems != null)
			{
				foreach (ToolStripItem item in menuItem.DropDownItems)
				{
					if (item is ToolStripMenuItem)
						Apply(item as ToolStripMenuItem);
				}
			}
		}

		public static void Apply(ContextMenuStrip contextMenu)
		{
			contextMenu.Renderer = CreateToolStripRenderer();
			contextMenu.ForeColor = Current.MenuForeground;
			foreach (ToolStripItem menuItem in contextMenu.Items)
			{
				if (menuItem is ToolStripMenuItem)
					Apply(menuItem as ToolStripMenuItem);
			}
		}

		public static void Apply(MenuStrip menuStrip)
		{
			menuStrip.Renderer = CreateToolStripRenderer();
			menuStrip.ForeColor = Current.MenuForeground;
			foreach (ToolStripItem menuItem in menuStrip.Items)
			{
				if (menuItem is ToolStripMenuItem)
					Apply(menuItem as ToolStripMenuItem);
			}
		}

		private static ToolStripRenderer CreateToolStripRenderer()
		{
			if (IsDarkModeEnabled)
				return new ToolStripProfessionalRenderer(new DarkToolStripColorTable());
			else
				return new ToolStripProfessionalRenderer();
		}

		public static IColorTheme Current { get { return IsDarkModeEnabled ? _darkTheme : _lightTheme; } }
		public static IColorTheme Light { get { return _lightTheme; } }
		public static IColorTheme Dark { get { return _darkTheme; } }

		public class DarkToolStripColorTable : ProfessionalColorTable
		{
			public override Color ToolStripDropDownBackground => Current.MenuBackground;

			public override Color MenuStripGradientBegin => Current.MenuBackground;
			public override Color MenuStripGradientEnd => Current.MenuBackground;

			public override Color ImageMarginGradientBegin => Current.MenuGradientBegin;
			public override Color ImageMarginGradientMiddle => Current.MenuGradientMiddle;
			public override Color ImageMarginGradientEnd => Current.MenuGradientEnd;

			public override Color MenuBorder => Current.MenuBorder;
			public override Color MenuItemBorder => Current.MenuHighlight;

			public override Color MenuItemPressedGradientBegin => Current.MenuBackground;
			public override Color MenuItemPressedGradientEnd => Current.MenuBackground;

			public override Color ButtonSelectedBorder => Current.MenuHighlight;

			public override Color SeparatorLight => Current.MenuSeparator;
			public override Color SeparatorDark => Current.MenuSeparator;

			public static Color SelectedBackground = Color.FromArgb(48, 78, 102);
			public static Color HighlightBackground = Color.FromArgb(32, 92, 140);

			public override Color MenuItemSelected => SelectedBackground;
			public override Color ButtonSelectedHighlight => SelectedBackground;

			public override Color CheckBackground => HighlightBackground;
			public override Color CheckSelectedBackground => HighlightBackground;
			public override Color MenuItemSelectedGradientBegin => HighlightBackground;
			public override Color MenuItemSelectedGradientEnd => HighlightBackground;
		}

		private static IColorTheme _lightTheme = new LightTheme();
		private static IColorTheme _darkTheme = new DarkTheme();
	}

	public class LightTheme : IColorTheme
	{
		// Interface
		public Color ControlBackground => SystemColors.Control;
		public Color ControlForeground => SystemColors.ControlText;
		
		public Color TextBoxBackground => SystemColors.Window;
		public Color TextBoxForeground => SystemColors.WindowText;
		public Color TextBoxBorder => Color.Gray;
		public Color TextBoxPlaceholder => Color.FromArgb(144, 144, 144);
		public Color TextBoxDisabledBackground => SystemColors.Control;
		public Color TextBoxDisabledBorder => SystemColors.InactiveBorder;

		public Color MenuBackground => SystemColors.MenuBar;
		public Color MenuForeground => SystemColors.MenuText;
		public Color MenuBorder => SystemColors.ControlDark;

		public Color MenuGradientBegin => SystemColors.MenuBar;
		public Color MenuGradientMiddle => SystemColors.MenuBar;
		public Color MenuGradientEnd => SystemColors.MenuBar;
		public Color MenuSeparator => SystemColors.ControlDark;
		public Color MenuHighlight => SystemColors.Highlight;

		public Color Border => Color.Gray;
		public Color GroupBoxBorder => ColorTranslator.FromHtml("#d3d3d3");
		public Color TreeViewForeground => SystemColors.WindowText;
		public Color TreeViewBackground => SystemColors.Window;

		public Color Button => Color.WhiteSmoke;
		public Color ButtonBorder => Color.Silver;
		public Color ButtonHover => Color.White;
		public Color ButtonPressed => Color.White;

		public Color WarningRed => Color.Red;
		public Color Highlight => SystemColors.Highlight;
		public Color GrayText => SystemColors.GrayText;

		public Color SeletedTabButtonLight => Color.FromArgb(242, 242, 242);
		public Color SeletedTabButtonDark => Color.FromArgb(207, 207, 207);
		public Color TabInactiveText => SystemColors.GrayText;
		public Color TabBorder => Color.Silver;
		public Color TabEdgeBorder => SystemColors.ControlDark;
		public Color SelectedTabBorder => SystemColors.ControlDark;

		public Color RecipeListBackground => Color.Gray;
		public Color RecipeListGradient => Color.DarkGray;

		public Color OutputForeground => Color.Beige;
		public Color OutputBackground => Color.FromArgb(64, 64, 64);

		public Color NotesForeground => Color.Black;
		public Color NotesBackground => Color.FromArgb(255, 255, 248);

		// Text
		public Color Dialogue	=> ColorTranslator.FromHtml("#C06000");
		public Color Narration	=> ColorTranslator.FromHtml("#406080");
		public Color Number		=> ColorTranslator.FromHtml("#800080");
		public Color Name		=> ColorTranslator.FromHtml("#0000C0");
		public Color Command	=> ColorTranslator.FromHtml("#800000");
		public Color Pronoun	=> ColorTranslator.FromHtml("#C000C0");
		public Color Comment	=> ColorTranslator.FromHtml("#969696");
		public Color Code		=> ColorTranslator.FromHtml("#606060");
		public Color Error		=> ColorTranslator.FromHtml("#C00000");
		public Color Wildcard	=> ColorTranslator.FromHtml("#008080");
		public Color Decorator	=> ColorTranslator.FromHtml("#800080");

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

	public class DarkTheme : IColorTheme
	{
		// Interface
		public Color ControlForeground => ColorTranslator.FromHtml("#f0f0f0");
		public Color ControlBackground => ColorTranslator.FromHtml("#404040");

		public Color TextBoxForeground => ColorTranslator.FromHtml("#DEDEDE");
		public Color TextBoxBackground => ColorTranslator.FromHtml("#262626");
		public Color TextBoxBorder => Color.Gray;
		public Color TextBoxPlaceholder => ColorTranslator.FromHtml("#606060");
		public Color TextBoxDisabledBackground => ColorTranslator.FromHtml("#363636");
		public Color TextBoxDisabledBorder => Color.Gray;

		public Color MenuForeground => Color.White;
		public Color MenuBackground => ColorTranslator.FromHtml("#404040");
		public Color MenuBorder => ColorTranslator.FromHtml("#202020");
		public Color MenuGradientBegin =>  ColorTranslator.FromHtml("#404040");
		public Color MenuGradientMiddle =>  ColorTranslator.FromHtml("#3c3c3c");
		public Color MenuGradientEnd => ColorTranslator.FromHtml("#343434");
		public Color MenuSeparator => ColorTranslator.FromHtml("#303030");
		public Color MenuHighlight => SystemColors.Highlight;

		public Color Button => ColorTranslator.FromHtml("#363636");
		public Color ButtonHover => ColorTranslator.FromHtml("#2d2d2d");
		public Color ButtonPressed => ColorTranslator.FromHtml("#2d2d2d");
		public Color ButtonBorder => Color.Gray;

		public Color Border => Color.Gray;
		public Color GroupBoxBorder => ColorTranslator.FromHtml("#606060");
		public Color TreeViewForeground =>TextBoxForeground;
		public Color TreeViewBackground => ColorTranslator.FromHtml("#303030");

		public Color WarningRed => ColorTranslator.FromHtml("#ff4040");
		public Color Highlight => Color.LightGray;
		public Color GrayText => SystemColors.GrayText;

		public Color TabInactiveText => ColorTranslator.FromHtml("#c0c0c0");
		public Color TabBorder => ColorTranslator.FromHtml("#808080");
		public Color TabEdgeBorder => ColorTranslator.FromHtml("#606060");
		public Color SelectedTabBorder => ColorTranslator.FromHtml("#c0c0c0");

		public Color SeletedTabButtonLight => ColorTranslator.FromHtml("#808080");
		public Color SeletedTabButtonDark => ColorTranslator.FromHtml("#606060");

		public Color RecipeListBackground => ColorTranslator.FromHtml("#282828");
		public Color RecipeListGradient => ColorTranslator.FromHtml("#3a3a3a");

		public Color OutputForeground => ColorTranslator.FromHtml("#f9eeb9");
		public Color OutputBackground => TextBoxBackground;

		public Color NotesForeground => TextBoxForeground;
		public Color NotesBackground => TextBoxBackground;

		// Text
		public Color Dialogue	=> ColorTranslator.FromHtml("#ffcd62");
		public Color Narration	=> ColorTranslator.FromHtml("#97a2ac");
		public Color Number		=> ColorTranslator.FromHtml("#BE7BE3");
		public Color Name		=> ColorTranslator.FromHtml("#4FB0FF");
		public Color Command	=> ColorTranslator.FromHtml("#c2814c");
		public Color Pronoun	=> ColorTranslator.FromHtml("#D000D0");
		public Color Comment	=> ColorTranslator.FromHtml("#636363");
		public Color Code		=> ColorTranslator.FromHtml("#969696");
		public Color Error		=> ColorTranslator.FromHtml("#ff5569");
		public Color Wildcard	=> ColorTranslator.FromHtml("#00A0A0");
		public Color Decorator	=> ColorTranslator.FromHtml("#A000A0");

		// Icons
		public Image MenuIcon => Resources.dark_menu;
		public Image MenuEditIcon => Resources.dark_menu_edit;
		public Image MenuFolder => Resources.dark_folder;
		public Image MenuRedDot => Resources.dark_red_dot;
		public Image MenuSnippet => Resources.dark_snippet_small;
		public Image MenuLore => Resources.dark_lore_small;

		public Image ButtonModel => Resources.dark_model;
		public Image ButtonCharacter => Resources.dark_persona;
		public Image ButtonTraits => Resources.dark_characteristic;
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
	}

	public interface IVisualThemed
	{
		void ApplyVisualTheme();
	}

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
