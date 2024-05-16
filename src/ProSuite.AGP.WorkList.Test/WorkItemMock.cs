using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Test
{
	public class WorkItemMock : WorkItem
	{
		const int _tableId = 42;
		public WorkItemMock(int id, Geometry geometry = null) : base(
			id,
			_tableId,
			new GdbRowIdentity(id, _tableId, "Homer Simpson",
			                   new GdbWorkspaceIdentity(
				                   new FileGeodatabaseConnectionPath(
					                   new Uri(@"C:\temp\foo.gdb", UriKind.Absolute)),
				                   @"C:\temp\foo.gdb")))
		{
			Status = WorkItemStatus.Todo;
		}

		public override string ToString()
		{
			return $"OID {OID}";
		}
	}
}
