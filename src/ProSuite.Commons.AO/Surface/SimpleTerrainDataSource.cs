using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface
{
	public class SimpleTerrainDataSource
	{
		public SimpleTerrainDataSource([NotNull] IFeatureClass featureClass,
		                               esriTinSurfaceType tinSurfaceType,
		                               [CanBeNull] string whereClause = null)
		{
			FeatureClass = featureClass;
			TinSurfaceType = tinSurfaceType;
			WhereClause = whereClause;
		}

		[NotNull]
		public IFeatureClass FeatureClass { get; set; }

		public esriTinSurfaceType TinSurfaceType { get; set; }

		[CanBeNull]
		public string WhereClause { get; set; }

		public override string ToString()
		{
			return $"SimpleTerrainDataSource (FeatureClass {DatasetUtils.GetName(FeatureClass)}, " +
			       $"TinSurfaceType {TinSurfaceType})";
		}

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

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((SimpleTerrainDataSource) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = FeatureClass.GetHashCode();
				hashCode = (hashCode * 397) ^ (int) TinSurfaceType;
				hashCode = (hashCode * 397) ^ (WhereClause != null ? WhereClause.GetHashCode() : 0);
				return hashCode;
			}
		}

		protected bool Equals(SimpleTerrainDataSource other)
		{
			return TinSurfaceType == other.TinSurfaceType && WhereClause == other.WhereClause &&
			       DatasetUtils.IsSameObjectClass(FeatureClass, other.FeatureClass,
			                                      ObjectClassEquality.SameTableSameVersion);
		}
	}
}
