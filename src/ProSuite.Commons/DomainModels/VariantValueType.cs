namespace ProSuite.Commons.DomainModels
{
	/// <summary>
	/// The currently supported types for variant values.
	/// </summary>
	/// <remarks>This type is used in nhibernate mapping files, 
	/// enum values must not be changed.</remarks>
	public enum VariantValueType
	{
		Null = 0,
		String = 1,
		Integer = 2,
		Double = 3,
		Boolean = 4,
		DateTime = 5
	}
}