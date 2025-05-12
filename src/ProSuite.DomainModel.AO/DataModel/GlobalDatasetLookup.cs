using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.AO.DataModel
{
	/// <summary>
	/// An implementation of IDatasetLookup that looks up domain model datasets
	/// registered in the data dictionary, and uses an internal cache for speed.
	/// </summary>
	public class GlobalDatasetLookup : IDatasetLookup, IDetachedState
	{
		[NotNull] private readonly Dictionary<WorkspaceElement, Dataset> _datasetIndex =
			new Dictionary<WorkspaceElement, Dataset>();

		[NotNull] private readonly Dictionary<WorkspaceElement, Association> _associationIndex =
			new Dictionary<WorkspaceElement, Association>();

		[NotNull] private readonly IDomainTransactionManager _domainTransactions;
		[NotNull] private readonly IDatasetRepository _datasetRepository;
		[NotNull] private readonly IAssociationRepository _associationRepository;
		[CanBeNull] private readonly ICurrentModelContext _currentModelContext;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Initializes a new instance of the <see cref="GlobalDatasetLookup"/> class.
		/// </summary>
		/// <param name="domainTransactions">The domain transactions.</param>
		/// <param name="datasetRepository">The dataset repository</param>
		/// <param name="associationRepository">The association repository</param>
		/// <param name="currentModelContext">The _current model context.</param>
		public GlobalDatasetLookup([NotNull] IDomainTransactionManager domainTransactions,
		                           [NotNull] IDatasetRepository datasetRepository,
		                           [NotNull] IAssociationRepository associationRepository,
		                           [CanBeNull] ICurrentModelContext currentModelContext)
		{
			Assert.ArgumentNotNull(domainTransactions, nameof(domainTransactions));
			Assert.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
			Assert.ArgumentNotNull(associationRepository, nameof(associationRepository));

			_domainTransactions = domainTransactions;
			_datasetRepository = datasetRepository;
			_associationRepository = associationRepository;
			_currentModelContext = currentModelContext;
		}

		#region IDatasetLookup Members

		public VectorDataset GetDataset(IFeature feature)
		{
			IWorkspace workspace = GetWorkspace(feature.Class);
			if (workspace == null)
			{
				return null;
			}

			return (VectorDataset) GetDataset(DatasetUtils.GetName(feature.Class),
			                                  workspace);
		}

		public VectorDataset GetDataset(IFeatureClass featureClass)
		{
			IWorkspace workspace = GetWorkspace(featureClass);
			if (workspace == null)
			{
				return null;
			}

			return (VectorDataset) GetDataset(DatasetUtils.GetName(featureClass),
			                                  workspace);
		}

		public ObjectDataset GetDataset(IObject obj)
		{
			IWorkspace workspace = GetWorkspace(obj.Class);
			if (workspace == null)
			{
				return null;
			}

			return (ObjectDataset) GetDataset(DatasetUtils.GetName(obj.Class),
			                                  workspace);
		}

		public ObjectDataset GetDataset(IObjectClass objectClass)
		{
			IWorkspace workspace = GetWorkspace(objectClass);
			if (workspace == null)
			{
				return null;
			}

			return (ObjectDataset) GetDataset(DatasetUtils.GetName(objectClass),
			                                  workspace);
		}

		public TopologyDataset GetDataset(ITopology topology)
		{
			IFeatureWorkspace featureWorkspace =
				(IFeatureWorkspace) TopologyUtils.GetWorkspace(topology);

			return (TopologyDataset) GetDataset(featureWorkspace, TopologyUtils.GetName(topology));
		}

		public Dataset GetDataset(IFeatureWorkspace workspace, string gdbDatasetName)
		{
			return GetDataset(gdbDatasetName, (IWorkspace) workspace);
		}

		public AttributedAssociation GetAttributedAssociation(IRow relationshipRow)
		{
			ITable table = relationshipRow.Table;

			return GetAssociation((IFeatureWorkspace) DatasetUtils.GetWorkspace(table),
			                      DatasetUtils.GetName(table)) as AttributedAssociation;
		}

		public Association GetAssociation(IFeatureWorkspace workspace,
		                                  string relationshipClassName)
		{
			return GetAssociation(relationshipClassName,
			                      WorkspaceUtils.GetWorkspaceName(workspace));
		}

		public Dataset GetDataset(IDatasetName datasetName)
		{
			return GetDataset(datasetName.Name, datasetName.WorkspaceName);
		}

		#endregion

		#region Implementation of IDetachedState

		public void ReattachState(IUnitOfWork unitOfWork)
		{
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			var entities = new List<Entity>();
			var models = new HashSet<DdxModel>();

			foreach (Dataset dataset in _datasetIndex.Values)
			{
				if (dataset == null)
				{
					continue;
				}

				models.Add(dataset.Model);
				entities.Add(dataset);
			}

			foreach (Association association in _associationIndex.Values)
			{
				if (association == null)
				{
					continue;
				}

				if (! models.Contains(association.Model))
				{
					models.Add(association.Model);
				}

				entities.Add(association);
			}

			foreach (DdxModel model in models)
			{
				unitOfWork.Reattach(model);
			}

			foreach (Entity entity in entities)
			{
				unitOfWork.Reattach(entity);
			}
		}

		#endregion

		public void InvalidateCache()
		{
			_datasetIndex.Clear();
			_associationIndex.Clear();
		}

		[CanBeNull]
		private static IWorkspace GetWorkspace([NotNull] IObjectClass objectClass)
		{
			var dataset = objectClass as IDataset;

			return dataset?.Workspace;
		}

		[CanBeNull]
		private Dataset GetDataset([NotNull] string fullName,
		                           [NotNull] IWorkspace workspace)
		{
			return GetDataset(fullName, WorkspaceUtils.GetWorkspaceName(workspace));
		}

		[CanBeNull]
		private Dataset GetDataset([NotNull] string fullName,
		                           [NotNull] IWorkspaceName workspaceName)
		{
			Assert.ArgumentNotNullOrEmpty(fullName, nameof(fullName));
			Assert.ArgumentNotNull(workspaceName, nameof(workspaceName));

			const bool includeDeleted = false;
			return GetModelElement(fullName, workspaceName, _datasetIndex,
			                       name => _datasetRepository.Get(name, includeDeleted),
			                       GetDatasetForCurrentContextFunction());
		}

		[CanBeNull]
		private T GetModelElement<T>(
			[NotNull] string fullName,
			[NotNull] IWorkspaceName workspaceName,
			[NotNull] IDictionary<WorkspaceElement, T> index,
			[NotNull] Func<string, IList<T>> getForName,
			[CanBeNull] Func<string, IWorkspaceName, T> getForCurrentContext)
			where T : class, IModelElement
		{
			Assert.ArgumentNotNullOrEmpty(fullName, nameof(fullName));
			Assert.ArgumentNotNull(workspaceName, nameof(workspaceName));
			Assert.ArgumentNotNull(index, nameof(index));
			Assert.ArgumentNotNull(getForName, nameof(getForName));

			var workspaceElement = new WorkspaceElement(workspaceName, fullName);

			T modelElement;
			if (index.TryGetValue(workspaceElement, out modelElement))
			{
				return modelElement;
			}

			// model element not yet cached - search for it

			IWorkspace workspace;
			try
			{
				workspace = WorkspaceUtils.OpenWorkspace(workspaceName);
			}
			catch (Exception e)
			{
				throw new InvalidOperationException(
					$"Error opening workspace name to select model element for name '{fullName}'",
					e);
			}

			var inaccessibleModels = new List<string>();

			_domainTransactions.UseTransaction(
				delegate
				{
					if (getForCurrentContext != null)
					{
						modelElement = getForCurrentContext(fullName, workspaceName);
						if (modelElement != null)
						{
							return;
						}
					}

					IList<T> candidates = getForName(fullName);
					Assert.NotNull(candidates, "no dataset list returned");

					if (! ModelElementNameUtils.IsQualifiedName(fullName))
					{
						// the gdb element name is unqualified
						// search as is (in models that allow opening from default db)
						modelElement = GetCandidateInSameDatabase(
							candidates, workspace, ModelElementNameType.Any, inaccessibleModels);
					}
					else
					{
						// the dataset name is qualified
						// search datasets in models that use qualified names (and that allow opening from default db)
						modelElement = GetCandidateInSameDatabase(
							candidates, workspace, ModelElementNameType.Qualified,
							inaccessibleModels);

						// not found: unqualify, search in models with unqualified dataset names
						if (modelElement == null)
						{
							candidates =
								getForName(ModelElementNameUtils.GetUnqualifiedName(fullName));

							modelElement = GetCandidateInSameDatabase(
								candidates, workspace, ModelElementNameType.Unqualified,
								inaccessibleModels);
						}
					}
				});

			if (modelElement == null && inaccessibleModels.Count > 0)
			{
				// only if no candidate was found: warn if any of the candidates
				// could not be verified because of an invalid workspace reference
				if (inaccessibleModels.Count == 1)
				{
					string inaccessibleModelName = inaccessibleModels.First();

					_msg.Warn(
						$"Workspace for model '{inaccessibleModelName}' containing '{fullName}' cannot be opened.");
				}
				else
				{
					_msg.Warn(
						"The master database workspaces for the following models cannot be opened:");
					using (_msg.IncrementIndentation())
					{
						foreach (string inaccessibleModelName in inaccessibleModels)
						{
							_msg.Warn($"{inaccessibleModelName}");
						}
					}
				}
			}

			// add element even if null (to not search again for unregistered element)
			index.Add(workspaceElement, modelElement);

			return modelElement;
		}

		private enum ModelElementNameType
		{
			Any,
			Qualified,
			Unqualified
		}

		[CanBeNull]
		private static T GetCandidateInSameDatabase<T>(
			[NotNull] IEnumerable<T> candidates,
			[NotNull] IWorkspace elementWorkspace,
			ModelElementNameType expectedModelElementNameType,
			[NotNull] List<string> inaccessibleModels)
			where T : class, IModelElement
		{
			Assert.ArgumentNotNull(candidates, nameof(candidates));
			Assert.ArgumentNotNull(elementWorkspace, nameof(elementWorkspace));
			Assert.ArgumentNotNull(inaccessibleModels, nameof(inaccessibleModels));

			foreach (T candidate in candidates)
			{
				DdxModel model = candidate.Model;
				if (model == null)
				{
					continue;
				}

				if (expectedModelElementNameType == ModelElementNameType.Qualified &&
				    ! model.ElementNamesAreQualified)
				{
					// ignore candidate
					continue;
				}

				if (expectedModelElementNameType == ModelElementNameType.Unqualified &&
				    model.ElementNamesAreQualified)
				{
					// ignore candidate
					continue;
				}

				IWorkspace modelWorkspace = model.GetMasterDatabaseWorkspace();

				if (modelWorkspace == null)
				{
					inaccessibleModels.Add(candidate.Model.Name);
					continue;
				}

				if (! WorkspaceUtils.IsSameDatabase(modelWorkspace, elementWorkspace))
				{
					// this model element is from another database/model
					continue;
				}

				// the candidate element is in the model's workspace - use it
				return candidate;
			}

			return null;
		}

		private Association GetAssociation([NotNull] string relClassName,
		                                   [NotNull] IWorkspaceName workspaceName)
		{
			Assert.ArgumentNotNullOrEmpty(relClassName, nameof(relClassName));
			Assert.ArgumentNotNull(workspaceName, nameof(workspaceName));

			const bool includeDeleted = false;
			return GetModelElement(relClassName, workspaceName, _associationIndex,
			                       name => _associationRepository.Get(name, includeDeleted),
			                       GetAssociationForCurrentContextFunction());
		}

		[CanBeNull]
		private Func<string, IWorkspaceName, Dataset> GetDatasetForCurrentContextFunction()
		{
			return _currentModelContext == null
				       ? (Func<string, IWorkspaceName, Dataset>) null
				       : _currentModelContext.GetDataset;
		}

		[CanBeNull]
		private Func<string, IWorkspaceName, Association>
			GetAssociationForCurrentContextFunction()
		{
			return _currentModelContext == null
				       ? (Func<string, IWorkspaceName, Association>) null
				       : _currentModelContext.GetAssociation;
		}

		private class WorkspaceElement : IEquatable<WorkspaceElement>
		{
			[NotNull] private readonly string _connectionString;
			[NotNull] private readonly string _datasetName;

			public WorkspaceElement([NotNull] IWorkspaceName workspaceName,
			                        [NotNull] string datasetName)
			{
				Assert.ArgumentNotNull(workspaceName, nameof(workspaceName));
				Assert.ArgumentNotNullOrEmpty(datasetName, nameof(datasetName));

				var workspaceName2 = workspaceName as IWorkspaceName2;
				Assert.ArgumentCondition(workspaceName2 != null,
				                         "workspace name does not implement IWorkspaceName2");

				string connectionString = workspaceName2.ConnectionString;
				Assert.ArgumentCondition(! string.IsNullOrEmpty(connectionString),
				                         "workspace name has empty connection string");

				_connectionString = connectionString;
				_datasetName = datasetName;
			}

			public bool Equals(WorkspaceElement other)
			{
				if (ReferenceEquals(null, other))
				{
					return false;
				}

				if (ReferenceEquals(this, other))
				{
					return true;
				}

				return
					_connectionString.Equals(other._connectionString,
					                         StringComparison.OrdinalIgnoreCase) &&
					_datasetName.Equals(other._datasetName, StringComparison.OrdinalIgnoreCase);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj))
				{
					return false;
				}

				if (ReferenceEquals(this, obj))
				{
					return true;
				}

				if (obj.GetType() != typeof(WorkspaceElement))
				{
					return false;
				}

				return Equals((WorkspaceElement) obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (_connectionString.GetHashCode() * 397) ^ _datasetName.GetHashCode();
				}
			}
		}
	}
}
