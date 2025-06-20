using Ginger.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Linq;

namespace Ginger
{
	public class ByafCharacterV1
	{
		private static JsonSchema _schema;

		static ByafCharacterV1()
		{
//			JsonSchemaGenerator generator = new JsonSchemaGenerator();
//			JsonSchema schema = generator.Generate(typeof(ByafCharacterV1));
//			string jsonSchema = schema.ToString();

			_schema = JsonSchema.Parse(Resources.backyard_archive_character_v1_schema);
		}
		
		[JsonProperty("$schema", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public const string schemaUri = "https://backyard.ai/schemas/byaf-character.schema.json";

		[JsonProperty("schemaVersion", Required = Required.Always)]
		public int version = 1;

		[JsonProperty("id", Required = Required.Always)]
		public string id { get; set; }

		[JsonProperty("name", Required = Required.Always)]
		public string name { get; set; }

		[JsonProperty("displayName", Required = Required.Always)]
		public string displayName { get; set; }

		[JsonProperty("isNSFW", Required = Required.Always)]
		public bool isNSFW { get; set; }

		[JsonProperty("persona")]
		public string persona { get; set; }

		[JsonProperty("createdAt")]
		public string creationDate { get; set; }

		[JsonProperty("updatedAt")]
		public string updateDate { get; set; }

		[JsonProperty("loreItems", Required = Required.Always)]
		public LorebookItem[] loreItems = new LorebookItem[0];

		[JsonProperty("images", Required = Required.Always)]
		public Image[] images = new Image[0];
		
		public class LorebookItem
		{
			[JsonProperty("key", Required = Required.Always)]
			public string key;

			[JsonProperty("value", Required = Required.Always)]
			public string value;
		}
		
		public class Image
		{
			[JsonProperty("path", Required = Required.Always)]
			public string path;

			[JsonProperty("label", Required = Required.Always)]
			public string label;
		}

		public static ByafCharacterV1 FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_schema))
				{
					var card = JsonConvert.DeserializeObject<ByafCharacterV1>(json);
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

		public static ByafCharacterV1 FromFaradayCard(FaradayCardV4 card)
		{
			var character = new ByafCharacterV1() {
				id = card.data.id,
				creationDate = card.data.creationDate,
				updateDate = card.data.updateDate,
				name = card.data.name,
				displayName = card.data.displayName,
				persona = card.data.persona,
				isNSFW = card.data.isNSFW,
			};

			character.loreItems = card.data.loreItems
				.Select(i => new LorebookItem() {
					key = i.key,
					value = i.value,
				})
				.ToArray();
			return character;
		}
	}
}
