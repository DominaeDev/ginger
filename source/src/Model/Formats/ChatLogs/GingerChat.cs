using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using Ginger.Properties;
using Ginger.Integration;
using System.Text;

namespace Ginger
{
	public class GingerChat
	{
		private static JsonSchema _schema;

		static GingerChat()
		{
//			JsonSchemaGenerator generator = new JsonSchemaGenerator();
//			JsonSchema schema = generator.Generate(typeof(GingerChat));

			_schema = JsonSchema.Parse(Resources.ginger_chat_v1_schema);
		}

		[JsonProperty("title", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public string title;

		[JsonProperty("createdAt", Required = Required.Default)]
		public long createdAt = 0;

		[JsonProperty("speakers", Required = Required.Always)]
		public Dictionary<string, string> speakers = new Dictionary<string, string>();

		public class Message
		{
			[JsonProperty("speaker", Required = Required.Always)]
			public string speakerId;
			[JsonProperty("message", Required = Required.Always)]
			public string text;
			[JsonProperty("timestamp", Required = Required.Default)]
			public long timestamp = 0;
			[JsonProperty("regens", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
			public string[] regens = null;
		}

		public Message[] messages;

		public struct Speaker
		{
			public string id;
			public string name;

		}
		public static GingerChat FromChat(ChatInstance chatInstance, List<Speaker> speakers)
		{
			if (speakers == null || chatInstance == null || chatInstance.history.isEmpty)
				return null;

			var lsMessages = new List<Message>(chatInstance.history.count);

			foreach (var message in chatInstance.history.messages)
			{
				if (message.swipes.Length > 1)
				{
					lsMessages.Add(new Message() {
						speakerId = speakers[message.speaker].id,
						text = message.text,
						regens = message.swipes,
						timestamp = message.creationDate.ToUnixTimeMilliseconds(),
					});
				}
				else
				{
					lsMessages.Add(new Message() {
						speakerId = speakers[message.speaker].id,
						text = message.text,
						timestamp = message.creationDate.ToUnixTimeMilliseconds(),
					});
				}
			}

			return new GingerChat() {
				title = chatInstance.name,
				speakers = speakers.ToDictionary(s => s.id, s => s.name),
				createdAt = chatInstance.creationDate.ToUnixTimeMilliseconds(),
				messages = lsMessages.ToArray(),
			};
		}

		public ChatHistory ToChat()
		{
			int index = 0;
			var indexById = speakers.ToDictionary(s => s.Key, s => index++);

			var result = new List<ChatHistory.Message>();
			foreach (var message in this.messages)
			{
				DateTime timestamp = DateTimeExtensions.FromUnixTime(message.timestamp);

				int speakerIdx;
				if (indexById.TryGetValue(message.speakerId, out speakerIdx) == false)
					return null; // Error

				string[] swipes;
				int activeSwipe;
				if (message.regens != null && message.regens.Length > 0)
				{
					activeSwipe = Array.IndexOf(message.regens, message.text);
					if (activeSwipe == -1 && message.text != null)
					{
						swipes = new string[message.regens.Length + 1];
						swipes[swipes.Length - 1] = message.text;
						Array.Copy(message.regens, swipes, message.regens.Length);
						activeSwipe = swipes.Length - 1;
					}
					else
						swipes = message.regens;
				}
				else
				{
					swipes = new string[1] { message.text };
					activeSwipe = 0;
				}

				result.Add(new ChatHistory.Message() {
					speaker = speakerIdx,
					creationDate = timestamp,
					updateDate = timestamp,
					activeSwipe = activeSwipe,
					swipes = swipes,
				});
			}

			return new ChatHistory() {
				name = this.title,
				messages = result.ToArray(),
			};
		}
		
		public static GingerChat FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_schema))
				{
					var chat = JsonConvert.DeserializeObject<GingerChat>(json);
					return chat;
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
