using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using Ginger.Properties;
using Ginger.Integration;

namespace Ginger
{
	public class AgnaiChat
	{
		private static JsonSchema _schema;

		public class Message
		{
			[JsonProperty("userId", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
			public string userId;
			[JsonProperty("characterId", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
			public string characterId;
			[JsonProperty("msg", Required = Required.Always)]
			public string text;
		}

		[JsonProperty("messages", Required = Required.Always)]
		public Message[] messages = new Message[0];

		static AgnaiChat()
		{
			_schema = JsonSchema.Parse(Resources.agnai_chat_schema);
		}

		public static AgnaiChat FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_schema))
				{
					var card = JsonConvert.DeserializeObject<AgnaiChat>(json);
					return card;
				}
			}
			catch
			{
			}
			return null;
		}

		public ChatHistory ToChat()
		{
			var result = new List<ChatHistory.Message>();
			foreach (var message in messages)
			{
				DateTime messageTime = DateTime.Now;
				bool isUser = message.userId != null;

				if (string.IsNullOrEmpty(message.text) == false)
				{
					string text = message.text;
					text = text.Replace("<START>", "");
					text = GingerString.FromTavern(text).ToString();

					result.Add(new ChatHistory.Message() {
						speaker = isUser ? 0 : 1,
						creationDate = messageTime,
						updateDate = messageTime,
						activeSwipe = 0,
						swipes = new string[1] { text },
					});
				}
			}

			return new ChatHistory() {
				messages = result.ToArray(),
			};
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
