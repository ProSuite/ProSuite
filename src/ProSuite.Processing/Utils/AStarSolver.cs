using ProSuite.Commons.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProSuite.Processing.Utils
{
	public class AStarSolver<TN, TE> where TN : IEquatable<TN> where TE : IEquatable<TE>
	{
		private readonly Graph<TN, TE> _graph;
		private readonly Graph<TN, TE>.Node _targetNode;
		private readonly Func<TN, double> _estimator; // relative to target node!
		private readonly Dictionary<Graph<TN, TE>.Node, StarNode> _starNodes;

		private AStarSolver(Graph<TN, TE> graph, Graph<TN,TE>.Node targetNode, Func<TN, double> estimator)
		{
			_graph = graph ?? throw new ArgumentNullException(nameof(graph));
			_targetNode = targetNode ?? throw new ArgumentNullException(nameof(targetNode));
			_estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
			_starNodes = new Dictionary<Graph<TN, TE>.Node, StarNode>();
		}

		/// <summary>
		/// Find the shortest route from given start node to given target node.
		/// Roughly following the pseudocode in the Wikipedia article about the
		/// A* algorithm: https://de.wikipedia.org/wiki/A*-Algorithmus
		/// </summary>
		/// <returns>The adorned targetNode (G=cost, F irrelevant, use the Link
		/// property to trace back the path) or null (if no route exists)</returns>
		public static StarNode Solve(Graph<TN,TE> graph, Graph<TN, TE>.Node startNode, Graph<TN, TE>.Node targetNode, Func<TN, double> estimator)
		{
			if (graph is null)
				throw new ArgumentNullException(nameof(graph));
			if (startNode is null)
				throw new ArgumentNullException(nameof(startNode));
			if (targetNode is null)
				throw new ArgumentNullException(nameof(targetNode));
			if (estimator is null)
				throw new ArgumentNullException(nameof(estimator));

			var solver = new AStarSolver<TN,TE>(graph, targetNode, estimator);

			return solver.Solve(startNode);

			//var openList = new OpenQueue();
			//var closedList = new HashSet<Graph<TN, TE>.Node>();
			//var starNodes = new Dictionary<Graph<TN, TE>.Node, StarNode>();

			//openList.Add(new StarNode(startNode, 0)); // estimate ignored

			//while (openList.Count > 0)
			//{
			//	var currentNode = openList.Pop();

			//	if (currentNode.Node.Equals(targetNode))
			//	{
			//		return currentNode; // found
			//	}

			//	closedList.Add(currentNode.Node); // to avoid cycling

			//	ExpandNode(currentNode, estimator, openList, closedList, starNodes);
			//}

			//return null; // no path from start to target
		}

		private StarNode Solve(Graph<TN, TE>.Node startNode)
		{
			if (startNode is null)
				throw new ArgumentNullException(nameof(startNode));

			var openList = new OpenQueue();
			var closedList = new HashSet<Graph<TN, TE>.Node>();

			openList.Add(new StarNode(startNode, 0)); // estimate ignored

			while (openList.Count > 0)
			{
				var currentNode = openList.Pop();

				if (currentNode.Node.Equals(_targetNode))
				{
					return currentNode; // found
				}

				closedList.Add(currentNode.Node); // to avoid cycling

				ExpandNode(currentNode, openList, closedList);
			}

			return null; // no path from start to target
		}

		private StarNode SolveAll()
		{
			// build a spanning tree rooted at the _targetNode
			throw new NotImplementedException();
		}

		private object SolveAll(Graph<TN, TE>.Node[] startNodes)
		{
			if (startNodes is null)
				throw new ArgumentNullException(nameof(startNodes));
			if (startNodes.Any(n => n is null))
				throw new ArgumentNullException(nameof(startNodes), "must not be null");

			var openList = new OpenQueue();
			foreach (var startNode in startNodes)
			{
				openList.Add(GetStarNode(startNode));
			}

			throw new NotImplementedException();
		}

		private void ExpandNode(StarNode currentNode,
		                        OpenQueue openList, ISet<Graph<TN, TE>.Node> closedList)
		{
			var edges = _graph.GetIncidentEdges(currentNode.Node);

			foreach (var incidentEdge in edges)
			{
				var adjacent = incidentEdge.GetOtherNode(currentNode.Node);
				if (closedList.Contains(adjacent)) continue; // already fully treated

				var successor = GetStarNode(adjacent);
				var tentativeG = currentNode.G + incidentEdge.Cost;
				if (openList.Contains(successor) && tentativeG >= successor.G) continue;

				successor.Link = currentNode;
				successor.G = tentativeG;

				openList.Remove(successor); // O(N) !!
				successor.F = tentativeG + H(successor.Node);
				openList.Add(successor); // O(log N)
			}
		}

		private StarNode GetStarNode(Graph<TN, TE>.Node node)
		{
			if (!_starNodes.TryGetValue(node, out var starNode))
			{
				starNode = new StarNode(node, H(node));
				_starNodes.Add(node, starNode);
			}

			return starNode;
		}

		private double H(Graph<TN, TE>.Node node)
		{
			return _estimator(node.Payload);
		}

		private class OpenQueue : PriorityQueue<StarNode>
		{
			// Required OpenList operations:
			// - Count: int
			// - Pop(): Node // remove least estimate node
			// - Contains(Node): bool // presently: O(N)
			// - PriorityChanged(Node) // reorder internally; presently: Remove(node) and Add(node), which is O(N)!

			protected override bool Priority(StarNode a, StarNode b)
			{
				// smaller estimate = higher priority
				return a.F < b.F;
			}
		}

		public class StarNode
		{
			public Graph<TN, TE>.Node Node { get; }
			public double G { get; set; } // known cost so far
			public double F { get; set; } // = G+H (estimate)
			public StarNode Link { get; set; }

			public StarNode(Graph<TN, TE>.Node node, double estimate)
			{
				Node = node ?? throw new ArgumentNullException(nameof(node));

				if (estimate < 0 || double.IsNaN(estimate))
					throw new ArgumentOutOfRangeException(nameof(estimate), estimate,
														  "most be non-negative");

				G = 0;
				F = estimate;
			}

			public override string ToString()
			{
				return $"{Node}, G = {G}, F = {F}";
			}
		}
	}

	public class Graph<TN, TE> where TN : IEquatable<TN> where TE : IEquatable<TE>
	{
		private readonly Dictionary<TN, Node> _nodes = new();
		private readonly Dictionary<TE, Edge> _edges = new();
		private readonly Dictionary<Node, IReadOnlyList<Edge>> _cache = new();

		public Node AddNode(TN payload)
		{
			if (payload is null)
				throw new ArgumentNullException(nameof(payload));

			if (!_nodes.TryGetValue(payload, out Node node))
			{
				node = new Node(payload);
				_nodes.Add(payload, node);
			}

			return node;
		}

		public void AddEdge(Node fromNode, Node toNode, double cost, TE payload)
		{
			if (payload is null)
				throw new ArgumentNullException(nameof(payload));

			if (_edges.ContainsKey(payload))
				throw new InvalidOperationException($"Duplicate edge: {payload}");

			var edge = new Edge(fromNode, toNode, cost, payload);
			_cache.Remove(fromNode);
			_cache.Remove(toNode);
			_edges.Add(payload, edge);
		}

		public Node FindNode(TN payload)
		{
			if (payload is null) return null;
			return _nodes.TryGetValue(payload, out Node node) ? node : null;
		}

		public Edge FindEdge(TE payload)
		{
			if (payload is null) return null;
			return _edges.TryGetValue(payload, out Edge edge) ? edge : null;
		}

		public IReadOnlyList<Edge> GetIncidentEdges(Node node)
		{
			if (!_cache.TryGetValue(node, out var list))
			{
				list = _edges.Values.Where(e => node.Equals(e.FromNode) || node.Equals(e.ToNode)).ToList();
				_cache.Add(node, list);
			}

			return list;
		}

		public Node SplitEdge(Edge edge, Func<TE, Tuple<TE, TE>> splitter)
		{
			throw new NotImplementedException();
		}

		public Edge Unsplit(Node node, Func<TE, TE, TE> merger)
		{
			// only if node degree 2
			// and merger returns a non-null payload for the merged edge
			throw new NotImplementedException();
		}

		public class Node : IEquatable<Node>
		{
			public TN Payload { get; }

			public Node(TN payload)
			{
				Payload = payload ?? throw new ArgumentNullException(nameof(payload));
			}

			public bool Equals(Node other)
			{
				if (other is null) return false;
				return Equals(other.Payload, Payload);
			}

			public override bool Equals(object obj)
			{
				return Equals(obj as Node);
			}

			public override int GetHashCode()
			{
				return Payload.GetHashCode();
			}

			public override string ToString()
			{
				return Payload.ToString();
			}
		}

		public class Edge
		{
			public Node FromNode { get; }
			public Node ToNode { get; }
			public double Cost { get; }
			public TE Payload { get; }

			public Edge(Node fromNode, Node toNode, double cost, TE payload)
			{
				FromNode = fromNode ?? throw new ArgumentNullException(nameof(fromNode));
				ToNode = toNode ?? throw new ArgumentNullException(nameof(toNode));
				if (double.IsNaN(cost) || cost < 0)
					throw new ArgumentOutOfRangeException(nameof(cost), cost, "must be non-negative");
				Cost = cost;
				Payload = payload; // can be null or anything
			}

			public Node GetOtherNode(Node node)
			{
				if (node.Equals(FromNode))
					return ToNode;
				if (node.Equals(ToNode))
					return FromNode;
				throw new InvalidOperationException("This edge is not incident to given node");
			}
		}
	}
}
