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
			string sDefault = xmlNode.GetAttribute("default", default(string));
			sDefault = xmlNode.GetValueElement("Default", sDefault);
			defaultValue = new HashSet<string>(Utility.ListFromCommaSeparatedString(sDefault));
			value = defaultValue;
			return base.LoadFromXml(xmlNode);
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("List");
			base.SaveToXml(node);

			if (defaultValue.Count > 0)
				node.AddValueElement("Default", Utility.ListToCommaSeparatedString(defaultValue));
		}

		public override void OnApplyToContext(Context context, Context localContext, ContextString.EvaluationConfig evalConfig)
		{
			if (value != null && value.Count > 0)
			{
				var collection = new HashSet<string>(value.Select(t => GingerString.FromParameter(t).ToString()));

//				context.AddTags(collection.Select(s => new StringHandle(s)));
				string sCollection = Utility.ListToDelimitedString(collection, Text.Delimiter);
				context.SetValue(id, sCollection);
				context.SetValue(id + ":count", collection.Count);
				localContext.SetValue(string.Concat(id.ToString(), ":local"), sCollection);
				localContext.AddTag(string.Concat(id.ToString(), ":local"));
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

	}
}
