using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public static class ModelElementUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private static readonly IDictionary<string, string> _tableNamesByQueryClassNames =
			new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public static bool IsInSchema([NotNull] IWorkspace workspace,
		                              [NotNull] string gdbDatasetName,
		                              [CanBeNull] string schemaOwner)
		{
			if (string.IsNullOrEmpty(schemaOwner))
			{
				return true;
			}

			string ownerName = DatasetUtils.GetOwnerName(workspace, gdbDatasetName);

			return ownerName.Equals(schemaOwner, StringComparison.OrdinalIgnoreCase);
		}

		public static bool CanOpenFromMasterDatabase([NotNull] IDdxDataset dataset)
		{
			Model model = dataset.Model as Model;

			if (model == null)
			{
				return false;
			}

			if (model.UseDefaultDatabaseOnlyForSchema)
			{
				return false;
			}

			IWorkspaceContext masterDatabaseWorkspaceContext =
				model.MasterDatabaseWorkspaceContext;

			if (masterDatabaseWorkspaceContext == null)
			{
				return false;
			}

			if (dataset is IDatasetCollection datasetCollection)
			{
				return datasetCollection.ContainedDatasets.All(
					containedDataset => masterDatabaseWorkspaceContext.Contains(containedDataset));
			}

			return masterDatabaseWorkspaceContext.Contains(dataset);
		}

		[CanBeNull]
		public static IFeatureClass TryOpenFromMasterDatabase(
			[NotNull] IVectorDataset dataset, bool allowAlways = false)
		{
			IDatasetContext context = GetMasterDatabaseWorkspaceContext(dataset,
				allowAlways);
			return context?.OpenFeatureClass(dataset);
		}

		[CanBeNull]
		public static IObjectClass TryOpenFromMasterDatabase(
			[NotNull] IObjectDataset dataset, bool allowAlways = false)
		{
			IDatasetContext context = GetMasterDatabaseWorkspaceContext(dataset,
				allowAlways);
			return context?.OpenObjectClass(dataset);
		}

		[CanBeNull]
		public static IRelationshipClass TryOpenFromMasterDatabase(
			[NotNull] Association association, bool allowAlways = false)
		{
			IWorkspaceContext context = GetMasterDatabaseWorkspaceContext(association,
				allowAlways);
			return context?.OpenRelationshipClass(association);
		}

		[CanBeNull]
		public static RasterDatasetReference TryOpenFromMasterDatabase(
			IDdxRasterDataset dataset,
			bool allowAlways = false)
		{
			IDatasetContext context = GetMasterDatabaseWorkspaceContext(dataset,
				allowAlways);

			return context?.OpenRasterDataset(dataset);
		}

		[CanBeNull]
		public static TerrainReference TryOpenFromMasterDatabase(
			ISimpleTerrainDataset dataset, bool allowAlways = false)
		{
			IDatasetContext context = GetMasterDatabaseWorkspaceContext(dataset,
				allowAlways);

			return context?.OpenTerrainReference(dataset);
		}

		[CanBeNull]
		public static MosaicRasterReference TryOpenFromMasterDatabase(
			IRasterMosaicDataset dataset, bool allowAlways = false)
		{
			IDatasetContext context = GetMasterDatabaseWorkspaceContext(dataset,
				allowAlways);

			return context?.OpenSimpleRasterMosaic(dataset);
		}

		[CanBeNull]
		public static ITopology TryOpenFromMasterDatabase(
			ITopologyDataset dataset, bool allowAlways = false)
		{
			IDatasetContext context = GetMasterDatabaseWorkspaceContext(dataset,
				allowAlways);

			return context?.OpenTopology(dataset);
		}

		[NotNull]
		public static List<SimpleTerrainDataSource> GetTerrainDataSources(
			[NotNull] ISimpleTerrainDataset dataset,
			[NotNull] Func<IObjectDataset, IObjectClass> openDataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			var terrainSources = new List<SimpleTerrainDataSource>();
			foreach (TerrainSourceDataset source in dataset.Sources)
			{
				IFeatureClass featureClass =
					Assert.NotNull((IFeatureClass) openDataset(source.Dataset));

				terrainSources.Add(new SimpleTerrainDataSource(
					                   featureClass,
					                   (esriTinSurfaceType) source.SurfaceFeatureType,
					                   source.WhereClause));
			}

			return terrainSources;
		}

		/// <summary>
		/// Gets the master database's workspace context of the specified model element, or null,
		/// if it is not accessible.
		/// </summary>
		/// <param name="modelElement"></param>
		/// <param name="allowAlways">Whether it the context shall be used even if the model is
		/// configured as 'Schema only'.</param>
		/// <returns></returns>
		[CanBeNull]
		public static IWorkspaceContext GetMasterDatabaseWorkspaceContext(
			[NotNull] IModelElement modelElement, bool allowAlways = false)
		{
			Assert.ArgumentNotNull(modelElement, nameof(modelElement));

			Model model = modelElement.Model as Model;

			if (model == null)
			{
				return null;
			}

			if (! allowAlways && model.UseDefaultDatabaseOnlyForSchema)
			{
				return null;
			}

			return model.MasterDatabaseWorkspaceContext;
		}

		/// <summary>
		/// Gets the master database's workspace context of the specified model element, or null,
		/// if it is not accessible.
		/// </summary>
		/// <param name="modelElement"></param>
		/// <param name="allowAlways">Whether it the context shall be used even if the model is
		/// configured as 'Schema only'.</param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		[NotNull]
		public static IWorkspaceContext GetAccessibleMasterDatabaseWorkspaceContext(
			[NotNull] IModelElement modelElement, bool allowAlways = false)
		{
			Assert.ArgumentNotNull(modelElement, nameof(modelElement));

			if (! (modelElement.Model is Model model))
			{
				throw new InvalidOperationException($"{modelElement.Name} has no model");
			}

			if (! allowAlways && model.UseDefaultDatabaseOnlyForSchema)
			{
				throw new InvalidOperationException(
					$"The model {model.Name} is configured as 'Schema only' and no data access has been allowed.");
			}

			if (! model.TryGetMasterDatabaseWorkspaceContext(out IWorkspaceContext result,
			                                                 out string noAccessReason))
			{
				throw new InvalidOperationException(
					$"The master database of the model {model.Name} referenced by {modelElement.Name} " +
					$"is not accessible: {noAccessReason}");
			}

			return result;
		}

		public static bool UseCaseSensitiveSql([NotNull] IReadOnlyTable table,
		                                       SqlCaseSensitivity caseSensitivity)
		{
			switch (caseSensitivity)
			{
				case SqlCaseSensitivity.CaseInsensitive:
					_msg.VerboseDebug(() => $"{table.Name}: not case sensitive");

					return false;

				case SqlCaseSensitivity.CaseSensitive:
					_msg.VerboseDebug(() => $"{table.Name}: case sensitive");

					return true;

				case SqlCaseSensitivity.SameAsDatabase:
					var sqlSyntax = table.Workspace as ISQLSyntax;
					bool result = sqlSyntax != null && sqlSyntax.GetStringComparisonCase();

					if (_msg.IsVerboseDebugEnabled)
					{
						_msg.VerboseDebug(() => sqlSyntax == null
							                        ? $"{table.Name}: database case sensitivity: UNKNOWN (use {result})"
							                        : $"{table.Name}: database case sensitivity: {result}");
					}

					return result;

				default:
					throw new InvalidOperationException(
						string.Format("Unsupported SqlCaseSensitivity: {0}", caseSensitivity));
			}
		}

		[NotNull]
		public static string GetGdbElementName([NotNull] IModelElement modelElement,
		                                       [NotNull] IWorkspace workspace,
		                                       [CanBeNull] string databaseName,
		                                       [CanBeNull] string schemaOwner)
		{
			Assert.ArgumentNotNull(modelElement, nameof(modelElement));
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			if (! WorkspaceUtils.UsesQualifiedDatasetNames(workspace) ||
			    ModelElementNameUtils.IsQualifiedName(modelElement.Name))
			{
				return modelElement.Name;
			}

			// TODO check if workspace required database name part, and add assertion?

			Assert.NotNullOrEmpty(schemaOwner,
			                      $"Unknown schema owner, cannot qualify name for unqualified dataset {modelElement.Name}");

			return DatasetUtils.QualifyTableName(workspace,
			                                     databaseName,
			                                     schemaOwner,
			                                     modelElement.Name);
		}

		[Obsolete]
		[NotNull]
		public static string GetQualifiedName([CanBeNull] string masterDatabaseName,
		                                      [CanBeNull] string schemaOwner,
		                                      [NotNull] string modelElementName)
		{
			return ModelElementNameUtils.GetQualifiedName(masterDatabaseName, schemaOwner,
			                                              modelElementName);
		}

		[NotNull]
		public static IObjectClass OpenObjectClass([NotNull] IFeatureWorkspace workspace,
		                                           [NotNull] string gdbDatasetName,
		                                           [NotNull] IObjectDataset dataset)
		{
			Model model = (Model) dataset.Model;

			return OpenObjectClass(workspace, gdbDatasetName,
			                       dataset.GetAttribute(AttributeRole.ObjectID)?.Name,
			                       model.SpatialReferenceDescriptor);
		}

		[NotNull]
		public static IObjectClass OpenObjectClass(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string gdbDatasetName,
			[CanBeNull] string oidFieldName = null,
			[CanBeNull] SpatialReferenceDescriptor spatialReferenceDescriptor = null)
		{
			return (IObjectClass) OpenTable(workspace, gdbDatasetName, oidFieldName,
			                                spatialReferenceDescriptor);
		}

		[NotNull]
		public static ITable OpenTable(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string gdbDatasetName,
			[CanBeNull] string oidFieldName = null,
			[CanBeNull] SpatialReferenceDescriptor spatialReferenceDescriptor = null)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNull(gdbDatasetName, nameof(gdbDatasetName));

			var sqlWorksace = workspace as ISqlWorkspace;

			if (sqlWorksace == null ||
			    DatasetUtils.IsRegisteredAsObjectClass((IWorkspace) workspace, gdbDatasetName))
			{
				return DatasetUtils.OpenTable(workspace, gdbDatasetName);
			}

			IQueryDescription queryDescription;
			try
			{
				string query = $"SELECT * FROM {gdbDatasetName}";
				queryDescription = sqlWorksace.GetQueryDescription(query);
			}
			catch (Exception ex)
			{
				_msg.WarnFormat(
					"Unable to get query description for unregistered table {0}: {1}",
					gdbDatasetName, ex.Message);

				return DatasetUtils.OpenTable(workspace, gdbDatasetName);
			}

			bool hasUnknownSref =
				queryDescription.SpatialReference == null ||
				queryDescription.SpatialReference is IUnknownCoordinateSystem;

			bool hasUnknownOid =
				StringUtils.IsNullOrEmptyOrBlank(queryDescription.OIDColumnName) ||
				queryDescription.IsOIDMappedColumn;

			if (! hasUnknownOid && (! hasUnknownSref || ! queryDescription.IsSpatialQuery))
			{
				return DatasetUtils.OpenTable(workspace, gdbDatasetName);
			}

			if (hasUnknownOid)
			{
				if (StringUtils.IsNotEmpty(oidFieldName))
				{
					queryDescription.OIDFields = oidFieldName;
				}
				else
				{
					IField uniqueIntegerField = GetUniqueIntegerField(workspace, gdbDatasetName);

					if (uniqueIntegerField != null)
					{
						queryDescription.OIDFields = uniqueIntegerField.Name;
					}
				}
			}

			if (hasUnknownSref && queryDescription.IsSpatialQuery)
			{
				queryDescription.SpatialReference = spatialReferenceDescriptor?.SpatialReference;
			}

			try
			{
				// NOTE: the unqualified name of the query class must start with a '%'
				string queryLayerName =
					DatasetUtils.GetQueryLayerClassName(workspace, gdbDatasetName);

				_msg.DebugFormat("Opening query layer with name {0}", queryLayerName);

				ITable queryClass = sqlWorksace.OpenQueryClass(queryLayerName, queryDescription);

				// NOTE: the query class is owned by the *connected* user, not by the owner of the underlying table/view

				string queryClassName = DatasetUtils.GetName(queryClass);

				_msg.DebugFormat("Name of opened query layer class: {0}", queryClassName);

				_tableNamesByQueryClassNames[queryClassName] = gdbDatasetName;

				return queryClass;
			}
			catch (Exception ex)
			{
				_msg.WarnFormat(
					"Unable to open unregistered table {0} as query layer: {1}",
					gdbDatasetName, ex.Message);

				return DatasetUtils.OpenTable(workspace, gdbDatasetName);
			}
		}

		[NotNull]
		public static string GetBaseTableName([NotNull] string gdbDatasetOrQueryClassName)
		{
			Assert.ArgumentNotNullOrEmpty(gdbDatasetOrQueryClassName,
			                              nameof(gdbDatasetOrQueryClassName));

			if (! DatasetUtils.IsQueryLayerClassName(gdbDatasetOrQueryClassName))
			{
				// not a query class name
				return gdbDatasetOrQueryClassName;
			}

			string baseTableName;
			if (_tableNamesByQueryClassNames.TryGetValue(gdbDatasetOrQueryClassName,
			                                             out baseTableName))
			{
				return baseTableName;
			}

			// return name as is
			return gdbDatasetOrQueryClassName;
		}

		[NotNull]
		public static string TranslateToMasterDatabaseDatasetName(
			[NotNull] IModelElement modelElement)
		{
			return TranslateToMasterDatabaseDatasetName(modelElement.Name, modelElement.Model);
		}

		[NotNull]
		public static string TranslateToMasterDatabaseDatasetName(
			[NotNull] string modelElementName, DdxModel ddxModel)
		{
			return ModelElementNameUtils.IsQualifiedName(modelElementName)
				       ? modelElementName
				       : ModelElementNameUtils.GetQualifiedName(ddxModel.DefaultDatabaseName,
				                                                ddxModel.DefaultDatabaseSchemaOwner,
				                                                modelElementName);
		}

		[CanBeNull]
		private static IField GetUniqueIntegerField([NotNull] IFeatureWorkspace workspace,
		                                            [NotNull] string gdbDatasetName)
		{
			ITable table = DatasetUtils.OpenTable(workspace, gdbDatasetName);

			return DatasetUtils.GetUniqueIntegerField(table) ??
			       DatasetUtils.GetUniqueIntegerField(table, requireUniqueIndex: false);
		}
	}
}
