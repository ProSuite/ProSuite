namespace ProSuite.DomainModel.Core.QA.VerificationProgress
{
	public interface IQualityVerificationResult
	{
		bool HasQualityVerification();

		QualityVerification GetQualityVerification();

		string HtmlReportPath { get; }

		string IssuesGdbPath { get; }

		bool HasIssues { get; }

		bool CanSaveIssues { get; }

		int SaveIssues(ErrorDeletionInPerimeter errorDeletion =
			               ErrorDeletionInPerimeter.VerifiedQualityConditions);
	}
}
