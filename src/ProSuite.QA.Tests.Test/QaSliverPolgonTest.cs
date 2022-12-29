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
	public class QaSliverPolgonTest
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

			construction.StartRing(0, 0, 0)
			            .Add(5, 0, 0)
			            .Add(5, 0, 1)
			            .Add(0, 0, 1);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var noErrorRunner = new QaTestRunner(
				new QaSliverPolygon(ReadOnlyTableFactory.Create(featureClassMock), 50, 1000));
			noErrorRunner.Execute(row1);
			Assert.AreEqual(0, noErrorRunner.Errors.Count);

			var oneErrorRunner = new QaTestRunner(
				new QaSliverPolygon(ReadOnlyTableFactory.Create(featureClassMock), 20, 1000));
			oneErrorRunner.Execute(row1);
			Assert.AreEqual(1, oneErrorRunner.Errors.Count);
		}

		[Test]
		public void CanTestInvalidMultiPatches()
		{
			var featureClassMock = new FeatureClassMock("mock",
			                                            esriGeometryType.esriGeometryMultiPatch, 1);

			var construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 0)
			            .Add(5, 0, 0);
			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			construction = new MultiPatchConstruction();
			construction.StartFan(0, 0, 0)
			            .Add(5, 0, 0);
			IFeature row2 = featureClassMock.CreateFeature(construction.MultiPatch);

			construction = new MultiPatchConstruction();
			construction.StartStrip(0, 0, 0)
			            .Add(5, 0, 0);
			IFeature row3 = featureClassMock.CreateFeature(construction.MultiPatch);

			construction = new MultiPatchConstruction();
			construction.StartTris(0, 0, 0)
			            .Add(5, 0, 0);
			IFeature row4 = featureClassMock.CreateFeature(construction.MultiPatch);

			var runner = new QaTestRunner(
				new QaSliverPolygon(ReadOnlyTableFactory.Create(featureClassMock), 50, 1000));
			runner.Execute(row1);
			runner.Execute(row2);
			runner.Execute(row3);
			runner.Execute(row4);
		}
	}
}
