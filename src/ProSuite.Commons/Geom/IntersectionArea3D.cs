using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	// TODO: Rename all Intersection<GeometryType>3D classes to Intersection<GeometryType>
	public class IntersectionArea3D
	{
		public MultiLinestring IntersectionArea { get; set; }

		public MultiLinestring SourceRings { get; set; }
		public MultiLinestring Target { get; set; }

		public IntersectionArea3D([NotNull] MultiLinestring intersectionArea,
		                          [NotNull] MultiLinestring sourceRings,
		                          [NotNull] MultiLinestring target)
		{
			IntersectionArea = intersectionArea;
			SourceRings = sourceRings;
			Target = target;
		}
	}
}
