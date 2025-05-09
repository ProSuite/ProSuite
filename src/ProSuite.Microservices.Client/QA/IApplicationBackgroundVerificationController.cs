using System.Collections.Generic;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;

namespace ProSuite.Microservices.Client.QA
{
	/// <summary>
	/// Encapsulates application-specific functionality in the application or domain
	/// layer, such as flashing the current tile on the map or opening the work list after a
	/// successful run.
	/// </summary>
	public interface IApplicationBackgroundVerificationController
	{
		/// <summary>
		/// Flashes the progress of the verification by highlighting the tiles that have been
		/// processed and the currently processing tile.
		/// </summary>
		/// <param name="tiles">The processed tiles. The last entry in the list is the currently
		/// processing tile.</param>
		/// <param name="currentProgressStep">The current process step</param>
		void FlashProgress([NotNull] IList<EnvelopeXY> tiles,
		                   ServiceCallStatus currentProgressStep);

		/// <summary>
		/// Whether or not flashing the current progress is possible or not. If it is not, the reason why
		/// it is not possible shall be provided.
		/// </summary>
		/// <param name="currentProgressStep"></param>
		/// <param name="tiles">The processed tiles. The last entry in the list is the currently
		/// processing tile.</param>
		/// <param name="reason"></param>
		/// <returns></returns>
		bool CanFlashProgress([CanBeNull] ServiceCallStatus? currentProgressStep,
		                      [NotNull] IList<EnvelopeXY> tiles,
		                      [CanBeNull] out string reason);

		/// <summary>
		/// Zooms or pans the map to the verified perimeter.
		/// </summary>
		void ZoomToVerifiedPerimeter();

		/// <summary>
		/// Whether zooming to the verified perimeter is possible or not.
		/// </summary>
		/// <param name="reason">The reason why zooming to the perimeter is not possible.</param>
		/// <returns></returns>
		bool CanZoomToVerifiedPerimeter(out string reason);

		/// <summary>
		/// Opens the work list associated with the current verification run.
		/// </summary>
		Task OpenWorkList([NotNull] IQualityVerificationResult verificationResult,
		                  bool replaceExisting);

		/// <summary>
		/// Whether opening a work list with the resulting issues is possible or not.
		/// </summary>
		/// <param name="currentProgressStep"></param>
		/// <param name="verificationResult"></param>
		/// <param name="reason">The reason why opening the work list is not possible.</param>
		/// <returns></returns>
		bool CanOpenWorkList([CanBeNull] ServiceCallStatus? currentProgressStep,
		                     [CanBeNull] IQualityVerificationResult verificationResult,
		                     out string reason);

		/// <summary>
		/// Shows a report for this verification.
		/// </summary>
		/// <param name="verificationResult"></param>
		void ShowReport([NotNull] IQualityVerificationResult verificationResult);

		/// <summary>
		/// Whether showing the report is possible or not.
		/// </summary>
		/// <param name="currentProgressStep"></param>
		/// <param name="verificationResult"></param>
		/// <param name="reason">The reason why showing the report is not possible</param>
		/// <returns></returns>
		bool CanShowReport([CanBeNull] ServiceCallStatus? currentProgressStep,
		                   [CanBeNull] IQualityVerificationResult verificationResult,
		                   out string reason);

		/// <summary>
		/// Save the issues found in this verification run.
		/// </summary>
		/// <param name="verificationResult"></param>
		/// <param name="errorDeletion"></param>
		/// <param name="updateLatestTestDate"></param>
		void SaveIssues([NotNull] IQualityVerificationResult verificationResult,
		                ErrorDeletionInPerimeter errorDeletion,
		                bool updateLatestTestDate);

		/// <summary>
		/// Save the issues found in this verification run. This might not be implemented in all platforms.
		/// </summary>
		/// <param name="verificationResult"></param>
		/// <param name="errorDeletion"></param>
		/// <param name="updateLatestTestDate"></param>
		Task<int> SaveIssuesAsync([NotNull] IQualityVerificationResult verificationResult,
		                          ErrorDeletionInPerimeter errorDeletion,
		                          bool updateLatestTestDate);

		/// <summary>
		/// Whether saving the issues is possible or not.
		/// </summary>
		/// <param name="verificationResult"></param>
		/// <param name="reason"></param>
		/// <returns></returns>
		bool CanSaveIssues([CanBeNull] IQualityVerificationResult verificationResult,
		                   out string reason);
	}
}
