using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public interface IIssueTableFields
	{
		[ContractAnnotation("optional : false => notnull")]
		string GetName(IssueAttribute attribute, bool optional = false);

		int GetIndex(IssueAttribute attribute, [NotNull] ITable table,
		             bool optional = false);

		bool HasField(IssueAttribute attribute, [NotNull] ITable table);
	}
}
