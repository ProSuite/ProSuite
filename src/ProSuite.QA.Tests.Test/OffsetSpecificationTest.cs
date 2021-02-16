using System;
using System.Globalization;
using NUnit.Framework;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class OffsetSpecificationTest
	{
		[Test]
		public void CanParseNullOffset()
		{
			Assert.IsNull(OffsetSpecification.Parse(null, CultureInfo.InvariantCulture));
		}

		[Test]
		public void CanParseEmptyOffset()
		{
			Assert.IsNull(OffsetSpecification.Parse(" ", CultureInfo.InvariantCulture));
		}

		[Test]
		public void CanParseSimpleOffset()
		{
			const double value = 123.123456789;

			CultureInfo formatProvider = CultureInfo.InvariantCulture;

			string offset = string.Format(formatProvider, "{0}", value);

			OffsetSpecification offsetSpecification = OffsetSpecification.Parse(offset,
			                                                                    formatProvider);

			Assert.IsNotNull(offsetSpecification);

			Assert.False(offsetSpecification.IsPercentage);
			Assert.AreEqual(value, offsetSpecification.OffsetValue);
		}

		[Test]
		public void CanParseNegativeOffset()
		{
			const double value = 123.123456789;

			CultureInfo formatProvider = CultureInfo.InvariantCulture;

			string offset = string.Format(formatProvider, "-{0}", value);

			OffsetSpecification offsetSpecification = OffsetSpecification.Parse(offset,
			                                                                    formatProvider);

			Assert.IsNotNull(offsetSpecification);

			Assert.False(offsetSpecification.IsPercentage);
			Assert.AreEqual(value * -1, offsetSpecification.OffsetValue);
		}

		[Test]
		public void CanParsePercentageOffset()
		{
			const double value = 123.123456789;

			CultureInfo formatProvider = CultureInfo.InvariantCulture;

			string offset = string.Format(formatProvider, "{0}%", value);

			OffsetSpecification offsetSpecification = OffsetSpecification.Parse(offset,
			                                                                    formatProvider);

			Assert.IsNotNull(offsetSpecification);

			Assert.True(offsetSpecification.IsPercentage);
			Assert.AreEqual(value, offsetSpecification.OffsetValue);
		}

		[Test]
		public void CanParseNegativePercentageOffset()
		{
			const double value = 123.123456789;

			CultureInfo formatProvider = CultureInfo.InvariantCulture;

			string offset = string.Format(formatProvider, "-{0}%", value);

			OffsetSpecification offsetSpecification = OffsetSpecification.Parse(offset,
			                                                                    formatProvider);

			Assert.IsNotNull(offsetSpecification);

			Assert.True(offsetSpecification.IsPercentage);
			Assert.AreEqual(value * -1, offsetSpecification.OffsetValue);
		}

		[Test]
		public void CanParseNegativePercentageOffsetWithBlanks()
		{
			const double value = 123.123456789;

			CultureInfo formatProvider = CultureInfo.InvariantCulture;

			string offset = string.Format(formatProvider, " -  {0} %  ", value);

			OffsetSpecification offsetSpecification = OffsetSpecification.Parse(offset,
			                                                                    formatProvider);

			Assert.IsNotNull(offsetSpecification);

			Assert.True(offsetSpecification.IsPercentage);
			Assert.AreEqual(value * -1, offsetSpecification.OffsetValue);
		}

		[Test]
		public void CantParseInvalidOffset1()
		{
			Assert.Throws<ArgumentException>(
				() => OffsetSpecification.Parse("+", CultureInfo.InvariantCulture));
		}

		[Test]
		public void CantParseInvalidOffset2()
		{
			Assert.Throws<ArgumentException>(
				() => OffsetSpecification.Parse("%", CultureInfo.InvariantCulture));
		}

		[Test]
		public void CantParseInvalidOffset3()
		{
			Assert.Throws<ArgumentException>(
				() => OffsetSpecification.Parse("-%", CultureInfo.InvariantCulture));
		}

		[Test]
		public void CantParseInvalidOffset4()
		{
			Assert.Throws<ArgumentException>(
				() => OffsetSpecification.Parse("-a%", CultureInfo.InvariantCulture));
		}

		[Test]
		public void CanApplyNegativePercentageOffset()
		{
			var offsetSpecification = new OffsetSpecification(-10, true);

			Assert.AreEqual(90, offsetSpecification.ApplyTo(100));
		}

		[Test]
		public void CanApplyNegativePercentageOffset2()
		{
			var offsetSpecification = new OffsetSpecification(-100, true);

			Assert.AreEqual(0, offsetSpecification.ApplyTo(100));
		}

		[Test]
		public void CanApplySimpleOffset()
		{
			var offsetSpecification = new OffsetSpecification(50);

			Assert.AreEqual(150, offsetSpecification.ApplyTo(100));
		}
	}
}
