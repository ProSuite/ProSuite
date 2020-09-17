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
			QualityVerificationGrpc.QualityVerificationGrpcClient qaClient,
			VerificationRequest request,
			BackgroundVerificationRun verificationRun)
		{
			async Task<ServiceCallStatus> VerificationAction()
			{
				return await verificationRun.ExecuteAndProcessMessagesAsync(
					       qaClient, request);
			}

			var progressForm = CreateVerificationProgressForm(
				verificationRun.Progress, VerificationAction, verificationRun.ShowReportAction,
				verificationRun.SaveAction);

			progressForm.SetMinimumSize(275, 0);
			progressForm.SetMaximumSize(900, int.MaxValue);

			return progressForm;
		}

		public static WpfHostingWinForm CreateVerificationProgressForm(
			IQualityVerificationProgressTracker progressTracker,
			Func<Task<ServiceCallStatus>> verificationAction,
			[CanBeNull] Action<QualityVerification> showReportAction,
			Action<IQualityVerificationResult> saveAction)
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

			return progressForm;
		}
	}
}
