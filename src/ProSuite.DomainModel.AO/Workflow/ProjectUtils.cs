using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.NamedValuesExpressions;
using ProSuite.Commons.Notifications;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Workflow.WorkspaceFilters;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.Workflow
{
	public static class ProjectUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CanBeNull]
		public static ObjectDataset GetDataset<P, M>(
			[NotNull] P project,
			[NotNull] IObjectClass objectClass,
			[CanBeNull] Predicate<Dataset> ignoreDataset)
			where P : Project<M>
			where M : ProductionModel
		{
			DdxModel model = project.ProductionModel;

			if (model == null)
			{
				_msg.DebugFormat("Project {0} has no production model", project.Name);
				return null;
			}

			var gdbDatasetName = DatasetUtils.GetName(objectClass);

			_msg.DebugFormat(
				"Get matching dataset candidate for class {0} in model {1} for project {2}",
				gdbDatasetName, model.Name, project.Name);

			Dataset dataset;
			using (_msg.IncrementIndentation())
			{
				dataset = GetMatchingDatasetCandidate(
					objectClass, model,
					project.UseOnlyModelDefaultDatabase,
					project.ChildDatabaseWorkspaceFilter,
					project.ChildDatabaseDatasetNameTransformer,
					ignoreDataset);
			}

			if (dataset == null)
			{
				_msg.VerboseDebug(() => "No match found");
				return null;
			}

			var objectDataset = dataset as ObjectDataset;
			if (objectDataset == null)
			{
				_msg.DebugFormat(
					"Matching dataset for {0} in model {1} is not an object dataset, ignore",
					gdbDatasetName,
					project.ProductionModel.Name);

				return null;
			}

			if (! ModelContextUtils.HasMatchingGeometryType(objectDataset, objectClass))
			{
				_msg.DebugFormat(
					"Matching dataset for {0} in model {1} has a different geometry type, ignore",
					gdbDatasetName,
					project.ProductionModel.Name);

				return null;
			}

			_msg.DebugFormat("Match found: {0}", objectDataset.Name);

			return objectDataset;
		}

		[CanBeNull]
		public static Dataset GetDataset<P, M>([NotNull] P project,
		                                       [NotNull] IWorkspace workspace,
		                                       [NotNull] IDatasetName datasetName)
			where P : Project<M>
			where M : ProductionModel
		{
			bool isModelDefaultDatabase = ModelContextUtils.IsModelDefaultDatabase(
				workspace, project.ProductionModel);

			return GetDataset<P, M>(project, datasetName,
			                        isModelDefaultDatabase);
		}

		[NotNull]
		public static IEnumerable<WorkspaceDataset> GetWorkspaceDatasets<P, M>(
			[NotNull] IWorkspace workspace,
			[NotNull] P project)
			where P : Project<M>
			where M : ProductionModel
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNull(project, nameof(project));

			Stopwatch watch = _msg.DebugStartTiming();

			M model = project.ProductionModel;

			IWorkspaceContext masterDatabaseWorkspaceContext =
				model.GetMasterDatabaseWorkspaceContext();

			bool isModelMasterDatabase = masterDatabaseWorkspaceContext != null &&
			                             WorkspaceUtils.IsSameDatabase(
				                             workspace,
				                             masterDatabaseWorkspaceContext.Workspace);

			string schemaOwner = isModelMasterDatabase
				                     ? model.DefaultDatabaseSchemaOwner
				                     : null;

			IEnumerable<DatasetMapping> datasetMappings = GetDatasetMappings<P, M>(
				workspace, project, isModelMasterDatabase, schemaOwner);

			List<WorkspaceDataset> result;

			if (! model.ElementNamesAreQualified &&
			    string.IsNullOrEmpty(schemaOwner) &&
			    WorkspaceUtils.UsesQualifiedDatasetNames(workspace))
			{
				// in this situation, more than one workspace dataset may be mapped to the
				// same model dataset. This should be supported for error datasets, where
				// this situation is not uncommon. 

				result = GetWorkspaceDatasetsWithDisambiguation(datasetMappings);
			}
			else
			{
				result = datasetMappings.Select(CreateWorkspaceDataset).ToList();
			}

			_msg.DebugStopTiming(
				watch,
				"Read datasets for project {0} in workspace {1} ({2} of {3} model datasets found)",
				project.Name,
				WorkspaceUtils.GetConnectionString(workspace, true),
				result.Count, model.GetDatasets().Count);

			return result;
		}

		[NotNull]
		public static IEnumerable<WorkspaceAssociation> GetWorkspaceAssociations<P, M>(
			[NotNull] IWorkspace workspace,
			[NotNull] P project)
			where P : Project<M>
			where M : ProductionModel
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNull(project, nameof(project));

			Stopwatch watch = _msg.DebugStartTiming();

			int associationCount = project.ProductionModel.GetAssociations().Count;
			if (associationCount == 0)
			{
				// there are no associations in the model, no need to enumerate relationship classes
				yield break;
			}

			bool? isModelMasterDatabase = null;

			var count = 0;
			foreach (IDatasetName datasetName in DatasetUtils.GetDatasetNames(
				         workspace, esriDatasetType.esriDTRelationshipClass))
			{
				if (isModelMasterDatabase == null)
				{
					isModelMasterDatabase = IsModelMasterDatabase(
						workspace, project.ProductionModel);
				}

				Association association = GetAssociation<P, M>(
					project, datasetName, isModelMasterDatabase.Value);

				if (association == null)
				{
					continue;
				}

				count++;
				yield return CreateWorkspaceAssociation(datasetName, association);
			}

			_msg.DebugStopTiming(
				watch,
				"Read relationship classes for project {0} in workspace {1} ({2} of {3} model associations found)",
				project.Name,
				WorkspaceUtils.GetConnectionString(workspace, true),
				count, associationCount);
		}

		[NotNull]
		public static IWorkspaceFilter CreateChildDatabaseWorkspaceFilter(
			[CanBeNull] string childDatabaseRestrictions)
		{
			var parser = new NamedValuesParser('=',
			                                   new[] { ";", Environment.NewLine },
			                                   new[] { "," },
			                                   " AND ");

			NotificationCollection notifications;
			IList<NamedValuesExpression> restrictionExpressions;
			if (! parser.TryParse(childDatabaseRestrictions,
			                      out restrictionExpressions,
			                      out notifications))
			{
				throw new RuleViolationException(notifications,
				                                 "Error reading child database restrictions");
			}

			IWorkspaceFilter filter =
				WorkspaceFilterFactory.TryCreate(restrictionExpressions,
				                                 out notifications);
			if (filter == null)
			{
				throw new RuleViolationException(notifications,
				                                 "Error creating child database workspace filter");
			}

			return filter;
		}

		[NotNull]
		public static DatasetNameTransformer CreateDatasetNameTransformer(
			[CanBeNull] string transformationPatterns)
		{
			return new DatasetNameTransformer(transformationPatterns);
		}

		[CanBeNull]
		private static Dataset GetMatchingDatasetCandidate(
			[NotNull] IObjectClass objectClass,
			[NotNull] DdxModel model,
			bool useOnlyModelDefaultDatabase,
			[NotNull] IWorkspaceFilter childDatabaseWorkspaceFilter,
			[NotNull] IDatasetNameTransformer datasetNameTransformer,
			[CanBeNull] Predicate<Dataset> ignoreDataset)
		{
			// called when evaluating added layers etc.

			Assert.ArgumentNotNull(objectClass, nameof(objectClass));
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNull(childDatabaseWorkspaceFilter,
			                       nameof(childDatabaseWorkspaceFilter));

			IWorkspace workspace = DatasetUtils.GetWorkspace(objectClass);

			string gdbDatasetName = DatasetUtils.GetName(objectClass);
			string modelDatasetName = model.TranslateToModelElementName(gdbDatasetName);

			Dataset dataset = model.GetDatasetByModelName(modelDatasetName);

			if (dataset != null && (ignoreDataset == null || ! ignoreDataset(dataset)))
			{
				if (ModelContextUtils.IsModelDefaultDatabase(workspace, model))
				{
					// the object class is from the model master database
					return IsFromOtherMasterDatabaseSchema(model, gdbDatasetName)
						       ? null
						       : dataset;
				}

				_msg.VerboseDebug(
					() =>
						$"Found matching model dataset {dataset.Name} in model {model.Name} but it is either not " +
						"accessible or not from the model's master workspace.");
			}

			// the object class is not from the model master database
			if (useOnlyModelDefaultDatabase)
			{
				// ... but all other workspaces have to be ignored
				return null;
			}

			string reason;
			if (childDatabaseWorkspaceFilter.Ignore(workspace, out reason))
			{
				_msg.DebugFormat("Workspace for object class {0} does not meet " +
				                 "child database filter criteria for model {1}: {2} ({3})",
				                 gdbDatasetName, model.Name, reason,
				                 WorkspaceUtils.GetConnectionString(workspace, true));

				return null;
			}

			var datasetName = GetModelElementNameForChildDatabaseElement(
				gdbDatasetName, model, datasetNameTransformer);

			_msg.VerboseDebug(
				() =>
					$"Dataset name for {gdbDatasetName} in model {model.Name} (non-master db): {datasetName ?? "<null>"}");

			return datasetName == null
				       ? null
				       : GetDataset(model, datasetName, ignoreDataset);
		}

		[CanBeNull]
		private static Dataset GetDataset([NotNull] DdxModel model,
		                                  [NotNull] string datasetModelName,
		                                  [CanBeNull] Predicate<Dataset> ignoreDataset)
		{
			Dataset dataset = model.GetDatasetByModelName(datasetModelName);

			return ignoreDataset == null || ! ignoreDataset(dataset)
				       ? dataset
				       : null;
		}

		/// <summary>
		/// Indicates if a qualified gdb element is from another database schema than the one harvested
		/// for a model with unqualified element names (i.e. a mismatch would happen if simply
		/// un-qualifying the gdb element name)
		/// </summary>
		/// <param name="model"></param>
		/// <param name="gdbElementName"></param>
		/// <returns></returns>
		private static bool IsFromOtherMasterDatabaseSchema(
			[NotNull] DdxModel model,
			[NotNull] string gdbElementName)
		{
			if (model.ElementNamesAreQualified ||
			    ! ModelElementNameUtils.IsQualifiedName(gdbElementName))
			{
				return false;
			}

			string owner = ModelElementNameUtils.GetOwnerName(gdbElementName).Trim();

			// the workspace is from the model master database
			string masterDatabaseSchemaOwner =
				(model.DefaultDatabaseSchemaOwner ?? string.Empty).Trim();

			// true if dataset is from a different schema:
			if (! string.IsNullOrEmpty(masterDatabaseSchemaOwner) &&
			    ! string.Equals(masterDatabaseSchemaOwner, owner,
			                    StringComparison.OrdinalIgnoreCase))
			{
				_msg.VerboseDebug(
					() =>
						$"Dataset {gdbElementName} is from master database of model {model.Name}, but from a different schema: {owner} (<> {masterDatabaseSchemaOwner})");

				return true;
			}

			return false;
		}

		[CanBeNull]
		private static Association GetAssociation<P, M>(
			[NotNull] P project,
			[NotNull] IDatasetName datasetName,
			bool isModelMasterDatabase) where P : Project<M>
			                            where M : ProductionModel
		{
			// called when activating a work context

			DdxModel model = project.ProductionModel;
			if (model == null)
			{
				return null;
			}

			string modelName = GetModelElementName(
				datasetName.Name,
				project.ChildDatabaseDatasetNameTransformer,
				model, isModelMasterDatabase);

			_msg.VerboseDebug(
				() =>
					$"Association name for {datasetName.Name} in model {model.Name} (from master db: {isModelMasterDatabase}): {modelName ?? "<null>"}");

			if (modelName == null)
			{
				return null;
			}

			Association association = model.GetAssociationByModelName(modelName);

			// TODO check cardinality, related object datasets etc.

			return association;
		}

		[CanBeNull]
		private static string GetModelElementNameForChildDatabaseElement(
			[NotNull] string gdbElementName,
			[NotNull] DdxModel model,
			[NotNull] IDatasetNameTransformer datasetNameTransformer)
		{
			if (! model.ElementNamesAreQualified)
			{
				return datasetNameTransformer.TransformName(
					ModelElementNameUtils.GetUnqualifiedName(gdbElementName));
			}

			// the model uses qualified element names

			if (DoNotMatchChildDatabaseForQualifiedModelElements())
			{
				_msg.VerboseDebug(() =>
					                  "Matching child database datasets for qualified model elements is disabled");

				// restore previous behavior: don't try to match child database elements for
				// model that was harvested with *qualified* element names
				return null;
			}

			if (ModelElementNameUtils.IsQualifiedName(gdbElementName))
			{
				_msg.VerboseDebug(
					() => "Gdb element name is qualified, and model uses qualified names");

				// gdb element name is also qualified, but from child database
				// rely on transformer for changing schema owner/database name, if required.
				return datasetNameTransformer.TransformName(gdbElementName);
			}

			// gdb element name is unqualified, but model element names are qualified

			if (string.IsNullOrEmpty(model.DefaultDatabaseSchemaOwner))
			{
				_msg.VerboseDebug(() =>
					                  "Gdb element name is unqualified, but qualified model has no unique schema owner " +
					                  "to allow name qualification");

				// default database schema owner is not known, cannot qualify name
				return null; // give up
			}

			_msg.VerboseDebug(
				() => "Using unique master database schema information to qualify dataset name");

			string transformedName = datasetNameTransformer.TransformName(gdbElementName);

			string owner = model.DefaultDatabaseSchemaOwner.Trim();
			return string.IsNullOrEmpty(model.DefaultDatabaseName)
				       ? $"{owner}.{transformedName}"
				       : $"{model.DefaultDatabaseName.Trim()}.{owner}.{transformedName}";
		}

		private static bool DoNotMatchChildDatabaseForQualifiedModelElements()
		{
			return EnvironmentUtils.GetBooleanEnvironmentVariableValue(
				"PROSUITE_DONT_MATCH_CHILD_DATABASE_FOR_QUALIFIED_MODEL_ELEMENT_NAMES");
		}

		[CanBeNull]
		private static string GetModelElementName(
			[NotNull] string gdbElementName,
			[NotNull] IDatasetNameTransformer datasetNameTransformer,
			[NotNull] DdxModel model,
			bool isModelMasterDatabase)
		{
			if (! isModelMasterDatabase)
			{
				return GetModelElementNameForChildDatabaseElement(
					gdbElementName, model, datasetNameTransformer);
			}

			// element is from master database

			if (IsFromOtherMasterDatabaseSchema(model, gdbElementName))
			{
				// Exclude datasetNames that are from ANOTHER schema/(database) than the model schema
				// error scenario:
				// - harvest from one schema --> unique names
				// - get workspace datasets from workspace with access to multiple schemas
				// - unqualified datasets in these schemas are not unique
				//   --> multiple matches by unqualified name to datasets
				//   --> multiple datasets returned
				// two cases
				// - the workspace corresponds to the model master database
				//   -> filter datasetNames by DefaultDatabaseSchemaOwner
				// - a "checkout" database with qualified names
				//   -> ???
				return null;
			}

			return model.ElementNamesAreQualified
				       ? gdbElementName // no need to check, element name must also be qualified
				       : ModelElementNameUtils
					       .GetUnqualifiedName(gdbElementName); // don't apply transformer
		}

		private static bool IsModelMasterDatabase([NotNull] IWorkspace workspace,
		                                          [NotNull] ProductionModel model)
		{
			IWorkspaceContext masterDatabaseWorkspaceContext =
				model.GetMasterDatabaseWorkspaceContext();

			return masterDatabaseWorkspaceContext != null &&
			       WorkspaceUtils.IsSameDatabase(
				       workspace,
				       masterDatabaseWorkspaceContext.Workspace);
		}

		[NotNull]
		private static WorkspaceAssociation CreateWorkspaceAssociation(
			[NotNull] IDatasetName datasetName,
			[NotNull] Association association)
		{
			IDatasetName featureDatasetName =
				DatasetUtils.GetFeatureDatasetName(datasetName);

			return new WorkspaceAssociation(datasetName.Name,
			                                featureDatasetName?.Name,
			                                association);
		}

		[CanBeNull]
		private static Dataset GetDataset<P, M>([NotNull] P project,
		                                        [NotNull] IDatasetName datasetName,
		                                        bool isModelMasterDatabase)
			where P : Project<M>
			where M : ProductionModel
		{
			// called when activating a work context

			// assume that the workspace is valid (according to the project) 
			DdxModel model = project.ProductionModel;
			if (model == null)
			{
				return null;
			}

			string modelName = GetModelElementName(
				datasetName.Name,
				project.ChildDatabaseDatasetNameTransformer,
				model, isModelMasterDatabase);

			_msg.VerboseDebug(
				() =>
					$"Dataset name for {datasetName.Name} in model {model.Name} (from master db: {isModelMasterDatabase}): {modelName ?? "<null>"}");

			if (modelName == null)
			{
				return null;
			}

			Dataset dataset = model.GetDatasetByModelName(modelName);

			if (dataset == null)
			{
				return null;
			}

			return ModelContextUtils.HasMatchingDatasetType(dataset, datasetName)
				       ? dataset
				       : null;
		}

		[NotNull]
		private static WorkspaceDataset CreateWorkspaceDataset(
			[NotNull] DatasetMapping datasetMapping)
		{
			IDatasetName datasetName = datasetMapping.DatasetName;
			IDatasetName featureDatasetName =
				DatasetUtils.GetFeatureDatasetName(datasetName);

			return new WorkspaceDataset(datasetName.Name,
			                            featureDatasetName?.Name,
			                            datasetMapping.Dataset);
		}

		[NotNull]
		private static IEnumerable<esriDatasetType> GetInvolvedDatasetTypes(
			[NotNull] DdxModel model)
		{
			Assert.ArgumentNotNull(model, nameof(model));

			var result = new HashSet<esriDatasetType>();

			foreach (Dataset dataset in model.GetDatasets())
			{
				result.Add(GetDatasetType(dataset));
			}

			return result;
		}

		private static esriDatasetType GetDatasetType([NotNull] Dataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			if (dataset is IVectorDataset)
			{
				return esriDatasetType.esriDTFeatureClass;
			}

			if (dataset is ITableDataset)
			{
				return esriDatasetType.esriDTTable;
			}

			if (dataset is ITopologyDataset)
			{
				return esriDatasetType.esriDTTopology;
			}

			if (dataset.GeometryType is GeometryTypeGeometricNetwork)
			{
				return esriDatasetType.esriDTGeometricNetwork;
			}

			if (dataset.GeometryType is GeometryTypeTerrain)
			{
				return esriDatasetType.esriDTTerrain;
			}

			if (dataset is IRasterMosaicDataset)
			{
				return esriDatasetType.esriDTMosaicDataset;
			}

			if (dataset is IDdxRasterDataset)
			{
				return esriDatasetType.esriDTRasterDataset;
			}

			throw new ArgumentException(
				string.Format("Unsupported dataset type: {0}", dataset.Name));
		}

		[NotNull]
		private static IEnumerable<DatasetMapping> GetDatasetMappings<P, M>(
			[NotNull] IWorkspace workspace,
			[NotNull] P project,
			bool isModelMasterDatabase,
			[CanBeNull] string schemaOwner)
			where P : Project<M>
			where M : ProductionModel
		{
			var datasetNames = DatasetUtils.GetDatasetNames(workspace,
			                                                GetInvolvedDatasetTypes(
				                                                project.ProductionModel),
			                                                schemaOwner)
			                               .ToList();

			var result = new List<DatasetMapping>();

			foreach (IDatasetName datasetName in datasetNames)
			{
				_msg.VerboseDebug(() => $"Workspace dataset: {datasetName.Name}");

				Dataset dataset = GetDataset<P, M>(project, datasetName,
				                                   isModelMasterDatabase);
				if (dataset == null)
				{
					continue;
				}

				_msg.VerboseDebug(() => $"- mapped dataset: {dataset.Name} [{dataset.Model.Name}]");

				result.Add(new DatasetMapping(datasetName, dataset));
			}

			return result;
		}

		[NotNull]
		private static List<WorkspaceDataset> GetWorkspaceDatasetsWithDisambiguation(
			[NotNull] IEnumerable<DatasetMapping> datasetMappings)
		{
			Assert.ArgumentNotNull(datasetMappings, nameof(datasetMappings));

			var errorDatasetMappings =
				new Dictionary<string, List<DatasetMapping>>(
					StringComparer.OrdinalIgnoreCase);
			var otherDatasetMappings =
				new Dictionary<string, List<DatasetMapping>>(
					StringComparer.OrdinalIgnoreCase);

			foreach (DatasetMapping datasetMapping in datasetMappings)
			{
				// possible extension: distinguish between "model dataset" and "non-model" or "system" dataset
				// --> abstract method on Model or Project "IsModelDataset()" or "IsSystemDataset()" would be needed
				AddDatasetMapping(datasetMapping,
				                  datasetMapping.Dataset is IErrorDataset
					                  ? errorDatasetMappings
					                  : otherDatasetMappings);
			}

			var result = new List<WorkspaceDataset>();

			string uniqueModelDatasetOwner = null;
			var datasetOwnerIsUnique = true;

			var ambiguousMappingCount = 0;
			foreach (KeyValuePair<string, List<DatasetMapping>> pair in
			         otherDatasetMappings)
			{
				List<DatasetMapping> mappings = pair.Value;

				if (mappings.Count > 1)
				{
					ambiguousMappingCount++;

					_msg.DebugFormat(
						"Model dataset {0} maps to {1} workspace dataset(s):", pair.Key,
						mappings.Count);
					foreach (DatasetMapping mapping in mappings)
					{
						_msg.DebugFormat("-> {0}", mapping.FullName);
					}
				}
				else if (mappings.Count == 1)
				{
					DatasetMapping mapping = mappings[0];

					result.Add(CreateWorkspaceDataset(mapping));

					if (datasetOwnerIsUnique)
					{
						string datasetOwner =
							DatasetUtils.GetOwnerName(mapping.DatasetName);

						if (uniqueModelDatasetOwner == null)
						{
							uniqueModelDatasetOwner = datasetOwner;
						}
						else
						{
							if (! string.Equals(uniqueModelDatasetOwner, datasetOwner,
							                    StringComparison.OrdinalIgnoreCase))
							{
								datasetOwnerIsUnique = false;
							}
						}
					}
				}
				else
				{
					throw new AssertionException("Unexpected dataset mapping count");
				}
			}

			foreach (KeyValuePair<string, List<DatasetMapping>> pair in
			         errorDatasetMappings)
			{
				List<DatasetMapping> mappings = pair.Value;

				if (mappings.Count > 1)
				{
					DatasetMapping mappingForUniqueOwner = null;
					if (datasetOwnerIsUnique && uniqueModelDatasetOwner != null)
					{
						mappingForUniqueOwner = GetMappingForDatasetOwner(
							uniqueModelDatasetOwner, mappings);
					}

					if (mappingForUniqueOwner != null)
					{
						result.Add(CreateWorkspaceDataset(mappingForUniqueOwner));
					}
					else
					{
						ambiguousMappingCount++;

						_msg.DebugFormat(
							"Error dataset {0} maps to {1} workspace dataset(s):",
							pair.Key,
							mappings.Count);
						foreach (DatasetMapping mapping in mappings)
						{
							_msg.DebugFormat("-> {0}", mapping.FullName);
						}
					}
				}
				else if (mappings.Count == 1)
				{
					result.Add(CreateWorkspaceDataset(mappings[0]));
				}
				else
				{
					throw new AssertionException("Unexpected dataset mapping count");
				}
			}

			if (ambiguousMappingCount > 0)
			{
				throw new InvalidConfigurationException(
					string.Format(ambiguousMappingCount == 1
						              ? "{0} dataset corresponds to more than one workspace dataset. This can happen if the datasets have been harvested without qualified names. See log for details"
						              : "{0} datasets each correspond to more than one workspace dataset. This can happen if the datasets have been harvested without qualified names. See log for details",
					              ambiguousMappingCount));
			}

			return result;
		}

		[CanBeNull]
		private static DatasetMapping GetMappingForDatasetOwner(
			[NotNull] string datasetOwner,
			[NotNull] IEnumerable<DatasetMapping> mappings)
		{
			foreach (DatasetMapping mapping in mappings)
			{
				if (string.Equals(DatasetUtils.GetOwnerName(mapping.DatasetName),
				                  datasetOwner,
				                  StringComparison.OrdinalIgnoreCase))
				{
					return mapping;
				}
			}

			return null;
		}

		private static void AddDatasetMapping(
			[NotNull] DatasetMapping datasetMapping,
			[NotNull] IDictionary<string, List<DatasetMapping>> mappingsPerGdbDatasetName)
		{
			string modelName = datasetMapping.Dataset.Name;

			List<DatasetMapping> mappings;
			if (! mappingsPerGdbDatasetName.TryGetValue(
				    modelName, out mappings))
			{
				mappings = new List<DatasetMapping>();
				mappingsPerGdbDatasetName.Add(modelName, mappings);
			}

			mappings.Add(datasetMapping);
		}

		private class DatasetMapping
		{
			[CanBeNull] private string _fullName;

			public DatasetMapping([NotNull] IDatasetName datasetName,
			                      [NotNull] Dataset dataset)
			{
				Assert.ArgumentNotNull(datasetName, nameof(datasetName));
				Assert.ArgumentNotNull(dataset, nameof(dataset));

				DatasetName = datasetName;
				Dataset = dataset;
			}

			[NotNull]
			public string FullName => _fullName ?? (_fullName = DatasetName.Name);

			[NotNull]
			public IDatasetName DatasetName { get; }

			[NotNull]
			public Dataset Dataset { get; }
		}
	}
}
