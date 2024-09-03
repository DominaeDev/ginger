using Ginger.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
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

		public static CAIChat FromChat(ChatHistory chatHistory)
		{
			CAIChat caiChatV2 = new CAIChat();
			var lsItems = new List<ChatItem>(chatHistory.count / 2 + 1);

			foreach (var pair in chatHistory.messagesWithoutGreeting.Pairwise())
			{
				lsItems.Add(new ChatItem() {
					input = pair.Item1.text ?? "",
					output = pair.Item2?.text ?? "",
					timestamp = pair.Item1.creationDate.ToUnixTimeMilliseconds(),
				});
			}

			caiChatV2.data.items = lsItems.ToArray();
			return caiChatV2;
		}

		public ChatHistory ToChat()
		{
			var messages = new List<ChatHistory.Message>();
			foreach (var item in data.items)
			{
				DateTime inputTime = DateTimeExtensions.FromUnixTime(item.timestamp);
				DateTime outputTime = DateTimeExtensions.FromUnixTime(item.timestamp) + TimeSpan.FromMilliseconds(10);
				if (string.IsNullOrEmpty(item.input) == false)
				{
					messages.Add(new ChatHistory.Message() {
						speaker = 0,
						creationDate = inputTime,
						updateDate = inputTime,
						activeSwipe = 0,
						swipes = new string[1] { item.input },
					});
				}
				if (string.IsNullOrEmpty(item.output) == false)
				{
					messages.Add(new ChatHistory.Message() {
						speaker = 1,
						creationDate = outputTime,
						updateDate = outputTime,
						activeSwipe = 0,
						swipes = new string[1] { item.output },
					});
				}
			}

			return new ChatHistory() {
				messages = messages.ToArray(),
			};
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
