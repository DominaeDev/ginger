using System;
using System.Collections.Generic;
using System.Linq;

namespace Ginger
{
	public class ParameterState
	{
		public ParameterCollection outerScope = new ParameterCollection();		// Global in
		public ParameterCollection globalScope = new ParameterCollection();		// Global out
		public ParameterCollection localScope = new ParameterCollection();

		public Context evalContext
		{
			get
			{
				if (_bDirty || _evalContext == null)
				{
					_evalContext = Context.Merge(outerScope.context, localScope.context);
					_bDirty = false;
				}
				return _evalContext;
			}
		}
		private Context _evalContext = null;
		public ContextString.EvaluationConfig evalConfig;

		public enum State
		{
			Default,
			Inactive,
			Reserved,
		}
		private Dictionary<StringHandle, State> _resolvedStates = new Dictionary<StringHandle, State>();
		private Dictionary<StringHandle, string> _reserved = new Dictionary<StringHandle, string>();
		private bool _bDirty = true;

		public void SetFlag(StringHandle flag, Parameter.Scope scope)
		{
			if (scope.Contains(Parameter.Scope.Global))
				globalScope.SetFlag(flag);
			if (scope.Contains(Parameter.Scope.Local))
			{
				localScope.SetFlag(flag);
				if (flag.ToString().IndexOf(':') == -1)
					localScope.SetFlag(string.Concat(flag.ToString(), ":local"));
			}
			Dirty();
		}

		public void SetFlags(IEnumerable<StringHandle> flags, Parameter.Scope scope)
		{
			if (scope.Contains(Parameter.Scope.Global))
				globalScope.SetFlags(flags);
			if (scope.Contains(Parameter.Scope.Local))
			{
				foreach (var flag in flags.Where(f => f.ToString().IndexOf(':') == -1))
					localScope.SetFlag(string.Concat(flag.ToString(), ":local"));
			}
			Dirty();
		}

		public void SetValue(StringHandle id, string value, Parameter.Scope scope)
		{
			if (scope.Contains(Parameter.Scope.Global))
				globalScope.SetValue(id, value);
			if (scope.Contains(Parameter.Scope.Local))
			{
				if (id.ToString().IndexOf(':') == -1)
				{
					localScope.SetFlag(string.Concat(id.ToString(), ":local"));
					localScope.SetValue(string.Concat(id.ToString(), ":local"), value);
				}
			}
			Dirty();
		}

		public void SetValue(StringHandle id, float value, Parameter.Scope scope)
		{
			if (scope.Contains(Parameter.Scope.Global))
				globalScope.SetValue(id, value);
			if (scope.Contains(Parameter.Scope.Local))
			{
				if (id.ToString().IndexOf(':') == -1)
				{
					localScope.SetFlag(string.Concat(id.ToString(), ":local"));
					localScope.SetValue(string.Concat(id.ToString(), ":local"), value);
				}
			}
			Dirty();
		}

		public void SetValue(StringHandle id, ContextualValue value, Parameter.Scope scope)
		{
			if (scope.Contains(Parameter.Scope.Global))
				globalScope.SetValue(id, value);
			if (scope.Contains(Parameter.Scope.Local))
			{
				if (id.ToString().IndexOf(':') == -1)
				{
					localScope.SetFlag(string.Concat(id.ToString(), ":local"));
					localScope.SetValue(string.Concat(id.ToString(), ":local"), value);
				}
			}
			Dirty();
		}

		public void EraseFlag(StringHandle id, Parameter.Scope scope)
		{
			if (scope.Contains(Parameter.Scope.Global))
				globalScope.EraseFlag(id);

			if (scope.Contains(Parameter.Scope.Local))
				localScope.EraseFlag(id);
			Dirty();
		}

		public void EraseValue(StringHandle id, Parameter.Scope scope)
		{
			if (scope.Contains(Parameter.Scope.Global))
				globalScope.EraseValue(id);

			if (scope.Contains(Parameter.Scope.Local))
				localScope.EraseValue(id);

			Dirty();
		}

		public void Reserve(StringHandle id, string reservedValue)
		{
			_resolvedStates.TryAdd(id, State.Reserved);
			_reserved.TryAdd(id, reservedValue);
			Dirty();
		}

		public void Unreserve(StringHandle id)
		{
			_reserved.Remove(id);
		}

		public bool TryGetReservedValue(StringHandle id, out string reservedValue)
		{
			return _reserved.TryGetValue(id, out reservedValue);
		}

		public bool IsReserved(StringHandle id)
		{
			State state;
			if (_resolvedStates.TryGetValue(id, out state))
				return state == State.Reserved;
			return false;
		}

		public void CopyReserved(ParameterState state)
		{
			_reserved = _reserved.Union(state._reserved)
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		}

		public void Inactivate(StringHandle id)
		{
			_resolvedStates.TryAdd(id, State.Inactive);
		}

		public bool IsInactive(StringHandle id)
		{
			State state;
			if (_resolvedStates.TryGetValue(id, out state))
				return state == State.Inactive;
			return false;
		}

		public void Dirty()
		{
			_bDirty = true;
		}
	}

	public class ParameterStates : IValueSupplier
	{
		public ParameterStates(ICollection<Recipe> recipes)
		{
			_states = new ParameterState[recipes.Count];
			for (int i = 0; i < _states.Length; ++i)
				_states[i] = new ParameterState();
		}

		public ParameterState this[int index]
		{
			get { return _states[index]; }
			set { _states[index] = value; }
		}

		private ParameterState[] _states;

		public int Length { get { return _states.Length; } }

		public bool TryGetValue(StringHandle id, out string value)
		{
			for (int i = _states.Length - 1; i >= 0; --i)
			{
				if (_states[i] == null)
					continue;

				ContextualValue ctxValue;
				if (_states[i].localScope.TryGetValue(id, out ctxValue))
				{
					value = ctxValue.ToString();
					return true;
				}
			}

			value = default(string);
			return false;
		}
	}

	public class ParameterCollection
	{
		public IEnumerable<KeyValuePair<StringHandle, ContextualValue>> values { get { return _values; } }
		private Dictionary<StringHandle, ContextualValue> _values = new Dictionary<StringHandle, ContextualValue>();
		public IEnumerable<StringHandle> flags { get { return _flags; } }
		private HashSet<StringHandle> _flags = new HashSet<StringHandle>();

		public IEnumerable<StringHandle> erasedValues { get { return _erasedValues; } }
		public IEnumerable<StringHandle> erasedFlags { get { return _erasedFlags; } }
		private HashSet<StringHandle> _erasedValues = new HashSet<StringHandle>();
		private HashSet<StringHandle> _erasedFlags = new HashSet<StringHandle>();

		public void SetValue(StringHandle id, ContextualValue value)
		{
			if (_values.ContainsKey(id))
				_values[id] = value;
			else
				_values.Add(id, value);

			_erasedValues.Remove(id);
		}

		public bool TryGetValue(StringHandle id, out ContextualValue value)
		{
			return _values.TryGetValue(id, out value);
		}

		public void SetFlag(StringHandle flag)
		{
			_flags.Add(flag);
			_erasedFlags.Remove(flag);
		}

		public void SetFlags(IEnumerable<StringHandle> flags)
		{
			_flags.UnionWith(flags);
			_erasedFlags.ExceptWith(flags);
		}

		public void ApplyToContext(Context context)
		{
			context.SetFlags(_flags);
			foreach (var kvp in _values)
				context.SetValue(kvp.Key, kvp.Value.ToString());
		}

		public void CopyFromContext(Context context)
		{
			if (context == null)
				return;
			if (context.flags != null)
				SetFlags(context.flags);
			if (context.values != null)
			{
				foreach (var value in context.values)
					SetValue(value.Key, value.Value);
			}
		}

		public void EraseFlag(StringHandle id)
		{
			_flags.Remove(id);
			_erasedFlags.Add(id);
		}

		public void EraseValue(StringHandle id)
		{
			_values.Remove(id);
			_erasedValues.Add(id);
		}

		public Context context
		{
			get
			{
				var ctx = Context.CreateEmpty();
				ApplyToContext(ctx);
				return ctx;
			}
		}
	}

}
