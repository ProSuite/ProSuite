using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Db
{
	public interface IDbRow
	{
		bool HasOID { get; }

		long OID { get; }

		object GetValue(int index);

		IDbTable DbTable { get; }
	}

	public interface IDbFeature : IDbRow
	{
		object Shape { get; }

		IBoundedXY Extent { get; }

		IFeatureClassSchema FeatureClass { get; }
	}
}
