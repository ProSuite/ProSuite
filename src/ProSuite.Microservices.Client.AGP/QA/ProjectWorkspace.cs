using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;

namespace ProSuite.Microservices.Client.AGP.QA
{
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
