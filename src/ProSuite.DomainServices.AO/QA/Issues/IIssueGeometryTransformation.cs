using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	[CLSCompliant(false)]
	public interface IIssueGeometryTransformation
	{
		[CanBeNull]
		IGeometry TransformGeometry([NotNull] Issue issue,
		                            [CanBeNull] IGeometry issueGeometry);
	}
}
