using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface
{
	public interface ITinGenerator
	{
		ITin GenerateTin([NotNull] IEnvelope extent, ITrackCancel trackCancel = null);

		IList<IEnvelope> SuggestSubdivisions([NotNull] IEnvelope areaOfInterest,
		                                     int maxTinPointCount);
	}
}
