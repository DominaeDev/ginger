
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Ginger
{
	public interface IContextual
	{
		Context context { get; }

		void MarkDirtyContext();

		bool HasFlag(StringHandle tag);
		HashSet<StringHandle> GetFlags();
		ContextualValue GetContextualValue(StringHandle valueName);
	}

	public abstract class Contextual<T> : IContextual where T : IContextual
	{
		public Context context
		{
			get
			{
				if (isDirty || _context == null)
					_context = __CreateContext();

				return _context;
			}
		}

		public bool isDirty { get { return _bDirtyContext || _bDirtyTags || _bDirtyFunctions; } }

		private Context __CreateContext()
		{
			// Refresh context
			_context = CreateContext();
			_bDirtyContext = false; 

			// Refresh tags
			_bDirtyTags = true;
			__RefreshTags();
			_context.SetFlags(_contextualTags);

			// Refresh functions
			_bDirtyFunctions = true;
			__RefreshFunctions();

			return _context;
		}

		protected virtual Context CreateContext() { return Context.CreateEmpty(); }
		protected virtual void RefreshTags() { }
		protected virtual void RefreshValueFunctions() { }

		public void RecreateContext()
		{
			MarkDirtyContext();
			__CreateContext();
		}

		public void MarkDirtyContext()
		{
			_bDirtyContext = true;
			_bDirtyTags = true;
			_bDirtyFunctions = true;
		}

		public HashSet<StringHandle> GetFlags()
		{
			__RefreshTags();
			return _contextualTags;
		}

		public virtual bool HasFlag(StringHandle tag)
		{
			if (GetFlags().Contains(tag))
				return true;

			// Function?
			if (_bDirtyFunctions)
				__RefreshFunctions();

			BoolDelegate @delegate;
			if (_contextualBoolFunctions.TryGetValue(tag, out @delegate) && @delegate != null)
				return @delegate.Invoke();
			return false;
		}
	
		private void __RefreshTags()
		{
			if (!_bDirtyTags)
				return;

			_contextualTags.Clear();
			RefreshTags();
			_bDirtyTags = false;
		}

		private void __RefreshFunctions()
		{
			if (!_bDirtyFunctions)
				return;

			_contextualBoolFunctions.Clear();
			_contextualValueFunctions.Clear();

			RefreshValueFunctions();

			_bDirtyFunctions = false;
		}

		public virtual ContextualValue GetContextualValue(StringHandle valueName)
		{
			// Function?
			if (_bDirtyFunctions)
				__RefreshFunctions();

			ValueDelegate fnValue;
			if (_contextualValueFunctions.TryGetValue(valueName, out fnValue))
				return fnValue.Invoke();

			return null; // Invalid
		}

		public IEnumerable<StringHandle> contextualTags
		{
			get
			{
				return _contextualTags.Union(_contextualBoolFunctions
					.Where(kvp => kvp.Value != null)
					.Select(kvp => new { tag = kvp.Key, fn = kvp.Value.Invoke() })
					.Where(x => x.fn)
					.Select(x => x.tag));
			}
		}

		private Context _context;
		protected HashSet<StringHandle> _contextualTags = new HashSet<StringHandle>();

		protected delegate bool BoolDelegate();
		private Dictionary<StringHandle, BoolDelegate> _contextualBoolFunctions = new Dictionary<StringHandle, BoolDelegate>();

		protected delegate ContextualValue ValueDelegate();
		private Dictionary<StringHandle, ValueDelegate> _contextualValueFunctions = new Dictionary<StringHandle, ValueDelegate>();

		private bool _bDirtyContext = true;
		private bool _bDirtyTags = true;
		private bool _bDirtyFunctions = true;

		protected void AddContextualTag(StringHandle tag)
		{
			if (StringHandle.IsNullOrEmpty(tag) == false)
				_contextualTags.Add(tag);
		}

		protected void AddContextualTags(IEnumerable<StringHandle> tags)
		{
			_contextualTags.UnionWith(tags.Where(t => StringHandle.IsNullOrEmpty(t) == false));
		}

		protected void AddContextualFunction(StringHandle id, BoolDelegate @delegate)
		{
			_contextualBoolFunctions.TryAdd(id, @delegate);
		}

		protected void AddContextualFunction(StringHandle id, ValueDelegate @delegate)
		{
			_contextualValueFunctions.TryAdd(id, @delegate);
		}

		public virtual int selectableIndicesCount { get { return 0; } }

		public virtual IContextual[] SelectDefaultCollection()
		{
			return null;
		}
	}

	public struct ContextualValue
	{
		private string _stringValue;
		private float? _floatValue;

		public static readonly ContextualValue Nil = new ContextualValue("__NIL__");

		public ContextualValue(string value)
		{
			_stringValue = value;
			_floatValue = null;
		}

		public ContextualValue(float value)
		{
			_stringValue = null;
			_floatValue = value;
		}

		public static implicit operator ContextualValue(string value)
		{
			return new ContextualValue(value);
		}

		public static implicit operator ContextualValue(StringHandle value)
		{
			return new ContextualValue(value.ToString());
		}

		public static implicit operator ContextualValue(float value)
		{
			return new ContextualValue(value);
		}

		public bool isDefined { get { return _floatValue.HasValue || _stringValue != null; } }
		public bool isFloat { get { return _floatValue.HasValue; } }
		public bool isString { get { return _stringValue != null; } }
		public bool isNil { get { return string.Compare(Nil._stringValue, _stringValue) == 0; } }

		public override string ToString()
		{
			if (_floatValue.HasValue)
				return _floatValue.Value.ToString("G", CultureInfo.InvariantCulture);
			if (isNil)
				return "";
			return _stringValue;
		}

		public float ToFloat()
		{
			return _floatValue ?? 0.0f;
		}

		public static ContextualValue Parse(string sValue)
		{
			float fValue;
			if (float.TryParse(sValue, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out fValue))
				return new ContextualValue(fValue);
			return new ContextualValue(sValue);
		}
	}

	public static class IContextualExtensions
	{
		public static Context GetContext(this IContextual contextual)
		{
			if (ReferenceEquals(contextual, null))
				return null;
			return contextual.context;
		}
	}

}
