using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
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
	/// <summary>
	/// Compares what a feature is recorded as with what the image shows: a VLM
	/// service judges each feature against a rule written in text.
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaSemanticImageMatch : ContainerTest
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const int _defaultMinimumErrorProbability = 50;
		private const string _temporaryFolderName = "ProSuiteSemanticImageMatch";

		// The service judges about 8 features at a time; a full request of 500 features
		// takes several minutes.
		private static readonly TimeSpan _requestTimeout = TimeSpan.FromHours(2);

		[NotNull] private readonly IReadOnlyFeatureClass _featureClass;
		[NotNull] private readonly RasterDatasetReference _imageRaster;
		[NotNull] private readonly string _apiUrl;
		[NotNull] private readonly IFeatureCheckClient _client;
		[NotNull] private readonly IRasterTileExporter _rasterTileExporter;
		private readonly int _epsgCode;

		private readonly Dictionary<long, IPolygon> _pendingFeatures =
			new Dictionary<long, IPolygon>();

		private readonly HashSet<long> _processedObjectIds = new HashSet<long>();

		[CanBeNull] private IList<IReadOnlyFeatureClass> _contextFeatureClasses;
		[CanBeNull] private IList<IFeatureClassFilter> _contextFilters;
		[CanBeNull] private IList<QueryFilterHelper> _contextFilterHelpers;
		[CanBeNull] private SemanticImageMatchCache _cache;
		private bool _cacheInitialized;
		private RunStatistics _statistics = new RunStatistics();

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string FeatureReported = "FeatureReported";
			public const string FeatureOutsideImage = "FeatureOutsideImage";
			public const string FeatureTooLarge = "FeatureTooLarge";
			public const string FeatureCheckFailed = "FeatureCheckFailed";
			public const string FeatureCheckJobFailed = "FeatureCheckJobFailed";

			public Code() : base("SemanticImageMatch") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaSemanticImageMatch_0))]
		public QaSemanticImageMatch(
			[Doc(nameof(DocStrings.QaSemanticImageMatch_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSemanticImageMatch_imageRaster))] [NotNull]
			RasterDatasetReference imageRaster,
			[Doc(nameof(DocStrings.QaSemanticImageMatch_apiUrl))] [NotNull]
			string apiUrl,
			[Doc(nameof(DocStrings.QaSemanticImageMatch_rule))] [NotNull]
			string rule)
			: this(featureClass, imageRaster, apiUrl, rule, new FeatureCheckClient(),
			       new RasterDatasetTileExporter()) { }

		[Doc(nameof(DocStrings.QaSemanticImageMatch_0))]
		public QaSemanticImageMatch(
			[Doc(nameof(DocStrings.QaSemanticImageMatch_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSemanticImageMatch_imageMosaic))] [NotNull]
			MosaicRasterReference imageMosaic,
			[Doc(nameof(DocStrings.QaSemanticImageMatch_apiUrl))] [NotNull]
			string apiUrl,
			[Doc(nameof(DocStrings.QaSemanticImageMatch_rule))] [NotNull]
			string rule)
			: this(featureClass,
			       RasterDatasetTileExporter.GetRasterDatasetReference(imageMosaic),
			       apiUrl, rule) { }

		internal QaSemanticImageMatch(
			[NotNull] IReadOnlyFeatureClass featureClass,
			[NotNull] RasterDatasetReference imageRaster,
			[NotNull] string apiUrl,
			[NotNull] string rule,
			[NotNull] IFeatureCheckClient client,
			[NotNull] IRasterTileExporter rasterTileExporter)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(imageRaster, nameof(imageRaster));
			Assert.ArgumentNotNullOrEmpty(apiUrl, nameof(apiUrl));
			QaSemanticImageMatchDefinition.AssertValidRule(rule);
			Assert.ArgumentCondition(
				featureClass.ShapeType == esriGeometryType.esriGeometryPolygon,
				"The feature class {0} does not contain polygons", featureClass.Name);

			ISpatialReference spatialReference = featureClass.SpatialReference;
			_epsgCode = spatialReference?.FactoryCode ?? 0;
			Assert.ArgumentCondition(
				_epsgCode > 0,
				"The coordinate system of {0} has no EPSG code; the service could not " +
				"read the coordinates", featureClass.Name);

			_featureClass = featureClass;
			_imageRaster = imageRaster;
			_apiUrl = apiUrl;
			Rule = rule;
			_client = client;
			_rasterTileExporter = rasterTileExporter;

			InvolvedRasters = new List<RasterReference> { imageRaster };
		}

		/// <summary>
		/// Constructor using Definition. Must always be the last constructor!
		/// </summary>
		[InternallyUsedTest]
		[Doc(nameof(DocStrings.QaSemanticImageMatch_0))]
		public QaSemanticImageMatch([NotNull] QaSemanticImageMatchDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass,
			       RasterDatasetTileExporter.GetRasterDatasetReference(
				       (RasterReference) definition.ImageRaster),
			       definition.ApiUrl,
			       definition.Rule)
		{
			MinimumErrorProbability = definition.MinimumErrorProbability;
			CacheDirectory = definition.CacheDirectory;

			if (definition.ContextFeatureClasses?.Count > 0)
			{
				ContextFeatureClasses = definition.ContextFeatureClasses
				                                  .Cast<IReadOnlyFeatureClass>().ToList();
			}
		}

		[NotNull]
		[MultilineText]
		public string Rule { get; }

		[Doc(nameof(DocStrings.QaSemanticImageMatch_MinimumErrorProbability))]
		[TestParameter(_defaultMinimumErrorProbability)]
		public int MinimumErrorProbability { get; set; } = _defaultMinimumErrorProbability;

		[Doc(nameof(DocStrings.QaSemanticImageMatch_CacheDirectory))]
		[TestParameter(QaSemanticImageMatchDefinition.DefaultCacheDirectory)]
		[CanBeNull]
		public string CacheDirectory { get; set; } =
			QaSemanticImageMatchDefinition.DefaultCacheDirectory;

		/// <summary>
		/// The context feature classes are involved tables 1 to n, after the checked
		/// feature class.
		/// </summary>
		[Doc(nameof(DocStrings.QaSemanticImageMatch_contextFeatureClasses))]
		[TestParameter]
		[CanBeNull]
		public IList<IReadOnlyFeatureClass> ContextFeatureClasses
		{
			get => _contextFeatureClasses;
			set
			{
				Assert.False(_contextFeatureClasses?.Count > 0,
				             "The context feature classes are already set");

				_contextFeatureClasses = value;

				if (value == null)
				{
					return;
				}

				foreach (IReadOnlyFeatureClass contextFeatureClass in value)
				{
					AddInvolvedTable(contextFeatureClass, null, false);
				}
			}
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			// Each feature is sent once, in the tile where the container delivers it first.
			return tableIndex != 0 && base.RetestRowsPerIntersectedTile(tableIndex);
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			if (tableIndex != 0 || _processedObjectIds.Contains(row.OID) ||
			    _pendingFeatures.ContainsKey(row.OID))
			{
				return NoError;
			}

			var polygon = ((IReadOnlyFeature) row).Shape as IPolygon;
			if (polygon == null || polygon.IsEmpty)
			{
				return NoError;
			}

			_pendingFeatures.Add(row.OID, GeometryFactory.Clone(polygon));
			return NoError;
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			if (args.State == TileState.Initial)
			{
				ReleasePendingFeatures();
				_processedObjectIds.Clear();
				_statistics = new RunStatistics();
				_cache = null;
				_cacheInitialized = false;
				return base.CompleteTileCore(args);
			}

			int errorCount;
			try
			{
				errorCount = ProcessPendingFeatures();
			}
			finally
			{
				ReleasePendingFeatures();
			}

			if (args.State == TileState.Final)
			{
				LogSummary();
			}

			return errorCount + base.CompleteTileCore(args);
		}

		private int ProcessPendingFeatures()
		{
			if (_pendingFeatures.Count == 0)
			{
				return NoError;
			}

			SemanticImageMatchCache cache = GetCache();

			var errorCount = 0;
			var envelopesToSend = new Dictionary<long, PlanarExtent>();
			var coordinatesByOid = new Dictionary<long, string>();

			foreach (KeyValuePair<long, IPolygon> pair in _pendingFeatures)
			{
				long oid = pair.Key;
				IPolygon polygon = pair.Value;
				_processedObjectIds.Add(oid);

				PlanarExtent envelope = ToPlanarExtent(polygon.Envelope);
				if (SemanticImageMatchGrouping.IsTooLarge(envelope))
				{
					errorCount += ReportFeature(
						oid, polygon,
						string.Format(
							CultureInfo.InvariantCulture,
							"The feature is larger than {0} map units ({1:N0} x {2:N0}) and was not checked",
							SemanticImageMatchGrouping.MaximumFeatureSize,
							envelope.Width, envelope.Height),
						Code.FeatureTooLarge);
					continue;
				}

				string coordinates = SemanticImageMatchGeoJson.GetCoordinates(polygon);
				FeatureCheckResult cachedResult;
				if (cache != null &&
				    cache.TryGet(oid, SemanticImageMatchCache.GetFingerprint(coordinates),
				                 out cachedResult))
				{
					_statistics.CachedCount++;
					errorCount += ReportResult(cachedResult, polygon);
					continue;
				}

				envelopesToSend.Add(oid, envelope);
				coordinatesByOid.Add(oid, coordinates);
			}

			if (envelopesToSend.Count > 0)
			{
				errorCount += CheckFeatures(envelopesToSend, coordinatesByOid, cache);
			}

			return errorCount;
		}

		private int CheckFeatures(
			[NotNull] IDictionary<long, PlanarExtent> envelopes,
			[NotNull] IDictionary<long, string> coordinatesByOid,
			[CanBeNull] SemanticImageMatchCache cache)
		{
			var errorCount = 0;
			string directory = CreateTemporaryDirectory();
			try
			{
				var requests = new List<FeatureCheckRequest>();
				var sentObjectIds = new List<IList<long>>();

				IList<FeatureCheckGroup> groups = SemanticImageMatchGrouping.Group(envelopes);
				for (var i = 0; i < groups.Count; i++)
				{
					FeatureCheckGroup group = groups[i];
					string imagePath = IOPath.Combine(directory, $"image_{i:0000}.tif");
					PlanarExtent exportedExtent = ExportImage(group.ImageExtent, imagePath);

					var inside = new List<long>();
					foreach (long oid in group.ObjectIds)
					{
						if (exportedExtent != null && exportedExtent.Contains(envelopes[oid]))
						{
							inside.Add(oid);
						}
						else
						{
							errorCount += ReportResult(
								new FeatureCheckResult(oid, FeatureCheckStatus.OutsideImage,
								                       null, null, null),
								_pendingFeatures[oid]);
						}
					}

					if (inside.Count == 0)
					{
						continue;
					}

					string featuresGeoJson = SemanticImageMatchGeoJson.WriteFeatureCollection(
						inside.Select(oid => new KeyValuePair<long?, string>(
							              oid, coordinatesByOid[oid])),
						_epsgCode);

					requests.Add(new FeatureCheckRequest(
						             imagePath, featuresGeoJson,
						             GetContextGeoJson(group.ImageExtent), Rule));
					sentObjectIds.Add(inside);
				}

				if (requests.Count == 0)
				{
					return errorCount;
				}

				_msg.InfoFormat("Sending {0} feature(s) of {1} in {2} request(s) to {3}",
				                sentObjectIds.Sum(ids => ids.Count), _featureClass.Name,
				                requests.Count, _apiUrl);

				IList<FeatureCheckJobResult> jobResults =
					_client.CheckFeatures(requests, _apiUrl, _requestTimeout);

				for (var i = 0; i < jobResults.Count; i++)
				{
					errorCount += ProcessJobResult(jobResults[i], sentObjectIds[i],
					                               coordinatesByOid, cache);
				}

				return errorCount;
			}
			finally
			{
				DeleteTemporaryDirectory(directory);
			}
		}

		private int ProcessJobResult([NotNull] FeatureCheckJobResult jobResult,
		                             [NotNull] IList<long> sentObjectIds,
		                             [NotNull] IDictionary<long, string> coordinatesByOid,
		                             [CanBeNull] SemanticImageMatchCache cache)
		{
			if (! jobResult.Succeeded)
			{
				_statistics.FailedJobCount++;
				string description =
					"The feature was not checked, the request to the service failed " +
					$"(job {jobResult.JobId ?? "not created"}): {jobResult.Error}";

				return sentObjectIds.Sum(oid => ReportFeature(
					                         oid, _pendingFeatures[oid], description,
					                         Code.FeatureCheckJobFailed));
			}

			_statistics.CostUsd += jobResult.CostUsd ?? 0;
			if (jobResult.Model != null)
			{
				_statistics.Model = jobResult.Model;
			}

			Dictionary<long, FeatureCheckResult> resultsByOid =
				jobResult.Results.ToDictionary(result => result.ObjectId);

			cache?.Add(sentObjectIds
			           .Where(resultsByOid.ContainsKey)
			           .Select(oid => new KeyValuePair<FeatureCheckResult, string>(
				                   resultsByOid[oid],
				                   SemanticImageMatchCache.GetFingerprint(
					                   coordinatesByOid[oid]))));

			var errorCount = 0;
			foreach (long oid in sentObjectIds)
			{
				FeatureCheckResult result;
				if (! resultsByOid.TryGetValue(oid, out result))
				{
					result = new FeatureCheckResult(oid, FeatureCheckStatus.Failed, null, null,
					                                "The service returned no result");
				}
				else if (result.Status == FeatureCheckStatus.Checked)
				{
					_statistics.SentCount++;
				}

				errorCount += ReportResult(result, _pendingFeatures[oid]);
			}

			return errorCount;
		}

		private int ReportResult([NotNull] FeatureCheckResult result,
		                         [NotNull] IPolygon polygon)
		{
			switch (result.Status)
			{
				case FeatureCheckStatus.Checked:
					int probability = result.ErrorProbability ?? 0;
					if (probability <= MinimumErrorProbability)
					{
						return NoError;
					}

					_statistics.ReportedCount++;
					return ReportFeature(
						result.ObjectId, polygon,
						$"{result.Category ?? "?"} ({probability} %): {result.Reason}",
						Code.FeatureReported, probability);

				case FeatureCheckStatus.OutsideImage:
					_statistics.OutsideImageCount++;
					return ReportFeature(
						result.ObjectId, polygon,
						"The feature does not lie fully inside the image and was not checked",
						Code.FeatureOutsideImage);

				case FeatureCheckStatus.Failed:
					_statistics.FailedCount++;
					return ReportFeature(
						result.ObjectId, polygon,
						"The feature could not be checked" +
						(result.Reason == null ? string.Empty : ": " + result.Reason),
						Code.FeatureCheckFailed);

				default:
					throw new ArgumentOutOfRangeException(nameof(result));
			}
		}

		private int ReportFeature(long oid,
		                          [NotNull] IPolygon polygon,
		                          [NotNull] string description,
		                          [NotNull] string issueCode,
		                          int? errorProbability = null)
		{
			var rows = new InvolvedRows { new InvolvedRow(_featureClass.Name, oid) };

			return ReportError(description, rows, GeometryFactory.Clone(polygon),
			                   Codes[issueCode], null,
			                   values: errorProbability.HasValue
				                           ? new object[] { errorProbability.Value }
				                           : null);
		}

		/// <returns>The extent of the written image, in the coordinate system of the
		/// features; null if nothing could be exported.</returns>
		[CanBeNull]
		private PlanarExtent ExportImage([NotNull] PlanarExtent extent,
		                                 [NotNull] string imagePath)
		{
			IEnvelope requested = CreateEnvelope(extent);
			RasterTileInfo tileInfo = null;
			try
			{
				GeometryUtils.EnsureSpatialReference(requested,
				                                     _imageRaster.GeoDataset.SpatialReference);
				tileInfo = _rasterTileExporter.ExportTile(_imageRaster, requested, imagePath);

				IEnvelope exported = GeometryFactory.Clone(tileInfo.Extent);
				try
				{
					if (exported.IsEmpty)
					{
						return null;
					}

					GeometryUtils.EnsureSpatialReference(exported,
					                                     _featureClass.SpatialReference);
					return ToPlanarExtent(exported);
				}
				finally
				{
					ComUtils.ReleaseComObject(exported);
				}
			}
			finally
			{
				ComUtils.ReleaseComObject(requested);
				if (tileInfo != null)
				{
					ComUtils.ReleaseComObject(tileInfo.Extent);
				}
			}
		}

		[CanBeNull]
		private string GetContextGeoJson([NotNull] PlanarExtent extent)
		{
			int contextClassCount = _contextFeatureClasses?.Count ?? 0;
			if (contextClassCount == 0)
			{
				return null;
			}

			if (_contextFilters == null)
			{
				InitContextFilters();
			}

			var features = new List<KeyValuePair<long?, string>>();
			IEnvelope searchEnvelope = CreateEnvelope(extent);
			try
			{
				for (var tableIndex = 1; tableIndex <= contextClassCount; tableIndex++)
				{
					IFeatureClassFilter filter = _contextFilters[tableIndex];
					filter.FilterGeometry = searchEnvelope;

					foreach (IReadOnlyRow row in Search(InvolvedTables[tableIndex], filter,
					                                    _contextFilterHelpers[tableIndex]))
					{
						var polygon = ((IReadOnlyFeature) row).Shape as IPolygon;
						if (polygon == null || polygon.IsEmpty)
						{
							continue;
						}

						IPolygon projected = GeometryFactory.Clone(polygon);
						try
						{
							GeometryUtils.EnsureSpatialReference(
								projected, _featureClass.SpatialReference);
							features.Add(new KeyValuePair<long?, string>(
								             null,
								             SemanticImageMatchGeoJson.GetCoordinates(projected)));
						}
						finally
						{
							ComUtils.ReleaseComObject(projected);
						}
					}
				}
			}
			finally
			{
				ComUtils.ReleaseComObject(searchEnvelope);
			}

			return features.Count == 0
				       ? null
				       : SemanticImageMatchGeoJson.WriteFeatureCollection(features, _epsgCode);
		}

		private void InitContextFilters()
		{
			IList<IFeatureClassFilter> filters;
			IList<QueryFilterHelper> helpers;
			CopyFilters(out filters, out helpers);

			foreach (IFeatureClassFilter filter in filters)
			{
				filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			}

			// The image of a request can reach beyond the current tile.
			foreach (QueryFilterHelper helper in helpers)
			{
				helper.FullGeometrySearch = true;
			}

			_contextFilters = filters;
			_contextFilterHelpers = helpers;
		}

		[CanBeNull]
		private SemanticImageMatchCache GetCache()
		{
			if (_cacheInitialized)
			{
				return _cache;
			}

			_cacheInitialized = true;
			string directory = GetExpandedCacheDirectory();
			if (directory == null)
			{
				return null;
			}

			string scope = string.Join("|", _featureClass.Name, _imageRaster.Name,
			                           SemanticImageMatchCache.GetFingerprint(Rule));
			try
			{
				_cache = new SemanticImageMatchCache(directory, scope);
			}
			catch (Exception e)
			{
				_msg.WarnFormat("The result cache in {0} cannot be used, all features are " +
				                "sent: {1}", directory, e.Message);
			}

			return _cache;
		}

		[CanBeNull]
		private string GetExpandedCacheDirectory()
		{
			if (string.IsNullOrWhiteSpace(CacheDirectory))
			{
				_msg.Info("No cache directory is set, all features are sent");
				return null;
			}

			return Environment.ExpandEnvironmentVariables(CacheDirectory.Trim());
		}

		private void LogSummary()
		{
			RunStatistics s = _statistics;
			_msg.InfoFormat(
				"Semantic image match of {0}: {1} feature(s) checked by the service, {2} " +
				"taken from the result cache, {3} reported, {4} outside the image, {5} " +
				"failed, {6} failed request(s). Cost {7} USD{8}.{9}",
				_featureClass.Name, s.SentCount, s.CachedCount, s.ReportedCount,
				s.OutsideImageCount, s.FailedCount, s.FailedJobCount,
				s.CostUsd.ToString("0.00", CultureInfo.InvariantCulture),
				s.Model == null ? string.Empty : $" (model {s.Model})",
				_cache == null ? string.Empty : $" Result cache: {_cache.FilePath}");
		}

		private void ReleasePendingFeatures()
		{
			foreach (IPolygon polygon in _pendingFeatures.Values)
			{
				ComUtils.ReleaseComObject(polygon);
			}

			_pendingFeatures.Clear();
		}

		[NotNull]
		private IEnvelope CreateEnvelope([NotNull] PlanarExtent extent)
		{
			IEnvelope result = new EnvelopeClass();
			result.PutCoords(extent.XMin, extent.YMin, extent.XMax, extent.YMax);
			result.SpatialReference = _featureClass.SpatialReference;
			return result;
		}

		[NotNull]
		private static PlanarExtent ToPlanarExtent([NotNull] IEnvelope envelope)
		{
			double xMin, yMin, xMax, yMax;
			envelope.QueryCoords(out xMin, out yMin, out xMax, out yMax);
			return new PlanarExtent(xMin, yMin, xMax, yMax);
		}

		[NotNull]
		private static string CreateTemporaryDirectory()
		{
			string directory = IOPath.Combine(IOPath.GetTempPath(), _temporaryFolderName,
			                                  Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);
			return directory;
		}

		private static void DeleteTemporaryDirectory([NotNull] string directory)
		{
			try
			{
				if (Directory.Exists(directory))
				{
					Directory.Delete(directory, recursive: true);
				}
			}
			catch (IOException e)
			{
				_msg.Debug($"Unable to delete {directory}: {e.Message}");
			}
			catch (UnauthorizedAccessException e)
			{
				_msg.Debug($"Unable to delete {directory}: {e.Message}");
			}
		}

		private class RunStatistics
		{
			public int SentCount { get; set; }
			public int CachedCount { get; set; }
			public int ReportedCount { get; set; }
			public int OutsideImageCount { get; set; }
			public int FailedCount { get; set; }
			public int FailedJobCount { get; set; }
			public double CostUsd { get; set; }
			public string Model { get; set; }
		}
	}
}
