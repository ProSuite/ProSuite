using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel.Harvesting
{
	public abstract class DatasetListBuilderBase : IDatasetListBuilder
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly IList<Dataset> _datasets = new List<Dataset>();
		private readonly IGeometryTypeConfigurator _geometryTypeConfigurator;
		[NotNull] private readonly ErrorDatasetSchema _errorDatasetSchema;
		[CanBeNull] private readonly string _modelDatasetPrefix;
		private readonly bool _ignoreUnversionedDatasets;
		private readonly bool _ignoreUnregisteredTables;

		[CanBeNull] private readonly IDatasetFilter _datasetFilter;
		[CanBeNull] private readonly string _modelSchemaOwner;
		[NotNull] private readonly IDictionary<string, Type> _typeMap;

		protected bool UnqualifyDatasetName { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetListBuilderBase"/> class.
		/// </summary>
		/// <param name="geometryTypeConfigurator">The geometry type configurator.</param>
		/// <param name="errorDatasetSchema">The error dataset schema.</param>
		/// <param name="modelSchemaOwner">The name of the model schema owner.</param>
		/// <param name="modelDatasetPrefix">The model dataset prefix.</param>
		/// <param name="ignoreUnversionedDatasets">if set to <c>true</c> unversioned datasets in a versioned workspace are ignored.</param>
		/// <param name="ignoreUnregisteredTables">if set to <c>true</c> unregistered tables are ignored.</param>
		/// <param name="unqualifyDatasetName">if set to <c>true</c> dataset names (including feature dataset names) are unqualified.</param>
		/// <param name="datasetFilter">The dataset filter.</param>
		protected DatasetListBuilderBase(
			[NotNull] IGeometryTypeConfigurator geometryTypeConfigurator,
			[NotNull] ErrorDatasetSchema errorDatasetSchema,
			[CanBeNull] string modelSchemaOwner,
			[CanBeNull] string modelDatasetPrefix,
			bool ignoreUnversionedDatasets,
			bool ignoreUnregisteredTables,
			bool unqualifyDatasetName,
			[CanBeNull] IDatasetFilter datasetFilter)
		{
			Assert.ArgumentNotNull(geometryTypeConfigurator, nameof(geometryTypeConfigurator));
			Assert.ArgumentNotNull(errorDatasetSchema, nameof(errorDatasetSchema));

			_geometryTypeConfigurator = geometryTypeConfigurator;
			_errorDatasetSchema = errorDatasetSchema;
			_modelSchemaOwner = modelSchemaOwner;
			_modelDatasetPrefix = modelDatasetPrefix;
			_ignoreUnversionedDatasets = ignoreUnversionedDatasets;
			_ignoreUnregisteredTables = ignoreUnregisteredTables;
			UnqualifyDatasetName = unqualifyDatasetName;
			_datasetFilter = datasetFilter;

			_typeMap = CreateTypeMap();
		}

		#region IDatasetListBuilder implementation

		public bool IgnoreDataset(IDatasetName datasetName,
		                          out string reason)
		{
			IWorkspace workspace = WorkspaceUtils.OpenWorkspace(datasetName);

			if (! ModelElementUtils.IsInSchema(workspace, datasetName.Name, _modelSchemaOwner))
			{
				reason = string.Format("not in model schema ({0})", _modelSchemaOwner);
				return true;
			}

			if (! IgnoreNameFilters(datasetName))
			{
				if (! HasModelPrefix(datasetName))
				{
					reason = string.Format("prefix does not match ({0})", _modelDatasetPrefix);
					return true;
				}

				if (_datasetFilter != null &&
				    _datasetFilter.Exclude(datasetName, out reason))
				{
					reason = string.Format("excluded by filter criteria: {0}", reason);
					return true;
				}
			}

			bool isUnregisteredTable = DatasetListBuilderUtils.IsUnregisteredTable(datasetName);

			if (_ignoreUnregisteredTables && isUnregisteredTable)
			{
				reason = "not registered with geodatabase";
				return true;
			}

			if (_ignoreUnversionedDatasets &&
			    DatasetListBuilderUtils.SupportsVersioning(workspace) &&
			    ! NeverRequireVersioned(datasetName) &&
			    (isUnregisteredTable || ! DatasetListBuilderUtils.IsVersioned(datasetName)))
			{
				reason = "not registered as versioned";
				return true;
			}

			if (isUnregisteredTable)
			{
				string errorMessage;
				if (! DatasetListBuilderUtils.CanOpenDataset(datasetName, out errorMessage))
				{
					reason = string.Format(
						"Unable to open unregistered table: {0}; skipping dataset",
						errorMessage);
					return true;
				}
			}

			reason = string.Empty;
			return false;
		}

		public void UseDataset(IDatasetName datasetName)
		{
			Dataset dataset = CreateDataset(datasetName);

			if (dataset != null)
			{
				_datasets.Add(dataset);
			}
		}

		public void AddDatasets(DdxModel model)
		{
			foreach (Dataset dataset in _datasets)
			{
				_msg.InfoFormat("Including dataset {0} in model", dataset.Name);

				model.AddDataset(dataset);
			}

			AddDatasetsCore(model);
		}

		protected virtual void AddDatasetsCore(DdxModel model) { }

		#endregion

		private static bool NeverRequireVersioned([NotNull] IDatasetName datasetName)
		{
			if (datasetName is IMosaicDatasetName)
			{
				return true;
			}

			if (datasetName is IRasterDatasetName)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Gets the unqualified name of the table based on the name part without the model prefix
		/// </summary>
		/// <param name="datasetNameWithoutPrefix">The dataset name without prefix 
		/// (e.g. "ERRORS_ROW")</param>
		/// <returns>The unqualified, but otherwise complete table name 
		/// (e.g. "TLM_ERRORS_ROW")</returns>
		protected string AddPrefix([NotNull] string datasetNameWithoutPrefix)
		{
			if (string.IsNullOrEmpty(_modelDatasetPrefix))
			{
				return datasetNameWithoutPrefix;
			}

			return $"{_modelDatasetPrefix}{datasetNameWithoutPrefix}";
		}

		[CanBeNull]
		protected abstract VectorDataset CreateVectorDataset([NotNull] IDatasetName datasetName,
		                                                     [NotNull] string name);

		[CanBeNull]
		protected abstract TableDataset CreateTableDataset([NotNull] IDatasetName datasetName,
		                                                   [NotNull] string name);

		[CanBeNull]
		protected abstract TopologyDataset CreateTopologyDataset([NotNull] string name);

		[CanBeNull]
		protected abstract RasterMosaicDataset CreateRasterMosaicDataset([NotNull] string name);

		[CanBeNull]
		protected abstract RasterDataset CreateRasterDataset([NotNull] string name);

		private bool IgnoreNameFilters([NotNull] IDatasetName datasetName)
		{
			string tableName = DatasetUtils.GetTableName(datasetName);

			if (! _typeMap.TryGetValue(tableName, out Type type))
			{
				return false;
			}

			if (type == null)
			{
				return false;
			}

			if (typeof(IErrorDataset).IsAssignableFrom(type))
			{
				// don't apply name filter to error datasets
				return true;
			}

			return false;
		}

		[NotNull]
		protected string GetModelElementName([NotNull] string dbElementName)
		{
			return UnqualifyDatasetName
				       ? ModelElementNameUtils.GetUnqualifiedName(dbElementName)
				       : dbElementName;
		}

		[CanBeNull]
		private Dataset CreateDataset([NotNull] IDatasetName datasetName)
		{
			string reason;
			if (IgnoreDataset(datasetName, out reason))
			{
				_msg.WarnFormat("Ignoring dataset {0}: {1}", datasetName.Name, reason);
				return null;
			}

			if (datasetName is IFeatureClassName featureClassName)
			{
				if (featureClassName.ShapeType != esriGeometryType.esriGeometryAny)
				{
					// Note: A raster in a geodatabase is IFeatureClassName with ShapeType -1!
					// -> Get actual dataset
					var dataset = (IDataset) ((IName) datasetName).Open();

					if (dataset.Type == esriDatasetType.esriDTRasterCatalog)
					{
						return GetRasterDataset(datasetName.Name);
					}
				}

				return GetVectorDataset(featureClassName);
			}

#if ArcGIS || ARCGIS_11_0_OR_GREATER
			if (datasetName is ITopologyName)
			{
				return GetTopologyDataset(datasetName);
			}
#endif
			if (datasetName is ITableName)
			{
				return GetTableDataset(datasetName);
			}

			if (datasetName is IMosaicDatasetName)
			{
				return GetRasterMosaicDataset(datasetName);
			}

			if (datasetName is IRasterDatasetName)
			{
				return GetRasterDataset(datasetName.Name);
			}

			return CreateDatasetCore(datasetName);
		}

		[CanBeNull]
		protected virtual Dataset CreateDatasetCore([NotNull] IDatasetName datasetName)
		{
			return null;
		}

		private bool HasModelPrefix([NotNull] IDatasetName datasetName)
		{
			string prefix = _modelDatasetPrefix;

			if (string.IsNullOrEmpty(prefix))
			{
				return true;
			}

			string name = DatasetUtils.GetTableName(datasetName);

			return name.StartsWith(prefix,
			                       StringComparison.InvariantCultureIgnoreCase);
		}

		[CanBeNull]
		protected T GetGeometryType<T>() where T : GeometryType
		{
			return _geometryTypeConfigurator.GetGeometryType<T>();
		}

		[CanBeNull]
		protected GeometryTypeShape GetGeometryType(esriGeometryType esriGeometryType)
		{
			return _geometryTypeConfigurator.GetGeometryType(esriGeometryType);
		}

		[CanBeNull]
		private VectorDataset GetVectorDataset([NotNull] IFeatureClassName featureClassName)
		{
			Assert.ArgumentNotNull(featureClassName, nameof(featureClassName));

			esriGeometryType shapeType = DatasetUtils.GetShapeType(featureClassName);

			GeometryTypeShape geometryType = GetGeometryType(shapeType);

			if (geometryType == null)
			{
				// unknown shape type -> ignore dataset
				_msg.WarnFormat(
					"Ignoring dataset {0}: geometry shape type ({1}) not defined in data dictionary",
					((IDatasetName) featureClassName).Name, shapeType);
				return null;
			}

			var datasetName = (IDatasetName) featureClassName;

			string tableName = DatasetUtils.GetTableName(datasetName);

			VectorDataset vectorDataset;
			Type type;
			if (_typeMap.TryGetValue(tableName, out type))
			{
				if (type == null)
				{
					// ignore
					return null;
				}

				object dataset = Activator.CreateInstance(type, GetModelElementName(datasetName));

				vectorDataset = dataset as VectorDataset;
				Assert.NotNull(vectorDataset,
				               string.Format(
					               "Unexpected dataset type: {0}; Expected: Vector Dataset",
					               type.Name));
			}
			else
			{
				vectorDataset = CreateVectorDataset(datasetName, GetModelElementName(datasetName));
			}

			if (vectorDataset == null)
			{
				return null;
			}

			vectorDataset.GeometryType = geometryType;

			return vectorDataset;
		}

		[CanBeNull]
		private TableDataset GetTableDataset([NotNull] IDatasetName datasetName)
		{
			string tableName = DatasetUtils.GetTableName(datasetName);

			GeometryType geometryType = GetGeometryType<GeometryTypeNoGeometry>();

			if (geometryType == null)
			{
				// unknown geometry type -> ignore dataset
				_msg.WarnFormat(
					"Ignoring dataset {0}: no-geometry geometry type not defined in data dictionary",
					datasetName.Name);
				return null;
			}

			TableDataset tableDataset;
			Type type;
			if (_typeMap.TryGetValue(tableName, out type))
			{
				if (type == null)
				{
					// ignore
					return null;
				}

				object dataset = Activator.CreateInstance(type, GetModelElementName(datasetName));

				tableDataset = dataset as TableDataset;
				Assert.NotNull(tableDataset,
				               string.Format(
					               "Unexpected dataset type: {0}; Expected: Table Dataset",
					               type.Name));
			}
			else
			{
				tableDataset = CreateTableDataset(datasetName, GetModelElementName(datasetName));
			}

			if (tableDataset == null)
			{
				return null;
			}

			tableDataset.GeometryType = geometryType;

			return tableDataset;
		}

		[NotNull]
		protected string GetModelElementName([NotNull] IDatasetName datasetName)
		{
			return GetModelElementName(datasetName.Name);
		}

		[CanBeNull]
		private TopologyDataset GetTopologyDataset([NotNull] IDatasetName datasetName)
		{
			var geometryType = GetGeometryType<GeometryTypeTopology>();

			if (geometryType == null)
			{
				// the geometry type has not yet been defined in the data dictionary
				// -> ignore the dataset
				_msg.WarnFormat(
					"Ignoring dataset {0}: topology geometry type not defined in data dictionary",
					datasetName.Name);
				return null;
			}

			TopologyDataset dataset = CreateTopologyDataset(GetModelElementName(datasetName));

			if (dataset == null)
			{
				return null;
			}

			dataset.GeometryType = geometryType;

			return dataset;
		}

		[CanBeNull]
		private RasterMosaicDataset GetRasterMosaicDataset(
			[NotNull] IDatasetName datasetName)
		{
			var geometryType = GetGeometryType<GeometryTypeRasterMosaic>();

			if (geometryType == null)
			{
				// the geometry type has not yet been defined in the data dictionary
				// -> ignore the dataset
				_msg.WarnFormat(
					"Ignoring dataset {0}: raster mosaic geometry type not defined in data dictionary",
					datasetName.Name);
				return null;
			}

			RasterMosaicDataset dataset =
				CreateRasterMosaicDataset(GetModelElementName(datasetName));

			if (dataset == null)
			{
				return null;
			}

			dataset.GeometryType = geometryType;

			return dataset;
		}

		[CanBeNull]
		private RasterDataset GetRasterDataset([NotNull] string datasetName)
		{
			var geometryType = GetGeometryType<GeometryTypeRasterDataset>();

			if (geometryType == null)
			{
				// the geometry type has not yet been defined in the data dictionary
				// -> ignore the dataset
				_msg.WarnFormat(
					"Ignoring dataset {0}: raster dataset geometry type not defined in data dictionary",
					datasetName);

				return null;
			}

			RasterDataset dataset = CreateRasterDataset(GetModelElementName(datasetName));

			if (dataset == null)
			{
				return null;
			}

			dataset.GeometryType = geometryType;

			return dataset;
		}

		[NotNull]
		private IDictionary<string, Type> CreateTypeMap()
		{
			var result = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

			foreach (IssueDatasetName datasetName in _errorDatasetSchema.IssueDatasetNames)
			{
				result.Add(AddPrefix(datasetName.Name), datasetName.DatasetType);
			}

			AddDatasetTypesCore(result);

			return result;
		}

		protected virtual void AddDatasetTypesCore(IDictionary<string, Type> typeMap) { }
	}
}
