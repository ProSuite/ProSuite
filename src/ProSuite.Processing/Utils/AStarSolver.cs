using System;
using System.Collections.Generic;

using ProSuite.Commons.Collections;

namespace ProSuite.Processing.Utils;

public class AStarSolver<TNode, TEdge> where TNode : IEquatable<TNode>
{
	/// <summary>
	/// The graph used by the A* pathfinder.
	/// Designed to be easy to implement while providing what A* needs.
	/// </summary>
	public interface IGraph //<TNode, TEdge> where TNode : IEquatable<TNode>
	{
		/// <summary>
		/// Get all edges that are incident to the given node.
		/// </summary>
		IEnumerable<TEdge> GetIncidentEdges(TNode node);

		/// <summary>
		/// Expect the given edge to be incident on the given node
		/// and return the node at the other end of the edge, that
		/// is, the adjacent node.
		/// </summary>
		TNode GetOtherNode(TNode node, TEdge edge);

		/// <returns>True iff <paramref name="edge"/> is incident on <paramref name="node"/></returns>
		bool IsIncident(TEdge edge, TNode node);

		/// <returns>The cost of travelling along the given edge.
		/// Must not be negative or search algorithms may fail.</returns>
		double GetEdgeCost(TEdge edge);

		/// <summary>
		/// Get an estimate for the cost of the cheapest route from
		/// <paramref name="fromNode"/> to <paramref name="toNode"/>.
		/// Can be zero, but not negative. Must not overestimate!
		/// </summary>
		/// <remarks>This is the heuristic that speeds up A* search.
		/// Straight-line distance is an excellent such heuristic.</remarks>
		double GetCostEstimate(TNode fromNode, TNode toNode);
	}

	private readonly IGraph _graph;

	public AStarSolver(IGraph graph)
	{
		_graph = graph ?? throw new ArgumentNullException(nameof(graph));
	}

	/// <summary>
	/// Find the shortest path from given start node to given target node.
	/// Roughly following the pseudocode in the Wikipedia article about the
	/// A* algorithm: https://de.wikipedia.org/wiki/A*-Algorithmus
	/// </summary>
	/// <returns>The list of edges along the shortest path from start to target</returns>
	public IReadOnlyList<TEdge> Solve(TNode startNode, TNode targetNode)
	{
		if (startNode is null)
			throw new ArgumentNullException(nameof(startNode));
		if (targetNode is null)
			throw new ArgumentNullException(nameof(targetNode));

		var openList = new OpenQueue(_graph, targetNode);
		var closedList = new HashSet<TNode>();

		openList.Add(startNode);

		while (openList.Count > 0)
		{
			// node with least F value (heuristically cheapest)
			var currentNode = openList.Pop();

			if (Equals(currentNode, targetNode))
			{
				return RetrievePath(currentNode, openList);
			}

			closedList.Add(currentNode); // avoid cycling

			ExpandNode(currentNode, openList, closedList);
		}

		return null; // no path from start to target
	}

	private object SolveAll(TNode targetNode)
	{
		// build a spanning tree rooted at the given targetNode
		// sketch: add all nodes (except target) to openlist
		// here the heuristic is of no value, essentially Dijkstra
		throw new NotImplementedException();
	}

	private void ExpandNode(TNode currentNode,
	                        OpenQueue openList, ISet<TNode> closedList)
	{
		var incidentEdges = _graph.GetIncidentEdges(currentNode);

		foreach (var edge in incidentEdges)
		{
			var successor = _graph.GetOtherNode(currentNode, edge);
			if (closedList.Contains(successor))
				continue; // already fully treated

			var tentativeG = openList.G(currentNode) + _graph.GetEdgeCost(edge);
			if (openList.Contains(successor) && tentativeG >= openList.G(successor))
				continue; // new path is not cheaper

			openList.SetLink(successor, edge);
			openList.SetG(successor, tentativeG);

			// If successor is already on open list, reorder because
			// changing G changes priority (G increased, priority decreases);
			// otherwise, add successor to open list.
			// Our PQ only offers Remove() (slow) and Add() (ok):
			openList.Remove(successor); // TODO O(N) !! (combine a hash with pq, hash points into heap)
			openList.Add(successor); // O(log N)
		}
	}

	/// <summary>
	/// Given the target node and the open list (which in our implementation
	/// also collects predecessor links) after A* has completed, build the
	/// list of edges that make up the shortest path.
	/// </summary>
	private IReadOnlyList<TEdge> RetrievePath(TNode targetNode, OpenQueue openList)
	{
		var path = new List<TEdge>();

		var node = targetNode;
		var edge = openList.Link(node);
		while (edge is not null)
		{
			path.Add(edge);

			node = _graph.GetOtherNode(node, edge);
			edge = openList.Link(node);
		}

		path.Reverse();
		return path;
	}

	private class OpenQueue : PriorityQueue<TNode>
	{
		// Required OpenList operations:
		// - Count: int
		// - Pop(): Node // remove least estimate node
		// - Contains(Node): bool // presently O(N) --> want a parallel hash (also for next operation)
		// - PriorityChanged(Node) // reorder internally; presently: Remove(node) and Add(node), which is O(N)!

		private readonly IGraph _graph;
		private readonly Dictionary<TNode, double> _g = new();
		private readonly Dictionary<TNode, TEdge> _link = new();

		public OpenQueue(IGraph graph, TNode targetNode)
		{
			_graph = graph ?? throw new ArgumentNullException(nameof(graph));
			Target = targetNode ?? throw new ArgumentNullException(nameof(targetNode));
		}

		private TNode Target { get; }

		public double G(TNode node)
		{
			if (node is null) return 0.0;
			return _g.TryGetValue(node, out var value) ? value : 0.0;
		}

		/// <remarks>Changing G changes the node's priority: Remove and re-Add!</remarks>
		public void SetG(TNode node, double value)
		{
			if (node is null)
				throw new ArgumentNullException(nameof(node));
			if (!(value >= 0) || double.IsNaN(value) || double.IsInfinity(value))
				throw new ArgumentOutOfRangeException(nameof(value), "must non-negative and finite");
			_g[node] = value;
		}

		public TEdge Link(TNode node)
		{
			if (node is null) return default;
			return _link.TryGetValue(node, out var edge) ? edge : default;
		}

		public void SetLink(TNode node, TEdge incidentEdge)
		{
			if (node is null)
				throw new ArgumentNullException(nameof(node));
			if (incidentEdge is null)
				throw new ArgumentNullException(nameof(incidentEdge));
			if (!_graph.IsIncident(incidentEdge, node))
				throw new ArgumentException("Node must be start or end of edge");

			_link[node] = incidentEdge;
		}

		protected override bool Priority(TNode a, TNode b)
		{
			double aF = F(a);
			double bF = F(b);
			// smaller estimate = higher priority
			return aF < bF;
		}

		private double F(TNode node)
		{
			var g = G(node); // known cost so far
			var h = H(node); // optimistic estimate (typically straight-line distance)
			return g + h;
		}

		/// <summary>
		/// The heuristic: optimistic estimate for remaining
		/// cost to target. Must NOT overestimate the cost!
		/// </summary>
		private double H(TNode node)
		{
			return _graph.GetCostEstimate(node, Target);
		}
	}
}
