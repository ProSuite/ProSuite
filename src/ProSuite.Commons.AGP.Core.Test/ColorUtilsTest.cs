using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Carto;

namespace ProSuite.Commons.AGP.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ColorUtilsTest
	{
		[Test]
		public void CanRandomColors()
		{
			foreach (var color in ColorUtils.RandomColors().Take(100))
			{
				Console.WriteLine($"{color.R:F0}, {color.G:F0}, {color.B:F0}");
				AssertRGB(color.R, color.G, color.B);
			}
		}

		[Test]
		public void CanSetAlpha()
		{
			const float alpha = 50;
			const int n = 3;

			var colors = ColorUtils.RandomColors().Select(c => c.SetAlpha(alpha))
			                       .Take(3).ToList();

			for (int i = 0; i < n; i++)
			{
				Assert.AreEqual(alpha, colors[i].Alpha);
			}
		}

		[Test]
		public void CanBlend()
		{
			var c1 = ColorUtils.Blend(ColorUtils.RedRGB, ColorUtils.BlackRGB);
			AssertRGB(127, 0, 0, c1.R, c1.G, c1.B, "red/black");
		}

		[Test]
		public void CanCMYKtoRGB()
		{
			float r, g, b;

			ColorUtils.CMYKtoRGB(0, 0, 0, 100, out r, out g, out b);
			AssertRGB(0, 0, 0, r, g, b, "black");

			ColorUtils.CMYKtoRGB(0, 0, 0, 0, out r, out g, out b);
			AssertRGB(255, 255, 255, r, g, b, "white");

			ColorUtils.CMYKtoRGB(0, 100, 100, 0, out r, out g, out b);
			AssertRGB(255, 0, 0, r, g, b, "red");

			ColorUtils.CMYKtoRGB(100, 0, 100, 0, out r, out g, out b);
			AssertRGB(0, 255, 0, r, g, b, "green");

			ColorUtils.CMYKtoRGB(100, 100, 0, 0, out r, out g, out b);
			AssertRGB(0, 0, 255, r, g, b, "blue");

			ColorUtils.CMYKtoRGB(100, 0, 0, 0, out r, out g, out b);
			AssertRGB(0, 255, 255, r, g, b, "cyan");

			ColorUtils.CMYKtoRGB(0, 100, 0, 0, out r, out g, out b);
			AssertRGB(255, 0, 255, r, g, b, "magenta");

			ColorUtils.CMYKtoRGB(0, 0, 100, 0, out r, out g, out b);
			AssertRGB(255, 255, 0, r, g, b, "yellow");
		}

		[Test]
		public void CanHSLtoRGB()
		{
			float r, g, b;

			ColorUtils.HSLtoRGB(0, 0, 0, out r, out g, out b);
			AssertRGB(0, 0, 0, r, g, b, "black");

			ColorUtils.HSLtoRGB(0, 0, 100, out r, out g, out b);
			AssertRGB(255, 255, 255, r, g, b, "white");

			ColorUtils.HSLtoRGB(0, 100, 50, out r, out g, out b);
			AssertRGB(255, 0, 0, r, g, b, "red");

			ColorUtils.HSLtoRGB(120, 100, 50, out r, out g, out b);
			AssertRGB(0, 255, 0, r, g, b, "green");

			ColorUtils.HSLtoRGB(240, 100, 50, out r, out g, out b);
			AssertRGB(0, 0, 255, r, g, b, "blue");

			ColorUtils.HSLtoRGB(180, 100, 50, out r, out g, out b);
			AssertRGB(0, 255, 255, r, g, b, "cyan");

			ColorUtils.HSLtoRGB(300, 100, 50, out r, out g, out b);
			AssertRGB(255, 0, 255, r, g, b, "magenta");

			ColorUtils.HSLtoRGB(60, 100, 50, out r, out g, out b);
			AssertRGB(255, 255, 0, r, g, b, "yellow");

			ColorUtils.HSLtoRGB(0, 0, 50, out r, out g, out b);
			AssertRGB(128, 128, 128, r, g, b, "gray");

			ColorUtils.HSLtoRGB(180, 100, 25, out r, out g, out b);
			AssertRGB(0, 128, 128, r, g, b, "teal");
		}

		[Test]
		public void CanHSVtoRGB()
		{
			float r, g, b;

			ColorUtils.HSVtoRGB(0, 0, 0, out r, out g, out b);
			AssertRGB(0, 0, 0, r, g, b, "black");

			ColorUtils.HSVtoRGB(0, 0, 100, out r, out g, out b);
			AssertRGB(255, 255, 255, r, g, b, "white");

			ColorUtils.HSVtoRGB(0, 100, 100, out r, out g, out b);
			AssertRGB(255, 0, 0, r, g, b, "red");

			ColorUtils.HSVtoRGB(120, 100, 100, out r, out g, out b);
			AssertRGB(0, 255, 0, r, g, b, "green");

			ColorUtils.HSVtoRGB(240, 100, 100, out r, out g, out b);
			AssertRGB(0, 0, 255, r, g, b, "blue");

			ColorUtils.HSVtoRGB(180, 100, 100, out r, out g, out b);
			AssertRGB(0, 255, 255, r, g, b, "cyan");

			ColorUtils.HSVtoRGB(300, 100, 100, out r, out g, out b);
			AssertRGB(255, 0, 255, r, g, b, "magenta");

			ColorUtils.HSVtoRGB(60, 100, 100, out r, out g, out b);
			AssertRGB(255, 255, 0, r, g, b, "yellow");

			ColorUtils.HSVtoRGB(0, 0, 50, out r, out g, out b);
			AssertRGB(128, 128, 128, r, g, b, "gray");

			ColorUtils.HSVtoRGB(180, 100, 50, out r, out g, out b);
			AssertRGB(0, 128, 128, r, g, b, "teal");
		}

		private static void AssertRGB(float rx, float gx, float bx, float ra, float ga, float ba, string name)
		{
			const double delta = 0.501; // allow rounding to integers

			float dr = Math.Abs(rx - ra);
			float dg = Math.Abs(gx - ga);
			float db = Math.Abs(bx - ba);

			Assert.True(dr < delta && dg < delta && db < delta,
			            $"Bad RGB for {name}: expected {rx},{gx},{bx} but got {ra},{ga},{ba}");
		}

		private static void AssertRGB(float r, float g, float b)
		{
			bool ok = 0 <= r && r < 256 &&
			          0 <= g && g < 255 &&
			          0 <= b && b < 256;

			Assert.True(ok, $"RGB components out of range: {r}, {g}, {b}");
		}
	}
}