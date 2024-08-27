using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto;

/// <summary>
/// The white selection over all layers in a given map (we need a MapView,
/// not just the map, to have the MapView.GetFeatures() function available).
/// Maintains a collection of <see cref="IWhiteSelection"/> instances.
/// </summary>
public class MapWhiteSelection : IDisposable
{
	//private SubscriptionToken _mapSelectionChangedToken;
	// TODO probably should keep _layerSelections ordered as in the ToC
	private readonly Dictionary<string, IWhiteSelection> _layerSelections = new();
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public MapWhiteSelection(MapView mapView, bool syncWithRegularSelection = false)
	{
		MapView = mapView ?? throw new ArgumentNullException(nameof(mapView));
		//SyncWithRegularSelection = syncWithRegularSelection;

		// TODO Set this MapWhiteSelection from regular selection

		//_mapSelectionChangedToken = MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);
	}

	private void OnMapSelectionChanged(MapSelectionChangedEventArgs obj)
	{
		try
		{
			if (SyncWithRegularSelection)
			{
				// TODO update white selection from regular selection
				// TODO how to identify calls due to our own activity? (latching is probably unreliable due to multi-threading)
			}
		}
		catch (Exception ex)
		{
			_msg.Error($"{GetType().Name} SelectionChanged: {ex.Message}", ex);
		}
	}

	public MapView MapView { get; }

	public bool SyncWithRegularSelection { get; set; }

	/// <returns>true iff the white selection of all layers in the map are empty</returns>
	public bool IsEmpty => _layerSelections.Values.All(ws => ws.IsEmpty);

	/// <returns>true iff <paramref name="point"/> is within
	/// <paramref name="tolerance"/> of a selected vertex</returns>
	public bool HitTestVertex(MapPoint point, double tolerance, out MapPoint vertex)
	{
		var all = GetLayerSelections();

		// TODO either find closest hit or first in layer order?!
		foreach (var ws in all)
		{
			if (ws.HitTestVertex(point, tolerance, out vertex))
			{
				return true;
			}
		}

		vertex = null;
		return false;
	}

	public ICollection<IWhiteSelection> GetLayerSelections()
	{
		return _layerSelections.Values;
	}

	/// <summary>
	/// Just a convenience to enumerate all features in the white selection
	/// </summary>
	public IEnumerable<ValueTuple<FeatureLayer, long, IShapeSelection>> Enumerate()
	{
		foreach (var ws in GetLayerSelections())
		{
			var featureLayer = ws.Layer;
			if (!featureLayer.IsEditable) continue;

			foreach (var oid in ws.GetInvolvedOIDs())
			{
				var shapeSelection = ws.GetShapeSelection(oid);

				yield return (featureLayer, oid, shapeSelection);
			}
		}
	}

	[NotNull]
	public IWhiteSelection GetLayerSelection([NotNull] FeatureLayer layer)
	{
		if (layer is null)
			throw new ArgumentNullException(nameof(layer));

		var uri = layer.URI ?? throw new InvalidOperationException("Layer has no URI");

		if (!_layerSelections.TryGetValue(uri, out var result))
		{
			result = new WhiteSelection(layer);
			_layerSelections.Add(uri, result);
		}

		return result;
	}

	/// <returns>number of features actually removed from selection</returns>
	/// <remarks>Must call on MCT (if syncing with regular selection)</remarks>
	public int Remove(FeatureLayer layer, IEnumerable<long> oids)
	{
		if (! _layerSelections.TryGetValue(layer.URI, out var ws))
		{
			return 0; // layer has no white selection
		}

		int removed = 0;

		foreach (var oid in oids)
		{
			if (ws.Remove(oid))
			{
				removed += 1;
			}
		}

		if (ws.IsEmpty)
		{
			_layerSelections.Remove(layer.URI);
		}

		//if (SyncWithRegularSelection)
		//{
		//	var regular = layer.GetSelection();
		//	regular.Remove(oids);
		//	layer.SetSelection(regular);
		//}

		return removed;
	}

	/// <returns>true iff the selection changed</returns>
	/// <remarks>Must call on MCT (if syncing with regular selection)</remarks>
	public bool Select(MapPoint clickPoint, double tolerance, SetCombineMethod method)
	{
		var changed = false;

		if (method == SetCombineMethod.New)
		{
			changed = SetEmpty();
			method = SetCombineMethod.Add;
		}

		var extent = clickPoint.Extent.Expand(tolerance, tolerance, false);

		var dict = GetFeatures(MapView, extent);

		foreach (var pair in dict)
		{
			var featureLayer = pair.Key;
			var featureOids = pair.Value.ToArray();

			var selection = GetLayerSelection(featureLayer);

			selection.CacheGeometries(featureOids);

			foreach (var oid in featureOids)
			{
				var shape = selection.GetGeometry(oid);
				if (shape is null || shape.IsEmpty) continue;

				bool added = selection.Add(oid);
				if (added)
				{
					changed = true;
				}

				if (SelectVertex(selection, oid, shape, clickPoint, tolerance, method, out bool vertexTouched))
				{
					changed = true;
				}

				if (method == SetCombineMethod.Xor && ! added && ! vertexTouched)
				{
					// User xor-selected a previously selected shape, but not a vertex
					// of this shape: remove the entire shape/oid from the white selection
					selection.Remove(oid);
					changed = true;
				}
			}

			//if (SyncWithRegularSelection)
			//{
			//	UpdateRegularSelection(selection, featureOids);
			//}
		}

		return changed;
	}

	private static bool SelectVertex(
		IWhiteSelection selection, long oid, Geometry shape,
		MapPoint clickPoint, double tolerance, SetCombineMethod method, out bool vertexTouched)
	{
		var proximity = GeometryEngine.Instance.NearestVertex(shape, clickPoint);
		if (proximity.Distance <= tolerance)
		{
			vertexTouched = true;

			var vertexIndex = proximity.PointIndex ??
			                  throw new InvalidOperationException(
				                  "NearestVertex() must return non-null PointIndex");

			if (shape is Multipoint)
			{
				// by convention, a multipoint's ith point is both part i and vertex i
				Assert.AreEqual(proximity.PartIndex, proximity.PointIndex,
				                "Expect partIndex == vertexIndex for multipoint");
			}

			return selection.Combine(oid, proximity.PartIndex, vertexIndex, method);
		}

		vertexTouched = false;
		return false;
	}

	/// <returns>true iff the selection changed</returns>
	/// <remarks>Must call on MCT (if syncing with regular selection)</remarks>
	public bool Select(Geometry geometry, SetCombineMethod method)
	{
		var changed = false;

		if (method == SetCombineMethod.New)
		{
			changed = SetEmpty();
			method = SetCombineMethod.Add;
		}

		var dict = GetFeatures(MapView, geometry);

		foreach (var pair in dict)
		{
			var featureLayer = pair.Key;
			var featureOids = pair.Value.ToArray();

			var selection = GetLayerSelection(featureLayer);

			selection.CacheGeometries(featureOids);

			foreach (var oid in featureOids)
			{
				var shape = selection.GetGeometry(oid);
				if (shape is null || shape.IsEmpty) continue;

				bool added = selection.Add(oid);
				if (added)
				{
					changed = true;
				}

				if (SelectVertices(selection, oid, shape, geometry, method, out int touchedVertices))
				{
					changed = true;
				}

				if (method == SetCombineMethod.Xor && !added && touchedVertices == 0)
				{
					// user xor-selected a previously selected shape but not a single vertex:
					// remove this entire shape/oid from the white selection
					selection.Remove(oid);
					changed = true;
				}
			}

			//if (SyncWithRegularSelection)
			//{
			//	UpdateRegularSelection(selection, featureOids);
			//}
		}

		return changed;
	}

	private static bool SelectVertices(
		IWhiteSelection selection, long oid, Geometry shape,
		Geometry perimeter, SetCombineMethod method, out int touchedVertices)
	{
		bool changed = false;
		touchedVertices = 0;

		if (shape is MapPoint point)
		{
			if (GeometryEngine.Instance.Contains(perimeter, point))
			{
				touchedVertices += 1;
				if (selection.Combine(oid, 0, 0, method))
				{
					changed = true;
				}
			}
		}
		else if (shape is Multipoint multipoint)
		{
			for (int i = 0; i < multipoint.PointCount; i++)
			{
				if (GeometryEngine.Instance.Contains(perimeter, multipoint.Points[i]))
				{
					touchedVertices += 1;
					// by convention, a multipoint's ith point is both part i and vertex i
					if (selection.Combine(oid, i, i, method))
					{
						changed = true;
					}
				}
			}
		}
		else if (shape is Multipart multipart)
		{
			for (int j = 0; j < multipart.PartCount; j++)
			{
				var part = multipart.Parts[j];
				int segmentCount = part.Count;
				Segment segment = null;
				for (int i = 0; i < segmentCount; i++)
				{
					segment = part[i]; // segment i is between vertex i and i+1

					if (GeometryEngine.Instance.Contains(perimeter, segment.StartPoint))
					{
						touchedVertices += 1;
						if (selection.Combine(oid, j, i, method))
						{
							changed = true;
						}
					}
				}

				if (segment != null && shape is Polyline)
				{
					// open path (not closed ring): also check last segment's end point:
					if (GeometryEngine.Instance.Contains(perimeter, segment.EndPoint))
					{
						touchedVertices += 1;
						if (selection.Combine(oid, j, segmentCount, method))
						{
							changed = true;
						}
					}
				}
			}
		}
		//else: not supported (silently ignore)

		return changed;
	}

	private Dictionary<FeatureLayer, List<long>> GetFeatures(MapView mapView, Geometry geometry)
	{
		if (mapView is null)
			throw new ArgumentNullException(nameof(mapView));
		if (geometry is null || geometry.IsEmpty)
			return new Dictionary<FeatureLayer, List<long>>(0);

		// Notes:
		// - MapView.GetFeatures() checks for intersection with symbolization,
		//   not geometry, that is, a vertex or segment in the gap of a dashed
		//   line will not be selected -- this is BAD for our White Tool
		// - The optional visualIntersect bool parameter (default is true)
		//   sounds promising but seems to have no effect when set to false
		// - What's the difference between GetFeatures() and GetFeaturesEx()?
		// - Could use our own code to select (see SelectionTool)
		// - Bug: does not find all features if the symbol has an Offset effect (K2#38)

		var selectionSet = mapView.GetFeatures(geometry, VisualIntersect);
		//var selectionSet = mapView.GetFeaturesEx(geometry, VisualIntersect);
		return selectionSet.ToDictionary<FeatureLayer>();
	}

	public bool VisualIntersect { get; set; } = true;

	/// <remarks>Must call on MCT</remarks>
	private static void UpdateRegularSelection(IWhiteSelection selection, long[] involvedOids)
	{
		var selectedOids = involvedOids.Where(oid => ! (selection.GetShapeSelection(oid)?.IsEmpty ?? true));
		var unselectedOids = involvedOids.Where(oid => selection.GetShapeSelection(oid)?.IsEmpty ?? false);

		var regular = selection.Layer.GetSelection();

		regular.Add(selectedOids);
		regular.Remove(unselectedOids);

		// The above has no effect on the map until we layer.SetSelection():
		selection.Layer.SetSelection(regular);
	}

	/// <returns>true iff the selection changed (was not already empty)</returns>
	public bool SetEmpty()
	{
		var all = GetLayerSelections();

		var changed = false;

		foreach (var selection in all)
		{
			if (selection.SetEmpty())
			{
				changed = true;
			}
		}

		//if (SyncWithRegularSelection)
		//{
		//	MapView.Map.ClearSelection();
		//}

		return changed;
	}

	public void ClearGeometryCache()
	{
		var all = GetLayerSelections();

		foreach (var selection in all)
		{
			selection.ClearGeometryCache();
		}
	}


	/// <returns>number of features updated and removed</returns>
	public ValueTuple<int,int> UpdateGeometries()
	{
		int updated = 0;
		int removed = 0;

		var all = GetLayerSelections();

		foreach (var selection in all)
		{
			var list = selection.UpdateGeometries();

			updated += list.Count(r => r.State == IWhiteSelection.RefreshState.Updated);
			removed += list.Count(r => r.State == IWhiteSelection.RefreshState.Removed);
		}

		return (updated, removed);
	}

	/// <summary>
	/// Try update geometries in selection (reload from data store),
	/// but if part/vertex count don't agree, remove from selection.
	/// </summary>
	/// <returns>number of features updated and removed</returns>
	public ValueTuple<int,int> UpdateOrRemove(FeatureLayer layer, IEnumerable<long> oids)
	{
		if (!_layerSelections.TryGetValue(layer.URI, out var ws))
		{
			return (0, 0); // layer has no white selection
		}

		var result = ws.UpdateGeometries(oids);

		if (ws.IsEmpty)
		{
			_layerSelections.Remove(layer.URI);
		}

		int updated = result.Count(r => r.State == IWhiteSelection.RefreshState.Updated);
		int removed = result.Count(r => r.State == IWhiteSelection.RefreshState.Removed);

		return (updated, removed);
	}

	public void Dispose()
	{
		//if (_mapSelectionChangedToken is not null)
		//{
		//	MapSelectionChangedEvent.Unsubscribe(_mapSelectionChangedToken);
		//	_mapSelectionChangedToken = null;
		//}
	}
}
