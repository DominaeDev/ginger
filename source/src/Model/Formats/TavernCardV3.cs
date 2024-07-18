using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Schema;
using Ginger.Properties;
using Newtonsoft.Json.Linq;

namespace Ginger
{
	public class TavernCardV3
	{
		private static JsonSchema _tavernCharacterCardSchemaV3;

		static TavernCardV3()
		{
//			JsonSchemaGenerator generator = new JsonSchemaGenerator();
//			JsonSchema schema = generator.Generate(typeof(TavernLorebookV3_Export));
//			string jsonSchema = schema.ToString();

			_tavernCharacterCardSchemaV3 = JsonSchema.Parse(Resources.tavern_charactercard_v3_schema);
		}

		public TavernCardV3()
		{
			data = new Data();
		}

		[JsonProperty("data", Required = Required.Always)]
		public Data data;
		[JsonProperty("spec", Required = Required.Always)]
		string spec = null; // "chara_card_v3"
		[JsonProperty("spec_version", Required = Required.Always)]
#pragma warning disable 0414 // Remove unread private members
		string spec_version = null;
#pragma warning restore 0414 // Remove unread private members

		public class Data
		{
			// Same as V2
			[JsonProperty("name", Required = Required.Always)]
			public string name = "";

			[JsonProperty("description")]
			public string persona = "";

			[JsonProperty("personality")]
			public string personality = "";

			[JsonProperty("scenario")]
			public string scenario = "";

			[JsonProperty("first_mes")]
			public string greeting = "";

			[JsonProperty("mes_example")]
			public string example = "";

			[JsonProperty("system_prompt")]
			public string system = "";

			[JsonProperty("creator_notes")]
			public string creator_notes = "";

			[JsonProperty("post_history_instructions")]
			public string post_history_instructions = "";

			[JsonProperty("alternate_greetings")]
			public string[] alternate_greetings = new string[0];

			[JsonProperty("character_book")]
			public TavernCardV3.CharacterBook character_book = null; // New v3 spec

			[JsonProperty("tags")]
			public string[] tags = new string[0];

			[JsonProperty("creator")]
			public string creator = "";

			[JsonProperty("character_version")]
			public string character_version = "";

			[JsonProperty("extensions")]
			public GingerJsonExtensionData extensions = new GingerJsonExtensionData();

			// New in V3
			[JsonProperty("nickname")]
			public string nickname = "";
			[JsonProperty("creator_notes_multilingual")]
			public Dictionary<string, string> creator_notes_multilingual = new Dictionary<string, string>();
			[JsonProperty("source")]
			public string source = "";
			[JsonProperty("group_only_greetings")]
			public string[] alternate_greetings_for_groups = new string[0];

			[JsonProperty("creation_date")]
			public int? creationDate = null;
			[JsonProperty("modification_date")]
			public int? updateDate = null;

			public class Asset
			{
				[JsonProperty("name", Required = Required.Always)]
				public string name = "";
				[JsonProperty("type", Required = Required.Always)]
				public string type = "";
				[JsonProperty("uri", Required = Required.Always)]
				public string uri = "";
				[JsonProperty("ext")]
				public string ext = "";
			}

			[JsonProperty("assets")]
			public Asset[] assets = null;
		}

		// Lorebook (v3)
		public class CharacterBook
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
			public Entry[] entries = new Entry[0];

			[JsonProperty("extensions")]
			public JsonExtensionData extensions = new JsonExtensionData();

			public class Entry
			{
				[JsonProperty("id")]
				public int id;

				[JsonProperty("keys")]
				public string[] keys;

				[JsonProperty("secondary_keys")]
				public string[] secondary_keys = new string[0];

				[JsonProperty("comment")]
				public string comment = "";

				[JsonProperty("content", Required = Required.Always)]
				public string content;

				[JsonProperty("constant")]
				public bool constant = false;

				[JsonProperty("selective")]
				public bool selective = false;

				[JsonProperty("insertion_order", Required = Required.Always)]
				public int insertion_order = 100;

				[JsonProperty("enabled", Required = Required.Always)]
				public bool enabled = true;

				[JsonProperty("position")]
				public string position = "before_char"; // 'before_char' | 'after_char'

				[JsonProperty("case_sensitive")]
				public bool case_sensitive = false;

				[JsonProperty("name")]
				public string name = "";

				[JsonProperty("priority")]
				public int priority = 10;

				[JsonProperty("extensions")]
				public JsonExtensionData extensions = new JsonExtensionData();

				// New in V3
				[JsonProperty("use_regex")]
				public bool use_regex = false;
			}
		}

		private static TavernCardV3 FromV1(TavernCardV1 card)
		{
			return new TavernCardV3() {
				data = new Data() {
					name = card.name,
					persona = card.description,
					personality = card.personality,
					scenario = card.scenario,
					greeting = card.greeting,
					example = card.example,
				}
			};
		}

		private static TavernCardV3 FromV2(TavernCardV2 cardV2)
		{
			TavernCardV3 card = new TavernCardV3();
			card.spec = "chara_card_v3";
			card.spec_version = "3.0";
			card.data.name = cardV2.data.name;
			card.data.creator = cardV2.data.creator;
			card.data.creator_notes = cardV2.data.creator_notes;
			card.data.character_version = cardV2.data.character_version;
			card.data.tags = cardV2.data.tags != null ? (string[])cardV2.data.tags.Clone() : new string[0];

			card.data.extensions = (GingerJsonExtensionData)cardV2.data.extensions.Clone();

			card.data.system = cardV2.data.system;
			card.data.post_history_instructions = cardV2.data.post_history_instructions;
			card.data.persona = cardV2.data.persona;
			card.data.personality = cardV2.data.personality;
			card.data.scenario = cardV2.data.scenario;
			card.data.greeting = cardV2.data.greeting;
			card.data.example = cardV2.data.example;
			card.data.alternate_greetings = cardV2.data.alternate_greetings != null ? (string[])cardV2.data.alternate_greetings.Clone() : new string[0];

			card.data.character_book = null; //!!
			if (cardV2.data.character_book != null)
			{
				card.data.character_book = new TavernCardV3.CharacterBook();

				card.data.character_book.recursive_scanning = cardV2.data.character_book.recursive_scanning;
				card.data.character_book.scan_depth = cardV2.data.character_book.scan_depth;
				card.data.character_book.token_budget = cardV2.data.character_book.token_budget;
				card.data.character_book.extensions = cardV2.data.character_book.extensions;
				
				card.data.character_book.entries =
					cardV2.data.character_book.entries.Select(e => {
						string[] keys = e.keys;
						var entry = new TavernCardV3.CharacterBook.Entry() {
							id = e.id,
							comment = keys.Length > 0 ? keys[0] : "",
							keys = keys,
							content = e.content,
							case_sensitive = e.case_sensitive,
							constant = e.constant,
							enabled = e.enabled,
							insertion_order = e.insertion_order,
							position = e.position,
							priority = e.priority,
							secondary_keys = e.secondary_keys,
							selective = e.selective,
							extensions = e.extensions ?? new JsonExtensionData(),
							use_regex = false,
						};
						return entry;
					}).ToArray();
			}

			return card;
		}

		public static TavernCardV3 FromOutput(Generator.Output output)
		{
			TavernCardV3 card = new TavernCardV3();
			card.spec = "chara_card_v3";
			card.spec_version = "3.0";
			card.data.name = Current.Name;
			card.data.creator = Current.Card.creator;
			card.data.creator_notes = Current.Card.comment.ConvertLinebreaks(Linebreak.LF);
			card.data.character_version = Current.Card.versionString;
			card.data.tags = Current.Card.tags.ToArray();

			if (Current.Card.extensionData != null)
				card.data.extensions = Current.Card.extensionData.WithGinger();

			card.data.system = output.system.ToTavern();
			card.data.post_history_instructions = output.system_post_history.ToTavern();
			card.data.persona = output.persona.ToTavern();
			card.data.personality = output.personality.ToTavern();
			card.data.scenario = output.scenario.ToTavern();
			card.data.greeting = output.greeting.ToTavern();
			card.data.example = output.example.ToTavernChat();
			card.data.alternate_greetings = output.alternativeGreetings.Select(s => s.ToTavern()).ToArray();

			// Append user persona
			string userPersona = output.userPersona.ToTavern();
			if (string.IsNullOrEmpty(userPersona) == false)
				card.data.persona = string.Concat(userPersona, "\n\n", card.data.persona).Trim();

			if (output.hasLore)
			{
				card.data.character_book = new TavernCardV3.CharacterBook();

				if (output.lorebook.unused != null)
				{
					card.data.character_book.recursive_scanning = output.lorebook.unused.recursive_scanning;
					card.data.character_book.scan_depth = output.lorebook.unused.scan_depth;
					card.data.character_book.token_budget = output.lorebook.unused.token_budget;
					card.data.character_book.extensions = output.lorebook.unused.extensions ?? new JsonExtensionData();
				}

				card.data.character_book.entries =
					output.lorebook.entries.Select(e => {
						string[] keys = e.keys;
						var entry = new TavernCardV3.CharacterBook.Entry() {
							comment = keys.Length > 0 ? keys[0] : "",
							keys = keys,
							content = GingerString.FromString(e.value).ToTavern(),
						};
						if (e.unused != null)
						{
							entry.case_sensitive = e.unused.case_sensitive;
							entry.comment = e.unused.comment;
							entry.constant = e.unused.constant;
							entry.enabled = e.unused.enabled;
							entry.insertion_order = e.unused.insertion_order;
							entry.position = e.unused.placement;
							entry.priority = e.unused.priority;
							entry.secondary_keys = e.unused.secondary_keys;
							entry.selective = e.unused.selective;
							entry.extensions = e.unused.extensions ?? new JsonExtensionData();
							entry.use_regex = false;
						}
						return entry;
					}).ToArray();

				for (int i = 0; i < card.data.character_book.entries.Length; ++i)
					card.data.character_book.entries[i].id = i + 1;
			}

			return card;
		}

		public static TavernCardV3 FromJson(string json, out int errors)
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

			// Version 3
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_tavernCharacterCardSchemaV3))
				{
					var card = JsonConvert.DeserializeObject<TavernCardV3>(json, settings);
					errors = lsErrors.Count;
					if (Validate(card))
						return card;
				}
			}
			catch
			{
			}

			errors = 0;
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

		private static bool Validate(TavernCardV3 card)
		{
			if (card.data == null)
				return false;
			if (string.IsNullOrEmpty(card.data.name))
				return false;
			return card.spec == "chara_card_v3";
		}

	}

	public class TavernLorebookV3
	{
		[JsonProperty("spec", Required = Required.Always)]
		public string spec = null; //"lorebook_v3";
		[JsonProperty("data", Required = Required.Always)]
		public TavernCardV3.CharacterBook data;

		private static JsonSchema _tavernLorebookSchemaV3;

		static TavernLorebookV3()
		{
			_tavernLorebookSchemaV3 = JsonSchema.Parse(Resources.tavern_characterbook_v3_schema);
		}

		public static TavernLorebookV3 FromJson(string json)
		{
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_tavernLorebookSchemaV3))
				{
					var lorebook = JsonConvert.DeserializeObject<TavernLorebookV3>(json);
					if (Validate(lorebook))
						return lorebook;
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

		private static bool Validate(TavernLorebookV3 lorebook)
		{
			if (lorebook.data == null)
				return false;
			return lorebook.spec == "lorebook_v3";
		}
	}


}
