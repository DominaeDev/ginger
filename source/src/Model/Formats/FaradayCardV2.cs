using System;
using Ginger.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Ginger
{
	public class FaradayCardV2
	{
		private static JsonSchema _faradayCardV2Schema;

		static FaradayCardV2()
		{
			_faradayCardV2Schema = JsonSchema.Parse(Resources.faraday_charactercard_v2_schema);
		}

		public FaradayCardV2()
		{
			data = new Data();
		}

#pragma warning disable 0414
#pragma warning disable 0169
		[JsonProperty("character", Required = Required.Always)]
		public Data data;
				
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
			private int repeatLastN = 128;
			
			[JsonProperty("repeatPenalty")]
			private decimal repeatPenalty = 1;
			
			[JsonProperty("temperature")]
			private decimal temperature = 0.8m;
			
			[JsonProperty("topK")]
			private int topK = 30;
			
			[JsonProperty("minP")]
			private decimal minP = 0.1m; // New in v2
			
			[JsonProperty("minPEnabled")]
			private bool minPEnabled = true; // New in v2
			
			[JsonProperty("topP")]
			private decimal topP = 0.9m;

			[JsonProperty("loreItems")]
			public FaradayCardV1.LoreBookEntry[] loreItems = new FaradayCardV1.LoreBookEntry[0];
		}

		[JsonProperty("version", Required = Required.Always)]
		public int version = 2;

		public static string GenerateUniqueID()
		{
			return Cuid.NewCuid();
		}

		public static FaradayCardV2 FromV1(FaradayCardV1 cardV1)
		{
			var cardV2 = new FaradayCardV2();
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

		public static bool Validate(string jsonData)
		{
			try
			{
				JObject jObject = JObject.Parse(jsonData);
				return jObject.IsValid(_faradayCardV2Schema);
			}
			catch
			{
				return false;
			}
		}
	}

}