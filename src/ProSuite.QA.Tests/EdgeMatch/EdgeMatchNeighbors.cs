using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.EdgeMatch
{
	internal abstract class EdgeMatchNeighbors<TNeighborConnection, TBorderConnection>
		where TNeighborConnection : EdgeMatchNeighborConnection<TBorderConnection>
		where TBorderConnection : EdgeMatchSingleBorderConnection
	{
		[NotNull] private readonly List<TNeighborConnection> _neighborConnections =
			new List<TNeighborConnection>();

		protected EdgeMatchNeighbors([NotNull] TBorderConnection borderConnection)
		{
			BorderConnection = borderConnection;
		}

		[NotNull]
		public TBorderConnection BorderConnection { get; }

		[NotNull]
		public IEnumerable<TNeighborConnection> NeighborConnections => _neighborConnections;

		public void AddNeighbor([NotNull] TNeighborConnection neighborConnection)
		{
			_neighborConnections.Add(neighborConnection);
		}

		public bool ContainsAny([NotNull] TBorderConnection borderConnection)
		{
			int featureOID = borderConnection.Feature.OID;
			int classIndex = borderConnection.ClassIndex;
			int borderOID = borderConnection.BorderFeature.OID;
			int borderClassIndex = borderConnection.BorderClassIndex;

			foreach (TNeighborConnection neighborConnection in _neighborConnections)
			{
				TBorderConnection candidate = neighborConnection.NeighborBorderConnection;
				if (candidate.Feature.OID == featureOID &&
				    candidate.BorderFeature.OID == borderOID &&
				    candidate.ClassIndex == classIndex &&
				    candidate.BorderClassIndex == borderClassIndex)
				{
					return true;
				}
			}

			return false;
		}

		public void Clear(WKSEnvelope tileEnvelope, WKSEnvelope allEnvelope)
		{
			var toRemove = new List<TNeighborConnection>();
			foreach (TNeighborConnection neighborConnection in NeighborConnections)
			{
				if (
					EdgeMatchUtils.VerifyHandled(
						neighborConnection.NeighborBorderConnection.Feature.Shape,
						tileEnvelope, allEnvelope))
				{
					toRemove.Add(neighborConnection);
				}
			}

			foreach (TNeighborConnection remove in toRemove)
			{
				_neighborConnections.Remove(remove);
			}
		}
	}
}
