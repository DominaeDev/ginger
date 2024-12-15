using System.Globalization;

namespace Ginger
{
	public class ValueComparison
	{
		private static readonly string[,] Operators = new string[,]
		{
			{ "<>",		" neq ",	" is not " },
			{ ">=",		" ge ",		null },
			{ "<=",		" le ",		null },
			{ ">",		" gt ",		null },
			{ "<",		" lt ",		null },
			{ "=",		" eq ",		" is " },
			{ " in ",	null,		null },
		};

		private static readonly string Nil = "nil";

		public enum Operator
		{
			Invalid = -1,
			Inequality,
			GreaterOrEqualTo,
			LessOrEqualTo,
			GreaterThan,
			LessThan,
			Equality,
			InSet,
		}
		private static readonly Operator[] _operators = { Operator.Inequality, Operator.GreaterOrEqualTo, Operator.LessOrEqualTo, Operator.GreaterThan, Operator.LessThan, Operator.Equality, Operator.InSet };

		public bool isValid { get { return _operator != Operator.Invalid; } }
	
		private Operator _operator = Operator.Invalid;

		private string _lhsString;
		private string _rhsString;

		public static ValueComparison Parse(string expression)
		{
			// Parse operator
			var nOperators = Operators.GetLength(0);
			var nVariations = Operators.GetLength(1);

			// Other operators
			for (int i = 0; i < nOperators; ++i)
			{
				for (int j = 0; j < nVariations; ++j)
				{
					string sOperator = Operators[i, j];
					if (sOperator == null)
						continue;

					int pos = expression.IndexOf(sOperator, System.StringComparison.Ordinal);

					if (pos != -1)
					{
						var lhs = expression.Substring(0, pos).Trim();
						var rhs = expression.Substring(pos + sOperator.Length).Trim();
						return new ValueComparison(lhs, rhs, _operators[i]);
					}
				}
			}

			return new ValueComparison();
		}

		public ValueComparison()
		{
			_operator = Operator.Invalid;
		}

		public ValueComparison(string lhs, string rhs, Operator op)
		{
			_lhsString = string.IsNullOrEmpty(lhs) ? null : lhs;
			_rhsString = string.IsNullOrEmpty(rhs) ? null : rhs;
			_operator = op;
		}

		public bool Evaluate(Context context)
		{
			float? lhs_f = null;
			float? rhs_f = null;
			string lhs_str = _lhsString;
			string lhs_arg = _lhsString;
			string rhs_str = _rhsString;
			string rhs_arg = _rhsString;

			// Parse LHS			
			if (lhs_str != null)
			{
				// Variable?
				if (context != null)
				{
					float fTmp;
					if (lhs_arg.EndsWith("%") && float.TryParse(lhs_arg.Substring(0, lhs_arg.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out fTmp)) // Percentile
					{
						lhs_f = fTmp * 0.01f;
						lhs_str = null;
					}
					else if (float.TryParse(lhs_arg, NumberStyles.Float, CultureInfo.InvariantCulture, out fTmp))
					{
						lhs_f = fTmp;
						lhs_str = null;
					}
					else
					{ 
						float ctxf;
						string ctxstr;

						if (context.GetContextualValue(lhs_arg, out ctxf))
							lhs_f = ctxf;
						if (context.GetContextualValue(lhs_arg, out ctxstr))
							lhs_str = ctxstr;
						else if (context.TryGetValue(lhs_arg, out ctxstr))
							lhs_str = ctxstr;
					}
				}
			}

			// Parse RHS
			if (rhs_str != null)
			{
				// Variable?
				if (context != null)
				{
					float fTmp;
					if (rhs_arg.EndsWith("%") && float.TryParse(rhs_arg.Substring(0, rhs_arg.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out fTmp)) // Percentile
					{
						rhs_f = fTmp * 0.01f;
						rhs_str = null;
					}
					else if (float.TryParse(rhs_arg, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out fTmp))
					{
						rhs_f = fTmp;
						rhs_str = null;
					}
					else
					{
						float ctxf;
						string ctxstr;

						if (context.GetContextualValue(rhs_arg, out ctxf))
							rhs_f = ctxf;
						if (context.GetContextualValue(rhs_arg, out ctxstr))
							rhs_str = ctxstr;
						else if (context.TryGetValue(rhs_arg, out ctxstr))
							rhs_str = ctxstr;
					}
				}
			}

			// In range
			if (_operator == Operator.InSet && rhs_str != null && lhs_f.HasValue && rhs_str.IndexOf('~') != -1)
			{
				Range range;
				if (Range.TryParse(rhs_str, out range))
					return range.Contains(lhs_f.Value);
			}

			// In set
			if (_operator == Operator.InSet && lhs_str != null && rhs_str != null)
			{
				string rhs = rhs_str.ToLowerInvariant();
				rhs = rhs.Replace(Text.Delimiter, ";");
				return Utility.ListFromDelimitedString(rhs, ';', true).Contains(lhs_str.ToLowerInvariant());
			}

			// Parse numeric values
			float f;
			if (lhs_f.HasValue == false && float.TryParse(lhs_str, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out f))
			{
				lhs_f = f;
				lhs_str = null;
			}

			if (rhs_f.HasValue == false && float.TryParse(rhs_str, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out f))
			{
				rhs_f = f;
				rhs_str = null;
			}

			if (lhs_f.HasValue == false && lhs_str == null) // Invalid expression
				return false;
			if (rhs_f.HasValue == false && rhs_str == null) // Invalid expression
				return false;

			// Compare
			int comparison = 0;

			if (lhs_f.HasValue == false && lhs_str == Nil)
				lhs_str = null;
			if (rhs_f.HasValue == false && rhs_str == Nil)
				rhs_str = null;

			if (lhs_f.HasValue && rhs_f.HasValue)
			{
				if (System.Math.Abs(lhs_f.Value - rhs_f.Value) < float.Epsilon)
					comparison = 0;
				else
					comparison = lhs_f.Value.CompareTo(rhs_f.Value);
			}
			else if (lhs_str != null && rhs_str != null)
			{
				if (_operator == Operator.Equality)
					return string.Compare(lhs_str, rhs_str, true) == 0;
				else if (_operator == Operator.Inequality)
					return string.Compare(lhs_str, rhs_str, true) != 0;
				return false;
			}
			else // Null equality
			{
				if (_operator == Operator.Equality)
					return (lhs_str == null) == (rhs_str == null);
				else if (_operator == Operator.Inequality)
					return (lhs_str == null) != (rhs_str == null);
				return false;
			}
						
			switch (_operator)
			{
			case Operator.Equality:
				return comparison == 0;
			case Operator.GreaterOrEqualTo:
				return comparison >= 0;
			case Operator.LessOrEqualTo:
				return comparison <= 0;
			case Operator.GreaterThan:
				return comparison > 0;
			case Operator.LessThan:
				return comparison < 0;
			case Operator.Inequality:
				return comparison != 0;
			default:
				return false;
			}
		}
		
	}
}
