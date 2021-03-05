using NUnit.Framework;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Test
{
	[TestFixture]
	public class NumberUtilsTest
	{
		[Test]
		public void CanToPositiveDegrees()
		{
			Assert.AreEqual(NumberUtils.ToPositiveDegrees(0.0), 0.0);

			Assert.AreEqual(NumberUtils.ToPositiveDegrees(1.0), 1.0);
			Assert.AreEqual(NumberUtils.ToPositiveDegrees(360.0), 0.0);
			Assert.AreEqual(NumberUtils.ToPositiveDegrees(361.0), 1.0);
			Assert.AreEqual(NumberUtils.ToPositiveDegrees(720.0), 0.0);
			Assert.AreEqual(NumberUtils.ToPositiveDegrees(721.0), 1.0);

			Assert.AreEqual(NumberUtils.ToPositiveDegrees(-1.0), 359.0);
			Assert.AreEqual(NumberUtils.ToPositiveDegrees(-360.0), 0.0);
			Assert.AreEqual(NumberUtils.ToPositiveDegrees(-361.0), 359.0);
			Assert.AreEqual(NumberUtils.ToPositiveDegrees(-720.0), 0.0);
			Assert.AreEqual(NumberUtils.ToPositiveDegrees(-721.0), 359.0);
		}

		[Test]
		public void CanIsFinite()
		{
			Assert.False(double.NaN.IsFinite());
			Assert.False(double.NegativeInfinity.IsFinite());
			Assert.False(double.PositiveInfinity.IsFinite());
			Assert.True(1.23.IsFinite());
			Assert.True((-1.23e-12).IsFinite());
			Assert.True(double.Epsilon.IsFinite());
		}

		[Test]
		public void CanClamp()
		{
			Assert.AreEqual(5, 5.Clamp(1, 9, "test"));
			Assert.AreEqual(9, 12.Clamp(1, 9, "test"));
			Assert.AreEqual(1, (-2).Clamp(1, 9, "test"));

			Assert.AreEqual(12.3, 12.3.Clamp(1.1, 22.2, "test"));
			Assert.AreEqual(1.1, 0.99.Clamp(1.1, 22.2, "test"));
			Assert.AreEqual(22.2, 1e5.Clamp(1.1, 22.2, "test"));
		}
	}
}
