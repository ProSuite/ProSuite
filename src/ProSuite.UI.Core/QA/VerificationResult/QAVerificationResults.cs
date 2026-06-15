using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Env;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.UI.Core.QA.VerificationResult
{
	public static class QAVerificationResults
	{
		public static void ShowResults(
			[CanBeNull] IWin32Window owner,
			[NotNull] QualityVerification verification,
			string contextType,
			string contextName,
			[NotNull] IDomainTransactionManager domainTransactionManager,
			[CanBeNull] IInstanceConfigurationRepository transformerConfigurations = null)
		{
			Assert.ArgumentNotNull(verification, nameof(verification));
			Assert.ArgumentNotNull(domainTransactionManager, nameof(domainTransactionManager));

			using (var form = new QAVerificationForm(domainTransactionManager))
			{
				domainTransactionManager.UseTransaction(
					delegate
					{
						Initialize(verification, domainTransactionManager,
						           transformerConfigurations);

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
			[NotNull] IDomainTransactionManager domainTransactionManager,
			[CanBeNull] IInstanceConfigurationRepository transformerConfigurations)
		{
			Assert.ArgumentNotNull(verification, nameof(verification));
			Assert.ArgumentNotNull(domainTransactionManager, nameof(domainTransactionManager));

			if (verification.IsPersistent)
			{
				domainTransactionManager.Reattach(verification);
				domainTransactionManager.Initialize(verification.VerificationDatasets);
			}

			IList<QualityCondition> displayableConditions = verification.ConditionVerifications
				.Select(v => v.DisplayableCondition).ToList();

			InstanceConfigurationUtils.InitializeAssociatedConfigurationsTx(
				displayableConditions, domainTransactionManager, transformerConfigurations);
		}
	}
}
