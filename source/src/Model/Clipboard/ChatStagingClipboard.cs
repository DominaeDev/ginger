using System;

namespace Ginger.Integration
{
	using ChatStaging = Backyard.ChatStaging;

	[Serializable]
	public class ChatStagingClipboard
	{
		public static readonly string Format = "Ginger.Integration.ChatStagingClipboard";

		public int version;
		public ChatStaging staging;

		public static ChatStagingClipboard FromStaging(ChatStaging staging)
		{
			return new ChatStagingClipboard() {
				version = 1,
				staging = staging,
			};
		}
	}
}