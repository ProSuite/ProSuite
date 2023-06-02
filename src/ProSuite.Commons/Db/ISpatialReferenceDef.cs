namespace ProSuite.Commons.Db
{
	public interface ISpatialReferenceDef
	{
		string Name { get; }

		string Alias { get; }

		string Abbreviation { get; }

		int FactoryCode { get; }
	}
}
