using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.Core.Workflow;
using Dataset = ProSuite.DomainModel.Core.DataModel.Dataset;

namespace ProSuite.DomainModel.AGP.Workflow
{
	public interface IMapBasedSessionContext
	{
		/// <summary>
		/// Whether the data dictionary can be accessed or not (by the microservices).
		/// </summary>
		bool DdxAccessDisabled { get; }

		///// <summary>
		///// The currently active edit context / work unit.
		///// </summary>
		[CanBeNull]
		IEditContext EditContext { get; }

		/// <summary>
		/// The currently active project workspace.
		/// </summary>
		[CanBeNull]
		ProjectWorkspace ProjectWorkspace { get; }

		event EventHandler ProjectWorkspaceChanged;

		[CanBeNull]
		IQualityVerificationEnvironment VerificationEnvironment { get; }

		event EventHandler QualitySpecificationsRefreshed;

		Task<List<ProjectWorkspace>> GetProjectWorkspaceCandidates(
			[NotNull] ICollection<Table> objectClasses);

		Task<bool> TrySelectProjectWorkspaceFromFocusMapAsync();

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

		/// <summary>
		/// Adds details, such as
		/// - Attributes
		/// - Object categories
		/// - Association Ends and referenced associations
		/// to the specified datasets.
		/// Consider encapsulating DDX-methods in a separate interface (sessionContext.Ddx...)
		/// </summary>
		/// <param name="toDatasets"></param>
		void AddDatasetDetails(IList<Dataset> toDatasets);
	}
}
