using Ginger.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Ginger
{
	public class UserData
	{
		private static JsonSchema _schema;

		static UserData()
		{
//			JsonSchemaGenerator generator = new JsonSchemaGenerator();
//			JsonSchema schema = generator.Generate(typeof(UserData));
//			string json = schema.ToString();
			_schema = JsonSchema.Parse(Resources.ginger_user_data_schema_v1_schema);
		}

		[JsonProperty("name", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public string name;

		[JsonProperty("persona", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public string persona;

		[JsonProperty("image", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
		public string image = null;

		public static UserData FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_schema))
				{
					var userData = JsonConvert.DeserializeObject<UserData>(json);
					return userData;
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
