using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP.Test
{
	/// <summary>
	/// Regression tests for three defects in <see cref="ArcRow"/> / <see cref="ArcFeature"/>:
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ArcRowDisposalAndRefreshTest
	{
		private ArcGIS.Core.Data.Geodatabase _gdb;

		private static readonly SpatialReference _sr = SpatialReferences.WGS84;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();

			_gdb = SchemaBuilder.CreateGeodatabase(
				new MemoryConnectionProperties("ArcRowDisposalAndRefreshTest"));
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			_gdb?.Dispose();
		}

		/// <summary>
		/// Disposing a row must not dispose its (shared) parent table. With the defect
		/// <c>ArcRow.Dispose</c> calls <c>(_parentTable as ArcTable)?.Dispose()</c>, disposing the
		/// underlying Pro <see cref="Table"/> and its definition - breaking every other row and any
		/// future use of the cached table.
		/// </summary>
		[Test]
		public void Disposing_a_row_does_not_dispose_its_parent_table()
		{
			Table proTable = CreateTableWithRow("disposal_table", out long oid);

			var arcTable = new ArcTable(proTable);
			Row proRow = GetProRow(proTable, oid);

			IRow arcRow = ArcRow.Create(proRow, arcTable);

			((ArcRow) arcRow).Dispose();

			// The parent table must still be usable after a single row was disposed.
			Assert.DoesNotThrow(
				() => arcTable.ProTable.GetCount(),
				"Disposing a row disposed its shared parent table (ProTable). " +
				"A row must not own/dispose the table it belongs to.");
		}

		/// <summary>
		/// After the underlying Pro feature was disposed (e.g. an edit operation
		/// completed and disposed it), <c>TryOrRefreshRow</c> re-opens the row and reassigns the
		/// base <c>ProRow</c>. <see cref="ArcFeature.Extent"/> must read from that refreshed row.
		/// With the defect it reads from the stale <c>_proFeature</c> field instead and throws
		/// <see cref="System.ObjectDisposedException"/>.
		/// </summary>
		[Test]
		public void Extent_reads_from_refreshed_row_not_stale_feature_field()
		{
			FeatureClass proFc =
				CreateFeatureClassWithFeature("refresh_fc", 10, 20, out long oid);

			var arcFeatureClass = new ArcFeatureClass(proFc);

			Feature proFeature = (Feature) GetProRow(proFc, oid);

			var arcFeature = new ArcFeature(proFeature, arcFeatureClass);

			// Simulate the underlying Pro row being disposed out from under the wrapper
			// (the situation TryOrRefreshRow is meant to recover from).
			proFeature.Dispose();

			// Typed as object to avoid referencing the geometry API assembly.
			object extent = null;

			Assert.DoesNotThrow(
				() => extent = arcFeature.Extent,
				"ArcFeature.Extent must read from the refreshed ProRow, not the stale " +
				"_proFeature field that was disposed.");

			Assert.NotNull(extent, "Extent should be available after the row was refreshed.");
		}

		/// <summary>
		/// Finding 3: <see cref="ArcFeature.Extent"/> must not throw a NullReferenceException when
		/// there is no underlying row (which is reachable, e.g. after a failed refresh leaves
		/// <c>ProRow</c> null). The expected contract is a null envelope.
		/// </summary>
		[Test]
		public void Extent_of_feature_without_row_returns_null_instead_of_throwing()
		{
			FeatureClass proFc =
				CreateFeatureClassWithFeature("no_row_fc", 30, 40, out long _);

			var arcFeatureClass = new ArcFeatureClass(proFc);

			// A feature wrapper with no underlying Pro row.
			var arcFeature = new ArcFeature(null, arcFeatureClass);

			object extent = null;

			Assert.DoesNotThrow(
				() => extent = arcFeature.Extent,
				"ArcFeature.Extent must handle a missing underlying row gracefully, " +
				"not dereference a null geometry (NullReferenceException).");

			Assert.IsNull(extent,
			              "Extent should be null when there is no underlying row/geometry.");
		}

		#region Test data helpers

		private Table CreateTableWithRow(string name, out long oid)
		{
			var schemaBuilder = new SchemaBuilder(_gdb);

			var tableDescription = new TableDescription(
				name, new List<FieldDescription>
				      {
					      new FieldDescription("NAME", FieldType.String)
				      });

			schemaBuilder.Create(tableDescription);

			Assert.True(schemaBuilder.Build(), $"Failed to create test table {name}.");

			Table table = _gdb.OpenDataset<Table>(name);

			using RowBuffer buffer = table.CreateRowBuffer();
			buffer["NAME"] = "row";

			using Row row = table.CreateRow(buffer);
			oid = row.GetObjectID();

			return table;
		}

		private FeatureClass CreateFeatureClassWithFeature(
			string name, double x, double y, out long oid)
		{
			FeatureClass featureClass = DatasetUtils.CreateFeatureClass(
				_gdb, name,
				new List<FieldDescription> { new FieldDescription("NAME", FieldType.String) },
				GeometryType.Point, _sr, hasZ: false);

			FeatureClassDefinition definition = featureClass.GetDefinition();

			using RowBuffer buffer = featureClass.CreateRowBuffer();
			buffer[definition.GetShapeField()] = MapPointBuilderEx.CreateMapPoint(x, y, _sr);

			using Feature feature = (Feature) featureClass.CreateRow(buffer);
			oid = feature.GetObjectID();

			return featureClass;
		}

		private static Row GetProRow(Table table, long oid)
		{
			var queryFilter = new QueryFilter { ObjectIDs = new List<long> { oid } };

			using RowCursor cursor = table.Search(queryFilter, false);

			Assert.True(cursor.MoveNext(), $"No row found with OID {oid}.");

			return cursor.Current;
		}

		#endregion
	}
}
