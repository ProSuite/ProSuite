using System;
using System.Data.SqlTypes;
using System.Diagnostics;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using Assert = ProSuite.Commons.Essentials.Assertions.Assert;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	[Ignore("TODO: Create FGDB first with table that has random guid and random text")]
	public class TableSortUtilsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanSortByStringField()
		{
			IFeatureWorkspace featureWs =
				WorkspaceUtils.OpenFileGdbFeatureWorkspace(TestData.GetArealeFileGdbPath());

			ITable table = DatasetUtils.OpenTable(featureWs, "TLM_NUTZUNGSAREAL");

			const string operatorFieldName = "OPERATEUR";
			ICursor cursor = TableSortUtils.GetSortedTableCursor(table, operatorFieldName);

			int fieldIndex = cursor.FindField(operatorFieldName);
			Assert.True(fieldIndex >= 0, "Field not found");

			string lastValue = null;
			IRow row;
			while ((row = cursor.NextRow()) != null)
			{
				object value = row.get_Value(fieldIndex);

				Assert.False(value == DBNull.Value, "Empty field");

				var currentValue = (string) value;
				Console.WriteLine(currentValue);

				if (lastValue != null)
				{
					Assert.False(currentValue.CompareTo(lastValue) < 0, "Not sorted");
				}

				lastValue = currentValue;
			}
		}

		[Test]
		public void CanSortOnFgdbGuids()
		{
			IFeatureWorkspace featureWs =
				WorkspaceUtils.OpenFileGdbFeatureWorkspace(TestData.GetArealeFileGdbPath());
			//IFeatureWorkspace featureWs = OpenTestWorkspace();

			ITable table = DatasetUtils.OpenTable(featureWs, "TLM_NUTZUNGSAREAL");
			//ITable table = DatasetUtils.OpenTable(featureWs, "TOPGIS_TLM.TLM_NUTZUNGSAREAL");

			const string uuidFieldName = "UUID";

			ICursor cursor = TableSortUtils.GetGuidFieldSortedCursor(table, uuidFieldName);

			int fieldIndex = cursor.FindField(uuidFieldName);
			Assert.True(fieldIndex >= 0, "Field not found");

			Guid lastGuid = Guid.Empty;
			IRow row;
			while ((row = cursor.NextRow()) != null)
			{
				object value = row.get_Value(fieldIndex);

				Assert.False(value == DBNull.Value, "Empty UUID field");

				var currentGuid = new Guid((string) value);
				Console.WriteLine(currentGuid);

				if (lastGuid != Guid.Empty)
				{
					Assert.False(currentGuid.CompareTo(lastGuid) < 0, "Not sorted");
				}

				lastGuid = currentGuid;
			}
		}

		[Test]
		public void TableSortOnFgdbGuidsPerformance()
		{
			IFeatureWorkspace featureWs =
				WorkspaceUtils.OpenFileGdbFeatureWorkspace(TestData.GetArealeFileGdbPath());
			//IFeatureWorkspace featureWs = OpenTestWorkspace();

			ITable table = DatasetUtils.OpenTable(featureWs, "TLM_NUTZUNGSAREAL");
			//ITable table = DatasetUtils.OpenTable(featureWs, "TOPGIS_TLM.TLM_NUTZUNGSAREAL");

			const string uuidFieldName = "UUID";

			var watch = new Stopwatch();
			watch.Start();

			ICursor cursor = TableSortUtils.GetSortedTableCursor(table, uuidFieldName);
			LoopAndWrite(cursor, uuidFieldName);

			watch.Stop();
			long standardSort = watch.ElapsedMilliseconds;

			//featureWs = OpenTestWorkspace();
			//table = DatasetUtils.OpenTable(featureWs, "TOPGIS_TLM.TLM_NUTZUNGSAREAL");

			watch = new Stopwatch();
			watch.Start();

			cursor = TableSortUtils.GetGuidFieldSortedCursor(table, uuidFieldName);
			LoopAndWrite(cursor, uuidFieldName);

			watch.Stop();
			Console.WriteLine(@"Standard Sorter: {0}", standardSort);
			Console.WriteLine(@"Guid Sorter: {0}", watch.ElapsedMilliseconds);
		}

		private static void LoopAndWrite(ICursor cursor, string uuidFieldName)
		{
			int fieldIndex = cursor.FindField(uuidFieldName);
			Assert.True(fieldIndex >= 0, "Field not found");

			IRow row;
			while ((row = cursor.NextRow()) != null)
			{
				object value = row.get_Value(fieldIndex);

				Assert.False(value == DBNull.Value, "Empty UUID field");

				var currentGuid = new Guid((string) value);
				IComparable currentValue = currentGuid; // value as IComparable; // currentGuid;

				Console.WriteLine(currentValue);
			}
		}

		#region Learning Tests

		/// <summary>
		/// Starting with 10.0 the sort order of GUIDs has changed when using ITableSort. The algorithm used
		/// is not quite obvious.
		/// </summary>
		[Test]
		public void TableSortGuidFileGdb()
		{
			IFeatureWorkspace featureWs =
				WorkspaceUtils.OpenFileGdbFeatureWorkspace(TestData.GetArealeFileGdbPath());
			//IFeatureWorkspace featureWs = OpenTestWorkspace();

			ITable table = DatasetUtils.OpenTable(featureWs, "TLM_NUTZUNGSAREAL");

			ITableSort tableSort = new TableSortClass();

			tableSort.Table = table;
			tableSort.Fields = "UUID";

			tableSort.Sort(null);

			ICursor cursor = tableSort.Rows;

			IRow row;
			Guid lastValue = Guid.Empty;
			while ((row = cursor.NextRow()) != null)
			{
				object value = row.get_Value(1);

				Assert.False(value == DBNull.Value, "Empty UUID field");

				var currentGuid = new Guid((string) value);
				IComparable currentValue = currentGuid;

				Console.WriteLine(currentValue);

				if (lastValue != Guid.Empty)
				{
					// ITableSort on a GUID field in a file GDB does not sort the byte array or at least not in an obvious way
					byte[] currentGuidAsByteArray = currentGuid.ToByteArray();
					byte[] lastGuidAsByteArray = lastValue.ToByteArray();
					int byteArrayCompare = ByteArrayCompare(currentGuidAsByteArray,
					                                        lastGuidAsByteArray);
					//Assert.True(byteArrayCompare > 0, "Different compare algorithm from byte array");

					// ITableSort on a GUID field in a file GDB does not sort the same way as SqlGuid in .NET (i.e. SQL Server sorting)
					var sqlGuid = new SqlGuid(currentGuid);
					int sqlGuidCompare = sqlGuid.CompareTo(new SqlGuid(lastValue));
					//Assert.True(sqlGuidCompare < 0, "Different compare algorithm from sql server");

					// ITableSort on a GUID field in a file GDB does not sort the same way as Guid in .NET
					IComparable comparableGuid = lastValue;
					int guidCompare = comparableGuid.CompareTo(currentValue);
					//Assert.True(guidCompare < 0, "Different compare algorithm from .Net.");
				}

				lastValue = currentGuid;
			}
		}

		private static int ByteArrayCompare(byte[] a1, byte[] a2)
		{
			Assert.AreEqual(a1.Length, a2.Length, "Length difference");

			for (int i = 0; i < a1.Length; i++)
			{
				int compareValue = a1[i].CompareTo(a2[i]);

				if (compareValue != 0)
				{
					return compareValue;
				}
			}

			return 0;
		}

		/// <summary>
		/// Order by Guid in the post fix clause seems to use the same sorting algorithm as ITableSort
		/// </summary>
		[Test]
		public void OrderByGuidFileGdb()
		{
			IFeatureWorkspace featureWs =
				WorkspaceUtils.OpenFileGdbFeatureWorkspace(TestData.GetArealeFileGdbPath());
			ITable table = DatasetUtils.OpenTable(featureWs, "TLM_NUTZUNGSAREAL");

			IQueryFilter queryFilter = new QueryFilterClass();

			var queryFilterDef = (IQueryFilterDefinition) queryFilter;
			queryFilterDef.PostfixClause = "ORDER BY UUID";

			foreach (IRow row in GdbQueryUtils.GetRows(table, queryFilter, true))
			{
				object value = row.get_Value(1);

				Assert.False(value == DBNull.Value, "Empty UUID field");

				var currentGuid = new Guid((string) value);

				Console.WriteLine(currentGuid);
			}
		}

		#endregion
	}
}
