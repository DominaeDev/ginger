﻿using System.Linq;
using System.Xml;

namespace Ginger
{
	public class HintParameter : BaseParameter<string>
	{
		public HintParameter() : base()
		{
		}

		public HintParameter(Recipe recipe) : base(recipe)
		{
			id = string.Format("__hint-{0}", recipe.parameters.Count(p => p is HintParameter));
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			value = xmlNode.GetTextValue();
			defaultValue = value;
			isEnabled = true;
			isOptional = false;
			if (xmlNode.HasAttribute("rule"))
				condition = Rule.Parse(xmlNode.GetAttribute("rule"));

			return true;
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("Hint");
			node.AddTextValue(value);
			if (condition != null)
				node.AddAttribute("rule", condition.ToString());
		}

		public override void OnApply(ParameterState state, Parameter.Scope scope) { }

		public override object Clone()
		{
			return CreateClone<HintParameter>();
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "Hint".GetHashCode();
			return hash;
		}

	}
}
