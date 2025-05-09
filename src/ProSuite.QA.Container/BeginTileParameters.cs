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

		/// <summary>
		/// The extent of the current tile.
		/// </summary>
		[CanBeNull]
		public IEnvelope TileEnvelope { get; }

		/// <summary>
		/// The full extent of the test run.
		/// </summary>
		[CanBeNull]
		public IEnvelope TestRunEnvelope { get; }
	}
}
