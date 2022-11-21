using System;
using System.Globalization;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Processing.Domain;
using ProSuite.Processing.Evaluation;
using Polygon = ArcGIS.Core.Geometry.Polygon;

namespace ProSuite.Processing.AGP.Core.Utils
{
	public static class EnvironmentUtils
	{
		// Functions of the same name must differ by their arity!
		// This is different from C# method overloading!

		public static StandardEnvironment RegisterGeometryFunctions(
			[NotNull] this StandardEnvironment env)
		{
			env.Register<object, object>("SHAPEAREA", Area);
			env.Register<object, object>("SHAPELENGTH", Length);
			// Nice to have: Area/0 and Length/0 that refer to current row's Shape field (whatever it's named)

			return env;
		}

		public static StandardEnvironment RegisterColorFunctions(
			[NotNull] this StandardEnvironment env)
		{
			env.Register<double, double, double, CIMColor>("RGBCOLOR", RgbColor);
			env.Register<object, CIMColor>("RGBCOLOR", RgbColor);

			env.Register<double, double, double, double, CIMColor>("CMYKCOLOR", CmykColor);
			env.Register<object, CIMColor>("CMYKCOLOR", CmykColor);

			env.Register<object, CIMColor>("GRAYCOLOR", GrayColor);
			env.Register<object, CIMColor>("GREYCOLOR", GrayColor);

			env.Register<double, double, double, CIMColor>("HSVCOLOR", HsvColor);
			env.Register<object, CIMColor>("HSVCOLOR", HsvColor);

			env.Register<double, double, double, CIMColor>("HLSCOLOR", HlsColor);
			env.Register<object, CIMColor>("HLSCOLOR", HlsColor);

			env.Register<string, CIMColor>("MAKECOLOR", MakeColor);
			env.Register<CIMColor, string>("FORMATCOLOR", FormatColor); // rename?

			return env;
		}

		public static StandardEnvironment RegisterConversionFunctions(
			[NotNull] this StandardEnvironment env,
			[CanBeNull] IMapContext mapContext)
		{
			var instance = new ConversionFunctions {MapContext = mapContext};

			env.Register<object, object>("mm2pt", mm2pt);
			env.Register<object, object>("pt2mm", pt2mm);
			env.Register<object, object>("mm2mu", instance.mm2mu, instance);
			env.Register<object, object>("mu2mm", instance.mu2mm, instance);
			env.Register<object, object>("pt2mu", instance.pt2mu, instance);
			env.Register<object, object>("mu2pt", instance.mu2pt, instance);

			return env;
		}

		public static StandardEnvironment RegisterAllFunctions(
			this StandardEnvironment environment, IMapContext mapContext)
		{
			if (environment != null)
			{
				environment.RegisterConversionFunctions(mapContext);
				environment.RegisterColorFunctions();
				environment.RegisterGeometryFunctions();
			}

			return environment;
		}

		#region Conversion functions

		private class ConversionFunctions
		{
			public IMapContext MapContext { get; set; }

			public object mm2mu(object value)
			{
				if (value == null)
					return null;
				if (MapContext == null)
					throw NoMapContext("mm2mu");
				if (IsNumeric(value))
					return MapContext.PointsToMapUnits(
						ToDouble(ToDouble(value) * Constants.PointsPerMillimeter));
				throw InvalidArgumentType("mm2mu", value);
			}

			public object mu2mm(object value)
			{
				if (value == null)
					return null;
				if (MapContext == null)
					throw NoMapContext("mu2mm");
				if (IsNumeric(value))
					return MapContext.MapUnitsToPoints(ToDouble(value)) /
					       Constants.PointsPerMillimeter;
				throw InvalidArgumentType("mu2mm", value);
			}

			public object pt2mu(object value)
			{
				if (value == null)
					return null;
				if (MapContext == null)
					throw NoMapContext("pt2mu");
				if (IsNumeric(value))
					return MapContext.PointsToMapUnits(ToDouble(value));
				throw InvalidArgumentType("pt2mu", value);
			}

			public object mu2pt(object value)
			{
				if (value == null)
					return null;
				if (MapContext == null)
					throw NoMapContext("mu2pt");
				if (IsNumeric(value))
					return MapContext.MapUnitsToPoints(ToDouble(value));
				throw InvalidArgumentType("mu2pt", value);
			}

			private static Exception NoMapContext(string functionName)
			{
				return new InvalidOperationException(
					$"{functionName}: missing map context");
			}
		}

		private static object mm2pt(object value)
		{
			if (value == null)
				return null;
			if (IsNumeric(value))
				return ToDouble(value) * Constants.PointsPerMillimeter;
			throw InvalidArgumentType("mm2pt", value);
		}

		private static object pt2mm(object value)
		{
			if (value == null)
				return null;
			if (IsNumeric(value))
				return ToDouble(value) / Constants.PointsPerMillimeter;
			throw InvalidArgumentType("pt2mm", value);
		}

		#endregion

		#region Geometry functions

		private static object Area(object value)
		{
			return value is Polygon polygon ? polygon.Area : 0.0;
		}

		private static object Length(object value)
		{
			return value is Multipart multipart ? multipart.Length : 0.0;
		}

		#endregion

		#region Color functions

		private static CIMColor RgbColor(double red, double green, double blue)
		{
			int r = Convert.ToInt32(Clip(red, 0, 255));
			int g = Convert.ToInt32(Clip(green, 0, 255));
			int b = Convert.ToInt32(Clip(blue, 0, 255));

			return ColorUtils.CreateRGB(r, g, b);
		}

		private static CIMColor RgbColor(object value)
		{
			if (value is string text)
			{
				return ColorUtils.ParseHexColorRGB(text);
			}

			if (value is CIMRGBColor rgb)
			{
				return rgb;
			}

			if (value is CIMColor)
			{
				throw new NotImplementedException(); // TODO
			}

			return null;
		}

		private static CIMColor CmykColor(double cyan, double magenta, double yellow, double black)
		{
			int c = Convert.ToInt32(Clip(cyan, 0, 255));
			int m = Convert.ToInt32(Clip(magenta, 0, 255));
			int y = Convert.ToInt32(Clip(yellow, 0, 255));
			int k = Convert.ToInt32(Clip(black, 0, 255));

			return ColorUtils.CreateCMYK(c, m, y, k);
		}

		private static CIMColor CmykColor(object value)
		{
			if (value is CIMCMYKColor cmyk)
			{
				return cmyk;
			}

			if (value is CIMColor)
			{
				throw new NotImplementedException(); // TODO
			}

			return null;
		}

		private static CIMColor GrayColor(object value)
		{
			if (IsNumeric(value))
			{
				int level = Clip(ToInt32(value), 0, 255);

				return ColorUtils.CreateGray(level);
			}

			if (value is CIMGrayColor gray)
			{
				return gray;
			}

			if (value is CIMColor color)
			{
				return color.ToGray();
			}

			return null;
		}

		private static CIMColor HsvColor(double hue, double saturation, double value)
		{
			int h = Convert.ToInt32(Clip(hue, 0, 360));
			int s = Convert.ToInt32(Clip(saturation, 0, 100));
			int v = Convert.ToInt32(Clip(value, 0, 100));

			return ColorUtils.CreateHSV(h, s, v);
		}

		private static CIMColor HsvColor(object value)
		{
			if (IsNumeric(value))
			{
				double hue = ToDouble(value);
				return HsvColor(hue, 100, 100);
			}

			if (value is CIMHSVColor hsv)
			{
				return hsv;
			}

			if (value is CIMColor)
			{
				throw new NotImplementedException(); // TODO
			}

			return null;
		}

		private static CIMColor HlsColor(double hue, double lightness, double saturation)
		{
			int h = Convert.ToInt32(Clip(hue, 0, 360));
			int l = Convert.ToInt32(Clip(lightness, 0, 100));
			int s = Convert.ToInt32(Clip(saturation, 0, 100));

			return ColorUtils.CreateHSL(h, s, l);
		}

		private static CIMColor HlsColor(object value)
		{
			if (IsNumeric(value))
			{
				double hue = ToDouble(value);
				return HlsColor(hue, 50, 100);
			}

			if (value is CIMHSLColor hsl)
			{
				return hsl;
			}

			if (value is CIMColor)
			{
				throw new NotImplementedException(); // TODO
			}

			return null;
		}

		private static CIMColor MakeColor(object value)
		{
			if (value == null || value == DBNull.Value)
			{
				return null;
			}

			if (value is CIMColor color)
			{
				return color;
			}

			if (value is string text)
			{
				return ColorUtils.ParseColor(text);
			}

			throw InvalidArgumentType("MakeColor", value);
		}

		private static string FormatColor(CIMColor color)
		{
			return ColorUtils.FormatColor(color); // handles null by returning null - good
		}

		#endregion

		#region Utilities

		private static bool IsNumeric(object value)
		{
			TypeCode code = Convert.GetTypeCode(value);

			switch (code)
			{
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return true;
			}

			return false;
		}

		private static double ToDouble(object value)
		{
			return Convert.ToDouble(value, CultureInfo.InvariantCulture);
		}

		private static int ToInt32(object value)
		{
			return Convert.ToInt32(value, CultureInfo.InvariantCulture);
		}

		private static double Clip(double value, double min, double max)
		{
			if (value < min)
				return min;
			if (value > max)
				return max;
			return value;
		}

		private static int Clip(int value, int min, int max)
		{
			if (value < min)
				return min;
			if (value > max)
				return max;
			return value;
		}

		private static EvaluationException InvalidArgumentType(string method, object value)
		{
			string typeName = value == null ? "null" : value.GetType().Name;
			return new EvaluationException($"{method}: invalid argument type: {typeName}");
		}

		#endregion
	}
}
