using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Ginger
{
	public class SetFlagParameter : BaseParameter<string>, IInvisibleParameter
	{
		public SetFlagParameter() : base()
		{
			isEnabled = true;
			isOptional = false;
			scope = Parameter.Scope.Global;
		}

		public SetFlagParameter(Recipe recipe) : base(recipe)
		{
			isEnabled = true;
			isOptional = false;
			scope = Parameter.Scope.Global;
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			value = xmlNode.GetTextValue().SingleLine();
			id = new StringHandle(string.Concat("__flag_", value));
			scope = xmlNode.GetAttributeEnum("scope", Parameter.Scope.Global);

			if (xmlNode.HasAttribute("rule"))
				condition = Rule.Parse(xmlNode.GetAttribute("rule"));
			return true;
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("SetFlag");
			node.AddTextValue(value);

			if (scope != Parameter.Scope.Global)
				node.AddAttribute("scope", EnumHelper.ToString(scope).ToLowerInvariant());

			if (condition != null)
				node.AddAttribute("rule", condition.ToString());
		}

		public override void OnApply(ParameterState state, Parameter.Scope scope)
		{
			if (string.IsNullOrEmpty(value) == false && this.scope.Contains(scope))
			{
				string sValue;
				if (value.IndexOfAny(new char[] { '{', '}', '[', ']' }) != -1)
					sValue = Text.Eval(value, state.evalContext, state.evalConfig);
				else
					sValue = value;

				var flags = new HashSet<StringHandle>(Utility.ListFromCommaSeparatedString(sValue).Select(s => new StringHandle(s)));
				state.SetFlags(flags, scope);
			}
		}

		public override object Clone()
		{
			return CreateClone<SetFlagParameter>();
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "SetFlag".GetHashCode();
			return hash;
		}
	}
}
