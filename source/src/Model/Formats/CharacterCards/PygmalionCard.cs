using System;
using Ginger.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Ginger
{
	public class PygmalionCard
	{
		private static JsonSchema _pygmalionCardSchema;

		static PygmalionCard()
		{
			_pygmalionCardSchema = JsonSchema.Parse(Resources.pygmalion_charactercard_schema);
		}

		[JsonProperty("char_name", Required = Required.Always)]
		public string name;
		
		[JsonProperty("char_persona", Required = Required.Always)]
		public string persona;
		
		[JsonProperty("world_scenario", Required = Required.Always)]
		public string scenario;
		
		[JsonProperty("char_greeting", Required = Required.Always)]
		public string greeting;
		
		[JsonProperty("example_dialogue", Required = Required.Always)]
		public string example;

		[JsonProperty("metadata")]
		public MetaData metaData = new MetaData();

		public class MetaData
		{
			public MetaData()
			{
				creationDate = updateDate = DateTime.UtcNow.ToUnixTimeMilliseconds();
				tool = new Tool() {
					id = Current.Card.uuid,
				};
			}

			[JsonProperty("version")]
			public int version = 1;

			[JsonProperty("created")]
			public long creationDate;

			[JsonProperty("modified")]
			public long updateDate;

			[JsonProperty("source")]
			public string source = null;

			[JsonProperty("tool")]
			public Tool tool = new Tool();

			[JsonProperty("creator")]
			public string creator = "";

			[JsonProperty("comment")]
			public string comment = "";

			public class Tool
			{
				[JsonProperty("name")]
				public string name = "Ginger";

				[JsonProperty("version")]
				public string version = AppVersion.ProductVersion;

				[JsonProperty("card_id")]
				public string id;

				[JsonProperty("url")]
				public string url = Constants.WebsiteURL;
			}
		}

		public static PygmalionCard FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_pygmalionCardSchema))
				{
					var card = JsonConvert.DeserializeObject<PygmalionCard>(json);
					return card;
				}
			}
			catch
			{
			}
			return null;
		}

		public static PygmalionCard FromOutput(Generator.Output output)
		{
			PygmalionCard card = new PygmalionCard();
			card.name = Current.Name;
			card.persona = output.persona.ToTavern();
			card.scenario = output.scenario.ToTavern();
			card.greeting = output.greeting.ToTavern();
			card.example = output.example.ToTavernChat();

			// Append personality
			string personality = output.personality.ToTavern();
			if (string.IsNullOrEmpty(personality) == false)
				card.persona = string.Concat(card.persona, "\n", personality).Trim();

			// Append user persona
			string userPersona = output.userPersona.ToTavern();
			if (string.IsNullOrEmpty(userPersona) == false)
			{
				if (Current.Card.extraFlags.Contains(CardData.Flag.UserPersonaInScenario)
					&& Current.Card.extraFlags.Contains(CardData.Flag.OmitScenario) == false)
					card.scenario = string.Concat(card.scenario, "\n\n", userPersona).Trim();
				else
					card.persona = string.Concat(card.persona, "\n\n", userPersona).Trim();
			}

			card.metaData.creationDate = (Current.Card.creationDate ?? DateTime.UtcNow).ToUnixTimeMilliseconds();
			card.metaData.updateDate = DateTime.UtcNow.ToUnixTimeMilliseconds();
			card.metaData.creator = Current.Card.creator;
			card.metaData.comment = (Current.Card.comment ?? "").ConvertLinebreaks(Linebreak.LF);
			card.metaData.source = string.Concat("ginger:", Current.Card.uuid);
			return card;
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

		public static bool Validate(string jsonData)
		{
			try
			{
				JObject jObject = JObject.Parse(jsonData);
				return jObject.IsValid(_pygmalionCardSchema);
			}
			catch
			{
				return false;
			}
		}
	}
}
