using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.Commons.AGP.Carto;

/// <summary>
/// A selection of vertices from involved features in a feature layer.
/// The name "white selection" is for historical reasons (ArcMap).
/// </summary>
public interface IWhiteSelection
{
	FeatureLayer Layer { get; }

	bool Add(long oid, bool selectVertices = false); // add involved feature, optionally select all vertices
	bool Remove(long oid); // also removes oid's geom from cache
	bool Combine(long oid, int part, int vertex, SetCombineMethod method);
	bool Combine(long oid, SetCombineMethod method);
	bool Clear(); // true iff changed, even if only an "empty oid" is removed; cache not cleared

	int InvolvedFeatureCount { get; }
	int SelectedVertexCount { get; }

	bool SelectedVertex(MapPoint hitPoint, double tolerance, out MapPoint vertex);

	IEnumerable<long> GetInvolvedOIDs();

	IEnumerable<Geometry> GetInvolvedShapes();

	IEnumerable<MapPoint> GetSelectedVertices();

	IEnumerable<MapPoint> GetUnselectedVertices();

	IShapeSelection GetShapeSelection(long oid);

	// Implementation has a cache oid => Geometry
	Geometry GetGeometry(long oid);
	void CacheGeometries(params long[] oid);
	void ClearGeometryCache();

	/// <summary>
	/// Reload (given or all) geometries from data store.
	/// If a geometry is no longer compatible with the current
	/// shape selection, this shape is removed from the selection!
	/// </summary>
	/// <param name="oids">Optional list of OIDs (if missing: reload all)</param>
	/// <returns>List updated (shape compatible with selection) and removed
	/// (shape incompatible with selection) OIDs</returns>
	List<RefreshInfo> RefreshGeometries(IEnumerable<long> oids = null);

	public class RefreshInfo
	{
		public readonly long OID;
		public readonly RefreshState State;
		public readonly string OptionalDetails;

		private RefreshInfo(long oid, RefreshState state, string details = null)
		{
			OID = oid;
			State = state;
			OptionalDetails = details;
		}

		public static RefreshInfo Retained(long oid)
		{
			return new RefreshInfo(oid, RefreshState.SelectionRetained);
		}

		public static RefreshInfo Modified(long oid, string details = null)
		{
			return new RefreshInfo(oid, RefreshState.SelectionModified, details);
		}

		public override string ToString()
		{
			return $"{State} OID {OID} (details: {OptionalDetails ?? "none"})";
		}
	}

	public enum RefreshState
	{
		SelectionRetained, // new shape was compatible, selection retained
		SelectionModified, // incompatible new shape, selection modified (usually cleared)
	}
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

	public int InvolvedFeatureCount => _shapes.Count;

	public int SelectedVertexCount => _shapes.Values.Sum(ss => ss.SelectedVertexCount);

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

	public bool Combine(long oid, SetCombineMethod method)
	{
		if (! _shapes.TryGetValue(oid, out var selection))
		{
			var shape = GetGeometry(oid);
			selection = new ShapeSelection(shape);
			_shapes.Add(oid, selection);
		}

		return selection.CombineShape(method);
	}

	public bool Remove(long oid)
	{
		_geometryCache.Remove(oid);
		return _shapes.Remove(oid);
	}

	public bool Add(long oid, bool selectVertices = false)
	{
		bool changed = false;

		if (! _shapes.TryGetValue(oid, out var selection))
		{
			var shape = GetGeometry(oid);
			selection = new ShapeSelection(shape);
			_shapes.Add(oid, selection);
			changed = true;
		}

		if (selectVertices)
		{
			if (selection.CombineShape(SetCombineMethod.Add))
			{
				changed = true;
			}
		}

		return changed;
	}

	public bool Clear()
	{
		var changed = _shapes.Count > 0;
		_shapes.Clear();
		return changed;
	}

	public bool SelectedVertex(MapPoint hitPoint, double tolerance, out MapPoint vertex)
	{
		// TODO This finds *first* in tolerance, should get *closest* instead
		foreach (var pair in _shapes)
		{
			var selection = pair.Value;

			if (selection.SelectedVertex(hitPoint, tolerance, out vertex))
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
			var selection = pair.Value;

			foreach (var vertex in selection.GetSelectedVertices())
			{
				yield return vertex;
			}
		}
	}

	public IEnumerable<MapPoint> GetUnselectedVertices()
	{
		foreach (var pair in _shapes)
		{
			var selection = pair.Value;

			foreach (var vertex in selection.GetUnselectedVertices())
			{
				yield return vertex;
			}
		}
	}

	public IEnumerable<Geometry> GetInvolvedShapes()
	{
		return _shapes.Values.Select(sel => sel.Shape);
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
		var missingOids = oids.Where(oid => !HasGeometry(oid)).ToList();
		if (missingOids.Any())
		{
			ReloadGeometries(missingOids);
		}
	}

	private void ReloadGeometries(IReadOnlyList<long> objectIDs)
	{
		if (objectIDs is null || objectIDs.Count < 1) return;

		foreach (var oid in objectIDs)
		{
			_geometryCache.Remove(oid);
		}

		using var featureClass = Layer.GetFeatureClass();
		if (featureClass is null) return; // layer invalid (during shutdown/map removal/etc)

		using var definition = featureClass.GetDefinition();

		var shapeField = definition.GetShapeField();
		var oidField = definition.GetObjectIDField();

		var filter = new QueryFilter { SubFields = $"{oidField},{shapeField}" };

		filter.ObjectIDs = objectIDs;

		using var cursor = featureClass.Search(filter);
		if (cursor is null) return; // paranoia

		while (cursor.MoveNext())
		{
			using var feature = (Feature)cursor.Current;
			var oid = feature.GetObjectID();
			var shape = feature.GetShape();
			_geometryCache[oid] = shape;
		}
	}

	public void ClearGeometryCache()
	{
		_geometryCache.Clear();
	}

	public List<IWhiteSelection.RefreshInfo> RefreshGeometries(IEnumerable<long> oids = null)
	{
		if (oids is null) oids = _shapes.Keys;
		else oids = oids.Intersect(_shapes.Keys);

		var list = oids.ToList();

		// (re)load into geometry cache:

		ReloadGeometries(list);

		// update on IShapeSelection objects, if still compatible:

		var result = new List<IWhiteSelection.RefreshInfo>();

		foreach (var oid in list)
		{
			var newShape = GetGeometry(oid);
			var selection = GetShapeSelection(oid) ??
			                throw new AssertionException($"No shape selection for OID {oid}");

			selection.UpdateShape(newShape, out bool cleared, out string reason);

			result.Add(cleared
				           ? IWhiteSelection.RefreshInfo.Modified(oid, reason)
				           : IWhiteSelection.RefreshInfo.Retained(oid));
		}

		return result;
	}

	#endregion
}
