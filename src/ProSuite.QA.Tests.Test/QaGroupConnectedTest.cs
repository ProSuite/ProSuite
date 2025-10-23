using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.TestFactories;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaGroupConnectedTest
	{
		private IFeatureWorkspace _fgdbWorkspace;

		private const string DatabaseName = "QaGroupConnectedTest";

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense(activateAdvancedLicense: true);

			_fgdbWorkspace = TestWorkspaceUtils.CreateTestFgdbWorkspace(DatabaseName);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		private static void TestGroupConnected(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Group",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "Fc", fields);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 10);
			f1.Shape = CurveConstruction.StartLine(100, 100)
			                            .LineTo(200, 200)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(1, 10);
			f2.Shape = CurveConstruction.StartLine(200, 200)
			                            .LineTo(300, 200)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc.CreateFeature();
			f3.set_Value(1, 10);
			f3.Shape = CurveConstruction.StartLine(300, 200)
			                            .LineTo(400, 100)
			                            .Curve;
			f3.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(ReadOnlyTableFactory.Create(fc),
			                                new List<string> { "Group" },
			                                ShapeAllowed.All);

			TestRunnerUtils.RunTests(test, 0, 1000);

			TestRunnerUtils.RunTests(test, 0, 20);

			TestRunnerUtils.RunTests(test,
			                         GeometryFactory.CreateEnvelope(50, 50, 500, 150),
			                         0,
			                         20);

			TestRunnerUtils.RunTests(test,
			                         GeometryFactory.CreateEnvelope(50, 50, 500, 250),
			                         0,
			                         20);
		}

		private static void TestCircularErrors(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Group",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "TestCircularErrors",
				fields);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 10);
			f1.Shape =
				CurveConstruction.StartLine(100, 100)
				                 .LineTo(200, 100)
				                 .Line(100, 200)
				                 .Curve;
			f1.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(1, 10);
			f2.Shape = CurveConstruction.StartLine(100, 200)
			                            .LineTo(100, 100)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc.CreateFeature();
			f3.set_Value(1, 10);
			f3.Shape = CurveConstruction.StartLine(100, 100)
			                            .LineTo(105, 105)
			                            .Curve;
			f3.Store();

			IFeature f4 = fc.CreateFeature();
			f4.set_Value(1, 10);
			f4.Shape =
				CurveConstruction.StartLine(105, 105)
				                 .LineTo(110, 105)
				                 .LineTo(105, 110)
				                 .Curve;
			f4.Store();

			IFeature f5 = fc.CreateFeature();
			f5.set_Value(1, 10);
			f5.Shape = CurveConstruction.StartLine(105, 110)
			                            .LineTo(105, 105)
			                            .Curve;
			f5.Store();

			IFeature f6 = fc.CreateFeature();
			f6.set_Value(1, 10);
			f6.Shape = CurveConstruction.StartLine(95, 95)
			                            .LineTo(100, 100)
			                            .Curve;
			f6.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(new[] { ReadOnlyTableFactory.Create(fc) },
			                                new List<string> { "Group" }, null,
			                                ShapeAllowed.InsideBranches,
			                                GroupErrorReporting.CombineParts,
			                                50);

			var runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };

			runner.Execute();
			Assert.AreEqual(2, runner.Errors.Count);
		}

		private static void TestMultiPartErrors(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Group",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "TestMultiPartErrors",
				fields);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 10);
			f1.Shape = CurveConstruction.StartLine(100, 100)
			                            .LineTo(190, 190)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(1, 10);
			f2.Shape = CurveConstruction.StartLine(200, 200)
			                            .LineTo(290, 200)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc.CreateFeature();
			f3.set_Value(1, 10);
			f3.Shape = CurveConstruction.StartLine(300, 200)
			                            .LineTo(150, 240)
			                            .Curve;
			f3.Store();

			IFeature f4 = fc.CreateFeature();
			f4.set_Value(1, 10);
			f4.Shape = CurveConstruction.StartLine(150, 250)
			                            .LineTo(100, 106)
			                            .Curve;
			f4.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(new[] { ReadOnlyTableFactory.Create(fc) },
			                                new List<string> { "Group" }, null,
			                                ShapeAllowed.All,
			                                GroupErrorReporting.CombineParts,
			                                7);

			var runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };

			runner.Execute();
		}

		private static void TestMultiPartErrorsTestextent(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("GroupField",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "TestMultiPartErrorsTestextent", fields);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 10);
			f1.Shape = CurveConstruction.StartLine(100, 100)
			                            .LineTo(190, 190)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(1, 10);
			f2.Shape = CurveConstruction.StartLine(200, 200)
			                            .LineTo(290, 200)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc.CreateFeature();
			f3.set_Value(1, 10);
			f3.Shape = CurveConstruction.StartLine(290, 200)
			                            .LineTo(290, 250)
			                            .Curve;
			f3.Store();

			IFeature f4 = fc.CreateFeature();
			f4.set_Value(1, 10);
			f4.Shape = CurveConstruction.StartLine(200, 200)
			                            .LineTo(200, 500)
			                            .Curve;
			f4.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(new[] { ReadOnlyTableFactory.Create(fc) },
			                                new List<string> { "GroupField" }, null,
			                                ShapeAllowed.All,
			                                GroupErrorReporting.CombineParts,
			                                7);

			var runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };

			IEnvelope box0 = GeometryFactory.CreateEnvelope(150, 150, 195, 195);
			IEnvelope box1 = GeometryFactory.CreateEnvelope(0, 0, 195, 195);
			IEnvelope box2 = GeometryFactory.CreateEnvelope(150, 150, 210, 210);
			IEnvelope box3 = GeometryFactory.CreateEnvelope(0, 0, 210, 210);

			Assert.AreEqual(0, runner.Execute(box0));
			Assert.AreEqual(0, runner.Execute(box1));
			Assert.AreEqual(0, runner.Execute(box2));
			Assert.AreEqual(1, runner.Execute(box3));

			test.CompleteGroupsOutsideTestArea = true;
			Assert.AreEqual(1, runner.Execute(box0));
			Assert.AreEqual(1, runner.Execute(box1));
			Assert.AreEqual(1, runner.Execute(box2));
			Assert.AreEqual(1, runner.Execute(box3));
		}

		private static void TestCircularMultiPartErrors(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Group",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws,
				"TestCircularMultiPartErrors",
				fields);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 10);
			f1.Shape = CurveConstruction.StartLine(100, 100)
			                            .Line(10, 0)
			                            .Line(0, 10)
			                            .Line(-10, -10)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(1, 10);
			f2.Shape = CurveConstruction.StartLine(200, 200)
			                            .Line(10, 0)
			                            .Line(0, 10)
			                            .Line(-10, -10)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc.CreateFeature();
			f3.set_Value(1, 10);
			f3.Shape = CurveConstruction.StartLine(300, 200)
			                            .Line(10, 0)
			                            .Line(0, 10)
			                            .Line(-10, -10)
			                            .Curve;
			f3.Store();

			IFeature f4 = fc.CreateFeature();
			f4.set_Value(1, 10);
			f4.Shape = CurveConstruction.StartLine(300, 201)
			                            .Line(10, 0)
			                            .Line(0, 10)
			                            .Line(-10, -10)
			                            .Curve;
			f4.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(new[] { ReadOnlyTableFactory.Create(fc) },
			                                new List<string> { "Group" }, null,
			                                ShapeAllowed.All,
			                                GroupErrorReporting.CombineParts,
			                                50);

			var runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };

			runner.Execute();
		}

		private static void TestMultiFeatureClassSameAttributeNames(IFeatureWorkspace ws)
		{
			IFieldsEdit fields1 = new FieldsClass();
			fields1.AddField(FieldUtils.CreateOIDField());
			fields1.AddField(FieldUtils.CreateField("Group",
			                                        esriFieldType.esriFieldTypeString));
			fields1.AddField(FieldUtils.CreateShapeField(
				                 "Shape", esriGeometryType.esriGeometryPolyline,
				                 SpatialReferenceUtils.CreateSpatialReference
				                 ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                  true), 1000));

			IFeatureClass fc1 = DatasetUtils.CreateSimpleFeatureClass(ws,
				"TestMultiFeatureClass1",
				fields1);

			IFieldsEdit fields2 = new FieldsClass();
			fields2.AddField(FieldUtils.CreateOIDField());
			fields2.AddField(FieldUtils.CreateField("Dummy",
			                                        esriFieldType.esriFieldTypeInteger));
			fields2.AddField(FieldUtils.CreateField("Group",
			                                        esriFieldType.esriFieldTypeString));
			fields2.AddField(FieldUtils.CreateShapeField(
				                 "Shape", esriGeometryType.esriGeometryPolyline,
				                 SpatialReferenceUtils.CreateSpatialReference
				                 ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                  true), 1000));
			IFeatureClass fc2 = DatasetUtils.CreateSimpleFeatureClass(ws,
				"TestMultiFeatureClass2",
				fields2);
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc1.CreateFeature();
			f1.set_Value(1, "A");
			f1.Shape = CurveConstruction.StartLine(100, 100)
			                            .LineTo(200, 200)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc2.CreateFeature();
			f2.set_Value(1, 10);
			f2.set_Value(2, "A");
			f2.Shape = CurveConstruction.StartLine(200, 200)
			                            .LineTo(300, 200)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc1.CreateFeature();
			f3.set_Value(1, "A");
			f3.Shape = CurveConstruction.StartLine(300, 200)
			                            .LineTo(400, 100)
			                            .Curve;
			f3.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(
				new[] { ReadOnlyTableFactory.Create(fc1), ReadOnlyTableFactory.Create(fc2) },
				new List<string> { "Group" },
				null, ShapeAllowed.All,
				GroupErrorReporting.CombineParts,
				10);

			TestRunnerUtils.RunTests(test, 0, 1000);
		}

		private static void TestMultiFeatureClassDiffAttributeNames(IFeatureWorkspace ws)
		{
			IFieldsEdit fields1 = new FieldsClass();
			fields1.AddField(FieldUtils.CreateOIDField());
			fields1.AddField(FieldUtils.CreateField("GroupName",
			                                        esriFieldType.esriFieldTypeString));
			fields1.AddField(FieldUtils.CreateShapeField(
				                 "Shape", esriGeometryType.esriGeometryPolyline,
				                 SpatialReferenceUtils.CreateSpatialReference
				                 ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                  true), 1000));

			IFeatureClass fc1 =
				DatasetUtils.CreateSimpleFeatureClass(ws, "DiffAttrs1", fields1);

			IFieldsEdit fields2 = new FieldsClass();
			fields2.AddField(FieldUtils.CreateOIDField());
			fields2.AddField(FieldUtils.CreateField("Dummy",
			                                        esriFieldType.esriFieldTypeInteger));
			fields2.AddField(FieldUtils.CreateField("OtherName",
			                                        esriFieldType.esriFieldTypeString));
			fields2.AddField(FieldUtils.CreateShapeField(
				                 "Shape", esriGeometryType.esriGeometryPolyline,
				                 SpatialReferenceUtils.CreateSpatialReference
				                 ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                  true), 1000));
			IFeatureClass fc2 =
				DatasetUtils.CreateSimpleFeatureClass(ws, "DiffAttrs2", fields2);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc1.CreateFeature();
			f1.set_Value(1, "A");
			f1.Shape = CurveConstruction.StartLine(100, 100)
			                            .LineTo(200, 200)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc2.CreateFeature();
			f2.set_Value(1, 10);
			f2.set_Value(2, "A");
			f2.Shape = CurveConstruction.StartLine(200, 200)
			                            .LineTo(300, 200)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc1.CreateFeature();
			f3.set_Value(1, "A");
			f3.Shape = CurveConstruction.StartLine(300, 200)
			                            .LineTo(400, 100)
			                            .Curve;
			f3.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(
				new[] { ReadOnlyTableFactory.Create(fc1), ReadOnlyTableFactory.Create(fc2) },
				new List<string> { "GroupName;OtherName" },
				null, ShapeAllowed.All,
				GroupErrorReporting.CombineParts,
				10);

			TestRunnerUtils.RunTests(test, 0, 1000);
		}

		private static void TestMultiFeatureClassValueGroups(IFeatureWorkspace ws)
		{
			IFieldsEdit fields1 = new FieldsClass();
			fields1.AddField(FieldUtils.CreateOIDField());
			fields1.AddField(FieldUtils.CreateField("GroupName",
			                                        esriFieldType.esriFieldTypeString));
			fields1.AddField(FieldUtils.CreateShapeField(
				                 "Shape", esriGeometryType.esriGeometryPolyline,
				                 SpatialReferenceUtils.CreateSpatialReference
				                 ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                  true), 1000));

			IFeatureClass fc1 = DatasetUtils.CreateSimpleFeatureClass(ws, "ValueGroups1",
				fields1);

			IFieldsEdit fields2 = new FieldsClass();
			fields2.AddField(FieldUtils.CreateOIDField());
			fields2.AddField(FieldUtils.CreateField("Dummy",
			                                        esriFieldType.esriFieldTypeInteger));
			fields2.AddField(FieldUtils.CreateField("OtherName",
			                                        esriFieldType.esriFieldTypeString));
			fields2.AddField(FieldUtils.CreateShapeField(
				                 "Shape", esriGeometryType.esriGeometryPolyline,
				                 SpatialReferenceUtils.CreateSpatialReference
				                 ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                  true), 1000));
			IFeatureClass fc2 = DatasetUtils.CreateSimpleFeatureClass(ws, "ValueGroups2",
				fields2);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc1.CreateFeature();
			f1.set_Value(1, "A|B|C");
			f1.Shape = CurveConstruction.StartLine(100, 100)
			                            .LineTo(200, 200)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc2.CreateFeature();
			f2.set_Value(1, 10);
			f2.set_Value(2, "A-B-D");
			f2.Shape = CurveConstruction.StartLine(200, 200)
			                            .LineTo(300, 200)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc1.CreateFeature();
			f3.set_Value(1, "A|C|D");
			f3.Shape = CurveConstruction.StartLine(300, 200)
			                            .LineTo(400, 100)
			                            .Curve;
			f3.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			const string groupByProposal =
				@"<Fields separator='?'><Field name='GroupName' separator='|' /><Field name='OtherName' separator='-' /><\Fields>";
			Assert.IsNotNull(groupByProposal);
			var test = new QaGroupConnected(
				new[] { ReadOnlyTableFactory.Create(fc1), ReadOnlyTableFactory.Create(fc2) },
				new List<string> { "GroupName(|);OtherName(-)" },
				null, ShapeAllowed.All,
				GroupErrorReporting.CombineParts,
				10);

			TestRunnerUtils.RunTests(test, 1, 1000); // Group C is not connected
		}

		private static void CanDetectConnectedGroups_1toN_1([NotNull] IFeatureWorkspace ws)
		{
			// NOTE: field name "group" is not allowed in the join condition (ON ...)

			const string groupFieldName = "LINEGROUP";
			const string foreignKeyFieldName = "FK";
			const string groupTableName = "LineGroups_1";

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField(foreignKeyFieldName,
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "Lines", fields);

			ITable lineGroupTable = DatasetUtils.CreateTable(
				ws, groupTableName,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateField(groupFieldName, esriFieldType.esriFieldTypeInteger));

			IRelationshipClass relClass = TestWorkspaceUtils.CreateSimple1NRelationship(
				ws, "relConnected", lineGroupTable, (ITable) fc,
				lineGroupTable.OIDFieldName, foreignKeyFieldName);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			int groupFieldIndex = lineGroupTable.FindField(groupFieldName);
			int foreignKeyFieldIndex = fc.FindField(foreignKeyFieldName);

			IRow row1 = lineGroupTable.CreateRow();
			row1.set_Value(groupFieldIndex, 10);
			row1.Store();

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(foreignKeyFieldIndex, row1.OID);
			f1.Shape = CurveConstruction.StartLine(100, 100)
			                            .LineTo(200, 200)
			                            .Curve;
			f1.Store();

			IRow row2 = lineGroupTable.CreateRow();
			row2.set_Value(groupFieldIndex, 10);
			row2.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(foreignKeyFieldIndex, row2.OID);
			f2.Shape = CurveConstruction.StartLine(200, 200)
			                            .LineTo(300, 200)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc.CreateFeature();
			f3.set_Value(foreignKeyFieldIndex, row2.OID);
			f3.Shape = CurveConstruction.StartLine(300, 200)
			                            .LineTo(400, 100)
			                            .Curve;
			f3.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var queryFeatureClass = (IFeatureClass) TableJoinUtils.CreateQueryTable(
				relClass, JoinType.InnerJoin);

			var test = new QaGroupConnected(ReadOnlyTableFactory.Create(queryFeatureClass),
			                                new List<string>
			                                {
				                                groupTableName + "." +
				                                groupFieldName
			                                },
			                                ShapeAllowed.All);

			// succeeds:
			TestRunnerUtils.RunTests(test, 0, 1000);

			// succeeds:
			TestRunnerUtils.RunTests(test,
			                         GeometryFactory.CreateEnvelope(50, 50, 500, 150),
			                         0,
			                         20);

			// FAILS
			TestRunnerUtils.RunTests(test, 0, 20);

			// FAILS
			TestRunnerUtils.RunTests(test,
			                         GeometryFactory.CreateEnvelope(50, 50, 500, 250),
			                         0,
			                         20);
		}

		private static void CanDetectConnectedGroups_1toN_2([NotNull] IFeatureWorkspace ws)
		{
			// NOTE: field name "group" is not allowed in the join condition (ON ...)

			const string groupFieldName = "LINEGROUP";
			const string foreignKeyFieldName = "FK";
			const string groupTableName = "LineGroups_2";

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField(foreignKeyFieldName,
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "Lines1", fields);

			ITable lineGroupTable = DatasetUtils.CreateTable(
				ws, groupTableName,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateField(groupFieldName, esriFieldType.esriFieldTypeInteger));

			IRelationshipClass rel = TestWorkspaceUtils.CreateSimple1NRelationship(
				ws, "relConnected1", lineGroupTable, (ITable) fc,
				lineGroupTable.OIDFieldName, foreignKeyFieldName);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			int groupFieldIndex = lineGroupTable.FindField(groupFieldName);
			int foreignKeyFieldIndex = fc.FindField(foreignKeyFieldName);

			IRow row1 = lineGroupTable.CreateRow();
			row1.set_Value(groupFieldIndex, 10);
			row1.Store();

			IFeature f11 = fc.CreateFeature();
			f11.set_Value(foreignKeyFieldIndex, row1.OID);
			f11.Shape = CurveConstruction.StartLine(100, 100)
			                             .LineTo(150, 150)
			                             .Curve;
			f11.Store();

			IFeature f12 = fc.CreateFeature();
			f12.set_Value(foreignKeyFieldIndex, row1.OID);
			f12.Shape = CurveConstruction.StartLine(150, 150)
			                             .LineTo(160, 150)
			                             .Curve;
			f12.Store();

			IFeature f15 = fc.CreateFeature();
			f15.set_Value(foreignKeyFieldIndex, row1.OID);
			f15.Shape = CurveConstruction.StartLine(155, 155)
			                             .LineTo(165, 165)
			                             .Curve;
			f15.Store();

			IFeature f18 = fc.CreateFeature();
			f18.set_Value(foreignKeyFieldIndex, row1.OID);
			f18.Shape = CurveConstruction.StartLine(165, 165)
			                             .LineTo(170, 170)
			                             .Curve;
			f18.Store();

			IFeature f19 = fc.CreateFeature();
			f19.set_Value(foreignKeyFieldIndex, row1.OID);
			f19.Shape = CurveConstruction.StartLine(165, 165)
			                             .LineTo(170, 175)
			                             .Curve;
			f19.Store();

			IFeature f20 = fc.CreateFeature();
			f20.set_Value(foreignKeyFieldIndex, row1.OID);
			f20.Shape = CurveConstruction.StartLine(170, 175)
			                             .LineTo(200, 200)
			                             .Curve;
			f20.Store();

			IFeature f13 = fc.CreateFeature();
			f13.set_Value(foreignKeyFieldIndex, row1.OID);
			f13.Shape = CurveConstruction.StartLine(150, 150)
			                             .LineTo(155, 155)
			                             .Curve;
			f13.Store();

			IFeature f14 = fc.CreateFeature();
			f14.set_Value(foreignKeyFieldIndex, row1.OID);
			f14.Shape = CurveConstruction.StartLine(155, 155)
			                             .LineTo(160, 150)
			                             .Curve;
			f14.Store();

			IFeature f16 = fc.CreateFeature();
			f16.set_Value(foreignKeyFieldIndex, row1.OID);
			f16.Shape = CurveConstruction.StartLine(160, 150)
			                             .LineTo(170, 170)
			                             .Curve;
			f16.Store();

			IFeature f17 = fc.CreateFeature();
			f17.set_Value(foreignKeyFieldIndex, row1.OID);
			f17.Shape = CurveConstruction.StartLine(170, 170)
			                             .LineTo(200, 200)
			                             .Curve;
			f17.Store();

			IRow row2 = lineGroupTable.CreateRow();
			row2.set_Value(groupFieldIndex, 10);
			row2.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(foreignKeyFieldIndex, row2.OID);
			f2.Shape = CurveConstruction.StartLine(200, 200)
			                            .LineTo(300, 200)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc.CreateFeature();
			f3.set_Value(foreignKeyFieldIndex, row2.OID);
			f3.Shape = CurveConstruction.StartLine(300, 200)
			                            .LineTo(401, 100)
			                            .LineTo(400, 100)
			                            .Curve;
			f3.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			((IFeatureClassManage) fc).UpdateExtent();

			QaGroupConnected test = CreateGroupConnectedTest(rel, groupTableName,
			                                                 groupFieldName);

			TestRunnerUtils.RunTests(test, 0, 1000);

			TestRunnerUtils.RunTests(test, 0, 500);

			TestRunnerUtils.RunTests(test, 0, 250);

			TestRunnerUtils.RunTests(test, 0, 100);

			// TODO: for FDDB only: fails with AssertionException (actual error count: 1)
			TestRunnerUtils.RunTests(test, 0, 50);

			// TODO: for FDDB only: fails with AssertionException (actual error count: 1)
			TestRunnerUtils.RunTests(test, 0, 20);

			// TODO: for FGDB only: fails with "An entry with the same key already exists."
			TestRunnerUtils.RunTests(test,
			                         GeometryFactory.CreateEnvelope(50, 50, 500, 165),
			                         0, 20);

			// TODO: for FGDB only: fails with "An entry with the same key already exists."
			TestRunnerUtils.RunTests(test,
			                         GeometryFactory.CreateEnvelope(50, 50, 500, 250),
			                         0, 20);
		}

		private static void CanDetectConnectedGroups_1toN_3([NotNull] IFeatureWorkspace ws)
		{
			const string groupFieldName = "LINEGROUP";
			const string foreignKeyFieldName = "FK";
			const string groupTableName = "LineGroups_3";

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField(foreignKeyFieldName,
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "RelFc2", fields);

			ITable groupTable = DatasetUtils.CreateTable(
				ws, groupTableName,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateField(groupFieldName, esriFieldType.esriFieldTypeInteger));

			IRelationshipClass rel = TestWorkspaceUtils.CreateSimple1NRelationship(
				ws, "relConnected2", groupTable, (ITable) fc,
				groupTable.OIDFieldName, foreignKeyFieldName);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			for (var i = 0; i < 10; i++)
			{
				IRow row = groupTable.CreateRow();
				row.set_Value(1, 8);
				row.Store();
			}

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 1);
			f1.Shape = CurveConstruction.StartLine(717340, 183660)
			                            .LineTo(719440, 184520)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(1, 2);
			f2.Shape = CurveConstruction.StartLine(719440, 184520)
			                            .LineTo(719700, 184500)
			                            .LineTo(719940, 184580)
			                            .LineTo(720240, 184620)
			                            .LineTo(720500, 184740)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc.CreateFeature();
			f3.set_Value(1, 3);
			f3.Shape = CurveConstruction.StartLine(719440, 184520)
			                            .LineTo(719800, 184640)
			                            .Curve;
			f3.Store();

			IFeature f4 = fc.CreateFeature();
			f4.set_Value(1, 4);
			f4.Shape = CurveConstruction.StartLine(719800, 184640)
			                            .LineTo(720060, 184800)
			                            .LineTo(720300, 184840)
			                            .Curve;
			f4.Store();

			IFeature f5 = fc.CreateFeature();
			f5.set_Value(1, 5);
			f5.Shape = CurveConstruction.StartLine(720300, 184840)
			                            .LineTo(720660, 185120)
			                            .LineTo(720940, 185040)
			                            .Curve;
			f5.Store();

			IFeature f6 = fc.CreateFeature();
			f6.set_Value(1, 6);
			f6.Shape = CurveConstruction.StartLine(720940, 185040)
			                            .LineTo(721220, 185140)
			                            .Curve;
			f6.Store();

			IFeature f7 = fc.CreateFeature();
			f7.set_Value(1, 7);
			f7.Shape = CurveConstruction.StartLine(720300, 184840)
			                            .LineTo(720820, 185000)
			                            .Curve;
			f7.Store();

			IFeature f8 = fc.CreateFeature();
			f8.set_Value(1, 8);
			f8.Shape = CurveConstruction.StartLine(720820, 185000)
			                            .LineTo(720940, 185040)
			                            .Curve;
			f8.Store();

			IFeature f9 = fc.CreateFeature();
			f9.set_Value(1, 9);
			f9.Shape = CurveConstruction.StartLine(720500, 184740)
			                            .LineTo(720740, 184840)
			                            .LineTo(720820, 185000)
			                            .Curve;
			f9.Store();

			IFeature f10 = fc.CreateFeature();
			f10.set_Value(1, 10);
			f10.Shape = CurveConstruction.StartLine(719800, 184640)
			                             .LineTo(720000, 184660)
			                             .LineTo(720160, 184680)
			                             .LineTo(720500, 184740)
			                             .Curve;
			f10.Store();

			//IFeature f2 = fc.CreateFeature();
			//f2.set_Value(1, row2.OID);
			//f2.Shape = CurveConstruction.StartLine(200, 200)
			//    .LineTo(300, 200).Curve;
			//f2.Store();

			//IFeature f3 = fc.CreateFeature();
			//f3.set_Value(1, row2.OID);
			//f3.Shape = CurveConstruction.StartLine(300, 200)
			//    .LineTo(400, 100).Curve;
			//f3.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			ITable relTab = TableJoinUtils.CreateQueryTable(rel, JoinType.InnerJoin);

			var test = new QaGroupConnected(ReadOnlyTableFactory.Create((IFeatureClass) relTab),
			                                new List<string>
			                                {
				                                groupTableName + "." +
				                                groupFieldName
			                                },
			                                ShapeAllowed.All);

			TestRunnerUtils.RunTests(test, 0, 1000);

			TestRunnerUtils.RunTests(test, 0, 200);
		}

		private static void CanDetectDisjointGroup_1toN([NotNull] IFeatureWorkspace ws)
		{
			const string groupFieldName = "LINEGROUP";
			const string foreignKeyFieldName = "FK";
			const string groupTableName = "DisjointLineGroups";

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField(foreignKeyFieldName,
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "RelFc3", fields);

			ITable groupTable = DatasetUtils.CreateTable(
				ws, groupTableName,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateField(groupFieldName, esriFieldType.esriFieldTypeInteger));

			IRelationshipClass rel = TestWorkspaceUtils.CreateSimple1NRelationship(
				ws, "relConnected3", groupTable, (ITable) fc, groupTable.OIDFieldName,
				foreignKeyFieldName);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			for (var i = 0; i < 10; i++)
			{
				IRow row = groupTable.CreateRow();
				row.set_Value(1, 8);
				row.Store();
			}

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 8);
			f1.Shape = CurveConstruction.StartLine(100, 50)
			                            .LineTo(200, 100)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(1, 8);
			f2.Shape = CurveConstruction.StartLine(100, 60)
			                            .LineTo(200, 100)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc.CreateFeature();
			f3.set_Value(1, 8);
			f3.Shape = CurveConstruction.StartLine(100, 70)
			                            .LineTo(200, 100)
			                            .Curve;
			f3.Store();

			IFeature f4 = fc.CreateFeature();
			f4.set_Value(1, 8);
			f4.Shape = CurveConstruction.StartLine(100, 80)
			                            .LineTo(200, 100)
			                            .Curve;
			f4.Store();

			IFeature f5 = fc.CreateFeature();
			f5.set_Value(1, 8);
			f5.Shape = CurveConstruction.StartLine(100, 90)
			                            .LineTo(200, 100)
			                            .Curve;
			f5.Store();

			IFeature f6 = fc.CreateFeature();
			f6.set_Value(1, 8);
			f6.Shape = CurveConstruction.StartLine(100, 100)
			                            .LineTo(200, 100)
			                            .Curve;
			f6.Store();

			IFeature f7 = fc.CreateFeature();
			f7.set_Value(1, 8);
			f7.Shape = CurveConstruction.StartLine(100, 110)
			                            .LineTo(200, 100)
			                            .Curve;
			f7.Store();

			IFeature f8 = fc.CreateFeature();
			f8.set_Value(1, 8);
			f8.Shape = CurveConstruction.StartLine(100, 120)
			                            .LineTo(200, 100)
			                            .Curve;
			f8.Store();

			IFeature f9 = fc.CreateFeature();
			f9.set_Value(1, 8);
			f9.Shape = CurveConstruction.StartLine(100, 130)
			                            .LineTo(200, 100)
			                            .Curve;
			f9.Store();

			IFeature f10 = fc.CreateFeature();
			f10.set_Value(1, 8);
			f10.Shape = CurveConstruction.StartLine(100, 140)
			                             .LineTo(200, 100)
			                             .Curve;
			f10.Store();

			// Disconnected feature
			IFeature f11 = fc.CreateFeature();
			f11.set_Value(1, 8);
			f11.Shape = CurveConstruction.StartLine(110, 95)
			                             .LineTo(190, 95)
			                             .Curve;
			f11.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			ITable relTab = TableJoinUtils.CreateQueryTable(rel, JoinType.InnerJoin);

			var test = new QaGroupConnected(ReadOnlyTableFactory.Create((IFeatureClass) relTab),
			                                new List<string>
			                                {
				                                groupTableName + "." +
				                                groupFieldName
			                                },
			                                ShapeAllowed.All);

			TestRunnerUtils.RunTests(test, 1, 1000);

			TestRunnerUtils.RunTests(test, 1, 20);
		}

		private const string _fkGroup = "fkGroup";
		private const string _pkField = "idGroup";
		private const string _groupField = "groupName";

		private IFeature CreateFeature(IFeatureClass fc, int fkGroup,
		                               IEnumerable<double[]> coords)
		{
			IFeature f = fc.CreateFeature();
			f.set_Value(1, fkGroup);
			CurveConstruction constr = null;
			foreach (double[] coord in coords)
			{
				if (constr == null)
				{
					constr = CurveConstruction.StartLine(coord[0], coord[1]);
				}
				else
				{
					constr = constr.LineTo(coord[0], coord[1]);
				}
			}

			f.Shape = constr?.Curve;
			f.Store();
			return f;
		}

		private IRow CreateRow(ITable tbl, int pkGroup, string group)
		{
			IRow row = tbl.CreateRow();
			row.set_Value(1, pkGroup);
			row.set_Value(2, group);
			row.Store();
			return row;
		}

		private static QaRelGroupConnected CreateRelGroupConnectedFactory(
			IFeatureWorkspace testWs, string fcName, out SimpleModel model,
			string relTableName = null)
		{
			IFieldsEdit fcFields = new FieldsClass();
			fcFields.AddField(FieldUtils.CreateOIDField());
			fcFields.AddField(FieldUtils.CreateField(_fkGroup,
			                                         esriFieldType.esriFieldTypeInteger));
			fcFields.AddField(FieldUtils.CreateShapeField(
				                  "Shape", esriGeometryType.esriGeometryPolyline,
				                  SpatialReferenceUtils.CreateSpatialReference
				                  ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                   true), 1000));

			IFeatureClass fc1 = DatasetUtils.CreateSimpleFeatureClass(
				testWs, fcName, fcFields);

			var ds1 = (IDataset) fc1;

			IFieldsEdit tblFields = new FieldsClass();
			tblFields.AddField(FieldUtils.CreateOIDField());
			tblFields.AddField(FieldUtils.CreateIntegerField(_pkField));
			tblFields.AddField(FieldUtils.CreateTextField(_groupField, 100));

			relTableName = relTableName ?? "Rel_" + ds1.Name;
			ITable relTable = TestWorkspaceUtils.CreateSimpleTable(testWs, relTableName,
				tblFields);
			var dsRel = (IDataset) relTable;

			string relName = "relName" + Environment.TickCount;
			TestWorkspaceUtils.CreateSimple1NRelationship(
				testWs, relName, relTable, (ITable) fc1, _pkField, _fkGroup);

			((IWorkspaceEdit) testWs).StartEditing(false);

			((IWorkspaceEdit) testWs).StopEditing(true);

			model = new SimpleModel("model", fc1);
			Dataset mds1 = model.AddDataset(new ModelVectorDataset(ds1.Name));
			Dataset mdsRel = model.AddDataset(new ModelTableDataset(dsRel.Name));

			var clsDesc = new ClassDescriptor(typeof(QaRelGroupConnected));
			var tstDesc = new TestDescriptor("GroupConnected", clsDesc);
			var condition = new QualityCondition("cndGroupConnected", tstDesc);
			InstanceConfigurationUtils.AddParameterValue(condition, "relationTables", mds1);
			InstanceConfigurationUtils.AddParameterValue(condition, "relationTables", mdsRel);
			InstanceConfigurationUtils.AddParameterValue(condition, "relation", relName);
			InstanceConfigurationUtils.AddParameterValue(condition, "join", JoinType.InnerJoin);
			InstanceConfigurationUtils.AddParameterValue(
				condition, "groupBy", string.Format("{0}.{1}", dsRel.Name, _groupField));
			InstanceConfigurationUtils.AddParameterValue(condition, "allowedShape",
			                                             ShapeAllowed.All);

			var fact = new QaRelGroupConnected();
			fact.Condition = condition;

			return fact;
		}

		private void testContainer_ProgressChanged(object sender, ProgressArgs args)
		{
			if (args.CurrentStep == Step.TileProcessing)
			{
				Console.WriteLine(@"Tile {0} of {1}: {2}", args.Current, args.Total,
				                  GeometryUtils.Format(args.CurrentEnvelope));

				//        args.CurrentEnvelope.Expand(0.02, 0.02, false);
			}
		}

		[NotNull]
		private static QaGroupConnected CreateGroupConnectedTest(
			[NotNull] IRelationshipClass relClass,
			[NotNull] string groupTableName,
			[NotNull] string groupFieldName)
		{
			ITable relTab = TableJoinUtils.CreateQueryTable(relClass, JoinType.InnerJoin);

			return new QaGroupConnected(ReadOnlyTableFactory.Create((IFeatureClass) relTab),
			                            new List<string>
			                            {
				                            $"{groupTableName}.{groupFieldName}"
			                            },
			                            ShapeAllowed.All);
		}

		[Test]
		public void CanDetectConnectedGroups_1toN_1_FileGdb()
		{
			CanDetectConnectedGroups_1toN_1(_fgdbWorkspace);
		}

		[Test]
		[Category(TestCategory.x86)]
		public void CanDetectConnectedGroups_1toN_1_PersonalGdb()
		{
			IFeatureWorkspace pgdbWorkspace =
				TestWorkspaceUtils.CreateTestAccessWorkspace(DatabaseName);

			CanDetectConnectedGroups_1toN_1(pgdbWorkspace);
		}

		[Test]
		public void CanDetectConnectedGroups_1toN_2_FileGdb()
		{
			CanDetectConnectedGroups_1toN_2(_fgdbWorkspace);
		}

		[Test]
		[Category(TestCategory.x86)]
		public void CanDetectConnectedGroups_1toN_2_PersonalGdb()
		{
			IFeatureWorkspace pgdbWorkspace =
				TestWorkspaceUtils.CreateTestAccessWorkspace(DatabaseName);

			CanDetectConnectedGroups_1toN_2(pgdbWorkspace);
		}

		[Test]
		public void CanDetectConnectedGroups_1toN_3_FileGdb()
		{
			CanDetectConnectedGroups_1toN_3(_fgdbWorkspace);
		}

		[Test]
		[Category(TestCategory.x86)]
		public void CanDetectConnectedGroups_1toN_3_PersonalGdb()
		{
			IFeatureWorkspace pgdbWorkspace =
				TestWorkspaceUtils.CreateTestAccessWorkspace(DatabaseName);

			CanDetectConnectedGroups_1toN_3(pgdbWorkspace);
		}

		[Test]
		public void CanDetectDisjointGroup_1toN_FileGdb()
		{
			CanDetectDisjointGroup_1toN(_fgdbWorkspace);
		}

		[Test]
		[Category(TestCategory.x86)]
		public void CanDetectDisjointGroup_1toN_PersonalGdb()
		{
			IFeatureWorkspace pgdbWorkspace =
				TestWorkspaceUtils.CreateTestAccessWorkspace(DatabaseName);

			CanDetectDisjointGroup_1toN(pgdbWorkspace);
		}

		[Test]
		[Ignore("requires connection to TOPGISP")]
		public void Psm377Test()
		{
			var ws =
				(IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspaceFromString(
					"ENCRYPTED_PASSWORD=00022e68306268624d4463775a38584f654757593269494364673d3d2a00;INSTANCE=sde:oracle11g:TOPGISP:sde;DBCLIENT=oracle;DB_CONNECTION_PROPERTIES=TOPGISP;PROJECT_INSTANCE=sde;USER=MAUH;VERSION=SDE.DEFAULT;AUTHENTICATION_MODE=DBMS");

			var tables =
				new List<IReadOnlyTable>
				{
					ReadOnlyTableFactory.Create(ws.OpenTable("TOPGIS_TLM.TLM_FLIESSGEWAESSER")),
					ReadOnlyTableFactory.Create(ws.OpenTable("TOPGIS_TLM.TLM_GEWAESSER_LAUF"))
				};
			IRelationshipClass relationshipClass =
				ws.OpenRelationshipClass("TOPGIS_TLM.TLM_FLIESSGEWAESSER_LAUF");

			ITable joinedTable = TableJoinUtils.CreateQueryTable(relationshipClass,
			                                                     JoinType.InnerJoin);

			var test = new QaGroupConnected(
				ReadOnlyTableFactory.Create((IFeatureClass) joinedTable),
				new[] { "TOPGIS_TLM.TLM_GEWAESSER_LAUF.GWL_NR" },
				ShapeAllowed.None);

			var runner = new QaContainerTestRunner(10000, test);
			runner.TestContainer.ProgressChanged += testContainer_ProgressChanged;

			runner.Execute();
		}

		[Test]
		public void TestAllowedShapes()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TestAllowedShapes");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Group",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "TestCircularErrors",
				fields);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 10);
			f1.Shape =
				CurveConstruction.StartLine(170, 170)
				                 .LineTo(200, 200)
				                 .Curve;
			f1.Store();

			IFeature f1a = fc.CreateFeature();
			f1a.set_Value(1, 10);
			f1a.Shape =
				CurveConstruction.StartLine(100, 100)
				                 .LineTo(170, 170)
				                 .Curve;
			f1a.Store();

			IFeature f1b = fc.CreateFeature();
			f1b.set_Value(1, 10);
			f1b.Shape =
				CurveConstruction.StartLine(150, 100)
				                 .LineTo(170, 170)
				                 .Curve;
			f1b.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(1, 10);
			f2.Shape = CurveConstruction.StartLine(200, 200)
			                            .LineTo(300, 200)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc.CreateFeature();
			f3.set_Value(1, 10);
			f3.Shape = CurveConstruction.StartLine(200, 200)
			                            .LineTo(200, 300)
			                            .Curve;
			f3.Store();

			IFeature f4 = fc.CreateFeature();
			f4.set_Value(1, 10);
			f4.Shape = CurveConstruction.StartLine(300, 200)
			                            .LineTo(200, 300)
			                            .Curve;
			f4.Store();

			IFeature f5 = fc.CreateFeature();
			f5.set_Value(1, 10);
			f5.Shape = CurveConstruction.StartLine(300, 200)
			                            .LineTo(301, 200)
			                            .Curve;
			f5.Store();

			IFeature f6 = fc.CreateFeature();
			f6.set_Value(1, 10);
			f6.Shape = CurveConstruction.StartLine(200, 300)
			                            .LineTo(200, 301)
			                            .Curve;
			f6.Store();

			IFeature f7 = fc.CreateFeature();
			f7.set_Value(1, 10);
			f7.Shape = CurveConstruction.StartLine(200, 300)
			                            .LineTo(201, 250)
			                            .Curve;
			f7.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(ReadOnlyTableFactory.Create(fc), new[] { "Group" },
			                                ShapeAllowed.None);

			var runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };

			IEnvelope extent = GeometryFactory.CreateEnvelope(150, 150, 350, 350);

			Console.WriteLine($@"Shape:{ShapeAllowed.None}");
			Console.WriteLine(@"------------------------------------------------");
			runner.Execute();
			Console.WriteLine(@"------------------------------------------------");
			runner.Execute(extent);

			test = new QaGroupConnected(ReadOnlyTableFactory.Create(fc), new[] { "Group" },
			                            ShapeAllowed.Cycles);
			runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };
			Console.WriteLine($@"Shape:{ShapeAllowed.Cycles}");
			Console.WriteLine(@"------------------------------------------------");
			runner.Execute();
			Console.WriteLine(@"------------------------------------------------");
			runner.Execute(extent);

			test = new QaGroupConnected(ReadOnlyTableFactory.Create(fc), new[] { "Group" },
			                            ShapeAllowed.Branches);
			runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };
			Console.WriteLine($@"Shape:{ShapeAllowed.Branches}");
			Console.WriteLine(@"------------------------------------------------");
			runner.Execute();
			Console.WriteLine(@"------------------------------------------------");
			runner.Execute(extent);

			test = new QaGroupConnected(ReadOnlyTableFactory.Create(fc), new[] { "Group" },
			                            ShapeAllowed.InsideBranches);
			runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };
			Console.WriteLine($@"Shape:{ShapeAllowed.InsideBranches}");
			Console.WriteLine(@"------------------------------------------------");
			runner.Execute();
			Console.WriteLine(@"------------------------------------------------");
			runner.Execute(extent);

			test = new QaGroupConnected(ReadOnlyTableFactory.Create(fc), new[] { "Group" },
			                            ShapeAllowed.CyclesAndBranches);
			runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };
			Console.WriteLine($@"Shape:{ShapeAllowed.CyclesAndBranches}");
			Console.WriteLine(@"------------------------------------------------");
			runner.Execute();
			Console.WriteLine(@"------------------------------------------------");
			runner.Execute(extent);

			test = new QaGroupConnected(ReadOnlyTableFactory.Create(fc), new[] { "Group" },
			                            ShapeAllowed.All);
			runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };
			Console.WriteLine($@"Shape:{ShapeAllowed.All}");
			Console.WriteLine(@"------------------------------------------------");
			runner.Execute();
			Console.WriteLine(@"------------------------------------------------");
			runner.Execute(extent);
		}

		[Test]
		[Ignore("requires local data")]
		public void TestBkgDlm250()
		{
			IWorkspace ws = TestDataUtils.OpenFileGdb(@"Bkg\DLM250.gdb");
			IFeatureClass ax44004L = DatasetUtils.OpenFeatureClass(ws, "AX_44004_L");
			IFeatureClass ax57003L = DatasetUtils.OpenFeatureClass(ws, "AX_57003_L");
			var test = new QaGroupConnected(
				new[]
				{
					ReadOnlyTableFactory.Create(ax44004L),
					ReadOnlyTableFactory.Create(ax57003L)
				},
				new[] { "GWK" }, "#",
				ShapeAllowed.All,
				GroupErrorReporting.CombineParts,
				100);
			test.SetConstraint(0, "GWK IS NOT NULL AND GWK <> 'zzzz'");
			test.SetConstraint(1, "GWK IS NOT NULL AND GWK <> 'zzzz'");

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();
		}

		[Test]
		[Ignore("requires local data")]
		public void TestBkgERMRoad29()
		{
			IWorkspace ws = TestDataUtils.OpenFileGdb(@"Bkg\ERM_Samples.gdb");
			IFeatureClass roadL = DatasetUtils.OpenFeatureClass(ws, "RoadL");
			var test = new QaGroupConnected(
				new[] { ReadOnlyTableFactory.Create(roadL) }, new[] { "RTE" }, "#",
				ShapeAllowed.All,
				GroupErrorReporting.CombineParts,
				0.01);
			test.SetConstraint(0, "RTE IS NOT NULL AND RTE NOT IN ('UNK','N_A')");

			var runner = new QaContainerTestRunner(1, test);
			IEnvelope box = GeometryFactory.CreateEnvelope(5.88, 49.11, 7.16, 50.63);
			runner.Execute(box);
		}

		[Test]
		[Ignore("requires local data")]
		public void TestBkgERMSamples()
		{
			IWorkspace ws = TestDataUtils.OpenFileGdb(@"Bkg\ERM_Samples.gdb");
			IFeatureClass roadL = DatasetUtils.OpenFeatureClass(ws, "RoadL");
			var test = new QaGroupConnected(
				new[] { ReadOnlyTableFactory.Create(roadL) }, new[] { "RTE" }, "#",
				ShapeAllowed.None,
				GroupErrorReporting.CombineParts,
				100);
			test.SetConstraint(0, "RTE IS NOT NULL AND RTE NOT IN ('UNK','N_A')");

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();
		}

		[Test]
		public void TestCircularErrors()
		{
			TestCircularErrors(_fgdbWorkspace);
		}

		[Test]
		public void TestCircularMultiPartErrors()
		{
			TestCircularMultiPartErrors(_fgdbWorkspace);
		}

		[Test]
		public void TestConnectedOutside()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("ConnectedOutside");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Group",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "Fc", fields);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 10);
			f1.Shape = CurveConstruction.StartLine(100, 100)
			                            .LineTo(200, 200)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(1, 10);
			f2.Shape = CurveConstruction.StartLine(200, 200)
			                            .LineTo(200, 201)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc.CreateFeature();
			f3.set_Value(1, 10);
			f3.Shape = CurveConstruction.StartLine(200, 201)
			                            .LineTo(100, 250)
			                            .Curve;
			f3.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(ReadOnlyTableFactory.Create(fc),
			                                new List<string> { "Group" },
			                                ShapeAllowed.All);

			var runner = new QaContainerTestRunner(10000, test);
			runner.TestContainer.ProgressChanged += testContainer_ProgressChanged;

			Assert.AreEqual(0,
			                runner.Execute(
				                GeometryFactory.CreateEnvelope(-10, -10, 150, 300)));
		}

		[Test]
		public void TestEqualErrorGeometries()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TestMultipart");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Group",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "Fc", fields);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 10);
			f1.Shape = CurveConstruction.StartLine(10, 210)
			                            .LineTo(90, 290)
			                            .Curve;
			f1.Store();

			IFeature f2_1 = fc.CreateFeature();
			f2_1.set_Value(1, 10);
			f2_1.Shape = CurveConstruction.StartLine(230, 30)
			                              .LineTo(250, 50)
			                              .Curve;
			f2_1.Store();

			IFeature f2_2 = fc.CreateFeature();
			f2_2.set_Value(1, 10);
			f2_2.Shape = CurveConstruction.StartLine(250, 50)
			                              .LineTo(270, 70)
			                              .Curve;
			f2_2.Store();

			IFeature f3 = fc.CreateFeature();
			f3.set_Value(1, 20);
			f3.Shape = CurveConstruction.StartLine(0, 0)
			                            .LineTo(295, 295)
			                            .Curve;
			f3.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(new[] { ReadOnlyTableFactory.Create(fc) },
			                                new List<string> { "Group" }, null,
			                                ShapeAllowed.None,
			                                GroupErrorReporting.ShortestGaps,
			                                0.1);

			test.CompleteGroupsOutsideTestArea = true;

			var runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };
			runner.TestContainer.ProgressChanged += testContainer_ProgressChanged;

			Assert.AreEqual(1, runner.Execute());
			IGeometry errGeom1 = runner.ErrorGeometries[0];

			runner = new QaContainerTestRunner(100, test) { KeepGeometry = true };
			runner.TestContainer.ProgressChanged += testContainer_ProgressChanged;

			Assert.AreEqual(1, runner.Execute());
			IGeometry errGeom2 = runner.ErrorGeometries[0];

			Assert.True(GeometryUtils.AreEqual(errGeom1, errGeom2));
		}

		[Test]
		public void TestGroupConnected()
		{
			TestGroupConnected(_fgdbWorkspace);
		}

		[Test]
		public void TestMultiFeatureClassDiffAttributeNames()
		{
			TestMultiFeatureClassDiffAttributeNames(_fgdbWorkspace);
		}

		[Test]
		public void TestMultiFeatureClassSameAttributeNames()
		{
			TestMultiFeatureClassSameAttributeNames(_fgdbWorkspace);
		}

		[Test]
		public void TestMultiFeatureClassValueGroups()
		{
			TestMultiFeatureClassValueGroups(_fgdbWorkspace);
		}

		[Test]
		public void TestMultipart()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TestMultipart");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Group",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "Fc", fields);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 10);
			f1.Shape = CurveConstruction.StartLine(1, 1)
			                            .LineTo(2, 2)
			                            .MoveTo(3, 3)
			                            .LineTo(4, 4)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(1, 20);
			f2.Shape = CurveConstruction.StartLine(5, 5)
			                            .LineTo(9, 5)
			                            .MoveTo(12, 5)
			                            .LineTo(9, 5)
			                            .MoveTo(9, 9)
			                            .LineTo(9, 5)
			                            .Curve;
			f2.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(ReadOnlyTableFactory.Create(fc),
			                                new List<string> { "Group" },
			                                ShapeAllowed.All);

			test.UseMultiParts = false;
			var runner = new QaContainerTestRunner(10000, test);
			Assert.AreEqual(0, runner.Execute());

			test.UseMultiParts = true;
			runner = new QaContainerTestRunner(10000, test);
			Assert.AreEqual(1, runner.Execute());
		}

		[Test]
		public void TestMultipartError()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TestMultipart");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Group",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "Fc", fields);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 10);
			f1.Shape = CurveConstruction.StartLine(100, 100)
			                            .LineTo(200, 200)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(1, 10);
			f2.Shape = CurveConstruction.StartLine(200, 200)
			                            .LineTo(300, 200)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc.CreateFeature();
			f3.set_Value(1, 10);
			f3.Shape = CurveConstruction.StartLine(300, 200).LineTo(350, 200)
			                            .MoveTo(350, 150)
			                            .LineTo(420, 120)
			                            .LineTo(400, 100)
			                            .Curve;
			f3.Store();

			IFeature f4 = fc.CreateFeature();
			f4.set_Value(1, 10);
			f4.Shape = CurveConstruction.StartLine(400, 100)
			                            .LineTo(100, 100)
			                            .Curve;
			f4.Store();

			IFeature f5 = fc.CreateFeature();
			f5.set_Value(1, 10);
			f5.Shape = CurveConstruction.StartLine(0, 100)
			                            .LineTo(100, 100)
			                            .Curve;
			f5.Store();

			IFeature f6 = fc.CreateFeature();
			f6.set_Value(1, 10);
			f6.Shape = CurveConstruction.StartLine(400, 100)
			                            .LineTo(500, 100)
			                            .Curve;
			f6.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(ReadOnlyTableFactory.Create(fc),
			                                new List<string> { "Group" },
			                                ShapeAllowed.All);

			var runner = new QaContainerTestRunner(10000, test);
			runner.TestContainer.ProgressChanged += testContainer_ProgressChanged;

			Assert.AreEqual(0, runner.Execute());
		}

		[Test]
		public void TestMultiPartErrors()
		{
			TestMultiPartErrors(
				TestWorkspaceUtils.CreateInMemoryWorkspace("TestMultiPartErrors"));
		}

		[Test]
		public void TestMultiPartErrorsTestextent()
		{
			TestMultiPartErrorsTestextent(_fgdbWorkspace);
		}

		[Test]
		public void TestPerformance()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TestPerformance");
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Group",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "Fc", fields);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);
			const int featureCount = 100;
			var nX = (int) Math.Sqrt(featureCount);
			var iFeature = 0;
			while (iFeature < featureCount)
			{
				int x0 = iFeature % nX;
				int y0 = iFeature / nX;

				IFeature f1 = fc.CreateFeature();
				f1.set_Value(1, 10);
				f1.Shape = CurveConstruction.StartLine(x0, y0).Line(0.5, 0.5).Curve;
				f1.Store();

				iFeature++;
			}

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(new[] { ReadOnlyTableFactory.Create(fc) },
			                                new List<string> { "Group" }, null,
			                                ShapeAllowed.All,
			                                GroupErrorReporting.CombineParts,
			                                0.1);
			var runner = new QaContainerTestRunner(10000, test);
			IEnvelope box = new EnvelopeClass();
			box.PutCoords(-1, -1, nX + 5, nX + 5);
			runner.KeepGeometry = true;
			runner.Execute(box);
			runner.Execute();
		}

		[Test]
		public void TestRecheckMultiparts()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("ConnectedOutside");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("GroupFld",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "Fc", fields);

			((IWorkspaceEdit) ws).StartEditing(false);

			// connected
			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 10);
			f1.Shape = CurveConstruction.StartLine(100, 100)
			                            .LineTo(200, 200)
			                            .Curve;
			f1.Store();

			// connected, ommitted in TestingRow in some cases
			IFeature f2 = fc.CreateFeature();
			f2.set_Value(1, 10);
			f2.Shape = CurveConstruction.StartLine(200, 200)
			                            .LineTo(200, 201)
			                            .Curve;
			f2.Store();

			// connected
			IFeature f3 = fc.CreateFeature();
			f3.set_Value(1, 10);
			f3.Shape = CurveConstruction.StartLine(200, 201)
			                            .LineTo(100, 250)
			                            .Curve;
			f3.Store();

			// not connected outside testextent
			IFeature f4 = fc.CreateFeature();
			f4.set_Value(1, 10);
			f4.Shape = CurveConstruction.StartLine(-20, -20)
			                            .LineTo(-50, -50)
			                            .Curve;
			f4.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(ReadOnlyTableFactory.Create(fc),
			                                new List<string> { "GroupFld" },
			                                ShapeAllowed.All);

			var runner = new QaContainerTestRunner(10000, test);
			runner.TestContainer.ProgressChanged += testContainer_ProgressChanged;

			IEnvelope testExtent = GeometryFactory.CreateEnvelope(-10, -10, 300, 300);
			Assert.AreEqual(0, runner.Execute(testExtent));

			test.CompleteGroupsOutsideTestArea = true;
			Assert.AreEqual(1, runner.Execute(testExtent));

			test.CompleteGroupsOutsideTestArea = false;

			runner.TestContainer.TestingRow +=
				(sender, args) =>
				{
					if (args.Row.OID == f2.OID)
					{
						args.Cancel = true;
					}
				};
			Assert.AreEqual(1, runner.Execute(testExtent));

			test.RecheckMultiplePartIssues = true;
			Assert.AreEqual(0, runner.Execute(testExtent));

			test.CompleteGroupsOutsideTestArea = true;
			Assert.AreEqual(1, runner.Execute(testExtent));
			test.CompleteGroupsOutsideTestArea = false;

			test.SetConstraint(0, "ObjectId <> 2");
			test.RecheckMultiplePartIssues = true;
			Assert.AreEqual(1, runner.Execute(testExtent));

			test.CompleteGroupsOutsideTestArea = true;
			//Assert.AreEqual(2, runner.Execute(testExtent));
		}

		[Test]
		public void TestRelatedBug()
		{
			IFeatureWorkspace testWs = _fgdbWorkspace;

			const string fcName = "TestRelatedBugFc";
			const string relTableName = "TestRelatedBugTbl";
			QaRelGroupConnected fact =
				CreateRelGroupConnectedFactory(testWs, fcName, out SimpleModel model, relTableName);

			IFeatureClass fc = testWs.OpenFeatureClass(fcName);
			ITable relTable = testWs.OpenTable(relTableName);

			((IWorkspaceEdit) testWs).StartEditing(false);

			CreateRow(relTable, 10, "Fluss");
			CreateRow(relTable, 11, "Fluss");

			CreateFeature(fc, 10, new Coords { { 0, 0 }, { 500, 500 } });
			CreateFeature(fc, 10, new Coords { { 0, 0 }, { 251, 251 }, { 500, 500 } });
			CreateFeature(fc, 10, new Coords { { 0, 0 }, { 249, 249 }, { 500, 500 } });
			CreateFeature(fc, 10, new Coords { { 0, 0 }, { 1900, 500 } });
			//CreateFeature(fc, 11, new Coords { { 500, 500 }, { 1900, 1900 } });
			CreateFeature(fc, 11, new Coords { { 1900, 1900 }, { 1990, 1990 } });

			//CreateFeature(fc, 11, new Coords { { 500, 500 }, { 801, 800 } });
			//CreateFeature(fc, 11, new Coords { { 801, 800 }, { 1201, 1200 } });
			CreateFeature(fc, 11, new Coords { { 1201, 1200 }, { 1900, 1900 } });
			CreateFeature(fc, 11, new Coords { { 801, 800 }, { 1000, 1000 } });
			CreateFeature(fc, 11, new Coords { { 1000, 1000 }, { 1201, 1200 } });

			//CreateFeature(fc, 11, new Coords { { 500, 500 }, { 1900, 500 } });
			CreateFeature(fc, 11, new Coords { { 500, 500 }, { 800, 501 } });
			//CreateFeature(fc, 11, new Coords { { 800, 501 }, { 1200, 501 } });
			CreateFeature(fc, 11, new Coords { { 1200, 501 }, { 1900, 500 } });
			CreateFeature(fc, 11, new Coords { { 800, 501 }, { 1000, 500 } });
			CreateFeature(fc, 11, new Coords { { 1000, 500 }, { 1200, 501 } });

			//CreateFeature(fc, 11, new Coords { { 1900, 500 }, { 1900, 1900 } });
			CreateFeature(fc, 11, new Coords { { 1900, 500 }, { 1901, 800 } });
			//CreateFeature(fc, 11, new Coords { { 1901, 800 }, { 1901, 1200 } });
			CreateFeature(fc, 11, new Coords { { 1901, 1200 }, { 1900, 1900 } });
			CreateFeature(fc, 11, new Coords { { 1901, 800 }, { 1900, 1000 } });
			CreateFeature(fc, 11, new Coords { { 1900, 1000 }, { 1901, 1200 } });

			//CreateFeature(fc, 11, new Coords { { 500, 500 }, { 500, 1900 } });
			//CreateFeature(fc, 11, new Coords { { 500, 500 }, { 501, 800 } });
			//CreateFeature(fc, 11, new Coords { { 501, 800 }, { 501, 1200 } });
			CreateFeature(fc, 11, new Coords { { 501, 1200 }, { 500, 1900 } });
			CreateFeature(fc, 11, new Coords { { 501, 800 }, { 500, 1000 } });
			CreateFeature(fc, 11, new Coords { { 500, 1000 }, { 501, 1200 } });

			//CreateFeature(fc, 11, new Coords { { 500, 1900 }, { 1900, 1900 } });
			CreateFeature(fc, 11, new Coords { { 500, 1900 }, { 800, 1901 } });
			//CreateFeature(fc, 11, new Coords { { 800, 1901 }, { 1200, 1901 } });
			CreateFeature(fc, 11, new Coords { { 1200, 1901 }, { 1900, 1900 } });
			CreateFeature(fc, 11, new Coords { { 800, 1901 }, { 1000, 1900 } });
			//CreateFeature(fc, 11, new Coords { { 1000, 1900 }, { 1200, 1901 } });

			((IWorkspaceEdit) testWs).StopEditing(true);

			IList<ITest> tests =
				fact.CreateTests(new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));
			Assert.AreEqual(1, tests.Count);
			var test = (QaGroupConnected) tests[0];
			test.CompleteGroupsOutsideTestArea = true;

			var runner = new QaContainerTestRunner(1000, test);

			runner.Execute(GeometryFactory.CreateEnvelope(0, 0, 1100, 1100));
			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		[Ignore("TODO reason")]
		public void TestRelatedFactory()
		{
			IFeatureWorkspace testWs = TestWorkspaceUtils.CreateTestAccessWorkspace(DatabaseName);

			QaRelGroupConnected fact =
				CreateRelGroupConnectedFactory(testWs, "TestRelatedFactoryFc",
				                               out SimpleModel model);

			InstanceConfiguration condition = fact.Condition;

			IList<ITest> tests =
				fact.CreateTests(new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));
			Assert.AreEqual(1, tests.Count);

			Assert.AreNotEqual(((QaGroupConnected) tests[0]).ErrorReporting,
								GroupErrorReporting.ShortestGaps);
			Assert.AreNotEqual(((QaGroupConnected) tests[0]).CompleteGroupsOutsideTestArea,
			                   true);

			// test optional parameters
			InstanceConfigurationUtils.AddParameterValue(
				condition, nameof(QaGroupConnected.ErrorReporting),
				GroupErrorReporting.ShortestGaps);
			InstanceConfigurationUtils.AddParameterValue(
				condition, nameof(QaGroupConnected.CompleteGroupsOutsideTestArea), true);

			tests = fact.CreateTests(new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));
			Assert.AreEqual(((QaGroupConnected) tests[0]).ErrorReporting,
			                GroupErrorReporting.ShortestGaps);
			Assert.AreEqual(((QaGroupConnected) tests[0]).CompleteGroupsOutsideTestArea,
			                true);
		}

		[Test]
		public void TestReportCombineParts()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TestPerformance");
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Group",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "Fc", fields);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 10);
			f1.Shape = CurveConstruction.StartLine(0, 0)
			                            .LineTo(0.5, 0.5)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(1, 10);
			f2.Shape = CurveConstruction.StartLine(1, 1)
			                            .LineTo(0.55, 0.55)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc.CreateFeature();
			f3.set_Value(1, 10);
			f3.Shape = CurveConstruction.StartLine(2, 2)
			                            .LineTo(1.05, 1.05)
			                            .Curve;
			f3.Store();

			IFeature f4 = fc.CreateFeature();
			f4.set_Value(1, 10);
			f4.Shape = CurveConstruction.StartLine(3, 3)
			                            .LineTo(5, 5)
			                            .Curve;
			f4.Store();

			IFeature f5 = fc.CreateFeature();
			f5.set_Value(1, 10);
			f5.Shape = CurveConstruction.StartLine(-1, -1)
			                            .LineTo(6, -1)
			                            .LineTo(6, 6)
			                            .Curve;
			f5.Store();

			IFeature f6 = fc.CreateFeature();
			f6.set_Value(1, 10);
			f6.Shape = CurveConstruction.StartLine(1, 0)
			                            .LineTo(0.6, 0.5)
			                            .Curve;
			f6.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(new[] { ReadOnlyTableFactory.Create(fc) },
			                                new List<string> { "Group" }, null,
			                                ShapeAllowed.All,
			                                GroupErrorReporting.CombineParts,
			                                0.1)
			           {
				           ReportIndividualGaps = true
			           };
			var runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };

			runner.Execute();
			Assert.AreEqual(5, runner.Errors.Count);
			// 2 short gaps (points) now reported only once

			test.ReportIndividualGaps = false;
			runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };

			runner.Execute();
			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void TestReportIgnoreLongerThan()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TestPerformance");
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Group",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "Fc", fields);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 10);
			f1.Shape = CurveConstruction.StartLine(0, 0)
			                            .LineTo(0.5, 0.5)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(1, 10);
			f2.Shape = CurveConstruction.StartLine(1, 1)
			                            .LineTo(2, 2)
			                            .Curve;
			f2.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(new[] { ReadOnlyTableFactory.Create(fc) },
			                                new List<string> { "Group" }, null,
			                                ShapeAllowed.All,
			                                GroupErrorReporting.ShortestGaps,
			                                0.1);
			var runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };

			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);

			test.IgnoreGapsLongerThan = 0.5;
			runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };

			runner.Execute();
			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void TestReportShortestGaps()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("TestPerformance");
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Group",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "Fc", fields);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);

			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, 10);
			f1.Shape = CurveConstruction.StartLine(0, 0)
			                            .LineTo(0.5, 0.5)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc.CreateFeature();
			f2.set_Value(1, 10);
			f2.Shape = CurveConstruction.StartLine(1, 1)
			                            .LineTo(0.55, 0.55)
			                            .Curve;
			f2.Store();

			IFeature f3 = fc.CreateFeature();
			f3.set_Value(1, 10);
			f3.Shape = CurveConstruction.StartLine(2, 2)
			                            .LineTo(1.05, 1.05)
			                            .Curve;
			f3.Store();

			IFeature f4 = fc.CreateFeature();
			f4.set_Value(1, 10);
			f4.Shape = CurveConstruction.StartLine(3, 3)
			                            .LineTo(5, 5)
			                            .Curve;
			f4.Store();

			IFeature f5 = fc.CreateFeature();
			f5.set_Value(1, 10);
			f5.Shape = CurveConstruction.StartLine(-1, -1)
			                            .LineTo(6, -1)
			                            .LineTo(6, 6)
			                            .Curve;
			f5.Store();

			IFeature f6 = fc.CreateFeature();
			f6.set_Value(1, 10);
			f6.Shape = CurveConstruction.StartLine(1, 0)
			                            .LineTo(0.6, 0.5)
			                            .Curve;
			f6.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaGroupConnected(new[] { ReadOnlyTableFactory.Create(fc) },
			                                new List<string> { "Group" }, null,
			                                ShapeAllowed.All,
			                                GroupErrorReporting.ShortestGaps,
			                                0.1)
			           {
				           ReportIndividualGaps = true
			           };
			var runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };

			runner.Execute();
			Assert.AreEqual(5, runner.Errors.Count);
			// 2 short gaps (points) now reported only once

			test.ReportIndividualGaps = false;
			runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };

			runner.Execute();
			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		[Ignore("requires connection to TOPGIST")]
		public void Top3875Test()
		{
			IWorkspace ws = TestDataUtils.OpenTopgisTlm();

			var tables = new List<IReadOnlyTable>();
			tables.Add(
				ReadOnlyTableFactory.Create(DatasetUtils.OpenTable(ws, "TOPGIS_TLM.TLM_STRASSE")));
			tables.Add(
				ReadOnlyTableFactory.Create(
					DatasetUtils.OpenTable(ws, "TOPGIS_TLM.TLM_STRASSENROUTE")));
			IRelationshipClass rel =
				DatasetUtils.OpenRelationshipClass((IFeatureWorkspace) ws,
				                                   "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE");

			ITable queryTable = TableJoinUtils.CreateQueryTable(rel,
			                                                    JoinType.InnerJoin);

			var test =
				new QaGroupConnected(ReadOnlyTableFactory.Create((IFeatureClass) queryTable),
				                     new[] { "TOPGIS_TLM.TLM_STRASSENROUTE.UUID" },
				                     ShapeAllowed.Cycles);

			ITableFilter filter = new AoTableFilter();
			filter.WhereClause = "ObjectID in (2370953,2370947)";
			IEnvelope extent = null;
			foreach (IReadOnlyRow row in tables[0].EnumRows(filter, false))
			{
				var feature = (IReadOnlyFeature) row;
				if (extent == null)
				{
					extent = GeometryFactory.Clone(feature.Extent);
				}
				else
				{
					extent.Union(feature.Extent);
				}
			}

			//ISelectionSet sel = queryTable.Select(filter, esriSelectionType.esriSelectionTypeIDSet,
			//    esriSelectionOption.esriSelectionOptionNormal, null);
			extent = GeometryFactory.CreateEnvelope(2694695, 1265000, 2694705, 1265010);
			//			extent = GeometryFactory.CreateEnvelope(2694422.2, 1264660.975, 2695014.79, 1265468.3775);
			//			extent = GeometryFactory.CreateEnvelope(2694422.2, 1264660.975, 2695014.79, 1265100.3775);

			var runner = new QaContainerTestRunner(10000, test);

			runner.Execute(extent);
		}

		[Test]
		[Ignore("requires connection to TOPGIST")]
		public void TopOutsideArea()
		{
			var ws = (IFeatureWorkspace) TestDataUtils.OpenTopgisTlm();

			var tables =
				new List<IReadOnlyTable>
				{
					ReadOnlyTableFactory.Create(ws.OpenTable("TOPGIS_TLM.TLM_FLIESSGEWAESSER")),
					ReadOnlyTableFactory.Create(ws.OpenTable("TOPGIS_TLM.TLM_GEWAESSER_LAUF"))
				};
			IRelationshipClass relationshipClass =
				ws.OpenRelationshipClass("TOPGIS_TLM.TLM_FLIESSGEWAESSER_LAUF");

			ITable joinedTable = TableJoinUtils.CreateQueryTable(relationshipClass,
			                                                     JoinType.InnerJoin);

			var test = new QaGroupConnected(
				ReadOnlyTableFactory.Create((IFeatureClass) joinedTable),
				new[] { "TOPGIS_TLM.TLM_GEWAESSER_LAUF.GWL_NR" },
				ShapeAllowed.None);
			test.CompleteGroupsOutsideTestArea = true;

			var runner = new QaContainerTestRunner(10000, test);
			runner.TestContainer.ProgressChanged += testContainer_ProgressChanged;

			runner.Execute(GeometryFactory.CreateEnvelope(2600000, 1200000, 2602000, 1202000));

			runner.Execute(GeometryFactory.CreateEnvelope(2600000, 1199000, 2602000, 1200000));
		}

		[Test]
		[Ignore("requires connection to TOPGIST")]
		public void Topgis5203()
		{
			var ws = (IFeatureWorkspace) TestDataUtils.OpenTopgisTlm();

			var tables =
				new List<IReadOnlyTable>
				{
					ReadOnlyTableFactory.Create(ws.OpenTable("TOPGIS_TLM.TLM_FLIESSGEWAESSER")),
					ReadOnlyTableFactory.Create(ws.OpenTable("TOPGIS_TLM.TLM_GEWAESSER_LAUF"))
				};
			IRelationshipClass relationshipClass =
				ws.OpenRelationshipClass("TOPGIS_TLM.TLM_FLIESSGEWAESSER_LAUF");

			ITable joinedTable = TableJoinUtils.CreateQueryTable(relationshipClass,
			                                                     JoinType.InnerJoin);

			var test = new QaGroupConnected(
				ReadOnlyTableFactory.Create((IFeatureClass) joinedTable),
				new[] { "TOPGIS_TLM.TLM_GEWAESSER_LAUF.GWL_NR" },
				ShapeAllowed.None);
			test.CompleteGroupsOutsideTestArea = true;

			double tileSize = 10000;
			var runner = new QaContainerTestRunner(tileSize, test);
			runner.TestContainer.ProgressChanged += testContainer_ProgressChanged;

			runner.Execute(
				GeometryFactory.CreateEnvelope(2707400.00, 1217900.00, 2725100.00, 1230100.00));
			//runner.Execute(GeometryFactory.CreateEnvelope(2730900, 1164500, 2731000, 1164600));

			//runner.ClearErrors();
			//runner.Execute(GeometryFactory.CreateEnvelope(2731000 - tileSize, 1164500, 2731100,
			//                                              1165000));
		}

		[Test]
		[Ignore("uses temporary local data")]
		public void Topgis5342()
		{
			var ws = (IFeatureWorkspace) TestDataUtils.OpenFileGdb(@"C:\temp\TOP-5342_2.gdb");

			var tables =
				new List<IReadOnlyTable>
				{
					ReadOnlyTableFactory.Create(ws.OpenTable("TLM_FLIESSGEWAESSER")),
					ReadOnlyTableFactory.Create(ws.OpenTable("TLM_GEWAESSER_LAUF"))
				};
			IRelationshipClass relationshipClass =
				ws.OpenRelationshipClass("TLM_FLIESSGEWAESSER_LAUF");

			ITable joinedTable = TableJoinUtils.CreateQueryTable(relationshipClass,
			                                                     JoinType.InnerJoin);

			var test = new QaGroupConnected(
				ReadOnlyTableFactory.Create((IFeatureClass) joinedTable),
				new[] { "TLM_GEWAESSER_LAUF.GWL_NR" },
				ShapeAllowed.None);
			test.CompleteGroupsOutsideTestArea = true;

			double tileSize = 10000;
			var runner = new QaContainerTestRunner(tileSize, test);
			runner.TestContainer.ProgressChanged += testContainer_ProgressChanged;

			runner.Execute(
				GeometryFactory.CreateEnvelope(2707400.00, 1217900.00, 2725100.00, 1230100.00));

			//runner.ClearErrors();
			//runner.Execute(GeometryFactory.CreateEnvelope(2731000 - tileSize, 1164500, 2731100,
			//                                              1165000));
		}
	}
}
