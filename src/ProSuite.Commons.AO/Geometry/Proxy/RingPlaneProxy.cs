using ESRI.ArcGIS.esriSystem;
using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geometry.Proxy
{
	internal class RingPlaneProxy : PlaneProxy
	{
		public RingPlaneProxy(int partIndex, IWKSPointCollection points)
			: base(partIndex, points) { }

		protected override IEnumerable<SegmentProxy> GetSegmentsCore()
		{
			int pointCount = Points.Points.Count;
			for (int i = 1; i < pointCount; i++)
			{
				var segment = new WksSegmentProxy(Points, PartIndex, i - 1);
				yield return segment;
			}
		}

		protected override int GetSegmentCountCore()
		{
			return Points.Points.Count - 1;
		}

		protected override SegmentProxy GetSegmentCore(int segmentIndex)
		{
			var segment = new WksSegmentProxy(Points, PartIndex, segmentIndex);
			return segment;
		}

		protected override WKSPointZ GetPlanePoint(int pointIndex)
		{
			return Points.Points[pointIndex];
		}
	}
}
