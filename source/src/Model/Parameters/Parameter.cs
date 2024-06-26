﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;

namespace Ginger
{
	public interface IParameter : IXmlLoadable, IXmlSaveable, ICloneable
	{
		StringHandle id { get; }
		string label { get; }
		string description { get; }
		bool isOptional { get; }
		bool isEnabled { get; }
		bool isLocal { get; }
		bool isGlobal { get; }
		bool isImmediate { get; }

		bool IsActive(Context context);

		void ApplyToContext(Context globalContext, Context localContext, ContextString.EvaluationConfig evalConfig);
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
			StringBuilder sb = new StringBuilder(value);

			Utility.ReplaceWholeWord(sb, GingerString.CharacterMarker, "__CCCC__", true);
			Utility.ReplaceWholeWord(sb, GingerString.UserMarker, "__UUUU__", true);

			if (AppSettings.Settings.AutoConvertNames)
			{
				string userPlaceholder = (Current.Card.userPlaceholder ?? "").Trim();
				if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
					Utility.ReplaceWholeWord(sb, userPlaceholder, "__UUUU__", false);
				string characterPlaceholder = (Current.Character.namePlaceholder ?? "").Trim();
				if (string.IsNullOrWhiteSpace(characterPlaceholder) == false)
					Utility.ReplaceWholeWord(sb, characterPlaceholder, "__CCCC__", false);
			}

			sb.Replace("__CCCC__", GingerString.CharacterMarker);
			sb.Replace("__UUUU__", GingerString.UserMarker);

			return sb.ToString();
		}

		public static string FromClipboard(string value)
		{
			StringBuilder sb = new StringBuilder(value);

			if (AppSettings.Settings.AutoConvertNames)
			{
				string userPlaceholder = (Current.Card.userPlaceholder ?? "").Trim();
				string characterPlaceholder = (Current.MainCharacter.spokenName ?? "").Trim();

				if (string.IsNullOrWhiteSpace(characterPlaceholder) == false)
					sb.Replace(GingerString.CharacterMarker, characterPlaceholder);
				if (string.IsNullOrWhiteSpace(userPlaceholder) == false)
					sb.Replace(GingerString.UserMarker, userPlaceholder);
			}

			return sb.ToString();
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
		public ICondition condition { get; protected set; }
		public abstract void OnApplyToContext(Context context, Context localContext, ContextString.EvaluationConfig evalConfig);
		public Recipe recipe { get; protected set; }

		public T defaultValue;
		public T value;

		public virtual void Set(T value)
		{
			this.value = value;
		}

		protected BaseParameter() { }

		protected BaseParameter(Recipe recipe)
		{
			this.recipe = recipe;
		}

		public void ApplyToContext(Context globalContext, Context localContext, ContextString.EvaluationConfig evalConfig)
		{
			if (!isEnabled || !IsActive(localContext))
				return;

			if (isLocal)
				OnApplyToContext(localContext, localContext, evalConfig);
			if (isGlobal)
				OnApplyToContext(globalContext, localContext, evalConfig);
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

		public virtual void ResetToDefault()
		{
			value = defaultValue;
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
		public static bool LoadValueFromXml(this IParameter parameter, XmlNode parameterNode, bool bLoadFromClipboard = false)
		{
			if (parameter is SetFlagParameter || parameter is SetVarParameter || parameter is HintParameter)
				return false;

			bool isEnabled = parameterNode.GetAttributeBool("enabled", true);

			if (parameter is BaseParameter<string>)
			{
				var textParameter = parameter as BaseParameter<string>;
				string value = parameterNode.GetTextValue(textParameter.defaultValue);
				textParameter.Set(bLoadFromClipboard ? Parameter.FromClipboard(value) : value);
				textParameter.isEnabled = isEnabled;
				return true;
			}
			else if (parameter is BaseParameter<bool>)
			{
				var boolParameter = parameter as BaseParameter<bool>;
				boolParameter.Set(parameterNode.GetTextValueBool(boolParameter.defaultValue));
				boolParameter.isEnabled = isEnabled;
				return true;
			}
			else if (parameter is BaseParameter<decimal>)
			{
				var numberParameter = parameter as BaseParameter<decimal>;
				numberParameter.Set(parameterNode.GetTextValueDecimal(numberParameter.defaultValue));
				numberParameter.isEnabled = isEnabled;
				return true;
			}
			else if (parameter is BaseParameter<HashSet<string>>)
			{
				var collectionParameter = parameter as BaseParameter<HashSet<string>>;
				collectionParameter.Set(new HashSet<string>(Utility.ListFromCommaSeparatedString(parameterNode.GetTextValue())));
				collectionParameter.isEnabled = isEnabled;
				return true;
			}
			else if (parameter is BaseParameter<Lorebook>)
			{
				var lorebookParameter = parameter as BaseParameter<Lorebook>;
				lorebookParameter.value.LoadFromXml(parameterNode, bLoadFromClipboard);
				lorebookParameter.isEnabled = isEnabled;
				return true;
			}

			return false;
		}

		public static XmlNode SaveValueToXml(this IParameter parameter, XmlNode xmlNode, bool bSaveToClipboard = false)
		{
			string value = null;
			if (parameter is SetFlagParameter || parameter is SetVarParameter || parameter is HintParameter)
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
						value = (bSaveToClipboard && textParameter.isRaw == false) ? Parameter.ToClipboard(textParameter.value) : textParameter.value;
					}
				}
				else
				{
					var stringParameter = parameter as BaseParameter<string>;
					if (!string.IsNullOrEmpty(stringParameter.value) && stringParameter.value != stringParameter.defaultValue)
						value = bSaveToClipboard ? Parameter.ToClipboard(stringParameter.value) : stringParameter.value;
				}
			}
			else if (parameter is BaseParameter<bool>)
			{
				var boolParameter = parameter as BaseParameter<bool>;
				if (boolParameter.value != boolParameter.defaultValue)
					value = boolParameter.value ? "true" : "false";
			}
			else if (parameter is BaseParameter<decimal>)
			{
				var numberParameter = parameter as BaseParameter<decimal>;
				if (numberParameter.value != default(decimal) && numberParameter.value != numberParameter.defaultValue)
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

				lorebookParameter.value.SaveToXml(parameterNode, bSaveToClipboard);
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

}
