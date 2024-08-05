using System.Linq;
using System.Threading;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Carto;
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
		var selection = new ShapeSelection();
		Assert.True(selection.IsEmpty);
		Assert.False(selection.IsFully);
		Assert.False(selection.IsVertexSelected(0, 0));
		Assert.AreEqual(ShapeSelectionState.Not, selection.GetSelectionState());
		Assert.AreEqual(ShapeSelectionState.Not, selection.GetPartSelectionState(0));
	}

	[Test]
	public void CanCombineVertices()
	{
		var selection = new ShapeSelection();

		Assert.True(selection.CombineVertex(0, 2, SetCombineMethod.New)); // (0,2)
		Assert.True(selection.IsVertexSelected(0, 2));

		Assert.True(selection.CombineVertex(0, 3, SetCombineMethod.Add)); // (0,2) (0,3)
		Assert.True(selection.IsVertexSelected(0, 2));
		Assert.True(selection.IsVertexSelected(0, 3));

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
	}

	[Test]
	public void CanGetSelectedVertices()
	{
		var selection = new ShapeSelection();

		// Point
		var point = CreatePointXY(0, 0);
		selection.CombineVertex(0, 0, SetCombineMethod.New);
		Assert.AreEqual(point, selection.GetSelectedVertices(point).Single());
		selection.SetEmpty();
		Assert.False(selection.GetSelectedVertices(point).Any());

		// Multipoint: constituent points are parts (not vertices)
		var multipoint = CreateMultipointXY(1,1, 2,2, 3,1);
		selection.CombineVertex(2, 0, SetCombineMethod.New); // 3,1
		selection.CombineVertex(0, 0, SetCombineMethod.Add); // 1,1
		Assert.AreEqual(2, selection.GetSelectedVertices(multipoint).Count());
		selection.CombineShape(SetCombineMethod.Add); // add entire shape
		Assert.AreEqual(3, selection.GetSelectedVertices(multipoint).Count());
		selection.CombineVertex(1, 0, SetCombineMethod.New);
		point = selection.GetSelectedVertices(multipoint).Single();
		Assert.AreEqual(2, point.X);
		Assert.AreEqual(2, point.Y);

		// Polyline
		var polyline = CreatePolylineXY(0,0, 2,1);
		selection.CombineVertex(0, 0, SetCombineMethod.New);
		point = selection.GetSelectedVertices(polyline).Single();
		Assert.AreEqual(0, point.X);
		Assert.AreEqual(0, point.Y);
		selection.CombineVertex(0, 1, SetCombineMethod.Xor);
		selection.CombineVertex(0, 0, SetCombineMethod.Xor);
		point = selection.GetSelectedVertices(polyline).Single();
		Assert.AreEqual(2, point.X);
		Assert.AreEqual(1, point.Y);

		// Polygon
		var polygon = CreatePolygonXY(0,0, 0,5, 5,5, 5,0);
		selection.CombineVertex(0, 3, SetCombineMethod.New);
		point = selection.GetSelectedVertices(polygon).Single();
		Assert.AreEqual(5, point.X);
		Assert.AreEqual(0, point.Y);
		selection.CombineVertex(0, 1, SetCombineMethod.Xor); // add 0,1
		selection.CombineVertex(0, 3, SetCombineMethod.Xor); // remove 0,3
		point = selection.GetSelectedVertices(polygon).Single();
		Assert.AreEqual(0, point.X);
		Assert.AreEqual(5, point.Y);
		selection.CombineVertex(0, 0, SetCombineMethod.Add);
		Assert.AreEqual(2, selection.GetSelectedVertices(polygon).Count());
		selection.SetEmpty();
		Assert.False(selection.GetSelectedVertices(polygon).Any());
	}

	[Test]
	public void CanHitTestVertex()
	{
		var selection = new ShapeSelection();

		var hitPoint = CreatePointXY(0.1, 0.1);
		var tolerance = 0.15; // a little more than pyth(hitPoint)

		var point = CreatePointXY(0, 0);
		selection.CombineVertex(0, 0, SetCombineMethod.New);
		Assert.True(selection.HitTestVertex(point, hitPoint, tolerance));
		Assert.False(selection.HitTestVertex(point, hitPoint, tolerance / 2));

		// other geometry types... but since HitTestVertex is implemented
		// in terms of GetSelectedVertices this should be enough for now
	}

	private static MapPoint CreatePointXY(double x, double y)
	{
		return MapPointBuilderEx.CreateMapPoint(new Coordinate2D(x, y));
	}

	private static Multipoint CreateMultipointXY(params double[] xys)
	{
		var builder = new MultipointBuilderEx();
		builder.HasZ = builder.HasM = builder.HasID = false;
		for (int i = 0; i < xys.Length - 1; i += 2)
		{
			builder.AddPoint(new Coordinate2D(xys[i], xys[i + 1]));
		}
		return builder.ToGeometry();
	}

	private static Polyline CreatePolylineXY(params double[] xys)
	{
		var builder = new PolylineBuilderEx();
		builder.HasZ = builder.HasM = builder.HasID = false;
		if (xys.Length < 2) return builder.ToGeometry(); // empty
		var p0 = new Coordinate2D(xys[0], xys[1]);
		for (int i = 2; i < xys.Length - 1; i += 2)
		{
			var p1 = new Coordinate2D(xys[i], xys[i + 1]);
			var segment = LineBuilderEx.CreateLineSegment(p0, p1);
			builder.AddSegment(segment);
			p0 = p1;
		}
		return builder.ToGeometry();
	}

	private static Polygon CreatePolygonXY(params double[] xys)
	{
		var builder = new PolygonBuilderEx();
		builder.HasZ = builder.HasM = builder.HasID = false;
		if (xys.Length < 2) return builder.ToGeometry(); // empty
		var p0 = new Coordinate2D(xys[0], xys[1]);
		for (int i = 2; i < xys.Length - 1; i += 2)
		{
			var p1 = new Coordinate2D(xys[i], xys[i + 1]);
			var segment = LineBuilderEx.CreateLineSegment(p0, p1);
			builder.AddSegment(segment);
			p0 = p1;
		}
		return builder.ToGeometry();
	}
}
