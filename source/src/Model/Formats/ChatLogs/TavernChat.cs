using Ginger.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ginger
{
	public class TavernChat
	{
		private static JsonSchema _starterSchema;
		private static JsonSchema _entrySchema;

		static TavernChat()
		{
			_starterSchema = JsonSchema.Parse(Resources.tavern_chat_starter_schema);
			_entrySchema = JsonSchema.Parse(Resources.tavern_chat_entry_schema);
		}

		public class Starter
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

		public Starter starter = new Starter();
		public Entry[] entries = new Entry[0];

		public static TavernChat FromJson(string json)
		{
			try
			{
				string[] lines = json.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

				if (lines.Length == 0)
					return null;

				// Parse starter
				JObject jStarter = JObject.Parse(lines[0]);
				if (jStarter.IsValid(_starterSchema) == false)
					return null;

				DateTime createdAt;
				var starter = JsonConvert.DeserializeObject<Starter>(lines[0]);
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
					starter = starter,
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
				sbJson.AppendLine(JsonConvert.SerializeObject(starter, new JsonSerializerSettings() {
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

		public static TavernChat FromChat(ChatHistory chatHistory, string characterName, string userName)
		{
			if (characterName == null)
				characterName = Constants.DefaultCharacterName;
			if (userName == null)
				userName = Constants.DefaultUserName;

			if (chatHistory == null || chatHistory.isEmpty)
			{
				return new TavernChat() {
					starter = new Starter() {
						creationDate = DateTime.UtcNow.ToTavernDate(),
						characterName = characterName,
						userName = userName,
					},
					entries = new Entry[0],
				};
			}
			
			List<Entry> lsEntries = new List<Entry>();
			foreach (var message in chatHistory.messages)
			{
				lsEntries.Add(new Entry() {
					isUser = message.speaker == 0,
					name = message.speaker == 0 ? userName : characterName,
					creationDate = message.creationDate.ToTavernDate(),
					text = message.text,
				});
			}
			return new TavernChat() {
				starter = new Starter() {
					creationDate = DateTime.UtcNow.ToTavernDate(),
					characterName = characterName,
					userName = userName,
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
					if (string.IsNullOrEmpty(starter.characterName) == false)
						text = Utility.ReplaceWholeWord(text, starter.characterName, GingerString.CharacterMarker, false);
					if (string.IsNullOrEmpty(starter.userName) == false)
						text = Utility.ReplaceWholeWord(text, starter.userName, GingerString.UserMarker, false);
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
				return jObject.IsValid(_starterSchema);
			}
			catch
			{
				return false;
			}
		}

	}
}
