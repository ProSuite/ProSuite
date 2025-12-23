using ArcGIS.Core.Data;

namespace ProSuite.Commons.AGP.Gdb;

/// <remarks>
/// Be conservative in writing extension methods
/// and scope them rather tight. So here are only
/// extensions that are really missing on the API.
/// </remarks>
public static class GdbExtensions
{
	/// <summary>
	/// Get a feature given its ObjectID; return null if no such feature.
	/// </summary>
	public static Feature GetFeature(this FeatureClass featureClass, long oid)
	{
		if (featureClass is null) return null;
		var queryFilter = new QueryFilter { ObjectIDs = new[] { oid } };
		using var cursor = featureClass.Search(queryFilter);
		if (! cursor.MoveNext()) return null;
		return (Feature) cursor.Current;
	}

	/// <summary>
	/// Get a row given its ObjectID; return null if no such row.
	/// </summary>
	public static Row GetRow(this Table table, long oid)
	{
		if (table is null) return null;
		var queryFilter = new QueryFilter { ObjectIDs = new[] { oid } };
		using var cursor = table.Search(queryFilter);
		if (! cursor.MoveNext()) return null;
		return cursor.Current;
	}
}
