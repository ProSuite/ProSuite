using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaCoplanarRingsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanTestMultiPatch()
		{
			var fc = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryMultiPatch);

			var construction = new MultiPatchConstruction();
			construction.StartRing(5, 4, 1)
			            .Add(5, 8, 1)
			            .Add(8, 8, 1)
			            .Add(8, 4, 1);
			IFeature f = fc.CreateFeature(construction.MultiPatch);

			var test = new QaCoplanarRings(fc, 0, false);
			var runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(0, runner.Errors.Count);

			construction = new MultiPatchConstruction();
			construction.StartRing(5, 4, 1)
			            .Add(5, 8, 1)
			            .Add(8, 8, 1)
			            .Add(8, 4, 1.01);
			f = fc.CreateFeature(construction.MultiPatch);

			test = new QaCoplanarRings(fc, 0, false);
			runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanTestMultiPatch1()
		{
			var fc = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryMultiPatch);

			var construction = new MultiPatchConstruction();
			construction.StartRing(2579203.89625, 1079769.675, 2485.86625000001)
			            .Add(2579201.77375, 1079771.97375, 2488.5175)
			            .Add(2579198.27, 1079775.7675, 2484.82375);
			IFeature f = fc.CreateFeature(construction.MultiPatch);

			var test = new QaCoplanarRings(fc, 0, false);
			var runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void CanTestPolygon()
		{
			var fc = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryPolygon);

			CurveConstruction construction = CurveConstruction.StartPoly(5, 4, 1)
			                                                  .LineTo(5, 8, 1)
			                                                  .LineTo(8, 8, 1)
			                                                  .LineTo(8, 4, 1);
			IFeature f = fc.CreateFeature(construction.ClosePolygon());
			GeometryUtils.EnsureSpatialReference(f.Shape, fc);

			var test = new QaCoplanarRings(fc, 0, false);
			var runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(0, runner.Errors.Count);

			construction = CurveConstruction.StartPoly(5, 4, 1)
			                                .LineTo(5, 8, 1)
			                                .LineTo(8, 8, 1)
			                                .LineTo(8, 4, 1.01);
			f = fc.CreateFeature(construction.ClosePolygon());
			GeometryUtils.EnsureSpatialReference(f.Shape, fc);

			test = new QaCoplanarRings(fc, 0, false);
			runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifyErrorInToleranceNotReported()
		{
			var fc = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryMultiPatch);

			var construction = new MultiPatchConstruction();
			construction.StartRing(5, 4, 1)
			            .Add(5, 8, 1)
			            .Add(8, 8, 1)
			            .Add(8, 4, 1.01);
			IFeature f = fc.CreateFeature(construction.MultiPatch);

			var test = new QaCoplanarRings(fc, 0.005, false);
			var runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(0, runner.Errors.Count);

			test = new QaCoplanarRings(fc, 0.002, false);
			runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifyErrorInSrResolutionNotReported()
		{
			var fc = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryMultiPatch);
			var geodataset = (IGeoDataset) fc;
			var srt = (ISpatialReferenceResolution) geodataset.SpatialReference;
			double xySrResolution = srt.XYResolution[false];
			double zSrResolution = srt.ZResolution[false];

			// zTolerance
			var construction = new MultiPatchConstruction();
			construction.StartRing(5, 4, 1)
			            .Add(5, 8, 1)
			            .Add(8, 8, 1)
			            .Add(8, 4, 1 + 5 * zSrResolution);
			// was: 4x (but end point now no longer repeated)
			IFeature f = fc.CreateFeature(construction.MultiPatch);

			var test = new QaCoplanarRings(fc, 0, false);
			var runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(1, runner.Errors.Count); // NOTE failed with Plane3D (0 errors)

			construction = new MultiPatchConstruction();
			construction.StartRing(5, 4, 1)
			            .Add(5, 8, 1)
			            .Add(8, 8, 1)
			            .Add(8, 4, 1 + 2 * zSrResolution);
			f = fc.CreateFeature(construction.MultiPatch);

			test = new QaCoplanarRings(fc, 0, false);
			runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(0, runner.Errors.Count);

			// xyTolerance
			construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 1)
			            .Add(5, 5, 1)
			            .Add(5, 5, 8)
			            .Add(0 + 3 * xySrResolution, 0 - 3 * xySrResolution, 8);
			// was: 2x (but end point now no longer repeated)
			f = fc.CreateFeature(construction.MultiPatch);

			test = new QaCoplanarRings(fc, 0, false);
			runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(1, runner.Errors.Count); // NOTE failed with Plane3D (0 errors)

			construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 1)
			            .Add(5, 5, 1)
			            .Add(5, 5, 8)
			            .Add(0 + xySrResolution, 0 - xySrResolution, 8);
			f = fc.CreateFeature(construction.MultiPatch);

			test = new QaCoplanarRings(fc, 0, false);
			runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void VerticalPlaneIssue()
		{
			var fc = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryMultiPatch);

			var construction = new MultiPatchConstruction();
			construction.StartRing(2646275.33625, 1249624.19375, 379.188750000001)
			            .Add(2646269.675, 1249621.20625, 374.068750000006)
			            .Add(2646280.9975, 1249627.18125, 374.068750000006);

			IFeature f = fc.CreateFeature(construction.MultiPatch);

			var test = new QaCoplanarRings(fc, 0, false);
			var runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(0, runner.Errors.Count);
		}
	}
}
