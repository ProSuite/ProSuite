using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Geodatabase;

public interface IRowReference
{
	bool References([NotNull] Row row);

	bool References([NotNull] Table table, long objectId);

	[CanBeNull]
	ITableReference TableReference { get; }
}
