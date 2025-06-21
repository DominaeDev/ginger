using Ginger.Integration;
using Ginger.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;

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
		
		[JsonProperty("$schema", NullValueHandling = NullValueHandling.Ignore, Order = -100)]
		public const string schemaUri = "https://backyard.ai/schemas/byaf-scenario.schema.json";

		[JsonProperty("schemaVersion", Required = Required.Always, Order = -1)]
		public int version = 1;

		[JsonProperty("title", NullValueHandling = NullValueHandling.Ignore, Order = 0)]
		public string title { get; set; }

		// Scenario settings
		[JsonProperty("narrative", Required = Required.Always, Order = 10)]
		public string narrative;

		[JsonProperty("formattingInstructions", Required = Required.Always, Order = 11)]
		public string formattingInstructions { get; set; }

		[JsonProperty("firstMessages", Order = 12)]
		public CharacterText[] greetings = new CharacterText[0]; // n = 1

		[JsonProperty("exampleMessages", Order = 13)]
		public CharacterText[] exampleMessages = new CharacterText[0];

		[JsonProperty("canDeleteExampleMessages", Order = 14)]
		public bool pruneExampleChat = true;

		[JsonProperty("backgroundImage", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore, Order = 15)]
		public string backgroundImage { get; set; }

		[JsonProperty("grammar", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Include, Order = 16)]
		public string grammar { get; set; }

		// Model settings
		[JsonProperty("model", NullValueHandling = NullValueHandling.Ignore, Order = 50)]
		public string model { get; set; }

		[JsonProperty("promptTemplate", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Include, Order = 51)]
		public string promptTemplate = null; // (null | "general" | "ChatML" | "Llama3" | "Gemma2" | "CommandR" | "MistralInstruct")

		[JsonProperty("minP", Required = Required.Always, Order = 60)]
		public decimal minP = 0.1m;

		[JsonProperty("minPEnabled", Required = Required.Always, Order = 61)]
		public bool minPEnabled = true;

		[JsonProperty("temperature", Required = Required.Always, Order = 62)]
		public decimal temperature = 1.2m;

		[JsonProperty("topK", Required = Required.Always, Order = 63)]
		public int topK = 30;

		[JsonProperty("topP", Required = Required.Always, Order = 64)]
		public decimal topP = 0.9m;

		[JsonProperty("repeatLastN", Required = Required.Always, Order = 65)]
		public int repeatLastN = 256;

		[JsonProperty("repeatPenalty", Required = Required.Always, Order = 66)]
		public decimal repeatPenalty = 1.05m;

		// Chat messages
		[JsonProperty("messages", Required = Required.Always, Order = 100)]
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

			// Greeting
			if (chat.staging.greeting.IsEmpty() == false)
			{
				scenario.greetings = new CharacterText[] {
					new CharacterText() {
						characterID = "character1",
						text = chat.staging.greeting.text,
					}
				};
			}

			// Example messages
			if (chat.staging.exampleMessages.IsEmpty() == false)
			{
				scenario.exampleMessages = new CharacterText[] {
					new CharacterText() {
						characterID = "character1",
						text = chat.staging.example,
					}
				};
			}

			return scenario;
		}
	}
}
