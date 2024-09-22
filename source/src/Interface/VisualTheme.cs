using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public static class VisualTheme
	{
		public static bool DarkModeEnabled { get { return AppSettings.Settings.DarkTheme; } }

		public static ContextMenuStrip CreateContextMenuStrip()
		{
			ContextMenuStrip menu = new ContextMenuStrip();
			if (DarkModeEnabled)
			{
				menu.Renderer = new ToolStripProfessionalRenderer(new DarkToolStripColorTable());
				menu.ForeColor = Theme.ControlForeground;
			}
			return menu;
		}

		public static ToolStripRenderer CreateToolStripRenderer()
		{
			if (DarkModeEnabled)
				return new ToolStripProfessionalRenderer(new DarkToolStripColorTable());
			else
				return new ToolStripProfessionalRenderer();
		}

		public static IColorTheme Theme { get { return DarkModeEnabled ? _darkTheme : _lightTheme; } }

		public class DarkToolStripColorTable : ProfessionalColorTable
		{
			public override Color ToolStripDropDownBackground
			{
				get
				{
					return Theme.MenuBackground;
				}
			}

			public override Color ImageMarginGradientBegin
			{
				get
				{
					return Theme.MenuBackground;
				}
			}

			public override Color ImageMarginGradientMiddle
			{
				get
				{
					return Theme.MenuBackground;
				}
			}

			public override Color ImageMarginGradientEnd
			{
				get
				{
					return Theme.MenuBackground;
				}
			}

			public override Color MenuBorder
			{
				get
				{
					return Color.Black;
				}
			}

			public override Color MenuItemBorder
			{
				get
				{
					return Color.Black;
				}
			}

			public override Color MenuItemSelected
			{
				get
				{
					return Color.Navy;
				}
			}

			public override Color MenuStripGradientBegin
			{
				get
				{
					return Theme.MenuBackground;
				}
			}

			public override Color MenuStripGradientEnd
			{
				get
				{
					return Theme.MenuBackground;
				}
			}
			/*
			public override Color MenuItemSelectedGradientBegin
			{
				get
				{
					return Color.Navy;
				}
			}

			public override Color MenuItemSelectedGradientEnd
			{
				get
				{
					return Color.Navy;
				}
			}

			public override Color MenuItemPressedGradientBegin
			{
				get
				{
					return Color.Blue;
				}
			}

			public override Color MenuItemPressedGradientEnd
			{
				get
				{
					return Color.Blue;
				}
			}
			*/
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
		
		public Color MenuBackground => SystemColors.MenuBar;
		public Color MenuForeground => SystemColors.MenuText;
		public Color MenuBorder => SystemColors.ActiveBorder;
	}

	public class DarkTheme : IColorTheme
	{
		public Color ControlBackground => ColorTranslator.FromHtml("#404040");
		public Color ControlForeground => Color.Silver;

		public Color TextBoxBackground => ColorTranslator.FromHtml("#202020");
		public Color TextBoxForeground => Color.White;

		public Color MenuBackground => ColorTranslator.FromHtml("#404040");
		public Color MenuForeground => Color.White;
		public Color MenuBorder => Color.Gray;
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

		Color MenuBackground { get; }
		Color MenuForeground { get; }
		Color MenuBorder { get; }
	}
}
