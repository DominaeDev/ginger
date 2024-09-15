using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Text;
using Ginger.Properties;
using Ginger.Integration;

namespace Ginger
{
	public class TavernChat
	{
		private static JsonSchema _headerSchema;
		private static JsonSchema _entrySchema;

		static TavernChat()
		{
			_headerSchema = JsonSchema.Parse(Resources.tavern_chat_header_schema);
			_entrySchema = JsonSchema.Parse(Resources.tavern_chat_entry_schema);
		}

		public class Header
		{
			[JsonProperty("user_name", Required = Required.Always)]
			public string userName;
			[JsonProperty("character_name", Required = Required.Always)]
			public string characterName;
			[JsonProperty("create_date", Required = Required.Default)]
			public string creationDate;
		}

		public class Entry
		{
			[JsonProperty("name", Required = Required.Always)]
			public string name;
			[JsonProperty("is_user", Required = Required.Always)]
			public bool isUser;
			[JsonProperty("send_date", Required = Required.Default)]
			public string creationDate;
			[JsonProperty("mes", Required = Required.Default)]
			public string text;
		}

		public Header header = new Header();
		public Entry[] entries = new Entry[0];

		public static TavernChat FromJson(string json)
		{
			try
			{
				string[] lines = json.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

				if (lines.Length == 0)
					return null;

				// Parse starter
				JObject jHeader = JObject.Parse(lines[0]);
				if (jHeader.IsValid(_headerSchema) == false)
					return null;

				DateTime createdAt;
				var starter = JsonConvert.DeserializeObject<Header>(lines[0]);
				createdAt = DateTimeExtensions.FromTavernDate(starter.creationDate);

				List<Entry> lsEntries = new List<Entry>();
				for (int i = 1; i < lines.Length; ++i)
				{
					JObject jObject = JObject.Parse(lines[i]);
					if (jObject.IsValid(_entrySchema))
					{
						var entry = JsonConvert.DeserializeObject<Entry>(lines[i]);
						lsEntries.Add(entry);
					}
				}

				return new TavernChat() {
					header = starter,
					entries = lsEntries.ToArray(),
				};
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
				var sbJson = new StringBuilder();
				// Starter
				sbJson.AppendLine(JsonConvert.SerializeObject(header, new JsonSerializerSettings() {
						StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
					}));

				foreach (var entry in entries)
				{
					sbJson.AppendLine(JsonConvert.SerializeObject(entry, new JsonSerializerSettings() {
						StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
					}));
				}

				sbJson.ConvertLinebreaks(Linebreak.LF);
				return sbJson.ToString();
			}
			catch
			{
				return null;
			}
		}

		public static TavernChat FromChat(ChatHistory chatHistory, string[] names)
		{
			string userName			= names != null && names.Length > 0 ? names[0] : "You";
			string characterName	= names != null && names.Length > 1 ? names[1] : Constants.DefaultCharacterName;

			if (chatHistory == null || chatHistory.isEmpty)
			{
				return new TavernChat() {
					header = new Header() {
						userName = userName,
						characterName = characterName,
						creationDate = DateTime.UtcNow.ToTavernDate(),
					},
					entries = new Entry[0],
				};
			}
			
			List<Entry> lsEntries = new List<Entry>();
			foreach (var message in chatHistory.messages) // Include greeting
			{
				lsEntries.Add(new Entry() {
					isUser = message.speaker == 0,
					name = message.speaker == 0 ? userName : characterName,
					creationDate = message.creationDate.ToTavernDate(),
					text = message.text,
				});
			}
			return new TavernChat() {
				header = new Header() {
					userName = userName,
					characterName = characterName,
					creationDate = DateTime.UtcNow.ToTavernDate(),
				},
				entries = lsEntries.ToArray(),
			};
		}

		public ChatHistory ToChat()
		{
			var messages = new List<ChatHistory.Message>();
			foreach (var entry in entries)
			{
				DateTime messageTime = DateTimeExtensions.FromTavernDate(entry.creationDate);

				if (string.IsNullOrEmpty(entry.text) == false)
				{
					string text = entry.text;
					if (string.IsNullOrEmpty(header.characterName) == false)
						text = Utility.ReplaceWholeWord(text, header.characterName, GingerString.CharacterMarker, false);
					if (string.IsNullOrEmpty(header.userName) == false)
						text = Utility.ReplaceWholeWord(text, header.userName, GingerString.UserMarker, false);
					text = text.Replace("<START>", "");
					text = GingerString.FromTavern(text).ToString();

					messages.Add(new ChatHistory.Message() {
						speaker = entry.isUser ? 0 : 1,
						creationDate = messageTime,
						updateDate = messageTime,
						activeSwipe = 0,
						swipes = new string[1] { text },
					});
				}
			}

			return new ChatHistory() {
				messages = messages.ToArray(),
			};
		}

		public static bool Validate(string jsonData)
		{
			try
			{
				string[] lines = jsonData.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

				if (lines.Length == 0)
					return false;

				// Parse starter
				JObject jObject = JObject.Parse(lines[0]);
				return jObject.IsValid(_headerSchema);
			}
			catch
			{
				return false;
			}
		}

	}
}
