using System;
using System.Collections.Generic;
using System.Linq;

namespace Ginger
{
	public class ChatHistory : ICloneable
	{
		public Message[] messages;

		public IEnumerable<Message> messagesWithoutGreeting
		{
			get
			{
				if (count > 0 && messages[0].speaker == 1)
					return messages.Skip(1); // Skip greeting
				return messages;
			}
		}

		public int count { get { return messages != null ? messages.Length : 0; } }
		public bool isEmpty { get { return count == 0; } }

		public DateTime lastMessageTime
		{
			get
			{
				if (count > 0)
					return messages.Max(m => m.updateDate);
				return DateTime.MinValue;
			}
		}

		public class Message : ICloneable
		{
			public string instanceId;		// Message.id
			public int speaker;
			public DateTime creationDate;	// Message.createdAt
			public DateTime updateDate;     // Message.updatedAt
			
			public int activeSwipe;
			public string[] swipes;

			public string text { get { return swipes != null && activeSwipe >= 0 && activeSwipe < swipes.Length ? swipes[activeSwipe] : null; } }

			public object Clone()
			{
				var clone = (Message)MemberwiseClone();
				clone.swipes = new string[this.swipes.Length];
				Array.Copy(this.swipes, clone.swipes, this.swipes.Length);
				return clone;
			}
		}

		public object Clone()
		{
			var clone = new ChatHistory();
			clone.messages = new Message[messages.Length];
			for (int i = 0; i < messages.Length; ++i)
				clone.messages[i] = (Message)messages[i].Clone();
			return clone;
		}
	}
}
