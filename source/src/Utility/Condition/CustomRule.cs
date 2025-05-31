using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace Ginger
{
	public sealed class Rule : IConditionalExpression
	{
		public static IRandom DefaultRandomizer = new RandomDefault((int)System.DateTime.Now.Ticks ^ 990937417);

		public enum ExpressionType
		{
			Unknown = 0,
			Invalid,
			Expression,
			RuleReference,
			ValueComparison,
		}

		public ExpressionType expressionType = ExpressionType.Unknown;

		public string expression
		{
			get { return _expression; }
			set { _expression = value.ToLowerInvariant(); }
		}
		private string _expression;

		public Rule()
		{
		}

		private Rule(string ruleString)
		{
			_expression = ruleString.ToLowerInvariant();
		}

		public ICondition[] conditions { get { return null; } }

		public bool Evaluate(IContextual contextual, EvaluationCookie cookie = default(EvaluationCookie))
		{
			if (contextual == null)
				return false;

			var context = contextual.context;

			if (expressionType == ExpressionType.Unknown)
				Validate(cookie.ruleSuppliers);

			switch (expressionType)
			{
			case ExpressionType.Invalid:
				return false;

			case ExpressionType.RuleReference:
			{
				if (cookie.ruleSuppliers != null)
				{
					foreach (var ruleSupplier in cookie.ruleSuppliers)
					{
						bool bResult;
						if (ruleSupplier.GetRuleBank().EvaluateRuleByID(expression, context, cookie, out bResult))
							return bResult;
					}
				}
				break;
			}
			case ExpressionType.ValueComparison:
			{
				ValueComparison comparison = ValueComparison.Parse(expression);
				if (comparison.isValid)
					return comparison.Evaluate(context);
				break;
			}
			default:
				break;
			}

			return Evaluate(_expression, context, cookie);
		}

		public static bool Evaluate(string expression, IContextual contextual, EvaluationCookie cookie = default(EvaluationCookie))
		{
			if (string.IsNullOrEmpty(expression))
				return false;

			if (contextual == null)
				return false;

			var context = contextual.context;

			// Evaluate rule
			if (cookie.ruleSuppliers != null)
			{
				foreach (var ruleSupplier in cookie.ruleSuppliers)
				{
					bool bCustomRuleResult;
					if (ruleSupplier.GetRuleBank().EvaluateRuleByID(expression, context, cookie, out bCustomRuleResult))
						return bCustomRuleResult;
				}
			}

			// Comparison expression
			ValueComparison comparison = ValueComparison.Parse(expression);
			if (comparison.isValid)
				return comparison.Evaluate(context);

			// Contextual tag?
			if (context.HasFlag(expression))
				return true;

			// Value exists?
			if (context.values != null && context.values.ContainsKey(expression))
			{
				string value;
				if (context.TryGetValue(expression, out value) && string.IsNullOrEmpty(value) == false)
					return true;
			}

			return false;
		}

		public bool Validate(IRuleSupplier[] ruleSuppliers)
		{
			if (string.IsNullOrEmpty(expression))
			{
				expressionType = ExpressionType.Invalid;
				return false;
			}

			// Number check
			int number;
			if (int.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
				&& number > 0) // [1?...] Expression is a number: check current subject
			{
				expressionType = ExpressionType.Expression;
				return true;
			}
	
			int colon = expression.IndexOf(':');
			if (colon != -1)
			{
				// Random fraction? [1:4?...]
				double nA, nB;
				if (double.TryParse(expression.Substring(0, colon), NumberStyles.Number, CultureInfo.InvariantCulture, out nA)
					&& double.TryParse(expression.Substring(colon + 1), NumberStyles.Number, CultureInfo.InvariantCulture, out nB)
					&& nA > 0 && nB > 0)
				{
					expressionType = ExpressionType.Expression;
					return true;
				}

				// Selector?
				var subExpression = expression.Substring(colon + 1).Trim();

				// Custom rule?
				if (ruleSuppliers == null)
					ruleSuppliers = new IRuleSupplier[] { Current.Strings };

				for (int i = 0; i < ruleSuppliers.Length; ++i)
				{
					if (ruleSuppliers[i].GetRuleBank().HasRule(subExpression))
					{
						expressionType = ExpressionType.RuleReference;
						return true;
					}
				}
			}
			else
			{
				// Custom rule?
				if (ruleSuppliers == null)
					ruleSuppliers = new IRuleSupplier[] { Current.Strings };

				for (int i = 0; i < ruleSuppliers.Length; ++i)
				{
					if (ruleSuppliers[i].GetRuleBank().HasRule(expression))
					{
						expressionType = ExpressionType.RuleReference;
						return true;
					}
				}
			}

			expressionType = ExpressionType.Expression;
			return true;
		}
		
		public static ICondition Parse(string expression, bool bValidate = true)
		{
			ICondition condition;
			if (Condition.ParseExpression<Rule>(expression, out condition) == false)
				return Condition.Never();

			if (bValidate)
			{
				if (condition.Validate(null))
					RuleBank.ResolveRule(ref condition, Current.Strings.GetRuleBank());
			}
			return condition;
		}

		public static bool TryParse(string expression, out ICondition condition, bool bValidate = false)
		{
			if (Condition.ParseExpression<Rule>(expression, out condition) == false)
				return false;

			if (bValidate)
			{
				if (condition.Validate(null))
					RuleBank.ResolveRule(ref condition, Current.Strings.GetRuleBank());
			}
			return true;
		}

		public static ICondition LoadFromXml(XmlNode xmlNode)
		{
			List<ICondition> conditions = new List<ICondition>();

			if (xmlNode.HasTextValue())
			{
				var expression = Utility.CleanExpression(xmlNode.GetTextValue());
				ICondition condition;
				if (Condition.ParseExpression<Rule>(expression, out condition) == false)
					return null;

				conditions.Add(condition);
			}

			var childNode = xmlNode.GetFirstElementAny();
			while (childNode != null)
			{
				if (childNode.Name != "Rule")
				{
					var childCondition = LoadFromXml(childNode);
					if (childCondition != null)
						conditions.Add(childCondition);
				}
				childNode = childNode.GetNextSiblingAny();
			}

			var ruleNode = xmlNode.GetFirstElement("Rule");
			while (ruleNode != null)
			{
				var expression = ruleNode.GetTextValue().Trim().ToLowerInvariant();
				ICondition condition;
				if (Condition.ParseExpression<Rule>(expression, out condition) == false)
					return null;

				conditions.Add(condition);
				ruleNode = ruleNode.GetNextSibling();
			}

			if (conditions.Count == 0)
				return null;

			if (xmlNode.Name == "And")
			{
				return Condition.And(conditions.ToArray());
			}
			else if (xmlNode.Name == "Or")
			{
				return Condition.Or(conditions.ToArray());
			}
			else if (xmlNode.Name == "Xor")
			{
				return Condition.Xor(conditions.ToArray());
			}
			else if (xmlNode.Name == "Not")
			{
				return Condition.Not(Condition.And(conditions.ToArray()));
			}
			else if (xmlNode.Name == "Nor")
			{
				return Condition.Not(Condition.Or(conditions.ToArray()));
			}

			// Default is AND
			if (conditions.Count > 0)
				return Condition.And(conditions.ToArray());

			return null;
		}

		public override string ToString()
		{
			return _expression ?? "";
		}

		public override int GetHashCode()
		{
			return _expression != null ? _expression.GetHashCode() : 0;
		}

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(obj, null))
				return false;

			if (!this.GetType().Equals(obj.GetType()))
				return false;

			Rule p = (Rule)obj;
			return p.GetHashCode() == this.GetHashCode();
		}
	}
}
