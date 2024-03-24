using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public static class SubcurveUtils
	{
		[NotNull]
		internal static Linestring CreateClosedRing(
			[NotNull] IList<IntersectionRun> subcurveInfos,
			[CanBeNull] Pnt3D ringStart,
			double tolerance)
		{
			// Make sure the ring closes (in case there are multiple intersections within the tolerance)

			IList<Linestring> connectedCurves = subcurveInfos.Select(i => i.Subcurve).ToList();

			return CreateClosedRing(connectedCurves, ringStart, tolerance);
		}

		public static Linestring CreateClosedRing(
			[NotNull] IList<Linestring> connectedSubCurves,
			[CanBeNull] Pnt3D ringStart,
			double tolerance)
		{
			Pnt3D firstPoint = connectedSubCurves[0].StartPoint;
			Linestring lastSubcurve = connectedSubCurves[connectedSubCurves.Count - 1];
			Line3D lastSegment = lastSubcurve.Segments[lastSubcurve.SegmentCount - 1];

			lastSegment.SetEndPoint(firstPoint.ClonePnt3D());

			Linestring finishedRing = GeomTopoOpUtils.MergeConnectedLinestrings(
				connectedSubCurves, ringStart, tolerance);

			Assert.True(finishedRing.IsClosed, "The ring is not closed.");

			return finishedRing;
		}
	}
}
