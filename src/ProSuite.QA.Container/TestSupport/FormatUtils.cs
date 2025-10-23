using System;
using System.Data;
using System.Globalization;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Core.ParameterTypes;

namespace ProSuite.QA.Container.TestSupport
{
	public static class FormatUtils
	{
		[ThreadStatic] private static IUnitConverter _unitConverter;

		[ThreadStatic] private static DataView _compareView;

		[NotNull]
		private static IUnitConverter UnitConverter
			=> _unitConverter ?? (_unitConverter = new UnitConverterClass());

		[NotNull]
		private static DataView CompareView
		{
			get
			{
				if (_compareView == null)
				{
					var tbl = new DataTable();

					tbl.Columns.Add("dummy");
					tbl.Rows.Add("dummy");
					tbl.AcceptChanges();

					_compareView = new DataView(tbl);
				}

				return _compareView;
			}
		}

		[NotNull]
		public static string FormatComparison(string initialFormat, double v0, double v1,
		                                      string compare, string expressionString)
		{
			string format = CompareFormat(v0, compare, v1, initialFormat);

			string result = string.Format(expressionString,
			                              GetValueString(v0, format),
			                              compare,
			                              GetValueString(v1, format));

			return result;
		}

		[NotNull]
		internal static string FormatAngle(string format, double radians,
		                                   AngleUnit angleUnit)
		{
			switch (angleUnit)
			{
				case AngleUnit.Radiant:
					string radiansFormat = "{0:" + format + "}";
					return string.Format(radiansFormat, radians);

				case AngleUnit.Degree:
					string degreeFormat = "{0:" + format + "}°";
					return string.Format(degreeFormat, MathUtils.ToDegrees(radians));

				default:
					throw new ArgumentException("Unhandled AngleUnit " + angleUnit);
			}
		}

		public static double AngleInUnits2Radians(double angleInUnits, AngleUnit angleUnit)
		{
			switch (angleUnit)
			{
				case AngleUnit.Radiant:
					return angleInUnits;

				case AngleUnit.Degree:
					return MathUtils.ToRadians(angleInUnits);

				default:
					throw new ArgumentException("Unhandled AngleUnit " + angleUnit);
			}
		}

		public static double Radians2AngleInUnits(double radians, AngleUnit angleUnit)
		{
			switch (angleUnit)
			{
				case AngleUnit.Radiant:
					return radians;

				case AngleUnit.Degree:
					return MathUtils.ToDegrees(radians);

				default:
					throw new ArgumentException("Unhandled AngleUnit " + angleUnit);
			}
		}

		[NotNull]
		public static string CompareFormat(double v0,
		                                   [NotNull] string compare,
		                                   double v1,
		                                   [NotNull] string initFormat)
		{
			int numberOfDigits = GetNumberOfDigits(initFormat);

			bool resultsInVisibleDifference;
			int initialNumberOfDigits = numberOfDigits;

			var formatType = "N";

			// TODO scientificLimit lowered; calculation of number of digits seems to be incorrect for E format
			const double scientificLimit = 0.000001;
			if ((Math.Abs(v0) < double.Epsilon ||
			     Math.Log10(Math.Abs(v0)) < numberOfDigits &&
			     Math.Abs(v0) < scientificLimit) &&
			    (Math.Abs(v1) < double.Epsilon ||
			     Math.Log10(Math.Abs(v1)) < numberOfDigits &&
			     Math.Abs(v1) < scientificLimit))
			{
				formatType = "E";
			}
			else
			{
				double max = Math.Max(Math.Abs(v0), Math.Abs(v1));
				if (max > 0)
				{
					numberOfDigits = Math.Max(numberOfDigits, (int) Math.Ceiling(-Math.Log10(max)));
				}
			}

			const int maxNumberOfDigits = 22;
			string format;
			do
			{
				// proposed fix for https://issuetracker02.eggits.net/browse/TOP-3936 
				// (replace "N" with "F" for the call to GetResultsInVisibleDifference(); or also for the return value?)
				format = string.Format("{0}{1}", formatType == "N"
					                                 ? "F"
					                                 : formatType, numberOfDigits);

				resultsInVisibleDifference = GetResultsInVisibleDifference(
					format, v0, v1, compare, CultureInfo.InvariantCulture);

				numberOfDigits++;
			} while ((! resultsInVisibleDifference ||
			          Math.Abs(v0 - v1).ToString(format) == 0.ToString(format))
			         && numberOfDigits < maxNumberOfDigits);

			return initialNumberOfDigits == numberOfDigits - 1
				       ? initFormat
				       : formatType + (numberOfDigits - 1);
		}

		private static int GetNumberOfDigits(string initFormat)
		{
			string calib = (1.0).ToString(initFormat, CultureInfo.InvariantCulture);
			int numberOfDigits;

			const char decimalSeparator = '.';

			if (calib.IndexOf(decimalSeparator) < 0)
			{
				numberOfDigits = 0;
			}
			else
			{
				int index = calib.IndexOf(decimalSeparator) + 1;
				numberOfDigits = 0;

				while (index < calib.Length && calib[index] == '0')
				{
					numberOfDigits++;
					index++;
				}
			}

			return numberOfDigits;
		}

		[NotNull]
		internal static string GetValueString(double value, string format)
		{
			int numberOfDigits = GetNumberOfDigits(format);
			var addDigits = 0;
			string valueString;
			string preFormat = format;
			do
			{
				format = preFormat;
				string fullFormat = "{0:" + format + "}";
				valueString = string.Format(fullFormat, value);

				addDigits++;
				preFormat = "N" + (numberOfDigits + addDigits);
			} while (Math.Abs(value) > double.Epsilon &&
			         valueString == 0.ToString(format) &&
			         addDigits < 4);

			if (Math.Abs(value) > double.Epsilon &&
			    valueString == 0.ToString(format))
			{
				valueString = value.ToString("#.0E0");
			}

			return valueString;
		}

		internal static double GetLengthUnitFactor(
			[NotNull] ISpatialReference spatialReference,
			esriUnits esriUnits, double referenceScale)
		{
			if (spatialReference is IProjectedCoordinateSystem pc)
			{
				double f = UnitConverter.ConvertUnits(1, esriUnits.esriMeters, esriUnits);

				f *= pc.CoordinateUnit.MetersPerUnit;
				f *= referenceScale;

				return f;
			}

			return 1;
		}

		internal static string GetUnitString(esriUnits lengthUnit)
		{
			switch (lengthUnit)
			{
				case esriUnits.esriKilometers:
					return "km";

				case esriUnits.esriMeters:
					return "m";

				case esriUnits.esriDecimeters:
					return "dm";

				case esriUnits.esriCentimeters:
					return "cm";

				case esriUnits.esriMillimeters:
					return "mm";

				case esriUnits.esriUnknownUnits:
					return null;

				default:
					return GetEsriUnitsAsString(lengthUnit);
			}
		}

		[NotNull]
		private static string GetEsriUnitsAsString(esriUnits esriUnits)
		{
			return UnitConverter.EsriUnitsAsString(
				esriUnits, esriCaseAppearance.esriCaseAppearanceUnchanged, false);
		}

		private static bool GetResultsInVisibleDifference(
			[NotNull] string format, double v0,
			[NotNull] IFormattable v1,
			[NotNull] string comparisonOperator,
			[NotNull] IFormatProvider culture)
		{
			if (double.IsNaN(v0) || double.IsInfinity(v0))
			{
				return true;
			}

			if (v1 is double d1)
			{
				if (double.IsNaN(d1) || double.IsInfinity(d1))
				{
					return true;
				}
			}

			string s0 = v0.ToString(format, culture);
			string s1 = v1.ToString(format, culture);

			string expr = $"{s0} {comparisonOperator} {s1}";

			// TODO this fails if v0 or v1 is >= 1000 and format = "Nx". RowFilter does not handle thousand separators ("1,000" in the InvariantCulture). 
			// https://issuetracker02.eggits.net/browse/TOP-3936
			CompareView.RowFilter = expr;

			return CompareView.Count > 0;
		}
	}
}
