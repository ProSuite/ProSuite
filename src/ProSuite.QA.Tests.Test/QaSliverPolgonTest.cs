using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSliverPolgonTest
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
		public void CanTestMultiPatches()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);

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
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);

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
