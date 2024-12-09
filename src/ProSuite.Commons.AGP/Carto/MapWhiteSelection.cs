using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto;

public interface IMapWhiteSelection
{
	/// <summary>The map to which this white selection belongs</summary>
	Map Map { get; }

	/// <returns>true iff no involved features (regardless of vertices)</returns>
	bool IsEmpty { get; }

	bool Select(MapPoint clickPont, double tolerance, SetCombineMethod method, Dictionary<FeatureLayer, List<long>> candidates);
	bool Select(Geometry geometry, SetCombineMethod method, Dictionary<FeatureLayer, List<long>> candidates);
	int Remove(FeatureLayer layer, IEnumerable<long> oids);
	bool SetEmpty();

	bool SelectedVertex(MapPoint point, double tolerance, out MapPoint vertex);

	MapPoint NearestPoint(MapPoint clickPoint, double tolerance,
	                      out FeatureLayer layer, out long oid, out int partIndex,
	                      out int? segmentIndex, out int? vertexIndex);

	ICollection<IWhiteSelection> GetLayerSelections();
	IWhiteSelection GetLayerSelection(FeatureLayer layer);

	ValueTuple<int, int> RefreshGeometries(); // all in mws; remove from sel where incompatible
	ValueTuple<int, int> RefreshGeometries(FeatureLayer layer, IEnumerable<long> oids);

	void ClearGeometryCache();

	void UpdateRegularSelection();
	bool UpdateWhiteSelection(SelectionSet regular);
}

/// <summary>
/// The white selection over all layers in a given map (we need a MapView,
/// not just the map, to have the MapView.GetFeatures() function available).
/// Maintains one <see cref="IWhiteSelection"/> instance per layer, which
/// in turn maintains one <see cref="IShapeSelection"/> per involved feature.
/// </summary>
public class MapWhiteSelection : IMapWhiteSelection, IDisposable
{
	private readonly MapView _mapView;
	// TODO probably should keep _layerSelections ordered as in the ToC
	private readonly Dictionary<string, IWhiteSelection> _layerSelections = new();

	public MapWhiteSelection(MapView mapView)
	{
		_mapView = mapView ?? throw new ArgumentNullException(nameof(mapView));
	}

	public Map Map => _mapView.Map;

	/// <returns>true iff the no involved features (regardless of selected vertices)</returns>
	public bool IsEmpty => _layerSelections.Values.Sum(ws => ws.InvolvedFeatureCount) <= 0;
	//public bool IsEmpty => _layerSelections.Values.All(ws => ws.IsEmpty);

	/// <returns>true iff <paramref name="point"/> is within
	/// <paramref name="tolerance"/> of a selected vertex</returns>
	public bool SelectedVertex(MapPoint point, double tolerance, out MapPoint vertex)
	{
		var all = GetLayerSelections();

		// TODO either find closest hit or first in layer order?!
		foreach (var ws in all)
		{
			if (ws.SelectedVertex(point, tolerance, out vertex))
			{
				return true;
			}
		}

		vertex = null;
		return false;
	}

	public MapPoint NearestPoint(MapPoint clickPoint, double tolerance,
	                             out FeatureLayer layer, out long oid, out int partIndex,
	                             out int? segmentIndex, out int? vertexIndex)
	{
		var all = GetLayerSelections();

		var minDistance = double.MaxValue;
		FeatureLayer minLayer = null;
		long minOid = -1;
		int minPartIndex = -1;
		int? minSegIndex = null;
		int? minVertIndex = null;
		MapPoint minPoint = null;

		// - find the closest point within tolerance
		// - vertex within tolerance wins over an even closer segment

		foreach (var ws in all)
		{
			foreach (var involvedOid in ws.GetInvolvedOIDs())
			{
				var ss = ws.GetShapeSelection(involvedOid) ?? throw new AssertionException();

				var proximity = GeometryEngine.Instance.NearestVertex(ss.Shape, clickPoint);
				if (proximity is null || ! (proximity.Distance <= tolerance)) continue;

				if (proximity.Distance < minDistance)
				{
					minDistance = proximity.Distance;
					minLayer = ws.Layer;
					minOid = involvedOid;
					minPartIndex = proximity.PartIndex;
					minVertIndex = proximity.PointIndex ?? throw new AssertionException();
					minPoint = proximity.Point;
				}
			}
		}

		if (minPoint is null)
		{
			// No *vertex* was within tolerance of the given clickPoint:
			// make a 2nd round looking for the nearest *segment* instead:

			foreach (var ws in all)
			{
				foreach (var involvedOid in ws.GetInvolvedOIDs())
				{
					var ss = ws.GetShapeSelection(involvedOid) ?? throw new AssertionException();

					var proximity = GeometryEngine.Instance.NearestPoint(ss.Shape, clickPoint);
					if (proximity is null || ! (proximity.Distance <= tolerance)) continue;

					if (proximity.Distance < minDistance)
					{
						minDistance = proximity.Distance;
						minLayer = ws.Layer;
						minOid = involvedOid;
						minPartIndex = proximity.PartIndex;
						minSegIndex = proximity.SegmentIndex ?? throw new AssertionException();
						minPoint = proximity.Point;
					}
				}
			}
		}

		layer = minLayer;
		oid = minOid;
		partIndex = minPartIndex;
		segmentIndex = minSegIndex;
		vertexIndex = minVertIndex;
		return minPoint;
	}

	public ICollection<IWhiteSelection> GetLayerSelections()
	{
		return _layerSelections.Values;
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

		if (ws.InvolvedFeatureCount <= 0)
		{
			_layerSelections.Remove(layer.URI);
		}

		return removed;
	}

	/// <returns>true iff the selection changed</returns>
	/// <remarks>Must call on MCT</remarks>
	public bool Select(MapPoint clickPoint, double tolerance, SetCombineMethod method, Dictionary<FeatureLayer, List<long>> candidates)
	{
		//var extent = clickPoint.Extent.Expand(tolerance, tolerance, false);
		//var dict = GetFeatures(extent);
		var dict = candidates;

		var changed = false;

		if (method == SetCombineMethod.New)
		{
			changed = SetEmpty();
			method = SetCombineMethod.Add;
		}

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

			var globalIndex = proximity.PointIndex ??
			                  throw new InvalidOperationException(
				                  "NearestVertex() must return non-null PointIndex");

			var localIndex = GeometryUtils.GetLocalVertexIndex(shape, globalIndex, out _);
			// assert: proximity.PartIndex == partIndex (out param from GetLocalVertexIndex)

			if (shape is Multipoint)
			{
				// by convention, a multipoint's ith point is both part i and vertex i
				Assert.AreEqual(proximity.PartIndex, proximity.PointIndex,
				                "Expect partIndex == vertexIndex for multipoint");
			}

			return selection.Combine(oid, proximity.PartIndex, localIndex, method);
		}

		vertexTouched = false;
		return false;
	}

	/// <returns>true iff the selection changed</returns>
	/// <remarks>Must call on MCT (if syncing with regular selection)</remarks>
	public bool Select(Geometry geometry, SetCombineMethod method, Dictionary<FeatureLayer, List<long>> candidates)
	{
		//var dict = GetFeatures(geometry);
		var dict = candidates;

		var changed = false;

		if (method == SetCombineMethod.New)
		{
			changed = SetEmpty();
			method = SetCombineMethod.Add;
		}

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

	/// <remarks>Must call on MCT</remarks>
	private Dictionary<FeatureLayer, List<long>> GetFeatures(Geometry geometry)
	{
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

		var selectionSet = _mapView.GetFeatures(geometry);
		//var selectionSet = mapView.GetFeaturesEx(geometry);
		return selectionSet.ToDictionary<FeatureLayer>();
	}

	/// <returns>true iff the selection changed (was not already empty)</returns>
	public bool SetEmpty()
	{
		var all = GetLayerSelections();

		var changed = false;

		foreach (var selection in all)
		{
			if (selection.Clear())
			{
				changed = true;
			}
		}

		_layerSelections.Clear();

		return changed;
	}

	/// <summary>
	/// Try update all geometries in selection (reload from data store),
	/// but if part/vertex count don't agree, remove from selection.
	/// </summary>
	/// <returns>number of shape selections retained unchanged
	/// and modified (presently: cleared)</returns>
	/// <remarks>Must call on MCT</remarks>
	public ValueTuple<int,int> RefreshGeometries()
	{
		int retained = 0;
		int removed = 0;

		var all = GetLayerSelections();

		foreach (var ws in all)
		{
			var list = ws.RefreshGeometries();

			retained += list.Count(r => r.State == IWhiteSelection.RefreshState.SelectionRetained);
			removed += list.Count(r => r.State == IWhiteSelection.RefreshState.SelectionModified);
		}

		return (retained, removed);
	}

	/// <summary>
	/// Try update given geometries in selection (reload from data store),
	/// but if part/vertex count don't agree, remove from selection.
	/// </summary>
	/// <returns>number of shape selections retained unchanged
	/// and modified (presently: cleared)</returns>
	/// <remarks>Must call on MCT</remarks>
	public ValueTuple<int,int> RefreshGeometries(FeatureLayer layer, IEnumerable<long> oids)
	{
		if (!_layerSelections.TryGetValue(layer.URI, out var ws))
		{
			return (0, 0); // layer has no white selection
		}

		var result = ws.RefreshGeometries(oids);

		int retained = result.Count(r => r.State == IWhiteSelection.RefreshState.SelectionRetained);
		int modified = result.Count(r => r.State == IWhiteSelection.RefreshState.SelectionModified);

		return (retained, modified);
	}

	public void ClearGeometryCache()
	{
		var all = GetLayerSelections();

		foreach (var selection in all)
		{
			selection.ClearGeometryCache();
		}
	}

	/// <summary>
	/// Select on the Map exactly those features that are involved
	/// in this white selection (regardless of selected vertices).
	/// </summary>
	/// <remarks>Must call on MCT</remarks>
	public void UpdateRegularSelection()
	{
		var dict = GetLayerSelections()
			.ToDictionary(s => s.Layer, s => s.GetInvolvedOIDs().ToList());

		var regular = SelectionSet.FromDictionary(dict);

		// TODO This triggers an OnSelectionChangedAsync some time later (!) -- how to latch?
		// DARO reports that using IDisplayTable to set selection does not (or synchronously) raise OnSelectionChanged
		Map.SetSelection(regular);
	}

	/// <summary>
	/// Modify this white selection to contain exactly the features
	/// in the given regular selection. Selected vertices of features
	/// already in the white selection remain unchanged; vertices of
	/// features newly added are in state unselected.
	/// </summary>
	/// <returns>true iff the while selection changed in any way
	/// (and thus should be redrawn)</returns>
	/// <remarks>The white selection is much more resource intensive
	/// than the regular selection (which is just a list of OIDs);
	/// consider not updating the white selection if the regular
	/// selection is large (say more than 50 features).</remarks>
	public bool UpdateWhiteSelection(SelectionSet regular)
	{
		if (regular is null) return false;

		// features in regular but not in white: add (select all vertices? none?)
		// features in white but not in regular: remove
		// features in both white and regular: keep (don't select all vertices?)

		var dict = regular.ToDictionary<FeatureLayer>();

		bool changed = false;

		if (dict.Count == 0)
		{
			changed = SetEmpty();
		}
		else
		{
			foreach (var pair in dict)
			{
				var featureLayer = pair.Key;
				var oids = pair.Value;

				var ws = GetLayerSelection(featureLayer);
				foreach (var oid in oids)
				{
					var sel = ws.GetShapeSelection(oid);
					if (sel is null)
					{
						if (ws.Add(oid))
						{
							changed = true;
						}
					}
					//else: leave alone
				}

				var hashSet = oids.ToHashSet();
				var drops = ws.GetInvolvedOIDs().Where(oid => !hashSet.Contains(oid)).ToList();
				foreach (var oid in drops)
				{
					if (ws.Remove(oid))
					{
						changed = true;
					}
				}
			}

			// layers in white but not in regular:
			var regularLayers = dict.Keys.ToHashSet();
			foreach (var ws in GetLayerSelections())
			{
				if (!regularLayers.Contains(ws.Layer)) // stable instances or better use URI?
				{
					if (ws.Clear())
					{
						changed = true;
					}
				}
			}
		}

		return changed;
	}

	public void Dispose()
	{
		// nothing to dispose at the moment
	}
}
