using System.Xml;

namespace Ginger
{
	public class CharacterAdjective
	{
		public enum Order
		{
			Undefined	= -1,
			Opinion		= 0,		// Beautiful
			Size		= 1,		// Large
			Quality		= 2,		// Androgynous
			Age			= 3,		// New
			Shape		= 4,		// Round
			Color		= 5,		// Silver
			Pattern		= 6,		// Striped
			Origin		= 7,		// Elven
			Material	= 8,		// Leather
			Qualifier	= 9,		// Traveling
			
			Default	= Opinion
		}

		public static readonly int[] CountByOrder = { 3, 1, 2, 1, 1, 1, 1, 1, 1, 3 };

		public string value;
		public int order;
		public ICondition condition;
		public int priority = 0;

		public bool LoadFromXml(XmlNode xmlNode)
		{
			value = xmlNode.GetTextValue(null);
			if (string.IsNullOrEmpty(value))
				return false;
			value = value.Trim();

			string sOrder = xmlNode.GetAttribute("order", null);
			if (sOrder != null)
			{
				if (int.TryParse(sOrder, out order) == false)
				{
					Order eOrder = EnumHelper.FromString(sOrder, Order.Undefined);
					if (eOrder != Order.Undefined)
						order = EnumHelper.ToInt(eOrder);
					else
						order = EnumHelper.ToInt(Order.Default);
				}
			}
			else
			{ 
				order = 0; 
			}

			priority = xmlNode.GetAttributeInt("priority", 0);

			if (xmlNode.HasAttribute("rule"))
				condition = Rule.Parse(xmlNode.GetAttribute("rule"));
			return true;
		}

		public void SaveToXml(XmlNode xmlNode)
		{
			if (order != 0)
				xmlNode.AddAttribute("order", order);
			if (priority != 0)
				xmlNode.AddAttribute("priority", priority);
			if (condition != null)
				xmlNode.AddAttribute("rule", condition.ToString());

			xmlNode.AddTextValue(value);
		}

		public override int GetHashCode()
		{
			return Utility.MakeHashCode(
				"Adjective",
				value,
				condition, 
				order,
				priority);
		}
	}

	public class CharacterNoun
	{
		public string value;
		public ICondition condition;
		public int priority = 0;
		public Affix affix = Affix.Default;

		public enum Affix
		{
			None = 0,
			Prefix,
			Suffix,
			Default	= None,
		}

		public bool LoadFromXml(XmlNode xmlNode)
		{
			value = xmlNode.GetTextValue(null);
			if (string.IsNullOrEmpty(value))
				return false;
			value = value.Trim();

			priority = xmlNode.GetAttributeInt("priority", 0);
			affix = xmlNode.GetAttributeEnum("affix", Affix.Default);

			if (xmlNode.HasAttribute("rule"))
				condition = Rule.Parse(xmlNode.GetAttribute("rule"));
			return true;
		}

		public void SaveToXml(XmlNode xmlNode)
		{
			if (affix != Affix.None)
				xmlNode.AddAttribute("affix", EnumHelper.ToString(affix));
			if (priority != 0)
				xmlNode.AddAttribute("priority", priority);
			if (condition != null)
				xmlNode.AddAttribute("rule", condition.ToString());

			xmlNode.AddTextValue(value);
		}

		public override int GetHashCode()
		{
			return Utility.MakeHashCode(
				"Noun",
				value,
				condition, 
				priority);
		}
	}
}
