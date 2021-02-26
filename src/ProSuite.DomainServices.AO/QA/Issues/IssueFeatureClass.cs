using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IssueFeatureClass : IssueDataset, IIssueFeatureClass
	{
		private readonly IssueFeatureWriter _issueWriter;

		public IssueFeatureClass([NotNull] IssueFeatureWriter issueWriter,
		                         [NotNull] IIssueTableFields fields)
			: base(issueWriter, fields)
		{
			_issueWriter = issueWriter;
		}

		public IFeatureClass FeatureClass => _issueWriter.FeatureClass;
	}
}
