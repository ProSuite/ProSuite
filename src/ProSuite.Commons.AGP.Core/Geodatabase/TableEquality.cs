namespace ProSuite.Commons.AGP.Core.Geodatabase;

/// <summary>
/// Definition of equality among tables.
/// </summary>
public enum TableEquality
{
	/// <summary>
	/// No two tables are considered equal.
	/// </summary>
	DontEquate,

	/// <summary>
	/// Two tables are considered equal
	/// if they represent the same database table (in any version).
	/// </summary>
	SameTableAnyVersion,

	/// <summary>
	/// Two tables are considered equal
	/// if they represent the same database table in the same version.
	/// </summary>
	SameTableSameVersion,

	/// <summary>
	/// Two tables are considered equal
	/// if they have the same name.
	/// </summary>
	SameDatasetName,

	/// <summary>
	/// Two tables are considered equal
	/// if they wrap the same geodatabase object.
	/// </summary>
	SameInstance
}
