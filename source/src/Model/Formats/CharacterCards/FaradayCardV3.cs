using System;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Ginger.Properties;
using Newtonsoft.Json.Linq;

namespace Ginger
{
	public class FaradayCardV3
	{
		private static JsonSchema _faradayCardV3Schema;

		static FaradayCardV3()
		{
			_faradayCardV3Schema = JsonSchema.Parse(Resources.faraday_charactercard_v3_schema);
		}

		public FaradayCardV3()
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

				creationDate = updateDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
				chat = new FaradayCardV1.Chat[0];
			}

			[JsonProperty("id", Required = Required.Always)]
			public string id { get; set; }

			[JsonProperty("aiDisplayName", Required = Required.Always)]
			public string displayName { get; set; }

			[JsonProperty("aiName", Required = Required.Always)]
			public string name { get; set; }

			[JsonProperty("aiPersona")]
			public string persona { get; set; }

			[JsonProperty("scenario")]
			public string scenario { get; set; }

			[JsonProperty("basePrompt")]
			public string system { get; set; }

			[JsonProperty("customDialogue")]
			public string example { get; set; }

			[JsonProperty("firstMessage")]
			public string greeting { get; set; }

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
			public string promptTemplate = null; // New in v3 (null|"general"|"ChatML")

			[JsonProperty("loreItems")]
			public FaradayCardV1.LoreBookEntry[] loreItems = new FaradayCardV1.LoreBookEntry[0];
		}

		[JsonProperty("version", Required = Required.Always)]
		public int version = 3;

		public static FaradayCardV3 FromOutput(Generator.Output output)
		{
			FaradayCardV3 card = new FaradayCardV3();
			card.data.displayName = Current.CardName;
			card.data.name = Current.Name;
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
				if (Current.Card.extraFlags.Contains(CardData.Flag.UserPersonaInScenario))
					card.data.scenario = string.Concat(card.data.scenario, "\n\n", userPersona).Trim();
				else
					card.data.persona = string.Concat(card.data.persona, "\n\n", userPersona).Trim();
			}

			// Join system prompt + post_history (jic. post_history should be empty)
			string system_post = output.system_post_history.ToFaraday();
			if (string.IsNullOrEmpty(system_post) == false)
				card.data.system = string.Join("\n", card.data.system, system_post).TrimStart();

			// Insert default system prompt if empty
			if (string.IsNullOrWhiteSpace(card.data.system))
				card.data.system = OriginalModelInstructionsByFormat[EnumHelper.ToInt(Current.Card.textStyle)];
			else
			{
				// Replace 
				int pos_original = card.data.system.IndexOf("{original}", 0, StringComparison.OrdinalIgnoreCase);
				if (pos_original != -1)
				{
					var sbSystem = new StringBuilder(card.data.system);
					sbSystem.Remove(pos_original, 10);
					sbSystem.Insert(pos_original, OriginalModelInstructionsByFormat[EnumHelper.ToInt(Current.Card.textStyle)]);
					sbSystem.Replace("{original}", ""); // Remove any remaining
					card.data.system = sbSystem.ToString();
				}
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

			var chatSettings = AppSettings.BackyardSettings.UserSettings;
			card.data.repeatLastN = chatSettings.repeatLastN;
			card.data.repeatPenalty = chatSettings.repeatPenalty;
			card.data.temperature = chatSettings.temperature;
			card.data.topK = chatSettings.topK;
			card.data.topP = chatSettings.topP;
			card.data.minPEnabled = chatSettings.minPEnabled;
			card.data.minP = chatSettings.minP;
			card.data.promptTemplate = chatSettings.promptTemplate;
			card.data.isNSFW = Current.IsNSFW;
			
			return card;
		}

		public static FaradayCardV3 FromJson(string json)
		{
			// Version 3
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_faradayCardV3Schema))
				{
					var card = JsonConvert.DeserializeObject<FaradayCardV3>(json);
					if (card.version >= 3)
						return card;
				}
			}
			catch
			{ }

			// Version 2
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_faradayCardV3Schema))
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
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_faradayCardV3Schema))
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
			catch
			{
				return null;
			}
		}

		public static string GenerateUniqueID()
		{
			return Cuid.NewCuid();
		}

		public static FaradayCardV3 FromV1(FaradayCardV1 cardV1)
		{
			var cardV2 = new FaradayCardV3();
			cardV2.data.creationDate = cardV1.data.creationDate;
			cardV2.data.updateDate = cardV1.data.updateDate;
			cardV2.data.displayName = cardV1.data.displayName;
			cardV2.data.example = cardV1.data.example;
			cardV2.data.grammar = cardV1.data.grammar;
			cardV2.data.greeting = cardV1.data.greeting;
			cardV2.data.id = cardV1.data.id;
			cardV2.data.isNSFW = cardV1.data.isNSFW;
			cardV2.data.name = cardV1.data.name;
			cardV2.data.persona = cardV1.data.persona;
			cardV2.data.scenario = cardV1.data.scenario;
			cardV2.data.system = cardV1.data.system;
			cardV2.data.loreItems = cardV1.data.loreItems;

			return cardV2;
		}

		public static FaradayCardV3 FromV2(FaradayCardV2 cardV2)
		{
			var cardV3 = new FaradayCardV3();
			cardV3.data.creationDate = cardV2.data.creationDate;
			cardV3.data.updateDate = cardV2.data.updateDate;
			cardV3.data.displayName = cardV2.data.displayName;
			cardV3.data.example = cardV2.data.example;
			cardV3.data.grammar = cardV2.data.grammar;
			cardV3.data.greeting = cardV2.data.greeting;
			cardV3.data.id = cardV2.data.id;
			cardV3.data.isNSFW = cardV2.data.isNSFW;
			cardV3.data.name = cardV2.data.name;
			cardV3.data.persona = cardV2.data.persona;
			cardV3.data.scenario = cardV2.data.scenario;
			cardV3.data.system = cardV2.data.system;
			cardV3.data.loreItems = cardV2.data.loreItems;
			return cardV3;
		}

		public static bool Validate(string jsonData)
		{
			try
			{
				JObject jObject = JObject.Parse(jsonData);
				return jObject.IsValid(_faradayCardV3Schema);
			}
			catch
			{
				return false;
			}
		}
	}

}