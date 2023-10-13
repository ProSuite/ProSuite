using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.LinearNetwork.ShortestPath
{
	/// <summary>
	/// Builds the connectivity between polyline features based on geometric adjacency.
	/// Represents a planar graph between edge features (and optional node features).
	/// The features are represented by GdbObjectReferences in order to limit memory usage.
	/// </summary>
	public class PolylineGraphConnectivity
	{
		// TODO:
		// - Consider adding an interface with only the relevant methods
		// - Make the public Nodes getter an IReadonlyList (Requires .net 4.5)
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IPoint _queryPointFrom;
		private readonly IPoint _queryPointTo;

		private readonly double _originX;
		private readonly double _originY;
		private readonly double _resolution = 0.001;

		/// <summary>
		/// Initializes a new instance of the <see cref="PolylineGraphConnectivity"/> class.
		/// </summary>
		/// <param name="spatialReference"></param>
		/// <param name="areaOfInterest">An optional area of interest which is used to
		/// filter connectivities at points outside the AoI. This can be important if the
		/// node degree must be determined correctly (Example: two of three line features
		/// intersect the AoI and are connected -> the node is incorrectly determined to
		/// have degree 2 because the third line has never been added to the graph.</param>
		public PolylineGraphConnectivity(ISpatialReference spatialReference,
		                                 [CanBeNull] IGeometry areaOfInterest = null)
		{
			SpatialReference = spatialReference;
			AreaOfInterest = areaOfInterest;

			_queryPointFrom = new PointClass { SpatialReference = SpatialReference };
			_queryPointTo = new PointClass { SpatialReference = SpatialReference };

			if (spatialReference != null)
			{
				spatialReference.GetDomain(out _originX, out _, out _originY, out _);

				_resolution = SpatialReferenceUtils.GetXyResolution(spatialReference);
			}

			NodeIndexesByNode = new Dictionary<Node, int>();

			EdgeReferences = new List<GdbObjectReference>();

			// Two lists with corresponding indexes: The nodes and the list of connections for each node:
			Nodes = new List<Node>();
			Connections = new List<List<AdjacentNode>>();

			EdgeInteriorNodesByEdge = new Dictionary<GdbObjectReference, List<int>>();
		}

		private ISpatialReference SpatialReference { get; }

		/// <summary>
		/// The optional area of interest which is used to exclude connections at points outside
		/// the AoI. This can be important if the node degree must be determined correctly or all
		/// involved edges of a Node must be found.
		/// Example: two of three line features intersect the AoI and are connected -> the node is
		/// incorrectly determined to have degree 2 because the third line has never been added to the
		/// graph.
		/// </summary>
		[CanBeNull]
		public IGeometry AreaOfInterest { get; }

		/// <summary>
		/// Nodes populated from the polylines' From-/To-points (or from junction features).
		/// </summary>
		public List<Node> Nodes { get; }

		/// <summary>
		/// Connections to adjacent nodes for each node (index corresponds to Nodes list).
		/// </summary>
		public List<List<AdjacentNode>> Connections { get; }

		/// <summary>
		/// The node index in nodes. Used to determine connectivity (without using external logical network).
		/// </summary>
		public IDictionary<Node, int> NodeIndexesByNode { get; }

		/// <summary>
		/// The link back to the involved features once the shortest path was found. 
		/// The index into this list is known by AdjacentNode.
		/// </summary>
		public List<GdbObjectReference> EdgeReferences { get; }

		/// <summary>
		/// The optional references of node features with its associated Node index
		/// </summary>
		private IDictionary<GdbObjectReference, int> NodeIndexesByNodeReference { get; } =
			new Dictionary<GdbObjectReference, int>();

		private Dictionary<GdbObjectReference, List<int>> EdgeInteriorNodesByEdge { get; }

		public void Reset()
		{
			Nodes.Clear();
			NodeIndexesByNode.Clear();

			Connections.Clear();
			EdgeReferences.Clear();

			EdgeInteriorNodesByEdge.Clear();
		}

		public void BuildConnectivity(
			[NotNull] IEnumerable<IFeature> lineFeatures,
			bool respectLineOrientation = false,
			[CanBeNull] Func<IFeature, float> getWeight = null,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			Reset();

			AddConnectivity(lineFeatures, respectLineOrientation, getWeight, trackCancel);
		}

		public void AddConnectivity(
			[NotNull] IEnumerable<IFeature> lineFeatures,
			bool respectLineOrientation,
			[CanBeNull] Func<IFeature, float> getWeight = null,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			Stopwatch stopwatch =
				_msg.DebugStartTiming("Building / Enlarging connectivity graph...");

			var count = 0;
			foreach (IFeature lineFeature in lineFeatures)
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					_msg.DebugStopTiming(stopwatch,
					                     "Building connectivity graph cancelled after {0} features",
					                     count);
					return;
				}

				AddConnectivity(lineFeature, respectLineOrientation, getWeight);
				count++;
			}

			_msg.DebugStopTiming(stopwatch,
			                     "Added connectivity information for {0} features. Total node count: {1}",
			                     count, Nodes.Count);
		}

		public void AddConnectivity([NotNull] IFeature forEdgeFeature,
		                            bool respectLineOrientation,
		                            [CanBeNull] Func<IFeature, float> getWeight = null)
		{
			AddConnectivity(forEdgeFeature, respectLineOrientation, getWeight,
			                out int _, out int _);
		}

		/// <summary>
		/// Adds the connectivity between the provided feature's from/to points/nodes to the
		/// graph. The from/to nodes are also added if necessary and their respective indexes
		/// are returned as out parameters.
		/// </summary>
		/// <param name="forEdgeFeature">The line feature</param>
		/// <param name="respectLineOrientation">Whether the connection should only be added
		/// in one direction (along the line orientation).</param>
		/// <param name="getWeight">The weight func. If null (or returning NaN), the polyline
		/// length is used.</param>
		/// <param name="fromNodeIndex">The from-node index in <see cref="Nodes"/></param>
		/// <param name="toNodeIndex">The to-node index in <see cref="Nodes"/></param>
		public void AddConnectivity([NotNull] IFeature forEdgeFeature,
		                            bool respectLineOrientation,
		                            Func<IFeature, float> getWeight,
		                            out int fromNodeIndex, out int toNodeIndex)
		{
			var polyline = (IPolyline) forEdgeFeature.Shape;

			polyline.QueryFromPoint(_queryPointFrom);
			polyline.QueryToPoint(_queryPointTo);

			GdbObjectReference objRef = new GdbObjectReference(forEdgeFeature);

			float weight = -1;
			if (getWeight != null)
			{
				weight = getWeight(forEdgeFeature);
			}

			if (weight < 0)
			{
				weight = (float) polyline.Length;
			}

			fromNodeIndex = AddNode(_queryPointFrom, out List<AdjacentNode> connectedNodesAtFrom);
			toNodeIndex = AddNode(_queryPointTo, out List<AdjacentNode> connectedNodesAtTo);

			AddConnectivity(objRef, fromNodeIndex, toNodeIndex, weight,
			                respectLineOrientation, connectedNodesAtFrom, connectedNodesAtTo);

			Marshal.ReleaseComObject(polyline);
		}

		/// <summary>
		/// The <see cref="NodeIndexesByNodeReference"/> allows for a correlation of the node
		/// feature's GdbObjectReference with the Node.
		/// This can be used to include the junction features in a linear network into the graph.
		/// </summary>
		/// <param name="nodeFeature"></param>
		/// <returns>The index of the added node.</returns>
		public int AddNodeFeatureRef([NotNull] IFeature nodeFeature)
		{
			IPoint nodeLocation = (IPoint) nodeFeature.Shape;

			int nodeIndex;
			if (! TryGetNodeIndex(nodeLocation, out nodeIndex))
			{
				nodeIndex = AddNode(nodeLocation, out List<AdjacentNode> _);
			}

			NodeIndexesByNodeReference.Add(new GdbObjectReference(nodeFeature), nodeIndex);

			return nodeIndex;
		}

		public void RemoveNodesAt(IEnumerable<IPoint> points)
		{
			foreach (IPoint point in points)
			{
				RemoveNodeAt(point);
			}
		}

		public void RemoveNodeAt([NotNull] IPoint point)
		{
			Node node = CreateNode(point);

			int nodeIndex;
			if (! NodeIndexesByNode.TryGetValue(node, out nodeIndex))
			{
				return;
			}

			// instead of removing the node (and updating all the lists), make it a dead-end -> set node to null as well?
			Nodes[nodeIndex] = null;
			Connections[nodeIndex].Clear();

			// Check if it is an interior node, remove from the interior list, if it is in the list
			foreach (List<int> interiorNodes in EdgeInteriorNodesByEdge.Values)
			{
				interiorNodes.Remove(nodeIndex);
			}

			NodeIndexesByNode.Remove(node);
		}

		public void RemoveConnection(GdbObjectReference edge,
		                             IEnumerable<IPoint> fromPoints)
		{
			foreach (IPoint point in fromPoints)
			{
				int index;

				if (TryGetNodeIndex(point, out index))
				{
					var connectionsToRemove = new List<AdjacentNode>(1);

					foreach (AdjacentNode adjacentNode in Connections[index])
					{
						if (EdgeReferences[adjacentNode.EdgeIndex].Equals(edge))
						{
							connectionsToRemove.Add(adjacentNode);
						}
					}

					foreach (AdjacentNode adjacentNodeToRemove in connectionsToRemove)
					{
						Connections[index].Remove(adjacentNodeToRemove);
					}
				}
			}
		}

		public int AddEdgeInteriorNode([NotNull] IFeature edgeFeature,
		                               [NotNull] IPoint location,
		                               bool respectLineOrientation)
		{
			if (AreaOfInterest != null &&
			    GeometryUtils.Disjoint(AreaOfInterest, location))
			{
				throw new InvalidOperationException(
					$"The provided location {GeometryUtils.ToString(location)} is outside the AoI. Cannot add edge interior node");
			}

			// TODO: Consider blocking the existing edge (requires the BlockedEdgeIndex list from Dijkstra to be referenced here)
			//		 However, an extra path that has the same lenght as the 'split' paths do not harm the resolution. Furthermore
			//		 It simplifies removing the edge-interior node later on - the original connection is still valid!

			// Virtual split, add the two parts
			var polyline = (IPolyline) edgeFeature.Shape;

			double distanceAlong = GeometryUtils.GetDistanceAlongCurve(polyline, location);

			// Edges
			// The blocking logic works by edge index. Therefore it is ok to add the same GdbObjRef twice.
			// Alternatively we could create an edge list that can also contain sub-feature edges.
			var gdbObjRef = new GdbObjectReference(edgeFeature);

			polyline.QueryFromPoint(_queryPointFrom);
			polyline.QueryToPoint(_queryPointTo);

			int fromIndex = AddNode(_queryPointFrom, out List<AdjacentNode> _);
			int toIndex = AddNode(_queryPointTo, out List<AdjacentNode> _);

			// split the weight
			var weightToLocation = (float) distanceAlong;
			var weightFromLocation = (float) (polyline.Length - distanceAlong);

			// check other interior nodes on the same edge
			List<int> interiorNodes;
			if (! EdgeInteriorNodesByEdge.TryGetValue(gdbObjRef, out interiorNodes))
			{
				interiorNodes = new List<int>(2);
				EdgeInteriorNodesByEdge.Add(gdbObjRef, interiorNodes);
			}

			if (interiorNodes.Count > 1)
			{
				throw new NotImplementedException(
					"More than two nodes in the interior of a single polyline is not supported.");
			}

			// special case: there is already an intermediate node on the same edge -> update from-/to- information
			if (interiorNodes.Count == 1)
			{
				int existingNodeIndex = interiorNodes[0];
				Node existingNode = Assert.NotNull(Nodes[existingNodeIndex]);

				_queryPointFrom.PutCoords(existingNode.X, existingNode.Y);
				double distanceAlongExisting = GeometryUtils.GetDistanceAlongCurve(polyline,
					_queryPointFrom);

				if (distanceAlongExisting < distanceAlong)
				{
					// the existing interior node is before this node along the line

					weightToLocation = (float) (distanceAlong - distanceAlongExisting);
					fromIndex = existingNodeIndex;
				}
				else
				{
					// the existing interior node is after this node along the line

					weightFromLocation = (float) (distanceAlongExisting - distanceAlong);
					toIndex = existingNodeIndex;
				}
			}

			// Add the interior node and the split edges:
			int thisNodeIndex = AddNode(location, out List<AdjacentNode> _);

			AddConnectivity(gdbObjRef, fromIndex, thisNodeIndex, weightToLocation,
			                respectLineOrientation);
			AddConnectivity(gdbObjRef, thisNodeIndex, toIndex, weightFromLocation,
			                respectLineOrientation);

			// add to interior edges list
			interiorNodes.Add(thisNodeIndex);

			return thisNodeIndex;
		}

		public int GetNodeIndex([NotNull] IPoint point)
		{
			Node node = CreateNode(point);

			int index = NodeIndexesByNode[node];

			return index;
		}

		public bool TryGetNodeIndex([NotNull] IPoint point, out int index)
		{
			if (AreaOfInterest != null &&
			    GeometryUtils.Disjoint(AreaOfInterest, point))
			{
				index = -1;
				return false;
			}

			Node node = CreateNode(point);

			return NodeIndexesByNode.TryGetValue(node, out index);
		}

		public IEnumerable<GdbObjectReference> GetIncidentEdges([NotNull] IPoint nodeLocation)
		{
			int nodeIndex;
			if (! TryGetNodeIndex(nodeLocation, out nodeIndex))
			{
				yield break;
			}

			foreach (GdbObjectReference edgeRef in GetIncidentEdges(nodeIndex))
			{
				yield return edgeRef;
			}
		}

		public IEnumerable<GdbObjectReference> GetIncidentEdges(GdbObjectReference nodeFeatureRef)
		{
			int nodeIndex;
			if (! NodeIndexesByNodeReference.TryGetValue(nodeFeatureRef, out nodeIndex))
			{
				yield break;
			}

			foreach (GdbObjectReference edgeRef in GetIncidentEdges(nodeIndex))
			{
				yield return edgeRef;
			}
		}

		private IEnumerable<GdbObjectReference> GetIncidentEdges(int nodeIndex)
		{
			List<AdjacentNode> connections = Connections[nodeIndex];

			foreach (AdjacentNode adjacentNode in connections)
			{
				int edgeIndex = adjacentNode.EdgeIndex;

				GdbObjectReference edgeRef = EdgeReferences[edgeIndex];

				yield return edgeRef;
			}
		}

		// Additional methods to be considered, once there is a need:
		//public int GetIncidentEdges(GdbObjectReference nodeFeatureRef,
		//                            IList<GdbObjectReference> result){}

		//public int GetIncidentEdges(IPoint nodeLocation, IList<GdbObjectReference> result){}

		// Add additional dictionary <EdgeRef,Adjacent> once this is needed
		//public int GetConnectedEdges(GdbObjectReference edgeFeatureRef,
		//                             IList<GdbObjectReference> result,
		//                             LineEnd ends = LineEnd.Both) { }

		private void AddConnectivity(GdbObjectReference objRef,
		                             int fromNodeIndex, int toNodeIndex, float weight,
		                             bool respectLineOrientation)
		{
			List<AdjacentNode> connectedNodesAtFrom = Connections[fromNodeIndex];
			List<AdjacentNode> connectedNodesAtTo = Connections[toNodeIndex];

			AddConnectivity(objRef, fromNodeIndex, toNodeIndex, weight, respectLineOrientation,
			                connectedNodesAtFrom, connectedNodesAtTo);
		}

		private void AddConnectivity(GdbObjectReference objRef,
		                             int fromNodeIndex, int toNodeIndex, float weight,
		                             bool respectLineOrientation,
		                             ICollection<AdjacentNode> connectedNodesAtFrom,
		                             ICollection<AdjacentNode> connectedNodesAtTo)
		{
			// adjacency:
			int edgeIndex = EdgeReferences.Count;
			EdgeReferences.Add(objRef);

			// Allow adding the edge, even if its to point is outside the AOI and no connection can
			// be made. It is still relevant for the node degree count (number of edges at from).
			if (fromNodeIndex >= 0)
			{
				var fromToEdge = new AdjacentNode(toNodeIndex, weight, edgeIndex);
				connectedNodesAtFrom.Add(fromToEdge);
			}

			if (! respectLineOrientation && toNodeIndex >= 0)
			{
				var toFromEdge = new AdjacentNode(fromNodeIndex, weight, edgeIndex);
				connectedNodesAtTo.Add(toFromEdge);
			}
		}

		private int AddNode([NotNull] IPoint point,
		                    [CanBeNull] out List<AdjacentNode> adjacentNodeList)
		{
			if (AreaOfInterest != null &&
			    ! GeometryUtils.Contains(AreaOfInterest, point))
			{
				adjacentNodeList = null;
				return -1;
			}

			Node node = CreateNode(point);

			int index = EnsureInList(Nodes, node, NodeIndexesByNode);

			adjacentNodeList = EnsureInList(Connections, index);

			return index;
		}

		private Node CreateNode([NotNull] IPoint point)
		{
			var result = new Node(point.X, point.Y, _originX, _originY, _resolution);

			return result;
		}

		private static List<AdjacentNode> EnsureInList(
			[NotNull] IList<List<AdjacentNode>> edgesByIndex,
			int atIndex)
		{
			List<AdjacentNode> result;

			if (edgesByIndex.Count == atIndex)
			{
				result = new List<AdjacentNode>(3);
				edgesByIndex.Add(result);

				Assert.AreEqual(edgesByIndex.Count, atIndex + 1, "Unexpected list size");
			}
			else
			{
				result = edgesByIndex[atIndex];
			}

			return result;
		}

		private static int EnsureInList([NotNull] ICollection<Node> nodeList,
		                                [NotNull] Node node,
		                                [NotNull] IDictionary<Node, int> nodeIndexByNode)
		{
			int index;
			if (! nodeIndexByNode.TryGetValue(node, out index))
			{
				// not yet in the list
				index = nodeList.Count;

				nodeList.Add(node);

				nodeIndexByNode.Add(node, index);
			}

			return index;
		}
	}
}
