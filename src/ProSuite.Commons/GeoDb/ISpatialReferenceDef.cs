namespace ProSuite.Commons.GeoDb
{
	public interface ISpatialReferenceDef
	{
		string Name { get; }

		string Alias { get; }

		string Abbreviation { get; }

		int FactoryCode { get; }
	}
}
