namespace ProSuite.Commons.AO.Geometry
{
	public enum GeometryNonSimpleReason
	{
		Unknown,
		ShortSegments,
		SelfIntersections,
		DuplicatePoints,
		IdenticalRings,
		UnclosedRing,
		EmptyPart,
		IncorrectRingOrientation,
		IncorrectSegmentOrientation
	}
}
