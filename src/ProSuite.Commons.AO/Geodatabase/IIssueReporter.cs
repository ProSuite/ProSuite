using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public interface IIssueReporter
	{
		void Report([CanBeNull] IFeature feature,
		            [CanBeNull] IGeometry geometry,
		            [NotNull] string message);
	}
}
