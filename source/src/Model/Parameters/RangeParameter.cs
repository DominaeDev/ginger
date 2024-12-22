using System;
using System.Globalization;
using System.Xml;

namespace Ginger
{
	public class RangeParameter : BaseParameter<decimal>
	{
		public enum Mode
		{
			Unknown = -1,
			Integer = 0,
			Decimal,
			Percent,
		}

		public Mode mode = Mode.Decimal;
		public decimal minValue = decimal.MaxValue;
		public decimal maxValue = decimal.MinValue;
		public string suffix = null;

		public RangeParameter() : base()
		{
		}

		public RangeParameter(Recipe recipe) : base(recipe)
		{
			mode = Mode.Integer;
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			if (base.LoadFromXml(xmlNode) == false)
				return false;

//			value = GetDefaultValue();
			suffix = xmlNode.GetValueElement("Suffix", null);

			decimal min = xmlNode.GetAttributeDecimal("min", decimal.MinValue);
			decimal max = xmlNode.GetAttributeDecimal("max", decimal.MinValue);

			if (xmlNode.HasAttribute("style"))
			{
				string style = xmlNode.GetAttribute("style");
				switch (style)
				{
				case "decimal":
				case "float":
				case "real":
					mode = Mode.Decimal;
					break;
				case "percent":
					mode = Mode.Percent;
					min = 0;
					max = 100;
					break;
				default:
					mode = Mode.Integer;
					break;
				}
			}

			if (min == decimal.MinValue || max == decimal.MinValue || min == max)
				return false;

			minValue = Math.Min(min, max);
			maxValue = Math.Max(min, max);
			return true;
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("Slider");
			base.SaveToXml(node);

			node.AddAttribute("min", minValue);
			node.AddAttribute("max", maxValue);

			switch (mode)
			{
			default:
				break;
			case Mode.Decimal:
				node.AddAttribute("style", "real");
				break;	
			case Mode.Percent:
				node.AddAttribute("style", "real");
				break;
			}

			if (string.IsNullOrEmpty(suffix) == false)
				node.AddValueElement("Suffix", suffix);

		}

		public override void OnApply(ParameterState state, Parameter.Scope scope)
		{
			string sValue;
			switch (mode)
			{
			case Mode.Percent:
				sValue = Convert.ToSingle(value * 0.01m).ToString(CultureInfo.InvariantCulture);
				break;
			default:
				sValue = Convert.ToSingle(value).ToString(CultureInfo.InvariantCulture);
				break;
			}

			state.SetValue(id, sValue, scope);

			if (isGlobal && scope == Parameter.Scope.Global)
				state.Reserve(id, uid, sValue);
		}

		public override object Clone()
		{
			var clone = CreateClone<RangeParameter>();
			clone.mode = this.mode;
			clone.minValue = this.minValue;
			clone.maxValue = this.maxValue;
			clone.suffix = this.suffix;
			return clone;
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "Slider".GetHashCode();
			hash ^= Utility.MakeHashCode(
				minValue,
				maxValue,
				mode,
				suffix);
			return hash;
		}

		public override decimal GetDefaultValue()
		{
			return Utility.StringToDecimal(defaultValue);
		}
	}
}
