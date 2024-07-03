using System.Xml;

namespace Ginger
{
	public class SetVarParameter : BaseParameter<string>, IInvisibleParameter
	{
		public SetVarParameter() : base()
		{
			isEnabled = true;
			isOptional = false;
			isGlobal = true;
		}

		public SetVarParameter(Recipe recipe) : base(recipe)
		{
			isEnabled = true;
			isOptional = false;
			isGlobal = true;
		}

		public override bool isLocal { get { return false; } }

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			id = xmlNode.GetAttribute("id", null);
			if (StringHandle.IsNullOrEmpty(id))
				return false;

			if (xmlNode.HasAttribute("rule"))
				condition = Rule.Parse(xmlNode.GetAttribute("rule"));

			value = xmlNode.GetTextValue().SingleLine();
			return true;
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("SetVar");
			node.AddAttribute("id", id.ToString());
			node.AddTextValue(value);

			if (condition != null)
				node.AddAttribute("rule", condition.ToString());
		}

		public override void OnApply(ParameterState state, ParameterScope scope)
		{
			if (string.IsNullOrEmpty(value) == false && scope == ParameterScope.Global) // Global only
			{
				if (value.IndexOfAny(new char[] { '{', '}', '[', ']' }) != -1)
					state.SetValue(id, Text.Eval(value, state.evalContext, state.evalConfig), ParameterScope.Global);
				else
					state.SetValue(id, value, ParameterScope.Global);
			}
		}

		public override object Clone()
		{
			return CreateClone<SetVarParameter>();
		}

		public override void ResetToDefault()
		{
			// Do nothing
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "SetVar".GetHashCode();
			return hash;
		}

	}
}
