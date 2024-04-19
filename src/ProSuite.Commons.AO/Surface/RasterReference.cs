using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using IDatasetContainer = ProSuite.Commons.GeoDb.IDatasetContainer;

namespace ProSuite.Commons.AO.Surface
{
	/// <summary>
	/// A raster data type hierarchy that can be used to create surfaces which implements
	/// <see cref="IRasterDatasetDef"/>. Tests / test definitions hence can use
	/// <see cref="IRasterDatasetDef"/> as parameter type to allow for the instantiation
	/// of test definitions on all platforms.
	/// </summary>
	public abstract class RasterReference : IRasterDatasetDef
	{
		public abstract bool EqualsCore([NotNull] RasterReference rasterReference);

		public abstract int GetHashCodeCore();

		[NotNull]
		public abstract ISimpleSurface CreateSurface(
			[NotNull] IEnvelope extent,
			double? defaultValueForUnassignedZs = null,
			UnassignedZValueHandling? unassignedZValueHandling = null
		);

		[NotNull]
		public abstract IReadOnlyDataset Dataset { get; }

		[NotNull]
		public abstract IReadOnlyGeoDataset GeoDataset { get; }

		public abstract double CellSize { get; }

		/// <summary>
		/// Whether the raster should be assumed to be fully loaded into memory and therefore
		/// requires a sub-tiling to avoid out-of-memory situations.
		/// </summary>
		public virtual bool AssumeInMemory => true;

		#region Implementation of IDbDataset

		public string Name => Dataset.Name;

		public IDatasetContainer DbContainer
		{
			get
			{
				IWorkspace workspace = Dataset.Workspace;
				return new GeoDbWorkspace(workspace);
			}
		}

		public abstract DatasetType DatasetType { get; }

		public bool Equals(IDatasetDef otherDataset)
		{
			if (otherDataset is RasterReference rasterDataset)
			{
				return EqualsCore(rasterDataset);
			}

			return false;
		}

		#endregion

		public double DefaultValueForUnassignedZs { get; set; } = double.NaN;
		public bool ReturnNullGeometryIfNotCompletelyCovered { get; set; } = true;

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

			RasterReference other = obj as RasterReference;

			return other != null && EqualsCore(other);
		}

		public override int GetHashCode()
		{
			return GetHashCodeCore();
		}
	}
}
