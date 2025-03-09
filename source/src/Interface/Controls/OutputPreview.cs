using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WinFormsSyntaxHighlighter;

namespace Ginger
{
	public class OutputPreview : TextBoxEx
	{
		private bool _bRightDown = false; // Context menu
		public Generator.Output output;

		public OutputPreview() : base()
		{
			MouseLeave += OnMouseLeave;
			LostFocus += OnMouseLeave;
			HighlightBorder = false;
			InnerBorder = false;
		}

		public void SetOutput(Generator.Output output)
		{
			this.output = output;

			var sbOutput = new StringBuilder();
			string outputSystem = output.system.ToOutputPreview();
			string outputSystemPostHistory = output.system_post_history.ToOutputPreview();
			string outputPersona = output.persona.ToOutputPreview();
			string outputPersonality = output.personality.ToOutputPreview();
			string outputScenario = output.scenario.ToOutputPreview();
			string outputGreeting = output.greeting.ToOutputPreview(Recipe.Component.Greeting);
			string outputExample = output.example.ToOutputPreview(Recipe.Component.Example);
			string outputGrammar = output.grammar.ToOutputPreview();
			string outputUserPersona = output.userPersona.ToOutputPreview();

			if (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday 
				&& (Integration.Backyard.ConnectionEstablished && AppSettings.BackyardLink.WriteAuthorNote) == false)
			{
				// Combine system prompts
				if (string.IsNullOrEmpty(outputSystemPostHistory) == false)
					outputSystem = string.Join("\r\n", outputSystem, outputSystemPostHistory).TrimStart();
				outputSystemPostHistory = null;

			}
			if (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday)
			{
				// Replace {original}
				string original = FaradayCardV4.OriginalModelInstructionsByFormat[EnumHelper.ToInt(Current.Card.textStyle)];
				if (string.IsNullOrWhiteSpace(outputSystem) == false)
				{
					int pos_original = outputSystem.IndexOf(GingerString.OriginalMarker, 0);
					if (pos_original != -1)
					{
						var sbSystem = new StringBuilder(outputSystem);
						sbSystem.Remove(pos_original, 10);
						sbSystem.Insert(pos_original, original);
						sbSystem.Replace(GingerString.OriginalMarker, ""); // Only once
						outputSystem = sbSystem.ToString();
					}
				}
			}
			
			if (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.SillyTavern
				|| AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday)
			{
				// Combine user persona
				if (string.IsNullOrEmpty(outputUserPersona) == false)
				{
					if (Current.Card.extraFlags.Contains(CardData.Flag.UserPersonaInScenario) 
						&& Current.Card.extraFlags.Contains(CardData.Flag.OmitScenario) == false) // -> Scenario
						outputScenario = string.Concat(outputScenario, "\r\n\r\n", outputUserPersona).Trim();
					else // -> Persona
						outputPersona = string.Concat(outputPersona, "\r\n\r\n", outputUserPersona).Trim();
					outputUserPersona = null;
				}
			}

			if (output.userPersona.IsNullOrEmpty() == false)
			{
				// Append user persona to the persona for a more accurate token count
				string scenario = output.scenario.ToString() ?? "";
				string userPersona = output.userPersona.ToString();

				scenario = string.Concat(scenario, "\n\n", userPersona).Trim();
				output.scenario = GingerString.FromString(scenario);
				output.userPersona = new GingerString(); // Empty
			}

			if (string.IsNullOrEmpty(outputSystem) == false)
			{
				sbOutput.AppendLine(Header("MODEL INSTRUCTIONS"));
				sbOutput.AppendLine();
				sbOutput.AppendLine(outputSystem);
				sbOutput.AppendLine();
			}
			if (string.IsNullOrEmpty(outputSystemPostHistory) == false)
			{
				sbOutput.AppendLine(Header("MODEL INSTRUCTIONS (IMPORTANT)"));
				sbOutput.AppendLine();
				sbOutput.AppendLine(outputSystemPostHistory);
				sbOutput.AppendLine();
			}
			if (string.IsNullOrEmpty(outputPersona) == false)
			{
				sbOutput.AppendLine(Header("CHARACTER PERSONA"));
				sbOutput.AppendLine();
				sbOutput.AppendLine(outputPersona);
				sbOutput.AppendLine();
			}
			if (string.IsNullOrEmpty(outputPersonality) == false)
			{
				sbOutput.AppendLine(Header("PERSONALITY SUMMARY"));
				sbOutput.AppendLine();
				sbOutput.AppendLine(outputPersonality);
				sbOutput.AppendLine();
			}
			if (string.IsNullOrEmpty(outputUserPersona) == false)
			{
				sbOutput.AppendLine(Header("USER PERSONA"));
				sbOutput.AppendLine();
				sbOutput.AppendLine(outputUserPersona);
				sbOutput.AppendLine();
			}
			if (string.IsNullOrEmpty(outputScenario) == false)
			{
				sbOutput.AppendLine(Header("SCENARIO"));
				sbOutput.AppendLine();
				sbOutput.AppendLine(outputScenario);
				sbOutput.AppendLine();
			}
			if (string.IsNullOrEmpty(outputExample) == false)
			{
				sbOutput.AppendLine(Header("EXAMPLE CHAT"));
				sbOutput.AppendLine();
				sbOutput.AppendLine(outputExample);
				sbOutput.AppendLine();
			}
			if (string.IsNullOrEmpty(outputGreeting) == false)
			{
				if (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday)
					sbOutput.AppendLine(Header("FIRST MESSAGE"));
				else
					sbOutput.AppendLine(Header("GREETING"));

				sbOutput.AppendLine();
				sbOutput.AppendLine(outputGreeting);
				sbOutput.AppendLine();
			}
			if (output.greetings != null && output.greetings.Length > 1)
			{
				for (int i = 1; i < output.greetings.Length; ++i)
				{
					var greeting = output.greetings[i].ToOutputPreview(Recipe.Component.Greeting);
					if (output.greetings.Length > 2)
						sbOutput.AppendLine(Header(string.Format("ALTERNATE GREETING #{0}", i)));
					else
						sbOutput.AppendLine(Header("ALTERNATE GREETING"));
					sbOutput.AppendLine();
					sbOutput.AppendLine(greeting);
					sbOutput.AppendLine();
				}
			}
			if (output.group_greetings != null && output.group_greetings.Length > 0)
			{
				for (int i = 0; i < output.group_greetings.Length; ++i)
				{
					var greeting = output.group_greetings[i].ToOutputPreview(Recipe.Component.Greeting);
					if (output.group_greetings.Length > 1)
						sbOutput.AppendLine(Header(string.Format("GROUP-ONLY GREETING #{0}", i + 1)));
					else
						sbOutput.AppendLine(Header("GROUP-ONLY GREETING"));
					sbOutput.AppendLine();
					sbOutput.AppendLine(greeting);
					sbOutput.AppendLine();
				}
			}
			if (string.IsNullOrEmpty(outputGrammar) == false)
			{
				sbOutput.AppendLine(Header("GRAMMAR"));
				sbOutput.AppendLine();
				sbOutput.AppendLine(outputGrammar);
				sbOutput.AppendLine();
			}

			if (output.hasLore)
			{
				sbOutput.AppendLine(Header(string.Format("LOREBOOK ({0} {1})", output.lorebook.entries.Count, output.lorebook.entries.Count == 1 ? "ENTRY" : "ENTRIES")));
				
				for (int i = 0; i < output.lorebook.entries.Count; ++i)
				{
					var entry = output.lorebook.entries[i];
					sbOutput.AppendLine();
					sbOutput.AppendLine(string.Format("#{0} [{1}]", i + 1, GingerString.FromString(entry.key).ToOutputPreview(Recipe.Component.Invalid)));
					sbOutput.AppendLine(GingerString.FromString(entry.value).ToOutputPreview(Recipe.Component.Invalid));
				}
			}

			if (sbOutput.Length == 0)
			{
				sbOutput.AppendLine("( NO OUTPUT )");
			}

			this.DisableThenDoThenEnable(() => {
				this.Text = sbOutput.TrimEnd().ToString();
				this.InitUndo();
			});
		}

		private static string Header(string text)
		{
			string line = "--------------------------------------------------"; // 50
			return string.Concat("---- ", text, " ", line.Substring(0, Math.Max(line.Length - text.Length, 0)));
		}

		private void OnMouseLeave(object sender, EventArgs e)
		{
			_bRightDown = false;
		}

		private void OnRightClick(Point location)
		{
			ContextMenuStrip menu = new ContextMenuStrip();

			menu.Items.Add(new ToolStripMenuItem("Copy selection", null, (s, e) => { Copy(); }, Keys.Control | Keys.C) {
				Enabled = SelectionLength > 0,
			});

			menu.Items.Add(new ToolStripMenuItem("Copy all", null, (s, e) => { CopyAll(); }));

			menu.Items.Add(new ToolStripSeparator());

			string labelSystem;
			string labelPersona;
			string labelGreeting;
			if (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday)
			{
				labelSystem = "Copy model instructions";
				labelPersona = "Copy character persona";
				labelGreeting = "Copy first message";
			}
			else
			{
				labelSystem = "Copy system prompt";
				labelPersona = "Copy character persona";
				labelGreeting = "Copy greeting";
			}

			menu.Items.Add(new ToolStripMenuItem(labelSystem, null,
				(s, e) => { Copy(Recipe.Component.System); }) {
				Enabled = output.system.IsNullOrEmpty() == false || output.system_post_history.IsNullOrEmpty() == false,
			});
			menu.Items.Add(new ToolStripMenuItem(labelPersona, null, 
				(s, e) => { Copy(Recipe.Component.Persona); }) {
				Enabled = output.persona.IsNullOrEmpty() == false,
			});
			if (output.userPersona.IsNullOrEmpty() == false 
				&& (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Default 
				|| AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.PlainText))
			{
				menu.Items.Add(new ToolStripMenuItem("Copy user persona", null,
					(s, e) => { Copy(Recipe.Component.UserPersona); }) {
					Enabled = output.persona.IsNullOrEmpty() == false,
				});
			}


			menu.Items.Add(new ToolStripMenuItem("Copy scenario", null, 
				(s, e) => { Copy(Recipe.Component.Scenario); }) {
				Enabled = output.scenario.IsNullOrEmpty() == false,
			});
			menu.Items.Add(new ToolStripMenuItem("Copy example chat", null, 
				(s, e) => { Copy(Recipe.Component.Example); }) {
				Enabled = output.example.IsNullOrEmpty() == false,
			});
			menu.Items.Add(new ToolStripMenuItem(labelGreeting, null, 
				(s, e) => { Copy(Recipe.Component.Greeting); }) {
				Enabled = output.greeting.IsNullOrEmpty() == false,
			});
			menu.Items.Add(new ToolStripMenuItem("Copy grammar", null, 
				(s, e) => { Copy(Recipe.Component.Grammar); }) {
				Enabled = output.grammar.IsNullOrEmpty() == false,
			});

			menu.Items.Add(new ToolStripSeparator());

			var formatMenu = new ToolStripMenuItem("Preview format");
			menu.Items.Add(formatMenu);
			formatMenu.DropDownItems.Add(new ToolStripMenuItem("Ginger (default)", null, (s, e) => {
				AppSettings.Settings.PreviewFormat = AppSettings.Settings.OutputPreviewFormat.Default;
				Regenerate();
			}) {
				Checked = AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Default,
			});
			formatMenu.DropDownItems.Add(new ToolStripMenuItem("SillyTavern", null, (s, e) => {
				AppSettings.Settings.PreviewFormat = AppSettings.Settings.OutputPreviewFormat.SillyTavern;
				Regenerate();
			}) {
				Checked = AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.SillyTavern,
			});
			formatMenu.DropDownItems.Add(new ToolStripMenuItem("Backyard AI", null, (s, e) => {
				AppSettings.Settings.PreviewFormat = AppSettings.Settings.OutputPreviewFormat.Faraday;
				Regenerate();
			}) {
				Checked = AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday,
			});
			formatMenu.DropDownItems.Add(new ToolStripMenuItem("Plain text", null, (s, e) => {
				AppSettings.Settings.PreviewFormat = AppSettings.Settings.OutputPreviewFormat.PlainText;
				Regenerate();
			}) {
				Checked = AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.PlainText,
			});

			Theme.Apply(menu);
			menu.Show(this, location);
		}

		private void Copy(Recipe.Component component)
		{
			string outputSystem = output.system.ToOutputPreview();
			string outputSystemPostHistory = output.system_post_history.ToOutputPreview();
			string outputPersona = output.persona.ToOutputPreview();
			string outputUserPersona = output.userPersona.ToOutputPreview();
			string outputScenario = output.scenario.ToOutputPreview();
			string outputGreeting = output.greeting.ToOutputPreview(Recipe.Component.Greeting);
			string outputExample = output.example.ToOutputPreview(Recipe.Component.Example);
			string outputGrammar = output.grammar.ToOutputPreview();

			// Combine system prompts
			if (string.IsNullOrEmpty(outputSystemPostHistory) == false)
				outputSystem = string.Join("\r\n", outputSystem, outputSystemPostHistory).TrimStart();

			// Combine user info
			if (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.SillyTavern
				|| AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday)
			{
				if (string.IsNullOrEmpty(outputUserPersona) == false)
				{
					if (Current.Card.extraFlags.Contains(CardData.Flag.UserPersonaInScenario) 
						&& Current.Card.extraFlags.Contains(CardData.Flag.OmitScenario) == false) // -> Scenario
						outputScenario = string.Concat(outputScenario, "\r\n\r\n", outputUserPersona).Trim();
					else // -> Persona
						outputPersona = string.Concat(outputPersona, "\r\n\r\n", outputUserPersona).Trim();
				}
				outputUserPersona = null;
			}

			if (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday)
			{
				// Add default system prompt if empty
				string original = FaradayCardV4.OriginalModelInstructionsByFormat[EnumHelper.ToInt(Current.Card.textStyle)];
				if (string.IsNullOrWhiteSpace(outputSystem))
					outputSystem = original;
				else
				{
					int pos_original = outputSystem.IndexOf(GingerString.OriginalMarker, 0);
					if (pos_original != -1)
					{
						var sbSystem = new StringBuilder(outputSystem);
						sbSystem.Remove(pos_original, 10);
						sbSystem.Insert(pos_original, original);
						sbSystem.Replace(GingerString.OriginalMarker, ""); // Only once
						outputSystem = sbSystem.ToString();
					}
				}
			}

			switch (component)
			{
			case Recipe.Component.System:
				CopyToClipboard(outputSystem);
				break;
			case Recipe.Component.Persona:
				CopyToClipboard(outputPersona);
				break;
			case Recipe.Component.UserPersona:
				CopyToClipboard(outputUserPersona);
				break;
			case Recipe.Component.Scenario:
				CopyToClipboard(outputScenario);
				break;
			case Recipe.Component.Greeting:
				CopyToClipboard(outputGreeting);
				break;
			case Recipe.Component.Example:
				CopyToClipboard(outputExample);
				break;
			case Recipe.Component.Grammar:
				CopyToClipboard(outputGrammar);
				break;
			}
		}

		private void CopyAll()
		{
			string outputSystem = output.system.ToOutputPreview();
			string outputSystemPostHistory = output.system_post_history.ToOutputPreview();
			string outputPersona = output.persona.ToOutputPreview();
			string outputUserPersona = output.userPersona.ToOutputPreview();
			string outputScenario = output.scenario.ToOutputPreview();
			string outputGreeting = output.greeting.ToOutputPreview(Recipe.Component.Greeting);
			string outputExample = output.example.ToOutputPreview(Recipe.Component.Example);
			string outputGrammar = output.grammar.ToOutputPreview();

			// Combine system prompts
			if (string.IsNullOrEmpty(outputSystemPostHistory) == false)
				outputSystem = string.Join("\r\n", outputSystem, outputSystemPostHistory).TrimStart();

			// Combine user info
			if (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.SillyTavern
				|| AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday)
			{
				if (string.IsNullOrEmpty(outputUserPersona) == false)
				{
					if (Current.Card.extraFlags.Contains(CardData.Flag.UserPersonaInScenario)
						&& Current.Card.extraFlags.Contains(CardData.Flag.OmitScenario) == false) // -> Scenario
						outputScenario = string.Concat(outputScenario, "\r\n\r\n", outputUserPersona).Trim();
					else // -> Persona
						outputPersona = string.Concat(outputPersona, "\r\n\r\n", outputUserPersona).Trim();
				}
				outputUserPersona = null;
			}

			if (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday)
			{
				// Add default system prompt if empty
				string original = FaradayCardV4.OriginalModelInstructionsByFormat[EnumHelper.ToInt(Current.Card.textStyle)];
				if (string.IsNullOrWhiteSpace(outputSystem))
					outputSystem = original;
				else
				{
					int pos_original = outputSystem.IndexOf(GingerString.OriginalMarker, 0);
					if (pos_original != -1)
					{
						var sbSystem = new StringBuilder(outputSystem);
						sbSystem.Remove(pos_original, 10);
						sbSystem.Insert(pos_original, original);
						sbSystem.Replace(GingerString.OriginalMarker, ""); // Only once
						outputSystem = sbSystem.ToString();
					}
				}
			}

			StringBuilder sb = new StringBuilder();
			if (string.IsNullOrEmpty(outputSystem) == false)
			{
				sb.AppendLine("System prompt:");
				sb.AppendLine(outputSystem);
			}
			if (string.IsNullOrEmpty(outputPersona) == false)
			{
				sb.NewParagraph();
				sb.AppendLine("Character persona:");
				sb.AppendLine(outputPersona);
			}
			if (string.IsNullOrEmpty(outputUserPersona) == false)
			{
				sb.NewParagraph();
				sb.AppendLine("User persona:");
				sb.AppendLine(outputUserPersona);
			}			
			if (string.IsNullOrEmpty(outputScenario) == false)
			{
				sb.NewParagraph();
				sb.AppendLine("Scenario:");
				sb.AppendLine(outputScenario);
			}

			if (string.IsNullOrEmpty(outputGreeting) == false)
			{
				sb.NewParagraph();
				sb.AppendLine("Greeting:");
				sb.AppendLine(outputGreeting);
			}
			if (output.greetings != null && output.greetings.Length > 1)
			{
				for (int i = 1; i < output.greetings.Length; ++i)
				{
					sb.NewParagraph();
					var greeting = output.greetings[i].ToOutputPreview(Recipe.Component.Greeting);
					if (output.greetings.Length > 2)
						sb.AppendLine(string.Format("Alternate greeting #{0}:", i));
					else
						sb.AppendLine("Alternate greeting:");
					sb.AppendLine(greeting);
				}
			}
			if (output.group_greetings != null && output.group_greetings.Length > 0)
			{
				for (int i = 0; i < output.group_greetings.Length; ++i)
				{
					sb.NewParagraph();
					var greeting = output.group_greetings[i].ToOutputPreview(Recipe.Component.Greeting);
					if (output.group_greetings.Length > 1)
						sb.AppendLine(string.Format("Group-only greeting #{0}:", i + 1));
					else
						sb.AppendLine("Group-only greeting:");
					sb.AppendLine(greeting);
				}
			}

			if (string.IsNullOrEmpty(outputExample) == false)
			{
				sb.NewParagraph();
				sb.AppendLine("Example chat:");
				sb.AppendLine(outputExample);
			}
			if (string.IsNullOrEmpty(outputGrammar) == false)
			{
				sb.NewParagraph();
				sb.AppendLine("Grammar:");
				sb.AppendLine(outputGrammar);
			}

			CopyToClipboard(sb.ToString());
		}

		private static void CopyToClipboard(string text)
		{
			if (string.IsNullOrEmpty(text))
				return;
			Clipboard.SetText(text, TextDataFormat.UnicodeText);
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == Win32.WM_RBUTTONDOWN)
			{
				_bRightDown = true;
				return;
			}

			if (m.Msg == Win32.WM_RBUTTONUP)
			{
				if (_bRightDown)
				{
					unchecked
					{
						short x = (short)Win32.LoWord(m.LParam);
						short y = (short)Win32.HiWord(m.LParam);
						if (x == (short)0xFFFF) // Is negative
							x = 0;
						if (y == (short)0xFFFF) // Is negative
							y = 0;
						OnRightClick(new Point(x, y));
					}
				}
				return;
			}

			base.WndProc(ref m); 
		}

		private void Regenerate()
		{
			Current.IsDirty = true;
		}

	}
}
