using ArcGIS.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.Commons.AGP.Core.Carto;

public enum ShapeSelectionState { Not, Partially, Entirely }

/// <summary>
/// Represents a selection of vertices of a shape (geometry).
/// Agnostic of the Geometry itself (must be provided externally).
/// </summary>
public interface IShapeSelection
{
	bool IsEmpty { get; }
	bool IsFully { get; }

	ShapeSelectionState GetSelectionState();
	ShapeSelectionState GetPartSelectionState(int partIndex);
	bool IsVertexSelected(int partIndex, int vertexIndex);

	/// <remarks>For multipoints, set part = vertex = point's index!</remarks>
	bool CombineVertex(int part, int vertex, SetCombineMethod method);
	bool CombinePart(int part, int vertexCount, SetCombineMethod method);
	//bool CombineShape(SetCombineMethod method);
	bool SetEmpty();

	IEnumerable<MapPoint> GetSelectedVertices(Geometry shape);
	IEnumerable<MapPoint> GetUnselectedVertices(Geometry shape);

	bool HitTestVertex(Geometry shape, MapPoint hitPoint, double tolerance);
}

public class ShapeSelection : IShapeSelection
{
	// Internal representation: list pairs (partIndex,vertexIndex)
	// for each selected vertex; if an entire part j is selected,
	// the pair (j,-1) is in the list and all other pairs (j,*)
	// will eventually be removed; if the entire shape is selected,
	// the pair (-1,-1) is in the list and all other pairs removed;
	// an empty list represents the empty selection (no vertices).

	private readonly List<Index> _items = new();
	private bool _itemsOrdered;

	//public Geometry Shape { get; }

	public bool IsEmpty => _items is null || _items.Count == 0;

	public bool IsFully => _items?.Contains(new Index()) ?? false;

	public ShapeSelectionState GetSelectionState()
	{
		if (_items is not null)
		{
			var key = new Index();
			if (_items.Contains(key))
				return ShapeSelectionState.Entirely;
			if (_items.Count > 0)
				return ShapeSelectionState.Partially;
		}
		return ShapeSelectionState.Not;
	}

	public ShapeSelectionState GetPartSelectionState(int partIndex)
	{
		if (_items is not null)
		{
			foreach (var item in OrderedItems)
			{
				if (item.PartIndex == partIndex)
				{
					return item.IsFullPart
						? ShapeSelectionState.Entirely
						: ShapeSelectionState.Partially;
				}
			}
		}

		return ShapeSelectionState.Not;
	}

	public bool IsVertexSelected(int partIndex, int vertexIndex)
	{
		if (_items is null) return false;

		return _items.Any(item => item.PartIndex == partIndex && item.VertexIndex == vertexIndex);
		// TODO cope with (j,*) and (*,*) items; use binary search on OrderedItems
	}

	public bool CombineShape(SetCombineMethod method) // TODO drop?
	{
		bool changed;

		switch (method)
		{
			case SetCombineMethod.Add:
			case SetCombineMethod.New:
				changed = !IsFully;
				_items.Clear();
				_items.Add(new Index());
				_itemsOrdered = true;
				break;
			case SetCombineMethod.Remove:
				changed = !IsEmpty;
				_items.Clear();
				_itemsOrdered = true;
				break;
			case SetCombineMethod.And:
				changed = false;
				// nothing to do
				break;
			case SetCombineMethod.Xor:
				// cannot flip without the geom; instead:
				// select all unless already fully selected
				changed = true;
				var fully = IsFully;
				_items.Clear();
				if (!fully)
					_items.Add(new Index());
				_itemsOrdered = true;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(method), method, null);
		}

		return changed;
	}

	public bool CombinePart(int partIndex, int vertexCount, SetCombineMethod method)
	{
		// Add: _items.Add(part,-1) unless exists; remove all items (part,>=0)
		// Remove: _items.Remove(part,*)
		// Xor: if exists item (part,-1) remove (part,*); else add (part,-1)
		// And: if exists (part,-1) remove all other items; else remove all items but (part,*)
		// New: _items.Clear(), then proceed as with Add
		throw new NotImplementedException();
	}

	public bool CombineVertex(int partIndex, int vertexIndex, SetCombineMethod method)
	{
		bool changed = false;

		if (method == SetCombineMethod.New)
		{
			if (!IsEmpty)
				changed = true;
			_items.Clear();
			_itemsOrdered = true;
			method = SetCombineMethod.Add;
		}

		// TODO this method is wrong if full shape or full part selected (low prio as presently user cannot create such selections)

		var candidate = new Index(partIndex, vertexIndex);
		int index;
		if (_itemsOrdered)
			index = _items.BinarySearch(candidate, IndexComparer.Singleton);
		else
			index = _items.IndexOf(candidate);

		switch (method)
		{
			case SetCombineMethod.Add:
				if (index < 0)
				{
					// not found: add it
					_items.Add(candidate);
					_itemsOrdered = false;
					changed = true;
				}
				//else: already in selection set, nothing to do
				break;

			case SetCombineMethod.Remove:
				if (index >= 0)
				{
					// found at index: remove it
					_items.RemoveAt(index);
					changed = true;
				}
				break;

			case SetCombineMethod.And:
				// probably not that useful for a single vertex:
				// remove all but the given vertex:
				changed = _items.Count != 1 || !Equals(_items[0], candidate);
				_items.Clear();
				if (index >= 0) // was contained
					_items.Add(candidate); // add again
				_itemsOrdered = true;
				break;

			case SetCombineMethod.Xor:
				if (index < 0)
				{
					_items.Add(candidate); // not found: add it
					_itemsOrdered = false;
					changed = true;
				}
				else
				{
					_items.RemoveAt(index); // found: remove it
					changed = true;
				}
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(method), method, null);
		}

		return changed;
	}

	public bool SetEmpty()
	{
		bool changed = !IsEmpty;
		_items.Clear();
		_itemsOrdered = true;
		return changed;
	}

	/// <remarks>Assumes given <paramref name="shape"/> is compatible
	/// with this shape selection; this assumption is not verified</remarks>
	public IEnumerable<MapPoint> GetSelectedVertices(Geometry shape)
	{
		if (shape is null) yield break;

		if (IsEmpty) yield break;

		if (IsFully)
		{
			foreach (var point in GetVertices(shape))
			{
				yield return point;
			}
		}
		else if (shape is MapPoint mapPoint)
		{
			yield return mapPoint;
		}
		else if (shape is Multipoint multipoint)
		{
			// by convention, a multipoint's ith point is both part i and vertex i
			foreach (var item in OrderedItems)
			{
				Assert.AreEqual(item.PartIndex, item.VertexIndex,
				                "Expect partIndex==vertexIndex for multipoint");
				yield return multipoint.Points[item.VertexIndex];
			}
		}
		else if (shape is Multipart multipart)
		{
			foreach (var item in OrderedItems)
			{
				if (item.IsFullPart)
				{
					foreach (var point in GetVertices(multipart, item.PartIndex))
					{
						yield return point;
					}
				}
				else
				{
					yield return GetPoint(multipart, item.PartIndex, item.VertexIndex);
				}
			}
		}
	}

	public IEnumerable<MapPoint> GetUnselectedVertices(Geometry shape)
	{
		if (shape is null) yield break;
		if (shape.IsEmpty) yield break;

		if (IsEmpty)
		{
			foreach (var point in GetVertices(shape))
			{
				yield return point;
			}
		}
		else if (IsFully)
		{
			// yield nothing: no unselected vertices
		}
		else if (shape is MapPoint mapPoint)
		{
			if (IsEmpty) yield return mapPoint;
		}
		else if (shape is Multipoint multipoint)
		{
			int pointCount = multipoint.PointCount;
			for (int i = 0; i < pointCount; i++)
			{
				if (! IsVertexSelected(i, i))
				{
					yield return multipoint.Points[i];
				}
			}
		}
		else if (shape is Multipart multipart)
		{
			int partCount = multipart.PartCount;
			for (int k = 0; k < partCount; k++)
			{
				var part = multipart.Parts[k];
				int segmentCount = part.Count;
				for (int i = 0; i < segmentCount; i++)
				{
					if (! IsVertexSelected(k, i))
					{
						yield return part[i].StartPoint;
					}
				}

				if (segmentCount > 0 && multipart is Polyline)
				{
					if (! IsVertexSelected(k, segmentCount))
					{
						yield return part[segmentCount - 1].EndPoint;
					}
				}
			}
		}
	}

	/// <returns>true iff any selected vertex is within
	/// <paramref name="tolerance"/> of <paramref name="hitPoint"/></returns>
	/// <remarks>Assumes given <paramref name="shape"/> is compatible
	/// with this shape selection; this assumption is not verified</remarks>
	public bool HitTestVertex(Geometry shape, MapPoint hitPoint, double tolerance)
	{
		if (shape is null || shape.IsEmpty) return false;
		if (hitPoint is null || hitPoint.IsEmpty) return false;

		var toleranceSquared = tolerance * tolerance;

		foreach (var vertex in GetSelectedVertices(shape))
		{
			var dx = vertex.X - hitPoint.X;
			var dy = vertex.Y - hitPoint.Y;
			var dd = dx * dx + dy * dy;
			if (dd <= toleranceSquared)
			{
				return true;
			}
		}

		return false;
	}

	private IReadOnlyList<Index> OrderedItems
	{
		get
		{
			if (!_itemsOrdered)
			{
				_items.Sort(IndexComparer.Singleton);
				_itemsOrdered = true;
				RemoveRedundantIndices(_items);
			}

			return _items;
		}
	}

	private static void RemoveRedundantIndices(List<Index> list)
	{
		// Assume list is sorted by part, then by vertex!
		// If (-1,*) in list, remove all other items; otherwise:
		// remove vertex indices (j,i) immediately after (j,-1),
		// i.e., individual vertices if an entire part is selected.

		if (list.Count < 1) return; // nothing to do

		if (list[0].IsFullShape)
		{
			list.RemoveRange(1, list.Count - 1);
			return;
		}

		int i = 0; // read index
		int j = 0; // write index
		while (i < list.Count)
		{
			list[j++] = list[i];

			if (list[i].IsFullPart)
			{
				int curPart = list[i].PartIndex;
				i += 1; // skip the neg vertex
				while (i < list.Count && list[i].PartIndex == curPart)
					i += 1;
			}
			else
			{
				i += 1;
			}
		}

		list.RemoveRange(j, i - j);
	}

	private static IEnumerable<MapPoint> GetVertices(Geometry shape)
	{
		if (shape is null) yield break;

		if (shape is MapPoint mapPoint)
		{
			yield return mapPoint;
		}
		else if (shape is Multipoint multipoint)
		{
			foreach (var point in multipoint.Points)
			{
				yield return point;
			}
		}
		else if (shape is Multipart multipart)
		{
			foreach (var point in multipart.Points)
			{
				yield return point;
			}
		}
		else
		{
			throw new NotSupportedException($"Geometry type not supported: {shape.GetType().Name}");
		}
	}

	private static IEnumerable<MapPoint> GetVertices(Geometry shape, int partIndex)
	{
		if (shape is null) yield break;
		if (partIndex < 0) yield break;
		if (shape is MapPoint mapPoint && partIndex == 0)
		{
			yield return mapPoint;
		}
		else if (shape is Multipoint multipoint && partIndex < multipoint.PointCount)
		{
			yield return multipoint.Points[partIndex];
		}
		else if (shape is Multipart multipart && partIndex < multipart.PartCount)
		{
			var segments = multipart.Parts[partIndex];
			var segmentCount = segments.Count;
			if (segmentCount < 1) yield break;
			for (int k = 0; k < segmentCount; k++)
			{
				var segment = segments[k];
				yield return segment.StartPoint;
			}

			if (multipart is Polyline)
			{
				yield return segments[segmentCount - 1].EndPoint;
			}
		}
	}

	private static MapPoint GetPoint(Geometry shape, int partIndex, int vertexIndex = -1)
	{
		if (shape is null)
			throw new ArgumentNullException(nameof(shape));

		if (shape is MapPoint point)
		{
			return point;
		}

		if (shape is Multipoint multipoint)
		{
			return multipoint.Points[partIndex];
		}

		if (shape is Multipart multipart)
		{
			var part = multipart.Parts[partIndex];
			var segmentCount = part.Count;

			return vertexIndex == segmentCount
					   ? part[vertexIndex - 1].EndPoint
					   : part[vertexIndex].StartPoint;
		}

		throw new NotSupportedException($"Shape of type {shape.GetType().Name} is not supported");
	}

	private class IndexComparer : IComparer<Index>
	{
		public int Compare(Index a, Index b)
		{
			if (a.PartIndex < b.PartIndex) return -1;
			if (a.PartIndex > b.PartIndex) return +1;
			if (a.VertexIndex < b.VertexIndex) return -1;
			if (a.VertexIndex > b.VertexIndex) return +1;
			return 0;
		}

		public static IndexComparer Singleton { get; } = new();
	}

	/// <summary>Address of a vertex in a geometry</summary>
	private readonly struct Index
	{
		public readonly int PartIndex;
		public readonly int VertexIndex;
		// TODO store -VertexCount in VertexIndex if full part (allows CombineVertex to work correctly)

		public Index()
		{
			PartIndex = -1;
			VertexIndex = -1;
		}

		public Index(int part, int vertex = -1)
		{
			PartIndex = part;
			VertexIndex = vertex;
		}

		public bool IsFullShape => PartIndex < 0;
		public bool IsFullPart => VertexIndex < 0;

		public override string ToString()
		{
			return $"Part={PartIndex}, Vertex={VertexIndex}";
		}
	}
}
