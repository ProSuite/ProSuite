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
		public WorkItemMock(int id, Geometry geometry = null) : base(
			id,
			new GdbRowIdentity(id, 42, "Homer Simpson",
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
