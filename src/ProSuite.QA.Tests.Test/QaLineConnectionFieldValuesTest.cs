using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	[CLSCompliant(false)]
	public class QaLineConnectionFieldValuesTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();

			_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace("QaLineConnectionFieldValues");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void TestFieldValues()
		{
			TestFieldValues(_testWs);
		}

		private static void TestFieldValues(IFeatureWorkspace ws)
		{
			IFieldsEdit lineFields = new FieldsClass();
			lineFields.AddField(FieldUtils.CreateOIDField());
			lineFields.AddField(FieldUtils.CreateTextField("FromVal", 50));
			lineFields.AddField(FieldUtils.CreateTextField("ToVal", 50));
			lineFields.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline,
				                    SpatialReferenceUtils.CreateSpatialReference
				                    ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                     true), 1000));

			IFeatureClass lineFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TestFieldValues1", lineFields);

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateTextField("Name", 50));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPoint,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass pointFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TestFieldValues2", fields);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IPoint center = new PointClass();
			center.PutCoords(200, 200);

			IFeature row1 = lineFc.CreateFeature();
			row1.set_Value(1, "Name");
			row1.Shape = CurveConstruction.StartLine(100, 100).LineTo(center).Curve;
			row1.Store();

			IFeature row2 = lineFc.CreateFeature();
			row2.set_Value(1, "Name");
			row2.Shape = CurveConstruction.StartLine(200, 100).LineTo(center).Curve;
			row2.Store();

			IFeature row3 = lineFc.CreateFeature();
			row3.set_Value(1, "Name");
			row3.Shape = CurveConstruction.StartLine(300, 100).LineTo(center).Curve;
			row3.Store();

			IFeature row4 = lineFc.CreateFeature();
			row4.set_Value(1, "Name");
			row4.Shape = CurveConstruction.StartLine(300, 100).LineTo(400, 100).Curve;
			row4.Store();

			IFeature row5 = pointFc.CreateFeature();
			row5.set_Value(1, "Name");
			row5.Shape = center;
			row5.Store();

			// configure and run the test
			var test = new QaLineConnectionFieldValues(
				lineFc, "FromVal",
				LineFieldValuesConstraint.AllEqualOrValidPointExists,
				pointFc, "Name",
				PointFieldValuesConstraint.AllEqualAndMatchAnyLineValue);

			var runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(0, runner.Errors.Count);

			var containerRunner = new QaContainerTestRunner(500, test);
			containerRunner.Execute();
			Assert.AreEqual(0, containerRunner.Errors.Count);
		}

		[Test]
		public void TestFieldValuesError()
		{
			TestFieldValuesError(_testWs);
		}

		[Test]
		public void TestUniqueLineFieldValuesError()
		{
			TestUniqueLineFieldValuesError(_testWs);
		}

		private static void TestFieldValuesError(IFeatureWorkspace ws)
		{
			IFieldsEdit lineFields = new FieldsClass();
			lineFields.AddField(FieldUtils.CreateOIDField());
			lineFields.AddField(FieldUtils.CreateTextField("FromVal", 50));
			lineFields.AddField(FieldUtils.CreateTextField("ToVal", 50));
			lineFields.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline,
				                    SpatialReferenceUtils.CreateSpatialReference
				                    ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                     true), 1000));

			IFeatureClass lineFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TestFieldValuesError1", lineFields);

			IFieldsEdit pointFields = new FieldsClass();
			pointFields.AddField(FieldUtils.CreateOIDField());
			pointFields.AddField(FieldUtils.CreateTextField("Name", 50));
			pointFields.AddField(FieldUtils.CreateShapeField(
				                     "Shape", esriGeometryType.esriGeometryPoint,
				                     SpatialReferenceUtils.CreateSpatialReference
				                     ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                      true), 1000));

			IFeatureClass pointFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TestFieldValuesError2", pointFields);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IPoint center = new PointClass();
			center.PutCoords(200, 200);

			IFeature row1 = lineFc.CreateFeature();
			row1.set_Value(1, "Name");
			row1.Shape = CurveConstruction.StartLine(100, 100).LineTo(center).Curve;
			row1.Store();

			IFeature row2 = lineFc.CreateFeature();
			row2.set_Value(1, "Name");
			row2.Shape = CurveConstruction.StartLine(200, 100).LineTo(center).Curve;
			row2.Store();

			IFeature row3 = lineFc.CreateFeature();
			row3.set_Value(1, "Name");
			row3.Shape = CurveConstruction.StartLine(300, 100).LineTo(center).Curve;
			row3.Store();

			// unconnected line
			IFeature row4 = lineFc.CreateFeature();
			row4.set_Value(1, "AndererName");
			row4.Shape = CurveConstruction.StartLine(300, 100).LineTo(400, 100).Curve;
			row4.Store();

			// point connected to line rows 1,2,3
			IFeature row5 = pointFc.CreateFeature();
			row5.set_Value(1, "AndererName");
			row5.Shape = center;
			row5.Store();

			// configure and run the test
			var test = new QaLineConnectionFieldValues(
				lineFc, "FromVal",
				LineFieldValuesConstraint.AllEqualOrValidPointExists,
				pointFc, "Name",
				PointFieldValuesConstraint.AllEqualAndMatchMostFrequentLineValue);

			var runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(2, runner.Errors.Count);

			var containerRunner = new QaContainerTestRunner(500, test);
			containerRunner.Execute();
			Assert.AreEqual(2, containerRunner.Errors.Count);
		}

		private static void TestUniqueLineFieldValuesError(IFeatureWorkspace ws)
		{
			IFieldsEdit lineFields = new FieldsClass();
			lineFields.AddField(FieldUtils.CreateOIDField());
			lineFields.AddField(FieldUtils.CreateTextField("Name", 50));
			lineFields.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline,
				                    SpatialReferenceUtils.CreateSpatialReference
				                    ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                     true), 1000));

			IFeatureClass lineFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TestUniqueLineFieldValuesError1", lineFields);

			IFieldsEdit pointFields = new FieldsClass();
			pointFields.AddField(FieldUtils.CreateOIDField());
			pointFields.AddField(FieldUtils.CreateShapeField(
				                     "Shape", esriGeometryType.esriGeometryPoint,
				                     SpatialReferenceUtils.CreateSpatialReference
				                     ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                      true), 1000));

			IFeatureClass pointFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TestUniqueLineFieldValuesError2", pointFields);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IPoint center = new PointClass();
			center.PutCoords(200, 200);

			IFeature line1 = lineFc.CreateFeature();
			line1.set_Value(1, "UniqueValue");
			line1.Shape = CurveConstruction.StartLine(100, 100).LineTo(center).Curve;
			line1.Store();

			IFeature line2 = lineFc.CreateFeature();
			line2.set_Value(1, "DuplicateValue");
			line2.Shape = CurveConstruction.StartLine(200, 100).LineTo(center).Curve;
			line2.Store();

			IFeature line3 = lineFc.CreateFeature();
			line3.set_Value(1, "DuplicateValue");
			line3.Shape = CurveConstruction.StartLine(300, 100).LineTo(center).Curve;
			line3.Store();

			// line connected to line4 (no point at junction) --> ERROR
			IFeature line4 = lineFc.CreateFeature();
			line4.set_Value(1, "DuplicateValue");
			line4.Shape = CurveConstruction.StartLine(300, 100).LineTo(400, 100).Curve;
			line4.Store();

			// line connected to row4 (no point at junction, but different field value)
			IFeature row5 = lineFc.CreateFeature();
			row5.set_Value(1, "OtherValue");
			row5.Shape = CurveConstruction.StartLine(400, 100).LineTo(500, 100).Curve;
			row5.Store();

			// point connected to line rows 1,2,3
			IFeature point = pointFc.CreateFeature();
			point.Shape = center;
			point.Store();

			// configure and run the test
			var test = new QaLineConnectionFieldValues(
				lineFc, "Name",
				LineFieldValuesConstraint.UniqueOrValidPointExists,
				pointFc, null,
				PointFieldValuesConstraint.NoConstraint);

			var runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);

			var containerRunner = new QaContainerTestRunner(1000, test);
			containerRunner.Execute();
			Assert.AreEqual(1, containerRunner.Errors.Count);
		}

		[Test]
		public void TestFieldValuesExpression()
		{
			TestFieldValuesExpression(_testWs);
		}

		private static void TestFieldValuesExpression(IFeatureWorkspace ws)
		{
			IFieldsEdit lineFields = new FieldsClass();
			lineFields.AddField(FieldUtils.CreateOIDField());
			lineFields.AddField(FieldUtils.CreateTextField("FromVal", 50));
			lineFields.AddField(FieldUtils.CreateTextField("ToVal", 50));
			lineFields.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline,
				                    SpatialReferenceUtils.CreateSpatialReference
				                    ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                     true), 1000));

			IFeatureClass lineFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TestFieldValuesExpression1", lineFields);

			IFieldsEdit pointFields = new FieldsClass();
			pointFields.AddField(FieldUtils.CreateOIDField());
			pointFields.AddField(FieldUtils.CreateTextField("Name", 50));
			pointFields.AddField(FieldUtils.CreateShapeField(
				                     "Shape", esriGeometryType.esriGeometryPoint,
				                     SpatialReferenceUtils.CreateSpatialReference
				                     ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                      true), 1000));

			IFeatureClass pointFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TestFieldValuesExpression2", pointFields);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IPoint center = new PointClass();
			center.PutCoords(200, 200);

			IFeature row1 = lineFc.CreateFeature();
			row1.set_Value(1, "Name");
			row1.set_Value(2, "AndererName");
			row1.Shape = CurveConstruction.StartLine(100, 100).LineTo(center).Curve;
			row1.Store();

			IFeature row2 = lineFc.CreateFeature();
			row2.set_Value(1, "Name");
			row2.Shape = CurveConstruction.StartLine(200, 100).LineTo(center).Curve;
			row2.Store();

			IFeature row3 = lineFc.CreateFeature();
			row3.set_Value(1, "Name");
			row3.Shape = CurveConstruction.StartLine(300, 100).LineTo(center).Curve;
			row3.Store();

			IFeature row4 = pointFc.CreateFeature();
			row4.set_Value(1, "AndererName");
			row4.Shape = center;
			row4.Store();

			string lineField = $"IIF({QaConnections.StartsIn}, FromVal, ToVal)";
			var test = new QaLineConnectionFieldValues(
				lineFc, lineField,
				LineFieldValuesConstraint.AllEqualOrValidPointExists,
				pointFc, "Name",
				PointFieldValuesConstraint.AllEqualAndMatchAnyLineValue);

			var runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(0, runner.Errors.Count);

			var containerRunner = new QaContainerTestRunner(500, test);
			containerRunner.Execute();
			Assert.AreEqual(0, containerRunner.Errors.Count);
		}

		[Test]
		public void TestMultipart()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TestMultipart");
			IFieldsEdit lineFields = new FieldsClass();
			lineFields.AddField(FieldUtils.CreateOIDField());
			lineFields.AddField(FieldUtils.CreateTextField("FromVal", 50));
			lineFields.AddField(FieldUtils.CreateTextField("ToVal", 50));
			lineFields.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline,
				                    SpatialReferenceUtils.CreateSpatialReference
				                    ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                     true), 1000));

			IFeatureClass lineFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TestFieldValuesExpression1", lineFields);

			IFieldsEdit pointFields = new FieldsClass();
			pointFields.AddField(FieldUtils.CreateOIDField());
			pointFields.AddField(FieldUtils.CreateTextField("Name", 50));
			pointFields.AddField(FieldUtils.CreateShapeField(
				                     "Shape", esriGeometryType.esriGeometryPoint,
				                     SpatialReferenceUtils.CreateSpatialReference
				                     ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                      true), 1000));

			IFeatureClass pointFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TestFieldValuesExpression2", pointFields);

			IPoint center = new PointClass();
			center.PutCoords(200, 200);

			IFeature row1 = lineFc.CreateFeature();
			row1.set_Value(1, "Name");
			row1.set_Value(2, "AndererName");
			row1.Shape =
				CurveConstruction.StartLine(100, 100).LineTo(200, 160).MoveTo(100, 160)
				                 .LineTo(center).Curve;
			row1.Store();

			IFeature row2 = lineFc.CreateFeature();
			row2.set_Value(1, "Name");
			row2.Shape = CurveConstruction
			             .StartLine(200, 100).LineTo(300, 140).MoveTo(200, 160).LineTo(center)
			             .Curve;
			row2.Store();

			IFeature row3 = lineFc.CreateFeature();
			row3.set_Value(1, "Name");
			row3.Shape = CurveConstruction
			             .StartLine(300, 100).LineTo(300, 140).MoveTo(300, 160).LineTo(center)
			             .Curve;
			row3.Store();

			IFeature row4 = pointFc.CreateFeature();
			row4.set_Value(1, "AndererName");
			row4.Shape = center;
			row4.Store();

			string lineField = $"IIF({QaConnections.StartsIn}, FromVal, ToVal)";
			var test = new QaLineConnectionFieldValues(
				lineFc, lineField,
				LineFieldValuesConstraint.AllEqualOrValidPointExists,
				pointFc, "Name",
				PointFieldValuesConstraint.AllEqualAndMatchAnyLineValue);

			test.UseMultiParts = false;
			var runner = new QaContainerTestRunner(500, test);
			Assert.AreEqual(0, runner.Execute());

			test.UseMultiParts = true;
			runner = new QaContainerTestRunner(500, test);
			Assert.AreEqual(1, runner.Execute());
		}

		[Test]
		public void TestMultiTables()
		{
			TestMultiTables(_testWs);
		}

		private static void TestMultiTables(IFeatureWorkspace ws)
		{
			IFieldsEdit lineFields = new FieldsClass();
			lineFields.AddField(FieldUtils.CreateOIDField());
			lineFields.AddField(FieldUtils.CreateTextField("FromVal1", 50));
			lineFields.AddField(FieldUtils.CreateTextField("ToVal1", 50));
			lineFields.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline,
				                    SpatialReferenceUtils.CreateSpatialReference
				                    ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                     true), 1000));

			IFeatureClass lineFc1 = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TestMultiTablesLineFc1", lineFields);

			lineFields = new FieldsClass();
			lineFields.AddField(FieldUtils.CreateOIDField());
			lineFields.AddField(FieldUtils.CreateTextField("FromVal2", 50));
			lineFields.AddField(FieldUtils.CreateTextField("ToVal2", 50));
			lineFields.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline,
				                    SpatialReferenceUtils.CreateSpatialReference
				                    ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                     true), 1000));

			IFeatureClass lineFc2 = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TestMultiTablesLineFc2", lineFields);

			IFieldsEdit pointFields = new FieldsClass();
			pointFields.AddField(FieldUtils.CreateOIDField());
			pointFields.AddField(FieldUtils.CreateTextField("Name1", 50));
			pointFields.AddField(FieldUtils.CreateShapeField(
				                     "Shape", esriGeometryType.esriGeometryPoint,
				                     SpatialReferenceUtils.CreateSpatialReference
				                     ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                      true), 1000));

			IFeatureClass pointFc1 = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TestMultiTablesPointFc1", pointFields);

			pointFields = new FieldsClass();
			pointFields.AddField(FieldUtils.CreateOIDField());
			pointFields.AddField(FieldUtils.CreateTextField("Name2", 50));
			pointFields.AddField(FieldUtils.CreateShapeField(
				                     "Shape", esriGeometryType.esriGeometryPoint,
				                     SpatialReferenceUtils.CreateSpatialReference
				                     ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                      true), 1000));

			IFeatureClass pointFc2 = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TestMultiTablesPointFc2", pointFields);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IPoint center = new PointClass();
			center.PutCoords(200, 200);

			IFeature row1 = lineFc1.CreateFeature();
			row1.set_Value(1, "Name");
			row1.set_Value(2, "AndererName");
			row1.Shape = CurveConstruction.StartLine(100, 100).LineTo(center).Curve;
			row1.Store();

			IFeature row2 = lineFc1.CreateFeature();
			row2.set_Value(1, "Name");
			row2.Shape = CurveConstruction.StartLine(200, 100).LineTo(center).Curve;
			row2.Store();

			IFeature row3 = lineFc2.CreateFeature();
			row3.set_Value(1, "Name");
			row3.Shape = CurveConstruction.StartLine(300, 100).LineTo(center).Curve;
			row3.Store();

			IFeature row4 = pointFc1.CreateFeature();
			row4.set_Value(1, "AndererName");
			row4.Shape = center;
			row4.Store();

			IFeature row5 = pointFc2.CreateFeature();
			row5.set_Value(1, "AndererName");
			row5.Shape = center;
			row5.Store();

			IFeature row6 = pointFc2.CreateFeature();
			row6.set_Value(1, "Ignore2");
			row6.Shape = center;
			row6.Store();

			string lineField = string.Format("IIF({0}, FromVal1, ToVal1)",
			                                 QaConnections.StartsIn);
			var test = new QaLineConnectionFieldValues(
				new[] {lineFc1, lineFc2}, new[] {lineField, "FromVal2"},
				LineFieldValuesConstraint.AllEqualOrValidPointExists,
				new[] {pointFc1, pointFc2}, new[] {"Name1", "Name2"},
				PointFieldValuesConstraint.AllEqualAndMatchAnyLineValue,
				new[] {null, "Name2 = 'Ignore2'"});

			var runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(0, runner.Errors.Count);

			var containerRunner = new QaContainerTestRunner(500, test);
			containerRunner.Execute();
			Assert.AreEqual(0, containerRunner.Errors.Count);
		}
	}
}
