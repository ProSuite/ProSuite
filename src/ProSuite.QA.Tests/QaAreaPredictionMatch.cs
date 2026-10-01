using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using IOPath = System.IO.Path;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaAreaPredictionMatch : ContainerTest
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();
		private const double _defaultMinimumArea = 6.0;
		private const double _defaultMinimumOverlapRatio = 0.9;
		private const int _requestTimeoutSeconds = 10000;
		private const double _requestTileSize = 1000.0;
		private const double _requestBorder = 100.0;
		private const string _defaultPredictionShapefilePath = "";
		private const AreaPredictionMatchMode _defaultMatchMode =
			AreaPredictionMatchMode.NewPredictions;

		private readonly RasterDatasetReference _imageRaster;
		private readonly string _apiUrl;
		private readonly string _modelName;
		private readonly IAreaPredictionClient _predictionClient;
		private readonly IRasterTileExporter _rasterTileExporter;

		private IFeatureClassFilter _featureFilter;
		private QueryFilterHelper _featureFilterHelper;
		private string _predictionShapefilePath = _defaultPredictionShapefilePath;
		private bool _predictionShapefileInitialized;
		private readonly List<AreaPredictionJobResult> _predictionJobResults =
			new List<AreaPredictionJobResult>();
		private readonly List<AreaPredictionReconciliationStatistics>
			_reconciliationStatistics =
				new List<AreaPredictionReconciliationStatistics>();
		private IEnvelope _completeRasterExtent;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string PotentialUnmatchedPrediction = "PotentialUnmatchedPrediction";
			public const string PotentialMissingPrediction = "PotentialMissingPrediction";
			public const string PredictionJobFailed = "PredictionJobFailed";

			public Code() : base("AreaPredictionMatch") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaAreaPredictionMatch_0))]
		public QaAreaPredictionMatch(
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_imageRaster))] [NotNull]
			RasterDatasetReference imageRaster,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_apiUrl))] [NotNull]
			string apiUrl,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_modelName))] [NotNull]
			string modelName)
			: this(featureClass, imageRaster, apiUrl, modelName,
			       _defaultMinimumArea,
			       _defaultMinimumOverlapRatio) { }

		[Doc(nameof(DocStrings.QaAreaPredictionMatch_0))]
		public QaAreaPredictionMatch(
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_imageRaster))] [NotNull]
			RasterDatasetReference imageRaster,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_apiUrl))] [NotNull]
			string apiUrl,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_modelName))] [NotNull]
			string modelName,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_minimumArea))]
			double minimumArea,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_minimumOverlapRatio))]
			double minimumOverlapRatio)
			: this(featureClass, imageRaster, apiUrl, modelName,
			       minimumArea, minimumOverlapRatio, new AreaPredictionClient(),
			       new RasterDatasetTileExporter()) { }

		[Doc(nameof(DocStrings.QaAreaPredictionMatch_0))]
		public QaAreaPredictionMatch(
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_imageMosaic))] [NotNull]
			MosaicRasterReference imageMosaic,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_apiUrl))] [NotNull]
			string apiUrl,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_modelName))] [NotNull]
			string modelName)
			: this(featureClass, imageMosaic, apiUrl, modelName,
			       _defaultMinimumArea,
			       _defaultMinimumOverlapRatio) { }

		[Doc(nameof(DocStrings.QaAreaPredictionMatch_0))]
		public QaAreaPredictionMatch(
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_imageMosaic))] [NotNull]
			MosaicRasterReference imageMosaic,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_apiUrl))] [NotNull]
			string apiUrl,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_modelName))] [NotNull]
			string modelName,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_minimumArea))]
			double minimumArea,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_minimumOverlapRatio))]
			double minimumOverlapRatio)
			: this(featureClass, GetRasterDatasetReference(imageMosaic), apiUrl, modelName,
			       minimumArea, minimumOverlapRatio) { }

		internal QaAreaPredictionMatch(
			[NotNull] IReadOnlyFeatureClass featureClass,
			[NotNull] RasterDatasetReference imageRaster,
			[NotNull] string apiUrl,
			[NotNull] string modelName,
			double minimumArea,
			double minimumOverlapRatio,
			[NotNull] IAreaPredictionClient predictionClient,
			[NotNull] IRasterTileExporter rasterTileExporter)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentNotNull(imageRaster, nameof(imageRaster));
			Assert.ArgumentNotNullOrEmpty(apiUrl, nameof(apiUrl));
			Assert.ArgumentNotNullOrEmpty(modelName, nameof(modelName));
			Assert.ArgumentCondition(minimumArea >= 0,
			                         "Minimum area must be greater than or equal to zero");
			Assert.ArgumentCondition(minimumOverlapRatio >= 0 &&
			                         minimumOverlapRatio <= 1,
			                         "Minimum overlap ratio must be between 0 and 1");
			Assert.ArgumentNotNull(predictionClient, nameof(predictionClient));
			Assert.ArgumentNotNull(rasterTileExporter, nameof(rasterTileExporter));

			_imageRaster = imageRaster;
			_apiUrl = apiUrl;
			_modelName = modelName;
			_predictionClient = predictionClient;
			_rasterTileExporter = rasterTileExporter;

			MinimumArea = minimumArea;
			MinimumOverlapRatio = minimumOverlapRatio;
			InvolvedRasters = new List<RasterReference> { imageRaster };
		}

		/// <summary>
		/// Constructor using Definition. Must always be the last constructor!
		/// </summary>
		[InternallyUsedTest]
		[Doc(nameof(DocStrings.QaAreaPredictionMatch_0))]
		public QaAreaPredictionMatch([NotNull] QaAreaPredictionMatchDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass,
			       GetRasterDatasetReference((RasterReference) definition.ImageRaster),
			       definition.ApiUrl,
			       definition.ModelName,
			       definition.MinimumArea,
			       definition.MinimumOverlapRatio)
		{
			MatchMode = definition.MatchMode;
		}

		[NotNull]
		private static RasterDatasetReference GetRasterDatasetReference(
			[NotNull] RasterReference rasterReference)
		{
			if (rasterReference is RasterDatasetReference rasterDatasetReference)
			{
				return rasterDatasetReference;
			}

			if (rasterReference is MosaicRasterReference)
			{
				IRasterDataset rasterDataset = DatasetUtils.OpenRasterDataset(
					rasterReference.Dataset.Workspace, rasterReference.Name);
				return new RasterDatasetReference(rasterDataset);
			}

			throw new ArgumentException(
				$"Unsupported image raster type {rasterReference.GetType().Name}",
				nameof(rasterReference));
		}

		public double MinimumArea { get; }

		public double MinimumOverlapRatio { get; }

		[Doc(nameof(DocStrings.QaAreaPredictionMatch_PredictionShapefilePath))]
		[TestParameter(_defaultPredictionShapefilePath)]
		public string PredictionShapefilePath
		{
			get => _predictionShapefilePath;
			set => _predictionShapefilePath = value ?? _defaultPredictionShapefilePath;
		}

		[Doc(nameof(DocStrings.QaAreaPredictionMatch_MatchMode))]
		[TestParameter(_defaultMatchMode)]
		public AreaPredictionMatchMode MatchMode { get; set; } = _defaultMatchMode;

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			return NoError;
		}

		protected override int ExecuteCore(ISurfaceRow surfaceRow, int rasterIndex)
		{
			if (rasterIndex >= InvolvedRasters.Count ||
			    ! Equals(InvolvedRasters[rasterIndex], _imageRaster))
			{
				return NoError;
			}

			if (_completeRasterExtent == null)
			{
				_completeRasterExtent = GeometryFactory.Clone(surfaceRow.Extent);
			}
			else
			{
				_completeRasterExtent.Union(surfaceRow.Extent);
			}

			return NoError;
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			if (args.State == TileState.Initial)
			{
				_predictionJobResults.Clear();
				_reconciliationStatistics.Clear();
				ComUtils.ReleaseComObject(_completeRasterExtent);
				_completeRasterExtent = null;
				_predictionShapefileInitialized = false;
			}
			else if (args.State == TileState.Final)
			{
				var errorCount = 0;
				try
				{
					if (_completeRasterExtent != null)
					{
						errorCount = ProcessRasterExtent(_completeRasterExtent);
					}
				}
				finally
				{
					ComUtils.ReleaseComObject(_completeRasterExtent);
					_completeRasterExtent = null;
				}

				if (_predictionJobResults.Count > 0)
				{
					string summary = FormatPredictionJobSummary(_predictionJobResults) +
					                 FormatReconciliationSummary(_reconciliationStatistics);
					if (HasFailedJobs(_predictionJobResults))
					{
						_msg.Warn(summary);
					}
					else
					{
						_msg.Info(summary);
					}
				}

				_predictionJobResults.Clear();
				_reconciliationStatistics.Clear();
				return errorCount + base.CompleteTileCore(args);
			}

			return base.CompleteTileCore(args);
		}

		private int ProcessRasterExtent([NotNull] IEnvelope completeExtent)
		{
			ISpatialReference rasterSpatialReference =
				_imageRaster.GeoDataset.SpatialReference;
			if (rasterSpatialReference != null)
			{
				completeExtent.SpatialReference = rasterSpatialReference;
			}

			if (_featureFilter == null)
			{
				InitFeatureFilter();
			}

			string requestDirectory = CreateTemporaryTiffDirectory();
			IList<PredictionRequestTile> requestTiles = null;
			try
			{
				requestTiles = ExportRequestTiles(
					completeExtent, requestDirectory);
				_msg.InfoFormat(
					"Exported {0} TIFF request tile(s) from combined raster extent {1:N2}," +
					"{2:N2},{3:N2},{4:N2}; submitting one Prediction service job per TIFF",
					requestTiles.Count, completeExtent.XMin, completeExtent.YMin,
					completeExtent.XMax, completeExtent.YMax);
				IList<AreaPredictionJobResult> jobResults = GetPredictions(requestTiles);
				_predictionJobResults.AddRange(jobResults);

				var errorCount = 0;
				for (var i = 0; i < requestTiles.Count; i++)
				{
					PredictionRequestTile requestTile = requestTiles[i];
					AreaPredictionJobResult jobResult = jobResults[i];
					if (! jobResult.Succeeded)
					{
						errorCount += ReportPredictionJobFailure(
							jobResult, requestTile.CoreExtent);
					}
				}

				var reconciler = new AreaPredictionReconciler();
				AreaPredictionReconciliationResult reconciliationResult =
					reconciler.Reconcile(requestTiles, jobResults, completeExtent);
				_reconciliationStatistics.Add(reconciliationResult.Statistics);

				errorCount += CheckPredictions(
					reconciliationResult.Predictions, requestTiles, jobResults,
					requestTiles[0].TileInfo.SpatialReference);
				return errorCount;
			}
			finally
			{
				ReleaseRequestTiles(requestTiles);
				DeleteTemporaryTiffDirectory(requestDirectory);
			}
		}

		private int ReportPredictionJobFailure(
			[NotNull] AreaPredictionJobResult jobResult,
			[NotNull] IEnvelope errorExtent)
		{
			string description =
				$"Prediction service prediction failed for {IOPath.GetFileName(jobResult.ImagePath)}" +
				$" (job {jobResult.JobId ?? "not created"}): " +
				(jobResult.Error ?? "unknown error");
			return ReportError(
				description, new InvolvedRows(),
				GeometryFactory.CreatePolygon(errorExtent),
				Codes[Code.PredictionJobFailed], null);
		}

		private void InitFeatureFilter()
		{
			IList<IFeatureClassFilter> filters;
			IList<QueryFilterHelper> helpers;
			CopyFilters(out filters, out helpers);

			_featureFilter = filters[0];
			_featureFilterHelper = helpers[0];
			_featureFilterHelper.FullGeometrySearch = true;
			_featureFilter.SpatialRelationship =
				esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
		}

		private int CheckPredictions([NotNull] IEnumerable<IPolygon> predictions,
		                             [NotNull] IList<PredictionRequestTile> requestTiles,
		                             [NotNull] IList<AreaPredictionJobResult> jobResults,
		                             [CanBeNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentCondition(requestTiles.Count == jobResults.Count,
			                         "Request tile and job result counts differ");
			var errorCount = 0;
			var predictionResults = new List<PredictionResult>();
			IList<IPolygon> predictionPolygons = predictions.ToList();
			try
			{
				foreach (IPolygon polygon in predictionPolygons)
				{
					if (polygon.IsEmpty)
					{
						continue;
					}

					// Every accepted prediction belongs to exactly one non-overlapping core.
					// This also drops predictions centred outside the processed raster extent.
					if (GetOwnerCoreIndex(polygon, requestTiles) < 0)
					{
						continue;
					}

					double area = GeometryProperties.GetArea(polygon);
					if (area < MinimumArea)
					{
						continue;
					}

					bool coveredByExistingFeature = IsCoveredByExistingFeature(polygon, area);
					predictionResults.Add(
						new PredictionResult(polygon, area, ! coveredByExistingFeature));

					if (coveredByExistingFeature ||
					    ! ReportsNewPredictions(MatchMode))
					{
						continue;
					}

					string description =
						$"Potential unmatched area prediction detected by {_modelName}; prediction area: {area:N2}";

					errorCount += ReportError(
						description, new InvolvedRows(), GeometryFactory.Clone(polygon),
						Codes[Code.PotentialUnmatchedPrediction], null);
				}

				if (ReportsMissingPredictions(MatchMode))
				{
					for (var tileIndex = 0; tileIndex < requestTiles.Count; tileIndex++)
					{
						if (! jobResults[tileIndex].Succeeded)
						{
							continue;
						}

						errorCount += CheckExistingFeaturesCoveredByPredictions(
							predictionResults, requestTiles, tileIndex);
					}
				}

				ExportPredictions(predictionResults, spatialReference);

				return errorCount;
			}
			finally
			{
				foreach (IPolygon polygon in predictionPolygons)
				{
					ComUtils.ReleaseComObject(polygon);
				}
			}
		}

		private int CheckExistingFeaturesCoveredByPredictions(
			[NotNull] IList<PredictionResult> predictionResults,
			[NotNull] IList<PredictionRequestTile> requestTiles,
			int tileIndex)
		{
			var errorCount = 0;
			IEnvelope tileExtent = requestTiles[tileIndex].CoreExtent;

			SetFeatureFilterGeometry(tileExtent);

			foreach (IReadOnlyRow row in Search(InvolvedTables[0],
			                                    _featureFilter,
			                                    _featureFilterHelper))
			{
				var feature = (IReadOnlyFeature) row;
				IGeometry featureShape = feature.Shape;
				if (featureShape == null || featureShape.IsEmpty)
				{
					continue;
				}

				if (GetOwnerCoreIndex(featureShape, requestTiles) != tileIndex)
				{
					continue;
				}

				double featureArea = GeometryProperties.GetArea(featureShape);
				if (featureArea < MinimumArea)
				{
					continue;
				}

				if (IsCoveredByPredictions(featureShape, featureArea, predictionResults))
				{
					continue;
				}

				string description =
					$"Potential database area not detected by {_modelName}; feature area: {featureArea:N2}";

				errorCount += ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(feature),
					GeometryFactory.Clone(featureShape),
					Codes[Code.PotentialMissingPrediction], null);
			}

			return errorCount;
		}

		internal static int GetOwnerCoreIndex(
			[NotNull] IGeometry geometry,
			[NotNull] IList<PredictionRequestTile> requestTiles)
		{
			IEnvelope envelope = geometry.Envelope;
			IPoint ownershipPoint = geometry is IArea area
				                        ? area.LabelPoint
				                        : GeometryFactory.CreatePoint(
					                        (envelope.XMin + envelope.XMax) / 2,
					                        (envelope.YMin + envelope.YMax) / 2);
			try
			{
				for (var index = 0; index < requestTiles.Count; index++)
				{
					IEnvelope core = requestTiles[index].CoreExtent;
					if (ownershipPoint.X >= core.XMin && ownershipPoint.X <= core.XMax &&
					    ownershipPoint.Y >= core.YMin && ownershipPoint.Y <= core.YMax)
					{
						// Iteration order breaks the tie deterministically for an interior
						// point located exactly on a shared core boundary.
						return index;
					}
				}

				return -1;
			}
			finally
			{
				ComUtils.ReleaseComObject(ownershipPoint);
				ComUtils.ReleaseComObject(envelope);
			}
		}

		private bool IsCoveredByPredictions(
			[NotNull] IGeometry featureShape,
			double featureArea,
			[NotNull] IEnumerable<PredictionResult> predictionResults)
		{
			return AreaPredictionCoverage.IsCovered(
				featureShape, featureArea,
				predictionResults.Select(prediction => (IGeometry) prediction.Polygon),
				MinimumOverlapRatio);
		}

		private static bool ReportsNewPredictions(AreaPredictionMatchMode matchMode)
		{
			return matchMode == AreaPredictionMatchMode.NewPredictions ||
			       matchMode == AreaPredictionMatchMode.Both;
		}

		private static bool ReportsMissingPredictions(AreaPredictionMatchMode matchMode)
		{
			return matchMode == AreaPredictionMatchMode.MissingPredictions ||
			       matchMode == AreaPredictionMatchMode.Both;
		}

		private void ExportPredictions([NotNull] IList<PredictionResult> predictionResults,
		                               [CanBeNull] ISpatialReference spatialReference)
		{
			if (string.IsNullOrWhiteSpace(PredictionShapefilePath))
			{
				return;
			}

			PredictionShapefileWriter.Write(
				PredictionShapefilePath, predictionResults, spatialReference,
				! _predictionShapefileInitialized, _modelName);

			_predictionShapefileInitialized = true;
		}

		private bool IsCoveredByExistingFeature([NotNull] IPolygon predictedPolygon,
		                                        double predictionArea)
		{
			IEnvelope envelope = predictedPolygon.Envelope;
			try
			{
				SetFeatureFilterGeometry(envelope);
			}
			finally
			{
				ComUtils.ReleaseComObject(envelope);
			}
			IEnumerable<IGeometry> candidates = Search(
				InvolvedTables[0], _featureFilter, _featureFilterHelper)
				.Select(row => ((IReadOnlyFeature) row).Shape);
			return AreaPredictionCoverage.IsCovered(
				predictedPolygon, predictionArea, candidates, MinimumOverlapRatio);
		}

		private void SetFeatureFilterGeometry([NotNull] IGeometry geometry)
		{
			IGeometry previous = _featureFilter.FilterGeometry;
			_featureFilter.FilterGeometry = null;
			ComUtils.ReleaseComObject(previous);
			_featureFilter.FilterGeometry = GeometryFactory.Clone(geometry);
		}

		private TimeSpan GetRequestTimeout()
		{
			return TimeSpan.FromSeconds(_requestTimeoutSeconds);
		}

		[NotNull]
		private IList<PredictionRequestTile> ExportRequestTiles(
			[NotNull] IEnvelope extent,
			[NotNull] string outputDirectory)
		{
			Assert.ArgumentNotNull(extent, nameof(extent));
			var result = new List<PredictionRequestTile>();
			IList<AreaPredictionTileExtent> tileExtents =
				AreaPredictionTileGrid.Create(
					extent, _requestTileSize, _requestBorder);
			try
			{
				for (var index = 0; index < tileExtents.Count; index++)
				{
					AreaPredictionTileExtent tileExtent = tileExtents[index];
					string path = IOPath.Combine(
						outputDirectory, $"tile_{index:0000}.tif");
					RasterTileInfo tileInfo = _rasterTileExporter.ExportTile(
						_imageRaster, tileExtent.RequestExtent, path);
					LogUnexpectedRequestExtent(
						path, tileExtent.RequestExtent, tileInfo.Extent);
					result.Add(new PredictionRequestTile(
						path, tileExtent.CoreExtent, tileInfo));
				}

				return result;
			}
			catch
			{
				foreach (PredictionRequestTile requestTile in result)
				{
					ComUtils.ReleaseComObject(requestTile.TileInfo.Extent);
				}

				foreach (AreaPredictionTileExtent tileExtent in tileExtents)
				{
					ComUtils.ReleaseComObject(tileExtent.CoreExtent);
				}

				throw;
			}
			finally
			{
				foreach (AreaPredictionTileExtent tileExtent in tileExtents)
				{
					ComUtils.ReleaseComObject(tileExtent.RequestExtent);
				}
			}
		}

		private static void ReleaseRequestTiles(
			[CanBeNull] IEnumerable<PredictionRequestTile> requestTiles)
		{
			if (requestTiles == null)
			{
				return;
			}

			foreach (PredictionRequestTile requestTile in requestTiles)
			{
				ComUtils.ReleaseComObject(requestTile.CoreExtent);
				ComUtils.ReleaseComObject(requestTile.TileInfo.Extent);
			}
		}

		private static void LogUnexpectedRequestExtent(
			[NotNull] string path,
			[NotNull] IEnvelope requestedExtent,
			[NotNull] IEnvelope actualExtent)
		{
			double xyTolerance = actualExtent.SpatialReference == null
				                     ? 0
				                     : GeometryUtils.GetXyTolerance(actualExtent);
			double tolerance = Math.Max(xyTolerance, 0.001);
			if (Math.Abs(actualExtent.XMin - requestedExtent.XMin) > tolerance ||
			    Math.Abs(actualExtent.YMin - requestedExtent.YMin) > tolerance ||
			    Math.Abs(actualExtent.XMax - requestedExtent.XMax) > tolerance ||
			    Math.Abs(actualExtent.YMax - requestedExtent.YMax) > tolerance)
			{
				_msg.WarnFormat(
					"Exported TIFF {0} has extent {1:N3},{2:N3},{3:N3},{4:N3}; " +
					"requested {5:N3},{6:N3},{7:N3},{8:N3}",
					IOPath.GetFileName(path),
					actualExtent.XMin, actualExtent.YMin,
					actualExtent.XMax, actualExtent.YMax,
					requestedExtent.XMin, requestedExtent.YMin,
					requestedExtent.XMax, requestedExtent.YMax);
			}
		}

		[NotNull]
		private IList<AreaPredictionJobResult> GetPredictions(
			[NotNull] IList<PredictionRequestTile> requestTiles)
		{
			var paths = new List<string>(requestTiles.Count);
			foreach (PredictionRequestTile requestTile in requestTiles)
			{
				paths.Add(requestTile.ImagePath);
			}

			AreaPredictionJobStore jobStore = CreateJobStore();
			Action<AreaPredictionJobResult> jobCompleted = jobStore == null
				? (Action<AreaPredictionJobResult>) null
				: job => TryStoreResult(jobStore, requestTiles, job);

			var streamingBatchClient =
				_predictionClient as IAreaPredictionStreamingBatchClient;
			if (streamingBatchClient != null)
			{
				return streamingBatchClient.GetPredictionJobs(
					paths, _apiUrl, _modelName, GetRequestTimeout(),
					null, jobCompleted);
			}

			var batchClient = _predictionClient as IAreaPredictionBatchClient;
			if (batchClient != null)
			{
				IList<AreaPredictionJobResult> batchResults =
					batchClient.GetPredictionJobs(
						paths, _apiUrl, _modelName, GetRequestTimeout());
				if (batchResults.Count != paths.Count)
				{
					throw new InvalidOperationException(
						$"Prediction client returned {batchResults.Count} job results " +
						$"for {paths.Count} submitted TIFFs");
				}

				foreach (AreaPredictionJobResult job in batchResults)
				{
					if (jobStore != null)
					{
						if (! string.IsNullOrEmpty(job.JobId))
						{
							_msg.InfoFormat(
								"Prediction service TIFF/job mapping: file {0} -> job {1}",
								IOPath.GetFileName(job.ImagePath), job.JobId);
						}

						TryStoreResult(jobStore, requestTiles, job);
					}
				}

				return batchResults;
			}

			var result = new List<AreaPredictionJobResult>(paths.Count);
			foreach (string path in paths)
			{
				IList<AreaPrediction> predictions = _predictionClient.GetPredictions(
					path, _apiUrl, _modelName, GetRequestTimeout());
				var job = new AreaPredictionJobResult(
					path, null, predictions, null);
				result.Add(job);
				if (jobStore != null)
				{
					TryStoreResult(jobStore, requestTiles, job);
				}
			}

			return result;
		}

		[CanBeNull]
		private AreaPredictionJobStore CreateJobStore()
		{
			return string.IsNullOrWhiteSpace(PredictionShapefilePath)
				? null
				: new AreaPredictionJobStore(PredictionShapefilePath, _modelName);
		}

		private static void TryStoreResult(
			[NotNull] AreaPredictionJobStore jobStore,
			[NotNull] IEnumerable<PredictionRequestTile> requestTiles,
			[NotNull] AreaPredictionJobResult job)
		{
			try
			{
				if (! job.Succeeded)
				{
					_msg.WarnFormat(
						"Prediction service job {0} for file {1} failed: {2}",
						job.JobId ?? "<unknown>",
						IOPath.GetFileName(job.ImagePath), job.Error);
					return;
				}

				PredictionRequestTile requestTile = requestTiles.FirstOrDefault(
					tile => string.Equals(
						tile.ImagePath, job.ImagePath,
						StringComparison.OrdinalIgnoreCase));
				ISpatialReference spatialReference =
					requestTile?.TileInfo.SpatialReference;
				string shapefilePath = jobStore.StoreResult(job, spatialReference);
				if (! string.IsNullOrEmpty(shapefilePath))
				{
					_msg.InfoFormat(
						"Completed Prediction service file {0}, job {1}: {2} predictions; " +
						"raw result {3}",
						IOPath.GetFileName(job.ImagePath), job.JobId ?? "<unknown>",
						job.Predictions.Count, shapefilePath);
				}

				if (! string.IsNullOrWhiteSpace(job.Diagnostics))
				{
					_msg.InfoFormat(
						"Prediction service diagnostics for file {0}, job {1}: {2}",
						IOPath.GetFileName(job.ImagePath), job.JobId ?? "<unknown>",
						job.Diagnostics);
				}
			}
			catch (Exception e)
			{
				_msg.WarnFormat(
					"Unable to persist raw Prediction service result for file {0}, job {1}: {2}",
					IOPath.GetFileName(job.ImagePath), job.JobId ?? "<unknown>",
					e.Message);
			}
		}

		[NotNull]
		private static string FormatPredictionJobSummary(
			[NotNull] IList<AreaPredictionJobResult> jobResults)
		{
			var succeeded = 0;
			var failed = 0;
			foreach (AreaPredictionJobResult job in jobResults)
			{
				if (job.Succeeded)
				{
					succeeded++;
				}
				else
				{
					failed++;
				}
			}

			var result = new StringBuilder();
			result.AppendFormat(
				CultureInfo.InvariantCulture,
				"Prediction service inference completed: {0} TIFF job(s), {1} succeeded, {2} failed",
				jobResults.Count, succeeded, failed);

			foreach (AreaPredictionJobResult job in jobResults)
			{
				result.AppendLine();
				result.AppendFormat(
					CultureInfo.InvariantCulture,
					"  - file {0}, job {1}, status {2}, predictions {3}",
					IOPath.GetFileName(job.ImagePath), job.JobId ?? "<unknown>",
					job.Succeeded ? "done" : "failed", job.Predictions.Count);

				if (! job.Succeeded && ! string.IsNullOrEmpty(job.Error))
				{
					result.AppendLine();
					result.Append("    error: ");
					result.Append(job.Error.Replace(
						Environment.NewLine, Environment.NewLine + "    "));
				}

				if (! string.IsNullOrEmpty(job.Diagnostics))
				{
					result.AppendLine();
					result.Append("    ");
					result.Append(job.Diagnostics.Replace(
						Environment.NewLine, Environment.NewLine + "    "));
				}
			}

			return result.ToString();
		}

		[NotNull]
		private static string FormatReconciliationSummary(
			[NotNull] IEnumerable<AreaPredictionReconciliationStatistics> statistics)
		{
			var candidates = 0;
			var boundaryCandidates = 0;
			var duplicates = 0;
			var mergedGroups = 0;
			var rejectedFragments = 0;
			var finalPredictions = 0;

			foreach (AreaPredictionReconciliationStatistics item in statistics)
			{
				candidates += item.CandidateCount;
				boundaryCandidates += item.BoundaryCandidateCount;
				duplicates += item.DuplicateCount;
				mergedGroups += item.MergedFragmentGroupCount;
				rejectedFragments += item.RejectedFragmentCount;
				finalPredictions += item.FinalPredictionCount;
			}

			return string.Format(
				CultureInfo.InvariantCulture,
				"{0}Prediction reconciliation: {1} candidates, {2} boundary candidates, " +
				"{3} duplicates removed, {4} fragment groups merged, " +
				"{5} unresolved fragments rejected, {6} final predictions",
				Environment.NewLine, candidates, boundaryCandidates, duplicates,
				mergedGroups, rejectedFragments, finalPredictions);
		}

		private static bool HasFailedJobs(
			[NotNull] IEnumerable<AreaPredictionJobResult> jobResults)
		{
			foreach (AreaPredictionJobResult job in jobResults)
			{
				if (! job.Succeeded)
				{
					return true;
				}
			}

			return false;
		}

		[NotNull]
		private static string CreateTemporaryTiffDirectory()
		{
			string directory = IOPath.Combine(
				IOPath.GetTempPath(), "ProSuiteAreaPredictionMatch",
				Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);

			return directory;
		}

		private static void DeleteTemporaryTiffDirectory([NotNull] string directory)
		{
			try
			{
				if (Directory.Exists(directory) &&
				    string.Equals(
					    IOPath.GetFileName(IOPath.GetDirectoryName(directory)),
					    "ProSuiteAreaPredictionMatch", StringComparison.OrdinalIgnoreCase))
				{
					Directory.Delete(directory, recursive: true);
				}
			}
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
		}
	}
}
