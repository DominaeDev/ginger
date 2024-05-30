using Newtonsoft.Json;

namespace Ginger
{
	public class TavernCardV1
	{
		[JsonProperty("name", Required = Required.Always)]
		public string name = "";
		[JsonProperty("description", Required = Required.Always)]
		public string description = "";
		[JsonProperty("personality", Required = Required.Always)]
		public string personality = "";
		[JsonProperty("scenario", Required = Required.Always)]
		public string scenario = "";
		[JsonProperty("first_mes", Required = Required.Always)]
		public string greeting = "";
		[JsonProperty("mes_example", Required = Required.Always)]
		public string example = "";
	}
}
