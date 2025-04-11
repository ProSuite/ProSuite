using System.Globalization;
using NUnit.Framework;

namespace ProSuite.Commons.Test
{
	[TestFixture]
	public class DimensionTest
	{
		[Test]
		public void CanCreate()
		{
			var dim0 = new Dimension(0.0, null);
			Assert.AreEqual(0.0, dim0.Value);
			Assert.IsNull(dim0.Unit);

			var dim1 = new Dimension(1.25, "mm");
			Assert.AreEqual(1.25, dim1.Value);
			Assert.AreEqual("mm", dim1.Unit);

			var dim2 = new Dimension(-27.5, "");
			Assert.AreEqual(-27.5, dim2.Value);
			Assert.IsNull(dim2.Unit); // sic (empty and blank map to null)

			var dim3 = new Dimension(double.NaN, null);
			Assert.True(double.IsNaN(dim3.Value));

			var dim4 = new Dimension(1.875, "  padded  ");
			Assert.AreEqual(1.875, dim4.Value);
			Assert.AreEqual("padded", dim4.Unit); // unit is trimmed

			var dim5 = new Dimension(); // compiler generated default constructor
			Assert.AreEqual(0.0, dim5.Value);
			Assert.IsNull(dim5.Unit);
		}

		[Test]
		public void CanEquality()
		{
			var a = new Dimension(1.25, "cm");
			var b = new Dimension(12.5, "mm");
			var c = new Dimension(12.5, "pt");
			var d = new Dimension(12.5, null);
			var aa = new Dimension(a.Value, a.Unit);

			Assert.True(a.Equals(aa));
			Assert.True(a == aa);

			Assert.False(a.Equals(b)); // no conversion
			Assert.True(a != b);

			Assert.False(b.Equals(c)); // unit differs
			Assert.False(b == c);
			Assert.True(b != c);

			Assert.False(c.Equals(d)); // unit vs no unit
			Assert.False(c == d);
			Assert.True(c != d);
		}

		[Test]
		public void CanParse()
		{
			var invariant = CultureInfo.InvariantCulture;

			var dim1 = Dimension.Parse(null, invariant);
			Assert.AreEqual(0.0, dim1.Value);
			Assert.IsNull(dim1.Unit);

			var dim2 = Dimension.Parse("0", invariant);
			Assert.AreEqual(0.0, dim2.Value);
			Assert.IsNull(dim2.Unit);

			var dim3 = Dimension.Parse("  -1234.5678   foo bar  ", invariant);
			Assert.AreEqual(-1234.5678, dim3.Value);
			Assert.AreEqual("foo bar", dim3.Unit);

			var dim4 = Dimension.Parse("+0.125mm", invariant);
			Assert.AreEqual(0.125, dim4.Value);
			Assert.AreEqual("mm", dim4.Unit);

			var dim5 = Dimension.Parse("NaN", invariant);
			Assert.IsNaN(dim5.Value);
			Assert.IsNull(dim5.Unit);

			var dim6 = Dimension.Parse("NaN unit", invariant);
			Assert.IsNaN(dim6.Value);
			Assert.AreEqual("unit", dim6.Unit);

			var dim7 = Dimension.Parse("  Infinity  whatever  ", invariant);
			Assert.IsTrue(double.IsPositiveInfinity(dim7.Value));
			Assert.AreEqual("whatever", dim7.Unit);

			var dim8 = Dimension.Parse("-Infinity quux", invariant);
			Assert.IsTrue(double.IsNegativeInfinity(dim8.Value));
			Assert.AreEqual("quux", dim8.Unit);
		}

		[Test]
		public void CanToString()
		{
			var invariant = CultureInfo.InvariantCulture;

			Assert.AreEqual("0", new Dimension().ToString(invariant));
			Assert.AreEqual("-1.2 peanuts", new Dimension(-1.2, "peanuts").ToString(invariant));
			Assert.AreEqual("2.75", new Dimension(2.75, null).ToString(invariant));
			Assert.AreEqual("Infinity km", new Dimension(double.PositiveInfinity, "km").ToString(invariant));
			Assert.AreEqual("-Infinity km", new Dimension(double.NegativeInfinity, "km").ToString(invariant));
			// For NaN, the unit is irrelevant and omitted:
			Assert.AreEqual("NaN", new Dimension(double.NaN, "foo").ToString(invariant));
		}
	}
}
