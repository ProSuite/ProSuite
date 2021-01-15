
using ESRI.ArcGIS.Geodatabase;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Geodatabase;

namespace ProSuite.QA.Tests.Test.TestRunners
{
	internal class SimpleModel : ProductionModel
	{
		public SimpleModel(string name, IWorkspace ws)
			: base(name)
		{
			UserConnectionProvider = new OpenWorkspaceConnectionProvider(ws);
		}

		protected override IWorkspaceContext CreateMasterDatabaseWorkspaceContext()
		{
			return CreateDefaultMasterDatabaseWorkspaceContext();
		}
	}
}
