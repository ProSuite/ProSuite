using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	[PublicAPI]
	public class DefaultIssueGeometryProvider : IIssueGeometryTransformation
	{
		[NotNull] private readonly IGeometry _defaultGeometry;

		public DefaultIssueGeometryProvider([NotNull] IGeometry defaultGeometry)
		{
			Assert.ArgumentNotNull(defaultGeometry, nameof(defaultGeometry));

			_defaultGeometry = defaultGeometry;
		}

		public IGeometry TransformGeometry(Issue issue, IGeometry issueGeometry)
		{
			return issueGeometry == null || issueGeometry.IsEmpty
				       ? _defaultGeometry
				       : issueGeometry;
		}
	}
}
