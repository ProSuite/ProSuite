using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Env;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.QA.VerificationResult
{
	public static class QAVerificationResults
	{
		public static void ShowResults(
			[CanBeNull] IWin32Window owner,
			[NotNull] QualityVerification verification,
			string contextType,
			string contextName,
			[NotNull] IDomainTransactionManager domainTransactionManager)
		{
			Assert.ArgumentNotNull(verification, nameof(verification));
			Assert.ArgumentNotNull(domainTransactionManager, nameof(domainTransactionManager));

			using (var form = new QAVerificationForm(domainTransactionManager))
			{
				domainTransactionManager.UseTransaction(
					delegate
					{
						Initialize(verification, domainTransactionManager);

						// ReSharper disable once AccessToDisposedClosure
						form.SetVerification(verification,
						                     contextType,
						                     contextName);
					});

				form.StartPosition = FormStartPosition.CenterScreen;
				//if (owner != null)
				//{
				//    owner = owner.TopLevelControl;
				//}
				UIEnvironment.ShowDialog(form, owner);
			}
		}

		private static void Initialize(
			[NotNull] QualityVerification verification,
			[NotNull] IDomainTransactionManager domainTransactionManager)
		{
			Assert.ArgumentNotNull(verification, nameof(verification));
			Assert.ArgumentNotNull(domainTransactionManager, nameof(domainTransactionManager));

			if (verification.IsPersistent)
			{
				domainTransactionManager.Reattach(verification);
				domainTransactionManager.Initialize(verification.VerificationDatasets);
			}

			foreach (
				QualityConditionVerification conditionVerification in
				verification.ConditionVerifications)
			{
				QualityCondition qualityCondition =
					conditionVerification.DisplayableCondition;

				if (qualityCondition.IsPersistent)
				{
					domainTransactionManager.Reattach(qualityCondition);
				}

				foreach (TestParameterValue value in qualityCondition.ParameterValues)
				{
					if (value.IsPersistent)
					{
						domainTransactionManager.Reattach(value);
					}
				}
			}

			// NOTE this causes NonUniqueObjectExceptions in case of datasets from other models (only?)
			// But: it does not seem to be really needed
			//foreach (
			//    QualityVerificationDataset verificationDataset in
			//        verification.VerificationDatasets)
			//{
			//    domainTransactionManager.Reattach(verificationDataset.Dataset);
			//}
		}
	}
}
