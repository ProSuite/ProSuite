using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IssueTable : IssueDataset, IIssueTable
	{
		[NotNull] private readonly IssueRowWriter _issueWriter;

		public IssueTable([NotNull] IssueRowWriter issueWriter,
		                  [NotNull] IIssueTableFields fields)
			: base(issueWriter, fields)
		{
			_issueWriter = issueWriter;
		}

		public ITable Table => (ITable) _issueWriter.ObjectClass;
	}
}
