using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Geodatabase;

public interface ITableReference
{
	bool ReferencesTable([NotNull] Table table);

	bool ReferencesTable(long otherTableId, [CanBeNull] string otherTableName);

	IDatastoreReference DatastoreReference { get; }
}
