using System;
using System.Globalization;
using System.Xml;

namespace Ginger
{
	public class NumberParameter : BaseParameter<decimal>
	{
		public enum Mode
		{
			Unknown = -1,
			Integer = 0,
			Decimal,

			// Measurement
			Length,
			Weight,
			Volume,
		}

		public Mode mode = Mode.Decimal;
		public decimal minValue = decimal.MaxValue;
		public decimal maxValue = decimal.MinValue;
		public string suffix = null;

		public NumberParameter() : base()
		{
		}

		public NumberParameter(Recipe recipe) : base(recipe)
		{
			mode = Mode.Integer;
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			if (base.LoadFromXml(xmlNode) == false)
				return false;

//			value = GetDefaultValue();

			suffix = xmlNode.GetValueElement("Suffix", null);
			minValue = xmlNode.GetAttributeDecimal("min", decimal.MinValue);
			maxValue = xmlNode.GetAttributeDecimal("max", decimal.MaxValue);

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
				default:
					mode = Mode.Integer;
					break;
				}
			}

			return true;
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("Number");
			base.SaveToXml(node);

			if (minValue != decimal.MinValue)
				node.AddAttribute("min", minValue);
			if (maxValue != decimal.MaxValue)
				node.AddAttribute("max", maxValue);
			if (mode == Mode.Decimal)
				node.AddAttribute("style", "real");
			if (string.IsNullOrEmpty(suffix) == false)
				node.AddValueElement("Suffix", suffix);
		}

		public override void OnApply(ParameterState state, Parameter.Scope scope)
		{
			if (value == default(decimal))
				return;

			string sValue = Convert.ToSingle(value).ToString(CultureInfo.InvariantCulture);
			state.SetValue(id, sValue, scope);
			
			if (isGlobal && scope == Parameter.Scope.Global)
				state.Reserve(id, sValue);
		}

		public override object Clone()
		{
			var clone = CreateClone<NumberParameter>();
			clone.mode = this.mode;
			clone.minValue = this.minValue;
			clone.maxValue = this.maxValue;
			clone.suffix = this.suffix;
			return clone;
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "Number".GetHashCode();
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
