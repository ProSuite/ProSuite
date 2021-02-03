using System;
using System.Globalization;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Processing.Evaluation
{
	// TODO Will need two implementations: one for ArcObjects, one for Pro SDK

	//public static class EnvironmentUtils
	//{
	//	// Functions of the same name must differ by their arity!
	//	// This is different from C# method overloading!

	//	public static StandardEnvironment RegisterGeometryFunctions(
	//		[NotNull] this StandardEnvironment env)
	//	{
	//		env.Register<object, object>("SHAPEAREA", Area);
	//		env.Register<object, object>("SHAPELENGTH", Length);
	//		// Nice to have: Area/0 and Length/0 that refer to current row's Shape field (whatever it's named)

	//		return env;
	//	}

	//	//public static StandardEnvironment RegisterColorFunctions([NotNull] this StandardEnvironment env)
	//	//{
	//	//	env.Register<double, double, double, IColor>("RGBCOLOR", RgbColor);
	//	//	env.Register<object, IColor>("RGBCOLOR", RgbColor);

	//	//	env.Register<double, double, double, double, IColor>("CMYKCOLOR", CmykColor);
	//	//	env.Register<object, IColor>("CMYKCOLOR", CmykColor);

	//	//	env.Register<object, IColor>("GRAYCOLOR", GrayColor);
	//	//	env.Register<object, IColor>("GREYCOLOR", GrayColor);

	//	//	env.Register<double, double, double, IColor>("HSVCOLOR", HsvColor);
	//	//	env.Register<object, IColor>("HSVCOLOR", HsvColor);

	//	//	env.Register<double, double, double, IColor>("HLSCOLOR", HlsColor);
	//	//	env.Register<object, IColor>("HLSCOLOR", HlsColor);

	//	//	env.Register<string, IColor>("MAKECOLOR", MakeColor);
	//	//	env.Register<IColor, string>("FORMATCOLOR", FormatColor); // rename?

	//	//	return env;
	//	//}

	//	//public static StandardEnvironment RegisterRepOverrideFunctions(
	//	//	[NotNull] this StandardEnvironment env)
	//	//{
	//	//	env.Register<object, object>("PACKREPOVERRIDE", PackRepOverride);
	//	//	env.Register<object, object>("UNPACKREPOVERRIDE", UnpackRepOverride);

	//	//	return env;
	//	//}

	//	public static StandardEnvironment RegisterConversionFunctions(
	//		[NotNull] this StandardEnvironment env /*,
	//		[CanBeNull] IMapContext mapContext*/)
	//	{
	//		//var instance = new ConversionFunctions {MapContext = mapContext};

	//		env.Register<object, object>("mm2pt", ConversionFunctions.mm2pt);
	//		env.Register<object, object>("pt2mm", ConversionFunctions.pt2mm);
	//		//env.Register<object, object>("mm2mu", instance.mm2mu, instance);
	//		//env.Register<object, object>("mu2mm", instance.mu2mm, instance);
	//		//env.Register<object, object>("pt2mu", instance.pt2mu, instance);
	//		//env.Register<object, object>("mu2pt", instance.mu2pt, instance);

	//		return env;
	//	}

	//	#region Conversion functions

	//	private class ConversionFunctions
	//	{
	//		//public IMapContext MapContext { get; set; }

	//		public static object mm2pt(object value)
	//		{
	//			if (value == null)
	//				return null;
	//			if (IsNumeric(value))
	//				return SymbolUtils.MillimetersToPoints(ToDouble(value));
	//			throw InvalidArgumentType("mm2pt", value);
	//		}

	//		public static object pt2mm(object value)
	//		{
	//			if (value == null)
	//				return null;
	//			if (IsNumeric(value))
	//				return SymbolUtils.PointsToMillimeters(ToDouble(value));
	//			throw InvalidArgumentType("pt2mm", value);
	//		}

	//		//public object mm2mu(object value)
	//		//{
	//		//	if (value == null)
	//		//		return null;
	//		//	if (MapContext == null)
	//		//		throw NoMapContext("mm2mu");
	//		//	if (IsNumeric(value))
	//		//		return ProcessingUtils.MillimetersToMapUnits(ToDouble(value), MapContext);
	//		//	throw InvalidArgumentType("mm2mu", value);
	//		//}

	//		//public object mu2mm(object value)
	//		//{
	//		//	if (value == null)
	//		//		return null;
	//		//	if (MapContext == null)
	//		//		throw NoMapContext("mu2mm");
	//		//	if (IsNumeric(value))
	//		//		return ProcessingUtils.MapUnitsToMillimeters(ToDouble(value), MapContext);
	//		//	throw InvalidArgumentType("mu2mm", value);
	//		//}

	//		//public object pt2mu(object value)
	//		//{
	//		//	if (value == null)
	//		//		return null;
	//		//	if (MapContext == null)
	//		//		throw NoMapContext("pt2mu");
	//		//	if (IsNumeric(value))
	//		//		return ProcessingUtils.PointsToMapUnits(ToDouble(value), MapContext);
	//		//	throw InvalidArgumentType("pt2mu", value);
	//		//}

	//		//public object mu2pt(object value)
	//		//{
	//		//	if (value == null)
	//		//		return null;
	//		//	if (MapContext == null)
	//		//		throw NoMapContext("mu2pt");
	//		//	if (IsNumeric(value))
	//		//		return ProcessingUtils.MapUnitsToPoints(ToDouble(value), MapContext);
	//		//	throw InvalidArgumentType("mu2pt", value);
	//		//}
	//	}

	//	#endregion

	//	#region Geometry functions

	//	private static object Area(object value)
	//	{
	//		return value is Polygon polygon ? polygon.Area : 0.0;
	//	}

	//	private static object Length(object value)
	//	{
	//		return value is Polyline polyline ? polyline.Length : 0.0;
	//	}

	//	#endregion

	//	#region Color functions

	//	//private static IColor RgbColor(double red, double green, double blue)
	//	//{
	//	//	int r = Convert.ToInt32(Clip(red, 0, 255));
	//	//	int g = Convert.ToInt32(Clip(green, 0, 255));
	//	//	int b = Convert.ToInt32(Clip(blue, 0, 255));

	//	//	return new RgbColorClass { Red = r, Green = g, Blue = b };
	//	//}

	//	//private static IColor RgbColor(object value)
	//	//{
	//	//	// TODO Allow string values like "#rgb" and "#rrggbb" ?

	//	//	var color = value as IColor;
	//	//	if (color == null)
	//	//		return null;

	//	//	double l, a, b;
	//	//	color.GetCIELAB(out l, out a, out b);

	//	//	var result = new RgbColorClass();
	//	//	result.SetCIELAB(l, a, b);

	//	//	return result;
	//	//}

	//	//private static IColor CmykColor(double cyan, double magenta, double yellow, double black)
	//	//{
	//	//	int c = Convert.ToInt32(Clip(cyan, 0, 255));
	//	//	int m = Convert.ToInt32(Clip(magenta, 0, 255));
	//	//	int y = Convert.ToInt32(Clip(yellow, 0, 255));
	//	//	int k = Convert.ToInt32(Clip(black, 0, 255));

	//	//	return new CmykColorClass { Cyan = c, Magenta = m, Yellow = y, Black = k };
	//	//}

	//	//private static IColor CmykColor(object value)
	//	//{
	//	//	var color = value as IColor;
	//	//	if (color == null)
	//	//		return null;

	//	//	double l, a, b;
	//	//	color.GetCIELAB(out l, out a, out b);

	//	//	var result = new CmykColorClass();
	//	//	result.SetCIELAB(l, a, b);

	//	//	return result;
	//	//}

	//	//private static IColor GrayColor(object value)
	//	//{
	//	//	if (IsNumeric(value))
	//	//	{
	//	//		int level = Clip(ToInt32(value), 0, 255); // 0 = black, 255 = white

	//	//		return new GrayColorClass { Level = level };
	//	//	}

	//	//	var color = value as IColor;
	//	//	if (color == null)
	//	//		return null;

	//	//	double l, a, b;
	//	//	color.GetCIELAB(out l, out a, out b);

	//	//	var result = new GrayColorClass();
	//	//	result.SetCIELAB(l, a, b);

	//	//	return result;
	//	//}

	//	//private static IColor HsvColor(double hue, double saturation, double value)
	//	//{
	//	//	int h = Convert.ToInt32(Clip(hue, 0, 360));
	//	//	int s = Convert.ToInt32(Clip(saturation, 0, 100));
	//	//	int v = Convert.ToInt32(Clip(value, 0, 100));

	//	//	return new HsvColorClass { Hue = h, Saturation = s, Value = v };
	//	//}

	//	//private static IColor HsvColor(object value)
	//	//{
	//	//	if (IsNumeric(value))
	//	//	{
	//	//		double hue = ToDouble(value);
	//	//		return HsvColor(hue, 100, 100);
	//	//	}

	//	//	var color = value as IColor;
	//	//	if (color == null)
	//	//		return null;

	//	//	double l, a, b;
	//	//	color.GetCIELAB(out l, out a, out b);

	//	//	var result = new HsvColorClass();
	//	//	result.SetCIELAB(l, a, b);

	//	//	return result;
	//	//}

	//	//private static IColor HlsColor(double hue, double lightness, double saturation)
	//	//{
	//	//	int h = Convert.ToInt32(Clip(hue, 0, 360));
	//	//	int l = Convert.ToInt32(Clip(lightness, 0, 100));
	//	//	int s = Convert.ToInt32(Clip(saturation, 0, 100));

	//	//	return new HlsColorClass { Hue = h, Lightness = l, Saturation = s };
	//	//}

	//	//private static IColor HlsColor(object value)
	//	//{
	//	//	if (IsNumeric(value))
	//	//	{
	//	//		double hue = ToDouble(value);
	//	//		return HlsColor(hue, 50, 100);
	//	//	}

	//	//	var color = value as IColor;
	//	//	if (color == null)
	//	//		return null;

	//	//	double l, a, b;
	//	//	color.GetCIELAB(out l, out a, out b);

	//	//	var result = new HlsColorClass();
	//	//	result.SetCIELAB(l, a, b);

	//	//	return result;
	//	//}

	//	//private static IColor MakeColor(object value)
	//	//{
	//	//	if (value == null || value == DBNull.Value)
	//	//	{
	//	//		return null;
	//	//	}

	//	//	var color = value as IColor;
	//	//	if (color != null)
	//	//	{
	//	//		return color;
	//	//	}

	//	//	var text = value as string;
	//	//	if (text != null)
	//	//	{
	//	//		return SymbolUtils.ColorFromText(text);
	//	//	}

	//	//	throw InvalidArgumentType("MakeColor", value);
	//	//}

	//	//private static string FormatColor(IColor color)
	//	//{
	//	//	return SymbolUtils.ColorToText(color); // handles null by returning null - good
	//	//}

	//	#endregion

	//	#region Blob functions

	//	//private static object PackRepOverride(object value)
	//	//{
	//	//	if (value == null || value == DBNull.Value)
	//	//		return null;
	//	//	if (value is bool || value is string || IsNumeric(value))
	//	//		return value;

	//	//	return RepresentationUtils.StoreFieldOverrideBlob(value);

	//	//	// TODO should Marshal.ReleaseComObject() on returned blob? How?
	//	//}

	//	//private static object UnpackRepOverride(object blob)
	//	//{
	//	//	if (blob == null || blob == DBNull.Value)
	//	//		return null;
	//	//	if (IsNumeric(blob))
	//	//		return ToDouble(blob);
	//	//	if (blob is bool)
	//	//		return (bool)blob;
	//	//	if (blob is string)
	//	//		return (string)blob;

	//	//	return RepresentationUtils.LoadFieldOverrideBlob(blob);
	//	//}

	//	#endregion

	//	#region Utilities

	//	private static bool IsNumeric(object value)
	//	{
	//		TypeCode code = Convert.GetTypeCode(value);

	//		switch (code)
	//		{
	//			case TypeCode.SByte:
	//			case TypeCode.Byte:
	//			case TypeCode.Int16:
	//			case TypeCode.UInt16:
	//			case TypeCode.Int32:
	//			case TypeCode.UInt32:
	//			case TypeCode.Int64:
	//			case TypeCode.UInt64:
	//			case TypeCode.Single:
	//			case TypeCode.Double:
	//			case TypeCode.Decimal:
	//				return true;
	//		}

	//		return false;
	//	}

	//	private static double ToDouble(object value)
	//	{
	//		return Convert.ToDouble(value, CultureInfo.InvariantCulture);
	//	}

	//	private static int ToInt32(object value)
	//	{
	//		return Convert.ToInt32(value, CultureInfo.InvariantCulture);
	//	}

	//	private static double Clip(double value, double min, double max)
	//	{
	//		if (value < min)
	//			return min;
	//		if (value > max)
	//			return max;
	//		return value;
	//	}

	//	private static int Clip(int value, int min, int max)
	//	{
	//		if (value < min)
	//			return min;
	//		if (value > max)
	//			return max;
	//		return value;
	//	}

	//	private static EvaluationException InvalidArgumentType(string method, object value)
	//	{
	//		string typeName = value == null ? "null" : value.GetType().Name;
	//		return new EvaluationException($"{method}: invalid argument type: {typeName}");
	//	}

	//	private static Exception NoMapContext(string functionName)
	//	{
	//		return new InvalidOperationException($"{functionName}: map context is null");
	//	}

	//	#endregion
	//}
}
