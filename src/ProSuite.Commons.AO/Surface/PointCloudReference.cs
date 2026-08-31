using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.Commons.AO.Surface
{
	/// <summary>
	/// A point cloud data type hierarchy that implements <see cref="IPointCloudDatasetDef"/>.
	/// Tests / test definitions hence can use <see cref="IPointCloudDatasetDef"/> as parameter type
	/// to allow for the instantiation of test definitions on all platforms.
	/// <para>
	/// The point cloud counterpart of <see cref="RasterReference"/>. What it hands a test is the
	/// LAS <em>files</em> covering a search area, not an interpolated surface: that is what the
	/// requirement asks for, and it carries no interpolation semantics.
	/// </para>
	/// </summary>
	public abstract class PointCloudReference : IPointCloudDatasetDef
	{
		/// <summary>
		/// The LAS files covering the given search area, with the extent of the tile each belongs
		/// to. Tiles without a file are not returned.
		/// </summary>
		/// <param name="searchArea">The area of interest, or null for all files.</param>
		[NotNull]
		public abstract IEnumerable<(string FilePath, IEnvelope TileExtent)> GetFiles(
			[CanBeNull] IEnvelope searchArea);

		/// <summary>The spatial reference of the tiles.</summary>
		[NotNull]
		public abstract ISpatialReference SpatialReference { get; }

		/// <summary>The union of all tiles, whether they have a file or not.</summary>
		[NotNull]
		public abstract IEnvelope Extent { get; }

		public abstract bool EqualsCore([NotNull] PointCloudReference pointCloudReference);

		public abstract int GetHashCodeCore();

		#region Implementation of IDatasetDef

		public abstract string Name { get; }

		public abstract IDatasetContainer DbContainer { get; }

		public DatasetType DatasetType => DatasetType.PointCloud;

		public bool Equals(IDatasetDef otherDataset)
		{
			return otherDataset is PointCloudReference pointCloud && EqualsCore(pointCloud);
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return EqualsCore((PointCloudReference) obj);
		}

		public override int GetHashCode()
		{
			return GetHashCodeCore();
		}

		public override string ToString()
		{
			return $"{Name} (point cloud)";
		}
	}
}
