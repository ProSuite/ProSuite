using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public class CrackPoint
	{
		public CrackPoint([NotNull] IntersectionPoint3D intersectionPoint,
		                  Pnt3D targetPoint)
		{
			IntersectionPoint = intersectionPoint;
			TargetPoint = targetPoint;
		}

		public IntersectionPoint3D IntersectionPoint { get; }

		public Pnt3D TargetPoint { get; set; }

		public int? SnapVertexIndex { get; set; }

		public double? SegmentSplitFactor { get; set; }
	}
}