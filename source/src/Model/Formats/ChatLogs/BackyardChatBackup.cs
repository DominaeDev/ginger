﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Ginger.Properties;
using Ginger.Integration;

namespace Ginger
{
	public class BackyardChatBackup
	{
		private static JsonSchema _schema;

		static BackyardChatBackup()
		{
			_schema = JsonSchema.Parse(Resources.backup_chat_schema);
		}

		[JsonProperty("name", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
		public string name = null;

		[JsonProperty("createdAt", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public long? createdAt;

		[JsonProperty("updatedAt", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public long? updatedAt;

		[JsonProperty("staging", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public Staging staging = null;

		[JsonProperty("parameters", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public Parameters parameters = null;

		// Same as Backyard chat
		[JsonProperty("chat", Required = Required.Always)]
		public Chat chat = new Chat();

		// Same as Backyard chat
		[JsonProperty("version", Required = Required.Always)]
		public int version = 2; // For Backyard compatibility

		public class Staging
		{
			[JsonProperty("system", Required = Required.DisallowNull)]
			public string system = null;

			[JsonProperty("greeting", Required = Required.DisallowNull)]
			public string greeting = null;

			[JsonProperty("scenario", Required = Required.DisallowNull)]
			public string scenario = null;

			[JsonProperty("example", Required = Required.DisallowNull)]
			public string example = null;

			[JsonProperty("grammar", Required = Required.DisallowNull)]
			public string grammar = null;
		}

		public class Parameters
		{
			[JsonProperty("model", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
			public string model = "";

			[JsonProperty("authorNote", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
			public string authorNote = "";

			[JsonProperty("temperature")]
			public decimal temperature = 1.2m;

			[JsonProperty("topP")]
			public decimal topP = 0.9m;

			[JsonProperty("minP")]
			public decimal minP = 0.1m;

			[JsonProperty("topK")]
			public int topK = 30;

			[JsonProperty("minPEnabled")]
			public bool minPEnabled = true;

			[JsonProperty("repeatLastN")]
			public int repeatLastN = 256;

			[JsonProperty("repeatPenalty")]
			public decimal repeatPenalty = 1.05m;

			[JsonProperty("promptTemplate", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Include)]
			public string promptTemplate = null;

			[JsonProperty("pruneExampleChat")]
			public bool pruneExampleChat = true;

			[JsonProperty("ttsAutoPlay")]
			public bool ttsAutoPlay = false;

			[JsonProperty("ttsInputFilter", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
			public string ttsInputFilter = null;
		}

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

		public static BackyardChatBackup FromChat(BackupData.Chat chat)
		{
			BackyardChatBackup backup = new BackyardChatBackup();
			var lsItems = new List<ChatItem>(chat.history.count / 2 + 1);

			foreach (var pair in chat.history.messagesWithoutGreeting.Pairwise())
			{
				lsItems.Add(new ChatItem() {
					input = pair.Item1.text ?? "",
					output = pair.Item2?.text ?? "",
					timestamp = pair.Item1.creationDate.ToUnixTimeMilliseconds(),
				});
			}

			backup.name = chat.name;
			backup.createdAt = chat.creationDate.ToUnixTimeMilliseconds();
			backup.updatedAt = chat.updateDate.ToUnixTimeMilliseconds();

			if (chat.staging != null)
			{
				backup.staging = new Staging() {
					system = chat.staging.system ?? "",
					scenario = chat.staging.scenario ?? "",
					greeting = chat.staging.greeting ?? "",
					example = chat.staging.example ?? "",
					grammar = chat.staging.grammar ?? "",
				};
			}
			if (chat.parameters != null)
			{
				backup.parameters = new Parameters() {
					model = chat.parameters.model ?? Backyard.DefaultModel,
					authorNote = chat.parameters.authorNote ?? "",
					temperature = chat.parameters.temperature,
					topP = chat.parameters.topP,
					minP = chat.parameters.minP,
					topK = chat.parameters.topK,
					minPEnabled = chat.parameters.minPEnabled,
					repeatLastN = chat.parameters.repeatLastN,
					repeatPenalty = chat.parameters.repeatPenalty,
					promptTemplate = chat.parameters.promptTemplate,
					pruneExampleChat = chat.parameters.pruneExampleChat,
					ttsAutoPlay = chat.parameters.ttsAutoPlay,
					ttsInputFilter = chat.parameters.ttsInputFilter ?? "default",
				};
			}
			backup.chat.items = lsItems.ToArray();
			return backup;
		}

		public BackupData.Chat ToChat()
		{
			var messages = new List<ChatHistory.Message>();
			foreach (var item in chat.items)
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

			ChatStaging staging = null;
			ChatParameters parameters = null;

			if (this.staging != null)
			{
				staging = new ChatStaging() {
					system = this.staging.system,
					scenario = this.staging.scenario,
					greeting = this.staging.greeting,
					example = this.staging.example,
					grammar = this.staging.grammar,
				};
			}

			if (this.parameters != null)
			{
				parameters = new ChatParameters() {
					model = this.parameters.model,
					temperature = this.parameters.temperature,
					topP = this.parameters.topP,
					minP = this.parameters.minP,
					minPEnabled = this.parameters.minPEnabled,
					topK = this.parameters.topK,
					repeatPenalty = this.parameters.repeatPenalty,
					repeatLastN = this.parameters.repeatLastN,
					promptTemplate = this.parameters.promptTemplate,
					pruneExampleChat = this.parameters.pruneExampleChat,
					authorNote = this.parameters.authorNote,
					ttsAutoPlay = this.parameters.ttsAutoPlay,
					ttsInputFilter = this.parameters.ttsInputFilter,
				};
			}

			return new BackupData.Chat() {
				name = name,
				creationDate = createdAt.HasValue ? DateTimeExtensions.FromUnixTime(createdAt.Value) : DateTime.Now,
				updateDate = updatedAt.HasValue ? DateTimeExtensions.FromUnixTime(updatedAt.Value) : DateTime.Now,
				staging = staging,
				parameters = parameters,
				history = new ChatHistory() {
					messages = messages.ToArray(),
				},
			};
		}

		public static BackyardChatBackup FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_schema))
				{
					var card = JsonConvert.DeserializeObject<BackyardChatBackup>(json);
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