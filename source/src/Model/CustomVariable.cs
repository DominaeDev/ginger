using System;
using System.Text;

namespace Ginger
{
	public struct CustomVariable
	{
		public CustomVariable(CustomVariableName name, string value = "")
		{
			Name = name;
			Value = value;
		}

		public CustomVariableName Name;
		public string Value;
	}

	public struct CustomVariableName : IComparable
	{
		public static readonly CustomVariableName Empty = null;

		private string _name;

		public CustomVariableName(string value)
		{
			if (string.IsNullOrEmpty(value) == false)
				_name = Format(value);
			else
				_name = null;
		}

		private static string Format(string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;

			// Collapse whitespace
			value = string.Join(" ", value.Split(new char[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));

			StringBuilder sbFormat = new StringBuilder(value.Length);
			for (int i = 0; i < value.Length; ++i)
			{
				char c = value[i];
				if (char.IsWhiteSpace(c))
					sbFormat.Append('-');
				else if (c == '_' || c == '-')
					sbFormat.Append(c);
				else if (char.IsControl(c) || char.IsPunctuation(c) || char.IsSymbol(c))
					continue;
				else if (char.IsLetterOrDigit(c))
					sbFormat.Append(c);
			}
			return sbFormat.ToString();
		}

		public CustomVariableName(CustomVariableName other)
		{
			_name = other._name;
		}

		public int Length { get { return string.IsNullOrEmpty(_name) ? 0 : _name.Length; } }

		public static implicit operator CustomVariableName(string s)
		{
			return new CustomVariableName(s);
		}

		public static implicit operator string(CustomVariableName handle)
		{
			return handle._name;
		}

		public override bool Equals(Object @object)
		{
			// Check for null values and compare run-time types.
			if (@object == null || GetType() != @object.GetType())
				return false;

			CustomVariableName handle = (CustomVariableName)@object;
			return CompareTo(handle._name) == 0;
		}

		public override int GetHashCode()
		{
			return _name.GetHashCode();
		}

		public int CompareTo(object @object)
		{
			if (@object == null || GetType() != @object.GetType())
				return 1;

			CustomVariableName handle = (CustomVariableName)@object;
			return string.Compare(this._name, handle._name, StringComparison.OrdinalIgnoreCase);
		}

		public static bool operator == (CustomVariableName a, CustomVariableName b)
        {
            return a.Equals(b);
        }

        public static bool operator != (CustomVariableName a, CustomVariableName b)
        {
            return !(a == b);
        }
		
		public static bool operator < (CustomVariableName a, CustomVariableName b)
        {
            return string.Compare(a._name, b._name, StringComparison.OrdinalIgnoreCase) < 0;
        }

		public static bool operator > (CustomVariableName a, CustomVariableName b)
        {
            return string.Compare(a._name, b._name, StringComparison.OrdinalIgnoreCase) > 0;
        }
				
		public static bool operator <= (CustomVariableName a, CustomVariableName b)
        {
            return string.Compare(a._name, b._name, StringComparison.OrdinalIgnoreCase) <= 0;
        }

		public static bool operator >= (CustomVariableName a, CustomVariableName b)
        {
            return string.Compare(a._name, b._name, StringComparison.OrdinalIgnoreCase) >= 0;
        }

		public static bool operator == (CustomVariableName a, string b)
        {
            return string.Compare(a._name, b, StringComparison.OrdinalIgnoreCase) == 0;
        }

		public static bool operator != (CustomVariableName a, string b)
        {
            return string.Compare(a._name, b, StringComparison.OrdinalIgnoreCase) != 0;
        }

		public static bool operator == (string a, CustomVariableName b)
        {
            return string.Compare(a, b._name, StringComparison.OrdinalIgnoreCase) == 0;
        }

		public static bool operator != (string a, CustomVariableName b)
        {
            return string.Compare(a, b._name, StringComparison.OrdinalIgnoreCase) != 0;
        }

		public char this[int index]
		{
			get { return _name[index]; }
		}

		public override string ToString()
		{
			return _name;
		}

		public static bool IsNullOrEmpty(CustomVariableName handle)
		{
			return string.IsNullOrEmpty(handle._name);
		}
	}

}
