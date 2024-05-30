using System;
using System.Text;

namespace Ginger
{
	public class StringID
	{
		private static readonly string Empty = "";
		public static readonly char Delimiter = '/';

		public string identifier { get { return _id; } }
		private readonly string _id = Empty;

		public bool isEmpty { get { return string.IsNullOrEmpty(_id); } }

		public StringID(string id)
		{
			_id = FilterID(id);
		}

		public StringID(StringID other)
		{
			_id = other._id;
		}

		private StringID(params string[] ids)
		{
			if (ids == null || ids.Length == 0)
				_id = Empty;
			else if (ids.Length == 1)
			{
				_id = FilterID(ids[0]);
			}
			else if (ids.Length == 2)
			{
				_id = string.Concat(FilterID(ids[0]), Delimiter, FilterID(ids[1]));
			}
			else
			{
				StringBuilder sb = new StringBuilder(64);
				for (int i = 0; i < ids.Length - 1; ++i)
				{
					sb.Append(FilterID(ids[i]));
					sb.Append(Delimiter);
				}
				sb.Append(FilterID(ids[ids.Length - 1]));

				_id = sb.ToString();
			}
		}
		
		public static StringID Make(params string[] ids)
		{
			return new StringID(ids);
		}

		public static StringID FromString(string value)
		{
			return new StringID(value);
		}

		public static StringID FromHandle(StringHandle id)
		{
			return new StringID(id.ToString());
		}

		public static StringID FromEnum<T>(T e) where T : struct, IConvertible
		{
			return new StringID(EnumInfo<T>.ToString(e));
		}

		private static string FilterID(string value)
		{
			if (string.IsNullOrEmpty(value))
				return value;

			char[] chars = new char[value.Length];
			char c;
			for (int i = 0; i < value.Length; ++i)
			{
				c = value[i];
				if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_' || c == Delimiter)
					chars[i] = c;
				else if (c >= 'A' && c <= 'Z')
					chars[i] = char.ToLowerInvariant(c);
				else
					chars[i] = '_';
			}
			return new string(chars);
		}

		public override string ToString()
		{
			return _id;
		}

		public override int GetHashCode()
		{
			return (_id ?? "").GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			if (!(obj is StringID))
				return false;

			StringID other = (StringID)obj;
			if ((System.Object)other == null)
				return false;

			return string.Equals(identifier, other.identifier);
		}

		public static StringID operator | (StringID a, StringID b)
		{
			if (b == null || b.isEmpty)
				return a;
			if (a == null || a.isEmpty)
				return b;

			return new StringID(string.Concat(a.identifier, Delimiter, b.identifier));
		}

		public static StringID operator | (StringID a, string b)
		{
			if (string.IsNullOrEmpty(b))
				return a;
			if (a == null || a.isEmpty)
				return Make(b);

			return new StringID(string.Concat(a.identifier, Delimiter, Make(b)));
		}

		public static StringID operator | (string a, StringID b)
		{
			if (b == null || b.isEmpty)
				return Make(a);
			if (string.IsNullOrEmpty(a))
				return b;

			return new StringID(string.Concat(Make(a), Delimiter, b.identifier));
		}
	}
}
