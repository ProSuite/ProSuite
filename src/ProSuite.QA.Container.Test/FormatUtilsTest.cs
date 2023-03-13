using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.QA.Container.Test
{
	[TestFixture]
	public class FormatUtilsTest
	{
		[OneTimeSetUp]
		public void TestFixtureSetUp()
		{
			Commons.AO.Test.TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TestFixtureTearDown()
		{
			Commons.AO.Test.TestUtils.ReleaseLicense();
		}

		[Test]
		public void FormatComparisonTest()
		{
			CultureInfo origCulture = Thread.CurrentThread.CurrentCulture;

			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				string expression;

				expression = FormatUtils.FormatComparison("N0", 1.4, 2.3, "<", "{0} {1} {2}");
				Assert.AreEqual("1 < 2", expression);

				expression = FormatUtils.FormatComparison("N0", 0.8, 1.1, "<", "{0} {1} {2}");
				Assert.AreEqual("0.8 < 1.1", expression);

				expression = FormatUtils.FormatComparison("N0", 0.3, 0.6, "<", "{0} {1} {2}");
				Assert.AreEqual("0.3 < 0.6", expression);

				expression = FormatUtils.FormatComparison("N0", Math.E, Math.PI, "<",
				                                          "{0} {1} {2}");
				Assert.AreEqual("2.7 < 3.1", expression);

				expression = FormatUtils.FormatComparison("N0", Math.PI, 355.0 / 113.0, "<",
				                                          "{0} {1} {2}");
				Assert.AreEqual("3.1415927 < 3.1415929", expression);

				expression = FormatUtils.FormatComparison("N0", 4.998, 5, "<", "{0} {1} {2}");
				Assert.AreEqual("4.998 < 5.000", expression);

				expression = FormatUtils.FormatComparison("N0", 3.93, 3.95, "<", "{0} {1} {2}");
				Assert.AreEqual("3.93 < 3.95", expression);

				expression = FormatUtils.FormatComparison("N0", 3.95, 3.94, ">", "{0} {1} {2}");
				Assert.AreEqual("3.95 > 3.94", expression);

				expression = FormatUtils.FormatComparison("N0", 3.99, 3.94, ">", "{0} {1} {2}");
				Assert.AreEqual("4.0 > 3.9", expression);

				expression = FormatUtils.FormatComparison("E0", 3.33e-50, 3.34e-50, "<",
				                                          "{0} {1} {2}");
				Assert.AreEqual("3.33E-050 < 3.34E-050", expression);

				expression = FormatUtils.FormatComparison("N0", 0.0314, 0.5, "<", "{0} {1} {2}");
				Assert.AreEqual("0.03 < 0.5", expression);

				expression = FormatUtils.FormatComparison("N0", 0.00294, 0.5, "<", "{0} {1} {2}");
				Assert.AreEqual("0.003 < 0.5", expression);

				expression = FormatUtils.FormatComparison("N0", 0.0003294, 0.5, "<", "{0} {1} {2}");
				Assert.AreEqual("0.0003 < 0.5", expression);

				expression = FormatUtils.FormatComparison("N0", 0.00003294, 0.5, "<",
				                                          "{0} {1} {2}");
				Assert.AreEqual("3.3E-5 < 0.5", expression);

				// Changed behaviour in .NET 6: either they changed to bankers rounding or
				// (more likely) the internal representation of the double is rounded, which
				// most likely is a very small amount above or below the provided input.
				// See also https://github.com/dotnet/runtime/issues/1640
				// TOP-3936:
				expression = FormatUtils.FormatComparison("N0", 1227.5, 1250.0, "<", "{0} {1} {2}");
				Assert.AreEqual("1,228 < 1,250", expression);

				expression = FormatUtils.FormatComparison("N0", double.NegativeInfinity, 0.1, "<",
				                                          "{0} {1} {2}");
				Assert.AreEqual("-Infinity < 0.1", expression);

				expression = FormatUtils.FormatComparison("N0", 3000.0, double.PositiveInfinity,
				                                          "<",
				                                          "{0} {1} {2}");
				Assert.AreEqual("3,000 < Infinity", expression);

				// TODO: Revise
				expression = FormatUtils.FormatComparison("N0", double.NaN, 0.1, "<",
				                                          "{0} {1} {2}");
				Assert.AreEqual("NaN < 0.1", expression);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = origCulture;
			}
		}

		[Test]
		public void CompareFormatTest()
		{
			CultureInfo origCulture = Thread.CurrentThread.CurrentCulture;

			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				Assert.AreEqual("N0", FormatUtils.CompareFormat(double.NaN, "<", 0.0008, "N0"));
				Assert.AreEqual("N0", FormatUtils.CompareFormat(0.0001, "<", double.NaN, "N0"));

				Assert.AreEqual("N0",
				                FormatUtils.CompareFormat(double.NegativeInfinity, "<", 0.0008,
				                                          "N0"));
				Assert.AreEqual("N0",
				                FormatUtils.CompareFormat(0.0001, "<", double.PositiveInfinity,
				                                          "N0"));

				Assert.AreEqual("N1", FormatUtils.CompareFormat(0.5, "<", 1, "N0"));

				Assert.AreEqual("N1", FormatUtils.CompareFormat(0.3, "<", 0.6, "N0"));

				Assert.AreEqual("N2", FormatUtils.CompareFormat(3.95, ">", 3.94, "N0"));

				Assert.AreEqual("N3", FormatUtils.CompareFormat(0.007, "<", 0.008, "N0"));

				Assert.AreEqual("N4", FormatUtils.CompareFormat(0.0007, "<", 0.0008, "N0"));

				Assert.AreEqual("N4", FormatUtils.CompareFormat(0.00001, "<", 0.0008, "N0"));
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = origCulture;
			}
		}
	}
}
