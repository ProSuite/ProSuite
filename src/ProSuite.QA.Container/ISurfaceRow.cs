using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	[CLSCompliant(false)]
	public interface ISurfaceRow
	{
		[NotNull]
		ISimpleSurface Surface { get; }

		[NotNull]
		IEnvelope Extent { get; }
	}
}
