using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Tests.EdgeMatch
{
	internal abstract class EdgeMatchBorderConnectionUnion<TNeighbors,
	                                                       TNeighborConnection,
	                                                       TBorderConnection>
		where TNeighbors : EdgeMatchNeighbors<TNeighborConnection, TBorderConnection>
		where TNeighborConnection : EdgeMatchNeighborConnection<TBorderConnection>
		where TBorderConnection : EdgeMatchSingleBorderConnection
	{
		private readonly
			Dictionary<FeatureKey, Dictionary<FeatureKey, TNeighbors>>
			_borderConnections;

		protected EdgeMatchBorderConnectionUnion()
		{
			_borderConnections =
				new Dictionary<FeatureKey, Dictionary<FeatureKey, TNeighbors>>(
					new FeatureKeyComparer());
		}

		[NotNull]
		public TNeighbors GetNeighbors(
			[NotNull] TBorderConnection borderConnection)
		{
			Dictionary<FeatureKey, TNeighbors> neighborsByFeature;
			var geometryFeatureKey = new FeatureKey(borderConnection.Feature.OID,
			                                        borderConnection.ClassIndex);
			if (! _borderConnections.TryGetValue(geometryFeatureKey, out neighborsByFeature))
			{
				neighborsByFeature =
					new Dictionary<FeatureKey, TNeighbors>(new FeatureKeyComparer());
				_borderConnections.Add(geometryFeatureKey, neighborsByFeature);
			}

			TNeighbors neighbors;
			var borderFeatureKey = new FeatureKey(
				borderConnection.BorderFeature.OID, borderConnection.BorderClassIndex);

			if (! neighborsByFeature.TryGetValue(borderFeatureKey, out neighbors))
			{
				neighbors = CreateNeighbors(borderConnection);
				neighborsByFeature.Add(borderFeatureKey, neighbors);
			}

			return neighbors;
		}

		protected abstract TNeighbors CreateNeighbors(TBorderConnection borderConnection);

		[CanBeNull]
		public IPolyline GetUnmatchedBoundary([CanBeNull] IPolyline completeBoundary)
		{
			if (completeBoundary == null)
			{
				return null;
			}

			IPolyline toReduce = GeometryFactory.Clone(completeBoundary);

			foreach (TNeighbors neighbors in Neighbors)
			{
				foreach (TNeighborConnection neighbor in neighbors.NeighborConnections)
				{
					if (neighbor.IsGap)
					{
						continue;
					}

					toReduce = EdgeMatchUtils.GetDifference(toReduce, neighbor.CommonLine);
				}
			}

			return toReduce;
		}

		[CanBeNull]
		public IPolyline GetCompleteBorder([NotNull] TileInfo tileInfo)
		{
			IEnvelope tileEnvelope = Assert.NotNull(tileInfo.CurrentEnvelope);

			var boundaries = new List<IGeometry>();

			foreach (TNeighbors neighbor in Neighbors)
			{
				EdgeMatchSingleBorderConnection borderConnection = neighbor.BorderConnection;

				if (IsDisjoint(borderConnection.GeometryAlongBoundary.Envelope, tileEnvelope))
				{
					continue;
				}

				boundaries.Add(borderConnection.GeometryAlongBoundary);
			}

			if (boundaries.Count <= 0)
			{
				return null;
			}

			var completeBorder = (IPolyline) GeometryFactory.CreateUnion(boundaries, 0);

			return completeBorder;
		}

		public IEnumerable<TNeighbors> Neighbors
		{
			get
			{
				foreach (
					Dictionary<FeatureKey, TNeighbors> neighborsDict in
					_borderConnections.Values)
				{
					foreach (TNeighbors neighbors in neighborsDict.Values)
					{
						yield return neighbors;
					}
				}
			}
		}

		public void Clear()
		{
			_borderConnections.Clear();
		}

		public void Clear(WKSEnvelope tileEnvelope, WKSEnvelope allEnvelope)
		{
			var toRemove = new List<FeatureKey>();
			foreach (
				KeyValuePair<FeatureKey, Dictionary<FeatureKey, TNeighbors>> areaPair in
				_borderConnections)
			{
				Dictionary<FeatureKey, TNeighbors> borderPairs = areaPair.Value;
				var borderConnectionRemoves = new List<FeatureKey>();
				foreach (KeyValuePair<FeatureKey, TNeighbors> borderPair in borderPairs)
				{
					TNeighbors neighbors = borderPair.Value;
					neighbors.Clear(tileEnvelope, allEnvelope);

					if (IsDone(neighbors.BorderConnection.Feature.Shape, tileEnvelope))
					{
						borderConnectionRemoves.Add(borderPair.Key);
					}
				}

				foreach (FeatureKey borderConnectionRemove in borderConnectionRemoves)
				{
					borderPairs.Remove(borderConnectionRemove);
				}

				if (borderPairs.Count <= 0)
				{
					toRemove.Add(areaPair.Key);
				}
			}

			foreach (FeatureKey remove in toRemove)
			{
				_borderConnections.Remove(remove);
			}
		}

		private static bool IsDisjoint([NotNull] IGeometry geometry1,
		                               [NotNull] IGeometry geometry2)
		{
			return ((IRelationalOperator) geometry1).Disjoint(geometry2);
		}

		private static bool IsDone([NotNull] IGeometry geometry, WKSEnvelope tileEnvelope)
		{
			WKSEnvelope geometryEnvelope = QaGeometryUtils.GetWKSEnvelope(geometry);

			return geometryEnvelope.XMax < tileEnvelope.XMax &&
			       geometryEnvelope.YMax < tileEnvelope.YMax;
		}
	}
}
