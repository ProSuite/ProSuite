using System;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.Commons.AGP.Core.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class ShapeSelectionTest
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();
	}

	[Test]
	public void CanCreateShapeSelection()
	{
		var polygon = CreatePolygonXY(0, 0, 0, 5, 5, 5, 5, 0);
		var selection = new ShapeSelection(polygon);
		Assert.True(selection.IsEmpty);
		Assert.False(selection.IsFully);
		Assert.False(selection.IsVertexSelected(0, 0));
		Assert.AreEqual(ShapeSelectionState.Not, selection.IsShapeSelected());
		Assert.AreEqual(ShapeSelectionState.Not, selection.IsPartSelected(0));
	}

	[Test]
	public void CanCombineVertices()
	{
		var shape = CreatePolylineXY(0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6);
		IShapeSelection selection = new ShapeSelection(shape);

		Assert.True(selection.CombineVertex(0, 2, SetCombineMethod.New)); // (0,2)
		Assert.True(selection.IsVertexSelected(0, 2));

		Assert.True(selection.CombineVertex(0, 3, SetCombineMethod.Add)); // (0,2) (0,3)
		Assert.True(selection.IsVertexSelected(0, 2));
		Assert.True(selection.IsVertexSelected(0, 3));
		Assert.AreEqual(ShapeSelectionState.Partially, selection.IsShapeSelected());

		Assert.True(selection.CombineVertex(0, 4, SetCombineMethod.New)); // (0,4)
		Assert.False(selection.IsVertexSelected(0, 2));
		Assert.False(selection.IsVertexSelected(0, 3));
		Assert.True(selection.IsVertexSelected(0, 4));

		Assert.True(selection.CombineVertex(0, 5, SetCombineMethod.Xor)); // (0,4) (0,5)
		Assert.True(selection.IsVertexSelected(0, 4));
		Assert.True(selection.IsVertexSelected(0, 5));

		Assert.True(selection.CombineVertex(0, 4, SetCombineMethod.Xor)); // (0,5)
		Assert.False(selection.IsVertexSelected(0, 4));
		Assert.True(selection.IsVertexSelected(0, 5));

		Assert.False(selection.CombineVertex(0, 5, SetCombineMethod.Add)); // no-op
		Assert.True(selection.CombineVertex(0, 6, SetCombineMethod.Add)); // (0,5) (0,6)

		Assert.False(selection.CombineVertex(0, 7, SetCombineMethod.Remove)); // no-op
		Assert.True(selection.CombineVertex(0, 6, SetCombineMethod.Remove)); // (0,5)

		Assert.False(selection.CombineVertex(0, 5, SetCombineMethod.And)); // (0,5)
		Assert.True(selection.CombineVertex(0, 6, SetCombineMethod.And)); // empty

		Assert.True(selection.IsEmpty);
		Assert.False(selection.IsFully);
		Assert.AreEqual(ShapeSelectionState.Not, selection.IsShapeSelected());
	}

	[Test]
	public void CanGetSelectedVertices()
	{
		// Point
		var point = CreatePointXY(0, 0);
		var selection1 = new ShapeSelection(point);
		selection1.CombineVertex(0, 0, SetCombineMethod.New);
		Assert.AreEqual(point, selection1.GetSelectedVertices().Single());
		selection1.Clear();
		Assert.False(selection1.GetSelectedVertices().Any());

		// Multipoint: constituent points are parts (not vertices)
		var multipoint = CreateMultipointXY(1,1, 2,2, 3,1);
		var selection2 = new ShapeSelection(multipoint);
		selection2.CombineVertex(2, 0, SetCombineMethod.New); // 3,1
		selection2.CombineVertex(0, 0, SetCombineMethod.Add); // 1,1
		Assert.AreEqual(2, selection2.GetSelectedVertices().Count());
		selection2.CombineShape(SetCombineMethod.Add); // add entire shape
		Assert.AreEqual(3, selection2.GetSelectedVertices().Count());
		selection2.CombineVertex(1, 0, SetCombineMethod.New);
		point = selection2.GetSelectedVertices().Single();
		Assert.AreEqual(2, point.X);
		Assert.AreEqual(2, point.Y);

		// Polyline
		var polyline = CreatePolylineXY(0,0, 2,1);
		var selection3 = new ShapeSelection(polyline);
		selection3.CombineVertex(0, 0, SetCombineMethod.New);
		point = selection3.GetSelectedVertices().Single();
		Assert.AreEqual(0, point.X);
		Assert.AreEqual(0, point.Y);
		selection3.CombineVertex(0, 1, SetCombineMethod.Xor);
		selection3.CombineVertex(0, 0, SetCombineMethod.Xor);
		point = selection3.GetSelectedVertices().Single();
		Assert.AreEqual(2, point.X);
		Assert.AreEqual(1, point.Y);

		// Polygon
		var polygon = CreatePolygonXY(0,0, 0,5, 5,5, 5,0);
		var selection4 = new ShapeSelection(polygon);
		selection4.CombineVertex(0, 3, SetCombineMethod.New);
		point = selection4.GetSelectedVertices().Single();
		Assert.AreEqual(5, point.X);
		Assert.AreEqual(0, point.Y);
		selection4.CombineVertex(0, 1, SetCombineMethod.Xor); // add 0,1
		selection4.CombineVertex(0, 3, SetCombineMethod.Xor); // remove 0,3
		point = selection4.GetSelectedVertices().Single();
		Assert.AreEqual(0, point.X);
		Assert.AreEqual(5, point.Y);
		selection4.CombineVertex(0, 0, SetCombineMethod.Add);
		Assert.AreEqual(2, selection4.GetSelectedVertices().Count());
		selection4.Clear();
		Assert.False(selection4.GetSelectedVertices().Any());
	}

	[Test]
	public void CanHitTestVertex()
	{
		var point = CreatePointXY(0, 0);
		var selection = new ShapeSelection(point);

		var hitPoint = CreatePointXY(0.1, 0.1);
		var tolerance = 0.15; // a little more than pyth(hitPoint)

		selection.CombineVertex(0, 0, SetCombineMethod.New);
		Assert.True(selection.SelectedVertex(hitPoint, tolerance, out var vertex));
		Assert.NotNull(vertex);
		Assert.False(selection.SelectedVertex(hitPoint, tolerance / 2, out vertex));
		Assert.Null(vertex);

		// other geometry types... but since HitTestVertex is implemented
		// in terms of GetSelectedVertices this should be enough for now
	}

	#region BlockList tests

	[Test]
	public void CanBlockList()
	{
		var blocks = new BlockList();

		Assert.False(blocks.IsSelected(0, 0));
		Assert.True(blocks.IsEmpty);
		Assert.IsEmpty(blocks);

		Assert.False(blocks.Unselect(0, 0));

		Assert.True(blocks.Select(0, 2)); // new node
		Assert.True(blocks.Select(0, 3)); // append
		Assert.True(blocks.Select(0, 1)); // prepend
		Assert.True(blocks.Select(0, 5)); // new node
		Assert.True(blocks.Select(0, 4)); // merge

		Assert.False(blocks.Select(0, 5)); // already in list
		Assert.False(blocks.IsEmpty);

		Assert.True(blocks.IsSelected(0, 1));
		Assert.True(blocks.IsSelected(0, 5));

		var list = blocks.ToList();
		Assert.AreEqual(1, list.Count);
		Assert.AreEqual(1, list.Single().First);
		Assert.AreEqual(5, list.Single().Count);

		Assert.True(blocks.Unselect(0, 2)); // split
		Assert.True(blocks.IsSelected(0, 1));
		Assert.False(blocks.IsSelected(0, 2));
		Assert.True(blocks.IsSelected(0, 3));

		list = blocks.ToList();
		Assert.AreEqual(2, list.Count);
		var block0 = list[0];
		Assert.AreEqual(1, block0.First);
		Assert.AreEqual(1, block0.Count);
		var block1 = list[1];
		Assert.AreEqual(3, block1.First);
		Assert.AreEqual(3, block1.Count);

		Assert.True(blocks.Unselect(0, 1)); // drop node
		Assert.False(blocks.IsSelected(0, 1));
		Assert.False(blocks.Unselect(0, 1)); // idempotent

		Assert.True(blocks.Unselect(0, 5)); // cut end
		Assert.True(blocks.Unselect(0, 3)); // cut begin
		Assert.True(blocks.IsSelected(0, 4));
		Assert.True(blocks.Unselect(0, 4)); // drop node
		Assert.False(blocks.IsSelected(0, 4));
		Assert.True(blocks.IsEmpty);

		Assert.True(blocks.Select(0, 1));
		Assert.True(blocks.Select(1, 2));
		// can't merge blocks because part differs:
		Assert.AreEqual(2, blocks.Count());

		Assert.True(blocks.Clear());
		Assert.False(blocks.Clear());
	}

	[Test]
	public void CanBlockListSelect()
	{
		var blocks = new BlockList();

		Assert.True(blocks.Select(0, 0, 2)); // vertices 0,1 in part 0
		Assert.False(blocks.Select(0, 1, 1)); // vertex 1 in part 0
		Assert.True(blocks.Select(0, 3, 2)); // vertices 3,4 in part 0
		Assert.True(blocks.Select(1, 4, 3)); // vertices 4,5,6 in part 1
		Assert.False(blocks.Select(1, 5, 2)); // vertices 5,6 in part 1
		Assert.True(blocks.Select(0, 2, 5)); // vertices 2,3,4,5,6 in part 0 (merges)
		var list = blocks.ToList();
		Assert.AreEqual(2, list.Count);
		Assert.AreEqual(0, list[0].Part);
		Assert.AreEqual(0, list[0].First);
		Assert.AreEqual(7, list[0].Count);
		Assert.AreEqual(1, list[1].Part);
		Assert.AreEqual(4, list[1].First);
		Assert.AreEqual(3, list[1].Count);
	}

	[Test]
	public void CanBlockListVertexShifting()
	{
		var blocks = new BlockList();

		blocks.Select(0, 0, 2);
		blocks.Select(0, 5, 1);
		blocks.Select(1, 2, 2);

		var list = blocks.ToList();

		Assert.AreEqual(3, list.Count);
		AssertBlock(list[0], 0, 0, 2);
		AssertBlock(list[1], 0, 5, 1);
		AssertBlock(list[2], 1, 2, 2);

		blocks.VertexAdded(0, 0); // enlarge (0,0,2)=>(0,0,3) and shift (0,5,1)=>(0,6,1)
		blocks.VertexAdded(1, 1); // shift (1,2,2)=>(1,3,2)
		blocks.VertexRemoved(0, 4); // shift (0,6,1)=>(0,5,1)
		blocks.VertexRemoved(0, 5); // drop the (0,5,1) block
		blocks.VertexRemoved(1, 3); // shorten (1,3,2)=>(1,3,1)

		list = blocks.ToList();

		Assert.AreEqual(2, list.Count);
		AssertBlock(list[0], 0, 0, 3);
		AssertBlock(list[1], 1, 3, 1);
	}

	[Test]
	public void CanBlockListPartReversed()
	{
		var blocks = new BlockList();
		blocks.Select(0, 0); // vertex 0
		blocks.Select(0, 2); // vertex 2
		blocks.Select(0, 4, 2); // vertices 4,5
		// 0  1  2  3  4  5
		// *--o--*--o--*--*
		// *--*--o--*--o--*
		blocks.PartReversed(0, 6);

		var list = blocks.ToList();
		Assert.AreEqual(3, list.Count);
		AssertBlock(list[0], 0, 0, 2);
		AssertBlock(list[1], 0, 3, 1);
		AssertBlock(list[2], 0, 5, 1);

		blocks.PartReversed(0, 6); // revert
		blocks.PartReversed(0, 8);
		// 0  1  2  3  4  5  6  7
		// *--o--*--o--*--*--o--o
		// o--o--*--*--o--*--o--*

		list = blocks.ToList();
		Assert.AreEqual(3, list.Count);
		AssertBlock(list[0], 0, 2, 2);
		AssertBlock(list[1], 0, 5, 1);
		AssertBlock(list[2], 0, 7, 1);

		// Expect error if numVerticesInPart is too small:
		Assert.Throws<ArgumentOutOfRangeException>(() => blocks.PartReversed(0, 5));
	}

	private static void AssertBlock(BlockList.Block block, int part, int first, int count)
	{
		if (block.Part != part || block.First != first || block.Count != count)
		{
			Assert.Fail($"Expect Block({part},{first},{count}) but " +
			            $"got Block({block.Part},{block.First},{block.Count})");
		}
	}

	[Test]
	public void CanInvertVertexBlocks()
	{
		// Point
		var point = CreatePointXY(0, 0);
		var blocks1 = new BlockList();
		var inverted1 = ShapeSelection.Invert(blocks1, point).ToList();
		Assert.AreEqual(1, inverted1.Count);
		Assert.AreEqual(0, inverted1[0].Part);
		Assert.AreEqual(0, inverted1[0].First);
		Assert.AreEqual(1, inverted1[0].Count);

		blocks1.Select(0, 0); // add the single vertex
		inverted1 = ShapeSelection.Invert(blocks1, point).ToList();
		Assert.IsEmpty(inverted1);

		// Multipoint
		var multipoint = CreateMultipointXY(0, 0, 1, 1, 2, 2); // vertices 0,1,2
		var blocks2 = new BlockList();
		blocks2.Select(0, 1); // select the middle vertex
		var inverted2 = ShapeSelection.Invert(blocks2, multipoint).ToList();
		Assert.AreEqual(2, inverted2.Count);
		blocks2.Clear();
		blocks2.Select(0, 0);
		blocks2.Select(0, 2);
		inverted2 = ShapeSelection.Invert(blocks2, multipoint).ToList();
		Assert.AreEqual(1, inverted2.Count);
		Assert.AreEqual(1, inverted2[0].First);
		Assert.AreEqual(1, inverted2[0].Count);

		// Polyline (multipart)
		var polyline = CreatePolylineXY(0, 0, 1, 1, 2, 2, double.NaN, 3, 3, 4, 4); // part 0: 0,1,2, part1: 0,1
		var blocks3 = new BlockList();
		blocks3.Select(0, 1); // select middle vertex of 1st part
		var inverted3 = ShapeSelection.Invert(blocks3, polyline).ToList();
		Assert.AreEqual(3, inverted3.Count);
		Assert.AreEqual(0, inverted3[0].Part);
		Assert.AreEqual(0, inverted3[1].Part);
		Assert.AreEqual(1, inverted3[2].Part);
		Assert.AreEqual(0, inverted3[2].First);
		Assert.AreEqual(2, inverted3[2].Count);
		blocks3.Clear();
		blocks3.Select(0, 0);
		blocks3.Select(0, 2);
		blocks3.Select(1, 0, 2); // entire 2nd part
		inverted3 = ShapeSelection.Invert(blocks3, polyline).ToList();
		Assert.AreEqual(1, inverted3.Count);
		Assert.AreEqual(1, inverted3[0].First);
		Assert.AreEqual(1, inverted3[0].Count);

		// Polygon (multipart)
		var polygon = CreatePolygonXY(0, 0, 1, 3, 2, 0, double.NaN, 3, 0, 4, 6, 5, 0);
		var blocks4 = new BlockList();
		blocks4.Select(0, 1); // select middle vertex of 1st part
		var inverted4 = ShapeSelection.Invert(blocks4, polygon).ToList();
		Assert.AreEqual(3, inverted4.Count);
		Assert.AreEqual(0, inverted4[0].Part);
		Assert.AreEqual(0, inverted4[1].Part);
		Assert.AreEqual(1, inverted4[2].Part);
		Assert.AreEqual(0, inverted4[2].First);
		Assert.AreEqual(3, inverted4[2].Count);
		blocks4.Clear();
		blocks4.Select(0, 0);
		blocks4.Select(0, 2);
		blocks4.Select(1, 0, 3); // entire 2nd part
		inverted4 = ShapeSelection.Invert(blocks4, polygon).ToList();
		Assert.AreEqual(1, inverted4.Count);
		Assert.AreEqual(1, inverted4[0].First);
		Assert.AreEqual(1, inverted4[0].Count);
	}

	#endregion

	#region Private test utils

	private static MapPoint CreatePointXY(double x, double y)
	{
		return MapPointBuilderEx.CreateMapPoint(new Coordinate2D(x, y));
	}

	private static Multipoint CreateMultipointXY(params double[] xys)
	{
		return GeometryFactory.CreateMultipointXY(xys);
	}

	private static Polyline CreatePolylineXY(params double[] xys)
	{
		return GeometryFactory.CreatePolylineXY(xys);
	}

	private static Polygon CreatePolygonXY(params double[] xys)
	{
		return GeometryFactory.CreatePolygonXY(xys);
	}

	#endregion
}
