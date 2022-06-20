using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	internal class LinearIntersectionPseudoBreak
	{
		[NotNull]
		public IntersectionPoint3D PreviousEnd { get; }

		[NotNull]
		public IntersectionPoint3D Restart { get; }

		public bool IsSourceBoundaryLoop { get; set; }
		public bool IsTargetBoundaryLoop { get; set; }

		public bool IsBoundaryLoop => IsSourceBoundaryLoop || IsTargetBoundaryLoop;

		public LinearIntersectionPseudoBreak(
			[NotNull] IntersectionPoint3D previousEnd,
			[NotNull] IntersectionPoint3D restart)
		{
			PreviousEnd = previousEnd;
			Restart = restart;
		}
	}
}
