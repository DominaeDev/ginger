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
				return new ToolStripProfessionalRenderer(new DarkTheme.ToolStripColorTable());
			else
				return new ToolStripProfessionalRenderer();
		}

		public static IColorTheme Current { get { return IsDarkModeEnabled ? _darkTheme : _lightTheme; } }
		public static IColorTheme Light { get { return _lightTheme; } }
		public static IColorTheme Dark { get { return _darkTheme; } }

		private static IColorTheme _lightTheme = new LightTheme.Colors();
		private static IColorTheme _darkTheme = new DarkTheme.Colors();
	}

	public interface IVisualThemed
	{
		void ApplyVisualTheme();
	}
}
