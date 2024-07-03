using System.Xml;

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
			defaultValue = xmlNode.GetAttributeBool("default", default(bool));
			defaultValue = xmlNode.GetValueElementBool("Default", defaultValue);
			value = defaultValue;
			return base.LoadFromXml(xmlNode);
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("Toggle");
			base.SaveToXml(node);

			if (defaultValue)
				node.AddValueElement("Default", true);
		}

		public override void OnApply(ParameterState state, ParameterScope scope)
		{
			if (value)
				state.SetFlag(id, scope);
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
	}
}
