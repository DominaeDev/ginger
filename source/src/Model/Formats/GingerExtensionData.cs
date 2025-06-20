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
			
			[JsonProperty("character_version")]
			public string characterVersion = "";
			
			[JsonProperty("tags")]
			public string[] tags;
			
			[JsonProperty("user_gender")]
			public string userGender = "";
						
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

			[JsonProperty("tokens")]
			public int[] tokens { get; set; } // total, permanent (with system), permanent (without system)
		}

		public static GingerExtensionData FromOutput(Generator.Output output)
		{
			GingerExtensionData ext = new GingerExtensionData();

			ext.data = new Data() {
				id = Current.Card.uuid,
				name = Current.Card.name ?? "",
				spokenName = Current.MainCharacter.name ?? "",
				gender = Current.MainCharacter.gender ?? "",
				userGender = Current.Card.userGender ?? "",
				characterVersion = Current.Card.versionString ?? "",
				creator = Current.Card.creator ?? "",
				comment = (Current.Card.comment ?? "").ConvertLinebreaks(Linebreak.LF),
				tags = Current.Card.tags.ToArray(),
				creationDate = (Current.Card.creationDate ?? DateTime.UtcNow).ToISO8601(),
				updateDate = DateTime.UtcNow.ToISO8601(),
				detailLevel = EnumHelper.ToInt(Current.Card.detailLevel),
				textStyle = EnumHelper.ToInt(Current.Card.textStyle),
				tokens = Current.Card.lastTokenCounts, // ! Last calculated value
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
