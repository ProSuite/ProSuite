using NUnit.Framework;
using ProSuite.Commons.CommandLine;

namespace ProSuite.Commons.Test.CommandLine
{
	[TestFixture]
	public class CommandArgumentsTest
	{
		[Test]
		public void CanPrintUsageRequired()
		{
			var writer = new UsageWriter(UsageTarget.Console);
			string usage = writer.WriteArgUsage("-y", "the message", false, 4);

			Assert.AreEqual("    -y the message", usage);
		}

		[Test]
		public void CanPrintUsageOptional()
		{
			var writer = new UsageWriter(UsageTarget.Console);
			string usage = writer.WriteArgUsage("-y", "the message", true, 4);

			Assert.AreEqual("    [-y] the message", usage);
		}
	}
}