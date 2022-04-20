using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaBorderSenseTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private const double _xyTolerance = 0.001;

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
		public void CanHandleZeroLengthEndSegments()
		{
			const string featureClassName = "TLM_STEHENDES_GEWAESSER";

			var locator = TestDataUtils.GetTestDataLocator();
			string path = locator.GetPath("QaBorderSense.gdb");

			IFeatureWorkspace workspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);

			IFeatureClass featureClass = workspace.OpenFeatureClass(featureClassName);

			// expect counter-clockwise: 0 errors
			var runnerCounterClockwise = new QaContainerTestRunner(1000,
				new QaBorderSense(
					featureClass, false));
			Assert.AreEqual(0, runnerCounterClockwise.Execute());

			// expect clockwise: 1 error
			var runnerClockwise = new QaContainerTestRunner(1000,
			                                                new QaBorderSense(featureClass,
				                                                true));
			Assert.AreEqual(1, runnerClockwise.Execute());
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
				1000, new QaBorderSense(featureClass, true));

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
				1000, new QaBorderSense(featureClass, false));
			Assert.AreEqual(0, runnerCounterClockwise.Execute());

			// expect clockwise: 1 error
			var runnerClockwise = new QaContainerTestRunner(
				1000, new QaBorderSense(featureClass, true));

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
