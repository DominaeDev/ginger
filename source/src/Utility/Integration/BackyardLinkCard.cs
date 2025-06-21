using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Ginger.Integration
{
	using CharacterMessage = Backyard.CharacterMessage;

	public class BackyardLinkCard
	{
		public Data data = new Data();

		private static readonly string[][] StandardModelInstructionsByFormat = new string[][]
		{
			// Solo
			new string[8] {
				// None
				"",
				// Asterisks
				"",
				// Quotes
				"",
				// Quotes + Asterisks
				"This is a never-ending story between {user} and {characters}.\nWrite in the third-person, using double quotes for spoken dialogue. For example: {user} said \"Hello!\".\nDescribe any other narration or non-verbal actions between asterisks. For example: *{user} waves hello.*\nAvoid using parentheses or brackets.\nEach reply should be 3-4 sentences, with detailed accounts of movements, appearances, actions, smell, texture, and feelings.\nStay in character to provide the most immersive response that progresses the story.",
				// Decorative quotes
				"This is a never-ending story between {user} and {characters}.\nWrite in the third-person, using double quotes for spoken dialogue. For example: {user} said \u201CHello!\u201D.\nAvoid using parentheses or brackets.\nEach reply should be 3-4 sentences, with detailed accounts of movements, appearances, actions, smell, texture, and feelings.\nStay in character to provide the most immersive response that progresses the story.",
				// Bold
				"This is a never-ending chat transcript between {user} and {characters}.\nDialogue should be written in plaintext, excluding quotes and double quotes.\nDescribe any other narration or non-verbal actions between double asterisks. For example: **{user} waves hello.**\nAvoid using parentheses or brackets.\nEach reply should be 3-4 sentences. Include detailed accounts of movements, appearances, actions, smell, texture, and feelings.\nStay in character to provide the most immersive response that progresses the dialogue.",
				// Parentheses
				"This is a never-ending chat transcript between {user} and {characters}.\nDialogue should be written in plaintext, excluding quotes and double quotes.\nDescribe any other narration or non-verbal actions between parentheses. For example: ({user} waves hello.)\nAvoid using parentheses or brackets.\nEach reply should be 3-4 sentences. Include detailed accounts of movements, appearances, actions, smell, texture, and feelings.\nStay in character to provide the most immersive response that progresses the dialogue.",
				// Japanese
				"This is a never-ending story between {user} and {characters}.\nWrite in the third-person, using CJK quotes for spoken dialogue. For example: {user} said \u300CHello!\u300D.\nAvoid using parentheses or brackets.\nEach reply should be 3-4 sentences, with detailed accounts of movements, appearances, actions, smell, texture, and feelings.\nStay in character to provide the most immersive response that progresses the story.",
			},

			// Group
			new string[8] {
				// None
				"",
				// Asterisks
				"",
				// Quotes
				"",
				// Quotes + Asterisks
				"This is a never-ending story between {user} and {characters}.\nWrite in the third-person, using double quotes for spoken dialogue. For example: {user} said \"Hello!\".\nDescribe any other narration or non-verbal actions between asterisks. For example: *{user} waves hello.*\nAvoid using parentheses or brackets.\nEach reply should be 3-4 sentences, with detailed accounts of movements, appearances, actions, smell, texture, and feelings.\nStay in character to provide the most immersive response that progresses the story.",
				// Decorative quotes
				"This is a never-ending story between {user} and {characters}.\nWrite in the third-person, using double quotes for spoken dialogue. For example: {user} said \u201CHello!\u201D.\nAvoid using parentheses or brackets.\nEach reply should be 3-4 sentences, with detailed accounts of movements, appearances, actions, smell, texture, and feelings.\nStay in character to provide the most immersive response that progresses the story.",
				// Bold
				"This is a never-ending chat transcript between {user} and {characters}.\nDialogue should be written in plaintext, excluding quotes and double quotes.\nDescribe any other narration or non-verbal actions between double asterisks. For example: **{user} waves hello.**\nAvoid using parentheses or brackets.\nEach reply should be 3-4 sentences. Include detailed accounts of movements, appearances, actions, smell, texture, and feelings.\nStay in character to provide the most immersive response that progresses the dialogue.",
				// Parentheses
				"This is a never-ending chat transcript between {user} and {characters}.\nDialogue should be written in plaintext, excluding quotes and double quotes.\nDescribe any other narration or non-verbal actions between parentheses. For example: ({user} waves hello.)\nAvoid using parentheses or brackets.\nEach reply should be 3-4 sentences. Include detailed accounts of movements, appearances, actions, smell, texture, and feelings.\nStay in character to provide the most immersive response that progresses the dialogue.",
				// Japanese
				"This is a never-ending story between {user} and {characters}.\nWrite in the third-person, using CJK quotes for spoken dialogue. For example: {user} said \u300CHello!\u300D.\nAvoid using parentheses or brackets.\nEach reply should be 3-4 sentences, with detailed accounts of movements, appearances, actions, smell, texture, and feelings.\nStay in character to provide the most immersive response that progresses the story.",
			}
		};

		public class Data
		{
			public Data()
			{
				creationDate = updateDate = DateTime.UtcNow.ToISO8601();
			}

			public string id;
			public string displayName;
			public string name;
			public string persona;
			public string scenario;
			public string system;
			public string grammar;
			public CharacterMessage greeting;
			public CharacterMessage[] exampleMessages;
			public CardData.TextStyle textStyle;

			public string example
			{
				get { return BackyardUtil.MessagesToString(exampleMessages); }
				set { exampleMessages = BackyardUtil.MessagesFromString(value); }
			}

			public string creationDate;
			public string updateDate;
			public bool isNSFW;

			public LoreBookEntry[] loreItems = new LoreBookEntry[0];
		}

		public string creator;
		public string hubCharacterId;		// Backyard
		public string hubAuthorUsername;	// Backyard
		public string authorNote;
		public string userPersona;

		public class LoreBookEntry
		{
			public string id;
			public string key;
			public string value;
		}

		public static BackyardLinkCard FromOutput(Generator.Output output)
		{
			BackyardLinkCard card = new BackyardLinkCard();
			card.data.displayName = Current.CardName;
			card.data.name = Current.MainCharacter.name;
			card.data.system = output.system.ToFaraday();
			card.data.persona = output.persona.ToFaraday();
			card.data.scenario = output.scenario.ToFaraday();
			card.data.greeting = CharacterMessage.FromString(output.greeting.ToFaradayGreeting());
			card.data.example = output.example.ToString();
			card.data.grammar = output.grammar.ToString();
			card.data.creationDate = (Current.Card.creationDate ?? DateTime.UtcNow).ToISO8601();
			card.data.textStyle = Current.Card.textStyle;
			card.creator = Current.Card.creator;

			// Append user persona
			string userPersona = output.userPersona.ToFaraday();
			if (string.IsNullOrEmpty(userPersona) == false)
			{
				if (Current.Card.extraFlags.Contains(CardData.Flag.UserPersonaInScenario)
					&& Current.Card.extraFlags.Contains(CardData.Flag.OmitScenario) == false)
					card.data.scenario = string.Concat(card.data.scenario, "\n\n", userPersona).Trim();
				else
					card.data.persona = string.Concat(card.data.persona, "\n\n", userPersona).Trim();
			}

			string postHistoryInstructions = output.system_post_history.ToFaraday();
			if (string.IsNullOrEmpty(postHistoryInstructions) == false)
			{
				if (AppSettings.BackyardLink.WriteAuthorNote)
				{
					// system_post_history is equivalent to the author note.
					card.authorNote = output.system_post_history.ToFaraday();
					output.system_post_history = GingerString.Empty;
				}
				else
				{
					var sbSystem = new StringBuilder(card.data.system);
					sbSystem.NewParagraph();
					sbSystem.AppendLine(postHistoryInstructions);
					card.data.system = sbSystem.ToString();
				}
			}

			// Resolve {original} 
			int pos_original = card.data.system.IndexOf("{original}", 0, StringComparison.OrdinalIgnoreCase);
			if (pos_original != -1)
			{
				bool bGroup = output.context.HasFlag(Constants.Flag.Group) || output.context.HasFlag(Constants.Flag.MultiCharacter);
				var sbSystem = new StringBuilder(card.data.system);
				sbSystem.Remove(pos_original, 10);
				sbSystem.Insert(pos_original, GetStandardModelInstructions(card.data.textStyle, bGroup));
				sbSystem.Replace("{original}", ""); // Remove any remaining
				card.data.system = sbSystem.ToString();
			}

			if (output.hasLore)
			{
				card.data.loreItems = output.lorebook.entries
					.Select(e => new LoreBookEntry() {
						key = e.key,
						value = GingerString.FromString(e.value).ToFaraday(),
					}).ToArray();
			}
			else
				card.data.loreItems = new LoreBookEntry[0];

			card.data.isNSFW = Current.IsNSFW && AppSettings.BackyardLink.MarkNSFW;

			return card;
		}

		public static string GetStandardModelInstructions(CardData.TextStyle textStyle, bool bGroup)
		{
			if (BackyardValidation.CheckFeature(BackyardValidation.Feature.GroupChat) == false)
				return FaradayCardV4.OriginalModelInstructionsByFormat[EnumHelper.ToInt(textStyle)];
			return StandardModelInstructionsByFormat[bGroup ? 1 : 0][EnumHelper.ToInt(textStyle)];
		}

		public void EnsureSystemPrompt(bool bGroup)
		{
			if (BackyardValidation.CheckFeature(BackyardValidation.Feature.DefaultSystemPrompts) && data.textStyle <= CardData.TextStyle.Novel)
				return; // No prompt necessary

			// Insert default system prompt if empty
			if (string.IsNullOrWhiteSpace(data.system))
			{
				var ctx = Current.MainCharacter.GetContext(CharacterData.ContextType.None);
				data.system = Text.Eval(GetStandardModelInstructions(data.textStyle, bGroup), ctx, Text.EvalOption.None);
			}
		}

		public FaradayCardV4 ToFaradayCard()
		{
			var card = new FaradayCardV4()
			{
				data = new FaradayCardV4.Data() 
				{
					name = data.name ?? Constants.DefaultCharacterName,
					displayName = data.displayName ?? data.name ?? Constants.DefaultCharacterName,
					system = data.system,
					persona = data.persona,
					scenario = data.scenario,
					greeting = data.greeting.text,
					example = data.example,
					grammar = data.grammar,
					isNSFW = data.isNSFW,
					creationDate = data.creationDate,
					updateDate = data.creationDate,
				},

				creator = this.creator,
				hubAuthorUsername = this.hubAuthorUsername,
				hubCharacterId = this.hubCharacterId,
				authorNote = this.authorNote,
				userPersona = this.userPersona,
			};

			if (string.IsNullOrEmpty(data.greeting.name) == false)
			{
				if (data.greeting.name != card.data.name && data.greeting.name != card.data.displayName)
					card.data.greeting = string.Concat(data.greeting.name, ": ", card.data.greeting);
			}

			// Ensure system prompt
			if (string.IsNullOrEmpty(card.data.system))
				card.data.system = FaradayCardV4.OriginalModelInstructionsByFormat[EnumHelper.ToInt(data.textStyle)]; // Legacy

			if (data.loreItems != null)
			{
				card.data.loreItems = data.loreItems
					.Select(i => new FaradayCardV1.LoreBookEntry() {
						key = i.key,
						value = i.value,
					})
					.ToArray();
			}
			return card;
		}

		public static BackyardLinkCard FromFaradayCard(FaradayCardV4 cardV4)
		{
			var card = new BackyardLinkCard() {
				data = new Data() {
					name = cardV4.data.name,
					displayName = cardV4.data.displayName,
					system = cardV4.data.system,
					persona = cardV4.data.persona,
					scenario = cardV4.data.scenario,
					greeting = CharacterMessage.FromString(cardV4.data.greeting),
					example = cardV4.data.example,
					grammar = cardV4.data.grammar,
					isNSFW = cardV4.data.isNSFW,
					creationDate = cardV4.data.creationDate,
					updateDate = cardV4.data.creationDate,
				},

				creator = cardV4.creator,
				hubAuthorUsername = cardV4.hubAuthorUsername,
				hubCharacterId = cardV4.hubCharacterId,
				authorNote = cardV4.authorNote,
				userPersona = cardV4.userPersona,
			};

			if (cardV4.data.loreItems != null)
			{
				cardV4.data.loreItems = cardV4.data.loreItems
					.Select(i => new FaradayCardV1.LoreBookEntry() {
						key = i.key,
						value = i.value,
					})
					.ToArray();
			}
			return card;
		}
	}
}