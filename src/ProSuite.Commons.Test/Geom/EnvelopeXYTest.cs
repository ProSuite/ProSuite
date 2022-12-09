using System;
using System.Globalization;
using NUnit.Framework;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Globalization;

namespace ProSuite.Commons.Test.Geom
{
	[TestFixture]
	public class EnvelopeXYTest
	{
		[Test]
		public void CanCreateStringInvariant()
		{
			EnvelopeXY envelope = new EnvelopeXY(2600000.1234, 1200000.987654, 2601000.12,
			                                     1201000.98);

			// This produces the closest to double.ToString()
			string invariantString = envelope.ToString();

			Assert.AreEqual(
				"XMin: 2600000.1234 YMin: 1200000.987654 XMax: 2601000.12 YMax: 1201000.98",
				invariantString);

			// Use null culture to force the F:1 formatting:
			invariantString = envelope.ToString(null, 1);

			Assert.AreEqual(
				"XMin: 2600000.1 YMin: 1200001.0 XMax: 2601000.1 YMax: 1201001.0",
				invariantString);

			// This estimates the decimal digits to 3:
			invariantString = envelope.ToString(null);

			Assert.AreEqual(
				"XMin: 2600000.123 YMin: 1200000.988 XMax: 2601000.120 YMax: 1201000.980",
				invariantString);

			// Invariant Culture:
			// NOTE: The default double.ToString() produces no thousand separators,
			// but using the invariant culture does:
			invariantString = envelope.ToString(CultureInfo.InvariantCulture, 2);

			Assert.AreEqual(
				"XMin: 2,600,000.12 YMin: 1,200,000.99 XMax: 2,601,000.12 YMax: 1,201,000.98",
				invariantString);
		}

		[Test]
		public void CanCreateStringDeCh()
		{
			EnvelopeXY envelope = new EnvelopeXY(2600000.1234, 1200000.987654, 2601000.12,
			                                     1201000.98);

			const string deCh = "de-CH";

			// setting current culture temporarily to de-CH:
			// The default decimal digits for cartesian coordinates is 3:
			string stringInDeCh = ToStringInCulture(deCh, envelope);
			Assert.AreEqual(
				"XMin: 2’600’000.123 YMin: 1’200’000.988 XMax: 2’601’000.120 YMax: 1’201’000.980",
				stringInDeCh);

			// Using culture as parameter instead current culture:
			CultureInfo deChCulture = CultureInfo.GetCultureInfo(deCh);

			string deChString = envelope.ToString(deChCulture);
			Console.WriteLine(deChString);

			Assert.AreEqual(stringInDeCh, deChString);

			// Now with significant digits:
			stringInDeCh = envelope.ToString(deChCulture, 2);
			Assert.AreEqual(
				"XMin: 2’600’000.12 YMin: 1’200’000.99 XMax: 2’601’000.12 YMax: 1’201’000.98",
				stringInDeCh);
		}

		[Test]
		public void CanCreateStringDeDe()
		{
			EnvelopeXY envelope = new EnvelopeXY(2600000.1234, 1200000.987654, 2601000.12,
			                                     1201000.98);

			const string deDe = "de-DE";

			// setting current culture temporarily to de-CH:
			// The default decimal digits for cartesian coordinates is 3:
			string stringInDeDe = ToStringInCulture(deDe, envelope);
			Assert.AreEqual(
				"XMin: 2.600.000,123 YMin: 1.200.000,988 XMax: 2.601.000,120 YMax: 1.201.000,980",
				stringInDeDe);

			// Using culture as parameter instead current culture:
			CultureInfo deDeCulture = CultureInfo.GetCultureInfo(deDe);

			string deDeString = envelope.ToString(deDeCulture);
			Console.WriteLine(deDeString);

			Assert.AreEqual(stringInDeDe, deDeString);

			// Now with significant digits:
			stringInDeDe = envelope.ToString(deDeCulture, 2);
			Assert.AreEqual(
				"XMin: 2.600.000,12 YMin: 1.200.000,99 XMax: 2.601.000,12 YMax: 1.201.000,98",
				stringInDeDe);
		}

		[Test]
		public void CanUnion()
		{
			EnvelopeXY envelope1 = new EnvelopeXY(2600000.1234, 1200000.987654, 2601000.12,
			                                      1201000.98);

			EnvelopeXY envelope2 = new EnvelopeXY(2600050, 1200050, 2601111.12,
			                                      1201222.98);

			envelope1.EnlargeToInclude(envelope2);

			Assert.IsTrue(envelope1.Equals(
				              new EnvelopeXY(2600000.1234, 1200000.987654,
				                             2601111.12, 1201222.98)));

			EnvelopeXY envelope3 = new EnvelopeXY(2500000.1234, 1100000.987654,
			                                      2600050, 1200050);

			envelope1.EnlargeToInclude(envelope3);

			Assert.IsTrue(envelope1.Equals(
				              new EnvelopeXY(2500000.1234, 1100000.987654,
				                             2601111.12, 1201222.98)));
		}

		[Test]
		public void CanCompareEquality()
		{
			double xMin = 2600000.1234;
			EnvelopeXY envelope1 = new EnvelopeXY(xMin, 1200000.987654, 2601000.12,
			                                      1201000.98);

			EnvelopeXY envelope2 = new EnvelopeXY(xMin - 0.0001, 1200000.987654, 2601000.12,
			                                      1201000.98);

			Assert.IsFalse(envelope1.Equals(envelope2));
			Assert.IsTrue(envelope1.Equals(envelope2, 0.0001));
		}

		private static string ToStringInCulture(string deCh, EnvelopeXY envelope)
		{
			string formatted = null;
			CultureInfoUtils.ExecuteUsing(
				CultureInfo.GetCultureInfo(deCh),
				() =>
				{
					string cultureInfo =
						CultureInfoUtils.GetCultureInfoDescription(CultureInfo.CurrentCulture);

					formatted = envelope.ToString(CultureInfo.CurrentCulture);

					Console.WriteLine(formatted);
					Console.WriteLine("... formatted using {0}", cultureInfo);
				});
			return formatted;
		}
	}
}
