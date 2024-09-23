using Ginger.Properties;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public static class VisualTheme
	{
		public static bool DarkModeEnabled { get { return AppSettings.Settings.DarkTheme; } }

		public static ToolStripRenderer CreateToolStripRenderer()
		{
			if (DarkModeEnabled)
				return new ToolStripProfessionalRenderer(new DarkToolStripColorTable());
			else
				return new ToolStripProfessionalRenderer();
		}

		public static void ApplyTheme(Form form)
		{
			form.BackColor = Theme.ControlBackground;
			form.ForeColor = Theme.ControlForeground;

			var menuStrips = form.FindAllControlsOfType<MenuStrip>();
			foreach (var menuStrip in menuStrips)
				ApplyTheme(menuStrip);

			var textBoxes = form.FindAllControlsOfType<TextBoxBase>();
			foreach (var control in textBoxes)
			{
				control.ForeColor = Theme.TextBoxForeground;
				control.BackColor = Theme.TextBoxBackground;
			}

			var comboBoxes = form.FindAllControlsOfType<ComboBox>();
			foreach (var control in comboBoxes)
			{
				control.ForeColor = Theme.MenuForeground;
				control.BackColor = Theme.TextBoxBackground;
			}

			var buttons = form.FindAllControlsOfType<Button>();
			foreach (var button in buttons)
			{
				ApplyVisualStyle(button);
			}

		}

		public static void ApplyTheme(Control parentControl)
		{
			var groupBoxes = parentControl.FindAllControlsOfType<GroupBox>();
			foreach (var control in groupBoxes)
			{
				control.ForeColor = Theme.ControlForeground;
				control.BackColor = Theme.ControlBackground;
			}

			var textBoxes = parentControl.FindAllControlsOfType<TextBoxBase>();
			foreach (var control in textBoxes)
			{
				control.ForeColor = Theme.TextBoxForeground;
				control.BackColor = Theme.TextBoxBackground;
			}

			var comboBoxes = parentControl.FindAllControlsOfType<ComboBox>();
			foreach (var control in comboBoxes)
			{
				control.ForeColor = Theme.MenuForeground;
				control.BackColor = Theme.TextBoxBackground;
			}

			var buttons = parentControl.FindAllControlsOfType<Button>();
			foreach (var button in buttons)
				ApplyVisualStyle(button);

			foreach (Control control in parentControl.Controls)
			{
				control.ForeColor = Theme.ControlForeground;
				control.BackColor = Theme.ControlBackground;
			}
		}
		
		public static void ApplyVisualStyle(Button button)
		{
			button.FlatAppearance.BorderColor = Theme.ButtonBorder;
			button.FlatAppearance.MouseDownBackColor = Theme.ButtonPressed;
			button.FlatAppearance.MouseOverBackColor = Theme.ButtonHover;
			button.FlatAppearance.BorderSize = 1;
			button.ForeColor = Theme.ControlForeground;
			button.BackColor = Theme.Button;
		}

		public static void ApplyTheme(ToolStripMenuItem menuItem)
		{
			menuItem.ForeColor = Theme.MenuForeground;
			if (menuItem.DropDownItems != null)
			{
				foreach (ToolStripItem item in menuItem.DropDownItems)
				{
					if (item is ToolStripMenuItem)
						ApplyTheme(item as ToolStripMenuItem);
				}
			}
		}

		
		public static void ApplyTheme(ContextMenuStrip contextMenu)
		{
			contextMenu.Renderer = CreateToolStripRenderer();
			contextMenu.ForeColor = Theme.MenuForeground;
			foreach (ToolStripItem menuItem in contextMenu.Items)
			{
				if (menuItem is ToolStripMenuItem)
					ApplyTheme(menuItem as ToolStripMenuItem);
			}
		}


		public static void ApplyTheme(MenuStrip menuStrip)
		{
			menuStrip.Renderer = CreateToolStripRenderer();
			menuStrip.ForeColor = Theme.MenuForeground;
			foreach (ToolStripItem menuItem in menuStrip.Items)
			{
				if (menuItem is ToolStripMenuItem)
					ApplyTheme(menuItem as ToolStripMenuItem);
			}
		}

		public static IColorTheme Theme { get { return DarkModeEnabled ? _darkTheme : _lightTheme; } }

		public class DarkToolStripColorTable : ProfessionalColorTable
		{
			public override Color ToolStripDropDownBackground => Theme.MenuBackground;

			public override Color MenuStripGradientBegin => Theme.MenuBackground;
			public override Color MenuStripGradientEnd => Theme.MenuBackground;

			public override Color ImageMarginGradientBegin => Theme.MenuGradientBegin;
			public override Color ImageMarginGradientMiddle => Theme.MenuGradientMiddle;
			public override Color ImageMarginGradientEnd => Theme.MenuGradientEnd;

			public override Color MenuBorder => Theme.MenuBorder;
			public override Color MenuItemBorder => Theme.Highlight;

			public override Color MenuItemPressedGradientBegin => Theme.MenuBackground;
			public override Color MenuItemPressedGradientEnd => Theme.MenuBackground;

			/*
			public override Color ButtonPressedBorder => Color.Fuchsia;
			public override Color ButtonPressedHighlight => Color.Green;
			public override Color ButtonPressedHighlightBorder => Color.Red;

			public override Color RaftingContainerGradientBegin => Color.Orange;
			public override Color OverflowButtonGradientBegin => Color.Beige;
			public override Color ToolStripBorder => Color.BurlyWood;
			public override Color ButtonCheckedGradientBegin => Color.Yellow;
			public override Color ButtonPressedGradientBegin => Color.Fuchsia;

			public override Color GripDark => Color.DarkBlue;
			public override Color CheckPressedBackground => Color.Purple;

			public override Color ButtonCheckedHighlight => Color.Green;
			public override Color ButtonSelectedHighlightBorder => Color.Plum;
			public override Color ToolStripPanelGradientBegin => Color.Green;
			public override Color ToolStripGradientBegin => Color.Pink;
			


			public override Color ButtonSelectedGradientBegin => Color.Yellow;
			public override Color ButtonSelectedGradientMiddle => Color.Yellow;
			public override Color ButtonSelectedGradientEnd => Color.Yellow;
			*/

			public override Color ButtonSelectedBorder => Theme.Highlight;

			public override Color SeparatorLight => Theme.MenuSeparator;
			public override Color SeparatorDark => Theme.MenuSeparator;

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
		public Color ControlBackground => SystemColors.Control;
		public Color ControlForeground => SystemColors.ControlText;
		
		public Color TextBoxBackground => SystemColors.Window;
		public Color TextBoxForeground => SystemColors.WindowText;
		public Color TextBoxBorder => Color.Gray;
		public Color TextBoxPlaceholder => Color.FromArgb(144, 144, 144);
		
		public Color MenuBackground => SystemColors.MenuBar;
		public Color MenuForeground => SystemColors.MenuText;
		public Color MenuBorder => SystemColors.ControlDark;

		public Color MenuGradientBegin => SystemColors.MenuBar;
		public Color MenuGradientMiddle => SystemColors.MenuBar;
		public Color MenuGradientEnd => SystemColors.MenuBar;
		public Color MenuSeparator => SystemColors.ControlDark;

		public Color Border => Color.Gray;
		public Color GroupBoxBorder => ColorTranslator.FromHtml("#d3d3d3");

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
	}

	public class DarkTheme : IColorTheme
	{
		public Color ControlBackground => ColorTranslator.FromHtml("#404040");
		public Color ControlForeground => ColorTranslator.FromHtml("#f0f0f0");

		public Color TextBoxBackground => ColorTranslator.FromHtml("#202020");
		public Color TextBoxForeground => Color.White;
		public Color TextBoxBorder => Color.Gray;
		public Color TextBoxPlaceholder => Color.FromArgb(180, 180, 180);

		public Color MenuBackground => ColorTranslator.FromHtml("#404040");
		public Color MenuForeground => Color.White;
		public Color MenuBorder => ColorTranslator.FromHtml("#202020");
		public Color MenuGradientBegin =>  ColorTranslator.FromHtml("#404040");
		public Color MenuGradientMiddle =>  ColorTranslator.FromHtml("#3c3c3c");
		public Color MenuGradientEnd => ColorTranslator.FromHtml("#343434");
		public Color MenuSeparator => ColorTranslator.FromHtml("#303030");

		public Color Button => ColorTranslator.FromHtml("#363636");
		public Color ButtonHover => ColorTranslator.FromHtml("#2d2d2d");
		public Color ButtonPressed => ColorTranslator.FromHtml("#2d2d2d");
		public Color ButtonBorder => Color.Gray;

		public Color Border => Color.Gray;
		public Color GroupBoxBorder => ColorTranslator.FromHtml("#606060");

		public Color WarningRed => ColorTranslator.FromHtml("#ff4040");
		public Color Highlight => SystemColors.Highlight;
		public Color GrayText => SystemColors.GrayText;

		public Color TabInactiveText => ColorTranslator.FromHtml("#c0c0c0");
		public Color TabBorder => ColorTranslator.FromHtml("#808080");
		public Color TabEdgeBorder => ColorTranslator.FromHtml("#606060");
		public Color SelectedTabBorder => ColorTranslator.FromHtml("#c0c0c0");

		public Color SeletedTabButtonLight => ColorTranslator.FromHtml("#808080");
		public Color SeletedTabButtonDark => ColorTranslator.FromHtml("#606060");

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
	}

	public interface IVisualThemed
	{
		void ApplyVisualTheme();
	}

	public interface IColorTheme
	{
		Color ControlForeground { get; }
		Color ControlBackground { get; }

		Color TextBoxBackground { get; }
		Color TextBoxForeground { get; }
		Color TextBoxBorder { get; }
		Color TextBoxPlaceholder { get; }

		Color MenuBackground { get; }
		Color MenuForeground { get; }
		Color MenuBorder { get; }
		Color MenuGradientBegin { get; }
		Color MenuGradientMiddle { get; }
		Color MenuGradientEnd { get; }
		Color MenuSeparator { get; }

		Color Border { get; }
		Color GroupBoxBorder { get; }
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

		
	}
}
