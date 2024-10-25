using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core.UnitFormats;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using UnitType = ArcGIS.Core.Geometry.UnitType;

namespace ProSuite.Commons.AGP.Carto
{
	// Note: MapUtils MUST NEVER use MapView.Active (always pass a Map instance as (the first) argument)

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

		#region Conversions

		public static double GetMapUnitsPerPoint(this Map map)
		{
			if (map is null)
				throw new ArgumentNullException(nameof(map));

			double referenceScale = map.ReferenceScale;
			if (! (referenceScale > 0))
				throw new InvalidOperationException(
					"Map has no ReferenceScale; cannot convert between points and map units");
			// TODO or use MapView's current scale instead?

			var unit = GetMapUnit(map, UnitType.Linear);
			return unit.GetUnitsPerPoint(referenceScale);
		}

		public static double GetPointsPerMapUnit(this Map map)
		{
			if (map is null)
				throw new ArgumentNullException(nameof(map));

			double referenceScale = map.ReferenceScale;
			if (! (referenceScale > 0))
				throw new InvalidOperationException(
					"Map has no ReferenceScale; cannot convert between points and map units");
			// TODO use MapView's current scale instead?

			var unit = GetMapUnit(map, UnitType.Linear);
			return unit.GetPointsPerUnit(referenceScale);
		}

		private static Unit GetMapUnit(Map map, UnitType? requiredType = null)
		{
			if (map is null)
				throw new ArgumentNullException(nameof(map));

			var sref = map.SpatialReference ??
			           throw new InvalidOperationException("Map has no spatial reference");

			var unit = sref.Unit ??
			           throw new InvalidOperationException("Map's spatial reference has no unit");

			if (requiredType.HasValue && unit.UnitType != requiredType)
			{
				throw new InvalidOperationException(
					$"Map's spatial reference units ({unit.Name}) are not of type {requiredType}");
			}

			return unit;
		}

		#endregion

		public static Dictionary<Table, List<long>> GetDistinctSelectionByTable(
			Dictionary<MapMember, List<long>> oidsByLayer)
		{
			var result = new Dictionary<Table, SimpleSet<long>>();
			var distinctTableIds = new Dictionary<GdbTableIdentity, Table>();

			foreach (KeyValuePair<MapMember, List<long>> pair in oidsByLayer)
			{
				Table table = DatasetUtils.GetDatabaseTable(GetTable(pair.Key));

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
		public static Table GetTable([NotNull] MapMember mapMember)
		{
			Assert.ArgumentNotNull(mapMember, nameof(mapMember));

			if (mapMember is IDisplayTable displayTable)
			{
				Table table = displayTable.GetTable();
				if (table == null)
				{
					throw new InvalidOperationException(
						$"Layer {mapMember.Name} is invalid has no table");
				}

				return table;
			}

			throw new ArgumentException(
				$"{nameof(mapMember)} is not of type BasicFeatureLayer nor StandaloneTable");
		}

		public static IEnumerable<Table> GetTables(IEnumerable<MapMember> mapMembers)
		{
			foreach (MapMember mapMember in mapMembers)
			{
				if (mapMember is BasicFeatureLayer basicFeatureLayer)
				{
					//Note: Invalid layers have null tables
					Table table = basicFeatureLayer.GetTable();
					if (table != null)
					{
						yield return table;
					}
				}

				if (mapMember is StandaloneTable standaloneTable)
				{
					Table table = standaloneTable.GetTable();
					if (table != null)
					{
						yield return table;
					}
				}
			}
		}

		public static IEnumerable<Feature> GetFeatures(
			[NotNull] SelectionSet selectionSet,
			[CanBeNull] SpatialReference outputSpatialReference = null)
		{
			return GetFeatures(selectionSet.ToDictionary(), false, outputSpatialReference);
		}

		public static IEnumerable<Feature> GetFeatures(
			[NotNull] IEnumerable<KeyValuePair<MapMember, List<long>>> oidsByMapMembers,
			bool withoutJoins = false,
			[CanBeNull] SpatialReference outputSpatialReference = null)
		{
			foreach (var oidsByMapMember in oidsByMapMembers)
			{
				var featureLayer = oidsByMapMember.Key as BasicFeatureLayer;

				if (featureLayer == null) continue;

				foreach (Feature feature in GetFeatures(featureLayer, oidsByMapMember.Value,
				                                        withoutJoins, recycling: false,
				                                        outputSpatialReference))
				{
					yield return feature;
				}
			}
		}

		public static IEnumerable<Feature> GetFeatures(
			[NotNull] IEnumerable<KeyValuePair<FeatureClass, List<long>>> oidsByTable,
			bool withoutJoins = false,
			[CanBeNull] SpatialReference outputSpatialReference = null)
		{
			foreach ((FeatureClass featureClass, List<long> oids) in oidsByTable)
			{
				if (featureClass == null) continue;

				foreach (Feature feature in GetFeatures(featureClass, oids,
				                                        withoutJoins, recycling: false,
				                                        outputSpatialReference))
				{
					yield return feature;
				}
			}
		}

		/// <summary>
		/// Loads the features for the specified object ids from the mapMember's feature class.
		/// </summary>
		/// <param name="mapMember">The layer</param>
		/// <param name="oids"></param>
		/// <param name="withoutJoins">Whether the features shall be retrieved from the un-joined
		/// feature class even if the layer has a join.</param>
		/// <param name="recycling"></param>
		/// <param name="outputSpatialReference"></param>
		/// <returns></returns>
		public static IEnumerable<Feature> GetFeatures(
			[NotNull] MapMember mapMember,
			[NotNull] IEnumerable<long> oids,
			bool withoutJoins,
			bool recycling = false,
			[CanBeNull] SpatialReference outputSpatialReference = null)
		{
			var basicFeatureLayer = mapMember as BasicFeatureLayer;

			if (basicFeatureLayer == null)
			{
				yield break;
			}

			FeatureClass featureClass = basicFeatureLayer.GetFeatureClass();

			foreach (Feature feature in GetFeatures(featureClass, oids, withoutJoins,
			                                        recycling,
			                                        outputSpatialReference))
			{
				yield return feature;
			}
		}

		public static IEnumerable<Feature> GetFeatures(
			[CanBeNull] FeatureClass featureClass,
			[NotNull] IEnumerable<long> oids,
			bool withoutJoin,
			bool recycling,
			[CanBeNull] SpatialReference outputSpatialReference = null)
		{
			if (featureClass == null)
			{
				yield break;
			}

			if (featureClass.IsJoinedTable() && withoutJoin)
			{
				// Get the features only based on the feature class, otherwise storing results in NotImplementedExceptions
				featureClass = GetUnJoinedFeatureClass(featureClass);
			}

			// TODO: Split by 1000 OIDs to avoid too large queries
			var filter = new QueryFilter
			             {
				             WhereClause =
					             $"{featureClass.GetDefinition().GetObjectIDField()} IN ({StringUtils.Concatenate(oids, ", ")})"
			             };

			// NOTE: The spatial reference of the layer is the same as the feature class rather than the map.
			filter.OutputSpatialReference =
				outputSpatialReference ?? featureClass.GetSpatialReference();

			foreach (var feature in GdbQueryUtils.GetFeatures(featureClass, filter, recycling))
			{
				yield return feature;
			}
		}

		public static IEnumerable<T> GetLayers<T>(
			Map map, Predicate<T> layerPredicate = null) where T : Layer
		{
			if (map is null) return Enumerable.Empty<T>();
			return map.GetLayersAsFlattenedList()
			   .OfType<T>()
			   .Where(l => layerPredicate is null || layerPredicate(l));
		}

		/// <summary>
		/// Returns feature layers and stand-alone tables that contain a set of specified OIDs.
		/// This method can be used to filter layers that have a restrictive definition query
		/// which potentially excludes the specified OIDs. These layers ca be used for flashing
		/// or zooming to the respective features or selecting the respective rows.
		/// </summary>
		public static IEnumerable<T> GetDisplayTablesContainingOids<T>(
			[NotNull] IEnumerable<MapMember> mapMembers,
			[NotNull] Predicate<T> predicate,
			IReadOnlyList<long> objectIds) where T : class, IDisplayTable
		{
			// NOTE: Flashing works fine on invisible layers, but not if there is a definition
			//       query that excludes the feature.

			var filteredLayers = new List<T>();

			foreach (T displayTable in GetDisplayTables(mapMembers, predicate))
			{
				string definitionQuery = null;
				if (displayTable is BasicFeatureLayer featureLayer)
				{
					definitionQuery = featureLayer.DefinitionQuery;
				}
				else if (displayTable is StandaloneTable standaloneTable)
				{
					definitionQuery = standaloneTable.DefinitionQuery;
				}

				if (string.IsNullOrWhiteSpace(definitionQuery))
				{
					// Return the first layer without definition query:
					return new[] { displayTable };
				}

				filteredLayers.Add(displayTable);
			}

			var layersWithSomeOids = new List<T>();
			foreach (T restrictedLayer in filteredLayers)
			{
				var queryFilter = new QueryFilter
				                  {
					                  ObjectIDs = objectIds,
					                  // OID will be ensured in SearchObjectIds:
					                  SubFields = string.Empty
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
			[NotNull] Map map,
			[CanBeNull] FeatureClass featureClass) where T : BasicFeatureLayer
		{
			// TODO: WorkspaceEquality.SameVersion
			Predicate<T> sameTablePredicate =
				l => DatasetUtils.IsSameTable(l.GetFeatureClass(), featureClass);

			return GetFeatureLayersForSelection(map, sameTablePredicate);
		}

		/// <summary>
		/// Gets the first visible, selectable feature layer without definition query.
		/// If all visible, selectable layers have a definition query, all layers are yielded.
		/// </summary>
		public static IEnumerable<T> GetFeatureLayersForSelection<T>(
			[NotNull] Map map,
			[NotNull] Predicate<T> layerPredicate) where T : BasicFeatureLayer
		{
			var filteredVisibleSelectableLayers = new List<T>();

			foreach (T featureLayer in GetFeatureLayers<T>(
				         map,
				         l => LayerUtils.IsVisible(l) && l.IsSelectable))
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
		public static IEnumerable<StandaloneTable> GetStandaloneTablesForSelection(
			[NotNull] Map map,
			[NotNull] Predicate<StandaloneTable> tablePredicate)
		{
			var filteredSelectableLayers = new List<StandaloneTable>();

			foreach (StandaloneTable standaloneTable in
			         GetStandaloneTables(map, tablePredicate)
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

		public static IEnumerable<T> GetDisplayTables<T>(
			[NotNull] IEnumerable<MapMember> mapMembers,
			[CanBeNull] Predicate<T> predicate,
			bool includeInvalid = false) where T : class, IDisplayTable
		{
			// TODO: Redirect GetLayers, GetStandaloneTables to this method to avoid code duplication
			Predicate<T> displayTablePredicate;

			if (includeInvalid)
			{
				displayTablePredicate = predicate;
			}
			else
			{
				// Check for validity first because in most cases the specified layerPredicate
				// uses the FeatureClass name etc. which results in null-pointers if evaluated first.
				displayTablePredicate = l => l.GetTable() != null &&
				                             (predicate == null || predicate(l));
			}

			Predicate<MapMember> mapMemberPredicate = mm =>
				mm is T t && (displayTablePredicate == null || displayTablePredicate(t));

			foreach (MapMember mapMember in mapMembers)
			{
				if (mapMemberPredicate(mapMember))
				{
					yield return (T) (IDisplayTable) mapMember;
				}
			}
		}

		public static IEnumerable<T> GetFeatureLayers<T>(
			Map map, Predicate<T> layerPredicate = null,
			bool includeInvalid = false) where T : BasicFeatureLayer
		{
			Predicate<T> combinedPredicate;

			if (includeInvalid)
			{
				combinedPredicate = layerPredicate;
			}
			else
			{
				// Check for validity first so that the predicate can assume the layer has a FeatureClass
				combinedPredicate = l =>
					LayerUtils.IsLayerValid(l) && (layerPredicate == null || layerPredicate(l));
			}

			return GetLayers(map, combinedPredicate);
		}

		/// <summary>
		/// Returns the layers for which
		/// (a) the user has data source level permission to edit and
		/// (b) it is made editable on the map
		/// </summary>
		/// <param name="map"></param>
		/// <returns></returns>
		public static IEnumerable<BasicFeatureLayer> GetEditableLayers(
			[NotNull] Map map)
		{
			IEnumerable<BasicFeatureLayer> editLayers =
				GetFeatureLayers<BasicFeatureLayer>(map, bfl => bfl?.IsEditable == true);

			return editLayers;
		}

		public static IEnumerable<StandaloneTable> GetStandaloneTables(
			[NotNull] Map map,
			[CanBeNull] Predicate<StandaloneTable> tablePredicate,
			bool includeInvalid = false)
		{
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

			foreach (StandaloneTable table in map.GetStandaloneTablesAsFlattenedList())
			{
				if (combinedPredicate == null || combinedPredicate(table))
				{
					yield return table;
				}
			}
		}

		[CanBeNull]
		public static StandaloneTable GetStandaloneTable(
			[NotNull] Map map,
			[CanBeNull] string tableName)
		{
			return GetStandaloneTables(
					map,
					table => string.Equals(table.GetTable().GetName(),
					                       tableName,
					                       StringComparison.OrdinalIgnoreCase))
				.FirstOrDefault();
		}

		public static string GetLocationUnitAbbreviation([NotNull] Map map)
		{
			DisplayUnitFormat locationUnitFormat = map.GetLocationUnitFormat();

			string locationUnitAbbreviation = locationUnitFormat?.Abbreviation;

			return locationUnitAbbreviation;
		}

		public static string GetElevationUnitAbbreviation(Map map)
		{
			DisplayUnitFormat elevationUnitFormat = map.GetElevationUnitFormat();

			string elevationUnitAbbreviation = elevationUnitFormat?.Abbreviation;

			return elevationUnitAbbreviation;
		}

		#region Not MapUtils --> move elsewhere

		/// <summary>
		/// Converts a screen point to a map point.
		/// </summary>
		/// <param name="mapView"></param>
		/// <param name="screenPoint">The global screen coordinates.</param>
		/// <returns></returns>
		public static MapPoint ToMapPoint(MapView mapView, Point screenPoint)
		{
			return mapView.ScreenToMap(screenPoint);
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

		/// <summary>
		/// Gets the pixel size for the specified map view in the map space at the specified point.
		/// Note that the point must have the correct Z value in order to return correct results
		/// in a stereo map in floating cursor mode.
		/// BUG: In fixed cursor mode this method always returns 0 because ScreenToMap seems not to
		/// work correctly.
		/// </summary>
		/// <param name="mapView"></param>
		/// <param name="pixels"></param>
		/// <param name="atPoint"></param>
		/// <returns></returns>
		/// <remarks>Must run on MCT</remarks>
		public static double ConvertScreenPixelToMapLength(
			MapView mapView,
			int pixels,
			[NotNull] MapPoint atPoint)
		{
			if (mapView.ViewingMode == MapViewingMode.MapStereo)
			{
				return GetPixelSizeInMapUnits(mapView, atPoint) * pixels;
			}

			// The point as screen point
			var screenPoint = mapView.MapToScreen(atPoint);

			//Add tolerance pixels to get a "radius".
			var radiusScreenPoint = new Point(screenPoint.X + pixels, screenPoint.Y);
			var radiusMapPoint = mapView.ScreenToMap(radiusScreenPoint);

			return GeometryEngine.Instance.Distance(atPoint, radiusMapPoint);
		}

		/// <summary>
		/// Gets the pixel size for the specified map view in the map space without
		/// using the ScreenToMap method (which is incorrect in stereo maps at 3.3).
		/// This method is not particularly robust against rotated maps!
		/// </summary>
		/// <param name="mapView"></param>
		/// <param name="atPoint"></param>
		/// <returns></returns>
		private static double GetPixelSizeInMapUnits(MapView mapView,
		                                             [NotNull] MapPoint atPoint)
		{
			Envelope mapExtent = mapView.Map.GetDefaultExtent();
			SpatialReference sr = mapExtent.SpatialReference;

			double z = atPoint.Z;

			MapPoint mapLowerLeft = GeometryFactory.CreatePoint(
				mapExtent.XMin, mapExtent.YMin, z, sr);
			MapPoint mapUpperRight = GeometryFactory.CreatePoint(
				mapExtent.XMax, mapExtent.YMax, z, sr);

			Point screenLowerLeft = mapView.MapToScreen(mapLowerLeft);
			Point screenUpperRight = mapView.MapToScreen(mapUpperRight);

			// Client window coordinates (probably makes no difference for this calculation but it is correct)
			Point clientLowerLeft = mapView.ScreenToClient(screenLowerLeft);
			Point clientUpperRight = mapView.ScreenToClient(screenUpperRight);

			double widthPixels = Math.Abs(clientUpperRight.X - clientLowerLeft.X);
			double heightPixels = Math.Abs(clientUpperRight.Y - clientLowerLeft.Y);

			double pixelSizeX = mapExtent.Width / widthPixels;
			double pixelSizeY = mapExtent.Height / heightPixels;

			return pixelSizeX + pixelSizeY / 2;
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
			return HasSelection(mapView?.Map);
		}

		public static bool HasSelection([CanBeNull] Map map)
		{
			return map?.SelectionCount > 0;
		}

		public static async Task<bool> FlashGeometryAsync(
			[NotNull] MapView mapView,
			[NotNull] Geometry geometry,
			CIMSymbolReference symbolReference,
			int milliseconds = 400,
			bool useReferenceScale = false)
		{
			return await FlashGeometryAsync(mapView, new Overlay(geometry, symbolReference),
			                                milliseconds, useReferenceScale);
		}

		public static async Task<bool> FlashGeometryAsync(
			[NotNull] MapView mapView,
			[NotNull] Overlay overlay,
			int milliseconds = 400,
			bool useReferenceScale = false)
		{
			using (await overlay.AddToMapAsync(mapView, useReferenceScale))
			{
				await Task.Delay(milliseconds);
			}

			return true;
		}

		public static async Task<bool> FlashGeometriesAsync(
			[NotNull] MapView mapView,
			IEnumerable<Overlay> overlays,
			int milliseconds = 400,
			bool useReferenceScale = false)
		{
			List<IDisposable> disposables = new List<IDisposable>();

			try
			{
				foreach (Overlay overlay in overlays)
				{
					disposables.Add(await overlay.AddToMapAsync(mapView, useReferenceScale));
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
			int milliseconds = 400,
			bool useReferenceScale = false)
		{
			List<IDisposable> disposables = new List<IDisposable>();

			try
			{
				foreach (Overlay overlay in overlays)
				{
					disposables.Add(overlay.AddToMap(mapView, useReferenceScale));
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

		#endregion

		private static FeatureClass GetUnJoinedFeatureClass(FeatureClass featureClass)
		{
			// Get the shape's table name
			string shapeField = featureClass.GetDefinition().GetShapeField();

			List<string> tokens = shapeField.Split('.').ToList();

			if (tokens.Count < 2)
			{
				return featureClass;
			}

			tokens.RemoveAt(tokens.Count - 1);

			string tableName = StringUtils.Concatenate(tokens, ".");

			foreach (Table databaseTable in DatasetUtils.GetDatabaseTables(featureClass))
			{
				if (databaseTable is FeatureClass dbFeatureClass &&
				    dbFeatureClass.GetName()
				                  .Equals(tableName, StringComparison.InvariantCultureIgnoreCase))
				{
					return dbFeatureClass;
				}
			}

			return featureClass;
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
