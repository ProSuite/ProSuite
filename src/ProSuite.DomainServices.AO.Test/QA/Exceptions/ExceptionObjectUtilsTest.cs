using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.Test.QA.Exceptions
{
	[TestFixture]
	public class ExceptionObjectUtilsTest
	{
		[Test]
		public void CanIgnoreInvolvedRowTable()
		{
			var involvedRows = new List<InvolvedRow>
			                   {
				                   new InvolvedRow("Table2", 100),
				                   new InvolvedRow("Table1", 1000),
				                   new InvolvedRow("Table2", 200)
			                   };

			string key = ExceptionObjectUtils.GetKey(involvedRows, t => t == "Table1");

			Console.WriteLine(key);

			Assert.AreEqual("Table2:100:200;", key);
		}

		[Test]
		public void CanIgnoreInvolvedTable()
		{
			var involvedTables = new List<InvolvedTable>
			                     {
				                     new InvolvedTable("Table1",
				                                       new[]
				                                       {
					                                       new OIDRowReference(100)
				                                       }),
				                     new InvolvedTable("Table2",
				                                       new[]
				                                       {
					                                       new OIDRowReference(200),
					                                       new OIDRowReference(100)
				                                       }),
			                     };

			string key = ExceptionObjectUtils.GetKey(involvedTables, t => t == "Table1");

			Console.WriteLine(key);

			Assert.AreEqual("Table2:100:200;", key);
		}

		[Test]
		public void CanGetEqualKeys()
		{
			AssertEqualKeys(new List<InvolvedRow>
			                {
				                new InvolvedRow("Table2", 100),
				                new InvolvedRow("Table1", 1000),
				                new InvolvedRow("Table2", 200)
			                });
		}

		[Test]
		public void CanGetEqualKeysNoRows()
		{
			AssertEqualKeys(new List<InvolvedRow> {new InvolvedRow("Table1")});
		}

		[Test]
		public void CanGetEqualKeysEmpty()
		{
			AssertEqualKeys(new List<InvolvedRow>());
		}

		[Test]
		public void CanGetInvolvedRowsKeyFastEnough()
		{
			var involvedRows = new List<InvolvedRow>
			                   {
				                   new InvolvedRow("Table2", 100),
				                   new InvolvedRow("Table1", 1000),
				                   new InvolvedRow("Table2", 200)
			                   };

			AssertFastEnough(() => ExceptionObjectUtils.GetKey(involvedRows), 10000, 0.1);
		}

		[Test]
		public void CanGetInvolvedTablesKeyFastEnough()
		{
			var involvedRows = new List<InvolvedRow>
			                   {
				                   new InvolvedRow("Table2", 100),
				                   new InvolvedRow("Table1", 1000),
				                   new InvolvedRow("Table2", 200)
			                   };
			IEnumerable<InvolvedTable> involvedTables =
				IssueUtils.GetInvolvedTables(involvedRows);

			AssertFastEnough(() => ExceptionObjectUtils.GetKey(involvedTables), 10000, 0.1);
		}

		private static void AssertFastEnough([NotNull] Action procedure,
		                                     int count,
		                                     double maximumMilliseconds)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			for (int i = 0; i < count; i++)
			{
				procedure();
			}

			stopwatch.Stop();
			long milliseconds = stopwatch.ElapsedMilliseconds;

			double millisecondsPerOperation = (double) milliseconds / count;
			Console.WriteLine(@"{0} ms", millisecondsPerOperation);

			Assert.LessOrEqual(millisecondsPerOperation, maximumMilliseconds);
		}

		private static void AssertEqualKeys([NotNull] ICollection<InvolvedRow> involvedRows)
		{
			string involvedRowsKey = ExceptionObjectUtils.GetKey(involvedRows);

			string involvedTablesKey = ExceptionObjectUtils.GetKey(
				IssueUtils.GetInvolvedTables(involvedRows));

			Console.WriteLine(@"Involved rows key:   [{0}]", involvedRowsKey);
			Console.WriteLine(@"Involved tables key: [{0}]", involvedTablesKey);

			Assert.AreEqual(involvedRowsKey, involvedTablesKey);
		}
	}
}
