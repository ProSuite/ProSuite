using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public class BeginTileParameters
	{
		public BeginTileParameters([CanBeNull] IEnvelope tileEnvelope,
		                           [CanBeNull] IEnvelope testRunEnvelope)
		{
			TileEnvelope = tileEnvelope;
			TestRunEnvelope = testRunEnvelope;
		}

		[CanBeNull]
		public IEnvelope TileEnvelope { get; }

		[CanBeNull]
		public IEnvelope TestRunEnvelope { get; }
	}
}
