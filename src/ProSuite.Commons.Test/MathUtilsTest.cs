using System;
using System.Diagnostics;
using NUnit.Framework;

namespace ProSuite.Commons.Test
{
	[TestFixture]
	public class MathUtilsTest
	{
		[Test]
		public void CanRoundToSignificantDigits0()
		{
			Assert.AreEqual(0.0, MathUtils.RoundToSignificantDigits(0.0, 1));
			Assert.AreEqual(0.0, MathUtils.RoundToSignificantDigits(0.0, 8));
			Assert.AreEqual(0.0, MathUtils.RoundToSignificantDigits(0.0, 15));
			Assert.AreEqual(0.0, MathUtils.RoundToSignificantDigits(0.0, 0));
		}

		[Test]
		public void CanRoundToSignificantDigits1()
		{
			Assert.AreEqual(1.123,
			                MathUtils.RoundToSignificantDigits(1.12300000000000000011, 15));
		}

		[Test]
		public void CanRoundToSignificantDigits2()
		{
			Assert.AreEqual(-1.123,
			                MathUtils.RoundToSignificantDigits(-1.12300000000000000011, 15));
		}

		[Test]
		public void CanRoundToSignificantDigits3()
		{
			Assert.AreEqual(0.000125,
			                MathUtils.RoundToSignificantDigits(0.000125000000000000099, 15));
		}

		[Test]
		public void CanRoundToSignificantDigits4()
		{
			// Note the odd 99 at the end
			Assert.AreEqual(1.1234567891234599,
			                MathUtils.RoundToSignificantDigits(1.12345678912345678, 15));
		}

		[Test]
		public void CanRoundToSignificantDigits5()
		{
			Assert.AreEqual(0.0, MathUtils.RoundToSignificantDigits(12.34567, 0));
			Assert.AreEqual(10.0, MathUtils.RoundToSignificantDigits(12.34567, 1));
			Assert.AreEqual(12.0, MathUtils.RoundToSignificantDigits(12.34567, 2));
			Assert.AreEqual(12.3, MathUtils.RoundToSignificantDigits(12.34567, 3));
			Assert.AreEqual(12.35, MathUtils.RoundToSignificantDigits(12.34567, 4));
			Assert.AreEqual(12.346, MathUtils.RoundToSignificantDigits(12.34567, 5));
		}

		[Test]
		public void CanRoundToSignificantDigits6()
		{
			Assert.AreEqual(0.0, MathUtils.RoundToSignificantDigits(-123.4567, 0));
			Assert.AreEqual(-100.0, MathUtils.RoundToSignificantDigits(-123.4567, 1));
			Assert.AreEqual(-120.0, MathUtils.RoundToSignificantDigits(-123.4567, 2));
			Assert.AreEqual(-123.0, MathUtils.RoundToSignificantDigits(-123.4567, 3));
			Assert.AreEqual(-123.5, MathUtils.RoundToSignificantDigits(-123.4567, 4));
			Assert.AreEqual(-123.46, MathUtils.RoundToSignificantDigits(-123.4567, 5));
		}

		[Test]
		public void CanRoundToSignificantDigits7()
		{
			// Rounding 1283554.3404949855 to 8 significant figures gave
			// 1283554.29999998 with the old code; now it gives the expected result:

			Assert.AreEqual(1283554.3, MathUtils.RoundToSignificantDigits(1283554.3404949855, 8));
		}

		[Test]
		public void CanGetSignificanceEpsilonDouble()
		{
			Assert.AreEqual(1E-5, MathUtils.GetDoubleSignificanceEpsilon(1000000000d));
			Assert.AreEqual(1E-14, MathUtils.GetDoubleSignificanceEpsilon(1d));
		}

		[Test]
		public void CanGetSignificanceEpsilonFloat()
		{
			Assert.AreEqual(100f, MathUtils.GetFloatSignificanceEpsilon(1000000000f));
			Assert.AreEqual(1E-7f, MathUtils.GetFloatSignificanceEpsilon(1f));
		}

		[Test]
		public void CanCompareSignificantDigitsForDoubles()
		{
			Assert.True(MathUtils.AreSignificantDigitsEqual(1.0000000000001,
			                                                1.00000000000011));
			Assert.False(MathUtils.AreSignificantDigitsEqual(1.000000000001,
			                                                 1.0000000000011));
			Assert.False(MathUtils.AreSignificantDigitsEqual(0.0000000000000000001,
			                                                 0.00000000000000000011));
		}

		[Test]
		public void CanCompareSignificantDigitsForFloats()
		{
			Assert.True(MathUtils.AreSignificantDigitsEqual(1.0000001f, 1.00000011f));
			Assert.False(MathUtils.AreSignificantDigitsEqual(1.000001f, 1.0000011f));
		}

		[Test]
		public void CanCompareSignificantDigitsForZero()
		{
			Assert.True(MathUtils.AreSignificantDigitsEqual(0d, 0d));
		}

		[Test]
		public void CanCompareSignificantDigitsFastEnough()
		{
			var watch = new Stopwatch();
			watch.Start();

			var equal = false;
			const int count = 1000000;

			for (var i = 0; i < count; i++)
			{
				const double d1 = 1.0000000000001;
				const double d2 = 1.00000000000011;
				equal = d1.Equals(d2);
			}

			watch.Stop();

			double reference = watch.ElapsedMilliseconds / (double) count;

			Assert.False(equal);
			Console.WriteLine($@"{reference:F7} ms per comparison (double.Equals())");

			watch.Reset();
			watch.Start();

			for (var i = 0; i < count; i++)
			{
				equal = MathUtils.AreSignificantDigitsEqual(1.0000000000001, 1.00000000000011);
			}

			watch.Stop();
			Assert.True(equal);

			double compare = watch.ElapsedMilliseconds / (double) count;

			Console.WriteLine($@"{compare:F7} ms per comparison (significant digits)");
			Assert.Less(compare, 0.0001);
		}

		[Test]
		public void CanCompareToToleranceWithEpsilon()
		{
			Assert.True(MathUtils.IsWithinTolerance(100.1001d, 100.1d, 0.00011d));
			Assert.False(MathUtils.IsWithinTolerance(100.1001d, 100.1d, 0.00009d));
		}
	}
}
