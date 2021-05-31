using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.Cut
{
	public interface INetworkFeatureCutter
	{
		/// <summary>
		/// Whether the provided feture is a network edge supported by this implementation.
		/// </summary>
		/// <param name="feature"></param>
		/// <returns></returns>
		bool IsNetworkEdge(IFeature feature);

		/// <summary>
		/// Calculates the multipoint geometry containing the intersection points between
		/// the input and the cut polyline.
		/// </summary>
		/// <param name="inputPolyline"></param>
		/// <param name="cutPolyline"></param>
		/// <returns></returns>
		IGeometry CalculateSplitPoints([NotNull] IGeometry inputPolyline,
		                               [NotNull] IPolyline cutPolyline);

		/// <summary>
		/// Split (and store) the network edge at the provided split points.
		/// </summary>
		/// <param name="feature"></param>
		/// <param name="splitPoints"></param>
		/// <returns>The resulting edge features, including the updated original.</returns>
		IList<IFeature> SplitNetworkEdge(IFeature feature, IPointCollection splitPoints);
	}
}
