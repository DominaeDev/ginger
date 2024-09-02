using System;
using System.Collections.Generic;
using System.Linq;

namespace Ginger
{
	public class ChatHistory
	{
		public string greeting;				// Chat.greetingDialogue
		public Message[] messages;

		public bool hasGreeting { get { return string.IsNullOrEmpty(greeting) == false; } }
		public int Count { get { return messages != null ? messages.Length : 0; } }
		public bool isEmpty { get { return Count == 0; } }

		public IEnumerable<Message> MessagesWithoutGreeting
		{
			get
			{
				if (hasGreeting) 
					return messages.Skip(1);
				return messages;
			}
		}

		public class Message
		{
			public string instanceId;		// Message.id
			public int speaker;
			public DateTime creationDate;	// Message.createdAt
			public DateTime updateDate;     // Message.updatedAt
			
			public int activeSwipe;
			public string[] swipes;

			public string message { get { return swipes != null && activeSwipe >= 0 && activeSwipe < swipes.Length ? swipes[activeSwipe] : null; } }
		}
	}
}
