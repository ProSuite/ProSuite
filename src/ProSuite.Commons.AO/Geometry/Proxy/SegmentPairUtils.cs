using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using System;
using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geometry.Proxy
{
	public static class SegmentProxyUtils
	{
		[ThreadStatic] private static IPoint _helpPoint;

		internal static IPoint HelpPoint => _helpPoint ?? (_helpPoint = new PointClass());

		public static IList<double[]> GetLimits(
			[NotNull] SegmentProxy segmentProxy,
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

				result.Add(new[] { tMin, tMax });
			}

			// Handle spatial tolerance problems for segments near tolerance size!
			if (intersectCount == 0 && ! ((IRelationalOperator) buffer).Disjoint(segmentLine))
			{
				((ITopologicalOperator) segmentLine).Simplify();
				if (segmentLine.IsEmpty)
				{
					result.Add(new[] { 0.0, 1.0 });
				}
			}

			return result;
		}
	}
}
