using System.Collections.Generic;
using System.Xml;

namespace Ginger
{
	public class ChoiceParameter : BaseParameter<string>
	{
		public struct Item
		{
			public StringHandle id;
			public string label;
			public string value;
		}

		public enum Style
		{
			List,
			Radio,
			Multiple,
			Slider,
			Custom,
			Default = List,
		}
		public Style style;

		public List<Item> items = new List<Item>();
		public int selectedIndex = -1;

		public ChoiceParameter() : base()
		{
		}

		public ChoiceParameter(Recipe recipe) : base(recipe)
		{
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			if (base.LoadFromXml(xmlNode) == false)
				return false;

			style = xmlNode.GetAttributeEnum("style", Style.Default);

			defaultValue = xmlNode.GetAttribute("default", default(string));
			defaultValue = xmlNode.GetValueElement("Default", defaultValue);
			if (defaultValue != default(string))
				value = defaultValue;

			var itemNode = xmlNode.GetFirstElement("Option");
			while (itemNode != null)
			{
				StringHandle id = itemNode.GetAttribute("id", null);
				string label = itemNode.GetTextValue(null).SingleLine();
				if (StringHandle.IsNullOrEmpty(id) && string.IsNullOrEmpty(label))
				{
					itemNode = itemNode.GetNextSibling();
					continue;
				}
				if (StringHandle.IsNullOrEmpty(id))
					id = label;
				if (string.IsNullOrEmpty(label))
					label = id.ToString();

				if (StringHandle.IsNullOrEmpty(id) == false)
				{
					items.Add(new Item() {
						id = id,
						label = label,
						value = id.ToString(),
					});
				}

				itemNode = itemNode.GetNextSibling();
			}

			selectedIndex = -1;

			if (items.Count == 0)
				return false;

			if (isOptional == false && defaultValue == default(string)) // Auto-set first item
			{
				value = items[0].value;
				defaultValue = value;
				selectedIndex = 0;
			}
			else
			{
				selectedIndex = items.FindIndex(i => string.Compare(i.value, value, true) == 0);
				if (selectedIndex == -1 && string.IsNullOrWhiteSpace(value) == false)
					selectedIndex = -2; // custom
			}

			return true;
		}

		public override void Set(string value)
		{
			base.Set(value);

			// Calculate index
			selectedIndex = items.FindIndex(i => string.Compare(i.value, value, true) == 0 || string.Compare(i.label, value, true) == 0);
			if (selectedIndex == -1 && string.IsNullOrWhiteSpace(value) == false)
				selectedIndex = -2; // custom
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("Choice");
			base.SaveToXml(node);

			if (style != Style.Default)
				node.AddAttribute("style", EnumHelper.ToString(style).ToLowerInvariant());

			if (string.IsNullOrEmpty(defaultValue) == false)
				node.AddValueElement("Default", defaultValue);

			foreach (var item in items)
			{
				var itemNode = node.AddElement("Option");
				itemNode.AddAttribute("id", item.id.ToString());
				itemNode.AddTextValue(item.label);
			}
		}

		public override void OnApply(ParameterState state, ParameterScope scope)
		{
			if (string.IsNullOrEmpty(value) == false)
			{
				state.SetValue(id, value, scope);
				state.SetFlag(value, scope);
			}

			state.SetValue(id + ":index", selectedIndex, scope);
			if (selectedIndex == -2) // Custom
			{
				state.SetFlag(id + ":custom", scope);
				state.SetValue(id + ":id", "custom", scope);
				state.SetValue(id + ":value", value, scope);
			}
			else if (selectedIndex >= 0 && selectedIndex < items.Count)
			{
				state.SetValue(id + ":id", items[selectedIndex].id.ToString(), scope);
				state.SetValue(id + ":value", items[selectedIndex].label, scope);
			}
		}

		public override object Clone()
		{
			var clone = CreateClone<ChoiceParameter>();
			clone.items = new List<Item>(this.items);
			clone.style = this.style;
			clone.selectedIndex = this.selectedIndex;
			return clone;
		}

		public void CopyValuesTo(ChoiceParameter other)
		{
			base.CopyValuesTo(other);
			other.selectedIndex = this.selectedIndex;
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "Choice".GetHashCode();
			hash ^= Utility.MakeHashCode(style);
			hash ^= Utility.MakeHashCode(items, Utility.HashOption.Ordered);
			return hash;
		}

		public override void ResetToDefault() // Called on instantiation
		{
			Context context = Current.Character.GetContext(CharacterData.ContextType.WithFlags);

			// Evaluate default value
			if (string.IsNullOrEmpty(defaultValue) == false)
			{
				value = GingerString.FromString(Text.Eval(defaultValue, context,
					new ContextString.EvaluationConfig() {
						macroSuppliers = new IMacroSupplier[] { Current.Strings },
						referenceSuppliers = new IStringReferenceSupplier[] { Current.Strings },
						ruleSuppliers = new IRuleSupplier[] { Current.Strings },
					},
					Text.EvalOption.Minimal)).ToBaked();

				return;
			}

			base.ResetToDefault();
		}
	}
}
