using System.Collections.Generic;
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
	public class QaMpConstantPointIdsPerRingTest
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
			var fc = new FeatureClassMock("mock", esriGeometryType.esriGeometryMultiPatch, 1);

			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(5, 4, 0, 1).Add(5, 8, 0, 1).Add(8, 8, 0, 1).Add(8, 4, 0, 1)
			            .StartInnerRing(6, 5, 0, 2).Add(6, 7, 0, 2).Add(7, 7, 0, 2);
			IMultiPatch multiPatch = construction.MultiPatch;
			multiPatch.SpatialReference = ((IGeoDataset) fc).SpatialReference;
			multiPatch.SnapToSpatialReference();

			IFeature row1 = fc.CreateFeature();
			row1.Shape = multiPatch;
			row1.Store();
			const bool includeInnerRings = true;
			const bool doNotIncludeInnerRings = false;

			var test =
				new QaMpConstantPointIdsPerRing(ReadOnlyTableFactory.Create(fc),
				                                doNotIncludeInnerRings);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);

			test = new QaMpConstantPointIdsPerRing(ReadOnlyTableFactory.Create(fc),
			                                       includeInnerRings);
			runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifyErrorTypes()
		{
			var fc = new FeatureClassMock("mock", esriGeometryType.esriGeometryMultiPatch, 1);

			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(5, 4, 0, 1).Add(5, 8, 0, 1).Add(8, 8, 0, 1).Add(8, 4, 0, 1)
			            .StartInnerRing(6, 5, 0, 2).Add(6, 7, 0, 2).Add(7, 7, 0, 2);
			IMultiPatch multiPatch = construction.MultiPatch;
			multiPatch.SpatialReference = ((IGeoDataset) fc).SpatialReference;
			multiPatch.SnapToSpatialReference();

			var errorDict = new Dictionary<string, string>();
			// throws an error if the same error description is added

			IFeature row1 = fc.CreateFeature(multiPatch);
			const bool includeInnerRings = true;
			const bool doNotIncludeInnerRings = false;

			var test =
				new QaMpConstantPointIdsPerRing(ReadOnlyTableFactory.Create(fc), includeInnerRings);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
			errorDict.Add(runner.Errors[0].Description, "");

			construction = new MultiPatchConstruction();
			construction.StartOuterRing(5, 4, 0, 1).Add(5, 8, 0, 1).Add(8, 8, 0, 1).Add(8, 4, 0, 1)
			            .StartInnerRing(6, 5, 0, 2).Add(6, 7, 0, 2).Add(7, 7, 0, 3);
			multiPatch = construction.MultiPatch;
			multiPatch.SpatialReference = ((IGeoDataset) fc).SpatialReference;
			multiPatch.SnapToSpatialReference();

			row1 = fc.CreateFeature(multiPatch);

			test = new QaMpConstantPointIdsPerRing(ReadOnlyTableFactory.Create(fc),
			                                       includeInnerRings);
			runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
			errorDict.Add(runner.Errors[0].Description, "");

			construction = new MultiPatchConstruction();
			construction.StartOuterRing(5, 4, 0, 1).Add(5, 8, 0, 3).Add(8, 8, 0, 4).Add(8, 4, 0, 1)
			            .StartInnerRing(6, 5, 0, 2).Add(6, 7, 0, 2).Add(7, 7, 0, 3);
			multiPatch = construction.MultiPatch;
			multiPatch.SpatialReference = ((IGeoDataset) fc).SpatialReference;
			multiPatch.SnapToSpatialReference();

			row1 = fc.CreateFeature(multiPatch);

			test = new QaMpConstantPointIdsPerRing(ReadOnlyTableFactory.Create(fc),
			                                       includeInnerRings);
			runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
			errorDict.Add(runner.Errors[0].Description, "");

			construction = new MultiPatchConstruction();
			construction.StartOuterRing(5, 4, 0, 1).Add(5, 8, 0, 2).Add(8, 8, 0, 3).Add(8, 4, 0, 3)
			            .StartInnerRing(6, 5, 0, 1).Add(6, 7, 0, 1).Add(7, 7, 0, 1);
			multiPatch = construction.MultiPatch;
			multiPatch.SpatialReference = ((IGeoDataset) fc).SpatialReference;
			multiPatch.SnapToSpatialReference();

			row1 = fc.CreateFeature(multiPatch);

			test = new QaMpConstantPointIdsPerRing(ReadOnlyTableFactory.Create(fc),
			                                       doNotIncludeInnerRings);
			runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
			errorDict.Add(runner.Errors[0].Description, "");

			construction = new MultiPatchConstruction();
			construction.StartOuterRing(5, 4, 0, 1).Add(5, 8, 0, 1).Add(8, 8, 0, 3).Add(8, 4, 0, 1)
			            .StartInnerRing(6, 5, 0, 1).Add(6, 7, 0, 1).Add(7, 7, 0, 1);
			multiPatch = construction.MultiPatch;
			multiPatch.SpatialReference = ((IGeoDataset) fc).SpatialReference;
			multiPatch.SnapToSpatialReference();

			row1 = fc.CreateFeature(multiPatch);

			test = new QaMpConstantPointIdsPerRing(ReadOnlyTableFactory.Create(fc),
			                                       doNotIncludeInnerRings);
			runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
			errorDict.Add(runner.Errors[0].Description, "");
		}
	}
}
