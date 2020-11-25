using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public class BeginTileParameters
	{
		[CLSCompliant(false)]
		public BeginTileParameters([CanBeNull] IEnvelope tileEnvelope,
		                           [CanBeNull] IEnvelope testRunEnvelope)
		{
			TileEnvelope = tileEnvelope;
			TestRunEnvelope = testRunEnvelope;
		}

		[CLSCompliant(false)]
		[CanBeNull]
		public IEnvelope TileEnvelope { get; }

		[CLSCompliant(false)]
		[CanBeNull]
		public IEnvelope TestRunEnvelope { get; }
	}
}
