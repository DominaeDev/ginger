﻿using System.Xml;

namespace Ginger
{
	public class BooleanParameter : BaseParameter<bool>
	{
		public BooleanParameter() : base()
		{
		}

		public BooleanParameter(Recipe recipe) : base(recipe)
		{
			isEnabled = true;
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			return base.LoadFromXml(xmlNode);
//			value = GetDefaultValue();
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("Toggle");
			base.SaveToXml(node);
		}

		public override void OnApply(ParameterState state, Parameter.Scope scope)
		{
			if (value)
				state.SetFlag(id, scope);
			if (isGlobal && scope == Parameter.Scope.Global)
				state.Reserve(id, uid, this.value ? "Yes" : "No");
		}

		public override object Clone()
		{
			return CreateClone<BooleanParameter>();
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "Toggle".GetHashCode();
			return hash;
		}

		public override bool GetDefaultValue()
		{
			return Utility.StringToBool(defaultValue);
		}
	}
}
