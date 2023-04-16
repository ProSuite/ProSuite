
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geometry.Proxy
{
	public abstract class PlaneProxy
	{
		protected PlaneProxy(int partIndex,
		                     [NotNull] IWKSPointCollection points)
		{
			PartIndex = partIndex;
			Points = points;
		}

		public bool IsClosed => true;

		[NotNull]
		protected IWKSPointCollection Points { get; }

		protected int PartIndex { get; }

		[NotNull]
		public IEnumerable<SegmentProxy> GetSegments()
		{
			return GetSegmentsCore();
		}

		[NotNull]
		protected abstract IEnumerable<SegmentProxy> GetSegmentsCore();

		public int GetSegmentCount()
		{
			return GetSegmentCountCore();
		}

		protected abstract int GetSegmentCountCore();

		[NotNull]
		public SegmentProxy GetSegment(int segmentIndex)
		{
			return GetSegmentCore(segmentIndex);
		}

		[NotNull]
		protected abstract SegmentProxy GetSegmentCore(int segmentIndex);

		protected abstract WKSPointZ GetPlanePoint(int pointIndex);

		public IPolyline GetSubpart(int startSegmentIndex, double startFraction,
		                            int endSegmentIndex, double endFraction)
		{
			IPointCollection4 subpart = new PolylineClass();
			((IZAware)subpart).ZAware = true;

			var add = 2;
			if (endFraction == 0)
			{
				add = 1;
			}

			int pointCount = endSegmentIndex - startSegmentIndex + add;
			var points = new WKSPointZ[pointCount];

			SegmentProxy seg0 = GetSegment(startSegmentIndex);
			IPnt p = seg0.GetPointAt(startFraction, true);

			points[0] = ProxyUtils.GetWksPoint(p);
			for (int i = startSegmentIndex + 1; i <= endSegmentIndex; i++)
			{
				points[i - startSegmentIndex] = GetPlanePoint(i);
			}

			if (endFraction > 0)
			{
				SegmentProxy seg1 = GetSegment(endSegmentIndex);
				IPnt end = seg1.GetPointAt(endFraction, true);
				points[pointCount - 1] = ProxyUtils.GetWksPoint(end);
			}

			GeometryUtils.SetWKSPointZs(subpart, points);
			return (IPolyline)subpart;
		}
	}
}
