using System.Collections.Generic;
using ArcGIS.Core.Data;

namespace ProSuite.Commons.AGP.Core.Geodatabase;

// TODO: This class is not used anywhere!
public class DatastoreComparer : IEqualityComparer<Datastore>
{
	public bool Equals(Datastore x, Datastore y)
	{
		return WorkspaceUtils.IsSameDatastore(x, y, DatastoreComparison.ReferenceEquals);
	}

	public int GetHashCode(Datastore datastore)
	{
		// NOTE: We cannot use the table handle because it is a 64-bit integer!
		// On the server side, it will be converted to a 32-bit integer which changes its value
		// -> it cannot be used to re-associate the returned feature message with the local class!

		// In theory, this could be non-unique and needs to be compared to a process-wide dictionary
		// containing this ID and the table handle...
		unchecked
		{
			return (datastore.GetConnectionString().GetHashCode() * 397) ^
			       datastore.Handle.GetHashCode();
		}
	}
}
