using System.Collections.Generic;
using ArcGIS.Core.Data;

namespace ProSuite.Commons.AGP.Core.Geodatabase;

public class TableComparer : IEqualityComparer<Table>
{
	public bool Equals(Table x, Table y)
	{
		return DatasetUtils.IsSameTable(x, y);
	}

	public int GetHashCode(Table table)
	{
		// NOTE: We cannot use the table handle because it is a 64-bit integer!
		// On the server side, it will be converted to a 32-bit integer which changes its value
		// -> it cannot be used to re-associate the returned feature message with the local class!

		// In theory, this could be non-unique and needs to be compared to a process-wide dictionary
		// containing this ID and the table handle...
		unchecked
		{
			return (table.GetID().GetHashCode() * 397) ^
			       table.GetDatastore().Handle.GetHashCode();
		}
	}
}
