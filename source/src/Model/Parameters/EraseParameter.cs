using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Ginger
{
	public class EraseParameter : BaseParameter<string>, IInvisibleParameter
	{
		private HashSet<StringHandle> flags = new HashSet<StringHandle>();

		public EraseParameter() : base()
		{
			isEnabled = true;
			isOptional = false;
			isGlobal = true;
		}

		public EraseParameter(Recipe recipe) : base(recipe)
		{
			isEnabled = true;
			isOptional = false;
			isGlobal = true;
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			string value = xmlNode.GetTextValue();
			flags = new HashSet<StringHandle>(Utility.ListFromCommaSeparatedString(value).Select(s => new StringHandle(s)));
			if (flags.Count == 0)
				return false;

			id = Utility.ListToCommaSeparatedString(flags);

			if (xmlNode.HasAttribute("rule"))
				condition = Rule.Parse(xmlNode.GetAttribute("rule"));
			return true;
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("Erase");
			node.AddTextValue(Utility.ListToCommaSeparatedString(flags));

			if (condition != null)
				node.AddAttribute("rule", condition.ToString());
		}

		public override void OnApplyToContext(Context context, Context localContext, ContextString.EvaluationConfig evalConfig)
		{
			foreach (var flag in flags)
			{
				context.SetValue(flag, null);
				context.RemoveTag(flag);
				context.AddTag(string.Concat("__erase-", flag)); // Mark flag to be erased from global context
			}
		}

		public override void ResetToDefault()
		{
		}

		public override object Clone()
		{
			var clone = CreateClone<EraseParameter>();
			clone.flags = new HashSet<StringHandle>(this.flags);
			return clone;
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "Erase".GetHashCode();
			return hash;
		}
	}
}
