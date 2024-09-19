using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Test
{
	[TestFixture]
	public class AStarSolverTest
	{
		[Test]
		public void CanGraph()
		{
			var graph = MakeTestGraph();

			var start = graph.FindNode("Saarbrücken");
			Assert.NotNull(start);
			Assert.Null(graph.FindNode("NoNodeWithThisPayload"));

			var target = graph.FindNode("Würzburg");
			Assert.NotNull(target);

			Assert.AreEqual(222.0, graph.GetCostEstimate(start, target), 10.0);

			var edges = graph.GetIncidentEdges(start).OrderBy(e => e.Cost).ToList();
			Assert.AreEqual(2, edges.Count);
			Assert.AreEqual(70.0, edges[0].Cost);
			Assert.AreEqual(145.0, edges[1].Cost);
			Assert.AreEqual("Kaiserslautern", edges[0].GetOtherNode(start).Name);
			Assert.AreEqual("Karlsruhe", edges[1].GetOtherNode(start).Name);
		}

		[Test]
		public void CanAStarSolve()
		{
			var graph = MakeTestGraph();

			var start = graph.FindNode("Saarbrücken");
			Assert.NotNull(start);

			var target = graph.FindNode("Würzburg");
			Assert.NotNull(target);

			var solver = new AStarSolver<Node, Edge>(graph);
			var result = solver.Solve(start, target);
			Assert.NotNull(result);

			Assert.AreEqual(3, result.Count);
			Assert.AreEqual("Saarbrücken", result[0].FromNode.Name);
			Assert.AreEqual("Kaiserslautern", result[0].ToNode.Name);
			Assert.AreEqual("Frankfurt", result[1].ToNode.Name);
			Assert.AreEqual("Würzburg", result[2].ToNode.Name);
		}

		private static Graph MakeTestGraph()
		{
			// Graph as used in the A* wikipedia article:
			// https://de.wikipedia.org/wiki/A*-Algorithmus

			var graph = new Graph();

			// Coordinates: UTM zone 32U, main station of city,
			// read from https://www.koordinaten-umrechner.de/

			var ff = graph.AddNode("Frankfurt", 475870.646, 5550541.049);
			var kl = graph.AddNode("Kaiserslautern", 410746.223, 5476651.038);
			var lh = graph.AddNode("Ludwigshafen", 459006.265, 5480697.732);
			var wb = graph.AddNode("Würzburg", 567372.399, 5517048.004);
			var sb = graph.AddNode("Saarbrücken", 353757.400, 5456210.696);
			var kr = graph.AddNode("Karlsruhe", 456258.725, 5426918.016);
			var hb = graph.AddNode("Heilbronn", 515143.745, 5443387.372);

			graph.AddEdge(sb, kr, 145);
			graph.AddEdge(sb, kl, 70);
			graph.AddEdge(kr, hb, 84);
			graph.AddEdge(kl, lh, 53);
			graph.AddEdge(kl, ff, 103);
			graph.AddEdge(ff, wb, 116);
			graph.AddEdge(lh, wb, 183);
			graph.AddEdge(hb, wb, 102);

			//estimator = (node,target) =>
			//{
			//	if (node is null)
			//		throw new ArgumentNullException(nameof(node));
			//	if (target.Name != wb.Name)
			//		throw new NotImplementedException("This estimator only works with target node Würzburg");
			//	if (node.Name == ff.Name) return 96;
			//	if (node.Name == kl.Name) return 158;
			//	if (node.Name == lh.Name) return 108;
			//	if (node.Name == wb.Name) return 0;
			//	if (node.Name == sb.Name) return 222;
			//	if (node.Name == kr.Name) return 140;
			//	if (node.Name == hb.Name) return 87;
			//	return double.PositiveInfinity;
			//};

			return graph;
		}

		public class Graph : AStarSolver<Node,Edge>.IGraph/*<Node, Edge>*/
		{
			private readonly Dictionary<string, Node> _nodes = new();
			private readonly Dictionary<Edge, Edge> _edges = new();
			private readonly Dictionary<Node, IReadOnlyList<Edge>> _cache = new();

			public Node AddNode(string name, double x, double y)
			{
				if (name is null)
					throw new ArgumentNullException(nameof(name));

				if (! _nodes.TryGetValue(name, out Node node))
				{
					node = new Node(name, x, y);
					_nodes.Add(name, node);
				}

				return node;
			}

			public Edge AddEdge(Node fromNode, Node toNode, double cost)
			{
				var edge = new Edge(fromNode, toNode, cost);
				_cache.Remove(fromNode);
				_cache.Remove(toNode);
				_edges.Add(edge, edge);
				return edge;
			}

			public Node FindNode(string name)
			{
				if (name is null) return null;
				return _nodes.TryGetValue(name, out var node) ? node : null;
			}

			public IEnumerable<Edge> GetIncidentEdges(Node node)
			{
				if (!_cache.TryGetValue(node, out var list))
				{
					list = _edges.Values.Where(e => e.IsIncident(node)).ToList();
					_cache.Add(node, list);
				}

				return list;
			}

			public Node GetOtherNode(Node node, Edge edge)
			{
				return edge.GetOtherNode(node);
			}

			public bool IsIncident(Edge edge, Node node)
			{
				return edge.IsIncident(node);
			}

			public double GetEdgeCost(Edge edge)
			{
				return edge.Cost;
			}

			public double GetCostEstimate(Node fromNode, Node toNode)
			{
				var dx = toNode.X - fromNode.X;
				var dy = toNode.Y - fromNode.Y;
				return Math.Sqrt(dx * dx + dy * dy) / 1000; // km
			}
		}

		public class Node : IEquatable<Node>
		{
			public string Name { get; }
			public double X { get; }
			public double Y { get; }

			public Node(string name, double x, double y)
			{
				Name = name ?? throw new ArgumentNullException(nameof(name));
				X = x;
				Y = y;
			}

			#region Equality by Name

			public bool Equals(Node other)
			{
				if (other is null) return false;
				if (ReferenceEquals(other, this)) return true;
				return string.Equals(other.Name, Name);
			}

			public override bool Equals(object obj)
			{
				return Equals(obj as Node);
			}

			public override int GetHashCode()
			{
				return Name.GetHashCode();
			}

			#endregion

			public override string ToString()
			{
				return Name;
			}
		}

		public class Edge
		{
			public double Cost { get; }
			public Node FromNode { get; }
			public Node ToNode { get; }

			public Edge(Node from, Node to, double cost)
			{
				FromNode = from;
				ToNode = to;
				Cost = cost;
			}

			public bool IsIncident(Node node)
			{
				if (node is null) return false;
				return Equals(node, FromNode) || Equals(node, ToNode);
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
