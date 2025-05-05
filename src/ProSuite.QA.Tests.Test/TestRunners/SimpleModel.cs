using ESRI.ArcGIS.Geodatabase;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Geodatabase;

namespace ProSuite.QA.Tests.Test.TestRunners
{
	public class SimpleModel : ProductionModel, IModelMasterDatabase
	{
		public SimpleModel(string name, IFeatureClass anyWorkspaceFeatureClass)
			: this(name, (ITable) anyWorkspaceFeatureClass) { }

		public SimpleModel(string name, ITable anyWorkspaceTable)
			: base(name)
		{
			IWorkspace ws = ((IDataset) anyWorkspaceTable).Workspace;
			UserConnectionProvider = new OpenWorkspaceConnectionProvider(ws);
		}

		public SimpleModel(string name, IWorkspace workspace)
			: base(name)
		{
			UserConnectionProvider = new OpenWorkspaceConnectionProvider(workspace);
		}

		public override string QualifyModelElementName(string modelElementName)
		{
			return ModelUtils.QualifyModelElementName(this, modelElementName);
		}

		public override string TranslateToModelElementName(string masterDatabaseDatasetName)
		{
			return ModelUtils.TranslateToModelElementName(this, masterDatabaseDatasetName);
		}

		IWorkspaceContext IModelMasterDatabase.CreateMasterDatabaseWorkspaceContext()
		{
			return ModelUtils.CreateDefaultMasterDatabaseWorkspaceContext(this);
		}
	}
}
