using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.DomainModel.AGP.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AGP.Workflow
{
	/// <summary>
	/// The workspace and the project with the datasets currently relevant to the user e.g.
	/// because they are in the map. The project workspace is a concept the users need to be
	/// aware of in order to get the expected specifications and for the system to know which
	/// datasets from which version shall be verified. For more details, see the admin help.
	/// </summary>
	public class ProjectWorkspace
	{
		private DatasetLookup _datasetLookup;

		// TODO: Add project, dataset implementations to DomainModel
		public ProjectWorkspace(int projectId,
		                        string projectName,
		                        IList<IDdxDataset> datasets,
		                        Datastore datastore,
		                        SpatialReference modelSpatialReference)
		{
			ProjectId = projectId;
			ProjectName = projectName;
			Datasets = datasets;
			Datastore = datastore;
			DatastoreConnector = datastore.GetConnector();
			DatastoreConnectionString = datastore.GetConnectionString();
			ModelSpatialReference = modelSpatialReference;
			DisplayName =
				$"{ProjectName} - {WorkspaceUtils.GetDatastoreDisplayText(DatastoreConnector)}";
		}

		public int ProjectId { get; }
		public string ProjectName { get; }
		public IList<IDdxDataset> Datasets { get; }
		public Datastore Datastore { get; }
		public Connector DatastoreConnector { get; set; }
		public string DatastoreConnectionString { get; set; }
		public SpatialReference ModelSpatialReference { get; }

		public string DisplayName { get; }

		public bool IsMasterDatabaseWorkspace { get; set; }

		public bool ExcludeReadOnlyDatasetsFromProjectWorkspace { get; set; }

		public double MinimumScaleDenominator { get; set; }

		public string ToolConfigDirectory { get; set; }

		public string WorkListConfigDir { get; set; }

		public string AttributeEditorConfigDir { get; set; }

		public string GetVersionName()
		{
			return WorkspaceUtils.GetCurrentVersion(Datastore)?.GetName();
		}

		public IList<int> GetDatasetIds()
		{
			return Datasets.Select(d => d.Id).ToList();
		}

		public DatasetLookup GetDatasetLookup()
		{
			return _datasetLookup ??= new DatasetLookup(Datasets);
		}
	}
}
