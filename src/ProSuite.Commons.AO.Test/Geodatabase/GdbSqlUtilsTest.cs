using System;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	[Category(TestCategory.Sde)]
	public class GdbSqlUtilsTest
	{
		[Test]
		public void CanGetOracleDateLiteral()
		{
			var dateTime = new DateTime(2001, 12, 31, 23, 59, 59, 99);

			string literal = GdbSqlUtils.GetOracleDateLiteral(dateTime);

			Assert.AreEqual("TO_DATE('2001-12-31 23:59:59', 'YYYY-MM-DD HH24:MI:SS')", literal);
		}

		[Test]
		public void CanGetSqlServerDateLiteral()
		{
			var dateTime = new DateTime(2001, 12, 31, 23, 59, 59, 99);

			string literal = GdbSqlUtils.GetSqlServerDateLiteral(dateTime);

			Assert.AreEqual("'2001-12-31 23:59:59'", literal);
		}

		[Test]
		public void CanGetPostgreSQLDateLiteral()
		{
			var dateTime = new DateTime(2001, 12, 31, 23, 59, 59, 99);

			string literal = GdbSqlUtils.GetPostgreSQLDateLiteral(dateTime);

			Assert.AreEqual("'2001-12-31 23:59:59'", literal);
		}

		[Test]
		public void CanGetPGDBDateLiteral()
		{
			var dateTime = new DateTime(2001, 12, 31, 23, 59, 59, 99);

			string literal = GdbSqlUtils.GetPGDBDateLiteral(dateTime);

			Assert.AreEqual("#12-31-2001 23:59:59#", literal);
		}

		[Test]
		public void CanGetFGDBDateLiteral()
		{
			var dateTime = new DateTime(2001, 12, 31, 23, 59, 59, 99);

			string literal = GdbSqlUtils.GetFGDBDateLiteral(dateTime);

			Assert.AreEqual("date '2001-12-31 23:59:59'", literal);
		}
	}
}
