using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Ginger
{
	public class SetFlagParameter : BaseParameter<string>, IInvisibleParameter
	{
        public override bool isLocal { get { return false; } }

		private HashSet<StringHandle> flags = new HashSet<StringHandle>();

		public SetFlagParameter() : base()
		{
			isEnabled = true;
			isOptional = false;
			isGlobal = true;
		}

		public SetFlagParameter(Recipe recipe) : base(recipe)
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
			isGlobal = xmlNode.GetAttributeBool("shared", true);

			if (xmlNode.HasAttribute("rule"))
				condition = Rule.Parse(xmlNode.GetAttribute("rule"));
			return true;
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("SetFlag");
			node.AddTextValue(Utility.ListToCommaSeparatedString(flags));

			if (condition != null)
				node.AddAttribute("rule", condition.ToString());
		}

		public override void OnApply(ParameterState state, Parameter.Scope scope)
		{
			if (scope == Parameter.Scope.Global) // Global only
				state.SetFlags(flags, scope);
		}

		public override object Clone()
		{
			var clone = CreateClone<SetFlagParameter>();
			clone.flags = new HashSet<StringHandle>(this.flags);
			return clone;
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "SetFlag".GetHashCode();
			return hash;
		}

		public override string GetDefaultValue()
		{
			return null;
		}
	}
}
