#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Text;
using Path = System.IO.Path;

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class MosaicUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string _zOrderFieldName = "ZORDER";

		public static string ZOrderFieldName => _zOrderFieldName;

		/// <summary>
		/// Returns the RasterCatalogItems from the Catalog feature class of the specified mosaic dataset.
		/// </summary>
		/// <param name="mosaicDataset"></param>
		/// <param name="filter"></param>
		/// <param name="recycle"></param>
		/// <returns></returns>
		public static IEnumerable<IFeature> GetCatalogItems(
			[NotNull] IMosaicDataset mosaicDataset,
			[NotNull] IQueryFilter filter,
			bool recycle = false)
		{
			Assert.ArgumentNotNull(mosaicDataset, nameof(mosaicDataset));
			Assert.ArgumentNotNull(filter, nameof(filter));

			return GdbQueryUtils.GetFeatures(mosaicDataset.Catalog, filter, recycle);
		}

		[NotNull]
		public static IList<string> DeleteItemsFromMosaic(
			[NotNull] IMosaicDataset mosaicDataset,
			[NotNull] IEnvelope extent,
			[CanBeNull] int? exceptObjectId = null)
		{
			// TODO: Polygon rather than Extent! Generally use polygon from raster boundary??
			Assert.ArgumentNotNull(mosaicDataset, nameof(mosaicDataset));
			Assert.ArgumentNotNull(extent, nameof(extent));

			IQueryFilter spatialFilter =
				GdbQueryUtils.CreateSpatialFilter(mosaicDataset.Catalog, extent,
				                                  esriSpatialRelEnum
					                                  .esriSpatialRelContains);

			if (exceptObjectId != null)
			{
				spatialFilter.WhereClause = string.Format(
					"{0} <> {1}", mosaicDataset.Catalog.OIDFieldName, exceptObjectId);
			}

			return DeleteItemsFromMosaic(mosaicDataset, spatialFilter);
		}

		public static string DeleteItemFromMosaic(
			[NotNull] IMosaicDataset mosaicDataset, int footprintOid)
		{
			Assert.ArgumentNotNull(mosaicDataset, nameof(mosaicDataset));

			_msg.DebugFormat("Deleting footprint with object id {0}", footprintOid);

			string deletedItemPath;
			try
			{
				IQueryFilter filter = new QueryFilterClass();
				filter.WhereClause = string.Format("{0}={1}",
				                                   mosaicDataset.Catalog.OIDFieldName,
				                                   footprintOid);

				List<IFeature> catalogItems =
					GetCatalogItems(mosaicDataset, filter).ToList();

				int nameFieldIdx =
					DatasetUtils.GetFieldIndex(mosaicDataset.Catalog, "NAME");

				Assert.AreEqual(1, catalogItems.Count,
				                "Unexpected number of footprints with OID {0}: {1}",
				                footprintOid,
				                catalogItems.Count);

				deletedItemPath = DeleteFootprint(catalogItems[0], nameFieldIdx);
			}
			catch (Exception e)
			{
				_msg.Debug("Error deleting item from mosaic", e);
				throw;
			}

			_msg.DebugFormat("Removed raster from mosaic dataset: {0}", deletedItemPath);

			return deletedItemPath;
		}

		/// <summary>
		/// Deletes the catalog items conforming to the specified filter. The actual rasters are not deleted.
		/// </summary>
		/// <param name="mosaicDataset"></param>
		/// <param name="spatialFilter"></param>
		/// <returns></returns>
		[NotNull]
		public static IList<string> DeleteItemsFromMosaic(
			[NotNull] IMosaicDataset mosaicDataset,
			[NotNull] IQueryFilter spatialFilter)
		{
			Assert.ArgumentNotNull(mosaicDataset, nameof(mosaicDataset));
			Assert.ArgumentNotNull(spatialFilter, nameof(spatialFilter));

			var deletedItemsPath = new List<string>();

			try
			{
				int nameFieldIdx =
					DatasetUtils.GetFieldIndex(mosaicDataset.Catalog, "NAME");

				IEnumerable<IFeature> catalogItems =
					GetCatalogItems(mosaicDataset, spatialFilter);

				foreach (IFeature feature in catalogItems)
				{
					string rasterName = DeleteFootprint(feature, nameFieldIdx);

					deletedItemsPath.Add(rasterName);
				}
			}
			catch (Exception e)
			{
				_msg.Debug("Error deleting item from mosaic", e);
				throw;
			}

			_msg.DebugFormat("Removed rasters from mosaic dataset: {0}",
			                 StringUtils.Concatenate(deletedItemsPath, ", "));

			return deletedItemsPath;

			// This only works if the mosaic dataset is not registered as versioned (The GP Tool has the same bug)
			// TODO: Report to ESRI Inc.

			//IRemoveItemsParameters2 removeItemsParams = new RemoveItemsParametersClass();
			//((ISelectionParameters) removeItemsParams).QueryFilter = filter;

			//removeItemsParams.RemoveItem = true;
			//removeItemsParams.RemoveUnreferencedInstances = true;

			//((IMosaicDatasetOperationParameters2)removeItemsParams).PrepareResults = true;

			//IGdbTransaction transaction = new GdbTransaction();

			//transaction.Execute(((IDataset) mosaicDataset).Workspace,
			//					() =>
			//					((IMosaicDatasetOperation2) mosaicDataset).RemoveItems(
			//						removeItemsParams, null),
			//					"Add raster to mosaic");

			//IPropertySet resultSet = ((IMosaicDatasetOperationParameters2) removeItemsParams).Results;

			//IDictionary<string, object> dict = PropertySetUtils.GetDictionary(resultSet);
		}

		public static int AddRasterToMosaic(
			[NotNull] IMosaicDataset mosaicDataset,
			[NotNull] string rasterPath,
			double minCellSizeFactor, double maxCellSizeFactor,
			[CanBeNull] IPropertySet auxFieldValues = null)
		{
			IList<int> added = AddRastersToMosaic(mosaicDataset, rasterPath,
			                                      minCellSizeFactor, maxCellSizeFactor,
			                                      auxFieldValues);

			Assert.AreEqual(1, added.Count,
			                "Unexpected number of rasters added to mosaic: {0}. Make sure the raster {1} exists and can be accessed.",
			                added.Count, rasterPath);

			return added[0];
		}

		public static bool IsCoveredByValidFootprints(
			[NotNull] IMosaicDataset mosaicDataset,
			[NotNull] IGeometry area,
			[CanBeNull] string whereClause = null,
			[CanBeNull] NotificationCollection notifications = null)
		{
			var boundary = (IPolygon) mosaicDataset.BoundaryGeometry;

			if (boundary == null)
			{
				return false;
			}

			if (! GeometryUtils.Contains(boundary, area))
			{
				NotificationUtils.Add(notifications,
				                      "The requested extent is not fully within the mosaic boundary");

				return false;
			}

			IFeatureClass footprintClass = mosaicDataset.Catalog;

			IQueryFilter spatialFilter =
				GdbQueryUtils.CreateSpatialFilter(footprintClass, area);

			if (! string.IsNullOrEmpty(whereClause))
			{
				spatialFilter.WhereClause = whereClause;
			}

			var geometries = new List<IGeometry>();
			IEnumerable<IFeature> footprints =
				GetItemsWithGeometries(footprintClass, spatialFilter, geometries);

			List<int> brokenMosaicItems = GetMosaicItemsWithBrokenPaths(mosaicDataset,
				footprints);

			Marshal.ReleaseComObject(footprintClass);

			if (brokenMosaicItems.Count > 0)
			{
				NotificationUtils.Add(notifications,
				                      "There are invalid mosaic items in the requested extent <Footprint OIDs>: {0}",
				                      StringUtils.Concatenate(brokenMosaicItems, ", "));
				return false;
			}

			// Extra check on footprints (boundary could theoretically be different from footprints or not up-to-date):
			_msg.DebugFormat("Checking {0} footprints whether they cover the AOI.",
			                 geometries.Count);

			IGeometry unionedFootprints = GeometryUtils.UnionGeometries(geometries);

			if (! GeometryUtils.Contains(unionedFootprints, area))
			{
				NotificationUtils.Add(notifications,
				                      "The requested area is not fully covered with DTM rasters");
				return false;
			}

			return true;
		}

		private static IEnumerable<IFeature> GetItemsWithGeometries(
			[NotNull] IFeatureClass footprintClass,
			[CanBeNull] IQueryFilter spatialFilter,
			[CanBeNull] List<IGeometry> shapes)
		{
			const bool recycle = true;
			foreach (
				IFeature feature in
				GdbQueryUtils.GetFeatures(footprintClass, spatialFilter, recycle))
			{
				shapes?.Add(feature.ShapeCopy);
				yield return feature;
			}
		}

		[NotNull]
		public static List<int> GetMosaicItemsWithBrokenPaths(
			[NotNull] IMosaicDataset mosaic,
			[CanBeNull] IQueryFilter filter = null)
		{
			const bool recycle = true;

			IFeatureClass footprintClass = mosaic.Catalog;
			IEnumerable<IFeature> footprints = GdbQueryUtils.GetFeatures(
				footprintClass, filter, recycle);

			List<int> oidsWithBrokenPaths =
				GetMosaicItemsWithBrokenPaths(mosaic, footprints);

			Marshal.ReleaseComObject(footprintClass);

			return oidsWithBrokenPaths;
		}

		[NotNull]
		public static List<int> GetMosaicItemsWithBrokenPaths(
			[NotNull] IMosaicDataset mosaic,
			[NotNull] IEnumerable<IFeature> footprints)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			var featureCount = 0;
			var oidsWithBrokenPaths = new List<int>();

			foreach (IFeature feature in footprints)
			{
				featureCount++;
				string rasterPath = QueryRasterPath(mosaic, feature);

				if (! File.Exists(rasterPath))
				{
					oidsWithBrokenPaths.Add(feature.OID);
				}
			}

			_msg.DebugStopTiming(
				watch, "Checked {0} footprints and found {1} broken path(s).",
				featureCount, oidsWithBrokenPaths.Count);
			return oidsWithBrokenPaths;
		}

		[NotNull]
		public static List<int> GetStaleAndInvalidMosaicItems(
			[NotNull] IMosaicDataset mosaic,
			[CanBeNull] IQueryFilter filter = null)
		{
			var failingItems = new List<int>();

			var result = GetStaleMosaicItems(mosaic, filter, failingItems);

			result.AddRange(failingItems);

			return result;
		}

		/// <summary>
		/// Gets the stale mosaic items. Do not use in conjunction with stereo analyst (TOP-4740).
		/// </summary>
		/// <param name="mosaic"></param>
		/// <param name="filter"></param>
		/// <param name="failingItems"></param>
		/// <returns></returns>
		[NotNull]
		public static List<int> GetStaleMosaicItems(
			[NotNull] IMosaicDataset mosaic,
			[CanBeNull] IQueryFilter filter = null,
			[CanBeNull] List<int> failingItems = null)
		{
			var mosaicOperation = (IMosaicDatasetOperation2) mosaic;

			var oidsWithBrokenPaths = new List<int>();

			const bool recycle = true;

			IFeatureClass footprintClass = mosaic.Catalog;

			foreach (
				IFeature feature in GdbQueryUtils.GetFeatures(
					footprintClass, filter, recycle))
			{
				try
				{
					if (mosaicOperation.IsStale(feature))
					{
						oidsWithBrokenPaths.Add(feature.OID);
					}
				}
				catch (Exception e)
				{
					_msg.Debug(string.Format("Error calling IsStale for feature {0}",
					                         GdbObjectUtils.ToString(feature)), e);

					if (failingItems != null)
					{
						failingItems.Add(feature.OID);
					}

					// Exception intentionally caught. IsStale is not reliable (TOP-4776)
				}
			}

			Marshal.ReleaseComObject(footprintClass);

			return oidsWithBrokenPaths;
		}

		/// <summary>
		/// Adds the rasters in the provided search path to the mosaic dataset. This method is not available
		/// in 10.0 because it uses the interfaces ISynchronizeParameters2, IMosaicDatasetOperationParameters2,
		/// which were added later.
		/// </summary>
		/// <param name="mosaicDataset"></param>
		/// <param name="searchPath"></param>
		/// <param name="minCellSizeFactor"></param>
		/// <param name="maxCellSizeFactor"></param>
		/// <param name="auxFieldValues"></param>
		/// <returns></returns>
		[NotNull]
		public static IList<int> AddRastersToMosaic(
			[NotNull] IMosaicDataset mosaicDataset,
			[NotNull] string searchPath,
			double minCellSizeFactor, double maxCellSizeFactor,
			[CanBeNull] IPropertySet auxFieldValues = null)
		{
			Assert.ArgumentNotNull(mosaicDataset, nameof(mosaicDataset));
			Assert.ArgumentNotNullOrEmpty(searchPath, nameof(searchPath));
			Assert.ArgumentCondition(minCellSizeFactor > 0,
			                         "Minimum cell size factor must be greater 0");
			Assert.ArgumentCondition(maxCellSizeFactor > 0,
			                         "Maximum cell size factor must be greater 0");

			IDataSourceCrawler fileCrawler = CreateFileCrawler(searchPath);

			var mosaicDatasetOperation = (IMosaicDatasetOperation) mosaicDataset;

			IAddRastersParameters addRastersArgs = new AddRastersParametersClass();
			addRastersArgs.Crawler = fileCrawler;

			// Specify the raster type to use to add the data.
			IRasterType rasterType = new RasterTypeClass();
			rasterType.RasterBuilder = new RasterDatasetBuilderClass();
			addRastersArgs.RasterType = rasterType;

			// NOTE: MinPS and MaxPS must be populated properly, otherwise no rasters are ever displayed in ArcMap
			// The GP Tool 'Calculate Cell Size' does not seem to work on versioned mosaic datasets
			// MinimumCellSizeFactor == 0 results in NULL value, the raster is never displayed in the mosaic

			((ISynchronizeParameters2) addRastersArgs).MinimumCellSizeFactor =
				minCellSizeFactor;

			((ISynchronizeParameters2) addRastersArgs).MaximumCellSizeFactor =
				maxCellSizeFactor;

			// Duplicates are not expected, but still:
			addRastersArgs.DuplicateItemsAction =
				esriDuplicateItemsAction.esriDuplicateItemsOverwrite;

			if (auxFieldValues != null)
			{
				((ISynchronizeParameters2) addRastersArgs).AuxiliaryFieldValues =
					auxFieldValues;
			}

			((IMosaicDatasetOperationParameters2) addRastersArgs).PrepareResults = true;

			_msg.DebugFormat("Adding raster(s) {0} to mosaic...", searchPath);

			mosaicDatasetOperation.AddRasters(addRastersArgs, null);

			IPropertySet resultSet =
				((IMosaicDatasetOperationParameters2) addRastersArgs).Results;
			IDictionary<string, object> dict = PropertySetUtils.GetDictionary(resultSet);

			const string fidSetKey = "FIDSet";
			var rasterIds = (IFIDSet) dict[fidSetKey];

			List<int> addedOIds = ToList(rasterIds);

			// Release everything that might prevent the mosaic dataset from a later deletion
			Marshal.ReleaseComObject(rasterIds);
			Marshal.ReleaseComObject(resultSet);
			Marshal.ReleaseComObject(rasterType.RasterBuilder);
			Marshal.ReleaseComObject(rasterType);
			Marshal.ReleaseComObject(fileCrawler);
			Marshal.ReleaseComObject(addRastersArgs);

			_msg.DebugFormat("Added mosaic dataset item(s) <OID>: {0}",
			                 StringUtils.Concatenate(addedOIds, ", "));

			return addedOIds;
		}

		public static IMosaicDataset CreateDtmMosaicDataset(
			IWorkspace workspace,
			string name,
			ISpatialReference spatialReference,
			string rastersPath,
			string configKeyword = null,
			double minCellSizeFactor = 0.001,
			double maxCellSizeFactor = 50)
		{
			const int bandCount = 1;
			const rstPixelType pixelType = rstPixelType.PT_FLOAT;

			ICreateMosaicDatasetParameters creationParams =
				new CreateMosaicDatasetParametersClass();

			creationParams.BandCount = bandCount;

			creationParams.PixelType = pixelType;

			IMosaicWorkspaceExtensionHelper mosaicExtHelper = new
				MosaicWorkspaceExtensionHelperClass
				();

			IMosaicWorkspaceExtension
				mosaicExt = mosaicExtHelper.FindExtension(workspace);

			IMosaicDataset mosaicDataset = mosaicExt.CreateMosaicDataset(
				name, spatialReference,
				creationParams,
				configKeyword);

			// Add the rasters - If another process is already editing, this will fail with 
			// System.Runtime.InteropServices.COMException (0x80004005): Cannot acquire a lock.
			AddRastersToMosaic(mosaicDataset, rastersPath, minCellSizeFactor,
			                   maxCellSizeFactor);

			return mosaicDataset;
		}

		[NotNull]
		public static IMosaicDataset CreateMosaicDataset(
			[NotNull] IWorkspace workspace,
			[NotNull] string name,
			[NotNull] IMosaicDataset templateDataset,
			[CanBeNull] string configKeyword = null)
		{
			ICreateMosaicDatasetParameters2 creationParams =
				new CreateMosaicDatasetParametersClass();

			creationParams.BandCount = 1;
			creationParams.PixelType = rstPixelType.PT_FLOAT;

			creationParams.TemplateMosaicDataset = templateDataset;

			IMosaicWorkspaceExtensionHelper mosaicExtHelper = new
				MosaicWorkspaceExtensionHelperClass
				();

			IMosaicWorkspaceExtension
				mosaicExt = mosaicExtHelper.FindExtension(workspace);

			ISpatialReference spatialReference =
				((IGeoDataset) templateDataset).SpatialReference;

			IMosaicDataset mosaicDataset = mosaicExt.CreateMosaicDataset(
				name, spatialReference,
				creationParams,
				configKeyword);

			foreach (
				KeyValuePair<string, object> keyValuePair in GetAllProperties(
					templateDataset))
			{
				SetKeyProperty(mosaicDataset, keyValuePair.Key, keyValuePair.Value);
			}

			return mosaicDataset;
		}

		/// <summary>
		/// Set the Key Property 'value' corresponding to the key 'key' on the dataset.
		///     Key Properties:
		///Key Properties of type 'double':
		///CloudCover
		///SunElevation
		///SunAzimuth
		///SensorElevation
		///SensorAzimuth
		///OffNadir
		///VerticalAccuracy
		///HorizontalAccuracy
		///LowCellSize
		///HighCellSize
		///MinCellSize
		///MaxCellSize
		///
		///Key Properties of type 'date':
		///AcquisitionDate
		///
		///Key Properties of type 'string':
		///SensorName
		///ParentRasterType
		///DataType
		///ProductName
		///DatasetTag
		/// </summary>
		/// <param name="mosaicDataset">Dataset to set the property on.</param>
		/// <param name="key">The key on which to set the property.</param>
		/// <param name="value">The value to set.</param>
		public static void SetKeyProperty([NotNull] IMosaicDataset mosaicDataset,
		                                  [NotNull] string key,
		                                  object value)
		{
			var rasterKeyProps = (IRasterKeyProperties) mosaicDataset;

			rasterKeyProps.SetProperty(key, value);
		}

		/// <summary>
		/// Get all the properties associated with the dataset.
		/// </summary>
		/// <param name="mosaicDataset">Dataset to get the property from.</param>
		private static IEnumerable<KeyValuePair<string, object>> GetAllProperties(
			[NotNull] IMosaicDataset mosaicDataset)
		{
			var rasterKeyProps = (IRasterKeyProperties) mosaicDataset;

			IStringArray keys;
			IVariantArray values;
			rasterKeyProps.GetAllProperties(out keys, out values);

			var result = new Dictionary<string, object>(keys.Count);

			for (var i = 0; i < keys.Count; i++)
			{
				result.Add(keys.Element[i], values.Element[i]);
			}

			return result;
		}

		/// <summary>
		/// Builds the mosaic dataset's boundary. A new feature will aways be created and any existing
		/// boundary feature(s) is/are deleted.
		/// </summary>
		/// <param name="mosaicDataset"></param>
		/// <param name="appendToExisting">From the GP tool help:
		/// Appends the perimeter of footprints to the existing boundary. This can save time when adding 
		/// additional raster data to the mosaic dataset, as the entire boundary will not be recalculated. 
		/// </param>
		public static void BuildBoundary([NotNull] IMosaicDataset mosaicDataset,
		                                 bool appendToExisting)
		{
			_msg.DebugFormat("Building Boundary");

			var datasetOperation = (IMosaicDatasetOperation) mosaicDataset;

			IBuildBoundaryParameters boundaryArgs = new BuildBoundaryParametersClass();

			boundaryArgs.AppendToExistingBoundary = appendToExisting;

			datasetOperation.BuildBoundary(boundaryArgs, null);

			Marshal.ReleaseComObject(boundaryArgs);
		}

		/// <summary>
		/// Extracts the raster path from the mosaic item by creating the raster dataset.
		/// For better performance, use QueryRasterPath
		/// </summary>
		/// <param name="mosaicItem"></param>
		/// <returns></returns>
		public static string GetRasterPath([NotNull] IFeature mosaicItem)
		{
			var rasterCatalogItem = (IRasterCatalogItem) mosaicItem;

			IRasterDataset rasterDataset = rasterCatalogItem.RasterDataset;

			var itemPaths = (IItemPaths) rasterDataset;

			IStringArray stringArray = itemPaths.GetPaths();

			Marshal.ReleaseComObject(rasterDataset);

			return GetSingleRasterPath(stringArray);
		}

		/// <summary>
		/// Gets the raster path from the mosaic item using IItemPathsQuery which apparently parses
		/// the information directly in the row. This is several orders of magnitude faster than
		/// <see cref="GetRasterPath"/>.
		/// </summary>
		/// <param name="mosaicDataset"></param>
		/// <param name="mosaicItem"></param>
		/// <returns></returns>
		public static string QueryRasterPath([NotNull] IMosaicDataset mosaicDataset,
		                                     [NotNull] IFeature mosaicItem)
		{
			var itemPathsQuery = (IItemPathsQuery) mosaicDataset;

			if (itemPathsQuery.QueryPathsParameters == null)
			{
				itemPathsQuery.QueryPathsParameters = new QueryPathsParametersClass();
			}

			IStringArray stringArray = itemPathsQuery.GetItemPaths(mosaicItem);

			return GetSingleRasterPath(stringArray);
		}

		public static IEnumerable<string> GetRasterFiles([NotNull] IFeature mosaicItem)
		{
			string sourcePath = GetRasterPath(mosaicItem);

			string sourceDirectory = Assert.NotNull(Path.GetDirectoryName(sourcePath));

			string rasterBaseName = Path.GetFileNameWithoutExtension(sourcePath);

			// This is a short cut and might not work for all kinds of rasters
			string searchPattern = $"{rasterBaseName}.*";

			_msg.DebugFormat("Getting raster files {0} from {1}", searchPattern,
			                 sourceDirectory);

			// EnumerateFiles only compiles in framework 4.0
			//return Directory.EnumerateFiles(sourceDirectory, searchPattern);

			return Directory.GetFiles(sourceDirectory, searchPattern);
		}

		public static void UpdateExtent([NotNull] string mosaicName,
		                                [NotNull] IFeatureWorkspace schemaOwnerWorkspace)
		{
			IMosaicDataset mosaicAsOwner = OpenMosaicDataset(schemaOwnerWorkspace,
			                                                 mosaicName);

			try
			{
				using (new SchemaLock((IDataset) mosaicAsOwner))
				{
					IEnumName children = mosaicAsOwner.Children;

					children.Reset();

					IName childName;
					while ((childName = children.Next()) != null)
					{
						object child = childName.Open();

						var featureClassManage = child as IFeatureClassManage;

						if (featureClassManage != null)
						{
							featureClassManage.UpdateExtent();
						}

						Marshal.ReleaseComObject(child);
					}
				}
			}
			finally
			{
				Marshal.ReleaseComObject(mosaicAsOwner);
			}
		}

		#region Consider moving to DatasetUtils and reference ESRI.ArcGIS.DatasourcesRaster in many projects...

		[NotNull]
		public static IMosaicDataset OpenMosaicDataset(
			[NotNull] IFeatureWorkspace featureWorkspace, [NotNull] string name)
		{
			return OpenMosaicDataset((IWorkspace) featureWorkspace, name);
		}

		[NotNull]
		public static IMosaicDataset OpenMosaicDataset([NotNull] IWorkspace workspace,
		                                               [NotNull] string name)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			IMosaicDataset result;

			try
			{
				IMosaicWorkspaceExtensionHelper mosaicExtHelper = new
					MosaicWorkspaceExtensionHelperClass();

				IMosaicWorkspaceExtension mosaicExt =
					mosaicExtHelper.FindExtension(workspace);

				result = mosaicExt.OpenMosaicDataset(name);
			}
			catch (Exception e)
			{
				_msg.Debug(
					string.Format("Error opening mosaic dataset {0} in workspace {1}",
					              name,
					              WorkspaceUtils.WorkspaceToString(workspace)), e);

				throw;
			}

			return result;
		}

		public static bool TryOpenMosaicDataset([NotNull] IWorkspace workspace,
		                                        [NotNull] string mosaicName,
		                                        [CanBeNull] out IMosaicDataset mosaicDataset,
		                                        [CanBeNull] out string notification)
		{
			mosaicDataset = null;
			notification = null;

			try
			{
				mosaicDataset = OpenMosaicDataset(workspace, mosaicName);
			}
			catch (COMException e)
			{
				if (e.ErrorCode == (int) fdoError.FDO_E_ITEM_NOT_FOUND)
				{
					notification =
						$"Mosaic {mosaicName} was not found in " +
						$"{WorkspaceUtils.WorkspaceToString(workspace)}.";

					return false;
				}

				_msg.Debug("Error opening mosaic.", e);

				notification =
					$"Cannot open mosaic {mosaicName} in workspace " +
					$"{WorkspaceUtils.WorkspaceToString(workspace)}: {e.Message}.";

				return false;
			}
			catch (FileNotFoundException e)
			{
				// FileNotFoundException has been observed if the .gdb directory is not a valid FGDB,
				// for example because it has been only partially deleted due to locks.
				_msg.Debug("Error opening mosaic.", e);

				notification =
					$"Cannot open mosaic {mosaicName} in workspace " +
					$"{WorkspaceUtils.WorkspaceToString(workspace)}. The workspace might be corrupt: {e.Message}.";

				return false;
			}
			catch (Exception e)
			{
				_msg.Debug("Error opening mosaic.", e);

				notification =
					$"Cannot open mosaic {mosaicName} in workspace " +
					$"{WorkspaceUtils.WorkspaceToString(workspace)}: {e.Message}.";

				return false;
			}

			return true;
		}

		[NotNull]
		public static IWorkspace GetWorkspace([NotNull] IMosaicDataset mosaicDataset)
		{
			Assert.ArgumentNotNull(mosaicDataset, nameof(mosaicDataset));

			return ((IDataset) mosaicDataset).Workspace;
		}

		[NotNull]
		public static string GetUnqualifiedName([NotNull] IMosaicDataset mosaicDataset)
		{
			Assert.ArgumentNotNull(mosaicDataset, nameof(mosaicDataset));

			return DatasetUtils.GetUnqualifiedName((IDataset) mosaicDataset);
		}

		#endregion

		private static string DeleteFootprint([NotNull] IFeature footprintFeature,
		                                      int nameFieldIdx)
		{
			// Consider testing the found rasters to ensure they are ok to be deleted...

			object nameObj = footprintFeature.Value[nameFieldIdx];

			string tileName = nameObj == DBNull.Value ? null : (string) nameObj;

			var catalogItem = (IRasterCatalogItem) footprintFeature;

			_msg.DebugFormat("Found mosaic dataset item to delete: {0} - {1}",
			                 tileName ?? Convert.ToString(footprintFeature.OID),
			                 catalogItem.RasterDataset.CompleteName);

			var rasterDataset = (IDataset) catalogItem.RasterDataset;

			string rasterName = rasterDataset.BrowseName;

			string deletedItemPath = rasterName;

			//// We cannot do this here:
			//rasterDataset.Delete();
			// because:
			// - We don't know whether other versions still reference this file
			// - It fails.  
			// Even releasing the feature and the feature class and reading the raster name directly
			// from the feature rather than referencing the RasterDataset does not help. The file is locked...
			// -> they need to be removed by a separate clean-up process

			footprintFeature.Delete();

			return deletedItemPath;
		}

		private static IDataSourceCrawler CreateFileCrawler(string searchPath,
		                                                    bool recurse = false,
		                                                    string filter = null)
		{
			IDataSourceCrawler result = new FileCrawlerClass();

			// Specify the source path.
			((IFileCrawler) result).Path = searchPath;

			// Specify whether to search subdirectories.
			((IFileCrawler) result).Recurse = recurse;

			// Specify a file filter.
			// result.Filter = ".TIF";

			if (! string.IsNullOrEmpty(filter))
			{
				result.Filter = filter;
			}

			return result;
		}

		private static List<int> ToList(IFIDSet idSet)
		{
			idSet.Reset();
			int oid;

			var addedOIds = new List<int>();

			idSet.Next(out oid);

			while (oid != -1)
			{
				addedOIds.Add(oid);
				idSet.Next(out oid);
			}

			return addedOIds;
		}

		private static string GetSingleRasterPath(IStringArray stringArray)
		{
			var allPaths = new List<string>(stringArray.Count);

			for (var i = 0; i < stringArray.Count; i++)
			{
				allPaths.Add(stringArray.Element[i]);
			}

			Assert.AreEqual(1, allPaths.Count, "Unexpected number of item paths: {0}",
			                StringUtils.Concatenate(allPaths, ", "));

			return allPaths[0];
		}

		/// <summary>
		/// Analyses the mosaic dataset with all the options enabled.
		/// </summary>
		/// <param name="mosaicDataset"></param>
		public static void Analyze([NotNull] IMosaicDataset mosaicDataset)
		{
			IAnalyzeParameters analyzeParameters = new AnalyzeParametersClass();

			analyzeParameters.AnalyzeDataSourceValidity = false;
			analyzeParameters.AnalyzeDatasetPaths = false;
			analyzeParameters.AnalyzeDatasets = false;
			analyzeParameters.AnalyzeFootprints = false;
			analyzeParameters.AnalyzeStaleItems = false;
			analyzeParameters.AnalyzeStatistics = false;
			analyzeParameters.AnalyzeVisibility = false;
			analyzeParameters.AnalyzeFunctions = false;
			analyzeParameters.AnalyzeKeyProperties = false;

			Analyze(mosaicDataset, analyzeParameters);

			Marshal.ReleaseComObject(analyzeParameters);
		}

		public static void Analyze([NotNull] IMosaicDataset mosaicDataset,
		                           [NotNull] IAnalyzeParameters analyzeParameters)
		{
			Assert.ArgumentNotNull(mosaicDataset, nameof(mosaicDataset));
			Assert.ArgumentNotNull(analyzeParameters, nameof(analyzeParameters));

			_msg.DebugFormat("Analysing mosaic dataset");

			var datasetOperation = (IMosaicDatasetOperation2) mosaicDataset;

			datasetOperation.Analyze(analyzeParameters, null);
		}

		public static void CalculateItemVisibility(IMosaicDataset mosaicDataset,
		                                           bool missingValuesOnly)
		{
			ICalculateCellSizeRangesParameters parameters =
				new CalculateCellSizeRangesParametersClass();

			parameters.UpdateMissingValuesOnly = missingValuesOnly;

			var datasetOperation = (IMosaicDatasetOperation2) mosaicDataset;

			datasetOperation.CalculateCellSizeRanges(parameters, null);
		}
	}
}
