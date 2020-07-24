//using System;
//using ArcGIS.Core.Data;
//using ProSuite.AGP.WorkList.Contracts;
//using ProSuite.DomainModel.DataModel;

//namespace ProSuite.AGP.WorkList.Service
//{
//	[CLSCompliant(false)]
//	public class ErrorItemRepository : GdbWorkItemRepository
//	{
//		public ErrorItemRepository(IWorkspaceContext workspaceContext) : base(workspaceContext) { }

//		protected override WorkItem CreateWorkItemCore(Row row)
//		{
//			return new ErrorItem(row);
//		}
//	}
//}
