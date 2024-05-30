using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Ginger
{
	public static class IniConfigLoader
	{
		public static bool LoadGlobalConfig(string filePath, Type configType)
		{
			var parser = new FileIniDataParser();
			parser.Parser.Configuration.CommentString = "#";
			parser.Parser.Configuration.SkipInvalidLines = true;
			parser.Parser.Configuration.OverrideDuplicateKeys = true;
			parser.Parser.Configuration.AllowKeysWithoutSection = true;			

			try
			{
				var iniData = parser.ReadFile(filePath, Encoding.UTF8);
				if (iniData == null)
					return false;

				SetIniConfig(iniData, configType);				
			}
			catch
			{
				return false;
			}
			return true;
		}

		private static void SetIniConfig(IniData iniData, Type configType)
		{
			if (iniData == null)
				return;

			Func<string, int> parseInt = (value) =>
			{
				int intValue;
				if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
					return intValue;
				return default(int);
			};
			Func<string, float> parseFloat = (value) =>
			{
				float floatValue;
				if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out floatValue))
					return floatValue;
				return default(float);
			};
			Func<string, double> parseDouble = (value) =>
			{
				double doubleValue;
				if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out doubleValue))
					return doubleValue;
				return default(double);
			};

			IniConfigSectionAttribute sectionAttribute = (IniConfigSectionAttribute)System.Attribute.GetCustomAttribute(configType, typeof(IniConfigSectionAttribute));
			if (sectionAttribute != null)
			{
				var sectionName = sectionAttribute.name;
				if (string.IsNullOrEmpty(sectionName))
					sectionName = configType.Name;

				if (iniData.Sections.ContainsSection(sectionName))
				{
					var iniSection = iniData.Sections[sectionName];

					var fields = configType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
					for (int iField = 0; iField < fields.Length; ++iField)
					{
						IniConfigValueAttribute valueAttribute = (IniConfigValueAttribute)System.Attribute.GetCustomAttribute(fields[iField], typeof(IniConfigValueAttribute));
						if (valueAttribute == null)
							continue;

						var valueName = valueAttribute.name;
						if (string.IsNullOrEmpty(valueName))
							valueName = fields[iField].Name;

						bool bEnforceDataLength = valueAttribute.bEnforceDataLength;

						if (iniSection.ContainsKey(valueName) == false)
							continue;

						// Set value
						var fieldType = fields[iField].FieldType;
						var strValue = iniSection[valueName].Trim();

						try
						{

							if (fieldType == typeof(string))        // string
							{
								fields[iField].SetValue(null, strValue);
							}
							else if (fieldType == typeof(int))      // int
							{
								int intValue;
								if (int.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
									fields[iField].SetValue(null, intValue);
							}
							else if (fieldType == typeof(float))    // float
							{
								float floatValue;
								if (float.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out floatValue))
									fields[iField].SetValue(null, floatValue);
							}
							else if (fieldType == typeof(double))   // double
							{
								double doubleValue;
								if (double.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out doubleValue))
									fields[iField].SetValue(null, doubleValue);
							}
							else if (fieldType == typeof(bool))     // bool
							{
								if (string.Compare(strValue, "yes", true) == 0
									|| string.Compare(strValue, "true", true) == 0)
								{
									fields[iField].SetValue(null, true);
								}
								else if (string.Compare(strValue, "no", true) == 0
									|| string.Compare(strValue, "false", true) == 0)
								{
									fields[iField].SetValue(null, false);
								}
								else
								{
									int intValue;
									if (int.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
										fields[iField].SetValue(null, intValue != 0);
								}
							}
							else if (fieldType == typeof(int[]))    // int[]
							{
								ReadArray(fields[iField], sectionAttribute, valueAttribute, strValue, bEnforceDataLength, parseInt);
							}
							else if (fieldType == typeof(float[]))  // float[]
							{
								ReadArray(fields[iField], sectionAttribute, valueAttribute, strValue, bEnforceDataLength, parseFloat);
							}
							else if (fieldType == typeof(double[])) // double[]
							{
								ReadArray(fields[iField], sectionAttribute, valueAttribute, strValue, bEnforceDataLength, parseDouble);
							}
							else if (fieldType == typeof(int[][]))    // int[][]
							{
								ReadArrayMulti(fields[iField], sectionAttribute, valueAttribute, strValue, bEnforceDataLength, parseInt);
							}
							else if (fieldType == typeof(float[][]))    // float[][]
							{
								ReadArrayMulti(fields[iField], sectionAttribute, valueAttribute, strValue, bEnforceDataLength, parseFloat);
							}
							else if (fieldType == typeof(double[][]))    // double[][]
							{
								ReadArrayMulti(fields[iField], sectionAttribute, valueAttribute, strValue, bEnforceDataLength, parseDouble);
							}
							else if (fieldType == typeof(int[,]))    // int[,]
							{
								ReadArray2D(fields[iField], sectionAttribute, valueAttribute, strValue, bEnforceDataLength, parseInt);
							}
							else if (fieldType == typeof(float[,]))    // float[,]
							{
								ReadArray2D(fields[iField], sectionAttribute, valueAttribute, strValue, bEnforceDataLength, parseFloat);
							}
							else if (fieldType == typeof(double[,]))    // double[,]
							{
								ReadArray2D(fields[iField], sectionAttribute, valueAttribute, strValue, bEnforceDataLength, parseDouble);
							}
						}
						catch (FieldAccessException)
						{
						}
						catch (System.Reflection.TargetException)
						{
						}
						catch (ArgumentException)
						{
						}
					}
				}
			}

			// Nested types
			var nestedTypes = configType.GetNestedTypes();
			for (int iType = 0; iType < nestedTypes.Length; ++iType)
				SetIniConfig(iniData, nestedTypes[iType]);
		}

		private static void ReadArray<T>(System.Reflection.FieldInfo field, IniConfigSectionAttribute sectionAttribute, IniConfigValueAttribute valueAttribute, string strValue, bool bEnforceDataLength, Func<string, T> fnParse)
		{
			var strValues = Utility.ListFromCommaSeparatedString(strValue);
			int expectedLength = 0;
			if (bEnforceDataLength && ValidateDataLength(field, strValues.Count, out expectedLength) == false)
			{
				return;
			}

			try
			{
				var array1D = strValues.Select(s => fnParse(s)).ToArray();
				field.SetValue(null, array1D);
			}
			catch
			{
			}
		}

		private static void ReadArrayMulti<T>(System.Reflection.FieldInfo field, IniConfigSectionAttribute sectionAttribute, IniConfigValueAttribute valueAttribute, string strValue, bool bEnforceDataLength, Func<string, T> fnParse)
		{
			var rows = Utility.ListFromDelimitedString(strValue, ';', true);
			var array2D = rows
				.Select(row => {
					var strValues = Utility.ListFromDelimitedString(row, ',', true);
					return strValues.Select(s => fnParse.Invoke(s)).ToArray();
				})
				.ToArray();

			if (array2D.Length == 0)
			{
				return;
			}

			int length = array2D[0].Length;
			int expectedRows = 0;
			int expectedCols = 0;
			if (bEnforceDataLength && ValidateDataLength<int>(field, array2D.Length, length, out expectedRows, out expectedCols) == false)
			{
				return;
			}

			try
			{
				field.SetValue(null, array2D);
			}
			catch
			{
			}
		}

		private static void ReadArray2D<T>(System.Reflection.FieldInfo field, IniConfigSectionAttribute sectionAttribute, IniConfigValueAttribute valueAttribute, string strValue, bool bEnforceDataLength, Func<string, T> fnParse)
		{
			var sRows = Utility.ListFromDelimitedString(strValue, ';', true);

			List<T[]> rows = new List<T[]>();
			foreach (var row in sRows)
			{
				List<T> values = new List<T>();
				var strValues = Utility.ListFromDelimitedString(row, ',', true);
				foreach (var s in strValues)
				{
					try
					{
						values.Add(fnParse.Invoke(s));
					}
					catch
					{
						values.Add(default(T));
					}
				}
				rows.Add(values.ToArray());
			}
			if (rows.Count == 0)
			{
				return;
			}

			int nRows = rows.Count;
			int nCols = rows[0].Length;

			int expectedRows = 0;
			int expectedCols = 0;
			if (bEnforceDataLength && ValidateDataLength<T>(field, nRows, nCols, out expectedRows, out expectedCols) == false)
			{
				return;
			}

			T[,] array2D = new T[nRows, nCols];
			for (int iRow = 0; iRow < nRows; ++iRow)
			{
				if (rows[iRow].Length != nCols)
				{
					return;
				}
				for (int iCol = 0; iCol < nCols; ++iCol)
					array2D[iRow, iCol] = rows[iRow][iCol];
			}
			try
			{
				field.SetValue(null, array2D);
			}
			catch
			{
			}
		}

		private static bool ValidateDataLength(System.Reflection.FieldInfo field, int length, out int expectedLength)
		{
			var array = field.GetValue(null) as Array;
			expectedLength = array.Length;
			return array != null && array.Length == length;
		}

		private static bool ValidateDataLength<T>(System.Reflection.FieldInfo field, int rows, int cols, out int expectedRows, out int expectedCols)
		{
			var array = field.GetValue(null) as T[][];
			if (array != null)
			{
				expectedRows = array.Length;
				expectedCols = array[0].Length;
				return expectedRows == rows && expectedCols == cols;
			}

			var array2D = field.GetValue(null) as T[,];
			if (array2D != null)
			{
				expectedRows = array2D.GetLength(0);
				expectedCols = array2D.GetLength(1);
				return expectedRows == rows && expectedCols == cols;
			}
			expectedRows = default(int);
			expectedCols = default(int);
			return false;
		}
	}
}
