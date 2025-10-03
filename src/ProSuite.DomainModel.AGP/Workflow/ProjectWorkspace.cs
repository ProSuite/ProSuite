using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.Workflow;

namespace ProSuite.DomainModel.AGP.Workflow
{
	/// <summary>
	/// The workspace and the project with the datasets currently relevant to the user e.g.
	/// because they are in the map. The project workspace is a concept the users need to be
	/// aware of in order to get the expected specifications and for the system to know which
	/// datasets from which version shall be verified. For more details, see the admin help.
	/// The project workspace, managed by a <see cref="ISessionContext"/> can largely replace
	/// the work unit concept in a simplified way, i.e. provide the datasets that are relevant
	/// and their representation in an actual workspace, together with project-specific settings.
	/// </summary>
	public class ProjectWorkspace : IProjectWorkspace
	{
		// Consider re-naminig to ProProjectWorkspace
		private static readonly IMsg _msg = Msg.ForCurrentClass();

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

		public IProjectSettings ProjectSettings { get; set; }

		public IList<IDdxDataset> Datasets { get; }
		public Datastore Datastore { get; }
		public Connector DatastoreConnector { get; set; }
		public string DatastoreConnectionString { get; set; }
		public SpatialReference ModelSpatialReference { get; }

		public string DisplayName { get; }

		public bool IsMasterDatabaseWorkspace { get; set; }

		/// <summary>
		/// Some projects could exclude read-only datasets from the project workspace determination
		/// logic.
		/// </summary>
		public bool ExcludeReadOnlyDatasetsFromProjectWorkspace { get; set; }

		public string GetVersionName()
		{
			return WorkspaceUtils.GetCurrentVersion(Datastore)?.GetName();
		}

		public VersionAccessType? GetSdeVersionAccessType()
		{
			return WorkspaceUtils.GetCurrentVersion(Datastore)?.GetAccessType();
		}

		public IList<int> GetDatasetIds()
		{
			return Datasets.Select(d => d.Id).ToList();
		}

		public IDdxDataset GetDataset(string gdbTableName)
		{
			if (Datasets.Count == 0)
			{
				return null;
			}

			DdxModel ddxModel = Datasets.Select(d => d.Model).FirstOrDefault();

			Assert.NotNull(ddxModel);

			IDdxDataset result;

			if (IsMasterDatabaseWorkspace && ddxModel.ElementNamesAreQualified)
			{
				result = Datasets.FirstOrDefault(
					d => d.Name.Equals(gdbTableName, StringComparison.InvariantCultureIgnoreCase));
			}
			else
			{
				string unqualifiedName = ModelElementNameUtils.GetUnqualifiedName(gdbTableName);

				result = Datasets.FirstOrDefault(
					d => d.Name.Equals(gdbTableName, StringComparison.InvariantCultureIgnoreCase) ||
					     d.Name.Equals(unqualifiedName,
					                   StringComparison.InvariantCultureIgnoreCase));

				if (result == null)
				{
					result = Datasets.FirstOrDefault(
						d => UnqualifiedDatasetNameEquals(d, gdbTableName) ||
						     DatasetNameEquals(d, unqualifiedName));
				}
			}

			_msg.VerboseDebug(() => $"Found project workspace dataset using " +
			                        $"table name {gdbTableName}: {result != null}");

			return result;
		}

		private static bool DatasetNameEquals<T>(T dataset, string name) where T : IDdxDataset
		{
			string datasetName = dataset.Name;

			return datasetName.Equals(name, StringComparison.InvariantCultureIgnoreCase);
		}

		private static bool UnqualifiedDatasetNameEquals<T>(T dataset, string name)
			where T : IDdxDataset
		{
			string unqualifiedName = ModelElementNameUtils.GetUnqualifiedName(dataset.Name);

			return unqualifiedName.Equals(name, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
