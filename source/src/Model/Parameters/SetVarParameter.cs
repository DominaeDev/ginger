using System.Xml;

namespace Ginger
{
	public class SetVarParameter : BaseParameter<string>, IInvisibleParameter
	{
		public SetVarParameter() : base()
		{
			isEnabled = true;
			isOptional = false;
			scope = Parameter.Scope.Global;
		}

		public SetVarParameter(Recipe recipe) : base(recipe)
		{
			isEnabled = true;
			isOptional = false;
			scope = Parameter.Scope.Global;
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			id = xmlNode.GetAttribute("id", null);
			if (StringHandle.IsNullOrEmpty(id))
				return false;

			if (xmlNode.HasAttribute("rule"))
				condition = Rule.Parse(xmlNode.GetAttribute("rule"));
			scope = xmlNode.GetAttributeEnum("scope", Parameter.Scope.Global);

			value = xmlNode.GetTextValue().SingleLine();
			defaultValue = value;
			return true;
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("SetVar");
			node.AddAttribute("id", id.ToString());
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
				state.SetValue(id, sValue, scope);
			}
		}

		public override object Clone()
		{
			return CreateClone<SetVarParameter>();
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "SetVar".GetHashCode();
			return hash;
		}

		public override string GetDefaultValue()
		{
			return null;
		}
	}
}
