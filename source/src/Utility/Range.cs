using System;
using System.Globalization;

namespace Ginger
{
	public interface IRange<T>
	{
		T min { get; }
		T max { get; }
		T length { get; }
		bool Contains(T value);
	}

	public struct Range : IRange<float>
    {
		public static char Delimiter = '~';

		public float min { get; set; }
		public float max { get; set; }
		public float length { get { return Math.Abs(max - min); } }

		public Range(float min = 0.0f, float max = 0.0f)
		{
			this.min = min;
			this.max = max;
		}

		public bool Contains(float value)
		{
			float lower = Math.Min(min, max);
			float upper = Math.Max(min, max);
			return value >= lower && value <= upper;
		}

		public static Range FromFloat(float value)
		{
			return new Range(value, value);
		}

		public static Range operator *(Range r, float factor)
		{
			return new Range(r.min * factor, r.max * factor);
		}

		public float Lerp(float value)
		{
			return min + System.Math.Min(System.Math.Max(value, 0.0f), 1.0f) * (max - min);
		}

		public static bool TryParse(string s, out Range range)
		{
			if (string.IsNullOrEmpty(s))
			{
				range = default(Range);
				return false;
			}

			int posDelim = s.IndexOf(Delimiter);
			if (posDelim == -1)
			{
				string trimmed = s.Trim();
				float value;

				if (trimmed.EndsWith("%") && float.TryParse(trimmed.Substring(0, trimmed.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
				{
					value /= 100.0f;
				}
				else if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value) == false)
				{
					range = default(Range);
					return false;
				}
				range = new Range(value, value);
				return true;
			}

			string a = s.Substring(0, posDelim).Trim();
			string b = s.Substring(posDelim + 1).Trim();

			float min, max;
			if (a.EndsWith("%") && float.TryParse(a.Substring(0, a.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out min))
			{
				min /= 100.0f;
			}
			else if (float.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out min) == false)
			{
				range = default(Range);
				return false;
			}

			if (b.EndsWith("%") && float.TryParse(b.Substring(0, b.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out max))
			{
				max /= 100.0f;
			}
			else if (float.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out max) == false)
			{
				range = default(Range);
				return false;
			}
			range = new Range(min, max);
			return true;
		}

		public override string ToString()
		{
			return string.Concat(min.ToString("0.000"), Delimiter, max.ToString("0.000"));
		}
	}

	public struct RangeInt : IRange<int>
    {
		public int min { get; set; }
		public int max { get; set; }
		public int length { get { return Math.Abs(max - min); } }

		public RangeInt(int min = 0, int max = 0)
		{
			this.min = min;
			this.max = max;
		}

		public bool Contains(int value)
		{
			if (min <= max)
				return value >= min && value <= max;
			else
				return value >= max && value <= min;
		}

		public static RangeInt FromInt(int value)
		{
			return new RangeInt(value, value);
		}

		public static implicit operator RangeInt(Range range)
		{
			return new RangeInt((int)range.min, (int)range.max);
		}
		
		public static RangeInt operator *(RangeInt r, int factor)
		{
			return new RangeInt(r.min * factor, r.max * factor);
		}

		public static bool TryParse(string s, out RangeInt range)
		{
			int posDelim = s.IndexOf(Range.Delimiter);
			if (posDelim == -1)
			{
				float value;
				if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
				{
					range = new Range(value, value);
					return true;
				}

				range = new RangeInt(0, 0);
				return false;
			}

			int min, max;
			if (int.TryParse(s.Substring(0, posDelim), NumberStyles.Integer, CultureInfo.InvariantCulture, out min) == false)
			{
				range = new RangeInt(0, 0);
				return false;
			}
			if (int.TryParse(s.Substring(posDelim + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out max) == false)
			{
				range = new RangeInt(0, 0);
				return false;
			}
			range = new RangeInt(min, max);
			return true;
		}

		public override string ToString()
		{
			return string.Concat(min, Range.Delimiter, max);
		}
	}

	public struct RangeEnum<T> : IRange<int> where T : struct, System.IConvertible
	{
		public int min { get; private set; }
		public int max { get; private set; }
		public int length { get { return Math.Abs(max - min); } }

		public RangeEnum(T min = default(T), T max = default(T))
		{
			int iMin = min.ToInt32(CultureInfo.InvariantCulture);
			int iMax = max.ToInt32(CultureInfo.InvariantCulture);
			this.min = System.Math.Min(iMin, iMax);
			this.max = System.Math.Max(iMin, iMax);
		}

		public bool Contains(T value)
		{
			return Contains(value.ToInt32(CultureInfo.InvariantCulture));
		}

		public bool Contains(int value)
		{
			if (min <= max)
				return value >= min && value <= max;
			else
				return value >= max && value <= min;
		}
	}
}
