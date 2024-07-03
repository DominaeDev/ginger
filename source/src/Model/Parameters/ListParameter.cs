using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Ginger
{
	public class ListParameter : BaseParameter<HashSet<string>>
	{
		public ListParameter() : base()
		{
		}

		public ListParameter(Recipe recipe) : base(recipe)
		{
			value = new HashSet<string>();
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			if (base.LoadFromXml(xmlNode) == false)
				return false;

			//			value = GetDefaultValue();
			return true;
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("List");
			base.SaveToXml(node);
		}

		public override void OnApply(ParameterState state, ParameterScope scope)
		{
			if (value != null && value.Count > 0)
			{
				var items = new HashSet<string>(value.Select(t => GingerString.FromParameter(t).ToString()));
				string sItems = Utility.ListToDelimitedString(items, Text.Delimiter);
				state.SetValue(id, sItems, scope);
				state.SetValue(id + ":count", items.Count, scope);
			}
		}

		public override object Clone()
		{
			var clone = CreateClone<ListParameter>();
			clone.value = new HashSet<string>(this.value);
			return clone;
		}

		public void CopyValuesTo(ListParameter other)
		{
			base.CopyValuesTo(other);
			other.value = new HashSet<string>(this.value);
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "List".GetHashCode();
			return hash;
		}

		public override HashSet<string> GetDefaultValue()
		{
			return new HashSet<string>(Utility.ListFromCommaSeparatedString(defaultValue));
		}
	}
}
