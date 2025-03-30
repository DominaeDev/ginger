using System.Collections.Generic;
using System.Xml;

namespace Ginger
{
	public class ChoiceParameter : BaseParameter<string>, IResettableParameter
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
			Actors,
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

			if (style == Style.Actors)
			{
				defaultValue = null;
				items.Clear();
				return true;
			}

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

		public override void OnApply(ParameterState state, Parameter.Scope scope)
		{
			if (string.IsNullOrEmpty(value))
				return;

			if (selectedIndex == -2) // Custom
			{
				state.SetValue(id, value, scope);
				state.SetValue(id + ":id", "custom", scope);
				state.SetValue(id + ":text", value, scope);
				state.SetValue(id + ":value", value, scope);	// Deprecated
				state.SetValue(id + ":index", -1, scope);
				state.SetFlag(id + ":custom", scope);
				state.SetFlag(id, scope);

				if (scope == Parameter.Scope.Local)
					state.SetFlag(value, scope);

				if (isGlobal && scope == Parameter.Scope.Global)
					state.Reserve(id, uid, value);
			}
			else if (style == Style.Actors) // Actors
			{
				string actorID;
				string actorName;
				int actorIndex;
				if (value == "user")
				{
					actorID = "user";
					actorName = AppSettings.Settings.AutoConvertNames ? Current.Card.userPlaceholder : GingerString.UserMarker;
				}
				else if (int.TryParse(value, out actorIndex) && actorIndex >= 0 && actorIndex < Current.Characters.Count)
				{
					actorID = string.Format("actor-{0}", actorIndex);
					actorName = Current.Characters[actorIndex].name;
				}
				else
					return;

				state.SetValue(id, actorName, scope);
				state.SetValue(id + ":id", actorID, scope);
				state.SetValue(id + ":text", actorName, scope);
				state.SetValue(id + ":value", actorName, scope);    // Deprecated
				if (isGlobal && scope == Parameter.Scope.Global)
					state.Reserve(id, uid, value);
			}
			else if (selectedIndex >= 0 && selectedIndex < items.Count)
			{
				string textValue = items[selectedIndex].label;
				state.SetValue(id, textValue, scope);
				state.SetValue(id + ":id", value, scope);
				state.SetValue(id + ":text", textValue, scope);
				state.SetValue(id + ":value", textValue, scope);	// Deprecated
				state.SetValue(id + ":index", selectedIndex, scope);

				if (scope == Parameter.Scope.Local)
					state.SetFlag(value, scope);

				if (isGlobal && scope == Parameter.Scope.Global)
					state.Reserve(id, uid, textValue);
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

		public void ResetValue(string value)
		{
			this.value = value;
		}
	}
}
