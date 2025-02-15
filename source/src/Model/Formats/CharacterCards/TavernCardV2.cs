﻿using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Schema;
using Ginger.Properties;
using Newtonsoft.Json.Linq;

namespace Ginger
{
	public class TavernCardV2
	{
		private static JsonSchema _tavernCharacterCardV1Schema;
		private static JsonSchema _tavernCharacterCardV2Schema;
		private static JsonSchema _tavernCharacterCardV2FilterSchema;
		private static JsonSchema _tavernCharacterBookV2Schema;
		private static JsonSchema _tavernCharacterBookV2FilterSchema;

		static TavernCardV2()
		{
			_tavernCharacterCardV1Schema = JsonSchema.Parse(Resources.tavern_charactercard_v1_schema);
			_tavernCharacterCardV2Schema = JsonSchema.Parse(Resources.tavern_charactercard_v2_schema);
			_tavernCharacterCardV2FilterSchema = JsonSchema.Parse(Resources.tavern_charactercard_v2_filter_schema);
			_tavernCharacterBookV2Schema = JsonSchema.Parse(Resources.tavern_characterbook_v2_schema);
			_tavernCharacterBookV2FilterSchema = JsonSchema.Parse(Resources.tavern_characterbook_v2_filter_schema);
		}

		public TavernCardV2()
		{
			data = new Data();
		}

		[JsonProperty("data", Required = Required.Always)]
		public Data data;
		[JsonProperty("spec", Required = Required.Always)]
		string spec = null; // "chara_card_v2"
#pragma warning disable 0414 // Remove unread private members
		[JsonProperty("spec_version", Required = Required.Always)]
		string spec_version = null; //"2.0";
#pragma warning restore 0414 // Remove unread private members

		public class Data
		{
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
			public CharacterBook character_book = new CharacterBook();
			
			[JsonProperty("tags")]
			public string[] tags = new string[0];
			
			[JsonProperty("creator")]
			public string creator = "";
			
			[JsonProperty("character_version")]
			public string character_version = "";

			[JsonProperty("extensions")]
			public GingerJsonExtensionData extensions = new GingerJsonExtensionData();
		}
		
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
			}

			public static CharacterBook FromJson(string json, out int errors)
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
					if (jObject.IsValid(_tavernCharacterBookV2FilterSchema))
					{
						IList<string> validationErrors;
						if (jObject.IsValid(_tavernCharacterBookV2Schema, out validationErrors) == false)
							lsErrors.AddRange(validationErrors.Distinct());

						var lorebook = JsonConvert.DeserializeObject<CharacterBook>(json, settings);
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
					return jObject.IsValid(_tavernCharacterBookV2FilterSchema);
				}
				catch
				{
					return false;
				}
			}
		}

		private static TavernCardV2 FromV1(TavernCardV1 card)
		{
			return new TavernCardV2() {
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

		public static TavernCardV2 FromOutput(Generator.Output output)
		{
			TavernCardV2 card = new TavernCardV2();
			card.spec = "chara_card_v2";
			card.spec_version = "2.0";
			card.data.name = Current.Name;
			card.data.creator = Current.Card.creator;
			card.data.creator_notes = (Current.Card.comment ?? "").ConvertLinebreaks(Linebreak.LF);
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
			{
				if (Current.Card.extraFlags.Contains(CardData.Flag.UserPersonaInScenario)
					&& Current.Card.extraFlags.Contains(CardData.Flag.OmitScenario) == false)
					card.data.scenario = string.Concat(card.data.scenario, "\n\n", userPersona).Trim();
				else
					card.data.persona = string.Concat(card.data.persona, "\n\n", userPersona).Trim();
			}

			if (output.hasLore)
			{
				card.data.character_book = new CharacterBook();

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
						var entry = new CharacterBook.Entry() {
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
						}
						return entry;
					}).ToArray();

				for (int i = 0; i < card.data.character_book.entries.Length; ++i)
					card.data.character_book.entries[i].id = i + 1;
			}
			else
				card.data.character_book = null;

			return card;
		}

		public static TavernCardV2 FromJson(string json, out int errors)
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

			// Version 2
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_tavernCharacterCardV2FilterSchema))
				{
					IList<string> validationErrors;
					if (jObject.IsValid(_tavernCharacterCardV2Schema, out validationErrors) == false)
						lsErrors.AddRange(validationErrors.Distinct());

					var card = JsonConvert.DeserializeObject<TavernCardV2>(json, settings);
					if (card != null)
					{
						errors = lsErrors.Count;
						if (Validate(card))
							return card;
					}
				}
			}
			catch (Exception e)
			{
			}

			// Version 1
			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_tavernCharacterCardV1Schema))
				{
					var cardV1 = JsonConvert.DeserializeObject<TavernCardV1>(json, settings);
					errors = lsErrors.Count;
					if (Validate(cardV1))
					{
						var cardV2 = FromV1(cardV1);
						return cardV2;
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

		private static bool Validate(TavernCardV2 card)
		{
			if (card == null || card.data == null)
				return false;

			if (card.spec != "chara_card_v2")
				return true;

			if (string.IsNullOrEmpty(card.data.name))
				card.data.name = Constants.DefaultCharacterName;

			if (card.data.character_book != null)
			{
				if (card.data.character_book.entries == null)
					card.data.character_book.entries = new CharacterBook.Entry[0];

				foreach (var entry in card.data.character_book.entries)
				{
					if (entry.position != "before_char" && entry.position != "after_char")
						entry.position = "before_char";
				}
			}

			return true;
		}

		private static bool Validate(TavernCardV1 card)
		{
			if (card == null)
				return false;
			if (card.name == null)
				return false;
			if (string.IsNullOrEmpty(card.description) && string.IsNullOrEmpty(card.personality))
				return false;
			return true;
		}

		public static bool Validate(string jsonData)
		{
			try
			{
				JObject jObject = JObject.Parse(jsonData);
				if (jObject.IsValid(_tavernCharacterCardV2FilterSchema))
				{
					var card = JsonConvert.DeserializeObject<TavernCardV2>(jsonData);
					return Validate(card);
				}
				if (jObject.IsValid(_tavernCharacterCardV1Schema))
				{
					var card = JsonConvert.DeserializeObject<TavernCardV1>(jsonData);
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

	public class TavernCardV2_Export // Tavern v2 + v1 merge
	{
		public TavernCardV2_Export()
		{
			data = new TavernCardV2.Data();
		}

#pragma warning disable 0414
		[JsonProperty("spec", Required = Required.Always)]
		string spec = "chara_card_v2";
		[JsonProperty("spec_version", Required = Required.Always)]
		string spec_version = "2.0";
#pragma warning restore 0414

		#region V1 fields
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
		#endregion

		#region V2 fields
		[JsonProperty("data", Required = Required.Always)]
		public TavernCardV2.Data data;
		#endregion

		public static TavernCardV2_Export FromOutput(Generator.Output output)
		{
			TavernCardV2_Export card = new TavernCardV2_Export();
			var cardV2 = TavernCardV2.FromOutput(output);
			card.data = cardV2.data;

			// V2 fields
			card.name = card.data.name;
			card.description = card.data.persona;
			card.personality = card.data.personality;
			card.scenario = card.data.scenario;
			card.greeting = card.data.greeting;
			card.example = card.data.example;

			return card;
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
	}
}
