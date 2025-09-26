using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaExtentTest
	{
		private IFeatureWorkspace _ws;

		[OneTimeSetUp]
		public void OneTimeSetup()
		{
			TestUtils.InitializeLicense();
			_ws = TestWorkspaceUtils.CreateInMemoryWorkspace(nameof(QaExtentTest));
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanReportExtentLargerThanLimit()
		{
			IFeatureClass fc = CreatePolygonClass("Extent_LargerThanLimit");

			// 100 x 10 => max extent = 100
			IFeature f = fc.CreateFeature();
			f.Shape = GeometryFactory.CreatePolygon(0, 0, 100, 10);
			f.Store();

			var test = new QaExtent(ReadOnlyTableFactory.Create(fc), 50d);
			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();

			AssertUtils.OneError(runner, "Extent.ExtentLargerThanLimit");
		}

		[Test]
		public void NoErrorWhenExtentEqualOrLessThanLimit()
		{
			IFeatureClass fc = CreatePolygonClass("Extent_NoErrorOnBoundary");

			// 50 x 10 => max extent = 50
			IFeature f = fc.CreateFeature();
			f.Shape = GeometryFactory.CreatePolygon(0, 0, 50, 10);
			f.Store();

			var test = new QaExtent(ReadOnlyTableFactory.Create(fc), 50d);
			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();

			AssertUtils.NoError(runner);
		}

		[Test]
		public void PerPartAffectsCombinedExtent()
		{
			IFeatureClass fc = CreatePolygonClass("Extent_PerPart");

			// Two disjoint 10x10 squares far apart -> combined width 110
			IPolygon p1 = GeometryFactory.CreatePolygon(0, 0, 10, 10);
			IPolygon p2 = GeometryFactory.CreatePolygon(100, 0, 110, 10);
			IGeometry union = GeometryUtils.Union(p1, p2);

			IFeature f = fc.CreateFeature();
			f.Shape = union;
			f.Store();

			// Without per-part: combined extent causes error
			var testWhole = new QaExtent(ReadOnlyTableFactory.Create(fc), 20d, perPart: false);
			var runnerWhole = new QaContainerTestRunner(10000, testWhole);
			runnerWhole.Execute();
			AssertUtils.OneError(runnerWhole, "Extent.ExtentLargerThanLimit");

			// With per-part: each part's extent is 10 -> no error
			var testPerPart = new QaExtent(ReadOnlyTableFactory.Create(fc), 20d, perPart: true);
			var runnerPerPart = new QaContainerTestRunner(10000, testPerPart);
			runnerPerPart.Execute();
			AssertUtils.NoError(runnerPerPart);
		}

		[Test]
		public void NoErrorForEmptyGeometry()
		{
			IFeatureClass fc = CreatePolygonClass("Extent_EmptyGeometry");

			IFeature f = fc.CreateFeature(); // no shape set => empty
			f.Store();

			var test = new QaExtent(ReadOnlyTableFactory.Create(fc), 10d);
			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();

			AssertUtils.NoError(runner);
		}

		private IFeatureClass CreatePolygonClass(string name)
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000, 0.0001, 0.001);

			IFields fields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField("Shape", esriGeometryType.esriGeometryPolygon, sref,
				                            1000));

			return DatasetUtils.CreateSimpleFeatureClass(_ws, name, fields);
		}
	}
}
