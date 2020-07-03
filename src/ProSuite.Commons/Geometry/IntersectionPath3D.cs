using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geometry
{
	public class IntersectionPath3D
	{
		public Linestring Segments { get; }

		/// <summary>
		/// The location of the segments of the first ring which are at the left of the intersection
		/// line with respect to the plane of the second ring. Positive: distance to the plane is positive,
		/// e.g. for a horizontal, clockwise oriented second ring this means the first ring's segments left
		/// of the intersection line are 'above' the plane of the second ring.
		/// </summary>
		public RingPlaneTopology RingPlaneTopology { get; }

		public IntersectionPath3D(
			[NotNull] Linestring segments,
			RingPlaneTopology ringPlaneTopology)
		{
			Segments = segments;
			RingPlaneTopology = ringPlaneTopology;
		}

		public override string ToString()
		{
			return $"RingPlaneTopology: {RingPlaneTopology}, Linestring: {Segments}";
		}
	}

	public enum RingPlaneTopology
	{
		LeftNegative = -1,
		InPlane = 0,
		LeftPositive = 1
	}
}