﻿using System;
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
				return messages ?? new Message[0];
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

		public int numSpeakers
		{
			get
			{
				if (count == 0)
					return 0;
				return messages.Max(m => m.speaker) + 1;
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

		public static ChatHistory LegacyFix(ChatHistory chat)
		{
			if (chat.isEmpty)
				return chat;

			for (int i = 0; i < chat.messages.Length; ++i)
			{
				for (int j = 0; j < chat.messages[i].swipes.Length; ++j)
				{
					string text = chat.messages[i].swipes[j];
					int pos_begin = text.IndexOf("#{character}:");
					int pos_end = text.IndexOf("\n#{user}:");
					bool bFront = pos_begin == 0;
					bool bBack = pos_end >= 0 && pos_end >= text.Length - 10;
					if (bFront && bBack)
						chat.messages[i].swipes[j] = text.Substring(pos_begin + 13, pos_end - pos_begin - 13).Trim();
					else if (bFront)
						chat.messages[i].swipes[j] = text.Substring(pos_begin + 13).TrimStart();
					else if (bBack)
						chat.messages[i].swipes[j] = text.Substring(0, pos_end).TrimEnd();
				}
			}
			return chat;
		}

		public void ApplyNames(string[] names)
		{
			if (isEmpty)
				return;

			for (int i = 0; i < messages.Length; ++i)
			{
				for (int j = 0; j < messages[i].swipes.Length; ++j)
				{
					if (names.Length > 0)
						messages[i].swipes[j] = Utility.ReplaceWholeWord(messages[i].swipes[j], GingerString.UserMarker, names[0] ?? Constants.DefaultUserName);
					if (names.Length > 1)
						messages[i].swipes[j] = Utility.ReplaceWholeWord(messages[i].swipes[j], GingerString.CharacterMarker, names[1] ?? Constants.DefaultCharacterName);
				}
			}
		}
	}
}