using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Surface.PointCloud;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using IDatasetContainer = ProSuite.Commons.GeoDb.IDatasetContainer;

namespace ProSuite.Commons.AO.Surface
{
	/// <summary>
	/// A <see cref="PointCloudReference"/> backed by a catalog feature class: one polygon feature
	/// per LAS tile, with the tile's file path in a field. This is what a DDX point cloud dataset
	/// is opened as.
	/// </summary>
	public class LasCatalogReference : PointCloudReference
	{
		[NotNull] private readonly LasPointCloudCatalogSource _catalogSource;

		public LasCatalogReference([NotNull] string name,
		                           [NotNull] LasPointCloudCatalogSource catalogSource)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNull(catalogSource, nameof(catalogSource));

			Name = name;
			_catalogSource = catalogSource;
		}

		/// <summary>The catalog feature class the tiles are read from.</summary>
		[NotNull]
		public IFeatureClass CatalogClass => _catalogSource.CatalogClass;

		public override IEnumerable<(string FilePath, IEnvelope TileExtent)> GetFiles(
			IEnvelope searchArea)
		{
			return searchArea == null
				       ? _catalogSource.GetAllFiles()
				       : _catalogSource.GetIntersectingFiles(searchArea);
		}

		public override ISpatialReference SpatialReference => _catalogSource.SpatialReference;

		public override IEnvelope Extent => _catalogSource.Extent;

		public override string Name { get; }

		public override IDatasetContainer DbContainer =>
			new GeoDbWorkspace(((IDataset) CatalogClass).Workspace);

		public override bool EqualsCore(PointCloudReference pointCloudReference)
		{
			if (! (pointCloudReference is LasCatalogReference other))
			{
				return false;
			}

			return DatasetUtils.IsSameObjectClass(CatalogClass, other.CatalogClass) &&
			       string.Equals(_catalogSource.FilePathFieldName,
			                     other._catalogSource.FilePathFieldName);
		}

		public override int GetHashCodeCore()
		{
			return DatasetUtils.GetName(CatalogClass).GetHashCode();
		}
	}
}
