using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Ginger
{
	public struct BlockID : IComparable<BlockID>
	{
		public static readonly char Delimiter = '/';
		public StringHandle handle { get; private set; }
		public StringHandle[] path { get; private set; }
		public int depth { get; private set; }
		public Root root { get; private set; }

		public enum Root
		{
			Undefined = 0,
			System,
			Persona,
			Scenario,
			User,
			Example,
		}

		public BlockID(StringHandle id)
		{
			handle = id;

			if (StringHandle.IsNullOrEmpty(handle))
			{
				depth = 0;
				path = new StringHandle[0];
				root = Root.Undefined;
				return;
			}

			string sID = id.ToString();
			depth = sID.Count(c => c == Delimiter);
			path = sID.Split(new char[] { Delimiter }).Select(s => new StringHandle(s)).ToArray();

			string sRoot = path[0].ToString();
			switch (sRoot)
			{
			case "system": root = Root.System; break;
			case "persona": root = Root.Persona; break;
			case "scenario": root = Root.Scenario; break;
			case "user": root = Root.User; break;
			case "example": root = Root.Example; break;
			default: root = Root.Undefined; break;
			}
		}


		public bool IsParentOf(BlockID child)
		{
			return child.IsChildOf(this);
		}

		public bool IsChildOf(BlockID parent)
		{
			if (parent.depth >= this.depth)
				return false;

			for (int i = parent.depth; i >= 0; --i)
			{
				if (parent.path[i] != this.path[i])
					return false;
			}
			return true;
		}

		public bool IsSiblingOf(BlockID sibling)
		{
			return this.depth == sibling.depth
				&& this.handle != sibling.handle
				&& this.GetParent() == sibling.GetParent();
		}

		public BlockID GetParent()
		{
			if (depth == 0)
				return "";

			string sPath = handle.ToString();
			int pos_slash = sPath.LastIndexOf(Delimiter);
			return sPath.Substring(0, pos_slash);
		}

		public static implicit operator BlockID(string s)
		{
			return new BlockID(s);
		}

		public static implicit operator BlockID(StringHandle s)
		{
			return new BlockID(s);
		}

		public static bool IsNullOrEmpty(BlockID id)
		{
			return StringHandle.IsNullOrEmpty(id.handle);
		}

		public override bool Equals(object obj)
		{
			// Check for null values and compare run-time types.
			if (obj == null || GetType() != obj.GetType())
				return false;

			BlockID blockID = (BlockID)obj;
			return this.handle == blockID.handle;
		}

		public static bool operator == (BlockID a, BlockID b)
		{
			return a.handle == b.handle;
		}

		public static bool operator != (BlockID a, BlockID b)
		{
			return a.handle != b.handle;
		}

		public override int GetHashCode()
		{
			return handle.GetHashCode();
		}

		public override string ToString()
		{
			return handle.ToString();
		}

		public int CompareTo(BlockID obj)
		{
			return handle.ToString().CompareTo(obj.handle.ToString());
		}

		public static BlockID EnsureRoot(BlockID blockID, Root root)
		{
			if (StringHandle.IsNullOrEmpty(blockID.handle) == false)
			{
				if (root == Root.System && blockID.path[0] != "system")
					return string.Concat("system/", blockID.handle);
				else if (root == Root.Persona && blockID.path[0] != "persona")
					return string.Concat("persona/", blockID.handle);
				else if (root == Root.Scenario && blockID.path[0] != "scenario")
					return string.Concat("scenario/", blockID.handle);
				else if (root == Root.User && blockID.path[0] != "user")
					return string.Concat("user/", blockID.handle);
				else if (root == Root.Example && blockID.path[0] != "example")
					return string.Concat("example/", blockID.handle);
			}

			return blockID;
		}
	}

	public class Block : IXmlLoadable, IXmlSaveable, ICloneable
	{
		public BlockID id;
		public ICondition condition;
		public string value;

		public enum Style
		{
			Undefined,
			Join,			// abc
			Space,			// a b c
			Stop,			// a b c.
			Quote,			// "a b c"
			Bracket,		// [a b c]
			Parenthesis,    // (a b c)
			Curly,          // {a b c}
			// Arrays (horizontal)
			Comma,			// a, b, c
			CommaStop,		// a, b, c.
			Group,			// (a, b, c)
			Semicolon,		// a; b; c
			Plus,			// a + b + c
			Hyphen,			// a - b - c
			CommaList,	    // "a", "b", "c"
			Array,			// ["a", "b", "c"]
			PlusList,	    // "a" + "b" + "c"
			SemiGroup,		// (a; b; c)
			PlusGroup,		// (a + b + c)
			PlusArray,		// ["a" + "b" + "c"]
			// List (vertical)
			Line,			// a\nb\nc
			Paragraph,		// a\n\nb\n\nc
			Number,			// 1. a\n 2. b\n 3. c
			Bullet,         // * a\n* b\n* c

			Default = Space,
		}
		public Style style = Style.Undefined;
		
		public enum Formatting
		{
			Undefined,
			Default,
			Limited,
			None,
		}
		public Formatting formatting = Formatting.Undefined;
		protected string _styleName;

		public enum Mode
		{
			Append = 0,
			Replace,	// Replace existing
			Discard,	// Remove existing
			Exclusive,	// Can only be replaced by another exclusive
			Exclude,	// Can be replaced by another
			Parent,		// Require parent
			Sibling,	// Require sibling

			Default = Append,
		}
		public Mode mode = Mode.Default;
		public int order = DefaultOrder;
		public bool isConditionalOnMode	{ get { return mode == Mode.Sibling || mode == Mode.Parent; } }
		public bool isPerActor = false;

		protected static readonly int DefaultOrder = 500;
		protected static readonly int MaxOrder = 1000;

		public bool LoadFromXml(XmlNode xmlNode)
		{
			string id = xmlNode.GetAttribute("path", null);
			if (StringHandle.IsNullOrEmpty(id))
				id = xmlNode.GetAttribute("id", null);
			if (StringHandle.IsNullOrEmpty(id))
				return false;

			this.id = new BlockID(id);

			if (xmlNode.HasAttribute("rule"))
				condition = Rule.Parse(xmlNode.GetAttribute("rule"));

			_styleName = xmlNode.GetAttribute("style", null);
			style = BlockStyles.FromString(_styleName);
			mode = xmlNode.GetAttributeEnum("mode", Mode.Default);
			isPerActor = xmlNode.GetAttributeBool("per-actor", false);
			
			string sOrder = xmlNode.GetAttribute("order", null);
			if (string.IsNullOrEmpty(sOrder) == false)
			{
				int iOrder;
				if (int.TryParse(sOrder, out iOrder))
				{
					order = iOrder;
					if (order == 0 && sOrder.BeginsWith("-")) // Negative zero
						order = MaxOrder;
					else if (order >= 0)
						order = Math.Min(Math.Max(order, 0), MaxOrder);
					else
						order = Math.Min(Math.Max(MaxOrder + order, 0), MaxOrder);
				}
				else
					order = DefaultOrder;
			}
			else
				order = DefaultOrder;

			if (xmlNode.HasAttribute("format"))
				formatting = EnumHelper.FromString(xmlNode.GetAttribute("format"), Formatting.Undefined);
			else
				formatting = Formatting.Undefined;

			value = xmlNode.GetTextValue(BlockBuilder.InnerBlock);
			return !string.IsNullOrEmpty(value);
		}

		public void SaveToXml(XmlNode xmlNode)
		{
			xmlNode.AddAttribute("path", id.ToString());
			if (string.IsNullOrEmpty(_styleName) == false)
				xmlNode.AddAttribute("style", _styleName);
			if (order != DefaultOrder)
				xmlNode.AddAttribute("order", order);
			if (mode != Mode.Default)
				xmlNode.AddAttribute("mode", EnumHelper.ToString(mode).ToLowerInvariant());
			if (formatting != Formatting.Undefined)
				xmlNode.AddAttribute("format", EnumHelper.ToString(formatting).ToLowerInvariant());
			if (isPerActor)
				xmlNode.AddAttribute("per-actor", true);
			if (condition != null)
				xmlNode.AddAttribute("rule", condition.ToString());
			xmlNode.AddTextValue(value);
		}

		public override int GetHashCode()
		{
			return Utility.MakeHashCode(
				id,
				condition,
				value,
				style,
				mode,
				formatting,
				order,
				isPerActor);
		}

		public virtual object Clone()
		{
			return new Block() {
				id = this.id,
				value = this.value,
				condition = this.condition,
				style = this.style,
				mode = this.mode,
				formatting = this.formatting,
				order = this.order,
				isPerActor = this.isPerActor,
			};
		}
	}

	public class AttributeBlock : Block
	{
		public string name;

		public new bool LoadFromXml(XmlNode xmlNode)
		{
			name = xmlNode.GetValueElement("Name", null).SingleLine();
			value = xmlNode.GetValueElement("Value", null);

			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
				return false;

			id = new BlockID(name.Replace("/", "_"));

			if (xmlNode.HasAttribute("rule"))
				condition = Rule.Parse(xmlNode.GetAttribute("rule"));

			mode = xmlNode.GetAttributeEnum("mode", Mode.Default);
			if (mode == Mode.Discard)
				return false; // Not allowed for attributes

			order = xmlNode.GetAttributeInt("order", DefaultOrder);
			_styleName = xmlNode.GetAttribute("style", "attribute");
			style = BlockStyles.FromString(_styleName);

			if (xmlNode.HasAttribute("format"))
				formatting = EnumHelper.FromString(xmlNode.GetAttribute("format"), Formatting.Undefined);
			else
				formatting = Formatting.Undefined;
			return true;
		}

		public new void SaveToXml(XmlNode xmlNode)
		{
			if (string.IsNullOrEmpty(_styleName) == false)
				xmlNode.AddAttribute("style", _styleName);
			if (order != DefaultOrder)
				xmlNode.AddAttribute("order", order);
			if (mode != Mode.Default)
				xmlNode.AddAttribute("mode", EnumHelper.ToString(mode).ToLowerInvariant());
			if (formatting != Formatting.Undefined)
				xmlNode.AddAttribute("format", EnumHelper.ToString(formatting).ToLowerInvariant());
			if (condition != null)
				xmlNode.AddAttribute("rule", condition.ToString());

			xmlNode.AddValueElement("Name", name);
			xmlNode.AddValueElement("Value", value);
		}

		public override int GetHashCode()
		{
			return Utility.MakeHashCode(
				id,
				name,
				condition,
				value,
				style,
				mode,
				formatting,
				order);
		}

		public override object Clone()
		{
			return new AttributeBlock() {
				id = this.id,
				name = this.name,
				value = this.value,
				condition = this.condition,
				style = this.style,
				mode = this.mode,
				formatting = this.formatting,
				order = this.order,
			};
		}
	}

	public class BlockBuilder
	{
		public static readonly string InnerBlock = "<##INNER##>";

		private struct Entry
		{
			public Block.Style style;
			public int order;
			public string text;
			public Block.Mode mode;
			public Block.Formatting formatting;
			public bool hasInnerBlock;
		}

		public struct AttributeEntry
		{
			public BlockID id;
			public string name;
			public Block.Style style;
			public int order;
			public string text;
			public Block.Formatting formatting;
			public Block.Mode mode;
		}

		private struct BlockOutput
		{
			public BlockID id;
			public string text;
		}

		public IEnumerable<BlockID> blocks { get { return _entries.Keys; } }
		private Dictionary<BlockID, List<Entry>> _entries = new Dictionary<BlockID, List<Entry>>();

		public IEnumerable<BlockID> finishedBlocks { get { return _finishedBlocks.Keys; } }
		private Dictionary<BlockID, BlockOutput> _finishedBlocks = new Dictionary<BlockID, BlockOutput>();

		public IEnumerable<AttributeEntry> attributes { get { return _attributeEntries; } }
		private List<AttributeEntry> _attributeEntries = new List<AttributeEntry>();

		public BlockBuilder()
		{
		}

		public void Add(Block blockInfo, string text)
		{
			Add(blockInfo.id, text, blockInfo.style, blockInfo.mode, blockInfo.formatting, blockInfo.order);
		}

		public bool Add(BlockID blockID, string text, Block.Style style, Block.Mode mode, Block.Formatting formatting, int order)
		{
			if (string.IsNullOrEmpty(text) || BlockID.IsNullOrEmpty(blockID))
				return false;
			
			BlockID.Root root = blockID.root;
			if (root == BlockID.Root.Undefined)
			{
				root = blockID.root;
				if (root == BlockID.Root.Undefined)
					root = BlockID.Root.Persona;
				blockID = BlockID.EnsureRoot(blockID, root);
			}

			List<Entry> existingBlocks;
			_entries.TryGetValue(blockID, out existingBlocks);

			bool hasInner = text.IndexOf(InnerBlock, StringComparison.OrdinalIgnoreCase) != -1;
			Block.Mode? existingMode = null;
			if (existingBlocks != null && existingBlocks.Count > 0)
				existingMode = existingBlocks[existingBlocks.Count - 1].mode;

			if (existingBlocks == null)
				_entries.Add(blockID, new List<Entry>()); // Add
			else if (mode == Block.Mode.Exclusive)
				_entries[blockID].Clear(); // Replace
			else if (mode == Block.Mode.Discard)
			{
				_entries = _entries.Where(e => !(e.Key == blockID || e.Key.IsChildOf(blockID))) // Remove (incl. children)
					.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
				return false; // Don't add
			}
			else if (existingBlocks.Count > 0 && mode == Block.Mode.Exclude)
			{
				if (existingMode == Block.Mode.Exclude)
					_entries[blockID].Clear(); // Replace
				else
					return false; // Don't add
			}
			else if (existingBlocks.Count > 0
				&& (mode == Block.Mode.Replace
					|| existingMode == Block.Mode.Exclude
					|| existingMode == Block.Mode.Sibling
					|| existingMode == Block.Mode.Parent))
			{
				_entries[blockID].Clear(); // Replace
			}
			else if (existingMode == Block.Mode.Exclusive)
				return false; // Can't add
			else if (hasInner && _entries[blockID].ContainsAny(e => e.hasInnerBlock))
				return false; // Can't have multiple inner blocks

			// Default style
			if (blockID.depth == 1 && style == Block.Style.Undefined)
				style = Block.Style.Default;

			_entries[blockID].Add(new Entry() {
				text = text,
				style = style,
				order = order,
				mode = mode,
				formatting = formatting,
				hasInnerBlock = hasInner,
			});
			return true;
		}

		public void AddAttribute(AttributeBlock attribute, string attributeName, string attributeLabel, string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return; // Empty string

			BlockID blockID = string.Concat("persona/attributes/", attributeName.Replace('/', '_'));

			List<Entry> existingBlocks;
			bool bCanAdd = true;
			if (_entries.TryGetValue(blockID, out existingBlocks))
			{
				if (existingBlocks.Count > 0)
				{
					Entry entry = existingBlocks[existingBlocks.Count - 1];
					bCanAdd = entry.mode == Block.Mode.Exclude
						|| entry.mode == Block.Mode.Sibling
						|| entry.mode == Block.Mode.Parent
						|| attribute.mode == Block.Mode.Replace 
						|| attribute.mode == Block.Mode.Exclusive;
				}
			}

			if (bCanAdd)
				Add(blockID, string.Concat(Text.DontProcess(attributeLabel), InnerBlock), attribute.style, attribute.mode, attribute.formatting, attribute.order);

			var valueMode = attribute.isConditionalOnMode ? Block.Mode.Default : attribute.mode;
			if (Add(string.Concat(blockID, "/value"), text, Block.Style.Undefined, valueMode, attribute.formatting, attribute.order))
			{
				_attributeEntries.Add(new AttributeEntry() {
					id = blockID,
					name = attributeName,
					style = attribute.style,
					mode = attribute.mode,
					formatting = attribute.formatting,
					order = attribute.order,
					text = text,
				});
			}
		}

		public void Build()
		{
			List<BlockID> allKeys = _entries.Keys.Distinct().OrderBy(x => x).ToList();

			List<BlockID> removeKeys = new List<BlockID>();

			// Remove childless parents
			for (int i = allKeys.Count - 1; i >= 0; --i)
			{
				var key = allKeys[i];
				var list = _entries[key];
				for (int j = list.Count - 1; j >= 0; --j)
				{
					var entry = list[j];
					if (entry.hasInnerBlock == false)
						continue;
					if (allKeys.ContainsAny(k => k.IsChildOf(key)))
						continue;
					list.RemoveAt(j);
				}
				if (list.Count == 0)
				{
					allKeys.RemoveAt(i);
					removeKeys.Add(key);
				}
			}

			foreach (var key in removeKeys)
				_entries.Remove(key);
			removeKeys.Clear();

			// Remove lonely children (mode = parent)
			foreach (var kvp in _entries.Where(kvp => kvp.Key.depth > 0).OrderBy(kvp => kvp.Key).ToList())
			{
				BlockID key = kvp.Key;
				var entries = kvp.Value;

				for (int i = entries.Count - 1; i >= 0; --i)
				{
					if (entries[i].mode != Block.Mode.Parent)
						continue;

					BlockID parentID = key.GetParent();
					if (allKeys.Contains(parentID) == false)
						entries.RemoveAt(i);
				}
				if (entries.Count == 0)
				{
					allKeys.Remove(key);
					_entries.Remove(key);
				}
			}

			// Remove lonely siblings (mode = sibling)
			foreach (var kvp in _entries)
			{
				BlockID key = kvp.Key;
				var siblings = kvp.Value.Where(e => e.mode == Block.Mode.Sibling).ToList();
				if (siblings.IsEmpty())
					continue;
				
				int count = siblings.Count();
				if (count < kvp.Value.Count)
					continue;

				// Must have at least one sibling that doesn't also need a sibling
				if (_entries.ContainsAny(k => (k.Key == key || k.Key.IsSiblingOf(key)) && k.Value.ContainsAny(e => e.mode != Block.Mode.Sibling)) )
					continue;

				// Remove (including children)
				foreach (var x in _entries.Keys.Where(k => k == key || k.IsSiblingOf(key) || k.IsChildOf(key)))
					removeKeys.Add(x);
			}
			foreach (var key in removeKeys)
				_entries.Remove(key);

			if (_entries.Count == 0)
				return;

			IEnumerable<BlockID> keys;
			if (_entries.Count == 1)
				keys = _entries.Keys;
			else
			{
				var orderedKeys = _entries
					.OrderBy(kvp => kvp.Value.First().order)
					.Select(kvp => kvp.Key)
					.Distinct()
					.ToList();

				var shortestKeys = new List<BlockID>();
				foreach (var key in orderedKeys)
				{
					if (shortestKeys.ContainsAny(k => key.IsChildOf(k))) 
						continue;

					int childIndex = shortestKeys.FindIndex(k => k.IsChildOf(key)); 
					if (childIndex != -1)
					{
						shortestKeys.RemoveAt(childIndex);
						shortestKeys.Insert(childIndex, key);
					}
					else
						shortestKeys.Add(key);
				}
				keys = shortestKeys;
			}

			foreach (var key in keys)
			{
				string text = BuildBlock(key);

				_finishedBlocks.TryAdd(key, new BlockOutput() {
					id = key,
					text = text,
				});
			}
		}

		public string Build(BlockID blockID)
		{
			BlockOutput output;
			if (_finishedBlocks.TryGetValue(blockID, out output))
				return output.text;

			string text = BuildBlock(blockID);
			_finishedBlocks.TryAdd(blockID, new BlockOutput() {
				id = blockID,
				text = text,
			});

			RemoveBlock(blockID);
			return text;
		}

		private string BuildBlock(BlockID blockID, HashSet<BlockID> builtBlocks = null, Block.Style defaultMode = Block.Style.Default, Block.Formatting defaultFormatting = Block.Formatting.Undefined)
		{
			int depth = blockID.depth;

			if (builtBlocks == null)
				builtBlocks = new HashSet<BlockID>();

			List<BlockID> allInnerKeys = _entries
				.OrderBy(b => b.Value.Min(x => x.order))
				.Select(b => b.Key)
				.Where(id => id != blockID
					&& id.IsChildOf(blockID))
				.ToList();

			List<BlockID> innerKeys = allInnerKeys
				.Where(id => id.depth == blockID.depth + 1)
				.ToList();

			List<string> blockTexts = new List<string>();

			List<Entry> list;
			if (_entries.TryGetValue(blockID, out list))
			{
				// Use same mode as siblings?
				Block.Style style = Block.Style.Undefined;
				for (int i = 0; i < list.Count; ++i)
				{
					if (list[i].style > style)
						style = list[i].style;
				}
				// Use same mode as parent?
				if (style == Block.Style.Undefined)
					style = defaultMode;

				for (int i = 0; i < list.Count; ++i)
				{
					string blockText = list[i].text;
					int pos_inner = blockText.IndexOf(InnerBlock, System.StringComparison.OrdinalIgnoreCase);

					var formatting = list[i].formatting;
					var innerFormatting = formatting;
					if (innerFormatting == Block.Formatting.Undefined && BlockStyles.IsVerticalList(style))
						innerFormatting = Block.Formatting.Default;
					else if (innerFormatting == Block.Formatting.Undefined && BlockStyles.IsHorizontalList(style))
						innerFormatting = Block.Formatting.Limited; // No capitalization in horizintal lists
					if (formatting == Block.Formatting.Undefined)
						formatting = defaultFormatting;

					if (pos_inner != -1) // Has inner block(s)
					{
						var innerBlocks = new HashSet<BlockID>();
						List<string> innerEntries = new List<string>();

						for (int j = 0; j < innerKeys.Count; ++j)
						{
							string innerText = BuildBlock(innerKeys[j], innerBlocks, style, innerFormatting).Trim();
							if (string.IsNullOrWhiteSpace(innerText) == false)
								innerEntries.Add(innerText);
						}

						builtBlocks.UnionWith(innerBlocks);

						// Resolve orphans
						var orphanKeys = allInnerKeys.Except(innerBlocks).OrderBy(id => id.handle).ToList();
						if (orphanKeys.Count > 0)
						{
							int j = 0;
							for (; j < orphanKeys.Count;)
							{
								var orphanBlocks = new HashSet<BlockID>();
								string innerText = BuildBlock(orphanKeys[j], orphanBlocks, style, innerFormatting).Trim();
								if (string.IsNullOrWhiteSpace(innerText) == false)
									innerEntries.Add(innerText);

								if (orphanBlocks.Count > 0)
								{
									builtBlocks.UnionWith(orphanBlocks);
									orphanKeys = orphanKeys.Except(orphanBlocks).ToList();
									j = 0;
								}
								else
								{
									++j;
								}
							}
						}

						if (innerEntries.Count == 0)
						{
							blockText = "";
							continue; // No inner text, skip to next block
						}
						else
						{
							// Split inner entries
							if (BlockStyles.IsHorizontalList(style) || BlockStyles.IsVerticalList(style))
							{
								innerEntries = innerEntries
									.SelectMany(s => Utility.ListFromDelimitedString(s, new string[] { Text.Delimiter, ";" }))
									.DistinctBy(s => s.ToLowerInvariant())
									.ToList();
							}
							else
							{
								innerEntries = innerEntries
									.SelectMany(s => Utility.ListFromDelimitedString(s, Text.Delimiter))
									.ToList();
							}

							string innerBlock;
							if (style == Block.Style.Line)
								innerBlock = string.Join(Text.Break, innerEntries);
							else if (style == Block.Style.Paragraph)
								innerBlock = string.Join(Text.ParagraphBreak, innerEntries);
							else if (style == Block.Style.Comma)
								innerBlock = string.Join(", ", innerEntries);
							else if (style == Block.Style.Stop)
								innerBlock = string.Concat(string.Join(" ", innerEntries), ".");
							else if (style == Block.Style.CommaStop)
								innerBlock = string.Concat(string.Join(", ", innerEntries), ".");
							else if (style == Block.Style.Group)
								innerBlock = string.Concat("(", string.Join(", ", innerEntries), ")");
							else if (style == Block.Style.SemiGroup)
								innerBlock = string.Concat("(", string.Join("; ", innerEntries), ")");
							else if (style == Block.Style.PlusGroup)
								innerBlock = string.Concat("(", string.Join(" + ", innerEntries), ")");
							else if (style == Block.Style.Semicolon)
								innerBlock = string.Join("; ", innerEntries);
							else if (style == Block.Style.Plus)
								innerBlock = string.Join(" + ", innerEntries);
							else if (style == Block.Style.Hyphen)
								innerBlock = string.Join(" - ", innerEntries);
							else if (style == Block.Style.PlusList)
							{
								StringBuilder sb = new StringBuilder();
								for (int n = 0; n < innerEntries.Count; ++n)
								{
									if (n > 0)
										sb.Append(" + ");
									sb.Append('"');
									sb.Append(innerEntries[n]);
									sb.Append('"');
								}
								innerBlock = sb.ToString();
							}
							else if (style == Block.Style.CommaList)
							{
								StringBuilder sb = new StringBuilder();
								for (int n = 0; n < innerEntries.Count; ++n)
								{
									if (n > 0)
										sb.Append(", ");
									sb.Append('"');
									sb.Append(innerEntries[n]);
									sb.Append('"');
								}
								innerBlock = sb.ToString();
							}
							else if (style == Block.Style.Array)
							{
								StringBuilder sb = new StringBuilder();
								sb.Append("[");
								for (int n = 0; n < innerEntries.Count; ++n)
								{
									if (n > 0)
										sb.Append(", ");
									sb.Append('"');
									sb.Append(innerEntries[n]);
									sb.Append('"');
								}
								sb.Append("]");
								innerBlock = sb.ToString();
							}
							else if (style == Block.Style.PlusArray)
							{
								StringBuilder sb = new StringBuilder();
								sb.Append("[");
								for (int n = 0; n < innerEntries.Count; ++n)
								{
									if (n > 0)
										sb.Append(" + ");
									sb.Append('"');
									sb.Append(innerEntries[n]);
									sb.Append('"');
								}
								sb.Append("]");
								innerBlock = sb.ToString();
							}
							else if (style == Block.Style.Number && innerEntries.Count > 1)
							{
								StringBuilder sb = new StringBuilder();
								for (int n = 0; n < innerEntries.Count; ++n)
								{
									sb.Append(Text.SoftBreak);
									sb.AppendFormat("{0}. ", n + 1);
									sb.Append(innerEntries[n]);
								}
								innerBlock = sb.ToString();
							}
							else if (style == Block.Style.Bullet && innerEntries.Count > 1)
							{
								StringBuilder sb = new StringBuilder();
								for (int n = 0; n < innerEntries.Count; ++n)
								{
									sb.Append(Text.SoftBreak);
									sb.Append("- ");
//									sb.Append("\u2022 ");
									sb.Append(innerEntries[n]);
								}
								innerBlock = sb.ToString();
							}
							else if (style == Block.Style.Quote)
							{
								StringBuilder sb = new StringBuilder();
								sb.Append('"');
								sb.Append(string.Join(" ", innerEntries));
								sb.Append('"');
								innerBlock = sb.ToString();
							}
							else if (style == Block.Style.Bracket)
							{
								StringBuilder sb = new StringBuilder();
								sb.Append('[');
								sb.Append(string.Join(" ", innerEntries));
								sb.Append(']');
								innerBlock = sb.ToString();
							}
							else if (style == Block.Style.Parenthesis)
							{
								StringBuilder sb = new StringBuilder();
								sb.Append('(');
								sb.Append(string.Join(" ", innerEntries));
								sb.Append(')');
								innerBlock = sb.ToString();
							}
							else if (style == Block.Style.Curly)
							{
								StringBuilder sb = new StringBuilder();
								sb.Append('{');
								sb.Append(string.Join(" ", innerEntries));
								sb.Append('}');
								innerBlock = sb.ToString();
							}
							else if (style == Block.Style.Join)
							{
								innerBlock = string.Join("", innerEntries);
							}
							else
							{
								innerBlock = string.Join(Text.Separator, innerEntries);
							}
							blockText = blockText.Replace(InnerBlock, innerBlock, true);
						}
					}
					else
					{
						if (BlockStyles.IsHorizontalList(style))
						{
							blockText = blockText
								.Replace(",", Text.Delimiter)
								.Replace(";", Text.Delimiter);
						}
						builtBlocks.Add(blockID);
					}

					if (formatting == Block.Formatting.None)
						blockText = Text.DontProcess(blockText);
					else if (formatting == Block.Formatting.Limited)
						blockText = Text.DontProcess(Text.Process(blockText, Text.EvalOption.LimitedBlockFormatting));
					else if (formatting == Block.Formatting.Default)
						blockText = Text.Process(blockText, Text.EvalOption.StandardBlockFormatting);

					blockTexts.Add(blockText.Trim());
				}

				if (blockTexts.Count > 0)
				{
					builtBlocks.Add(blockID);
					return string.Join(Text.Delimiter, blockTexts);
				}
			}

			builtBlocks.Add(blockID);
			return string.Empty;
		}

		public string GetFinishedBlock(BlockID blockID)
		{
			BlockOutput output;
			if (_finishedBlocks.TryGetValue(blockID, out output))
				return output.text;
			return "";
		}

		public void ClearFinishedBlocks()
		{
			_finishedBlocks.Clear();
		}

		public bool HasBlock(BlockID name)
		{
			return _entries.ContainsKey(name.ToString());
		}

		public bool BlockHasChildren(BlockID name, bool bWithContent)
		{
			string key = string.Concat(name.ToString(), "/");
			var children = _entries
				.Where(kvp => kvp.Key.ToString().BeginsWith(key))
				.SelectMany(kvp => kvp.Value);

			if (bWithContent)
				children = children.Where(b => b.hasInnerBlock == false);
			return !children.IsEmpty();
		}

		public void RemoveBlock(BlockID blockID, bool includeChildren = true)
		{
			_entries = _entries.Where(kvp => !(kvp.Key == blockID || (includeChildren && kvp.Key.IsChildOf(blockID))))
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
			_finishedBlocks = _finishedBlocks.Where(kvp => !(kvp.Key == blockID || (includeChildren && kvp.Key.IsChildOf(blockID))))
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		}
	}
}
