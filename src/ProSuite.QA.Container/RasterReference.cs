using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public abstract class RasterReference
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
