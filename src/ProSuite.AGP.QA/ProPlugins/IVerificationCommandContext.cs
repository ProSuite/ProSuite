using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.Microservices.Client.QA;

namespace ProSuite.AGP.QA.ProPlugins
{
	/// <summary>
	/// Supplies the application-specific dependencies needed by <see cref="VerificationCommandRunner"/>
	/// to launch a verification: the current environment, the application's background-verification
	/// controller and any preflight checks/prompts (such as save-edits or required-specification
	/// validation) to run before the progress window is shown.
	/// </summary>
	public interface IVerificationCommandContext
	{
		[CanBeNull]
		IQualityVerificationEnvironment Environment { get; }

		[NotNull]
		IApplicationBackgroundVerificationController CreateController(
			[NotNull] MapView mapView, [CanBeNull] Geometry perimeter);

		/// <summary>
		/// Runs application-specific preflight checks (e.g. save-edits prompt, required
		/// specification validation) before the progress window is shown. Returns false
		/// (after informing the user, if appropriate) to abort the verification.
		/// </summary>
		Task<bool> PrepareAsync([NotNull] string title);
	}
}
