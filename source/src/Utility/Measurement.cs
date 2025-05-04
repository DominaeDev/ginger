using System;
using System.Globalization;

namespace Ginger
{
	using Type = NumberParameter.Mode;
	public static class Measurement
	{
		public enum UnitSystem
		{
			Unknown = -1,
			Metric = 0,
			Imperial = 1,
			Default = Metric,
		}

		public static bool Parse(string valueString, Type mode, out decimal magnitude, out string unit, out UnitSystem unitSystem)
		{
			magnitude = 0.0m;
			unitSystem = UnitSystem.Unknown;
			unit = "";

			if (string.IsNullOrEmpty(valueString))
				return false;

			valueString = valueString.TrimStart().ToLowerInvariant();
			bool bNegative = false;
			if (valueString.BeginsWith('-'))
			{
				bNegative = true;
				valueString = valueString.Substring(1).TrimStart();
			}

			if (valueString.Length == 0)
				return false;

			var arrValue = valueString.ToCharArray();

			// Parse first number
			int pos_end_of_first_number = Array.FindIndex(arrValue, ch => !(char.IsNumber(ch) || ch == '.'));
			if (pos_end_of_first_number == -1) // No unit
				pos_end_of_first_number = valueString.Length;

			string strFirstNumber = valueString.Substring(0, pos_end_of_first_number);
			decimal firstNumber;
			if (decimal.TryParse(strFirstNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out firstNumber) == false)
				return false;

			string firstUnit;
			string secondUnit;

			// Parse second number

			int pos_second_number = Array.FindIndex(arrValue, pos_end_of_first_number, ch => char.IsNumber(ch));
			decimal? secondNumber = null;
			if (pos_second_number != -1) // Second number (feet + inches)
			{
				int pos_end_of_second_number = Array.FindIndex(arrValue, pos_second_number, ch => !(char.IsNumber(ch) || ch == '.'));
				if (pos_end_of_second_number == -1) // No unit
					pos_end_of_second_number = valueString.Length;

				string strSecondNumber = valueString.Substring(pos_second_number, pos_end_of_second_number - pos_second_number);
				decimal n;
				if (decimal.TryParse(strSecondNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out n))
					secondNumber = n;

				firstUnit = valueString.Substring(pos_end_of_first_number, pos_second_number - pos_end_of_first_number).Trim();
				secondUnit = valueString.Substring(pos_end_of_second_number).Trim();
			}
			else
			{
				firstUnit = valueString.Substring(pos_end_of_first_number).Trim();
				secondUnit = null;
			}

			// Remove trailing junk
			if (string.IsNullOrEmpty(firstUnit) == false)
			{
				int pos_end_of_unit = Array.FindIndex(firstUnit.ToCharArray(), 0, ch => !(char.IsLetter(ch) || ch == '\'' || ch == '"'));
				if (pos_end_of_unit != -1)
					firstUnit = firstUnit.Substring(0, pos_end_of_unit);
			}
			// Remove trailing junk
			if (string.IsNullOrEmpty(secondUnit) == false)
			{
				int pos_end_of_unit = Array.FindIndex(secondUnit.ToCharArray(), 0, ch => !(char.IsLetter(ch) || ch == '\'' || ch == '"'));
				if (pos_end_of_unit != -1)
					secondUnit = secondUnit.Substring(0, pos_end_of_unit);
			}

			// Normalize units
			if (mode == Type.Length || mode == Type.Unknown)
			{
				switch (firstUnit)
				{
				case "m":
				case "meter":
				case "meters":
					firstUnit = "m";
					unitSystem = UnitSystem.Metric;
					break;
				case "dm":
				case "deci":
				case "decimeter":
				case "decimeters":
					firstUnit = "dm";
					unitSystem = UnitSystem.Metric;
					break;
				case "cm":
				case "centi":
				case "centimeter":
				case "centimeters":
					firstUnit = "cm";
					unitSystem = UnitSystem.Metric;
					break;
				case "mm":
				case "milli":
				case "millimeter":
				case "millimeters":
					firstUnit = "mm";
					unitSystem = UnitSystem.Metric;
					break;
				case "i":
				case "in":
				case "\"":
				case "inch":
				case "inches":
					firstUnit = "in";
					unitSystem = UnitSystem.Imperial;
					break;
				case "f":
				case "ft":
				case "\'":
				case "feet":
				case "foot":
					firstUnit = "ft";
					unitSystem = UnitSystem.Imperial;
					break;
				case "yd":
				case "yard":
				case "yards":
					firstUnit = "yd";
					unitSystem = UnitSystem.Imperial;
					break;
				default:
					if (mode == Type.Length)
					{
						firstUnit = "cm";
						unitSystem = UnitSystem.Default;
					}
					break;
				}

				// Trailing inches
				if (unitSystem == UnitSystem.Imperial)
				{
					switch (secondUnit)
					{
					case "i":
					case "in":
					case "\"":
					case "inch":
					case "inches":
						secondUnit = "in";
						break;
					case "":
					case null:
						if (firstUnit == "ft")
							secondUnit = "in";
						else
							secondUnit = null;
						break;
					default:
						secondUnit = null;
						break;
					}
				}
				else
				{
					secondUnit = null;
				}
			}
			else if (mode == Type.Weight || mode == Type.Unknown)
			{
				switch (firstUnit)
				{
				case "kg":
				case "kilo":
				case "kilogram":
				case "kilograms":
					firstUnit = "kg";
					unitSystem = UnitSystem.Metric;
					break;
				case "hg":
				case "hecto":
				case "hectogram":
				case "hectograms":
					firstUnit = "hg";
					unitSystem = UnitSystem.Metric;
					break;
				case "g":
				case "gramme":
				case "grammes":
				case "gram":
				case "grams":
					firstUnit = "g";
					unitSystem = UnitSystem.Metric;
					break;
				case "ton":
				case "tons":
				case "tonne":
				case "tonnes":
					firstUnit = "ton";
					unitSystem = UnitSystem.Metric;
					break;

				case "oz":
				case "ounce":
				case "ounces":
					firstUnit = "oz";
					unitSystem = UnitSystem.Imperial;
					break;
				case "lb":
				case "lbs":
				case "pound":
				case "pounds":
					firstUnit = "lb";
					unitSystem = UnitSystem.Imperial;
					break;
				default:
					if (mode == Type.Weight)
					{
						firstUnit = "kg";
						unitSystem = UnitSystem.Default;
					}
					break;
				}
				secondUnit = null;
			}
			else if (mode == Type.Volume || mode == Type.Unknown)
			{
				switch (firstUnit)
				{
				case "ml":
				case "milliliter":
				case "milliliters":
					firstUnit = "ml";
					unitSystem = UnitSystem.Metric;
					break;
				case "cl":
				case "centiliter":
				case "centiliters":
					firstUnit = "cl";
					unitSystem = UnitSystem.Metric;
					break;
				case "dl":
				case "deciliter":
				case "deciliters":
					firstUnit = "dl";
					unitSystem = UnitSystem.Metric;
					break;
				case "l":
				case "lt":
				case "liter":
				case "liters":
					firstUnit = "l";
					unitSystem = UnitSystem.Metric;
					break;
				case "cc":
				case "ccs":
					firstUnit = "cc";
					unitSystem = UnitSystem.Metric;
					break;
				case "floz":
				case "fl":
				case "fl.oz":
					firstUnit = "floz";
					unitSystem = UnitSystem.Imperial;
					break;
				case "gal":
				case "gallon":
				case "gallons":
					firstUnit = "gal";
					unitSystem = UnitSystem.Imperial;
					break;
				default:
					if (mode == Type.Volume)
					{
						firstUnit = "ml";
						unitSystem = UnitSystem.Default;
					}
					break;
				}
				secondUnit = null;
			}

			if (bNegative)
			{
				firstNumber *= -1m;
				if (secondNumber.HasValue)
					secondNumber *= -1m;
			}

			if (firstUnit == null)
			{
				magnitude = firstNumber;
				switch (mode)
				{
				case Type.Length:
					unit = "cm";
					break;
				case Type.Weight:
					unit = "kg";
					break;
				case Type.Volume:
					unit = "ml";
					break;
				}
				unitSystem = UnitSystem.Default;
				return true;
			}

			unit = firstUnit;

			switch (firstUnit)
			{
			// Length (mm)
			case "m":
				magnitude = firstNumber * 1000m;
				return true;
			case "dm":
				magnitude = firstNumber * 100m;
				return true;
			case "cm":
				magnitude = firstNumber * 10m;
				return true;
			case "mm":
				magnitude = firstNumber;
				return true;
			case "ft":
				magnitude = firstNumber * 304.8m;
				if (secondUnit == "in" && secondNumber.HasValue)
				{
					magnitude += 25.4m * secondNumber.Value;
					return true;
				}
				return true;
			case "in":
				magnitude = firstNumber * 25.4m;
				return true;
			case "yd":
				magnitude = firstNumber * 914.4m;
				return true;
			// Weight (g)
			case "g":
				magnitude = firstNumber;
				return true;
			case "hg":
				magnitude = firstNumber * 100m;
				return true;
			case "kg":
				magnitude = firstNumber * 1000m;
				return true;
			case "ton":
				magnitude = firstNumber * 1000000m;
				return true;
			case "oz":
				magnitude = firstNumber * 28.35952m;
				return true;
			case "lb":
			case "lbs":
				magnitude = firstNumber * 453.59237m;
				return true;
			// Volume (ml)
			case "l":
				magnitude = firstNumber * 1000m;
				return true;
			case "dl":
				magnitude = firstNumber * 100m;
				return true;
			case "cl":
				magnitude = firstNumber * 10m;
				return true;
			case "cc":
			case "ml":
				magnitude = firstNumber;
				return true;
			case "floz":
				magnitude = firstNumber * 29.5735296m;
				return true;
			case "gal":
				magnitude = firstNumber * 3785.41178m;
				return true;
			default:
				return false;
			}
		}

		public static string ToString(decimal magnitude, string unit)
		{
			switch (unit)
			{
			// Length
			case "m":
				return string.Format(CultureInfo.InvariantCulture, "{0:0.##} m", magnitude / 1000m);
			case "dm":
				return string.Format(CultureInfo.InvariantCulture, "{0:0} dm", magnitude / 100m);
			case "cm":
				return string.Format(CultureInfo.InvariantCulture, "{0:0} cm", magnitude / 10m);
			case "mm":
				return string.Format(CultureInfo.InvariantCulture, "{0:0} mm", magnitude);
			case "in":
				return string.Format(CultureInfo.InvariantCulture, "{0:0.#} in", magnitude / 25.4m);
			case "ft":
			{
				bool negative = magnitude < 0m;
				decimal magFeet = Math.Abs(magnitude / 304.8m);
				int feet = Convert.ToInt32(Math.Floor(magFeet));
				int inch = Convert.ToInt32(Math.Round((magFeet - feet) * 12.0m));
				if (inch == 12)
				{
					inch = 0;
					feet += 1;
				}
				if (feet == 0 && inch > 0)
					return string.Format(CultureInfo.InvariantCulture, "{1}{0:0.#} in", inch, negative ? "-" : "");
				else if (inch > 0)
					return string.Format(CultureInfo.InvariantCulture, "{2}{0}ft {1}in", feet, inch, negative ? "-" : "");
				else
					return string.Format(CultureInfo.InvariantCulture, "{1}{0} ft", feet, negative ? "-" : "");
			}
			case "ftin":
			{
				bool negative = magnitude < 0m;
				decimal magFeet = Math.Abs(magnitude / 304.8m);
				int feet = Convert.ToInt32(Math.Floor(magFeet));
				int inch = Convert.ToInt32(Math.Round((magFeet - feet) * 12.0m));
				if (inch == 12)
				{
					inch = 0;
					feet += 1;
				}
				if (feet == 0 && inch > 0)
					return string.Format(CultureInfo.InvariantCulture, "{1}{0:0.#}\"", inch, negative ? "-" : "");
				else if (inch > 0)
					return string.Format(CultureInfo.InvariantCulture, "{2}{0}'{1}\"", feet, inch, negative ? "-" : "");
				else
					return string.Format(CultureInfo.InvariantCulture, "{1}{0}'", feet, negative ? "-" : "");
			}
			case "yd":
				return string.Format(CultureInfo.InvariantCulture, "{0:0.#} yd", magnitude / 914.4m);
			// Weight
			case "ton":
				return string.Format(CultureInfo.InvariantCulture, "{0:0.#} ton", magnitude / 1000000m);
			case "kg":
				return string.Format(CultureInfo.InvariantCulture, "{0:0.#} kg", magnitude / 1000m);
			case "hg":
				return string.Format(CultureInfo.InvariantCulture, "{0:0} hg", magnitude / 100m);
			case "g":
				return string.Format(CultureInfo.InvariantCulture, "{0:0} g", magnitude);
			case "oz":
				return string.Format(CultureInfo.InvariantCulture, "{0:0} oz", magnitude / 28.35952m);
			case "lb":
			case "lbs":
				return string.Format(CultureInfo.InvariantCulture, "{0:0.#} lb", magnitude / 453.59237m);

			// Volume
			case "l":
				return string.Format(CultureInfo.InvariantCulture, "{0:0.#} l", magnitude / 1000m);
			case "dl":
				return string.Format(CultureInfo.InvariantCulture, "{0:0} dl", magnitude / 100m);
			case "cl":
				return string.Format(CultureInfo.InvariantCulture, "{0:0} cl", magnitude / 10m);
			case "ml":
				return string.Format(CultureInfo.InvariantCulture, "{0:0} ml", magnitude);
			case "floz":
				return string.Format(CultureInfo.InvariantCulture, "{0:0} fl oz", magnitude / 29.5735296m);
			case "gal":
				return string.Format(CultureInfo.InvariantCulture, "{0:0.#} gal", magnitude / 3785.41178m);
			default:
				return magnitude.ToString("0.##", CultureInfo.InvariantCulture);
			}
		}

		public static string ConvertToImperial(string value)
		{
			decimal magnitude;
			string unit;
			UnitSystem unitSystem;
			if (Parse(value, Type.Unknown, out magnitude, out unit, out unitSystem))
			{
				if (unitSystem == UnitSystem.Imperial)
					return value;

				switch (unit)
				{
				default:
				case "m":
				case "dm":
				case "cm":
				case "mm":
					return ToString(magnitude, "ft");

				// Weight
				case "ton":
				case "kg":
				case "hg":
				case "g":
					return ToString(magnitude, "lb");

				// Volume
				case "l":
				case "dl":
				case "cl":
				case "ml":
					return ToString(magnitude, "floz");
				}
			}
			return string.Empty;
		}

		public static string ConvertToMetric(string value)
		{
			decimal magnitude;
			string unit;
			UnitSystem unitSystem;
			if (Parse(value, Type.Unknown, out magnitude, out unit, out unitSystem))
			{
				if (unitSystem == UnitSystem.Metric)
					return value;

				switch (unit)
				{
				// Length
				default:
				case "ft":
				case "in":
					return ToString(magnitude, "cm");
				case "yd":
					return ToString(magnitude, "m");
				// Weight
				case "oz":
				case "lb":
				case "lbs":
					return ToString(magnitude, "kg");

				// Volume
				case "floz":
				case "gal":
					return ToString(magnitude, "ml");
				}
			}
			return string.Empty;
		}
	}
}
