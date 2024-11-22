using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public class DbStatusWorkItem : WorkItem
	{
		public DbStatusWorkItem(long itemId,
		                        long uniqueTableId,
		                        [NotNull] Row row,
		                        WorkItemStatus status)
			: base(itemId, uniqueTableId, row)
		{
			Status = status;
		}
	}
}
