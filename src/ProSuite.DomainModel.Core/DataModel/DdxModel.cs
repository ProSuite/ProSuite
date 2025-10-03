using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class DdxModel : VersionedEntityWithMetadata, IDetachedState, INamed,
	                                 IAnnotated, IDatasetContainer, IModelHarvest
	{
		public const string EnvironmentVariableNoModelSchemaCache =
			"PROSUITE_NO_MODEL_SCHEMA_CACHE";

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private int _cloneId = -1;

		#region Persistent state

		[UsedImplicitly] private string _name;
		[UsedImplicitly] private string _description;
		[UsedImplicitly] private bool _elementNamesAreQualified;
		[UsedImplicitly] private string _defaultDatabaseName;
		[UsedImplicitly] private string _defaultDatabaseSchemaOwner;
		[UsedImplicitly] private string _schemaOwner;
		[UsedImplicitly] private string _datasetPrefix;

		[UsedImplicitly]
		private SqlCaseSensitivity _sqlCaseSensitivity = SqlCaseSensitivity.SameAsDatabase;

		[UsedImplicitly] private bool _useDefaultDatabaseOnlyForSchema;

		[UsedImplicitly] private readonly IList<Dataset> _datasets = new List<Dataset>();

		[UsedImplicitly]
		private readonly IList<Association> _associations = new List<Association>();

		[UsedImplicitly] private SpatialReferenceDescriptor _spatialReferenceDescriptor;

		[UsedImplicitly] private ConnectionProvider _userConnectionProvider;

		[UsedImplicitly] private SdeDirectConnectionProvider
			_repositoryOwnerConnectionProvider;

		[UsedImplicitly] private ConnectionProvider _schemaOwnerConnectionProvider;

		[UsedImplicitly] private double _defaultMinimumSegmentLength = 2;

		[UsedImplicitly] private bool _harvestQualifiedElementNames;
		[UsedImplicitly] private bool _updateAliasNamesOnHarvest = true;
		[UsedImplicitly] private DateTime? _lastHarvestedDate;
		[UsedImplicitly] private string _lastHarvestedByUser;
		[UsedImplicitly] private string _lastHarvestedConnectionString;

		[UsedImplicitly] private bool _ignoreUnversionedDatasets;
		[UsedImplicitly] private bool _ignoreUnregisteredTables;

		[UsedImplicitly] private string _datasetInclusionCriteria;
		[UsedImplicitly] private string _datasetExclusionCriteria;

		[UsedImplicitly] private ClassDescriptor _attributeConfiguratorFactoryClassDescriptor;
		[UsedImplicitly] private ClassDescriptor _datasetListBuilderFactoryClassDescriptor;

		#endregion

		private bool _keepDatasetLocks;

		private bool _specialDatasetsAssigned;

		private Dictionary<SimpleTerrainDataset, SimpleTerrainDataset> _terrainDatasets;

		[NotNull] private readonly Dictionary<string, Dataset> _datasetIndex =
			new Dictionary<string, Dataset>(100, StringComparer.OrdinalIgnoreCase);

		[NotNull] private readonly Dictionary<string, Association> _associationIndex =
			new Dictionary<string, Association>(100, StringComparer.OrdinalIgnoreCase);

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DdxModel"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected DdxModel() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="DdxModel"/> class.
		/// </summary>
		/// <param name="name">The name of the model.</param>
		protected DdxModel(string name)
		{
			_name = name;
		}

		#endregion

		#region NHibernated properties

		[Required]
		[UsedImplicitly]
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		[UsedImplicitly]
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		[UsedImplicitly]
		public bool ElementNamesAreQualified
		{
			get { return _elementNamesAreQualified; }
			set { _elementNamesAreQualified = value; }
		}

		/// <summary>
		/// Gets the name of the master database (catalog), as used by sql server and postgresql.
		/// </summary>
		/// <value>
		/// The name of the master database (catalog).
		/// </value>
		/// <remarks>This is set during harvesting. 
		/// If the database name is not unique for the harvested datasets, the field is NULL. 
		/// For ArcSDE databases, this may only be the case if qualified dataset names are stored. 
		/// When harvesting UNqualified dataset names, it is guaranteed that
		/// they all are from the same schema and, where applicable, database</remarks>
		[CanBeNull]
		[UsedImplicitly]
		public string DefaultDatabaseName
		{
			get { return _defaultDatabaseName; }
			set { _defaultDatabaseName = value; }
		}

		/// <summary>
		/// Gets the name of the master database owner (for ArcSDE master databases), if it is unique for the harvested datasets.
		/// </summary>
		/// <value>
		/// The owner name for the datasets in the master database. 
		/// </value>
		/// <remarks>
		/// If the datasets were harvested from multiple schemas, this value will be NULL. For ArcSDE
		/// databases, this may only be the case if qualified dataset names are stored. 
		/// When harvesting UNqualified dataset names, it is guaranteed that
		/// they all are from the same schema (and if applicable, database)
		/// </remarks>
		[CanBeNull]
		[UsedImplicitly]
		public string DefaultDatabaseSchemaOwner
		{
			get { return _defaultDatabaseSchemaOwner; }
			set { _defaultDatabaseSchemaOwner = value; }
		}

		/// <summary>
		/// Gets or sets the schema owner of the model datasets, e.g. "TOPGIS_TLM"
		/// </summary>
		/// <value>The schema owner.</value>
		/// <remarks>Used for harvesting datasets.</remarks>
		public string SchemaOwner
		{
			get { return _schemaOwner; }
			set { _schemaOwner = value; }
		}

		/// <summary>
		/// Gets or sets the dataset prefix, e.g. "TLM_".
		/// </summary>
		/// <value>The dataset prefix.</value>
		/// <remarks>Used for harvesting datasets, to detect model-specific 
		/// instances of standard datasets, e.g. "TLM_ERRORS_LINE", 
		/// where "ERRORS_LINE" is the standard part.</remarks>
		[UsedImplicitly]
		public string DatasetPrefix
		{
			get { return _datasetPrefix; }
			set { _datasetPrefix = value; }
		}

		public SqlCaseSensitivity SqlCaseSensitivity
		{
			get { return _sqlCaseSensitivity; }
			set { _sqlCaseSensitivity = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the master database should be used only for schema information.
		/// If <c>true</c>, the master database is never accessed as a fallback data source when a dataset is not 
		/// present in the local workspace context.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the master database should only be used for schema information; 
		/// otherwise (the database contains useful data ancd, <c>false</c>.
		/// </value>
		[UsedImplicitly]
		public bool UseDefaultDatabaseOnlyForSchema
		{
			get { return _useDefaultDatabaseOnlyForSchema; }
			set { _useDefaultDatabaseOnlyForSchema = value; }
		}

		/// <summary>
		/// Gets all datasets (including deleted datasets)
		/// </summary>
		/// <value>The list of datasets (read-only).</value>
		[NotNull]
		public IList<Dataset> Datasets => new ReadOnlyList<Dataset>(_datasets);

		/// <summary>
		/// Gets all associations registered with the model (including deleted associations)
		/// </summary>
		/// <value>The list associations (read-only).</value>
		[NotNull]
		public IList<Association> Associations => new ReadOnlyList<Association>(_associations);

		[Required]
		public SpatialReferenceDescriptor SpatialReferenceDescriptor
		{
			get { return _spatialReferenceDescriptor; }
			set { _spatialReferenceDescriptor = value; }
		}

		[Required]
		public ConnectionProvider UserConnectionProvider
		{
			get { return _userConnectionProvider; }
			set { _userConnectionProvider = value; }
		}

		public SdeDirectConnectionProvider RepositoryOwnerConnectionProvider
		{
			get { return _repositoryOwnerConnectionProvider; }
			set { _repositoryOwnerConnectionProvider = value; }
		}

		public ConnectionProvider SchemaOwnerConnectionProvider
		{
			get { return _schemaOwnerConnectionProvider; }
			set { _schemaOwnerConnectionProvider = value; }
		}

		[UsedImplicitly]
		public double DefaultMinimumSegmentLength
		{
			get { return _defaultMinimumSegmentLength; }
			set { _defaultMinimumSegmentLength = value; }
		}

		[UsedImplicitly]
		public bool HarvestQualifiedElementNames
		{
			get { return _harvestQualifiedElementNames; }
			set { _harvestQualifiedElementNames = value; }
		}

		/// <summary>
		/// Gets a value indicating whether alias names of applicable datasets are
		/// updated when harvesting. Template methods for subclasses to override.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if alias names should be updated based on the current
		///	    value in the geodatabase; otherwise, <c>false</c>.
		/// </value>
		[UsedImplicitly]
		public bool UpdateAliasNamesOnHarvest
		{
			get { return _updateAliasNamesOnHarvest; }
			set { _updateAliasNamesOnHarvest = value; }
		}

		[UsedImplicitly]
		public DateTime? LastHarvestedDate
		{
			get { return _lastHarvestedDate; }
			set { _lastHarvestedDate = value; }
		}

		[UsedImplicitly]
		public string LastHarvestedByUser
		{
			get { return _lastHarvestedByUser; }
			set { _lastHarvestedByUser = value; }
		}

		[UsedImplicitly]
		public string LastHarvestedConnectionString
		{
			get { return _lastHarvestedConnectionString; }
			set { _lastHarvestedConnectionString = value; }
		}

		[UsedImplicitly]
		public bool IgnoreUnversionedDatasets
		{
			get { return _ignoreUnversionedDatasets; }
			set
			{
				if (_ignoreUnversionedDatasets == value)
				{
					return;
				}

				_ignoreUnversionedDatasets = value;

				if (value)
				{
					// if unversioned datasets are ignored, unregistered tables are always ignored as they are unversioned
					_ignoreUnregisteredTables = true;
				}
			}
		}

		[UsedImplicitly]
		public bool IgnoreUnregisteredTables
		{
			get { return _ignoreUnregisteredTables; }
			set { _ignoreUnregisteredTables = value; }
		}

		public bool CanChangeIgnoreUnregisteredTables =>
			! (_ignoreUnversionedDatasets && _ignoreUnregisteredTables);

		[UsedImplicitly]
		public string DatasetInclusionCriteria
		{
			get { return _datasetInclusionCriteria; }
			set { _datasetInclusionCriteria = value; }
		}

		[UsedImplicitly]
		public string DatasetExclusionCriteria
		{
			get { return _datasetExclusionCriteria; }
			set { _datasetExclusionCriteria = value; }
		}

		/// <summary>
		/// Gets or sets the class descriptor for the attribute configurator. Attribute
		/// configurators must implement IAttributeConfigurator interface.
		/// </summary>
		/// <value>The attribute configurator factory class descriptor.</value>
		public ClassDescriptor AttributeConfiguratorFactoryClassDescriptor
		{
			get { return _attributeConfiguratorFactoryClassDescriptor; }
			set { _attributeConfiguratorFactoryClassDescriptor = value; }
		}

		/// <summary>
		/// Gets or sets the class descriptor for the dataset list builder. Dataset
		/// list builder factories must implement IDatasetListBuilderFactory interface.
		/// </summary>
		/// <value>The dataset list builder factory class descriptor.</value>
		public ClassDescriptor DatasetListBuilderFactoryClassDescriptor
		{
			get { return _datasetListBuilderFactoryClassDescriptor; }
			set { _datasetListBuilderFactoryClassDescriptor = value; }
		}

		#endregion

		public new int Id
		{
			get
			{
				if (base.Id < 0 && _cloneId != -1)
				{
					return _cloneId;
				}

				return base.Id;
			}
		}

		/// <summary>
		/// Whether the user is allowed to change the 'Harvest qualified element names' property.
		/// </summary>
		/// <value>
		/// <c>true</c> if the user should be allowed to change this property; otherwise, <c>false</c>.
		/// </value>
		public bool AllowUserChangingHarvestQualifiedElementNames =>
			AllowUserChangingHarvestQualifiedElementNamesCore;

		/// <summary>
		/// Whether the user is allowed to change the 'Use master database only for schema' property.
		/// </summary>
		/// <value>
		/// <c>true</c> if the user should be allowed to change this property; otherwise, <c>false</c>.
		/// </value>
		public bool AllowUserChangingUseMasterDatabaseOnlyForSchema =>
			AllowUserChangingUseMasterDatabaseOnlyForSchemaCore;

		/// <remarks>For internal use only, use GetMasterDatabaseWorkspaceContext()</remarks>
		public object CachedMasterDatabaseWorkspaceContext { get; set; }

		/// <remarks>For internal use only, use IsMasterDatabaseAccessible()</remarks>
		public bool? CachedIsMasterDatabaseAccessible { get; set; }

		/// <remarks>For internal use only, use GetMasterDatabaseNoAccessReason()</remarks>
		public string CachedMasterDatabaseNoAccessReason { get; set; }

		public bool KeepDatasetLocks
		{
			get { return _keepDatasetLocks; }
			set
			{
				if (value == _keepDatasetLocks)
				{
					return;
				}

				// Value changed. Discard current workspace proxy
				_keepDatasetLocks = value;

				//if (_masterDatabaseWorkspaceContext == null)
				if (CachedMasterDatabaseWorkspaceContext == null)
				{
					return;
				}

				//_masterDatabaseWorkspaceContext = null;
				CachedMasterDatabaseWorkspaceContext = null;

				if (_keepDatasetLocks)
				{
					return;
				}

				// make sure any locks are released immediately
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}

		#region Object overrides

		public override string ToString()
		{
			return Name ?? "<no name>";
		}

		public override int GetHashCode()
		{
			return _name?.GetHashCode() ?? 0;
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}

			var model = obj as DdxModel;
			if (model == null)
			{
				return false;
			}

			if (! Equals(_name, model._name))
			{
				return false;
			}

			return true;
		}

		#endregion

		/// <summary>
		/// The clone Id can be set if this instance is a (remote) clone of a persistent DdxModel.
		/// </summary>
		/// <param name="id"></param>
		public void SetCloneId(int id)
		{
			Assert.True(base.Id < 0, "Persistent entity or already initialized clone.");
			_cloneId = id;
		}

		#region Datasets

		[CanBeNull]
		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual Dataset GetDatasetCore([NotNull] string name, bool includeDeleted = false)
		{
			const bool useIndex = true;
			Dataset result = GetDataset(name, useIndex);

			return ! includeDeleted && result != null && result.Deleted
				       ? null // dataset is deleted, not included
				       : result;
		}

		public T AddDataset<T>([NotNull] T dataset) where T : Dataset
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			InvalidateDatasetIndex();
			InvalidateSpecialDatasetAssignment();

			if (_datasets.Contains(dataset))
			{
				throw new ArgumentException(
					$@"Dataset already registered {dataset}", nameof(dataset));
			}

			// assert unique names
			if (_datasets.Any(existing => string.Equals(existing.Name, dataset.Name,
			                                            StringComparison.OrdinalIgnoreCase)))
			{
				throw new ArgumentException(
					$"A dataset with the same name is already registered ({dataset.Name})");
			}

			_datasets.Add(dataset);

			dataset.Model = this;

			DatasetAddedCore(dataset);

			return dataset;
		}

		protected virtual void DatasetAddedCore<T>(T dataset) where T : Dataset { }

		public void RemoveDataset([NotNull] Dataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			if (! _datasets.Contains(dataset))
			{
				return;
			}

			InvalidateDatasetIndex();
			InvalidateSpecialDatasetAssignment();

			_datasets.Remove(dataset);

			dataset.Model = null;
		}

		/// <summary>
		/// Gets a dataset from type T:Dataset given its model dataset name.
		/// </summary>
		/// <param name="name">The dataset name as defined in the data model.</param>
		/// <param name="includeDeleted">if set to <c>true</c>, datasets registered as deleted are 
		/// included. Otherwise, they are excluded.</param>
		/// <returns>The dataset or null if not found or not of type T</returns>
		[CanBeNull]
		public T GetDatasetByModelName<T>([NotNull] string name, bool includeDeleted = false)
			where T : Dataset
		{
			return GetDatasetCore(name, includeDeleted) as T;
		}

		/// <summary>
		/// Gets a dataset given its model dataset name.
		/// </summary>
		/// <param name="name">The dataset name as defined in the data model.</param>
		/// <param name="includeDeleted">if set to <c>true</c>, datasets registered as deleted are 
		/// included. Otherwise, they are excluded.</param>
		/// <returns>The dataset or null if not found</returns>
		[CanBeNull]
		public Dataset GetDatasetByModelName([NotNull] string name, bool includeDeleted = false)
		{
			Dataset dataset = GetDatasetCore(name, includeDeleted);

			if (dataset != null)
			{
				return dataset;
			}

			// TODO: Create ModelExtensions that can be added dynamically?
			// ModelExtensions.FirstOrDefault(me => me.CanCreateDataset(name))?.Create(name, (dsName) => GetDatasetByModelName(dsName) as VectorDataset));

			// Do we need this? Should we add XmlModelExtensions that transport things like simple terrains?
			if (ModelSimpleTerrainDataset.CanCreateDataset(name))
			{
				var terrainDataset = ModelSimpleTerrainDataset.Create(name, this,
					dsName => GetDatasetByModelName(dsName));

				_datasets.Add(terrainDataset);
				_datasetIndex.Add(name, terrainDataset);
			}

			return dataset;
		}

		/// <summary>
		/// Gets all datasets.
		/// </summary>
		/// <param name="includeDeleted">if set to <c>true</c>, datasets registered as deleted are 
		/// included. Otherwise, they are excluded.</param>
		/// <returns>The list of datasets.</returns>
		[NotNull]
		public IList<Dataset> GetDatasets(bool includeDeleted = false)
		{
			return GetDatasets<Dataset>(null, includeDeleted);
		}

		/// <summary>
		/// Gets all datasets that match the conditions defined by the specified predicate.
		/// </summary>
		/// <param name="match">The <see cref="Predicate{T}"/> delegate that
		/// defines the conditions of the element to search for.</param>
		/// <param name="includeDeleted">if set to <c>true</c>, datasets registered as deleted are 
		/// included. Otherwise, they are excluded.</param>
		/// <returns>The list of matching datasets.</returns>
		[NotNull]
		public IList<Dataset> GetDatasets([CanBeNull] Predicate<Dataset> match,
		                                  bool includeDeleted = false)
		{
			return GetDatasets<Dataset>(match, includeDeleted);
		}

		/// <summary>
		/// Gets all datasets of a specified type.
		/// </summary>
		/// <typeparam name="T">The type of dataset to return.</typeparam>
		/// <param name="includeDeleted">if set to <c>true</c>, datasets registered as deleted are 
		/// included. Otherwise, they are excluded.</param>
		/// <returns>The list of datasets of the specified type.</returns>
		[NotNull]
		public IList<T> GetDatasets<T>(bool includeDeleted = false) where T : Dataset
		{
			return GetDatasets<T>(null, includeDeleted);
		}

		/// <summary>
		/// Gets all datasets of a specified type that match the conditions
		/// defined by the specified predicate.
		/// </summary>
		/// <typeparam name="T">The type of dataset to return.</typeparam>
		/// <param name="match">The <see cref="Predicate{T}"/> delegate that
		/// defines the conditions of the element to search for.</param>
		/// <param name="includeDeleted">if set to <c>true</c>, datasets registered as deleted are 
		/// included. Otherwise, they are excluded.</param>
		/// <returns>The list of matching datasets.</returns>
		[NotNull]
		public IList<T> GetDatasets<T>([CanBeNull] Predicate<T> match, bool includeDeleted = false)
			where T : Dataset
		{
			var result = new List<T>();

			foreach (Dataset dataset in _datasets)
			{
				var datasetOfType = dataset as T;

				if (datasetOfType == null)
				{
					continue;
				}

				if (dataset.Deleted && ! includeDeleted)
				{
					// ignore deleted dataset
				}
				else
				{
					if (match != null && ! match(datasetOfType))
					{
						// ignore because of predicate mismatch
					}
					else
					{
						result.Add(datasetOfType);
					}
				}
			}

			return result.AsReadOnly();
		}

		#endregion

		#region Associations

		/// <summary>
		/// Gets an association given the name of a relationship class.
		/// </summary>
		/// <param name="associationName">Name of the relationship class.</param>
		/// <param name="includeDeleted">if set to <c>true</c>, associations registered as deleted are 
		/// included. Otherwise, they are excluded.</param>
		/// <returns></returns>
		// ReSharper disable once VirtualMemberNeverOverridden.Global
		[CanBeNull]
		// ReSharper disable once VirtualMemberNeverOverridden.Global
		public virtual Association GetAssociationByModelName(
			[NotNull] string associationName,
			bool includeDeleted = false)
		{
			const bool useIndex = true;
			Association result = GetAssociation(associationName, useIndex);

			return ! includeDeleted && result != null && result.Deleted
				       ? null // association is deleted, not included
				       : result;
		}

		[NotNull]
		public Association AddAssociation([NotNull] Association association)
		{
			Assert.ArgumentNotNull(association, nameof(association));

			InvalidateAssociationIndex();

			// assert unique names
			if (_associations.Any(existing => string.Equals(existing.Name, association.Name,
			                                                StringComparison.OrdinalIgnoreCase)))
			{
				throw new ArgumentException(
					$"An association with the same name is already registered ({association.Name})");
			}

			_associations.Add(association);
			association.Model = this;

			return association;
		}

		public void RemoveAssociation([NotNull] Association association)
		{
			if (! _associations.Contains(association))
			{
				return;
			}

			InvalidateAssociationIndex();

			_associations.Remove(association);
			association.Model = null;
		}

		/// <summary>
		/// Gets the associations.
		/// </summary>
		/// <param name="includeDeleted">if set to <c>true</c> associations that are 
		/// registered as deleted are included in the result. Otherwise, they are excluded.</param>
		/// <returns>The list of associations.</returns>
		[NotNull]
		public IList<Association> GetAssociations(bool includeDeleted = false)
		{
			var result = new List<Association>(_associations.Count);

			foreach (Association association in _associations)
			{
				if (includeDeleted || ! association.Deleted &&
				    ! association.OriginDataset.Deleted &&
				    ! association.DestinationDataset.Deleted)
				{
					result.Add(association);
				}
			}

			return result;
		}

		#endregion

		public bool Contains([NotNull] IDdxDataset dataset)
		{
			return Contains((ModelElement) dataset);
		}

		public bool Contains([NotNull] ModelElement modelElement)
		{
			Assert.ArgumentNotNull(modelElement, nameof(modelElement));

			// NOTE: reference equals might return wrong result if 
			// the modelElement's model was not re-attached
			//return modelElement.Model == this;

			// TODO: consider ArgumentCondition? For backward compatibility:
			if (modelElement.Model == null)
			{
				_msg.DebugFormat("Model property of '{0}' is not set", modelElement);
				return false;
			}

			return Equals(modelElement.Model.Name, Name);
		}

		/// <summary>
		/// Reattaches any detached state that is held by the implementing instance.
		/// </summary>
		/// <param name="unitOfWork">The unit of work.</param>
		public void ReattachState(IUnitOfWork unitOfWork)
		{
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			if (IsPersistent)
			{
				unitOfWork.Reattach(this);
			}

			foreach (KeyValuePair<string, Dataset> pair in _datasetIndex)
			{
				Dataset dataset = pair.Value;

				if (dataset != null && dataset.IsPersistent)
				{
					unitOfWork.Reattach(dataset);
				}
			}

			foreach (KeyValuePair<string, Association> pair in _associationIndex)
			{
				Association association = pair.Value;

				if (association != null && association.IsPersistent)
				{
					unitOfWork.Reattach(association);
				}
			}

			// if initialized, reattach entities in collections

			if (unitOfWork.IsInitialized(_datasets))
			{
				foreach (Dataset dataset in _datasets)
				{
					if (dataset.IsPersistent)
					{
						unitOfWork.Reattach(dataset);
					}
				}
			}

			if (unitOfWork.IsInitialized(_associations))
			{
				foreach (Association association in _associations)
				{
					if (association.IsPersistent)
					{
						unitOfWork.Reattach(association);
					}
				}
			}

			ReattachStateCore(unitOfWork);
		}

		protected virtual void ReattachStateCore([NotNull] IUnitOfWork unitOfWork) { }

		public abstract string QualifyModelElementName(string modelElementName);

		[NotNull]
		public abstract string TranslateToModelElementName(
			[NotNull] string masterDatabaseDatasetName);

		#region Indexing

		public void InvalidateDatasetIndex()
		{
			_datasetIndex.Clear();
		}

		public void InvalidateAssociationIndex()
		{
			_associationIndex.Clear();
		}

		[CanBeNull]
		protected Dataset GetDataset([NotNull] string datasetName, bool useIndex)
		{
			if (useIndex)
			{
				return GetDatasetFromIndex(datasetName);
			}

			return _datasets.FirstOrDefault(dataset => string.Equals(dataset.Name, datasetName,
				                                StringComparison.OrdinalIgnoreCase));
		}

		Dataset IModelHarvest.GetExistingDataset(string datasetName, bool useIndex)
		{
			return GetDataset(datasetName, useIndex);
		}

		[CanBeNull]
		private Dataset GetDatasetFromIndex([NotNull] string name)
		{
			PrepareDatasetIndex();

			Dataset cachedDataset;
			return _datasetIndex.TryGetValue(name, out cachedDataset)
				       ? cachedDataset
				       : null;
		}

		private void PrepareDatasetIndex()
		{
			if (_datasetIndex.Count != 0)
			{
				return;
			}

			foreach (Dataset dataset in _datasets)
			{
				// DPS #185: Ignore deleted datasets to avoid duplicate keys
				if (! dataset.Deleted)
				{
					_datasetIndex.Add(dataset.Name, dataset);
				}
			}
		}

		[CanBeNull]
		protected Association GetAssociation([NotNull] string associationName, bool useIndex)
		{
			if (useIndex)
			{
				return GetAssociationFromIndex(associationName);
			}

			foreach (Association association in _associations)
			{
				if (string.Equals(association.Name, associationName,
				                  StringComparison.OrdinalIgnoreCase))
				{
					return association;
				}
			}

			return null; // not found
		}

		Association IModelHarvest.GetExistingAssociation(string associationName, bool useIndex)
		{
			return GetAssociation(associationName, useIndex);
		}

		private Association GetAssociationFromIndex(string relClassName)
		{
			PrepareAssociationIndex();

			Association cachedAssociation;
			return _associationIndex.TryGetValue(relClassName, out cachedAssociation)
				       ? cachedAssociation
				       : null; // not found
		}

		private void PrepareAssociationIndex()
		{
			if (_associationIndex.Count != 0)
			{
				return;
			}

			foreach (Association association in _associations)
			{
				_associationIndex.Add(association.Name, association);
			}
		}

		#endregion

		#region Special Dataset Assignments

		public void InvalidateSpecialDatasetAssignment()
		{
			_specialDatasetsAssigned = false;
		}

		protected abstract void CheckAssignSpecialDatasetCore(Dataset dataset);

		public void AssignSpecialDatasets()
		{
			if (_specialDatasetsAssigned)
			{
				return;
			}

			foreach (Dataset dataset in _datasets)
			{
				if (! dataset.Deleted)
				{
					CheckAssignSpecialDataset(dataset);
				}
			}

			_specialDatasetsAssigned = true;
		}

		protected void CheckAssignSpecialDataset(Dataset dataset)
		{
			// add any standard special datasets here

			// allow subclasses to add their own
			CheckAssignSpecialDatasetCore(dataset);
		}

		#endregion

		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual bool AllowUserChangingHarvestQualifiedElementNamesCore => true;

		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual bool AllowUserChangingUseMasterDatabaseOnlyForSchemaCore => true;

		[PublicAPI]
		public bool DisableAutomaticSchemaCaching { get; set; }

		public virtual bool AutoEnableSchemaCache => false;

		#region Implementation of IDbDatasetContainer

		public T GetDataset<T>(string tableName) where T : class, IDatasetDef
		{
			Dataset dataset = GetDataset(tableName, true);

			return dataset as T;
		}

		public IEnumerable<IDatasetDef> GetDatasetDefs(DatasetType ofType = DatasetType.Any)
		{
			foreach (Dataset dataset in Datasets)
			{
				if (ofType == DatasetType.Any ||
				    dataset.DatasetType == ofType)
				{
					yield return dataset;
				}
			}
		}

		public IEnumerable<IDatasetDef> GetGdbDatasets()
		{
			foreach (Dataset dataset in Datasets)
			{
				yield return dataset;
			}
		}

		public bool Equals(IDatasetContainer otherWorkspace)
		{
			return Equals((object) otherWorkspace);
		}

		#endregion
	}
}
