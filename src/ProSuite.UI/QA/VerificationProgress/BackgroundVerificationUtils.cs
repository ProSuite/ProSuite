using System;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Progress;
using ProSuite.Commons.UI.WinForms;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.UI.QA.VerificationProgress
{
	public static class BackgroundVerificationUtils
	{
		public static WpfHostingWinForm CreateVerificationProgressForm(
			[NotNull] QualityVerificationGrpc.QualityVerificationGrpcClient qaClient,
			[NotNull] BackgroundVerificationRun verificationRun,
			[CanBeNull] string title)
		{
			async Task<ServiceCallStatus> VerificationAction()
			{
				return await verificationRun.ExecuteAndProcessMessagesAsync(qaClient);
			}

			var progressForm = CreateVerificationProgressForm(
				verificationRun.Progress, VerificationAction, verificationRun.ShowReportAction,
				verificationRun.SaveAction, title);

			progressForm.SetMinimumSize(275, 0);
			progressForm.SetMaximumSize(900, int.MaxValue);

			return progressForm;
		}

		public static WpfHostingWinForm CreateVerificationProgressForm(
			[NotNull] IQualityVerificationProgressTracker progressTracker,
			[NotNull] Func<Task<ServiceCallStatus>> verificationAction,
			[CanBeNull] Action<QualityVerification> showReportAction,
			[CanBeNull] Action<IQualityVerificationResult> saveAction,
			[CanBeNull] string title)
		{
			var qaProgressViewmodel =
				new VerificationProgressViewModel
				{
					ProgressTracker = progressTracker,
					VerificationAction = verificationAction,
					ShowReportAction = showReportAction,
					SaveAction = saveAction
				};

			VerificationProgressWpfControl progressControl = new VerificationProgressWpfControl();
			progressControl.SetDataSource(qaProgressViewmodel);

			var progressForm = new WpfHostingWinForm(progressControl);

			progressForm.FixedHeight = true;

			progressForm.Text = title;

			return progressForm;
		}
	}
}
