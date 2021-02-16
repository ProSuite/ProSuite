using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public interface IIssueAttributeWriter
	{
		void WriteAttributes([NotNull] Issue issue, [NotNull] IRowBuffer rowBuffer);
	}
}
