using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using System.Collections.Generic;
using ArcGIS.Core.Geometry;


namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker {
	public class CrackPoint {

		public MapPoint Point { get; }

		public CrackPoint([NotNull] MapPoint point) {
			Point = point;

		}

		public CrackPoint(MapPoint point, bool violatesMinimumSegmentLength) : this(point) {
			ViolatesMinimumSegmentLength = violatesMinimumSegmentLength;
		}

		public bool ViolatesMinimumSegmentLength { get; set; }

		public bool TargetVertexOnlyDifferentInZ { get; set; }

		public bool TargetVertexDifferentWithinTolerance { get; set; }

		// TODO: use it or get rid of it:
		public int? PlanarPointLocationIndex { get; set; }

		[CanBeNull]
		public List<IntersectionPoint3D> Intersections { get; set; }

		}
	}



