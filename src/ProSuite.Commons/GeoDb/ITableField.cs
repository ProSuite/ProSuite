using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.GeoDb
{
	/// <summary>
	/// Basic field interface that can be implemented both by DDX attributes and actual data elements.
	/// </summary>
	public interface ITableField
	{
		[NotNull]
		string Name { get; }

		[CanBeNull]
		string AliasName { get; }

		FieldType FieldType { get; }

		int FieldLength { get; }
	}
}
