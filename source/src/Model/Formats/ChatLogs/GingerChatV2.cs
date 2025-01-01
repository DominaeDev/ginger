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
	public class GingerChatV2
	{
		private static JsonSchema _schema;

		static GingerChatV2()
		{
//			JsonSchemaGenerator generator = new JsonSchemaGenerator();
//			JsonSchema schema = generator.Generate(typeof(GingerChat));
//			string json = schema.ToString();
			_schema = JsonSchema.Parse(Resources.ginger_chat_v2_schema);
		}

		[JsonProperty("title", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public string title;

		[JsonProperty("createdAt", Required = Required.Default)]
		public long createdAt = 0;

		[JsonProperty("users", Required = Required.Always)]
		[JsonConverter(typeof(JsonSpeakerListConverter))]
		public SpeakerList speakers = new SpeakerList();
		
		[JsonProperty("staging", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public Staging staging = null;

		[JsonProperty("parameters", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public Parameters parameters = null;

		[JsonProperty("background", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public string backgroundName = null;

		public class SpeakerList : List<Speaker>
		{
			public Speaker this[string id]
			{
				get
				{
					for (int i = 0; i < this.Count; ++i)
					{
						if (this[i].id == id)
							return this[i];
					}
					return default(Speaker);
				}
			}
		}

		public class Message
		{
			[JsonProperty("user", Required = Required.Always)]
			public string speakerId;
			[JsonProperty("text", Required = Required.Always)]
			public string text;
			[JsonProperty("timestamp", Required = Required.Default)]
			public long timestamp = 0;
			[JsonProperty("alt-texts", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
			public string[] regens = null;
		}

		[JsonProperty("messages", Required = Required.Always)]
		public Message[] messages;

		public struct Speaker
		{
			public string id;
			public string name;
		}

		public class Staging
		{
			[JsonProperty("system", Required = Required.AllowNull)]
			public string system = null;

			[JsonProperty("greeting", Required = Required.AllowNull)]
			public string greeting = null;

			[JsonProperty("scenario", Required = Required.AllowNull)]
			public string scenario = null;

			[JsonProperty("example", Required = Required.AllowNull)]
			public string example = null;

			[JsonProperty("grammar", Required = Required.AllowNull)]
			public string grammar = null;

			[JsonProperty("authorNote", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
			public string authorNote = "";

			[JsonProperty("pruneExampleChat")]
			public bool pruneExampleChat = true;

			[JsonProperty("ttsAutoPlay")]
			public bool ttsAutoPlay = false;

			[JsonProperty("ttsInputFilter", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
			public string ttsInputFilter = null;
		}

		public class Parameters
		{
			[JsonProperty("model", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
			public string model = "";

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

			[JsonProperty("promptTemplate", Required = Required.Default, NullValueHandling = NullValueHandling.Include)]
			public string promptTemplate = null;
		}

		public static GingerChatV2 FromChat(ChatInstance chatInstance, SpeakerList speakers)
		{
			if (speakers == null || chatInstance == null)
				return null;

			var lsMessages = new List<Message>(chatInstance.history.count);

			foreach (var message in chatInstance.history.messages) // Include greeting
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

			return new GingerChatV2() {
				title = chatInstance.name,
				speakers = speakers,
				createdAt = chatInstance.creationDate.ToUnixTimeMilliseconds(),
				messages = lsMessages.ToArray(),
			};
		}

		public static GingerChatV2 FromBackup(BackupData.Chat backup)
		{
			if (backup == null)
				return null;

			var speakers = new SpeakerList();
			for (int i = 0; i < backup.participants.Length; ++i)
			{
				speakers.Add(new Speaker() {
					id = i.ToString(),
					name = backup.participants[i],
				});
			}

			var lsMessages = new List<Message>(backup.history.count);

			foreach (var message in backup.history.messages) // Include greeting
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

			var chat = new GingerChatV2() {
				title = backup.name,
				speakers = speakers,
				createdAt = backup.creationDate.ToUnixTimeMilliseconds(),
				messages = lsMessages.ToArray(),
			};
			chat.backgroundName = backup.backgroundName;
			if (backup.staging != null)
			{
				chat.staging = new Staging() {
					system = backup.staging.system ?? "",
					scenario = backup.staging.scenario ?? "",
					greeting = backup.staging.greeting ?? "",
					example = backup.staging.example ?? "",
					grammar = backup.staging.grammar ?? "",
					authorNote = backup.staging.authorNote ?? "",
					pruneExampleChat = backup.staging.pruneExampleChat,
					ttsAutoPlay = backup.staging.ttsAutoPlay,
					ttsInputFilter = backup.staging.ttsInputFilter ?? "default",
				};
			}
			if (backup.parameters != null)
			{
				chat.parameters = new Parameters() {
					model = backup.parameters.model ?? Backyard.DefaultModel,
					temperature = backup.parameters.temperature,
					topP = backup.parameters.topP,
					minP = backup.parameters.minP,
					topK = backup.parameters.topK,
					minPEnabled = backup.parameters.minPEnabled,
					repeatLastN = backup.parameters.repeatLastN,
					repeatPenalty = backup.parameters.repeatPenalty,
					promptTemplate = backup.parameters.promptTemplate,
				};
			}
			return chat;
		}

		public ChatHistory ToChat()
		{
			int index = 0;
			var indexById = speakers.ToDictionary(s => s.id, s => index++);

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

				for (int i = 0; i < swipes.Length; ++i)
					Anonymize(ref swipes[i]);

				result.Add(new ChatHistory.Message() {
					speaker = speakerIdx,
					creationDate = timestamp,
					updateDate = timestamp,
					activeSwipe = activeSwipe,
					swipes = swipes,
				});
			}

			return new ChatHistory() {
				name = Utility.FirstNonEmpty(this.title, ChatInstance.DefaultName),
				messages = result.ToArray(),
			};
		}

		public BackupData.Chat ToBackupChat()
		{
			var chat = new BackupData.Chat() {
				name = title ?? ChatInstance.DefaultName,
				creationDate = DateTimeExtensions.FromUnixTime(createdAt),
				updateDate = DateTimeExtensions.FromUnixTime(createdAt),
				backgroundName = backgroundName,
				history = ToChat(),
			};

			if (this.staging != null)
			{
				chat.staging = new ChatStaging() {
					system = this.staging.system ?? "",
					scenario = this.staging.scenario ?? "",
					greeting = this.staging.greeting ?? "",
					example = this.staging.example ?? "",
					grammar = this.staging.grammar ?? "",
					pruneExampleChat = this.staging.pruneExampleChat,
					authorNote = this.staging.authorNote ?? "",
					ttsAutoPlay = this.staging.ttsAutoPlay,
					ttsInputFilter = this.staging.ttsInputFilter ?? "default",
				};
			}

			if (this.parameters != null)
			{
				chat.parameters = new ChatParameters() {
					model = this.parameters.model ?? Backyard.DefaultModel,
					temperature = this.parameters.temperature,
					topP = this.parameters.topP,
					minP = this.parameters.minP,
					minPEnabled = this.parameters.minPEnabled,
					topK = this.parameters.topK,
					repeatPenalty = this.parameters.repeatPenalty,
					repeatLastN = this.parameters.repeatLastN,
					promptTemplate = this.parameters.promptTemplate,
				};
			}

			return chat;
		}

		private void Anonymize(ref string text)
		{
			if (speakers == null || speakers.Count == 0)
				return;

			StringBuilder sb = new StringBuilder(text);
			if (speakers.Count > 0)
				Utility.ReplaceWholeWord(sb, speakers[0].name, GingerString.UserMarker, StringComparison.Ordinal);
			if (speakers.Count > 1)
				Utility.ReplaceWholeWord(sb, speakers[1].name, GingerString.CharacterMarker, StringComparison.Ordinal);
			text = sb.ToString();
		}

		public static GingerChatV2 FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_schema))
				{
					var chat = JsonConvert.DeserializeObject<GingerChatV2>(json);
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

		private class JsonSpeakerListConverter : JsonConverter
		{
			public override bool CanConvert(Type objectType)
			{
				return objectType == typeof(SpeakerList);
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				SpeakerList list = new SpeakerList();
				JObject jObj = JObject.Load(reader);

				foreach (JProperty jProp in (JToken)jObj)
				{
					list.Add(new Speaker() {
						id = jProp.Name.ToString(),
						name = jProp.Value.ToString(),
					});
				}

				return list;
			}

			public override bool CanWrite
			{
				get { return true; }
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				SpeakerList speakers = (SpeakerList)value;
				writer.WriteStartObject();
				foreach (var speaker in speakers)
				{
					writer.WritePropertyName(speaker.id);
					writer.WriteValue(speaker.name);
				}
				writer.WriteEndObject();
			}
		}
	}
}
