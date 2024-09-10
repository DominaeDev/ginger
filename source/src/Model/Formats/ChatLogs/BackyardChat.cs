using Ginger.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;

namespace Ginger
{
	public class BackyardChat
	{
		private static JsonSchema _schema;

		static BackyardChat()
		{
			_schema = JsonSchema.Parse(Resources.cai_chat_v2_schema);
		}

		[JsonProperty("version", Required = Required.Always)]
		public int version = 2;

		[JsonProperty("chat", Required = Required.Always)]
		public Data chat = new Data();

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

		public static BackyardChat FromChat(ChatHistory chatHistory)
		{
			BackyardChat chat = new BackyardChat();
			var lsItems = new List<ChatItem>(chatHistory.count / 2 + 1);

			foreach (var pair in chatHistory.messagesWithoutGreeting.Pairwise())
			{
				lsItems.Add(new ChatItem() {
					input = pair.Item1.text ?? "",
					output = pair.Item2?.text ?? "",
					timestamp = pair.Item1.creationDate.ToUnixTimeMilliseconds(),
				});
			}

			chat.chat.items = lsItems.ToArray();
			return chat;
		}

		public ChatHistory ToChat()
		{
			var messages = new List<ChatHistory.Message>();
			foreach (var item in chat.items)
			{
				DateTime inputTime;
				DateTime outputTime;

				if (item.timestamp != 0)
				{
					inputTime = DateTimeExtensions.FromUnixTime(item.timestamp);
					outputTime = DateTimeExtensions.FromUnixTime(item.timestamp) + TimeSpan.FromMilliseconds(10);
				}
				else
				{
					inputTime = DateTime.Now;
					outputTime = DateTime.Now;
				}

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
		
		public static BackyardChat FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_schema))
				{
					var card = JsonConvert.DeserializeObject<BackyardChat>(json);
					return card;
				}
			}
			catch
			{
			}
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
