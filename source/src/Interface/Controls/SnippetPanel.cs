using System;
using System.Drawing;
using System.Windows.Forms;
using WinFormsSyntaxHighlighter;

namespace Ginger
{
	public partial class SnippetPanel : UserControl
	{
		public Recipe.Component channel;

		public string CurrentText { get { return _bSwapped ? SwappedText : OriginalText; } }
		public string OriginalText { get; set; }
		public string SwappedText { get; set; }
		private bool _bSwapped = false;

		private bool _bIgnoreEvents = false;

		public bool isEnabled { get { return textBox_Text.Enabled; } }

		public SnippetPanel()
		{
			InitializeComponent();

			textBox_Text.richTextBox.syntaxFlags = RichTextBoxEx.SyntaxFlags.Default & ~RichTextBoxEx.SyntaxFlags.Names;
			textBox_Text.HighlightBorder = false;
			textBox_Text.BorderColor = Color.White;
			textBox_Text.TextChanged += TextBox_Text_TextChanged;
			this.FontChanged += FontDidChange;
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

			this.channel = channel;

			switch (channel)
			{
			case Recipe.Component.System:
				labelChannel.Text = "Model instructions"; 
				BackColor = Constants.RecipeColorByCategory[Recipe.Category.Model];
				break;
			case Recipe.Component.System_PostHistory:
				labelChannel.Text = "Model instructions (Important)"; 
				BackColor = Constants.RecipeColorByCategory[Recipe.Category.Model];
				break;
			case Recipe.Component.Persona:
				labelChannel.Text = "Persona";
				BackColor = Constants.RecipeColorByCategory[Recipe.Category.Character];
				break;
			case Recipe.Component.Scenario:
				labelChannel.Text = "Scenario";
				BackColor = Constants.RecipeColorByCategory[Recipe.Category.Story];
				break;
			case Recipe.Component.Greeting:
				labelChannel.Text = "Greeting";
				BackColor = Constants.RecipeColorByCategory[Recipe.Category.Chat];
				break;
			case Recipe.Component.Example:
				labelChannel.Text = "Example";
				BackColor = Constants.RecipeColorByCategory[Recipe.Category.Chat];
				break;
			case Recipe.Component.Grammar:
				labelChannel.Text = "Grammar";
				BackColor = Constants.RecipeColorByCategory[Recipe.Category.Model];
				break;
			case Recipe.Component.UserPersona:
				labelChannel.Text = "User persona";
				BackColor = Color.Azure;
				break;
			default:
				labelChannel.Text = "Text";
				BackColor = Constants.RecipeColorByCategory[Recipe.Category.Undefined];
				break;
			}

			labelChannel.ForeColor = Utility.GetContrastColor(BackColor, false);
		}

		public void SetSwapped(bool bSwapped)
		{
			_bSwapped = bSwapped;

			_bIgnoreEvents = true;
			textBox_Text.richTextBox.DisableThenDoThenEnable(() => {
				textBox_Text.Text = bSwapped ? SwappedText : OriginalText;
				textBox_Text.richTextBox.RefreshSyntaxHighlight(true); // Rehighlight
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

				// Change background color
				if (textBox_Text.Enabled)
				{
					switch (channel)
					{
					case Recipe.Component.System:
						BackColor = Constants.RecipeColorByCategory[Recipe.Category.Model];
						break;
					case Recipe.Component.System_PostHistory:
						BackColor = Constants.RecipeColorByCategory[Recipe.Category.Model];
						break;
					case Recipe.Component.Persona:
						BackColor = Constants.RecipeColorByCategory[Recipe.Category.Character];
						break;
					case Recipe.Component.Scenario:
						BackColor = Constants.RecipeColorByCategory[Recipe.Category.Story];
						break;
					case Recipe.Component.Greeting:
						BackColor = Constants.RecipeColorByCategory[Recipe.Category.Chat];
						break;
					case Recipe.Component.Example:
						BackColor = Constants.RecipeColorByCategory[Recipe.Category.Chat];
						break;
					case Recipe.Component.Grammar:
						BackColor = Constants.RecipeColorByCategory[Recipe.Category.Model];
						break;
					case Recipe.Component.UserPersona:
						BackColor = Color.Azure;
						break;
					default:
						BackColor = Constants.RecipeColorByCategory[Recipe.Category.Undefined];
						break;
					}
				}
				else
				{
					BackColor = Constants.RecipeColorByCategory[Recipe.Category.Undefined];
				}
				labelChannel.ForeColor = Utility.GetContrastColor(BackColor, false);
			}
		}
	}
}
