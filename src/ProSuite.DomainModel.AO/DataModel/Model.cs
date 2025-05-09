using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel.Harvesting;
using ProSuite.DomainModel.AO.Geodatabase;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DomainModel.AO.DataModel
{
	public abstract class Model : DdxModel
	{
		protected const string EnvironmentVariableNoModelSchemaCache =
			"PROSUITE_NO_MODEL_SCHEMA_CACHE";

		#region Fields

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

		private bool _keepDatasetLocks;

		[CanBeNull] private IWorkspaceContext _masterDatabaseWorkspaceContext;

		// private IWorkspaceProxy _workspaceProxy;
		private bool? _isMasterDatabaseAccessible;

		private string _lastMasterDatabaseAccessError;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Model"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected Model() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Model"/> class.
		/// </summary>
		/// <param name="name">The name of the model.</param>
		protected Model(string name) : base(name) { }

		#endregion

		[UsedImplicitly]
		public bool HarvestQualifiedElementNames
		{
			get { return _harvestQualifiedElementNames; }
			set { _harvestQualifiedElementNames = value; }
		}

		/// <summary>
		/// Gets a value indicating whether the user is allowed to change the 'Harvest qualified element names' property.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the user should be allowed to change this property; otherwise, <c>false</c>.
		/// </value>
		public bool AllowUserChangingHarvestQualifiedElementNames =>
			AllowUserChangingHarvestQualifiedElementNamesCore;

		/// <summary>
		/// Gets a value indicating whether the user is allowed to change the 'Use master database only for schema' property.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the user should be allowed to change this property; otherwise, <c>false</c>.
		/// </value>
		public bool AllowUserChangingUseMasterDatabaseOnlyForSchema =>
			AllowUserChangingUseMasterDatabaseOnlyForSchemaCore;

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
			private set { _lastHarvestedDate = value; }
		}

		[UsedImplicitly]
		public string LastHarvestedByUser
		{
			get { return _lastHarvestedByUser; }
			private set { _lastHarvestedByUser = value; }
		}

		[UsedImplicitly]
		public string LastHarvestedConnectionString
		{
			get { return _lastHarvestedConnectionString; }
			private set { _lastHarvestedConnectionString = value; }
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

		[CanBeNull]
		public IWorkspaceContext MasterDatabaseWorkspaceContext
		{
			get
			{
				if (! IsMasterDatabaseAccessible)
				{
					_masterDatabaseWorkspaceContext = null;
				}
				else if (_masterDatabaseWorkspaceContext == null)
				{
					_masterDatabaseWorkspaceContext = CreateMasterDatabaseWorkspaceContext();
				}

				return _masterDatabaseWorkspaceContext;
			}
		}

		public bool IsMasterDatabaseAccessible
		{
			get
			{
				if (! _isMasterDatabaseAccessible.HasValue)
				{
					_isMasterDatabaseAccessible = DetermineMasterDatabaseWorkspaceAccessibility();
				}

				return _isMasterDatabaseAccessible.Value;
			}
		}

		public string MasterDatabaseNoAccessReason
		{
			get
			{
				if (UserConnectionProvider == null)
				{
					return "No user connection provider defined for model";
				}

				return _lastMasterDatabaseAccessError;
			}
		}

		/// <summary>
		/// Gets or sets the class descriptor for the attribute configurator. Attribute
		/// configurators must implement <see cref="IAttributeConfigurator"/> interface.
		/// </summary>
		/// <value>The attribute configurator factory class descriptor.</value>
		public ClassDescriptor AttributeConfiguratorFactoryClassDescriptor
		{
			get { return _attributeConfiguratorFactoryClassDescriptor; }
			set { _attributeConfiguratorFactoryClassDescriptor = value; }
		}

		/// <summary>
		/// Gets or sets the class descriptor for the dataset list builder. Dataset
		/// list builder factories must implement <see cref="IDatasetListBuilderFactory"/> interface.
		/// </summary>
		/// <value>The dataset list builder factory class descriptor.</value>
		public ClassDescriptor DatasetListBuilderFactoryClassDescriptor
		{
			get { return _datasetListBuilderFactoryClassDescriptor; }
			set { _datasetListBuilderFactoryClassDescriptor = value; }
		}

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

				if (_masterDatabaseWorkspaceContext == null)
				{
					return;
				}

				_masterDatabaseWorkspaceContext = null;

				if (_keepDatasetLocks)
				{
					return;
				}

				// make sure any locks are released immediately
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}

		public bool TryGetMasterDatabaseWorkspaceContext(out IWorkspaceContext result,
		                                                 out string noAccessReason)
		{
			result = MasterDatabaseWorkspaceContext;

			if (result == null)
			{
				noAccessReason = MasterDatabaseNoAccessReason;
				return false;
			}

			noAccessReason = null;
			return true;
		}

		[NotNull]
		public IWorkspaceContext AssertMasterDatabaseWorkspaceContextAccessible()
		{
			if (! TryGetMasterDatabaseWorkspaceContext(out IWorkspaceContext workspaceContext,
			                                           out string noAccessReason))
			{
				throw new AssertionException(
					$"The master database of model {Name} is not accessible: {noAccessReason}");
			}

			return workspaceContext;
		}

		#region Harvesting

		[NotNull]
		public IEnumerable<ObjectAttributeType> Harvest(
			[NotNull] IList<GeometryType> geometryTypes,
			[CanBeNull] IEnumerable<AttributeType> existingAttributeTypes = null)
		{
			Assert.ArgumentNotNull(geometryTypes, nameof(geometryTypes));

			IDatasetListBuilderFactory datasetListBuilderFactory =
				GetDatasetListBuilderFactory(geometryTypes);

			Assert.NotNull(datasetListBuilderFactory,
			               "DatasetListBuilderFactory not assigned to model");

			return Harvest(datasetListBuilderFactory,
			               GetAttributeConfigurator(existingAttributeTypes));
		}

		[NotNull]
		public IEnumerable<ObjectAttributeType> Harvest(
			[NotNull] IDatasetListBuilderFactory datasetListBuilderFactory,
			[CanBeNull] IAttributeConfigurator attributeConfigurator)
		{
			Assert.ArgumentNotNull(datasetListBuilderFactory,
			                       nameof(datasetListBuilderFactory));

			AssertMasterDatabaseWorkspaceContextAccessible();

			DatasetFilter datasetFilter = HarvestingUtils.CreateDatasetFilter(
				_datasetInclusionCriteria, _datasetExclusionCriteria);

			IDatasetListBuilder datasetListBuilder =
				datasetListBuilderFactory.Create(SchemaOwner,
				                                 DatasetPrefix,
				                                 IgnoreUnversionedDatasets,
				                                 IgnoreUnregisteredTables,
				                                 ! HarvestQualifiedElementNames,
				                                 datasetFilter);

			IGeometryTypeConfigurator geometryTypeConfigurator =
				new GeometryTypeConfigurator(datasetListBuilderFactory.GeometryTypes);

			return Harvest(datasetListBuilder, attributeConfigurator, geometryTypeConfigurator);
		}

		[NotNull]
		public IEnumerable<ObjectAttributeType> Harvest(
			[NotNull] IDatasetListBuilder datasetListBuilder,
			[NotNull] IEnumerable<AttributeType> existingAttributeTypes,
			[NotNull] IGeometryTypeConfigurator geometryTypeConfigurator)
		{
			Assert.ArgumentNotNull(datasetListBuilder, nameof(datasetListBuilder));
			Assert.ArgumentNotNull(existingAttributeTypes, nameof(existingAttributeTypes));

			IAttributeConfigurator attributeConfigurator;
			if (_attributeConfiguratorFactoryClassDescriptor == null)
			{
				attributeConfigurator = null;
			}
			else
			{
				var attributeConfiguratorFactory =
					(IAttributeConfiguratorFactory)
					_attributeConfiguratorFactoryClassDescriptor.CreateInstance();

				attributeConfigurator =
					attributeConfiguratorFactory.Create(existingAttributeTypes);
			}

			return Harvest(datasetListBuilder, attributeConfigurator, geometryTypeConfigurator);
		}

		// TODO temporary solution to allow setting current catalog; replace with full solution for
		// - varying field db catalog name (same model in different databases)
		// - handling central / local case (parent / child replicas)
		[Obsolete]
		protected virtual void OnHarvesting() { }

		[NotNull]
		public IEnumerable<ObjectAttributeType> Harvest(
			[NotNull] IDatasetListBuilder datasetListBuilder,
			[CanBeNull] IAttributeConfigurator attributeConfigurator,
			[NotNull] IGeometryTypeConfigurator geometryTypeConfigurator)
		{
			Assert.ArgumentNotNull(datasetListBuilder, nameof(datasetListBuilder));

			IWorkspaceContext workspaceContext = AssertMasterDatabaseWorkspaceContextAccessible();

			return Harvest(workspaceContext, datasetListBuilder, attributeConfigurator,
			               geometryTypeConfigurator);
		}

		[NotNull]
		private IEnumerable<ObjectAttributeType> Harvest(
			[NotNull] IWorkspaceContext workspaceContext,
			[NotNull] IDatasetListBuilder datasetListBuilder,
			[CanBeNull] IAttributeConfigurator attributeConfigurator,
			[NotNull] IGeometryTypeConfigurator geometryTypeConfigurator)
		{
			OnHarvesting();

			IList<ObjectAttributeType> result =
				attributeConfigurator?.DefineAttributeTypes() ??
				new List<ObjectAttributeType>();

			IWorkspace workspace = workspaceContext.Workspace;

			using (_msg.IncrementIndentation("Refreshing content of model {0}", Name))
			{
				using (_msg.IncrementIndentation("Refreshing datasets"))
				{
					HarvestDatasets(workspaceContext,
					                datasetListBuilder,
					                attributeConfigurator, geometryTypeConfigurator);
				}

				bool datasetNamesAreQualified = HarvestQualifiedElementNames &&
				                                WorkspaceUtils.UsesQualifiedDatasetNames(
					                                workspace);

				using (_msg.IncrementIndentation("Refreshing associations"))
				{
					HarvestAssociations(workspaceContext,
					                    DefaultDatabaseName,
					                    DefaultDatabaseSchemaOwner,
					                    datasetNamesAreQualified);
				}
			}

			//DefaultDatabaseName = uniqueDatabaseName;
			//DefaultDatabaseSchemaOwner = uniqueSchemaOwner;
			ElementNamesAreQualified = HarvestQualifiedElementNames &&
			                           WorkspaceUtils.UsesQualifiedDatasetNames(workspace);

			LastHarvestedByUser = EnvironmentUtils.UserDisplayName;
			LastHarvestedConnectionString =
				WorkspaceUtils.GetConnectionString(workspace, true);
			LastHarvestedDate = DateTime.Now;

			_msg.InfoFormat("Refreshing complete for model {0}", Name);

			return result;
		}

		public void HarvestAssociations()
		{
			IWorkspaceContext workspaceContext = AssertMasterDatabaseWorkspaceContextAccessible();

			HarvestAssociations(workspaceContext, DefaultDatabaseName,
			                    DefaultDatabaseSchemaOwner,
			                    ElementNamesAreQualified);
		}

		private void HarvestAssociations([NotNull] IWorkspaceContext workspaceContext,
		                                 [CanBeNull] string uniqueDatabaseName,
		                                 [CanBeNull] string uniqueSchemaOwner,
		                                 bool datasetNamesAreQualified)
		{
			InvalidateAssociationIndex();

			IList<IRelationshipClass> relClasses =
				DatasetUtils.GetRelationshipClasses(workspaceContext.FeatureWorkspace);

			IWorkspace workspace = workspaceContext.Workspace;
			var existingAssociationByRelClass =
				new Dictionary<IRelationshipClass, Association>();

			foreach (IRelationshipClass relationshipClass in relClasses)
			{
				string relClassName = DatasetUtils.GetName(relationshipClass);

				const bool useAssociationIndex = true;
				Association association = GetExistingAssociation(relClassName, workspace,
				                                                 ElementNamesAreQualified,
				                                                 useAssociationIndex);
				existingAssociationByRelClass.Add(relationshipClass, association);
			}

			foreach (
				KeyValuePair<IRelationshipClass, Association> pair in
				existingAssociationByRelClass)
			{
				IRelationshipClass relationshipClass = pair.Key;
				Association association = pair.Value;

				AddOrUpdateAssociation(relationshipClass, association,
				                       workspace,
				                       uniqueDatabaseName,
				                       uniqueSchemaOwner,
				                       datasetNamesAreQualified);
			}

			InvalidateAssociationIndex();

			DeleteAssociationsNotInList(relClasses);
		}

		#endregion

		/// <summary>
		/// Opens the workspace, without building the schema cache even if <see cref="AutoEnableSchemaCache"/>
		/// is set to <c>true</c>.
		/// </summary>
		/// <returns></returns>
		[CanBeNull]
		public IWorkspace GetMasterDatabaseWorkspace()
		{
			if (_masterDatabaseWorkspaceContext != null)
			{
				return _masterDatabaseWorkspaceContext.Workspace;
			}

			if (UserConnectionProvider == null)
			{
				return null;
			}

			if (! IsMasterDatabaseAccessible)
			{
				return null;
			}

			return (IWorkspace) UserConnectionProvider.OpenWorkspace();
		}

		[NotNull]
		public string TranslateToModelElementName(
			[NotNull] string masterDatabaseDatasetName)
		{
			Assert.ArgumentNotNullOrEmpty(masterDatabaseDatasetName,
			                              nameof(masterDatabaseDatasetName));

			// the master database context does not support any prefix mappings etc.

			// translate query class name (if it is one) to table name
			string gdbDatasetName = ModelElementUtils.GetBaseTableName(
				masterDatabaseDatasetName, MasterDatabaseWorkspaceContext);

			return ElementNamesAreQualified
				       ? gdbDatasetName // expected to be qualified also
				       : ModelElementNameUtils.GetUnqualifiedName(gdbDatasetName);
		}

		public override string ToString()
		{
			return Name ?? "<no name>";
		}

		public override string QualifyModelElementName(string modelElementName)
		{
			IWorkspace workspace = GetMasterDatabaseWorkspace();

			if (workspace == null)
			{
				return modelElementName;
			}

			return DatasetUtils.QualifyTableName(workspace,
			                                     DefaultDatabaseName,
			                                     DefaultDatabaseSchemaOwner,
			                                     modelElementName);
		}

		#region Schema Cache Control

		[PublicAPI]
		public void EnableSchemaCache()
		{
			// schema cache might have been discarded (e.g. Reconcile does this)

			// Note:
			// The model Schema Cache can be turned OFF by environment variable.
			// This is experimental and used while analysing memory consumption.
			bool noModelSchemaCache =
				EnvironmentUtils.GetBooleanEnvironmentVariableValue(
					EnvironmentVariableNoModelSchemaCache);

			if (! noModelSchemaCache && MasterDatabaseWorkspaceContext != null)
			{
				WorkspaceUtils.EnableSchemaCache(MasterDatabaseWorkspaceContext.Workspace);
			}
		}

		[PublicAPI]
		public void DisableSchemaCache()
		{
			// schema cache might have been discarded
			if (MasterDatabaseWorkspaceContext != null)
			{
				// workspace already cached, make sure it has schema cache disabled
				WorkspaceUtils.DisableSchemaCache(MasterDatabaseWorkspaceContext.Workspace);
			}
		}

		#endregion

		#region Non-public members

		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual bool AllowUserChangingHarvestQualifiedElementNamesCore => true;

		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual bool AllowUserChangingUseMasterDatabaseOnlyForSchemaCore => true;

		// ReSharper disable once VirtualMemberNeverOverridden.Global

		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual SpatialReferenceDescriptor CreateDefaultSpatialReferenceDescriptor()
		{
			return null;
		}

		[PublicAPI]
		public bool DisableAutomaticSchemaCaching { get; set; }

		protected virtual bool AutoEnableSchemaCache => false;

		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual IEnumerable<ConnectionProvider> GetConnectionProvidersCore()
		{
			return new List<ConnectionProvider>();
		}

		[CanBeNull]
		private IDatasetListBuilderFactory GetDatasetListBuilderFactory(
			[NotNull] IList<GeometryType> geometryTypes)
		{
			Assert.ArgumentNotNull(geometryTypes, nameof(geometryTypes));

			if (_datasetListBuilderFactoryClassDescriptor == null)
			{
				return null;
			}

			var factory = (IDatasetListBuilderFactory)
				_datasetListBuilderFactoryClassDescriptor.CreateInstance();

			factory.GeometryTypes = geometryTypes;
			return factory;
		}

		[CanBeNull]
		private IAttributeConfigurator GetAttributeConfigurator(
			[CanBeNull] IEnumerable<AttributeType> existingAttributeTypes)
		{
			if (_attributeConfiguratorFactoryClassDescriptor == null)
			{
				return null;
			}

			var factory = (IAttributeConfiguratorFactory)
				_attributeConfiguratorFactoryClassDescriptor.CreateInstance();

			IAttributeConfigurator attributeConfigurator = factory.Create(existingAttributeTypes);

			return attributeConfigurator;
		}

		private void HarvestDatasets(
			[NotNull] IWorkspaceContext workspaceContext,
			[NotNull] IDatasetListBuilder datasetListBuilder,
			[CanBeNull] IAttributeConfigurator attributeConfigurator,
			[NotNull] IGeometryTypeConfigurator geometryTypeConfigurator)
		{
			Assert.ArgumentNotNull(workspaceContext, nameof(workspaceContext));
			Assert.ArgumentNotNull(datasetListBuilder, nameof(datasetListBuilder));

			if (attributeConfigurator == null)
			{
				_msg.Warn("No attribute configurator specified, " +
				          "attributes will not be specifically configured");
			}

			InvalidateDatasetIndex();
			InvalidateSpecialDatasetAssignment();

			IFeatureWorkspace featureWorkspace = workspaceContext.FeatureWorkspace;
			var workspace = (IWorkspace) featureWorkspace;

			bool workspaceUsesQualifiedDatasetNames =
				WorkspaceUtils.UsesQualifiedDatasetNames(workspace);

			// get all relevant dataset names

			var gdbDatasetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			var ignoredExistingDatasetIds = new HashSet<int>();
			var ignoredExistingDatasets = new List<Dataset>();

			var databaseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var ownerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var anyRelevantDatasetsFound = false;

			foreach (IDatasetName datasetName in
			         HarvestingUtils.GetHarvestableDatasetNames(featureWorkspace))
			{
				// ALL dataset names in the workspace are returned!

				string gdbDatasetName = datasetName.Name;

				_msg.DebugFormat("Processing dataset {0}", gdbDatasetName);

				if (gdbDatasetNames.Contains(gdbDatasetName))
				{
					_msg.VerboseDebug(() => "Ignoring duplicate dataset name");
					// in postgres workspaces, feature classes are also reported as
					// table datasets --> ignore duplicates
					continue;
				}

				gdbDatasetNames.Add(gdbDatasetName);

				// search for an existing dataset 
				const bool useIndex = false;
				Dataset dataset = GetExistingDataset(gdbDatasetName, workspace,
				                                     useIndex,
				                                     ElementNamesAreQualified);

				string reason;
				if (datasetListBuilder.IgnoreDataset(datasetName, out reason))
				{
					_msg.WarnFormat("Ignoring dataset {0}: {1}", datasetName.Name, reason);

					if (dataset != null && dataset.IsPersistent)
					{
						// must update its name
						HarvestingUtils.UpdateName(dataset, datasetName,
						                           HarvestQualifiedElementNames);

						ignoredExistingDatasetIds.Add(dataset.Id);
						ignoredExistingDatasets.Add(dataset);
					}

					_msg.Debug("Dataset is ignored");
					continue;
				}

				anyRelevantDatasetsFound = true;

				if (workspaceUsesQualifiedDatasetNames)
				{
					string databaseName;
					string ownerName;
					DatasetUtils.ParseTableName(workspace, gdbDatasetName,
					                            out databaseName,
					                            out ownerName,
					                            out string _);
					if (! string.IsNullOrEmpty(ownerName))
					{
						ownerNames.Add(ownerName);
					}

					if (! string.IsNullOrEmpty(databaseName))
					{
						databaseNames.Add(databaseName);
					}
				}

				if (dataset == null)
				{
					_msg.Debug("No existing dataset found");

					// the *added* dataset(s) will have the correctly qualified names
					datasetListBuilder.UseDataset(datasetName);
				}
				else
				{
					// TODO REVISE scenarios that lead to non-unique dataset names?
					HarvestingUtils.UpdateName(dataset, datasetName, HarvestQualifiedElementNames);
				}
			}

			string uniqueSchemaOwner = GetUniqueSchemaOwner(ownerNames);
			string uniqueDatabaseName = GetUniqueDatabaseName(databaseNames);

			// TODO find a cleaner way (must already assign properties otherwise OpenXYZ from master db workspace context will fail)

			if (anyRelevantDatasetsFound)
			{
				DefaultDatabaseName = uniqueDatabaseName;
				DefaultDatabaseSchemaOwner = uniqueSchemaOwner;
			}

			datasetListBuilder.AddDatasets(this);

			// remove obsolete datasets
			foreach (Dataset dataset in Datasets)
			{
				_msg.VerboseDebug(() => $"Checking existing dataset {dataset.Name}");

				string gdbDatasetName = ModelElementUtils.GetGdbElementName(dataset, workspace,
					DefaultDatabaseName, DefaultDatabaseSchemaOwner);

				bool datasetExists = gdbDatasetNames.Contains(gdbDatasetName);

				if (datasetExists)
				{
					// The dataset exists in the geodatabase
					if (dataset.Deleted && ! ignoredExistingDatasetIds.Contains(dataset.Id))
					{
						_msg.WarnFormat(
							"Dataset {0} was registered as deleted, but exists now. " +
							"Resurrecting it...", dataset.Name);

						dataset.RegisterExisting();
					}
				}
				else
				{
					// the dataset no longer exists in the geodatabase
					if (! dataset.Deleted)
					{
						// The dataset does not exists in the geodatabase
						_msg.WarnFormat("Registering dataset {0} as deleted",
						                dataset.Name);

						dataset.RegisterDeleted();
					}
				}
			}

			foreach (Dataset dataset in ignoredExistingDatasets)
			{
				if (dataset.Deleted)
				{
					continue;
				}

				// The dataset exists in the geodatabase, but it is ignored (and not yet marked as deleted)
				_msg.WarnFormat("Registering ignored dataset {0} as deleted",
				                dataset.Name);

				dataset.RegisterDeleted();
			}

			AssignSpecialDatasets();

			// harvest elements of each individual dataset
			foreach (Dataset dataset in Datasets)
			{
				if (dataset.Deleted)
				{
					// don't harvest for deleted dataset
					continue;
				}

				var objectDataset = dataset as ObjectDataset;

				if (objectDataset != null)
				{
					IObjectClass objectClass = workspaceContext.OpenObjectClass(objectDataset);
					Assert.NotNull(objectClass);

					// TODO determine if dataset was newly created
					// -> only warn about alias update if the dataset already existed
					HarvestObjectDataset(objectDataset, objectClass, attributeConfigurator,
					                     geometryTypeConfigurator);
				}
				else
				{
					if (StringUtils.IsNullOrEmptyOrBlank(dataset.AliasName))
					{
						dataset.AliasName = dataset.UnqualifiedName;
					}

					// missing geometry type:
					if (dataset.GeometryType == null)
					{
						GeometryType newGeometryType =
							geometryTypeConfigurator.GetGeometryType(dataset);

						if (newGeometryType != null)
						{
							_msg.WarnFormat("Assigning new geometry type {0} for dataset {1}",
							                newGeometryType.Name, dataset.Name);
							dataset.GeometryType = newGeometryType;
						}
					}

					// TODO update feature dataset name for geometric network and terrain datasets (these may change)
				}
			}

			// dataset names were updated
			InvalidateDatasetIndex();
		}

		[CanBeNull]
		private string GetUniqueDatabaseName([NotNull] ICollection<string> databaseNames)
		{
			if (databaseNames.Count <= 1)
			{
				return databaseNames.Count == 0
					       ? null
					       : databaseNames.ToList()[0];
			}

			if (HarvestQualifiedElementNames)
			{
				return null;
			}

			// this should not be possible, workspaces should be database-specific
			throw new InvalidConfigurationException(
				string.Format("Harvested datasets are from more than one database: {0}",
				              StringUtils.ConcatenateSorted(databaseNames, ",")));
		}

		[CanBeNull]
		private string GetUniqueSchemaOwner([NotNull] ICollection<string> ownerNames)
		{
			if (ownerNames.Count <= 1)
			{
				return ownerNames.Count == 0
					       ? null
					       : ownerNames.ToList()[0];
			}

			if (HarvestQualifiedElementNames)
			{
				return null;
			}

			throw new InvalidConfigurationException(
				string.Format(
					"Harvested datasets are from more than one schema: {0}. " +
					"Please filter model content to one schema when harvesting unqualified names",
					StringUtils.ConcatenateSorted(ownerNames, ",")));
		}

		/// <summary>
		/// Gets the existing association based on a relationship class name and a workspace.
		/// </summary>
		/// <param name="relationshipClassName">Name of the relationship class.</param>
		/// <param name="workspace">The workspace.</param>
		/// <param name="associationNamesAreQualified">if set to <c>true</c> [association names are qualified].</param>
		/// <param name="useIndex">if set to <c>true</c> [use index].</param>
		/// <returns></returns>
		/// <remarks>
		/// Used during association harvesting
		/// </remarks>
		[CanBeNull]
		private Association GetExistingAssociation([NotNull] string relationshipClassName,
		                                           [NotNull] IWorkspace workspace,
		                                           bool associationNamesAreQualified,
		                                           bool useIndex)
		{
			if (associationNamesAreQualified ||
			    ! ModelElementNameUtils.IsQualifiedName(relationshipClassName))
			{
				return GetAssociation(relationshipClassName, useIndex);
			}

			// the relationship class name is qualified, but the stored association names are (allegedly) not 
			// (if the stored flag is correct - it could also be an incorrect initial value).

			if (! ModelElementUtils.IsInSchema(workspace, relationshipClassName,
			                                   DefaultDatabaseSchemaOwner))
			{
				// the relationship class belongs to a different schema than the one last harvested
				return null;
			}

			// the relationship class belongs to the schema that was last harvested
			// -> search using the unqualified name.
			Association result = GetAssociation(
				ModelElementNameUtils.GetUnqualifiedName(relationshipClassName),
				useIndex);

			// if not found, try with the qualified name (if association names are really qualified)
			return result ?? GetAssociation(relationshipClassName, useIndex);
		}

		/// <summary>
		/// Gets the existing dataset based on a database dataset name and a workspace.
		/// </summary>
		/// <param name="gdbDatasetName">Full database name of the dataset.</param>
		/// <param name="workspace">The workspace.</param>
		/// <param name="useIndex">if set to <c>true</c> [use index].</param>
		/// <param name="datasetNamesAreQualified">if set to <c>true</c> [assume qualified names].</param>
		/// <returns></returns>
		/// <remarks>
		/// Used during dataset harvesting
		/// </remarks>
		[CanBeNull]
		private Dataset GetExistingDataset([NotNull] string gdbDatasetName,
		                                   [NotNull] IWorkspace workspace,
		                                   bool useIndex,
		                                   bool datasetNamesAreQualified)
		{
			Assert.ArgumentNotNullOrEmpty(gdbDatasetName, nameof(gdbDatasetName));
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			if (datasetNamesAreQualified ||
			    ! ModelElementNameUtils.IsQualifiedName(gdbDatasetName))
			{
				return GetDataset(gdbDatasetName, useIndex);
			}

			// the gdb dataset name is qualified, but the ddx dataset names are (allegedly) not 
			// (if the stored flag is correct - it could also be an incorrect initial value).

			// TODO also check database name (sql server/postgresql)?
			if (! ModelElementUtils.IsInSchema(workspace, gdbDatasetName,
			                                   DefaultDatabaseSchemaOwner))
			{
				// the gdb dataset belongs to a different schema than the one last harvested
				return null;
			}

			// the gdb dataset belongs to the schema that was last harvested
			// -> search using the unqualified name.
			Dataset result = GetDataset(ModelElementNameUtils.GetUnqualifiedName(gdbDatasetName),
			                            useIndex);

			// if not found, try with the qualified name (if dataset names are really qualified)
			return result ?? GetDataset(gdbDatasetName, useIndex);
		}

		private void HarvestObjectDataset([NotNull] ObjectDataset objectDataset,
		                                  [NotNull] IObjectClass objectClass,
		                                  [CanBeNull] IAttributeConfigurator attributeConfigurator,
		                                  [NotNull]
		                                  IGeometryTypeConfigurator geometryTypeConfigurator)
		{
			// alias name
			if (UpdateAliasNamesOnHarvest)
			{
				if (! Equals(objectClass.AliasName, objectDataset.AliasName))
				{
					string origAliasName = objectDataset.AliasName;
					objectDataset.AliasName = objectClass.AliasName;

					// warn about change if the dataset already existed
					if (objectDataset.IsPersistent)
					{
						_msg.WarnFormat("Updated alias name for dataset {0}: {1} (was: {2})",
						                objectDataset.Name, objectDataset.AliasName, origAliasName);
					}
					else
					{
						_msg.DebugFormat("Assigned alias name {0} to newly registered dataset {1}",
						                 objectDataset.AliasName, objectDataset.Name);
					}
				}
			}
			else
			{
				// set the alias name if it is undefined
				if (StringUtils.IsNullOrEmptyOrBlank(objectDataset.AliasName))
				{
					objectDataset.AliasName = objectClass.AliasName;

					_msg.DebugFormat(
						"Assigned alias name {0} to dataset {1} (alias name was undefined)",
						objectDataset.AliasName, objectDataset.Name);
				}
			}

			// display format
			if (StringUtils.IsNullOrEmptyOrBlank(objectDataset.DisplayFormat))
			{
				objectDataset.DisplayFormat = FieldDisplayUtils.GetDefaultRowFormat(objectClass);
			}

			// object types
			using (_msg.IncrementIndentation("Refreshing object types for dataset '{0}'",
			                                 objectDataset.Name))
			{
				ObjectTypeHarvestingUtils.HarvestObjectTypes(objectDataset, objectClass);
			}

			// attributes
			using (_msg.IncrementIndentation("Refreshing attributes for dataset '{0}'",
			                                 objectDataset.Name))
			{
				AttributeHarvestingUtils.HarvestAttributes(objectDataset, attributeConfigurator,
				                                           objectClass);
			}

			// shape type
			if (objectDataset.HasGeometry)
			{
				using (_msg.IncrementIndentation("Refreshing geometry type for dataset '{0}'",
				                                 objectDataset.Name))
				{
					AttributeHarvestingUtils.HarvestGeometryType(
						objectDataset, geometryTypeConfigurator, objectClass);
				}
			}
		}

		private void DeleteAssociationsNotInList(
			[NotNull] ICollection<IRelationshipClass> relClasses)
		{
			Assert.ArgumentNotNull(relClasses, nameof(relClasses));

			foreach (Association association in Associations)
			{
				if (HarvestingUtils.ExistsAssociation(relClasses, association.Name) ||
				    association.Deleted)
				{
					continue;
				}

				_msg.WarnFormat("Registering association {0} as deleted",
				                association.Name);

				association.RegisterDeleted();
			}
		}

		private void AddOrUpdateAssociation([NotNull] IRelationshipClass relClass,
		                                    [CanBeNull] Association association,
		                                    [NotNull] IWorkspace workspace,
		                                    [CanBeNull] string expectedDatabaseName,
		                                    [CanBeNull] string expectedSchemaOwner,
		                                    bool datasetNamesAreQualified)
		{
			Assert.ArgumentNotNull(relClass, nameof(relClass));
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			// TODO incorrect warnings 
			// 1. harvest empty (non-matching schema owner), unqualified names
			// 2. erase schema owner, re-harvest qualified names
			// -> warnings about not found origin/destination datasets

			// TODO error when switching to qualified names when all datasets are registered as deleted

			// TODO schema owner not respected in some situation

			string relClassName = DatasetUtils.GetName(relClass);

			string databaseName = null;
			string schemaOwnerName = null;
			if (WorkspaceUtils.UsesQualifiedDatasetNames(workspace))
			{
				DatasetUtils.ParseTableName(workspace, relClassName,
				                            out databaseName,
				                            out schemaOwnerName,
				                            out string _);
			}

			if (association == null)
			{
				// association not yet registered

				if (! string.IsNullOrEmpty(expectedDatabaseName))
				{
					if (! string.Equals(databaseName, expectedDatabaseName,
					                    StringComparison.OrdinalIgnoreCase))
					{
						_msg.DebugFormat("Skipping relationship class in other database: {0}",
						                 relClassName);
						return;
					}
				}

				if (! string.IsNullOrEmpty(expectedSchemaOwner))
				{
					if (! string.Equals(schemaOwnerName, expectedSchemaOwner,
					                    StringComparison.OrdinalIgnoreCase))
					{
						_msg.DebugFormat("Skipping relationship class in other schema: {0}",
						                 relClassName);
						return;
					}
				}

				// if both ends are on registered datasets, add the association
				ObjectDataset originDataset = null;
				ObjectDataset destinationDataset = null;

				IObjectClass destinationClass = null;
				IObjectClass originClass = null;

				try
				{
					destinationClass = relClass.DestinationClass;
				}
				catch (Exception e)
				{
					_msg.DebugFormat(
						"Error opening destination class of relationship class '{0}': {1}",
						relClassName, e.Message);
				}

				const bool useIndex = true;
				if (destinationClass != null)
				{
					destinationDataset = (ObjectDataset) GetExistingDataset(
						DatasetUtils.GetName(destinationClass), workspace,
						useIndex, datasetNamesAreQualified);

					if (destinationDataset == null)
					{
						_msg.DebugFormat("Dataset not registered: {0}", destinationClass.AliasName);
					}
				}

				try
				{
					originClass = relClass.OriginClass;
				}
				catch (Exception e)
				{
					_msg.DebugFormat("Error opening origin class of relationship class '{0}': {1}",
					                 relClassName, e.Message);
				}

				if (originClass != null)
				{
					originDataset =
						(ObjectDataset) GetExistingDataset(
							DatasetUtils.GetName(originClass), workspace,
							useIndex, datasetNamesAreQualified);

					if (originDataset == null)
					{
						_msg.DebugFormat("Dataset not registered: {0}", originClass.AliasName);
					}
				}

				if (originDataset == null || destinationDataset == null)
				{
					_msg.DebugFormat("Skipping relationship class {0}", relClassName);
				}
				else
				{
					try
					{
						association = HarvestingUtils.CreateAssociation(
							relClass, destinationDataset, originDataset, this);

						_msg.InfoFormat("Adding association {0}", association.Name);

						AddAssociation(association);
					}
					catch (Exception e)
					{
						_msg.Debug(e.Message, e);
						_msg.WarnFormat(
							"Error creating association for relationship class {0}: {1}",
							relClassName, e.Message);
					}
				}
			}
			else
			{
				// association already registered

				// TODO check if the association and the ends are still of the correct type. 
				// if not: delete them (really) and add them again

				// TODO apply same checks as for unregistered case? 

				if (association.Deleted)
				{
					_msg.WarnFormat(
						"Association {0} was registered as deleted, but exists now. " +
						"Resurrecting it...", association.Name);

					association.RegisterExisting();
				}

				if (! association.Deleted)
				{
					bool incorrectContext = ! string.IsNullOrEmpty(expectedDatabaseName) &&
					                        ! string.Equals(databaseName, expectedDatabaseName,
					                                        StringComparison.OrdinalIgnoreCase);

					bool incorrectSchema = ! string.IsNullOrEmpty(expectedSchemaOwner) &&
					                       ! string.Equals(schemaOwnerName, expectedSchemaOwner,
					                                       StringComparison.OrdinalIgnoreCase);

					if (incorrectContext || incorrectSchema)
					{
						_msg.WarnFormat(
							"Relationship class {0} is in an unexpected schema, registering the association as as deleted",
							relClassName);
						association.RegisterDeleted();
					}
				}

				association.Cardinality = HarvestingUtils.GetCardinality(relClass);

				HarvestingUtils.UpdateName(association,
				                           DatasetUtils.GetName(relClass),
				                           HarvestQualifiedElementNames);

				if (! association.Deleted)
				{
					// check if the association refers to the datasets referenced by the underlying relationship class
					// if not --> change the ends

					const bool useIndex = true;
					var expectedDestinationDataset = (ObjectDataset) GetExistingDataset(
						DatasetUtils.GetName(relClass.DestinationClass), workspace,
						useIndex, datasetNamesAreQualified);
					// TODO what if null or deleted?

					if (expectedDestinationDataset == null)
					{
						_msg.WarnFormat(
							"The destination dataset of association {0} was not found: {1}",
							association.Name,
							DatasetUtils.GetName(relClass.DestinationClass));
					}
					else
					{
						if (! Equals(expectedDestinationDataset, association.DestinationDataset))
						{
							_msg.WarnFormat(
								"The destination dataset of association {0} has changed.{3}" +
								"- Previous destination dataset: {1}{3}" +
								"- New destination dataset: {2}{3}" +
								"Redirecting association end to new dataset.",
								association.Name,
								association.DestinationDataset.Name,
								expectedDestinationDataset.Name,
								Environment.NewLine);

							HarvestingUtils.RedirectAssociationEnd(
								relClass, association.DestinationEnd, expectedDestinationDataset);
						}
					}

					var expectedOriginDataset = (ObjectDataset) GetExistingDataset(
						DatasetUtils.GetName(relClass.OriginClass), workspace,
						useIndex, datasetNamesAreQualified);

					if (expectedOriginDataset == null)
					{
						_msg.WarnFormat("The origin dataset of association {0} was not found: {1}",
						                association.Name,
						                DatasetUtils.GetName(relClass.OriginClass));
					}
					else
					{
						if (! Equals(expectedOriginDataset, association.OriginDataset))
						{
							_msg.WarnFormat(
								"The origin dataset of association {0} has changed.{3}" +
								"- Previous origin dataset: {1}{3}" +
								"- New origin dataset: {2}{3}" +
								"Redirecting association end to new dataset.",
								association.Name,
								association.OriginDataset.Name,
								expectedOriginDataset.Name,
								Environment.NewLine);

							HarvestingUtils.RedirectAssociationEnd(
								relClass, association.OriginEnd, expectedOriginDataset);
						}
					}
				}
			}

			// No matter if the association already existed or was added, 
			// if it is an attributed association, harvest its attributes
			if (association != null)
			{
				var attributedAssociation = association as AttributedAssociation;

				if (attributedAssociation != null)
				{
					AttributeHarvestingUtils.HarvestAttributes(attributedAssociation);
				}
			}
		}

		[NotNull]
		protected abstract IWorkspaceContext CreateMasterDatabaseWorkspaceContext();

		[NotNull]
		protected IWorkspaceContext CreateDefaultMasterDatabaseWorkspaceContext()
		{
			_msg.Debug("Opening default master database workspace context...");

			IFeatureWorkspace featureWorkspace = UserConnectionProvider.OpenWorkspace();

			var result = new MasterDatabaseWorkspaceContext(featureWorkspace, this);

			if (AutoEnableSchemaCache && ! DisableAutomaticSchemaCaching)
			{
				// The model schema cache can be turned OFF by environment variable.
				bool noModelSchemaCache =
					EnvironmentUtils.GetBooleanEnvironmentVariableValue(
						EnvironmentVariableNoModelSchemaCache);

				if (! noModelSchemaCache)
				{
					WorkspaceUtils.EnableSchemaCache(result.Workspace);
				}
			}

			return result;
		}

		private bool DetermineMasterDatabaseWorkspaceAccessibility()
		{
			if (UserConnectionProvider == null)
			{
				return false;
			}

			try
			{
				// try to open the workspace
				UserConnectionProvider.OpenWorkspace();

				return true;
			}
			catch (Exception e)
			{
				_msg.DebugFormat("Error opening master database for model {0}: {1}",
				                 Name, e.Message);

				_lastMasterDatabaseAccessError = e.Message;

				return false;
			}
		}

		#endregion
	}
}
