namespace ProSuite.Commons.GeoDb
{
	/// <summary>
	/// Basic field interface that can be implemented both by DDX attributes and actual data elements.
	/// </summary>
	public interface ITableField
	{
		string Name { get; }

		FieldType FieldType { get; }

		int FieldLength { get; }
	}
}
