using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class MasterDatabaseWorkspaceContextLookup : IWorkspaceContextLookup
	{
		[CLSCompliant(false)]
		public IWorkspaceContext GetWorkspaceContext(IDdxDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			return ModelElementUtils.GetMasterDatabaseWorkspaceContext(dataset);
		}
	}
}
