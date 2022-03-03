using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.DomainModel.AGP.DataModel;

namespace ProSuite.DomainModel.AGP.Workflow
{
	/// <summary>
	/// The workspace and the project with the datasets currently relevant to the user  e.g.
	/// because they are in the map. The project workspace is a concept the users need to be
	/// aware of in order to get the expected specifications and for the system to know which
	/// datasets from which version shall be verified. For more details, see the admin help.
	/// </summary>
	public class ProjectWorkspace
	{
		// TODO: Add project, dataset implementations to DomainModel
		public ProjectWorkspace(int projectId,
		                        IList<BasicDataset> datasets,
		                        Datastore workspace,
		                        SpatialReference modelSpatialReference)
		{
			ProjectId = projectId;
			Datasets = datasets;
			Workspace = workspace;
			ModelSpatialReference = modelSpatialReference;
		}

		public int ProjectId { get; }
		public IList<BasicDataset> Datasets { get; }
		public Datastore Workspace { get; }
		public SpatialReference ModelSpatialReference { get; }

		public string GetVersionName()
		{
			return WorkspaceUtils.GetCurrentVersion(Workspace)?.GetName();
		}

		public IList<int> GetDatasetIds()
		{
			return Datasets.Select(d => d.Id).ToList();
		}

		public DatasetLookup GetDatasetLookup()
		{
			return new DatasetLookup(Datasets);
		}
	}
}
