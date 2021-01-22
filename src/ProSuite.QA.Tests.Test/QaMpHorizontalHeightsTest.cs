using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMpHorizontalHeightsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanTestMultiPatches()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);

			var construction = new MultiPatchConstruction();

			construction.StartRing(0, 0, 0).Add(5, 0, 0).Add(5, 0, 1).Add(0, 0, 1);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalHeights(featureClassMock, 5, 0);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifyHeightDiffsLargerNearHeightNotChecked()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);
			// make sure the table is known by the workspace

			const double nearHeight = 5;
			const double zNotChecked = nearHeight + 0.001;

			var construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 0).Add(0, 10, 0).Add(0, 10, zNotChecked).Add(0, 0,
			                                                                          zNotChecked);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalHeights(featureClassMock, nearHeight, 0);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);

			const double zChecked = nearHeight - 0.001;
			construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 0).Add(0, 10, 0).Add(0, 10, zChecked).Add(0, 0,
			                                                                       zChecked);

			row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			test = new QaMpHorizontalHeights(featureClassMock, nearHeight, 0);
			runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifyHeightDiffsSmallerToleranceNotReported()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);
			// make sure the table is known by the workspace

			const double nearHeight = 5;
			const double tolerance = 0.5;
			const double zReported = tolerance + 0.001;

			var construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 0).Add(0, 10, 0).Add(0, 10, zReported).Add(0, 0,
			                                                                        zReported);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalHeights(featureClassMock, nearHeight, tolerance);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);

			const double zNotReported = tolerance;
			construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 0).Add(0, 10, 0).Add(0, 10, zNotReported).Add(0, 0,
			                                                                           zNotReported);

			row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			test = new QaMpHorizontalHeights(featureClassMock, nearHeight, tolerance);
			runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);
		}
	}
}
