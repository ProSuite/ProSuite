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
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using UnitType = ArcGIS.Core.Geometry.UnitType;

namespace ProSuite.Commons.AGP.Carto
{
	// Note: MapUtils MUST NEVER use MapView.Active (always pass a Map instance as (the first) argument)

	public static class MapUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

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

		public static bool IsStereoMapView([CanBeNull] MapView mapView)
		{
			return mapView?.ViewingMode == MapViewingMode.MapStereo;
		}

		public static Dictionary<Table, List<long>> GetDistinctSelectionByTable(
			[NotNull] Dictionary<MapMember, List<long>> oidsByLayer,
			[CanBeNull] Predicate<Table> predicate = null)
		{
			var result = new Dictionary<Table, SimpleSet<long>>();
			var distinctTableIds = new Dictionary<GdbTableIdentity, Table>();

			foreach (KeyValuePair<MapMember, List<long>> pair in oidsByLayer)
			{
				Table table = DatasetUtils.GetDatabaseTable(GetTable(pair.Key));

				if (predicate != null && ! predicate(table))
				{
					continue;
				}

				var tableId = new GdbTableIdentity(table);

				if (! distinctTableIds.TryGetValue(tableId, out Table distinctTable))
				{
					distinctTableIds.Add(tableId, table);
					result.Add(table, new SimpleSet<long>(pair.Value));
				}
				else
				{
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
		public static Table GetTable([NotNull] MapMember mapMember,
		                             bool unJoined = false)
		{
			Assert.ArgumentNotNull(mapMember, nameof(mapMember));

			if (mapMember is IDisplayTable displayTable)
			{
				Table table = LayerUtils.GetTable(displayTable, unJoined);

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

		public static IEnumerable<Table> GetTables(IEnumerable<MapMember> mapMembers,
		                                           bool unJoined)
		{
			foreach (MapMember mapMember in mapMembers)
			{
				if (mapMember is IDisplayTable tableBasedMapMember)
				{
					yield return LayerUtils.GetTable(tableBasedMapMember, unJoined);
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
		/// Find layers whose name and parent names match a pattern,
		/// see <see cref="LayerUtils.MatchPattern"/> for details.
		/// </summary>
		/// <typeparam name="T">The type of layers to find</typeparam>
		/// <param name="map">The map from which to find layers</param>
		/// <param name="pattern">The glob-like search pattern</param>
		/// <param name="ignoreCase">Whether to ignore case in pattern matching</param>
		/// <param name="separator">The separator character used in <paramref name="pattern"/></param>
		/// <returns></returns>
		public static IEnumerable<T> FindLayers<T>(
			Map map, string pattern, bool ignoreCase = false, char separator = '\\') where T : Layer
		{
			if (map is null || pattern is null)
			{
				yield break;
			}

			var allLayers = map.GetLayersAsFlattenedList()
			                   .OfType<T>();

			foreach (var layer in allLayers)
			{
				if (LayerUtils.MatchPattern(layer, pattern, ignoreCase, separator))
				{
					yield return layer;
				}
			}
		}

		/// <summary>
		/// On the given <paramref name="map"/>, make all layers that
		/// pull data from <paramref name="findDatastore"/> to pull it
		/// from <paramref name="replaceDatastore"/>.
		/// </summary>
		/// <returns>The number of layers that were (re)connected</returns>
		/// <remarks>Same as <see cref="Map.ReplaceDatasource(Datastore, Datastore, bool)"/>
		/// but should work around a Pro SDK bug in the SDE--FGDB replacement case</remarks>
		public static int ReplaceDataSource(
			Map map, Geodatabase findDatastore, Geodatabase replaceDatastore)
		{
			if (map is null)
				throw new ArgumentNullException(nameof(map));
			if (findDatastore is null)
				throw new ArgumentNullException(nameof(findDatastore));
			if (replaceDatastore is null)
				throw new ArgumentNullException(nameof(replaceDatastore));

			int count = 0;

			// Sadly, map.ReplaceDatasource(findDatastore, replaceDatastore)
			// is buggy when going from SDE to FGDB (at least at Pro 3.5).
			// So we try to do it "manually", layer by layer:

			//var geodatabaseType = replaceDatastore.GetGeodatabaseType();
			var schemaOwner = WorkspaceUtils.FindSchemaOwner(replaceDatastore);

			var layers = map.GetLayersAsFlattenedList()
							.OfType<BasicFeatureLayer>().ToList();

			foreach (var layer in layers)
			{
				//var cimBefore = layer.GetDataConnection(); // TESTING

				using var featureClass = layer.GetFeatureClass();
				if (featureClass is null) continue; // skip invalid layer

				using var datastore = featureClass.GetDatastore();
				if (datastore is not Geodatabase geodatabase) continue;

				bool isSame = WorkspaceUtils.IsSameDatastore(geodatabase, findDatastore);

				if (isSame)
				{
					var originName = featureClass.GetName();
					var targetName = DatasetNameUtils.QualifyDatasetName(originName, schemaOwner);

					var dataset = replaceDatastore.OpenDataset<Table>(targetName);
					layer.ReplaceDataSource(dataset);

					//var cimAfter = layer.GetDataConnection(); // TESTING

					count += 1;
				}
			}

			return count;
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
			[NotNull] MapView mapView,
			[CanBeNull] FeatureClass featureClass) where T : BasicFeatureLayer
		{
			// TODO: WorkspaceEquality.SameVersion
			Predicate<T> sameTablePredicate =
				l => DatasetUtils.IsSameTable(l.GetFeatureClass(), featureClass);

			return GetFeatureLayersForSelection(mapView, sameTablePredicate);
		}

		/// <summary>
		/// Gets the first visible, selectable feature layer without definition query.
		/// If all visible, selectable layers have a definition query, all layers are yielded.
		/// </summary>
		public static IEnumerable<T> GetFeatureLayersForSelection<T>(
			[NotNull] MapView mapView,
			[NotNull] Predicate<T> layerPredicate) where T : BasicFeatureLayer
		{
			var filteredVisibleSelectableLayers = new List<T>();

			foreach (T featureLayer in GetFeatureLayers<T>(
				         mapView.Map,
				         l => l.IsSelectable && l.IsVisibleInView(mapView)))
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
		public static IEnumerable<T> GetEditableLayers<T>([NotNull] Map map)
			where T : BasicFeatureLayer
		{
			IEnumerable<T> editLayers = GetFeatureLayers<T>(map, bfl => bfl?.IsEditable == true);

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

		public static bool RemoveLayer(Map map, Layer layer)
		{
			try
			{
#if ARCGISPRO_GREATER_3_2
				if (! map.CanRemoveLayer(layer))
				{
					return false;
				}
#endif

				map.RemoveLayer(layer);
				return true;
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}

			return false;
		}

		public static bool RemoveLayers(Map map, ICollection<Layer> layers)
		{
			try
			{
#if ARCGISPRO_GREATER_3_2
				if (! map.CanRemoveLayers(layers))
				{
					return false;
				}
#endif

				map.RemoveLayers(layers);
				return true;
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}

			return false;
		}

		/// <summary>
		/// Gets the first elevation surface layer in the map with the specified name.
		/// This layer contains the layers that provide the actual elevation.
		/// </summary>
		/// <param name="map"></param>
		/// <param name="name"></param>
		/// <param name="evenIfEmpty">Whether also empty surface layers containing no actual
		/// elevation sources should be returned.</param>
		/// <returns></returns>
		public static ElevationSurfaceLayer GetElevationSurfaceGroupLayer(
			[NotNull] Map map,
			bool evenIfEmpty = false,
			[CanBeNull] string name = "Ground")
		{
			ElevationSurfaceLayer existingSurfaceLayer = null;
			foreach (ElevationSurfaceLayer elevationSurfaceLayer in map.GetElevationSurfaceLayers())
			{
				if (elevationSurfaceLayer.ElevationMode != ElevationMode.BaseGlobeSurface ||
				    elevationSurfaceLayer.Name != name)
				{
					continue;
				}

				if (! evenIfEmpty && elevationSurfaceLayer.GetLayersAsFlattenedList().Count == 0)
				{
					continue;
				}

				existingSurfaceLayer = elevationSurfaceLayer;
			}

			return existingSurfaceLayer;
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

		/// <summary>
		/// Converts the mapView's client coordinates to screen coordinates. This overload
		/// also returns the correct result for stereo maps in fixed cursor mode.
		/// </summary>
		/// <param name="mapView">The map view from which the client coordinates originate</param>
		/// <param name="clientPoint">Client coordinates relative to the mapView</param>
		/// <param name="mapZValue">Map z coordinate (used for stereo maps)</param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public static async Task<Point> ClientToScreenPointAsync([NotNull] MapView mapView,
		                                                         Point clientPoint,
		                                                         double mapZValue)
		{
			Point defaultScreenLocation = mapView.ClientToScreen(clientPoint);

			if (! IsStereoMapView(mapView))
			{
				return defaultScreenLocation;
			}

			bool isInFixedCursorMode = await IsInStereoFixedCursorMode(mapView);

			if (! isInFixedCursorMode)
			{
				return defaultScreenLocation;
			}

			Envelope mapExtent = mapView.Extent;

			MapPoint mapLowerLeft = GeometryFactory.CreatePoint(
				mapExtent.XMin, mapExtent.YMin, mapZValue, mapExtent.SpatialReference);
			MapPoint mapUpperRight = GeometryFactory.CreatePoint(
				mapExtent.XMax, mapExtent.YMax, mapZValue, mapExtent.SpatialReference);

			return await QueuedTask.Run(
				       () =>
				       {
					       Point screenLowerLeft = mapView.MapToScreen(mapLowerLeft);
					       Point screenUpperRight = mapView.MapToScreen(mapUpperRight);

					       return new Point((screenLowerLeft.X + screenUpperRight.X) / 2,
					                        (screenLowerLeft.Y + screenUpperRight.Y) / 2);
				       });
		}

		public static async Task<bool> IsInStereoFixedCursorMode([NotNull] MapView mapView)
		{
			bool isInFixedCursorMode = false;
#if ARCGISPRO_GREATER_3_5

			Map stereoMap = Assert.NotNull(mapView.Map);

			// TODO: Hopefully at some point we can find out the stereo cursor mode in the UI thread!
			isInFixedCursorMode = await QueuedTaskUtils.Run(() =>
			{
				CIMMap cimMap = Assert.NotNull(stereoMap.GetDefinition(),
				                               "StereoMap's definition is null");

				CIMMapStereoProperties stereoProps = cimMap.StereoProperties;

				Assert.NotNull(stereoProps, $"No stereo properties for map {stereoMap.Name}");

				return stereoProps.IsStereoCursorFixed;
			});
#endif
			return isInFixedCursorMode;
		}

		/// <summary>
		/// Converts a client point to a map point.
		/// </summary>
		/// <param name="mapView"></param>
		/// <param name="clientPoint">The global screen coordinates.</param>
		/// <returns></returns>
		public static MapPoint ClientToMapPoint(MapView mapView, Point clientPoint)
		{
			Point screenPoint = MapView.Active.ClientToScreen(clientPoint);

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

		public static double ConvertScreenPixelToMapLength([NotNull] MapView mapView,
		                                                   int pixels, Point screenPoint)
		{
			MapPoint atPoint = mapView.ScreenToMap(screenPoint);

			// Add pixels to get a "radius".
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
		/// <param name="duration"></param>
		public static async Task<bool> ZoomToAsync([NotNull] MapView mapView,
		                                           [NotNull] Envelope extent,
		                                           double expansionFactor,
		                                           double minimumScale,
		                                           TimeSpan? duration = null)
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

			await mapView.ZoomToAsync(zoomExtent, duration);

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

		public static void FlashGeometries(
			[NotNull] MapView mapView,
			IEnumerable<Geometry> geometries,
			int milliseconds = 400,
			CIMColor color = null,
			bool useReferenceScale = false)
		{
			if (color == null)
			{
				color = ColorUtils.CreateRGB(0, 200, 0);
			}

			List<Overlay> overlays = new List<Overlay>();

			CIMSymbol symbol = null;
			foreach (var group in geometries.GroupBy(g => g.Dimension))
			{
				if (group.Key == 0)
				{
					symbol = SymbolUtils.CreateMarker(color, 4, SymbolUtils.MarkerStyle.Circle)
					                    .MakePointSymbol();
				}

				if (group.Key == 1)
				{
					symbol = SymbolUtils.CreateLineSymbol(color, 2);
				}

				if (group.Key == 2)
				{
					symbol = SymbolUtils.CreatePolygonSymbol(SymbolUtils.CreateSolidFill(color));
				}

				if (group.Count() > 1)
				{
					Geometry union = GeometryEngine.Instance.Union(group);
					overlays.Add(new Overlay(union, symbol));
				}
				else
				{
					overlays.AddRange(group.Select(geometry => new Overlay(geometry, symbol)));
				}
			}

			if (overlays.Count > 0)
			{
				FlashGeometries(mapView, overlays, milliseconds,
				                useReferenceScale);
			}
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
					// mapView.AddOverlay can return null (e.g. for GeometryBags).
					disposable?.Dispose();
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
		public static Envelope GetZoomExtent([NotNull] Envelope newExtent,
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
