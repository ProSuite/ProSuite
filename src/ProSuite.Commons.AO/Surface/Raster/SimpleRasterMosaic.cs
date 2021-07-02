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
	public class SimpleRasterMosaic : IRasterProvider, IDataset, IGeoDataset,
	                                  IEquatable<SimpleRasterMosaic>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private double? _cellSize;
		[CanBeNull] private IPolygon _interpolationDomain;

		// TODO: Consider supporting VRT file format directly or
		//       a polygon feature class with a raster path field and a boundary polygon

		public SimpleRasterMosaic(IMosaicDataset mosaicDataset,
		                          string mosaicRuleZOrderFieldName = null,
		                          bool mosaicRuleDescending = false)
		{
			if (string.IsNullOrEmpty(mosaicRuleZOrderFieldName))
			{
				esriMosaicMethod mosaicMethod = mosaicDataset.MosaicFunction.MosaicMethod;

				Assert.AreEqual(esriMosaicMethod.esriMosaicAttribute, mosaicMethod,
				                "Unsupported mosaic method. Currently the only mosaic method is by field order is supported.");

				MosaicRuleZOrderFieldName = mosaicDataset.MosaicFunction.OrderByFieldName;
				MosaicRuleDescending = ! mosaicDataset.MosaicFunction.Ascending;
			}
			else
			{
				MosaicRuleZOrderFieldName = mosaicRuleZOrderFieldName;
				MosaicRuleDescending = mosaicRuleDescending;
			}

			Name = ((IDataset) mosaicDataset).Name;
			FullName = ((IDataset) mosaicDataset).FullName;

			CellSizeFieldName = mosaicDataset.MosaicFunction.CellsizeFieldName;

			ItemPathsQuery = (IItemPathsQuery) mosaicDataset;

			BoundaryClass = mosaicDataset.Boundary;
			CatalogClass = mosaicDataset.Catalog;
		}

		[CanBeNull]
		private IFeatureClass BoundaryClass { get; }

		[NotNull]
		private IFeatureClass CatalogClass { get; }

		[CanBeNull]
		private string MosaicRuleZOrderFieldName { get; }

		private bool MosaicRuleDescending { get; }

		private string CellSizeFieldName { get; }

		[CanBeNull]
		private IItemPathsQuery ItemPathsQuery { get; }

		public double GetCellSize()
		{
			if (_cellSize == null)
			{
				_cellSize = DetermineCellSize();

				_msg.DebugFormat("Using cell size: {0}", _cellSize ?? double.NaN);
			}

			return _cellSize ?? double.NaN;
		}

		#region IRasterProvider members

		public IPolygon GetInterpolationDomain()
		{
			if (_interpolationDomain == null)
			{
				if (BoundaryClass != null)
				{
					var boundaryFeatures =
						GdbQueryUtils.GetFeatures(BoundaryClass, false).ToList();

					_interpolationDomain = (IPolygon) GeometryUtils.UnionFeatures(boundaryFeatures);
				}
				else
				{
					_msg.Debug("Boundary feature class is null. Unioning all catalog items...");

					var catalogFeatures = GdbQueryUtils.GetFeatures(CatalogClass, false).ToList();

					_interpolationDomain = (IPolygon) GeometryUtils.UnionFeatures(catalogFeatures);
				}
			}

			return _interpolationDomain;
		}

		public IEnumerable<ISimpleRaster> GetSimpleRasters(IEnvelope envelope)
		{
			return GetCatalogFeatures(CatalogClass, envelope).Select(CreateSimpleRaster);
		}

		public ISimpleRaster GetSimpleRaster(double atX, double atY)
		{
			IPoint searchGeometry = GeometryFactory.CreatePoint(
				atX, atY, DatasetUtils.GetSpatialReference(CatalogClass));

			IFeature catalogFeature = GetCatalogFeature(CatalogClass, searchGeometry);

			Marshal.ReleaseComObject(searchGeometry);

			if (catalogFeature == null)
			{
				return null;
			}

			ISimpleRaster result = CreateSimpleRaster(catalogFeature);

			Marshal.ReleaseComObject(catalogFeature);

			return result;
		}

		public void Dispose()
		{
			Marshal.ReleaseComObject(CatalogClass);

			if (BoundaryClass != null)
			{
				Marshal.ReleaseComObject(BoundaryClass);
			}

			if (_interpolationDomain != null)
			{
				Marshal.ReleaseComObject(_interpolationDomain);
				_interpolationDomain = null;
			}
		}

		#endregion

		#region IGeoDataset members

		public ISpatialReference SpatialReference => ((IGeoDataset) CatalogClass).SpatialReference;

		public IEnvelope Extent => BoundaryClass != null
			                           ? ((IGeoDataset) BoundaryClass).Extent
			                           : ((IGeoDataset) CatalogClass).Extent;

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

		public string Name { get; }

		public IName FullName { get; }

		public string BrowseName
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public esriDatasetType Type => esriDatasetType.esriDTMosaicDataset;

		public IWorkspace Workspace => DatasetUtils.GetWorkspace(CatalogClass);

		public string Category => throw new NotImplementedException();

		public IEnumDataset Subsets => throw new NotImplementedException();

		public IPropertySet PropertySet => throw new NotImplementedException();

		#endregion

		#region Equality members

		public bool Equals(SimpleRasterMosaic other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			var classComparer = new ObjectClassComparer(ObjectClassEquality.SameTableSameVersion);

			return Name == other.Name &&
			       classComparer.Equals(CatalogClass, other.CatalogClass) &&
			       classComparer.Equals(BoundaryClass, other.BoundaryClass) &&
			       MosaicRuleZOrderFieldName == other.MosaicRuleZOrderFieldName &&
			       MosaicRuleDescending == other.MosaicRuleDescending &&
			       CellSizeFieldName == other.CellSizeFieldName;
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

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((SimpleRasterMosaic) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (Name != null ? Name.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^
				           (BoundaryClass != null ? BoundaryClass.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ CatalogClass.GetHashCode();
				hashCode = (hashCode * 397) ^
				           (MosaicRuleZOrderFieldName != null
					            ? MosaicRuleZOrderFieldName.GetHashCode()
					            : 0);
				hashCode = (hashCode * 397) ^ MosaicRuleDescending.GetHashCode();
				hashCode = (hashCode * 397) ^
				           (CellSizeFieldName != null ? CellSizeFieldName.GetHashCode() : 0);
				return hashCode;
			}
		}

		#endregion

		private ISimpleRaster CreateSimpleRaster(IFeature catalogFeature)
		{
			string path;

			// Faster but requires Standard or Advanced license:
			try
			{
				path = GetPathByPathQuery(catalogFeature);

				if (path == null)
				{
					path = GetPathViaCatalogItemDataset(catalogFeature);
				}
			}
			catch (Exception e)
			{
				_msg.Debug("Error getting path. Trying different method...", e);

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

			// This could be more sophisticated (and slower) if there are non-rectangular catalog
			// features, which should probably be tested here.
			var orderedCatalogFeatures = GdbQueryUtils.GetFeatures(
				rasterCatalog, spatialFilter, false);

			if (! string.IsNullOrEmpty(MosaicRuleZOrderFieldName))
			{
				int fieldIndex = rasterCatalog.FindField(MosaicRuleZOrderFieldName);

				if (fieldIndex < 0)
				{
					throw new InvalidConfigurationException(
						$"Field {MosaicRuleZOrderFieldName} not found in {DatasetUtils.GetName(rasterCatalog)}");
				}

				Func<IFeature, object> getFieldValue = f => f.get_Value(fieldIndex);

				if (MosaicRuleDescending)
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

		[CanBeNull]
		private string GetPathByPathQuery(IFeature catalogFeature)
		{
			if (ItemPathsQuery == null)
			{
				return null;
			}

			if (ItemPathsQuery.QueryPathsParameters == null)
			{
				ItemPathsQuery.QueryPathsParameters = new QueryPathsParametersClass();
			}

			IStringArray stringArray = ItemPathsQuery.GetItemPaths(catalogFeature);

			string result = stringArray.Element[0];

			Marshal.ReleaseComObject(stringArray);

			return result;
		}

		private double? DetermineCellSize()
		{
			Stopwatch watch = _msg.DebugStartTiming();

			if (string.IsNullOrEmpty(CellSizeFieldName))
			{
				return null;
			}

			try
			{
				IFeatureClass rasterCatalog = CatalogClass;

				string lowPSFieldName = CellSizeFieldName;
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
				_msg.Debug($"Error getting cell size from {Name}", e);

				return null;
			}
			finally
			{
				_msg.DebugStopTiming(watch, "Determined cell size of mosaic dataset");
			}
		}
	}
}
