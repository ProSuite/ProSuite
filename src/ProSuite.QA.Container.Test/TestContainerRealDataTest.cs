using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Tests;
using ProSuite.QA.Tests.IssueFilters;
using ProSuite.QA.Tests.Transformers;
using Path = System.IO.Path;

namespace ProSuite.QA.Container.Test
{
	[TestFixture]
	public class TestContainerRealDataTest
	{
		// NOTE:
		// In order to run tests in this class, copy the test data from <GoTop Dev Team>\General\Data\UnitTestData
		// to C:\Temp\UnitTestData\

		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();
			Commons.AO.Test.TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			Commons.AO.Test.TestUtils.ReleaseLicense();
		}

		[Test]
		[Category(TestCategory.Repro)]
		[Category(TestCategory.Slow)]
		public void CanExecuteContainerMultipleTileCachesWithOutOfTileLoading()
		{
			// NOTE: Copy the test data from <GoTop Dev Team>\General\Data\UnitTestData
			const string unitTestDataPath = @"C:\Temp\UnitTestData\QA_DissolveIntersectOutOfTile";

			string sourceZipFile =
				Path.Combine(unitTestDataPath, "TestDataSG11_Churfirsten.zip");

			string targetFgdbPath = Path.Combine(unitTestDataPath, "Gebaeudeeinheit_SG_11.gdb");

			// Extract fails if the files already exist. For a deep-clean just remove the extracted Gdb.
			if (! Directory.Exists(targetFgdbPath))
			{
				ZipFile.ExtractToDirectory(sourceZipFile, unitTestDataPath);
			}

			// TODO: This test currently shows the non-optimal behaviour w.r.t multiple loading of
			//       the same tile. This shall be optimized further (keep 1 tile to the left in the cache,
			//       possibly load a reduced-size tile in the south of the current tile or use direct access).

			// TOP-5911: Some errors are found outside the current tile (due to dissolved features)
			// and hence the issue filter must be able to load features from outside the current
			// tile. This is done using the TilesAdmin class. However, ALL transformers must
			// ensure that it is actually used (by propagating the TileExtent in the spatial filter)

			IFeatureWorkspace featureWorkspace =
				WorkspaceUtils.OpenFeatureWorkspace(targetFgdbPath);

			ReadOnlyFeatureClass fcGebaeudeEinheit =
				ReadOnlyTableFactory.Create(
					featureWorkspace.OpenFeatureClass("TLM_GEBAEUDEEINHEIT"));

			var fcDachKoerper = ReadOnlyTableFactory.Create(
				featureWorkspace.OpenFeatureClass("TLM_GEBAEUDEKOERPER"));

			TrFootprint trFootprint = new TrFootprint(fcDachKoerper);
			trFootprint.SetConstraint(0, "OBJEKTART = 1");
			trFootprint.Tolerance = 0.01;
			trFootprint.TransformerName = "DachKoerperFootprint";
			IReadOnlyFeatureClass fcDachFootprint = trFootprint.GetTransformed();

			TrDissolve trDachFootprintDissolve = new TrDissolve(fcDachFootprint);
			trDachFootprintDissolve.NeighborSearchOption = TrDissolve.SearchOption.Tile;
			trDachFootprintDissolve.Search = 100;
			trDachFootprintDissolve.GroupBy = new List<string> { "TLM_GEBAEUDEEINHEIT_UUID" };
			trDachFootprintDissolve.TransformerName = "DachFootprintDissolved";
			IReadOnlyFeatureClass fcDachFootprintDissolved =
				trDachFootprintDissolve.GetTransformed();

			QaIsCoveredByOther isCovered = new QaIsCoveredByOther(
				new List<IReadOnlyFeatureClass> { fcDachFootprintDissolved },
				new List<GeometryComponent> { GeometryComponent.EntireGeometry },
				new List<IReadOnlyFeatureClass> { fcGebaeudeEinheit },
				new List<GeometryComponent> { GeometryComponent.EntireGeometry },
				new List<string> { "G2.UUID = G1.TLM_GEBAEUDEEINHEIT_UUID" },
				0);

			TrIntersect trIntersect = new TrIntersect(fcGebaeudeEinheit, fcGebaeudeEinheit);

			trIntersect.TransformerName = "GebEinheitIntersected";

			IReadOnlyFeatureClass fcIntersect = trIntersect.GetTransformed();

			TrDissolve trDissolve = new TrDissolve(fcIntersect);
			trDissolve.NeighborSearchOption = TrDissolve.SearchOption.Tile;
			trDissolve.Search = 100;
			trDissolve.CreateMultipartFeatures = true;
			IReadOnlyFeatureClass fcDissolve = trDissolve.GetTransformed();

			var fcPolyErrors =
				ReadOnlyTableFactory.Create(
					featureWorkspace.OpenFeatureClass("TLM_ERRORS_POLYGON"));

			((IFilterEditTest) isCovered).SetIssueFilters(null, new List<IIssueFilter>
			                                                    {
				                                                    new IfWithin(fcDissolve)
			                                                    });

			var container = new TestContainer.TestContainer { TileSize = 10000 };

			container.AddTest(isCovered);

			IEnvelope aoi =
				GeometryFactory.CreateEnvelope(2715082.90, 1218000.00, 2757000, 1230000);

			int errorCount = container.Execute(aoi);

			Console.WriteLine($"Error count: {errorCount}");

			Assert.AreEqual(33, errorCount);
		}
	}
}
