using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Testing;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaBorderSenseTest
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
		public void CanHandleZeroLengthEndSegments()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateTestFgdbWorkspace("CanHandleZeroLengthEndSegments");
			IFeatureClass featureClass = TestWorkspaceUtils.CreateSimpleFeatureClass(
				ws, "polylines", null, esriGeometryType.esriGeometryPolyline,
				esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95);

			{
				IFeature f = featureClass.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(5,15).Curve;
				f.Store();
			}
			{
				IFeature f = featureClass.CreateFeature();
				f.Shape = CurveConstruction.StartLine(5, 15).LineTo(20, 10).Curve; 
				f.Store();
			}
			{
				IFeature f = featureClass.CreateFeature();
				f.Shape = CurveConstruction.StartLine(20, 10).LineTo(15, 15).Curve;
				f.Store();
			}
			{
				IFeature f = featureClass.CreateFeature();
				f.Shape = CurveConstruction.StartLine(15, 15).LineTo(10, 10).Curve;
				f.Store();
			}

			{
				IFeature f = featureClass.CreateFeature();
				f.Shape = CurveConstruction.StartLine(6, 15).LineTo(6, 15).Curve; // ZeroLength
				f.Store();
			}


			// expect counter-clockwise: 0 errors
			var runnerCounterClockwise = new QaContainerTestRunner(
				1000, new QaBorderSense(ReadOnlyTableFactory.Create(featureClass), false));
			Assert.AreEqual(1, runnerCounterClockwise.Execute());

			// expect clockwise: 1 error
			var runnerClockwise = new QaContainerTestRunner(
				1000, new QaBorderSense(ReadOnlyTableFactory.Create(featureClass), true));
			Assert.AreEqual(2, runnerClockwise.Execute());
		}

		[Test]
		public void VerifyBorderHandling()
		{
			IFeatureWorkspace workspace =
				TestWorkspaceUtils.CreateInMemoryWorkspace("BorderHandling");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference(
					                WellKnownHorizontalCS.LV95),
				                1000));

			IFeatureClass featureClass = DatasetUtils.CreateSimpleFeatureClass(
				workspace, "Border", fields);

			// invalid line
			AddFeature(featureClass,
			           CurveConstruction.StartLine(110, 20).LineTo(90, 30).LineTo(110, 40).Curve);

			// valid lines combination
			AddFeature(featureClass,
			           CurveConstruction.StartLine(110, 50).LineTo(90, 60).LineTo(110, 70).Curve);
			AddFeature(featureClass,
			           CurveConstruction.StartLine(110, 70).LineTo(110, 50).Curve);

			// expect clockwise: 1 error
			var runnerClockwise = new QaContainerTestRunner(
				1000, new QaBorderSense(ReadOnlyTableFactory.Create(featureClass), true));

			// errors outside of checked area
			Assert.AreEqual(
				0, runnerClockwise.Execute(GeometryFactory.CreateEnvelope(0, 0, 100, 100)));

			// errors within of checked area
			Assert.AreEqual(
				4, runnerClockwise.Execute(GeometryFactory.CreateEnvelope(0, 0, 200, 200)));

			runnerClockwise.ClearErrors();
			Assert.AreEqual(4, runnerClockwise.Execute());
		}

		[Test]
		public void MultipartTest()
		{
			IFeatureWorkspace workspace =
				TestWorkspaceUtils.CreateInMemoryWorkspace("MultipartTest");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference(
					                WellKnownHorizontalCS.LV95),
				                1000));

			IFeatureClass featureClass = DatasetUtils.CreateSimpleFeatureClass(
				workspace, "Border", fields);

			AddFeature(featureClass,
			           CurveConstruction.StartLine(0, 0).LineTo(4, 0).MoveTo(6, 0).LineTo(6, 10)
			                            .Curve);
			AddFeature(featureClass, CurveConstruction.StartLine(6, 10).LineTo(4, 10).Curve);
			AddFeature(featureClass,
			           CurveConstruction.StartLine(4, 0).LineTo(6, 0).MoveTo(4, 10).LineTo(0, 0)
			                            .Curve);

			// expect counter-clockwise: 0 errors
			var runnerCounterClockwise = new QaContainerTestRunner(
				1000, new QaBorderSense(ReadOnlyTableFactory.Create(featureClass), false));
			Assert.AreEqual(0, runnerCounterClockwise.Execute());

			// expect clockwise: 1 error
			var runnerClockwise = new QaContainerTestRunner(
				1000, new QaBorderSense(ReadOnlyTableFactory.Create(featureClass), true));

			Assert.AreEqual(1, runnerClockwise.Execute());
		}

		[NotNull]
		private static IFeature AddFeature(
			[NotNull] IFeatureClass featureClass,
			[NotNull] IGeometry geometry)
		{
			IFeature feature = featureClass.CreateFeature();
			feature.Shape = geometry;

			feature.Store();

			return feature;
		}
	}
}
