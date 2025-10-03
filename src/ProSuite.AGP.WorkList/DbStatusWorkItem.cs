using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList;

public class DbStatusWorkItem : WorkItem
{
	protected DbStatusWorkItem(long uniqueTableId,
	                           [NotNull] Row row,
	                           WorkItemStatus status)
		: this(uniqueTableId, new GdbRowIdentity(row), status)
	{
		Status = status;
	}

	public DbStatusWorkItem(long uniqueTableId,
	                        GdbRowIdentity identity,
	                        WorkItemStatus status)
		: base(uniqueTableId, identity)
	{
		Status = status;
	}
}
