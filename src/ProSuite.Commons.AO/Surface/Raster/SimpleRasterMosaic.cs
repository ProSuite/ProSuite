#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Surface.Raster
{
	public class SimpleRasterMosaic : IRasterProvider, IDataset, IGeoDataset
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IMosaicDataset _mosaicDataset;

		private readonly string _mosaicRuleZOrderField;
		private readonly bool _mosaicRuleDescending;

		// TODO: Consider supporting VRT file format directly or
		//       a polygon feature class with a raster path field and a boundary polygon

		public SimpleRasterMosaic(IMosaicDataset mosaicDataset,
		                          string mosaicRuleZOrderField = null,
		                          bool mosaicRuleDescending = false)
		{
			_mosaicDataset = mosaicDataset;

			_mosaicRuleZOrderField = mosaicRuleZOrderField;
			_mosaicRuleDescending = mosaicRuleDescending;
		}

		public double GetMaxCellSize()
		{
			// Should be tested and possibly done in a different way altogether
			return _mosaicDataset.MosaicFunction.MaximumVisibleCellsize.X;
		}

		public IPolygon GetInterpolationDomain()
		{
			IFeatureClass boundary = _mosaicDataset.Boundary;

			var boundaryFeatures = GdbQueryUtils.GetFeatures(boundary, false).ToList();

			return (IPolygon) GeometryUtils.UnionFeatures(boundaryFeatures);
		}

		public ISimpleRaster GetSimpleRaster(double atX, double atY)
		{
			IFeatureClass rasterCatalog = _mosaicDataset.Catalog;

			IPoint searchGeometry = GeometryFactory.CreatePoint(
				atX, atY, DatasetUtils.GetSpatialReference(rasterCatalog));

			IFeature catalogFeature = GetCatalogFeature(rasterCatalog, searchGeometry);

			if (catalogFeature == null)
			{
				return null;
			}

			string path;

			// Faster but requires Standard or Advanced license:
			try
			{
				path = GetPathByPathQuery(catalogFeature);
			}
			catch (Exception e)
			{
				_msg.Debug("Error getting path. Trying different method", e);

				// Slow but requires no Standard license:
				path = GetPathViaCatalogItemDataset(catalogFeature);
			}

			return new SimpleAoRaster(path);
		}

		[CanBeNull]
		private IFeature GetCatalogFeature([NotNull] IFeatureClass rasterCatalog,
		                                   [NotNull] IGeometry searchGeometry)
		{
			IQueryFilter spatialFilter =
				GdbQueryUtils.CreateSpatialFilter(rasterCatalog, searchGeometry);

			var orderedCatalogFeatures = GdbQueryUtils.GetFeatures(
				rasterCatalog, spatialFilter, false);

			if (! string.IsNullOrEmpty(_mosaicRuleZOrderField))
			{
				int fieldIndex = rasterCatalog.FindField(_mosaicRuleZOrderField);

				if (fieldIndex < 0)
				{
					throw new InvalidConfigurationException(
						$"Field {_mosaicRuleZOrderField} not found in {DatasetUtils.GetName(rasterCatalog)}");
				}

				Func<IFeature, object> getFieldValue = f => f.get_Value(fieldIndex);

				if (_mosaicRuleDescending)
				{
					orderedCatalogFeatures =
						orderedCatalogFeatures.OrderByDescending(getFieldValue);
				}
				else
				{
					orderedCatalogFeatures = orderedCatalogFeatures.OrderBy(getFieldValue);
				}
			}

			return orderedCatalogFeatures.FirstOrDefault();
		}

		private static string GetPathViaCatalogItemDataset(IFeature catalogFeature)
		{
			var rasterCatalogItem = (IRasterCatalogItem) catalogFeature;

			IRasterDataset rasterDataset = rasterCatalogItem.RasterDataset;

			var itemPaths = (IItemPaths) rasterDataset;
			IStringArray stringArray = itemPaths.GetPaths();

			return stringArray.Element[0];
		}

		private string GetPathByPathQuery(IFeature catalogFeature)
		{
			var itemPathsQuery = (IItemPathsQuery) _mosaicDataset;

			if (itemPathsQuery.QueryPathsParameters == null)
			{
				itemPathsQuery.QueryPathsParameters = new QueryPathsParametersClass();
			}

			IStringArray stringArray = itemPathsQuery.GetItemPaths(catalogFeature);

			return stringArray.Element[0];
		}

		public void Dispose() { }

		#region IGeoDataset members

		public ISpatialReference SpatialReference =>
			((IGeoDataset) _mosaicDataset).SpatialReference;

		public IEnvelope Extent => ((IGeoDataset) _mosaicDataset).Extent;

		#endregion

		#region IDataset members

		public bool CanCopy()
		{
			return false;
		}

		public IDataset Copy(string copyName, IWorkspace copyWorkspace)
		{
			throw new NotImplementedException();
		}

		public bool CanDelete()
		{
			return false;
		}

		public void Delete()
		{
			throw new NotImplementedException();
		}

		public bool CanRename()
		{
			return false;
		}

		public void Rename(string name)
		{
			throw new NotImplementedException();
		}

		public string Name => ((IDataset) _mosaicDataset).Name;

		public IName FullName => ((IDataset) _mosaicDataset).FullName;

		public string BrowseName
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public esriDatasetType Type => ((IDataset) _mosaicDataset).Type;

		public string Category => throw new NotImplementedException();

		public IEnumDataset Subsets => throw new NotImplementedException();

		public IWorkspace Workspace => ((IDataset) _mosaicDataset).Workspace;

		public IPropertySet PropertySet => ((IDataset) _mosaicDataset).PropertySet;

		#endregion
	}
}
