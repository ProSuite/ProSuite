using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.Commons.AGP.Core.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class GeometricEffectsTest
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();
	}

	[Test]
	public void CanAddControlPoints()
	{
		// null geometry
		Assert.IsNull(GeometricEffects.AddControlPoints(null, 120));

		// non-polycurve geometry
		var pt = AssertType<MapPoint>(GeometricEffects.AddControlPoints(Pt(100, 200), 120));
		Assert.AreEqual(100, pt.X);
		Assert.AreEqual(200, pt.Y);
		Assert.AreEqual(0, pt.ID);

		// right(1)up(2)downright, angles at (1) 90°, (2) 45°
		var line = PolylineBuilderEx.CreatePolyline(
			new[] { Pt(0, 0), Pt(10, 0), Pt(10, 10), Pt(20, 0) });

		var line1 = AssertType<Polyline>(GeometricEffects.AddControlPoints(line, 30));
		Assert.AreEqual(4, line1.PointCount);
		Assert.AreEqual(0, line1.Points[0].ID);
		Assert.AreEqual(0, line1.Points[1].ID);
		Assert.AreEqual(0, line1.Points[2].ID);
		Assert.AreEqual(0, line1.Points[3].ID);

		var line2 = AssertType<Polyline>(GeometricEffects.AddControlPoints(line, 45));
		Assert.IsTrue(line2.HasID);
		Assert.AreEqual(4, line2.PointCount);
		Assert.AreEqual(0, line2.Points[0].ID);
		Assert.AreEqual(0, line2.Points[1].ID);
		Assert.AreEqual(1, line2.Points[2].ID);
		Assert.AreEqual(0, line2.Points[3].ID);

		var line3 = AssertType<Polyline>(GeometricEffects.AddControlPoints(line, 89.99));
		Assert.IsTrue(line3.HasID);
		Assert.AreEqual(4, line3.PointCount);
		Assert.AreEqual(0, line3.Points[0].ID);
		Assert.AreEqual(0, line3.Points[1].ID);
		Assert.AreEqual(1, line3.Points[2].ID);
		Assert.AreEqual(0, line3.Points[3].ID);

		var line4 = AssertType<Polyline>(GeometricEffects.AddControlPoints(line, 90));
		Assert.IsTrue(line4.HasID);
		Assert.AreEqual(4, line4.PointCount);
		Assert.AreEqual(0, line4.Points[0].ID);
		Assert.AreEqual(1, line4.Points[1].ID);
		Assert.AreEqual(1, line4.Points[2].ID);
		Assert.AreEqual(0, line4.Points[3].ID);

		var line5 = AssertType<Polyline>(GeometricEffects.AddControlPoints(line, 120, 2));
		Assert.IsTrue(line5.HasID);
		Assert.AreEqual(4, line5.PointCount);
		Assert.AreEqual(0, line5.Points[0].ID);
		Assert.AreEqual(2, line5.Points[1].ID);
		Assert.AreEqual(2, line5.Points[2].ID);
		Assert.AreEqual(0, line5.Points[3].ID);
	}

	[Test]
	public void CanCut()
	{
		// null geometry
		Assert.IsNull(GeometricEffects.Cut(null, 3.0, 4.0));

		// non-polyline geometry remains unchanged
		var poly = PolygonBuilderEx.CreatePolygon(Env(10, 10, 20, 20));
		var poly2 = AssertType<Polygon>(GeometricEffects.Cut(poly, 3.0, 4.0));
		Assert.AreEqual(poly.Length, poly2.Length, 0.00001);

		// two-part polyline (expect cut on each part)
		var builder = new PolylineBuilderEx();
		builder.AddPart(new[] { Pt(0, 0), Pt(10, 0), Pt(20, 0) });
		builder.AddPart(new[] { Pt(10, 0), Pt(10, 10) });
		var polyline = builder.ToGeometry();

		var cut1 = AssertType<Polyline>(GeometricEffects.Cut(polyline, 2.0, 3.0));
		Assert.AreEqual(2, cut1.PartCount);
		Assert.AreEqual(15.0, cut1.Parts[0].Sum(s => s.Length), 0.00001);
		Assert.AreEqual(5.0, cut1.Parts[1].Sum(s => s.Length));

		// inverted cut (keep cuttings, drop inner part)
		var cut2 = AssertType<Polyline>(GeometricEffects.Cut(polyline, 2.0, 3.0, true));
		Assert.AreEqual(4, cut2.PartCount);
		Assert.AreEqual(10.0, cut2.Length, 0.00001);

		// cuts longer than part
		var cut3 = AssertType<Polyline>(GeometricEffects.Cut(polyline, 5.5, 5.5));
		Assert.AreEqual(1, cut3.PartCount);
		Assert.AreEqual(9.0, cut3.Parts.Single().Sum(s => s.Length));

		// inverted cut of more than part length
		var cut4 = AssertType<Polyline>(GeometricEffects.Cut(polyline, 6.0, 5.0, true));
		Assert.AreEqual(3, cut4.PartCount);
		var lens4 = cut4.Parts.Select(p => p.Sum(s => s.Length)).OrderBy(l => l).ToArray();
		Assert.AreEqual(5.0, lens4[0], 0.00001);
		Assert.AreEqual(6.0, lens4[1], 0.00001);
		Assert.AreEqual(10.0, lens4[2], 0.00001);

		// NB. middleCut untested here
	}

	[Test]
	public void CanDashesUnconstrained()
	{
		// line of length 80 (like the line symbol preview in Pro's Symbology pane)
		var line = PolylineBuilderEx.CreatePolyline(new[] { Pt(0, 0), Pt(80, 0) });

		var pat1 = new[] { 20.0, 10.0 }; // P < L (pattern shorter than line)
		var dashes1 = Dashes(line, pat1);
		AssertDashes(dashes1, 20, 20, 20);
		AssertDashes(Dashes(line, pat1, 10.0), 10, 20, 20);
		AssertDashes(Dashes(line, pat1, -12.0), 2.0, 20.0, 20.0, 8.0);
		AssertDashes(Dashes(line, pat1, 33.0), 17.0, 20.0, 20.0);
		AssertDashes(Dashes(line, pat1, -65.0), 20.0, 20.0, 15.0);

		var pat2 = new[] { 30.0, 30.0 }; // P < L
		var dashes2 = Dashes(line, pat2);
		AssertDashes(dashes2, 30.0, 20.0);
		AssertDashes(Dashes(line, pat2, 10.0), 20.0, 30.0);
		AssertDashes(Dashes(line, pat2, -12.0), 30.0, 8.0);
		AssertDashes(Dashes(line, pat2, 123.0), 27.0, 23.0);
		AssertDashes(Dashes(line, pat2, -65.0), 30.0, 15.0);

		var pat3 = new[] { 30.0 }; // expect same effect as pat2
		var dashes3 = Dashes(line, pat3);
		AssertDashes(dashes3, 30.0, 20.0);
		AssertDashes(Dashes(line, pat3, 10.0), 20.0, 30.0);
		AssertDashes(Dashes(line, pat3, -12.0), 30.0, 8.0);
		AssertDashes(Dashes(line, pat3, 123.0), 27.0, 23.0);
		AssertDashes(Dashes(line, pat3, -65.0), 30.0, 15.0);

		var pat4 = new[] { 30.0, 30.0, 30.0 }; // expect same effect as pat2 (NB. not so if constrained!)
		var dashes4 = Dashes(line, pat4);
		AssertDashes(dashes4, 30.0, 20.0);
		AssertDashes(Dashes(line, pat4, 10.0), 20.0, 30.0);
		AssertDashes(Dashes(line, pat4, -12.0), 30.0, 8.0);
		AssertDashes(Dashes(line, pat4, 123.0), 27.0, 23.0);
		AssertDashes(Dashes(line, pat4, -65.0), 30.0, 15.0);

		// And just a few more on a line of length 40 with a pattern of length 25:
		var line40 = PolylineBuilderEx.CreatePolyline(new[] { Pt(0, 0), Pt(40, 0) });
		var pat_15_10 = new[] { 15.0, 10.0 };
		AssertDashes(Dashes(line40, pat_15_10), 15.0, 15.0);
		// Shift pattern left (positive offsetAlongLine)
		AssertDashes(Dashes(line40, pat_15_10, 10.0), 5.0, 15.0);
		AssertDashes(Dashes(line40, pat_15_10, 37.0), 3.0, 15.0, 2.0);
		// Shift pattern right (negative offsetAlongLine)
		AssertDashes(Dashes(line40, pat_15_10, -5.0), 15.0, 10.0);
		AssertDashes(Dashes(line40, pat_15_10, -27.0), 15.0, 13.0);

		// And even more on a line of length 100 with a pattern that fits 5 times:
		var line100 = PolylineBuilderEx.CreatePolyline(new[] { Pt(0, 0), Pt(100, 0) });
		var pat_14_6 = new[] { 14.0, 6.0 }; // fits exactly 5 times
		AssertDashes(Dashes(line100, pat_14_6), 14, 14, 14, 14, 14);
		// same pattern but with positive offset (pat shifted left)
		AssertDashes(Dashes(line100, pat_14_6, 8.0), 6, 14, 14, 14, 14, 8);
		// same pattern but with negative offset (pat shifted right)
		AssertDashes(Dashes(line100, pat_14_6, -8.0), 2, 14, 14, 14, 14, 12);
		// same pattern, positive offset greater than pattern length
		AssertDashes(Dashes(line100, pat_14_6, 25.0), 9, 14, 14, 14, 14, 5);
		// same pattern, negative offset greater than pattern length
		AssertDashes(Dashes(line100, pat_14_6, -25.0), 14, 14, 14, 14, 14);
	}

	[Test]
	public void CanDashesCustomEndOffset()
	{
		// line of length 80 (like the line symbol preview in Pro's Symbology pane)
		var line = PolylineBuilderEx.CreatePolyline(new[] { Pt(0, 0), Pt(80, 0) });

		// pattern length equals line length (P=L)
		var pat = new[] { 40.0, 40.0 };

		var dashes1 = Dashes(line, pat, 0.0, 0.0);
		AssertDashes(dashes1, 40.0);

		var dashes2 = Dashes(line, pat, 0.0, customEndOffset: 20.0);
		AssertDashes(dashes2, 32.0, 16.0);

		var dashes3 = Dashes(line, pat, 0.0, customEndOffset: 40.0);
		AssertDashes(dashes3, 26.666667, 26.666667);

		var dashes4 = Dashes(line, pat, 0.0, customEndOffset: 60.0); // E > .5(sum(pat))
		AssertDashes(dashes4, 40.0 * 8 / 14, 40.0 * 8 / 14);

		var dashes5 = Dashes(line, pat, 0.0, customEndOffset: -20.0);
		AssertDashes(dashes5, 40.0 * 4 / 3); // ok

		// TODO end offsets > than patlen not tested (don't understand Pro's behavior)

		// pattern shorter than line (P < L)
		var pat_20_20 = new[] { 20.0, 20.0 };

		var dashes11 = Dashes(line, pat_20_20, 0.0, customEndOffset: 0.0);
		AssertDashes(dashes11, 20.0, 20.0);

		var dashes12 = Dashes(line, pat_20_20, 0.0, customEndOffset: 10.0);
		AssertDashes(dashes12, 20.0 * 8 / 9, 20.0 * 8 / 9, 10.0 * 8 / 9);

		var dashes13 = Dashes(line, pat_20_20, 0.0, customEndOffset: 30.0);
		var dashes14 = Dashes(line, pat_20_20, 0.0, customEndOffset: 50.0); // E > sum(pat)
		var dashes15 = Dashes(line, pat_20_20, 0.0, customEndOffset: -10.0);
		var dashes16 = Dashes(line, pat_20_20, 0.0, customEndOffset: -30.0);
		var dahses17 = Dashes(line, pat_20_20, 0.0, customEndOffset: -50.0);

	}

	[Test, Ignore("known to fail")]
	public void KnownBadDashes1()
	{
		var line = PolylineBuilderEx.CreatePolyline(new[] { Pt(0, 0), Pt(80, 0) });
		var pat = new[] { 40.0, 40.0 };
		var dashes6 = Dashes(line, pat, 0.0, customEndOffset: -40.0);
		AssertDashes(dashes6, 40.0 * 2); // bombs
	}

	[Test]
	public void CanDashesFullDash()
	{
		// line of length 80 (like the line symbol preview in Pro's Symbology pane)
		var line = PolylineBuilderEx.CreatePolyline(new[] { Pt(0, 0), Pt(80, 0) });

		var pat1 = new[] { 20.0, 10.0 }; // P < L (pattern shorter than line)
		var dashes1 = Dashes(line, pat1, GeometricEffects.DashEndings.FullDash);
		AssertDashes(dashes1, 20.0, 20.0, 20.0);

		var pat2 = new[] { 30.0, 30.0 }; // P < L
		var dashes2 = Dashes(line, pat2, GeometricEffects.DashEndings.FullDash);
		AssertDashes(dashes2, 30.0 * 8 / 9, 30.0 * 8 / 9);

		var pat3 = new[] { 30.0 }; // expect same effect as pat2
		var dashes3 = Dashes(line, pat3, GeometricEffects.DashEndings.FullDash);
		AssertDashes(dashes3, 30.0 * 8 / 9, 30.0 * 8 / 9);

		// TODO however, [30 30 30] gives different result in Pro!
	}

	[Test]
	public void CanDashesHalfDash()
	{
		// line of length 80 (like the line symbol preview in Pro's Symbology pane)
		var line = PolylineBuilderEx.CreatePolyline(new[] { Pt(0, 0), Pt(80, 0) });

		var pat1 = new[] { 20.0, 10.0 }; // P < L (pattern shorter than line)
		var dashes1 = Dashes(line, pat1, GeometricEffects.DashEndings.HalfDash);
		AssertDashes(dashes1, 10.0 * 8 / 9, 20.0 * 8 / 9, 20.0 * 8 / 9, 10.0 * 8 / 9);

		var pat2 = new[] { 30.0, 30.0 }; // P < L
		var dashes2 = Dashes(line, pat2, GeometricEffects.DashEndings.HalfDash);
		AssertDashes(dashes2, 15.0 * 8 / 6, 15.0 * 8 / 6);

		var pat3 = new[] { 30.0 }; // expect same effect as pat2
		var dashes3 = Dashes(line, pat3, GeometricEffects.DashEndings.HalfDash);
		AssertDashes(dashes3, 15.0 * 8 / 6, 15.0 * 8 / 6);

		// TODO however, [30 30 30] gives different result in Pro!
	}

	[Test]
	public void CanDashesFullGap()
	{
		// line of length 80 (like the line symbol preview in Pro's Symbology pane)
		var line = PolylineBuilderEx.CreatePolyline(new[] { Pt(0, 0), Pt(80, 0) });

		var pat1 = new[] { 20.0, 10.0 }; // P < L (pattern shorter than line)
		var dashes1 = Dashes(line, pat1, GeometricEffects.DashEndings.FullGap);
		AssertDashes(dashes1, 20.0 * 8 / 7, 20.0 * 8 / 7);

		var pat2 = new[] { 30.0, 30.0 }; // P < L
		var dashes2 = Dashes(line, pat2, GeometricEffects.DashEndings.FullGap);
		AssertDashes(dashes2, 30.0 * 8 / 9);

		var pat3 = new[] { 30.0 }; // expect same effect as pat2
		var dashes3 = Dashes(line, pat3, GeometricEffects.DashEndings.FullGap);
		AssertDashes(dashes3, 30.0 * 8 / 9);

		// TODO however, [30 30 30] gives different result in Pro!
	}

	[Test]
	public void CanDashesHalfGap()
	{
		// line of length 80 (like the line symbol preview in Pro's Symbology pane)
		var line = PolylineBuilderEx.CreatePolyline(new[] { Pt(0, 0), Pt(80, 0) });

		var pat1 = new[] { 20.0, 10.0 }; // P < L (pattern shorter than line)
		var dashes1 = Dashes(line, pat1, GeometricEffects.DashEndings.HalfGap);
		AssertDashes(dashes1, 20.0 * 8 / 9, 20.0 * 8 / 9, 20.0 * 8 / 9);

		var pat2 = new[] { 30.0, 30.0 }; // P < L
		var dashes2 = Dashes(line, pat2, GeometricEffects.DashEndings.HalfGap);
		AssertDashes(dashes2, 30.0 * 8 / 6);

		var pat3 = new[] { 30.0 }; // expect same effect as pat2
		var dashes3 = Dashes(line, pat3, GeometricEffects.DashEndings.HalfGap);
		AssertDashes(dashes3, 30.0 * 8 / 6);

		// TODO however, [30 30 30] gives different result in Pro!
	}

	[Test, Ignore("Not yet implemented")]
	public void CanDashesSingletonPattern()
	{
		var pat = new[] { 10.0 };

		throw new NotImplementedException();
	}

	[Test, Ignore("Not yet implemented")]
	public void CanDashesOddPattern()
	{
		var pat = new[] { 12.0, 8.0, 4.0 };

		throw new NotImplementedException();
	}

	[Test]
	public void CanOffset()
	{
		// null geometry
		Assert.IsNull(GeometricEffects.Offset(null, 5, OffsetType.Round));

		// neither polyline nor polygon: remains unchanged
		var pt = AssertType<MapPoint>(GeometricEffects.Offset(Pt(10, 20), 5, OffsetType.Round));
		Assert.AreEqual(10.0, pt.X, 0.0000001);
		Assert.AreEqual(20.0, pt.Y, 0.0000001);

		// Very rough tests only: finer tests would just test the Pro SDK's Offset() method
		// Caution: for Offset() method positive distance is RIGHT, BUT for geom effect it is LEFT

		// Polyline

		var line = PolylineBuilderEx.CreatePolyline(
			new[] { Pt(0, 0), Pt(10, 0), Pt(10, 10), Pt(20, 0) });

		var ofs1 = AssertType<Polyline>(GeometricEffects.Offset(line, 2.0, OffsetType.Miter));
		Assert.Greater(ofs1.Length, line.Length); // positive is left

		var ofs2 = AssertType<Polyline>(GeometricEffects.Offset(line, -2.0, OffsetType.Miter));
		Assert.Less(ofs2.Length, line.Length); // negative is right

		// Polygon

		var polygon = PolygonBuilderEx.CreatePolygon(Env(10, 10, 20, 20));

		var ofs3 = AssertType<Polygon>(GeometricEffects.Offset(polygon, 2.0, OffsetType.Miter));
		Assert.AreEqual(56.0, ofs3.Length, 0.00001); // positive is left (out)

		var ofs4 = AssertType<Polygon>(GeometricEffects.Offset(polygon, -2.0, OffsetType.Miter));
		Assert.AreEqual(24.0, ofs4.Length, 0.00001); // negative is right (in)
	}

	[Test]
	public void CanMove()
	{
		Assert.IsNull(GeometricEffects.Move(null, 1.0, 2.0));

		var g = GeometricEffects.Move(Pt(10, 20), 0, 0);
		var m0 = AssertType<MapPoint>(g);
		Assert.AreEqual(10.0, m0.X);
		Assert.AreEqual(20.0, m0.Y);

		g = GeometricEffects.Move(Pt(10, 20), 1.1, 2.2);
		var m1 = AssertType<MapPoint>(g);
		Assert.AreEqual(11.1, m1.X, 0.000001);
		Assert.AreEqual(22.2, m1.Y, 0.000001);

		// Empirical
		g = GeometricEffects.Move(Pt(10, 20), double.NaN, double.PositiveInfinity);
		var m9 = AssertType<MapPoint>(g);
		Assert.IsTrue(double.IsNaN(m9.X)); // any operation with NaN is NaN
		Assert.IsTrue(double.IsInfinity(m9.Y)); // any + Inf is Inf
	}

	[Test]
	public void CanReverse()
	{
		Assert.IsNull(GeometricEffects.Reverse(null));

		// neither polyline nor polygon: remains unchanged
		var pt = AssertType<MapPoint>(GeometricEffects.Reverse(Pt(10, 20)));
		Assert.AreEqual(10.0, pt.X, 0.0000001);
		Assert.AreEqual(20.0, pt.Y, 0.0000001);

		// Implementation is just a call to Pro SDK's ReverseOrientation(),
		// so here we just check if it is having any effect:

		var poly = PolygonBuilderEx.CreatePolygon(Env(10, 10, 20, 20));
		Assert.AreEqual(100.0, poly.Area, 0.00001);

		var rev1 = AssertType<Polygon>(GeometricEffects.Reverse(poly));
		Assert.AreEqual(-100.0, rev1.Area, 0.00001);
	}

	[Test]
	public void CanSuppress()
	{
		Assert.IsNull(GeometricEffects.Suppress(null));

		// neither polyline nor polygon: remains unchanged
		var pt = AssertType<MapPoint>(GeometricEffects.Suppress(Pt(10, 20)));
		Assert.AreEqual(10.0, pt.X, 0.0000001);
		Assert.AreEqual(20.0, pt.Y, 0.0000001);
		var env = AssertType<Envelope>(GeometricEffects.Suppress(Env(10, 10, 20, 20)));
		Assert.AreEqual(100.0, env.Area);
		Assert.AreEqual(40.0, env.Length);

		// 0  10 20 30 40 50 60 70 80
		// o~~*--o--*~~o~~*--*~~*--o   (o Vertex, * CP)
		var line = PolylineBuilderEx.CreatePolyline(
			new[]
			{
				Pt(0, 0), CP(10, 0), Pt(20, 0), CP(30, 0), Pt(40, 0),
				CP(50, 0), CP(60, 0), CP(70, 0), Pt(80, 0)
			});

		var sup1 = AssertType<Polyline>(GeometricEffects.Suppress(line));
		Assert.AreEqual(3, sup1.PartCount);
		var len1 = sup1.Parts.Select(p => p.Sum(s => s.Length)).ToArray();
		Assert.AreEqual(10.0, len1[0], 0.00001);
		Assert.AreEqual(20.0, len1[1], 0.00001);
		Assert.AreEqual(10.0, len1[2], 0.00001);

		var sup2 = AssertType<Polyline>(GeometricEffects.Suppress(line, true));
		Assert.AreEqual(3, sup2.PartCount);
		var len2 = sup2.Parts.Select(p => p.Sum(s => s.Length)).ToArray();
		Assert.AreEqual(20.0, len2[0], 0.00001);
		Assert.AreEqual(10.0, len2[1], 0.00001);
		Assert.AreEqual(10.0, len2[2], 0.00001);

		// 20 o---*---o
		//    |       |      o Vertex
		// 10 *       *      * CP
		//    |       |      start at (0,0)
		//  0 o---*---o
		//    0   10  20
		var square = PolygonBuilderEx.CreatePolygon(
			new[]
			{
				Pt(0, 0), CP(0, 10), Pt(0, 20), CP(10, 20),
				Pt(20, 20), CP(20, 10), Pt(20, 0), CP(10, 0)
			});

		var sup3 = AssertType<Polyline>(GeometricEffects.Suppress(square));
		Assert.AreEqual(2, sup3.PartCount);
		Assert.AreEqual(40.0, sup3.Length, 0.00001);
		Assert.AreEqual(20.0, sup3.Parts[0].Sum(s => s.Length));
		Assert.AreEqual(20.0, sup3.Parts[1].Sum(s => s.Length));
		var pt31 = sup3.Parts[0][0].StartPoint;
		Assert.AreEqual(10.0, pt31.X, 0.00001);
		Assert.AreEqual(0.0, pt31.Y, 0.00001);
		var pt32 = sup3.Parts[1][0].StartPoint;
		Assert.AreEqual(10.0, pt32.X, 0.00001);
		Assert.AreEqual(20.0, pt32.Y, 0.00001);

		var sup4 = AssertType<Polyline>(GeometricEffects.Suppress(square, true));
		Assert.AreEqual(2, sup4.PartCount);
		Assert.AreEqual(40.0, sup4.Length, 0.00001);
		Assert.AreEqual(20.0, sup4.Parts[0].Sum(s => s.Length));
		Assert.AreEqual(20.0, sup4.Parts[1].Sum(s => s.Length));
		var pt41 = sup4.Parts[0][0].StartPoint;
		Assert.AreEqual(0.0, pt41.X, 0.00001);
		Assert.AreEqual(10.0, pt41.Y, 0.00001);
		var pt42 = sup4.Parts[1][0].StartPoint;
		Assert.AreEqual(20.0, pt42.X, 0.00001);
		Assert.AreEqual(10.0, pt42.Y, 0.00001);

		// two-part polygon (30x30 square with a 10x10 hole)
		var builder = new PolygonBuilderEx { HasID = true };
		builder.AddPart(new[] { Pt(0, 0), Pt(0, 30), Pt(30, 30), CP(30, 0), Pt(0, 0) });
		builder.AddPart(new[] { Pt(10, 10), Pt(20, 10), CP(20, 20), CP(10, 20), Pt(10, 10) });
		var polygon = builder.ToGeometry();
		Assert.AreEqual(800.0, polygon.Area, 0.00001);

		var sup5 = AssertType<Polyline>(GeometricEffects.Suppress(polygon));
		Assert.AreEqual(2, sup5.PartCount);
		Assert.AreEqual(120.0, sup5.Length, 0.00001);

		var sup6 = AssertType<Polyline>(GeometricEffects.Suppress(polygon, true));
		Assert.AreEqual(2, sup6.PartCount);
		Assert.AreEqual(40, sup6.Length);
	}

	#region Test utilities

	private static Polyline Dashes(
		Geometry shape, double[] pattern,
		double offsetAlongLine = 0.0)
	{
		return Dashes(shape, pattern, offsetAlongLine,
		              GeometricEffects.DashEndings.Unconstrained,
		              GeometricEffects.DashEndings.Unconstrained,
		              0.0);
	}

	private static Polyline Dashes(
		Geometry shape, double[] pattern,
		double offsetAlongLine, double customEndOffset)
	{
		return Dashes(shape, pattern, offsetAlongLine,
		              GeometricEffects.DashEndings.Custom,
		              GeometricEffects.DashEndings.Unconstrained,
		              customEndOffset);
	}

	private static Polyline Dashes(
		Geometry shape, double[] pattern,
		GeometricEffects.DashEndings endingsType,
		GeometricEffects.DashEndings cpType = GeometricEffects.DashEndings.Unconstrained)
	{
		return Dashes(shape, pattern, 0.0, endingsType, cpType, 0.0);
	}

	private static Polyline Dashes(
		Geometry shape, double[] pattern, double offsetAlongLine,
		GeometricEffects.DashEndings endingsType,
		GeometricEffects.DashEndings cpType,
		double customEndOffset)
	{
		var g = GeometricEffects.Dashes(shape, pattern, offsetAlongLine,
		                                endingsType, cpType, customEndOffset);
		return AssertType<Polyline>(g);
	}

	private static T AssertType<T>(object o)
	{
		Assert.IsInstanceOf<T>(o);
		return (T) o;
	}

	private static void AssertDashes(Polyline polyline, params double[] expectedLengths)
	{
		const double delta = 0.000001; // update formatting (:F6 below) when changing here

		var actualLengths = polyline.Parts.Select(p => p.Sum(s => s.Length)).ToArray();

		var comparer = new DeltaComparer(delta);
		bool ok = expectedLengths.Length == actualLengths.Length &&
		          expectedLengths.SequenceEqual(actualLengths, comparer);
		if (! ok)
		{
			var a = string.Join(" ", actualLengths.Select(l => $"{l:F6}"));
			var e = string.Join(" ", expectedLengths.Select(l => $"{l:F6}"));
			Assert.Fail($"Bad dashes: expected {e} but actual {a}");
		}
	}

	private class DeltaComparer : IEqualityComparer<double>
	{
		private readonly double _delta;

		public DeltaComparer(double delta)
		{
			_delta = delta;
		}

		public bool Equals(double x, double y)
		{
			return Math.Abs(x - y) <= _delta;
		}

		public int GetHashCode(double obj)
		{
			return obj.GetHashCode();
		}
	}

	private static MapPoint Pt(double x, double y)
	{
		return MapPointBuilderEx.CreateMapPoint(x, y);
	}

	private static MapPoint CP(double x, double y, int id = 1)
	{
		var builder = new MapPointBuilderEx();
		builder.HasZ = builder.HasM = false;
		builder.X = x;
		builder.Y = y;
		builder.HasID = true;
		builder.ID = id;
		return builder.ToGeometry();
	}

	private static Envelope Env(double xMin, double yMin, double xMax, double yMax)
	{
		return EnvelopeBuilderEx.CreateEnvelope(xMin, yMin, xMax, yMax);
	}

	#endregion
}
