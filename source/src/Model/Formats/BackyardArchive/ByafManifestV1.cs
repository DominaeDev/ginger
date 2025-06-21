using Ginger.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;

namespace Ginger
{
	public class ByafManifestV1
	{
		private static JsonSchema _schema;

		static ByafManifestV1()
		{
//			JsonSchemaGenerator generator = new JsonSchemaGenerator();
//			JsonSchema schema = generator.Generate(typeof(ByafManifestV1));
//			string jsonSchema = schema.ToString();

			_schema = JsonSchema.Parse(Resources.backyard_archive_manifest_v1_schema);
		}
		
		[JsonProperty("$schema", NullValueHandling = NullValueHandling.Ignore, Order = -100)]
		public const string schemaUri = "https://backyard.ai/schemas/byaf-manifest.schema.json";

		[JsonProperty("schemaVersion", Required = Required.Always, Order = -2)]
		public int version = 1;

		[JsonProperty("createdAt", Required = Required.Always, Order = -1)]
		public string creationDate { get; set; }

		[JsonProperty("characters", Required = Required.Always)]
		public string[] characters = new string[0];

		[JsonProperty("scenarios", Required = Required.Always)]
		public string[] scenarios = new string[0];

		[JsonProperty("author", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public Author author { get; set; }

		public ByafManifestV1()
		{
			creationDate = DateTime.UtcNow.ToISO8601();
		}

		public class Author
		{
			public string name;
			public string backyardURL;
		}

		public static ByafManifestV1 FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_schema))
				{
					var card = JsonConvert.DeserializeObject<ByafManifestV1>(json);
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
