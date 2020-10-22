using System;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geometry.Cracking
{
	[CLSCompliant(false)]
	public class CrackPoint
	{
		public IPoint Point { get; set; }

		public CrackPoint(IPoint point)
		{
			Point = point;
		}

		public CrackPoint(IPoint point, bool violatesMinimumSegmentLength) : this(point)
		{
			ViolatesMinimumSegmentLength = violatesMinimumSegmentLength;
		}

		public bool ViolatesMinimumSegmentLength { get; set; }

		public bool TargetVertexOnlyDifferentInZ { get; set; }

		public bool TargetVertexDifferentWithinTolerance { get; set; }

		// TODO: use it or get rid of it:
		public int? PlanarPointLocationIndex { get; set; }
	}
}
