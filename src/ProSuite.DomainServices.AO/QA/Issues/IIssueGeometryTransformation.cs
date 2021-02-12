using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public interface IIssueGeometryTransformation
	{
		[CanBeNull]
		IGeometry TransformGeometry([NotNull] Issue issue,
		                            [CanBeNull] IGeometry issueGeometry);
	}
}
