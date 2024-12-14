using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Ginger
{
	public class MultiChoiceParameter : BaseParameter<HashSet<string>>
	{
		public struct Item
		{
			public StringHandle id;
			public string label;
			public string value;
		}
		public List<Item> items = new List<Item>();

		public MultiChoiceParameter() : base()
		{
		}

		public MultiChoiceParameter(Recipe recipe) : base(recipe)
		{
			value = new HashSet<string>();
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			if (base.LoadFromXml(xmlNode) == false)
				return false;

//			value = GetDefaultValue();

			var itemNode = xmlNode.GetFirstElement("Option");
			while (itemNode != null)
			{
				StringHandle id = itemNode.GetAttribute("id", null);
				string label = itemNode.GetTextValue().SingleLine();
				string value = itemNode.GetAttribute("value", null);
				if (StringHandle.IsNullOrEmpty(id))
					id = label;
				if (string.IsNullOrEmpty(label))
					label = id.ToString();
				if (value == null)
					value = id.ToString();

				if (StringHandle.IsNullOrEmpty(id) == false)
				{
					items.Add(new Item() {
						id = id,
						label = label,
						value = value,
					});
				}

				itemNode = itemNode.GetNextSibling();
			}

			value.IntersectWith(items.Select(i => i.id.ToString()));
			return true;
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("Choice");
			base.SaveToXml(node);

			node.AddAttribute("style", "multiple");
			foreach (var item in items)
			{
				var itemNode = node.AddElement("Option");
				itemNode.AddAttribute("id", item.id.ToString());
				itemNode.AddAttribute("value", item.value);
				itemNode.AddTextValue(item.label);
			}
		}

		public override void OnApply(ParameterState state, ParameterScope scope)
		{
			if (value == null || value.Count == 0)
				return;

			var list = new HashSet<string>(value
				.Select(t => GingerString.FromParameter(t).ToString()));
			var text = Utility.ListToDelimitedString(list.Select(itemID => {
				int index = items.FindIndex(ii => ii.id == itemID);
				if (index != -1)
					return items[index].label;
				return itemID;
			}), Text.Delimiter);

			string sList = Utility.ListToDelimitedString(list, Text.Delimiter);
			state.SetValue(id, sList, scope);
			state.SetValue(id + ":value", text, scope);
			state.SetValue(id + ":text", text, scope);

			if (scope == ParameterScope.Local)
				state.SetFlags(list.Select(s => new StringHandle(s)), scope);
			
			if (isGlobal && scope == ParameterScope.Global)
				state.Reserve(id, sList);
		}

		public override object Clone()
		{
			var clone = CreateClone<MultiChoiceParameter>();
			clone.value = new HashSet<string>(this.value);
			clone.items = new List<Item>(this.items);
			return clone;
		}

		public void CopyValuesTo(MultiChoiceParameter other)
		{
			base.CopyValuesTo(other);
			other.value = new HashSet<string>(this.value.Intersect(items.Select(i => i.id.ToString())));
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "Choice".GetHashCode();
			hash ^= "multiple".GetHashCode();
			hash ^= Utility.MakeHashCode(items, Utility.HashOption.None);
			return hash;
		}

		public override void Set(HashSet<string> value)
		{
			this.value = new HashSet<string>(value.Intersect(items.Select(i => i.id.ToString())));
		}

		public override HashSet<string> GetDefaultValue()
		{
			return new HashSet<string>(Utility.ListFromCommaSeparatedString(defaultValue));
		}
	}
}
