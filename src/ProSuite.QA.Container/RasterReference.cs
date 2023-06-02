using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.CodeAnnotations;
using IDatasetContainer = ProSuite.Commons.GeoDb.IDatasetContainer;

namespace ProSuite.QA.Container
{
	public abstract class RasterReference : IRasterDatasetDef
	{
		public abstract bool EqualsCore([NotNull] RasterReference rasterReference);

		public abstract int GetHashCodeCore();

		[NotNull]
		public abstract ISimpleSurface CreateSurface([NotNull] IEnvelope extent);

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
