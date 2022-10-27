using System.Linq;
using NUnit.Framework;

namespace ProSuite.Processing.Test
{
	[TestFixture]
	public class CartoProcessConfigTest
	{
		[Test]
		public void CanParseConfigText()
		{
			var c1 = CartoProcessConfig.FromText("empty", string.Empty);
			Assert.AreEqual(0, c1.Count);

			var c2 = CartoProcessConfig.FromText("eol", "a=b\nc=d\r\ne=f\ng=h");
			Assert.AreEqual(4, c2.Count);
			Assert.AreEqual("b", c2.GetString("a"));
			Assert.AreEqual("d", c2.GetString("c"));
			Assert.AreEqual("f", c2.GetString("e"));
			Assert.AreEqual("h", c2.GetString("g"));

			Assert.IsNull(c2.GetString("x", null));
			Assert.AreEqual("default", c2.GetString("x", "default"));

			var c3 = CartoProcessConfig.FromText("real1", "InputDataset = MyPolylines\r\nMaxAngle = 42.1\r\n");
			Assert.AreEqual(2, c3.Count);
			Assert.AreEqual("MyPolylines", c3.GetString("InputDataset"));
			Assert.AreEqual(42.1, c3.GetValue<double>("MaxAngle"));
		}

		[Test]
		public void CanGetEmptyValue()
		{
			var config = CartoProcessConfig.FromText("test", "foo=\n bar = \t \n");

			Assert.AreEqual(2, config.Count);

			Assert.AreEqual(string.Empty, config.GetString("foo"));
			Assert.AreEqual("default", config.GetValue("foo", "default"));

			Assert.AreEqual(string.Empty, config.GetString("bar"));
			Assert.AreEqual("default", config.GetValue("bar", "default"));
		}

		[Test]
		public void CanGetMultiValue()
		{
			var config = CartoProcessConfig.FromText(
				"test", "foo=a\nbar=11\n bar = 22 \n baz = 'str' \n bar = 33\n");

			Assert.AreEqual(5, config.Count);

			var values = config.GetValues<int>("bar").ToList();
			Assert.AreEqual(3, values.Count);
			Assert.AreEqual(11, values[0]);
			Assert.AreEqual(22, values[1]);
			Assert.AreEqual(33, values[2]);
		}
	}
}
