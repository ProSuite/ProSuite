using ArcGIS.Core.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Carto;

public enum ShapeSelectionState { Not, Partially, Entirely }

/// <summary>
/// Represents a selection of vertices of a shape (geometry).
/// </summary>
public interface IShapeSelection
{
	Geometry Shape { get; }
	bool IsEmpty { get; }
	bool IsFully { get; }

	int SelectedVertexCount { get; }
	ShapeSelectionState IsShapeSelected();
	ShapeSelectionState IsPartSelected(int partIndex);
	bool IsVertexSelected(int partIndex, int vertexIndex);

	/// <remarks>For multipoints, set part = vertex = point's index!</remarks>
	bool CombineVertex(int part, int vertex, SetCombineMethod method);
	bool CombinePart(int part, SetCombineMethod method);
	bool CombineShape(SetCombineMethod method);
	bool Clear();

	IEnumerable<MapPoint> GetSelectedVertices();
	IEnumerable<MapPoint> GetUnselectedVertices();

	bool SelectedVertex(MapPoint hitPoint, double tolerance, out MapPoint vertex);

	/// <param name="hitPoint">where the user clicked</param>
	/// <param name="distance">distance to vertex found or undefined</param>
	/// <param name="partIndex">part index of vertex found or undefined</param>
	/// <param name="vertexIndex">vertex index of vertex found or undefined</param>
	/// <param name="selected">whether the vertex found is selected</param>
	/// <returns>the vertex found within <paramref name="distance"/>
	/// of <paramref name="hitPoint"/> or null</returns>
	MapPoint NearestVertex(MapPoint hitPoint, out double distance,
	                   out int partIndex, out int vertexIndex, out bool selected);

	/// <summary>
	/// Notify this shape selection that the <see cref="Shape"/> has
	/// a new vertex. The new vertex will be unselected. The internal
	/// representation will be adjusted accordingly.
	/// </summary>
	void VertexAdded(int partIndex, int vertexIndex);

	/// <summary>
	/// Notify this shape selection that a vertex was removed from the
	/// <see cref="Shape"/>. The internal representation will be adjusted
	/// accordingly.
	/// </summary>
	void VertexRemoved(int partIndex, int vertexIndex);

	/// <summary>
	/// Notify this shape selection that the path at the given
	/// part index has been reversed. The internal representation
	/// will be adjusted accordingly.
	/// </summary>
	void PathReversed(int partIndex);

	/// <summary>
	/// Replace <see cref="Shape"/> with the given <paramref name="newShape"/>,
	/// even if it is not compatible (see <see cref="IsCompatible"/>) with the
	/// current shape/selection.
	/// </summary>
	void UpdateShape(Geometry newShape);

	/// <returns>true iff given shape is compatible with this selection</returns>
	/// <remarks>shape and selection are compatible if part and vertex counts are in range</remarks>
	bool IsCompatible(Geometry geometry, out string message);
}

public class ShapeSelection : IShapeSelection
{
	private readonly BlockList _blocks;

	public ShapeSelection(Geometry shape)
	{
		Shape = shape ?? throw new ArgumentNullException(nameof(shape));
		_blocks = new BlockList();
	}

	public Geometry Shape { get; private set; }

	public bool IsEmpty => _blocks.IsEmpty;

	public bool IsFully => IsShapeSelected() == ShapeSelectionState.Entirely;

	public int SelectedVertexCount => _blocks.Sum(b => b.Count);

	public bool IsVertexSelected(int partIndex, int vertexIndex)
	{
		if (Shape is Multipoint)
		{
			// outside world: a multipoint's points are its parts
			// inside representation: all points in part 0 (for efficiency)
			vertexIndex = partIndex;
			partIndex = 0;
		}

		return _blocks.IsSelected(partIndex, vertexIndex);
	}

	public ShapeSelectionState IsPartSelected(int partIndex)
	{
		if (partIndex < 0)
			throw new ArgumentOutOfRangeException(nameof(partIndex));
		int partCount = GetPartCount(Shape);
		if (partIndex >= partCount)
			throw new ArgumentOutOfRangeException(nameof(partIndex));

		if (Shape is Multipoint)
		{
			// outside world: a multipoint's points are its parts
			// inside representation: all points in part 0 (for efficiency)
			return _blocks.IsSelected(0, partIndex)
				       ? ShapeSelectionState.Entirely
				       : ShapeSelectionState.Not;
		}

		int partVertexCount = GetVertexCount(Shape, partIndex);
		int selectedVertexCount = _blocks.CountSelected(partIndex);

		var result = ShapeSelectionState.Partially;
		if (selectedVertexCount >= partVertexCount)
			result = ShapeSelectionState.Entirely;
		else if (selectedVertexCount <= 0)
			result = ShapeSelectionState.Not;

		return result;
	}

	public ShapeSelectionState IsShapeSelected()
	{
		int totalVertexCount = GetVertexCount(Shape);
		int selectedVertexCount = _blocks.CountSelected();

		var result = ShapeSelectionState.Partially;
		if (selectedVertexCount >= totalVertexCount)
			result = ShapeSelectionState.Entirely;
		else if (selectedVertexCount <= 0)
			result = ShapeSelectionState.Not;

		return result;
	}

	public bool CombineVertex(int partIndex, int vertexIndex, SetCombineMethod method)
	{
		if (Shape is Multipoint)
		{
			// outside world: a multipoint's points are its parts
			// inside representation: all points in part 0 (for efficiency)
			vertexIndex = partIndex;
			partIndex = 0;
		}

		bool changed = false;

		if (method == SetCombineMethod.New)
		{
			changed = Clear();
			method = SetCombineMethod.Add;
		}

		if (method == SetCombineMethod.Add)
		{
			var added = _blocks.Select(partIndex, vertexIndex);
			return changed || added;
		}

		if (method == SetCombineMethod.Remove)
		{
			var removed = _blocks.Unselect(partIndex, vertexIndex);
			return changed || removed;
		}

		if (method == SetCombineMethod.And)
		{
			// remove all but the given vertex:
			var contained = _blocks.IsSelected(partIndex, vertexIndex);
			if (contained && _blocks.CountSelected() == 1) return false;
			Clear();
			if (contained) _blocks.Select(partIndex, vertexIndex);
			return true;
		}

		if (method == SetCombineMethod.Xor)
		{
			var contained = _blocks.IsSelected(partIndex, vertexIndex);
			if (contained)
				_blocks.Unselect(partIndex, vertexIndex);
			else _blocks.Select(partIndex, vertexIndex);
			return true;
		}

		throw new ArgumentOutOfRangeException(nameof(method), method, null);
	}

	public bool CombinePart(int partIndex, SetCombineMethod method)
	{
		throw new NotImplementedException();
	}

	public bool CombineShape(SetCombineMethod method)
	{
		bool changed;

		switch (method)
		{
			case SetCombineMethod.Add:
			case SetCombineMethod.New:
				changed = AddShape(Shape, _blocks);
				break;
			case SetCombineMethod.Remove:
				changed = _blocks.Clear();
				break;
			case SetCombineMethod.And:
				changed = false;
				// nothing to do
				break;
			case SetCombineMethod.Xor:
				changed = XorShape(Shape, _blocks);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(method), method, null);
		}

		return changed;
	}

	public bool Clear()
	{
		return _blocks.Clear();
	}

	public bool SelectedVertex(MapPoint hitPoint, double tolerance, out MapPoint vertex)
	{
		if (hitPoint is null || hitPoint.IsEmpty)
		{
			vertex = null;
			return false;
		}

		var toleranceSquared = tolerance * tolerance;

		foreach (var candidate in GetSelectedVertices())
		{
			var dx = candidate.X - hitPoint.X;
			var dy = candidate.Y - hitPoint.Y;
			var dd = dx * dx + dy * dy;
			if (dd <= toleranceSquared)
			{
				vertex = candidate;
				return true;
			}
		}

		vertex = null;
		return false;
	}

	public MapPoint NearestVertex(MapPoint hitPoint, out double distance,
	                              out int partIndex, out int vertexIndex, out bool selected)
	{
		distance = double.NaN;
		partIndex = vertexIndex = -1;
		selected = false;

		if (hitPoint is null || hitPoint.IsEmpty)
		{
			return null;
		}

		MapPoint minVertex = null;
		var minDistSquared = double.MaxValue;

		foreach (var tuple in GetVertices())
		{
			MapPoint candidate = tuple.Item1;
			var dx = candidate.X - hitPoint.X;
			var dy = candidate.Y - hitPoint.Y;
			var dd = dx * dx + dy * dy;
			if (dd < minDistSquared)
			{
				minDistSquared = dd;
				minVertex = candidate;
				partIndex = tuple.Item2;
				vertexIndex = tuple.Item3;
				selected = tuple.Item4;
			}
		}

		if (minVertex is not null)
		{
			distance = Math.Sqrt(minDistSquared);
		}

		return minVertex;
	}

	/// <returns>Yield tuples (point,part,vertex,selected)</returns>
	private IEnumerable<ValueTuple<MapPoint, int, int, bool>> GetVertices()
	{
		var shape = Shape;

		if (shape is MapPoint mapPoint)
		{
			bool selected = _blocks.IsSelected(0, 0);
			yield return (mapPoint, 0, 0, selected);
		}
		else if (shape is Multipoint multipoint)
		{
			var pointCount = multipoint.PointCount;
			int i = 0;
			foreach (var block in _blocks)
			{
				for (; i < block.First; i++)
				{
					yield return (multipoint.Points[i], i, i, false);
				}

				Debug.Assert(i == block.First);

				for (; i < block.First + block.Count; i++)
				{
					yield return (multipoint.Points[i], i, i, true);
				}
			}

			for (; i < pointCount; i++)
			{
				yield return (multipoint.Points[i], i, i, false);
			}
		}
		else if (shape is Multipart multipart)
		{
			int k = 0, i = 0;
			foreach (var block in _blocks)
			{
				// whole parts before current block:
				for (; k < block.Part; k++, i=0)
				{
					int vertexCount = GetVertexCount(multipart, k);
					if (multipart is Polygon) vertexCount -= 1;
					for (; i < vertexCount; i++)
					{
						var point = GetPoint(multipart, k, i);
						yield return (point, k, i, false);
					}
				}
				Debug.Assert(k == block.Part);
				// vertices before current block:
				for (; i < block.First; i++)
				{
					var point = GetPoint(multipart, k, i);
					yield return (point, k, i, false);
				}
				Debug.Assert(i == block.First);
				// vertices in current block:
				for (; i < block.First + block.Count; i++)
				{
					var point = GetPoint(multipart, k, i);
					yield return (point, k, i, true);
				}
			}
			// vertices after last block:
			int partCount = GetPartCount(multipart);
			for (; k < partCount; k++)
			{
				int vertexCount = GetVertexCount(multipart, k);
				if (multipart is Polygon) vertexCount -= 1;
				for (; i < vertexCount; i++)
				{
					var point = GetPoint(multipart, k, i);
					yield return (point, k, i, false);
				}
			}
		}
	}

	public IEnumerable<MapPoint> GetSelectedVertices()
	{
		return GetVertices(Shape, _blocks);
	}

	public IEnumerable<MapPoint> GetUnselectedVertices()
	{
		var blocks = Invert(_blocks, Shape);

		return GetVertices(Shape, blocks);
	}

	/// <summary>
	/// Yield a new sequence of blocks that represent those
	/// vertices of <paramref name="shape"/> that are not
	/// addressed by the given <paramref name="blocks"/>.
	/// </summary>
	public static IEnumerable<BlockList.Block> Invert(
		IEnumerable<BlockList.Block> blocks, Geometry shape)
	{
		if (blocks is null)
			throw new ArgumentNullException(nameof(blocks));

		if (shape is null || shape.IsEmpty)
		{
			return Enumerable.Empty<BlockList.Block>();
		}

		if (shape is MapPoint)
		{
			return blocks.Any()
				       ? Enumerable.Empty<BlockList.Block>()
				       : Enumerable.Repeat(new BlockList.Block(0, 0, 1), 1);
		}

		if (shape is Multipoint multipoint)
		{
			return Invert(blocks, multipoint);
		}

		if (shape is Multipart multipart)
		{
			return Invert(blocks, multipart);
		}

		throw new NotSupportedException($"Shape of type {shape.GetType().Name} is not supported");
	}

	public bool IsCompatible(Geometry shape, out string message)
	{
		message = string.Empty;
		if (IsEmpty) return true;

		if (shape is null)
		{
			message = "Shape is null (but selection is not empty)";
			return false;
		}

		if (shape.IsEmpty)
		{
			message = "Shape is empty (but selection is not empty)";
			return false;
		}

		if (shape.GeometryType != Shape.GeometryType)
		{
			message = $"Geometry type differs: {shape.GeometryType} (requested) vs. {Shape.GeometryType} (in selection)";
			return false;
		}

		int yourParts = GetPartCount(shape);
		int myParts = GetPartCount(Shape);
		if (yourParts != myParts)
		{
			message = $"Number of parts differ: {yourParts} (requested) vs. {myParts} (in selection)";
			return false;
		}

		if (shape is Multipart yourMultipart && Shape is Multipart myMultipart)
		{
			int m = yourMultipart.PartCount;
			if (m != myMultipart.PartCount)
			{
				message = $"Number of parts differ: {yourParts} (requested) vs. {myParts} (in selection)";
				return false;
			}

			for (int k = 0; k < m; k++)
			{
				int myVertexCount = GetVertexCount(myMultipart, k);
				int yourVertexCount = GetVertexCount(yourMultipart, k);
				if (myVertexCount != yourVertexCount)
				{
					message = $"Number of vertices in part {k} differ: {yourVertexCount} (requested) vs. {myVertexCount} (in selection)";
					return false;
				}
			}
		}

		if (shape.PointCount != Shape.PointCount)
		{
			message = $"Number of points differ: {shape.PointCount} (requested) vs. {Shape.PointCount} (in selection)";
			return false;
		}

		message = string.Empty;
		return true;
	}

	public void UpdateShape(Geometry newShape)
	{
		Shape = newShape ?? throw new ArgumentNullException(nameof(newShape));
	}

	/// <summary>
	/// Call after a vertex has been added to the shape.
	/// For Multipoints, let partIndex=vertexIndex=point's index.
	/// </summary>
	public void VertexAdded(int partIndex, int vertexIndex)
	{
		if (Shape is Multipoint)
		{
			// outside world: a multipoint's points are its parts
			// inside representation: all points in part 0 (for efficiency)
			vertexIndex = partIndex;
			partIndex = 0;
		}

		_blocks.VertexAdded(partIndex, vertexIndex);
	}

	/// <summary>
	/// Call after a vertex has been removed from the shape.
	/// For Multipoints, let partIndex=vertexIndex=point's index.
	/// </summary>
	public void VertexRemoved(int partIndex, int vertexIndex)
	{
		if (Shape is Multipoint)
		{
			// outside world: a multipoint's points are its parts
			// inside representation: all points in part 0 (for efficiency)
			vertexIndex = partIndex;
			partIndex = 0;
		}

		_blocks.VertexRemoved(partIndex, vertexIndex);
	}

	public void PathReversed(int partIndex)
	{
		int numVerticesInPart = GeometryUtils.GetPointCount(Shape, partIndex);
		_blocks.PartReversed(partIndex, numVerticesInPart);
	}

	#region Private methods

	private static IEnumerable<BlockList.Block> Invert(
		IEnumerable<BlockList.Block> blocks, Multipoint multipoint)
	{
		var pointCount = multipoint.PointCount;
		int i = 0;

		foreach (var block in blocks)
		{
			if (i < block.First)
			{
				yield return new BlockList.Block(0, i, block.First - i);
			}

			i = block.First + block.Count;
		}

		if (i < pointCount)
		{
			yield return new BlockList.Block(0, i, pointCount - i);
		}
	}

	private static IEnumerable<BlockList.Block> Invert(
		IEnumerable<BlockList.Block> blocks, Multipart multipart)
	{
		int k = 0;
		int i = 0;
		int vertexCount; // in current part

		foreach (var block in blocks)
		{
			// emit vertex ranges before block:
			while (k < block.Part)
			{
				// remaining vertices in part or entire part:
				vertexCount = GetVertexCount(multipart, k);
				if (multipart is Polygon) vertexCount -= 1;
				if (i < vertexCount)
				{
					yield return new BlockList.Block(k, i, vertexCount - i);
				}
				// go to first vertex in next part:
				i = 0;
				k += 1;
			}

			if (i < block.First)
			{
				// first vertices in current block's part:
				yield return new BlockList.Block(k, i, block.First - i);
			}
			i = block.First + block.Count;
		}

		// emit vertex ranges after last block:
		int partCount = GetPartCount(multipart);
		while (k < partCount)
		{
			vertexCount = GetVertexCount(multipart, k);
			if (multipart is Polygon) vertexCount -= 1;
			if (i < vertexCount)
			{
				yield return new BlockList.Block(k, i, vertexCount - i);
			}
			// go to first vertex in next part:
			i = 0;
			k += 1;
		}
	}

	private static IEnumerable<MapPoint> GetVertices(Geometry shape, IEnumerable<BlockList.Block> blocks)
	{
		if (shape is null) yield break;
		if (shape.IsEmpty) yield break;

		if (blocks is null)
			throw new ArgumentNullException(nameof(blocks));

		if (shape is MapPoint mapPoint)
		{
			if (blocks.Any())
			{
				yield return mapPoint;
			}
		}
		else if (shape is Multipoint multipoint)
		{
			foreach (var block in blocks)
			{
				// ignore block.Part: for multipoints it's assumed to be always zero
				for (int i = 0; i < block.Count; i++)
				{
					yield return multipoint.Points[block.First + i];
				}
			}
		}
		else if (shape is Multipart multipart)
		{
			foreach (var block in blocks)
			{
				if (block.First < 0) // entire part
				{
					foreach (var point in GetVertices(multipart, block.Part))
					{
						yield return point;
					}
				}
				else
				{
					for (int i = 0; i < block.Count; i++)
					{
						yield return GetPoint(multipart, block.Part, block.First + i);
					}
				}
			}
		}
	}

	private static int GetPartCount(Geometry shape)
	{
		return GeometryUtils.GetPartCount(shape);
	}

	private static int GetVertexCount(Geometry shape, int partIndex = -1)
	{
		return partIndex < 0
			       ? GeometryUtils.GetPointCount(shape)
			       : GeometryUtils.GetPointCount(shape, partIndex);
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

	private static MapPoint GetPoint(Multipart multipart, int partIndex, int vertexIndex)
	{
		var part = multipart.Parts[partIndex];
		var segmentCount = part.Count;

		return vertexIndex == segmentCount
			       ? part[vertexIndex - 1].EndPoint
			       : part[vertexIndex].StartPoint;
	}

	private static bool AddShape(Geometry shape, BlockList blocks)
	{
		if (shape is null)
			throw new ArgumentNullException(nameof(shape));
		if (blocks is null)
			throw new ArgumentNullException(nameof(blocks));

		if (shape.IsEmpty) return false;

		var selected = blocks.CountSelected();
		var total = GeometryUtils.GetPointCount(shape);
		var changed = selected != total; // not already entirely selected

		if (shape is MapPoint)
		{
			blocks.Clear();
			blocks.Select(0, 0);
		}
		else if (shape is Multipoint multipoint)
		{
			blocks.Clear();
			blocks.Select(0, 0, multipoint.PointCount);
		}
		else if (shape is Multipart multipart)
		{
			blocks.Clear();
			int partCount = multipart.PartCount;
			for (int k = 0; k < partCount; k++)
			{
				var part = multipart.Parts[k];
				var segmentCount = part.Count;
				var vertexCount = multipart is Polygon ? segmentCount : segmentCount + 1;
				blocks.Select(k, 0, vertexCount);
			}
		}
		else
		{
			throw new NotSupportedException($"Shape type {shape.GetType().Name} is not supported");
		}

		return changed;
	}

	private static bool XorShape(Geometry shape, BlockList blocks)
	{
		if (shape is null)
			throw new ArgumentNullException(nameof(shape));
		if (blocks is null)
			throw new ArgumentNullException(nameof(blocks));

		if (shape.IsEmpty) return false;

		var inverted = Invert(blocks, shape);

		blocks.Clear();

		foreach (var block in inverted)
		{
			blocks.Select(block.Part, block.First, block.Count);
		}

		return true;
	}

	#endregion
}

/// <summary>
/// Linked list of blocks of sequences of contiguous vertices,
/// always ordered by (part, first vertex). A new part always
/// starts a new block. Blocks never overlap (such blocks are merged).
/// Represents selected vertices of a shape/geometry.
/// </summary>
/// <remarks>Not thread-safe!</remarks>
public class BlockList : IEnumerable<BlockList.Block>
{
	// _head.Next points to first real node
	// _free points directly to recycled nodes
	private readonly Node _head = new(-1, 0, 0);
	private Node _free;

	public bool IsEmpty => _head.Next is null;

	public bool Select(int part, int vertex, int count = 1)
	{
		// if vertex is in existing block: nothing to do
		// if vertex abuts existing block: grow this block
		// if vertex is in existing block: grow this block and merge down the list
		// otherwise: append new block to before and merge down the list

		var node = Find(part, vertex, out var before);

		if (node is not null)
		{
			if (Contains(node, part, vertex, count))
			{
				return false; // already fully contained
			}

			// args extend beyond node: grow node to contain args
			var beyond = Math.Max(node.First + node.Count, vertex + count);
			node.Count = beyond - vertex;

			MergeBlocks(node);
		}
		else
		{
			node = NewNode(part, vertex, count);
			node.Next = before.Next;
			before.Next = node;

			MergeBlocks(before.IsHead ? node : before);
		}

		return true;
	}

	public bool Unselect(int part, int vertex)
	{
		var node = Find(part, vertex, out Node before);

		if (node is null) return false; // was not in list

		Assert.NotNull(before, "before must not be null");
		Assert.True(node.Part == part, "unexpected part index");

		// cut from node if at start or end
		// remove node if it becomes empty
		// split node if in the interior

		if (node.First == vertex)
		{
			node.First += 1;
			node.Count -= 1;
			if (node.Count <= 0)
			{
				before.Next = node.Next;
				FreeNode(node);
			}
			return true;
		}

		if (node.First + node.Count - 1 == vertex)
		{
			node.Count -= 1;
			if (node.Count <= 0)
			{
				before.Next = node.Next;
				FreeNode(node);
			}
			return true;
		}

		var postCount = node.First + node.Count - vertex - 1;
		var next = NewNode(node.Part, vertex + 1, postCount);
		next.Next = node.Next;

		node.Count = vertex - node.First;
		node.Next = next;

		return true;
	}

	public bool Clear()
	{
		var wasEmpty = IsEmpty;

		// release each node (chain into free list):

		var next = _head.Next;
		while (next is not null)
		{
			var node = next;
			next = node.Next;
			FreeNode(node);
		}

		// and clear block list:

		_head.Next = null;

		return ! wasEmpty;
	}

	public bool IsSelected(int part, int vertex)
	{
		var node = Find(part, vertex, out Node _);
		return node is not null;
	}

	public int CountSelected(int part = -1)
	{
		int count = 0;

		for (var node = _head.Next; node is not null; node = node.Next)
		{
			if (part >= 0 && node.Part > part) break;
			if (part >= 0 && node.Part < part) continue;
			count += node.Count;
		}

		return count;
	}

	/// <summary>
	/// Adjust the selection after a vertex has been added to the shape
	/// </summary>
	public void VertexAdded(int part, int vertex)
	{
		// Have ordered list of blocks b=(p,f,c)
		// See where given (p,v) falls into this list:
		// - if selected (ie, in a block b with b.p==p and b.f <= v < b.f+b.c):
		//   - set b.c += 1 on block (new vertex will be selected)
		//   - for remaining blocks in same part: set b.f += 1
		// - otherwise (ie, unselected):
		//   - for all blocks b (b.p==p, b.f > v): set b.f += 1
		//   - new vertex will be unselected

		var node = Find(part, vertex, out var before);

		if (node is not null)
		{
			// (p,v) is in a block (adjacent to or within selected vertices):
			// enlarge this block and shift up all following blocks in same part

			node.Count += 1;

			node = node.Next;
			while (node is not null && node.Part == part)
			{
				node.First += 1;
				node = node.Next;
			}
		}
		else
		{
			// (p,v) is not in a block:
			// shift up all blocks b with b.p==p and b.f > v

			node = before?.Next;
			while (node is not null && node.Part == part)
			{
				node.First += 1;
				node = node.Next;
			}
		}
	}

	/// <summary>
	/// Adjust the selection after a vertex has been removed from the shape
	/// </summary>
	public void VertexRemoved(int part, int vertex)
	{
		// Have ordered list of blocks b=(p,f,c)
		// See where given (p,v) falls into this list:
		// - if selected, i.e., in a block b (b.p==p and b.f <= v < b.f+b.c):
		//   - set b.c -= 1 on block (and remove if empty)
		//   - set b.f -= 1 in all remaining blocks in same part
		// - otherwise (i.e., unselected):
		//   - for all blocks b (b.p==p, b.f > v): set b.f -= 1

		var node = Find(part, vertex, out var before);

		if (node is not null)
		{
			// (p,v) is selected, thus in a block:
			// shorten this block and shift down all following blocks in same part

			Assert.NotNull(before, "before must not be null");
			Assert.True(node.Part == part, "unexpected part index");

			// Shorten (and remember) the containing block:

			node.Count -= 1;
			Node container = node;

			// Shift down all following blocks in same part:

			node = node.Next;
			while (node is not null && node.Part == part)
			{
				node.First -= 1;
				node = node.Next;
			}

			// Remove containing block if it became empty:

			if (container.Count <= 0)
			{
				before.Next = container.Next;
				FreeNode(container);
			}
		}
		else
		{
			// (p,v) is not in a block:
			// shift down all blocks b in same part with b.f > v

			node = before?.Next;
			while (node is not null && node.Part == part)
			{
				Assert.True(node.First > vertex, "unexpected first vertex");
				node.First -= 1;
				node = node.Next;
			}

			// Shifting might have brought blocks adjacent:

			MergeBlocks(before);
		}
	}

	public void PartReversed(int part, int numVerticesInPart)
	{
		var node = Find(part, 0, out var before);

		if (node is null)
		{
			node = before.Next;
		}

		var list = new List<Node>();

		while (node is not null && node.Part == part)
		{
			list.Add(node);
			node = node.Next;
		}

		Node tail = node;

		if (list.Count < 1)
		{
			return; // no selected vertices in given part
		}

		int last = list.Count - 1;

		int minVertexCount = list[last].First + list[last].Count;
		if (numVerticesInPart < minVertexCount)
		{
			throw new ArgumentOutOfRangeException(
				nameof(numVerticesInPart), numVerticesInPart,
				$"must be at least {minVertexCount} for this selection");
		}

		list.Reverse();

		for (int i = 0; i < last; i++)
		{
			list[i].First = numVerticesInPart - list[i].First - list[i].Count;
			list[i].Next = list[i + 1];
		}

		list[last].First = numVerticesInPart - list[last].First - list[last].Count;
		list[last].Next = tail;

		before.Next = list[0];
	}

	[MustDisposeResource]
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	[MustDisposeResource]
	public IEnumerator<Block> GetEnumerator()
	{
		return GetBlocks().GetEnumerator();
	}

	private IEnumerable<Block> GetBlocks(int part = -1)
	{
		for (var node = _head.Next; node is not null; node = node.Next)
		{
			if (part >= 0 && node.Part > part) break;
			if (part >= 0 && node.Part < part) continue;
			yield return new Block(node.Part, node.First, node.Count);
		}
	}

	private static bool Contains(Node node, int part, int first, int count = 1)
	{
		if (node is null) return false;
		if (node.Part != part) return false;
		var nodeLast = node.First + node.Count;
		var last = first + count;
		return node.First <= first && last <= nodeLast;
	}

	private void MergeBlocks(Node node)
	{
		if (node is null) return;

		var next = node.Next;

		while (next is not null && node.Part == next.Part && node.First + node.Count >= next.First)
		{
			// also hande case where node extends beyond the end of next:
			node.Count = Math.Max(node.Count, next.First + next.Count - node.First);
			node.Next = next.Next;
			FreeNode(next);

			next = node.Next;
		}
	}

	/// <summary>
	/// Get the node that contains the addressed vertex, or
	/// null if no node contains this vertex. In both cases,
	/// <paramref name="before"/> will be the node just before
	/// the one that contains the vertex, or where a node for
	/// the vertex would have to be inserted.
	/// </summary>
	private Node Find(int part, int vertex, out Node before)
	{
		if (part < 0)
			throw new ArgumentOutOfRangeException(nameof(part));
		if (vertex < 0)
			throw new ArgumentOutOfRangeException(nameof(vertex));

		before = _head;

		for (Node node = _head.Next; node is not null; before = node, node = node.Next)
		{
			if (node.Part > part) break;
			if (node.Part == part)
			{
				//if (node.IsWholePart) return node;
				if (node.First > vertex) break; // node is already beyond
				if (node.First <= vertex && vertex < node.First + node.Count)
				{
					return node;
				}
			}
		}

		return null; // not found
	}

	private Node NewNode(int part, int first, int count = 1)
	{
		if (_free is not null)
		{
			var node = _free;
			_free = _free.Next;
			node.Set(part, first, count);
			node.Next = null;
			return node;
		}

		return new Node(part, first, count);
	}

	private void FreeNode(Node node)
	{
		if (node is null) return;
		node.Set(0, 0, 0);
		node.Next = _free;
		_free = node;
	}

	/// <summary>
	/// Internal representation of a <see cref="Block"/>:
	/// has a next pointer to implement a linked list of blocks
	/// </summary>
	private class Node
	{
		public int Part { get; private set; }
		public int First { get; set; }
		public int Count { get; set; }
		public Node Next { get; set; }

		public Node(int part, int first, int count = 1, Node next = null)
		{
			Part = part;
			First = first;
			Count = count;
			Next = next;
		}

		public void Set(int part, int first, int count = 1)
		{
			Part = part;
			First = first;
			Count = count;
		}

		//public bool IsWholePart => First < 0;
		internal bool IsHead => Part < 0;

		public override string ToString()
		{
			return IsHead ? "Head" : $"Part {Part}, First {First}, Count {Count}";
		}
	}

	/// <summary>
	/// A sequence of consecutive vertices in a part of a geometry.
	/// </summary>
	public readonly struct Block
	{
		public readonly int Part;
		public readonly int First;
		public readonly int Count;

		public Block(int part, int first, int count)
		{
			Part = part;
			First = first;
			Count = count;
		}

		public override string ToString()
		{
			return $"Part {Part}, First {First}, Count {Count}";
		}
	}
}
