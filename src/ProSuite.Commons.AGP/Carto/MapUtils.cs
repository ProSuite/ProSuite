using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Collections;
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

			foreach (BasicFeatureLayer layer in layers.Where(HasSelection))
			{
				IReadOnlyList<long> selection = layer.GetSelection().GetObjectIDs();

				Table table = layer.GetTable();
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

			foreach (Table table in layers.Select(layer => layer.GetTable()).Distinct())
			{
				var geodatabase = (Geodatabase) table.GetDatastore();

				if (! result.ContainsKey(geodatabase))
				{
					result.Add(geodatabase, new SimpleSet<Table> {table});
				}
				else
				{
					result[geodatabase].TryAdd(table);
				}
			}

			return result.ToDictionary(pair => pair.Key, pair => pair.Value.ToList());
		}

		public static IEnumerable<Feature> GetFeatures(Dictionary<MapMember, List<long>> oidsByMapMembers)
		{
			foreach (var oidsByMapMember in oidsByMapMembers)
			{
				var featureLayer = oidsByMapMember.Key as BasicFeatureLayer;

				if (featureLayer == null) continue;

				foreach (var feature in GetFeatures(featureLayer, oidsByMapMember.Value)) yield return feature;
			}
		}

		public static IEnumerable<Feature> GetFeatures(
			IEnumerable<KeyValuePair<BasicFeatureLayer, List<long>>> oidsByMapMembers)
		{
			foreach (var selectionByLayer in oidsByMapMembers)
			foreach (var feature in GetFeatures(selectionByLayer))
				yield return feature;
		}
		
		public static IEnumerable<Feature> GetFeatures(KeyValuePair<BasicFeatureLayer, List<long>> layerOids)
		{
			var layer = layerOids.Key;

			foreach (var feature in GetFeatures(layer, layerOids.Value)) yield return feature;
		}

		
		private static IEnumerable<Feature> GetFeatures(BasicFeatureLayer layer, List<long> oids,
		                                                bool recycling = false)
		{
			if (layer == null) yield break;

			// TODO: Use layer search (there might habe been an issue with recycling?!)
			var featureClass = layer.GetTable();

			var filter = new QueryFilter
			             {
				             WhereClause =
					             $"{featureClass.GetDefinition().GetObjectIDField()} IN ({StringUtils.Concatenate(oids, ", ")})"
			             };

			filter.OutputSpatialReference = layer.GetSpatialReference();

			// TODO: Compare performance using inspector, use some feature abstraction?
			//foreach (long oid in oids)
			//{
			//	var inspector = layer.Inspect(oid);

			//	yield return inspector.;
			//}

			foreach (var feature in GdbQueryUtils.GetFeatures(featureClass, filter, recycling)) yield return feature;
		}

		public static ArcGIS.Core.Geometry.Geometry ToMapGeometry(MapView mapView,
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

		public static ArcGIS.Core.Geometry.Geometry ToScreenGeometry(
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
		public static IEnumerable<Feature> FilterFeaturesByGeometry(
			BasicFeatureLayer layer, ArcGIS.Core.Geometry.Geometry filterGeometry,
			SpatialRelationship spatialRelationship = SpatialRelationship.Intersects)
		{
			var qf = new SpatialQueryFilter()
			         {
				         FilterGeometry = filterGeometry,
				         SpatialRelationship = spatialRelationship
			         };
			var features = new List<Feature>();

			using (RowCursor rowCursor = layer.Search(qf))
			{
				while (rowCursor.MoveNext())
				{
					features.Add((Feature)rowCursor.Current);
				}
			}

			return features;
		}

		public static IEnumerable<long> GetFeaturesOidList(IEnumerable<Feature> features)
		{
			List<long> oids = new List<long>();
			foreach (Feature feature in features)
			{
				oids.Add(feature.GetObjectID());
				//yield return feature.GetObjectID();
			}

			return oids;
		}

		public static double ConvertScreenPixelToMapLength(int pixels)
		{
			var mapExtent = MapView.Active.Map.GetDefaultExtent();
			var mapPoint = mapExtent.Center;
			//Map center as screen point
			var screenPoint = MapView.Active.MapToScreen(mapPoint);
			//Add tolerance pixels to get a "radius".
			var radiusScreenPoint =
				new System.Windows.Point((screenPoint.X + pixels), screenPoint.Y);
			var radiusMapPoint = MapView.Active.ScreenToMap(radiusScreenPoint);
			return GeometryEngine.Instance.Distance(mapPoint, radiusMapPoint);
		}


		public static bool HasSelection(BasicFeatureLayer featureLayer)
		{
			return featureLayer.SelectionCount > 0;
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
