using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Linq;
using Ginger.Properties;

namespace Ginger
{
	public class TavernWorldBook
	{
		[JsonProperty("name")]
		public string name;

		[JsonProperty("description")]
		public string description;

		[JsonProperty("scan_depth")]
		public int scan_depth = 50;

		[JsonProperty("token_budget")]
		public int token_budget = 500;

		[JsonProperty("recursive_scanning")]
		public bool recursive_scanning = false;

		[JsonProperty("entries", Required = Required.Always)]
		public Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

		[JsonProperty("extensions")]
		public SmallGingerJsonExtensionData extensions = new SmallGingerJsonExtensionData();

		public class Entry
		{
			[JsonProperty("uid")]
			public int uid;

			[JsonProperty("key", Required = Required.Always)]
			public string[] key;

			[JsonProperty("keysecondary")]
			public string[] secondary_keys = new string[0];

			[JsonProperty("comment")]
			public string comment = "";

			[JsonProperty("content", Required = Required.Always)]
			public string content;

			[JsonProperty("constant")]
			public bool constant = false;

			[JsonProperty("selective")]
			public bool selective = false;

			[JsonProperty("order")]
			public int order = 100;

			[JsonProperty("position")]
			public int position = 0; // 0: before_char 1: after_char 2: before_authors_note 3:after_authors note 4:at_depth

			[JsonProperty("excludeRecursion")]
			public bool excludeRecursion = false;

			[JsonProperty("disable")]
			public bool disable = false;

			[JsonProperty("addMemo")]
			public bool addMemo = true;

			[JsonProperty("displayIndex")]
			public int displayIndex;

			[JsonProperty("probability")]
			public int probability = 100;

			[JsonProperty("useProbability")]
			public bool useProbability = true;

			[JsonProperty("depth")]
			public int depth = 4;

			[JsonProperty("selectiveLogic")]
			public int selectiveLogic = 0;

			[JsonProperty("group")]
			public string group = "";

			[JsonProperty("extensions")]
			public JsonExtensionData extensions = new JsonExtensionData();
		}

		private static JsonSchema _tavernWorldBookSchema;
		private static JsonSchema _tavernWorldBookFilterSchema;

		static TavernWorldBook()
		{
			_tavernWorldBookSchema = JsonSchema.Parse(Resources.tavern_worldbook_schema);
			_tavernWorldBookFilterSchema = JsonSchema.Parse(Resources.tavern_worldbook_filter_schema);
		}

		public static TavernWorldBook FromJson(string json, out int errors)
		{
			List<string> lsErrors = new List<string>();
			JsonSerializerSettings settings = new JsonSerializerSettings() {
				Error = delegate (object sender, ErrorEventArgs args) {
					if (args.ErrorContext.Error.Message.Contains(".extensions")) // Inconsequential
					{
						args.ErrorContext.Handled = true;
						return;
					}
					if (args.ErrorContext.Error.Message.Contains("Required")) // Required field
						return; // Throw

					lsErrors.Add(args.ErrorContext.Error.Message);
					args.ErrorContext.Handled = true;
				},
			};

			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_tavernWorldBookFilterSchema))
				{
					IList<string> validationErrors;
					if (jObject.IsValid(_tavernWorldBookSchema, out validationErrors) == false)
						lsErrors.AddRange(validationErrors.Distinct());

					var lorebook = JsonConvert.DeserializeObject<TavernWorldBook>(json, settings);
					if (lorebook != null)
					{
						errors = lsErrors.Count;
						return lorebook;
					}

				}
				
			}
			catch
			{
			}

			errors = 0;
			return null;
		}

		public static bool Validate(string jsonData)
		{
			try
			{
				JObject jObject = JObject.Parse(jsonData);
				return jObject.IsValid(_tavernWorldBookFilterSchema);
			}
			catch
			{
				return false;
			}
		}
	}
}
