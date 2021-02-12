using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.Test.TestData;

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
		public void MultipartTest()
		{
			IFeatureWorkspace workspace =
				TestWorkspaceUtils.CreateInMemoryWorkspace("MultipartTest");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline, CreateLV95(),
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

		[NotNull]
		private static ISpatialReference CreateLV95()
		{
			ISpatialReference result = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
			SpatialReferenceUtils.SetXYDomain(result, -10000, -10000, 10000, 10000,
			                                  0.0001, _xyTolerance);
			return result;
		}
	}
}
