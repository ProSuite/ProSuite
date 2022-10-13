using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.Geometry
{
	public class SegmentPair2D : SegmentPair
	{
		public SegmentPair2D([NotNull] SegmentHull segmentProxy,
		                     [NotNull] SegmentHull hullSegmentProxy) :
			base(segmentProxy, hullSegmentProxy, is3D: false) { }

		protected override LinPair GetLinPair()
		{
			const bool is3D = false;
			return new LinPair2D(new Lin2D(Hull.Segment.GetStart(is3D), Hull.Segment.GetEnd(is3D)),
			                     new Lin2D(Neighbor.Segment.GetStart(is3D),
			                               Neighbor.Segment.GetEnd(is3D)));
		}

		protected new LinPair2D Geom => (LinPair2D) base.Geom;
		//private IEnumerable<IHullPart> GetInflatedHullParts(double inflate)
		//{
		//	double fullOffset = Neighbor.Offset + inflate;
		//	Pnt s = P1s + fullOffset * L1Normal;
		//	Pnt e = P1e + fullOffset * L1Normal;
		//	yield return new LineHullPart(e, s);

		//	s = P1s - fullOffset * L1Normal;
		//	e = P1e - fullOffset * L1Normal;
		//	yield return new LineHullPart(e, s);

		//	foreach (
		//		IHullPart expandedHullPart in
		//		Neighbor.StartCap.GetInflatedHullParts(P1s, L1Normal, Neighbor.Offset, inflate))
		//	{
		//		yield return expandedHullPart;
		//	}
		//	foreach (
		//		IHullPart expandedHullPart in
		//		Neighbor.StartCap.GetInflatedHullParts(P1e, L1Normal, Neighbor.Offset, inflate))
		//	{
		//		yield return expandedHullPart;
		//	}
		//}

		private Pnt L0Normal => Geom.L0.LNormal;
		private Pnt L1Normal => Geom.L1.LNormal;
		private double T1 => Geom.T1;
		private double L12 => Geom.L1.L2;

		private double? _segmentDistance;

		public double SegmentDistance
			=> _segmentDistance ?? (_segmentDistance = GetSegmentDistance()).Value;

		private double GetSegmentDistance()
		{
			if (Hull.Segment.IsLinear && Neighbor.Segment.IsLinear)
			{
				return Geom.SegmentDistance;
			}

			var op =
				(IProximityOperator) Hull.Segment.GetPolyline(forceCreation: true);
			return op.ReturnDistance(Neighbor.Segment.GetPolyline(forceCreation: true));
		}

		private static IEnumerable<HullLine> GetLineHullParts(Lin2D lin, SegmentHull hull)
		{
			Pnt leftOffset = hull.LeftOffset * lin.LNormal;
			yield return new HullLineSimple
			             {Lin = lin.GetParallel(leftOffset), CutPart = CutPart.LeftSide};

			Pnt rightOffset = -hull.RightOffset * lin.LNormal;
			yield return new HullLineSimple
			             {Lin = lin.GetParallel(rightOffset), CutPart = CutPart.RightSide}
				;

			double meanOffset = (hull.LeftOffset + hull.RightOffset) / 2;
			Pnt capOffset = (hull.LeftOffset - hull.RightOffset) / 2 * lin.LNormal;
			Lin2D centerLin = lin.GetParallel(capOffset);

			foreach (HullLine startPart in hull.StartCap.GetHullLines(
				         centerLin, meanOffset, atEnd: false))
			{
				startPart.CutPart = CutPart.StartCap;
				yield return startPart;
			}

			foreach (HullLine endPart in hull.EndCap.GetHullLines(centerLin, meanOffset,
				         atEnd: true))
			{
				endPart.CutPart = CutPart.EndCap;
				yield return endPart;
			}
		}

		private static IEnumerable<IHullPart> GetNeighborHullParts(Lin2D lin, SegmentHull hull)
		{
			Pnt leftOffset = hull.LeftOffset * lin.LNormal;
			yield return
				new LineHullPart(lin.Ps + leftOffset, lin.Pe + leftOffset)
				{CutPart = CutPart.LeftSide};

			Pnt rightOffset = -hull.RightOffset * lin.LNormal;
			yield return
				new LineHullPart(lin.Ps + rightOffset, lin.Pe + rightOffset)
				{CutPart = CutPart.RightSide};

			Pnt capOffset = (hull.LeftOffset - hull.RightOffset) / 2 * lin.LNormal;
			Pnt pCapS = lin.Ps + capOffset;
			Pnt pCapE = lin.Pe + capOffset;

			double meanOffset = (hull.LeftOffset + hull.RightOffset) / 2;

			foreach (IHullPart startPart in hull.StartCap.GetInflatedHullParts(
				         pCapS, pCapE, meanOffset, 0))
			{
				startPart.CutPart = CutPart.StartCap;
				yield return startPart;
			}

			foreach (IHullPart endPart in hull.EndCap.GetInflatedHullParts(
				         pCapE, pCapS, meanOffset, 0))
			{
				endPart.CutPart = CutPart.EndCap;
				yield return endPart;
			}
		}

		private HullCutResult CutHullHullDetailed()
		{
			var result = new HullCutResult();
			double tMin = double.MaxValue;
			double tMax = double.MinValue;

			foreach (HullLine hullPart in GetLineHullParts(Geom.L0, Hull))
			{
				double tMinPart = double.MaxValue;
				double tMaxPart = double.MinValue;

				var simple = hullPart as HullLineSimple;
				var arc = hullPart as HullLineArc;
				var line = hullPart as HullLineLine;

				foreach (IHullPart neighborPart in GetNeighborHullParts(Geom.L1, Neighbor))
				{
					bool intersects;
					double tMinNb = double.MaxValue;
					double tMaxNb = double.MinValue;

					if (simple != null)
						intersects = neighborPart.Cut(simple, ref tMinNb, ref tMaxNb);
					else if (arc != null)
						intersects = neighborPart.Cut(arc, ref tMinNb, ref tMaxNb);
					else if (line != null)
						intersects = neighborPart.Cut(line, ref tMinNb, ref tMaxNb);
					else
						throw new NotImplementedException(hullPart.GetType().ToString());

					if (intersects)
					{
						if (tMinNb < tMinPart)
						{
							result.MinCutPart = neighborPart.CutPart;
							tMinPart = tMinNb;
						}

						if (tMaxNb > tMaxPart)
						{
							result.MaxCutPart = neighborPart.CutPart;
							tMaxPart = tMaxNb;
						}
					}
				}

				tMin = Math.Min(tMin, tMinPart);
				tMax = Math.Max(tMax, tMaxPart);
			}

			result.Min = tMin;
			result.Max = tMax;
			return result;
		}

		protected override bool CutCurveHullDetailed(double tolerance,
		                                             out IList<double[]> limits,
		                                             out NearSegment hullStartNear,
		                                             out NearSegment hullEndNear,
		                                             out bool coincident)
		{
			if (! Hull.Segment.IsLinear || ! Neighbor.Segment.IsLinear)
			{
				throw new NotImplementedException("Not implemented for non linear segments");
			}

			coincident = IsCoincident(tolerance, out hullStartNear, out hullEndNear);

			HullCutResult hullCut = CutHullHullDetailed();
			double tMax = hullCut.Max;
			double tMin = hullCut.Min;

			limits = new List<double[]>();
			if (tMin < tMax)
			{
				limits.Add(new[] {tMin, tMax});
			}

			return limits.Count > 0;
		}

		protected override bool CutLineHull(
			double centerOffset,
			double r2, double tolerance,
			out double tMin, out double tMax,
			out NearSegment hullStartNear,
			out NearSegment hullEndNear,
			out bool coincident)
		{
			coincident = IsCoincident(tolerance, out hullStartNear, out hullEndNear);
			if (! coincident)
			{
				Pnt offset = P0s;
				if (centerOffset != 0)
				{
					offset = P0s + centerOffset * L0Normal;
				}

				bool cut = CutLineHull2D(offset, L0, P1s, L1, r2, out tMin, out tMax,
				                         out hullStartNear, out hullEndNear);

				return cut;
			}

			tMin = -Math.Sqrt(r2 / L0.OrigDist2());
			tMax = 1 - tMin;
			return true;
		}

		private bool IsCoincident(
			double tolerance,
			out NearSegment hullStartNear,
			out NearSegment hullEndNear)
		{
			if (GeomUtils.Equals2D(P0s, P1s, tolerance) &&
			    GeomUtils.Equals2D(P0e, P1e, tolerance))
			{
				hullStartNear = NearSegment.NearStart;
				hullEndNear = NearSegment.NearEnd;
			}
			else if (GeomUtils.Equals2D(P0s, P1e, tolerance) &&
			         GeomUtils.Equals2D(P0e, P1s, tolerance))
			{
				hullStartNear = NearSegment.NearEnd;
				hullEndNear = NearSegment.NearStart;
			}
			else
			{
				hullStartNear = NearSegment.NotNear;
				hullEndNear = NearSegment.NotNear;
				return false;
			}

			return true;
		}

		private bool CutLineHull2D([NotNull] Pnt p0,
		                           [NotNull] Pnt l0,
		                           [NotNull] Pnt p1,
		                           [NotNull] Pnt l1,
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
				return Parallel(p0, l0, p1, l1, r2, out tMin, out tMax, ref hullStartNear,
				                ref hullEndNear);
			}

			double r2l12 = r2 * L12;

			double t0 = (p0.X - p1.X) * l1.Y - (p0.Y - p1.Y) * l1.X;
			if (SegmentUtils.SolveSqr(T1 * T1, 2 * T1 * t0, t0 * t0 - r2l12, out tMin, out tMax))
			{
				return Near(p0, l0, p1, l1, r2, L12, ref tMin, ref tMax,
				            ref hullStartNear, ref hullEndNear);
			}

			return false;
		}

		private class HullCutResult
		{
			public HullCutResult()
			{
				Min = double.MaxValue;
				Max = double.MinValue;
			}

			public double Min { get; set; }
			public double Max { get; set; }
			public CutPart MinCutPart { get; set; }
			public CutPart MaxCutPart { get; set; }

			public override string ToString()
			{
				string min = Math.Abs(Min) > 10 ? "?" : $"{Min:N2}";
				string max = Math.Abs(Max) > 10 ? "?" : $"{Max:N2}";
				return $"Min: {MinCutPart}, {min}; Max: {MaxCutPart}, {max}";
			}
		}
	}
}
