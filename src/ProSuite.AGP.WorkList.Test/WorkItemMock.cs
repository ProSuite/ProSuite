using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Test
{
	public class WorkItemMock : WorkItem
	{
		public WorkItemMock(int rowOid, Geometry geometry = null) : this(
			WorkListTestUtils.CreateRowProxy(rowOid), WorkListTestUtils.CreateTableProxy(),
			geometry) { }

		public WorkItemMock(GdbRowIdentity rowId, GdbTableIdentity tableId,
		                    Geometry geometry = null) : base(
			tableId.Id, rowId)
		{
			Status = WorkItemStatus.Todo;

			if (geometry != null)
			{
				SetExtent(geometry.Extent);
			}
		}

		public override string ToString()
		{
			return $"OID {OID}";
		}
	}
}
