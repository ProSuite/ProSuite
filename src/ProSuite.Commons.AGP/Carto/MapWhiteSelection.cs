using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto;

/// <summary>
/// The white selection over all layers in a given map (we need a MapView,
/// not just the map, to have the MapView.GetFeatures() function available).
/// Maintains a collection of <see cref="IWhiteSelection"/> instances.
/// </summary>
public class MapWhiteSelection : IDisposable
{
	private readonly Dictionary<string, IWhiteSelection> _layerSelections = new();

	public MapWhiteSelection(MapView mapView)
	{
		MapView = mapView ?? throw new ArgumentNullException(nameof(mapView));
	}

	public MapView MapView { get; }

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

		return changed;
	}

	/// <returns>true iff the selection changed</returns>
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
		}

		return changed;
	}

	/// <returns>true iff the selection changed</returns>
	public bool Combine(Envelope extent, SetCombineMethod method)
	{
		var changed = false;

		if (method == SetCombineMethod.New)
		{
			changed = SetEmpty();
			method = SetCombineMethod.Add;
		}

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

				if (shape is MapPoint point)
				{
					if (GeometryEngine.Instance.Contains(extent, point))
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
						if (GeometryEngine.Instance.Contains(extent, multipoint.Points[i]))
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

							if (GeometryEngine.Instance.Contains(extent, segment.StartPoint))
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
							if (GeometryEngine.Instance.Contains(extent, segment.EndPoint))
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
		}

		return changed;
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
		// presently nothing to dispose
	}
}
