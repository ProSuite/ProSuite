using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.Geometry
{
	[Obsolete("move to ProSuite.Commons.Ao rename")]
	public static class SegmentUtils_
	{
		[ThreadStatic] private static IPoint _helpPoint;

		internal static IPoint HelpPoint
		{
			get { return _helpPoint ?? (_helpPoint = new PointClass()); }
		}

		public static bool CutCurveCircle([NotNull] SegmentProxy segmentProxy,
		                                  [NotNull] Pnt circleCenter,
		                                  double r2, bool as3D,
		                                  out IList<double[]> limits)
		{
			if (segmentProxy.IsLinear)
			{
				double tMin;
				double tMax;
				bool cut = as3D
					           ? CutLineCircle3D(segmentProxy, circleCenter, r2,
					                             out tMin, out tMax)
					           : CutLineCircle2D(segmentProxy, circleCenter, r2,
					                             out tMin, out tMax);

				limits = new List<double[]>();
				if (cut && tMin < 1 && tMax > 0)
				{
					limits.Add(new[] {tMin, tMax});
				}

				return cut;
			}

			return CutCurveCircle(segmentProxy, circleCenter, r2, out limits);
		}

		public static double GetClosestPointFraction([NotNull] SegmentProxy segmentProxy,
		                                             [NotNull] Pnt nearPoint,
		                                             bool as3D)
		{
			return GetClosestPointFraction(segmentProxy, nearPoint, out _, out _, as3D);
		}

		public static double GetClosestPointFraction([NotNull] SegmentProxy segmentProxy,
		                                             [NotNull] Pnt nearPoint,
		                                             out double? offset,
		                                             out bool? onRightSide,
		                                             bool as3D)
		{
			if (segmentProxy.IsLinear)
			{
				Pnt p0s = segmentProxy.GetStart(as3D);
				Pnt p0e = segmentProxy.GetEnd(as3D);
				Pnt l0 = p0e - p0s;
				offset = null;
				onRightSide = null;
				return SegmentUtils.GetAlongFraction(nearPoint - p0s, l0);
			}

			const bool forceCreation = true;
			IPolyline segmentLine = segmentProxy.GetPolyline(forceCreation);

			IPoint near = new PointClass();
			near.PutCoords(nearPoint.X, nearPoint.Y);

			IPoint onPoint = new PointClass();

			double fraction = 0;
			double offsetValue = 0;
			var rightSide = false;
			const bool asRatio = true;
			segmentLine.QueryPointAndDistance(esriSegmentExtension.esriNoExtension, near,
			                                  asRatio, onPoint, ref fraction,
			                                  ref offsetValue, ref rightSide);

			offset = offsetValue;
			onRightSide = rightSide;
			return fraction;
		}


		private static bool CutCurveCircle([NotNull] SegmentProxy segmentProxy,
		                                   [NotNull] IPnt circleCenter,
		                                   double r2,
		                                   [NotNull] out IList<double[]> limits)
		{
			double radius = Math.Sqrt(r2);
			IPoint center = new Point();
			center.PutCoords(circleCenter.X, circleCenter.Y);

			ICircularArc arc = new CircularArcClass();
			arc.PutCoordsByAngle(center, 0, 2 * Math.PI, radius);

			object emptyRef = Type.Missing;

			IPolygon circle = new PolygonClass();
			((ISegmentCollection) circle).AddSegment((ISegment) arc, ref emptyRef,
			                                         ref emptyRef);

			limits = GetLimits(segmentProxy, circle);
			return limits.Count > 0;
		}

		[NotNull]
		internal static IList<double[]> GetLimits([NotNull] SegmentProxy segmentProxy,
		                                          [NotNull] IPolygon buffer)
		{
			// TODO this method would be extremely expensive when called on WksSegmentProxy instances

			var result = new List<double[]>();
			// Remark: segmentLine is altered by ITopologicalOperator.Intersect in  such a way
			// that equal segments may not be considered as equal anymore 
			IPolyline segmentLine = segmentProxy.GetPolyline(true);
			//IPolyline segmentLine = segmentProxy.GetPolyline();
			var intersects = (IGeometryCollection)
				((ITopologicalOperator) buffer).Intersect(
					segmentLine,
					esriGeometryDimension.esriGeometry1Dimension);

			int intersectCount = intersects.GeometryCount;

			for (var i = 0; i < intersectCount; i++)
			{
				var part = (IPath) intersects.Geometry[i];

				double t0 = 0;
				double t1 = 0;
				double offset = 0;
				var rightSide = false;

				// TODO if called frequently, create abstract GetSegmentDistance(IPoint) on SegmentProxy, 
				// with custom implementation on WksSegmentProxy.
				// Currently this seems to be called for AoSegmentProxys only, but this is not obvious.

				// TODO use a template point and part.QueryFromPoint() / part.QueryToPoint()?
				segmentLine.QueryPointAndDistance(esriSegmentExtension.esriExtendTangents,
				                                  part.FromPoint, true, HelpPoint,
				                                  ref t0, ref offset, ref rightSide);

				segmentLine.QueryPointAndDistance(esriSegmentExtension.esriExtendTangents,
				                                  part.ToPoint, true, HelpPoint,
				                                  ref t1, ref offset, ref rightSide);

				double tMin = Math.Min(t0, t1);
				double tMax = Math.Max(t0, t1);

				result.Add(new[] {tMin, tMax});
			}

			// Handle spatial tolerance problems for segments near tolerance size!
			if (intersectCount == 0 && ! ((IRelationalOperator) buffer).Disjoint(segmentLine))
			{
				((ITopologicalOperator) segmentLine).Simplify();
				if (segmentLine.IsEmpty)
				{
					result.Add(new[] {0.0, 1.0});
				}
			}

			return result;
		}

		internal static bool CutLineCircle([NotNull] Pnt p0,
		                                   [NotNull] Pnt l0,
		                                   [NotNull] Pnt center,
		                                   double r2,
		                                   out double tMin,
		                                   out double tMax)
		{
			Pnt p = p0 - center;

			double a = l0 * l0; // a = square length of l0
			double b = 2 * (p * l0);
			double c = p * p - r2;

			if (a > 0)
			{
				if (SegmentUtils.SolveSqr(a, b, c, out tMin, out tMax))
				{
					if (tMin > tMax)
					{
						throw new InvalidProgramException(
							string.Format(
								"error in software design assumption: {0} <= {1} is false!",
								tMin, tMax));
					}

					return true;
				}

				return false;
			}

			if (a == 0)
			{
				if (c > 0)
				{
					tMin = double.MaxValue;
					tMax = double.MaxValue;

					return false;
				}

				tMin = double.MinValue;
				tMax = double.MaxValue;

				return true;
			}

			// (a < 0)
			throw new InvalidOperationException(
				string.Format("sqare length of line = {0}", a));
		}

		private static bool CutLineCircle3D([NotNull] SegmentProxy segmentProxy,
		                                    [NotNull] Pnt center, double r2,
		                                    out double tMin, out double tMax)
		{
			const bool as3D = true;

			var p0s = (Pnt3D) segmentProxy.GetStart(as3D);
			var p0e = (Pnt3D) segmentProxy.GetEnd(as3D);
			Pnt l0 = p0e - p0s;

			return CutLineCircle(p0s, l0, center, r2, out tMin, out tMax);
		}

		private static bool CutLineCircle2D([NotNull] SegmentProxy segmentProxy,
		                                    [NotNull] Pnt center, double r2,
		                                    out double tMin, out double tMax)
		{
			const bool as3D = false;

			var p0s = (Pnt2D) segmentProxy.GetStart(as3D);
			var p0e = (Pnt2D) segmentProxy.GetEnd(as3D);
			Pnt l0 = p0e - p0s;

			return CutLineCircle(p0s, l0, center, r2, out tMin, out tMax);
		}

		[NotNull]
		public static IPolygon CreatePolygon([NotNull] IEnumerable<SegmentProxy> segments)
		{
			IGeometryCollection polygon = new PolygonClass();
			CreateGeometry(polygon, segments);
			return (IPolygon) polygon;
		}

		[NotNull]
		public static IMultiPatch CreateMultiPatch(
			[NotNull] IEnumerable<SegmentProxy> segments)
		{
			IGeometryCollection multiPatch = new MultiPatchClass();
			CreateGeometry(multiPatch, segments);
			return (IMultiPatch) multiPatch;
		}

		public static void CreateGeometry([NotNull] IGeometryCollection geometryCollection,
		                                  [NotNull] IEnumerable<SegmentProxy> segments)
		{
			int lastPartIndex = -1;
			ISpatialReference sr = null;

			var ringPoints = new List<WKSPointZ>();
			foreach (SegmentProxy segment in segments)
			{
				if (sr == null)
				{
					sr = segment.SpatialReference;
					((IGeometry) geometryCollection).SpatialReference = sr;
				}

				if (lastPartIndex != segment.PartIndex)
				{
					AddRing(geometryCollection, ringPoints);
					ringPoints.Clear();
					ringPoints.Add(QaGeometryUtils.GetWksPoint(segment.GetStart(true)));
				}

				lastPartIndex = segment.PartIndex;
				ringPoints.Add(QaGeometryUtils.GetWksPoint(segment.GetEnd(true)));
			}

			AddRing(geometryCollection, ringPoints);
		}

		private static void AddRing(IGeometryCollection multiPatch,
		                            List<WKSPointZ> ringPoints)
		{
			if (ringPoints.Count <= 0)
			{
				return;
			}

			object missing = Type.Missing;
			IPointCollection4 ring = new RingClass();
			WKSPointZ[] points = ringPoints.ToArray();
			GeometryUtils.SetWKSPointZs(ring, points);

			multiPatch.AddGeometry((IRing) ring, ref missing, ref missing);
		}
	}
}
