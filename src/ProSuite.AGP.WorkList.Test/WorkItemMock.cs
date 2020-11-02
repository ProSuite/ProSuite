using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.WorkList.Test
{
	public class WorkItemMock : WorkItem
	{
		public WorkItemMock(int id, Geometry geometry = null) : base(id, geometry)
		{
			Status = WorkItemStatus.Todo;
		}

		public override string ToString()
		{
			return $"OID {OID}";
		}
	}
}
