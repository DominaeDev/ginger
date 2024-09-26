using System;
using System.Drawing;
using System.Windows.Forms;
using WinFormsSyntaxHighlighter;

namespace Ginger
{
	public partial class SnippetPanel : UserControl
	{
		public string CurrentText { get { return _bSwapped ? SwappedText : OriginalText; } }
		public string OriginalText { get; set; }
		public string SwappedText { get; set; }
		private bool _bSwapped = false;

		private bool _bIgnoreEvents = false;
		public Recipe.Component _channel = Recipe.Component.Invalid;

		public bool isEnabled { get { return textBox_Text.Enabled; } }

		public SnippetPanel()
		{
			InitializeComponent();

			textBox_Text.richTextBox.syntaxFlags = RichTextBoxEx.SyntaxFlags.Default & ~RichTextBoxEx.SyntaxFlags.Names;
			textBox_Text.HighlightBorder = false;
			textBox_Text.BorderColor = Color.White;
			textBox_Text.TextChanged += TextBox_Text_TextChanged;
			textBox_Text.ForeColor = Theme.Current.TextBoxForeground;
			textBox_Text.BackColor = Theme.Current.TextBoxBackground;

			this.FontChanged += FontDidChange;
			this.Load += SnippetPanel_Load;
		}

		private void SnippetPanel_Load(object sender, EventArgs e)
		{
			textBox_Text.ApplyVisualTheme();
		}

		private void TextBox_Text_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			if (_bSwapped)
				SwappedText = textBox_Text.Text;
			else
				OriginalText = textBox_Text.Text;
		}

		public void SetText(string text, string swappedText, Recipe.Component channel)
		{
			OriginalText = text;
			SwappedText = swappedText;

			_channel = channel;

			switch (channel)
			{
			case Recipe.Component.System:
				labelChannel.Text = "Model instructions"; 
				break;
			case Recipe.Component.System_PostHistory:
				labelChannel.Text = "Model instructions (Important)"; 
				break;
			case Recipe.Component.Persona:
				labelChannel.Text = "Persona";
				break;
			case Recipe.Component.Scenario:
				labelChannel.Text = "Scenario";
				break;
			case Recipe.Component.Greeting:
				labelChannel.Text = "Greeting";
				break;
			case Recipe.Component.Greeting_Group:
				labelChannel.Text = "Greeting (Group)";
				break;
			case Recipe.Component.Example:
				labelChannel.Text = "Example";
				break;
			case Recipe.Component.Grammar:
				labelChannel.Text = "Grammar";
				textBox_Text.SpellChecking = false;
				textBox_Text.SyntaxHighlighting = false;
				break;
			case Recipe.Component.UserPersona:
				labelChannel.Text = "User persona";
				break;
			default:
				labelChannel.Text = "Text";
				break;
			}

			RefreshPanelColor();
		}

		public void SetSwapped(bool bSwapped)
		{
			_bSwapped = bSwapped;

			_bIgnoreEvents = true;
			textBox_Text.richTextBox.DisableThenDoThenEnable(() => {
				textBox_Text.SetTextSilent(bSwapped ? SwappedText : OriginalText);
				textBox_Text.richTextBox.SpellCheck(true, false);
				textBox_Text.richTextBox.RefreshSyntaxHighlight(true);
				textBox_Text.InitUndo();
			});
			_bIgnoreEvents = false;
		}

		private void FontDidChange(object sender, EventArgs e)
		{
			_bIgnoreEvents = true;
			textBox_Text.Font = this.Font;
			_bIgnoreEvents = false;
		}

		private void Btn_Remove_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				textBox_Text.Enabled = !textBox_Text.Enabled;
				btn_Remove.Image = textBox_Text.Enabled ? Properties.Resources.delete_small : Properties.Resources.delete_strike;

				RefreshPanelColor();
			}
		}

		private void RefreshPanelColor()
		{
			Color color;
			if (textBox_Text.Enabled)
			{
				switch (_channel)
				{
				case Recipe.Component.System:
					color = Constants.RecipeColorByCategory[Recipe.Category.Model];
					break;
				case Recipe.Component.System_PostHistory:
					color = Constants.RecipeColorByCategory[Recipe.Category.Model];
					break;
				case Recipe.Component.Persona:
					color = Constants.RecipeColorByCategory[Recipe.Category.Character];
					break;
				case Recipe.Component.Scenario:
					color = Constants.RecipeColorByCategory[Recipe.Category.Story];
					break;
				case Recipe.Component.Greeting:
					color = Constants.RecipeColorByCategory[Recipe.Category.Chat];
					break;
				case Recipe.Component.Greeting_Group:
					color = Constants.RecipeColorByCategory[Recipe.Category.Chat];
					break;
				case Recipe.Component.Example:
					color = Constants.RecipeColorByCategory[Recipe.Category.Chat];
					break;
				case Recipe.Component.Grammar:
					color = Constants.RecipeColorByCategory[Recipe.Category.Model];
					break;
				case Recipe.Component.UserPersona:
					color = Color.Azure;
					break;
				default:
					color = Constants.RecipeColorByCategory[Recipe.Category.Undefined];
					break;
				}
			}
			else
			{
				color = Constants.RecipeColorByCategory[Recipe.Category.Undefined];
			}

			if (Theme.IsDarkModeEnabled)
			{
				color = Utility.GetDarkColor(color, 0.60f);
			}

			BackColor = color;

			if (textBox_Text.Enabled)
				labelChannel.ForeColor = Utility.GetContrastColor(BackColor, false);
			else
			{
				if (Theme.IsDarkModeEnabled)
					labelChannel.ForeColor = Color.Black;
				else
					labelChannel.ForeColor = Color.Gray;
			}
		}
	}
}
