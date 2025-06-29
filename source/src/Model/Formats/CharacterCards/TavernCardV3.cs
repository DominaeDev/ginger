﻿using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Schema;
using Ginger.Properties;
using Newtonsoft.Json.Linq;
using System;
using System.Text;

namespace Ginger
{
	public class TavernCardV3
	{
		private static JsonSchema _tavernCharacterCardV3Schema;
		private static JsonSchema _tavernCharacterCardV3FilterSchema;

		static TavernCardV3()
		{
		//	JsonSchemaGenerator generator = new JsonSchemaGenerator();
		//	JsonSchema schema = generator.Generate(typeof(TavernCardV3));
		//	string jsonSchema = schema.ToString();

			_tavernCharacterCardV3Schema = JsonSchema.Parse(Resources.tavern_charactercard_v3_schema);
			_tavernCharacterCardV3FilterSchema = JsonSchema.Parse(Resources.tavern_charactercard_v3_filter_schema);
		}

		public TavernCardV3()
		{
			data = new Data();
		}

		[JsonProperty("data", Required = Required.Always)]
		public Data data;
		[JsonProperty("spec", Required = Required.Always)]
		string spec = null; // "chara_card_v3"
#pragma warning disable 0414 // Remove unread private members
		[JsonProperty("spec_version", Required = Required.Always)]
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

			[JsonProperty("creator_notes")]
			public string creator_notes = "";

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
			public string[] source = new string[0];

			[JsonProperty("group_only_greetings")]
			public string[] group_greetings = new string[0];

			[JsonProperty("creation_date")]
			public long? creationDate = null;

			[JsonProperty("modification_date")]
			public long? updateDate = null;

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

				[JsonProperty("tags", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
				public string[] tags = null;
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
				[JsonConverter(typeof(JsonIgnoreTypeConverter<int>))]
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
			else
				card.data.character_book = null;

			return card;
		}

		public static TavernCardV3 FromOutput(Generator.Output output)
		{
			TavernCardV3 card = new TavernCardV3();
			card.spec = "chara_card_v3";
			card.spec_version = "3.0";
			card.data.name = Current.CardName;
			card.data.nickname = Current.Name;
			card.data.creator = Current.Card.creator;
			card.data.character_version = Current.Card.versionString;
			card.data.tags = Current.Card.tags.ToArray();
			card.data.creationDate = (int)(Current.Card.creationDate ?? DateTime.UtcNow).ToUnixTimeSeconds();
			card.data.updateDate = (int)DateTime.UtcNow.ToUnixTimeSeconds();
			if (Current.Card.sources != null)
			{
				var lsSource = new List<string>(Current.Card.sources);
				string source = string.Concat("ginger:", Current.Card.uuid);
				if (lsSource.Contains(source) == false)
					lsSource.Add(source);
				card.data.source = lsSource.ToArray();
			}
			else
				card.data.source = null;

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
			card.data.group_greetings = output.group_greetings.Select(s => s.ToTavern()).ToArray();

			// Append user persona
			string userPersona = output.userPersona.ToTavern();
			if (string.IsNullOrEmpty(userPersona) == false)
			{
				if (Current.Card.extraFlags.Contains(CardData.Flag.UserPersonaInScenario)
					&& Current.Card.extraFlags.Contains(CardData.Flag.OmitScenario) == false)
					card.data.scenario = string.Concat(card.data.scenario, "\n\n", userPersona).Trim();
				else
					card.data.persona = string.Concat(card.data.persona, "\n\n", userPersona).Trim();
			}

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
					output.lorebook.entries
						.Select(e => {
							string[] keys = e.keys;
							var entry = new TavernCardV3.CharacterBook.Entry() {
								comment = keys.Length > 0 ? string.Join(", ", keys) : "",
								keys = keys,
								content = GingerString.FromString(e.value).ToTavern(),
								insertion_order = e.sortOrder,
							};
							if (e.unused != null)
							{
								entry.case_sensitive = e.unused.case_sensitive;
								entry.constant = e.unused.constant;
								entry.enabled = e.unused.enabled;
								entry.position = e.unused.placement;
								entry.priority = e.unused.priority;
								entry.secondary_keys = e.unused.secondary_keys;
								entry.selective = e.unused.selective;
								entry.extensions = e.unused.extensions ?? new JsonExtensionData();
								entry.use_regex = false;
							}
							return entry;
						})
						.ToArray();

				for (int i = 0; i < card.data.character_book.entries.Length; ++i)
					card.data.character_book.entries[i].id = i + 1;
			}
			else
				card.data.character_book = null;

			// Creator notes (multi-language)
			if (string.IsNullOrWhiteSpace(Current.Card.comment) == false)
			{
				var creator_notes_by_language = new Dictionary<string, StringBuilder>();
				var lines = (Current.Card.comment ?? "")
					.ConvertLinebreaks(Linebreak.LF)
					.Split(new char[] { '\n' }, StringSplitOptions.None);

				string currentLocale = "en";

				for (int i = 0; i < lines.Length; ++i)
				{
					string line = lines[i];

					// Change language?
					if (line.Length > 0 && line[0] == '#')
					{
						int pos_colon = line.IndexOf(':');
						if (pos_colon != -1)
						{
							string langCode = line.Substring(1, pos_colon - 1).Trim().ToLowerInvariant();
							if (Locales.AllLocales.ContainsKey(langCode)) // Is valid locale
							{
								// Strip region from language code (if any)
								int pos_region = langCode.IndexOf('_');
								if (pos_region != -1)
									langCode = langCode.Substring(0, pos_region);

								// Change current locale
								currentLocale = langCode;
							}

							// Remove language marker
							line = line.Substring(pos_colon + 1);
						}
					}

					if (creator_notes_by_language.ContainsKey(currentLocale) == false)
						creator_notes_by_language.Add(currentLocale, new StringBuilder());
					creator_notes_by_language[currentLocale].AppendLine(line);
				}

				if (creator_notes_by_language.Count > 0)
				{
					if (creator_notes_by_language.ContainsKey("en") && creator_notes_by_language.Count == 1) // English only
					{
						card.data.creator_notes = creator_notes_by_language["en"].ToString().Trim().ConvertLinebreaks(Linebreak.LF);
					}
					else // Multilingual
					{
						card.data.creator_notes_multilingual = creator_notes_by_language
							.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString().Trim().ConvertLinebreaks(Linebreak.LF));
						card.data.creator_notes = string.Join("\n\n", card.data.creator_notes_multilingual
							.Select(kvp => string.Format("#{0}:\n{1}", kvp.Key, kvp.Value)));
					}
				}
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

			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_tavernCharacterCardV3FilterSchema))
				{
					IList<string> validationErrors;
					if (jObject.IsValid(_tavernCharacterCardV3Schema, out validationErrors) == false)
						lsErrors.AddRange(validationErrors.Distinct());

					var card = JsonConvert.DeserializeObject<TavernCardV3>(json, settings);
					if (card != null)
					{
						errors = lsErrors.Count;
						if (Validate(card))
							return card;
					}
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
			if (card == null || card.data == null)
				return false;
			if (string.IsNullOrEmpty(card.data.name))
				return false;
			return card.spec == "chara_card_v3";
		}

		public static bool Validate(string jsonData)
		{
			try
			{
				JObject jObject = JObject.Parse(jsonData);
				if (jObject.IsValid(_tavernCharacterCardV3FilterSchema))
				{
					var card = JsonConvert.DeserializeObject<TavernCardV3>(jsonData);
					return Validate(card);
				}
				return false;
			}
			catch
			{
				return false;
			}
		}
	}

	public class TavernLorebookV3
	{
		[JsonProperty("spec", Required = Required.Always)]
		public string spec = null; //"lorebook_v3";
		[JsonProperty("data", Required = Required.Always)]
		public TavernCardV3.CharacterBook data;

		private static JsonSchema _tavernLorebookV3Schema;
		private static JsonSchema _tavernLorebookV3FilterSchema;

		static TavernLorebookV3()
		{
			_tavernLorebookV3Schema = JsonSchema.Parse(Resources.tavern_characterbook_v3_schema);
			_tavernLorebookV3FilterSchema = JsonSchema.Parse(Resources.tavern_characterbook_v3_filter_schema);
		}

		public static TavernLorebookV3 FromJson(string json, out int errors)
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
				if (jObject.IsValid(_tavernLorebookV3FilterSchema))
				{
					IList<string> validationErrors;
					if (jObject.IsValid(_tavernLorebookV3Schema, out validationErrors) == false)
						lsErrors.AddRange(validationErrors.Distinct());

					var lorebook = JsonConvert.DeserializeObject<TavernLorebookV3>(json, settings);
					if (lorebook != null)
					{
						errors = lsErrors.Count;
						if (Validate(lorebook))
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

		public static bool Validate(string jsonData)
		{
			try
			{
				JObject jObject = JObject.Parse(jsonData);
				return jObject.IsValid(_tavernLorebookV3FilterSchema);
			}
			catch
			{
				return false;
			}
		}
	}

	public class JsonIgnoreTypeConverter<T> : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
			writer.WriteValue(value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return default(T);
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }


}
