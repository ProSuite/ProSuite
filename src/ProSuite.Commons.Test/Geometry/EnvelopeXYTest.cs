using System;
using NUnit.Framework;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geometry
{
	[TestFixture]
	public class EnvelopeXYTest
	{
		[Test]
		public void CanCreateString()
		{
			EnvelopeXY envelope = new EnvelopeXY(2600000.1234, 1200000.987654, 2601000.12,
			                                     1201000.98);
			string text = envelope.ToString();

			Console.WriteLine(text);

			Assert.AreEqual(
				"XMin: 2600000.1234 YMin: 1200000.987654 XMax: 2601000.12 YMax: 1201000.98", text);

			string formatted = envelope.Format(envelope, 2);

			Console.WriteLine(formatted);

			Assert.AreEqual(
				"XMin: 2'600'000.12 YMin: 1'200'000.99 XMax: 2'601'000.12 YMax: 1'201'000.98",
				formatted);
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
	}
}
