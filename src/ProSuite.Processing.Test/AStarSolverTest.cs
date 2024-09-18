using System;
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
			var graph = MakeTestGraph(out var estimator);

			var node = graph.FindNode("Saarbrücken");
			Assert.NotNull(node);
			Assert.Null(graph.FindNode("NoNodeWithThisPayload"));

			var edge = graph.FindEdge("FF-WB");
			Assert.NotNull(edge);
			Assert.Null(graph.FindEdge("NoEdgeWithThisPayload"));

			Assert.AreEqual(222.0, estimator("Saarbrücken"), 1e-6);

			var edges = graph.GetIncidentEdges(node).OrderBy(e => e.Cost).ToList();
			Assert.AreEqual(2, edges.Count);
			Assert.AreEqual(70.0, edges[0].Cost);
			Assert.AreEqual(145.0, edges[1].Cost);
			Assert.AreEqual("Kaiserslautern", edges[0].GetOtherNode(node).Payload);
			Assert.AreEqual("Karlsruhe", edges[1].GetOtherNode(node).Payload);
		}

		[Test]
		public void CanAStarSolve()
		{
			var graph = MakeTestGraph(out var estimator);

			var start = graph.FindNode("Saarbrücken");
			Assert.NotNull(start);
			var target = graph.FindNode("Würzburg");
			Assert.NotNull(target);

			var result = AStarSolver<string, string>.Solve(graph, start, target, estimator);
			Assert.NotNull(result);

			Assert.AreEqual(result.Node.Payload, "Würzburg");
			Assert.AreEqual("Frankfurt", result.Link.Node.Payload);
			Assert.AreEqual("Kaiserslautern", result.Link.Link.Node.Payload);
			Assert.AreEqual("Saarbrücken", result.Link.Link.Link.Node.Payload);
			Assert.Null(result.Link.Link.Link.Link);
		}

		//[Test]
		//public void CanAStarSolveAll()
		//{
		//	var graph = MakeTestGraph(out var estimator);
		//	var aStar = new AStarSolver<string, string>(graph, estimator);

		//	var start = graph.FindNode("Frankfurt");
		//	var end1 = graph.FindNode("Ludwigshafen");
		//	var end2 = graph.FindNode("Karlsruhe");

		//	var result = aStar.SolveAll(start, end1, end2).OrderBy(n => n.Node.Payload).ToList();
		//	Assert.NotNull(result);
		//	Assert.AreEqual(2, result.Count);
		//	Assert.AreEqual("Karlsruhe", result[0].Node.Payload);
		//	Assert.AreEqual(302, result[0].G);
		//	Assert.AreEqual("Ludwigshafen", result[1].Node.Payload);
		//	Assert.AreEqual(156, result[1].G);
		//}

		private static Graph<string, string> MakeTestGraph(out Func<string, double> estimator)
		{
			// Graph as used in the A* wikipedia article:
			// https://de.wikipedia.org/wiki/A*-Algorithmus

			var graph = new Graph<string, string>();

			var ff = graph.AddNode("Frankfurt");
			var kl = graph.AddNode("Kaiserslautern");
			var lh = graph.AddNode("Ludwigshafen");
			var wb = graph.AddNode("Würzburg");
			var sb = graph.AddNode("Saarbrücken");
			var kr = graph.AddNode("Karlsruhe");
			var hb = graph.AddNode("Heilbronn");

			graph.AddEdge(sb, kr, 145, "SB-KR");
			graph.AddEdge(sb, kl, 70, "SB-KL");
			graph.AddEdge(kr, hb, 84, "KR-HB");
			graph.AddEdge(kl, lh, 53, "KL-LH");
			graph.AddEdge(kl, ff, 103, "KL-FF");
			graph.AddEdge(ff, wb, 116, "FF-WB");
			graph.AddEdge(lh, wb, 183, "LH-WB");
			graph.AddEdge(hb, wb, 102, "HB-WB");

			estimator = node =>
			{
				if (node is null)
					throw new ArgumentNullException(nameof(node));
				if (node == ff.Payload) return 96;
				if (node == kl.Payload) return 158;
				if (node == lh.Payload) return 108;
				if (node == wb.Payload) return 0;
				if (node == sb.Payload) return 222;
				if (node == kr.Payload) return 140;
				if (node == hb.Payload) return 87;
				return double.PositiveInfinity;
			};

			return graph;
		}
	}
}
