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

	bool Add(long oid); // add just the oid/shape, don't select any vertices
	bool Remove(long oid); // also removes oid's geom from cache
	bool Combine(long oid, int part, int vertex, SetCombineMethod method);
	bool SetEmpty(); // true iff changed, even if only an "empty oid" is removed; cache not cleared

	bool HitTestVertex(MapPoint hitPoint, double tolerance, out MapPoint vertex);

	IEnumerable<long> GetInvolvedOIDs();

	IEnumerable<Geometry> GetInvolvedShapes();

	IEnumerable<MapPoint> GetSelectedVertices();

	IEnumerable<MapPoint> GetUnselectedVertices();

	IShapeSelection GetShapeSelection(long oid);

	// Implementation has a cache oid => Geometry
	Geometry GetGeometry(long oid); // TODO drop from iface (now that we have IShapeSelection.Shape)
	void CacheGeometries(params long[] oid);
	void ClearGeometryCache();
	void RefreshGeometries(); // reload all cached geometries (call when they have changed)
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
			var shape = GetGeometry(oid);
			selection = new ShapeSelection(shape);
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

	public bool Add(long oid)
	{
		if (_shapes.ContainsKey(oid))
		{
			return false; // already in selection, nothing changed
		}

		var shape = GetGeometry(oid);
		var selection = new ShapeSelection(shape);
		_shapes.Add(oid, selection);
		return true;

		//var shape = GetGeometry(oid);

		//if (! _shapes.TryGetValue(oid, out var selection))
		//{
		//	selection = new ShapeSelection(shape);
		//	_shapes.Add(oid, selection);
		//}

		//bool changed = false;

		//if (shape is MapPoint)
		//{
		//	changed = selection.CombineVertex(0, 0, SetCombineMethod.Add);
		//}
		//else if (shape is Multipoint multipoint)
		//{
		//	int vertexCount = multipoint.PointCount;
		//	for (int i = 0; i < vertexCount; i++)
		//	{
		//		if (selection.CombineVertex(i, i, SetCombineMethod.Add))
		//		{
		//			changed = true;
		//		}
		//	}
		//}
		//else if (shape is Multipart multipart)
		//{
		//	int partCount = multipart.PartCount;
		//	for (int k = 0; k < partCount; k++)
		//	{
		//		var part = multipart.Parts[k];
		//		int vertexCount = part.Count;
		//		if (multipart is Polyline) vertexCount += 1;
		//		for (int i = 0; i < vertexCount; i++)
		//		{
		//			if (selection.CombineVertex(k, i, SetCombineMethod.Add))
		//			{
		//				changed = true;
		//			}
		//		}
		//	}
		//}

		//return changed;
	}

	public bool SetEmpty()
	{
		//var nonEmpty = _shapes.Values.Any(p => !p.IsEmpty);
		//_shapes.Clear();
		//return nonEmpty;

		var changed = _shapes.Count > 0;
		_shapes.Clear();
		return changed;
	}

	public bool HitTestVertex(MapPoint hitPoint, double tolerance, out MapPoint vertex)
	{
		foreach (var pair in _shapes)
		{
			//var oid = pair.Key;
			var selection = pair.Value;

			//var shape = GetGeometry(oid);

			if (selection.HitTestVertex(hitPoint, tolerance, out vertex))
			{
				return true;
			}
		}

		vertex = null;
		return false;
	}

	public IEnumerable<long> GetInvolvedOIDs()
	{
		return _shapes.Keys;
	}

	public IEnumerable<MapPoint> GetSelectedVertices()
	{
		foreach (var pair in _shapes)
		{
			//var oid = pair.Key;
			var selection = pair.Value;
			//var shape = GetGeometry(oid);
			foreach (var vertex in selection.GetSelectedVertices(/*shape*/))
			{
				yield return vertex;
			}
		}
	}

	public IEnumerable<MapPoint> GetUnselectedVertices()
	{
		foreach (var pair in _shapes)
		{
			//var oid = pair.Key;
			var selection = pair.Value;
			//var shape = GetGeometry(oid);
			foreach (var vertex in selection.GetUnselectedVertices(/*shape*/))
			{
				yield return vertex;
			}
		}
	}

	public IEnumerable<Geometry> GetInvolvedShapes()
	{
		return _shapes.Values.Select(sel => sel.Shape);
		//foreach (var pair in _shapes)
		//{
		//	var oid = pair.Key;
		//	var shape = GetGeometry(oid);
		//	yield return shape;
		//}
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

	public void RefreshGeometries()
	{
		ClearGeometryCache(); // force reload
		CacheGeometries(_shapes.Keys.ToArray());

		foreach (var pair in _shapes)
		{
			var shape = GetGeometry(pair.Key);
			pair.Value.SetShape(shape);
		}
	}

	#endregion
}
