using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons;
using ProSuite.DomainModel.AO.DataModel.Harvesting;
using ProSuite.DomainModel.Core.DataModel;
using System;
using System.Collections.Generic;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Exceptions;
using System.Linq;

namespace ProSuite.DomainModel.AO.DataModel
{
	public static class ModelHarvesting
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Harvesting

		[NotNull]
		public static IEnumerable<ObjectAttributeType> Harvest(
			[NotNull] DdxModel model,
			[NotNull] IList<GeometryType> geometryTypes,
			[CanBeNull] IEnumerable<AttributeType> existingAttributeTypes = null)
		{
			Assert.ArgumentNotNull(geometryTypes, nameof(geometryTypes));

			IDatasetListBuilderFactory datasetListBuilderFactory =
				GetDatasetListBuilderFactory(model, geometryTypes);

			Assert.NotNull(datasetListBuilderFactory,
						   "DatasetListBuilderFactory not assigned to model");

			return Harvest(model, datasetListBuilderFactory,
						   GetAttributeConfigurator(model, existingAttributeTypes));
		}

		[NotNull]
		public static IEnumerable<ObjectAttributeType> Harvest(
			[NotNull] DdxModel model, [NotNull] IWorkspaceContext workspaceContext,
			[NotNull] IList<GeometryType> geometryTypes,
			[CanBeNull] IEnumerable<AttributeType> existingAttributeTypes = null)
		{
			Assert.ArgumentNotNull(geometryTypes, nameof(geometryTypes));

			IDatasetListBuilderFactory datasetListBuilderFactory =
				GetDatasetListBuilderFactory(model, geometryTypes);

			Assert.NotNull(datasetListBuilderFactory,
			               "DatasetListBuilderFactory not assigned to model");

			return Harvest(model, workspaceContext, datasetListBuilderFactory,
			               existingAttributeTypes);
		}

		[NotNull]
		public static IEnumerable<ObjectAttributeType> Harvest(
			[NotNull] DdxModel model, [NotNull] IWorkspaceContext workspaceContext,
			[NotNull] IDatasetListBuilderFactory datasetListBuilderFactory,
			[CanBeNull] IEnumerable<AttributeType> existingAttributeTypes = null)
		{
			Assert.ArgumentNotNull(datasetListBuilderFactory, nameof(datasetListBuilderFactory));

			return Harvest(model, workspaceContext, datasetListBuilderFactory,
			               GetAttributeConfigurator(model, existingAttributeTypes));
		}

		[NotNull]
		public static IEnumerable<ObjectAttributeType> Harvest(
			[NotNull] DdxModel model,
			[NotNull] IDatasetListBuilderFactory datasetListBuilderFactory,
			[CanBeNull] IAttributeConfigurator attributeConfigurator)
		{
			Assert.ArgumentNotNull(datasetListBuilderFactory,
								   nameof(datasetListBuilderFactory));

			model.AssertMasterDatabaseWorkspaceContextAccessible();

			DatasetFilter datasetFilter = HarvestingUtils.CreateDatasetFilter(
				model.DatasetInclusionCriteria, model.DatasetExclusionCriteria);

			IDatasetListBuilder datasetListBuilder =
				datasetListBuilderFactory.Create(model.SchemaOwner,
				                                 model.DatasetPrefix,
				                                 model.IgnoreUnversionedDatasets,
				                                 model.IgnoreUnregisteredTables,
				                                 ! model.HarvestQualifiedElementNames,
				                                 datasetFilter);

			IGeometryTypeConfigurator geometryTypeConfigurator =
				new GeometryTypeConfigurator(datasetListBuilderFactory.GeometryTypes);

			return Harvest(model, datasetListBuilder, attributeConfigurator, geometryTypeConfigurator);
		}

		[NotNull]
		public static IEnumerable<ObjectAttributeType> Harvest(
			[NotNull] DdxModel model, [NotNull] IWorkspaceContext workspaceContext,
			[NotNull] IDatasetListBuilderFactory datasetListBuilderFactory,
			[CanBeNull] IAttributeConfigurator attributeConfigurator)
		{
			Assert.ArgumentNotNull(datasetListBuilderFactory,
			                       nameof(datasetListBuilderFactory));

			DatasetFilter datasetFilter = HarvestingUtils.CreateDatasetFilter(
				model.DatasetInclusionCriteria, model.DatasetExclusionCriteria);

			IDatasetListBuilder datasetListBuilder =
				datasetListBuilderFactory.Create(model.SchemaOwner,
				                                 model.DatasetPrefix,
				                                 model.IgnoreUnversionedDatasets,
				                                 model.IgnoreUnregisteredTables,
				                                 !model.HarvestQualifiedElementNames,
				                                 datasetFilter);

			IGeometryTypeConfigurator geometryTypeConfigurator =
				new GeometryTypeConfigurator(datasetListBuilderFactory.GeometryTypes);

			return Harvest(model, workspaceContext, datasetListBuilder, attributeConfigurator, geometryTypeConfigurator);
		}

		[NotNull]
		public static IEnumerable<ObjectAttributeType> Harvest(
			[NotNull] DdxModel model,
			[NotNull] IDatasetListBuilder datasetListBuilder,
			[NotNull] IEnumerable<AttributeType> existingAttributeTypes,
			[NotNull] IGeometryTypeConfigurator geometryTypeConfigurator)
		{
			Assert.ArgumentNotNull(datasetListBuilder, nameof(datasetListBuilder));
			Assert.ArgumentNotNull(existingAttributeTypes, nameof(existingAttributeTypes));

			IAttributeConfigurator attributeConfigurator = GetAttributeConfigurator(
				model, existingAttributeTypes);

			return Harvest(model, datasetListBuilder, attributeConfigurator, geometryTypeConfigurator);
		}

		[NotNull]
		public static IEnumerable<ObjectAttributeType> Harvest(
			[NotNull] DdxModel model,
			[NotNull] IDatasetListBuilder datasetListBuilder,
			[CanBeNull] IAttributeConfigurator attributeConfigurator,
			[NotNull] IGeometryTypeConfigurator geometryTypeConfigurator)
		{
			Assert.ArgumentNotNull(datasetListBuilder, nameof(datasetListBuilder));

			IWorkspaceContext workspaceContext = model.AssertMasterDatabaseWorkspaceContextAccessible();

			return Harvest(model, workspaceContext, datasetListBuilder, attributeConfigurator,
			               geometryTypeConfigurator);
		}

		[NotNull]
		private static IEnumerable<ObjectAttributeType> Harvest(
			[NotNull] DdxModel model,
			[NotNull] IWorkspaceContext workspaceContext,
			[NotNull] IDatasetListBuilder datasetListBuilder,
			[CanBeNull] IAttributeConfigurator attributeConfigurator,
			[NotNull] IGeometryTypeConfigurator geometryTypeConfigurator)
		{
			IList<ObjectAttributeType> result =
				attributeConfigurator?.DefineAttributeTypes() ??
				new List<ObjectAttributeType>();

			IWorkspace workspace = workspaceContext.Workspace;

			using (_msg.IncrementIndentation("Refreshing content of model {0}", model.Name))
			{
				using (_msg.IncrementIndentation("Refreshing datasets"))
				{
					HarvestDatasets(model, workspaceContext,
									datasetListBuilder,
									attributeConfigurator, geometryTypeConfigurator);
				}

				bool datasetNamesAreQualified = model.HarvestQualifiedElementNames &&
				                                WorkspaceUtils.UsesQualifiedDatasetNames(
					                                workspace);

				using (_msg.IncrementIndentation("Refreshing associations"))
				{
					HarvestAssociations(model, workspaceContext,
					                    model.DefaultDatabaseName,
					                    model.DefaultDatabaseSchemaOwner,
										datasetNamesAreQualified);
				}
			}

			//DefaultDatabaseName = uniqueDatabaseName;
			//DefaultDatabaseSchemaOwner = uniqueSchemaOwner;
			model.ElementNamesAreQualified = model.HarvestQualifiedElementNames &&
			                                 WorkspaceUtils.UsesQualifiedDatasetNames(workspace);

			model.LastHarvestedByUser = EnvironmentUtils.UserDisplayName;
			model.LastHarvestedConnectionString =
				WorkspaceUtils.GetConnectionString(workspace, true);
			model.LastHarvestedDate = DateTime.Now;

			_msg.InfoFormat("Refreshing complete for model {0}", model.Name);

			return result;
		}

		public static void HarvestAssociations([NotNull] DdxModel model)
		{
			IWorkspaceContext workspaceContext = model.AssertMasterDatabaseWorkspaceContextAccessible();

			HarvestAssociations(model, workspaceContext, model.DefaultDatabaseName,
			                    model.DefaultDatabaseSchemaOwner,
			                    model.ElementNamesAreQualified);
		}

		private static void HarvestAssociations(
			[NotNull] DdxModel model,
			[NotNull] IWorkspaceContext workspaceContext,
			[CanBeNull] string uniqueDatabaseName,
			[CanBeNull] string uniqueSchemaOwner,
			bool datasetNamesAreQualified)
		{
			model.InvalidateAssociationIndex();

			IList<IRelationshipClass> relClasses =
				DatasetUtils.GetRelationshipClasses(workspaceContext.FeatureWorkspace);

			IWorkspace workspace = workspaceContext.Workspace;
			var existingAssociationByRelClass =
				new Dictionary<IRelationshipClass, Association>();

			foreach (IRelationshipClass relationshipClass in relClasses)
			{
				string relClassName = DatasetUtils.GetName(relationshipClass);

				const bool useAssociationIndex = true;
				Association association = GetExistingAssociation(model, relClassName, workspace,
				                                                 model.ElementNamesAreQualified,
																 useAssociationIndex);
				existingAssociationByRelClass.Add(relationshipClass, association);
			}

			foreach (
				KeyValuePair<IRelationshipClass, Association> pair in
				existingAssociationByRelClass)
			{
				IRelationshipClass relationshipClass = pair.Key;
				Association association = pair.Value;

				AddOrUpdateAssociation(model, relationshipClass, association,
									   workspaceContext,
									   uniqueDatabaseName,
									   uniqueSchemaOwner,
									   datasetNamesAreQualified);
			}

			model.InvalidateAssociationIndex();

			DeleteAssociationsNotInList(model, relClasses);
		}

		#endregion

		#region Private harvesting utils

		[CanBeNull]
		private static IDatasetListBuilderFactory GetDatasetListBuilderFactory(
			[NotNull] DdxModel model,
			[NotNull] IList<GeometryType> geometryTypes)
		{
			Assert.ArgumentNotNull(geometryTypes, nameof(geometryTypes));

			if (model.DatasetListBuilderFactoryClassDescriptor == null)
			{
				return null;
			}

			var factory = (IDatasetListBuilderFactory)
				model.DatasetListBuilderFactoryClassDescriptor.CreateInstance();

			factory.GeometryTypes = geometryTypes;
			return factory;
		}

		[CanBeNull]
		private static IAttributeConfigurator GetAttributeConfigurator(
			[NotNull] DdxModel model,
			[CanBeNull] IEnumerable<AttributeType> existingAttributeTypes)
		{
			if (model.AttributeConfiguratorFactoryClassDescriptor == null)
			{
				return null;
			}

			var factory = (IAttributeConfiguratorFactory)
				model.AttributeConfiguratorFactoryClassDescriptor.CreateInstance();

			IAttributeConfigurator attributeConfigurator = factory.Create(existingAttributeTypes);

			return attributeConfigurator;
		}

		private static void HarvestDatasets(
			[NotNull] DdxModel model,
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

			model.InvalidateDatasetIndex();
			model.InvalidateSpecialDatasetAssignment();

			IFeatureWorkspace featureWorkspace = workspaceContext.FeatureWorkspace;
			var workspace = (IWorkspace)featureWorkspace;

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
				Dataset dataset = GetExistingDataset(
					model, gdbDatasetName, workspace, useIndex,
					model.ElementNamesAreQualified);

				string reason;
				if (datasetListBuilder.IgnoreDataset(datasetName, out reason))
				{
					_msg.WarnFormat("Ignoring dataset {0}: {1}", datasetName.Name, reason);

					if (dataset != null && dataset.IsPersistent)
					{
						// must update its name
						HarvestingUtils.UpdateName(dataset, datasetName,
						                           model.HarvestQualifiedElementNames);

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
					if (!string.IsNullOrEmpty(ownerName))
					{
						ownerNames.Add(ownerName);
					}

					if (!string.IsNullOrEmpty(databaseName))
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
					HarvestingUtils.UpdateName(dataset, datasetName, model.HarvestQualifiedElementNames);
				}
			}

			string uniqueSchemaOwner = GetUniqueSchemaOwner(model, ownerNames);
			string uniqueDatabaseName = GetUniqueDatabaseName(model, databaseNames);

			// TODO find a cleaner way (must already assign properties otherwise OpenXYZ from master db workspace context will fail)

			if (anyRelevantDatasetsFound)
			{
				model.DefaultDatabaseName = uniqueDatabaseName;
				model.DefaultDatabaseSchemaOwner = uniqueSchemaOwner;
			}

			datasetListBuilder.AddDatasets(model);

			// remove obsolete datasets
			foreach (Dataset dataset in model.Datasets)
			{
				_msg.VerboseDebug(() => $"Checking existing dataset {dataset.Name}");

				string gdbDatasetName = ModelElementUtils.GetGdbElementName(dataset, workspace,
					model.DefaultDatabaseName, model.DefaultDatabaseSchemaOwner);

				bool datasetExists = gdbDatasetNames.Contains(gdbDatasetName);

				if (datasetExists)
				{
					// The dataset exists in the geodatabase
					if (dataset.Deleted && !ignoredExistingDatasetIds.Contains(dataset.Id))
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
					if (!dataset.Deleted)
					{
						// The dataset does not exist in the geodatabase
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

			model.AssignSpecialDatasets();

			// harvest elements of each individual dataset
			foreach (Dataset dataset in model.Datasets)
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
					HarvestObjectDataset(model, objectDataset, objectClass, attributeConfigurator,
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
			model.InvalidateDatasetIndex();
		}

		[CanBeNull]
		private static string GetUniqueDatabaseName(
			[NotNull] DdxModel model,
			[NotNull] ICollection<string> databaseNames)
		{
			if (databaseNames.Count <= 1)
			{
				return databaseNames.Count == 0
						   ? null
						   : databaseNames.ToList()[0];
			}

			if (model.HarvestQualifiedElementNames)
			{
				return null;
			}

			// this should not be possible, workspaces should be database-specific
			throw new InvalidConfigurationException(
				string.Format("Harvested datasets are from more than one database: {0}",
							  StringUtils.ConcatenateSorted(databaseNames, ",")));
		}

		[CanBeNull]
		private static string GetUniqueSchemaOwner(
			[NotNull] DdxModel model,
			[NotNull] ICollection<string> ownerNames)
		{
			if (ownerNames.Count <= 1)
			{
				return ownerNames.Count == 0
						   ? null
						   : ownerNames.ToList()[0];
			}

			if (model.HarvestQualifiedElementNames)
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
		[CanBeNull]
		private static Association GetExistingAssociation(
			[NotNull] DdxModel model,
			[NotNull] string relationshipClassName,
			[NotNull] IWorkspace workspace,
			bool associationNamesAreQualified,
			bool useIndex)
		{
			IModelHarvest modelAdmin = model;

			if (associationNamesAreQualified ||
				!ModelElementNameUtils.IsQualifiedName(relationshipClassName))
			{
				return modelAdmin.GetExistingAssociation(relationshipClassName, useIndex);
			}

			// the relationship class name is qualified, but the stored association names are (allegedly) not 
			// (if the stored flag is correct - it could also be an incorrect initial value).

			if (!ModelElementUtils.IsInSchema(workspace, relationshipClassName,
			                                  model.DefaultDatabaseSchemaOwner))
			{
				// the relationship class belongs to a different schema than the one last harvested
				return null;
			}

			// the relationship class belongs to the schema that was last harvested
			// -> search using the unqualified name.
			Association result = modelAdmin.GetExistingAssociation(
				ModelElementNameUtils.GetUnqualifiedName(relationshipClassName),
				useIndex);

			// if not found, try with the qualified name (if association names are really qualified)
			return result ?? modelAdmin.GetExistingAssociation(relationshipClassName, useIndex);
		}

		/// <summary>
		/// Gets the existing dataset based on a database dataset name and a workspace.
		/// </summary>
		[CanBeNull]
		private static Dataset GetExistingDataset(
			[NotNull] DdxModel model, [NotNull] string gdbDatasetName,
			[NotNull] IWorkspace workspace,
			bool useIndex,
			bool datasetNamesAreQualified)
		{
			Assert.ArgumentNotNullOrEmpty(gdbDatasetName, nameof(gdbDatasetName));
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			IModelHarvest modelAdmin = model;

			if (datasetNamesAreQualified ||
				!ModelElementNameUtils.IsQualifiedName(gdbDatasetName))
			{
				return modelAdmin.GetExistingDataset(gdbDatasetName, useIndex);
			}

			// the gdb dataset name is qualified, but the ddx dataset names are (allegedly) not 
			// (if the stored flag is correct - it could also be an incorrect initial value).

			// TODO also check database name (sql server/postgresql)?
			if (!ModelElementUtils.IsInSchema(workspace, gdbDatasetName,
			                                  model.DefaultDatabaseSchemaOwner))
			{
				// the gdb dataset belongs to a different schema than the one last harvested
				return null;
			}

			// the gdb dataset belongs to the schema that was last harvested
			// -> search using the unqualified name.
			Dataset result = modelAdmin.GetExistingDataset(ModelElementNameUtils.GetUnqualifiedName(gdbDatasetName),
			                                  useIndex);

			// if not found, try with the qualified name (if dataset names are really qualified)
			return result ?? modelAdmin.GetExistingDataset(gdbDatasetName, useIndex);
		}

		private static void HarvestObjectDataset(
			[NotNull] DdxModel model,
			[NotNull] ObjectDataset objectDataset,
			[NotNull] IObjectClass objectClass,
			[CanBeNull] IAttributeConfigurator attributeConfigurator,
			[NotNull] IGeometryTypeConfigurator geometryTypeConfigurator)
		{
			// alias name
			if (model.UpdateAliasNamesOnHarvest)
			{
				if (!Equals(objectClass.AliasName, objectDataset.AliasName))
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

		private static void DeleteAssociationsNotInList(
			[NotNull] DdxModel model,
			[NotNull] ICollection<IRelationshipClass> relClasses)
		{
			Assert.ArgumentNotNull(relClasses, nameof(relClasses));

			foreach (Association association in model.Associations)
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

		private static void AddOrUpdateAssociation(
			[NotNull] DdxModel model,
			[NotNull] IRelationshipClass relClass,
			[CanBeNull] Association association,
			[NotNull] IWorkspaceContext workspaceContext,
			[CanBeNull] string expectedDatabaseName,
			[CanBeNull] string expectedSchemaOwner,
			bool datasetNamesAreQualified)
		{
			Assert.ArgumentNotNull(relClass, nameof(relClass));
			Assert.ArgumentNotNull(workspaceContext, nameof(workspaceContext));

			IWorkspace workspace = workspaceContext.Workspace;

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
						model,
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
							model,
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
							relClass, destinationDataset, originDataset, model);

						_msg.InfoFormat("Adding association {0}", association.Name);

						model.AddAssociation(association);
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
				                           model.HarvestQualifiedElementNames);

				if (! association.Deleted)
				{
					// check if the association refers to the datasets referenced by the underlying relationship class
					// if not --> change the ends

					const bool useIndex = true;
					var expectedDestinationDataset = (ObjectDataset) GetExistingDataset(
						model,
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
						model,
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
					AttributeHarvestingUtils.HarvestAttributes(attributedAssociation, workspaceContext);
				}
			}
		}

		#endregion
	}
}
