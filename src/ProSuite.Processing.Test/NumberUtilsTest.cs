using NUnit.Framework;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Test
{
	[TestFixture]
	public class NumberUtilsTest
	{
		[Test]
		public void ToPositiveDegreesTest()
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
	}
}
