using System.Collections.Generic;
using System.Linq;

namespace Ginger
{
	public class ParameterState
	{
		public Context outerContext;

		public ParameterCollection globalParameters = new ParameterCollection();
		public ParameterCollection localParameters = new ParameterCollection();

		public Context evalContext
		{
			get
			{
				if (_bDirty || _evalContext == null)
				{
					_evalContext = Context.Merge(outerContext, localParameters.context);
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

		private ParameterCollection GetCollection(Parameter.Scope scope)
		{
			return scope == Parameter.Scope.Global ? globalParameters : localParameters;
		}

		public void SetFlag(StringHandle flag, Parameter.Scope scope)
		{
			GetCollection(scope).SetFlag(flag);
			if (scope == Parameter.Scope.Local && flag.ToString().IndexOf(':') == -1)
				localParameters.SetFlag(string.Concat(flag.ToString(), ":local"));
			_bDirty = true;
		}

		public void SetFlags(IEnumerable<StringHandle> flags, Parameter.Scope scope)
		{
			GetCollection(scope).SetFlags(flags);
			if (scope == Parameter.Scope.Local)
			{
				foreach (var flag in flags.Where(f => f.ToString().IndexOf(':') == -1))
					localParameters.SetFlag(string.Concat(flag.ToString(), ":local"));
			}
			_bDirty = true;
		}

		public void SetValue(StringHandle id, string value, Parameter.Scope scope)
		{
			GetCollection(scope).SetValue(id, value);
			if (scope == Parameter.Scope.Local && id.ToString().IndexOf(':') == -1)
			{
				localParameters.SetFlag(string.Concat(id.ToString(), ":local"));
				localParameters.SetValue(string.Concat(id.ToString(), ":local"), value);
			}
			_bDirty = true;
		}

		public void SetValue(StringHandle id, float value, Parameter.Scope scope)
		{
			GetCollection(scope).SetValue(id, value);
			if (id.ToString().IndexOf(':') == -1)
			{
				localParameters.SetFlag(string.Concat(id.ToString(), ":local"));
				localParameters.SetValue(string.Concat(id.ToString(), ":local"), value);
			}
			_bDirty = true;
		}

		public void Erase(StringHandle id, Parameter.Scope scope)
		{
			GetCollection(scope).EraseFlag(id);
			GetCollection(scope).EraseValue(id);
			if (scope == Parameter.Scope.Global)
				_reserved.Remove(id);
			_bDirty = true;
		}

		public void Reserve(StringHandle id, string reservedValue)
		{
			_resolvedStates.TryAdd(id, State.Reserved);
			_reserved.TryAdd(id, reservedValue);
			_bDirty = true;
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
		
		public void CopyGlobals(ParameterState state)
		{
			// Erase flags
			foreach (var flag in state.globalParameters.erasedFlags)
			{
				outerContext.RemoveFlag(flag);
				globalParameters.EraseFlag(flag);
			}

			// Erase values
			foreach (var value in state.globalParameters.erasedValues)
			{
				outerContext.SetValue(value, null);
				globalParameters.EraseValue(value);
			}

			// Copy flags
			foreach (var flag in state.globalParameters.flags.Except(state.globalParameters.erasedFlags))
			{
				outerContext.SetFlag(flag);
				globalParameters.SetFlag(flag);
			}

			// Copy values
			foreach (var value in state.globalParameters.values.Where(kvp => state.globalParameters.erasedValues.Contains(kvp.Key) == false))
			{
				outerContext.SetValue(value.Key, value.Value);
				globalParameters.SetValue(value.Key, value.Value);
			}

			_reserved = _reserved.Union(state._reserved)
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
			_bDirty = true;
		}

		public Context GetFullContext()
		{
			return Context.Merge(Context.Merge(outerContext, globalParameters.context), localParameters.context);
		}

		public Context GetLocalContext()
		{
			return Context.Merge(Context.Merge(outerContext, globalParameters.context), localParameters.context);
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
				if (_states[i].localParameters.TryGetValue(id, out ctxValue))
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
		public IEnumerable<StringHandle> erasedFlags{ get { return _erasedFlags; } }
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
