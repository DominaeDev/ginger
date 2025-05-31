using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Ginger.Properties;
using Ginger.Integration;

namespace Ginger
{
	using ChatInstance = Backyard.ChatInstance;
	using ChatParameters = Backyard.ChatParameters;
	using ChatStaging = Backyard.ChatStaging;
	using CharacterMessage = Backyard.CharacterMessage;

	public class BackyardChatBackupV2
	{
		private static JsonSchema _schema;

		static BackyardChatBackupV2()
		{
//			JsonSchemaGenerator generator = new JsonSchemaGenerator();
//			JsonSchema schema = generator.Generate(typeof(BackyardChatBackupV2));
//			string jsonSchema = schema.ToString();

			_schema = JsonSchema.Parse(Resources.backup_chat_v2_schema);
		}

		[JsonProperty("name", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public string name = null;

		[JsonProperty("createdAt", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public long? createdAt;

		[JsonProperty("updatedAt", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public long? updatedAt;

		// Same as Backyard chat
		[JsonProperty("chat", Required = Required.Always)]
		public Chat chat = new Chat();

		// Same as Backyard chat
		[JsonProperty("version", Required = Required.Always)]
		public int version = 2; // For Backyard compatibility

		public class Chat
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

		public static BackyardChatBackupV2 FromChat(BackupData.Chat chat)
		{
			BackyardChatBackupV2 backup = new BackyardChatBackupV2();
			var lsEntries = new List<ChatItem>(chat.history.count / 2 + 1);

			int iMsg = 0;
			if (chat.history.hasGreeting) // Do not include greeting
				iMsg = 1;

			string lastMessage = null;
			for (; iMsg < chat.history.messages.Length; ++iMsg)
			{
				var message = chat.history.messages[iMsg];
				if (message.speaker == 0) // Is user
				{
					// Check if previous message was from user
					if (iMsg > 0 && chat.history.messages[iMsg - 1].speaker == 0)
					{
						lsEntries.Add(new ChatItem() {
							input = lastMessage,
							output = "",
							timestamp = chat.history.messages[iMsg - 1].creationDate.ToUnixTimeMilliseconds(),
						});
					}
					lastMessage = message.text;
				}
				else // Is character
				{
					lsEntries.Add(new ChatItem() {
						input = lastMessage ?? "",
						output = message.text ?? "",
						timestamp = message.creationDate.ToUnixTimeMilliseconds(),
					});
					lastMessage = null;
				}
			}

			backup.name = chat.name ?? ChatInstance.DefaultName;
			backup.createdAt = chat.creationDate.ToUnixTimeMilliseconds();
			backup.updatedAt = chat.updateDate.ToUnixTimeMilliseconds();
			backup.chat.items = lsEntries.ToArray();
			return backup;
		}

		public BackupData.Chat ToChat()
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

			return new BackupData.Chat() {
				name = name ?? ChatInstance.DefaultName,
				creationDate = createdAt.HasValue ? DateTimeExtensions.FromUnixTime(createdAt.Value) : DateTime.Now,
				updateDate = updatedAt.HasValue ? DateTimeExtensions.FromUnixTime(updatedAt.Value) : DateTime.Now,
				history = new ChatHistory() {
					messages = messages.ToArray(),
				},
			};
		}

		public static BackyardChatBackupV2 FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				IList<string> errorMessages;
				if (jObject.IsValid(_schema, out errorMessages))
				{
					var card = JsonConvert.DeserializeObject<BackyardChatBackupV2>(json);
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
