using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Container;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.DomainModel.AO.Test.QA
{
	[TestFixture]
	public class RowParserTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanFormatAndParseSingleRow()
		{
			ITable dummy = new ObjectClassMock(1, "Dummy");
			ITest test = new DummyTest(ReadOnlyTableFactory.Create(dummy));
			const string tableName = "Test";
			const int oid = 1;
			string formatted = RowParser.Format(test, 0,
			                                    new[] { new InvolvedRow(tableName, oid) });

			Console.WriteLine(formatted);
			Assert.IsNotNull(formatted);

			IList<InvolvedRow> parsed = RowParser.Parse(formatted);
			Assert.AreEqual(1, parsed.Count);
			Assert.AreEqual(tableName, parsed[0].TableName);
			Assert.AreEqual(oid, parsed[0].OID);
		}

		[Test]
		public void CanFormatAndParseSingleTable()
		{
			ITable dummy = new ObjectClassMock(1, "Dummy");
			ITest test = new DummyTest(ReadOnlyTableFactory.Create(dummy));
			const string tableName = "Test";
			string formatted = RowParser.Format(test, 0, new[] { new InvolvedRow(tableName) });

			Console.WriteLine(formatted);
			Assert.IsNotNull(formatted);

			IList<InvolvedRow> parsed = RowParser.Parse(formatted);
			Assert.AreEqual(1, parsed.Count);
			Assert.AreEqual(tableName, parsed[0].TableName);
			Assert.IsTrue(parsed[0].RepresentsEntireTable);
		}

		[Test]
		public void CanFormatAndParseMultiRow()
		{
			ITable dummy = new ObjectClassMock(1, "Dummy");
			ITest test = new DummyTest(ReadOnlyTableFactory.Create(dummy));
			const string tableName = "Test";
			IList<int> oids = new[] { 1, 2, 3, 4, 5, 6 };
			IList<InvolvedRow> rows = new List<InvolvedRow>(oids.Count);
			foreach (int oid in oids)
			{
				rows.Add(new InvolvedRow(tableName, oid));
			}

			string formatted = RowParser.Format(test, 0, rows);

			Console.WriteLine(formatted);
			Assert.IsNotNull(formatted);

			IList<InvolvedRow> parsed = RowParser.Parse(formatted);
			Assert.AreEqual(oids.Count, parsed.Count);
		}

		[Test]
		public void CanFormatAndParseMultiTables()
		{
			ITable dummy = new ObjectClassMock(1, "Dummy");
			ITest test = new DummyTest(ReadOnlyTableFactory.Create(dummy));
			const string table1 = "Tbl1";
			const string table2 = "Tbl0";
			IList<int> oids = new[] { 17, 2, 23, 14, 15, 6 };
			IList<InvolvedRow> rows = new List<InvolvedRow>(2 * oids.Count);
			foreach (int oid in oids)
			{
				rows.Add(new InvolvedRow(table1, oid));
			}

			foreach (int oid in oids)
			{
				rows.Add(new InvolvedRow(table2, oid));
			}

			string formatted = RowParser.Format(test, 0, rows);

			Console.WriteLine(formatted);
			Assert.IsNotNull(formatted);

			IList<InvolvedRow> parsed = RowParser.Parse(formatted);
			Assert.AreEqual(2 * oids.Count, parsed.Count);
			Assert.AreEqual(table1, parsed[0].TableName);
		}

		[Test]
		public void CanFormatAndParseMultiTables2()
		{
			ITable dummy = new ObjectClassMock(1, "Dummy");
			ITest test = new DummyTest(ReadOnlyTableFactory.Create(dummy));
			const string table1 = "Tbl1";
			const string table2 = "Tbl0";
			IList<int> oids = new[] { 17, 2, 23, 14, 15, 6 };
			IList<InvolvedRow> rows = new List<InvolvedRow>(2 * oids.Count);
			foreach (int oid in oids)
			{
				rows.Add(new InvolvedRow(table1, oid));
				rows.Add(new InvolvedRow(table2, oid));
			}

			string formatted = RowParser.Format(test, 0, rows);

			Console.WriteLine(formatted);
			Assert.IsNotNull(formatted);

			IList<InvolvedRow> parsed = RowParser.Parse(formatted);
			Assert.AreEqual(2 * oids.Count, parsed.Count);
			Assert.AreEqual(table1, parsed[0].TableName);
		}

		[Test]
		public void CanFormatAndParseTooManyRows()
		{
			const string table1 = "Tbl1";
			const string table2 = "Tbl0";
			IList<long> oids = new[] { 17463L, 2568L, 243L, 142622L, 15246L, 6462L };
			IList<InvolvedRow> rows = new List<InvolvedRow>(2 * oids.Count);
			foreach (int oid in oids)
			{
				rows.Add(new InvolvedRow(table1, oid));
				rows.Add(new InvolvedRow(table2, oid));
			}

			string formatted = RowParser.Format(rows, 62);

			Console.WriteLine(formatted);
			Assert.IsNotNull(formatted);

			InvolvedRows parsed = RowParser.Parse(formatted);
			Assert.IsTrue(2 * oids.Count > parsed.Count);
			Assert.IsTrue(parsed.HasAdditionalRows);
			foreach (InvolvedRow involvedRow in parsed)
			{
				Assert.IsTrue(oids.Contains(involvedRow.OID));
			}
		}

		private class DummyTest : ContainerTest
		{
			public DummyTest([NotNull] IReadOnlyTable table) : base(table) { }

			protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
			{
				return 0;
			}
		}
	}
}
