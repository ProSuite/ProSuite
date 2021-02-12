using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Container.PolygonGrower
{
	public static class TopologicalLineUtils
	{
		[CanBeNull] [ThreadStatic] private static IEnvelope _queryEnvelope;

		[CanBeNull] [ThreadStatic] private static ILine _tangent;

		public static int CalculateOrientation([NotNull] IPolyline polyline,
		                                       double resolution,
		                                       out double yMax)
		{
			return CalculateOrientation((ISegmentCollection) polyline,
			                            resolution,
			                            out yMax);
		}

		public static int CalculateOrientation([NotNull] ISegmentCollection lineSegments,
		                                       double resolution,
		                                       out double yMax)
		{
			double xMax;
			IEnumSegment segmentsAtXMax = GetSegmentsAtXMax(lineSegments, resolution,
			                                                out xMax);

			var segmentsEnum = new SegmentsEnumerator(segmentsAtXMax) {Recycle = false};

			var segments = new SegmentsEnumerable(segmentsEnum);
			var box = new WKSEnvelope();

			foreach (ISegment segment in segments)
			{
				Assert.True(segments.Enumerator.CurrentPartIndex == 0,
				            "Cannot handle multipart lines");
				segment.QueryWKSEnvelope(ref box);

				if (IsZeroLength(segment))
				{
					continue;
				}

				if (box.XMax + resolution < xMax)
				{
					continue;
				}

				WKSPoint fromPoint;
				WKSPoint toPoint;

				segment.QueryWKSFromPoint(out fromPoint);
				segment.QueryWKSToPoint(out toPoint);

				if (fromPoint.X + resolution >= xMax)
				{
					yMax = fromPoint.Y;
					for (int i = segments.Enumerator.CurrentSegmentIndex - 1;
					     i >= 0;
					     i--)
					{
						double toAngle;
						ISegment toSeg = lineSegments.Segment[i];
						if (CalculateToAngle(toSeg, out toAngle))
						{
							double fromAngle;
							CalculateFromAngle(segment, out fromAngle);

							return GetOrientation(toAngle, fromAngle);
						}
					}

					return -2;
				}

				if (toPoint.X + resolution >= xMax)
				{
					yMax = toPoint.Y;

					int segmentCount = lineSegments.SegmentCount;
					for (int i = segments.Enumerator.CurrentSegmentIndex + 1;
					     i < segmentCount;
					     i++)
					{
						double fromAngle;
						ISegment fromSeg = lineSegments.Segment[i];
						if (CalculateFromAngle(fromSeg, out fromAngle))
						{
							double toAngle;
							CalculateToAngle(segment, out toAngle);

							return GetOrientation(toAngle, fromAngle);
						}
					}

					return 2;
				}

				// linearize not linear segment and
				// get orientation of linearized segment
				var densify = 8;
				int orientation;
				do
				{
					object missing = Type.Missing;
					IPolyline line = new PolylineClass();
					((ISegmentCollection) line).AddSegment(segment, ref missing, ref missing);
					line.Densify(line.Length / densify, 0);

					orientation = CalculateOrientation((ISegmentCollection) line, resolution,
					                                   out yMax);

					densify *= 2;
				} // insufficent densification can lead to
				// that start or end point has maxX --> repeat
				while (Math.Abs(orientation) > 1);

				return orientation;
			}

			yMax = double.NaN;
			return 0;
		}

		public static bool CalculateFromAngle([NotNull] ISegmentCollection line,
		                                      out double fromAngle)
		{
			int segmentCount = line.SegmentCount;
			for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
			{
				ISegment segment = line.Segment[segmentIndex];

				if (CalculateFromAngle(segment, out fromAngle))
				{
					return true;
				}
			}

			fromAngle = double.NaN;
			return false;
		}

		public static bool CalculateToAngle([NotNull] ISegmentCollection line,
		                                    out double toAngle)
		{
			for (int segmentIndex = line.SegmentCount - 1; segmentIndex >= 0; segmentIndex--)
			{
				ISegment segment = line.Segment[segmentIndex];

				if (CalculateToAngle(segment, out toAngle))
				{
					return true;
				}
			}

			toAngle = double.NaN;
			return false;
		}

		public static bool CalculateToAngle([NotNull] ISegment segment,
		                                    out double toAngle)
		{
			if (IsZeroLength(segment))
			{
				toAngle = double.NaN;
				return false;
			}

			WKSPoint fromPt;
			WKSPoint toPt;
			if (IsLinear(segment))
			{
				segment.QueryWKSFromPoint(out toPt);
				segment.QueryWKSToPoint(out fromPt);
			}
			else
			{
				const bool asRatio = true;
				segment.QueryTangent(esriSegmentExtension.esriExtendTangentAtTo,
				                     1, asRatio, 1, Tangent);

				segment.QueryWKSToPoint(out toPt);
				((ISegment) Tangent).QueryWKSToPoint(out fromPt);
			}

			bool success = CalculateAngle(fromPt.X, fromPt.Y,
			                              toPt.X, toPt.Y,
			                              out toAngle);
			return success;
		}

		private static bool IsZeroLength([NotNull] ISegment segment)
		{
			return MathUtils.AreSignificantDigitsEqual(segment.Length, 0);
		}

		[NotNull]
		private static ILine Tangent => _tangent ?? (_tangent = new LineClass());

		[NotNull]
		private static IEnvelope QueryEnvelope =>
			_queryEnvelope ?? (_queryEnvelope = new EnvelopeClass());

		private static bool CalculateFromAngle([NotNull] ISegment segment,
		                                       out double fromAngle)
		{
			if (IsZeroLength(segment))
			{
				fromAngle = double.NaN;
				return false;
			}

			WKSPoint fromPt;
			WKSPoint toPt;
			if (IsLinear(segment))
			{
				segment.QueryWKSFromPoint(out fromPt);
				segment.QueryWKSToPoint(out toPt);
			}
			else
			{
				const bool asRatio = false;
				segment.QueryTangent(esriSegmentExtension.esriExtendTangentAtFrom,
				                     0, asRatio, 1, Tangent);

				segment.QueryWKSFromPoint(out fromPt);
				((ISegment) Tangent).QueryWKSToPoint(out toPt);
			}

			return CalculateAngle(fromPt.X, fromPt.Y, toPt.X, toPt.Y, out fromAngle);
		}

		private static bool CalculateAngle(double x0, double y0,
		                                   double x1, double y1,
		                                   out double angle)
		{
			double dx = x1 - x0;
			double dy = y1 - y0;

			if (Math.Abs(dx) < double.Epsilon && Math.Abs(dy) < double.Epsilon)
			{
				// points are coincident, can't calculate angle
				angle = double.NaN;
				return false;
			}

			angle = Math.Atan2(dy, dx);
			return true;
		}

		private static bool IsLinear([NotNull] ISegment segment)
		{
			if (segment is ILine)
			{
				return true;
			}

			// always treat segments other than ILine as effectively non-linear, i.e. get tangents
			// (distinguishing *straight* non-linear segments, i.e. IsLine == true, does not make a performance difference)
			return false;

			//var circularArc = segment as ICircularArc;
			//if (circularArc != null)
			//{
			//    return circularArc.IsLine;
			//}

			//var bezierCurve = segment as IBezierCurve2;
			//if (bezierCurve != null)
			//{
			//    return bezierCurve.IsLine;
			//}

			//var ellipticArc = segment as IEllipticArc;
			//if (ellipticArc != null)
			//{
			//    return ellipticArc.IsLine;
			//}

			//// whatever it is, it's probably not linear
			//return false;
		}

		[NotNull]
		private static IEnumSegment GetSegmentsAtXMax([NotNull] ISegmentCollection segments,
		                                              double resolution,
		                                              out double xMax)
		{
			((IGeometry) segments).QueryEnvelope(QueryEnvelope);

			xMax = QueryEnvelope.XMax;

			double dx = resolution * 20;

			QueryEnvelope.XMax = xMax + dx;
			QueryEnvelope.XMin = xMax;

			// Remark: ((IPointCollection4)_line).get_IndexedEnumVertices is not implemented for Polylines
			//          only for Multipoints ?
			// Remark: ((ISegmentCollection)_line).IndexedEnumSegments is not implemented for IPath

			var polyline = segments as IPolyline;
			return polyline == null
				       ? segments.EnumSegments
				       : segments.IndexedEnumSegments[QueryEnvelope];
		}

		private static int GetOrientation(double toAngle, double fromAngle)
		{
			Assert.True(Math.Abs(toAngle) - Math.PI / 2 > -1.0e-10,
			            "Invalid toAngle " + toAngle);
			Assert.True(Math.Abs(fromAngle) - Math.PI / 2 > -1.0e-10,
			            "Invalid fromAngle " + fromAngle);

			if (toAngle < 0)
			{
				toAngle += 2 * Math.PI;
			}

			if (fromAngle < 0)
			{
				fromAngle += 2 * Math.PI;
			}

			int orientation = toAngle > fromAngle
				                  ? -1
				                  : 1;
			return orientation;
		}
	}
}
