using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.EdgeMatch
{
	internal class EdgeMatchNeighborConnection<T>
		where T : EdgeMatchSingleBorderConnection
	{
		public EdgeMatchNeighborConnection([NotNull] T neighborBorderConnection,
		                                   [NotNull] IPolyline commonLine,
		                                   bool isGap = false)
		{
			NeighborBorderConnection = neighborBorderConnection;
			CommonLine = commonLine;
			IsGap = isGap;
		}

		[NotNull]
		public T NeighborBorderConnection { get; }

		[NotNull]
		public IPolyline CommonLine { get; }

		public bool IsGap { get; }
	}
}
