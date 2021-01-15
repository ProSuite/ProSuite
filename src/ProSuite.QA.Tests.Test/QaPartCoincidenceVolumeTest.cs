using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaPartCoincidenceVolumeTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void TestBigLandcoverPolygonsTileSize_1()
		{
			const double tileSize = 20000;

			IList<QaError> errors = Execute(tileSize);

			Assert.AreEqual(9, errors.Count); // 11 for both self and other
		}

		[Test]
		public void TestBigLandcoverPolygonsTileSize_2()
		{
			const double tileSize = 10000;

			IList<QaError> errors = Execute(tileSize);

			Assert.AreEqual(9, errors.Count);
		}

		[Test]
		public void TestBigLandcoverPolygonsTileSize_3()
		{
			const double tileSize = 8000;

			IList<QaError> errors = Execute(tileSize);

			Assert.AreEqual(9, errors.Count);
		}

		[Test]
		public void TestBigLandcoverPolygonsTileSize_4()
		{
			const double tileSize = 6000;

			IList<QaError> errors = Execute(tileSize);

			Assert.AreEqual(9, errors.Count);
		}

		[Test]
		public void TestBigLandcoverPolygonsTileSize_5()
		{
			const double tileSize = 4000;

			IList<QaError> errors = Execute(tileSize);

			Assert.AreEqual(9, errors.Count);
		}

		[Test]
		public void TestBigLandcoverPolygonsTileSize_6()
		{
			const double tileSize = 2000;

			IList<QaError> errors = Execute(tileSize);

			Assert.AreEqual(9, errors.Count);
		}

		[NotNull]
		private static IList<QaError> Execute(double tileSize)
		{
			var locator = new TestDataLocator(@"..\..\EsriDE.ProSuite\src");
			string path = locator.GetPath("QaPartCoincidenceVolumeTest.mdb");

			IFeatureWorkspace ws = WorkspaceUtils.OpenPgdbFeatureWorkspace(path);

			IFeatureClass featureClass = ws.OpenFeatureClass("BigPolygons");

			var test = new QaPartCoincidenceSelf(featureClass, 10, 40, false);
			var runner = new QaContainerTestRunner(tileSize, test);

			IEnvelope box = new EnvelopeClass();
			box.PutCoords(2480190, 1134100, 2497430, 1140500);

			runner.Execute(box);

			return runner.Errors;
		}
	}
}
