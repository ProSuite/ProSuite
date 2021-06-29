#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
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
		private double? _cellSize;

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

		public double GetCellSize()
		{
			if (_cellSize == null)
			{
				_cellSize = DetermineCellSize();

				_msg.DebugFormat("Using cell size: {0}", _cellSize ?? double.NaN);
			}

			return _cellSize ?? double.NaN;
		}

		public IPolygon GetInterpolationDomain()
		{
			IFeatureClass boundary = _mosaicDataset.Boundary;

			var boundaryFeatures = GdbQueryUtils.GetFeatures(boundary, false).ToList();

			return (IPolygon) GeometryUtils.UnionFeatures(boundaryFeatures);
		}

		public IEnumerable<ISimpleRaster> GetSimpleRasters(double x, double y,
		                                                   double searchTolerance)
		{
			IFeatureClass rasterCatalog = _mosaicDataset.Catalog;

			ISpatialReference spatialReference = DatasetUtils.GetSpatialReference(rasterCatalog);

			Assert.NotNull(spatialReference,
			               $"Raster catalog {DatasetUtils.GetName(rasterCatalog)} has no spatial reference");

			searchTolerance += SpatialReferenceUtils.GetXyResolution(spatialReference);

			IEnvelope envelope = GeometryFactory.CreateEnvelope(
				x - searchTolerance, y - searchTolerance,
				x + searchTolerance, y + searchTolerance, spatialReference);

			return GetCatalogFeatures(rasterCatalog, envelope).Select(CreateSimpleRaster);
		}

		public ISimpleRaster GetSimpleRaster(double atX, double atY)
		{
			IFeatureClass rasterCatalog = _mosaicDataset.Catalog;

			IPoint searchGeometry = GeometryFactory.CreatePoint(
				atX, atY, DatasetUtils.GetSpatialReference(rasterCatalog));

			IFeature catalogFeature = GetCatalogFeature(rasterCatalog, searchGeometry);

			Marshal.ReleaseComObject(searchGeometry);
			Marshal.ReleaseComObject(rasterCatalog);

			if (catalogFeature == null)
			{
				return null;
			}

			ISimpleRaster result = CreateSimpleRaster(catalogFeature);

			Marshal.ReleaseComObject(catalogFeature);

			return result;
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

		private ISimpleRaster CreateSimpleRaster(IFeature catalogFeature)
		{
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
			IEnumerable<IFeature> orderedCatalogFeatures =
				GetCatalogFeatures(rasterCatalog, searchGeometry);

			return orderedCatalogFeatures.FirstOrDefault();
		}

		private IEnumerable<IFeature> GetCatalogFeatures([NotNull] IFeatureClass rasterCatalog,
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

			return orderedCatalogFeatures;
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

			string result = stringArray.Element[0];

			Marshal.ReleaseComObject(stringArray);

			return result;
		}

		private double? DetermineCellSize()
		{
			Stopwatch watch = _msg.DebugStartTiming();

			try
			{
				IFeatureClass rasterCatalog = _mosaicDataset.Catalog;

				string lowPSFieldName = _mosaicDataset.MosaicFunction.CellsizeFieldName;
				int lowPSFieldIndex = rasterCatalog.FindField(lowPSFieldName);

				if (lowPSFieldIndex < 0)
				{
					return null;
				}

				IQueryFilter queryFilter = new QueryFilterClass();
				queryFilter.SubFields = lowPSFieldName;

				foreach (IFeature feature in GdbQueryUtils.GetFeatures(
					rasterCatalog, queryFilter, true))
				{
					object obj = feature.get_Value(lowPSFieldIndex);

					if (DBNull.Value != obj)
					{
						return Convert.ToDouble(obj);
					}
				}

				return null;
			}
			catch (Exception e)
			{
				_msg.Debug($"Error getting cell size from {((IDataset) _mosaicDataset).Name}", e);

				return null;
			}
			finally
			{
				_msg.DebugStopTiming(watch, "Determined cell size of mosaic dataset");
			}
		}
	}
}
