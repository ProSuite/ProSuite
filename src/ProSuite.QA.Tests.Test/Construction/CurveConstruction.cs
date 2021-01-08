using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test.Construction
{
	public class CurveConstruction
	{
		public CurveConstruction([NotNull] IPolycurve emptyCurve, double x, double y)
			: this(emptyCurve, GeometryFactory.CreatePoint(x, y)) { }

		public CurveConstruction([NotNull] IPolycurve emptyCurve,
		                         [NotNull] IPoint startPoint)
		{
			((IZAware) emptyCurve).ZAware = ((IZAware) startPoint).ZAware;

			Curve = emptyCurve;
			Curve.FromPoint = startPoint;
		}

		[NotNull]
		public static CurveConstruction StartLine(double startX, double startY)
		{
			return StartLine(GeometryFactory.CreatePoint(startX, startY));
		}

		[NotNull]
		public static CurveConstruction StartLine(double startX,
		                                          double startY,
		                                          double startZ)
		{
			return StartLine(GeometryFactory.CreatePoint(startX, startY, startZ));
		}

		[NotNull]
		public static CurveConstruction StartLine([NotNull] IPoint startPoint)
		{
			IPolycurve emptyCurve = new PolylineClass();
			return new CurveConstruction(emptyCurve, startPoint);
		}

		[NotNull]
		public static CurveConstruction StartPoly(double startX,
		                                          double startY,
		                                          double startZ)
		{
			return StartPoly(GeometryFactory.CreatePoint(startX, startY, startZ));
		}

		[NotNull]
		public static CurveConstruction StartPoly(double startX, double startY)
		{
			return StartPoly(GeometryFactory.CreatePoint(startX, startY));
		}

		[NotNull]
		public static CurveConstruction StartPoly([NotNull] IPoint startPoint)
		{
			IPolycurve emptyCurve = new PolygonClass();
			return new CurveConstruction(emptyCurve, startPoint);
		}

		[NotNull]
		public IPolycurve Curve { get; }

		[NotNull]
		public CurveConstruction LineTo(double x, double y)
		{
			return LineTo(GeometryFactory.CreatePoint(x, y));
		}

		[NotNull]
		public CurveConstruction LineTo(double x, double y, double z)
		{
			return LineTo(GeometryFactory.CreatePoint(x, y, z));
		}

		[NotNull]
		public CurveConstruction Line(double dx, double dy)
		{
			IPoint add = new PointClass();
			IPoint last = Curve.ToPoint;
			add.PutCoords(last.X + dx, last.Y + dy);
			return LineTo(add);
		}

		[NotNull]
		public CurveConstruction LineTo(IPoint point)
		{
			object missing = Type.Missing;
			((IPointCollection) Curve).AddPoint(point, ref missing, ref missing);

			return this;
		}

		[NotNull]
		public CurveConstruction BezierTo(double x0, double y0,
		                                  double x1, double y1,
		                                  double xe, double ye)
		{
			return BezierTo(GeometryFactory.CreatePoint(x0, y0),
			                GeometryFactory.CreatePoint(x1, y1),
			                GeometryFactory.CreatePoint(xe, ye));
		}

		[NotNull]
		public CurveConstruction BezierTo([NotNull] IPoint p0,
		                                  [NotNull] IPoint p1,
		                                  [NotNull] IPoint pe)
		{
			ILine tangentFrom = new LineClass();
			tangentFrom.FromPoint = Curve.ToPoint;
			tangentFrom.ToPoint = p0;

			ILine tangentTo = new LineClass();
			tangentTo.FromPoint = p1;
			tangentTo.ToPoint = pe;

			IConstructBezierCurve bezier = new BezierCurveClass();
			bezier.ConstructTangentsAtEndpoints(tangentFrom, tangentTo);

			object missing = Type.Missing;

			((ISegmentCollection) Curve).AddSegment((ISegment) bezier, ref missing,
			                                        ref missing);

			return this;
		}

		[NotNull]
		public CurveConstruction CircleTo(double x, double y)
		{
			return CircleTo(GeometryFactory.CreatePoint(x, y));
		}

		[NotNull]
		public CurveConstruction CircleTo([NotNull] IPoint point)
		{
			var segments = (ISegmentCollection) Curve;
			ISegment lastSeg = segments.get_Segment(segments.SegmentCount - 1);
			IConstructCircularArc arc = new CircularArcClass();
			const bool atFrom = false;
			arc.ConstructTangentAndPoint(lastSeg, atFrom, point);

			object missing = Type.Missing;

			((ISegmentCollection) Curve).AddSegment((ISegment) arc, ref missing, ref missing);

			return this;
		}

		[NotNull]
		public CurveConstruction MoveTo(double x, double y)
		{
			return MoveTo(GeometryFactory.CreatePoint(x, y));
		}

		[NotNull]
		public CurveConstruction MoveTo(double x, double y, double z)
		{
			return MoveTo(GeometryFactory.CreatePoint(x, y, z));
		}

		[NotNull]
		public CurveConstruction MoveTo([NotNull] IPoint point)
		{
			object missing = Type.Missing;
			if (Curve is IPolygon)
			{
				var rings = (IGeometryCollection) Curve;
				var lastRing = (IRing) rings.Geometry[rings.GeometryCount - 1];
				lastRing.Close();

				IPointCollection newRing = new RingClass();
				newRing.AddPoint(point, ref missing, ref missing);

				rings.AddGeometry((IGeometry) newRing, ref missing, ref missing);
			}
			else if (Curve is IPolyline)
			{
				var paths = (IGeometryCollection) Curve;

				IPointCollection newPath = new PathClass();
				newPath.AddPoint(point, ref missing, ref missing);

				paths.AddGeometry((IGeometry) newPath, ref missing, ref missing);
			}
			else
			{
				throw new NotImplementedException("Unhandled geometryType" + Curve.GeometryType);
			}

			return this;
		}

		/// <summary>
		/// Only valid for polygons
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public IPolygon ClosePolygon(bool noSimplify = false)
		{
			var poly = ((IPolygon) Curve);
			poly.Close();
			if (! noSimplify)
			{
				((ITopologicalOperator) poly).Simplify();
			}

			return poly;
		}
	}
}
