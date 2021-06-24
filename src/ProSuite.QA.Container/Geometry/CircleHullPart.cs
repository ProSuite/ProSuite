using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.Geometry
{
	public class CircleHullPart : IHullPart
	{
		private readonly Pnt _center;
		private readonly double _radius;

		public CircleHullPart(Pnt center, double radius)
		{
			_center = center;
			_radius = radius;
		}

		public double? Angle { get; set; }
		public double StartDirection { get; set; }

		CutPart IHullPart.CutPart { get; set; }

		public override string ToString()
		{
			return $"P:{_center}, R:{_radius:N1}, S:{StartDirection:N2}, A:{Angle:N2}";
		}

		public void Cut(Pnt segStart, Pnt segmentLine, double offset,
		                ref double tMin, ref double tMax)
		{
			if (! Angle.HasValue)
			{
				CutCircle(segStart, segmentLine, offset, ref tMin, ref tMax);
			}
			else
			{
				CutArc(segStart, segmentLine, offset, ref tMin, ref tMax);
			}
		}

		public bool Cut(HullLineSimple line, ref double tMin, ref double tMax)
		{
			return CutLineThis(line.Lin.Ps, line.Lin.L, ref tMin, ref tMax);
		}

		public bool Cut(HullLineArc line, ref double tMin, ref double tMax)
		{
			double angS = line.StartDirection;
			double r = line.Radius;

			// Handle start point of line-arc
			var aS = new Pnt2D(r * Math.Cos(angS), r * Math.Sin(angS));
			Lin2D s = line.Lin.GetParallel(aS);
			bool intersects = CutLineThis(s.Ps, s.L, ref tMin, ref tMax);

			// Handle end point of line-arc
			double angE = angS + line.Angle;
			var aE = new Pnt2D(r * Math.Cos(angE), r * Math.Sin(angE));
			Lin2D e = line.Lin.GetParallel(aE);
			intersects |= CutLineThis(e.Ps, e.L, ref tMin, ref tMax);

			if (Angle.HasValue)
			{
				intersects |=
					CutLineEndpoint(StartDirection + Angle.Value, line, ref tMin, ref tMax);

				intersects |= CutLineEndpoint(StartDirection, line, ref tMin, ref tMax);
			}

			intersects |= CutLineThis(line.Lin.Ps, line.Lin.L, ref tMin, ref tMax,
			                          new SignedHullLineArc(line, deflate: false));
			intersects |= CutLineThis(line.Lin.Ps, line.Lin.L, ref tMin, ref tMax,
			                          new SignedHullLineArc(line, deflate: true));
			return intersects;
		}

		private bool CutLineEndpoint(double angle, HullLineArc line, ref double tMin,
		                             ref double tMax)
		{
			var intersects = false;
			double r = line.Radius;
			Pnt p = _center + new Pnt2D(_radius * Math.Cos(angle), _radius * Math.Sin(angle));
			double tCircleMin, tCircleMax;
			if (SegmentUtils.CutLineCircle(line.Lin.Ps, line.Lin.L, p, r * r,
			                               out tCircleMin, out tCircleMax))
			{
				intersects |= ValidateIntersects(p, line, tCircleMin, ref tMin, ref tMax);
				intersects |= ValidateIntersects(p, line, tCircleMax, ref tMin, ref tMax);
			}

			return intersects;
		}

		private static bool ValidateIntersects(Pnt center, HullLineArc line, double t,
		                                       ref double tMin,
		                                       ref double tMax)
		{
			Pnt dir = center - (line.Lin.Ps + t * line.Lin.L);
			double angle = Math.Atan2(dir.Y, dir.X);

			if (! IsInArcAngle(angle, line.StartDirection, line.Angle))
			{
				return false;
			}

			tMin = Math.Min(tMin, t);
			tMax = Math.Max(tMax, t);
			return true;
		}

		public bool Cut(HullLineLine line, ref double tMin, ref double tMax)
		{
			// Handle start point of endPart
			Pnt lS = line.EndPart.Ps;
			Lin2D s = line.Lin.GetParallel(lS);
			bool intersects = CutLineThis(s.Ps, s.L, ref tMin, ref tMax);

			// Handle end point of endPart
			Pnt lE = line.EndPart.Pe;
			Lin2D e = line.Lin.GetParallel(lE);
			intersects |= CutLineThis(e.Ps, e.L, ref tMin, ref tMax);

			if (Angle.HasValue)
			{
				double r = _radius;
				// Handle start point of arc
				double angS = StartDirection;
				Pnt aS = new Pnt2D(r * Math.Cos(angS), r * Math.Sin(angS)) + _center - lS;
				Lin2D eS = line.EndPart.GetParallel(aS);
				intersects |= LineHullPart.CutLin(eS, e, ref tMin, ref tMax);

				// Handle end point of arc
				double angE = StartDirection + Angle.Value;
				Pnt aE = new Pnt2D(r * Math.Cos(angE), r * Math.Sin(angE)) + _center - lS;
				Lin2D eE = line.EndPart.GetParallel(aE);
				intersects |= LineHullPart.CutLin(eE, e, ref tMin, ref tMax);
			}

			// Handle tangents
			Pnt eNormal = line.EndPart.LNormal;
			double dir = Angle.HasValue
				             ? Math.Atan2(eNormal.Y, eNormal.X)
				             : double.NaN;

			if (! Angle.HasValue || IsInArcAngle(dir))
			{
				Lin2D eT0 = line.EndPart.GetParallel(_center + _radius * eNormal - lS);
				intersects |= LineHullPart.CutLin(eT0, e, ref tMin, ref tMax);
			}

			if (! Angle.HasValue || IsInArcAngle(dir + Math.PI))
			{
				Lin2D eT1 = line.EndPart.GetParallel(_center - _radius * eNormal - lS);
				intersects |= LineHullPart.CutLin(eT1, e, ref tMin, ref tMax);
			}

			return intersects;
		}

		private bool CutLineThis(Pnt p0, Pnt l0, ref double tMin, ref double tMax,
		                         SignedHullLineArc arc = null)
		{
			double r = _radius + (arc?.Radius ?? 0);
			double tCircleMin;
			double tCircleMax;
			if (! SegmentUtils.CutLineCircle(
				    p0, l0, _center, r * r,
				    out tCircleMin, out tCircleMax))
			{
				return false;
			}

			var cuts = false;

			double f = r > 0 ? 1 : -1;

			Pnt dirMin = p0 + tCircleMin * l0 - _center;
			if (ValidateAngles(f * dirMin, arc))
			{
				cuts = true;
				tMin = Math.Min(tMin, tCircleMin);
				tMax = Math.Max(tMax, tCircleMin);
			}

			Pnt dirMax = p0 + tCircleMax * l0 - _center;
			if (ValidateAngles(f * dirMax, arc))
			{
				cuts = true;
				tMin = Math.Min(tMin, tCircleMax);
				tMax = Math.Max(tMax, tCircleMax);
			}

			return cuts;
		}

		private bool ValidateAngles(Pnt dir, [CanBeNull] SignedHullLineArc arc)
		{
			double angle = Math.Atan2(dir.Y, dir.X);
			if (! IsInArcAngle(angle))
			{
				return false;
			}

			if (arc != null)
			{
				double arcAngle = angle + (arc.Deflate ? 0 : Math.PI);
				if (! IsInArcAngle(arcAngle, arc.Arc.StartDirection, arc.Arc.Angle))
				{
					return false;
				}
			}

			return true;
		}

		private void CutCircle(Pnt segStart, Pnt segmentLine, double offset, ref double tMin,
		                       ref double tMax)
		{
			Pnt near = _center - segStart;
			double centerOffset = SegmentUtils.GetOffset(near, segmentLine);

			if (Math.Abs(centerOffset) < offset)
			{
				double centerAlong = SegmentUtils.GetAlongFraction(near, segmentLine);
				double segmentLength = Math.Sqrt(segmentLine.OrigDist2());

				tMin = Math.Min(tMin, centerAlong - _radius / segmentLength);
				tMax = Math.Max(tMax, centerAlong + _radius / segmentLength);
			}
			else if (_radius + offset > Math.Abs(centerOffset))
			{
				Pnt offsetCenter;
				if (offset <= 0)
				{
					offsetCenter = _center;
				}
				else
				{
					double centerAlong = SegmentUtils.GetAlongFraction(near, segmentLine);
					Pnt along = segStart + centerAlong * segmentLine;
					double f = (Math.Abs(centerOffset) - offset) / Math.Abs(centerOffset);
					offsetCenter = along + f * (_center - along);
				}

				double tCircleMin, tCircleMax;
				if (SegmentUtils.CutLineCircle(segStart, segmentLine, offsetCenter,
				                               _radius * _radius,
				                               out tCircleMin, out tCircleMax))
				{
					tMin = Math.Min(tMin, tCircleMin);
					tMax = Math.Max(tMax, tCircleMax);
				}
			}
		}

		private bool IsInArcAngle(double direction)
		{
			if (! Angle.HasValue)
			{
				return true;
			}

			return IsInArcAngle(direction, StartDirection, Angle.Value);
		}

		private static bool IsInArcAngle(double direction, double startDir, double signedAngle)
		{
			double start;
			double angle;
			if (signedAngle >= 0)
			{
				start = startDir;
				angle = signedAngle;
			}
			else
			{
				start = startDir + signedAngle;
				angle = -signedAngle;
			}

			double d = (direction - start) % (2 * Math.PI);
			if (d < 0)
			{
				d += 2 * Math.PI;
			}

			return d <= angle;
		}

		private void CutArc(Pnt segStart, Pnt segmentLine, double offset, ref double tMin,
		                    ref double tMax)
		{
			Pnt near = _center - segStart;
			double centerOffset = SegmentUtils.GetOffset(near, segmentLine);

			if (Math.Abs(centerOffset) < offset)
			{
				var canReturn = false;
				double lineAngle = Math.Atan2(segmentLine.Y, segmentLine.X);
				double centerAlong = SegmentUtils.GetAlongFraction(near, segmentLine);
				double segmentLength = Math.Sqrt(segmentLine.OrigDist2());

				if (IsInArcAngle(lineAngle))
				{
					canReturn = true;
					tMax = Math.Max(tMax, centerAlong + _radius / segmentLength);
				}

				double reverseLineAngle = lineAngle - Math.PI;
				if (IsInArcAngle(reverseLineAngle))
				{
					tMin = Math.Min(tMin, centerAlong - _radius / segmentLength);
					if (canReturn)
					{
						return;
					}
				}
			}

			Pnt start = _center +
			            _radius * new Pnt2D(Math.Cos(StartDirection), Math.Sin(StartDirection))
			            - segStart;
			double startOffset = SegmentUtils.GetOffset(start, segmentLine);
			if (Math.Abs(startOffset) < offset)
			{
				double startAlong = SegmentUtils.GetAlongFraction(start, segmentLine);
				tMin = Math.Min(tMin, startAlong);
				tMax = Math.Max(tMax, startAlong);
			}

			double endDirection = StartDirection + (Angle ?? 0);
			Pnt end = _center +
			          _radius * new Pnt2D(Math.Cos(endDirection), Math.Sin(endDirection))
			          - segStart;
			double endOffset = SegmentUtils.GetOffset(end, segmentLine);
			if (Math.Abs(endOffset) < offset)
			{
				double endAlong = SegmentUtils.GetAlongFraction(end, segmentLine);
				tMin = Math.Min(tMin, endAlong);
				tMax = Math.Max(tMax, endAlong);
			}

			if (_radius + offset > Math.Abs(centerOffset))
			{
				var segmentStarts = new List<Pnt>();
				{
					double segmentLength = Math.Sqrt(segmentLine.OrigDist2());
					Pnt off = offset / segmentLength * new Pnt2D(-segmentLine.Y, segmentLine.X);
					segmentStarts.Add(segStart + off);
					segmentStarts.Add(segStart - off);
				}

				foreach (Pnt segmentStart in segmentStarts)
				{
					double tCircleMin, tCircleMax;
					if (SegmentUtils.CutLineCircle(segmentStart, segmentLine, _center,
					                               _radius * _radius,
					                               out tCircleMin, out tCircleMax))
					{
						foreach (double t in new[] {tCircleMin, tCircleMax})
						{
							Pnt dirPnt = segmentStart + t * segmentLine - _center;
							double dirAngle = Math.Atan2(dirPnt.Y, dirPnt.X);

							if (IsInArcAngle(dirAngle))
							{
								tMin = Math.Min(tMin, t);
								tMax = Math.Max(tMax, t);
							}
						}
					}
				}
			}
		}

		private class SignedHullLineArc
		{
			public SignedHullLineArc(HullLineArc arc, bool deflate)
			{
				Arc = arc;
				Deflate = deflate;
			}

			public HullLineArc Arc { get; }
			public bool Deflate { get; }

			public double Radius => Arc.Radius * (Deflate ? -1 : 1);
		}
	}
}
