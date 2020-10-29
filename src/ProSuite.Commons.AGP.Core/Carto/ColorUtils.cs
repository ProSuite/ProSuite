using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;

namespace ProSuite.Commons.AGP.Core.Carto
{
	/// <summary>
	/// Utilities for creating CIM colors
	/// </summary>
	public static class ColorUtils
	{
		public static CIMColor RedRGB => CreateRGB(255, 0, 0);
		public static CIMColor GreenRGB => CreateRGB(0, 255, 0);
		public static CIMColor BlueRGB => CreateRGB(0, 0, 255);

		public static CIMColor CyanRGB => CreateRGB(0, 255, 255);
		public static CIMColor MagentaRGB => CreateRGB(255, 0, 255);
		public static CIMColor YellowRGB => CreateRGB(255, 255, 0);

		public static CIMColor BlackRGB => CreateRGB(0, 0, 0);
		public static CIMColor GrayRGB => CreateRGB(127, 127, 127);
		public static CIMColor WhiteRGB => CreateRGB(255, 255, 255);

		public static CIMRGBColor CreateRGB(float red, float green, float blue, int alpha = 100)
		{
			return new CIMRGBColor {R = red, G = green, B = blue, Alpha = alpha};
		}

		public static CIMColor SetAlpha(this CIMColor color, float alpha)
		{
			if (color != null)
			{
				color.Alpha = Clip(alpha, 0, 100);
			}

			return color;
		}

		/// <summary>
		/// Additive color mixing: (1-f)*a + f*b.
		/// Requires RGB colors (including HLS and HSV) or gray level colors.
		/// Not supported for other color spaces.
		/// </summary>
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
					throw new NotSupportedException($"Cannot convert from {color.GetType().Name} to RGB");
			}
		}

		public static CIMGrayColor ToGray(this CIMRGBColor color)
		{
			const float sr = 0.299f, sg = 0.587f, sb = 0.114f;
			if (color == null) return null;
			float level = sr * color.R + sg * color.G + sb * color.B;
			return new CIMGrayColor {Level = Clip(level, 0, 255)};
		}

		/// <summary>
		/// An infinite sequence of pseudo random RGB colors.
		/// If you need a non-opaque alpha, use <see cref="SetAlpha"/>
		/// in a LINQ Select clause.
		/// </summary>
		public static IEnumerable<CIMRGBColor> RandomColors(float saturation = 60f, float lightness = 80f, float startHue = -1f)
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

				yield return new CIMRGBColor {R = r, G = g, B = b};
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
}
