using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Issues;
using AssertionException = ProSuite.Commons.Essentials.Assertions.AssertionException;

namespace ProSuite.DomainServices.AO.Test.QA.Issues
{
	[TestFixture]
	public class IssueUtilsTest
	{
		[Test]
		public void CanFormatInvolvedTablesWithOids()
		{
			var involvedTables =
				new List<InvolvedTable>
				{
					new InvolvedTable(
						"Table1",
						new[]
						{
							new OIDRowReference(1),
							new OIDRowReference(2)
						}),
					new InvolvedTable(
						"Table2",
						new[]
						{
							new OIDRowReference(1000),
							new OIDRowReference(1001)
						})
				};

			string formatted = IssueUtils.FormatInvolvedTables(involvedTables);

			Console.WriteLine(formatted);

			Assert.AreEqual("[Table1]1||2;[Table2]1000||1001", formatted);
		}

		[Test]
		public void CanFormatInvolvedTablesWithAlternateKeys()
		{
			var involvedTables =
				new List<InvolvedTable>
				{
					new InvolvedTable(
						"Table1",
						new[]
						{
							new AlternateKeyRowReference("AAA"),
							new AlternateKeyRowReference("BBB")
						}, "KEY"),
					new InvolvedTable(
						"Table2",
						new[]
						{
							new AlternateKeyRowReference("111"),
							new AlternateKeyRowReference("222")
						}, "KEY")
				};

			string formatted = IssueUtils.FormatInvolvedTables(involvedTables);

			Console.WriteLine(formatted);

			Assert.AreEqual("[Table1:KEY]AAA||BBB;[Table2:KEY]111||222", formatted);
		}

		[Test]
		public void CanFormatInvolvedTableWithoutRows()
		{
			var involvedTables =
				new List<InvolvedTable>
				{
					new InvolvedTable("Table1", new RowReference[] { })
				};

			string formatted = IssueUtils.FormatInvolvedTables(involvedTables);

			Console.WriteLine(formatted);

			Assert.AreEqual("[Table1]", formatted);
		}

		[Test]
		public void CanFormatInvolvedTablesWithoutRows()
		{
			var involvedTables =
				new List<InvolvedTable>
				{
					new InvolvedTable("Table1", new RowReference[] { }),
					new InvolvedTable("Table2", new RowReference[] { }, "KeyField"),
					new InvolvedTable("Table3", new RowReference[] { })
				};

			string formatted = IssueUtils.FormatInvolvedTables(involvedTables);

			Console.WriteLine(formatted);

			// field name is omitted (not used, there are no row references)
			Assert.AreEqual("[Table1];[Table2];[Table3]", formatted);
		}

		[Test]
		public void CanParseInvolvedTablesWithOids()
		{
			IList<InvolvedTable> tables =
				IssueUtils.ParseInvolvedTables("[Table1]1||2;[Table2]1000||1001");

			Assert.AreEqual(2, tables.Count);

			InvolvedTable table1 = tables[0];
			Assert.AreEqual("Table1", table1.TableName);
			Assert.IsNull(table1.KeyField);
			IList<RowReference> rows1 = table1.RowReferences;
			Assert.AreEqual(2, rows1.Count);
			Assert.AreEqual(1, rows1[0].OID);
			Assert.AreEqual(2, rows1[1].OID);

			InvolvedTable table2 = tables[1];
			Assert.AreEqual("Table2", table2.TableName);
			Assert.IsNull(table2.KeyField);
			IList<RowReference> rows2 = table2.RowReferences;
			Assert.AreEqual(2, rows2.Count);
			Assert.AreEqual(1000, rows2[0].OID);
			Assert.AreEqual(1001, rows2[1].OID);
		}

		[Test]
		public void CanParseInvolvedTableNoIds()
		{
			IList<InvolvedTable> tables = IssueUtils.ParseInvolvedTables("[Table]");

			Assert.AreEqual(1, tables.Count);

			InvolvedTable table = tables[0];
			Assert.AreEqual("Table", table.TableName);
			Assert.IsNull(table.KeyField);
			IList<RowReference> rows = table.RowReferences;
			Assert.AreEqual(0, rows.Count);
		}

		[Test]
		public void CanParseInvolvedTableWithKeyFieldNoIds()
		{
			IList<InvolvedTable> tables = IssueUtils.ParseInvolvedTables("[Table:FieldName]");

			Assert.AreEqual(1, tables.Count);

			InvolvedTable table = tables[0];
			Assert.AreEqual("Table", table.TableName);
			Assert.AreEqual("FieldName", table.KeyField);
			IList<RowReference> rows = table.RowReferences;
			Assert.AreEqual(0, rows.Count);
		}

		[Test]
		public void CanParseShortestValidTableString()
		{
			IList<InvolvedTable> tables = IssueUtils.ParseInvolvedTables("[T:F]1||2||3;[Y:G]A");

			Assert.AreEqual(2, tables.Count);

			InvolvedTable table1 = tables[0];
			Assert.AreEqual("T", table1.TableName);
			Assert.AreEqual("F", table1.KeyField);
			IList<RowReference> rows1 = table1.RowReferences;
			Assert.AreEqual(3, rows1.Count);
			Assert.AreEqual("1", rows1[0].Key);
			Assert.AreEqual("2", rows1[1].Key);
			Assert.AreEqual("3", rows1[2].Key);

			InvolvedTable table2 = tables[1];
			Assert.AreEqual("Y", table2.TableName);
			Assert.AreEqual("G", table2.KeyField);
			IList<RowReference> rows2 = table2.RowReferences;
			Assert.AreEqual(1, rows2.Count);
			Assert.AreEqual("A", rows2[0].Key);
		}

		[Test]
		public void CanParseEmptyTableString1()
		{
			IList<InvolvedTable> involvedTables = IssueUtils.ParseInvolvedTables(string.Empty);

			Assert.AreEqual(0, involvedTables.Count);
		}

		[Test]
		public void CannotParseInvalidTableString1()
		{
			ExpectException("[Table", "Invalid involved tables string: '[Table'");
		}

		[Test]
		public void CannotParseInvalidTableString2()
		{
			ExpectException("Table]",
			                "Invalid involved tables string: 'Table]'; expected: [ - actual: T");
		}

		[Test]
		public void CannotParseInvalidTableString3()
		{
			ExpectException("[Table1][Table2]",
			                "Invalid involved tables string: '[Table1][Table2]'");
		}

		[Test]
		public void CannotParseInvalidTableString4()
		{
			ExpectException(" ", "Invalid involved tables string: ' '");
		}

		[Test]
		public void CannotParseInvalidTableString5()
		{
			ExpectException(" [Table]",
			                "Invalid involved tables string: ' [Table]'; expected: [ - actual:  ");
		}

		[Test]
		public void CannotParseInvalidTableString6()
		{
			ExpectException("[Table1];[Table2",
			                "Invalid involved tables string: '[Table1];[Table2'");
		}

		[Test]
		public void CannotParseInvalidTableString7()
		{
			ExpectException("[Table1];Table2]",
			                "Invalid involved tables string: '[Table1];Table2]'");
		}

		[Test]
		public void CannotParseInvalidTableString8()
		{
			ExpectException("[Table1]100000 20000",
			                "Invalid involved tables string: '[Table1]100000 20000'");
		}

		[Test]
		public void CanParseOidReferencesFastEnough()
		{
			// typical case with oid references
			AssertFastEnough(
				"[OWNER1.FIRST_TABLE]10000000||20000000;[OWNER2.SECOND_TABLE]30000000",
				count: 10000, maximumMilliseconds: 0.05);
		}

		[Test]
		public void CanParseAlternateKeyReferencesFastEnough()
		{
			// typical case with alternate key references
			AssertFastEnough(
				"[OWNER1.FIRST_TABLE:KEYFIELD1]C1403891-5DF0-4DC8-95C5-8FA87FDC5651||A8D98B7A-88EC-4464-9E60-04C547BA189F;" +
				"[OWNER2.SECOND_TABLE:KEYFIELD2]C1F6B449-1DAB-479E-B521-A59E129E0EC1",
				count: 10000, maximumMilliseconds: 0.05);
		}

		private static void AssertFastEnough([NotNull] string involvedTables,
		                                     int count,
		                                     double maximumMilliseconds)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			for (int i = 0; i < count; i++)
			{
				IssueUtils.ParseInvolvedTables(involvedTables);
			}

			stopwatch.Stop();
			long milliseconds = stopwatch.ElapsedMilliseconds;

			double millisecondsPerOperation = (double) milliseconds / count;
			Console.WriteLine(@"Parsing {0}: {1} ms", involvedTables, millisecondsPerOperation);

			Assert.LessOrEqual(millisecondsPerOperation, maximumMilliseconds);
		}

		private static void ExpectException([NotNull] string involvedTablesString,
		                                    [CanBeNull] string expectedMessage)
		{
			try
			{
				IList<InvolvedTable> involvedTables =
					IssueUtils.ParseInvolvedTables(involvedTablesString);

				foreach (InvolvedTable involvedTable in involvedTables)
				{
					Console.WriteLine(@"- {0}", involvedTable);
				}

				Assert.Fail("Exception expected for invalid string: {0}", involvedTablesString);
			}
			catch (AssertionException exception)
			{
				Console.WriteLine(exception.Message);
				if (expectedMessage != null)
				{
					Assert.AreEqual(expectedMessage, exception.Message);
				}
			}
		}
	}
}
