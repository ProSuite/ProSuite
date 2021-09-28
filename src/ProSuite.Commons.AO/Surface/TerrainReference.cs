using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface
{
	public abstract class TerrainReference
	{
		[NotNull]
		public abstract IGeoDataset Dataset { get; }

		[NotNull]
		public abstract RectangularTilingStructure Tiling { get; }

		[NotNull]
		public abstract string Name { get; }

		[NotNull]
		public abstract ITin CreateTin([NotNull] IEnvelope extent, double resolution);

		public abstract bool EqualsCore([NotNull] TerrainReference terrainReference);

		public abstract int GetHashCodeCore();

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

			return Equals((TerrainReference) obj);
		}

		public override int GetHashCode()
		{
			return GetHashCodeCore();
		}

		public bool Equals(TerrainReference other)
		{
			return EqualsCore(other);
		}
	}
}
