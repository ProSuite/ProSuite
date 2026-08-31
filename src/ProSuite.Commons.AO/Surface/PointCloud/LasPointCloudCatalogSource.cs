using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;

namespace ProSuite.Commons.AO.Surface.PointCloud
{
	/// <summary>
	/// An <see cref="ILasFileSource" /> backed by a point cloud catalog feature class: one polygon
	/// feature per LAS tile, with the path to that tile's LAS file in a field.
	/// <para>
	/// Structurally the LAS analogue of
	/// <see cref="Raster.SimpleRasterMosaic"/>: same catalog shape, same
	/// tolerance for rows without a file, only the file kind differs.
	/// </para>
	/// </summary>
	public class LasPointCloudCatalogSource : ILasFileSource
	{
		/// <summary>
		/// The file path field of a GoTop LAS point cloud catalog feature class. Only a default
		/// for the callers that open the catalog by name; the QA path passes the field name it
		/// derives from the dataset's <c>FilePath</c> attribute role, which on the archive's
		/// generated view is <c>CURRENT_FILE_PATH</c>, not this.
		/// </summary>
		public const string DefaultFilePathFieldName = "FILE_PATH";

		private readonly IFeatureClass _featureClass;
		private readonly int _filePathFieldIndex;

		/// <summary>
		/// Initializes a new instance of the <see cref="LasPointCloudCatalogSource"/> class over an
		/// already opened catalog feature class. This is the constructor the QA seam uses: the
		/// caller (the dataset context) decides how the catalog is opened.
		/// </summary>
		/// <param name="catalogClass">The catalog feature class, one polygon feature per tile.</param>
		/// <param name="filePathFieldName">The name of the field holding each tile's LAS file path.</param>
		public LasPointCloudCatalogSource([NotNull] IFeatureClass catalogClass,
		                                  [NotNull] string filePathFieldName)
		{
			Assert.ArgumentNotNull(catalogClass, nameof(catalogClass));
			Assert.ArgumentNotNullOrEmpty(filePathFieldName, nameof(filePathFieldName));

			_featureClass = catalogClass;

			_filePathFieldIndex = _featureClass.FindField(filePathFieldName);

			if (_filePathFieldIndex < 0)
			{
				throw new InvalidConfigurationException(
					$"LAS catalog feature class '{DatasetUtils.GetName(_featureClass)}' does not " +
					$"have a '{filePathFieldName}' field.");
			}

			FilePathFieldName = filePathFieldName;

			SpatialReference = DatasetUtils.GetSpatialReference(_featureClass);
			Extent = ((IGeoDataset) _featureClass).Extent;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LasPointCloudCatalogSource"/> class by
		/// opening the catalog feature class by name, using the
		/// <see cref="DefaultFilePathFieldName"/> field.
		/// </summary>
		public LasPointCloudCatalogSource([NotNull] string featureClassName,
		                                  [NotNull] IFeatureWorkspace workspace)
			: this(OpenFeatureClass(featureClassName, workspace), DefaultFilePathFieldName) { }

		/// <summary>The catalog feature class the tiles are read from.</summary>
		[NotNull]
		public IFeatureClass CatalogClass => _featureClass;

		/// <summary>The name of the field holding each tile's LAS file path.</summary>
		[NotNull]
		public string FilePathFieldName { get; }

		public ISpatialReference SpatialReference { get; }

		public IEnvelope Extent { get; }

		public IEnumerable<(string FilePath, IEnvelope TileExtent)> GetIntersectingFiles(
			IEnvelope searchArea)
		{
			Assert.ArgumentNotNull(searchArea, nameof(searchArea));

			IQueryFilter filter = GdbQueryUtils.CreateSpatialFilter(_featureClass, searchArea);

			return GetTiles(GdbQueryUtils.GetFeatures(_featureClass, filter, false));
		}

		public IEnumerable<(string FilePath, IEnvelope TileExtent)> GetAllFiles()
		{
			return GetTiles(GdbQueryUtils.GetFeatures(_featureClass, false));
		}

		/// <summary>
		/// Yields the tiles that actually have a file and a geometry. A catalog row without a file
		/// is a legitimate state - the archive lists every registered tile, delivered or not - and
		/// is skipped rather than reported here.
		/// </summary>
		[NotNull]
		private IEnumerable<(string FilePath, IEnvelope TileExtent)> GetTiles(
			[NotNull] IEnumerable<IFeature> features)
		{
			foreach (IFeature feature in features)
			{
				var filePath = feature.Value[_filePathFieldIndex] as string;

				if (string.IsNullOrEmpty(filePath))
				{
					continue;
				}

				IEnvelope tileExtent = feature.Shape?.Envelope;

				if (tileExtent == null || tileExtent.IsEmpty)
				{
					continue;
				}

				yield return (filePath, tileExtent);
			}
		}

		[NotNull]
		private static IFeatureClass OpenFeatureClass([NotNull] string featureClassName,
		                                              [NotNull] IFeatureWorkspace workspace)
		{
			Assert.ArgumentNotNullOrEmpty(featureClassName, nameof(featureClassName));
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			try
			{
				return workspace.OpenFeatureClass(featureClassName);
			}
			catch (Exception ex)
			{
				throw new InvalidConfigurationException(
					$"Cannot open LAS catalog feature class '{featureClassName}'", ex);
			}
		}
	}
}
