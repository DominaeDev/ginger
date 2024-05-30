using System;
using System.Text;

namespace Ginger
{
	public struct StringHandle : IComparable
	{
		public static readonly StringHandle Empty = null;

		private string _handle;
		private int _hashCode;

		public StringHandle(string value)
		{
			if (string.IsNullOrEmpty(value) == false)
			{
				_handle = Format(value);
				_hashCode = _handle.GetHashCode();
			}
			else
			{
				_handle = null;
				_hashCode = 0;
			}
		}

		private static string Format(string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;

			StringBuilder sbFormat = new StringBuilder(value.Length);
			for (int i = 0; i < value.Length; ++i)
			{
				char c = value[i];
				if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_' || c == '/' || c == ':') // Valid chars
					sbFormat.Append(c);
				else if (c >= 'A' && c <= 'Z')
					sbFormat.Append(char.ToLowerInvariant(c));
				else if (c == ' ')
					sbFormat.Append('-');
				else if ((c & 0xFF) == c)
					sbFormat.Append(string.Format("_x{0:x2}_", c & 0xFF));
				else
					sbFormat.Append(string.Format("_x{0:x4}_", c & 0xFFFF));
			}
			return sbFormat.ToString();
		}

		public StringHandle(StringHandle other)
		{
			_handle = other._handle;
			_hashCode = other._hashCode;
		}

		public int Length { get { return string.IsNullOrEmpty(_handle) ? 0 : _handle.Length; } }

		public static implicit operator StringHandle(string s)
		{
			return new StringHandle(s);
		}

		public static explicit operator int(StringHandle handle)
		{
			return handle._hashCode;
		}

		public static explicit operator string(StringHandle handle)
		{
			return handle._handle;
		}

		public override bool Equals(Object @object)
		{
			// Check for null values and compare run-time types.
			if (@object == null || GetType() != @object.GetType())
				return false;

			StringHandle handle = (StringHandle)@object;
			return _hashCode == handle._hashCode;
		}

		public override int GetHashCode()
		{
			return _hashCode;
		}

		public int CompareTo(object @object)
		{
			if (@object == null || GetType() != @object.GetType())
				return 1;

			StringHandle handle = (StringHandle)@object;
			return this._hashCode.CompareTo(handle._hashCode);
		}

		public static bool operator == (StringHandle a, StringHandle b)
        {
            return a.Equals(b);
        }

        public static bool operator != (StringHandle a, StringHandle b)
        {
            return !(a == b);
        }
		
		public static bool operator < (StringHandle a, StringHandle b)
        {
            return a._hashCode < b._hashCode;
        }

		public static bool operator > (StringHandle a, StringHandle b)
        {
            return a._hashCode > b._hashCode;
        }
				
		public static bool operator <= (StringHandle a, StringHandle b)
        {
            return a._hashCode <= b._hashCode;
        }

		public static bool operator >= (StringHandle a, StringHandle b)
        {
            return a._hashCode >= b._hashCode;
        }

		public static bool operator == (StringHandle a, string b)
        {
            return string.Compare(a._handle, b, true) == 0;
        }

		public static bool operator != (StringHandle a, string b)
        {
            return string.Compare(a._handle, b, true) != 0;
        }

		public static bool operator == (string a, StringHandle b)
        {
            return string.Compare(a, b._handle, true) == 0;
        }

		public static bool operator != (string a, StringHandle b)
        {
            return string.Compare(a, b._handle, true) != 0;
        }

		public char this[int index]
		{
			get { return _handle[index]; }
		}

		public bool BeginsWith(string match)
		{
			if (string.IsNullOrEmpty(match))
				return IsNullOrEmpty(this);
			if (string.IsNullOrEmpty(_handle))
				return false;

			if (match == null || _handle.Length < match.Length)
				return false;

			for (int i = 0; i < _handle.Length && i < match.Length; ++i)
			{
				if (char.ToLowerInvariant(match[i]) != _handle[i])
					return false;
			}
			return true;
		}

		public override string ToString()
		{
			return _handle;
		}

		public static bool IsNullOrEmpty(StringHandle handle)
		{
			return string.IsNullOrEmpty(handle._handle);
		}
	}

}
