using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;

namespace ProSuite.Commons.AGP.Carto;

/// <summary>
/// A selection of vertices (not entire features) in a feature layer.
/// The name "white selection" is for historical reasons (ArcMap).
/// </summary>
public interface IWhiteSelection
{
	FeatureLayer Layer { get; }

	bool IsEmpty { get; }

	bool Combine(long oid, int part, int vertex, SetCombineMethod method);
	bool Remove(long oid); // also removes oid's geom from cache
	bool SetEmpty();

	bool HitTestVertex(MapPoint hitPoint, double tolerance);

	IEnumerable<long> GetSelectedOIDs();

	IEnumerable<MapPoint> GetSelectedVertices();

	IShapeSelection GetShapeSelection(long oid);

	// Implementation has a cache oid => Geometry
	Geometry GetGeometry(long oid);
	void CacheGeometries(params long[] oid);
	void ClearGeometryCache();
}

public class WhiteSelection : IWhiteSelection
{
	private readonly Dictionary<long, Geometry> _geometryCache = new();
	private readonly Dictionary<long, IShapeSelection> _shapes = new();

	public WhiteSelection(FeatureLayer layer)
	{
		Layer = layer ?? throw new ArgumentNullException(nameof(layer));
	}

	public FeatureLayer Layer { get; }

	public bool IsEmpty => _shapes.Values.All(ss => ss.IsEmpty);

	public bool Combine(long oid, int part, int vertex, SetCombineMethod method)
	{
		var changed = false;

		if (method == SetCombineMethod.New)
		{
			bool nonEmpty = _shapes.Values.Any(sel => !sel.IsEmpty);
			if (nonEmpty)
				changed = true;
			_shapes.Clear();
			method = SetCombineMethod.Add;
		}

		if (!_shapes.TryGetValue(oid, out var selection))
		{
			selection = new ShapeSelection();
			_shapes.Add(oid, selection);
		}

		if (selection.CombineVertex(part, vertex, method))
		{
			changed = true;
		}

		return changed;
	}

	public bool Remove(long oid)
	{
		_geometryCache.Remove(oid);
		return _shapes.Remove(oid);
	}

	public bool SetEmpty()
	{
		var nonEmpty = _shapes.Values.Any(p => !p.IsEmpty);
		_shapes.Clear();
		return nonEmpty;
	}

	public bool HitTestVertex(MapPoint hitPoint, double tolerance)
	{
		foreach (var pair in _shapes)
		{
			var oid = pair.Key;
			var selection = pair.Value;

			var shape = GetGeometry(oid);

			if (selection.HitTestVertex(shape, hitPoint, tolerance))
			{
				return true;
			}
		}

		return false;
	}

	public IEnumerable<long> GetSelectedOIDs()
	{
		return _shapes.Keys;
	}

	public IEnumerable<MapPoint> GetSelectedVertices()
	{
		foreach (var pair in _shapes)
		{
			var oid = pair.Key;
			var selection = pair.Value;
			var shape = GetGeometry(oid);
			foreach (var vertex in selection.GetSelectedVertices(shape))
			{
				yield return vertex;
			}
		}
	}

	public IShapeSelection GetShapeSelection(long oid)
	{
		return _shapes.GetValueOrDefault(oid);
	}

	#region Geometry cache (by OID)

	public Geometry GetGeometry(long oid)
	{
		if (_geometryCache.TryGetValue(oid, out var shape))
		{
			return shape;
		}

		CacheGeometries(oid);

		if (_geometryCache.TryGetValue(oid, out shape))
		{
			return shape;
		}

		throw new InvalidOperationException($"No geometry for OID {oid}");
	}

	private bool HasGeometry(long oid)
	{
		return _geometryCache.ContainsKey(oid);
	}

	public void CacheGeometries(params long[] oids)
	{
		using var fc = Layer.GetFeatureClass();
		using var defn = fc.GetDefinition();
		var shapeField = defn.GetShapeField();
		var oidField = defn.GetObjectIDField();

		var filter = new QueryFilter { SubFields = $"{oidField},{shapeField}" };

		var missingOids = oids.Where(oid => !HasGeometry(oid)).ToList();
		if (missingOids.Any())
		{
			filter.ObjectIDs = missingOids;

			using var cursor = Layer.Search(filter);

			while (cursor.MoveNext())
			{
				using var feature = (Feature) cursor.Current;
				var oid = feature.GetObjectID();
				var shape = feature.GetShape();
				_geometryCache[oid] = shape;
			}
		}
	}

	public void ClearGeometryCache()
	{
		_geometryCache.Clear();
	}

	#endregion
}
