using System;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList.Service
{

	public class ErrorItemRepository : GdbWorkItemRepository
	{
		public ErrorItemRepository(IWorkspaceContext workspaceContext,
		                           bool isQueryLanguageSupported = true) : base(
			workspaceContext, isQueryLanguageSupported)
		{ }

		protected override IWorkItem CreateWorkItemCore(Row row)
		{
			
			return new ErrorItem(row);
		}

		protected override DbStatusSourceClass CreateStatusSourceClass(IVectorDataset dataset, DbStatusSchema statusSchema)
		{
			return new DbStatusSourceClass(dataset, statusSchema);
		}
	}
}
