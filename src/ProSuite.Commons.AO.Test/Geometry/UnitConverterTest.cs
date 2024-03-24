using System;
using ESRI.ArcGIS.esriSystem;
using NUnit.Framework;

namespace ProSuite.Commons.AO.Test.Geometry
{
	[TestFixture]
	public class UnitConverterTest
	{
		#region Setup/Teardown

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		#endregion

		[Test]
		public void ShowUnitsAsString()
		{
			IUnitConverter converter = new UnitConverterClass();

			foreach (esriUnits units in Enum.GetValues(typeof(esriUnits)))
			{
				string singular = converter.EsriUnitsAsString(
					units, esriCaseAppearance.esriCaseAppearanceLower, false);
				string plural = converter.EsriUnitsAsString(
					units, esriCaseAppearance.esriCaseAppearanceLower, true);

				Console.WriteLine(@"{0}: 1 {1}, 3 {2}", units, singular, plural);
			}
		}

		[Test]
		public void ShowUnits()
		{
			IUnitConverter converter = new UnitConverterClass();

			Console.WriteLine(@"1 pt = {0} mm",
			                  converter.ConvertUnits(1, esriUnits.esriPoints,
			                                         esriUnits.esriMillimeters));
			Console.WriteLine(@"1 pt = {0} in",
			                  converter.ConvertUnits(1, esriUnits.esriPoints,
			                                         esriUnits.esriInches));
			Console.WriteLine(@"1 mm = {0} pt",
			                  converter.ConvertUnits(1, esriUnits.esriMillimeters,
			                                         esriUnits.esriPoints));
			Console.WriteLine(@"1 mm = {0} in",
			                  converter.ConvertUnits(1, esriUnits.esriMillimeters,
			                                         esriUnits.esriInches));
			Console.WriteLine(@"1 in = {0} mm",
			                  converter.ConvertUnits(1, esriUnits.esriInches,
			                                         esriUnits.esriMillimeters));
			Console.WriteLine(@"1 in = {0} pt",
			                  converter.ConvertUnits(1, esriUnits.esriInches,
			                                         esriUnits.esriPoints));
		}

		[Test]
		public void CanConvertUnits()
		{
			const double ptPerIn = 72;
			const double inPerPt = 0.0138888888888889;
			const double mmPerIn = 25.4000508001016;
			const double inPerMm = 0.03937;
			const double ptPerMm = 2.83464;
			const double mmPerPt = 0.352778483334744;

			IUnitConverter converter = new UnitConverterClass();

			// Check some basic assumptions about unit conversion:

			Expect(converter, 1, esriUnits.esriMeters, 1000, esriUnits.esriMillimeters);
			Expect(converter, 1, esriUnits.esriMeters, 100, esriUnits.esriCentimeters);
			Expect(converter, 1, esriUnits.esriMeters, 1.0 / 1000, esriUnits.esriKilometers);

			Expect(converter, 100, esriUnits.esriInches, 100 * mmPerIn,
			       esriUnits.esriMillimeters);
			Expect(converter, 100, esriUnits.esriMillimeters, 100 * inPerMm,
			       esriUnits.esriInches);

			Expect(converter, 100, esriUnits.esriInches, 100 * ptPerIn, esriUnits.esriPoints);
			Expect(converter, 100, esriUnits.esriPoints, 100 * inPerPt, esriUnits.esriInches);

			Expect(converter, 100, esriUnits.esriPoints, 100 * mmPerPt,
			       esriUnits.esriMillimeters);
			Expect(converter, 100, esriUnits.esriMillimeters, 100 * ptPerMm,
			       esriUnits.esriPoints);
		}

		private static void Expect(IUnitConverter converter,
		                           double a, esriUnits aUnits, double b, esriUnits bUnits)
		{
			double aPrime = converter.ConvertUnits(a, aUnits, bUnits);
			double bPrime = converter.ConvertUnits(b, bUnits, aUnits);

			const int significantDigits = 4;

			a = Math.Round(a, significantDigits);
			b = Math.Round(b, significantDigits);
			aPrime = Math.Round(aPrime, significantDigits);
			bPrime = Math.Round(bPrime, significantDigits);

			Assert.AreEqual(b, aPrime);
			Assert.AreEqual(a, bPrime);
		}
	}
}
