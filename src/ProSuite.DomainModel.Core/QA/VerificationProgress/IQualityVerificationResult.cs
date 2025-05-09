using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.VerificationProgress
{
	public interface IQualityVerificationResult
	{
		bool HasQualityVerification();

		[CanBeNull]
		QualityVerification GetQualityVerification();

		[CanBeNull]
		string HtmlReportPath { get; }

		[CanBeNull]
		string IssuesGdbPath { get; }

		int VerifiedConditionCount { get; }

		bool HasIssues { get; }

		bool IsFulfilled { get; }

		int RowCountWithStopConditions { get; }

		bool CanSaveIssues { get; }

		int IssuesSaved { get; }

		int SaveIssues(ErrorDeletionInPerimeter errorDeletion =
			               ErrorDeletionInPerimeter.VerifiedQualityConditions);

		Task<int> SaveIssuesAsync(ErrorDeletionInPerimeter errorDeletion =
			                          ErrorDeletionInPerimeter.VerifiedQualityConditions);
	}
}
