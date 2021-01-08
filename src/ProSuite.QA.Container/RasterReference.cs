using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	[CLSCompliant(false)]
	public abstract class RasterReference
	{
		public abstract bool EqualsCore([NotNull] RasterReference rasterReference);

		public abstract int GetHashCodeCore();

		[NotNull]
		public abstract IRaster CreateFullRaster();

		public abstract ISimpleSurface CreateSurface(IRaster raster);

		[NotNull]
		public abstract IDataset RasterDataset { get; }

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
