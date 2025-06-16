namespace ProSuite.Commons.Geom.SpatialIndex
{
	// Three typical distance metrices:
	// https://chris3606.github.io/GoRogue/articles/grid_components/measuring-distance.html
	public enum DistanceMetric
	{
		EuclideanDistance = 0,
		ChebyshevDistance = 1,
		ManhattanDistance = 2
	}
}
