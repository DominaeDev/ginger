using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Schema;
using Ginger.Properties;
using Newtonsoft.Json.Linq;

namespace Ginger
{
	public class AgnaisticCard
	{
#pragma warning disable 0414
#pragma warning disable 0169

		private static JsonSchema _agnaisticCharacterCardSchema;
		private static JsonSchema _agnaisticCharacterBookSchema;
		static AgnaisticCard()
		{
			_agnaisticCharacterCardSchema = JsonSchema.Parse(Resources.agnaistic_charactercard_schema);
			_agnaisticCharacterBookSchema = JsonSchema.Parse(Resources.agnaistic_characterbook_schema);
		}

		public AgnaisticCard()
		{
			creationDate = updateDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
		}

		[JsonProperty("name", Required = Required.Always)] 
		public string name;

		[JsonProperty("description")]
		public string description = "";

		[JsonProperty("culture")]
		public string culture = "en-us";

		[JsonProperty("tags")]
		public string[] tags = new string[0];

		[JsonProperty("scenario")]
		public string scenario = "";

		[JsonProperty("appearance")]
		public string appearance = "";

		[JsonProperty("visualType")]
		public string visualType = "avatar";

		[JsonProperty("sprite")]
		public JsonExtensionData sprite = null;

		[JsonProperty("greeting")]
		public string greeting = "";

		[JsonProperty("sampleChat")]
		public string example = "";

		[JsonProperty("voiceDisabled")]
		public bool voiceDisabled;

		[JsonProperty("voice")]
		public Voice voice = new Voice();

		[JsonProperty("systemPrompt")]
		public string system_prompt = "";

		[JsonProperty("postHistoryInstructions")]
		public string postHistoryInstructions = "";

		[JsonProperty("alternativeGreetings")]
		public string[] alternateGreetings = new string[0];

		[JsonProperty("characterBook")]
		public CharacterBook character_book = new CharacterBook();

		[JsonProperty("creator")]
		public string creator = "";

		[JsonProperty("characterVersion")]
		public string character_version = "";

		[JsonProperty("persona")]
		public Persona _persona = new Persona();

		[JsonProperty("createdAt")]
		public string creationDate = "";

		[JsonProperty("updatedAt")]
		public string updateDate = "";

		[JsonProperty("kind")]
		public string kind = "";

		[JsonProperty("extensions")]
		public GingerJsonExtensionData extensions = new GingerJsonExtensionData();

		[JsonProperty("avatar")]
		public string avatar = "";

		public class Voice 
		{
			[JsonProperty("service")]
			public string service;
			[JsonProperty("voiceId")]
			public string voiceId;
		}

		public class Insert
		{
			[JsonProperty("prompt")]
			public string prompt = "";
			[JsonProperty("depth")]
			public int depth = 3;
		}

		public class Persona
		{
			[JsonProperty("kind")]
			private string kind = "text";

			[JsonProperty("attributes")]
			public Dictionary<string, string[]> attributes = new Dictionary<string, string[]>();
		}

		public class CharacterBook
		{
			[JsonProperty("_id")]
			public string _id = "character_book";

			[JsonProperty("name")]
			public string name = "";
			
			[JsonProperty("description")]
			public string description = "";
			
			[JsonProperty("userId")]
			public string userId = "characterBook";
			
			[JsonProperty("scanDepth")]
			public int scanDepth = 50;
			
			[JsonProperty("tokenBudget")]
			public int tokenBudget = 500;
			
			[JsonProperty("recursiveScanning")]
			public bool recursiveScanning = false;
			
			[JsonProperty("entries")]
			public Entry[] entries = new Entry[0];
			
			[JsonProperty("kind")]
			private string kind = "memory";
			
			[JsonProperty("is_creation")]
			private bool isCreation = false;

			public class Entry
			{
				[JsonProperty("name", Required = Required.Always)]
				public string name = "";
				
				[JsonProperty("entry", Required = Required.Always)]
				public string entry = "";
				
				[JsonProperty("keywords", Required = Required.Always)]
				public string[] keywords = new string[0];
				
				[JsonProperty("priority")]
				public int priority = 0;
				
				[JsonProperty("weight")]
				public int weight = 0;
				
				[JsonProperty("enabled")]
				public bool enabled = true;
				
				[JsonProperty("id")]
				public int id;
				
				[JsonProperty("comment")]
				public string comment = "";
				
				[JsonProperty("selective")]
				public bool selective = false;
				
				[JsonProperty("secondaryKeys")]
				public string[] secondaryKeys = new string[0];
				
				[JsonProperty("constant")]
				public bool constant = false;
				
				[JsonProperty("position")]
				public string position = "before_char";
			}

			public static CharacterBook FromJson(string json)
			{
				try
				{
					JObject jObject = JObject.Parse(json);
					if (jObject.IsValid(_agnaisticCharacterBookSchema))
						return JsonConvert.DeserializeObject<CharacterBook>(json);
				}
				catch
				{
				}
				return null;
			}

			public static bool Validate(string jsonData)
			{
				try
				{
					JObject jObject = JObject.Parse(jsonData);
					return jObject.IsValid(_agnaisticCharacterBookSchema);
				}
				catch
				{
					return false;
				}
			}
		}

		[JsonIgnore]
		public string persona
		{
			get
			{
				string[] value;
				if (_persona.attributes.TryGetValue("text", out value) && value.Length > 0)
					return value[0];
				return null;
			}
			set
			{
				if (_persona.attributes.ContainsKey("text"))
					_persona.attributes["text"] = new string[1] { value };
				else
					_persona.attributes.Add("text", new string[1] { value });
			}
		}
	
#pragma warning restore 0414
#pragma warning restore 0169

		public static AgnaisticCard FromOutput(Generator.Output output)
		{
			AgnaisticCard card = new AgnaisticCard();
			card.kind = "character";
			card.name = Current.Name;
			card.creator = Current.Card.creator;
			card.description = Current.Card.comment.ConvertLinebreaks(Linebreak.LF);
			card.character_version = Current.Card.versionString;
			card.tags = Current.Card.tags.ToArray();
			card.creationDate = (Current.Card.creationDate ?? DateTime.UtcNow).ToString("yyyy-MM-ddTHH:mm:ss.fffK");

			card.system_prompt = output.system.ToTavern();
			card.postHistoryInstructions = output.system_post_history.ToTavern();
			card.persona = output.persona.ToTavern();
			card.scenario = output.scenario.ToTavern();
			card.example = output.example.ToTavernChat();
			card.greeting = output.greeting.ToTavern();
			card.alternateGreetings = output.alternativeGreetings.Select(s => s.ToTavern()).ToArray();

			// Append personality
			string personality = output.personality.ToTavern();
			if (string.IsNullOrEmpty(personality) == false)
				card.persona = string.Concat(card.persona, "\n", personality).Trim();

			// Append user persona
			string userPersona = output.userPersona.ToTavern();
			if (string.IsNullOrEmpty(userPersona) == false)
				card.scenario = string.Concat(card.scenario, "\n\n", userPersona).Trim();

			if (output.hasLore)
			{
				card.character_book = new CharacterBook();
				if (output.lorebook.unused != null)
				{
					card.character_book.recursiveScanning = output.lorebook.unused.recursive_scanning;
					card.character_book.scanDepth = output.lorebook.unused.scan_depth;
					card.character_book.tokenBudget = output.lorebook.unused.token_budget;
				}
				card.character_book.entries =
					output.lorebook.entries.Select(e => {
						string[] keys = e.keys;
						var entry = new CharacterBook.Entry() {
							keywords = keys,
							entry = GingerString.FromString(e.value).ToTavern(),
						};
						if (e.unused != null)
						{
							entry.comment = e.unused.comment;
							entry.constant = e.unused.constant;
							entry.enabled = e.unused.enabled;
							entry.weight = e.unused.insertion_order;
							entry.position = e.unused.placement;
							entry.priority = e.unused.priority;
							entry.secondaryKeys = e.unused.secondary_keys;
							entry.selective = e.unused.selective;
						}
						return entry;
					}).ToArray();

				for (int i = 0; i < card.character_book.entries.Length; ++i)
					card.character_book.entries[i].id = i + 1;
			}

			return card;
		}

		public static AgnaisticCard FromJson(string json, out int errors)
		{
			List<string> lsErrors = new List<string>();
			JsonSerializerSettings settings = new JsonSerializerSettings() {
				Error = delegate (object sender, ErrorEventArgs args) {
					if (args.ErrorContext.Error.Message.Contains("Required")) // Required field
						return; // Throw

					lsErrors.Add(args.ErrorContext.Error.Message);
					args.ErrorContext.Handled = true;
				},
			};

			try
			{
				JObject jObject = JObject.Parse(json);
				if (jObject.IsValid(_agnaisticCharacterCardSchema))
				{
					var card = JsonConvert.DeserializeObject<AgnaisticCard>(json, settings);
					if (Validate(card))
					{
						errors = lsErrors.Count;
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

		private static bool Validate(AgnaisticCard card)
		{
			if (card == null
				|| string.IsNullOrEmpty(card.name)
				|| string.IsNullOrEmpty(card.kind))
				return false;
			if (string.Compare(card.kind, "character", StringComparison.OrdinalIgnoreCase) != 0)
				return false;
			return true;
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
				if (jObject.IsValid(_agnaisticCharacterCardSchema))
				{
					var card = JsonConvert.DeserializeObject<AgnaisticCard>(jsonData);
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
}
