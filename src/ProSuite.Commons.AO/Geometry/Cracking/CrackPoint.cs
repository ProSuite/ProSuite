using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AO.Geometry.Cracking
{
	public class CrackPoint
	{
		public IPoint Point { get; }
		public Pnt3D Point3d { get; }

		public CrackPoint([NotNull] IPoint point)
		{
			Point = point;
			Point3d = new Pnt3D(point.X, point.Y, point.Z);
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

		[CanBeNull]
		public List<IntersectionPoint3D> Intersections { get; set; }

		public override string ToString()
		{
			string violatingMsg =
				ViolatesMinimumSegmentLength ? " (violates constraints)" : string.Empty;

			return $"{Point3d} ({Intersections?.Count ?? 0} intersections){violatingMsg}";
		}
	}
}
