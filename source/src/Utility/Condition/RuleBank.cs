using System.Collections.Generic;
using System.Xml;

namespace Ginger
{
	public interface IRuleSupplier
	{
		RuleBank GetRuleBank();
	}

	public class RuleBank : IRuleSupplier, IXmlLoadable
	{
		public IEnumerable<KeyValuePair<string, ICondition>> rules { get { return _rules; } }
		private Dictionary<string, ICondition> _rules = new Dictionary<string, ICondition>();

		public bool HasRule(string ruleID)
		{
			return _rules.ContainsKey(ruleID.ToLowerInvariant());
		}

		public ICondition GetRule(string ruleID)
		{
			ICondition condition;
			if (_rules.TryGetValue(ruleID.ToLowerInvariant(), out condition))
				return condition;
			return null;
		}

		public bool EvaluateRuleByID(string ruleID, Context context, EvaluationCookie cookie, out bool result)
		{
			if (context == null)
			{
				result = false;
				return false;
			}

			if (_rules.ContainsKey(ruleID))
			{
				result = _rules[ruleID].Evaluate(context, cookie);
				return true;
			}

			result = false;
			return false;
		}

		public int Count()
		{
			return _rules.Count;
		}

		public void Clear()
		{
			_rules.Clear();
		}

		public bool IsEmpty()
		{
			return _rules.Count == 0;
		}

		public bool LoadFromXml(XmlNode xmlNode)
		{
			var ruleNode = xmlNode.GetFirstElement("Rule");
			while (ruleNode != null)
			{
				string ruleID = ruleNode.GetAttribute("id", "").ToLowerInvariant();
				if (string.IsNullOrEmpty(ruleID) == false)
				{
					var ids = Utility.ListFromCommaSeparatedString(ruleNode.GetAttribute("id"));

					var condition = Rule.LoadFromXml(ruleNode);
					if (condition != null)
					{
						foreach (var id in ids)
							_rules.TryAdd(id, condition);
					}
				}
				ruleNode = ruleNode.GetNextSibling();
			}

			return true;
		}

		public RuleBank GetRuleBank()
		{
			return this;
		}

		public void AppendRules(RuleBank other)
		{
			foreach (var kvp in other._rules)
			{
				if (!_rules.ContainsKey(kvp.Key))
					_rules.Add(kvp.Key, kvp.Value);
			}
		}

		public void ValidateRules()
		{
			List<string> removeList = new List<string>();
			
			// Validate
			foreach (var kvp in _rules)
			{
				if (kvp.Value.Validate(new IRuleSupplier[] { this, Current.Strings }) == false)
				{
					removeList.Add(kvp.Key);
				}
			}

			foreach (var ruleID in removeList)
				_rules.Remove(ruleID);

			removeList.Clear();

			// Resolve rule references
			foreach (var kvp in _rules)
			{
				var cookie = new ResolveCookie(new HashSet<string>());
				var condition = kvp.Value;
				if (ResolveRule(ref condition, null, cookie, this) == false)
					removeList.Add(kvp.Key);
			}

			foreach (var ruleID in removeList)
				_rules.Remove(ruleID);
		}
		
		private struct ResolveCookie
		{
			public ResolveCookie(HashSet<string> refList)
			{
				references = refList;
				depth = 0;
			}

			public ResolveCookie(ResolveCookie cookie)
			{
				references = new HashSet<string>(cookie.references);
				depth = cookie.depth + 1;
			}

			public HashSet<string> references;
			public int depth;
		}

		public static bool ResolveRule(ref ICondition condition, RuleBank ruleBank)
		{
			var cookie = new ResolveCookie(new HashSet<string>());
			if (ResolveRule(ref condition, null, cookie, ruleBank))
				return true;

			condition = null;
			return false;
		}

		private static bool ResolveRule(ref ICondition condition, ICondition parent, ResolveCookie cookie, RuleBank ruleBank)
		{
			if (cookie.depth > 64)
			{
				return false;
			}

			// Depth first
			var conditions = condition.conditions;
			if (conditions != null)
			{
				for (int i = 0; i < conditions.Length; ++i)
				{
					if (ResolveRule(ref conditions[i], condition, new ResolveCookie(cookie), ruleBank) == false)
						return false;
				}
				return true;
			}
			
			// Leaf node
			if (condition is Rule)
			{
				var rule = condition as Rule;
				if (rule.expressionType == Rule.ExpressionType.Invalid)
				{
					return false;
				}

				if (rule.expressionType != Rule.ExpressionType.RuleReference)
					return true;

				if (cookie.references.Contains(rule.expression))
					return false; // Loop!

				cookie.references.Add(rule.expression);

				// Get rule id
				string otherRuleID;
				int colon = rule.expression.IndexOf(':');
				if (colon != -1)
					otherRuleID = rule.expression.Substring(colon + 1).Trim();
				else
					otherRuleID = rule.expression;

				ICondition otherRule = ruleBank.GetRule(otherRuleID);
				if (otherRule == null)
				{
					rule.expressionType = Rule.ExpressionType.Invalid;
					return false; // Invalid
				}

				if (ResolveRule(ref otherRule, condition, new ResolveCookie(cookie), ruleBank) == false)
				{
					rule.expressionType = Rule.ExpressionType.Invalid;
					return false; // Invalid
				}

				if (colon != -1)
					return true; // Don't mess with rules with selectors.

				// Replace node
				if (parent != null)
				{
					var siblings = parent.conditions;
					for (int i = 0; i < siblings.Length; ++i)
					{
						if (object.ReferenceEquals(siblings[i], condition))
						{
							siblings[i] = otherRule;
							break;
						}
					}
				}
				else
				{
					condition = otherRule;
				}
			}
			return true;
		}

		public override int GetHashCode()
		{
			int hash = 0x3C34CECB;
			foreach (var kvp in _rules)
				hash ^= Utility.MakeHashCode(Utility.HashOption.Ordered, kvp.Key, kvp.Value);
			return hash;
		}
	}
}
