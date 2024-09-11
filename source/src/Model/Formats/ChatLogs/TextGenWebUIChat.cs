using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using Ginger.Properties;
using Ginger.Integration;

namespace Ginger
{
	public class TextGenWebUIChat
	{
		private static JsonSchema _schema;

		static TextGenWebUIChat()
		{
			_schema = JsonSchema.Parse(Resources.textgenwebui_chat_schema);
		}

		[JsonProperty("internal", Required = Required.Always)]
		public string[][] internal_chat;

		[JsonProperty("visible", Required = Required.Always)]
		public string[][] visible_chat;

		public static TextGenWebUIChat FromChat(ChatHistory chatHistory)
		{
			if (chatHistory == null || chatHistory.isEmpty)
			{
				return new TextGenWebUIChat() {
					internal_chat = new string[0][],
					visible_chat = new string[0][],
				};
			}

			var lsInternal = new List<string[]>(chatHistory.count / 2 + 1);
			var lsVisible = new List<string[]>(chatHistory.count / 2 + 1);

			// Greeting
			int iMsg = 0;
			if (chatHistory.hasGreeting)
			{
				lsInternal.Add(new string[2] {"<|BEGIN-VISIBLE-CHAT|>", chatHistory.messages[0].text });
				lsVisible.Add(new string[2] { "", chatHistory.messages[0].text });
				iMsg = 1;
			}

			string lastMessage = null;
			for (; iMsg < chatHistory.messages.Length; ++iMsg)
			{
				var message = chatHistory.messages[iMsg];
				if (message.speaker == 0) // Is user
				{
					// Check if previous message was from user
					if (iMsg > 0 && chatHistory.messages[iMsg - 1].speaker == 0)
					{
						lsInternal.Add(new string[2] { lastMessage ?? "", "" });
						lsVisible.Add(new string[2] { lastMessage ?? "", "" });
					}
					lastMessage = message.text;
				}
				else
				{
					lsInternal.Add(new string[2] { lastMessage ?? "", message.text ?? "" });
					lsVisible.Add(new string[2] { lastMessage ?? "", message.text ?? "" });
					lastMessage = null;
				}

			}
			return new TextGenWebUIChat() {
				internal_chat = lsInternal.ToArray(),
				visible_chat = lsVisible.ToArray(),
			};
		}

		public ChatHistory ToChat()
		{
			var messages = new List<ChatHistory.Message>();
			foreach (var texts in visible_chat)
			{
				if (texts == null || texts.Length != 2)
					return null;

				DateTime messageTime = DateTime.Now;

				string userMessage = texts[0] ?? "";
				string characterMessage = texts[1] ?? "";
				if (string.IsNullOrEmpty(userMessage) == false)
				{
					messages.Add(new ChatHistory.Message() {
						speaker = 0,
						creationDate = messageTime,
						updateDate = messageTime,
						activeSwipe = 0,
						swipes = new string[1] { userMessage },
					});
				}
				if (string.IsNullOrEmpty(characterMessage) == false)
				{
					messages.Add(new ChatHistory.Message() {
						speaker = 1,
						creationDate = messageTime,
						updateDate = messageTime,
						activeSwipe = 0,
						swipes = new string[1] { characterMessage },
					});
				}
			}

			DateTime time = DateTime.Now;
			for (int i = messages.Count - 1; i >= 0; --i)
			{
				messages[i].creationDate = time;
				messages[i].updateDate = time;
				time -= TimeSpan.FromMilliseconds(50);
			}

			return new ChatHistory() {
				messages = messages.ToArray(),
			};
		}

		public static TextGenWebUIChat FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_schema))
				{
					var card = JsonConvert.DeserializeObject<TextGenWebUIChat>(json);
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
