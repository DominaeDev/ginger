using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;

namespace Ginger
{
	public interface IParameter : IXmlLoadable, IXmlSaveable, ICloneable
	{
		StringHandle id { get; }
		string defaultValue { get; set; }
		string label { get; }
		string description { get; }
		bool isOptional { get; }
		bool isEnabled { get; }
		bool isLocal { get; }
		bool isGlobal { get; }
		bool isImmediate { get; }
		bool isConditional { get; }

		bool IsActive(Context context);

		void Apply(ParameterState parameterState);
		Type GetParameterType();
		object GetValue();
		void ResetToDefault();
	}

	public interface IInvisibleParameter {}

	public static class Parameter
	{
		public static IParameter Create(XmlNode xmlNode, Recipe recipe)
		{
			string typeName = xmlNode.Name;

			switch (typeName)
			{
			case "Text": return new TextParameter(recipe);
			case "Toggle": return new BooleanParameter(recipe);
			case "Number":
			{
				var style = xmlNode.GetAttributeEnum("style", NumberParameter.Mode.Integer);
				switch (style)
				{
				default:
				case NumberParameter.Mode.Integer:
				case NumberParameter.Mode.Decimal:
					return new NumberParameter(recipe);
				case NumberParameter.Mode.Length:
				case NumberParameter.Mode.Weight:
				case NumberParameter.Mode.Volume:
					return new MeasurementParameter(recipe);
				}
			}
			case "Choice":
			{
				var style = xmlNode.GetAttributeEnum("style", ChoiceParameter.Style.Default);
				switch (style)
				{
				default:
				case ChoiceParameter.Style.List:
				case ChoiceParameter.Style.Radio:
					return new ChoiceParameter(recipe);
				case ChoiceParameter.Style.Multiple:
					return new MultiChoiceParameter(recipe);
				}
			}
			case "List": return new ListParameter(recipe);
			case "Slider": return new RangeParameter(recipe);
			case "SetVar": return new SetVarParameter(recipe);
			case "SetFlag": return new SetFlagParameter(recipe);
			case "Erase": return new EraseParameter(recipe);
			case "Hint": return new HintParameter(recipe);

			// Internal
			case "Lorebook":
			case "__Lorebook": return new LorebookParameter(recipe);
			}
			return null;
		}

		public static string ToClipboard(string value)
		{
			if (AppSettings.Settings.AutoConvertNames)
			{
				StringBuilder sb = new StringBuilder(value);

				Utility.ReplaceWholeWord(sb, GingerString.CharacterMarker, "__CCCC__", StringComparison.OrdinalIgnoreCase);
				Utility.ReplaceWholeWord(sb, GingerString.UserMarker, "__UUUU__", StringComparison.OrdinalIgnoreCase);

				string userPlaceholder = (Current.Card.userPlaceholder ?? "").Trim();
				if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
					Utility.ReplaceWholeWord(sb, userPlaceholder, "__UUUU__", StringComparison.Ordinal);
				string characterPlaceholder = (Current.Character.namePlaceholder ?? "").Trim();
				if (string.IsNullOrWhiteSpace(characterPlaceholder) == false)
					Utility.ReplaceWholeWord(sb, characterPlaceholder, "__CCCC__", StringComparison.Ordinal);

				sb.Replace("__CCCC__", GingerString.CharacterMarker);
				sb.Replace("__UUUU__", GingerString.UserMarker);
				return sb.ToString();
			}
			return value;
		}

		public static string FromClipboard(string value, string characterName, string userName)
		{
			if (AppSettings.Settings.AutoConvertNames)
			{
				StringBuilder sb = new StringBuilder(value);
				string characterPlaceholder = (characterName ?? "").Trim();
				string userPlaceholder = (userName ?? "").Trim();

				if (string.IsNullOrWhiteSpace(characterPlaceholder) == false)
					sb.Replace(GingerString.CharacterMarker, characterPlaceholder);
				if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
					sb.Replace(GingerString.UserMarker, userPlaceholder);
				return sb.ToString();
			}
			return value;
		}
	}

	public abstract class BaseParameter<T> : IParameter
	{
		public StringHandle id { get; protected set; }
		public string label { get; protected set; }
		public string description { get; protected set; }
		public string placeholder { get; protected set; }
		public bool isOptional { get; protected set; }
		public bool isEnabled { get; set; }
		public virtual bool isLocal { get { return true; } }
		public bool isGlobal { get; set; }
		public bool isImmediate { get; set; }
		public bool isConditional { get { return condition != null; } }
		public ICondition condition { get; protected set; }
		public abstract void OnApply(ParameterState state, ParameterScope scope);
		public Recipe recipe { get; protected set; }

		public T value;
		public string defaultValue { get; set; }

		public virtual void Set(T value)
		{
			this.value = value;
		}

		protected BaseParameter() { }

		protected BaseParameter(Recipe recipe)
		{
			this.recipe = recipe;
		}

		public void Apply(ParameterState parameterState)
		{
			if (!isEnabled || !IsActive(parameterState))
				return;

			if (isLocal) // Set local parameters
				OnApply(parameterState, ParameterScope.Local);
			if (isGlobal) // Set global parameters
				OnApply(parameterState, ParameterScope.Global);
		}

		public virtual bool LoadFromXml(XmlNode xmlNode)
		{
			id = xmlNode.GetAttribute("id", null);
			if (StringHandle.IsNullOrEmpty(id))
				return false;

			isOptional = xmlNode.GetAttributeBool("optional", true);
			isOptional &= !xmlNode.GetAttributeBool("required", false);
			isGlobal = xmlNode.GetAttributeBool("shared", false);
			isEnabled = xmlNode.GetAttributeBool("enabled", true);
			isImmediate = xmlNode.GetAttributeBool("immediate", false);

			if (xmlNode.HasAttribute("rule"))
				condition = Rule.Parse(xmlNode.GetAttribute("rule"));

			label = xmlNode.GetAttribute("label", default(string));
			label = xmlNode.GetValueElement("Label", label).SingleLine();
			if (string.IsNullOrEmpty(label))
				label = Text.Process(id.ToString(), Text.EvalOption.Capitalization);

			description = xmlNode.GetValueElement("Description");
			placeholder = xmlNode.GetValueElement("Placeholder", null).SingleLine();
			defaultValue = xmlNode.GetAttribute("default", null);
			defaultValue = xmlNode.GetValueElement("Default", defaultValue);
			return true;
		}


		public virtual void SaveToXml(XmlNode xmlNode)
		{
			xmlNode.AddAttribute("id", id.ToString());

			if (condition != null)
				xmlNode.AddAttribute("rule", condition.ToString());
			if (!isOptional)
				xmlNode.AddAttribute("required", true);
			if (isGlobal)
				xmlNode.AddAttribute("shared", true);
			if (!isEnabled)
				xmlNode.AddAttribute("enabled", false);
			if (isImmediate)
				xmlNode.AddAttribute("immediate", true);

			xmlNode.AddValueElement("Label", label);

			if (string.IsNullOrEmpty(description) == false)
				xmlNode.AddValueElement("Description", description);
			if (string.IsNullOrEmpty(placeholder) == false)
				xmlNode.AddValueElement("Placeholder", placeholder);
			if (string.IsNullOrEmpty(defaultValue) == false)
				xmlNode.AddValueElement("Default", defaultValue);
		}

		public abstract object Clone();
		
		protected U CreateClone<U>() where U : BaseParameter<T>, new()
		{
			U clone = new U();
			clone.recipe = this.recipe;
			clone.id = this.id;
			clone.label = this.label;
			clone.description = this.description;
			clone.placeholder = this.placeholder;
			clone.isOptional = this.isOptional;
			clone.isEnabled = this.isEnabled;
			clone.isGlobal = this.isGlobal;
			clone.isImmediate = this.isImmediate;
			clone.condition = this.condition;
			clone.defaultValue = this.defaultValue;
			clone.value = this.value;
			return clone;
		}

		public virtual void CopyValuesTo<U>(U other) where U : BaseParameter<T>, new()
		{
			other.value = this.value;
			other.isEnabled = this.isEnabled;
		}

		public void ResetToDefault() // Called on instantiation
		{
			var defaultValue = GetDefaultValue();
			if (defaultValue != null) // Parameters with no default behavior return null here
				value = defaultValue;
		}

		private bool IsActive(ParameterState parameterState)
		{
			if (condition == null)
				return true;

			var localContext = Context.Copy(parameterState.evalContext);
			parameterState.localParameters.ApplyToContext(localContext);

			return condition.Evaluate(localContext, new EvaluationCookie() {
				randomizer = parameterState.evalConfig.randomizer,
				ruleSuppliers = parameterState.evalConfig.ruleSuppliers,
			});
		}

		public bool IsActive(Context context)
		{
			if (condition == null)
				return true;

			return condition.Evaluate(context, new EvaluationCookie() {
				ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
			});
		}

		public Type GetParameterType()
		{
			return typeof(T);
		}

		public object GetValue()
		{ 
			return value;
		}

		public abstract T GetDefaultValue();

		public override int GetHashCode()
		{
			int hash = 0x4882786F;

			hash ^= Utility.MakeHashCode(
				id,
				label,
				placeholder,
				condition,
				isOptional,
				isGlobal,
				isImmediate
				);
			return hash;
		}
	}

	public static class ParameterExtensions
	{
		public static bool LoadValueFromXml(this IParameter parameter, XmlNode parameterNode, string characterName, string userName)
		{
			if (parameter is SetFlagParameter || parameter is SetVarParameter || parameter is HintParameter)
				return false;

			bool isEnabled = parameterNode.GetAttributeBool("enabled", true);
			string defaultValue = parameterNode.GetTextValue(parameter.defaultValue);

			if (parameter is BaseParameter<string>)
			{
				var textParameter = parameter as BaseParameter<string>;
				textParameter.Set(Parameter.FromClipboard(defaultValue, characterName, userName));
				textParameter.isEnabled = isEnabled;
				return true;
			}
			else if (parameter is BaseParameter<bool>)
			{
				var boolParameter = parameter as BaseParameter<bool>;
				boolParameter.Set(Utility.StringToBool(defaultValue));
				boolParameter.isEnabled = isEnabled;
				return true;
			}
			else if (parameter is BaseParameter<decimal>)
			{
				var numberParameter = parameter as BaseParameter<decimal>;
				numberParameter.Set(Utility.StringToDecimal(defaultValue));
				numberParameter.isEnabled = isEnabled;
				return true;
			}
			else if (parameter is BaseParameter<HashSet<string>>)
			{
				var collectionParameter = parameter as BaseParameter<HashSet<string>>;
				collectionParameter.Set(new HashSet<string>(Utility.ListFromCommaSeparatedString(defaultValue)));
				collectionParameter.isEnabled = isEnabled;
				return true;
			}
			else if (parameter is BaseParameter<Lorebook>)
			{
				var lorebookParameter = parameter as BaseParameter<Lorebook>;
				lorebookParameter.value.LoadFromXml(parameterNode, characterName, userName);
				lorebookParameter.isEnabled = isEnabled;
				return true;
			}

			return false;
		}

		public static XmlNode SaveValueToXml(this IParameter parameter, XmlNode xmlNode)
		{
			string value = null;
			if (parameter is SetFlagParameter || parameter is SetVarParameter || parameter is EraseParameter || parameter is HintParameter)
				return null;

			bool cdata = false;
			
			if (parameter is BaseParameter<string>)
			{
				if (parameter is TextParameter)
				{
					var textParameter = parameter as TextParameter;
					if (!string.IsNullOrEmpty(textParameter.value))
					{
						cdata = textParameter.mode == TextParameter.Mode.Code || textParameter.isRaw;
						value = textParameter.isRaw == false ? Parameter.ToClipboard(textParameter.value) : textParameter.value;
					}
				}
				else
				{
					var stringParameter = parameter as BaseParameter<string>;
					if (!string.IsNullOrEmpty(stringParameter.value) && stringParameter.value != stringParameter.GetDefaultValue())
						value = Parameter.ToClipboard(stringParameter.value);
				}
			}
			else if (parameter is BaseParameter<bool>)
			{
				var boolParameter = parameter as BaseParameter<bool>;
				if (boolParameter.value != boolParameter.GetDefaultValue())
					value = boolParameter.value ? "true" : "false";
			}
			else if (parameter is BaseParameter<decimal>)
			{
				var numberParameter = parameter as BaseParameter<decimal>;
				if (numberParameter.value != default(decimal) && numberParameter.value != numberParameter.GetDefaultValue())
					value = numberParameter.value.ToString(CultureInfo.InvariantCulture);
			}
			else if (parameter is BaseParameter<HashSet<string>>)
			{
				var collectionParameter = parameter as BaseParameter<HashSet<string>>;
				if (collectionParameter.value.Count > 0)
					value = Utility.ListToCommaSeparatedString(collectionParameter.value);
			}
			else if (parameter is BaseParameter<Lorebook>)
			{
				var lorebookParameter = parameter as BaseParameter<Lorebook>;

				var parameterNode = xmlNode.AddElement("Parameter");
				parameterNode.AddAttribute("id", parameter.id.ToString());
				if (parameter.isEnabled == false)
					parameterNode.AddAttribute("enabled", false);

				lorebookParameter.value.SaveToXml(parameterNode);
				return parameterNode;
			}

			if (string.IsNullOrEmpty(value) == false || parameter.isEnabled == false)
			{
				var parameterNode = xmlNode.AddElement("Parameter");
				parameterNode.AddAttribute("id", parameter.id.ToString());
				if (cdata)
					parameterNode.AddCData(value);
				else
					parameterNode.AddTextValue(value);
				if (parameter.isEnabled == false)
					parameterNode.AddAttribute("enabled", false);
				return parameterNode;
			}

			return null;
		}
	}

	public class ParameterCollection
	{
		private Dictionary<StringHandle, ContextualValue> _values = new Dictionary<StringHandle, ContextualValue>();
		private HashSet<StringHandle> _flags = new HashSet<StringHandle>();

		public void SetValue(StringHandle id, ContextualValue value)
		{
			if (_values.ContainsKey(id))
				_values[id] = value;
			else
				_values.Add(id, value);
		}

		public bool TryGetValue(StringHandle id, out ContextualValue value)
		{
			return _values.TryGetValue(id, out value);
		}

		public void SetFlag(StringHandle flag)
		{
			_flags.Add(flag);
		}

		public void SetFlags(IEnumerable<StringHandle> flags)
		{
			_flags.UnionWith(flags);
		}

		public void ApplyToContext(Context context)
		{
			context.AddTags(_flags);
			foreach (var kvp in _values)
				context.SetValue(kvp.Key, kvp.Value.ToString());
		}

		public void CopyFromContext(Context context)
		{
			SetFlags(context.tags);
			foreach (var value in context.values)
				SetValue(value.Key, value.Value);
		}

		public void Erase(StringHandle id)
		{
			_values.Remove(id);
			_flags.Remove(id);
		}

	}

	public enum ParameterScope
	{
		Global,
		Local
	}

	public class ParameterState
	{
		public ParameterCollection globalParameters = new ParameterCollection();
		public ParameterCollection localParameters = new ParameterCollection();

		public Context evalContext;
		public ContextString.EvaluationConfig evalConfig;

		private Dictionary<StringHandle, string> _reserved = new Dictionary<StringHandle, string>();

		private ParameterCollection GetCollection(ParameterScope scope)
		{
			return scope == ParameterScope.Global ? globalParameters : localParameters;
		}

		public void SetFlag(StringHandle flag, ParameterScope scope)
		{
			GetCollection(scope).SetFlag(flag);
			if (scope == ParameterScope.Global)
				evalContext.AddTag(flag);
			else
				localParameters.SetFlag(string.Concat(flag.ToString(), ":local"));
		}

		public void SetFlags(IEnumerable<StringHandle> flags, ParameterScope scope)
		{
			GetCollection(scope).SetFlags(flags);
			if (scope == ParameterScope.Global)
				evalContext.AddTags(flags);
			else
			{
				foreach (var flag in flags)
					localParameters.SetFlag(string.Concat(flag.ToString(), ":local"));
			}
		}

		public void SetValue(StringHandle id, string value, ParameterScope scope)
		{
			GetCollection(scope).SetValue(id, value);
			if (scope == ParameterScope.Global)
				evalContext.SetValue(id, value);
			else
			{
				localParameters.SetFlag(string.Concat(id.ToString(), ":local"));
				localParameters.SetValue(string.Concat(id.ToString(), ":local"), value);
			}
		}

		public void SetValue(StringHandle id, float value, ParameterScope scope)
		{
			GetCollection(scope).SetValue(id, value);
			if (scope == ParameterScope.Global)
				evalContext.SetValue(id, value);
			else if (id.ToString().IndexOf(':') == -1)
			{
				localParameters.SetFlag(string.Concat(id.ToString(), ":local"));
				localParameters.SetValue(string.Concat(id.ToString(), ":local"), value);
			}
		}

		public void Erase(StringHandle id)
		{
			globalParameters.Erase(id);
			evalContext.SetValue(id, null);
			evalContext.RemoveTag(id);
			_reserved.Remove(id);
		}

		public void Reserve(StringHandle id, string reservedValue)
		{
			_reserved.Add(id, reservedValue);
		}

		public bool TryGetReservedValue(StringHandle id, out string reservedValue)
		{
			return _reserved.TryGetValue(id, out reservedValue);
		}

		public bool IsReserved(StringHandle id)
		{
			return _reserved.ContainsKey(id);
		}
		
		public void CopyReserved(ParameterState state)
		{
			_reserved = _reserved.Union(state._reserved)
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		}
	}

	public class ParameterStates : IValueSupplier
	{
		public ParameterStates(ICollection<Recipe> recipes)
		{
			_states = new ParameterState[recipes.Count];
		}

		public ParameterState this[int index]
		{
			get { return _states[index]; }
			set { _states[index] = value; }
		}

		private ParameterState[] _states;

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

}
