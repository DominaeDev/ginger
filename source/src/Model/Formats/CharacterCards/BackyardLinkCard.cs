using System;
using System.Text;
using System.Linq;

namespace Ginger
{
	public class BackyardLinkCard
	{
		public Data data = new Data();

		public static readonly string[] OriginalModelInstructionsByFormat = new string[8]
		{
			// None
			"Text transcript of a never-ending conversation between {user} and {character}. In the transcript, gestures and other non-verbal actions are written between asterisks (for example, *waves hello* or *moves closer*).",
			// Asterisks
			"Text transcript of a never-ending conversation between {user} and {character}. In the transcript, gestures and other non-verbal actions are written between asterisks (for example, *waves hello* or *moves closer*).",
			// Quotes
			"Text transcript of a never-ending conversation between {user} and {character}.",
			// Quotes + Asterisks
			"Text transcript of a never-ending conversation between {user} and {character}. In the transcript, gestures and other non-verbal actions are written between asterisks (for example, *waves hello* or *moves closer*).",
			// Decorative quotes
			"Text transcript of a never-ending conversation between {user} and {character}.",
			// Bold
			"Text transcript of a never-ending conversation between {user} and {character}. In the transcript, gestures and other non-verbal actions are written between asterisks (for example, **waves hello** or **moves closer**).",
			// Parentheses
			"Text transcript of a never-ending conversation between {user} and {character}. In the transcript, gestures and other non-verbal actions are written between parentheses, for example (waves hello) or (moves closer).",
			// Japanese
			"Text transcript of a never-ending conversation between {user} and {character}.",
		};

		public class Data
		{
			public Data()
			{
				creationDate = updateDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
			}

			public string id;
			public string displayName;
			public string name;
			public string persona;
			public string scenario;
			public string system;
			public string example;
			public string greeting;
			public string grammar;

			public string creationDate;
			public string updateDate;
			public bool isNSFW;

			public LoreBookEntry[] loreItems = new LoreBookEntry[0];
		}

		public string hubCharacterId;
		public string hubAuthorUsername;
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
			card.data.greeting = output.greeting.ToFaradayGreeting();
			card.data.example = output.example.ToFaradayChat();
			card.data.grammar = output.grammar.ToString();
			card.data.creationDate = (Current.Card.creationDate ?? DateTime.UtcNow).ToString("yyyy-MM-ddTHH:mm:ss.fffK");

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
				var sbSystem = new StringBuilder(card.data.system);
				sbSystem.Remove(pos_original, 10);
				sbSystem.Insert(pos_original, OriginalModelInstructionsByFormat[EnumHelper.ToInt(Current.Card.textStyle)]);
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

		public void EnsureSystemPrompt()
		{
			// Insert default system prompt if empty
			if (string.IsNullOrWhiteSpace(data.system))
				data.system = OriginalModelInstructionsByFormat[EnumHelper.ToInt(Current.Card.textStyle)];
		}

		public FaradayCardV4 ToFaradayCard()
		{
			var card = new FaradayCardV4() {
				data = new FaradayCardV4.Data() {
					name = data.name,
					displayName = data.displayName,
					system = data.system,
					persona = data.persona,
					scenario = data.scenario,
					greeting = data.greeting,
					example = data.example,
					grammar = data.grammar,
					isNSFW = data.isNSFW,
					creationDate = data.creationDate,
					updateDate = data.creationDate,
				},

				authorNote = this.authorNote, //!
				hubAuthorUsername = this.hubAuthorUsername, //!
				hubCharacterId = this.hubCharacterId, //!
				userPersona = this.userPersona, //!
			};

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
				data = new BackyardLinkCard.Data() {
					name = cardV4.data.name,
					displayName = cardV4.data.displayName,
					system = cardV4.data.system,
					persona = cardV4.data.persona,
					scenario = cardV4.data.scenario,
					greeting = cardV4.data.greeting,
					example = cardV4.data.example,
					grammar = cardV4.data.grammar,
					isNSFW = cardV4.data.isNSFW,
					creationDate = cardV4.data.creationDate,
					updateDate = cardV4.data.creationDate,
				},

				authorNote = cardV4.authorNote, //!
				hubAuthorUsername = cardV4.hubAuthorUsername, //!
				hubCharacterId = cardV4.hubCharacterId, //!
				userPersona = cardV4.userPersona, //!
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