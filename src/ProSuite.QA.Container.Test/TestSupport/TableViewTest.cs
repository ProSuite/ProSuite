using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestSupport;
using Assert = ProSuite.Commons.Essentials.Assertions.Assert;

namespace ProSuite.QA.Container.Test.TestSupport
{
	public static class ReadOnlyUtils
	{
		[NotNull]
		public static ReadOnlyFeature Create(IFeature feature)
		{
			return ReadOnlyFeature.Create(
				ReadOnlyTableFactory.Create((IFeatureClass) feature.Table), feature);
		}

		public static ReadOnlyRow Create(IRow row)
		{
			if (row is IFeature f)
			{
				return Create(f);
			}

			return new ReadOnlyRow(ReadOnlyTableFactory.Create(row.Table), row);
		}
	}

	[TestFixture]
	public class TableViewTest
	{
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.AO.Test.TestUtils.InitializeLicense();
			_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace("TableViewTest");
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			Commons.AO.Test.TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanUseShapeAreaAlias()
		{
			IFeatureClass featureClass = CreateFeatureClass(
				"CanUseShapeAreaAlias",
				esriGeometryType.esriGeometryPolygon);

			IFeature f1 = featureClass.CreateFeature();
			f1.Shape = GeometryFactory.CreatePolygon(0, 0, 5, 5);
			f1.Store();

			IFeature f2 = featureClass.CreateFeature();
			f2.Shape = GeometryFactory.CreatePolygon(0, 0, 100, 100);
			f2.Store();

			AssertFilteredRowCount(1, "$ShapeArea < 100", ReadOnlyUtils.Create(f1),
			                       ReadOnlyUtils.Create(f2));
			AssertFilteredRowCount(1, "$ShapeArea > 100", ReadOnlyUtils.Create(f1),
			                       ReadOnlyUtils.Create(f2));
		}

		[Test]
		public void CanUseInteriorRingCountAlias()
		{
			IFeatureClass featureClass = CreateFeatureClass(
				"CanUseInteriorRingCountAlias",
				esriGeometryType.esriGeometryPolygon);

			IFeature f1 = featureClass.CreateFeature();
			IGeometry polygon = GeometryFactory.CreatePolygonWithHole(
				GeometryFactory.CreatePolygon(0, 0, 10, 10), 1, -1);
			GeometryUtils.Simplify(polygon);
			f1.Shape = polygon;
			f1.Store();

			IFeature f2 = featureClass.CreateFeature();
			f2.Shape = GeometryFactory.CreatePolygon(0, 0, 100, 100);
			GeometryUtils.Simplify(f2.Shape);
			f2.Store();

			IFeature f3 = featureClass.CreateFeature();
			f3.Shape = GeometryFactory.CreatePolygonWithHole(
				GeometryFactory.CreatePolygon(0, 0, 10, 10), 1, -1);
			// NOTE: result is non-simple
			f3.Store();

			AssertFilteredRowCount(1, "$ShapeInteriorRingCount = 1", f1, f2, f3);
			AssertFilteredRowCount(1, "$ShapeInteriorRingCount = 0", f1, f2, f3);
			AssertFilteredRowCount(1, "$ShapeInteriorRingCount IS NULL", f1, f2, f3);
		}

		[Test]
		public void CanUseExteriorRingCountAlias()
		{
			IFeatureClass featureClass = CreateFeatureClass(
				"CanUseExteriorRingCountAlias",
				esriGeometryType.esriGeometryPolygon);

			IFeature f1 = featureClass.CreateFeature();

			IGeometry polygon = GeometryUtils.Union(
				GeometryFactory.CreatePolygon(0, 0, 10, 10),
				GeometryFactory.CreatePolygon(20, 20, 30, 30));
			f1.Shape = polygon;
			f1.Store();

			IFeature f2 = featureClass.CreateFeature();
			f2.Shape = GeometryFactory.CreatePolygon(0, 0, 100, 100);
			GeometryUtils.Simplify(f2.Shape);
			f2.Store();

			IFeature f3 = featureClass.CreateFeature();
			f3.Shape = GeometryFactory.CreatePolygonWithHole(
				GeometryFactory.CreatePolygon(0, 0, 10, 10), 1, -1);
			// NOTE: result is non-simple
			f3.Store();

			AssertFilteredRowCount(1, "$ShapeExteriorRingCount = 2", f1, f2, f3);
			AssertFilteredRowCount(1, "$ShapeExteriorRingCount = 1", f1, f2, f3);
			AssertFilteredRowCount(1, "$ShapeExteriorRingCount IS NULL", f1, f2, f3);
		}

		[Test]
		public void CanUsePartCountAlias()
		{
			IFeatureClass featureClass = CreateFeatureClass(
				"CanUsePartCountAlias",
				esriGeometryType.esriGeometryPolygon);

			IFeature f1 = featureClass.CreateFeature();
			IGeometry polygon = GeometryFactory.CreatePolygonWithHole(
				GeometryFactory.CreatePolygon(0, 0, 10, 10), 1, -1);
			GeometryUtils.Simplify(polygon);
			f1.Shape = polygon;
			f1.Store();

			IFeature f2 = featureClass.CreateFeature();
			f2.Shape = GeometryFactory.CreatePolygon(0, 0, 100, 100);
			GeometryUtils.Simplify(f2.Shape);
			f2.Store();

			AssertFilteredRowCount(1, "$ShapePartCount = 2", f1, f2);
			AssertFilteredRowCount(1, "$ShapePartCount = 1", f1, f2);
		}

		[Test]
		public void CanUseShapeVertexCountAlias()
		{
			IFeatureClass featureClass = CreateFeatureClass(
				"CanUseShapeVertexCountAlias",
				esriGeometryType.esriGeometryMultipoint);

			IFeature f1 = featureClass.CreateFeature();
			f1.Shape = GeometryFactory.CreateMultipoint(GeometryFactory.CreatePoint(0, 0));
			f1.Store();

			IFeature f2 = featureClass.CreateFeature();
			f2.Shape = GeometryFactory.CreateMultipoint(GeometryFactory.CreatePoint(0, 0),
			                                            GeometryFactory.CreatePoint(10, 10),
			                                            GeometryFactory.CreatePoint(20, 20));
			f2.Store();

			AssertFilteredRowCount(1, "$ShapeVertexCount = 3", f1, f2);
			AssertFilteredRowCount(1, "$ShapeVertexCount = 1", f1, f2);
		}

		[Test]
		public void CanUseShapeLengthAlias()
		{
			IFeatureClass featureClass = CreateFeatureClass(
				"CanUseShapeLengthAlias",
				esriGeometryType.esriGeometryPolygon);

			IFeature f1 = featureClass.CreateFeature();
			f1.Shape = GeometryFactory.CreatePolygon(0, 0, 5, 5);
			f1.Store();

			IFeature f2 = featureClass.CreateFeature();
			f2.Shape = GeometryFactory.CreatePolygon(0, 0, 100, 100);
			f2.Store();

			AssertFilteredRowCount(1, "$ShapeLength < 25", f1, f2);
			AssertFilteredRowCount(1, "$ShapeLength = 400", f1, f2);
		}

		[Test]
		public void CanUseMMinMaxAlias()
		{
			IFeatureClass featureClass = CreateFeatureClass(
				"CanUseMMinMaxAlias",
				esriGeometryType.esriGeometryPolyline,
				zAware: true, mAware: true);

			IPolyline polyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(0, 0, 1000, 100),
				GeometryFactory.CreatePoint(5, 5, 1000, 200));

			IFeature feature = featureClass.CreateFeature();
			feature.Shape = polyline;
			feature.Store();

			AssertFilteredRowCount(1, "$ShapeMMin = 100 AND $ShapeMMax = 200", feature);
		}

		[Test]
		public void CanUseXyMinMaxAlias()
		{
			IFeatureClass featureClass = CreateFeatureClass(
				"CanUseXyMinMaxAlias",
				esriGeometryType.esriGeometryPolyline);

			IPolyline polyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(0, 100),
				GeometryFactory.CreatePoint(5, 105));

			IFeature feature = featureClass.CreateFeature();
			feature.Shape = polyline;
			feature.Store();

			AssertFilteredRowCount(1,
			                       "$ShapeXMin = 0 AND $ShapeXMax = 5 AND $ShapeYMin = 100 AND $ShapeYMax = 105",
			                       feature);
		}

		[Test]
		public void CanUseMMinMaxAliasInMultiTableView()
		{
			IFeatureClass featureClass = CreateFeatureClass(
				"CanUseMMinMaxAliasInMultiTableView",
				esriGeometryType.esriGeometryPolyline,
				zAware: true, mAware: true);

			ITable table = CreateTable("CanUseMMinMaxAliasInMultiTableView_table",
			                           FieldUtils.CreateOIDField(),
			                           FieldUtils.CreateIntegerField("NUMBER"));

			IFeature f1 = featureClass.CreateFeature();
			f1.Shape = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(0, 0, 1000, 100),
				GeometryFactory.CreatePoint(5, 5, 1000, 200));
			f1.Store();

			IFeature f2 = featureClass.CreateFeature();
			f2.Shape = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(0, 0, 1000, 300),
				GeometryFactory.CreatePoint(5, 5, 1000, 400));
			f2.Store();

			int fieldIndexNumber = table.FindField("NUMBER");

			IRow r1 = table.CreateRow();
			r1.Value[fieldIndexNumber] = 100;
			r1.Store();

			IRow r2 = table.CreateRow();
			r2.Value[fieldIndexNumber] = 200;
			r2.Store();

			AssertFilteredMultiViewRowCount(1,
			                                "F.$ShapeMMin = 100 AND F.$ShapeMMax = 200 AND T.NUMBER = 100",
			                                featureClass, f1, table, r1);
			AssertFilteredMultiViewRowCount(0,
			                                "F.$ShapeMMin = 100 AND F.$ShapeMMax = 200 AND T.NUMBER = 10000",
			                                featureClass, f1, table, r1);

			AssertFilteredMultiViewRowCount(1,
			                                "F.$ShapeMMin = 300 AND F.$ShapeMMax = 400 AND T.NUMBER = 200",
			                                featureClass, f2, table, r2);
			AssertFilteredMultiViewRowCount(0,
			                                "F.$ShapeMMin = 300 AND F.$ShapeMMax = 400 AND T.NUMBER = 10000",
			                                featureClass, f2, table, r2);
		}

		[Test]
		public void CanUseObjectIdAliasInMultiTableView()
		{
			IFeatureClass featureClass = CreateFeatureClass(
				"CanUseObjectIdAliasInMultiTableView",
				esriGeometryType.esriGeometryPoint);

			ITable table = CreateTable("CanUseObjectIdAliasInMultiTableView_table",
			                           FieldUtils.CreateOIDField(),
			                           FieldUtils.CreateIntegerField("NUMBER"));

			IFeature f1 = featureClass.CreateFeature();
			f1.Shape = GeometryFactory.CreatePoint(0, 0);
			f1.Store();

			IFeature f2 = featureClass.CreateFeature();
			f2.Shape = GeometryFactory.CreatePoint(5, 5);
			f2.Store();

			int fieldIndexNumber = table.FindField("NUMBER");

			IRow r1 = table.CreateRow();
			r1.Value[fieldIndexNumber] = 100;
			r1.Store();

			IRow r2 = table.CreateRow();
			r2.Value[fieldIndexNumber] = 200;
			r2.Store();

			AssertFilteredMultiViewRowCount(1,
			                                "F.$ObjectId = 1 AND T.$ObjectId = 1 AND T.NUMBER = 100",
			                                featureClass, f1, table, r1);
			AssertFilteredMultiViewRowCount(0,
			                                "F.$ObjectId = 1 AND T.$ObjectId = 1 AND T.NUMBER = 10000",
			                                featureClass, f1, table, r1);

			AssertFilteredMultiViewRowCount(1,
			                                "F.$ObjectId = 2 AND T.$ObjectId = 2 AND T.NUMBER = 200",
			                                featureClass, f2, table, r2);
			AssertFilteredMultiViewRowCount(0,
			                                "F.$ObjectId = 2 AND T.$ObjectId = 2 AND T.NUMBER = 10000",
			                                featureClass, f2, table, r2);
		}

		[Test]
		public void CanUseMMinMaxAliasIgnoringNaN()
		{
			IFeatureClass featureClass = CreateFeatureClass(
				"CanUseMMinMaxAliasIgnoringNaN",
				esriGeometryType.esriGeometryPolyline,
				zAware: true, mAware: true);

			IFeature feature = featureClass.CreateFeature();
			feature.Shape = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(0, 0, 1000, 100),
				GeometryFactory.CreatePoint(5, 5, 1000, double.NaN));
			feature.Store();

			AssertFilteredRowCount(1, "$ShapeMMin = 100 AND $ShapeMMax = 100", feature);
		}

		[Test]
		public void CanUseObjectIdAlias()
		{
			IFeatureClass featureClass = CreateFeatureClass(
				"CanUseObjectIdAlias",
				esriGeometryType.esriGeometryPoint);

			IFeature f1 = featureClass.CreateFeature();
			f1.Shape = GeometryFactory.CreatePoint(0, 0);
			f1.Store();

			IFeature f2 = featureClass.CreateFeature();
			f2.Shape = GeometryFactory.CreatePoint(5, 5);
			f2.Store();

			AssertFilteredRowCount(1, "$ObjectID = 1", f1);
			AssertFilteredRowCount(1, "$ObjectID = 2", f2);
			AssertFilteredRowCount(0, "$ObjectID = 3", f1);
		}

		[Test]
		public void CanUseZMinMaxAlias()
		{
			IFeatureClass featureClass = CreateFeatureClass(
				"CanUseZMinMaxAlias",
				esriGeometryType.esriGeometryPolyline,
				zAware: true);

			IFeature feature = featureClass.CreateFeature();
			feature.Shape = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(0, 0, 1000),
				GeometryFactory.CreatePoint(5, 5, 2000));
			feature.Store();

			AssertFilteredRowCount(1, "$ShapeZMin = 1000 AND $ShapeZMax = 2000", feature);
		}

		[Test]
		public void CanUseMMinMaxAliasEvenIfNotMAware()
		{
			IFeatureClass featureClass = CreateFeatureClass(
				"CanUseMMinMaxAliasEvenIfNotMAware",
				esriGeometryType.esriGeometryPolyline);

			IFeature feature = featureClass.CreateFeature();
			feature.Shape = GeometryFactory.CreatePolyline(0, 0, 5, 5);
			feature.Store();

			AssertFilteredRowCount(1, "$ShapeMMin IS NULL AND $ShapeMMax IS NULL", feature);
		}

		[Test]
		public void CanUseZMinMaxAliasEvenIfNotZAware()
		{
			IFeatureClass featureClass = CreateFeatureClass(
				"CanUseZMinMaxAliasEvenIfNotZAware",
				esriGeometryType.esriGeometryPolyline);

			IFeature feature = featureClass.CreateFeature();
			feature.Shape = GeometryFactory.CreatePolyline(0, 0, 5, 5);
			feature.Store();

			AssertFilteredRowCount(1, "$ShapeZMin IS NULL AND $ShapeZMax IS NULL", feature);
		}

		[Test]
		public void TestExpression()
		{
			const string intField = "Int";
			const string doubleField = "Dbl";
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateIntegerField(intField));
			fields.AddField(FieldUtils.CreateDoubleField(doubleField, doubleField));

			ITable tbl = DatasetUtils.CreateTable(_testWs, "TestExpression", null, fields);

			const int n = 10;
			for (var i = 0; i < n; i++)
			{
				IRow row = tbl.CreateRow();
				row.set_Value(1, i);
				row.set_Value(2, i);
				row.Store();
			}

			const double x = 2;
			string expression = string.Format("{0}, {1}, {2}", x, intField, doubleField);
			const bool useAsConstraint = false;
			TableView view =
				TableViewFactory.Create(ReadOnlyTableFactory.Create(tbl), expression,
				                        useAsConstraint);

			DataColumn constColumn = view.AddColumn("constValue", typeof(double));
			constColumn.Expression = x.ToString(CultureInfo.InvariantCulture);

			DataColumn intColumn = view.AddColumn("intValue", typeof(double));
			intColumn.Expression = intField;

			DataColumn exprColumn = view.AddColumn("exprValue", typeof(double));
			exprColumn.Expression = string.Format("2.3 * {0} + 1.2 * {1}", doubleField,
			                                      intField);

			DataColumn doubleColumn = view.AddColumn("doubleValue", typeof(double));
			doubleColumn.Expression = doubleField;

			foreach (IRow row in new EnumCursor(tbl, null, false))
			{
				view.ClearRows();
				var i = (int) row.Value[1];
				var d = (double) row.Value[2];

				DataRow expressionRow = Assert.NotNull(view.Add(ReadOnlyUtils.Create(row)));

				var constVal = (double) expressionRow[constColumn.ColumnName];
				NUnit.Framework.Assert.AreEqual(x, constVal);

				var intVal = (double) expressionRow[intColumn.ColumnName];
				NUnit.Framework.Assert.AreEqual(i, intVal);

				var doubleVal = (double) expressionRow[doubleColumn.ColumnName];
				NUnit.Framework.Assert.AreEqual(d, doubleVal);

				var exprVal = (double) expressionRow[exprColumn.ColumnName];
				NUnit.Framework.Assert.AreEqual(2.3 * d + 1.2 * i, exprVal);
			}
		}

		[Test]
		public void LearningTestColumnComparisonCaseSensitivity()
		{
			var dataTable = new DataTable("test");

			dataTable.Columns.Add("COL1", typeof(string));
			dataTable.Columns.Add("COL2", typeof(string));

			var dataView = new DataView(dataTable);

			dataTable.Rows.Add("aa", "AA");

			// case sensitivity does not apply to column names, only column content and literals
			dataView.RowFilter = "col1 = col2";

			dataTable.CaseSensitive = false;
			NUnit.Framework.Assert.AreEqual(1, dataView.Count);

			dataTable.CaseSensitive = true;
			NUnit.Framework.Assert.AreEqual(0, dataView.Count);
		}

		[Test]
		public void LearningTestConstantComparisonCaseSensitivity()
		{
			var dataTable = new DataTable("test");

			dataTable.Columns.Add("COL1", typeof(string));
			dataTable.Columns.Add("COL2", typeof(string));

			var dataView = new DataView(dataTable);

			dataTable.Rows.Add("aa", "AA");

			dataView.RowFilter = "'x' = 'X'";

			dataTable.CaseSensitive = false;
			NUnit.Framework.Assert.AreEqual(1, dataView.Count);

			dataTable.CaseSensitive = true;
			// NOTE: result for constant expression is cached --> Count is still 1 !
			NUnit.Framework.Assert.AreEqual(1, dataView.Count);

			// NOTE: must set row filter to a different value first
			dataView.RowFilter = null;
			dataView.RowFilter = "'x' = 'X'";
			NUnit.Framework.Assert.AreEqual(0, dataView.Count);
		}

		[Test]
		public void LearningTestColumnLiteralComparisonCaseSensitivity()
		{
			var dataTable = new DataTable("test");

			dataTable.Columns.Add("COL1", typeof(string));

			var dataView = new DataView(dataTable);

			dataTable.Rows.Add("aa");

			// case sensitivity does not apply to column names, only column content and literals
			dataView.RowFilter = "col1 = 'AA'";

			dataTable.CaseSensitive = false;
			NUnit.Framework.Assert.AreEqual(1, dataView.Count);

			dataTable.CaseSensitive = true;
			NUnit.Framework.Assert.AreEqual(0, dataView.Count);
		}

		private static void AssertFilteredRowCount(
			int expectedCount, [NotNull] string expression,
			params IFeature[] features)
		{
			IList<IReadOnlyFeature> fs = features.Select((x) =>
			{
				IReadOnlyFeature ro = ReadOnlyUtils.Create(x);
				return ro;
			}).ToList();
			AssertFilteredRowCount(expectedCount, expression, fs);
		}

		private static void AssertFilteredRowCount(
			int expectedCount, [NotNull] string expression,
			params IReadOnlyFeature[] features)
		{
			IList<IReadOnlyFeature> fs = features;
			AssertFilteredRowCount(expectedCount, expression, fs);
		}

		private static void AssertFilteredRowCount(
			int expectedCount,
			[NotNull] string expression,
			[NotNull] IList<IReadOnlyFeature> features)
		{
			TableView view = CreateTableView(expression, features);

			Assert.AreEqual(expectedCount, view.FilteredRowCount,
			                "Unexpected filtered row count");
		}

		private static void AssertFilteredMultiViewRowCount(
			int expectedCount, [NotNull] string expression,
			[NotNull] IFeatureClass featureClass, [NotNull] IFeature feature,
			[NotNull] ITable table, [NotNull] IRow row)
		{
			ReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(featureClass);
			ReadOnlyTable roTbl = ReadOnlyTableFactory.Create(table);
			MultiTableView view = TableViewFactory.Create(new[] { roFc, roTbl },
			                                              new[] { "F", "T" },
			                                              expression);

			view.Add(ReadOnlyUtils.Create(feature), ReadOnlyUtils.Create(row));

			Assert.AreEqual(expectedCount, view.FilteredRowCount,
			                "Unexpected filtered row count");
		}

		[NotNull]
		private static TableView CreateTableView([NotNull] string expression,
		                                         IList<IReadOnlyFeature> features)
		{
			Assert.ArgumentCondition(features.Count > 0, "no feature");

			var table = features[0].Table;

			const bool useAsConstraint = true;
			TableView view = TableViewFactory.Create(table, expression, useAsConstraint);
			foreach (var feature in features)
			{
				view.Add(feature);
			}

			return view;
		}

		[NotNull]
		private IFeatureClass CreateFeatureClass([NotNull] string name,
		                                         esriGeometryType geometryType,
		                                         bool zAware = false,
		                                         bool mAware = false)
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);

			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000, 0.0001,
			                                  0.001);

			IFields fields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField("Shape",
				                            geometryType,
				                            sref, 1000,
				                            zAware, mAware));

			return DatasetUtils.CreateSimpleFeatureClass(_testWs, name, fields);
		}

		[NotNull]
		private ITable CreateTable([NotNull] string name, [NotNull] params IField[] fields)
		{
			return DatasetUtils.CreateTable(_testWs, name, fields);
		}
	}
}
