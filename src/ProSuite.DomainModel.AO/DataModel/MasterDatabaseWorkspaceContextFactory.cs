using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class MasterDatabaseWorkspaceContextFactory : IMasterDatabaseWorkspaceContextFactory
	{
		public IWorkspaceContext Create(Model model)
		{
			IFeatureWorkspace featureWorkspace = model.UserConnectionProvider.OpenWorkspace();

			return new MasterDatabaseWorkspaceContext(featureWorkspace, model);
		}
	}
}
