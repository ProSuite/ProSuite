using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.Geometry
{
	public abstract class SegmentPair
	{
		[NotNull] private readonly SegmentHull _hull;
		[NotNull] private readonly SegmentHull _neighbor;
		private readonly bool _is3D;

		public static SegmentPair Create([NotNull] SegmentHull hull,
		                                 [NotNull] SegmentHull neighbor,
		                                 bool is3D)
		{
			SegmentPair created = is3D
				                      ? (SegmentPair) new SegmentPair3D(hull, neighbor)
				                      : new SegmentPair2D(hull, neighbor);
			return created;
		}

		protected SegmentPair([NotNull] SegmentHull hull,
		                      [NotNull] SegmentHull neighbor,
		                      bool is3D)
		{
			_hull = hull;
			_neighbor = neighbor;
			_is3D = is3D;
		}

		[NotNull]
		public SegmentHull Hull
		{
			get { return _hull; }
		}

		[NotNull]
		public SegmentHull Neighbor
		{
			get { return _neighbor; }
		}

		private LinPair _linPair;

		protected abstract LinPair GetLinPair();

		protected LinPair Geom => _linPair ?? (_linPair = GetLinPair());

		protected Pnt P0s => Geom.L0.Ps;
		protected Pnt P1s => Geom.L1.Ps;

		protected Pnt P0e => Geom.L0.Pe;
		protected Pnt P1e => Geom.L1.Pe;

		protected Pnt L0 => Geom.L0.L;
		protected Pnt L1 => Geom.L1.L;

		protected abstract bool CutCurveHullDetailed(double tolerance,
		                                             [NotNull] out IList<double[]> limits,
		                                             out NearSegment hullStartNear,
		                                             out NearSegment hullEndNear,
		                                             out bool coincident);

		public bool CutCurveHull(double tolerance,
		                         [NotNull] out IList<double[]> limits,
		                         out NearSegment hullStartNear,
		                         out NearSegment hullEndNear,
		                         out bool coincident)
		{
			if (_hull.IsFullDeflatable() && _neighbor.IsFullDeflatable())
			{
				return CutFullDeflatableCurveHull(tolerance, out limits, out hullStartNear,
				                                  out hullEndNear, out coincident);
			}

			return CutCurveHullDetailed(tolerance, out limits, out hullStartNear,
			                            out hullEndNear, out coincident);
		}

		public bool CutFullDeflatableCurveHull(double tolerance,
		                                       [NotNull] out IList<double[]> limits,
		                                       out NearSegment hullStartNear,
		                                       out NearSegment hullEndNear,
		                                       out bool coincident)
		{
			double r = _hull.MaxOffset + _neighbor.MaxOffset;
			double r2 = r * r;
			if (_hull.Segment.IsLinear && _neighbor.Segment.IsLinear)
			{
				double tMin;
				double tMax;
				bool cut = CutLineHull(0, r2, tolerance,
				                       out tMin, out tMax, out hullStartNear,
				                       out hullEndNear, out coincident);

				limits = new List<double[]>();
				if (cut && tMin <= 1 && tMax >= 0)
				{
					limits.Add(new[] {tMin, tMax});
				}

				return cut;
			}

			return CutCurveHullBuffer(_hull.Segment, _neighbor.Segment,
			                          _is3D, r2, out limits,
			                          out hullStartNear,
			                          out hullEndNear, out coincident);
		}

		public bool CutLineOffsetHull(double offset, double r2, double tolerance,
		                              out double tMin, out double tMax,
		                              out NearSegment hullStartNear,
		                              out NearSegment hullEndNear,
		                              out bool coincident)
		{
			if (! _hull.Segment.IsLinear || ! _neighbor.Segment.IsLinear)
			{
				throw new NotImplementedException("Cannot handle non linear segments");
			}

			return CutLineHull(offset, r2, tolerance, out tMin, out tMax, out hullStartNear,
			                   out hullEndNear, out coincident);
		}

		protected abstract bool CutLineHull(double centerOffset, double r2, double tolerance,
		                                    out double tMin, out double tMax,
		                                    out NearSegment hullStartNear,
		                                    out NearSegment hullEndNear,
		                                    out bool coincident);

		private static bool CutCurveHullBuffer([NotNull] SegmentProxy segment,
		                                       [NotNull] SegmentProxy hull,
		                                       bool is3D, double r2,
		                                       [NotNull] out IList<double[]> limits,
		                                       out NearSegment hullStartNear,
		                                       out NearSegment hullEndNear,
		                                       out bool coincident)
		{
			coincident = GetCoincident(segment, hull, is3D, out hullStartNear, out hullEndNear);
			if (r2 > 0)
			{
				IPolygon buffer = hull.CreateBuffer(Math.Sqrt(r2));
				limits = SegmentUtils.GetLimits(segment, buffer);
			}
			else if (coincident)
			{
				limits = new List<double[]>();
				limits.Add(new double[] {0, 1});
			}
			else
			{
				limits = new List<double[]>();
			}

			return limits.Count > 0;
		}

		private static bool GetCoincident([NotNull] SegmentProxy segment,
		                                  [NotNull] SegmentProxy hull,
		                                  bool is3D,
		                                  out NearSegment hullStartNear,
		                                  out NearSegment hullEndNear)
		{
			hullStartNear = NearSegment.NotNear; // TODO
			hullEndNear = NearSegment.NotNear; // TODO

			IPolyline segLine = segment.GetPolyline(false);
			IPolyline hullLine = hull.GetPolyline(false);

			if (! ((IRelationalOperator) segLine).Equals(hullLine))
			{
				return false;
			}

			var coincident = true;

			Pnt segmentStart = segment.GetStart(is3D);
			Pnt segmentEnd = segment.GetEnd(is3D);

			Pnt hullStart = hull.GetStart(is3D);
			Pnt hullEnd = hull.GetEnd(is3D);

			double distFromSegFromHull;
			{
				double dx = segmentStart.X - hullStart.X;
				double dy = segmentStart.Y - hullStart.Y;
				distFromSegFromHull = dx * dx + dy * dy;
			}
			double distFromSegToHull;
			{
				double dx = segmentStart.X - hullEnd.X;
				double dy = segmentStart.Y - hullEnd.Y;
				distFromSegToHull = dx * dx + dy * dy;
			}

			bool isInverse = (distFromSegFromHull > distFromSegToHull);
			Pnt hullMatchSegFrom;
			Pnt hullMatchSegTo;

			if (! isInverse)
			{
				hullMatchSegFrom = hullStart;
				hullMatchSegTo = hullEnd;
			}
			else
			{
				hullMatchSegFrom = hullEnd;
				hullMatchSegTo = hullStart;
			}

			if (is3D)
			{
				double zPrecision = 0;
				ISpatialReference spatialReference = segment.SpatialReference;

				if (spatialReference != null && spatialReference.HasZPrecision())
				{
					double falseZ;
					double zUnits;
					spatialReference.GetZFalseOriginAndUnits(out falseZ, out zUnits);
					zPrecision = 1 / zUnits;
				}

				if (Math.Abs(hullMatchSegFrom[2] - segmentStart[2]) > zPrecision ||
				    Math.Abs(hullMatchSegTo[2] - segmentStart[2]) > zPrecision)
				{
					coincident = false;
				}
			}

			if (coincident)
			{
				if (! isInverse)
				{
					hullStartNear = NearSegment.NearStart;
					hullEndNear = NearSegment.NearEnd;
				}
				else
				{
					hullStartNear = NearSegment.NearEnd;
					hullEndNear = NearSegment.NearStart;
				}
			}

			return coincident;
		}

		protected static bool Parallel([NotNull] Pnt p0,
		                               [NotNull] Pnt l0,
		                               [NotNull] Pnt p1,
		                               [NotNull] Pnt l1,
		                               double r2,
		                               out double tMin, out double tMax,
		                               ref NearSegment hullStartNear,
		                               ref NearSegment hullEndNear)
		{
			if (SegmentUtils.CutLineCircle(p0, l0, p1, r2, out tMin, out tMax) == false)
			{
				return false;
			}

			hullStartNear = Near(tMin, tMax);

			double cMin, cMax;
			if (SegmentUtils.CutLineCircle(p0, l0, p1 + l1, r2, out cMin, out cMax))
			{
				hullEndNear = Near(cMin, cMax);

				tMin = Math.Min(tMin, cMin);
				tMax = Math.Max(tMax, cMax);
			}

			return true;
		}

		protected static bool Near([NotNull] Pnt p0,
		                           [NotNull] Pnt l0,
		                           [NotNull] Pnt p1,
		                           [NotNull] Pnt l1,
		                           double r2,
		                           double l12,
		                           ref double tMin, ref double tMax,
		                           ref NearSegment hullStartNear,
		                           ref NearSegment hullEndNear)
		{
			double cMin, cMax;

			double u0 = (p0 + tMin * l0 - p1) * l1 / l12;
			double u1 = (p0 + tMax * l0 - p1) * l1 / l12;

			if (u0 < 0)
			{
				if (SegmentUtils.CutLineCircle(p0, l0, p1, r2, out cMin, out cMax) == false)
				{
					return false;
				}

				hullStartNear = Near(cMin, cMax);

				// Debug.Assert(cMin > tMin); numerical exceptions possible
				tMin = cMin;
				if (u1 < 0)
				{
					//Debug.Assert(cMax < tMax); numerical exceptions possible
					tMax = cMax;
				}
			}
			else if (u0 > 1)
			{
				if (SegmentUtils.CutLineCircle(p0, l0, p1 + l1, r2, out cMin, out cMax) == false)
				{
					return false;
				}

				hullEndNear = Near(cMin, cMax);

				//Debug.Assert(cMin > tMin); numerical exceptions possible
				tMin = cMin;
				if (u1 > 1)
				{
					//Debug.Assert(cMax < tMax); numerical exceptions possible
					tMax = cMax;
				}
			}

			if (u1 < 0 && u0 >= 0)
			{
				if (SegmentUtils.CutLineCircle(p0, l0, p1, r2, out cMin, out cMax) == false)
				{
					//throw new InvalidProgramException(
					//	"error in software design assumption"); numerical exceptions possible
					tMax = tMin + u0 / (u0 - u1) * (tMax - tMin);
					hullStartNear = Near(tMin, tMax);
				}
				else
				{
					hullStartNear = Near(cMin, cMax);

					//Debug.Assert(cMax < tMax); numerical exceptions possible
					tMax = cMax;
				}
			}
			else if (u1 > 1 && u0 <= 1)
			{
				if (SegmentUtils.CutLineCircle(p0, l0, p1 + l1, r2, out cMin, out cMax) == false)
				{
					//throw new InvalidProgramException(
					//	"error in software design assumption"); numerical exceptions possible
					tMax = tMin + (1 - u0) / (u1 - u0) * (tMax - tMin);
					hullEndNear = Near(tMin, tMax);
				}
				else
				{
					hullEndNear = Near(cMin, cMax);

					//Debug.Assert(cMax < tMax); numerical exceptions possible
					tMax = cMax;
				}
			}

			return true;
		}

		private static NearSegment Near(double tMin, double tMax)
		{
			if (tMin > 1)
			{
				return NearSegment.PostEnd;
			}

			if (tMax < 0)
			{
				return NearSegment.PreStart;
			}

			if (tMin <= 0)
			{
				return tMax - 1 > -tMin
					       ? NearSegment.NearEnd
					       : NearSegment.NearStart;
			}

			return tMax >= 1
				       ? NearSegment.NearEnd
				       : NearSegment.NearLine;
		}
	}
}
