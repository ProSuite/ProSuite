namespace ProSuite.Commons.Db
{
	public interface ISpatialReferenceInfo
	{
		string Name { get; }

		string Alias { get; }

		string Abbreviation { get; }

		int FactoryCode { get; }
	}
}
