using NUnit.Framework;
using ProSuite.Commons.CommandLine;

namespace ProSuite.Commons.Test.CommandLine
{
	[TestFixture]
	public class ArgumentsTest
	{
		[Test]
		public void CanGetArgumentValue()
		{
			var args = new Arguments("-a xx -b yy");

			Assert.AreEqual("xx", args.GetValue("-a"));
			Assert.AreEqual("yy", args.GetValue("-b"));
			Assert.IsNull(args.GetValue("-c"));
		}

		[Test]
		public void CanGetIsArgumentSpecified()
		{
			var args = new Arguments("-a xx -b yy");

			Assert.IsTrue(args.Exists("-a"));
			Assert.IsTrue(args.Exists("-b"));
			Assert.IsFalse(args.Exists("-c"));
		}
	}
}