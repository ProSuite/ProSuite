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
	public void CanDashesCustomEndOffset1()
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

	[Test]
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

	private static Envelope Env(double xMin, double yMin, double xMax, double yMax)
	{
		return EnvelopeBuilderEx.CreateEnvelope(xMin, yMin, xMax, yMax);
	}

	#endregion
}
