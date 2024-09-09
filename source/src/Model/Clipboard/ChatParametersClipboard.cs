using System;

namespace Ginger
{
	using Bridge = BackyardBridge;

	[Serializable]
	public class ChatParametersClipboard
	{
		public static readonly string Format = "Ginger.ChatParametersClipboard";

		public int version;
		public Bridge.ChatParameters parameters;

		public static ChatParametersClipboard FromParameters(Bridge.ChatParameters parameters)
		{
			return new ChatParametersClipboard() {
				version = 1,
				parameters = parameters,
			};
		}
	}
}
