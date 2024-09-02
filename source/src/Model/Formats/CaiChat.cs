using Ginger.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Collections.Generic;
using System.Linq;
using Bridge = Ginger.BackyardBridge;

namespace Ginger
{
	public class CAIChat
	{
		private static JsonSchema _schema;

		static CAIChat()
		{
			_schema = JsonSchema.Parse(Resources.cai_chat_v2_schema);
		}

		[JsonProperty("version", Required = Required.Always)]
		public int name = 2;

		[JsonProperty("chat", Required = Required.Always)]
		public Data data = new Data();

		public class Data
		{
			[JsonProperty("ChatItems")]
			public ChatItem[] items = new ChatItem[0];
		}

		public class ChatItem
		{
			[JsonProperty("input", Required = Required.Always)]
			public string input;
			[JsonProperty("output", Required = Required.Always)]
			public string output;
			[JsonProperty("createdAt", Required = Required.Always)]
			public long timestamp;
		}

		public static CAIChat FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_schema))
				{
					var card = JsonConvert.DeserializeObject<CAIChat>(json);
					return card;
				}
			}
			catch
			{
			}
			return null;
		}

		public static CAIChat FromChat(Bridge.ChatInstance chat)
		{
			CAIChat caiChatV2 = new CAIChat();
			var lsItems = new List<ChatItem>(chat.messages.Length / 2 + 1);
			IEnumerable<Bridge.ChatInstance.Message> messages = chat.messages;
			if (string.IsNullOrEmpty(chat.greeting) == false)
				messages = messages.Skip(1);
			foreach (var pair in messages.Pairwise())
			{
				lsItems.Add(new ChatItem() {
					input = pair.Item1.message ?? "",
					output = pair.Item2?.message ?? "",
					timestamp = pair.Item1.creationDate.ToUnixTimeMilliseconds(),
				});
			}

			caiChatV2.data.items = lsItems.ToArray();
			return caiChatV2;
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
				return jObject.IsValid(_schema);
			}
			catch
			{
				return false;
			}
		}
	}
}
