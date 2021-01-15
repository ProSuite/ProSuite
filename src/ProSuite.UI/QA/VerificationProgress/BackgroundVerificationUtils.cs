using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
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
		/// <summary>
		/// Creates the verification progress form that will start the verification once
		/// it is shown. 
		/// </summary>
		/// <param name="qaClient">The client endpoint</param>
		/// <param name="verificationRun">The verification run</param>
		/// <param name="openErrorWorklist">Optional command that should open the work list</param>
		/// <param name="zoomToPerimeter">Optional command that should zoom to the verified perimeter</param>
		/// <param name="flashTileProgressAction">The action to flash the current progress. The list of
		/// envelopes represents the processed tiles. The last entry is the currently processing tile.</param>
		/// <param name="title"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static WpfHostingWinForm CreateVerificationProgressForm(
			[NotNull] QualityVerificationGrpc.QualityVerificationGrpcClient qaClient,
			[NotNull] BackgroundVerificationRun verificationRun,
			[CanBeNull] ICommand openErrorWorklist,
			[CanBeNull] ICommand zoomToPerimeter,
			[CanBeNull] Action<IList<EnvelopeXY>> flashTileProgressAction,
			[CanBeNull] string title)
		{
			async Task<ServiceCallStatus> VerificationAction()
			{
				return await verificationRun.ExecuteAndProcessMessagesAsync(qaClient);
			}

			var progressForm = CreateVerificationProgressForm(
				verificationRun.Progress, VerificationAction, verificationRun.ShowReportAction,
				verificationRun.SaveAction, openErrorWorklist, zoomToPerimeter,
				flashTileProgressAction,
				title);

			progressForm.SetMinimumSize(275, 0);
			progressForm.SetMaximumSize(900, int.MaxValue);

			return progressForm;
		}

		public static WpfHostingWinForm CreateVerificationProgressForm(
			[NotNull] IQualityVerificationProgressTracker progressTracker,
			[NotNull] Func<Task<ServiceCallStatus>> verificationAction,
			[CanBeNull] Action<QualityVerification> showReportAction,
			[CanBeNull] Action<IQualityVerificationResult> saveAction,
			[CanBeNull] ICommand openErrorWorklist,
			[CanBeNull] ICommand zoomToPerimeter,
			[CanBeNull] Action<IList<EnvelopeXY>> flashTileProgressAction,
			[CanBeNull] string title)
		{
			var qaProgressViewmodel =
				new VerificationProgressViewModel
				{
					ProgressTracker = progressTracker,
					VerificationAction = verificationAction,
					ShowReportAction = showReportAction,
					SaveAction = saveAction,
					OpenWorkListCommand = openErrorWorklist,
					ZoomToPerimeterCommand = zoomToPerimeter,
					FlashProgressAction = flashTileProgressAction
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
