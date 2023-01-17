#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	public class SimpleDatasetOpener : IOpenDataset, IOpenAssociation
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IDatasetContext _datasetContext;

		public SimpleDatasetOpener([NotNull] IDatasetContext datasetContext)
		{
			_datasetContext = datasetContext;
		}

		#region IOpenDataset members

		public object OpenDataset(IDdxDataset dataset, Type knownType)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			if (knownType != null)
			{
				return OpenKnownDatasetType(dataset, knownType);
			}

			try
			{
				if (dataset is ObjectDataset objectDataset)
				{
					return _datasetContext.OpenObjectClass(objectDataset);
				}

				if (dataset is ISimpleTerrainDataset simpleTerrainDataset)
				{
					return _datasetContext.OpenTerrainReference(simpleTerrainDataset);
				}

				if (dataset is IRasterMosaicDataset simpleRasterMosaicDataset)
				{
					return _datasetContext.OpenSimpleRasterMosaic(simpleRasterMosaicDataset);
				}

				if (dataset is ITopologyDataset topologyDataset)
				{
					return _datasetContext.OpenTopology(topologyDataset);
				}

				if (dataset is IDdxRasterDataset rasterDataset)
				{
					return _datasetContext.OpenRasterDataset(rasterDataset);
				}

				return null;
			}
			catch (Exception e)
			{
				_msg.VerboseDebug(() => $"Error opening dataset {dataset.Name}", e);
				return null;
			}
		}

		public bool IsSupportedType(Type dataType)
		{
			Assert.ArgumentNotNull(dataType, nameof(dataType));

			if (typeof(IReadOnlyFeatureClass) == dataType)
				return true;

			if (typeof(IReadOnlyTable) == dataType)
				return true;

			if (typeof(IFeatureClass) == dataType)
				return true;

			if (typeof(ITable) == dataType)
				return true;

			if (typeof(ITopology) == dataType)
				return true;

			if (typeof(IMosaicDataset) == dataType)
				return true;

			if (typeof(IRasterDataset) == dataType)
				return true;

			if (typeof(IRasterDataset2) == dataType)
				return true;

			if (typeof(SimpleRasterMosaic) == dataType)
				return true;

			if (typeof(TerrainReference) == dataType)
				return true;

			return false;
		}

		#endregion

		private object OpenKnownDatasetType(IDdxDataset dataset, Type knownType)
		{
#if DEBUG
			if (typeof(IFeatureClass) == knownType ||
			    typeof(ITable) == knownType)
			{
				throw new AssertionException("Legacy type! Use IReadOnly interfaces.");
			}
#endif

			Assert.ArgumentNotNull(knownType, nameof(knownType));

			if (typeof(IFeatureClass) == knownType)
				return _datasetContext.OpenFeatureClass((IVectorDataset) dataset);
			if (typeof(IReadOnlyFeatureClass) == knownType)
			{
				IFeatureClass fc = _datasetContext.OpenFeatureClass((IVectorDataset) dataset);
				return fc != null ? ReadOnlyTableFactory.Create(fc) : null;
			}

			if (typeof(ITable) == knownType)
				return _datasetContext.OpenTable((IObjectDataset) dataset);
			if (typeof(IReadOnlyTable) == knownType)
			{
				ITable tbl = _datasetContext.OpenTable((IObjectDataset) dataset);
				return tbl != null ? ReadOnlyTableFactory.Create(tbl) : null;
			}

			if (typeof(ITopology) == knownType)
				return _datasetContext.OpenTopology((ITopologyDataset) dataset);

			if (typeof(IMosaicDataset) == knownType)
				return (IMosaicDataset) _datasetContext.OpenRasterDataset(
					(IDdxRasterDataset) dataset);

			if (typeof(IRasterDataset) == knownType)
				return _datasetContext.OpenRasterDataset((IDdxRasterDataset) dataset);

			if (typeof(IRasterDataset2) == knownType)
				return (IRasterDataset2) _datasetContext.OpenRasterDataset(
					(IDdxRasterDataset) dataset);

			if (typeof(SimpleRasterMosaic) == knownType)
				return _datasetContext.OpenSimpleRasterMosaic((IRasterMosaicDataset) dataset);

			if (typeof(TerrainReference) == knownType)
				return _datasetContext.OpenTerrainReference((ISimpleTerrainDataset) dataset);

			throw new ArgumentException($"Unsupported data type {knownType}");
		}

		#region IOpenAssociation members

		public string GetRelationshipClassName(string associationName,
		                                       Model model)
		{
			if (CanUseQueryTableContext(out IQueryTableContext queryTableContext))
			{
				return queryTableContext.GetRelationshipClassName(associationName, model);
			}

			IRelationshipClass relationshipClass = QueryTableUtils.OpenRelationshipClass(
				associationName, model, _datasetContext);

			return DatasetUtils.GetName(relationshipClass);
		}

		public IReadOnlyTable OpenQueryTable(Association association,
		                                     IList<IReadOnlyTable> tables,
		                                     JoinType joinType,
		                                     string whereClause = null)
		{
			Model model = (Model) association.Model;

			return OpenQueryTable(association.Name, model, tables, joinType, whereClause);
		}

		public IReadOnlyTable OpenQueryTable(string associationName,
		                                     Model model,
		                                     IList<IReadOnlyTable> tables,
		                                     JoinType joinType,
		                                     string whereClause = null)
		{
			if (CanUseQueryTableContext(out IQueryTableContext queryTableContext))
			{
				return QueryTableUtils.OpenQueryTable(associationName, model, tables,
				                                      queryTableContext, joinType, whereClause);
			}

			return QueryTableUtils.OpenAoQueryTable(associationName, model, tables,
			                                        _datasetContext, joinType, whereClause);
		}

		#endregion

		private bool CanUseQueryTableContext(out IQueryTableContext queryTableContext)
		{
			queryTableContext = _datasetContext as IQueryTableContext;

			bool result = queryTableContext != null &&
			              queryTableContext.CanOpenQueryTables();

			return result;
		}
	}
}
