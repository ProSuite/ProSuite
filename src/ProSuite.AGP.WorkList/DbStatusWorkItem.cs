using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList;

public class DbStatusWorkItem : WorkItem
{
	public DbStatusWorkItem(long uniqueTableId,
	                        [NotNull] Row row,
	                        WorkItemStatus status)
		: base(uniqueTableId, row)
	{
		Status = status;
	}
}
