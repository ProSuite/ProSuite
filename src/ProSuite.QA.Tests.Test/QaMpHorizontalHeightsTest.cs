using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMpHorizontalHeightsTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanTestMultiPatches()
		{
			var featureClassMock = new FeatureClassMock("mock",
			                                            esriGeometryType.esriGeometryMultiPatch, 1);

			var construction = new MultiPatchConstruction();

			construction.StartRing(0, 0, 0).Add(5, 0, 0).Add(5, 0, 1).Add(0, 0, 1);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalHeights(
				ReadOnlyTableFactory.Create(featureClassMock), 5, 0);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifyHeightDiffsLargerNearHeightNotChecked()
		{
			var featureClassMock = new FeatureClassMock("mock",
			                                            esriGeometryType.esriGeometryMultiPatch, 1);
			// make sure the table is known by the workspace

			const double nearHeight = 5;
			const double zNotChecked = nearHeight + 0.001;

			var construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 0).Add(0, 10, 0).Add(0, 10, zNotChecked).Add(0, 0,
				zNotChecked);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalHeights(
				ReadOnlyTableFactory.Create(featureClassMock), nearHeight, 0);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);

			const double zChecked = nearHeight - 0.001;
			construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 0).Add(0, 10, 0).Add(0, 10, zChecked).Add(0, 0,
				zChecked);

			row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			test = new QaMpHorizontalHeights(
				ReadOnlyTableFactory.Create(featureClassMock), nearHeight, 0);
			runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifyHeightDiffsSmallerToleranceNotReported()
		{
			var featureClassMock = new FeatureClassMock("mock",
			                                            esriGeometryType.esriGeometryMultiPatch, 1);
			// make sure the table is known by the workspace

			const double nearHeight = 5;
			const double tolerance = 0.5;
			const double zReported = tolerance + 0.001;

			var construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 0).Add(0, 10, 0).Add(0, 10, zReported).Add(0, 0,
				zReported);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalHeights(
				ReadOnlyTableFactory.Create(featureClassMock), nearHeight, tolerance);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);

			const double zNotReported = tolerance;
			construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 0).Add(0, 10, 0).Add(0, 10, zNotReported).Add(0, 0,
				zNotReported);

			row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			test = new QaMpHorizontalHeights(
				ReadOnlyTableFactory.Create(featureClassMock), nearHeight, tolerance);
			runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);
		}
	}
}
