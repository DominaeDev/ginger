using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Ginger
{
	public class Context : IContextual, ICloneable
	{
		public Dictionary<StringHandle, string> values = null;
		public HashSet<StringHandle> tags = null;

		public Context context { get { return this; } } // IContextual

		public static Context CreateEmpty()
		{
			return new Context();
		}
				
		public static Context Copy(Context other, bool shallow = false)
		{
			return shallow ? other.ShallowClone() : (Context)other.Clone();
		}

		private Context(Context other = null)
		{
			if (other != null)
			{
				if (other.tags != null)
				{
					tags = new HashSet<StringHandle>();
					tags.UnionWith(other.tags);
				}

				if (other.values != null)
				{
					values = new Dictionary<StringHandle, string>();
					foreach (var kvp in other.values)
						values.Add(kvp.Key, kvp.Value);
				}
			}
		}

		public object Clone()
		{
			Context clone = new Context();
			
			if (this.tags != null)
			{
				clone.tags = new HashSet<StringHandle>();
				clone.tags.UnionWith(this.tags);
			}

			if (this.values != null)
			{
				clone.values = new Dictionary<StringHandle, string>();
				foreach (var kvp in this.values)
					clone.values.Add(kvp.Key, kvp.Value);
			}

			return clone;
		}

		public Context ShallowClone()
		{
			return (Context)MemberwiseClone();
		}

		public void AddTag(StringHandle tag)
		{
			if (tags == null)
				tags = new HashSet<StringHandle>();

			tags.Add(tag);
		}

		public void AddTags(IEnumerable<StringHandle> tags)
		{
			if (tags == null)
				return;

			if (this.tags == null)
				this.tags = new HashSet<StringHandle>();

			this.tags.UnionWith(tags);
		}

		public void RemoveTag(StringHandle tag)
		{
			if (tags == null)
				return;

			tags.Remove(tag);
		}

		public bool HasTag(StringHandle tag)
		{
			if (tags == null)
				return false;

			return tags.Contains(tag);
		}

		public HashSet<StringHandle> GetTags()
		{
			return tags;
		}

		public Context WithValues(params string[] values)
		{
			Context newCtx = new Context(this);
			if (values != null)
			{
				for (int i = 0; i < values.Length - 1; i += 2)
					newCtx.SetValue(values[i], values[i + 1]);
			}
			return newCtx;
		}
		
		public Context WithValue(StringHandle name, string value)
		{
			Context newCtx = new Context(this);
			if (StringHandle.IsNullOrEmpty(name) == false && string.IsNullOrEmpty(value) != false)
				newCtx.SetValue(name, value);
			return newCtx;
		}		

		public Context WithValue(StringHandle name, float value)
		{
			Context newCtx = new Context(this);
			if (StringHandle.IsNullOrEmpty(name) == false)
				newCtx.SetValue(name, value);
			return newCtx;
		}

		public Context WithTags(params StringHandle[] tags)
		{
			Context newCtx = new Context(this);
			if (tags != null)
			{
				IEnumerable<StringHandle> lsTags = tags.Where(s => StringHandle.IsNullOrEmpty(s) == false);

				if (lsTags.Count() > 0)
				{
					if (newCtx.tags == null)
						newCtx.tags = new HashSet<StringHandle>();

					newCtx.tags.UnionWith(lsTags);
				}
			}
			return newCtx;
		}

		public void SetValue(StringHandle id, string value)
		{
			if (StringHandle.IsNullOrEmpty(id))
				return;

			if (values == null)
				values = new Dictionary<StringHandle, string>();

			if (value == null)
			{
				values.Remove(id);
				return;
			}

			if (values.ContainsKey(id))
				values[id] = value;
			else
				values.Add(id, value);
		}

		public void SetValue(StringHandle id, int value)
		{
			SetValue(id, value.ToString("d"));
		}

		public void SetValue(StringHandle id, float value)
		{
			SetValue(id, value.ToString("g"));
		}

		public void SetValue(StringHandle id, ContextualValue value)
		{
			if (value.isNil)
				SetValue(id, null);
			else if (value.isFloat)
				SetValue(id, value.ToFloat());
			else
				SetValue(id, value.ToString());
		}

		public bool TryGetValue(StringHandle id, out string value)
		{
			if (StringHandle.IsNullOrEmpty(id) || values == null)
			{
				value = default(string);
				return false;
			}

			return values.TryGetValue(id, out value);
		}

		public bool TryGetValue(StringHandle id, out float value)
		{
			if (StringHandle.IsNullOrEmpty(id) || values == null)
			{
				value = default(float);
				return false;
			}

			string strValue;
			if (values.TryGetValue(id, out strValue))
			{
				if (float.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out value))
					return true;
			}

			value = default(float);
			return false;
		}
				
		public ContextualValue GetContextualValue(StringHandle identifier)
		{
			if (values != null)
			{
				string fn = null;
				int pos_fn = identifier.ToString().LastIndexOf("_x2e_"); // '.'
				if (pos_fn != -1)
				{
					fn = identifier.ToString().Substring(pos_fn + 5);
					identifier = identifier.ToString().Substring(0, pos_fn);
				}

				string strValue;
				if (values.TryGetValue(identifier, out strValue))
				{
					if (fn != null)
					{
						ContextualValue result;
						if (ApplyFunction(fn, strValue, out result))
							return result;
					}

					float fValue;
					if (float.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out fValue))
						return fValue;
					return strValue;
				}
			}

			return null;
		}

		private bool ApplyFunction(string fn, string strValue, out ContextualValue result)
		{
			if (fn == null)
			{
				result = default(ContextualValue);
				return false;
			}
			fn = fn.ToLowerInvariant();

			// Numeric functions
			float fValue;
			if (float.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out fValue))
			{
				switch (fn)
				{
				case "ciel":
					result = (float)Math.Ceiling(fValue);
					return true;
				case "floor":
					result = (float)Math.Floor(fValue);
					return true;
				case "round": // list count
					result = (float)Math.Round(fValue);
					return true;
				}

				result = default(ContextualValue);
				return false;
			}

			// String functions
			switch (fn)
			{
			case "count": // list count
				result = Utility.ListFromDelimitedString(strValue, Text.Delimiter, true).Count;
				return true;
			}

			result = default(ContextualValue);
			return false;
		}

		public void MarkDirtyContext()
		{
		}

		public void MergeWith(Context other)
		{
			if (other.tags != null)
			{
				if (tags == null)
					tags = new HashSet<StringHandle>();
				tags.UnionWith(other.tags);
			}

			if (other.values != null)
			{
				if (values == null)
					values = new Dictionary<StringHandle, string>();
				foreach (var kvp in other.values)
					values.Set(kvp.Key, kvp.Value);
			}
		}

		public static Context Merge(Context first, Context second)
		{
			if (first == null)
				return second;
			if (second == null)
				return first;

			var ctx = Copy(first);
			ctx.MergeWith(second);
			return ctx;
		}
	}
}
