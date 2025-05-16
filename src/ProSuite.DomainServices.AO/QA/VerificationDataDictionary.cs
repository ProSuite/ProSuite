using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.AO.Workflow;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainServices.AO.QA
{
	public class VerificationDataDictionary<TModel> : IVerificationDataDictionary<TModel>
		where TModel : ProductionModel
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly IQualitySpecificationRepository _qualitySpecifications;
		[NotNull] private readonly IQualityConditionRepository _qualityConditions;
		[NotNull] private readonly IProjectRepository<TModel> _projects;
		[NotNull] private readonly IDatasetRepository _datasets;
		[NotNull] private readonly IAssociationRepository _associations;

		public VerificationDataDictionary(
			[NotNull] IDomainTransactionManager domainTransactions,
			[NotNull] IQualitySpecificationRepository qualitySpecifications,
			[NotNull] IQualityConditionRepository qualityConditions,
			[NotNull] IProjectRepository<TModel> projects,
			[NotNull] IDatasetRepository datasets,
			[NotNull] IAssociationRepository associations)
		{
			_qualitySpecifications = qualitySpecifications;
			_qualityConditions = qualityConditions;
			_projects = projects;
			_datasets = datasets;
			_associations = associations;
		}

		public IList<ProjectWorkspaceBase<Project<TModel>, TModel>> GetProjectWorkspaceCandidates(
			IList<IObjectClass> objectClasses)
		{
			IDictionary<IWorkspace, ICollection<IObjectClass>> classesByWorkspace =
				GetObjectClassesByWorkspace(objectClasses);

			var result = new List<ProjectWorkspaceBase<Project<TModel>, TModel>>();

			var allProjectWorkspaces = new List<ProjectWorkspace>();

			foreach (var kvp in GetProjectContentByWorkspaceTx(
				         classesByWorkspace))
			{
				allProjectWorkspaces.AddRange(kvp.Value);
			}

			foreach (ProjectWorkspace projectWorkspace in allProjectWorkspaces)
			{
				result.Add(projectWorkspace);
			}

			return result;
		}

		public QualityCondition GetQualityCondition(string conditionName)
		{
			return _qualityConditions.Get(conditionName);
		}

		public IList<QualitySpecification> GetQualitySpecifications(
			IList<int> datasetIds,
			bool includeHidden)
		{
			IList<QualitySpecification> result =
				_qualitySpecifications.Get(datasetIds, ! includeHidden);

			return result ?? new List<QualitySpecification>(0);
		}

		public QualitySpecification GetQualitySpecification(int qualitySpecificationId)
		{
			QualitySpecification result = _qualitySpecifications.Get(qualitySpecificationId);

			if (result != null)
			{
				// The parameters must be initialized!
				InstanceConfigurationUtils.InitializeParameterValues(result);
			}

			return result;
		}

		public IList<Dataset> GetDatasets(IList<int> datasetIds)
		{
			return _datasets.Get(datasetIds);
		}

		public IList<Association> GetAssociations(IList<int> referencedDatasetIds)
		{
			Assert.NotNull(_associations, "Association repository has not been set up.");
			return _associations.GetByReferencedDatasetIds(referencedDatasetIds);
		}

		[NotNull]
		private IEnumerable<KeyValuePair<IWorkspace, IList<ProjectWorkspace>>>
			GetProjectContentByWorkspaceTx(
				[NotNull] IDictionary<IWorkspace, ICollection<IObjectClass>> classesByWorkspace)
		{
			var result = new Dictionary<IWorkspace, IList<ProjectWorkspace>>();

			if (classesByWorkspace.Count == 0)
			{
				return result;
			}

			IList<Project<TModel>> projects = _projects.GetAll(true);

			_msg.DebugFormat("{0} projects exist in the data dictionary with a production model",
			                 projects.Count);

			if (projects.Count == 0)
			{
				return result;
			}

			foreach (
				KeyValuePair<IWorkspace, ICollection<IObjectClass>> pair in classesByWorkspace)
			{
				IWorkspace workspace = pair.Key;
				ICollection<IObjectClass> objectClasses = pair.Value;

				if (objectClasses.Count == 0)
				{
					continue;
				}

				IList<ProjectWorkspace> projectWorkspaces;
				if (! result.TryGetValue(workspace, out projectWorkspaces))
				{
					projectWorkspaces = new List<ProjectWorkspace>();
					result.Add(workspace, projectWorkspaces);
				}

				foreach (
					ProjectWorkspace projectWorkspace in
					GetProjectWorkspaceCandidatesTx(workspace, objectClasses, projects))
				{
					projectWorkspaces.Add(projectWorkspace);
				}
			}

			return result;
		}

		[NotNull]
		private IEnumerable<ProjectWorkspace> GetProjectWorkspaceCandidatesTx(
			[NotNull] IWorkspace workspace,
			[NotNull] IEnumerable<IObjectClass> objectClasses,
			[NotNull] ICollection<Project<TModel>> projects)
		{
			var projectWorkspaces = new Dictionary<Project<TModel>, ProjectWorkspace>();

			// for each object class, find a matching dataset
			foreach (IObjectClass objectClass in objectClasses)
			{
				bool? isReadOnly = null;

				foreach (Project<TModel> project in projects)
				{
					if (isReadOnly == true && project.ExcludeReadOnlyDatasetsFromProjectWorkspace)
					{
						continue;
					}

					ObjectDataset objectDataset =
						ProjectUtils.GetDataset<Project<TModel>, TModel>(
							project, objectClass, dataset => dataset is IErrorDataset);

					if (objectDataset == null)
					{
						// no match found
						continue;
					}

					// TODO: This has no effect and can probably not be supported going forward and if at all should be filtered by the client
					if (project.ExcludeReadOnlyDatasetsFromProjectWorkspace)
					{
						if (isReadOnly == null)
						{
							isReadOnly = ! DatasetUtils.UserHasWriteAccess(objectClass);
						}

						if (isReadOnly == true)
						{
							continue;
						}
					}

					ProjectWorkspace projectWorkspace;
					if (! projectWorkspaces.TryGetValue(project, out projectWorkspace))
					{
						projectWorkspace = new ProjectWorkspace(project, workspace);
						projectWorkspaces.Add(project, projectWorkspace);
					}

					projectWorkspace.Add(objectDataset);
				}
			}

			return projectWorkspaces.Select(pair => pair.Value);
		}

		[NotNull]
		private static IDictionary<IWorkspace, ICollection<IObjectClass>>
			GetObjectClassesByWorkspace(
				[NotNull] IEnumerable<IObjectClass> objectClasses)
		{
			var result = new Dictionary<IWorkspace, ICollection<IObjectClass>>();

			foreach (IObjectClass objectClass in objectClasses)
			{
				if (DatasetUtils.IsQueryBasedClass(objectClass))
				{
					continue;
				}

				// Once GIS.Geodatabase interface is used:
				//IWorkspace workspace = objectClass.Workspace;
				IWorkspace workspace = ((IDataset) objectClass).Workspace;

				ICollection<IObjectClass> tables;
				if (! result.TryGetValue(workspace, out tables))
				{
					tables = new HashSet<IObjectClass>();
					result.Add(workspace, tables);
				}

				tables.Add(objectClass);
			}

			return result;
		}

		private class ProjectWorkspace : ProjectWorkspaceBase<Project<TModel>, TModel>
		{
			public ProjectWorkspace([NotNull] Project<TModel> project,
			                        [NotNull] IWorkspace workspace) :
				base(project, workspace) { }
		}
	}
}
