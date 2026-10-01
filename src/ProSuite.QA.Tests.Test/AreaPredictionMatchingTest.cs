using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Com;
using ProSuite.QA.Tests.Test.TestRunners;
using Path = System.IO.Path;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class AreaPredictionMatchingTest
	{
		[OneTimeSetUp]
		public void SetupFixture() => TestUtils.InitializeLicense();

		[OneTimeTearDown]
		public void TeardownFixture() => TestUtils.ReleaseLicense();

		[TestCase(AreaPredictionMatchMode.NewPredictions, 1)]
		[TestCase(AreaPredictionMatchMode.MissingPredictions, 1)]
		[TestCase(AreaPredictionMatchMode.Both, 2)]
		public void MatchesShapefileFeaturesAcrossContainerTilesAndReplacesOutput(
			AreaPredictionMatchMode mode, int expectedErrors)
		{
			string directory = Path.Combine(Path.GetTempPath(),
				"AreaPredictionMatchingTest", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);
			IFeatureWorkspace workspace = null;
			IRasterWorkspace2 rasterWorkspace = null;
			IRasterDataset rasterDataset = null;
			IFeatureClass features = null;
			try
			{
				ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference(
					WellKnownHorizontalCS.LV95);
				IEnvelope extent = GeometryFactory.CreateEnvelope(
					2600000, 1200000, 2600300, 1200100, spatialReference);
				workspace = WorkspaceUtils.OpenShapefileWorkspace(directory);
				features = DatasetUtils.CreateSimpleFeatureClass(workspace, "existing",
					FieldUtils.CreateFields(FieldUtils.CreateOIDField(),
						FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolygon,
							spatialReference)));
				AddFeature(features, 10, spatialReference);
				AddFeature(features, 50, spatialReference);
				AddFeature(features, 250, spatialReference);

				rasterWorkspace = WorkspaceUtils.OpenRasterWorkspace(directory);
				rasterDataset = rasterWorkspace.CreateRasterDataset(
					"image.tif", "TIFF", extent.LowerLeft, 300, 100, 1, 1, 3,
					rstPixelType.PT_UCHAR, spatialReference);
				var client = new FixedPredictionClient
				             {
					             Predictions = new List<AreaPrediction>
					                           {
						                           CreatePrediction(10),
						                           CreatePrediction(150),
						                           CreatePrediction(250)
					                           }
				             };
				string outputPath = Path.Combine(directory, "predictions.shp");
				object exporter = Activator.CreateInstance(typeof(QaAreaPredictionMatch).Assembly
					.GetType("ProSuite.QA.Tests.RasterDatasetTileExporter", true));
				var test = (QaAreaPredictionMatch) Activator.CreateInstance(
					typeof(QaAreaPredictionMatch), BindingFlags.Instance | BindingFlags.NonPublic,
					null, new object[]
					{
						ReadOnlyTableFactory.Create(features), new RasterDatasetReference(rasterDataset),
						"http://localhost", "test-model", 6.0, 0.9, client, exporter
					}, null);
				test.MatchMode = mode;
				test.PredictionShapefilePath = outputPath;
				var runner = new QaContainerTestRunner(100, test);
				Assert.AreEqual(expectedErrors, runner.Execute(extent));
				Assert.AreEqual(1, client.RequestCount);
				Assert.That(runner.Errors.All(error => error.IssueCode.ID !=
					"AreaPredictionMatch.PredictionJobFailed"), Is.True);
				if (mode != AreaPredictionMatchMode.MissingPredictions)
				{
					Assert.AreEqual(1, runner.Errors.Count(error => error.IssueCode.ID ==
						"AreaPredictionMatch.PotentialUnmatchedPrediction"));
				}
				if (mode != AreaPredictionMatchMode.NewPredictions)
				{
					Assert.AreEqual(1, runner.Errors.Count(error => error.IssueCode.ID ==
						"AreaPredictionMatch.PotentialMissingPrediction"));
				}
				AssertOutputCount(workspace, 3);

				client.Predictions = new List<AreaPrediction> { CreatePrediction(10) };
				runner.ClearErrors();
				runner.Execute(extent);
				AssertOutputCount(workspace, 1);

				client.Predictions.Clear();
				runner.ClearErrors();
				runner.Execute(extent);
				AssertOutputCount(workspace, 0);
			}
			finally
			{
				ComUtils.ReleaseComObject(features);
				ComUtils.ReleaseComObject(rasterDataset);
				ComUtils.ReleaseComObject(rasterWorkspace);
				ComUtils.ReleaseComObject(workspace);
			}
		}

		private static void AssertOutputCount(IFeatureWorkspace workspace, int expected)
		{
			IFeatureClass output = workspace.OpenFeatureClass("predictions");
			try
			{
				Assert.AreEqual(expected, output.FeatureCount(null));
			}
			finally
			{
				ComUtils.ReleaseComObject(output);
			}
		}

		private static void AddFeature(IFeatureClass features, double x,
		                               ISpatialReference spatialReference)
		{
			IFeature feature = features.CreateFeature();
			try
			{
				feature.Shape = GeometryFactory.CreatePolygon(GeometryFactory.CreateEnvelope(
					2600000 + x, 1200010, 2600010 + x, 1200020, spatialReference));
				feature.Store();
			}
			finally
			{
				ComUtils.ReleaseComObject(feature);
			}
		}

		private static AreaPrediction CreatePrediction(double x)
		{
			x += 2600000;
			return new AreaPrediction(new[]
			{
				new PredictionPoint(x, 1200010),
				new PredictionPoint(x + 10, 1200010),
				new PredictionPoint(x + 10, 1200020),
				new PredictionPoint(x, 1200020),
				new PredictionPoint(x, 1200010)
			});
		}

		private class FixedPredictionClient : IAreaPredictionClient
		{
			public IList<AreaPrediction> Predictions { get; set; }
			public int RequestCount { get; private set; }

			public IList<AreaPrediction> GetPredictions(string imagePath, string apiUrl,
			                                          string modelName, TimeSpan timeout,
			                                          double? tileOverlapRatio = null)
			{
				Assert.That(File.Exists(imagePath), Is.True);
				RequestCount++;
				return Predictions;
			}
		}
	}
}
