using ProSuite.Commons.Geom;

namespace ProSuite.Commons.GeoDb
{
	public interface IDbRow
	{
		bool HasOID { get; }

		long OID { get; }

		object GetValue(int index);

		ITableData DbTable { get; }
	}

	public interface IDbFeature : IDbRow
	{
		object Shape { get; }

		IBoundedXY Extent { get; }

		IFeatureClassData FeatureClass { get; }
	}
}
