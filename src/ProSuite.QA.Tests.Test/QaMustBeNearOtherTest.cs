using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.TestFactories;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMustBeNearOtherTest
	{
		private IFeatureWorkspace _testWs;
		private IFeatureWorkspace _relTestWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense(activateAdvancedLicense: true);

			_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace("QaMustBeNearOtherTest");
		}

		private IFeatureWorkspace RelTestWs
		{
			get
			{
				return _relTestWs ??
				       (_relTestWs =
					        TestWorkspaceUtils.CreateTestFgdbWorkspace("QaMustBeNearOtherTest"));
			}
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanTestFullExtentSingleTile()
		{
			IFeatureClass fc = CreatePointClass(_testWs, "CanTestFullExtentSingleTile");
			AddPoints(fc);

			QaContainerTestRunner runner = CreateTestRunner(fc, 1000, 9.5);

			runner.Execute();

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void CanTestDefinedExtentSingleTile()
		{
			IFeatureClass fc = CreatePointClass(_testWs, "CanTestDefinedExtentSingleTile");
			AddPoints(fc);

			QaContainerTestRunner runner = CreateTestRunner(fc, 1000, 9.5);

			IEnvelope envelope = GeometryFactory.CreateEnvelope(9, 9, 41, 41);

			runner.Execute(GeometryFactory.CreatePolygon(envelope));

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void CanTestDefinedSmallPolySingleTile()
		{
			IFeatureClass fc = CreatePointClass(_testWs, "CanTestDefinedSmallPolySingleTile");
			AddPoints(fc);

			QaContainerTestRunner runner = CreateTestRunner(fc, 1000, 9.5);

			IEnvelope envelope = GeometryFactory.CreateEnvelope(10.5, 10.5, 11.5, 11.5);

			runner.Execute(GeometryFactory.CreatePolygon(envelope));

			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void CanTestFullExtentMultipleTiles()
		{
			IFeatureClass fc = CreatePointClass(_testWs, "CanTestFullExtentMultipleTiles");
			AddPoints(fc);

			QaContainerTestRunner runner = CreateTestRunner(fc, 10, 9.5);

			runner.Execute();

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void CanTestDefinedExtentMultipleTiles()
		{
			IFeatureClass fc = CreatePointClass(_testWs, "CanTestDefinedExtentMultipleTiles");
			AddPoints(fc);

			QaContainerTestRunner runner = CreateTestRunner(fc, 10, 9.5);

			IEnvelope envelope = GeometryFactory.CreateEnvelope(9, 9, 41, 41);

			runner.Execute(GeometryFactory.CreatePolygon(envelope));

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void CanTestDefinedExtentMultipleTilesWithLinesNearTileBoundary()
		{
			IPolycurve leftOfTileBoundary = CurveConstruction.StartLine(99.5, 10)
			                                                 .LineTo(99.5, 90)
			                                                 .Curve;
			IPolycurve rightOfTileBoundary = CurveConstruction.StartLine(100.5, 10)
			                                                  .LineTo(100.5, 90)
			                                                  .Curve;

			IFeatureClass fc = CreateLineClass(_testWs, "CanTestDefinedExtentMultipleTiles_fc");

			IFeature row = fc.CreateFeature();
			row.Shape = rightOfTileBoundary;
			row.Store();

			IFeatureClass nearClass = CreateLineClass(_testWs,
			                                          "CanTestDefinedExtentMultipleTiles_near");

			IFeature nearRow = nearClass.CreateFeature();
			nearRow.Shape = leftOfTileBoundary;
			nearRow.Store();

			IPolygon polygon = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(0, 0, 200, 200));

			const double maximumDistance = 1.5;

			// one tile for the entire verification extent
			QaContainerTestRunner runnerLargeTileSize = CreateTestRunner(
				fc, nearClass, 200, maximumDistance);
			runnerLargeTileSize.Execute(polygon);

			AssertUtils.NoError(runnerLargeTileSize);

			// 4 tiles. 
			// The feature is in the second tile, to the right of the left tile boundary, but within the search distance of it.
			// The 'near feature' is in the first tile, to the left of the right tile boundary, but within the search distance of it.

			// NOTE currently this results in an error, since the 'near feature' is not returned again for the second tile
			// (the test container assumes that it was already considered in the first tile; in this case however, it is
			// needed for a feature in the *second* tile, which was not yet returned for the first tile.

			// -> if the search geometry overlaps the tile boundary but the source feature for the search DOES NOT, then
			//    features must be returned by the search even if they overlap a previous tile
			QaContainerTestRunner runnerSmallTileSize = CreateTestRunner(
				fc, nearClass, 100, maximumDistance);
			runnerSmallTileSize.Execute(polygon);

			AssertUtils.NoError(runnerSmallTileSize);
		}

		[Test]
		public void CanTestDefinedExtentLines()
		{
			IFeatureClass fc = CreateLineClass(_testWs, "CanTestDefinedExtentLines");
			AddLines(fc);

			QaContainerTestRunner runner = CreateTestRunner(fc, 1000, 9.5);

			IEnvelope envelope = GeometryFactory.CreateEnvelope(9, 9, 25, 25);

			runner.Execute(GeometryFactory.CreatePolygon(envelope));

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanTestDefinedExtentPartialLines()
		{
			IFeatureClass fc = CreateLineClass(_testWs, "CanTestDefinedExtentPartialLines");
			AddLines(fc);

			QaContainerTestRunner runner = CreateTestRunner(fc, 1000, 9.5);

			IEnvelope envelope = GeometryFactory.CreateEnvelope(9, 9, 20.5, 20.5);

			runner.Execute(GeometryFactory.CreatePolygon(envelope));

			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void TestRelatedFactoryMinParameters()
		{
			IFeatureWorkspace testWs = RelTestWs;

			const string fkTable = "fkTable";
			IFeatureClass fc1 = CreateLineClass(
				testWs, "Fc1_" + Environment.TickCount,
				new List<IField> { FieldUtils.CreateIntegerField(fkTable) });
			IFeatureClass fc2 = CreateLineClass(testWs, "Fc2_" + Environment.TickCount);

			var ds1 = (IDataset) fc1;
			var ds2 = (IDataset) fc2;

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			const string IdField = "IdRel";
			fields.AddField(FieldUtils.CreateIntegerField(IdField));

			string relTableName = "Rel_" + ds1.Name;
			ITable relTable = TestWorkspaceUtils.CreateSimpleTable(testWs, relTableName,
				fields);

			var dsRel = (IDataset) relTable;

			string relName = "relName" + Environment.TickCount;
			IRelationshipClass rel = TestWorkspaceUtils.CreateSimple1NRelationship(
				testWs, relName, relTable, (ITable) fc1, relTable.OIDFieldName, fkTable);

			((IWorkspaceEdit) testWs).StartEditing(false);

			IRow r1 = AddRow(relTable, new object[] { 12 });
			IRow r2 = AddRow(relTable, new object[] { 14 });

			IFeature f11 = AddFeature(
				fc1, CurveConstruction.StartLine(0, 0).LineTo(4, 0).Curve, new object[] { r1.OID });
			IFeature f12 = AddFeature(
				fc1, CurveConstruction.StartLine(0, 0).LineTo(4, 0).Curve, new object[] { r2.OID });
			IFeature f2 = AddFeature(fc2, CurveConstruction.StartLine(10, 1).LineTo(14, 1).Curve);

			((IWorkspaceEdit) testWs).StopEditing(true);

			var model = new SimpleModel("model", fc1);
			Dataset mds1 = model.AddDataset(new ModelVectorDataset(ds1.Name));
			Dataset mdsRel = model.AddDataset(new ModelTableDataset(dsRel.Name));
			Dataset mds2 = model.AddDataset(new ModelVectorDataset(ds2.Name));

			var clsDesc = new ClassDescriptor(typeof(QaRelMustBeNearOther));
			var tstDesc = new TestDescriptor("testNear", clsDesc);
			var condition = new QualityCondition("cndNear", tstDesc);
			InstanceConfigurationUtils.AddParameterValue(condition, "relationTables", mds1);
			InstanceConfigurationUtils.AddParameterValue(condition, "relationTables", mdsRel,
			                                             $"{relTableName}.{IdField} = 12"); // --> only f11 get's checked
			InstanceConfigurationUtils.AddParameterValue(condition, "relation", relName);
			InstanceConfigurationUtils.AddParameterValue(condition, "join", JoinType.InnerJoin);
			InstanceConfigurationUtils.AddParameterValue(condition, "nearClasses", mds2);
			InstanceConfigurationUtils.AddParameterValue(condition, "maximumDistance", 5);
			InstanceConfigurationUtils.AddParameterValue(condition, "relevantRelationCondition",
			                                             string.Empty);

			var fact = new QaRelMustBeNearOther();
			fact.Condition = condition;

			IList<ITest> tests =
				fact.CreateTests(new SimpleDatasetOpener(model.MasterDatabaseWorkspaceContext));
			Assert.AreEqual(1, tests.Count);

			var runner = new QaContainerTestRunner(1000, tests[0]);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[NotNull]
		private static QaContainerTestRunner CreateTestRunner(
			[NotNull] IFeatureClass featureClass,
			double tileSize,
			double maximumDistance)
		{
			var test = new QaMustBeNearOther(
				ReadOnlyTableFactory.Create(featureClass),
				new[] { ReadOnlyTableFactory.Create(featureClass) },
				maximumDistance, null);
			var runner = new QaContainerTestRunner(tileSize, test);

			test.ErrorDistanceFormat = "{0:N2} m";

			return runner;
		}

		[NotNull]
		private static QaContainerTestRunner CreateTestRunner(
			[NotNull] IFeatureClass featureClass,
			[NotNull] IFeatureClass nearClass,
			double tileSize,
			double maximumDistance)
		{
			var test = new QaMustBeNearOther(
				ReadOnlyTableFactory.Create(featureClass),
				new[] { ReadOnlyTableFactory.Create(nearClass) },
				maximumDistance, null);
			var runner = new QaContainerTestRunner(tileSize, test);

			test.ErrorDistanceFormat = "{0:N2} m";

			return runner;
		}

		private static void AddPoints([NotNull] IFeatureClass fc)
		{
			IFeature row1 = fc.CreateFeature();
			row1.Shape = GeometryFactory.CreatePoint(10, 10);
			row1.Store();

			IFeature row2 = fc.CreateFeature();
			row2.Shape = GeometryFactory.CreatePoint(11, 11);
			row2.Store();

			IFeature row3 = fc.CreateFeature();
			row3.Shape = GeometryFactory.CreatePoint(20, 20);
			row3.Store();

			IFeature row4 = fc.CreateFeature();
			row4.Shape = GeometryFactory.CreatePoint(40, 40);
			row4.Store();
		}

		private static IFeatureClass CreatePointClass([NotNull] IFeatureWorkspace ws,
		                                              [NotNull] string name)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPoint,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			return DatasetUtils.CreateSimpleFeatureClass(ws, name, fields, null);
		}

		private static void AddLines([NotNull] IFeatureClass fc)
		{
			AddFeature(fc, CurveConstruction.StartLine(10, 10)
			                                .LineTo(9, 9)
			                                .Curve);

			AddFeature(fc, CurveConstruction.StartLine(11, 11)
			                                .LineTo(12, 12)
			                                .Curve);

			AddFeature(fc, CurveConstruction.StartLine(20, 20)
			                                .LineTo(21, 21)
			                                .Curve);

			AddFeature(fc, CurveConstruction.StartLine(40, 40)
			                                .LineTo(41, 41)
			                                .Curve);
		}

		private static IFeature AddFeature([NotNull] IFeatureClass fc,
		                                   [NotNull] IGeometry shape,
		                                   IList<object> values = null)
		{
			IFeature feature = fc.CreateFeature();
			feature.Shape = shape;

			if (values != null)
			{
				for (var i = 0; i < values.Count; i++)
				{
					feature.set_Value(i + 1, values[i]);
				}
			}

			feature.Store();
			return feature;
		}

		private static IRow AddRow([NotNull] ITable table, [NotNull] IList<object> values)
		{
			IRow row = table.CreateRow();

			for (var i = 0; i < values.Count; i++)
			{
				row.set_Value(i + 1, values[i]);
			}

			row.Store();
			return row;
		}

		[NotNull]
		private static IFeatureClass CreateLineClass([NotNull] IFeatureWorkspace ws,
		                                             [NotNull] string name,
		                                             [CanBeNull] IList<IField> addFields = null)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			if (addFields != null)
			{
				foreach (IField addField in addFields)
				{
					fields.AddField(addField);
				}
			}

			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			return DatasetUtils.CreateSimpleFeatureClass(ws, name, fields, null);
		}
	}
}
