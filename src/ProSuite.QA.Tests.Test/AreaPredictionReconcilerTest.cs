using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Test;
using Path = System.IO.Path;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class AreaPredictionReconcilerTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CreatesNonOverlappingCoresWithClippedHalos()
		{
			IList<AreaPredictionTileExtent> tiles = AreaPredictionTileGrid.Create(
				CreateEnvelope(0, 0, 1600, 800), 1000, 100);

			Assert.AreEqual(2, tiles.Count);
			Assert.AreEqual(900, tiles[0].RequestExtent.Width);
			Assert.AreEqual(800, tiles[0].RequestExtent.Height);
			Assert.AreEqual(0, tiles[0].RequestExtent.XMin);
			Assert.AreEqual(900, tiles[0].RequestExtent.XMax);
			Assert.AreEqual(700, tiles[1].RequestExtent.XMin);
			Assert.AreEqual(1600, tiles[1].RequestExtent.XMax);
			Assert.AreEqual(800, tiles[0].CoreExtent.Width);
			Assert.AreEqual(800, tiles[1].CoreExtent.Width);
			Assert.AreEqual(800, tiles[0].CoreExtent.XMax);
			Assert.AreEqual(800, tiles[1].CoreExtent.XMin);
			Assert.AreEqual(200,
			                tiles[0].RequestExtent.XMax -
			                tiles[1].RequestExtent.XMin);
		}

		[Test]
		public void CoversTwoAdjacentSwisstopoRastersCompletely()
		{
			IList<AreaPredictionTileExtent> tiles = AreaPredictionTileGrid.Create(
				CreateEnvelope(2494999.6, 1113999.9, 2497000.0, 1115000.1),
				1000, 100);

			Assert.AreEqual(2, tiles.Count);
			Assert.AreEqual(2494999.6, tiles[0].RequestExtent.XMin, 0.001);
			Assert.AreEqual(2496100.0, tiles[0].RequestExtent.XMax, 0.001);
			Assert.AreEqual(2495900.0, tiles[1].RequestExtent.XMin, 0.001);
			Assert.AreEqual(2497000.0, tiles[1].RequestExtent.XMax, 0.001);
			Assert.AreEqual(2496000.0, tiles[0].CoreExtent.XMax, 0.001);
			Assert.AreEqual(2496000.0, tiles[1].CoreExtent.XMin, 0.001);
			Assert.AreEqual(1113999.9,
			                tiles.Min(tile => tile.RequestExtent.YMin), 0.001);
			Assert.AreEqual(1115000.1,
			                tiles.Max(tile => tile.RequestExtent.YMax), 0.001);
		}

		[Test]
		public void DoesNotDropRemainderSmallerThanCoordinateTolerance()
		{
			IList<AreaPredictionTileExtent> tiles = AreaPredictionTileGrid.Create(
				CreateEnvelope(0, 0, 1000.5, 1000), 1000, 100);

			Assert.AreEqual(1, tiles.Count);
			Assert.AreEqual(0, tiles.Min(tile => tile.RequestExtent.XMin));
			Assert.AreEqual(1000.5, tiles.Max(tile => tile.RequestExtent.XMax));
			Assert.AreEqual(1000.5, tiles[0].CoreExtent.Width);
		}

		[Test]
		public void KeepsSingleRequestWhenWholeExtentFitsCorePlusHalos()
		{
			IList<AreaPredictionTileExtent> tiles = AreaPredictionTileGrid.Create(
				CreateEnvelope(0, 0, 1200.5, 1000), 1000, 100);

			Assert.AreEqual(1, tiles.Count);
			Assert.AreEqual(1200.5, tiles[0].CoreExtent.Width);
			Assert.AreEqual(1200.5, tiles[0].RequestExtent.Width);
		}

		[Test]
		public void SplitsLargerExtentWithoutTinyFinalCore()
		{
			IList<AreaPredictionTileExtent> tiles = AreaPredictionTileGrid.Create(
				CreateEnvelope(0, 0, 1201.5, 1000), 1000, 100);

			Assert.AreEqual(2, tiles.Count);
			Assert.AreEqual(600.75, tiles[0].CoreExtent.Width, 0.001);
			Assert.AreEqual(600.75, tiles[1].CoreExtent.Width, 0.001);
			Assert.AreEqual(700.75, tiles[0].RequestExtent.Width, 0.001);
			Assert.AreEqual(700.75, tiles[1].RequestExtent.Width, 0.001);
		}

		[Test]
		public void AbsorbsSubToleranceExcessAtMultiTileBoundary()
		{
			IList<AreaPredictionTileExtent> tiles = AreaPredictionTileGrid.Create(
				CreateEnvelope(0, 0, 2200.5, 1000), 1000, 100);

			Assert.AreEqual(2, tiles.Count);
			Assert.AreEqual(1100.25, tiles[0].CoreExtent.Width, 0.001);
			Assert.AreEqual(1100.25, tiles[1].CoreExtent.Width, 0.001);
			Assert.AreEqual(1200.25, tiles[0].RequestExtent.Width, 0.001);
			Assert.AreEqual(1200.25, tiles[1].RequestExtent.Width, 0.001);
		}

		[Test]
		public void GivesInteriorCoreFullHaloOnEverySide()
		{
			IList<AreaPredictionTileExtent> tiles = AreaPredictionTileGrid.Create(
				CreateEnvelope(0, 0, 3000, 3000), 1000, 100);

			AreaPredictionTileExtent centre = tiles[4];
			Assert.AreEqual(1000, centre.CoreExtent.XMin);
			Assert.AreEqual(1000, centre.CoreExtent.YMin);
			Assert.AreEqual(2000, centre.CoreExtent.XMax);
			Assert.AreEqual(2000, centre.CoreExtent.YMax);
			Assert.AreEqual(900, centre.RequestExtent.XMin);
			Assert.AreEqual(900, centre.RequestExtent.YMin);
			Assert.AreEqual(2100, centre.RequestExtent.XMax);
			Assert.AreEqual(2100, centre.RequestExtent.YMax);
		}

		[Test]
		public void KeepsOneOfTwoCompletePredictions()
		{
			IList<PredictionRequestTile> tiles = CreateAdjacentTiles();
			IList<AreaPredictionJobResult> jobs = CreateJobs(
				CreatePrediction(770, 100, 830, 140),
				CreatePrediction(771, 100, 831, 140));

			var reconciler = new AreaPredictionReconciler();
			AreaPredictionReconciliationResult result =
				reconciler.Reconcile(tiles, jobs);

			Assert.AreEqual(1, result.Predictions.Count);
			Assert.AreEqual(1, result.Statistics.DuplicateCount);
		}

		[Test]
		public void PrefersCompletePredictionOverBoundaryFragment()
		{
			IList<PredictionRequestTile> tiles = CreateAdjacentTiles();
			IList<AreaPredictionJobResult> jobs = CreateJobs(
				CreatePrediction(770, 100, 900, 140),
				CreatePrediction(770, 100, 930, 140));

			var reconciler = new AreaPredictionReconciler();
			AreaPredictionReconciliationResult result =
				reconciler.Reconcile(tiles, jobs);

			Assert.AreEqual(1, result.Predictions.Count);
			Assert.AreEqual(160, result.Predictions[0].Envelope.Width);
		}

		[Test]
		public void MergesMatchingBoundaryFragments()
		{
			IList<PredictionRequestTile> tiles = CreateAdjacentTiles();
			IList<AreaPredictionJobResult> jobs = CreateBoundaryFragmentJobs();
			var reconciler = new AreaPredictionReconciler();
			AreaPredictionReconciliationResult result =
				reconciler.Reconcile(tiles, jobs);

			Assert.AreEqual(1, result.Predictions.Count);
			Assert.AreEqual(250, result.Predictions[0].Envelope.Width);
			Assert.AreEqual(1, result.Statistics.MergedFragmentGroupCount);
		}

		[Test]
		public void RejectsUnmatchedBoundaryFragments()
		{
			IList<PredictionRequestTile> tiles = CreateAdjacentTiles();
			IList<AreaPredictionJobResult> jobs = CreateJobs(
				CreatePrediction(850, 100, 900, 140),
				CreatePrediction(700, 300, 750, 340));
			var reconciler = new AreaPredictionReconciler();
			AreaPredictionReconciliationResult result =
				reconciler.Reconcile(tiles, jobs);

			Assert.AreEqual(0, result.Predictions.Count);
			Assert.AreEqual(2, result.Statistics.RejectedFragmentCount);
		}

		[Test]
		public void KeepsPredictionAtOuterProcessingBoundary()
		{
			var tiles = new List<PredictionRequestTile>
			{
				CreateTile("tile_0.tif", CreateEnvelope(0, 0, 1000, 1000),
				           CreateEnvelope(0, 0, 1000, 1000))
			};
			var jobs = new List<AreaPredictionJobResult>
			{
				new AreaPredictionJobResult(
					"tile_0.tif", "job-0",
					new List<AreaPrediction>
					{
						CreatePrediction(0, 100, 50, 140)
					}, null)
			};

			AreaPredictionReconciliationResult result =
				new AreaPredictionReconciler().Reconcile(tiles, jobs);

			Assert.AreEqual(1, result.Predictions.Count);
			Assert.AreEqual(0, result.Statistics.RejectedFragmentCount);
		}

		[Test]
		public void DoesNotMergeBoundaryPredictionsWithSmallIncidentalOverlap()
		{
			IList<PredictionRequestTile> tiles = CreateAdjacentTiles();
			IList<AreaPredictionJobResult> jobs = CreateJobs(
				CreatePrediction(850, 100, 900, 140),
				CreatePrediction(700, 100, 855, 140));

			AreaPredictionReconciliationResult result =
				new AreaPredictionReconciler().Reconcile(tiles, jobs);

			Assert.AreEqual(0, result.Predictions.Count);
			Assert.AreEqual(2, result.Statistics.RejectedFragmentCount);
		}

		[Test]
		public void StoresRawResultInIndividualShapefile()
		{
			string directory = Path.Combine(
				Path.GetTempPath(), "AreaPredictionJobStoreTest",
				System.Guid.NewGuid().ToString("N"));
			string finalPath = Path.Combine(directory, "predictions.shp");

			try
			{
				var store = new AreaPredictionJobStore(finalPath, "buildings");
				var job = new AreaPredictionJobResult(
					"tile_0000.tif", "test-job",
					new List<AreaPrediction>
					{
						CreatePrediction(100, 100, 140, 130)
					}, null);

				string shapefilePath = store.StoreResult(job, null);

				Assert.True(File.Exists(shapefilePath));
				Assert.True(File.Exists(Path.ChangeExtension(shapefilePath, ".shx")));
				Assert.True(File.Exists(Path.ChangeExtension(shapefilePath, ".dbf")));
				Assert.That(Path.GetFileName(shapefilePath),
				            Does.Contain("test-job"));
			}
			finally
			{
				if (Directory.Exists(directory))
				{
					Directory.Delete(directory, true);
				}
			}
		}

		private static IList<PredictionRequestTile> CreateAdjacentTiles()
		{
			return new List<PredictionRequestTile>
			{
				CreateTile("tile_0.tif", CreateEnvelope(-100, -100, 900, 900),
				           CreateEnvelope(0, 0, 800, 800)),
				CreateTile("tile_1.tif", CreateEnvelope(700, -100, 1700, 900),
				           CreateEnvelope(800, 0, 1600, 800))
			};
		}

		private static IList<AreaPredictionJobResult> CreateBoundaryFragmentJobs()
		{
			return CreateJobs(
				CreatePrediction(750, 100, 900, 140),
				CreatePrediction(700, 100, 950, 140));
		}

		private static IList<AreaPredictionJobResult> CreateJobs(
			AreaPrediction first, AreaPrediction second)
		{
			return new List<AreaPredictionJobResult>
			{
				new AreaPredictionJobResult(
					"tile_0.tif", "job-0",
					new List<AreaPrediction> { first }, null),
				new AreaPredictionJobResult(
					"tile_1.tif", "job-1",
					new List<AreaPrediction> { second }, null)
			};
		}

		private static PredictionRequestTile CreateTile(
			string path, IEnvelope requestExtent, IEnvelope coreExtent)
		{
			return new PredictionRequestTile(
				path, coreExtent,
				new RasterTileInfo(requestExtent, 10000, 10000,
				                   requestExtent.SpatialReference));
		}

		private static AreaPrediction CreatePrediction(
			double xMin, double yMin, double xMax, double yMax)
		{
			return new AreaPrediction(new List<PredictionPoint>
			{
				new PredictionPoint(xMin, yMin),
				new PredictionPoint(xMax, yMin),
				new PredictionPoint(xMax, yMax),
				new PredictionPoint(xMin, yMax),
				new PredictionPoint(xMin, yMin)
			});
		}

		private static IEnvelope CreateEnvelope(
			double xMin, double yMin, double xMax, double yMax)
		{
			IEnvelope result = new EnvelopeClass();
			result.PutCoords(xMin, yMin, xMax, yMax);
			return result;
		}
	}
}
