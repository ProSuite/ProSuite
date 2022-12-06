using System.Text;
using NUnit.Framework;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Test
{
	[TestFixture]
	public class ProcessingUtilsTest
	{
		[Test]
		public void CanAppendScale()
		{
			const string sep = "'";
			var sb = new StringBuilder();

			sb.Append("Scales are ").AppendScale(500);
			var o = sb.Append(" and ").AppendScale(1000, sep);
			sb.Append(" and ").AppendScale(1000 * 1000, sep);

			Assert.AreSame(sb, o);
			Assert.AreEqual("Scales are 1:500 and 1:1'000 and 1:1'000'000", sb.ToString());

			var x1 = $"1:{1234.5}"; // use current culture
			Assert.AreEqual(x1, sb.Clear().AppendScale(1234.5, sep).ToString());

			var x2 = $"1:{0.025}"; // use current culture
			Assert.AreEqual(x2, sb.Clear().AppendScale(0.025, sep).ToString());

			var r3 = sb.Clear().AppendScale(25000).ToString();
			Assert.AreEqual("1:25\u2009000", r3); // default sep is THIN SPACE
		}
	}
}
