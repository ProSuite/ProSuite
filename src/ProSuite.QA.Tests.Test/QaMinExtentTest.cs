using System.Linq;
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
	public class QaMinExtentTest
	{
		private IFeatureWorkspace _ws;

		[OneTimeSetUp]
		public void OneTimeSetup()
		{
			TestUtils.InitializeLicense();
			_ws = TestWorkspaceUtils.CreateInMemoryWorkspace(nameof(QaMinExtentTest));
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanReportExtentSmallerThanLimit()
		{
			IFeatureClass fc = CreatePolygonClass("MinExtent_SmallerThanLimit");

			// 10 x 10 => max extent = 10 < 50
			IFeature f = fc.CreateFeature();
			f.Shape = GeometryFactory.CreatePolygon(0, 0, 10, 10,
			                                        DatasetUtils.GetSpatialReference(fc));
			f.Store();

			var test = new QaMinExtent(ReadOnlyTableFactory.Create(fc), 50d);
			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();

			AssertUtils.OneError(runner, "MinExtent.ExtentSmallerThanLimit");

			// TODO: Check description (and fix the missing units!)
		}

		[Test]
		public void NoErrorWhenExtentEqualOrGreaterThanLimit()
		{
			IFeatureClass fc = CreatePolygonClass("MinExtent_NoErrorOnBoundary");

			// 50 x 10 => max extent = 50
			IFeature f = fc.CreateFeature();
			f.Shape = GeometryFactory.CreatePolygon(0, 0, 50, 10);
			f.Store();

			var test = new QaMinExtent(ReadOnlyTableFactory.Create(fc), 50d);
			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();

			AssertUtils.NoError(runner);
		}

		[Test]
		public void PerPartFindsTooSmallPart()
		{
			IFeatureClass fc = CreatePolygonClass("MinExtent_PerPart");

			// Two disjoint squares: 10x10 and 100x10; combined max width ~110
			IPolygon small = GeometryFactory.CreatePolygon(0, 0, 10, 10);
			IPolygon large = GeometryFactory.CreatePolygon(100, 0, 200, 10);
			IGeometry union = GeometryUtils.Union(small, large);

			IFeature f = fc.CreateFeature();
			f.Shape = union;
			f.Store();

			// Without per-part: combined max >= 50 => no error
			var testWhole = new QaMinExtent(ReadOnlyTableFactory.Create(fc), 50d);
			testWhole.PerPart = false;
			var runnerWhole = new QaContainerTestRunner(10000, testWhole);
			runnerWhole.Execute();
			AssertUtils.NoError(runnerWhole);

			// With per-part: the 10x10 part violates the limit => error
			var testPerPart = new QaMinExtent(ReadOnlyTableFactory.Create(fc), 50d)
			                  { PerPart = true };
			var runnerPerPart = new QaContainerTestRunner(10000, testPerPart);
			runnerPerPart.KeepGeometry = true;
			runnerPerPart.Execute();
			AssertUtils.OneError(runnerPerPart, "MinExtent.ExtentSmallerThanLimit");

			// Check error geometry:
			IGeometry errorGeometry = runnerPerPart.ErrorGeometries.Single();
			Assert.IsTrue(GeometryUtils.AreEqualInXY(small, errorGeometry));
		}

		[Test]
		public void NoErrorForEmptyGeometry()
		{
			IFeatureClass fc = CreatePolygonClass("MinExtent_EmptyGeometry");

			IFeature f = fc.CreateFeature(); // empty
			f.Store();

			var test = new QaMinExtent(ReadOnlyTableFactory.Create(fc), 10d);
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
