using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;

namespace ProSuite.QA.Container.Geometry
{
	internal class TriPlaneProxy : PlaneProxy
	{
		private readonly int _index0;
		private readonly int _index1;
		private readonly int _index2;

		public TriPlaneProxy(int partIndex, IWKSPointCollection points, int index0,
		                     int index1, int index2)
			: base(partIndex, points)
		{
			_index0 = index0;
			_index1 = index1;
			_index2 = index2;
		}

		protected override IEnumerable<SegmentProxy> GetSegmentsCore()
		{
			yield return new WksFromToSegmentProxy(Points, PartIndex, 0, _index0, _index1);
			yield return new WksFromToSegmentProxy(Points, PartIndex, 1, _index1, _index2);
			yield return new WksFromToSegmentProxy(Points, PartIndex, 2, _index2, _index0);
		}

		protected override int GetSegmentCountCore()
		{
			return 3;
		}

		protected override SegmentProxy GetSegmentCore(int segmentIndex)
		{
			if (segmentIndex == 0)
			{
				return new WksFromToSegmentProxy(Points, PartIndex, 0, _index0, _index1);
			}

			if (segmentIndex == 1)
			{
				return new WksFromToSegmentProxy(Points, PartIndex, 1, _index1, _index2);
			}

			if (segmentIndex == 2)
			{
				return new WksFromToSegmentProxy(Points, PartIndex, 2, _index2, _index0);
			}

			throw new ArgumentOutOfRangeException("segmentIndex");
		}

		protected override WKSPointZ GetPlanePoint(int pointIndex)
		{
			if (pointIndex == 0)
			{
				return Points.Points[_index0];
			}

			if (pointIndex == 1)
			{
				return Points.Points[_index1];
			}

			if (pointIndex == 2)
			{
				return Points.Points[_index2];
			}

			if (pointIndex == 3)
			{
				return Points.Points[_index0];
			}

			throw new ArgumentOutOfRangeException("pointIndex");
		}
	}
}
