using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
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
		[NotNull]
		public static Dictionary<Table, List<long>> GetDistinctSelectionByTable(
			[NotNull] IEnumerable<BasicFeatureLayer> layers)
		{
			var result = new Dictionary<Table, SimpleSet<long>>();
			var distinctTableIds = new Dictionary<GdbTableIdentity, Table>();

			foreach (BasicFeatureLayer layer in layers.Where(LayerUtils.HasSelection))
			{
				IEnumerable<long> selection = layer.GetSelectionOids();

				Table table = layer.GetTable();

				if (table == null)
				{
					continue;
				}

				var tableId = new GdbTableIdentity(table);

				if (! distinctTableIds.ContainsKey(tableId))
				{
					distinctTableIds.Add(tableId, table);
					result.Add(table, new SimpleSet<long>(selection));
				}
				else
				{
					Table distinctTable = distinctTableIds[tableId];

					SimpleSet<long> ids = result[distinctTable];
					foreach (long id in selection)
					{
						ids.TryAdd(id);
					}
				}
			}

			return result.ToDictionary(pair => pair.Key, pair => pair.Value.ToList());
		}

		public static Dictionary<Geodatabase, List<Table>> GetDistinctTables(
			[NotNull] IEnumerable<BasicFeatureLayer> layers)
		{
			var result = new Dictionary<Geodatabase, SimpleSet<Table>>();

			foreach (Table table in layers.GetTables().Distinct())
			{
				var geodatabase = (Geodatabase) table.GetDatastore();

				if (! result.ContainsKey(geodatabase))
				{
					result.Add(geodatabase, new SimpleSet<Table> { table });
				}
				else
				{
					result[geodatabase].TryAdd(table);
				}
			}

			return result.ToDictionary(pair => pair.Key, pair => pair.Value.ToList());
		}

		public static IEnumerable<Feature> GetFeatures(
			[NotNull] Dictionary<MapMember, List<long>> oidsByMapMembers)
		{
			foreach (var oidsByMapMember in oidsByMapMembers)
			{
				var featureLayer = oidsByMapMember.Key as BasicFeatureLayer;

				if (featureLayer == null) continue;

				foreach (Feature feature in GetFeatures(featureLayer, oidsByMapMember.Value))
				{
					yield return feature;
				}
			}
		}

		public static IEnumerable<Feature> GetFeatures(
			[NotNull] IEnumerable<KeyValuePair<BasicFeatureLayer, List<long>>> oidsByMapMembers)
		{
			foreach (var selectionByLayer in oidsByMapMembers)
			{
				foreach (var feature in GetFeatures(selectionByLayer))
				{
					yield return feature;
				}
			}
		}

		public static IEnumerable<Feature> GetFeatures(
			KeyValuePair<BasicFeatureLayer, List<long>> layerOids)
		{
			BasicFeatureLayer layer = layerOids.Key;

			foreach (var feature in GetFeatures(layer, layerOids.Value))
			{
				yield return feature;
			}
		}

		public static IEnumerable<Feature> GetFeatures([NotNull] MapMember mapMember,
		                                               [NotNull] List<long> oidList,
		                                               bool recycling = false)
		{
			var basicFeatureLayer = mapMember as BasicFeatureLayer;

			if (basicFeatureLayer == null)
			{
				yield break;
			}

			foreach (Feature feature in GetFeatures(basicFeatureLayer, oidList, recycling))
			{
				yield return feature;
			}
		}

		private static IEnumerable<Feature> GetFeatures([CanBeNull] BasicFeatureLayer layer,
		                                                [NotNull] List<long> oids,
		                                                bool recycling = false)
		{
			if (layer == null)
			{
				yield break;
			}

			// TODO: Use layer search (there might habe been an issue with recycling?!)
			var featureClass = layer.GetTable();

			var filter = new QueryFilter
			             {
				             WhereClause =
					             $"{featureClass.GetDefinition().GetObjectIDField()} IN ({StringUtils.Concatenate(oids, ", ")})"
			             };

			filter.OutputSpatialReference = layer.GetSpatialReference();

			foreach (var feature in GdbQueryUtils.GetFeatures(featureClass, filter, recycling))
			{
				yield return feature;
			}
		}

		// todo daro: drop MapView from method signature, see GetLayers<T> below
		/// <summary>
		/// Finds the distinct visible features in the map that intersect the selected
		/// features and that fulfill the target-selection-type criteria.
		/// </summary>
		/// <param name="mapView">The map view.</param>
		/// <param name="intersectingSelectedFeatures">The selected features to use in the search for
		/// other visible features intersecting any of the selected features. When using target selection
		/// type SameClass these features are used to determine whether a potential target feature comes
		/// from the same class as one of them.</param>
		/// <param name="targetSelectionType">The target selection type. Must not be Undefined and must not be
		/// SelectedFeatures.</param>
		/// <param name="layerPredicate">An additional layer predicate to be tested.</param>
		/// <param name="extent">The area of interest to which the search can be limited</param>
		/// <param name="cancelableProgressor">The progress/cancel tracker.</param>
		/// <returns>The found features in the same spatial reference as the provided selected features</returns>
		[NotNull]
		public static IEnumerable<KeyValuePair<FeatureClass, List<Feature>>> FindFeatures(
			[NotNull] MapView mapView,
			[NotNull] Dictionary<MapMember, List<long>> intersectingSelectedFeatures,
			TargetFeatureSelection targetSelectionType,
			[CanBeNull] Predicate<FeatureLayer> layerPredicate = null,
			[CanBeNull] Envelope extent = null,
			[CanBeNull] CancelableProgressor cancelableProgressor = null)
		{
			Assert.ArgumentCondition(
				targetSelectionType != TargetFeatureSelection.SelectedFeatures &&
				targetSelectionType != TargetFeatureSelection.Undefined,
				"Unsupported target selection type");

			var selectedFeatures = GetFeatures(intersectingSelectedFeatures).ToList();

			var searchGeometry = GetSearchGeometry(selectedFeatures, extent);

			if (searchGeometry == null)
			{
				yield break;
			}

			foreach (var keyValuePair in FindFeatures(
				mapView, searchGeometry, SpatialRelationship.Intersects, targetSelectionType,
				layerPredicate, null, selectedFeatures, cancelableProgressor))
			{
				yield return keyValuePair;
			}
		}

		/// <summary>
		/// Finds the features in the map by the specified criteria, grouped by feature class
		/// </summary>
		/// <param name="mapView">The map view containing the layers to search</param>
		/// <param name="searchGeometry">The search geometry</param>
		/// <param name="spatialRelationship">The spatial relationship between the found features
		/// and the search geometry.</param>
		/// <param name="targetSelectionType">The target selection type that determines which layers
		/// are searched.</param>
		/// <param name="layerPredicate">An extra layer predicate that allows for a more
		/// fine-granular determination of the layers to be searched.</param>
		/// <param name="featurePredicate">An extra feature predicate that allows to determine
		/// criteria on the feature level.</param>
		/// <param name="selectedFeatures">The selected features, relevant only for
		/// <see cref="targetSelectionType"/> with value <see cref="TargetFeatureSelection.SameClass"/>. </param>
		/// <param name="cancelableProgressor"></param>
		/// <returns></returns>
		public static IEnumerable<KeyValuePair<FeatureClass, List<Feature>>> FindFeatures(
			[NotNull] MapView mapView,
			[NotNull] Geometry searchGeometry,
			SpatialRelationship spatialRelationship,
			TargetFeatureSelection targetSelectionType,
			[CanBeNull] Predicate<FeatureLayer> layerPredicate,
			[CanBeNull] Predicate<Feature> featurePredicate,
			List<Feature> selectedFeatures,
			CancelableProgressor cancelableProgressor = null)
		{
			// NOTE: FeatureLayer.Search is quite useless, as we cannot control recyclability and as soon as the cursor 
			//       is disposed, the feature's geometry is wrong!

			// -> Get the distinct feature classes (TODO: include layer definition queries)

			IEnumerable<FeatureLayer> featureLayers =
				GetLayers<FeatureLayer>(fl => IsLayerApplicable(fl, targetSelectionType, layerPredicate,
				                                                selectedFeatures));

			IEnumerable<IGrouping<IntPtr, FeatureLayer>> layersGroupedByClass =
				featureLayers.GroupBy(fl => fl.GetFeatureClass().Handle);

			foreach (var layersInClass in layersGroupedByClass)
			{
				// One query per distinct definition query, then make OIDs distinct

				FeatureClass featureClass = null;
				List<Feature> features = new List<Feature>();
				foreach (IGrouping<string, FeatureLayer> layers in layersInClass.GroupBy(
					fl => fl.DefinitionQuery))
				{
					if (cancelableProgressor != null
					    && cancelableProgressor.CancellationToken.IsCancellationRequested)
					{
						yield break;
					}

					featureClass = layers.First().GetFeatureClass();

					QueryFilter filter =
						GdbQueryUtils.CreateSpatialFilter(searchGeometry, spatialRelationship);
					filter.WhereClause = layers.Key;

					IEnumerable<Feature> foundFeatures = GdbQueryUtils
					                                     .GetFeatures(featureClass, filter, false)
					                                     .Where(f => featurePredicate == null ||
					                                                 featurePredicate(f));
					features.AddRange(foundFeatures);
				}

				if (featureClass != null && features.Count > 0)
				{
					yield return new KeyValuePair<FeatureClass, List<Feature>>(
						featureClass, features.DistinctBy(f => f.GetObjectID()).ToList());
				}
			}
		}

		public static IEnumerable<T> GetLayers<T>([CanBeNull] Predicate<T> layerPredicate)
			where T : Layer
		{
			MapView mapView = MapView.Active;

			if (mapView == null)
			{
				yield break;
			}

			foreach (Layer layer in mapView.Map.GetLayersAsFlattenedList())
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

		private static bool IsLayerApplicable(
			[CanBeNull] FeatureLayer featureLayer,
			TargetFeatureSelection targetSelectionType,
			[CanBeNull] Predicate<FeatureLayer> layerPredicate,
			[NotNull] ICollection<Feature> selectedFeatures)
		{
			if (featureLayer?.GetFeatureClass() == null)
			{
				return false;
			}

			if (layerPredicate != null && ! layerPredicate(featureLayer))
			{
				return false;
			}

			if ((targetSelectionType == TargetFeatureSelection.VisibleEditableFeatures ||
			     targetSelectionType == TargetFeatureSelection.VisibleSelectableEditableFeatures) &&
			    ! featureLayer.IsEditable)
			{
				return false;
			}

			if ((targetSelectionType == TargetFeatureSelection.VisibleSelectableFeatures ||
			     targetSelectionType == TargetFeatureSelection.VisibleSelectableEditableFeatures) &&
			    ! featureLayer.IsSelectable)
			{
				return false;
			}

			if (! featureLayer.IsVisible)
			{
				return false;
			}

			if (targetSelectionType == TargetFeatureSelection.SameClass &&
			    ! selectedFeatures.Any(
				    f => DatasetUtils.IsSameClass(f.GetTable(), featureLayer.GetTable())))
			{
				return false;
			}

			return true;
		}

		[CanBeNull]
		private static Geometry GetSearchGeometry(
			[NotNull] IList<Feature> intersectingFeatures,
			[CanBeNull] Envelope clipExtent)
		{
			var intersectingGeometries =
				GetSearchGeometries(intersectingFeatures, clipExtent);

			Geometry result = null;

			if (intersectingGeometries.Count != 0)
			{
				var sr = intersectingGeometries[0].SpatialReference;
				result = GeometryBagBuilder.CreateGeometryBag(intersectingGeometries, sr);
				//result = GeometryEngine.Instance.Union(intersectingGeometries);
			}

			return result;
		}

		/// <summary>
		///     Returns the list of geometries that can be used as spatial filter. Multipatches
		///     are translated into polygons, polycurves are clipped.
		/// </summary>
		/// <param name="features">The features.</param>
		/// <param name="clipExtent">The clip extent.</param>
		/// <returns></returns>
		[NotNull]
		private static IList<Geometry> GetSearchGeometries(
			[NotNull] ICollection<Feature> features,
			[CanBeNull] Envelope clipExtent)
		{
			var result = new List<Geometry>(features.Count);

			foreach (var geometry in GdbObjectUtils.GetGeometries(features))
			{
				if (clipExtent != null)
				{
					clipExtent =
						GeometryUtils.EnsureSpatialReference(clipExtent, geometry.SpatialReference);

					if (GeometryUtils.Disjoint(geometry, clipExtent))
					{
						continue;
					}
				}

				var multiPatch = geometry as Multipatch;

				// multipatches are not supported by ISpatialFilter (and neither are bags containing them)
				var polycurve = multiPatch != null
					                ? PolygonBuilder.CreatePolygon(
						                multiPatch
							                .Extent) // GeometryFactory.CreatePolygon(multiPatch)
					                : geometry as Multipart;

				if (polycurve != null)
				{
					// clipping is an optimization to pull less features from the db
					result.Add(clipExtent == null
						           ? polycurve
						           : GetClippedGeometry(polycurve, clipExtent));
				}
				else
				{
					// don't clip points etc.
					result.Add(geometry);
				}
			}

			return result;
		}

		[NotNull]
		private static Geometry GetClippedGeometry(
			[NotNull] Multipart polycurve,
			[NotNull] Envelope clipExtent)
		{
			Geometry clippedGeometry;

			if (GeometryUtils.Contains(clipExtent, polycurve))
			{
				return GeometryFactory.Clone(polycurve);
			}

			if (polycurve.GeometryType == GeometryType.Polygon)
			{
				clippedGeometry =
					GeometryUtils.GetClippedPolygon((Polygon) polycurve, clipExtent);
			}
			else
			{
				clippedGeometry = GeometryUtils.GetClippedPolyline((Polyline) polycurve,
					clipExtent);
			}

			return clippedGeometry;
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

				return PolygonBuilder.CreatePolygon(mapPoints, mapView.Camera.SpatialReference);
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

				return PolygonBuilder.CreatePolygon(screenPoints, mapView.Camera.SpatialReference);
			}

			// The screen is probably the entire screen
			var screenPoint = mapView.MapToScreen(mapGeometry.Extent.Center);

			// The client is probably the relevant map canvas, these coords seem to correspond
			// with the tool's mouse coordinates in SketchOutputMode.Screen!?!
			var clientPoint = mapView.ScreenToClient(screenPoint);

			return MapPointBuilder.CreateMapPoint(new Coordinate2D(clientPoint.X, clientPoint.Y));
		}

		/// <summary>
		/// Returns features filtered by spatial relationship. Honors definition queries on the layer. 
		/// </summary>
		public static IEnumerable<Feature> FilterLayerFeaturesByGeometry(
			BasicFeatureLayer layer, Geometry filterGeometry,
			SpatialRelationship spatialRelationship = SpatialRelationship.Intersects)
		{
			QueryFilter filter =
				GdbQueryUtils.CreateSpatialFilter(filterGeometry, spatialRelationship);

			var features = new List<Feature>();

			using (RowCursor rowCursor = layer.Search(filter))
			{
				while (rowCursor.MoveNext())
				{
					features.Add((Feature) rowCursor.Current);
				}
			}

			return features;
		}

		/// <summary>
		/// Returns oids of features filtered by spatial relationship. Honors definition queries on the layer. 
		/// </summary>
		public static IEnumerable<long> FilterLayerOidsByGeometry(
			BasicFeatureLayer layer, Geometry filterGeometry,
			SpatialRelationship spatialRelationship = SpatialRelationship.Intersects)
		{
			QueryFilter filter =
				GdbQueryUtils.CreateSpatialFilter(filterGeometry, spatialRelationship);

			var oids = new List<long>();

			using (RowCursor rowCursor = layer.Search(filter))
			{
				while (rowCursor.MoveNext())
				{
					oids.Add(rowCursor.Current.GetObjectID());
				}
			}

			return oids;
		}

		public static IEnumerable<long> GetFeaturesOidList(IEnumerable<Feature> features)
		{
			//List<long> oids = new List<long>();
			foreach (Feature feature in features)
			{
				//oids.Add(feature.GetObjectID());
				yield return feature.GetObjectID();
			}

			//return oids;
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

		public static bool HasSelection([CanBeNull] MapView mapView)
		{
			return mapView?.Map?.SelectionCount > 0;
		}

		public static IEnumerable<Table> Distinct(
			this IEnumerable<Table> tables)
		{
			return tables.Distinct(new TableComparer());
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
	}

	public class TableComparer : IEqualityComparer<Table>
	{
		public bool Equals(Table x, Table y)
		{
			if (ReferenceEquals(x, y))
			{
				// both null or reference equal
				return true;
			}

			if (x == null || y == null)
			{
				return false;
			}

			var left = new GdbTableIdentity(x);
			var right = new GdbTableIdentity(y);

			return Equals(left, right);
		}

		public int GetHashCode(Table obj)
		{
			return new GdbTableIdentity(obj).GetHashCode();
		}
	}

	public class BasicFeatureLayerComparer : IEqualityComparer<BasicFeatureLayer>
	{
		public bool Equals(BasicFeatureLayer x, BasicFeatureLayer y)
		{
			if (ReferenceEquals(x, y))
			{
				// both null or reference equal
				return true;
			}

			if (x == null || y == null)
			{
				return false;
			}

			var left = new GdbTableIdentity(x.GetTable());
			var right = new GdbTableIdentity(y.GetTable());

			return Equals(left, right);
		}

		public int GetHashCode(BasicFeatureLayer obj)
		{
			return new GdbTableIdentity(obj.GetTable()).GetHashCode();
		}
	}
}
