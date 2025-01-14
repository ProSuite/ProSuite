using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;

namespace ProSuite.Microservices.Client.QA
{
	public class StandAloneAppVerificationController : IApplicationBackgroundVerificationController
	{
		private bool _issuesSaved;

		[CanBeNull]
		public Action<IQualityVerificationResult, ErrorDeletionInPerimeter, bool> SaveAction
		{
			get;
			set;
		}

		public void FlashProgress(IList<EnvelopeXY> tiles, ServiceCallStatus currentProgressStep)
		{
			throw new NotImplementedException();
		}

		public bool CanFlashProgress(ServiceCallStatus? currentProgressStep,
		                             IList<EnvelopeXY> tiles, out string reason)
		{
			reason = "Flashing is not supported";
			return false;
		}

		public void ZoomToVerifiedPerimeter()
		{
			throw new NotImplementedException();
		}

		public bool CanZoomToVerifiedPerimeter(out string reason)
		{
			reason = "Zooming is not supported";
			return false;
		}

		public Task OpenWorkList(IQualityVerificationResult verificationResult,
		                         bool replaceExisting)
		{
			throw new NotImplementedException();
		}

		public bool CanOpenWorkList(ServiceCallStatus? currentProgressStep,
		                            IQualityVerificationResult verificationResult,
		                            out string reason)
		{
			reason = "Work List is not supported in this context.";
			return false;
		}

		public void ShowReport(IQualityVerificationResult verificationResult)
		{
			if (verificationResult.HtmlReportPath == null)
			{
				return;
			}

			ProcessUtils.StartProcess(verificationResult.HtmlReportPath);
		}

		public bool CanShowReport(ServiceCallStatus? currentProgressStep,
		                          IQualityVerificationResult verificationResult,
		                          out string reason)
		{
			if (verificationResult == null)
			{
				reason = "Dialog has not been fully initialized";

				return false;
			}

			if (currentProgressStep == ServiceCallStatus.Running ||
			    currentProgressStep == ServiceCallStatus.Undefined)
			{
				reason = "Shows the verification report when the verification is completed";

				return false;
			}

			if (string.IsNullOrEmpty(verificationResult.HtmlReportPath))
			{
				reason = "No HTML report has been created";

				return false;
			}

			if (! File.Exists(verificationResult.HtmlReportPath))
			{
				reason =
					$"HTML report at {verificationResult.HtmlReportPath} does not exist or cannot be accessed";

				return false;
			}

			reason = null;

			return true;
		}

		public void SaveIssues(IQualityVerificationResult verificationResult,
		                       ErrorDeletionInPerimeter errorDeletion,
		                       bool updateLatestTestDate)
		{
			SaveAction?.Invoke(verificationResult, errorDeletion,
			                   updateLatestTestDate);

			_issuesSaved = true;
		}

		public Task<int> SaveIssuesAsync(IQualityVerificationResult verificationResult,
		                                 ErrorDeletionInPerimeter errorDeletion,
		                                 bool updateLatestTestDate)
		{
			throw new NotImplementedException();
		}

		public bool CanSaveIssues(IQualityVerificationResult verificationResult, out string reason)
		{
			if (SaveAction == null)
			{
				reason = "Saving is not supported";
				return false;
			}

			if (_issuesSaved)
			{
				reason = "Issues have already been saved";
				return false;
			}

			if (verificationResult == null)
			{
				reason = "No verification result";
				return false;
			}

			bool result = verificationResult.CanSaveIssues;

			reason = result ? null : "No issues have been collected";

			return result;
		}
	}
}
