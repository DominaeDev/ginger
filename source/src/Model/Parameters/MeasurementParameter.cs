using System;
using System.Xml;

namespace Ginger
{
	public class MeasurementParameter : BaseParameter<string>
	{
		public readonly decimal minValue = 0;
		public decimal maxValue = decimal.MinValue;
		public NumberParameter.Mode mode = NumberParameter.Mode.Length;
		public Measurement.UnitSystem unitSystem = Measurement.UnitSystem.Unknown;

		public bool hasValue { get { return value != default(string) && unitSystem != Measurement.UnitSystem.Unknown; } }

		public decimal magnitude = default(decimal);
		public string unit = null;

		public MeasurementParameter() : base()
		{
		}

		public MeasurementParameter(Recipe recipe) : base(recipe)
		{
		}

		public override bool LoadFromXml(XmlNode xmlNode)
		{
			defaultValue = xmlNode.GetAttribute("default", default(string));
			defaultValue = xmlNode.GetValueElement("Default", defaultValue).SingleLine();
			value = defaultValue;

			maxValue = xmlNode.GetAttributeDecimal("max", decimal.MaxValue);
			mode = xmlNode.GetAttributeEnum("style", NumberParameter.Mode.Length);

			Measurement.Parse(value, mode, out this.magnitude, out this.unit, out this.unitSystem);
			return base.LoadFromXml(xmlNode);
		}

		public override void SaveToXml(XmlNode xmlNode)
		{
			var node = xmlNode.AddElement("Number");
			base.SaveToXml(node);

			if (maxValue != decimal.MaxValue)
				node.AddAttribute("max", maxValue);
			
			node.AddAttribute("style", EnumHelper.ToString(mode).ToLowerInvariant());

			if (defaultValue != default(string))
				node.AddValueElement("Default", defaultValue);
		}

		public override void OnApplyToContext(Context context, Context localContext, ContextString.EvaluationConfig evalConfig)
		{
			if (value != default(string))
			{
				context.SetValue(id, value);
				localContext.SetValue(string.Concat(id.ToString(), ":local"), value);
				localContext.AddTag(string.Concat(id.ToString(), ":local"));
			}

			context.SetValue(string.Concat(id, ":value"), Convert.ToSingle(this.magnitude));
		}

		public override object Clone()
		{
			var clone = CreateClone<MeasurementParameter>();
			clone.mode = this.mode;
			clone.maxValue = this.maxValue;
			clone.unitSystem = this.unitSystem;
			clone.unit = this.unit;
			clone.magnitude = this.magnitude;
			return clone;
		}

		public void CopyValueTo(MeasurementParameter other)
		{
			base.CopyValuesTo(other);
			other.unitSystem = this.unitSystem;
			other.unit = this.unit;
			other.magnitude = this.magnitude;
		}

		public override void Set(string valueString)
		{
			if (string.IsNullOrEmpty(valueString) == false)
			{
				if (Measurement.Parse(valueString, mode, out this.magnitude, out this.unit, out this.unitSystem))
				{
					// Clamp
					var min = Math.Min(minValue, maxValue);
					var max = Math.Max(minValue, maxValue);
					magnitude = Math.Min(Math.Max(magnitude, min), max);

					value = Measurement.ToString(magnitude, this.unit);
				}
			}
			else
			{
				value = default(string);
				magnitude = default(decimal);
				unit = null;
				unitSystem = Measurement.UnitSystem.Unknown;
			}
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash ^= "Number".GetHashCode();
			hash ^= Utility.MakeHashCode(
				maxValue,
				mode);
			return hash;
		}
	}
}
