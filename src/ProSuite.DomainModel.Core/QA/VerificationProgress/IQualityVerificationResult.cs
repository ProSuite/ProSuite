namespace ProSuite.DomainModel.Core.QA.VerificationProgress
{
	public interface IQualityVerificationResult
	{
		QualityVerification GetQualityVerification();

		bool HasIssues { get; }

		bool CanSaveIssues { get; }

		void SaveIssues();
	}
}