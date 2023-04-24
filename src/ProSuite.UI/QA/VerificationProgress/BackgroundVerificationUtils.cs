using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Progress;
using ProSuite.Commons.UI.WinForms;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.UI.QA.VerificationProgress
{
	public static class BackgroundVerificationUtils
	{
		/// <summary>
		/// Creates the verification progress form that will start the verification once
		/// it is shown. 
		/// </summary>
		/// <param name="qaClient">The client endpoint</param>
		/// <param name="verificationRun">The verification run</param>
		/// <param name="appController">The application controller to use for functionality
		/// during and after the background verification.</param>
		/// <param name="title"></param>
		/// <returns></returns>
		public static WpfHostingWinForm CreateVerificationProgressForm(
			[NotNull] QualityVerificationGrpc.QualityVerificationGrpcClient qaClient,
			[NotNull] BackgroundVerificationRun verificationRun,
			[NotNull] IApplicationBackgroundVerificationController appController,
			[CanBeNull] string title)
		{
			bool provideClientData = verificationRun.VerificationDataProvider != null;

			async Task<ServiceCallStatus> VerificationAction()
			{
				return await verificationRun.ExecuteAndProcessMessagesAsync(
					       qaClient, provideClientData);
			}

			var qaProgressViewmodel =
				new VerificationProgressViewModel
				{
					ProgressTracker = verificationRun.Progress,
					VerificationAction = VerificationAction,
					ApplicationController = appController
				};

			VerificationProgressWpfControl progressControl = new VerificationProgressWpfControl();
			progressControl.SetDataSource(qaProgressViewmodel);

			var progressForm = new WpfHostingWinForm(progressControl);

			progressForm.FixedHeight = true;

			progressForm.Text = title;

			progressForm.SetMinimumSize(275, 0);
			progressForm.SetMaximumSize(900, int.MaxValue);

			return progressForm;
		}
	}
}
