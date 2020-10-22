using System;
using System.Collections.Generic;
using C5;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.LinearNetwork.ShortestPath
{
	/// <summary>
	/// Provides single-source shortest path calculation using Edsger Dijkstra's algorithm.
	/// </summary>
	/// <typeparam name="T">The node type which is not directly used for the solution.</typeparam>
	public class Dijkstra<T>
	{
		/// <summary>
		/// The previous node's index of the node at the specified index.
		/// </summary>
		private int[] _previous;

		/// <summary>
		/// The distance of the node at the specified index to the previous node.
		/// </summary>
		private double[] _distance;

		[NotNull] private readonly System.Collections.Generic.IList<T> _nodes;

		[NotNull] private readonly Func<int, List<AdjacentNode>> _getAdjacentNodes;

		private IPriorityQueueHandle<int>[] _queueItemHandles;

		/// <summary>
		/// Create an instance of the <see cref="Dijkstra&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="nodes">The list of nodes</param>
		/// <param name="adjacentNodesByNodeIndex">The list of connections / edges. The
		/// index of the outer list must correspond to the nodes list, i.e. the adjacent nodes
		/// of node nodes[17] is found at adjacentNodesByNodeIndex[17]</param>
		public Dijkstra([NotNull] System.Collections.Generic.IList<T> nodes,
		                [NotNull] List<List<AdjacentNode>> adjacentNodesByNodeIndex)
			: this(nodes, nodeIndex => adjacentNodesByNodeIndex[nodeIndex]) { }

		/// <summary>
		/// Create an instance of the <see cref="Dijkstra&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="nodes">The list of nodes</param>
		/// <param name="getAdjacentNodes">The function that provides the <see cref="AdjacentNode"/> list 
		/// for a node at a specified index.</param>
		public Dijkstra([NotNull] System.Collections.Generic.IList<T> nodes,
		                [NotNull] Func<int, List<AdjacentNode>> getAdjacentNodes)
		{
			_nodes = nodes;
			_getAdjacentNodes = getAdjacentNodes;
		}

		/// <summary>
		/// The edges (edge index of AdjacentNode) that should not be traversed for path solving. 
		/// </summary>
		public System.Collections.Generic.HashSet<int> BlockedEdges { get; set; }

		/// <summary>
		/// Returns the connections / edges of the shortest path between the source node index
		/// and the target node index.
		/// </summary>
		/// <param name="sourceNodeIndex">The start node index</param>
		/// <param name="targetNodeIndex">The end node index</param>
		/// <param name="distance">The total distance of the resulting path, or NaN if the target 
		/// was not reached.</param>
		/// <returns>The connections that build the shortest path.</returns>
		[NotNull]
		public System.Collections.Generic.IList<AdjacentNode> GetShortestPath(
			int sourceNodeIndex, int targetNodeIndex, out double distance)
		{
			InitializeArrays(_nodes, sourceNodeIndex);

			bool destinationReached = SolveShortestPath(targetNodeIndex);

			if (! destinationReached)
			{
				distance = double.NaN;
				return new List<AdjacentNode>(0);
			}

			return SelectEdgesInSolution(targetNodeIndex, out distance);
		}

		/// <summary>
		/// Returns the connections / edges of the shortest path between the source node index
		/// and the target node index. Additionally the path must pass through the intermediate
		/// node indexes in the provided order.
		/// </summary>
		/// <param name="sourceNodeIndex">The start node index</param>
		/// <param name="targetNodeIndex">The end node index</param>
		/// <param name="intermediateIndexes">The ordered intermediate indexes</param>
		/// <param name="distance">The total distance of the resulting path, or NaN if the target 
		/// or some intermediate index was not reached.</param>
		/// <returns>The connections that build the shortest path.</returns>
		[NotNull]
		public System.Collections.Generic.IList<AdjacentNode> GetShortestPath(
			int sourceNodeIndex, int targetNodeIndex,
			[NotNull] IEnumerable<int> intermediateIndexes,
			out double distance)
		{
			distance = 0;

			InitializeArrays(_nodes, sourceNodeIndex);

			var result = new List<AdjacentNode>();

			if (BlockedEdges == null)
			{
				BlockedEdges = new System.Collections.Generic.HashSet<int>();
			}

			double legDistance;
			System.Collections.Generic.IList<AdjacentNode> intermediateResult;
			foreach (int intermediateIndex in intermediateIndexes)
			{
				intermediateResult =
					GetShortestPath(sourceNodeIndex, intermediateIndex, out legDistance);

				result.AddRange(intermediateResult);

				// if intermediate leg cannot be solved, distance becomes NaN.
				distance += legDistance;

				// Avoid backtracking on the same edges used by previous sections:
				foreach (AdjacentNode adjacentNode in intermediateResult)
				{
					BlockedEdges.Add(adjacentNode.EdgeIndex);
				}

				sourceNodeIndex = intermediateIndex;
			}

			intermediateResult = GetShortestPath(sourceNodeIndex, targetNodeIndex,
			                                     out legDistance);

			result.AddRange(intermediateResult);
			distance += legDistance;

			return result;
		}

		private void InitializeArrays(
			[NotNull] System.Collections.Generic.ICollection<T> nodes,
			int sourceIndex)
		{
			Assert.ArgumentNotNull(nodes, nameof(nodes));
			Assert.ArgumentCondition(sourceIndex >= 0 && sourceIndex < nodes.Count,
			                         "sourceIndex is out of range.");

			// re-use existing arrays for repeated calculations in the same graph
			if (_previous == null || nodes.Count != _previous.Length)
			{
				_previous = new int[nodes.Count];
				_distance = new double[nodes.Count];
			}

			for (var i = 0; i < _previous.Length; i++)
			{
				_previous[i] = -1;

				if (i == sourceIndex)
				{
					_distance[i] = 0;
				}
				else
				{
					_distance[i] = double.MaxValue;
				}
			}
		}

		/// <summary>
		/// Actual dijkstra core algorithm. To calculate the entire shortest path tree from the source
		/// to all other nodes, specify a targetNodeIndex that does not exist (e.g. -1).
		/// </summary>
		/// <param name="targetNodeIndex"></param>
		/// <returns>Whether the targetNodeIndex was reached or not.</returns>
		private bool SolveShortestPath(int targetNodeIndex)
		{
			IPriorityQueue<int> priorityQueue = InitializePriorityQueue();

			while (! priorityQueue.IsEmpty)
			{
				int u = priorityQueue.DeleteMin();

				if (u == targetNodeIndex)
				{
					return _previous[targetNodeIndex] >= 0;
				}

				foreach (AdjacentNode adjacentNode in GetAdjacentNodes(u))
				{
					double w = adjacentNode.Weight;

					int v = adjacentNode.AdjacentNodeIndex;

					if (v < 0)
					{
						// A negative node index means it is a node outside the area of interest
						// and therefore a dead-end. Consider a hook here to dynamically load more
						// data.
						continue;
					}

					double alternative = _distance[u] + w;

					if (alternative < _distance[v])
					{
						_distance[v] = alternative;
						_previous[v] = u;

						UpdatePriority(priorityQueue, v);
					}
				}
			}

			return false;
		}

		[NotNull]
		private List<AdjacentNode> SelectEdgesInSolution(
			int endNodeIndex, out double totalWeight)
		{
			var result = new List<AdjacentNode>();
			totalWeight = _distance[endNodeIndex];

			if (endNodeIndex < 0)
			{
				return result;
			}

			int current = endNodeIndex;
			int previous = _previous[current];
			while (previous >= 0)
			{
				IEnumerable<AdjacentNode> adjacent = GetAdjacentNodes(previous);

				result.Add(GetShortestEdge(adjacent, current));

				current = previous;
				previous = _previous[current];
			}

			result.Reverse();

			return result;
		}

		[CanBeNull]
		private static AdjacentNode GetShortestEdge(
			[NotNull] IEnumerable<AdjacentNode> edges,
			int endNodeIndex)
		{
			double shortestLength = double.MaxValue;

			AdjacentNode result = null;

			foreach (AdjacentNode edge in edges)
			{
				if (edge.AdjacentNodeIndex != endNodeIndex)
				{
					continue;
				}

				if (edge.Weight < shortestLength)
				{
					shortestLength = edge.Weight;
					result = edge;
				}
			}

			return result;
		}

		[NotNull]
		private IPriorityQueue<int> InitializePriorityQueue()
		{
			IComparer<int> weightComparer = ComparerFactory<int>.CreateComparer(
				(a, b) => _distance[a].CompareTo(_distance[b]));

			IPriorityQueue<int> priorityQueue =
				new IntervalHeap<int>(weightComparer);

			_queueItemHandles = new IPriorityQueueHandle<int>[_nodes.Count];
			for (var i = 0; i < _nodes.Count; i++)
			{
				IPriorityQueueHandle<int> itemHandle = null;
				priorityQueue.Add(ref itemHandle, i);

				_queueItemHandles[i] = itemHandle;
			}

			return priorityQueue;
		}

		private void UpdatePriority([NotNull] IPriorityQueue<int> priorityQueue,
		                            int nodeIndex)
		{
			// This is necessary to ensure correct internal ordering after updating a distance
			IPriorityQueueHandle<int> handle = _queueItemHandles[nodeIndex];

			priorityQueue.Replace(handle, nodeIndex);
		}

		private IEnumerable<AdjacentNode> GetAdjacentNodes(int nodeIndex)
		{
			foreach (AdjacentNode adjacentNode in _getAdjacentNodes(nodeIndex))
			{
				if (BlockedEdges == null || ! BlockedEdges.Contains(adjacentNode.EdgeIndex))
				{
					yield return adjacentNode;
				}
			}
		}
	}
}
