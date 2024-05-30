using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Ginger
{
	[Serializable]
	public class LoreClipboard
	{
		public static readonly string Format = "Ginger.LoreClipboard";

		public int version;
		public string data;

		public static LoreClipboard FromLoreEntries(IEnumerable<Lorebook.Entry> entries)
		{
			XmlDocument xmlDoc = new XmlDocument();
			XmlNode xmlNode = xmlDoc.CreateElement("Ginger");
			xmlNode.AddAttribute("version", GingerCardV1.Version);
			xmlDoc.AppendChild(xmlNode);

			XmlDeclaration xmlDecl = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", "no");
			xmlDecl.Encoding = "UTF-8";
			xmlDoc.InsertBefore(xmlDecl, xmlNode);

			var entriesNode = xmlNode.AddElement("Entries");
			foreach (var entry in entries)
			{
				var entryNode = entriesNode.AddElement("Entry");
				entryNode.AddValueElement("Name", entry.key);
				entryNode.AddValueElement("Value", Parameter.ToClipboard(entry.value));
				if (entry.isEnabled == false)
					entryNode.AddAttribute("enabled", false);

				if (entry.unused != null)
				{
					var propertiesNode = entryNode.AddElement("Properties");
					entry.unused.SaveToXml(propertiesNode);
				}
			}

			StringBuilder sbXml = new StringBuilder();
			using (var stringWriter = new StringWriterUTF8(sbXml))
			{
				XmlWriterSettings settings = new XmlWriterSettings();
				settings.Indent = false;

				using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
				{
					xmlDoc.Save(xmlWriter);
				}
			}
			
			return new LoreClipboard() {
				data = sbXml.ToString(),
				version = 1,
			};
		}

		public List<Lorebook.Entry> ToEntries()
		{
			if (string.IsNullOrEmpty(data))
				return null;

			var entries = new List<Lorebook.Entry>();

			XmlDocument xmlDoc = new XmlDocument();
			try
			{
				byte[] payload = Encoding.UTF8.GetBytes(data);
				using (var stream = new MemoryStream(payload))
				{
					xmlDoc.Load(stream);
				}
			}
			catch
			{
				return null;
			}

			if (xmlDoc.DocumentElement.Name != "Ginger")
				return null; // Unexpected root element
			int version = xmlDoc.DocumentElement.GetAttributeInt("version", 0);
			if (version > GingerCardV1.Version)
				return null; // Unsupported version

			var xmlNode = xmlDoc.DocumentElement;
			
			entries.Clear();
			var entriesNode = xmlNode.GetFirstElement("Entries");
			if (entriesNode != null)
			{
				var entryNode = entriesNode.GetFirstElement("Entry");
				while (entryNode != null)
				{
					string key = entryNode.GetValueElement("Name");
					string value = entryNode.GetValueElement("Value");
					bool isEnabled = entryNode.GetAttributeBool("enabled", true);
					var entry = new Lorebook.Entry() {
						key = key,
						value = Parameter.FromClipboard(value),
						isEnabled = isEnabled,
					};

					// Unused
					var propertiesNode = entryNode.GetFirstElement("Properties");
					if (propertiesNode != null)
					{
						entry.unused = new Lorebook.Entry.UnusedProperties();
						entry.unused.LoadFromXml(propertiesNode);
					}

					entries.Add(entry);

					entryNode = entryNode.GetNextSibling();
				}
			}

			return entries;
		}
	}
}