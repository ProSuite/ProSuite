using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public interface IIssueReporter
	{
		void Report([CanBeNull] IReadOnlyFeature feature,
		            [CanBeNull] IGeometry geometry,
		            [NotNull] string message);
	}
}
