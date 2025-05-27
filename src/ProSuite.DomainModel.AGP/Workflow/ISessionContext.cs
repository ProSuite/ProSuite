using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AGP.QA;

namespace ProSuite.DomainModel.AGP.Workflow
{
	public interface ISessionContext
	{
		/// <summary>
		/// The currently active project workspace.
		/// </summary>
		ProjectWorkspace ProjectWorkspace { get; }

		event EventHandler ProjectWorkspaceChanged;

		IQualityVerificationEnvironment VerificationEnvironment { get; }

		event EventHandler QualitySpecificationsRefreshed;

		/// <summary>
		/// Whether the current quality specification can be verified or not and, if it cannot
		/// be verified the reason why.
		/// </summary>
		/// <param name="reason"></param>
		/// <returns></returns>
		bool CanVerifyQuality(out string reason);

		/// <summary>
		/// Sets the <see cref="ProjectWorkspace"/> and triggers the
		/// <see cref="ProjectWorkspaceChanged"/> event, unless the current project workspace
		/// references the same project and workspace and contains all the datasets of the new
		/// project workspace.
		/// </summary>
		/// <param name="newProjectWorkspace">The new project workspace</param>
		/// <param name="forceChange">Whether the new project workspace should be set as is and
		/// the change events should be fired in every case.</param>
		void SetProjectWorkspace([CanBeNull] ProjectWorkspace newProjectWorkspace,
		                         bool forceChange = false);
	}
}
