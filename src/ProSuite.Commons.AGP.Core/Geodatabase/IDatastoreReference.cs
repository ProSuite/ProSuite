using ArcGIS.Core.Data;

namespace ProSuite.Commons.AGP.Core.Geodatabase;

public interface IDatastoreReference
{
	bool References(Datastore datastore);

	bool References(DatastoreName datastoreName);
}
