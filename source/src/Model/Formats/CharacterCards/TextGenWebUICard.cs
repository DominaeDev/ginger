using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Ginger
{
	public class TextGenWebUICard
	{
		public string name;
		public string context;
		public string greeting;
		public string example;

		public static TextGenWebUICard FromOutput(Generator.Output output)
		{
			var persona = output.persona.ToTavern();
			var scenario = output.scenario.ToTavern();
			var greeting = output.greeting.ToTavern();
			var example = output.example.ToTavernChat();

			// Append personality
			string personality = output.personality.ToTavern();
			if (string.IsNullOrEmpty(personality) == false)
				persona = string.Concat(persona, "\n", personality).Trim();

			// Append user persona
			string userPersona = output.userPersona.ToTavern();
			if (string.IsNullOrEmpty(userPersona) == false)
			{
				if (Current.Card.extraFlags.Contains(CardData.Flag.UserPersonaInScenario)
					&& Current.Card.extraFlags.Contains(CardData.Flag.OmitScenario) == false)
					scenario = string.Concat(scenario, "\n\n", userPersona).Trim();
				else
					persona = string.Concat(persona, "\n\n", userPersona).Trim();
			}

			var sbContext = new StringBuilder();
			if (string.IsNullOrWhiteSpace(persona) == false)
				sbContext.AppendLine(persona);
			if (string.IsNullOrWhiteSpace(scenario) == false)
				sbContext.AppendLine(scenario);

			sbContext.ConvertLinebreaks(Linebreak.LF);

			TextGenWebUICard card = new TextGenWebUICard();
			card.name = Current.Name;
			card.context = sbContext.ToString();
			card.example = example;
			card.greeting = greeting;
			return card;
		}
		
		public static TextGenWebUICard FromYaml(string yaml)
		{
			try
			{
				var deserializer = new Deserializer();
				var card = deserializer.Deserialize<TextGenWebUICard>(yaml);
				if (card.Validate())
				{
					card.SplitExampleChat();
					return card;
				}
			}
			catch
			{
			}
			return null;
		}

		private void SplitExampleChat()
		{
			string[] lines = context.Split(new char[] { '\n' });
			var sbContext = new StringBuilder();
			var sbExample = new StringBuilder();
			bool bExample = false;
			for (int i = 0; i < lines.Length; ++i)
			{
				if (lines[i].BeginsWith("<START>", true))
					bExample = true;

				if (bExample)
					sbExample.AppendLine(lines[i]);
				else
					sbContext.AppendLine(lines[i]);
			}
			sbContext.ConvertLinebreaks(Linebreak.LF);
			sbExample.ConvertLinebreaks(Linebreak.LF);
			sbContext.Trim();
			sbExample.Trim();
			this.context = sbContext.ToString();
			this.example = sbExample.ToString();
		}

		public string ToYaml()
		{
			try
			{
				var serializer = new SerializerBuilder()
					.WithNamingConvention(LowerCaseNamingConvention.Instance)
					.WithDefaultScalarStyle(YamlDotNet.Core.ScalarStyle.Literal)
					.Build();

				// Merge example chat
				var sbContext = new StringBuilder();
				if (string.IsNullOrWhiteSpace(context) == false)
					sbContext.AppendLine(context);
				if (string.IsNullOrWhiteSpace(example) == false)
					sbContext.AppendLine(example);

				string yaml = serializer.Serialize(new {
					name = this.name,
					greeting = this.greeting,
					context = sbContext.ToString(),
				});
				return yaml;
			}
			catch
			{
				return null;
			}
		}

		private bool Validate()
		{
			return string.IsNullOrWhiteSpace(name) == false
				&& string.IsNullOrWhiteSpace(context) == false;
		}

		public static bool Validate(string yaml)
		{
			try
			{
				var deserializer = new Deserializer();
				var card = deserializer.Deserialize<TextGenWebUICard>(yaml);
				return card.Validate();
			}
			catch
			{
			}
			return false;
		}
	}
}
