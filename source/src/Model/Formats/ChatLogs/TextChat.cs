﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Ginger.Integration;
using System.Globalization;

namespace Ginger
{
	class TextChat
	{
		public class Message
		{
			public string name;
			public string text;
			public DateTime timestamp;
		}

		public Message[] messages;

		public static TextChat FromChat(ChatInstance chatInstance, string[] names)
		{
			if (chatInstance == null || chatInstance.history.isEmpty)
				return null;

			var lsMessages = new List<Message>(chatInstance.history.count);

			foreach (var message in chatInstance.history.messagesWithoutGreeting)
			{
				string speakerName;
				if (names != null && message.speaker >= 0 && message.speaker < names.Length)
					speakerName = names[message.speaker];
				else if (message.speaker == 0)
					speakerName = "Me";
				else
					speakerName = "Unknown";

				lsMessages.Add(new Message() {
					name = speakerName,
					text = message.text,
					timestamp = message.creationDate,
				});
			}
			
			return new TextChat() {
				messages = lsMessages.ToArray(),
			};
		}

		public ChatHistory ToChat()
		{
			var speakers = messages.Select(m => m.name ?? "").Distinct().ToList();
			int idxUser = speakers.IndexOfAny(new string[] { "User", "Me", "You" });
			if (idxUser != -1)
			{
				string userName = speakers[idxUser];
				speakers.RemoveAt(idxUser);
				speakers.Insert(0, userName);
			}
			int index = 0;
			var indexById = speakers.ToDictionary(s => s, s => index++);

			var result = new List<ChatHistory.Message>();
			foreach (var message in this.messages)
			{
				int speakerIdx;
				if (indexById.TryGetValue(message.name, out speakerIdx) == false)
					return null; // Error

				result.Add(new ChatHistory.Message() {
					speaker = speakerIdx,
					creationDate = message.timestamp,
					updateDate = message.timestamp,
					activeSwipe = 0,
					swipes = new string[1] { message.text },
				});
			}

			return new ChatHistory() {
				messages = result.ToArray(),
			};
		}
		
		public static TextChat FromString(string textData)
		{
			if (string.IsNullOrEmpty(textData))
				return null;

			textData = textData.ConvertLinebreaks(Linebreak.LF);
			string[] lines = textData.Split(new string[] { "\n---\n" }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.ToArray();

			var lsMessages = new List<Message>();
			foreach (var line in lines)
			{
				int pos_date_begin = line.IndexOf('[', 0);
				int pos_date_end = line.IndexOf(']', 0);
				if (pos_date_begin == -1 || pos_date_end == -1)
					continue;

				string dateString = line.Substring(pos_date_begin + 1, pos_date_end - pos_date_begin - 1);

				DateTime timestamp;
				if (DateTime.TryParse(dateString, out timestamp) == false)
					continue;

				int pos_colon = line.IndexOf(':', pos_date_end + 1);
				if (pos_colon == -1)
					continue;

				string name = line.Substring(pos_date_end + 1, pos_colon - pos_date_end - 1).Trim();
				string text = line.Substring(pos_colon + 1).TrimStart();

				lsMessages.Add(new Message() {
					name = name,
					timestamp = timestamp,
					text = text,
				});
			}

			if (lsMessages.Count == 0)
				return null;

			return new TextChat() {
				messages = lsMessages.ToArray(),
			};
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (var message in this.messages)
			{
				sb.AppendFormat("[{0}, {1}] {2}: ",
					message.timestamp.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
					message.timestamp.ToString("h:mm:ss tt", CultureInfo.InvariantCulture),
					message.name);
				sb.Append(message.text);
				sb.Append("\n---\n");
			}
			sb.ConvertLinebreaks(Linebreak.LF);
			return sb.ToString();
		}

		public static bool Validate(string textData)
		{
			if (string.IsNullOrEmpty(textData))
				return false;
			int pos_endl = textData.IndexOf('\n', 0);
			if (pos_endl == -1)
				return false;
			
			string line = textData.Substring(0, pos_endl).TrimEnd();
			if (line.Length == 0 || line[0] != '[')
				return false;

			int pos_comma = line.IndexOf(',');
			int pos_bracket = line.IndexOf(']');
			if (!(pos_bracket != -1
				&& pos_comma != -1
				&& pos_bracket > pos_comma))
				return false;

			int pos_colon = line.IndexOf(':', pos_bracket + 1);
			return pos_colon != -1;
		}
	}
}