
using ESRI.ArcGIS.Geodatabase;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Geodatabase;

namespace ProSuite.QA.Tests.Test.TestRunners
{
	internal class SimpleModel : ProductionModel
	{
		public SimpleModel(string name, IFeatureClass anyWorkspaceFeatureClass)
			: this(name, (ITable) anyWorkspaceFeatureClass) { }
		public SimpleModel(string name, ITable anyWorkspaceTable)
			: base(name)
		{
			IWorkspace ws = ((IDataset) anyWorkspaceTable).Workspace;
			UserConnectionProvider = new OpenWorkspaceConnectionProvider(ws);
		}

		protected override IWorkspaceContext CreateMasterDatabaseWorkspaceContext()
		{
			return CreateDefaultMasterDatabaseWorkspaceContext();
		}
	}
}
