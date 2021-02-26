using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public abstract class IssueDataset : IIssueDataset
	{
		[NotNull] private readonly IssueWriter _issueWriter;

		protected IssueDataset([NotNull] IssueWriter issueWriter,
		                       [NotNull] IIssueTableFields fields)
		{
			Assert.ArgumentNotNull(issueWriter, nameof(issueWriter));
			Assert.ArgumentNotNull(fields, nameof(fields));

			_issueWriter = issueWriter;
			Fields = fields;
		}

		public IObjectClass ObjectClass => _issueWriter.ObjectClass;

		public int IssueCount => _issueWriter.WriteCount;

		public IIssueTableFields Fields { get; }
	}
}
