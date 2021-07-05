#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ProSuite.Commons.AO.Surface;
#endif
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public abstract class RasterReference
	{
		public abstract bool EqualsCore([NotNull] RasterReference rasterReference);

		public abstract int GetHashCodeCore();

		[NotNull]
		public abstract ISimpleSurface CreateSurface([NotNull] IEnvelope extent);

		[NotNull]
		public abstract IDataset Dataset { get; }

		[NotNull]
		public abstract IGeoDataset GeoDataset { get; }

		public abstract double CellSize { get; }

		/// <summary>
		/// Whether the raster should be assumed to be fully loaded into memory and therefore
		/// requires a sub-tiling to avoid out-of-memory situations.
		/// </summary>
		public virtual bool AssumeInMemory => true;

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
