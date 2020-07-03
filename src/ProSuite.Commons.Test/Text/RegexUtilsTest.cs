using System.Text.RegularExpressions;
using NUnit.Framework;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.Test.Text
{
	[TestFixture]
	public class RegexUtilsTest
	{
		[Test]
		public void CanMatchWildcard()
		{
			const bool matchCase = true;
			Regex regex = RegexUtils.GetWildcardMatchRegex("abc*123", matchCase);

			Assert.IsTrue(regex.IsMatch("abc___123"), "match expected");
			Assert.IsTrue(regex.IsMatch("abc123"), "match expected");
			Assert.IsTrue(regex.IsMatch("abc123x"), "match expected");
			Assert.IsTrue(regex.IsMatch("_abc123"), "match expected");

			Assert.IsFalse(regex.IsMatch("ABC_123"), "unexpected match");
		}

		[Test]
		public void CanMatchWildcardCompleteString()
		{
			const bool matchCase = true;
			Regex regex = RegexUtils.GetWildcardMatchRegex("abc*123", matchCase, true);

			Assert.IsTrue(regex.IsMatch("abc___123"), "match expected");
			Assert.IsTrue(regex.IsMatch("abc123"), "match expected");

			Assert.IsFalse(regex.IsMatch("abc123x"), "unexpected match");
			Assert.IsFalse(regex.IsMatch("_abc123"), "unexpected match");
			Assert.IsFalse(regex.IsMatch("ABC_123"), "unexpected match");
		}

		// example expressions from projects, here to serve as reference for new expressions

		[Test]
		public void CanMatchStructuredId()
		{
			var regex = new Regex(@"^\d{6}-[a-zA-Z0-9]{2}-\d{2}-[a-zA-Z0-9]{2}-\d{2}$");

			Assert.IsTrue(regex.IsMatch("123456-AB-78-CD-90"), "match expected");

			Assert.IsFalse(regex.IsMatch("123456-AB-78-CD-90 "), "unexpected match");
			Assert.IsFalse(regex.IsMatch("123456 AB-78-CD-90"), "unexpected match");
			Assert.IsFalse(regex.IsMatch("12 456-AB-78-CD-90"), "unexpected match");
			Assert.IsFalse(regex.IsMatch(" 123456-AB-78-CD-90"), "unexpected match");
		}
	}
}