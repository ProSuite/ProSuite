using ArcGIS.Core.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
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

	ShapeSelectionState IsShapeSelected();
	ShapeSelectionState IsPartSelected(int partIndex);
	bool IsVertexSelected(int partIndex, int vertexIndex);

	/// <remarks>For multipoints, set part = vertex = point's index!</remarks>
	bool CombineVertex(int part, int vertex, SetCombineMethod method);
	bool CombinePart(int part, SetCombineMethod method);
	bool CombineShape(SetCombineMethod method);
	bool SetEmpty();

	IEnumerable<MapPoint> GetSelectedVertices();
	IEnumerable<MapPoint> GetUnselectedVertices();

	/// <param name="hitPoint">where the user clicked</param>
	/// <param name="tolerance">maximum allowed distance</param>
	/// <param name="vertex">the actual vertex found or null</param>
	/// <returns>true iff any selected vertex is within
	/// <paramref name="tolerance"/> of <paramref name="hitPoint"/></returns>
	bool HitTestVertex(MapPoint hitPoint, double tolerance, out MapPoint vertex);

	/// <remarks>Callers duty to make sure the given <paramref name="shape"/>
	/// is compatible with this selection (part/vertex count)</remarks>
	void SetShape(Geometry shape);

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

	public void SetShape(Geometry shape)
	{
		Shape = shape ?? throw new ArgumentNullException(nameof(shape));
	}

	public bool IsEmpty => _blocks.IsEmpty;

	public bool IsFully => IsShapeSelected() == ShapeSelectionState.Entirely;

	public bool IsVertexSelected(int partIndex, int vertexIndex)
	{
		if (Shape is Multipoint)
		{
			// outside world: a multipoint's points are its parts
			// inside representation: all points in part 0 (for efficiency)
			vertexIndex = partIndex;
			partIndex = 0;
		}

		return _blocks.ContainsVertex(partIndex, vertexIndex);
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
			return _blocks.ContainsVertex(0, partIndex)
				       ? ShapeSelectionState.Entirely
				       : ShapeSelectionState.Not;
		}

		int partVertexCount = GetVertexCount(Shape, partIndex);
		int selectedVertexCount = _blocks.CountVertices(partIndex);

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
		int selectedVertexCount = _blocks.CountVertices();

		var result = ShapeSelectionState.Partially;
		if (selectedVertexCount >= totalVertexCount)
			result = ShapeSelectionState.Entirely;
		else if (selectedVertexCount <= 0)
			result = ShapeSelectionState.Not;

		return result;
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
				changed = _blocks.SetEmpty();
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

	private static bool AddShape(Geometry shape, BlockList blocks)
	{
		if (shape is null)
			throw new ArgumentNullException(nameof(shape));
		if (blocks is null)
			throw new ArgumentNullException(nameof(blocks));

		if (shape.IsEmpty) return false;

		var selected = blocks.CountVertices();
		var total = GeometryUtils.GetPointCount(shape);
		var changed = selected != total; // not already entirely selected

		if (shape is MapPoint)
		{
			blocks.SetEmpty();
			blocks.Add(0, 0);
		}
		else if (shape is Multipoint multipoint)
		{
			blocks.SetEmpty();
			blocks.Add(0, 0, multipoint.PointCount);
		}
		else if (shape is Multipart multipart)
		{
			blocks.SetEmpty();
			int partCount = multipart.PartCount;
			for (int k = 0; k < partCount; k++)
			{
				var part = multipart.Parts[k];
				var segmentCount = part.Count;
				var vertexCount = multipart is Polygon ? segmentCount : segmentCount + 1;
				blocks.Add(k, 0, vertexCount);
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

		blocks.SetEmpty();

		foreach (var block in inverted)
		{
			blocks.Add(block.Part, block.First, block.Count);
		}

		return true;
	}

	public bool CombinePart(int partIndex, SetCombineMethod method)
	{
		throw new NotImplementedException();
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
			changed = SetEmpty();
			method = SetCombineMethod.Add;
		}

		if (method == SetCombineMethod.Add)
		{
			var added = _blocks.Add(partIndex, vertexIndex);
			return changed || added;
		}

		if (method == SetCombineMethod.Remove)
		{
			var removed = _blocks.Remove(partIndex, vertexIndex);
			return changed || removed;
		}

		if (method == SetCombineMethod.And)
		{
			// remove all but the given vertex:
			var contained = _blocks.ContainsVertex(partIndex, vertexIndex);
			if (contained && _blocks.CountVertices() == 1) return false;
			SetEmpty();
			if (contained) _blocks.Add(partIndex, vertexIndex);
			return true;
		}

		if (method == SetCombineMethod.Xor)
		{
			var contained = _blocks.ContainsVertex(partIndex, vertexIndex);
			if (contained)
				_blocks.Remove(partIndex, vertexIndex);
			else _blocks.Add(partIndex, vertexIndex);
			return true;
		}

		throw new ArgumentOutOfRangeException(nameof(method), method, null);
	}

	public bool SetEmpty()
	{
		return _blocks.SetEmpty();
	}

	public bool HitTestVertex(MapPoint hitPoint, double tolerance, out MapPoint vertex)
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

	public IEnumerable<MapPoint> GetSelectedVertices()
	{
		return GetVertices(Shape, _blocks);
	}

	public IEnumerable<MapPoint> GetUnselectedVertices()
	{
		if (Shape.IsEmpty) return Enumerable.Empty<MapPoint>();

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
		if (shape is null)
			throw new ArgumentNullException(nameof(shape));

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

		int numParts = GetPartCount(shape);

		var block = _blocks.MaxBy(b => b.Part);
		if (block.Part >= numParts)
		{
			message =
				$"Selection refers to part {block.Part} but shape's max part index is {numParts - 1}";
			return false;
		}

		var groups = _blocks.GroupBy(b => b.Part);
		foreach (var group in groups)
		{
			int numVertices = GetVertexCount(shape, group.Key);
			int maxIndex = group.Max(b => b.First + b.Count - 1);
			if (! (maxIndex < numVertices))
			{
				message =
					$"Selection refers to vertex {maxIndex} but max vertex index is {numVertices - 1} (in part {group.Key})";
				return false;
			}
		}

		message = string.Empty;
		return true;
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
			// by convention, the points of a multipoint ar its parts:
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

	#endregion
}

/// <summary>
/// Linked list of blocks of contiguous sequences of vertices,
/// always ordered by (part,vertex). New part always entails a
/// new block. Blocks do not overlap (such blocks are merged).
/// </summary>
public class BlockList : IEnumerable<BlockList.Block>
{
	// _head.Next points to first real node
	// _free points directly to recycled nodes
	private readonly Node _head = new(-1, 0, 0);
	private Node _free;

	public bool IsEmpty => _head.Next is null;

	public bool Add(int part, int vertexIndex)
	{
		return Add(part, vertexIndex, 1);
	}

	public bool Add(int part, int first, int count)
	{
		// if first is in existing block: grow this block and merge down the list
		// else append block to before and merge down the list

		var node = Find(part, first, out var before);

		if (node is not null)
		{
			if (Contains(node, part, first, count))
			{
				return false; // already fully contained
			}

			// args extend beyond node: grow node to contain args
			var beyond = Math.Max(node.First + node.Count, first + count);
			node.Count = beyond - first;

			MergeBlocks(node);
		}
		else
		{
			node = NewNode(part, first, count);
			node.Next = before.Next;
			before.Next = node;

			MergeBlocks(before);
		}

		return true;
	}

	public bool Remove(int part, int vertex)
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

	// TODO Toggle(partIndex, vertexIndex): efficient implementation of this frequent XOR operation (for now: if (Contains) Remove else Add)

	public bool SetEmpty()
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

	public bool ContainsVertex(int part, int vertex)
	{
		var node = Find(part, vertex, out Node _);
		return node is not null;
	}

	public int CountVertices(int part = -1)
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
		private bool IsHead => Part < 0;

		public override string ToString()
		{
			return IsHead ? "Head" : $"Part {Part}, First {First}, Count {Count}";
		}
	}

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
