using System.Text;

namespace Ginger
{
	public struct EvaluationCookie
	{
		public IRandom randomizer;
		public IRuleSupplier[] ruleSuppliers;
		private int value;

		public bool Next()
		{
			return ++value < 32;
		}
	}

	public interface ICondition
	{
		ICondition[] conditions { get; }
		bool Evaluate(IContextual contextual, EvaluationCookie cookie);
		bool Validate(IRuleSupplier[] ruleSuppliers);
	}

	public interface IConditionalExpression : ICondition
	{
		string expression { get; set; }
	}

	public abstract class UnaryOperatorCondition<T> : ICondition where T : ICondition
	{
		protected ICondition _condition
		{
			get { return _conditions[0]; }
			set { _conditions[0] = value; }
		}
		protected ICondition[] _conditions = new ICondition[1] { null };

		public ICondition[] conditions { get { return _conditions; } }

		public abstract bool Evaluate(IContextual contextual, EvaluationCookie cookie = default(EvaluationCookie));

		public bool Validate(IRuleSupplier[] ruleSuppliers)
		{
			if (_conditions == null || _conditions.Length == 0)
				return false;

			for (int i = 0; i < _conditions.Length; ++i)
			{
				if (_conditions[i] == null || _conditions[i].Validate(ruleSuppliers) == false)
					return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			string ruleString = ToString();
			return ruleString.GetHashCode();
		}
	}

	public abstract class BinaryOperatorCondition<T> : ICondition where T : ICondition
	{
		protected ICondition[] _conditions;

		public ICondition[] conditions { get { return _conditions; } }

		public abstract bool Evaluate(IContextual contextual, EvaluationCookie cookie = default(EvaluationCookie));

		public bool Validate(IRuleSupplier[] ruleSuppliers)
		{
			if (_conditions == null || _conditions.Length == 0)
				return false;

			for (int i = 0; i < _conditions.Length; ++i)
			{
				if (_conditions[i] == null || _conditions[i].Validate(ruleSuppliers) == false)
					return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			string ruleString = ToString();
			return ruleString.GetHashCode();
		}
	}

	public class AndCondition : BinaryOperatorCondition<AndCondition>
	{
		public AndCondition(params ICondition[] conditions)
		{
			_conditions = conditions;
		}

		public override bool Evaluate(IContextual context, EvaluationCookie cookie)
		{
			if (_conditions == null || _conditions.Length == 0)
				return false;

			for (int i = 0; i < _conditions.Length; ++i)
				if (_conditions[i].Evaluate(context, cookie) == false)
					return false;
			return true;
		}

		public override string ToString()
		{
			if (_conditions == null || _conditions.Length == 0)
				return "";

			if (_conditions.Length == 1)
				return _conditions[0].ToString();

			StringBuilder sb = new StringBuilder();
			sb.Append('(');
			sb.Append(_conditions[0].ToString());
			for (int i = 1; i < _conditions.Length; ++i)
			{
				sb.Append(" and ");
				sb.Append(_conditions[i].ToString());
			}
			sb.Append(')');
			return sb.ToString();
		}

		//public override int GetHashCode()
		//{
		//	int hash = 1141917299;
		//	for (int i = 0; i < _conditions.Length; ++i)
		//		hash ^= 7 * _conditions[i].GetHashCode();
		//	return hash;
		//}
	}

	public class OrCondition : BinaryOperatorCondition<OrCondition>
	{
		public OrCondition(params ICondition[] conditions)
		{
			_conditions = conditions;
		}

		public override bool Evaluate(IContextual context, EvaluationCookie cookie)
		{
			if (_conditions == null || _conditions.Length == 0)
				return false;

			for (int i = 0; i < _conditions.Length; ++i)
			{
				if (_conditions[i].Evaluate(context, cookie))
					return true;
			}
			return false;
		}

		public override string ToString()
		{
			if (_conditions == null || _conditions.Length == 0)
				return "";

			if (_conditions.Length == 1)
				return _conditions[0].ToString();

			StringBuilder sb = new StringBuilder();
			sb.Append('(');
			sb.Append(_conditions[0].ToString());
			for (int i = 1; i < _conditions.Length; ++i)
			{
				sb.Append(" or ");
				sb.Append(_conditions[i].ToString());
			}
			sb.Append(')');
			return sb.ToString();
		}

		//public override int GetHashCode()
		//{
		//	int hash = 750073717;
		//	for (int i = 0; i < _conditions.Length; ++i)
		//		hash ^= 7 * _conditions[i].GetHashCode();
		//	return hash;
		//}
	}

	public class XorCondition : BinaryOperatorCondition<XorCondition>
	{
		public XorCondition(params ICondition[] conditions)
		{
			_conditions = conditions;
		}

		public override bool Evaluate(IContextual context, EvaluationCookie cookie)
		{
			if (_conditions == null || _conditions.Length == 0)
				return false;

			bool b = false;
			for (int i = 0; i < _conditions.Length; ++i)
				b ^= _conditions[i].Evaluate(context, cookie);
			return b;
		}

		public override string ToString()
		{
			if (_conditions == null || _conditions.Length == 0)
				return "";

			if (_conditions.Length == 1)
				return _conditions[0].ToString();

			StringBuilder sb = new StringBuilder();
			sb.Append('(');
			sb.Append(_conditions[0].ToString());
			for (int i = 1; i < _conditions.Length; ++i)
			{
				sb.Append(" xor ");
				sb.Append(_conditions[i].ToString());
			}
			sb.Append(')');
			return sb.ToString();
		}

		//public override int GetHashCode()
		//{
		//	int hash = 578263541;
		//	for (int i = 0; i < _conditions.Length; ++i)
		//		hash ^= 11 * _conditions[i].GetHashCode();
		//	return hash;
		//}
	}

	public class NotCondition : UnaryOperatorCondition<NotCondition>
	{
		public NotCondition(ICondition condition)
		{
			_condition = condition;
		}

		public override bool Evaluate(IContextual context, EvaluationCookie cookie)
		{
			if (_condition == null)
				return false;

			return _condition.Evaluate(context, cookie) == false;
		}

		public override string ToString()
		{
			if (_condition == null)
				return "";

			return string.Concat("not ", _condition);
		}

		//public override int GetHashCode()
		//{
		//	int hash = 651892987;
		//	hash ^= 13 * _condition.GetHashCode();
		//	return hash;
		//}
	}

	public class AlwaysCondition : ICondition
	{
		public ICondition[] conditions { get { return null; } }

		public bool Evaluate(IContextual context, EvaluationCookie cookie)
		{
			return true;
		}

		public bool Validate(IRuleSupplier[] ruleSuppliers)
		{
			return true;
		}

		public override string ToString()
		{
			return "always";
		}

		//public override int GetHashCode()
		//{
		//	return "always".GetHashCode();
		//}
	}

	public static class Condition
	{
		public static ICondition Or(params ICondition[] conditions)
		{
			return new OrCondition(conditions);
		}

		public static ICondition And(params ICondition[] conditions)
		{
			return new AndCondition(conditions);
		}

		public static ICondition Xor(params ICondition[] conditions)
		{
			return new XorCondition(conditions);
		}

		public static ICondition IsTrue(ICondition condition)
		{
			return And(condition);
		}

		public static ICondition IsFalse(ICondition condition)
		{
			return Not(condition);
		}

		public static ICondition Not(ICondition rule)
		{
			return new NotCondition(rule);
		}

		public static ICondition Always()
		{
			return new AlwaysCondition();
		}

		public static ICondition Never()
		{
			return Not(Always());
		}

		public static bool ParseExpression<T>(string expression, out ICondition condition) where T : IConditionalExpression, new()
		{
			if (string.IsNullOrEmpty(expression))
			{
				condition = null;
				return false;
			}

			expression = expression.Trim();
			if (expression.Length == 0)
			{
				condition = null;
				return false;
			}

			// Remove leading, ending parentheses	
			if (expression.Length >= 2 && expression[0] == '(')
			{
				int eos = FindEndOfScope(expression, 0);
				if (eos == expression.Length - 1)
					expression = expression.Substring(1, expression.Length - 2).Trim();
				else if (eos >= expression.Length)
				{
					condition = null;
					return false; // Error
				}
			}

			// Find Ors
			int pos_or = FindInScope(expression, " or ", 0);
			if (pos_or != -1)
			{
				string left = expression.Substring(0, pos_or);
				ICondition lhs;
				if (ParseExpression<T>(left, out lhs) == false)
				{
					condition = null;
					return false;
				}

				string right = expression.Substring(pos_or + 4);
				ICondition rhs;
				if (ParseExpression<T>(right, out rhs) == false)
				{
					condition = null;
					return false;
				}

				condition = Or(lhs, rhs);
				return true;
			}

			// Find Ors
			int pos_xor = FindInScope(expression, " xor ", 0);
			if (pos_xor != -1)
			{
				string left = expression.Substring(0, pos_xor);
				ICondition lhs;
				if (ParseExpression<T>(left, out lhs) == false)
				{
					condition = null;
					return false;
				}

				string right = expression.Substring(pos_xor + 5);
				ICondition rhs;
				if (ParseExpression<T>(right, out rhs) == false)
				{
					condition = null;
					return false;
				}

				condition = Xor(lhs, rhs);
				return true;
			}

			// Find Ands
			int pos_and = FindInScope(expression, " and ", 0);
			if (pos_and != -1)
			{
				string left = expression.Substring(0, pos_and);
				ICondition lhs;
				if (ParseExpression<T>(left, out lhs) == false)
				{
					condition = null;
					return false;
				}

				string right = expression.Substring(pos_and + 5);
				ICondition rhs;
				if (ParseExpression<T>(right, out rhs) == false)
				{
					condition = null;
					return false;
				}

				condition = And(lhs, rhs);
				return true;
			}

			// Not
			if (FindInScope(expression, "not ", 0) == 0)
			{
				string right = expression.Substring(4);
				ICondition rhs;
				if (ParseExpression<T>(right, out rhs) == false)
				{
					condition = null;
					return false;
				}

				condition = Not(rhs);
				return true;
			}

			// True literal
			if (expression == "true" || expression == "always")
			{
				condition = Always();
				return true;
			}

			// False literal
			if (expression == "false" || expression == "never")
			{
				condition = Never();
				return true;
			}
			
			// Single expression
			T rule = new T();
			rule.expression = expression;
			condition = rule;
			return true;
		}

		private static int FindEndOfScope(string source, int pos)
		{
			if (string.IsNullOrEmpty(source))
				return -1;

			int scope = 0;
			for (; pos < source.Length; ++pos)
			{
				char c = source[pos];
				if (c == '(')
					++scope;
				else if (c == ')')
				{
					if (--scope <= 0)
						return pos;
				}
			}
			return source.Length;
		}

		private static int FindInScope(string source, string word, int pos)
		{
			if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(word))
				return -1;

			for (; pos < source.Length; ++pos)
			{
				char c = source[pos];
				if (c == '(')
				{
					pos = FindEndOfScope(source, pos);
					continue;
				}
				else if (c == word[0]
					&& pos <= source.Length - word.Length
					&& source.Substring(pos, word.Length) == word)
				{
					return pos;
				}
			}
			return -1;
		}

	}
}
