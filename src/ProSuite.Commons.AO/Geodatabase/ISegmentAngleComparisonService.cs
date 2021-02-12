using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public interface ISegmentAngleComparisonService
	{
		void CompareSegmentAngles([NotNull] IPolyline sourceShape,
		                          [NotNull] IPolyline transformedShape,
		                          [NotNull] IFeature transformedFeature);

		void CompareSegmentAngles([NotNull] IPolygon sourceShape,
		                          [NotNull] IPolygon transformedShape,
		                          [NotNull] IFeature transformedFeature);
	}
}
