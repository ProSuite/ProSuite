using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DomainModel.AO.Geodatabase;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class MasterDatabaseWorkspaceContextFactory : IMasterDatabaseWorkspaceContextFactory
	{
		public IWorkspaceContext Create(Model model)
		{
			Assert.ArgumentNotNull(model, nameof(model));

			IFeatureWorkspace featureWorkspace = model.UserConnectionProvider.OpenWorkspace();

			return new MasterDatabaseWorkspaceContext(featureWorkspace, model);
		}
	}
}
