using System;
using Newtonsoft.Json;

namespace Ginger
{
	public class FaradayCardV1
	{
		public FaradayCardV1()
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
				id = Cuid.NewCuid();
				creationDate = updateDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
				chat = new Chat[0];
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
			private Chat[] chat = new Chat[0];

			[JsonProperty("grammar")]
			public string grammar { get; set; }

			[JsonProperty("isNSFW")]
			public bool isNSFW { get; set; }

			// Model settings
			[JsonProperty("mirostatEnabled")]
			private bool mirostatEnabled = true;
			
			[JsonProperty("mirostatEntropy")]
			private decimal mirostatEntropy = 5;
			
			[JsonProperty("mirostatLearningRate")]
			private decimal mirostatLearningRate = 0.1m;
			
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
			
			[JsonProperty("topP")]
			private decimal topP = 0.9m;

			[JsonProperty("loreItems")]
			public LoreBookEntry[] loreItems = new LoreBookEntry[0];
		}

		public class Chat
		{
			[JsonProperty("id")]
			public string id { get; set; }

			[JsonProperty("name")]
			private string name = "";

			[JsonProperty("modelConfigId")]
			public string modelConfigId = "";

			[JsonProperty("authorNote")]
			private string authorNote = "";

			[JsonProperty("createdAt")]
			public string creationDate { get; set; }

			[JsonProperty("updatedAt")]
			public string updateDate { get; set; }

			[JsonProperty("ChatItems")]
			private ChatItem[] chatItems = new ChatItem[0];
		}

		public class ChatItem
		{
			[JsonProperty("id")]
			public string id { get; set; }

			[JsonProperty("input")]
			private string input = "";

			[JsonProperty("output")]
			private string output = "";

			[JsonProperty("createdAt")]
			public string creationDate { get; set; }

			[JsonProperty("updatedAt")]
			public string updateDate { get; set; }

			[JsonProperty("RegenSwipes")]
			private RegenSwipe[] regenSwipes = new RegenSwipe[0];
		}

		public class RegenSwipe
		{
			[JsonProperty("id")]
			public string id { get; set; }

			[JsonProperty("text")]
			private string text = "";

			[JsonProperty("createdAt")]
			public string creationDate { get; set; }

			[JsonProperty("updatedAt")]
			public string updateDate { get; set; }

			[JsonProperty("activeTimestamp")]
			public string activeTimestamp { get; set; }
		}

		public class LoreBookEntry
		{
			public LoreBookEntry()
			{
				id = Cuid.NewCuid();
			}

			[JsonProperty("id")]
			public string id = "";

			[JsonProperty("key")]
			public string key { get; set; }

			[JsonProperty("value")]
			public string value { get; set; }
		}

		[JsonProperty("version", Required = Required.Always)]
		public int version = 1;
	}

}