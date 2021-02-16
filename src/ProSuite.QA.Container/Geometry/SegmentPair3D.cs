using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;

namespace ProSuite.QA.Container.Geometry
{
	public class SegmentPair3D : SegmentPair
	{
		public SegmentPair3D([NotNull] SegmentHull segmentProxy,
		                     [NotNull] SegmentHull hullSegmentProxy) :
			base(segmentProxy, hullSegmentProxy, is3D: true) { }

		protected override bool CutCurveHullDetailed(double tolerance,
		                                             out IList<double[]> limits,
		                                             out NearSegment hullStartNear,
		                                             out NearSegment hullEndNear,
		                                             out bool coincident)
		{
			throw new NotImplementedException("Not yet implemented for centerOffset > 0");
		}

		protected override LinPair GetLinPair()
		{
			const bool is3D = true;
			return new LinPair3D(new Lin3D(Hull.Segment.GetStart(is3D), Hull.Segment.GetEnd(is3D)),
			                     new Lin3D(Neighbor.Segment.GetStart(is3D),
			                               Neighbor.Segment.GetEnd(is3D)));
		}

		protected new LinPair3D Geom => (LinPair3D) base.Geom;

		protected override bool CutLineHull(
			double centerOffset, double r2, double tolerance,
			out double tMin, out double tMax,
			out NearSegment hullStartNear, out NearSegment hullEndNear, out bool coincident)
		{
			if (centerOffset != 0)
			{
				throw new NotImplementedException("Not yet implemented for centerOffset > 0");
			}

			if (GeomUtils.Equals3D((Pnt3D) P0s, (Pnt3D) P1s, tolerance) &&
			    GeomUtils.Equals3D((Pnt3D) P0e, (Pnt3D) P1e, tolerance))
			{
				hullStartNear = NearSegment.NearStart;
				hullEndNear = NearSegment.NearEnd;
			}
			else if (GeomUtils.Equals3D((Pnt3D) P0s, (Pnt3D) P1e, tolerance) &&
			         GeomUtils.Equals3D((Pnt3D) P0e, (Pnt3D) P1s, tolerance))
			{
				hullStartNear = NearSegment.NearEnd;
				hullEndNear = NearSegment.NearStart;
			}
			else
			{
				coincident = false;

				bool cut = CutLineHull3D((Pnt3D) P0s, (Pnt3D) L0, (Pnt3D) P1s, (Pnt3D) L1,
				                         r2, out tMin, out tMax,
				                         out hullStartNear, out hullEndNear);

				return cut;
			}

			coincident = true;
			tMin = -Math.Sqrt(r2 / L0.OrigDist2());
			tMax = 1 - tMin;

			return true;
		}

		private bool CutLineHull3D([NotNull] Pnt3D p0s,
		                           [NotNull] Pnt3D l0,
		                           [NotNull] Pnt3D p1s,
		                           [NotNull] Pnt3D l1,
		                           double r2,
		                           out double tMin, out double tMax,
		                           out NearSegment hullStartNear,
		                           out NearSegment hullEndNear)
		{
			// Equation for points X on cylinder around p1 + u * l1 with square radius = r2
			//                2          2
			// ((X - p1) x l1)  = r2 * l1
			//
			// with X = p0 + t * l0, tMin and tMax can be determined
			//
			hullStartNear = NearSegment.NotNear;
			hullEndNear = NearSegment.NotNear;

			if (Geom.IsParallel) // parallel
			{
				return Parallel(p0s, l0, p1s, l1, r2,
				                out tMin, out tMax, ref hullStartNear,
				                ref hullEndNear);
			}

			double l12 = l1 * l1;
			double r2l12 = r2 * l12;

			var p0_p1 = (Pnt3D) (p0s - p1s);
			Pnt3D p0_p1xl1 = p0_p1.VectorProduct(l1);

			if (SegmentUtils.SolveSqr(Geom.L0xl12, 2 * Geom.L0xl1 * p0_p1xl1,
			                          p0_p1xl1 * p0_p1xl1 - r2l12,
			                          out tMin, out tMax))
			{
				return Near(p0s, l0, p1s, l1, r2, l12, ref tMin, ref tMax,
				            ref hullStartNear, ref hullEndNear);
			}

			return false;
		}
	}
}
