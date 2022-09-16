using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaPartCoincidenceOtherTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

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

		[NotNull]
		private IFeatureWorkspace TestWorkspace =>
			_testWs ??
			(_testWs =
				 TestWorkspaceUtils.CreateInMemoryWorkspace("QaPartCoincidenceOtherTest")
			);

		[Test]
		[Ignore("Uses local Data")]
		public void TestTOP_4404()
		{
			var ws = (IFeatureWorkspace)
				TestDataUtils.OpenPgdb("AVR_CH1903_LV03_original.mdb");
			IFeatureClass avr_lie = ws.OpenFeatureClass("avr_lie");
			IFeatureClass avr_gem = ws.OpenFeatureClass("avr_gem");

			var test = new QaPartCoincidenceOther(
				ReadOnlyTableFactory.Create(avr_lie), ReadOnlyTableFactory.Create(avr_gem),
				                            0.02, 1, 0.1,
				                            false, 5000, 0);
			test.SetConstraint(0, "ObjectId=2988");
			test.SetConstraint(1, "ObjectId=3");
			var runner = new QaContainerTestRunner(10000, test);

			runner.Execute();
		}

		[Test]
		public void TestMultiPartError()
		{
			TestMultiPartErrorCore(TestWorkspace);
		}

		private static void TestMultiPartErrorCore(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass featureClass =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestMultiPartErrorCore1",
				                                      fields,
				                                      null);

			IFieldsEdit refFields = new FieldsClass();
			refFields.AddField(FieldUtils.CreateOIDField());
			refFields.AddField(FieldUtils.CreateShapeField(
				                   "Shape", esriGeometryType.esriGeometryPolygon,
				                   SpatialReferenceUtils.CreateSpatialReference
				                   ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                    true), 1000, false, false));

			IFeatureClass reference =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestMultiPartErrorCoreRef",
				                                      refFields,
				                                      null);

			IPolygon poly1 = CurveConstruction.StartPoly(0, 0.01)
			                                  .LineTo(1, 0.01)
			                                  .LineTo(1, 1)
			                                  .LineTo(0, 1)
			                                  .LineTo(0, 0.01)
			                                  .MoveTo(1.001, 0.01)
			                                  .LineTo(4, 0.01)
			                                  .LineTo(4, 1)
			                                  .LineTo(1.001, 1)
			                                  .LineTo(1.001, 0.01)
			                                  .ClosePolygon();
			((ITopologicalOperator) poly1).Simplify();
			IFeature row1 = featureClass.CreateFeature();
			row1.Shape = poly1;
			row1.Store();

			IPolygon referencePoly = CurveConstruction.StartPoly(-1, 0)
			                                          .LineTo(0.5, 0)
			                                          .LineTo(0.5, -10)
			                                          .LineTo(-1, -10)
			                                          .LineTo(-1, 0)
			                                          .MoveTo(0.81, 0)
			                                          .LineTo(10, 0)
			                                          .LineTo(10, -10)
			                                          .LineTo(0.81, 0)
			                                          .ClosePolygon();
			((ITopologicalOperator) referencePoly).Simplify();
			IFeature rowRef = reference.CreateFeature();
			rowRef.Shape = referencePoly;
			rowRef.Store();

			var test = new QaPartCoincidenceOther(
				ReadOnlyTableFactory.Create(featureClass), ReadOnlyTableFactory.Create(reference),
				                            0.02, 0.05, 0.1,
				                            false, 5000, 0);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(3, runner.Errors.Count);
		}

		[Test]
		public void CanReportNearlyCoincidentSectionErrorInLeftTile()
		{
			const string testName = "CanReportNearlyCoincidentSectionErrorInLeftTile";

			IFeatureClass testedClass =
				CreateFeatureClass(string.Format("{0}_tested", testName),
				                   esriGeometryType.esriGeometryPolyline);
			IFeatureClass referenceClass =
				CreateFeatureClass(string.Format("{0}_reference", testName),
				                   esriGeometryType.esriGeometryPolyline);

			IFeature testedRow = testedClass.CreateFeature();
			testedRow.Shape =
				CurveConstruction.StartLine(201, 150)
				                 .LineTo(201, 50)
				                 .Curve;
			testedRow.Store();

			IFeature referenceRow = referenceClass.CreateFeature();
			referenceRow.Shape =
				CurveConstruction.StartLine(199, 150)
				                 .LineTo(199, 50)
				                 .Curve;
			referenceRow.Store();

			IEnvelope verificationEnvelope =
				GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaPartCoincidenceOther(
				ReadOnlyTableFactory.Create(testedClass), ReadOnlyTableFactory.Create(referenceClass), 3, 50);

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner,
			                     "NearCoincidence.NearlyCoincidentSection.BetweenFeatures");
		}

		[Test]
		public void CanReportNearlyCoincidentSectionErrorInRightTile()
		{
			const string testName = "CanReportNearlyCoincidentSectionErrorInRightTile";

			IFeatureClass testedClass =
				CreateFeatureClass(string.Format("{0}_tested", testName),
				                   esriGeometryType.esriGeometryPolyline);
			IFeatureClass referenceClass =
				CreateFeatureClass(string.Format("{0}_reference", testName),
				                   esriGeometryType.esriGeometryPolyline);

			IFeature testedRow = testedClass.CreateFeature();
			testedRow.Shape = CurveConstruction.StartLine(199, 150)
			                                   .LineTo(199, 50)
			                                   .Curve;
			testedRow.Store();

			IFeature referenceRow = referenceClass.CreateFeature();
			referenceRow.Shape = CurveConstruction.StartLine(201, 150)
			                                      .LineTo(201, 50)
			                                      .Curve;
			referenceRow.Store();

			IEnvelope verificationEnvelope =
				GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaPartCoincidenceOther(
				ReadOnlyTableFactory.Create(testedClass), ReadOnlyTableFactory.Create(referenceClass), 3, 50);

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner,
			                     "NearCoincidence.NearlyCoincidentSection.BetweenFeatures");
		}

		[Test]
		public void CanReportNearlyCoincidentSectionErrorInSameTile()
		{
			const string testName = "CanReportNearlyCoincidentSectionErrorInSameTile";

			IFeatureClass testedClass =
				CreateFeatureClass(string.Format("{0}_tested", testName),
				                   esriGeometryType.esriGeometryPolyline);
			IFeatureClass referenceClass =
				CreateFeatureClass(string.Format("{0}_reference", testName),
				                   esriGeometryType.esriGeometryPolyline);

			IFeature testedRow = testedClass.CreateFeature();
			testedRow.Shape = CurveConstruction.StartLine(197, 150)
			                                   .LineTo(197, 50)
			                                   .Curve;
			testedRow.Store();

			IFeature referenceRow = referenceClass.CreateFeature();
			referenceRow.Shape = CurveConstruction.StartLine(199, 150)
			                                      .LineTo(199, 50)
			                                      .Curve;
			referenceRow.Store();

			IEnvelope verificationEnvelope =
				GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaPartCoincidenceOther(
				ReadOnlyTableFactory.Create(testedClass), ReadOnlyTableFactory.Create(referenceClass), 3, 50);

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner,
			                     "NearCoincidence.NearlyCoincidentSection.BetweenFeatures");
		}

		[Test]
		public void TestConditionCoincidence()
		{
			TestConditionCoincidence(TestWorkspace);
		}

		private static void TestConditionCoincidence(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateIntegerField("LandId"));
			fields.AddField(FieldUtils.CreateIntegerField("OtherId"));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc1 =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestConditionCoincidence1",
				                                      fields,
				                                      null);
			IFeatureClass fc2 =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestConditionCoincidence2",
				                                      fields,
				                                      null);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc1.CreateFeature();
				row.set_Value(1, 1);
				row.Shape = CurveConstruction.StartLine(100, 100).LineTo(200, 200).Curve;
				row.Store();
			}
			{
				IFeature row = fc2.CreateFeature();
				row.set_Value(1, 1);
				row.Shape = CurveConstruction
				            .StartLine(100.5, 100).LineTo(200.5, 200).Curve;
				row.Store();
			}

			// test without ignore conditions --> line is near, but not coincident
			var test = new QaPartCoincidenceOther(
				ReadOnlyTableFactory.Create(fc1), ReadOnlyTableFactory.Create(fc2), 1, 10);
			var testRunner = new QaTestRunner(test);
			testRunner.Execute();
			Assert.AreEqual(1, testRunner.Errors.Count);

			// Same test with ignore conditions --> nothing near
			test = new QaPartCoincidenceOther(
				ReadOnlyTableFactory.Create(fc1), ReadOnlyTableFactory.Create(fc2), 1, 10);
			test.IgnoreNeighborCondition = "G1.LandID = G2.LandID";

			testRunner = new QaTestRunner(test);
			testRunner.Execute();
			Assert.AreEqual(0, testRunner.Errors.Count);
		}

		private IFeatureClass CreateFeatureClass([NotNull] string name,
		                                         esriGeometryType type,
		                                         bool zAware = false)
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);

			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000, 0.0001,
			                                  0.001);

			const bool mAware = false;
			IFields fields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(
					"Shape",
					type,
					sref, 1000, zAware, mAware));

			return DatasetUtils.CreateSimpleFeatureClass(TestWorkspace, name, fields);
		}
	}
}
