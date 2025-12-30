using System;
using System.Collections.Generic;
using System.Globalization;
using ArcGIS.Core.CIM;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Carto;

/// <summary>
/// Utilities for creating CIM colors.
/// Component ranges:
/// Alpha is 0..100 (transparent to opaque),
/// Level is 0..255 (black to white),
/// R G B is 0..255,
/// C Y M K is 0..100,
/// H is 0..360,
/// S L V is 0..100.
/// </summary>
public static class ColorUtils
{
	// Always return a new instance (because colors are mutable objects):

	public static CIMColor RedRGB => CreateRGB(255, 0, 0);
	public static CIMColor GreenRGB => CreateRGB(0, 255, 0);
	public static CIMColor BlueRGB => CreateRGB(0, 0, 255);

	public static CIMColor CyanRGB => CreateRGB(0, 255, 255);
	public static CIMColor MagentaRGB => CreateRGB(255, 0, 255);
	public static CIMColor YellowRGB => CreateRGB(255, 255, 0);

	public static CIMColor BlackRGB => CreateRGB(0, 0, 0);
	public static CIMColor GrayRGB => CreateRGB(127, 127, 127);
	public static CIMColor WhiteRGB => CreateRGB(255, 255, 255);

	/// <remarks>RGB components are in 0..255</remarks>
	public static CIMRGBColor CreateRGB(float red, float green, float blue, int alpha = 100)
	{
		return new CIMRGBColor { R = red, G = green, B = blue, Alpha = alpha };
	}

	/// <remarks>CMYK components are in 0..100</remarks>
	public static CIMCMYKColor CreateCMYK(int cyan, int magenta, int yellow, int black,
	                                      int alpha = 100)
	{
		return new CIMCMYKColor { C = cyan, M = magenta, Y = yellow, K = black, Alpha = alpha };
	}

	/// <param name="level">level of gray: 0 is black, 255 is white</param>
	/// <param name="alpha">alpha channel: 0 is transparent, 100 is opaque (default)</param>
	public static CIMGrayColor CreateGray(int level, int alpha = 100)
	{
		return new CIMGrayColor { Level = level, Alpha = alpha };
	}

	/// <param name="hue">the color hue, 0 is red, 60 yellow, 120 green, 180 cyan, 240 blue, 300 magenta</param>
	/// <param name="saturation">saturation level: 0 is gray, 100 is saturated</param>
	/// <param name="lightness">lightness: 0 is black, 100 is white</param>
	/// <param name="alpha">alpha channel: 0 is transparent, 100 is opaque (default)</param>
	public static CIMHSLColor CreateHSL(int hue, int saturation, int lightness, int alpha = 100)
	{
		return new CIMHSLColor { H = hue, S = saturation, L = lightness, Alpha = alpha };
	}

	/// <param name="hue">the color hue, 0 is red, 60 yellow, 120 green, 180 cyan, 240 blue, 300 magenta</param>
	/// <param name="saturation">saturation level: 0 is gray, 100 is saturated</param>
	/// <param name="value">value: 0 is black, 100 is full color</param>
	/// <param name="alpha">alpha channel: 0 is transparent, 100 is opaque (default)</param>
	/// <remarks>HSV is also known as HSB (brightness)</remarks>
	public static CIMHSVColor CreateHSV(int hue, int saturation, int value, int alpha = 100)
	{
		return new CIMHSVColor { H = hue, S = saturation, V = value, Alpha = alpha };
	}

	/// <param name="color">the color whose alpha is to be modified</param>
	/// <param name="alpha">between 0 (transparent) and 100 (opaque)</param>
	public static T SetAlpha<T>(this T color, float alpha) where T : CIMColor
	{
		if (color != null)
		{
			color.Alpha = Clip(alpha, 0, 100);
		}

		return color;
	}

	/// <summary>Additive color mixing: (1-f)*x + f*y.</summary>
	/// <remarks>Input colors must be RGB (including HLS and HSV) or CMYK
	/// (will be converted to RGB) or gray level colors. Other color spaces
	/// are not supported. The result is always an RGB color.</remarks>
	/// <returns>a new RGB color instance</returns>
	public static CIMRGBColor Blend(CIMColor x, CIMColor y, float f = 0.5f)
	{
		var xx = x.ToRGB();
		var yy = y.ToRGB();

		float cf = 1 - f;

		float r = cf * xx.R + f * yy.R;
		float g = cf * xx.G + f * yy.G;
		float b = cf * xx.B + f * yy.B;

		return CreateRGB(r, g, b);
	}

	/// <summary>Create a lighter color by blending with white</summary>
	/// <remarks>Input color must be RGB (including HLS and HSV) or CMYK
	/// (will be converted to RGB) or gray level colors. Other color spaces
	/// are not supported. The result is always an RGB color.</remarks>
	/// <returns>a new RGB color instance</returns>
	public static CIMRGBColor Lighter(this CIMColor color, float f = 0.5f)
	{
		return Blend(color, WhiteRGB, f);
	}

	public static CIMRGBColor ToRGB(this CIMHSLColor color)
	{
		if (color == null) return null;
		HSLtoRGB(color.H, color.S, color.L, out float r, out float g, out float b);
		return CreateRGB(r, g, b);
	}

	public static CIMRGBColor ToRGB(this CIMHSVColor color)
	{
		if (color == null) return null;
		HSVtoRGB(color.H, color.S, color.V, out float r, out float g, out float b);
		return CreateRGB(r, g, b);
	}

	public static CIMRGBColor ToRGB(this CIMGrayColor color)
	{
		if (color == null) return null;
		float level = Clip(color.Level, 0, 255);
		return CreateRGB(level, level, level);
	}

	public static CIMRGBColor ToRGB(this CIMCMYKColor color)
	{
		if (color == null) return null;
		CMYKtoRGB(color.C, color.M, color.Y, color.K, out float r, out float g, out float b);
		return CreateRGB(r, g, b);
	}

	public static CIMRGBColor ToRGB(this CIMColor color)
	{
		if (color == null) return null;
		switch (color)
		{
			case CIMRGBColor rgb:
				return rgb;
			case CIMHSLColor hsl:
				return hsl.ToRGB();
			case CIMHSVColor hsv:
				return hsv.ToRGB();
			case CIMGrayColor gray:
				return gray.ToRGB();
			case CIMCMYKColor cmyk:
				return cmyk.ToRGB();
			default:
				throw new NotSupportedException(
					$"Cannot convert from {color.GetType().Name} to RGB");
		}
	}

	public static CIMGrayColor ToGray(this CIMRGBColor color)
	{
		const float sr = 0.299f, sg = 0.587f, sb = 0.114f;
		if (color == null) return null;
		float level = sr * color.R + sg * color.G + sb * color.B;
		return new CIMGrayColor { Level = Clip(level, 0, 255) };
	}

	public static CIMGrayColor ToGray(this CIMColor color)
	{
		if (color == null) return null;
		switch (color)
		{
			case CIMRGBColor rgb:
				return rgb.ToGray();
			case CIMCMYKColor cmyk:
				return cmyk.ToRGB().ToGray();
			case CIMGrayColor gray:
				return gray;
			case CIMHSVColor hsv:
				return hsv.ToRGB().ToGray();
			case CIMHSLColor hsl:
				return hsl.ToRGB().ToGray();
			default:
				throw new NotSupportedException(
					$"Cannot convert from {color.GetType().Name} to Gray");
		}
	}

	#region Conversion from IColor to Text and vice versa

	/// <summary>
	/// Return a textual representation of the given color.
	/// </summary>
	/// <param name="color">The color, can be null.</param>
	/// <returns>Textual representation or null.</returns>
	/// <remarks>
	/// This method generates textual representations that will be
	/// understood by <see cref="ParseColor"/>. Returns null if
	/// the <paramref name="color"/> passed in is null.
	/// </remarks>
	/// <seealso cref="ParseColor"/>
	[CanBeNull]
	public static string FormatColor([CanBeNull] CIMColor color)
	{
		if (color == null)
		{
			return null;
		}

		var invariant = CultureInfo.InvariantCulture;

		if (color is CIMRGBColor rgb)
		{
			return string.Format(invariant, "RGB({0:F0},{1:F0},{2:F0})", rgb.R, rgb.G, rgb.B);
		}

		if (color is CIMCMYKColor cmyk)
		{
			return string.Format(invariant, "CMYK({0:F0},{1:F0},{2:F0},{3:F0})", cmyk.C, cmyk.M,
			                     cmyk.Y, cmyk.K);
		}

		if (color is CIMGrayColor gray)
		{
			return string.Format(invariant, "Gray({0:F0})", gray.Level);
		}

		if (color is CIMHSLColor hls)
		{
			return string.Format(invariant, "HLS({0:F0},{1:F0},{2:F0})", hls.H, hls.L, hls.S);
		}

		if (color is CIMHSVColor hsv)
		{
			return string.Format(invariant, "HSV({0:F0},{1:F0},{2:F0})", hsv.H, hsv.S, hsv.V);
		}

		throw new NotSupportedException(
			$"Color of type {color.GetType().Name} is not supported");
	}

	/// <summary>
	/// Create a CIM Color object from the given textual representation.
	/// </summary>
	/// <param name="text">A color's textual representation (or null).</param>
	/// <returns>An CIM Color object or null.</returns>
	/// <remarks>
	/// This method understands textual color specifications according to
	/// this grammar:
	/// <code>
	/// ColorSpec ::= RgbSpec | CmykSpec | GraySpec | HlsSpec | HsvSpec | HexSpec
	/// RgbSpec ::= "RGB" "(" Red "," Green "," Blue ")"
	/// CmykSpec ::= "CMYK" "(" Cyan "," Magenta "," Yellow "," Black ")"
	/// GraySpec ::= "Gray" "(" GrayLevel ")"
	/// HlsSpec ::= "HLS" "(" Hue "," Lightness "," Saturation ")"
	/// HsvSpec ::= "HSV" "(" Hue "," Saturation "," Value ")"
	/// HexSpec ::= "#"RGB | "#"RRGGBB
	/// Red, Green, Blue ::= decimal integer number in range 0..255
	/// Cyan, Magenta, Yellow, Black ::= decimal integer number in range 0..255
	/// GrayLevel ::= decimal integer number in range 0..255
	/// Hue ::= decimal integer number in range 0..360
	/// Lightness, Saturation, Value: decimal integer number in range 0..100
	/// </code>
	/// White space between tokens is ignored.
	/// Case in the color model name is irrelevant.
	/// British folks may also write <i>Grey</i> instead of <i>Gray</i>.
	/// <para/>
	/// For example, <c>RGB(255,0,0)</c> is a bright red in the RGB model.
	/// <para/>
	/// The method <see cref="FormatColor"/> creates textual representations
	/// according to the grammar above.
	/// </remarks>
	/// <seealso cref="FormatColor"/>
	[CanBeNull]
	public static CIMColor ParseColor([CanBeNull] string text)
	{
		if (text == null)
		{
			return null;
		}

		var invariant = CultureInfo.InvariantCulture;
		var i = 0;

		SkipWhiteSpace(text, ref i);

		if (i >= text.Length)
		{
			return null; // white space only
		}

		if (text[i] == '#')
		{
			return ParseHexColorRGB(text.Substring(i));
		}

		int j = i;

		while (i < text.Length && char.IsLetter(text, i))
		{
			i += 1;
		}

		string model = text.Substring(j, i - j);
		var parameters = new List<int>();

		SkipWhiteSpace(text, ref i);

		Expect(text, ref i, '(');

		while (true)
		{
			SkipWhiteSpace(text, ref i);

			j = i;
			while (i < text.Length && ! char.IsWhiteSpace(text[i]) && text[i] != ',' &&
			       text[i] != ')')
			{
				i += 1;
			}

			string sub = text.Substring(j, i - j);

			if (! int.TryParse(sub, NumberStyles.Integer, invariant, out int value))
			{
				throw new FormatException(
					$"Invalid color specification: \"{sub}\" is not an integer");
			}

			parameters.Add(value);

			SkipWhiteSpace(text, ref i);
			if (i >= text.Length || text[i] != ',')
			{
				break;
			}

			i += 1; // skip the comma
		}

		Expect(text, ref i, ')');

		SkipWhiteSpace(text, ref i);

		// Be lenient and allow text after a complete color spec.
		// For full strictness, uncomment the following few lines:

		//if (i != text.Length)
		//{
		//    throw new FormatException("Text follows color specification");
		//}

		if (string.Equals(model, "RGB", StringComparison.OrdinalIgnoreCase))
		{
			CheckCount("RGB", parameters.Count, "Red", "Green", "Blue");

			var red = CheckRange(parameters[0], 0, 255, "RGB", "Red");
			var green = CheckRange(parameters[1], 0, 255, "RGB", "Green");
			var blue = CheckRange(parameters[2], 0, 255, "RGB", "Blue");

			return CreateRGB(red, green, blue);
		}

		if (string.Equals(model, "CMYK", StringComparison.OrdinalIgnoreCase))
		{
			CheckCount("CMYK", parameters.Count, "Cyan", "Magenta", "Yellow", "Black");

			var cyan = CheckRange(parameters[0], 0, 255, "CMYK", "Cyan");
			var magenta = CheckRange(parameters[1], 0, 255, "CMYK", "Magenta");
			var yellow = CheckRange(parameters[2], 0, 255, "CMYK", "Yellow");
			var black = CheckRange(parameters[3], 0, 255, "CMYK", "Black");

			return CreateCMYK(cyan, magenta, yellow, black);
		}

		if (string.Equals(model, "gray", StringComparison.OrdinalIgnoreCase) ||
		    string.Equals(model, "grey", StringComparison.OrdinalIgnoreCase))
		{
			CheckCount("Gray", parameters.Count, "Level");

			var level = CheckRange(parameters[0], 0, 255, "Gray", "Level");

			return CreateGray(level);
		}

		if (string.Equals(model, "HLS", StringComparison.OrdinalIgnoreCase) ||
		    string.Equals(model, "HSL", StringComparison.OrdinalIgnoreCase))
		{
			CheckCount("HLS", parameters.Count, "Hue", "Lightness", "Saturation");

			var hue = CheckRange(parameters[0], 0, 360, "HLS", "Hue");
			var lightness = CheckRange(parameters[1], 0, 100, "HLS", "Lightness");
			var saturation = CheckRange(parameters[2], 0, 100, "HLS", "Saturation");

			return CreateHSL(hue, lightness, saturation);
		}

		if (string.Equals(model, "HSV", StringComparison.OrdinalIgnoreCase))
		{
			CheckCount("HSV", parameters.Count, "Hue", "Saturation", "Value");

			var hue = CheckRange(parameters[0], 0, 360, "HSV", "Hue");
			var saturation = CheckRange(parameters[1], 0, 100, "HSV", "Saturation");
			var value = CheckRange(parameters[2], 0, 100, "HSV", "Value");

			return CreateHSV(hue, saturation, value);
		}

		throw new FormatException(
			$"Unknown color model: \"{model}\" (use one of RGB, CMYK, Gray, HLS, HSV)");
	}

	public static CIMRGBColor ParseHexColorRGB(string text)
	{
		if (text == null) return null;

		if (text.Length == 4)
		{
			int r = GetHexValue(text[1]);
			int g = GetHexValue(text[2]);
			int b = GetHexValue(text[3]);

			if (text[0] == '#' && r >= 0 && g >= 0 && b >= 0)
				return CreateRGB(r + 16 * r, g + 16 * g, b + 16 * b);
		}

		if (text.Length == 7 && text[0] == '#')
		{
			string rHex = text.Substring(1, 2);
			string gHex = text.Substring(3, 2);
			string bHex = text.Substring(5, 2);

			if (TryParseHex(rHex, out int red) &&
			    TryParseHex(gHex, out int green) &&
			    TryParseHex(bHex, out int blue))
			{
				return CreateRGB(red, green, blue);
			}
		}

		throw new FormatException(
			$"Expect #RGB or #RRGGBB with hex digits R,G,B, but got: {text}");
	}

	public static CIMRGBColor ParseHexColorARGB(string text)
	{
		if (text[0] != '#' || text.Length != 9)
		{
			ThrowARGBFormatException(text);
		}

		CIMRGBColor result = ParseHexColorRGB($"#{text.Substring(3)}");

		string alphaString = text.Substring(1, 2);

		if (int.TryParse(alphaString, NumberStyles.HexNumber,
		                 CultureInfo.InvariantCulture, out int alphaInt))
		{
			result.SetAlphaValue(alphaInt * 100 / 255d);
		}
		else
		{
			ThrowARGBFormatException(text);
		}

		return result;
	}

	private static void ThrowARGBFormatException(string text)
	{
		throw new FormatException(
			$"Expect #AARRGGBB with hex digits A,R,G,B, but got: {text}");
	}

	private static bool TryParseHex(string text, out int result)
	{
		return int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
		                    out result);
	}

	private static int GetHexValue(char c)
	{
		switch (c)
		{
			case '0': return 0;
			case '1': return 1;
			case '2': return 2;
			case '3': return 3;
			case '4': return 4;
			case '5': return 5;
			case '6': return 6;
			case '7': return 7;
			case '8': return 8;
			case '9': return 9;
			case 'A': return 10;
			case 'a': return 10;
			case 'B': return 11;
			case 'b': return 11;
			case 'C': return 12;
			case 'c': return 12;
			case 'D': return 13;
			case 'd': return 13;
			case 'E': return 14;
			case 'e': return 14;
			case 'F': return 15;
			case 'f': return 15;
			default: return -1;
		}
	}

	private static void Expect(string s, ref int i, char expected)
	{
		if (i >= s.Length || s[i] != expected)
		{
			if (i < s.Length)
			{
				throw new FormatException(
					$"Invalid color specification: expected '{expected}' but found '{s[i]}'");
			}

			throw new FormatException(
				$"Invalid color specification: expected '{expected}' but found end-of-text");
		}

		i += 1;
	}

	private static void SkipWhiteSpace(string s, ref int i)
	{
		while (i < s.Length && char.IsWhiteSpace(s, i))
		{
			i += 1;
		}
	}

	private static void CheckCount(string model, int count, params string[] expected)
	{
		if (count != expected.Length)
		{
			string suffix = expected.Length == 1 ? "" : "s";
			string components = string.Join(", ", expected);

			throw new FormatException(
				$"The {model} color model expects {expected.Length} parameter{suffix}: {components}");
		}
	}

	private static int CheckRange(int value, int min, int max, string model, string arg)
	{
		if (value < min || value > max)
		{
			throw new FormatException(
				$"{model} color's {arg} must be in range {min} to {max} (inclusive)");
		}

		return value;
	}

	#endregion

	/// <summary>
	/// An infinite sequence of pseudo random RGB colors.
	/// If you need a non-opaque alpha, use <see cref="SetAlpha{T}"/>
	/// in a LINQ Select clause.
	/// </summary>
	public static IEnumerable<CIMRGBColor> RandomColors(float saturation = 60f,
	                                                    float lightness = 80f,
	                                                    float startHue = -1f)
	{
		const double invphi = 0.618033988749895; // 1/phi
		double hue;

		if (startHue < 0) hue = new Random().NextDouble();
		else hue = Clip(startHue / 360, 0, 1);

		saturation = Clip(saturation, 0, 100);
		lightness = Clip(lightness, 0, 100);

		while (true)
		{
			hue += invphi;
			hue %= 1.0;

			HSLtoRGB((int) (hue * 361), saturation, lightness,
			         out float r, out float g, out float b);

			yield return new CIMRGBColor { R = r, G = g, B = b };
		}

		// ReSharper disable once IteratorNeverReturns
	}

	public static void CMYKtoRGB(float c, float m, float y, float k,
	                             out float r, out float g, out float b)
	{
		c = Clip(c / 100, 0, 1);
		m = Clip(m / 100, 0, 1);
		y = Clip(y / 100, 0, 1);
		k = Clip(k / 100, 0, 1);

		r = (1 - c) * (1 - k) * 255;
		g = (1 - m) * (1 - k) * 255;
		b = (1 - y) * (1 - k) * 255;
	}

	/*
	 * HSL to RGB
	 *
	 * H in 0..360, S in 0..1, L in 0..1
	 *
	 * let C = (1 - |2L-1|) * S  // chroma
	 * let H' = H/60
	 * let X = C * (1 - |H' mod 2 - 1|)
	 * let m = L - C/2
	 *
	 * let (R',G',B') = (C,X,0) if 0 <= H < 60
	 *                  (X,C,0) if 60 <= H < 120
	 *                  (0,C,X) if 120 <= H < 180
	 *                  (0,X,C) if 180 <= H < 240
	 *                  (X,0,C) if 240 <= H < 300
	 *                  (C,0,X) if 300 <= H < 360
	 *
	 * return R,G,B = (R'+m)*255, (G'+m)*255, (B'+m)*255
	 *
	 * See https://www.rapidtables.com/convert/color/
	 */

	public static void HSLtoRGB(float h, float s, float l,
	                            out float r, out float g, out float b)
	{
		h %= 360;
		s = Clip(s / 100, 0, 1);
		l = Clip(l / 100, 0, 1);

		float c = (1 - Math.Abs(2 * l - 1)) * s;
		float hh = h / 60;
		float x = c * (1 - Math.Abs(hh % 2 - 1));
		float m = l - c / 2;

		SectorToRGB(h, c, x, m, out r, out g, out b);
	}

	/*
	 * HSV to RGB
	 *
	 * H in 0..360, S in 0..1, V in 0..1
	 *
	 * let C = V * S
	 * let X = C * (1 - |(H/60) mod 2 - 1|)
	 * let m = V - C
	 *
	 * let R',G',B' = as above
	 * return R,G,B = as above
	 */

	public static void HSVtoRGB(float h, float s, float v,
	                            out float r, out float g, out float b)
	{
		h %= 360;
		s = Clip(s / 100, 0, 1);
		v = Clip(v / 100, 0, 1);

		float c = v * s;
		float hh = h / 60;
		float x = c * (1 - Math.Abs(hh % 2 - 1));
		float m = v - c;

		SectorToRGB(h, c, x, m, out r, out g, out b);
	}

	private static void SectorToRGB(float h, float c, float x, float m,
	                                out float r, out float g, out float b)
	{
		if (h < 60)
		{
			r = (c + m) * 255;
			g = (x + m) * 255;
			b = m * 255;
		}
		else if (h < 120)
		{
			r = (x + m) * 255;
			g = (c + m) * 255;
			b = m * 255;
		}
		else if (h < 180)
		{
			r = m * 255;
			g = (c + m) * 255;
			b = (x + m) * 255;
		}
		else if (h < 240)
		{
			r = m * 255;
			g = (x + m) * 255;
			b = (c + m) * 255;
		}
		else if (h < 300)
		{
			r = (x + m) * 255;
			g = m * 255;
			b = (c + m) * 255;
		}
		else
		{
			r = (c + m) * 255;
			g = m * 255;
			b = (x + m) * 255;
		}
	}

	private static float Clip(double value, float min, float max)
	{
		if (value < min) return min;
		if (value > max) return max;
		return (float) value;
	}
}
