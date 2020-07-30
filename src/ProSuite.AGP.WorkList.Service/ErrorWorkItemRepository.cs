using System;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList.Service
{
	public class ErrorWorkItemRepository : GdbWorkItemRepository
	{
		public ErrorWorkItemRepository(IWorkspaceContext workspaceContext) :
			base(workspaceContext) { }

		protected override IWorkItem CreateWorkItemCore(Row row)
		{
			throw new NotImplementedException();
		}
	}
}
