using System;
using System.Collections.Generic;
using System.Text;

namespace Ginger.Integration
{
	[Serializable]
	public class ChatClipboard
	{
		public static readonly string Format = "Ginger.Integration.ChatClipboard";

		public int version;
		public string text;
		public string rawText;

		public struct Message
		{
			public string name;
			public int characterIndex;
			public string text;
		}
		public static ChatClipboard FromMessages(IEnumerable<Message> messages)
		{
			StringBuilder sbText = new StringBuilder();
			StringBuilder sbRawText = new StringBuilder();
			foreach (var m in messages)
			{
				if (m.text == null)
				{
					sbText.NewParagraph();
					sbRawText.NewParagraph();
					continue;
				}

				sbText.Append(m.characterIndex == 0 ? GingerString.UserMarker : GingerString.CharacterMarker);
				sbText.Append(": ");
				sbText.AppendLine(m.text);
				sbRawText.Append(m.name ?? "");
				sbRawText.Append(": ");
				sbRawText.AppendLine(m.text);
			}

			return new ChatClipboard() {
				version = 1,
				text = sbText.ToString(),
				rawText = sbRawText.ToString(),
			};
		}

	}
}
