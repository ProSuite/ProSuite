using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Events;
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
	public bool HitTestVertex(MapPoint point, double tolerance)
	{
		var all = GetLayerSelections();

		return all.Any(ws => ws.HitTestVertex(point, tolerance));
	}

	public ICollection<IWhiteSelection> GetLayerSelections()
	{
		return _layerSelections.Values;
	}

	/// <summary>
	/// Just a convenience to enumerate all features in the white selection
	/// </summary>
	public IEnumerable<ValueTuple<FeatureLayer, long, Geometry, IShapeSelection>> Enumerate()
	{
		foreach (var ws in GetLayerSelections())
		{
			var featureLayer = ws.Layer;
			if (!featureLayer.IsEditable) continue;

			foreach (var oid in ws.GetSelectedOIDs())
			{
				var originalShape = ws.GetGeometry(oid);
				var shapeSelection = ws.GetShapeSelection(oid);

				yield return (featureLayer, oid, originalShape, shapeSelection);
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

	/// <returns>true iff the selection changed</returns>
	/// <remarks>Must call on MCT (if syncing with regular selection)</remarks>
	public bool Remove(FeatureLayer layer, IEnumerable<long> oids)
	{
		if (! _layerSelections.TryGetValue(layer.URI, out var ws))
		{
			return false; // layer has no white selection
		}

		bool changed = false;

		foreach (var oid in oids)
		{
			if (ws.Remove(oid))
			{
				changed = true;
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

		return changed;
	}

	/// <returns>true iff the selection changed</returns>
	/// <remarks>Must call on MCT (if syncing with regular selection)</remarks>
	public bool Combine(MapPoint clickPoint, double tolerance, SetCombineMethod method)
	{
		var changed = false;

		if (method == SetCombineMethod.New)
		{
			changed = SetEmpty();
			method = SetCombineMethod.Add;
		}

		var extent = clickPoint.Extent.Expand(tolerance / 2, tolerance / 2, false);

		var selectionSet = MapView.GetFeatures(extent);
		if (selectionSet.IsEmpty) return changed;

		var dict = selectionSet.ToDictionary<FeatureLayer>();

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

				var proximity = GeometryEngine.Instance.NearestVertex(shape, clickPoint);
				if (proximity.Distance <= tolerance)
				{
					var vertexIndex = proximity.PointIndex ??
					                  throw new InvalidOperationException(
						                  "NearestVertex() must return non-null PointIndex");

					if (shape is Multipoint)
					{
						// by convention, a multipoint's ith point is both part i and vertex i
						Assert.AreEqual(proximity.PartIndex, proximity.PointIndex,
						                "Expect partIndex == vertexIndex for multipoint");
					}

					if (selection.Combine(oid, proximity.PartIndex, vertexIndex, method))
					{
						changed = true;
					}
				}
			}

			//if (SyncWithRegularSelection)
			//{
			//	UpdateRegularSelection(selection, featureOids);
			//}
		}

		return changed;
	}

	/// <returns>true iff the selection changed</returns>
	/// <remarks>Must call on MCT (if syncing with regular selection)</remarks>
	public bool Combine(Geometry geometry, SetCombineMethod method)
	{
		var changed = false;

		if (method == SetCombineMethod.New)
		{
			changed = SetEmpty();
			method = SetCombineMethod.Add;
		}

		// Bug: does not find all features if the symbol has an Offset effect (K2#38)
		var selectionSet = MapView.GetFeatures(geometry);
		if (selectionSet.IsEmpty) return changed;

		var dict = selectionSet.ToDictionary<FeatureLayer>();

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

				if (shape is MapPoint point)
				{
					if (GeometryEngine.Instance.Contains(geometry, point))
					{
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
						if (GeometryEngine.Instance.Contains(geometry, multipoint.Points[i]))
						{
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

							if (GeometryEngine.Instance.Contains(geometry, segment.StartPoint))
							{
								if (selection.Combine(oid, j, i, method))
								{
									changed = true;
								}
							}
						}

						if (segment != null && shape is Polyline)
						{
							// open path (not closed ring): also check last segment's end point:
							if (GeometryEngine.Instance.Contains(geometry, segment.EndPoint))
							{
								if (selection.Combine(oid, j, segmentCount, method))
								{
									changed = true;
								}
							}
						}
					}
				}
				//else: not supported (silently ignore)
			}

			//if (SyncWithRegularSelection)
			//{
			//	UpdateRegularSelection(selection, featureOids);
			//}
		}

		return changed;
	}

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

	public void Dispose()
	{
		//if (_mapSelectionChangedToken is not null)
		//{
		//	MapSelectionChangedEvent.Unsubscribe(_mapSelectionChangedToken);
		//	_mapSelectionChangedToken = null;
		//}
	}
}
