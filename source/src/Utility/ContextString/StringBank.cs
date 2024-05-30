using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Ginger
{
	public interface IStringReferenceSupplier
	{
		StringBank.Entry[] FindStrings(StringID stringID, IContextual contextual, IRuleSupplier[] ruleSuppliers, bool prioritize = true);
		bool HasString(StringID stringID, IContextual contextual, IRuleSupplier[] ruleSuppliers);
	}

	public class StringBank : IMacroSupplier, IRuleSupplier, IStringReferenceSupplier, IXmlLoadable
	{
		private static int s_DefaultPriority = 0;
		private static string s_ExplicitElse = "*";

		public class Entry
		{
			public StringID id;
			public string value;
			public ICondition rule = null;
			public int priority = 0;
			public bool hasPriority = false;
			public int depth = 0;
			public bool explicitRule = false;

			public override string ToString()
			{
				return value;
			}

			public override int GetHashCode()
			{
				int hash = Utility.MakeHashCode(
					id,
					value,
					rule,
					priority
					);
				return hash;
			}
		}

		private class __StringNode
		{
			public StringID id;
			public string value;
			public string sRule;
			public ICondition rule;
			public bool explicitRule;
			public int priority = 0;
			public bool hasPriority = false;
			public bool isElseRule;
			public int depth = 0;
			public __StringNode parent;

			public void InheritFromParent()
			{
				if (this.parent == null)
					return;

				// Parent priority
				if (!hasPriority)
					priority = parent.priority;

				// Depth
				depth = this.parent.depth + 1;

				// Prepend parent rule
				if (parent.rule != null)
				{
					explicitRule = rule != null;

					if (rule != null)
						rule = Condition.And(parent.rule, rule);
					else
						rule = Condition.And(parent.rule);
				}
			}
		}

		private Dictionary<StringID, List<Entry>> _strings = new Dictionary<StringID, List<Entry>>();
		private MacroBank _macros = new MacroBank();
		private RuleBank _rules = new RuleBank();

		public int Count { get { return _strings.Values.Sum(s => s.Count); } }

		public bool LoadFromXml(XmlNode xmlNode)
		{
			var nodes = new List<__StringNode>(64);

			__LoadFromXml(xmlNode, null, nodes);
			ResolveStrings(nodes);

			LoadMacrosFromXml(xmlNode);
			LoadRulesFromXml(xmlNode);
			return true;
		}

		private void __LoadFromXml(XmlNode xmlNode, __StringNode parent, List<__StringNode> nodes)
		{
			bool bHasRule = false;
			bool bHasEmptyRule = false;
			List<__StringNode> addedNodes = new List<__StringNode>(32);

			var stringNode = xmlNode.GetFirstElement("String");
			while (stringNode != null)
			{
				// ID
				string sID = stringNode.GetAttribute("id", null);
                if (parent != null)
                {
                    if (string.IsNullOrEmpty(sID) == false)
                        sID = string.Concat(parent.id.ToString(), StringID.Delimiter, sID);
                    else
                        sID = parent.id.ToString();

                }
                StringID sid = StringID.Make(sID);

				// Priority
				int priority;
				bool bHasPriority;
				if (stringNode.HasAttribute("priority"))
				{
					priority = Math.Min(Math.Max(stringNode.GetAttributeInt("priority"), -99999), 99999);
					bHasPriority = true;
				}
				else
				{
					priority = s_DefaultPriority;
					bHasPriority = false;
				}

				// Text value
				string textValue = stringNode.GetTextValue().Trim();

				// Rule (string)
				string sRule = Utility.CleanExpression(stringNode.GetAttribute("rule", null));
				bool bExplicitElse = string.Compare(sRule, s_ExplicitElse, true) == 0;

				bHasRule |= string.IsNullOrEmpty(sRule) == false && !bExplicitElse;
				bHasEmptyRule |= string.IsNullOrEmpty(sRule);

				__StringNode node = new __StringNode()
				{
					id = sid,
					value = textValue,
					sRule = sRule,
					priority = priority,
					hasPriority = bHasPriority,
					parent = parent,
					isElseRule = bExplicitElse,
				};

				nodes.Add(node);
				addedNodes.Add(node);

				// Read child nodes
				if (stringNode.HasChildren())
					__LoadFromXml(stringNode, node, nodes);

				stringNode = stringNode.GetNextSibling();
			}

			// Parse rules (not 'else')
			foreach (var node in addedNodes)
			{
				if (string.IsNullOrEmpty(node.sRule) == false && node.isElseRule == false)
					node.rule = Rule.Parse(node.sRule);
				else if (node.isElseRule)
					node.rule = null;
			}

			// Group nodes by ID in dictionary (GroupBy() is substantially slower)
			Dictionary<StringID, List<__StringNode>> nodesByID = new Dictionary<StringID, List<__StringNode>>();
			foreach (var node in addedNodes)
			{
				if (nodesByID.ContainsKey(node.id))
					nodesByID[node.id].Add(node);
				else
					nodesByID.Add(node.id, new List<__StringNode>() { node });
			}

			// Generate else-rules
			foreach (var grouping in nodesByID.Select(kvp => kvp.Value))		
			{
				if(!(grouping.Count() > 1
					&& grouping.ContainsAny(ee => ee.isElseRule)
					&& grouping.ContainsAny(ee => ee.sRule != null && ee.isElseRule == false)) )
				{
					// Unable to resolve else-rule
					foreach (var entry in grouping.Where(e => e.isElseRule))
					{
						entry.isElseRule = false;
						entry.rule = Condition.Never();
					}
					continue;
				}

				var siblingConditions = grouping
					.Where(e => e.rule != null)
					.Select(e => e.rule)
					.Distinct()
					.ToArray();

				ICondition elseCondition = null;
				if (siblingConditions.Length == 1)
					elseCondition = Condition.Not(siblingConditions[0]);
				else if (siblingConditions.Length > 1)
					elseCondition = Condition.Not(Condition.Or(siblingConditions));

				foreach (var entry in grouping.Where(e => e.isElseRule))
					entry.rule = elseCondition;
			}
		}

		public void SaveToXml(XmlNode xmlNode)
		{
			// Macros
			foreach (var macro in _macros.macros)
			{
				var macroNode = xmlNode.AddElement("Macro");
				macroNode.AddAttribute("id", macro.Key.ToString());
				macroNode.AddTextValue(macro.Value);
			}

			// Wrappers
			foreach (var wrapper in _macros.wrappers)
			{
				var macroNode = xmlNode.AddElement("Wrapper");
				macroNode.AddAttribute("id", wrapper.Key.ToString());
				macroNode.AddTextValue(wrapper.Value);
			}

			// Rules
			foreach (var rule in _rules.rules)
			{
				var ruleNode = xmlNode.AddElement("Rule");
				ruleNode.AddAttribute("id", rule.Key.ToString());
				ruleNode.AddTextValue(rule.Value.ToString());
			}

			// Strings
			foreach (var key in _strings.Keys)
			{
				var entries = _strings[key];
				if (entries.Count == 1) // One string (no shared id)
				{
					var entry = entries[0];
					var stringNode = xmlNode.AddElement("String");
					stringNode.AddAttribute("id", entry.id.ToString());
					stringNode.AddTextValue(entry.value);
					if (entry.rule != null)
						stringNode.AddAttribute("rule", entry.rule.ToString());
					if (entry.hasPriority)
						stringNode.AddAttribute("priority", entry.priority);
				}
				else // Shared id
				{
					var parentNode = xmlNode.AddElement("String");
					parentNode.AddAttribute("id", entries[0].id.ToString());
					foreach (var entry in entries)
					{
						var stringNode = parentNode.AddElement("String");
						stringNode.AddTextValue(entry.value);
						if (entry.rule != null)
							stringNode.AddAttribute("rule", entry.rule.ToString());
						if (entry.hasPriority)
							stringNode.AddAttribute("priority", entry.priority);
					}
				}
			}
		}
		
		private void ResolveStrings(List<__StringNode> nodes)
		{
			foreach (var node in nodes)
			{
				// Prepend parent rules, priority
				node.InheritFromParent();

				// Create final entry
				if (string.IsNullOrEmpty(node.value) == false)
				{
					AddEntry(new Entry()
					{
						id = node.id,
						priority = node.priority,
						hasPriority = node.hasPriority,
						rule = node.rule,
						value = node.value,
						depth = node.depth,
						explicitRule = node.explicitRule,
					});
				}
			}
		}
		
		private void LoadMacrosFromXml(XmlNode xmlNode)
		{
			var macroNode = xmlNode.GetFirstElement("Macro");
			while (macroNode != null)
			{
				_macros.LoadFromXml(macroNode);
				macroNode = macroNode.GetNextSibling();
			}
			var wrapperNode = xmlNode.GetFirstElement("Wrapper");
			while (wrapperNode != null)
			{
				_macros.LoadWrapperFromXml(wrapperNode);
				wrapperNode = wrapperNode.GetNextSibling();
			}
		}

		private void LoadRulesFromXml(XmlNode xmlNode)
		{
			_rules.LoadFromXml(xmlNode);
		}

		public Entry AddEntry(StringID sid, string value)
		{
			if (sid == null || sid.isEmpty)
				return null;

			var entry = new Entry()
			{
				id = sid,
				value = value
			};

			if (!_strings.ContainsKey(sid))
			{
				var list = new List<Entry>();
				list.Add(entry);
				_strings.Add(sid, list);
			}
			else
				_strings[sid].Add(entry);

			return entry;
		}

		private void AddEntry(Entry entry)
		{
			if (entry == null || entry.id.isEmpty)
				return;

			if (!_strings.ContainsKey(entry.id))
			{
				var list = new List<Entry>();
				list.Add(entry);
				_strings.Add(entry.id, list);
			}
			else
				_strings[entry.id].Add(entry);
		}
		
		public void Clear()
		{
			_strings.Clear();
			_macros.Clear();
			_rules.Clear();
		}

		public bool TryGetString(StringID stringID, out string result, IContextual contextual = null, IRandom randomizer = null)
		{
			if (_strings.ContainsKey(stringID))
			{
				var context = contextual != null ? contextual.context : null;
				result = new ContextString(GetSourceString(stringID, context, randomizer)).Evaluate(context, randomizer);
				return true;
			}

			result = default(string);
			return false;
		}

		public string Eval(StringID stringID, IContextual contextual = null, Text.EvalOption options = Text.EvalOption.Default)
		{
			var context = contextual != null ? contextual.context : null;
			return Text.Process(GetString(stringID, context), options);
		}

		public string GetString(StringID stringID, IContextual contextual = null, IRandom randomizer = null)
		{
			var context = contextual != null ? contextual.context : null;
			return GetContextString(stringID, context, randomizer)
				.Evaluate(context, new ContextString.EvaluationConfig() 
				{ 
					referenceSuppliers = new IStringReferenceSupplier[] { this }, 
					macroSuppliers = new IMacroSupplier[] { this }, 
					randomizer = randomizer 
				});
		}

		public string GetString(StringID stringID, IContextual contextual, ContextString.EvaluationConfig evalConfig)
		{
			var context = contextual != null ? contextual.context : null;
			return ContextString.Evaluate(GetSourceString(stringID, context), context, evalConfig);
		}

		public string GetSourceString(StringID stringID, IContextual contextual = null, IRandom randomizer = null)
		{
			if (_strings.ContainsKey(stringID) == false)
				return string.Empty;

			var context = contextual != null ? contextual.context : null;
			var entries = FindStrings(stringID, context);

			if (entries.Length == 0)
				return string.Empty;
			else if (entries.Length == 1)
				return entries[0].value;
			else
			{
				IRandom local_random = randomizer ?? ContextString.DefaultRandomizer;
				return local_random.Item(entries).value;
			}
		}

		public Entry[] GetEntries(StringID stringID, bool includeChildren = true)
		{
			if (includeChildren)
			{
				return _strings.Values
					.SelectMany(list => list)
					.Where(e => e.id.identifier.BeginsWith(stringID.identifier))
					.ToArray();
			}
			else
			{
				return _strings.Values
					.SelectMany(list => list)
					.Where(e => e.id == stringID)
					.ToArray();
			}
		}

		public Entry[] FindStrings(StringID stringID, IContextual contextual = null, IRuleSupplier[] ruleSuppliers = null, bool prioritize = true)
		{
			if (_strings.ContainsKey(stringID) == false)
				return new Entry[0];

			var context = contextual != null ? contextual.context : null;
			Context ruleContext = context != null ? context.ShallowClone() : Context.CreateEmpty();

			IEnumerable<Entry> entries = null;

			// Evaluate rules
			var stringsWithRules = _strings[stringID]
				.Where(e => e.rule != null)
				.ToArray();

			if (stringsWithRules.Count() > 0)
			{
				// Evaluate primary
				entries = stringsWithRules
					.Where(e => e.rule.Evaluate(ruleContext, new EvaluationCookie() {
						ruleSuppliers = ruleSuppliers,
					}));
			}

			if (entries == null || entries.IsEmpty())
			{
				// Pick from remaining non-rule strings
				entries = _strings[stringID]
					.Where(e => e.rule == null);
			}

			if (entries.IsEmpty())
				return new Entry[0];

			// Priority
			if (prioritize)
				entries = PrioritizeEntries(entries);

			// Prioritize explicit rules
			if (stringsWithRules.IsEmpty() == false && entries.Count() > 1)
			{
				return entries.GroupBy(e => e.depth)
					.Select(g =>
					{
						var entriesWithExplicitRule = g.Where(e => e.explicitRule).ToArray();
						if (entriesWithExplicitRule.Count() > 0)
							return entriesWithExplicitRule;
						else
							return g.Select(ee => ee);
					})
					.SelectMany(g => g)
					.ToArray();
			}

			return entries.ToArray();
		}

		public static IEnumerable<Entry> PrioritizeEntries(IEnumerable<Entry> entries)
		{
			if (entries != null)
			{
				// Priority
				int minPriority = entries.Min(entry => entry.priority);
				int maxPriority = entries.Max(entry => entry.priority);
				if (maxPriority > minPriority)
					return entries.Where(entry => entry.priority == maxPriority).ToArray();
			}
			return entries;
		}

		public ContextString GetContextString(StringID stringID, IContextual contextual = null, IRandom randomizer = null)
		{
			return new ContextString(GetSourceString(stringID, contextual, randomizer));
		}

		public bool HasString(StringID stringID, IContextual contextual = null, IRuleSupplier[] ruleSuppliers = null)
		{
			if (_strings.ContainsKey(stringID) == false)
				return false;

			if (contextual == null)
				return true;

			// Check for valid strings
			var entries = _strings[stringID];
			if (entries.Count == 0)
				return false;

			if (entries.ContainsAny(e => e.rule == null))
				return true;

			foreach (var entry in entries)
			{
				if (entry.rule.Evaluate(contextual, 
					new EvaluationCookie() {
						ruleSuppliers = ruleSuppliers,
					}))
					return true;
			}

			return false;
		}

		public MacroBank GetMacroBank()
		{
			return _macros;
		}

		public RuleBank GetRuleBank()
		{
			return _rules;
		}

		public override int GetHashCode()
		{
			int hash = 0x325A0981;
			foreach (var entries in _strings.Values)
				hash ^= Utility.MakeHashCode(entries, Utility.HashOption.None);

			hash ^= _macros.GetHashCode();
			hash ^= _rules.GetHashCode();

			return hash;
		}
	}
}