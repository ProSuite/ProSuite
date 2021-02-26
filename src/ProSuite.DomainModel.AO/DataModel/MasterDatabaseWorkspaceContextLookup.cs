using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class MasterDatabaseWorkspaceContextLookup : IWorkspaceContextLookup
	{
		public IWorkspaceContext GetWorkspaceContext(IDdxDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			return ModelElementUtils.GetMasterDatabaseWorkspaceContext(dataset);
		}
	}
}
