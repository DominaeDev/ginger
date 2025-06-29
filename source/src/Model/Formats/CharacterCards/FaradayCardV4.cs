﻿using System;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Linq;
using Ginger.Properties;

namespace Ginger
{
	public class FaradayCardV4
	{
		private static JsonSchema _faradayCardV4Schema;

		static FaradayCardV4()
		{
			_faradayCardV4Schema = JsonSchema.Parse(Resources.faraday_charactercard_v4_schema);
		}

		public FaradayCardV4()
		{
			data = new Data();
		}

#pragma warning disable 0414
#pragma warning disable 0169
		[JsonProperty("character", Required = Required.Always)]
		public Data data;

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
				id = GenerateUniqueID();

				creationDate = updateDate = DateTime.UtcNow.ToISO8601();
				chat = new FaradayCardV1.Chat[0];
			}

			[JsonProperty("id", Required = Required.Always)]
			public string id { get; set; }

			[JsonProperty("aiDisplayName", Required = Required.Always)]
			public string displayName { get; set; }

			[JsonProperty("aiName", Required = Required.Always)]
			public string name { get; set; }

			[JsonProperty("aiPersona")]
			public string persona;

			[JsonProperty("scenario")]
			public string scenario;

			[JsonProperty("basePrompt")]
			public string system;

			[JsonProperty("customDialogue")]
			public string example;

			[JsonProperty("firstMessage")]
			public string greeting;

			[JsonProperty("createdAt")]
			public string creationDate { get; set; }

			[JsonProperty("updatedAt")]
			public string updateDate { get; set; }

			[JsonProperty("Chat")]
			private FaradayCardV1.Chat[] chat = new FaradayCardV1.Chat[0];

			[JsonProperty("grammar")]
			public string grammar { get; set; }

			[JsonProperty("isNSFW")]
			public bool isNSFW { get; set; }

			[JsonProperty("model")]
			private string model = "";

			[JsonProperty("repeatLastN")]
			public int repeatLastN = 256;

			[JsonProperty("repeatPenalty")]
			public decimal repeatPenalty = 1.05m;

			[JsonProperty("temperature")]
			public decimal temperature = 1.2m;

			[JsonProperty("topK")]
			public int topK = 30;

			[JsonProperty("minP")]
			public decimal minP = 0.1m;

			[JsonProperty("minPEnabled")]
			public bool minPEnabled = true;

			[JsonProperty("topP")]
			public decimal topP = 0.9m;

			[JsonProperty("promptTemplate")]
			public string promptTemplate = null; // (null | "general" | "ChatML" | "Llama3")

			[JsonProperty("canDeleteCustomDialogue")]
			public bool pruneExampleChat = true; // New in v4

			[JsonProperty("loreItems")]
			public FaradayCardV1.LoreBookEntry[] loreItems = new FaradayCardV1.LoreBookEntry[0];
		}

		// Transient values, not part of JSON
		public string creator;
		public string hubCharacterId;
		public string hubAuthorUsername;
		public string authorNote;
		public string userPersona;

		public string comment
		{
			get 
			{ 
				var sbComment = new StringBuilder();
				if (string.IsNullOrEmpty(hubAuthorUsername) == false)
					sbComment.AppendLine(string.Concat("Original character by ", hubAuthorUsername));
				if (string.IsNullOrEmpty(hubCharacterId) == false)
					sbComment.AppendLine(string.Concat("https://backyard.ai/hub/character/", hubCharacterId));
				sbComment.ConvertLinebreaks(Linebreak.LF);
				return sbComment.ToString();
			}
		}

		[JsonProperty("version", Required = Required.Always)]
		public int version = 4;

		public static FaradayCardV4 FromOutput(Generator.Output output)
		{
			FaradayCardV4 card = new FaradayCardV4();
			card.data.displayName = Current.CardName;
			card.data.name = Current.MainCharacter.name;
			card.data.system = output.system.ToFaraday();
			card.data.persona = output.persona.ToFaraday();
			card.data.scenario = output.scenario.ToFaraday();
			card.data.greeting = output.greeting.ToFaradayGreeting();
			card.data.example = output.example.ToFaradayChat(true);
			card.data.grammar = output.grammar.ToString();
			card.data.creationDate = (Current.Card.creationDate ?? DateTime.UtcNow).ToISO8601();
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
				var sbSystem = new StringBuilder(card.data.system);
				sbSystem.Remove(pos_original, 10);
				sbSystem.Insert(pos_original, OriginalModelInstructionsByFormat[EnumHelper.ToInt(Current.Card.textStyle)]);
				sbSystem.Replace("{original}", ""); // Remove any remaining
				card.data.system = sbSystem.ToString();
			}

			if (output.hasLore)
			{
				card.data.loreItems = output.lorebook.entries
					.Select(e => new FaradayCardV1.LoreBookEntry() {
						key = e.key,
						value = GingerString.FromString(e.value).ToFaraday(),
					}).ToArray();
			}
			else
				card.data.loreItems = new FaradayCardV1.LoreBookEntry[0];

			card.data.isNSFW = Current.IsNSFW && AppSettings.BackyardLink.MarkNSFW;

			return card;
		}

		public static FaradayCardV4 FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_faradayCardV4Schema))
				{
					var card = JsonConvert.DeserializeObject<FaradayCardV4>(json);
					if (card.version >= 4)
						return card;
				}
			}
			catch
			{ }

			// Version 3
			try
			{
				if (FaradayCardV3.Validate(json))
				{
					var card = JsonConvert.DeserializeObject<FaradayCardV3>(json);
					if (card.version == 3)
						return FromV3(card);
				}
			}
			catch
			{ }

			// Version 2
			try
			{
				if (FaradayCardV2.Validate(json))
				{
					var card = JsonConvert.DeserializeObject<FaradayCardV2>(json);
					if (card.version == 2)
						return FromV2(card);
				}
			}
			catch
			{}

			// Version 1
			try
			{
				if (FaradayCardV1.Validate(json))
				{
					var card = JsonConvert.DeserializeObject<FaradayCardV1>(json);
					if (card.version == 1)
						return FromV1(card);
				}
			}
			catch
			{ }

			return null;
		}

		public string ToJson()
		{
			try
			{
				string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings() {
					StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
				});
				return json;
			}
			catch (Exception e)
			{
				return null;
			}
		}

		public static string GenerateUniqueID()
		{
			return Cuid.NewCuid();
		}

		public static FaradayCardV4 FromV1(FaradayCardV1 cardV1)
		{
			var card = new FaradayCardV4();
			card.data.creationDate = cardV1.data.creationDate;
			card.data.updateDate = cardV1.data.updateDate;
			card.data.displayName = cardV1.data.displayName;
			card.data.example = cardV1.data.example;
			card.data.grammar = cardV1.data.grammar;
			card.data.greeting = cardV1.data.greeting;
			card.data.id = cardV1.data.id;
			card.data.isNSFW = cardV1.data.isNSFW;
			card.data.name = cardV1.data.name;
			card.data.persona = cardV1.data.persona;
			card.data.scenario = cardV1.data.scenario;
			card.data.system = cardV1.data.system;
			card.data.loreItems = cardV1.data.loreItems;
			return card;
		}

		public static FaradayCardV4 FromV2(FaradayCardV2 cardV2)
		{
			var card = new FaradayCardV4();
			card.data.creationDate = cardV2.data.creationDate;
			card.data.updateDate = cardV2.data.updateDate;
			card.data.displayName = cardV2.data.displayName;
			card.data.example = cardV2.data.example;
			card.data.grammar = cardV2.data.grammar;
			card.data.greeting = cardV2.data.greeting;
			card.data.id = cardV2.data.id;
			card.data.isNSFW = cardV2.data.isNSFW;
			card.data.name = cardV2.data.name;
			card.data.persona = cardV2.data.persona;
			card.data.scenario = cardV2.data.scenario;
			card.data.system = cardV2.data.system;
			card.data.loreItems = cardV2.data.loreItems;
			return card;
		}

		public static FaradayCardV4 FromV3(FaradayCardV3 cardV3)
		{
			var card = new FaradayCardV4();
			card.data.creationDate = cardV3.data.creationDate;
			card.data.updateDate = cardV3.data.updateDate;
			card.data.displayName = cardV3.data.displayName;
			card.data.example = cardV3.data.example;
			card.data.grammar = cardV3.data.grammar;
			card.data.greeting = cardV3.data.greeting;
			card.data.id = cardV3.data.id;
			card.data.isNSFW = cardV3.data.isNSFW;
			card.data.name = cardV3.data.name;
			card.data.persona = cardV3.data.persona;
			card.data.scenario = cardV3.data.scenario;
			card.data.system = cardV3.data.system;
			card.data.loreItems = cardV3.data.loreItems;
			card.data.promptTemplate = cardV3.data.promptTemplate;
			return card;
		}

		public static bool Validate(string jsonData)
		{
			try
			{
				JObject jObject = JObject.Parse(jsonData);
				return jObject.IsValid(_faradayCardV4Schema)
					|| FaradayCardV3.Validate(jsonData)
					|| FaradayCardV2.Validate(jsonData)
					|| FaradayCardV1.Validate(jsonData);
			}
			catch
			{
				return false;
			}
		}

	}

}