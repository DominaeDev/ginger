using Newtonsoft.Json;
using System;
using System.Linq;

namespace Ginger
{
	public class GingerExtensionData
	{
		[JsonProperty("version")]
		public int version = Constants.GingerExtensionVersion;

		[JsonProperty("data")]
		public Data data { get; set; }

		[JsonProperty("output")]
		public Content output { get; set; }

		public class Data
		{
			// Parameters
			[JsonProperty("card_id")]
			public string id;

			[JsonProperty("card_name")]
			public string name;
			
			[JsonProperty("character_name")]
			public string spokenName;
			
			[JsonProperty("character_gender")]
			public string gender = "";
			
			[JsonProperty("character_pronouns")]
			public string[] characterPronouns;
			
			[JsonProperty("character_version")]
			public string characterVersion = "";
			
			[JsonProperty("tags")]
			public string[] tags;
			
			[JsonProperty("user_gender")]
			public string userGender = "";
			
			[JsonProperty("user_pronouns")]
			public string[] userPronouns;
			
			[JsonProperty("author")]
			public string creator = "";
			
			[JsonProperty("comment")]
			public string comment = "";
			
			[JsonProperty("creation_date")]
			public string creationDate { get; set; }
			
			[JsonProperty("update_date")]
			public string updateDate { get; set; }
			
			[JsonProperty("text_style")]
			public int textStyle { get; set; }
			
			[JsonProperty("detail_level")]
			public int detailLevel { get; set; }
		}

		public class Content
		{
			// Output
			[JsonProperty("system_prompt")]
			public string systemPrompt { get; set; }
			
			[JsonProperty("persona")]
			public string persona { get; set; }
			
			[JsonProperty("user_persona")]
			public string userPersona { get; set; }
			
			[JsonProperty("scenario")]
			public string scenario { get; set; }
			
			[JsonProperty("greetings")]
			public string[] greetings { get; set; }
			
			[JsonProperty("example")]
			public string exampleDialogue { get; set; }
			
			[JsonProperty("grammar")]
			public string grammar { get; set; }
			
			[JsonProperty("tokens")]
			public int[] tokens { get; set; } // total, permanent (with system), permanent (without system)
		}

		public static GingerExtensionData FromOutput(Generator.Output output)
		{
			GingerExtensionData ext = new GingerExtensionData();

			ext.data = new Data() {
				id = Current.Card.uuid,
				name = Current.Card.name ?? "",
				spokenName = Current.MainCharacter.spokenName ?? "",
				gender = Current.MainCharacter.gender ?? "",
				userGender = Current.Card.userGender ?? "",
				characterVersion = Current.Card.versionString,
				creator = Current.Card.creator,
				comment = Current.Card.comment.ConvertLinebreaks(Linebreak.LF),
				tags = Current.Card.tags.ToArray(),
				creationDate = (Current.Card.creationDate ?? DateTime.UtcNow).ToString("yyyy-MM-ddTHH:mm:ss.fffK"),
				updateDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK"),
				detailLevel = EnumHelper.ToInt(Current.Card.detailLevel),
				textStyle = EnumHelper.ToInt(Current.Card.textStyle),
			};

#if DEBUG
			ext.output = new Content();
#else
			ext.output = new Content() {
				systemPrompt = output.system.ToGinger(),
				persona = output.persona.ToGinger(),
				userPersona = output.userPersona.ToGinger(),
				scenario = output.scenario.ToGinger(),
				exampleDialogue = output.example.ApplyStandardTextStyle().ToGinger(),
				greetings = output.greetings.Select(g => g.ApplyStandardTextStyle().ToGinger()).ToArray(),
				grammar = output.grammar.ToGinger(),
				tokens = Current.Card.lastTokenCounts, // ! Last calculated value
			};

			string system_post = output.system_post_history.ToGinger();
			if (string.IsNullOrEmpty(system_post) == false)
				ext.output.systemPrompt = string.Join("\n", ext.output.systemPrompt, system_post).TrimStart();
#endif
			// Pronouns
			var context = Current.MainCharacter.GetContext(CharacterData.ContextType.Full);
			var evalConfig = new ContextString.EvaluationConfig() {
				macroSuppliers = new IMacroSupplier[] { Current.Strings },
				referenceSuppliers = new IStringReferenceSupplier[] { Current.Strings },
				ruleSuppliers = new IRuleSupplier[] { Current.Strings }
			};
			ext.data.characterPronouns = new string[5] {
				Text.Eval("[they]", context, evalConfig, Text.EvalOption.None),
				Text.Eval("[them]", context, evalConfig, Text.EvalOption.None),
				Text.Eval("[their]", context, evalConfig, Text.EvalOption.None),
				Text.Eval("[theirs]", context, evalConfig, Text.EvalOption.None),
				Text.Eval("[themselves]", context, evalConfig, Text.EvalOption.None),
			};

			ext.data.userPronouns = new string[5] { 
				Text.Eval("[#they]", context, evalConfig, Text.EvalOption.None),
				Text.Eval("[#them]", context, evalConfig, Text.EvalOption.None), 
				Text.Eval("[#their]", context, evalConfig, Text.EvalOption.None), 
				Text.Eval("[#theirs]", context, evalConfig, Text.EvalOption.None), 
				Text.Eval("[#themselves]", context, evalConfig, Text.EvalOption.None),
			};

			return ext;
		}
	}

	public class GingerVersionExtensionData
	{
		[JsonProperty("version")]
		public int version = Constants.GingerExtensionVersion;
	}

}
