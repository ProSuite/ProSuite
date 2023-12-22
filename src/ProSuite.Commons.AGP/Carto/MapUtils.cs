using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.Carto
{
	public static class MapUtils
	{
		/// <summary>
		/// Asserts an active MapView.
		/// </summary>
		/// <returns>a not null map</returns>
		[NotNull]
		public static Map GetActiveMap()
		{
			MapView mapView = MapView.Active;
			Assert.NotNull(mapView, "no active MapView");

			return mapView.Map;
		}

		[NotNull]
		public static Uri GetMapUri(Project project, Map map)
		{
			Uri projectUri = new Uri(project.URI);
			Uri mapUri = new Uri(projectUri, map.URI);

			return mapUri;
		}

		public static Dictionary<Table, List<long>> GetDistinctSelectionByTable(
			Dictionary<MapMember, List<long>> oidsByLayer)
		{
			var result = new Dictionary<Table, SimpleSet<long>>();
			var distinctTableIds = new Dictionary<GdbTableIdentity, Table>();

			foreach (KeyValuePair<MapMember, List<long>> pair in oidsByLayer)
			{
				Table table = GetTable(pair.Key);

				var tableId = new GdbTableIdentity(table);

				if (! distinctTableIds.ContainsKey(tableId))
				{
					distinctTableIds.Add(tableId, table);
					result.Add(table, new SimpleSet<long>(pair.Value));
				}
				else
				{
					Table distinctTable = distinctTableIds[tableId];

					SimpleSet<long> ids = result[distinctTable];
					foreach (long id in pair.Value)
					{
						ids.TryAdd(id);
					}
				}
			}

			return result.ToDictionary(pair => pair.Key, pair => pair.Value.ToList());
		}

		[NotNull]
		public static Table GetTable<T>([NotNull] T mapMember) where T : MapMember
		{
			Assert.ArgumentNotNull(mapMember, nameof(mapMember));

			if (mapMember is BasicFeatureLayer basicFeatureLayer)
			{
				return Assert.NotNull(basicFeatureLayer.GetTable());
			}

			if (mapMember is StandaloneTable standaloneTable)
			{
				return Assert.NotNull(standaloneTable.GetTable());
			}

			throw new ArgumentException(
				$"{nameof(mapMember)} is not of type BasicFeatureLayer nor StandaloneTable");
		}

		public static IEnumerable<Feature> GetFeatures(
			[NotNull] SelectionSet selectionSet,
			[CanBeNull] SpatialReference outputSpatialReference = null)
		{
			return GetFeatures(selectionSet.ToDictionary(), outputSpatialReference);
		}

		public static IEnumerable<Feature> GetFeatures(
			[NotNull] IEnumerable<KeyValuePair<MapMember, List<long>>> oidsByMapMembers,
			[CanBeNull] SpatialReference outputSpatialReference = null)
		{
			foreach (var oidsByMapMember in oidsByMapMembers)
			{
				var featureLayer = oidsByMapMember.Key as BasicFeatureLayer;

				if (featureLayer == null) continue;

				foreach (Feature feature in GetFeatures(featureLayer, oidsByMapMember.Value,
				                                        false, outputSpatialReference))
				{
					yield return feature;
				}
			}
		}

		public static IEnumerable<Feature> GetFeatures(
			[NotNull] MapMember mapMember,
			[NotNull] List<long> oidList,
			bool recycling = false,
			[CanBeNull] SpatialReference outputSpatialReference = null)
		{
			var basicFeatureLayer = mapMember as BasicFeatureLayer;

			if (basicFeatureLayer == null)
			{
				yield break;
			}

			foreach (Feature feature in GetFeatures(basicFeatureLayer, oidList, recycling,
			                                        outputSpatialReference))
			{
				yield return feature;
			}
		}

		private static IEnumerable<Feature> GetFeatures(
			[CanBeNull] BasicFeatureLayer layer,
			[NotNull] List<long> oids,
			bool recycling = false,
			[CanBeNull] SpatialReference outputSpatialReference = null)
		{
			if (layer == null)
			{
				yield break;
			}

			// TODO: Use layer search (there might have been an issue with recycling?!)
			var featureClass = layer.GetTable();

			var filter = new QueryFilter
			             {
				             WhereClause =
					             $"{featureClass.GetDefinition().GetObjectIDField()} IN ({StringUtils.Concatenate(oids, ", ")})"
			             };

			// NOTE: The spatial reference of the layer is the same as the feature class rather than the map.
			filter.OutputSpatialReference = outputSpatialReference ?? layer.GetSpatialReference();

			foreach (var feature in GdbQueryUtils.GetFeatures(featureClass, filter, recycling))
			{
				yield return feature;
			}
		}

		public static IEnumerable<T> GetLayers<T>([CanBeNull] Predicate<T> layerPredicate,
		                                          [CanBeNull] MapView mapView = null)
			where T : Layer
		{
			if (mapView == null)
			{
				// Only take the active map if no other map has been provided.
				mapView = MapView.Active;
			}

			if (mapView == null)
			{
				yield break;
			}

			Map map = mapView.Map;

			foreach (T resultLayer in GetLayers(map, layerPredicate))
			{
				yield return resultLayer;
			}
		}

		public static IEnumerable<T> GetLayers<T>([NotNull] Map map,
		                                          [CanBeNull] Predicate<T> layerPredicate)
			where T : Layer
		{
			foreach (Layer layer in map.GetLayersAsFlattenedList())
			{
				var matchingTypeLayer = layer as T;

				if (matchingTypeLayer == null)
				{
					continue;
				}

				if (layerPredicate == null ||
				    layerPredicate(matchingTypeLayer))
				{
					yield return matchingTypeLayer;
				}
			}
		}

		/// <summary>
		/// Returns feature layers that contain a set of specified OIDs. This method can be used
		/// to filter out layers that have a restrictive definition query which potentially
		/// excludes the specified OIDs. These layers ca be used for flashing or zooming to the
		/// respective features.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="mapView"></param>
		/// <param name="layerPredicate"></param>
		/// <param name="objectIds"></param>
		/// <returns></returns>
		public static IEnumerable<T> GetFeatureLayersContainingOids<T>(
			[NotNull] MapView mapView,
			[NotNull] Predicate<T> layerPredicate,
			IReadOnlyList<long> objectIds) where T : BasicFeatureLayer
		{
			// NOTE: Flashing works fine on invisible layers, but not if there is a definition
			//       query that excludes the feature.

			var filteredLayers = new List<T>();

			foreach (T featureLayer in GetFeatureLayers(layerPredicate, mapView))
			{
				if (string.IsNullOrWhiteSpace(featureLayer.DefinitionQuery))
				{
					// Return the first layer without definition query:
					return new[] { featureLayer };
				}

				filteredLayers.Add(featureLayer);
			}

			var layersWithSomeOids = new List<T>();
			foreach (T restrictedLayer in filteredLayers)
			{
				var queryFilter = new QueryFilter
				                  {
					                  ObjectIDs = objectIds
				                  };

				int foundCount = LayerUtils.SearchObjectIds(restrictedLayer, queryFilter).Count();

				if (objectIds.Count == foundCount)
				{
					return new[] { restrictedLayer };
				}

				if (foundCount > 0)
				{
					layersWithSomeOids.Add(restrictedLayer);
				}
			}

			return layersWithSomeOids;
		}

		public static IEnumerable<IDisplayTable> GetFeatureLayersForSelection<T>(
			[CanBeNull] FeatureClass featureClass,
			[CanBeNull] MapView mapView = null) where T : BasicFeatureLayer
		{
			// TODO: WorkspaceEquality.SameVersion
			Predicate<T> sameTablePredicate =
				l => DatasetUtils.IsSameTable(l.GetFeatureClass(), featureClass);

			mapView ??= MapView.Active;

			return GetFeatureLayersForSelection(mapView, sameTablePredicate);
		}

		/// <summary>
		/// Gets the first visible, selectable feature layer without definition query.
		/// If all visible, selectable layers have a definition query, all layers are yielded.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="mapView"></param>
		/// <param name="layerPredicate">The layer predicate</param>
		/// <returns></returns>
		public static IEnumerable<T> GetFeatureLayersForSelection<T>(
			[NotNull] MapView mapView,
			[NotNull] Predicate<T> layerPredicate) where T : BasicFeatureLayer
		{
			var filteredVisibleSelectableLayers = new List<T>();

			foreach (T featureLayer in GetFeatureLayers<T>(
				         l => LayerUtils.IsVisible(l) && l.IsSelectable, mapView))
			{
				if (! layerPredicate(featureLayer))
				{
					continue;
				}

				if (string.IsNullOrWhiteSpace(featureLayer.DefinitionQuery))
				{
					// Return the first layer without definition query:
					// TODO: Consider favoring editable layers, if there are several.
					return new List<T> { featureLayer };
				}

				// TODO: Check if it references a relquery table -> skip them
				filteredVisibleSelectableLayers.Add(featureLayer);
			}

			// TODO: Exclude joined layers?
			return filteredVisibleSelectableLayers;
		}

		/// <summary>
		/// Gets the first selectable stand-alone table without definition query.
		/// If all selectable stand-alone tables have a definition query, all tables are yielded.
		/// </summary>
		/// <param name="mapView"></param>
		/// <param name="tablePredicate"></param>
		/// <returns></returns>
		public static IEnumerable<IDisplayTable> GetStandaloneTablesForSelection(
			[NotNull] MapView mapView,
			[NotNull] Predicate<StandaloneTable> tablePredicate)
		{
			var filteredSelectableLayers = new List<StandaloneTable>();

			foreach (StandaloneTable standaloneTable in
			         GetStandaloneTables(tablePredicate, mapView)
				         .Where(st => st != null))
			{
				if (! standaloneTable.IsSelectable)
				{
					continue;
				}

				if (string.IsNullOrWhiteSpace(standaloneTable.DefinitionQuery))
				{
					// Return just the first layer without definition query:
					return new List<StandaloneTable> { standaloneTable };
				}

				filteredSelectableLayers.Add(standaloneTable);
			}

			return filteredSelectableLayers;
		}

		public static IEnumerable<T> GetFeatureLayers<T>(
			[CanBeNull] Predicate<T> layerPredicate,
			[CanBeNull] MapView mapView = null,
			bool includeInvalid = false) where T : BasicFeatureLayer
		{
			Predicate<T> combinedPredicate;

			if (includeInvalid)
			{
				combinedPredicate = layerPredicate;
			}
			else
			{
				// Check for validity first because in most cases the specified layerPredicate
				// uses the FeatureClass name etc. which results in null-pointers if evaluated first.
				combinedPredicate = l =>
					LayerUtils.IsLayerValid(l) && (layerPredicate == null || layerPredicate(l));
			}

			foreach (T basicFeatureLayer in GetLayers(combinedPredicate, mapView))
			{
				yield return basicFeatureLayer;
			}
		}

		[CanBeNull]
		public static BasicFeatureLayer GetFeatureLayer(
			[CanBeNull] string featureClassName,
			[CanBeNull] MapView mapView = null)
		{
			return GetFeatureLayers<BasicFeatureLayer>(
				lyr => string.Equals(lyr.GetFeatureClass().GetName(),
				                     featureClassName,
				                     StringComparison.OrdinalIgnoreCase), mapView).FirstOrDefault();
		}

		public static IEnumerable<StandaloneTable> GetStandaloneTables(
			[CanBeNull] Predicate<StandaloneTable> tablePredicate,
			[CanBeNull] MapView mapView = null,
			bool includeInvalid = false)
		{
			if (mapView == null)
			{
				// Only take the active map if no other map has been provided.
				mapView = MapView.Active;
			}

			if (mapView == null)
			{
				yield break;
			}

			Predicate<StandaloneTable> combinedPredicate;

			if (includeInvalid)
			{
				combinedPredicate = tablePredicate;
			}
			else
			{
				// Check for validity first because in most cases the specified tablePredicate
				// uses the Table name etc. which results in null-pointers if evaluated first.
				combinedPredicate = t =>
					StandaloneTableUtils.IsStandaloneTableValid(t) &&
					(tablePredicate == null || tablePredicate(t));
			}

			foreach (StandaloneTable table in mapView.Map.GetStandaloneTablesAsFlattenedList())
			{
				if (combinedPredicate == null || combinedPredicate(table))
				{
					yield return table;
				}
			}
		}

		[CanBeNull]
		public static StandaloneTable GetStandaloneTable(
			[CanBeNull] string tableName,
			[CanBeNull] MapView mapView = null)
		{
			return GetStandaloneTables(
					table => string.Equals(table.GetTable().GetName(),
					                       tableName,
					                       StringComparison.OrdinalIgnoreCase), mapView)
				.FirstOrDefault();
		}

		public static Geometry ToMapGeometry(MapView mapView,
		                                     Polygon screenGeometry)
		{
			// TODO: ensure single-part, linear segments

			if (screenGeometry.Extent.Width > 0 || screenGeometry.Extent.Height > 0)
			{
				var mapPoints = new List<MapPoint>();
				foreach (var screenPoint in screenGeometry.Points)
				{
					var mapPoint = mapView.ScreenToMap(new Point(screenPoint.X, screenPoint.Y));

					mapPoints.Add(mapPoint);
				}

				return PolygonBuilderEx.CreatePolygon(mapPoints, mapView.Camera.SpatialReference);
			}

			return mapView.ScreenToMap(new Point(screenGeometry.Extent.XMin,
			                                     screenGeometry.Extent.YMin));
		}

		public static Geometry ToScreenGeometry(
			MapView mapView, Polygon mapGeometry)
		{
			// TODO: ensure single-part, linear segments

			var screenPoints = new List<Coordinate2D>();

			if (mapGeometry.Extent.Width > 0 || mapGeometry.Extent.Height > 0)
			{
				foreach (var mapPoint in mapGeometry.Points)
				{
					var screenVertex = mapView.MapToScreen(mapPoint);

					screenPoints.Add(new Coordinate2D(screenVertex.X, screenVertex.Y));
				}

				return PolygonBuilderEx.CreatePolygon(screenPoints,
				                                      mapView.Camera.SpatialReference);
			}

			// The screen is probably the entire screen
			var screenPoint = mapView.MapToScreen(mapGeometry.Extent.Center);

			// The client is probably the relevant map canvas, these coords seem to correspond
			// with the tool's mouse coordinates in SketchOutputMode.Screen!?!
			var clientPoint = mapView.ScreenToClient(screenPoint);

			return MapPointBuilderEx.CreateMapPoint(new Coordinate2D(clientPoint.X, clientPoint.Y));
		}

		public static double ConvertScreenPixelToMapLength(int pixels)
		{
			var mapExtent = MapView.Active.Map.GetDefaultExtent();
			var mapPoint = mapExtent.Center;
			//Map center as screen point
			var screenPoint = MapView.Active.MapToScreen(mapPoint);
			//Add tolerance pixels to get a "radius".
			var radiusScreenPoint =
				new Point(screenPoint.X + pixels, screenPoint.Y);
			var radiusMapPoint = MapView.Active.ScreenToMap(radiusScreenPoint);
			return GeometryEngine.Instance.Distance(mapPoint, radiusMapPoint);
		}

		/// <summary>
		/// Zooms a map to a given envelope, applying an expansion factor and making sure the resulting
		/// scale denominator is not larger than a given minimum denominator.
		/// </summary>
		/// <param name="mapView">The map view to zoom.</param>
		/// <param name="extent">The extent to zoom to.</param>
		/// <param name="expansionFactor">The expansion factor to apply to the extent. An expansion
		/// factor of 1.1 enlarges the extent by 10% in both x and y</param>
		/// <param name="minimumScale">The minimum scale denominator.</param>
		public static async Task<bool> ZoomToAsync([NotNull] MapView mapView,
		                                           [NotNull] Envelope extent,
		                                           double expansionFactor,
		                                           double minimumScale)
		{
			Assert.ArgumentNotNull(mapView, nameof(mapView));
			Assert.ArgumentNotNull(extent, nameof(extent));
			Assert.ArgumentCondition(! extent.IsEmpty, "extent must not be empty");

			Envelope newExtent = extent;
			if (expansionFactor > 0 && Math.Abs(expansionFactor - 1) > double.Epsilon)
			{
				newExtent = GeometryFactory.CreateEnvelope(extent, expansionFactor);
			}

			Map map = mapView.Map;
			var newExtentMap =
				GeometryUtils.EnsureSpatialReference(newExtent, map.SpatialReference);

			Envelope currentExtent = mapView.Extent;
			double currentScale = mapView.Camera.Scale;

			Envelope zoomExtent = GetZoomExtent(newExtentMap, currentExtent,
			                                    currentScale, minimumScale);

			await mapView.ZoomToAsync(zoomExtent);

			return true;
		}

		public static bool HasSelection([CanBeNull] MapView mapView)
		{
			return mapView?.Map?.SelectionCount > 0;
		}

		public static IEnumerable<BasicFeatureLayer> Distinct(
			this IEnumerable<BasicFeatureLayer> layers)
		{
			return layers.Distinct(new BasicFeatureLayerComparer());
		}

		public static async Task<bool> FlashGeometryAsync(
			[NotNull] MapView mapView,
			[NotNull] Geometry geometry,
			CIMSymbolReference symbolReference,
			int milliseconds = 400)
		{
			return await FlashGeometryAsync(mapView, new Overlay(geometry, symbolReference),
			                                milliseconds);
		}

		public static async Task<bool> FlashGeometryAsync(
			[NotNull] MapView mapView,
			[NotNull] Overlay overlay,
			int milliseconds = 400)
		{
			using (await overlay.AddToMapAsync(mapView))
			{
				await Task.Delay(milliseconds);
			}

			return true;
		}

		public static async Task<bool> FlashGeometriesAsync(
			[NotNull] MapView mapView,
			IEnumerable<Overlay> overlays,
			int milliseconds = 400)
		{
			List<IDisposable> disposables = new List<IDisposable>();

			try
			{
				foreach (Overlay overlay in overlays)
				{
					disposables.Add(await overlay.AddToMapAsync(mapView));
				}

				await Task.Delay(milliseconds);
			}
			finally
			{
				foreach (IDisposable disposable in disposables)
				{
					disposable.Dispose();
				}
			}

			return true;
		}

		public static bool FlashGeometries(
			[NotNull] MapView mapView,
			IEnumerable<Overlay> overlays,
			int milliseconds = 400)
		{
			List<IDisposable> disposables = new List<IDisposable>();

			try
			{
				foreach (Overlay overlay in overlays)
				{
					disposables.Add(overlay.AddToMap(mapView));
				}

				Thread.Sleep(milliseconds);
			}
			finally
			{
				foreach (IDisposable disposable in disposables)
				{
					disposable.Dispose();
				}
			}

			return true;
		}

		[NotNull]
		public static IEnumerable<string> GetUri(Map map, [NotNull] string mapMemberName)
		{
			Assert.ArgumentNotNull(mapMemberName, nameof(mapMemberName));

			MapView mapView = MapView.Active;

			// todo daro What if mapMember is map itself? Can it be found with this method?
			return mapView == null
				       ? Enumerable.Empty<string>()
				       : map.FindLayers(mapMemberName).Select(GetUri);
		}

		[NotNull]
		public static string GetUri([NotNull] MapMember mapMember)
		{
			return mapMember.URI;
		}

		public static IEnumerable<Layer> FindLayers([NotNull] string name,
		                                            bool recursive = true)
		{
			Assert.ArgumentNotNull(name, nameof(name));

			MapView mapView = MapView.Active;

			return mapView == null
				       ? Enumerable.Empty<Layer>()
				       : mapView.Map.FindLayers(name, recursive);
		}

		[CanBeNull]
		public static Layer GetLayer([NotNull] string uri, bool recursive = true)
		{
			Assert.ArgumentNotNull(uri, nameof(uri));

			MapView mapView = MapView.Active;

			return mapView.Map.FindLayer(uri, recursive);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="map"></param>
		/// <remarks>Doesn't throw an exception if there is no map</remarks>
		/// <returns></returns>
		public static IEnumerable<T> GetLayers<T>([CanBeNull] this Map map) where T : Layer
		{
			return map == null ? Enumerable.Empty<T>() : map.GetLayersAsFlattenedList().OfType<T>();
		}

		[NotNull]
		private static Envelope GetZoomExtent([NotNull] Envelope newExtent,
		                                      [NotNull] Envelope currentExtent,
		                                      double currentScale,
		                                      double minimumScale)
		{
			Assert.ArgumentNotNull(newExtent, nameof(newExtent));
			Assert.ArgumentNotNull(currentExtent, nameof(currentExtent));

			if (double.IsNaN(currentScale))
			{
				return newExtent;
			}

			if (currentScale < minimumScale)
			{
				// if the user zoomed in to below the minimum scale manually,
				// allow that scale to be maintained
				minimumScale = currentScale;
			}

			double minWidth = currentExtent.Width * (minimumScale / currentScale);
			double minHeight = currentExtent.Height * (minimumScale / currentScale);

			double width = newExtent.Width;
			double height = newExtent.Height;

			if (width >= minWidth && height >= minHeight)
			{
				return newExtent;
			}

			MapPoint mapPoint = GeometryUtils.Centroid(newExtent);

			double newWidth = width < minWidth
				                  ? minWidth
				                  : width;
			double newHeight = height < minHeight
				                   ? minHeight
				                   : height;

			return GeometryFactory.CreateEnvelope(mapPoint,
			                                      newWidth, newHeight);
		}
	}
}
