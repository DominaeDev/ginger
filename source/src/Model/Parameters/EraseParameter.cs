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
			scope = Parameter.Scope.Both;
		}

		public EraseParameter(Recipe recipe) : base(recipe)
		{
			isEnabled = true;
			isOptional = false;
			scope = Parameter.Scope.Both;
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			string value = xmlNode.GetTextValue();
			flags = new HashSet<StringHandle>(Utility.ListFromCommaSeparatedString(value).Select(s => new StringHandle(s)));
			if (flags.Count == 0)
				return false;

			id = Utility.ListToCommaSeparatedString(flags);
			scope = xmlNode.GetAttributeEnum("scope", Parameter.Scope.Both);

			if (xmlNode.HasAttribute("rule"))
				condition = Rule.Parse(xmlNode.GetAttribute("rule"));
			return true;
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("Erase");
			node.AddTextValue(Utility.ListToCommaSeparatedString(flags));

			if (scope != Parameter.Scope.Both)
				node.AddAttribute("scope", EnumHelper.ToString(scope).ToLowerInvariant());

			if (condition != null)
				node.AddAttribute("rule", condition.ToString());
		}

		public override void OnApply(ParameterState state, Parameter.Scope scope)
		{
			if (this.scope.Contains(scope))
			{
				foreach (var flag in flags)
					state.Erase(flag, scope);
			}
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
