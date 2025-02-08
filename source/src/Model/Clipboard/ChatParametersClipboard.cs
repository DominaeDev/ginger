using System;

namespace Ginger.Integration
{
	using ChatParameters = Backyard.ChatParameters;

	[Serializable]
	public class ChatParametersClipboard
	{
		public static readonly string Format = "Ginger.Integration.ChatParametersClipboard";

		public int version;
		public ChatParameters parameters;

		public static ChatParametersClipboard FromParameters(ChatParameters parameters)
		{
			return new ChatParametersClipboard() {
				version = 1,
				parameters = parameters,
			};
		}
	}
}
