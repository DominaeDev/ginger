using System.Drawing;
using System.Windows.Forms;

using DarkMode = Dark.Net.DarkNet;

namespace Ginger
{
	public static class Theme
	{
		public static bool IsDarkModeEnabled { get { return AppSettings.Settings.DarkTheme; } }

		public static IVisualTheme Current { get { return IsDarkModeEnabled ? _darkTheme : _lightTheme; } }
		public static IVisualTheme Light { get { return _lightTheme; } }
		public static IVisualTheme Dark { get { return _darkTheme; } }

		private static IVisualTheme _lightTheme = new LightTheme.ThemeColors();
		private static IVisualTheme _darkTheme = new DarkTheme.ThemeColors();

		public static bool IsTheming { get { return _isTheming > 0; } }
		private static int _isTheming = 0;

		public static void BeginTheming()
		{
			++_isTheming;
		}
		public static void EndTheming()
		{
			--_isTheming;
		}

		public static void Apply(Form form)
		{
			if (Utility.InDesignMode)
				return;
			
			BeginTheming();

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
				Apply(control);

			var comboBoxes = form.FindAllControlsOfType<ComboBox>();
			foreach (var control in comboBoxes)
				Apply(control);

			var buttons = form.FindAllControlsOfType<Button>();
			foreach (var button in buttons)
				Apply(button);

			ApplyToTitleBar(form, false);
			
			EndTheming();
		}

		public static void Apply(TextBoxBase control)
		{
			BeginTheming();

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

			EndTheming();
		}

		public static void Apply(ComboBox control)
		{
			BeginTheming();

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

			EndTheming();
		}

		public static void Apply(Control parentControl)
		{
			BeginTheming();

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

			EndTheming();
		}

		public static void Apply(Button button)
		{
			BeginTheming();

			button.FlatStyle = FlatStyle.Flat;
			button.FlatAppearance.BorderColor = Current.ButtonBorder;
			button.FlatAppearance.MouseDownBackColor = Current.ImageButtonPressed;
			button.FlatAppearance.MouseOverBackColor = Current.ImageButtonHover;
			button.FlatAppearance.BorderSize = 1;
			button.ForeColor = Current.ButtonText;
			button.BackColor = Current.ImageButton;

			EndTheming();
		}

		public static void Apply(ToolStripMenuItem menuItem)
		{
			BeginTheming();

			menuItem.ForeColor = Current.MenuForeground;
			if (menuItem.DropDownItems != null)
			{
				foreach (ToolStripItem item in menuItem.DropDownItems)
				{
					if (item is ToolStripMenuItem)
						Apply(item as ToolStripMenuItem);
				}
			}

			EndTheming();
		}

		public static void Apply(ContextMenuStrip contextMenu)
		{
			BeginTheming();

			contextMenu.Renderer = CreateToolStripRenderer();
			contextMenu.ForeColor = Current.MenuForeground;
			foreach (ToolStripItem menuItem in contextMenu.Items)
			{
				if (menuItem is ToolStripMenuItem)
					Apply(menuItem as ToolStripMenuItem);
			}
			EndTheming();
		}

		public static void Apply(MenuStrip menuStrip)
		{
			BeginTheming();

			menuStrip.Renderer = CreateToolStripRenderer();
			menuStrip.ForeColor = Current.MenuForeground;
			foreach (ToolStripItem menuItem in menuStrip.Items)
			{
				if (menuItem is ToolStripMenuItem)
					Apply(menuItem as ToolStripMenuItem);
			}

			EndTheming();
		}

		public static void Apply(DataGridView dataGridView)
		{
			BeginTheming();

			if (IsDarkModeEnabled)
			{
				dataGridView.DefaultCellStyle = new DataGridViewCellStyle() {
					BackColor = Dark.TextBoxBackground,
					ForeColor = Dark.TextBoxForeground,
					SelectionBackColor = Dark.Highlight,
					SelectionForeColor = Dark.HighlightText,
				};
				
				dataGridView.GridColor = Dark.TextBoxBorder;
			}
			else
			{
				dataGridView.DefaultCellStyle = new DataGridViewCellStyle();
				dataGridView.GridColor = SystemColors.ControlDark;
			}
			dataGridView.ForeColor = Current.ControlForeground;
			dataGridView.BackgroundColor = Current.ControlBackground;

			EndTheming();
		}

		private static ToolStripRenderer CreateToolStripRenderer()
		{
			if (IsDarkModeEnabled)
				return new ToolStripProfessionalRenderer(new DarkTheme.ToolStripColorTable());
			else
				return new ToolStripProfessionalRenderer();
		}

		public static void ApplyToTitleBar(Form form, bool bSetForProcess = false)
		{
			if (Utility.InDesignMode)
				return; // Ignore 

			try
			{
				var theme = IsDarkModeEnabled ? global::Dark.Net.Theme.Dark : global::Dark.Net.Theme.Auto;
				if (bSetForProcess)
					DarkMode.Instance.SetCurrentProcessTheme(theme);

				DarkMode.Instance.SetWindowThemeForms(form, theme);
			}
			catch
			{
				// Do nothing
			}
		}
	}

	public interface IVisualThemed
	{
		void ApplyVisualTheme();
	}
}
