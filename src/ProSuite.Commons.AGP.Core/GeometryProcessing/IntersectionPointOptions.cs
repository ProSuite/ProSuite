namespace ProSuite.Commons.AGP.Core.GeometryProcessing
{
	public enum IntersectionPointOptions
	{
		/// <summary>
		/// Only crossing points
		/// </summary>
		DisregardLinearIntersections = 0,

		/// <summary>
		/// Crossing points and start / end point of linear intersections
		/// </summary>
		IncludeLinearIntersectionEndpoints = 1,

		/// <summary>
		/// Crossing points and all common vertices along linear intersections
		/// </summary>
		IncludeLinearIntersectionAllPoints = 2
	}
}
