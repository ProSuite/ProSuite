using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.Workflow
{
	public interface ISessionContext
	{
		/// <summary>
		/// The currently active project workspace.
		/// </summary>
		IProjectWorkspace ProjectWorkspace { get; }

		event EventHandler ProjectWorkspaceChanged;

		/// <summary>
		/// The currently active edit context / work unit. TODO: Replace with
		/// dedicated IDatasetContext implementation for ProjectWorkspaces.
		/// </summary>
		[CanBeNull]
		IEditContext EditContext { get; }
	}
}
