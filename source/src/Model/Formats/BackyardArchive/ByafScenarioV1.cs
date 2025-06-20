using Ginger.Integration;
using Ginger.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;

namespace Ginger
{
	public class ByafScenarioV1
	{
		private static JsonSchema _schema;

		static ByafScenarioV1()
		{
//			JsonSchemaGenerator generator = new JsonSchemaGenerator();
//			JsonSchema schema = generator.Generate(typeof(ByafScenarioV1));
//			string jsonSchema = schema.ToString();

			_schema = JsonSchema.Parse(Resources.backyard_archive_scenario_v1_schema);
		}
		
		[JsonProperty("$schema", NullValueHandling = NullValueHandling.Ignore)]
		public const string schemaUri = "https://backyard.ai/schemas/byaf-scenario.schema.json";

		[JsonProperty("schemaVersion", Required = Required.Always)]
		public int version = 1;

		[JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
		public string title { get; set; }

		// Scenario settings
		[JsonProperty("backgroundImage", NullValueHandling = NullValueHandling.Ignore)]
		public string backgroundImage { get; set; }

		[JsonProperty("formattingInstructions", Required = Required.Always)]
		public string formattingInstructions { get; set; }

		[JsonProperty("narrative", Required = Required.Always)]
		public string narrative;

		[JsonProperty("firstMessages")]
		public CharacterText[] greetings = new CharacterText[0]; // n = 1

		[JsonProperty("exampleMessages")]
		public CharacterText[] exampleMessages = new CharacterText[0];

		[JsonProperty("canDeleteExampleMessages")]
		public bool pruneExampleChat = true;

		[JsonProperty("grammar", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Include)]
		public string grammar { get; set; }

		// Model settings
		[JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)]
		public string model { get; set; }

		[JsonProperty("minP", Required = Required.Always)]
		public decimal minP = 0.1m;

		[JsonProperty("minPEnabled", Required = Required.Always)]
		public bool minPEnabled = true;

		[JsonProperty("repeatLastN", Required = Required.Always)]
		public int repeatLastN = 256;

		[JsonProperty("repeatPenalty", Required = Required.Always)]
		public decimal repeatPenalty = 1.05m;

		[JsonProperty("temperature", Required = Required.Always)]
		public decimal temperature = 1.2m;

		[JsonProperty("topK", Required = Required.Always)]
		public int topK = 30;

		[JsonProperty("topP", Required = Required.Always)]
		public decimal topP = 0.9m;

		[JsonProperty("promptTemplate", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Include)]
		public string promptTemplate = null; // (null | "general" | "ChatML" | "Llama3" | "Gemma2" | "CommandR" | "MistralInstruct")

		// Chat messages
		[JsonProperty("messages", Required = Required.Always)]
		public Message[] messages = new Message[0];

		[JsonConverter(typeof(JsonMessageConverter))]
		public class Message
		{
			public enum MsgType { Undefined, Human, AI }
			public MsgType type = MsgType.Undefined;

			public Output[] outputs = new Output[0];

			public class Output
			{
				public string text;
				public string creationDate { get; set; }
				public string updateDate { get; set; }
				public string activeDate { get; set; }
			}
		}

		public class CharacterText
		{
			[JsonProperty("characterID")]
			public string characterID;
			[JsonProperty("text")]
			public string text;
		}

		public static ByafScenarioV1 FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_schema))
				{
					var card = JsonConvert.DeserializeObject<ByafScenarioV1>(json);
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
			catch (Exception e)
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

		private class JsonMessageConverter : JsonConverter
		{
			public override bool CanConvert(Type objectType)
			{
				return objectType == typeof(Message);
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				JObject jObj = JObject.Load(reader);

				bool isAI = false;
				bool isHuman = false;
				foreach (JProperty jProp in (JToken)jObj)
				{
					if (jProp.Name.ToString() == "type")
					{
						if (jProp.Value.ToString() == "ai")
						{
							isAI = true;
							break;
						}
						else if (jProp.Value.ToString() == "human")
						{
							isHuman = true;
							break;
						}
					}
				}

				if (isHuman)
				{
					string text = null;
					string createdAt = null;
					string updatedAt = null;

					foreach (JProperty jProp in (JToken)jObj)
					{
						switch (jProp.Name.ToString())
						{
						case "text": text = jProp.Value.ToString(); break;
						case "createdAt": createdAt = jProp.Value.ToString(); break;
						case "updatedAt": updatedAt = jProp.Value.ToString(); break;
						}
					}
					if (text != null && createdAt != null && updatedAt != null)
					{
						return new Message() {
							type = Message.MsgType.Human,
							outputs = new Message.Output[] {
								new Message.Output {
									text = text,
									creationDate = createdAt,
									updateDate = updatedAt,
									activeDate = updatedAt,
								},
							},
						};
					}
				}
				else if (isAI)
				{
					return jObj.ToObject<Message>(); //?
				}

				throw new JsonException("Invalid message");
			}

			public override bool CanWrite
			{
				get { return true; }
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				Message message = (Message)value;
				writer.WriteStartObject();
				writer.WritePropertyName("type");
				writer.WriteValue(message.type);

				if (message.type == Message.MsgType.Human && message.outputs != null && message.outputs.Length > 0)
				{
					writer.WritePropertyName("text");
					writer.WriteValue(message.outputs[0].text);
					writer.WritePropertyName("createdAt");
					writer.WriteValue(message.outputs[0].creationDate);
					writer.WritePropertyName("updatedAt");
					writer.WriteValue(message.outputs[0].creationDate);
				}
				else if (message.type == Message.MsgType.AI)
				{
					writer.WritePropertyName("output");
					writer.WriteStartArray();
					foreach (var o in message.outputs)
					{
						writer.WriteStartObject();
						writer.WritePropertyName("text");
						writer.WriteValue(o.text);
						writer.WritePropertyName("createdAt");
						writer.WriteValue(o.creationDate);
						writer.WritePropertyName("updatedAt");
						writer.WriteValue(o.creationDate);
						writer.WritePropertyName("activeTimestamp");
						writer.WriteValue(o.activeDate);
						writer.WriteEndObject();
					}
					writer.WriteEndArray();
				}
				else
				{
					writer.WriteNull();
				}

				writer.WriteEndObject();
			}
		}

		public static ByafScenarioV1 FromChat(BackupData.Chat chat)
		{
			var scenario = new ByafScenarioV1() {
				title = chat.name,
				// Staging
				formattingInstructions = chat.staging.system,
				narrative = chat.staging.scenario,
				pruneExampleChat = chat.staging.pruneExampleChat,
				promptTemplate = chat.parameters.promptTemplate,
				grammar = chat.staging.grammar,
				// Parameters
				model = chat.parameters.model,
				minP = chat.parameters.minP,
				minPEnabled = chat.parameters.minPEnabled,
				temperature = chat.parameters.temperature,
				repeatLastN = chat.parameters.repeatLastN,
				repeatPenalty = chat.parameters.repeatPenalty,
				topK = chat.parameters.topK,
				topP = chat.parameters.topP,
			};


			return scenario;
		}
	}
}
