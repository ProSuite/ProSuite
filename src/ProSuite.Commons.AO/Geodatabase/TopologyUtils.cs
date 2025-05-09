using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class TopologyUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static ITopology OpenTopology([NotNull] IFeatureWorkspace featureWorkspace,
		                                     [NotNull] string name)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			var topoWorkspace = (ITopologyWorkspace) featureWorkspace;

			try
			{
				return topoWorkspace.OpenTopology(name);
			}
			catch (Exception)
			{
				_msg.DebugFormat("Error opening topology {0}", name);
				throw;
			}
		}

		/// <summary>
		/// Gets the name of a topology.
		/// </summary>
		/// <param name="topology">The topology.</param>
		/// <returns>fully qualified name ({database.}owner.table) of the topology.</returns>
		[NotNull]
		public static string GetName([NotNull] ITopology topology)
		{
			Assert.ArgumentNotNull(topology, nameof(topology));

			return DatasetUtils.GetName((IDataset) topology);
		}

		[NotNull]
		public static IWorkspace GetWorkspace([NotNull] ITopology topology)
		{
			Assert.ArgumentNotNull(topology, nameof(topology));

			return Assert.NotNull(((IDataset) topology).Workspace);
		}

		[NotNull]
		public static IEnumerable<IFeatureClass> GetFeatureClasses(
			[NotNull] ITopology topology)
		{
			Assert.ArgumentNotNull(topology, nameof(topology));

			var container = (IFeatureClassContainer) topology;

			int classCount = container.ClassCount;
			for (var i = 0; i < classCount; i++)
			{
				yield return container.Class[i];
			}
		}

		/// <summary>
		/// Checks if the given object class belongs to an topology.
		/// If it belongs to one, the name of the topology is returned also.
		/// </summary>
		/// <param name="objectClass">ObjectClass to check</param>
		/// <param name="topologyName">Name of the topology if the objectClass belongs to one,
		/// NULL otherwise</param>
		/// <returns>TRUE if it belongs to a topology, FALSE otherwise</returns>
		public static bool ClassBelongsToTopology([NotNull] IObjectClass objectClass,
		                                          [CanBeNull] out string topologyName)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			ITopology topology;
			if (ClassBelongsToTopology(objectClass, out topology))
			{
				Assert.NotNull(topology, "topology");
				topologyName = GetName(topology);

				return true;
			}

			topologyName = null;
			return false;
		}

		/// <summary>
		/// Checks if the given object class belongs to a topology.
		/// If it belongs to one, the topology is returned also.
		/// </summary>
		/// <param name="objectClass">ObjectClass to check</param>
		/// <param name="topology">Topology the object class belongs to (null if none)</param>
		/// <returns>true if it belongs to a topology, false otherwise</returns>
		public static bool ClassBelongsToTopology([NotNull] IObjectClass objectClass,
		                                          [CanBeNull] out ITopology topology)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			topology = null;

			// ITopologyClass does not exist any more in AO11
			IWorkspace workspace = DatasetUtils.GetWorkspace(objectClass);

			ITopologyWorkspace topologyWorkspace = workspace as ITopologyWorkspace;

			if (topologyWorkspace == null)
			{
				return false;
			}

			IFeatureClass featureClass = objectClass as IFeatureClass;

			IFeatureDataset featureDataset = featureClass?.FeatureDataset;

			if (featureDataset == null)
			{
				return false;
			}

			ITopologyContainer topologyContainer = (ITopologyContainer) featureDataset;

			if (topologyContainer.TopologyCount == 0)
			{
				return false;
			}

			for (int i = 0; i < topologyContainer.TopologyCount; i++)
			{
				ITopology candidateTopo = topologyContainer.Topology[i];

				foreach (IFeatureClass topoClass in DatasetUtils.GetFeatureClasses(
					         (IFeatureClassContainer) candidateTopo))
				{
					if (DatasetUtils.IsSameObjectClass(topoClass, featureClass))
					{
						topology = candidateTopo;

						return true;
					}
				}
			}

			return false;
		}

		[CanBeNull]
		public static ITopology GetTopology([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			if (ClassBelongsToTopology(objectClass, out ITopology result))
			{
				return result;
			}

			return null;
		}

		[CanBeNull]
		public static ISpatialReference GetSpatialReference(
			[NotNull] ITopology topology)
		{
			Assert.ArgumentNotNull(topology, nameof(topology));

			return ((IGeoDataset) topology).SpatialReference;
		}

		[NotNull]
		public static IEnvelope ValidateTopology([NotNull] ITopology topology,
		                                         [CanBeNull] IEnvelope envelope = null)
		{
			Assert.ArgumentNotNull(topology, nameof(topology));

			IEnvelope validationEnvelope = envelope ??
			                               ((IGeoDataset) topology.FeatureDataset).Extent;

			string name = GetName(topology);

			_msg.DebugFormat("Validating topology '{0}'", name);

			try
			{
				topology.ValidateTopology(validationEnvelope);

				_msg.DebugFormat("Topology validated");

				return validationEnvelope;
			}
			catch (COMException e)
			{
				_msg.DebugFormat("Error validating topology '{0}' in extent ({1}): {2}",
				                 name,
				                 GeometryUtils.Format(validationEnvelope),
				                 e.Message);

				switch (e.ErrorCode)
				{
					case (int) fdoError.FDO_E_INVALID_TOPOLOGY_RULE:
						throw new InvalidOperationException(
							string.Format(
								"Error validating topology '{0}' due to an invalid topology rule",
								name),
							e);

					case (int) fdoError.FDO_E_TOPOLOGY_ENGINE_FAILURE:
						// https://issuetracker02.eggits.net/browse/COM-110
						throw new InvalidOperationException(
							string.Format(
								"Error validating topology '{0}' due to a failure in the topology engine; " +
								"possible cause: a validated feature has NULL value in subtype field",
								name),
							e);
				}

				throw;
			}
		}

		#region Selected edges and nodes

		/// <summary>
		/// Returns list of selected edges, can be null or empty
		/// </summary>
		/// <param name="topologyGraph"></param>
		/// <returns></returns>
		[NotNull]
		public static IList<ITopologyEdge> GetSelectedEdges(
			[NotNull] ITopologyGraph topologyGraph)
		{
			Assert.ArgumentNotNull(topologyGraph, nameof(topologyGraph));

			var result = new List<ITopologyEdge>();

			IEnumTopologyEdge enumEdges = topologyGraph.EdgeSelection;
			enumEdges.Reset();

			ITopologyEdge edge = enumEdges.Next();

			while (edge != null)
			{
				result.Add(edge);
				edge = enumEdges.Next();
			}

			return result;
		}

		/// <summary>
		/// Returns list of selected nodes, can be null or empty
		/// </summary>
		/// <param name="topologyGraph"></param>
		/// <returns></returns>
		[NotNull]
		public static IList<ITopologyNode> GetSelectedNodes(
			[NotNull] ITopologyGraph topologyGraph)
		{
			Assert.ArgumentNotNull(topologyGraph, nameof(topologyGraph));

			var nodes = new List<ITopologyNode>();

			IEnumTopologyNode enumNodes = topologyGraph.NodeSelection;
			enumNodes.Reset();

			ITopologyNode node = enumNodes.Next();

			while (node != null)
			{
				nodes.Add(node);
				node = enumNodes.Next();
			}

			return nodes;
		}

		/// <summary>
		/// Returns selected edge, when only one edge is selected
		/// </summary>
		/// <param name="topologyGraph"></param>
		/// <param name="nodeSelectionAllowed">if selection may include nodes</param>
		/// <returns></returns>
		[CanBeNull]
		public static ITopologyEdge GetSelectedEdge([NotNull] ITopologyGraph topologyGraph,
		                                            bool nodeSelectionAllowed)
		{
			Assert.ArgumentNotNull(topologyGraph, nameof(topologyGraph));

			var valid = false;
			if (nodeSelectionAllowed)
			{
				if (GetEdgeSelectionCount(topologyGraph) == 1)
				{
					valid = true;
				}
			}
			else
			{
				int nodes;
				int edges;
				GetSelectionCounts(topologyGraph, out nodes, out edges);
				if (nodes == 0 && edges == 1)
				{
					valid = true;
				}
			}

			if (! valid)
			{
				return null;
			}

			IEnumTopologyEdge enumEdges = topologyGraph.EdgeSelection;
			return enumEdges.Next();
		}

		/// <summary>
		/// Returns selected node, when only one node is selected
		/// </summary>
		/// <param name="topologyGraph"></param>
		/// <param name="edgeSelectionAllowed">if selection may include edges</param>
		/// <returns></returns>
		[CanBeNull]
		public static ITopologyNode GetSelectedNode([NotNull] ITopologyGraph topologyGraph,
		                                            bool edgeSelectionAllowed)
		{
			Assert.ArgumentNotNull(topologyGraph, nameof(topologyGraph));

			var valid = false;
			if (edgeSelectionAllowed)
			{
				if (GetNodeSelectionCount(topologyGraph) == 1)
				{
					valid = true;
				}
			}
			else
			{
				int nodes;
				int edges;
				GetSelectionCounts(topologyGraph, out nodes, out edges);
				if (nodes == 1 && edges == 0)
				{
					valid = true;
				}
			}

			if (! valid)
			{
				return null;
			}

			IEnumTopologyNode enumNodes = topologyGraph.NodeSelection;
			return enumNodes.Next();
		}

		#endregion Selected edges and nodes

		#region Selected features

		/// <summary>
		///  returns parent features of currently selected edges
		/// </summary>
		/// <param name="edges"></param>
		/// <returns></returns>
		[NotNull]
		public static IList<IFeature> GetEdgeFeatures([NotNull] IList<ITopologyEdge> edges)
		{
			Assert.ArgumentNotNull(edges, nameof(edges));

			var result = new List<IFeature>();
			GetEdgeFeatures(edges, result);
			return result;
		}

		/// <summary>
		/// fills features list with parent features of all selected edges (can include doubles)
		/// </summary>
		/// <param name="edges">The edges.</param>
		/// <param name="features">The features.</param>
		public static void GetEdgeFeatures([NotNull] IList<ITopologyEdge> edges,
		                                   [NotNull] IList<IFeature> features)
		{
			Assert.ArgumentNotNull(edges, nameof(edges));
			Assert.ArgumentNotNull(features, nameof(features));

			foreach (ITopologyEdge edge in edges)
			{
				IEnumTopologyParent enumParents = edge.Parents;
				enumParents.Reset();

				esriTopologyParent topologyParent = enumParents.Next();
				IFeatureClass featureClass = topologyParent.m_pFC;

				while (featureClass != null)
				{
					IFeature feature = featureClass.GetFeature(topologyParent.m_FID);
					features.Add(feature);

					topologyParent = enumParents.Next();
					featureClass = topologyParent.m_pFC;
				}
			}
		}

		/// <summary>
		///  returns parent features of currently selected edge
		/// </summary>
		/// <param name="edge"></param>
		/// <returns></returns>
		[NotNull]
		public static IList<IFeature> GetEdgeFeatures([NotNull] ITopologyEdge edge)
		{
			Assert.ArgumentNotNull(edge, nameof(edge));

			var result = new List<IFeature>();
			GetEdgeFeatures(edge, result);
			return result;
		}

		/// <summary>
		/// fills features list with parent features the selected edge
		/// </summary>
		/// <param name="edge">The edge.</param>
		/// <param name="features">The features.</param>
		public static void GetEdgeFeatures([NotNull] ITopologyEdge edge,
		                                   [NotNull] IList<IFeature> features)
		{
			Assert.ArgumentNotNull(edge, nameof(edge));
			Assert.ArgumentNotNull(features, nameof(features));

			IEnumTopologyParent enumParents = edge.Parents;
			enumParents.Reset();

			esriTopologyParent topologyParent = enumParents.Next();
			IFeatureClass featureClass = topologyParent.m_pFC;

			while (featureClass != null)
			{
				IFeature feature = featureClass.GetFeature(topologyParent.m_FID);
				features.Add(feature);

				topologyParent = enumParents.Next();
				featureClass = topologyParent.m_pFC;
			}
		}

		/// <summary>
		/// returns parent features of all selected nodes (can include doubles)
		/// </summary>
		/// <param name="nodes"></param>
		/// <returns></returns>
		[NotNull]
		public static IList<IFeature> GetNodeFeatures([NotNull] IList<ITopologyNode> nodes)
		{
			Assert.ArgumentNotNull(nodes, nameof(nodes));

			var result = new List<IFeature>();
			GetNodeFeatures(nodes, result);
			return result;
		}

		/// <summary>
		/// Fills features list with node's features
		/// </summary>
		/// <param name="nodes"></param>
		/// <param name="features"></param>
		public static void GetNodeFeatures([NotNull] IList<ITopologyNode> nodes,
		                                   [NotNull] IList<IFeature> features)
		{
			Assert.ArgumentNotNull(nodes, nameof(nodes));
			Assert.ArgumentNotNull(features, nameof(features));

			foreach (ITopologyNode node in nodes)
			{
				IEnumTopologyParent enumParents = node.Parents;
				enumParents.Reset();

				esriTopologyParent topologyParent = enumParents.Next();
				IFeatureClass featureClass = topologyParent.m_pFC;

				while (featureClass != null)
				{
					IFeature feature = featureClass.GetFeature(topologyParent.m_FID);
					features.Add(feature);

					topologyParent = enumParents.Next();
					featureClass = topologyParent.m_pFC;
				}
			}
		}

		/// <summary>
		/// returns parent features of a selected nodes 
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		[NotNull]
		public static IList<IFeature> GetNodeFeatures([NotNull] ITopologyNode node)
		{
			Assert.ArgumentNotNull(node, nameof(node));

			var result = new List<IFeature>();
			GetNodeFeatures(node, result);
			return result;
		}

		/// <summary>
		/// Fills features list with node's features
		/// </summary>
		/// <param name="node"></param>
		/// <param name="features"></param>
		public static void GetNodeFeatures([NotNull] ITopologyNode node,
		                                   [NotNull] IList<IFeature> features)
		{
			Assert.ArgumentNotNull(node, nameof(node));
			Assert.ArgumentNotNull(features, nameof(features));

			IEnumTopologyParent enumParents = node.Parents;
			enumParents.Reset();

			esriTopologyParent topologyParent = enumParents.Next();
			IFeatureClass featureClass = topologyParent.m_pFC;

			while (featureClass != null)
			{
				IFeature feature = featureClass.GetFeature(topologyParent.m_FID);
				features.Add(feature);

				topologyParent = enumParents.Next();
				featureClass = topologyParent.m_pFC;
			}
		}

		#endregion Selected features

		#region Selection counts

		public static int GetNodeSelectionCount([CanBeNull] ITopologyGraph topologyGraph)
		{
			if (topologyGraph == null)
			{
				return -1; // or 0?
			}

			return topologyGraph.SelectionCount[(int) esriTopologyElementType.esriTopologyNode];
		}

		public static int GetEdgeSelectionCount([CanBeNull] ITopologyGraph topologyGraph)
		{
			if (topologyGraph == null)
			{
				return -1;
			}

			return topologyGraph.SelectionCount[(int) esriTopologyElementType.esriTopologyEdge];
		}

		public static void GetSelectionCounts([CanBeNull] ITopologyGraph topologyGraph,
		                                      out int nodes, out int edges)
		{
			if (topologyGraph == null)
			{
				nodes = -1;
				edges = -1;
				return;
			}

			edges = topologyGraph.SelectionCount[
				(int) esriTopologyElementType.esriTopologyEdge];

			nodes = topologyGraph.SelectionCount[
				(int) esriTopologyElementType.esriTopologyNode];
		}

		#endregion Selection counts

		#region Topology search radius

		public static double GetSearchRadiusFromTopology([NotNull] ITopology topology)
		{
			Assert.ArgumentNotNull(topology, nameof(topology));

			return topology.ClusterTolerance;
		}

		#endregion Topology search radius
	}
}
