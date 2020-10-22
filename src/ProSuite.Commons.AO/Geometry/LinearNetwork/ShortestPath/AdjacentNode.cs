namespace ProSuite.Commons.AO.Geometry.LinearNetwork.ShortestPath
{
	/// <summary>
	/// Provides information on an edge that connects a node to an adjacent node.
	/// </summary>
	public class AdjacentNode
	{
		/// <summary>
		/// Create an instance of the <see cref="AdjacentNode"/> class.
		/// </summary>
		/// <param name="adjacentNodeIndex">The list index of the adjacent node in the nodes list.</param>
		/// <param name="weight">The weight / cost / distance associated with the edge</param>
		/// <param name="edgeIndex">An edge identifier (not used for path calculation).</param>
		public AdjacentNode(int adjacentNodeIndex, float weight, int edgeIndex)
		{
			AdjacentNodeIndex = adjacentNodeIndex;
			Weight = weight;
			EdgeIndex = edgeIndex;
		}

		/// <summary>
		/// The weight / cost / distance associated with the edge.
		/// </summary>
		public float Weight { get; }

		/// <summary>
		/// An edge identifier (not used for path calculation).
		/// </summary>
		public int EdgeIndex { get; }

		/// <summary>
		/// The list index of the adjacent node in the nodes list. -1 for a node that is outside
		/// the area of interest.
		/// </summary>
		public int AdjacentNodeIndex { get; private set; }
	}
}
