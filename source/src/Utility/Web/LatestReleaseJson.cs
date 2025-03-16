using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Ginger.Properties;

namespace Ginger
{
	public class LatestReleaseJson
	{
		private static JsonSchema _schema;

		[JsonProperty("name", Required = Required.Always)]
		public string name;

		[JsonProperty("tag_name", Required = Required.Always)]
		public string tag;

		[JsonProperty("html_url", Required = Required.Always)]
		public string url;

		static LatestReleaseJson()
		{
			_schema = JsonSchema.Parse(Resources.github_rest_latest_release_schema);
		}

		public static LatestReleaseJson FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_schema))
				{
					var data = JsonConvert.DeserializeObject<LatestReleaseJson>(json);
					return data;
				}
			}
			catch
			{
			}
			return null;
		}

		public VersionNumber ParseVersion()
		{
			// Version from release tag
			VersionNumber version = VersionNumber.ParseInside(tag);
			if (version.isDefined)
				return version;
			
			// Version from release name
			version = VersionNumber.ParseInside(name);
			if (version.isDefined)
				return version;

			return VersionNumber.Zero;
		}
	}
}
