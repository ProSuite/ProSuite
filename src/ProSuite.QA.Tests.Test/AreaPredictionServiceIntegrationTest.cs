using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using Path = System.IO.Path;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Com;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class AreaPredictionServiceIntegrationTest
	{
		private const double InriaTrainingGroundSampleDistance = 0.3;
		private const double TestImageLv95OriginX = 2600000;
		private const double TestImageLv95OriginY = 1200000;
		private const int RequestTimeoutSeconds = 10000;

		[OneTimeSetUp]
		public void SetupFixture() => TestUtils.InitializeLicense();

		[OneTimeTearDown]
		public void TeardownFixture() => TestUtils.ReleaseLicense();

		[Test]
		[Explicit("Calls remote prediction service and consumes GPU credits.")]
		public void PredictionClientCanCallRemoteService()
		{
			PredictionServiceTestConfig config = PredictionServiceTestConfig.LoadOrIgnore();
			config.AssertHealthy();

			string projectedImagePath = CreatePredictionSmokeTestTiff(config.ImagePath);
			try
			{
				var client = new AreaPredictionClient();
				IList<AreaPrediction> predictions = client.GetPredictions(
					projectedImagePath, config.ApiUrl, config.ModelName,
					TimeSpan.FromSeconds(RequestTimeoutSeconds));

				TestContext.WriteLine(
					$"Prediction returned {predictions.Count} prediction(s) for {projectedImagePath}");
				TestContext.WriteLine($"Source TIFF: {config.ImagePath}");
				TestContext.WriteLine($"API URL: {config.ApiUrl}");
				TestContext.WriteLine($"Model: {config.ModelName}");

				Assert.That(predictions, Is.Not.Empty);
				foreach (AreaPrediction prediction in predictions)
				{
					Assert.That(prediction.Shell.Count, Is.GreaterThanOrEqualTo(3));
				}
			}
			finally
			{
				DeleteTemporaryTiffDirectory(projectedImagePath);
			}
		}

		[Test]
		[Explicit("Runs QaAreaPredictionMatch against remote prediction service and consumes GPU credits.")]
		public void QaAreaPredictionMatchCanRunRemoteServiceCalculation()
		{
			PredictionServiceTestConfig config =
				PredictionServiceTestConfig.LoadOrIgnore(requireMatchingFeatures: true);
			config.AssertHealthy();

			IRasterDataset rasterDataset = null;
			IFeatureWorkspace workspace = null;
			IFeatureClass featureClass = null;
			try
			{
				rasterDataset = DatasetUtils.OpenRasterDataset(config.ImagePath);
				var rasterReference = new RasterDatasetReference(rasterDataset);

				workspace = WorkspaceUtils.OpenShapefileWorkspace(
					Path.GetDirectoryName(config.ShapefilePath));
				featureClass = workspace.OpenFeatureClass(
					Path.GetFileNameWithoutExtension(config.ShapefilePath));
				foreach (int oid in new[] { config.MatchedFeatureOid, config.MissingFeatureOid })
				{
					IFeature feature = featureClass.GetFeature(oid);
					Assert.That(GeometryProperties.GetArea(feature.Shape), Is.GreaterThanOrEqualTo(6));
					ComUtils.ReleaseComObject(feature);
				}

				var test = new QaAreaPredictionMatch(
					           ReadOnlyTableFactory.Create(featureClass),
					           rasterReference, config.ApiUrl, config.ModelName)
				           {
					           MatchMode = AreaPredictionMatchMode.Both
				           };

				var runner = new QaContainerTestRunner(10000, test);
				IEnvelope extent = ((IGeoDataset) featureClass).Extent;
				Assert.That(((IRelationalOperator) rasterReference.GeoDataset.Extent)
					.Contains(extent), Is.True, "Reference features must lie within the raster.");

				int errorCount = runner.Execute(extent);

				TestContext.WriteLine(
					$"QaAreaPredictionMatch returned {errorCount} error(s).");
				TestContext.WriteLine($"API URL: {config.ApiUrl}");
				TestContext.WriteLine($"Model: {config.ModelName}");
				TestContext.WriteLine($"Source TIFF: {config.ImagePath}");
				foreach (ProSuite.QA.Container.QaError error in runner.Errors)
				{
					TestContext.WriteLine(error.Description);
				}

				Assert.That(runner.Errors.All(error => error.IssueCode.ID ==
					"AreaPredictionMatch.PotentialUnmatchedPrediction" || error.IssueCode.ID ==
					"AreaPredictionMatch.PotentialMissingPrediction"), Is.True,
					"Inference failures must fail this test.");
				Assert.AreEqual(config.ExpectedNewPredictionCount,
					runner.Errors.Count(error => error.IssueCode.ID ==
						"AreaPredictionMatch.PotentialUnmatchedPrediction"));
				Assert.That(runner.Errors.Any(error => error.InvolvedRows.Any(
					row => row.OID == config.MatchedFeatureOid)), Is.False,
					"The known matched feature must remain covered.");
				Assert.That(runner.Errors.Any(error => error.IssueCode.ID ==
					"AreaPredictionMatch.PotentialMissingPrediction" && error.InvolvedRows.Any(
						row => row.OID == config.MissingFeatureOid)), Is.True,
					"The known missing feature must be reported.");
			}
			finally
			{
				ComUtils.ReleaseComObject(featureClass);
				ComUtils.ReleaseComObject(workspace);
				ComUtils.ReleaseComObject(rasterDataset);
			}
		}

		private static string CreatePredictionSmokeTestTiff(string imagePath)
		{
			string directory = Path.Combine(
				Path.GetTempPath(), "ProSuiteAreaPredictionMatchTest",
				Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);

			string outputPath = Path.Combine(
				directory,
				Path.GetFileNameWithoutExtension(imagePath) + "_projected.tif");

			if (Path.GetFileName(imagePath)
			        .StartsWith("swissimage-dop10_", StringComparison.OrdinalIgnoreCase))
			{
				CreateKnownPredictionCrop(imagePath, outputPath);
				return outputPath;
			}

			CreateFullSizeTiffWithLv95Projection(imagePath, outputPath);
			return outputPath;
		}

		private static void CreateKnownPredictionCrop(string imagePath, string outputPath)
		{
			IRasterDataset rasterDataset = null;
			try
			{
				rasterDataset = DatasetUtils.OpenRasterDataset(imagePath);

				IEnvelope extent = GeometryFactory.CreateEnvelope(
					2690040, 1263835, 2690185, 1263910,
					SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95));

				var rasterReference = new RasterDatasetReference(rasterDataset);
				rasterReference.ExportTileAsTiff(extent, outputPath);

				TestContext.WriteLine("Created prediction smoke-test TIFF: " +
				                      outputPath);
				TestContext.WriteLine("Projection: EPSG:2056");
				TestContext.WriteLine(
					$"Ground sample distance: {InriaTrainingGroundSampleDistance} m/px");
				TestContext.WriteLine(
					"Crop extent: 2690040,1263835,2690185,1263910");
			}
			finally
			{
				ComUtils.ReleaseComObject(rasterDataset);
			}
		}

		private static void CreateFullSizeTiffWithLv95Projection(
			string imagePath, string outputPath)
		{
			string directory = Path.GetDirectoryName(outputPath);

			IRasterDataset sourceRasterDataset = null;
			IDataset savedRasterDataset = null;
			IRasterWorkspace2 workspace = null;
			IRaster raster = null;
			try
			{
				sourceRasterDataset = DatasetUtils.OpenRasterDataset(imagePath);

				ISpatialReference spatialReference =
					SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

				raster = ((IRasterDataset2) sourceRasterDataset).CreateFullRaster();
				var rasterProps = (IRasterProps) raster;
				rasterProps.SpatialReference = spatialReference;

				IEnvelope extent = new EnvelopeClass();
				extent.PutCoords(
					TestImageLv95OriginX,
					TestImageLv95OriginY -
					rasterProps.Height * InriaTrainingGroundSampleDistance,
					TestImageLv95OriginX +
					rasterProps.Width * InriaTrainingGroundSampleDistance,
					TestImageLv95OriginY);
				extent.SpatialReference = spatialReference;
				rasterProps.Extent = extent;

				workspace = WorkspaceUtils.OpenRasterWorkspace(directory);
				string outputName = Path.GetFileName(outputPath);
				savedRasterDataset = ((ISaveAs2) raster).SaveAs(
					outputName, (IWorkspace) workspace, "TIFF");

				TestContext.WriteLine("Created full-size TIFF with embedded projection: " +
				                      outputPath);
				TestContext.WriteLine("Projection: EPSG:2056");
				TestContext.WriteLine(
					$"Ground sample distance: {InriaTrainingGroundSampleDistance} m/px");
			}
			finally
			{
				ComUtils.ReleaseComObject(savedRasterDataset);
				ComUtils.ReleaseComObject(workspace);
				ComUtils.ReleaseComObject(raster);
				ComUtils.ReleaseComObject(sourceRasterDataset);
			}
		}

		private static void DeleteTemporaryTiffDirectory(string path)
		{
			string directory = Path.GetDirectoryName(path);
			if (Directory.Exists(directory) &&
			    string.Equals(Path.GetFileName(Path.GetDirectoryName(directory)),
			                  "ProSuiteAreaPredictionMatchTest",
			                  StringComparison.OrdinalIgnoreCase))
			{
				Directory.Delete(directory, recursive: true);
			}
		}


	}
}
