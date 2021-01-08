using System;
using System.Threading;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public class LineIntersection
	{
		private static readonly ThreadLocal<IPoint> _alongPoint =
			new ThreadLocal<IPoint>(() => new PointClass());

		private static readonly ThreadLocal<ILine2> _lineTemplate =
			new ThreadLocal<ILine2>(() => new LineClass());

		[NotNull] private readonly IPolyline _a;
		[NotNull] private readonly IPolyline _b;
		private readonly bool _is3D;

		private double _distanceAlongA;
		private double _distanceAlongB;

		private double _angle;

		[CLSCompliant(false)]
		public LineIntersection([NotNull] IPolyline a, [NotNull] IPolyline b,
		                        [NotNull] IPoint at, bool is3D)
		{
			_a = a;
			_b = b;
			At = at;
			_is3D = is3D;

			_distanceAlongA = double.NaN;
			_distanceAlongB = double.NaN;

			_angle = double.NaN;
		}

		[NotNull]
		[CLSCompliant(false)]
		public IPoint At { get; }

		public double Angle
		{
			get
			{
				if (double.IsNaN(_angle))
				{
					_angle = GetCrossingAngle();
				}

				return _angle;
			}
		}

		public double DistanceAlongA
		{
			get
			{
				if (double.IsNaN(_distanceAlongA))
				{
					_distanceAlongA = GetDistanceAlongPolyline(_a, At);
				}

				return _distanceAlongA;
			}
		}

		public double DistanceAlongB
		{
			get
			{
				if (double.IsNaN(_distanceAlongB))
				{
					_distanceAlongB = GetDistanceAlongPolyline(_b, At);
				}

				return _distanceAlongB;
			}
		}

		private static double GetDistanceAlongPolyline([NotNull] IPolyline polyline,
		                                               [NotNull] IPoint point)
		{
			double distanceAlong = 0;
			double distanceFrom = 0;
			var rightSide = false;
			const bool asRatio = true;

			polyline.QueryPointAndDistance(
				esriSegmentExtension.esriNoExtension,
				point, asRatio, _alongPoint.Value,
				ref distanceAlong, ref distanceFrom, ref rightSide);

			return distanceAlong;
		}

		private double GetCrossingAngle()
		{
			double distanceAlongA = DistanceAlongA;

			QueryTangent(_a, distanceAlongA, _lineTemplate.Value);
			WKSPoint wksFromA;
			WKSPoint wksToA;
			_lineTemplate.Value.QueryWKSCoords(out wksFromA, out wksToA);
			double zFromA = 0, zToA = 0;
			if (_is3D)
			{
				QueryZs(_lineTemplate.Value, out zFromA, out zToA);
			}

			double distanceAlongB = DistanceAlongB;
			QueryTangent(_b, distanceAlongB, _lineTemplate.Value);
			WKSPoint wksFromB;
			WKSPoint wksToB;
			_lineTemplate.Value.QueryWKSCoords(out wksFromB, out wksToB);
			double zFromB = 0, zToB = 0;
			if (_is3D)
			{
				QueryZs(_lineTemplate.Value, out zFromB, out zToB);
			}

			double dx1 = wksToA.X - wksFromA.X;
			double dx2 = wksToB.X - wksFromB.X;
			double dy1 = wksToA.Y - wksFromA.Y;
			double dy2 = wksToB.Y - wksFromB.Y;

			double x = dx1 * dx2 + dy1 * dy2;

			if (_is3D)
			{
				double dz1 = zToA - zFromA;
				double dz2 = zToB - zFromB;

				x += dz1 * dz2;
			}

			double angle = Math.Abs(Math.Acos(x));

			if (angle > Math.PI / 2)
			{
				angle = Math.PI - angle;
			}

			return angle;
		}

		private static void QueryZs([NotNull] ILine line,
		                            out double fromZ,
		                            out double toZ)
		{
			((ISegmentZ) line).GetZs(out fromZ, out toZ);
		}

		private static void QueryTangent([NotNull] IPolyline polyline,
		                                 double distanceAlong,
		                                 [NotNull] ILine templateLine)
		{
			const bool asRatio = true;
			const double length = 1.0;

			polyline.QueryTangent(esriSegmentExtension.esriNoExtension, distanceAlong,
			                      asRatio, length, templateLine);
		}
	}
}
