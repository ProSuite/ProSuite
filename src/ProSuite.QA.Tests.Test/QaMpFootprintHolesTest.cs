using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMpFootprintHolesTest
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
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			var featureClassMock = new FeatureClassMock(
				"mock",
				esriGeometryType.esriGeometryMultiPatch,
				1, esriFeatureType.esriFTSimple, sref);

			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(5, 4, 0)
			            .Add(5, 8, 0)
			            .Add(8, 8, 0)
			            .Add(8, 4, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			construction = new MultiPatchConstruction();
			construction.StartOuterRing(5, 4, 0)
			            .Add(5, 8, 0)
			            .Add(8, 8, 0)
			            .Add(8, 4, 0)
			            .StartInnerRing(6, 5, 0)
			            .Add(7, 5, 0)
			            .Add(7, 7, 0)
			            .Add(6, 7, 0);

			IFeature row2 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpFootprintHoles(ReadOnlyTableFactory.Create(featureClassMock),
			                                  InnerRingHandling.None);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);

			runner.Execute(row2);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanTestIgnoreInnerRings()
		{
			var featureClassMock = new FeatureClassMock("mock",
			                                            esriGeometryType.esriGeometryMultiPatch, 1);

			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(5, 4, 0)
			            .Add(5, 8, 0)
			            .Add(8, 8, 0)
			            .Add(8, 4, 0)
			            .StartInnerRing(6, 5, 0)
			            .Add(7, 5, 0)
			            .Add(7, 7, 0)
			            .Add(6, 7, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			construction = new MultiPatchConstruction();

			construction.StartRing(0, 0, 0)
			            .Add(0, 1, 0)
			            .Add(5, 1, 0)
			            .Add(5, 0, 0)
			            .StartRing(5, 1, 0)
			            .Add(4, 1, 0)
			            .Add(4, 5, 0)
			            .Add(5, 5, 0)
			            .StartRing(5, 5, 0)
			            .Add(1, 5, 0)
			            .Add(1, 6, 0)
			            .Add(5, 6, 0)
			            .StartRing(1, 6, 0)
			            .Add(1, 1, 0)
			            .Add(0, 1, 0)
			            .Add(0, 6, 0);

			IFeature row2 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpFootprintHoles(
				ReadOnlyTableFactory.Create(featureClassMock),
				InnerRingHandling.IgnoreInnerRings);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);

			runner.Execute(row2);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanTestIgnoreHorizotalInnerRings()
		{
			var featureClassMock = new FeatureClassMock("mock",
			                                            esriGeometryType.esriGeometryMultiPatch, 1);

			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(0, 0, 0)
			            .Add(0, 10, 0)
			            .Add(10, 10, 0)
			            .Add(10, 0, 0)
			            .StartInnerRing(2, 2, 0)
			            .Add(2, 8, 0)
			            .Add(8, 8, 0)
			            .Add(8, 2, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			construction = new MultiPatchConstruction();

			construction.StartOuterRing(0, 0, 0)
			            .Add(0, 10, 10)
			            .Add(10, 10, 10)
			            .Add(10, 0, 0)
			            .StartInnerRing(2, 2, 2)
			            .Add(2, 8, 8)
			            .Add(8, 8, 8)
			            .Add(8, 2, 2);

			IFeature row2 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpFootprintHoles(
				ReadOnlyTableFactory.Create(featureClassMock),
				InnerRingHandling.IgnoreHorizontalInnerRings);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);

			runner.Execute(row2);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanTestIgnoreNearlyHorizotalInnerRings()
		{
			var featureClassMock = new FeatureClassMock("mock",
			                                            esriGeometryType.esriGeometryMultiPatch, 1);

			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(0, 0, 0)
			            .Add(0, 10, 0)
			            .Add(10, 10, 0)
			            .Add(10, 0, 0)
			            .StartInnerRing(2, 2, 0.1)
			            .Add(2, 8, 0)
			            .Add(8, 8, -0.1)
			            .Add(8, 2, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			construction = new MultiPatchConstruction();

			construction.StartOuterRing(0, 0, 0)
			            .Add(0, 10, 10)
			            .Add(10, 10, 10)
			            .Add(10, 0, 0)
			            .StartInnerRing(2, 2, 0.11)
			            .Add(2, 8, 0)
			            .Add(8, 8, -0.1)
			            .Add(8, 2, 0);

			IFeature row2 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpFootprintHoles(
				           ReadOnlyTableFactory.Create(featureClassMock),
				           InnerRingHandling.IgnoreHorizontalInnerRings)
			           {
				           HorizontalZTolerance = 0.2
			           };

			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);

			runner.Execute(row2);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanTestVerticalMultiPatches()
		{
			var featureClassMock = new FeatureClassMock("mock",
			                                            esriGeometryType.esriGeometryMultiPatch, 1);

			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(0, 0, 0)
			            .Add(5, 0, 0)
			            .Add(5, 0, 5)
			            .Add(0, 0, 5);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			construction = new MultiPatchConstruction();

			construction.StartOuterRing(0, 0, 0)
			            .Add(5, 0, 0)
			            .Add(5, 0, 5)
			            .Add(0, 0, 5)
			            .StartOuterRing(5, 0, 0)
			            .Add(2, 5, 0)
			            .Add(2, 5, 0)
			            .Add(5, 0, 5)
			            .StartOuterRing(2, 5, 0)
			            .Add(0, 0, 0)
			            .Add(0, 0, 5)
			            .Add(2, 5, 5)
			            .StartOuterRing(0, 0, 5)
			            .Add(5, 0, 5)
			            .Add(2, 5, 5)
			            .StartInnerRing(1, 1, 5)
			            .Add(2, 4, 5)
			            .Add(4, 1, 5);

			IFeature row2 = featureClassMock.CreateFeature(construction.MultiPatch);

			construction = new MultiPatchConstruction();
			// 1 vertical rings building building a hole --> leads to no error !? Handle specially?
			construction.StartOuterRing(0, 0, 0)
			            .Add(5, 0, 0)
			            .Add(5, 0, 5)
			            .Add(0, 0, 5)
			            .StartOuterRing(0, 0, 5)
			            .Add(2, 5, 5)
			            .Add(2, 8, 5)
			            .StartOuterRing(5, 0, 5)
			            .Add(2, 8, 5)
			            .Add(2, 5, 5);

			IFeature row3 = featureClassMock.CreateFeature(construction.MultiPatch);

			construction = new MultiPatchConstruction();
			// 3 vertical rings building a prisma --> leads to no error !? Handle specially?
			construction.StartOuterRing(0, 0, 0)
			            .Add(5, 0, 0)
			            .Add(5, 0, 5)
			            .Add(0, 0, 5)
			            .StartOuterRing(5, 0, 0)
			            .Add(2, 5, 0)
			            .Add(2, 5, 5)
			            .Add(5, 0, 5)
			            .StartOuterRing(2, 5, 0)
			            .Add(0, 0, 0)
			            .Add(0, 0, 5)
			            .Add(2, 5, 5);

			IFeature row4 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpFootprintHoles(
				ReadOnlyTableFactory.Create(featureClassMock), InnerRingHandling.None);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);

			runner.Execute(row2);
			Assert.AreEqual(1, runner.Errors.Count);

			runner.Errors.Clear();
			runner.Execute(row3);
			Assert.AreEqual(1, runner.Errors.Count);

			runner.Errors.Clear();
			runner.Execute(row4);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		[Ignore("Uses local data")]
		public void VerifyTOP4472()
		{
			IWorkspace ws = TestDataUtils.OpenFileGdb("gebaeude-footprint-qa.gdb");
			IFeatureClass fc =
				((IFeatureWorkspace) ws).OpenFeatureClass("TLM_GEBAEUDE");

			var test = new QaMpFootprintHoles(
				ReadOnlyTableFactory.Create(fc), InnerRingHandling.IgnoreHorizontalInnerRings);
			test.SetConstraint(0, "OBJECTID = 1219794");
			var runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(0, runner.Errors.Count);

			test.SetConstraint(0, "OBJECTID = 1219793");
			runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		[Ignore("Uses local data")]
		public void VerifyTOP4472_20140117_1()
		{
			IWorkspace ws = TestDataUtils.OpenPgdb(@"QA_Fehler\QA_Fehler.mdb");
			IFeatureClass fc =
				((IFeatureWorkspace) ws).OpenFeatureClass("TLM_GEBAEUDE");

			var test = new QaMpFootprintHoles(
				ReadOnlyTableFactory.Create(fc), InnerRingHandling.None);
			var runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		[Ignore("Uses local data")]
		public void VerifyTOP4472_20140117_2()
		{
			IWorkspace ws = TestDataUtils.OpenPgdb(@"QA_Fehler\QA_Fehler2.mdb");
			IFeatureClass fc =
				((IFeatureWorkspace) ws).OpenFeatureClass("TLM_GEBAEUDE");

			var test = new QaMpFootprintHoles(
				ReadOnlyTableFactory.Create(fc), InnerRingHandling.None);
			var runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(0, runner.Errors.Count);
		}
	}
}
