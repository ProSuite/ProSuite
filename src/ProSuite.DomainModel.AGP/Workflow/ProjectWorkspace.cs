using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;

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
		                        IList<int> datasets,
		                        Datastore workspace,
		                        SpatialReference modelSpatialReference)
		{
			ProjectId = projectId;
			Datasets = datasets;
			Workspace = workspace;
			ModelSpatialReference = modelSpatialReference;
		}

		public int ProjectId { get; }
		public IList<int> Datasets { get; }
		public Datastore Workspace { get; }
		public SpatialReference ModelSpatialReference { get; }

		public string GetVersionName()
		{
			if (Workspace is Geodatabase geodatabase &&
			    geodatabase.IsVersioningSupported())
			{
				VersionManager versionManager = geodatabase.GetVersionManager();

				return versionManager.GetCurrentVersion().GetName();
			}

			return null;
		}
	}
}
